using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.ViewModels;

namespace UniManage.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _um;
        private readonly IWebHostEnvironment _env;
        private static readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".txt", ".png", ".jpg", ".jpeg" };

        public LecturerController(AppDbContext db, UserManager<AppUser> um, IWebHostEnvironment env)
        { _db = db; _um = um; _env = env; }

        public async Task<IActionResult> Dashboard()
        {
            var uid = _um.GetUserId(User); 
            var user = await _um.GetUserAsync(User);
            var courses = await _db.Courses.Where(c => c.LecturerId == uid && !c.IsDeleted).ToListAsync();
            var cids = courses.Select(c => c.CourseId).ToList();
            var subs = await _db.Submissions.Where(s => cids.Contains(s.Assignment.CourseId)).Include(s => s.Grade).Include(s => s.Assignment).ToListAsync();
            var unread = await _db.Messages.CountAsync(m => m.ReceiverId == uid && !m.IsRead);
            var summaries = courses.Select(c => new GradingSummary { CourseName = c.Title, Total = subs.Count(s => s.Assignment.CourseId == c.CourseId), Graded = subs.Count(s => s.Assignment.CourseId == c.CourseId && s.Grade != null) }).ToList();
            return View(new LecturerDashVM { LecturerName = user != null ? user.FullName : "Lecturer", Courses = courses, TotalStudents = await _db.Enrollments.CountAsync(e => cids.Contains(e.CourseId) && e.Status == "Active"), PendingGrading = subs.Count(s => s.Grade == null), UnreadMessages = unread, Summaries = summaries });
        }

        public async Task<IActionResult> Assignments()
        {
            var uid = _um.GetUserId(User);
            var cids = await _db.Courses.Where(c => c.LecturerId == uid && !c.IsDeleted).Select(c => c.CourseId).ToListAsync();
            return View(await _db.Assignments.Where(a => cids.Contains(a.CourseId)).Include(a => a.Course).Include(a => a.Submissions).ThenInclude(s => s.Grade).OrderByDescending(a => a.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> CreateAssignment()
        {
            var uid = _um.GetUserId(User);
            ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid && !c.IsDeleted).ToListAsync();
            return View(new AssignmentVM { Deadline = DateTime.Now.AddDays(14) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> CreateAssignment(AssignmentVM vm)
        {
            // Remove AttachmentFile from model-state so it doesn't block validation
            ModelState.Remove(nameof(vm.AttachmentFile));

            if (!ModelState.IsValid)
            {
                var uid2 = _um.GetUserId(User);
                ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                return View(vm);
            }

            var assignment = new Assignment
            {
                CourseId    = vm.CourseId,
                Title       = vm.Title,
                Description = vm.Description,
                Deadline    = vm.Deadline,
                MaxScore    = vm.MaxScore
            };

            // --- handle optional reference document
            if (vm.AttachmentFile != null && vm.AttachmentFile.Length > 0)
            {
                var ext = Path.GetExtension(vm.AttachmentFile.FileName);
                if (!_allowed.Contains(ext))
                {
                    ModelState.AddModelError("AttachmentFile", $"File type '{ext}' is not allowed.");
                    var uid2 = _um.GetUserId(User);
                    ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                    return View(vm);
                }
                if (vm.AttachmentFile.Length > 20 * 1024 * 1024)
                {
                    ModelState.AddModelError("AttachmentFile", "File must be under 20 MB.");
                    var uid2 = _um.GetUserId(User);
                    ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                    return View(vm);
                }
                
                var dir = Path.Combine(_env.WebRootPath, "assignments");
                Directory.CreateDirectory(dir);
                var safeName = Path.GetFileNameWithoutExtension(vm.AttachmentFile.FileName).Replace(" ", "_") + ext;
                var fileName = $"{Guid.NewGuid()}_{safeName}";
                using (var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                    await vm.AttachmentFile.CopyToAsync(stream);

                assignment.AttachmentPath     = "/assignments/" + fileName;
                assignment.AttachmentFileName = vm.AttachmentFile.FileName;
            }

            _db.Assignments.Add(assignment);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment created.";
            return RedirectToAction("Assignments");
        }

        public async Task<IActionResult> EditAssignment(int id)
        {
            var a = await _db.Assignments.Include(x => x.Course).FirstOrDefaultAsync(x => x.AssignmentId == id);
            if (a == null) return NotFound();
            
            var uid = _um.GetUserId(User);
            ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid && !c.IsDeleted).ToListAsync();
            ViewBag.CurrentAttachment = a.AttachmentFileName;

            return View(new AssignmentVM
            {
                AssignmentId = a.AssignmentId,
                CourseId = a.CourseId,
                Title = a.Title,
                Description = a.Description,
                Deadline = a.Deadline,
                MaxScore = a.MaxScore
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> EditAssignment(AssignmentVM vm)
        {
            ModelState.Remove(nameof(vm.AttachmentFile));
            if (!ModelState.IsValid)
            {
                var uid2 = _um.GetUserId(User);
                ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                return View(vm);
            }

            var a = await _db.Assignments.FindAsync(vm.AssignmentId);
            if (a == null) return NotFound();

            a.Title = vm.Title;
            a.Description = vm.Description;
            a.Deadline = vm.Deadline;
            a.MaxScore = vm.MaxScore;
            a.CourseId = vm.CourseId;

            if (vm.AttachmentFile != null && vm.AttachmentFile.Length > 0)
            {
                var ext = Path.GetExtension(vm.AttachmentFile.FileName);
                if (!_allowed.Contains(ext))
                {
                    ModelState.AddModelError("AttachmentFile", $"File type '{ext}' is not allowed.");
                    var uid2 = _um.GetUserId(User);
                    ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                    return View(vm);
                }
                if (vm.AttachmentFile.Length > 20 * 1024 * 1024)
                {
                    ModelState.AddModelError("AttachmentFile", "File must be under 20 MB.");
                    var uid2 = _um.GetUserId(User);
                    ViewBag.Courses = await _db.Courses.Where(c => c.LecturerId == uid2 && !c.IsDeleted).ToListAsync();
                    return View(vm);
                }

                // Delete old file
                if (!string.IsNullOrEmpty(a.AttachmentPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, a.AttachmentPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var dir = Path.Combine(_env.WebRootPath, "assignments");
                Directory.CreateDirectory(dir);
                var safeName = Path.GetFileNameWithoutExtension(vm.AttachmentFile.FileName).Replace(" ", "_") + ext;
                var fileName = $"{Guid.NewGuid()}_{safeName}";
                using (var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
                    await vm.AttachmentFile.CopyToAsync(stream);

                a.AttachmentPath = "/assignments/" + fileName;
                a.AttachmentFileName = vm.AttachmentFile.FileName;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment updated.";
            return RedirectToAction("Assignments");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var a = await _db.Assignments.FindAsync(id);
            if (a == null) return NotFound();

            // Delete attachment file
            if (!string.IsNullOrEmpty(a.AttachmentPath))
            {
                var path = Path.Combine(_env.WebRootPath, a.AttachmentPath.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            // Also delete all student submissions files
            var submissions = await _db.Submissions.Where(s => s.AssignmentId == id).ToListAsync();
            foreach (var s in submissions)
            {
                if (!string.IsNullOrEmpty(s.FilePath))
                {
                    var sPath = Path.Combine(_env.WebRootPath, s.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(sPath)) System.IO.File.Delete(sPath);
                }
            }

            _db.Assignments.Remove(a);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment deleted.";
            return RedirectToAction("Assignments");
        }

        public async Task<IActionResult> Submissions(int assignmentId)
        {
            var a = await _db.Assignments.Include(x => x.Course).Include(x => x.Submissions).ThenInclude(s => s.Student).Include(x => x.Submissions).ThenInclude(s => s.Grade).FirstOrDefaultAsync(x => x.AssignmentId == assignmentId);
            if (a == null) return NotFound(); return View(a);
        }

        public async Task<IActionResult> GradeSubmission(int submissionId)
        {
            var s = await _db.Submissions.Include(x => x.Student).Include(x => x.Assignment).Include(x => x.Grade).FirstOrDefaultAsync(x => x.SubmissionId == submissionId);
            if (s == null) return NotFound();
            return View(new GradeVM { SubmissionId = s.SubmissionId, StudentName = s.Student != null ? s.Student.FullName : "", FileName = s.OriginalFileName, SubmittedAt = s.SubmittedAt, ExistingScore = s.Grade?.Score, ExistingFeedback = s.Grade?.Feedback ?? "", Score = s.Grade?.Score ?? 0, Feedback = s.Grade?.Feedback ?? "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeSubmission(GradeVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var g = await _db.Grades.FirstOrDefaultAsync(x => x.SubmissionId == vm.SubmissionId);
            if (g == null) _db.Grades.Add(new Grade { SubmissionId = vm.SubmissionId, Score = vm.Score, Feedback = vm.Feedback });
            else { g.Score = vm.Score; g.Feedback = vm.Feedback; g.GradedAt = DateTime.Now; }
            await _db.SaveChangesAsync(); TempData["Success"] = "Grade saved: " + vm.Score + "/100";
            var sub = await _db.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.SubmissionId == vm.SubmissionId);
            return RedirectToAction("Submissions", new { assignmentId = sub != null ? sub.AssignmentId : 0 });
        }

        public async Task<IActionResult> CourseStudents(int courseId)
        {
            var c = await _db.Courses.FindAsync(courseId); if (c == null) return NotFound();
            ViewBag.Course = c;
            return View(await _db.Enrollments.Where(e => e.CourseId == courseId && e.Status == "Active").Include(e => e.Student).ToListAsync());
        }
    }
}
