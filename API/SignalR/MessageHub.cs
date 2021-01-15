using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR {
    public class MessageHub : Hub {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;

        public MessageHub(
            IMessageRepository messageRepository, 
            IUserRepository userRepository,
            IMapper mapper,
            IHubContext<PresenceHub> presenceHub,
            PresenceTracker tracker
        ) {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _presenceHub = presenceHub;
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync() {
            // Get the other username from the url string
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);

            // Add the current connection to the Hub Group
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            // Add the current connection to the Group in the DB
            var group = await AddToGroup(groupName);
            // Send the group to the client
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _messageRepository.GetMessageThreadAsync(
                Context.User.GetUsername(), otherUser);

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception) {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto) {
            // Get the current user from ClaimPrincipal
            string username = Context.User.GetUsername();

            // Error checking on current user
            if (username == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("You cannot send a message to yourself.");

            // Get both Sender and Recipient AppUsers
            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            // Error checking on Recipient
            if (recipient == null)
                throw new HubException("Recipient with username " + createMessageDto.RecipientUsername + " could not be found.");

            // Create the Message entity
            Message message = new Message {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _messageRepository.GetMessageGroup(groupName);

            // If there are any MessageHub connections for the message receiver, set the message.DateRead to now
            if (group.Connections.Any(x => x.Username == recipient.UserName)) {
                message.DateRead = DateTime.UtcNow;
            } else {
                // Check if the user is online
                var connections = await _tracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null) {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new {
                        username = sender.UserName,
                        knownAs = sender.KnownAs
                    });
                }
            }

            // Add it to the Database, and save
            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAllAsync()) {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
        }

        private async Task<Group> AddToGroup(string groupName) {
            // Get the group from the MessageRepository, using the GroupName
            var group = await _messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            // If the group doesn't exist, create it
            if (group == null) {
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (await _messageRepository.SaveAllAsync()) return group;

            throw new HubException("Failed to join Group");
        }

        private async Task<Group> RemoveFromMessageGroup() {
            // Get the Group from the MessageRepository
            var group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);
            if (await _messageRepository.SaveAllAsync()) return group;

            throw new HubException("Failed to remove from Group");
        }

        private string GetGroupName(string caller, string other) {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}