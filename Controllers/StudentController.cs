using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.ViewModels;

namespace UniManage.Controllers
{
    [Authorize(Roles="Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _um;
        private readonly IWebHostEnvironment _env;
        private static readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx", ".zip", ".txt", ".png", ".jpg", ".jpeg" };

        public StudentController(AppDbContext db, UserManager<AppUser> um, IWebHostEnvironment env)
        { _db = db; _um = um; _env = env; }

        public async Task<IActionResult> Dashboard(){
            var uid=_um.GetUserId(User);
            var user=await _um.GetUserAsync(User);
            var enr=await _db.Enrollments.Where(e=>e.StudentId==uid&&e.Status=="Active").Include(e=>e.Course).ToListAsync();
            var cids=enr.Select(e=>e.CourseId).ToList();
            var upcoming=await _db.Assignments.Where(a=>cids.Contains(a.CourseId)&&a.Deadline>=DateTime.Now).OrderBy(a=>a.Deadline).Take(5).Include(a=>a.Course).ToListAsync();
            var grades=await _db.Grades.Where(g=>g.Submission.StudentId==uid).OrderByDescending(g=>g.GradedAt).Take(5).Include(g=>g.Submission).ThenInclude(s=>s.Assignment).ThenInclude(a=>a.Course).ToListAsync();
            var unread=await _db.Messages.CountAsync(m=>m.ReceiverId==uid&&!m.IsRead);
            return View(new StudentDashVM{StudentName=user!=null?user.FullName:"Student",Enrollments=enr,UpcomingAssignments=upcoming,RecentGrades=grades,UnreadMessages=unread});
        }

        public async Task<IActionResult> BrowseCourses(string search=""){
            var uid=_um.GetUserId(User);
            var enrolled=await _db.Enrollments.Where(e=>e.StudentId==uid&&e.Status=="Active").ToDictionaryAsync(e=>e.CourseId, e=>e.EnrollmentId);
            var q=_db.Courses.Where(c=>!c.IsDeleted).Include(c=>c.Lecturer).AsQueryable();
            if(!string.IsNullOrWhiteSpace(search)) q=q.Where(c=>c.Title.Contains(search)||c.Description.Contains(search));
            ViewBag.EnrolledIds=enrolled; ViewBag.Search=search;
            return View(await q.ToListAsync());
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId){
            var uid=_um.GetUserId(User);
            using var tx=await _db.Database.BeginTransactionAsync();
            try{
                var course=await _db.Courses.FirstOrDefaultAsync(c=>c.CourseId==courseId&&!c.IsDeleted);
                if(course==null) return NotFound();
                if(course.CurrentEnrollment>=course.MaxCapacity){TempData["Error"]="Course is fully booked.";return RedirectToAction("BrowseCourses");}
                if(await _db.Enrollments.AnyAsync(e=>e.CourseId==courseId&&e.StudentId==uid&&e.Status=="Active")){TempData["Error"]="Already enrolled.";return RedirectToAction("BrowseCourses");}
                _db.Enrollments.Add(new Enrollment{StudentId=uid,CourseId=courseId,Status="Active"});
                course.CurrentEnrollment++;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"]="Enrolled in "+course.Title+"!";
            }catch{await tx.RollbackAsync();TempData["Error"]="Enrollment failed.";}
            return RedirectToAction("Dashboard");
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int enrollmentId){
            var uid=_um.GetUserId(User);
            var e=await _db.Enrollments.Include(x=>x.Course).FirstOrDefaultAsync(x=>x.EnrollmentId==enrollmentId&&x.StudentId==uid);
            if(e==null) return NotFound();
            e.Status="Withdrawn";
            if(e.Course!=null) e.Course.CurrentEnrollment--;
            await _db.SaveChangesAsync();
            TempData["Success"]="Withdrawn from course.";
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Assignments(){
            var uid=_um.GetUserId(User);
            var cids=await _db.Enrollments.Where(e=>e.StudentId==uid&&e.Status=="Active").Select(e=>e.CourseId).ToListAsync();
            var list=await _db.Assignments.Where(a=>cids.Contains(a.CourseId)).Include(a=>a.Course).Include(a=>a.Submissions.Where(s=>s.StudentId==uid)).ThenInclude(s=>s.Grade).OrderBy(a=>a.Deadline).ToListAsync();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, IFormFile file)
        {
            var uid = _um.GetUserId(User);

            // --- validate file presence
            if (file == null || file.Length == 0)
            { TempData["Error"] = "Please select a file."; return RedirectToAction("Assignments"); }

            // --- validate size (also enforced by [RequestSizeLimit])
            if (file.Length > 10 * 1024 * 1024)
            { TempData["Error"] = "File must be under 10 MB."; return RedirectToAction("Assignments"); }

            // --- validate extension
            var ext = Path.GetExtension(file.FileName);
            if (!_allowed.Contains(ext))
            { TempData["Error"] = $"File type '{ext}' is not allowed. Accepted: pdf, doc, docx, zip, txt, png, jpg"; return RedirectToAction("Assignments"); }

            // --- check assignment exists and deadline not passed
            var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
            if (assignment == null) return NotFound();
            if (assignment.IsOverdue)
            { TempData["Error"] = "The deadline for this assignment has passed."; return RedirectToAction("Assignments"); }

            // --- build safe path inside wwwroot/uploads
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var safeOriginal = Path.GetFileNameWithoutExtension(file.FileName)
                                   .Replace(" ", "_") + ext;
            var fileName = $"{Guid.NewGuid()}_{safeOriginal}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            // --- save file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            // --- remove old submission record (+ its file) if exists
            var existing = await _db.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == uid);
            if (existing != null)
            {
                // delete old file from disk if it exists
                var oldFilePath = Path.Combine(_env.WebRootPath, existing.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
                _db.Submissions.Remove(existing);
            }

            // --- save new submission
            _db.Submissions.Add(new Submission
            {
                AssignmentId = assignmentId,
                StudentId    = uid,
                FilePath     = "/uploads/" + fileName,
                OriginalFileName = file.FileName,
                SubmittedAt  = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Assignment submitted successfully!";
            return RedirectToAction("Assignments");
        }

        public async Task<IActionResult> Grades(){
            var uid=_um.GetUserId(User);
            var g=await _db.Grades.Where(x=>x.Submission.StudentId==uid).Include(x=>x.Submission).ThenInclude(s=>s.Assignment).ThenInclude(a=>a.Course).OrderByDescending(x=>x.GradedAt).ToListAsync();
            return View(g);
        }
    }
}
