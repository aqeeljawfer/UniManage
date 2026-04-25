using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniManage.Models;

namespace UniManage.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
        public DbSet<Course>     Courses     { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Grade>      Grades      { get; set; }
        public DbSet<Message>    Messages    { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Enrollment>().HasOne(e=>e.Student).WithMany(u=>u.Enrollments).HasForeignKey(e=>e.StudentId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Submission>().HasOne(s=>s.Student).WithMany(u=>u.Submissions).HasForeignKey(s=>s.StudentId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Course>().HasOne(c=>c.Lecturer).WithMany(u=>u.TaughtCourses).HasForeignKey(c=>c.LecturerId).OnDelete(DeleteBehavior.SetNull);
            b.Entity<Message>().HasOne(m=>m.Sender).WithMany().HasForeignKey(m=>m.SenderId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Message>().HasOne(m=>m.Receiver).WithMany().HasForeignKey(m=>m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Enrollment>().HasIndex(e=>new{e.StudentId,e.CourseId}).IsUnique();
        }
    }

    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<AppDbContext>();
            var um = svc.GetRequiredService<UserManager<AppUser>>();
            var rm = svc.GetRequiredService<RoleManager<IdentityRole>>();
            await db.Database.MigrateAsync();
            foreach (var r in new[]{"Administrator","Lecturer","Student"})
                if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

            async Task<AppUser> Ensure(string email,string name,string role,string pass){
                var u=await um.FindByEmailAsync(email);
                if(u==null){u=new AppUser{UserName=email,Email=email,FullName=name,Role=role,EmailConfirmed=true};await um.CreateAsync(u,pass);await um.AddToRoleAsync(u,role);}
                return u;
            }
            await Ensure("admin@unimanage.ac.uk","System Administrator","Administrator","Admin@123");
            var lec=await Ensure("dr.smith@unimanage.ac.uk","Dr. John Smith","Lecturer","Lecturer@123");
            var stu=await Ensure("student@unimanage.ac.uk","Alice Johnson","Student","Student@123");
            if(!db.Courses.Any()){
                var c1=new Course{Title="Introduction to Programming",Description="Fundamentals of C# programming.",Credits=15,MaxCapacity=30,LecturerId=lec.Id};
                var c2=new Course{Title="Web Application Development",Description="ASP.NET MVC web development.",Credits=15,MaxCapacity=25,LecturerId=lec.Id};
                var c3=new Course{Title="Database Systems",Description="SQL and Entity Framework.",Credits=15,MaxCapacity=30,LecturerId=lec.Id};
                var c4=new Course{Title="Software Engineering",Description="SDLC, design patterns and testing.",Credits=30,MaxCapacity=20,LecturerId=lec.Id};
                db.Courses.AddRange(c1,c2,c3,c4);
                await db.SaveChangesAsync();
                db.Enrollments.Add(new Enrollment{StudentId=stu.Id,CourseId=c1.CourseId,Status="Active"});
                c1.CurrentEnrollment=1;
                db.Assignments.Add(new Assignment{CourseId=c1.CourseId,Title="Assignment 1: Hello World",Description="Write a simple C# console application.",Deadline=DateTime.Now.AddDays(14)});
                await db.SaveChangesAsync();
            }
        }
    }
}
