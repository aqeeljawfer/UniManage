using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.ViewModels;

namespace UniManage.Controllers
{
    [Authorize(Roles="Administrator")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _um;
        public AdminController(AppDbContext db,UserManager<AppUser> um){_db=db;_um=um;}

        public async Task<IActionResult> Dashboard(){
            var students=await _um.GetUsersInRoleAsync("Student");
            var lecturers=await _um.GetUsersInRoleAsync("Lecturer");
            var stats=await _db.Enrollments.Where(e=>e.Status=="Active").GroupBy(e=>e.Course.Title).Select(g=>new EnrollStat{CourseName=g.Key,Count=g.Count()}).OrderByDescending(x=>x.Count).Take(6).ToListAsync();
            return View(new AdminDashVM{TotalStudents=students.Count,TotalLecturers=lecturers.Count,TotalCourses=await _db.Courses.CountAsync(c=>!c.IsDeleted),TotalEnrollments=await _db.Enrollments.CountAsync(e=>e.Status=="Active"),RecentCourses=await _db.Courses.Where(c=>!c.IsDeleted).OrderByDescending(c=>c.CreatedAt).Take(5).Include(c=>c.Lecturer).ToListAsync(),Stats=stats});
        }

        public async Task<IActionResult> Courses() => View(await _db.Courses.Where(c=>!c.IsDeleted).Include(c=>c.Lecturer).OrderByDescending(c=>c.CreatedAt).ToListAsync());

        public async Task<IActionResult> CreateCourse() => View(new CourseVM{AvailableLecturers=(await _um.GetUsersInRoleAsync("Lecturer")).ToList()});

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CourseVM vm){
            if(!ModelState.IsValid){vm.AvailableLecturers=(await _um.GetUsersInRoleAsync("Lecturer")).ToList();return View(vm);}
            _db.Courses.Add(new Course{Title=vm.Title,Description=vm.Description,Credits=vm.Credits,MaxCapacity=vm.MaxCapacity,Prerequisites=vm.Prerequisites,LecturerId=vm.LecturerId});
            await _db.SaveChangesAsync();TempData["Success"]="Course created.";return RedirectToAction("Courses");
        }

        public async Task<IActionResult> EditCourse(int id){
            var c=await _db.Courses.FindAsync(id);if(c==null) return NotFound();
            return View(new CourseVM{CourseId=c.CourseId,Title=c.Title,Description=c.Description,Credits=c.Credits,MaxCapacity=c.MaxCapacity,Prerequisites=c.Prerequisites,LecturerId=c.LecturerId,AvailableLecturers=(await _um.GetUsersInRoleAsync("Lecturer")).ToList()});
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(CourseVM vm){
            if(!ModelState.IsValid){vm.AvailableLecturers=(await _um.GetUsersInRoleAsync("Lecturer")).ToList();return View(vm);}
            var c=await _db.Courses.FindAsync(vm.CourseId);if(c==null) return NotFound();
            c.Title=vm.Title;c.Description=vm.Description;c.Credits=vm.Credits;c.MaxCapacity=vm.MaxCapacity;c.Prerequisites=vm.Prerequisites;c.LecturerId=vm.LecturerId;
            await _db.SaveChangesAsync();TempData["Success"]="Course updated.";return RedirectToAction("Courses");
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id){
            var c=await _db.Courses.FindAsync(id);if(c!=null){c.IsDeleted=true;await _db.SaveChangesAsync();}
            TempData["Success"]="Course deleted.";return RedirectToAction("Courses");
        }

        public IActionResult Users() => View(_um.Users.ToList());

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id){
            var u=await _um.FindByIdAsync(id);if(u!=null) await _um.DeleteAsync(u);
            TempData["Success"]="User removed.";return RedirectToAction("Users");
        }

        public async Task<IActionResult> Reports(){
            var stats=await _db.Enrollments.Where(e=>e.Status=="Active").GroupBy(e=>e.Course.Title).Select(g=>new{Course=g.Key,Count=g.Count()}).OrderByDescending(x=>x.Count).ToListAsync();
            ViewBag.Labels=Newtonsoft.Json.JsonConvert.SerializeObject(stats.Select(s=>s.Course));
            ViewBag.Counts=Newtonsoft.Json.JsonConvert.SerializeObject(stats.Select(s=>s.Count));
            return View();
        }

        public async Task<IActionResult> ExportEnrollments(){
            var data=await _db.Enrollments.Where(e=>e.Status=="Active").Include(e=>e.Course).Include(e=>e.Student).OrderBy(e=>e.Course.Title).ToListAsync();
            using var wb=new XLWorkbook();var ws=wb.Worksheets.Add("Enrollments");
            ws.Cell(1,1).Value="Course";ws.Cell(1,2).Value="Student";ws.Cell(1,3).Value="Email";ws.Cell(1,4).Value="Enrolled";
            ws.Row(1).Style.Font.Bold=true;
            for(int i=0;i<data.Count;i++){ws.Cell(i+2,1).Value=data[i].Course?.Title??"";ws.Cell(i+2,2).Value=data[i].Student?.FullName??"";ws.Cell(i+2,3).Value=data[i].Student?.Email??"";ws.Cell(i+2,4).Value=data[i].EnrolledAt.ToString("yyyy-MM-dd");}
            ws.Columns().AdjustToContents();
            using var ms=new MemoryStream();wb.SaveAs(ms);
            return File(ms.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Enrollments.xlsx");
        }

        public async Task<IActionResult> ExportGrades(){
            var data=await _db.Grades.Include(g=>g.Submission).ThenInclude(s=>s.Student).Include(g=>g.Submission).ThenInclude(s=>s.Assignment).ThenInclude(a=>a.Course).ToListAsync();
            using var wb=new XLWorkbook();var ws=wb.Worksheets.Add("Grades");
            ws.Cell(1,1).Value="Course";ws.Cell(1,2).Value="Assignment";ws.Cell(1,3).Value="Student";ws.Cell(1,4).Value="Score";ws.Cell(1,5).Value="Grade";
            ws.Row(1).Style.Font.Bold=true;
            for(int i=0;i<data.Count;i++){ws.Cell(i+2,1).Value=data[i].Submission?.Assignment?.Course?.Title??"";ws.Cell(i+2,2).Value=data[i].Submission?.Assignment?.Title??"";ws.Cell(i+2,3).Value=data[i].Submission?.Student?.FullName??"";ws.Cell(i+2,4).Value=data[i].Score;ws.Cell(i+2,5).Value=data[i].LetterGrade;}
            ws.Columns().AdjustToContents();
            using var ms=new MemoryStream();wb.SaveAs(ms);
            return File(ms.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Grades.xlsx");
        }
    }
}
