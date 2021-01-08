using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers {
    [Authorize]
    public class MessagesController : BaseApiController {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        public MessagesController(
            IUserRepository userRepository, 
            IMessageRepository messageRepository, 
            IMapper mapper
        ) {
            _mapper = mapper;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto) {
            // Get the current user from ClaimPrincipal
            string username = User.GetUsername();

            // Error checking on current user
            if (username == createMessageDto.RecipientUsername.ToLower())
                return BadRequest("You cannot send a message to yourself.");

            // Get both Sender and Recipient AppUsers
            AppUser sender = await _userRepository.GetUserByUsernameAsync(username);
            AppUser recipient = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            // Error checking on Recipient
            if (recipient == null)
                return NotFound("Recipient with username " + createMessageDto.RecipientUsername + " could not be found.");

            // Create the Message entity
            Message message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            // Add it to the Database, and save
            _messageRepository.AddMessage(message);
            if (await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(
            [FromQuery]MessageParams messageParams
        ) {
            // Set the Username using ClaimsPrincipal
            messageParams.Username = User.GetUsername();

            // Get the messages for the user using the MessageRepository
            var messages = await _messageRepository.GetMessagesForUserAsync(messageParams);

            // Add the Pagination HTTP Header
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return Ok(messages);
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username) {
            // Get the current Username from ClaimsPrincipal
            var currentUsername = User.GetUsername();

            return Ok(await _messageRepository.GetMessageThreadAsync(currentUsername, username.ToLower()));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            // Get the current Username from ClaimsPrincipal
            var username = User.GetUsername();

            // Get the message using the Id
            var message = await _messageRepository.GetMessageAsync(id);

            if (message == null) return NotFound("Message with Id " + id + " not found.");

            if (message.SenderUsername != username && message.RecipientUsername != username) 
                return Unauthorized("This message does not belong to you");

            // Set either deleted flags
            if (message.SenderUsername == username) message.SenderDeleted = true;
            if (message.RecipientUsername == username) message.RecipientDeleted = true;

            if (message.SenderDeleted && message.RecipientDeleted) _messageRepository.DeleteMessage(message);

            var repoSaved = await _messageRepository.SaveAllAsync();

            if (repoSaved) return Ok();

            return BadRequest("Error while deleting message");
        }
    }
}