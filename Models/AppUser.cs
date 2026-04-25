
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class AppUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Course> TaughtCourses { get; set; } = new List<Course>();
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}