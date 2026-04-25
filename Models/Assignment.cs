
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Assignment
    {
        public int AssignmentId { get; set; }
        [Required] public int CourseId { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(2000)] public string Description { get; set; } = "";

        //edited
        //public string? FilePath { get; set; }
        //public string? FileName { get; set; }

        [Required] public DateTime Deadline { get; set; }
        public int MaxScore { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Lecturer-uploaded reference document
        public string? AttachmentPath { get; set; }
        public string? AttachmentFileName { get; set; }
        [ForeignKey("CourseId")] public virtual Course Course { get; set; }
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        [NotMapped] public bool IsOverdue => DateTime.Now > Deadline;
    }
}