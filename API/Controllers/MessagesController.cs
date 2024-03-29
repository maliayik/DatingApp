using API.DTOs;
using API.Entities;
using API.Extension;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController:BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IMapper mapper;
        public MessagesController(IUserRepository userRepository,IMessageRepository messageRepository,IMapper mapper)
        {
            this.mapper = mapper;
            this.messageRepository = messageRepository;
            this.userRepository = userRepository;
            
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUserName();

            if (username == createMessageDto.RecipientUsername.ToLower())
                return BadRequest("You cannot send messages to yourself");

            var sender = await userRepository.GetUserByUsernameAsync(username);
            var recipient = await userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            messageRepository.AddMessage(message);

            if (await messageRepository.SaveAllAsync()) return Ok(mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message");
         }  

         [HttpGet]
         public async Task<ActionResult<PagedList<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
         {
            messageParams.Username = User.GetUserName();
            
            var messages = await messageRepository.GetMEssagesForUser(messageParams);

            Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage,messages.PageSize,messages.TotalCount,messages.TotalPage));

            return messages;
         }

         [HttpGet("thread/{username}")]
         public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
         {
            var currentUserName = User.GetUserName();

            return Ok(await messageRepository.GetMessageThread(currentUserName,username));
            
         }

         [HttpDelete("{id}")]
         public async Task<ActionResult> DeleteMessage(int id)
         {
            var username = User.GetUserName();

            var message= await messageRepository.GetMessage(id);

            if(message.SenderUsername != username && message.RecipientUsername != username)
             return Unauthorized();

             if(message.SenderUsername == username) message.SenderDeleted = true;
             if(message.RecipientUsername == username) message.RecipientDeleted = true;

             if(message.SenderDeleted && message.RecipientDeleted)
             {
                messageRepository.DeleteMessage(message);
             }

            if(await messageRepository.SaveAllAsync()) return Ok();

            return BadRequest("Problem deleting the message");

         }     
         
    }

        
}