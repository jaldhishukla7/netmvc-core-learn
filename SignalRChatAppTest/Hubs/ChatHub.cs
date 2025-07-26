using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRChatAppTest.Data;
using SignalRChatAppTest.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SignalRChatAppTest.Hubs
{
    [Authorize]
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ApplicationDbContext _context;
        private static readonly Dictionary<string, string> _connections = new Dictionary<string, string>();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                _connections[Context.ConnectionId] = userId;

                // Update user online status
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = true;
                    await _context.SaveChangesAsync();
                }

                // Join user to their chat rooms
                var userChatRooms = await _context.ChatParticipants
                    .Where(p => p.UserId == userId)
                    .Select(p => p.ChatRoomId.ToString())
                    .ToListAsync();

                foreach (var roomId in userChatRooms)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{roomId}");
                }

                await Clients.Others.SendAsync("UserOnline", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out var userId))
            {
                _connections.Remove(Context.ConnectionId);

                // Update user offline status
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await Clients.Others.SendAsync("UserOffline", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int chatRoomId, string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            var chatMessage = new ChatMessage
            {
                Content = message,
                SenderId = userId,
                ChatRoomId = chatRoomId,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Load sender information
            var sender = await _context.Users.FindAsync(userId);

            // Send message to all participants in the chat room
            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", new
            {
                Id = chatMessage.Id,
                Content = chatMessage.Content,
                SentAt = chatMessage.SentAt,
                SenderId = chatMessage.SenderId,
                SenderName = $"{sender?.FirstName} {sender?.LastName}",
                ChatRoomId = chatRoomId
            });
        }

        public async Task JoinChatRoom(int chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
        }

        public async Task LeaveChatRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
        }

        public async Task TypingIndicator(int chatRoomId, bool isTyping)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FindAsync(userId);

            await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserTyping", new
                {
                    UserId = userId,
                    UserName = $"{user?.FirstName} {user?.LastName}",
                    IsTyping = isTyping
                });
        }
    }
}
