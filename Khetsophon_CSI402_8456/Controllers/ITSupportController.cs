using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Helpers;
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

    // เซ็ตข้อมูลสิทธิ์บทบาทผู้ใช้งานที่ได้รับอนุญาตในระบบ
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "customer", "staff", "accountant", "itsupport", "owner"
    };

    // ฟังก์ชันช่วยดึงรหัสผู้ใช้งานปัจจุบันของฝ่ายไอทีจากเซสชัน
    private int? GetCurrentITUserId()
    {
        // ค้นหารหัสผู้ใช้งานจากเซสชันตามลำดับสิทธิ์
        return HttpContext.Session.GetInt32("ITSupportUserId")
            ?? HttpContext.Session.GetInt32("OwnerUserId")
            ?? HttpContext.Session.GetInt32("UserId");
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
            // ทำความสะอาดและแปลงคำค้นหาเป็นตัวพิมพ์เล็ก
            var cleanKeyword = searchQuery.Trim().ToLower();
            // ลบเครื่องหมายขีดและช่องว่างเพื่อการค้นหาแบบยืดหยุ่น
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
            // ปรับแต่งบทบาทที่ต้องการกรองให้เป็นตัวพิมพ์เล็ก
            var normalizedRole = roleFilter.Trim().ToLowerInvariant();
            // กรองเฉพาะผู้ใช้งานที่มีบทบาทตรงตามเงื่อนไข
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
        // กำหนดขนาดรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // นับจำนวนผู้ใช้งานทั้งหมดตามเงื่อนไข
        int totalCount = allUsers.Count;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        // ตรวจสอบขอบเขตหน้าปัจจุบัน
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงข้อมูลเฉพาะรายการของหน้าปัจจุบัน
        var pagedUsers = allUsers.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ดึงข้อมูลเวลาเข้าสู่ระบบล่าสุดของผู้ใช้งานแต่ละรายจากตารางบันทึกกิจกรรมระบบ
        var lastLogins = _db.Systemlogs
            .Where(l => l.UserId != null && l.Module == "auth.login")
            .GroupBy(l => l.UserId!.Value)
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(l => l.Timestamp) })
            .ToDictionary(x => x.UserId, x => x.LastLogin);

        // ส่งพจนานุกรมเวลาเข้าสู่ระบบล่าสุดและค่าการค้นหาไปยัง ViewBag เพื่อความเข้ากันได้
        ViewBag.LastLogins = lastLogins;
        // ส่งคำค้นหา
        ViewBag.SearchQuery = searchQuery;
        // ส่งตัวกรองสถานะ
        ViewBag.StatusFilter = statusFilter;
        // ส่งตัวกรองบทบาท
        ViewBag.RoleFilter = roleFilter;

        // ประกอบข้อมูลลงใน UserManagementViewModel ตามมาตรฐานสถาปัตยกรรม MVC
        var vm = new UserManagementViewModel
        {
            // กำหนดคำค้นหาและตัวกรองสถานะ/บทบาทผู้ใช้
            SearchQuery = searchQuery,
            StatusFilter = statusFilter ?? "all",
            RoleFilter = roleFilter ?? "all",
            // ข้อมูลการเข้าสู่ระบบล่าสุดและรหัสผ่านชั่วคราว
            LastLogins = lastLogins,
            TempResetEmail = TempData["ResetEmail"] as string,
            TempResetPassword = TempData["TempPassword"] as string,
            // ข้อมูลการแบ่งหน้าแสดงผล
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize,
            // แปลงรายการผู้ใช้จาก Entity เป็น UserAccountItemViewModel
            UserItems = pagedUsers.Select(u => new UserAccountItemViewModel
            {
                // ข้อมูลรหัสผู้ใช้ รหัสโค้ด และชื่อเต็ม
                UserId = u.UserId,
                UserCode = u.UserCode,
                FullName = u.FullName,
                // ข้อมูลการติดต่อและบทบาทในระบบ
                Email = u.Email,
                Phone = u.Phone,
                RoleName = u.RoleName ?? "customer",
                // แต้มสะสมและสถานะการเปิดใช้งาน
                Points = u.Points,
                IsActive = u.IsActive ?? true,
                CreatedAt = u.CreatedAt,
                // วันเวลาเข้าสู่ระบบครั้งล่าสุด
                LastLoginAt = lastLogins.ContainsKey(u.UserId) ? lastLogins[u.UserId] : null
            }).ToList()
        };
        // เพิ่มรายการลงใน ViewModel
        vm.AddRange(pagedUsers);

        // ส่ง ViewModel ไปยังหน้าแสดงผลรายชื่อผู้ใช้งาน
        return View(vm);
    }

    // CREATE: เพิ่มผู้ใช้งานใหม่เข้าสู่ระบบฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser(string role, string? fullName, string email, string phone, string password)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความครบถ้วนของข้อมูลที่จำเป็น
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phone))
        {
            // แจ้งเตือนกรณีข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณากรอกอีเมล เบอร์โทรศัพท์ และรหัสผ่านให้ครบถ้วน";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตัดช่องว่างอีเมล
        var normalizedEmail = email.Trim();
        // ตัดช่องว่างเบอร์โทรศัพท์
        var cleanPhone = phone.Trim();
        // ตรวจสอบความถูกต้องของรูปแบบเบอร์โทรศัพท์
        if (!PromoAndRedemptionHelper.IsValidPhoneNumber(cleanPhone))
        {
            // แจ้งเตือนเมื่อเบอร์โทรศัพท์ผิดรูปแบบ
            TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9–15 หลัก";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบความยาวของรหัสผ่าน
        if (password.Trim().Length < 6)
        {
            // แจ้งเตือนเมื่อรหัสผ่านสั้นเกินไป
            TempData["ErrorMessage"] = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ปรับแต่งบทบาทผู้ใช้งานให้อยู่ในรูปแบบตัวพิมพ์เล็ก
        var targetRole = string.IsNullOrWhiteSpace(role) ? "customer" : role.Trim().ToLowerInvariant();
        // ตรวจสอบว่าบทบาทตรงตามสิทธิ์ที่อนุญาตหรือไม่
        if (!AllowedRoles.Contains(targetRole))
        {
            // แจ้งเตือนกรณีบทบาทไม่ถูกต้อง
            TempData["ErrorMessage"] = "ไม่พบสิทธิ์การใช้งานที่ระบุในระบบ";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบว่าอีเมลซ้ำกับในระบบหรือไม่
        var checkUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail.ToLower());
        // ตรวจสอบผลการค้นหา
        if (checkUser != null)
        {
            // แจ้งเตือนกรณีอีเมลซ้ำ
            TempData["ErrorMessage"] = "อีเมลนี้มีอยู่ในระบบแล้ว";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // สร้างออบเจ็กต์ผู้ใช้งานใหม่พร้อมแฮชรหัสผ่านด้วย BCrypt
        var newUser = new User
        {
            UserCode = "U" + DateTime.Now.ToString("yyMMddHHmmss") + RandomNumberGenerator.GetInt32(10, 99),
            RoleName = targetRole,
            FullName = string.IsNullOrWhiteSpace(fullName) ? "สมาชิกใหม่" : fullName.Trim(),
            Email = normalizedEmail,
            Phone = cleanPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim()),
            Points = string.Equals(targetRole, "customer", StringComparison.OrdinalIgnoreCase) ? 100 : 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // เพิ่มผู้ใช้งานใหม่ลงในฐานข้อมูล
        _db.Users.Add(newUser);

        // บันทึกกิจกรรมการสร้างผู้ใช้งานลงในตารางบันทึกระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("LOG"),
            LogLevel = "INFO",
            Module = "auth.users",
            Message = $"ผู้ดูแลระบบไอทีสร้างผู้ใช้งานใหม่: {normalizedEmail} สิทธิ์การใช้งาน: {newUser.RoleName}",
            UserId = GetCurrentITUserId(),
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนผลการสร้างบัญชีสำเร็จ
        TempData["SuccessMessage"] = "สร้างบัญชีผู้ใช้งาน \"" + normalizedEmail + "\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: แก้ไขข้อมูลผู้ใช้งานและสถานะบัญชีตามรูปแบบฟอร์มมาตรฐาน MVC
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(string email, string? phone, string? role, string? status, string? fullName, int? points)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบผู้ใช้งานนี้ในระบบ";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ดึงรหัสผู้ใช้งานฝ่ายไอทีปัจจุบัน
        var currentUserId = GetCurrentITUserId();

        // ตรวจสอบและอัปเดตเบอร์โทรศัพท์
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // ตัดช่องว่างเบอร์โทรศัพท์
            var cleanPhone = phone.Trim();
            // ตรวจสอบความถูกต้องของเบอร์โทรศัพท์
            if (!PromoAndRedemptionHelper.IsValidPhoneNumber(cleanPhone))
            {
                // แจ้งเตือนกรณีเบอร์โทรศัพท์ไม่ถูกต้อง
                TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9–15 หลัก";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดเบอร์โทรศัพท์ใหม่
            user.Phone = cleanPhone;
        }

        // อัปเดตชื่อ-นามสกุล
        if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName.Trim();

        // ตรวจสอบและอัปเดตบทบาทผู้ใช้งาน
        if (!string.IsNullOrWhiteSpace(role))
        {
            // ปรับแต่งบทบาทเป็นตัวพิมพ์เล็ก
            var normalizedRole = role.Trim().ToLowerInvariant();
            // ตรวจสอบว่าบทบาทที่ระบุได้รับอนุญาตหรือไม่
            if (!AllowedRoles.Contains(normalizedRole))
            {
                // แจ้งเตือนกรณีบทบาทไม่ถูกต้อง
                TempData["ErrorMessage"] = "ไม่พบสิทธิ์การใช้งานที่ระบุในระบบ";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // ป้องกันการลดสิทธิ์บัญชีตนเองที่กำลังล็อกอินอยู่
            if (currentUserId.HasValue && currentUserId.Value == user.UserId && normalizedRole != "itsupport" && normalizedRole != "owner")
            {
                // แจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่สามารถลดสิทธิ์บัญชีผู้ดูแลที่กำลังใช้งานอยู่ของตนเองได้";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดบทบาทใหม่
            user.RoleName = normalizedRole;
        }

        // อัปเดตคะแนนสะสม
        if (points.HasValue && points.Value >= 0) user.Points = points.Value;

        // ตรวจสอบและอัปเดตสถานะบัญชี
        if (!string.IsNullOrWhiteSpace(status))
        {
            // คำนวณสถานะเปิดใช้งาน
            bool willBeActive = status.Equals("active", StringComparison.OrdinalIgnoreCase) || status == "ปกติ";
            // ป้องกันการระงับบัญชีตนเองที่กำลังใช้งานอยู่
            if (!willBeActive && currentUserId.HasValue && currentUserId.Value == user.UserId)
            {
                // แจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่สามารถระงับการใช้งานบัญชีที่กำลังล็อกอินอยู่ของตนเองได้";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดสถานะบัญชีใหม่
            user.IsActive = willBeActive;
        }

        // บันทึกกิจกรรมการแก้ไขข้อมูลผู้ใช้งานลงในตารางบันทึกระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("EDIT"),
            LogLevel = "INFO",
            Module = "auth.users",
            Message = $"ผู้ดูแลระบบไอทีแก้ไขข้อมูลผู้ใช้: {user.Email} สถานะการใช้งาน: {(user.IsActive == true ? "ปกติ" : "ถูกล็อก")}",
            UserId = currentUserId,
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"อัปเดตข้อมูลบัญชี {user.Email} เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: รีเซ็ตรหัสผ่านชั่วคราวด้วยฟอร์มมาตรฐาน MVC
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(int userId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานตามรหัส UserId
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบบัญชี
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการรีเซ็ตรหัสผ่าน";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // สุ่มตัวเลข 4 หลักเพื่อสร้างรหัสผ่านชั่วคราว
        var randomNum = RandomNumberGenerator.GetInt32(1000, 10000);
        // สร้างข้อความรหัสผ่านชั่วคราว
        var tempPassword = "FH-Temp-" + randomNum;
        // แฮชรหัสผ่านชั่วคราวด้วย BCrypt
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // บันทึกกิจกรรมการรีเซ็ตรหัสผ่านลงในตารางบันทึกระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("RESET"),
            LogLevel = "WARNING",
            Module = "auth.security",
            Message = $"ผู้ดูแลระบบไอทีรีเซ็ตรหัสผ่านชั่วคราวให้ผู้ใช้: {user.Email}",
            UserId = GetCurrentITUserId(),
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // ส่งอีเมลและรหัสผ่านชั่วคราวไปยัง TempData เพื่อแสดงผล
        TempData["ResetEmail"] = user.Email;
        // ส่งรหัสผ่านชั่วคราว
        TempData["TempPassword"] = tempPassword;
        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"สร้างรหัสผ่านชั่วคราวสำหรับบัญชี {user.Email} เรียบร้อยแล้ว: {tempPassword}";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // DELETE: ลบหรือระงับการใช้งานบัญชีผู้ใช้งานเพื่อรักษาความสมบูรณ์ของข้อมูลอ้างอิงในฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(string email)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ดึงรหัสผู้ใช้งานฝ่ายไอทีปัจจุบัน
        var currentUserId = GetCurrentITUserId();
        // ป้องกันการลบบัญชีที่กำลังล็อกอินอยู่ของตนเอง
        if (currentUserId.HasValue && currentUserId.Value == user.UserId)
        {
            // แจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่สามารถลบหรือระงับบัญชีที่กำลังเข้าสู่ระบบอยู่ของตนเองได้";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบประวัติการสั่งจองพรีออเดอร์ของผู้ใช้งาน
        bool hasPreorders = _db.Preorders.Any(p => p.UserId == user.UserId);
        // ตรวจสอบบันทึกกิจกรรมระบบของผู้ใช้งาน
        bool hasLogs = _db.Systemlogs.Any(l => l.UserId == user.UserId);
        // ตรวจสอบประวัติการแลกแต้มของผู้ใช้งาน
        bool hasRedemptions = _db.Rewardredemptions.Any(r => r.UserId == user.UserId);
        // ตรวจสอบประวัติใบเสร็จรับเงินของผู้ใช้งาน
        bool hasReceipts = _db.Receipts.Any(r => r.CustomerId == user.UserId || r.CashierUserId == user.UserId);
        // ตรวจสอบประวัติในสมุดบัญชีของผู้ใช้งาน
        bool hasLedgers = _db.Financialledgers.Any(f => f.RecordedByUserId == user.UserId);

        // หากไม่มีข้อมูลเชื่อมโยงในระบบเลยสามารถลบออกจากฐานข้อมูลได้โดยตรง
        if (!hasPreorders && !hasLogs && !hasRedemptions && !hasReceipts && !hasLedgers)
        {
            // ค้นหารายการสินค้าในตะกร้าของผู้ใช้
            var carts = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
            // ลบสินค้าในตะกร้าหากมีอยู่
            if (carts.Any()) _db.Cartitems.RemoveRange(carts);
            // ลบบัญชีผู้ใช้งานออกจากฐานข้อมูล
            _db.Users.Remove(user);
        }
        else
        {
            // หากมีประวัติธุรกรรมให้ทำการ Soft Delete โดยการระงับการใช้งาน
            user.IsActive = false;
        }

        // บันทึกกิจกรรมการระงับหรือลบบัญชีลงในตารางบันทึกระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("DEL"),
            LogLevel = "WARNING",
            Module = "auth.users",
            Message = $"ผู้ดูแลระบบไอทีระงับและลบบัญชีผู้ใช้: {email}",
            UserId = currentUserId,
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"ดำเนินการลบบัญชีผู้ใช้งาน {email} เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // ฟังก์ชันช่วยสร้างคิวรีบันทึกกิจกรรมระบบตามระดับความสำคัญและช่วงเวลา
    private IQueryable<Systemlog> BuildFilteredLogsQuery(string? level, string? timeRange)
    {
        // เริ่มต้นสร้างคิวรีบันทึกกิจกรรมระบบ
        var query = _db.Systemlogs.AsQueryable();

        // ตรวจสอบการกรองตามระดับความสำคัญ
        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // ปรับแต่งระดับความสำคัญเป็นตัวพิมพ์ใหญ่
            var upperLevel = level.Trim().ToUpperInvariant();
            // กรองเฉพาะบันทึกที่มีระดับตรงตามเงื่อนไข
            query = query.Where(l => l.LogLevel.ToUpper() == upperLevel);
        }

        // ดึงเวลาปัจจุบันในรูปแบบ UTC
        var nowUtc = DateTime.UtcNow;
        // ตรวจสอบเงื่อนไขกรอง 24 ชั่วโมงล่าสุด
        if (string.Equals(timeRange, "24h", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 24 ชั่วโมง
            var since = nowUtc.AddHours(-24);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }
        // ตรวจสอบเงื่อนไขกรอง 7 วันล่าสุด
        else if (string.Equals(timeRange, "7d", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 7 วัน
            var since = nowUtc.AddDays(-7);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }
        // ตรวจสอบเงื่อนไขกรอง 30 วันล่าสุด
        else if (string.Equals(timeRange, "30d", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 30 วัน
            var since = nowUtc.AddDays(-30);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }

        // ส่งคืนผลลัพธ์คำสั่งคิวรี
        return query;
    }

    // READ: บันทึกกิจกรรมและข้อผิดพลาดของระบบพร้อมระบบแบ่งหน้าและกรองช่วงเวลา
    public IActionResult Logs(string? level, string? timeRange, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // กำหนดค่าช่วงเวลาที่ใช้งาน
        var activeRange = string.IsNullOrWhiteSpace(timeRange) ? "all" : timeRange;
        // ดึงรายการบันทึกทั้งหมดตามตัวกรองและเรียงตามเวลาล่าสุด
        var allLogs = BuildFilteredLogsQuery(level, activeRange)
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        // กำหนดจำนวนรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // นับจำนวนบันทึกทั้งหมด
        int totalCount = allLogs.Count;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        // ตรวจสอบขอบเขตหน้าปัจจุบัน
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงเฉพาะบันทึกของหน้าปัจจุบัน
        var pagedLogs = allLogs.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งค่าตัวแปรและข้อมูลการแบ่งหน้าไปยังมุมมองผ่าน ViewBag
        ViewBag.SelectedLevel = string.IsNullOrWhiteSpace(level) ? "all" : level.ToUpperInvariant();
        // ส่งช่วงเวลาที่เลือก
        ViewBag.SelectedTimeRange = activeRange;
        // ส่งจำนวนบันทึกทั้งหมด
        ViewBag.TotalLogCount = totalCount;
        // ส่งหน้าปัจจุบัน
        ViewBag.CurrentPage = currentPage;
        // ส่งจำนวนหน้าทั้งหมด
        ViewBag.TotalPages = totalPages;
        // ส่งขนาดรายการต่อหน้า
        ViewBag.PageSize = pageSize;

        // ส่งรายการบันทึกไปแสดงผลที่หน้า Logs
        return View(pagedLogs);
    }

    // EXPORT: ส่งออกบันทึกกิจกรรมระบบจากฐานข้อมูลจริงเป็นไฟล์ .CSV
    [HttpGet]
    public IActionResult ExportLogsCSV(string? level, string? timeRange)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลบันทึกตามตัวกรองและเรียงจากเวลาล่าสุด
        var rows = BuildFilteredLogsQuery(level, string.IsNullOrWhiteSpace(timeRange) ? "all" : timeRange)
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        // สร้างตัวสร้างข้อความ StringBuilder สำหรับเขียนข้อมูล CSV
        var sb = new StringBuilder();
        // เพิ่มแถวหัวคอลัมน์ภาษาไทย
        sb.AppendLine("รหัสบันทึก,เวลา (UTC),ระดับ,โมดูล,รายละเอียดข้อความ");
        // วนลูปแปลงข้อมูลบันทึกแต่ละรายการลงในบรรทัด CSV
        foreach (var log in rows)
        {
            // เขียนข้อมูลบันทึกทีละแถวพร้อมตัดอักขระพิเศษ
            sb.AppendLine(string.Join(",",
                PromoAndRedemptionHelper.EscapeCsvField(log.LogCode),
                PromoAndRedemptionHelper.EscapeCsvField(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                PromoAndRedemptionHelper.EscapeCsvField(log.LogLevel),
                PromoAndRedemptionHelper.EscapeCsvField(log.Module),
                PromoAndRedemptionHelper.EscapeCsvField(log.Message)));
        }

        // แปลงข้อความเป็นไบต์อาร์เรย์พร้อมแนบ UTF-8 BOM เพื่อให้เปิดอ่านภาษาไทยใน Excel ได้ถูกต้อง
        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        // ส่งคืนไฟล์ CSV สำหรับดาวน์โหลด
        return File(csvBytes, "text/csv; charset=utf-8", $"FriendHouse_SystemLogs_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }
}
