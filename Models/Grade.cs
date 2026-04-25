
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Grade
    {
        public int GradeId { get; set; }
        [Required] public int SubmissionId { get; set; }
        [Range(0, 100)] public int Score { get; set; }
        [MaxLength(1000)] public string Feedback { get; set; } = "";
        public DateTime GradedAt { get; set; } = DateTime.Now;
        [ForeignKey("SubmissionId")] public virtual Submission Submission { get; set; }
        [NotMapped] public string LetterGrade => Score switch { >= 70 => "A", >= 60 => "B", >= 50 => "C", >= 40 => "D", _ => "F" };
    }
}