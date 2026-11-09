using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;
using Microsoft.VisualBasic;
using Khetsophon_CSI402_8456.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Khetsophon_CSI402_8456.Controllers;

public class AccountController : Controller
{
    private readonly Csi402dbContext _db;

    public AccountController(Csi402dbContext db)
    {
        _db = db;
    }
    
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel data)
    {
        if (ModelState.IsValid)
        {
            // 1. READ: check email exits
            var checkUser = _db.Users.FirstOrDefault(u => u.Email == data.Email);
            
            if (checkUser == null)
            {
                // 2.CREATE: new user
                User newUser =  new User();
                newUser.UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss");
                newUser.Email = data.Email;
                newUser.Phone = data.Phone;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password);
                newUser.FullName = null;
                newUser.AvatarUrl = null;
                newUser.RoleName = "customer";
                newUser.Points = 0;
                newUser.IsActive = true;
                newUser.CreatedAt = DateTime.UtcNow;
                _db.Users.Add(newUser);
                _db.SaveChanges();

                // send data to login page for login process
                TempData["SuccessMessage"] = "Registered Successfully. Now you can login.";
                return RedirectToAction("Login", "Account");
            } else
            {
                ViewBag.Status = "Email is already exists";
                return View(data);
            }
        }
        return View(data);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel data)
    {
        if (ModelState.IsValid)
        {
            // READ: ค้นหาผู้ใช้จาก Email ในฐานข้อมูล
            var user = _db.Users.FirstOrDefault(u => u.Email == data.Email);
            
            // ตรวจสอบรหัสผ่านด้วย BCrypt
            if (user != null && BCrypt.Net.BCrypt.Verify(data.Password, user.PasswordHash))
            {
                // save status in session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Role", user.RoleName);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

                // บันทึก Session แยกตาม Role เพื่อป้องกันการแย่ง Session เมื่อเปิดทดสอบหลาย Tab
                string r = user.RoleName.ToLowerInvariant();
                if (r == "customer")
                {
                    HttpContext.Session.SetInt32("CustomerUserId", user.UserId);
                    HttpContext.Session.SetString("CustomerUserName", user.FullName ?? user.Email);
                }
                else if (r == "staff")
                {
                    HttpContext.Session.SetInt32("StaffUserId", user.UserId);
                    HttpContext.Session.SetString("StaffUserName", user.FullName ?? user.Email);
                }
                else if (r == "owner")
                {
                    HttpContext.Session.SetInt32("OwnerUserId", user.UserId);
                    HttpContext.Session.SetString("OwnerUserName", user.FullName ?? user.Email);
                }
                
                // บันทึก Log ประวัติการเข้าสู่ระบบลงตาราง Systemlogs
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัส Log
                    LogCode = "AUTH-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    // กำหนดระดับความสำคัญเป็น INFO
                    LogLevel = "INFO",
                    // กำหนดโมดูลที่เกี่ยวข้อง
                    Module = "auth.login",
                    // ข้อความรายละเอียดการเข้าสู่ระบบ
                    Message = $"ผู้ใช้งานเข้าสู่ระบบสำเร็จ: {user.Email} (สิทธิ์: {user.RoleName})",
                    // รหัสผู้ใช้งานที่เข้าสู่ระบบ
                    UserId = user.UserId,
                    // วันเวลาที่ทำการเข้าสู่ระบบ
                    Timestamp = DateTime.Now
                });
                // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
                _db.SaveChanges();

                // Login สำเร็จ
                return RedirectToAction("Index", "Home" );
            }
            else
            {
                ViewBag.Status = "Email or password is incorrect";
                return View(data);
            }
        }
        return View(data);
    }

    // FAST LOGIN (MOCK LOGIN): ระบบล็อกอินด่วนสำหรับการทดสอบและนำเสนองาน
    // WHY: ออกแบบมาเพื่อให้อาจารย์และผู้ตรวจสามารถสลับบทบาท (Role) ได้ใน 1 คลิก
    // WHY SESSION: แม้จะเป็น Mock Login แต่จำเป็นต้องบันทึก Session จริง (UserId, Role, UserEmail, UserName)
    // เพื่อให้ตัวตรวจสิทธิ์ (Route Guard) ใน Controller หลังบ้าน (Staff, Owner, IT, Accountant) ปล่อยให้ผ่านได้จริง
    [HttpGet]
    public IActionResult FastLogin(string role)
    {
        string normalizedRole = (role ?? "customer").ToLowerInvariant();
        
        int mockUserId = 1;
        string email = "customer@friendhouse.com";
        string name = "ลูกค้าทดสอบ (Demo Customer)";
        string targetAction = "Customer";
        string targetController = "Home";

        switch (normalizedRole)
        {
            case "staff":
                mockUserId = 2;
                email = "staff@friendhouse.com";
                name = "พนักงานหน้าร้าน (Demo Staff)";
                targetController = "Staff";
                targetAction = "Index";
                break;

            case "owner":
                mockUserId = 3;
                email = "owner@friendhouse.com";
                name = "เจ้าของร้าน (Demo Owner)";
                targetController = "Owner";
                targetAction = "Promotions";
                break;

            case "itsupport":
                mockUserId = 4;
                email = "it@friendhouse.com";
                name = "เจ้าหน้าที่ไอที (Demo IT Support)";
                targetController = "ITSupport";
                targetAction = "Dashboard";
                break;

            case "accountant":
                mockUserId = 5;
                email = "account@friendhouse.com";
                name = "นักบัญชี (Demo Accountant)";
                targetController = "Accountant";
                targetAction = "Index";
                break;

            default:
                normalizedRole = "customer";
                break;
        }

        // WHY: ค้นหาใน DB ก่อนเพื่อใช้ ID จริงหากมีการ Seed ไว้ แต่ถ้ายังไม่มีก็มี Fallback ค่าจำลองให้ระบบไม่พัง
        var dbUser = _db.Users.FirstOrDefault(u => u.RoleName.ToLower() == normalizedRole);
        if (dbUser != null)
        {
            mockUserId = dbUser.UserId;
            email = dbUser.Email;
            name = dbUser.FullName ?? dbUser.Email;
        }

        // บันทึก Session จำเป็นสำหรับใช้ตรวจสอบทุกหน้า
        HttpContext.Session.SetInt32("UserId", mockUserId);
        HttpContext.Session.SetString("Role", normalizedRole);
        HttpContext.Session.SetString("UserEmail", email);
        HttpContext.Session.SetString("UserName", name);

        // บันทึก Session แยกตาม Role เพื่อรองรับการเปิดหลายแท็บพร้อมกัน
        if (normalizedRole == "customer")
        {
            HttpContext.Session.SetInt32("CustomerUserId", mockUserId);
            HttpContext.Session.SetString("CustomerUserName", name);
        }
        else if (normalizedRole == "staff")
        {
            HttpContext.Session.SetInt32("StaffUserId", mockUserId);
            HttpContext.Session.SetString("StaffUserName", name);
        }
        else if (normalizedRole == "owner")
        {
            HttpContext.Session.SetInt32("OwnerUserId", mockUserId);
            HttpContext.Session.SetString("OwnerUserName", name);
        }
        else if (normalizedRole == "accountant")
        {
            HttpContext.Session.SetInt32("AccountantUserId", mockUserId);
            HttpContext.Session.SetString("AccountantUserName", name);
        }
        else if (normalizedRole == "itsupport")
        {
            HttpContext.Session.SetInt32("ITSupportUserId", mockUserId);
            HttpContext.Session.SetString("ITSupportUserName", name);
        }

        // บันทึก Log การเข้าสู่ระบบด่วนลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = "AUTH-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.login",
            // ข้อความรายละเอียด
            Message = $"ผู้ใช้งานเข้าสู่ระบบด่วนสำเร็จ (FastLogin): {email} (สิทธิ์: {normalizedRole})",
            // รหัสผู้ใช้งาน
            UserId = mockUserId,
            // วันเวลาที่ทำการเข้าสู่ระบบ
            Timestamp = DateTime.Now
        });
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"เข้าสู่ระบบด่วนในบทบาท {normalizedRole} สำเร็จ";
        return RedirectToAction(targetAction, targetController);
    }

    // Action สำหรับออกจากระบบ (Logout) และล้าง Session
    public IActionResult Logout(string? role)
    {
        // ตรวจสอบกรณีเป็นลูกค้า (Customer) ให้ออกจากระบบแล้วนำทางกลับไปยังหน้าแรก
        if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
        {
            // ลบ Session รหัสลูกค้า
            HttpContext.Session.Remove("CustomerUserId");
            // ลบ Session ชื่อลูกค้า
            HttpContext.Session.Remove("CustomerUserName");
            // นำทางกลับไปยังหน้าแรกของเว็บไซต์
            return RedirectToAction("Index", "Home");
        }

        // ล้างข้อมูลเซสชันทั้งหมดของผู้ใช้งานออกจากหน่วยความจำ
        HttpContext.Session.Clear();
        // นำทางกลับไปยังหน้าเข้าสู่ระบบ (Login) สำหรับทุกบทบาทการทำงาน
        return RedirectToAction("Login", "Account");
    }

    public IActionResult ResetPassword()
    {
        return View();
    }

    // READ: แสดงหน้าโปรไฟล์สมาชิก (Profile)
    public IActionResult Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        User? user = null;

        if (userId.HasValue)
        {
            user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        }

        // ดึงจำนวนคำสั่งจองพรีออเดอร์ที่ยังรอดำเนินการ
        if (user != null)
        {
            ViewBag.PendingPreorderCount = _db.Preorders.Count(p => p.UserId == user.UserId && p.CurrentStatus != "Completed" && p.CurrentStatus != "Cancelled");
        }

        return View(user);
    }

    // UPDATE: แก้ไขข้อมูลส่วนตัวของสมาชิก (Update Profile)
    [HttpPost]
    public IActionResult UpdateProfile(string fullName, string phone, string? avatarUrl)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อนดำเนินการ" });
        }

        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        if (user == null)
        {
            return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้งาน" });
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            user.FullName = fullName.Trim();
            HttpContext.Session.SetString("UserName", user.FullName);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            user.Phone = phone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        _db.SaveChanges();
        return Json(new { success = true, fullName = user.FullName, phone = user.Phone, message = "บันทึกข้อมูลส่วนตัวเรียบร้อยแล้ว" });
    }

    // UPDATE: เปลี่ยนรหัสผ่านของสมาชิก (Change Password)
    [HttpPost]
    public IActionResult ChangePassword(string oldPassword, string newPassword)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อนดำเนินการ" });
        }

        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        if (user == null)
        {
            return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้งาน" });
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return Json(new { success = false, message = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 6 ตัวอักษร" });
        }

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            return Json(new { success = false, message = "รหัสผ่านเดิมไม่ถูกต้อง" });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _db.SaveChanges();

        return Json(new { success = true, message = "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว" });
    }
}
