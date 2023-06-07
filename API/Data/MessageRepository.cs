using API.DTOs;
using API.Entities;
using API.Extension;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper mapper;
        public MessageRepository(DataContext context,IMapper mapper)
        {
            this.mapper = mapper;
            _context = context;
            
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<PagedList<MessageDto>> GetMEssagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
            .OrderByDescending(x=> x.MessageSent)
            .AsQueryable();

            query=messageParams.Container switch
            {
                "Inbox"=> query.Where(u=> u.RecipientUsername == messageParams.Username),
                "Outbox"=> query.Where(u=> u.SenderUsername == messageParams.Username),
                _=> query.Where(u=> u.RecipientUsername == messageParams.Username && u.DateRead == null)

            };

            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

            return await PagedList<MessageDto>
            .CreateAsync(messages,messageParams.PageNumber,messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string RecipientUsername)
        {
            var messages = await _context.Messages
            .Include(u=> u.Sender).ThenInclude(p=>p.Photos)
            .Include(u=> u.Recipient).ThenInclude(p=>p.Photos)
            .Where(
                m=> m.RecipientUsername == currentUserName &&
                m.SenderUsername == RecipientUsername ||
                m.RecipientUsername == RecipientUsername &&
                m.SenderUsername == currentUserName
            )
            .OrderByDescending(m=> m.MessageSent)
            .ToListAsync();

            var unreadMessages = messages.Where(m=> m.DateRead == null
                && m.RecipientUsername == currentUserName).ToList();

            if(unreadMessages.Any())    
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return mapper.Map<IEnumerable<MessageDto>>(messages);

            
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync()>0;
        }
    }
}