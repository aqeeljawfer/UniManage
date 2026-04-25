
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManage.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(2000)] public string Description { get; set; } = "";
        [Required, Range(1, 120)] public int Credits { get; set; }
        [Required, Range(1, 500)] public int MaxCapacity { get; set; }
        public int CurrentEnrollment { get; set; } = 0;
        [MaxLength(500)] public string Prerequisites { get; set; } = "";
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string LecturerId { get; set; } = "";
        [ForeignKey("LecturerId")] public virtual AppUser Lecturer { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        [NotMapped] public int AvailableSpaces => MaxCapacity - CurrentEnrollment;
        [NotMapped] public bool IsFullyBooked => CurrentEnrollment >= MaxCapacity;
    }
}