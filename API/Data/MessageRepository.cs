using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
    public class MessageRepository : IMessageRepository {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper) {
            _mapper = mapper;
            _context = context;
        }

        public void AddMessage(Message message) {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message) {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessageAsync(int id) {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams) {
            // Get the most recent messages as a query
            var query = _context.Messages
                .OrderByDescending(m => m.MessageSent)
                .AsQueryable();

            // Filter the messages by the container content
            query = messageParams.Container.ToLower() switch
            {
                "inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username && u.RecipientDeleted == false),
                "outbox" => query.Where(u => u.Sender.UserName == messageParams.Username && u.SenderDeleted == false),
                _ => query.Where(u => u.Recipient.UserName == messageParams.Username && u.DateRead == null && u.RecipientDeleted == false)
            };

            // Map the query to a MessageDto
            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            // Return a PagedList using the query
            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThreadAsync(string currentUsername, string recipientUsername) {
            // Get the messages that relate to the sender or recipient and vice versa
            var messages = await _context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(
                    m => m.RecipientUsername == currentUsername 
                    && m.SenderUsername == recipientUsername
                    && m.RecipientDeleted == false
                    || m.SenderUsername == currentUsername 
                    && m.RecipientUsername == recipientUsername
                    && m.SenderDeleted == false
                ).OrderBy(m => m.MessageSent)
                .ToListAsync();
            
            // Get the unread messages as a list, and set the DateRead property on them to now
            var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUsername == currentUsername).ToList();
            if (unreadMessages.Any()) {
                foreach (var message in unreadMessages) {
                    message.DateRead = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            // Map the messages to MessageDtos and return
            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync() {
            return await _context.SaveChangesAsync() > 0;
        }

        public void AddGroup(Group group) {
            _context.Groups.Add(group);
        }

        public async Task<Connection> GetConnection(string connectionId) {
            return await _context.Connnections.FindAsync(connectionId);
        }

        public void RemoveConnection(Connection connection) {
            _context.Connnections.Remove(connection);
        }

        public async Task<Group> GetMessageGroup(string groupName) {
            return await _context.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<Group> GetGroupForConnection(string connectionId) {
            return await _context.Groups
                .Include(c => c.Connections)
                .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }
    }
}