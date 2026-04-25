
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Submission
    {
        public int SubmissionId { get; set; }
        [Required] public int AssignmentId { get; set; }
        [Required] public string StudentId { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        [ForeignKey("AssignmentId")] public virtual Assignment Assignment { get; set; }
        [ForeignKey("StudentId")] public virtual AppUser Student { get; set; }
        public virtual Grade Grade { get; set; }
    }

}