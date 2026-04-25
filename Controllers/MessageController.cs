using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManage.Data;
using UniManage.Models;
using UniManage.ViewModels;

namespace UniManage.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _um;
        public MessageController(AppDbContext db, UserManager<AppUser> um) { _db = db; _um = um; }

        public async Task<IActionResult> Inbox() {
            var uid = _um.GetUserId(User);
            var msgs = await _db.Messages.Where(m => m.ReceiverId == uid).Include(m => m.Sender).OrderByDescending(m => m.SentAt).ToListAsync();
            return View(msgs);
        }

        public async Task<IActionResult> Sent() {
            var uid = _um.GetUserId(User);
            var msgs = await _db.Messages.Where(m => m.SenderId == uid).Include(m => m.Receiver).OrderByDescending(m => m.SentAt).ToListAsync();
            return View(msgs);
        }

        public async Task<IActionResult> Read(int id) {
            var uid = _um.GetUserId(User);
            var msg = await _db.Messages.Include(m => m.Sender).Include(m => m.Receiver).FirstOrDefaultAsync(m => m.MessageId == id);
            if (msg == null) return NotFound();
            if (msg.ReceiverId == uid && !msg.IsRead) { msg.IsRead = true; await _db.SaveChangesAsync(); }
            return View(msg);
        }

        public IActionResult Compose(string receiverId = "", int? parentMessageId = null, string subject = "") {
            var users = _um.Users.Where(u => u.Id != _um.GetUserId(User)).ToList();
            return View(new ComposeVM { ReceiverId = receiverId, Subject = subject, ParentMessageId = parentMessageId, Recipients = users });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(ComposeVM vm) {
            if (!ModelState.IsValid) { vm.Recipients = _um.Users.Where(u => u.Id != _um.GetUserId(User)).ToList(); return View(vm); }
            _db.Messages.Add(new Message { SenderId = _um.GetUserId(User), ReceiverId = vm.ReceiverId, Subject = vm.Subject, Body = vm.Body, ParentMessageId = vm.ParentMessageId });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Message sent!";
            return RedirectToAction("Sent");
        }
    }
}
