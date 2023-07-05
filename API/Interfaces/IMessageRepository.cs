using API.DTOs;
using API.Entities;
using API.Extension;
using API.Helpers;

namespace API.Interfaces
{
    public interface IMessageRepository
    {
        void AddMessage(Message message);
        void DeleteMessage(Message message);
        Task<Message> GetMessage(int id);
        Task<PagedList<MessageDto>>GetMEssagesForUser(MessageParams messageParams);
        Task<IEnumerable<MessageDto>>GetMessageThread(string currentUserName,string  RecipientUsername);      
        Task <bool>SaveAllAsync();  

    } 
}