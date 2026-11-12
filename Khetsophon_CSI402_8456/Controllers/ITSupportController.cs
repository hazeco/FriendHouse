using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับฝ่ายผู้ดูแลระบบไอที
public class ITSupportController : Controller
{
    // ประกาศตัวแปรเก็บบริบทฐานข้อมูลสำหรับเชื่อมต่อตารางใน MySQL
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับบริบทฐานข้อมูลผ่านระบบฉีดพึ่งพา
    public ITSupportController(Csi402dbContext db)
    {
        // กำหนดค่าบริบทฐานข้อมูลให้กับตัวแปรภายในคลาส
        _db = db;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทผู้ดูแลระบบไอทีหรือเจ้าของร้าน
    private bool IsAuthorizedIT()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชัน
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสผู้ใช้งานฝ่ายไอทีจากเซสชัน
        var itId = HttpContext.Session.GetInt32("ITSupportUserId");
        // ตรวจสอบว่ามีเซสชันเฉพาะของฝ่ายไอทีหรือไม่
        if (itId.HasValue) return true;
        // ตรวจสอบว่ามีบทบาทตรงกับผู้ดูแลระบบไอทีหรือเจ้าของร้านหรือไม่
        return role != null && (role.ToLower() == "itsupport" || role.ToLower() == "owner");
    }

    // แอ็กชันหน้าหลักของฝ่ายไอที นำทางไปยังหน้าภาพรวมสถานะระบบ
    public IActionResult Index()
    {
        // เปลี่ยนเส้นทางไปยังหน้าภาพรวมสถานะระบบ
        return RedirectToAction("Dashboard");
    }

    // READ: ภาพรวมสถานะระบบของฝ่ายผู้ดูแลระบบไอที
    public IActionResult Dashboard()
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // นับจำนวนผู้ใช้งานทั้งหมดในระบบจากตารางผู้ใช้งาน
        ViewBag.TotalUsers = _db.Users.Count();
        // นับจำนวนบันทึกกิจกรรมและข้อผิดพลาดทั้งหมดจากตารางบันทึกกิจกรรมระบบ
        ViewBag.TotalLogs = _db.Systemlogs.Count();
        // นับจำนวนข้อผิดพลาดระดับร้ายแรงจากตารางบันทึกกิจกรรมระบบ
        ViewBag.ErrorLogs = _db.Systemlogs.Count(l => l.LogLevel == "ERROR");
        // นับจำนวนสินค้าทั้งหมดในระบบจากตารางสินค้า
        ViewBag.TotalProducts = _db.Products.Count();
        // ดึงรายการบันทึกประวัติการทำงานล่าสุด 5 รายการจากฐานข้อมูลจริง
        ViewBag.RecentLogs = _db.Systemlogs.OrderByDescending(l => l.Timestamp).Take(5).ToList();

        // ส่งการทำงานไปแสดงผลที่หน้าภาพรวมสถานะระบบ
        return View();
    }

    // READ: จัดการบัญชีผู้ใช้งานทั้งหมดพร้อมค้นหา กรองข้อมูล และแบ่งหน้าผ่าน UserManagementViewModel
    public IActionResult Users(string? searchQuery, string? statusFilter, string? roleFilter, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างคำสั่งคิวรีพื้นฐานสำหรับดึงข้อมูลผู้ใช้งานจากตารางผู้ใช้งาน
        var query = _db.Users.AsQueryable();

        // ตรวจสอบว่ามีการกรอกคำค้นหาหรือไม่ (รองรับการค้นหาเบอร์โทรและรหัสผู้ใช้โดยไม่ต้องพิมพ์ขีด -)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var cleanKeyword = searchQuery.Trim().ToLower();
            var keywordNoHyphen = cleanKeyword.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองผู้ใช้งานด้วยอีเมล เบอร์โทรศัพท์ รหัสผู้ใช้ หรือชื่อ-นามสกุล ทั้งแบบมีขีดและไม่มีขีด
            query = query.Where(u =>
                u.Email.ToLower().Contains(cleanKeyword) ||
                u.Phone.ToLower().Contains(cleanKeyword) ||
                (!string.IsNullOrEmpty(keywordNoHyphen) && u.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(keywordNoHyphen)) ||
                u.UserCode.ToLower().Contains(cleanKeyword) ||
                (!string.IsNullOrEmpty(keywordNoHyphen) && u.UserCode.Replace("-", "").ToLower().Contains(keywordNoHyphen)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(cleanKeyword)));
        }

        // ตรวจสอบว่ามีการกรองตามบทบาทผู้ใช้งานหรือไม่
        if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "all")
        {
            var normalizedRole = roleFilter.Trim().ToLowerInvariant();
            query = query.Where(u => u.RoleName != null && u.RoleName.ToLower() == normalizedRole);
        }

        // ตรวจสอบว่ามีการกรองตามสถานะบัญชีหรือไม่
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            // ตรวจสอบเงื่อนไขสถานะปกติ
            if (statusFilter == "ปกติ")
            {
                // กรองเฉพาะบัญชีที่เปิดใช้งานปกติ
                query = query.Where(u => u.IsActive == true);
            }
            // ตรวจสอบเงื่อนไขสถานะถูกล็อก
            else if (statusFilter == "ถูกล็อก")
            {
                // กรองเฉพาะบัญชีที่ถูกระงับการใช้งาน
                query = query.Where(u => u.IsActive == false);
            }
        }

        // ดึงรายการผู้ใช้งานทั้งหมดเรียงลำดับจากวันที่สมัครล่าสุด และคำนวณการแบ่งหน้า (หน้าละ 10 รายการ)
        var allUsers = query.OrderByDescending(u => u.CreatedAt).ToList();
        int pageSize = 10;
        int totalCount = allUsers.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        int currentPage = Math.Clamp(page, 1, totalPages);
        var pagedUsers = allUsers.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ดึงข้อมูลเวลาเข้าสู่ระบบล่าสุดของผู้ใช้งานแต่ละรายจากตารางบันทึกกิจกรรมระบบ
        var lastLogins = _db.Systemlogs
            .Where(l => l.UserId != null && l.Module == "auth.login")
            .GroupBy(l => l.UserId!.Value)
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(l => l.Timestamp) })
            .ToDictionary(x => x.UserId, x => x.LastLogin);

        // ส่งพจนานุกรมเวลาเข้าสู่ระบบล่าสุดและค่าการค้นหาไปยัง ViewBag เพื่อความเข้ากันได้
        ViewBag.LastLogins = lastLogins;
        ViewBag.SearchQuery = searchQuery;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.RoleFilter = roleFilter;

        // ประกอบข้อมูลลงใน UserManagementViewModel ตามมาตรฐานสถาปัตยกรรม MVC
        var vm = new UserManagementViewModel
        {
            SearchQuery = searchQuery,
            StatusFilter = statusFilter ?? "all",
            RoleFilter = roleFilter ?? "all",
            LastLogins = lastLogins,
            TempResetEmail = TempData["ResetEmail"] as string,
            TempResetPassword = TempData["TempPassword"] as string,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize,
            UserItems = pagedUsers.Select(u => new UserAccountItemViewModel
            {
                UserId = u.UserId,
                UserCode = u.UserCode,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleName = u.RoleName ?? "customer",
                Points = u.Points,
                IsActive = u.IsActive ?? true,
                CreatedAt = u.CreatedAt,
                LastLoginAt = lastLogins.ContainsKey(u.UserId) ? lastLogins[u.UserId] : null
            }).ToList()
        };
        vm.AddRange(pagedUsers);

        // ส่ง ViewModel ไปยังหน้าแสดงผลรายชื่อผู้ใช้งาน
        return View(vm);
    }

    // CREATE: เพิ่มผู้ใช้งานใหม่เข้าสู่ระบบฐานข้อมูล
    [HttpPost]
    public IActionResult CreateUser(string role, string? fullName, string email, string phone, string password)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
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

        // สร้างอ็อบเจกต์ผู้ใช้งานใหม่พร้อมเข้ารหัสผ่านด้วยอัลกอริทึมความปลอดภัย
        var newUser = new User
        {
            // กำหนดรหัสโค้ดผู้ใช้งานขึ้นต้นด้วยตัวอักษร U ตามด้วยวันเวลา
            UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดบทบาทสิทธิ์การใช้งาน
            RoleName = string.IsNullOrEmpty(role) ? "customer" : role.ToLower(),
            // กำหนดชื่อ-นามสกุลของผู้ใช้งาน
            FullName = string.IsNullOrWhiteSpace(fullName) ? "สมาชิกใหม่" : fullName.Trim(),
            // กำหนดอีเมลของผู้ใช้งาน
            Email = email.Trim(),
            // กำหนดเบอร์โทรศัพท์ของผู้ใช้งาน
            Phone = phone.Trim(),
            // เข้ารหัสผ่านด้วยแฮชความปลอดภัย
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            // กำหนดแต้มสะสมเริ่มต้นสำหรับลูกค้าใหม่เป็น 100 แต้ม
            Points = string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase) ? 100 : 0,
            // กำหนดสถานะบัญชีเริ่มต้นเป็นเปิดใช้งาน
            IsActive = true,
            // บันทึกวันและเวลาที่สร้างบัญชี
            CreatedAt = DateTime.UtcNow
        };

        // เพิ่มข้อมูลผู้ใช้งานใหม่ลงในชุดข้อมูลผู้ใช้งาน
        _db.Users.Add(newUser);

        // บันทึกประวัติการสร้างบัญชีลงตารางบันทึกกิจกรรมระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = "LOG-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญของข้อมูล
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการสร้างบัญชี
            Message = $"ผู้ดูแลระบบไอทีสร้างผู้ใช้งานใหม่: {email} สิทธิ์การใช้งาน: {newUser.RoleName}",
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = "สร้างบัญชีผู้ใช้งาน \"" + email + "\" เรียบร้อยแล้ว";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: แก้ไขข้อมูลผู้ใช้งานและสถานะบัญชีตามรูปแบบฟอร์มมาตรฐาน MVC
    [HttpPost]
    public IActionResult EditUser(string email, string? phone, string? role, string? status, string? fullName, int? points)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            TempData["ErrorMessage"] = "ไม่พบผู้ใช้งานนี้ในระบบ";
            return RedirectToAction("Users");
        }

        // อัปเดตเบอร์โทรศัพท์หากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(phone)) user.Phone = phone.Trim();
        // อัปเดตชื่อ-นามสกุลหากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName.Trim();
        // อัปเดตสิทธิ์การใช้งานหากมีการระบุค่าใหม่
        if (!string.IsNullOrWhiteSpace(role)) user.RoleName = role.ToLower();
        // อัปเดตแต้มสะสมหากมีการระบุค่าใหม่
        if (points.HasValue && points.Value >= 0) user.Points = points.Value;
        // อัปเดตสถานะการใช้งานบัญชี
        if (!string.IsNullOrWhiteSpace(status))
        {
            // กำหนดสถานะเปิดใช้งานเมื่อเลือกค่าปกติ
            user.IsActive = (status.ToLower() == "active" || status == "ปกติ");
        }

        // บันทึกประวัติการแก้ไขลงตารางบันทึกกิจกรรมระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = "EDIT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญของข้อมูล
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการแก้ไขข้อมูล
            Message = $"ผู้ดูแลระบบไอทีแก้ไขข้อมูลผู้ใช้: {user.Email} สถานะการใช้งาน: {(user.IsActive == true ? "ปกติ" : "ถูกล็อก")}",
            // รหัสผู้ใช้เจ้าหน้าที่ไอทีที่แก้ไข
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งผลการอัปเดตสำเร็จผ่าน TempData และเปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้
        TempData["SuccessMessage"] = $"อัปเดตข้อมูลบัญชี {user.Email} เรียบร้อยแล้ว";
        return RedirectToAction("Users");
    }

    // UPDATE: รีเซ็ตรหัสผ่านชั่วคราวด้วยฟอร์มมาตรฐาน MVC
    [HttpPost]
    public IActionResult ResetPassword(int userId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลผู้ใช้งานจากรหัสประจำตัวผู้ใช้งานในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการรีเซ็ตรหัสผ่าน";
            return RedirectToAction("Users");
        }

        // สุ่มตัวเลข 4 หลักเพื่อสร้างรหัสผ่านชั่วคราว
        var randomNum = new Random().Next(1000, 9999);
        // ประกอบรหัสผ่านชั่วคราวสำหรับส่งมอบให้ผู้ใช้งาน
        var tempPassword = "FH-Temp-" + randomNum;

        // แฮชรหัสผ่านชั่วคราวและบันทึกลงฟิลด์รหัสผ่านของผู้ใช้
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // บันทึกประวัติการรีเซ็ตรหัสผ่านลงตารางบันทึกกิจกรรมระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = "RESET-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญเป็นแจ้งเตือน
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.security",
            // ข้อความรายละเอียดการรีเซ็ตรหัสผ่าน
            Message = $"ผู้ดูแลระบบไอทีรีเซ็ตรหัสผ่านชั่วคราวให้ผู้ใช้: {user.Email}",
            // รหัสเจ้าหน้าที่ไอทีผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // เก็บอีเมลและรหัสผ่านชั่วคราวลงใน TempData เพื่อแสดงในหน้าต่างแจ้งรหัสผ่านบนหน้า Users
        TempData["ResetEmail"] = user.Email;
        TempData["TempPassword"] = tempPassword;
        TempData["SuccessMessage"] = $"สร้างรหัสผ่านชั่วคราวสำหรับบัญชี {user.Email} เรียบร้อยแล้ว: {tempPassword}";
        return RedirectToAction("Users");
    }

    // DELETE: ลบหรือระงับการใช้งานบัญชีผู้ใช้งานเพื่อรักษาความสมบูรณ์ของข้อมูลอ้างอิงในฐานข้อมูล
    [HttpPost]
    public IActionResult DeleteUser(string email)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการลบ";
            return RedirectToAction("Users");
        }

        // ตรวจสอบว่าผู้ใช้มีประวัติคำสั่งซื้อหรือประวัติบันทึกกิจกรรมหรือไม่ หากไม่มีสามารถลบจริงได้ หากมีให้ปิดการใช้งาน
        bool hasPreorders = _db.Preorders.Any(p => p.UserId == user.UserId);
        bool hasLogs = _db.Systemlogs.Any(l => l.UserId == user.UserId);
        bool hasRedemptions = _db.Rewardredemptions.Any(r => r.UserId == user.UserId);

        if (!hasPreorders && !hasLogs && !hasRedemptions)
        {
            // ลบรายการในตะกร้าและลบข้อมูลผู้ใช้ออกจากตาราง
            var carts = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
            if (carts.Any()) _db.Cartitems.RemoveRange(carts);
            _db.Users.Remove(user);
        }
        else
        {
            // ปรับสถานะเป็นปิดการใช้งานบัญชีเพื่อรักษาประวัติคำสั่งซื้อและข้อมูลอ้างอิง
            user.IsActive = false;
        }

        // บันทึกประวัติการระงับหรือลบบัญชีลงตารางบันทึกกิจกรรมระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = "DEL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดระดับความสำคัญเป็นแจ้งเตือน
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการระงับบัญชี
            Message = $"ผู้ดูแลระบบไอทีระงับและลบบัญชีผู้ใช้: {email}",
            // รหัสเจ้าหน้าที่ไอทีผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"ดำเนินการลบบัญชีผู้ใช้งาน {email} เรียบร้อยแล้ว";
        return RedirectToAction("Users");
    }

    // READ: บันทึกกิจกรรมและข้อผิดพลาดของระบบพร้อมระบบแบ่งหน้า
    public IActionResult Logs(string? level, string? timeRange, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างคำสั่งคิวรีพื้นฐานสำหรับดึงข้อมูลบันทึกกิจกรรมจากตารางบันทึกกิจกรรมระบบ
        var query = _db.Systemlogs.AsQueryable();

        // ตรวจสอบว่ามีการกรองตามระดับความสำคัญหรือไม่
        if (!string.IsNullOrEmpty(level) && level != "all")
        {
            // กรองตามระดับความสำคัญที่เลือก
            query = query.Where(l => l.LogLevel == level);
        }

        // ดึงรายการบันทึกกิจกรรมเรียงจากเวลาล่าสุด และแบ่งหน้า (หน้าละ 10 รายการ)
        var allLogs = query.OrderByDescending(l => l.Timestamp).ToList();
        int pageSize = 10;
        int totalCount = allLogs.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        int currentPage = Math.Clamp(page, 1, totalPages);
        var pagedLogs = allLogs.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งค่าระดับและข้อมูลการแบ่งหน้าไปยัง View
        ViewBag.SelectedLevel = level ?? "all";
        ViewBag.SelectedTimeRange = timeRange ?? "24h";
        ViewBag.TotalLogCount = totalCount;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        // ส่งรายการบันทึกกิจกรรมของหน้าปัจจุบันไปยังหน้าแสดงผล
        return View(pagedLogs);
    }
}
