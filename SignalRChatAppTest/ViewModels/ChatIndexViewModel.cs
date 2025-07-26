using SignalRChatAppTest.Models;

namespace SignalRChatAppTest.ViewModels
{
    public class ChatIndexViewModel
    {
        public ApplicationUser CurrentUser { get; set; }
        public List<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    }
}
