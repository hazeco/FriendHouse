using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับฝ่ายไอทีซัพพอร์ต (IT Support)
public class ITSupportController : Controller
{
    // ประกาศตัวแปรเก็บ Database Context
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับ DbContext ผ่าน Dependency Injection
    public ITSupportController(Csi402dbContext db)
    {
        // กำหนดค่า DbContext ให้กับตัวแปรภายในคลาส
        _db = db;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาท IT Support หรือ Owner
    private bool IsAuthorizedIT()
    {
        // อ่านค่าบทบาทปัจจุบันจาก Session
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสผู้ใช้งาน IT Support จาก Session
        var itId = HttpContext.Session.GetInt32("ITSupportUserId");
        // ตรวจสอบว่ามี Session เฉพาะของ ITSupport หรือไม่
        if (itId.HasValue) return true;
        // ตรวจสอบว่ามีบทบาทตรงกับ itsupport หรือ owner หรือไม่
        return role != null && (role.ToLower() == "itsupport" || role.ToLower() == "owner");
    }

    // แอ็กชันหน้าหลักของไอที นำทางไปยัง Dashboard
    public IActionResult Index()
    {
        // เปลี่ยนเส้นทางไปยังหน้า Dashboard
        return RedirectToAction("Dashboard");
    }

    // READ: ภาพรวมสถานะระบบ (Dashboard)
    public IActionResult Dashboard()
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชี IT Support";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // นับจำนวนผู้ใช้งานทั้งหมดในระบบจากตาราง Users
        ViewBag.TotalUsers = _db.Users.Count();
        // นับจำนวนบันทึกกิจกรรมและข้อผิดพลาดทั้งหมดจากตาราง Systemlogs
        ViewBag.TotalLogs = _db.Systemlogs.Count();
        // นับจำนวนข้อผิดพลาดระดับ ERROR จากตาราง Systemlogs
        ViewBag.ErrorLogs = _db.Systemlogs.Count(l => l.LogLevel == "ERROR");
        // นับจำนวนสินค้าทั้งหมดในระบบจากตาราง Products
        ViewBag.TotalProducts = _db.Products.Count();
        // ดึงรายการบันทึกประวัติการทำงานล่าสุด 5 รายการจากฐานข้อมูลจริง
        ViewBag.RecentLogs = _db.Systemlogs.OrderByDescending(l => l.Timestamp).Take(5).ToList();

        // ส่งการทำงานไปแสดงผลที่หน้า Dashboard View
        return View();
    }

    // READ: จัดการบัญชีผู้ใช้งาน (Users)
    public IActionResult Users(string? searchQuery, string? statusFilter)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชี IT Support";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้าง Query พื้นฐานสำหรับดึงข้อมูลผู้ใช้งานจากตาราง Users
        var query = _db.Users.AsQueryable();

        // ตรวจสอบว่ามีการกรอกคำค้นหาหรือไม่
        if (!string.IsNullOrEmpty(searchQuery))
        {
            // กรองผู้ใช้งานด้วยอีเมล เบอร์โทร หรือชื่อ-นามสกุล
            query = query.Where(u => u.Email.Contains(searchQuery) || u.Phone.Contains(searchQuery) || (u.FullName != null && u.FullName.Contains(searchQuery)));
        }

        // ตรวจสอบว่ามีการกรองตามสถานะบัญชีหรือไม่
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
        {
            // ตรวจสอบเงื่อนไขสถานะปกติ
            if (statusFilter == "ปกติ")
            {
                // กรองเฉพาะบัญชีที่เปิดใช้งาน (IsActive เป็น true)
                query = query.Where(u => u.IsActive == true);
            }
            // ตรวจสอบเงื่อนไขสถานะถูกล็อก
            else if (statusFilter == "ถูกล็อก")
            {
                // กรองเฉพาะบัญชีที่ถูกระงับ (IsActive เป็น false)
                query = query.Where(u => u.IsActive == false);
            }
        }

        // ดึงรายการผู้ใช้งานเรียงลำดับจากวันที่สมัครล่าสุด
        var users = query.OrderByDescending(u => u.CreatedAt).ToList();

        // ดึงข้อมูลเวลาเข้าสู่ระบบล่าสุดของผู้ใช้งานแต่ละรายจากตาราง Systemlogs
        var lastLogins = _db.Systemlogs
            .Where(l => l.UserId != null && l.Module == "auth.login")
            .GroupBy(l => l.UserId!.Value)
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(l => l.Timestamp) })
            .ToDictionary(x => x.UserId, x => x.LastLogin);

        // ส่ง Dictionary เวลาเข้าสู่ระบบล่าสุดไปยัง View
        ViewBag.LastLogins = lastLogins;
        // ส่งคำค้นหากลับไปแสดงผลในช่องค้นหา
        ViewBag.SearchQuery = searchQuery;
        // ส่งค่าตัวกรองสถานะกลับไปแสดงผลในดรอปดาวน์
        ViewBag.StatusFilter = statusFilter;

        // ส่งรายการผู้ใช้งานไปยังหน้า View
        return View(users);
    }

    // CREATE: เพิ่มผู้ใช้งานใหม่ (Create User)
    [HttpPost]
    public IActionResult CreateUser(string role, string? fullName, string email, string phone, string password)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชี IT Support";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบว่ากรอกข้อมูลจำเป็นครบถ้วนหรือไม่
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(phone))
        {
            // กำหนดข้อความแจ้งเตือนกรณีข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณากรอกข้อมูลให้ครบถ้วน";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าจัดการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบว่ามีอีเมลนี้อยู่ในฐานข้อมูลแล้วหรือไม่
        var checkUser = _db.Users.FirstOrDefault(u => u.Email == email);
        // หากพบว่าอีเมลซ้ำในระบบ
        if (checkUser != null)
        {
            // กำหนดข้อความแจ้งเตือนว่าอีเมลซ้ำ
            TempData["ErrorMessage"] = "อีเมลนี้มีอยู่ในระบบแล้ว";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าจัดการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // สร้างอ็อบเจกต์ผู้ใช้งานใหม่พร้อมเข้ารหัสผ่านด้วย BCrypt
        var newUser = new User
        {
            // กำหนดรหัสโค้ดผู้ใช้งานขึ้นต้นด้วย U ตามด้วยวันเวลา
            UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดบทบาทสิทธิ์การใช้งาน
            RoleName = string.IsNullOrEmpty(role) ? "customer" : role.ToLower(),
            // กำหนดชื่อ-นามสกุลของผู้ใช้งาน
            FullName = fullName,
            // กำหนดอีเมลของผู้ใช้งาน
            Email = email,
            // กำหนดเบอร์โทรศัพท์ของผู้ใช้งาน
            Phone = phone,
            // เข้ารหัสผ่านด้วยแฮช BCrypt ปลอดภัย
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            // กำหนดแต้มสะสมเริ่มต้นเป็น 0 แต้ม
            Points = 0,
            // กำหนดสถานะบัญชีเริ่มต้นเป็นเปิดใช้งาน
            IsActive = true,
            // บันทึกวันและเวลาที่สร้างบัญชี
            CreatedAt = DateTime.UtcNow
        };

        // เพิ่มข้อมูลผู้ใช้งานใหม่ลงใน DbSet
        _db.Users.Add(newUser);

        // บันทึก Log การสร้างบัญชีลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = "LOG-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการสร้างบัญชี
            Message = "IT Support สร้างผู้ใช้งานใหม่: " + email + " (สิทธิ์: " + newUser.RoleName + ")",
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = "สร้างบัญชีผู้ใช้งาน \"" + email + "\" เรียบร้อยแล้ว";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: แก้ไขข้อมูลผู้ใช้งาน (Edit User)
    [HttpPost]
    public IActionResult EditUser(string email, string? phone, string? role, string? status, string? fullName)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // ส่งผลลัพธ์เป็น JSON แจ้งไม่มีสิทธิ์
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        // ค้นหาข้อมูลผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // ส่งผลลัพธ์ JSON แจ้งไม่พบบัญชี
            return Json(new { success = false, message = "ไม่พบผู้ใช้งานนี้ในระบบ" });
        }

        // อัปเดตเบอร์โทรศัพท์หากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(phone)) user.Phone = phone.Trim();
        // อัปเดตชื่อ-นามสกุลหากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName.Trim();
        // อัปเดตสิทธิ์การใช้งานหากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(role)) user.RoleName = role.ToLower();
        // อัปเดตสถานะการใช้งานบัญชี
        if (!string.IsNullOrWhiteSpace(status))
        {
            // กำหนด IsActive เป็น true เมื่อเลือก active หรือ ปกติ
            user.IsActive = (status.ToLower() == "active" || status == "ปกติ");
        }

        // บันทึกประวัติการแก้ไขลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = "EDIT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการแก้ไขข้อมูล
            Message = $"IT Support แก้ไขข้อมูลผู้ใช้: {user.Email} (สถานะ: {(user.IsActive == true ? "ปกติ" : "ถูกล็อก")})",
            // รหัสผู้ใช้เจ้าหน้าที่ไอทีที่แก้ไข
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();
        // ส่งผลลัพธ์ JSON ยืนยันการอัปเดตสำเร็จ
        return Json(new { success = true, message = "อัปเดตข้อมูลผู้ใช้งานเรียบร้อยแล้ว" });
    }

    // UPDATE: รีเซ็ตรหัสผ่านชั่วคราว (Reset Password)
    [HttpPost]
    public IActionResult ResetPassword(int userId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // ส่งผลลัพธ์เป็น JSON แจ้งไม่มีสิทธิ์
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        // ค้นหาข้อมูลผู้ใช้งานจากรหัส UserId ในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // ส่งผลลัพธ์ JSON แจ้งไม่พบบัญชี
            return Json(new { success = false, message = "ไม่พบบัญชีผู้ใช้งาน" });
        }

        // สุ่มตัวเลข 4 หลักเพื่อสร้างรหัสผ่านชั่วคราว
        var randomNum = new Random().Next(1000, 9999);
        // ประกอบรหัสผ่านชั่วคราวในรูปแบบ FH-Temp-XXXX
        var tempPassword = "FH-Temp-" + randomNum;

        // แฮชรหัสผ่านชั่วคราวและบันทึกลงฟิลด์ PasswordHash ของผู้ใช้
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // บันทึกประวัติการรีเซ็ตรหัสผ่านลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = "RESET-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.security",
            // ข้อความรายละเอียดการรีเซ็ตรหัสผ่าน
            Message = "IT Support รีเซ็ตรหัสผ่านชั่วคราวให้ผู้ใช้: " + user.Email,
            // รหัสเจ้าหน้าที่ไอทีผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // ส่งผลลัพธ์ JSON พร้อมรหัสผ่านชั่วคราวกลับไปแสดงผลใน Modal
        return Json(new { success = true, email = user.Email, tempPassword = tempPassword });
    }

    // DELETE: ลบผู้ใช้งาน (Soft Delete: ปิดการใช้งาน IsActive = false เพื่อรักษา Foreign Key)
    [HttpPost]
    public IActionResult DeleteUser(string email)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // ส่งผลลัพธ์เป็น JSON แจ้งไม่มีสิทธิ์
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        // ค้นหาข้อมูลผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // ส่งผลลัพธ์ JSON แจ้งไม่พบบัญชี
            return Json(new { success = false, message = "ไม่พบบัญชีผู้ใช้งานที่ต้องการลบ" });
        }

        // ปรับสถานะเป็น false เพื่อปิดการใช้งานบัญชี (Soft Delete ป้องกัน FK Error)
        user.IsActive = false;

        // บันทึกประวัติการระงับบัญชีลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = "DEL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการระงับบัญชี
            Message = $"IT Support ระงับ/ลบบัญชีผู้ใช้: {user.Email}",
            // รหัสเจ้าหน้าที่ไอทีผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();
        // ส่งผลลัพธ์ JSON แจ้งผลสำเร็จ
        return Json(new { success = true, message = $"ระงับและลบบัญชี {user.Email} สำเร็จ" });
    }

    // READ: บันทึกกิจกรรมและข้อผิดพลาด (System Logs)
    public IActionResult Logs(string? level, string? timeRange)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชี IT Support";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้าง Query พื้นฐานสำหรับดึงข้อมูล Log จากตาราง Systemlogs
        var query = _db.Systemlogs.AsQueryable();

        // ตรวจสอบว่ามีการกรองตามระดับ LogLevel หรือไม่
        if (!string.IsNullOrEmpty(level) && level != "all")
        {
            // กรองตามระดับ LogLevel ที่เลือก เช่น ERROR, WARNING, INFO
            query = query.Where(l => l.LogLevel == level);
        }

        // ดึงรายการ Log ล่าสุดสูงสุด 100 รายการเรียงจากเวลาล่าสุด
        var logs = query.OrderByDescending(l => l.Timestamp).Take(100).ToList();
        // ส่งค่าระดับที่เลือกไปยัง View
        ViewBag.SelectedLevel = level ?? "all";
        // ส่งค่าช่วงเวลาที่เลือกไปยัง View
        ViewBag.SelectedTimeRange = timeRange ?? "24h";

        // ส่งรายการ Log ไปยังหน้าแสดงผล Logs View
        return View(logs);
    }
}
