
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        [Required] public string StudentId { get; set; } = "";
        [Required] public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active";
        [ForeignKey("StudentId")] public virtual AppUser Student { get; set; }
        [ForeignKey("CourseId")] public virtual Course Course { get; set; }
    }
}