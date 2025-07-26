using System.ComponentModel.DataAnnotations;

namespace SignalRChatAppTest.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedById { get; set; }

        public virtual ApplicationUser? CreatedBy { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    }
}
