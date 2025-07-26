using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalRChatAppTest.Data;
using SignalRChatAppTest.Models;
using SignalRChatAppTest.ViewModels;
using System.Security.Claims;

namespace SignalRChatAppTest.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(userId);

            var chatRooms = await _context.ChatRooms
                .Where(r => r.Participants.Any(p => p.UserId == userId))
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.Messages.OrderByDescending(m => m.SentAt).Take(1))
                    .ThenInclude(m => m.Sender)
                .ToListAsync();

            var viewModel = new ChatIndexViewModel
            {
                CurrentUser = user,
                ChatRooms = chatRooms
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var isParticipant = await _context.ChatParticipants
                .AnyAsync(p => p.ChatRoomId == id && p.UserId == userId);

            if (!isParticipant)
                return NotFound();

            var chatRoom = await _context.ChatRooms
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (chatRoom == null)
                return NotFound();

            return View(chatRoom);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrGetChat([FromBody] CreateChatRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (request.ParticipantIds.Count == 1)
            {
                var otherUserId = request.ParticipantIds.First();

                var existingChat = await _context.ChatRooms
                    .Where(r => !r.IsGroup && r.Participants.Count == 2)
                    .Where(r => r.Participants.Any(p => p.UserId == currentUserId) &&
                                r.Participants.Any(p => p.UserId == otherUserId))
                    .FirstOrDefaultAsync();

                if (existingChat != null)
                {
                    return Json(new { chatRoomId = existingChat.Id });
                }
            }

            var chatRoom = new ChatRoom
            {
                Name = request.Name ?? (request.ParticipantIds.Count == 1 ? "Private Chat" : "Group Chat"),
                IsGroup = request.ParticipantIds.Count > 1,
                CreatedById = currentUserId
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatRoomId = chatRoom.Id,
                UserId = currentUserId
            });

            foreach (var participantId in request.ParticipantIds)
            {
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    ChatRoomId = chatRoom.Id,
                    UserId = participantId
                });
            }

            await _context.SaveChangesAsync();

            return Json(new { chatRoomId = chatRoom.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var users = await _context.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => new
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    Email = u.Email,
                    IsOnline = u.IsOnline
                })
                .ToListAsync();

            return Json(users);
        }
    }
}
