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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public MessagesController(IMapper mapper, IUnitOfWork unitOfWork) {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(
            [FromQuery] MessageParams messageParams
        ) {
            // Set the Username using ClaimsPrincipal
            messageParams.Username = User.GetUsername();

            // Get the messages for the user using the MessageRepository
            var messages = await _unitOfWork.MessageRepository.GetMessagesForUserAsync(messageParams);

            // Add the Pagination HTTP Header
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            // Get the current Username from ClaimsPrincipal
            var username = User.GetUsername();

            // Get the message using the Id
            var message = await _unitOfWork.MessageRepository.GetMessageAsync(id);

            if (message == null) return NotFound("Message with Id " + id + " not found.");

            if (message.SenderUsername != username && message.RecipientUsername != username)
                return Unauthorized("This message does not belong to you");

            // Set either deleted flags
            if (message.SenderUsername == username) message.SenderDeleted = true;
            if (message.RecipientUsername == username) message.RecipientDeleted = true;

            if (message.SenderDeleted && message.RecipientDeleted) _unitOfWork.MessageRepository.DeleteMessage(message);

            var repoSaved = await _unitOfWork.Complete();

            if (repoSaved) return Ok();

            return BadRequest("Error while deleting message");
        }
    }
}