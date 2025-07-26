using System.ComponentModel.DataAnnotations;

namespace SignalRChatAppTest.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }

        [Required]
        public string SenderId { get; set; }

        public int ChatRoomId { get; set; }

        public virtual ApplicationUser Sender { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }
    }
}
