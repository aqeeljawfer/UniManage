
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        [Required] public string SenderId { get; set; } = "";
        [Required] public string ReceiverId { get; set; } = "";
        [Required, MaxLength(200)] public string Subject { get; set; } = "";
        [Required, MaxLength(4000)] public string Body { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public int? ParentMessageId { get; set; }
        [ForeignKey("SenderId")] public virtual AppUser Sender { get; set; }
        [ForeignKey("ReceiverId")] public virtual AppUser Receiver { get; set; }
    }
}