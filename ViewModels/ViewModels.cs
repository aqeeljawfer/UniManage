using System.ComponentModel.DataAnnotations;
using UniManage.Models;

namespace UniManage.ViewModels
{
    public class LoginVM { [Required,EmailAddress] public string Email{get;set;}=""; [Required,DataType(DataType.Password)] public string Password{get;set;}=""; public bool RememberMe{get;set;} }
    public class RegisterVM { [Required,MaxLength(100)] public string FullName{get;set;}=""; [Required,EmailAddress] public string Email{get;set;}=""; [Required,DataType(DataType.Password),MinLength(6)] public string Password{get;set;}=""; [DataType(DataType.Password),Compare("Password")] public string ConfirmPassword{get;set;}=""; [Required] public string Role{get;set;}="Student"; }

    public class StudentDashVM { public string StudentName{get;set;}=""; public List<Enrollment> Enrollments{get;set;}=new(); public List<Assignment> UpcomingAssignments{get;set;}=new(); public List<Grade> RecentGrades{get;set;}=new(); public int UnreadMessages{get;set;} }
    public class LecturerDashVM { public string LecturerName{get;set;}=""; public List<Course> Courses{get;set;}=new(); public int TotalStudents{get;set;} public int PendingGrading{get;set;} public int UnreadMessages{get;set;} public List<GradingSummary> Summaries{get;set;}=new(); }
    public class GradingSummary { public string CourseName{get;set;}=""; public int Total{get;set;} public int Graded{get;set;} public int Pending=>Total-Graded; }
    public class AdminDashVM { public int TotalStudents{get;set;} public int TotalLecturers{get;set;} public int TotalCourses{get;set;} public int TotalEnrollments{get;set;} public List<Course> RecentCourses{get;set;}=new(); public List<EnrollStat> Stats{get;set;}=new(); }
    public class EnrollStat { public string CourseName{get;set;}=""; public int Count{get;set;} }

    public class CourseVM {
        public int CourseId{get;set;}
        [Required,MaxLength(200)] public string Title{get;set;}="";
        [MaxLength(2000)] public string Description{get;set;}="";
        [Required,Range(1,120)] public int Credits{get;set;}
        [Required,Range(1,500)] public int MaxCapacity{get;set;}
        public string Prerequisites{get;set;}="";
        public string LecturerId{get;set;}="";
        public List<AppUser> AvailableLecturers{get;set;}=new();
    }

    public class AssignmentVM {
        public int AssignmentId{get;set;}
        [Required] public int CourseId{get;set;}
        public string CourseName{get;set;}="";
        [Required,MaxLength(200)] public string Title{get;set;}="";
        [MaxLength(2000)] public string Description{get;set;}="";
        [Required] public DateTime Deadline{get;set;}=DateTime.Now.AddDays(14);
        public int MaxScore{get;set;}=100;
        // Optional lecturer reference document
        public IFormFile? AttachmentFile{get;set;}
    }

    public class GradeVM {
        public int SubmissionId{get;set;}
        public string StudentName{get;set;}="";
        public string FileName{get;set;}="";
        public DateTime SubmittedAt{get;set;}
        public int? ExistingScore{get;set;}
        public string ExistingFeedback{get;set;}="";
        [Required,Range(0,100)] public int Score{get;set;}
        [MaxLength(1000)] public string Feedback{get;set;}="";
    }

    public class ComposeVM {
        [Required] public string ReceiverId{get;set;}="";
        [Required,MaxLength(200)] public string Subject{get;set;}="";
        [Required,MaxLength(4000)] public string Body{get;set;}="";
        public int? ParentMessageId{get;set;}
        public List<AppUser> Recipients{get;set;}=new();
    }
}
