using System.ComponentModel.DataAnnotations;

namespace SignalRChatAppTest.Models
{
    public class ChatParticipant
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int ChatRoomId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }
    }
}
