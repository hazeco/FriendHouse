using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Helpers;
using Khetsophon_CSI402_8456.Models;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คลาส HomeController สำหรับจัดการโครงสร้างข้อมูลและการประมวลผลตามหน้าที่
public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    // ฟังก์ชัน HomeController สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public HomeController (Csi402dbContext db)
    {
        _db = db;
    }

    // CREATE: แสดงแบบฟอร์มบันทึกข้อมูลนักศึกษาเข้าสู่ระบบ (GET Method)
    // WHY: สนับสนุนการฝึกปฏิบัติการฐานข้อมูล Lab 9 สำหรับการสร้างข้อมูลนักศึกษาใหม่
    public IActionResult Lab9()
    {
        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View();
    }

    // CREATE: บันทึกข้อมูลนักศึกษาใหม่ลงในฐานข้อมูล MySQL ตาราง Labstudents (POST Method)
    // WHY: ตรวจสอบความถูกต้องของแบบฟอร์ม ตรวจสอบรหัสนักศึกษาซ้ำ และบันทึกข้อมูลลงฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน Lab9 สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Lab9(Labstudent data)
    {
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (ModelState.IsValid)
        {
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (data.StdPassword != null)
            {
                // ขั้นตอนที่ 1: ตรวจสอบว่ามีรหัสนักศึกษานี้อยู่ในฐานข้อมูลแล้วหรือไม่
                var CheckID = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (CheckID == null)
                {
                    // ขั้นตอนที่ 2: สร้างออบเจกต์นักศึกษาใหม่และบันทึกข้อมูลลงตาราง Labstudents
                    Labstudent s = new Labstudent();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                    _db.Add(s);
                    // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
                    _db.SaveChanges();
                    // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                    return RedirectToAction("Lab9_1", "Home", new { status = "บันทึกข้อมูลสำเร็จ" });
                }
                else
                {
                    // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
                    ViewBag.Status = "รหัสนักศึกษานี้มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
                    // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
                    return View();
                }
            }
            else
            {
                // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
                ViewBag.Status = "กรุณากรอกรหัสผ่าน";
                // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
                return View();
            }
        }
        else
        {
            // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
            return View();
        }
    }

    // READ: แสดงรายชื่อนักศึกษาทั้งหมดจากตาราง Labstudents (GET Method)
    // WHY: ดึงข้อมูลนักศึกษาทั้งหมดจากฐานข้อมูลมาแสดงผลในตารางสำหรับการฝึกปฏิบัติการ Lab 9.1
    public IActionResult Lab9_1()
    {
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var data = _db.Labstudents.ToList();
        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(data);
    }

    // UPDATE: แสดงแบบฟอร์มแก้ไขข้อมูลนักศึกษาตามรหัส (GET Method)
    // WHY: ค้นหาข้อมูลนักศึกษาจากรหัสผ่าน LINQ เพื่อนำข้อมูลเดิมมาแสดงในแบบฟอร์มแก้ไข
    public IActionResult Lab10(string? StdId)
    {
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(User);
    }

    // UPDATE: ปรับปรุงข้อมูลนักศึกษาในตาราง Labstudents (POST Method)
    // WHY: บันทึกการแก้ไขรหัสผ่าน ชื่อ และนามสกุลลงฐานข้อมูล MySQL
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน Lab10 สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Lab10(Labstudent data)
    {
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (User != null)
        {
            User.StdPassword = data.StdPassword;
            User.StdName = data.StdName;
            User.StdLastname = data.StdLastname;
            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Lab9_1", "Home", new { status = "ปรับปรุงข้อมูลสำเร็จ" });
        }
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("Lab9_1", "Home", new { status = "ไม่พบข้อมูลนักศึกษาที่ต้องการแก้ไข" });
    }

    // DELETE: ลบข้อมูลนักศึกษาออกจากตาราง Labstudents (POST Method)
    // WHY: ดำเนินการลบเรคอร์ดนักศึกษาตามรหัสที่ระบุสำหรับการฝึกปฏิบัติการ Lab 10.1
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน Lab10_1 สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Lab10_1(string? StdId)
    {
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var Check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (Check != null)
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Remove(Check);
            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Lab9_1", "Home", new { status = "ลบข้อมูลสำเร็จ" });
        }
        else
        {
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Lab9_1", "Home", new { status = "ไม่พบรหัสนักศึกษานี้ในระบบ" });
        }
    }

    // ฟังก์ชัน IsAuthorizedUserManager สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private bool IsAuthorizedUserManager()
    {
        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return role == "owner" || role == "itsupport" || HttpContext.Session.GetInt32("OwnerUserId").HasValue;
    }

    // READ: แสดงรายชื่อผู้ใช้งานทั้งหมดในระบบจากตาราง Users พร้อมระบบค้นหา ตัวกรอง และแบ่งหน้า (GET Method)
    // WHY: รองรับการทำงาน User Full CRUD และการสืบค้นข้อมูลผู้ใช้งาน (เน้นบทบาทลูกค้า) ผ่าน UserManagementViewModel
    public IActionResult UserList(string? searchQuery, string? roleFilter, string? statusFilter, int page = 1)
    {
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!IsAuthorizedUserManager())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "เฉพาะเจ้าของร้านหรือผู้ดูแลระบบเท่านั้นที่สามารถจัดการรายชื่อผู้ใช้ได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 1: สร้าง Query พื้นฐานจากตาราง Users ในฐานข้อมูล MySQL
        var query = _db.Users.AsQueryable();

        // ขั้นตอนที่ 2: กรองตามคำค้นหา (อีเมล เบอร์โทรศัพท์ ชื่อ-นามสกุล หรือรหัสผู้ใช้ รองรับการพิมพ์แบบไม่ใส่ขีด -)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // ตัดช่องว่างหัวท้ายและสร้างคำค้นหาแบบตัดเครื่องหมายขีด (-) และ # ออก
            string term = searchQuery.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองข้อมูลที่ตรงกับคำค้นหาทั้งแบบมีขีดและไม่มีขีด
            query = query.Where(u => u.Email.ToLower().Contains(term)
                || u.Phone.ToLower().Contains(term)
                || (!string.IsNullOrEmpty(termNoHyphen) && u.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(termNoHyphen))
                || u.UserCode.ToLower().Contains(term)
                || (!string.IsNullOrEmpty(termNoHyphen) && u.UserCode.Replace("-", "").ToLower().Contains(termNoHyphen))
                || (u.FullName != null && u.FullName.ToLower().Contains(term)));
        }

        // ขั้นตอนที่ 3: กรองตามบทบาทผู้ใช้งาน (เช่น customer)
        if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "all")
        {
            // แปลงตัวกรองบทบาทเป็นตัวพิมพ์เล็กเพื่อเปรียบเทียบ
            string roleLower = roleFilter.Trim().ToLower();
            // กรองเฉพาะผู้ใช้งานที่มีบทบาทตรงตามที่เลือก
            query = query.Where(u => u.RoleName.ToLower() == roleLower);
        }

        // ขั้นตอนที่ 4: กรองตามสถานะบัญชีผู้ใช้งาน
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (statusFilter == "ปกติ" || statusFilter.ToLower() == "active")
            {
                // กรองเฉพาะบัญชีที่เปิดใช้งานปกติ
                query = query.Where(u => u.IsActive == true);
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (statusFilter == "ถูกล็อก" || statusFilter.ToLower() == "locked")
            {
                // กรองเฉพาะบัญชีที่ถูกระงับการใช้งาน
                query = query.Where(u => u.IsActive == false);
            }
        }

        // ขั้นตอนที่ 5: ดึงรายการผู้ใช้งานเรียงลำดับจากวันที่สมัครล่าสุด และแบ่งหน้า (หน้าละ 10 รายการ)
        var allFilteredUsers = query.OrderByDescending(u => u.CreatedAt).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int pageSize = 10;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalCount = allFilteredUsers.Count;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var pagedUsers = allFilteredUsers.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ขั้นตอนที่ 6: สร้างและประกอบข้อมูลลงใน UserManagementViewModel
        var viewModel = new UserManagementViewModel
        {
            // เก็บคำค้นหาปัจจุบัน
            SearchQuery = searchQuery,
            // เก็บตัวกรองบทบาทปัจจุบัน
            RoleFilter = roleFilter ?? "all",
            // เก็บตัวกรองสถานะปัจจุบัน
            StatusFilter = statusFilter ?? "all",
            // กำหนดจำนวนทั้งหมดและหมายเลขหน้าปัจจุบัน
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };

        // เพิ่มรายการ User ของหน้าปัจจุบันลงในคอลเลกชันพื้นฐานเพื่อความเข้ากันได้
        viewModel.AddRange(pagedUsers);

        // แปลงแต่ละเรคอร์ดในหน้าปัจจุบันเป็น UserAccountItemViewModel
        viewModel.UserItems = pagedUsers.Select(u => new UserAccountItemViewModel
        {
            UserId = u.UserId,
            UserCode = u.UserCode,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            RoleName = u.RoleName,
            Points = u.Points,
            IsActive = u.IsActive ?? true,
            CreatedAt = u.CreatedAt
        }).ToList();

        // ส่ง UserManagementViewModel ไปแสดงผลที่หน้า View
        return View(viewModel);
    }

    // CREATE: เพิ่มข้อมูลสมาชิกใหม่จากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการเพิ่มข้อมูลผู้ใช้งานใหม่ (เน้นบทบาทลูกค้า) โดยแฮชรหัสผ่านด้วย BCrypt และบันทึกลงฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CreateCustomerUser สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult CreateCustomerUser(string? fullName, string email, string phone, string password, string? role = "customer", int points = 0)
    {
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!IsAuthorizedUserManager())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "เฉพาะเจ้าของร้านหรือผู้ดูแลระบบเท่านั้นที่สามารถเพิ่มผู้ใช้งานได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 1: ตรวจสอบว่ากรอกข้อมูลบังคับครบถ้วนหรือไม่
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            // แจ้งเตือนกรณีกรอกข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณากรอกอีเมล เบอร์โทรศัพท์ และรหัสผ่านให้ครบถ้วน";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!PromoAndRedemptionHelper.IsValidPhoneNumber(phone))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9-10 หลัก";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: ตรวจสอบว่ามีอีเมลซ้ำในระบบฐานข้อมูลหรือไม่
        var exists = _db.Users.FirstOrDefault(u => u.Email == email.Trim());
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (exists != null)
        {
            // แจ้งเตือนกรณีอีเมลซ้ำ
            TempData["ErrorMessage"] = "อีเมลนี้มีอยู่ในระบบแล้ว กรุณาใช้อีเมลอื่น";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 3: สร้างออบเจกต์ผู้ใช้งานใหม่
        var newUser = new User
        {
            // สร้างรหัสสมาชิกอัตโนมัติตามวันเวลา
            UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // กำหนดชื่อ-นามสกุล
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            // กำหนดอีเมล
            Email = email.Trim(),
            // กำหนดเบอร์โทรศัพท์
            Phone = phone.Trim(),
            // เข้ารหัสผ่านด้วย BCrypt
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            // กำหนดบทบาทเริ่มต้นเป็นลูกค้า
            RoleName = string.IsNullOrWhiteSpace(role) ? "customer" : role.Trim().ToLower(),
            // กำหนดแต้มสะสมเริ่มต้น
            Points = Math.Max(0, points),
            // กำหนดสถานะเปิดใช้งาน
            IsActive = true,
            // บันทึกวันเวลาที่สร้าง
            CreatedAt = DateTime.Now
        };

        // เพิ่มข้อมูลลงในตาราง Users
        _db.Users.Add(newUser);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จและเปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
        TempData["SuccessMessage"] = $"เพิ่มข้อมูลสมาชิก \"{newUser.Email}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("UserList");
    }

    // UPDATE: แก้ไขข้อมูลสมาชิกจากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการอัปเดตชื่อ-นามสกุล อีเมล เบอร์โทรศัพท์ แต้มสะสม บทบาท และสถานะการใช้งานของสมาชิก
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน UpdateCustomerUser สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult UpdateCustomerUser(int userId, string? fullName, string? email, string phone, string? role, int points, string? status, bool? isActive)
    {
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!IsAuthorizedUserManager())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "เฉพาะเจ้าของร้านหรือผู้ดูแลระบบเท่านั้นที่สามารถแก้ไขข้อมูลผู้ใช้งานได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 1: ค้นหาข้อมูลสมาชิกในตาราง Users ตามรหัส UserId
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบข้อมูลสมาชิก
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานที่ต้องการแก้ไข";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: อัปเดตชื่อ-นามสกุล อีเมล เบอร์โทรศัพท์ บทบาท แต้มสะสม และสถานะ
        user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName.Trim();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(email))
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string cleanEmail = email.Trim();
            // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
            if (_db.Users.Any(u => u.UserId != userId && u.Email.ToLower() == cleanEmail.ToLower()))
            {
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = $"อีเมล {cleanEmail} ถูกใช้งานโดยบัญชีอื่นแล้ว";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("UserList");
            }
            user.Email = cleanEmail;
        }
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!PromoAndRedemptionHelper.IsValidPhoneNumber(phone))
            {
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9-10 หลัก";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("UserList");
            }
            // อัปเดตเบอร์โทรศัพท์
            user.Phone = phone.Trim();
        }
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(role))
        {
            // อัปเดตบทบาทผู้ใช้งาน
            user.RoleName = role.Trim().ToLower();
        }
        // อัปเดตแต้มสะสมโดยไม่ให้น้อยกว่า 0
        user.Points = Math.Max(0, points);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (isActive.HasValue)
        {
            // อัปเดตสถานะการใช้งานบัญชีจากค่าบูลีน
            user.IsActive = isActive.Value;
        }
        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
        else if (!string.IsNullOrWhiteSpace(status))
        {
            // อัปเดตสถานะการใช้งานบัญชีจากข้อความสถานะ
            user.IsActive = (status == "ปกติ" || status.ToLower() == "active" || status.ToLower() == "true");
        }

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนผลสำเร็จและเปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
        TempData["SuccessMessage"] = $"แก้ไขข้อมูลสมาชิก \"{user.Email}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("UserList");
    }

    // DELETE: ลบหรือระงับบัญชีสมาชิกจากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการลบข้อมูลสมาชิก (หากไม่มีประวัติธุรกรรมจะลบถาวร หากมีประวัติจะทำ Soft Delete เพื่อรักษา Foreign Key)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน DeleteCustomerUser สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult DeleteCustomerUser(int userId)
    {
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!IsAuthorizedUserManager())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "เฉพาะเจ้าของร้านหรือผู้ดูแลระบบเท่านั้นที่สามารถลบผู้ใช้งานได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        int? currentOwnerId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId");
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (currentOwnerId.HasValue && currentOwnerId.Value == userId)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "ไม่สามารถลบบัญชีที่กำลังใช้งานอยู่ได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 1: ค้นหาข้อมูลสมาชิกจากตาราง Users
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบข้อมูลสมาชิก
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: ลบรายการในตะกร้าสินค้าของผู้ใช้งานรายนี้ก่อน
        var cartItems = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (cartItems.Any())
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Cartitems.RemoveRange(cartItems);
        }

        // ขั้นตอนที่ 3: ตรวจสอบว่ามีข้อมูลผูกพันในตาราง Preorders, Receipts หรือ Rewardredemptions หรือไม่
        bool hasLinkedRecords = _db.Preorders.Any(p => p.UserId == user.UserId)
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            || _db.Receipts.Any(r => r.CustomerId == user.UserId || r.CashierUserId == user.UserId)
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            || _db.Rewardredemptions.Any(r => r.UserId == user.UserId)
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            || _db.Financialledgers.Any(f => f.RecordedByUserId == user.UserId);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (hasLinkedRecords)
        {
            // กรณีมีประวัติธุรกรรมผูกพัน ให้ปรับสถานะเป็นระงับการใช้งาน (Soft Delete) เพื่อป้องกัน Foreign Key Error
            user.IsActive = false;
            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"ระงับและปิดการใช้งานบัญชี \"{user.Email}\" เรียบร้อยแล้ว";
        }
        else
        {
            // กรณีไม่มีประวัติธุรกรรมผูกพัน ลบประวัติ Log ที่อ้างอิงและลบเรคอร์ดผู้ใช้ออกจากตารางได้ทันที
            var logs = _db.Systemlogs.Where(l => l.UserId == user.UserId).ToList();
            // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
            foreach (var log in logs)
            {
                log.UserId = null;
            }
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string deletedEmail = user.Email;
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Users.Remove(user);
            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"ลบบัญชีสมาชิก \"{deletedEmail}\" ออกจากระบบเรียบร้อยแล้ว";
        }

        // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("UserList");
    }
    // READ: แสดงหน้าแรกของเว็บไซต์ พร้อมโมเดล 3D และรายการฟิกเกอร์เด่น (Home Index)
    // WHY: รวมรวมข้อมูล Hero Figure, รายการสินค้า และโปรโมชั่นผ่าน HomeIndexViewModel
    public IActionResult Index(LoginViewModel? data)
    {
        // ขั้นตอนที่ 1: รับข้อมูลการเข้าสู่ระบบสำหรับการจำลอง (ถ้ามี)
        var viewModel = new HomeIndexViewModel
        {
            LoginEmail = data?.Email,
            LoginPassword = data?.Password
        };

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (data != null)
        {
            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.Email = data.Email;
            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.Password = data.Password;
        }

        // ขั้นตอนที่ 2: ค้นหาฟิกเกอร์ที่เป็น Featured Hero หรือมีโมเดล 3D
        var heroProduct = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefault(p => p.IsFeaturedHero)
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            ?? _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Model3Dpath))
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            ?? _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault();

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (heroProduct != null)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int heroAvailableStock = (heroProduct.Inventory?.QuantityOnHand ?? 0) - (heroProduct.Inventory?.ReservedQuantity ?? 0);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            bool heroInStock = !heroProduct.IsPreorder && heroAvailableStock > 0;

            viewModel.HeroProduct = new HeroProductViewModel
            {
                ProductId = heroProduct.ProductId,
                Name = heroProduct.Name,
                Description = $"{heroProduct.Name} แบรนด์ {heroProduct.BrandName ?? "ลิขสิทธิ์แท้"} ซีรีส์ {heroProduct.SeriesName ?? "FriendHouse"} {(string.IsNullOrEmpty(heroProduct.Material) ? "" : $"วัสดุ {heroProduct.Material} ")}หมุนชมโมเดล 3 มิติได้รอบตัวก่อนสั่งซื้อ",
                PriceFormatted = $"฿{heroProduct.RetailPrice:N0}",
                Model3DPath = !string.IsNullOrEmpty(heroProduct.Model3Dpath) ? heroProduct.Model3Dpath : "~/media/models/naruto_kyubi_sage.glb",
                FallbackImageUrl = !string.IsNullOrEmpty(heroProduct.ImageUrl) ? heroProduct.ImageUrl : "~/media/images/naruto_hero_fallback.png",
                InStock = heroInStock,
                BadgeText = heroInStock ? "มีสินค้าพร้อมส่ง" : "สินค้าหมดชั่วคราว",
                NoticeText = heroInStock
                    ? "มีสินค้าพร้อมส่งมอบที่หน้าร้าน FriendHouse"
                    : "สินค้าหมดชั่วคราว — กรุณาตรวจสอบอีกครั้งเมื่อสินค้าเข้าสาขา",
                Specs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("ผู้ผลิต", string.IsNullOrWhiteSpace(heroProduct.BrandName) ? "ลิขสิทธิ์แท้" : heroProduct.BrandName),
                    new KeyValuePair<string, string>("วัสดุ", string.IsNullOrWhiteSpace(heroProduct.Material) ? "PVC / ABS" : heroProduct.Material),
                    new KeyValuePair<string, string>("ความสูง", heroProduct.HeightCm.HasValue ? $"{heroProduct.HeightCm.Value:0.#} ซม." : "มาตรฐาน"),
                    new KeyValuePair<string, string>("สเกล", string.IsNullOrWhiteSpace(heroProduct.Scale) ? "มาตรฐาน" : heroProduct.Scale)
                }
            };

            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.HeroModel3D = viewModel.HeroProduct.Model3DPath;
            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.HeroName = viewModel.HeroProduct.Name;
            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.HeroPrice = viewModel.HeroProduct.PriceFormatted;
            // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
            ViewBag.HeroId = viewModel.HeroProduct.ProductId;
        }

        // ขั้นตอนที่ 3: ดึงรายการฟิกเกอร์จริงจากฐานข้อมูล พร้อมข้อมูลสต็อกคงเหลือ
        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive == true || p.IsActive == null)
            .ToList();

        // ขั้นตอนที่ 4: แปลงรายการ Product เป็น ProductCardViewModel
        viewModel.FeaturedProducts = products.Select(p => new ProductCardViewModel
        {
            ProductId = p.ProductId,
            Sku = p.Sku,
            Name = p.Name,
            SeriesName = p.SeriesName,
            CategoryName = p.Category?.CategoryName,
            BrandName = p.BrandName,
            Scale = p.Scale,
            RetailPrice = p.RetailPrice,
            ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "~/media/images/naruto_hero_fig.png",
            AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0),
            IsPreorder = p.IsPreorder
        }).ToList();

        // ขั้นตอนที่ 5: ดึงการ์ดโปรโมชั่นที่เปิดใช้งานจริงจากฐานข้อมูล (แสดงสูงสุด 5 รายการ)
        var today = PromoAndRedemptionHelper.TodayGregorian();
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var activePromos = _db.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.PromotionId)
            .ToList()
            .Where(p => PromoAndRedemptionHelper.NormalizeGregorianDate(p.EndDate) >= today)
            .Take(5)
            .ToList();

        viewModel.Promotions = activePromos.Select(p => new HomePromotionCardViewModel
        {
            Tag = PromoAndRedemptionHelper.GetPromoTypeThaiLabel(p.PromoType),
            Title = p.Name,
            Description = $"{PromoAndRedemptionHelper.FormatPromoBenefitSummary(p)} · ขั้นต่ำ ฿{p.MinimumSpend ?? 0m:N0}",
            Code = p.VoucherCode,
            ApplicableScope = (p.ApplicableScope ?? "all").Trim().ToLowerInvariant(),
            PromoType = (p.PromoType ?? "percent").Trim().ToLowerInvariant(),
            ExpiryText = $"ถึง {PromoAndRedemptionHelper.FormatThaiDate(p.EndDate)}"
        }).ToList();

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // READ: Action สำหรับแสดงรายการสินค้าฟิกเกอร์ทั้งหมดในร้าน พร้อมระบบค้นหา ตัวกรองหลายมิติ และแบ่งหน้า (หน้าละ 20 ชิ้น)
    // WHY: รองรับการสืบค้นข้อมูลผ่านคำค้นหาและการกรองตามหมวดหมู่ ซีรีส์ สถานะสต็อก และช่วงราคา
    // โดยใช้ LINQ กับ Entity Framework Core และมีระบบสำรองข้อมูลแคตตาล็อกรองรับกรณีฐานข้อมูลออฟไลน์
    public IActionResult Collection(string? searchTerm, string? category, string? series, string? status, string? priceRange, string? promoCode = null, string? promoScope = null, int page = 1)
    {
        // ขั้นตอนที่ 1: สร้างออบเจกต์ ProductCatalogViewModel และกำหนดค่าตัวกรองเริ่มต้นที่ได้รับจากคำขอ
        var viewModel = new ProductCatalogViewModel
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            SelectedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            SelectedSeries = string.IsNullOrWhiteSpace(series) ? null : series.Trim(),
            SelectedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            SelectedPriceRange = string.IsNullOrWhiteSpace(priceRange) ? null : priceRange.Trim(),
            SelectedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : PromoAndRedemptionHelper.NormalizeCode(promoCode),
            SelectedPromoScope = string.IsNullOrWhiteSpace(promoScope) ? null : promoScope.Trim().ToLowerInvariant(),
            PageSize = 20
        };

        // กำหนดรายการตัวเลือกช่วงราคามาตรฐานสำหรับแสดงใน Select Dropdown
        viewModel.AvailablePriceRanges = new List<string>
        {
            "ทั้งหมด",
            "฿0 - ฿1,000",
            "฿1,001 - ฿3,000",
            "฿3,001 - ฿5,000",
            "มากกว่า ฿5,000"
        };

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool usedDatabase = false;

        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // ขั้นตอนที่ 2: ตรวจสอบการเชื่อมต่อฐานข้อมูลจริง และตรวจสอบว่ามีข้อมูลสินค้าพร้อมใช้งานหรือไม่
            if (_db.Database.CanConnect())
            {
                // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                var baseQuery = _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.IsActive == true || p.IsActive == null);

                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                int catalogTotal = baseQuery.Count();
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (catalogTotal > 0)
                {
                    usedDatabase = true;

                    // ดึงรายการหมวดหมู่สินค้าจริงที่มีอยู่ในฐานข้อมูล
                    var dbCategories = _db.Categories
                        .Where(c => c.IsActive == true || c.IsActive == null)
                        .OrderBy(c => c.CategoryId)
                        .Select(c => c.CategoryName)
                        .Distinct()
                        .ToList();

                    viewModel.AvailableCategories = new List<string> { "ทั้งหมด" };
                    viewModel.AvailableCategories.AddRange(dbCategories);

                    // ดึงรายการชื่อซีรีส์จริงที่มีอยู่ในฐานข้อมูล
                    var dbSeries = _db.Products
                        .Where(p => (p.IsActive == true || p.IsActive == null) && !string.IsNullOrEmpty(p.SeriesName))
                        .Select(p => p.SeriesName!)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    viewModel.AvailableSeries = new List<string> { "ทั้งหมด" };
                    viewModel.AvailableSeries.AddRange(dbSeries);

                    // ขั้นตอนที่ 3: กรองข้อมูลสินค้าตามเงื่อนไขผ่าน LINQ
                    var query = baseQuery.AsQueryable();

                    // เงื่อนไขที่ 1: ค้นหาคำค้นหา searchTerm แบบไม่แยกตัวพิมพ์เล็กใหญ่ ในชื่อสินค้า ซีรีส์ แบรนด์ หรือรหัสสินค้า (รองรับการพิมพ์รหัสโดยไม่ใส่ขีด -)
                    if (!string.IsNullOrWhiteSpace(viewModel.SearchTerm))
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string term = viewModel.SearchTerm.ToLower();
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
                        query = query.Where(p => p.Name.ToLower().Contains(term)
                            || (p.SeriesName != null && p.SeriesName.ToLower().Contains(term))
                            || (p.BrandName != null && p.BrandName.ToLower().Contains(term))
                            || (p.Sku != null && (p.Sku.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLower().Contains(termNoHyphen))))
                            || (p.Barcode != null && !string.IsNullOrEmpty(termNoHyphen) && p.Barcode.Replace("-", "").ToLower().Contains(termNoHyphen)));
                    }

                    // เงื่อนไขที่ 2: กรองตามหมวดหมู่สินค้า category
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedCategory) && viewModel.SelectedCategory != "ทั้งหมด")
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string selectedCat = viewModel.SelectedCategory;
                        query = query.Where(p => p.Category != null && (
                            p.Category.CategoryName == selectedCat ||
                            p.Category.Slug == selectedCat ||
                            (selectedCat == "สเกลฟิกเกอร์" && p.Category.CategoryName == "ฟิกเกอร์สเกล") ||
                            (selectedCat == "ตุ๊กตาผ้า" && p.Category.CategoryName == "ของสะสมและตุ๊กตา")
                        ));
                    }

                    // เงื่อนไขที่ 3: กรองตามซีรีส์ series
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedSeries) && viewModel.SelectedSeries != "ทั้งหมด")
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string selectedSeries = viewModel.SelectedSeries;
                        query = query.Where(p => p.SeriesName == selectedSeries);
                    }

                    // เงื่อนไขที่ 4: กรองตามสถานะสินค้า (รองรับทั้งภาษาไทยและรหัสจากหน้าแรก instock / preorder)
                    if (string.Equals(viewModel.SelectedStatus, "มีสินค้า", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(viewModel.SelectedStatus, "instock", StringComparison.OrdinalIgnoreCase))
                    {
                        viewModel.SelectedStatus = "มีสินค้า";
                        query = query.Where(p => !p.IsPreorder && p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0);
                    }
                    // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                    else if (string.Equals(viewModel.SelectedStatus, "สินค้าหมด", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(viewModel.SelectedStatus, "outofstock", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(viewModel.SelectedStatus, "เปิดสั่งจอง", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(viewModel.SelectedStatus, "preorder", StringComparison.OrdinalIgnoreCase))
                    {
                        viewModel.SelectedStatus = "สินค้าหมด";
                        query = query.Where(p => p.IsPreorder || p.Inventory == null || (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) <= 0);
                    }

                    // เงื่อนไขที่ 5: กรองตามช่วงราคา priceRange
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPriceRange) && viewModel.SelectedPriceRange != "ทั้งหมด")
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string pr = viewModel.SelectedPriceRange;
                        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                        if (pr == "฿0 - ฿1,000" || pr == "<= 1000" || pr == "0-1000")
                        {
                            query = query.Where(p => p.RetailPrice <= 1000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                        {
                            query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                        {
                            query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                        {
                            query = query.Where(p => p.RetailPrice > 5000m);
                        }
                    }

                    // ตรวจสอบข้อมูลโปรโมชั่นจริงจากฐานข้อมูลเมื่อมีพารามิเตอร์ promoCode หรือ promoScope
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoCode))
                    {
                        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                        var matchedPromo = _db.Promotions.FirstOrDefault(p => p.VoucherCode.ToUpper() == viewModel.SelectedPromoCode);
                        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                        if (matchedPromo != null)
                        {
                            viewModel.SelectedPromoScope = (matchedPromo.ApplicableScope ?? "all").Trim().ToLowerInvariant();
                            viewModel.PromoBannerTitle = $"{matchedPromo.Name} (รหัส {matchedPromo.VoucherCode})";
                            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                            string discountText = (matchedPromo.PromoType == "percent" || matchedPromo.PromoType == "percentage")
                                ? $"ส่วนลด {matchedPromo.DiscountValue:0.#}%"
                                : matchedPromo.PromoType == "double_points"
                                    ? "แต้มสะสม x2"
                                    : $"ส่วนลด ฿{matchedPromo.DiscountValue:N0}";
                            viewModel.PromoBannerCondition = $"ร่วมรายการ: {PromoAndRedemptionHelper.GetScopeThaiLabel(viewModel.SelectedPromoScope)} • {discountText} • ขั้นต่ำ ฿{matchedPromo.MinimumSpend:N0}";
                        }
                    }
                    // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                    else if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoScope))
                    {
                        viewModel.PromoBannerTitle = $"รายการสินค้าตามสิทธิ์โปรโมชั่น";
                        viewModel.PromoBannerCondition = $"ขอบเขตสินค้า: {PromoAndRedemptionHelper.GetScopeThaiLabel(viewModel.SelectedPromoScope)}";
                    }

                    // ดึงรายการผลลัพธ์และแปลงเป็น ProductCardViewModel (พร้อมกรองตามขอบเขตโปรโมชั่นหากระบุ)
                    var filteredProducts = query.ToList();
                    // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoScope))
                    {
                        filteredProducts = filteredProducts
                            .Where(p => PromoAndRedemptionHelper.MatchesScope(p, viewModel.SelectedPromoScope))
                            .ToList();
                    }
                    viewModel.Products = filteredProducts.Select(p => new ProductCardViewModel
                    {
                        ProductId = p.ProductId,
                        Sku = p.Sku,
                        Name = p.Name,
                        SeriesName = p.SeriesName,
                        CategoryName = p.Category?.CategoryName,
                        BrandName = p.BrandName,
                        Scale = p.Scale,
                        RetailPrice = p.RetailPrice,
                        ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "~/media/images/naruto_hero_fig.png",
                        AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0),
                        IsPreorder = p.IsPreorder
                    }).ToList();
                }
            }
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch (Exception)
        {
            // หากเกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล ให้สลับไปใช้ระบบสำรองข้อมูล
            usedDatabase = false;
        }

        // ขั้นตอนที่ 4: กรณีฐานข้อมูลออฟไลน์หรือไม่พบข้อมูล ให้ใช้ข้อมูลสำรองแคตตาล็อกและกรองผ่าน LINQ
        if (!usedDatabase)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var fallbackProducts = GetCatalogFallbackProducts();

            // รวบรวมตัวเลือกหมวดหมู่และซีรีส์ที่มีจากแคตตาล็อกสำรอง
            viewModel.AvailableCategories = new List<string> { "ทั้งหมด" };
            viewModel.AvailableCategories.AddRange(
                fallbackProducts.Select(p => p.CategoryName).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList()!
            );

            viewModel.AvailableSeries = new List<string> { "ทั้งหมด" };
            viewModel.AvailableSeries.AddRange(
                fallbackProducts.Select(p => p.SeriesName).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList()!
            );

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var query = fallbackProducts.AsEnumerable();

            // เงื่อนไขที่ 1: คำค้นหา (รองรับการค้นหารหัสสินค้าแบบไม่ใส่ขีด -)
            if (!string.IsNullOrWhiteSpace(viewModel.SearchTerm))
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string term = viewModel.SearchTerm.ToLower();
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.SeriesName != null && p.SeriesName.ToLower().Contains(term)) ||
                    (p.BrandName != null && p.BrandName.ToLower().Contains(term)) ||
                    (p.Sku != null && (p.Sku.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLower().Contains(termNoHyphen)))));
            }

            // เงื่อนไขที่ 2: หมวดหมู่
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedCategory) && viewModel.SelectedCategory != "ทั้งหมด")
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string selectedCat = viewModel.SelectedCategory;
                query = query.Where(p =>
                    p.CategoryName == selectedCat ||
                    (selectedCat == "scale-figures" && p.CategoryName == "ฟิกเกอร์สเกล") ||
                    (selectedCat == "action-figures" && p.CategoryName == "แอคชั่นฟิกเกอร์") ||
                    (selectedCat == "merchandise" && p.CategoryName == "ของสะสมและตุ๊กตา") ||
                    (selectedCat == "limited-collectibles" && p.CategoryName == "ของสะสมลิมิเต็ด") ||
                    (selectedCat == "สเกลฟิกเกอร์" && p.CategoryName == "ฟิกเกอร์สเกล") ||
                    (selectedCat == "ตุ๊กตาผ้า" && p.CategoryName == "ของสะสมและตุ๊กตา"));
            }

            // เงื่อนไขที่ 3: ซีรีส์
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedSeries) && viewModel.SelectedSeries != "ทั้งหมด")
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string selectedSeries = viewModel.SelectedSeries;
                query = query.Where(p => p.SeriesName == selectedSeries);
            }

            // เงื่อนไขที่ 4: สถานะสินค้ามีสินค้าหรือสินค้าหมด
            if (string.Equals(viewModel.SelectedStatus, "มีสินค้า", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewModel.SelectedStatus, "instock", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.SelectedStatus = "มีสินค้า";
                query = query.Where(p => !p.IsPreorder && p.AvailableStock > 0);
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (string.Equals(viewModel.SelectedStatus, "สินค้าหมด", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewModel.SelectedStatus, "outofstock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewModel.SelectedStatus, "เปิดสั่งจอง", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewModel.SelectedStatus, "preorder", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.SelectedStatus = "สินค้าหมด";
                query = query.Where(p => p.IsPreorder || p.AvailableStock <= 0);
            }

            // เงื่อนไขที่ 5: ช่วงราคา
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedPriceRange) && viewModel.SelectedPriceRange != "ทั้งหมด")
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string pr = viewModel.SelectedPriceRange;
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (pr == "฿0 - ฿1,000" || pr == "<= 1000" || pr == "0-1000")
                {
                    query = query.Where(p => p.RetailPrice <= 1000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                {
                    query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                {
                    query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                {
                    query = query.Where(p => p.RetailPrice > 5000m);
                }
            }

            viewModel.Products = query.ToList();
        }

        // ขั้นตอนที่ 5: คำนวณการแบ่งหน้าสำหรับรายการสินค้าฟิกเกอร์ (หน้าละ 20 ชิ้น)
        viewModel.TotalCount = viewModel.Products.Count;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalProductPages = Math.Max(1, (int)Math.Ceiling((double)viewModel.TotalCount / viewModel.PageSize));
        viewModel.CurrentPage = Math.Clamp(page, 1, totalProductPages);
        viewModel.Products = viewModel.Products
            .Skip((viewModel.CurrentPage - 1) * viewModel.PageSize)
            .Take(viewModel.PageSize)
            .ToList();

        // ขั้นตอนที่ 6: ส่งโมเดล ProductCatalogViewModel ไปยังหน้า View
        return View(viewModel);
    }

    // READ: เมธอดตัวช่วยสำหรับสร้างแคตตาล็อกสินค้าสำรองกรณีฐานข้อมูลออฟไลน์หรือไม่พบข้อมูล
    // WHY: รับประกันว่าหน้าค้นหาและแคตตาล็อกจะสามารถทำงาน กรอง และแสดงผลสินค้า 25 รายการได้อย่างถูกต้องเสมอ
    private static List<ProductCardViewModel> GetCatalogFallbackProducts()
    {
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new List<ProductCardViewModel>
        {            // สินค้าสำรองลำดับที่ 1: นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 1,
                Sku = "FIG-NARUTO-001",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Naruto Shippuden",
                BrandName = "Banpresto",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/6 Scale",
                RetailPrice = 4290.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/naruto_hero_fig.png",
                AvailableStock = 13
            },            // สินค้าสำรองลำดับที่ 2: ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 2,
                Sku = "FIG-TANJIRO-002",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Demon Slayer",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 2450.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/tanjiro_kamado.png",
                AvailableStock = 19
            },            // สินค้าสำรองลำดับที่ 3: ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล (Vocaloid)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 3,
                Sku = "FIG-MIKU-003",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Vocaloid",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 4200.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/hatsune_miku_magical.png",
                AvailableStock = 5
            },            // สินค้าสำรองลำดับที่ 4: มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 4,
                Sku = "FIG-LUFFY-004",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 950.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/luffy_gear5.png",
                AvailableStock = 26
            },            // สินค้าสำรองลำดับที่ 5: อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 5,
                Sku = "FIG-ANYA-005",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด",
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Spy x Family",
                BrandName = "Megahouse",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 1850.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/anya_forger.png",
                AvailableStock = 10
            },            // สินค้าสำรองลำดับที่ 6: นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 6,
                Sku = "FIG-NARUTO-006",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Naruto Shippuden",
                BrandName = "Banpresto",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/8 Scale",
                RetailPrice = 1200.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/naruto_vibration_stars.png",
                AvailableStock = 22
            },            // สินค้าสำรองลำดับที่ 7: โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 7,
                Sku = "FIG-GOJO-007",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Megahouse",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 3800.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/gojo_satoru.png",
                AvailableStock = 8
            },            // สินค้าสำรองลำดับที่ 8: โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 8,
                Sku = "FIG-RAIDEN-008",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Genshin Impact",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 5690.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/raiden_shogun.png",
                AvailableStock = 5
            },            // สินค้าสำรองลำดับที่ 9: ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล (FriendHouse Studio)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 9,
                Sku = "FIG-BEAR-009",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล",
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "FriendHouse Studio",
                BrandName = "FriendHouse Original",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 590.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/cloud_bear_plush.png",
                AvailableStock = 35
            },            // สินค้าสำรองลำดับที่ 10: โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 10,
                Sku = "FIG-ZORO-010",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 2790.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/roronoa_zoro.png",
                AvailableStock = 16
            },            // สินค้าสำรองลำดับที่ 11: คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 11,
                Sku = "FIG-NEZUKO-011",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Demon Slayer",
                BrandName = "Aniplex",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/8 Scale",
                RetailPrice = 3450.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/nezuko_kamado.png",
                AvailableStock = 12
            },            // สินค้าสำรองลำดับที่ 12: ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 12,
                Sku = "FIG-MEGUMI-012",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Kotobukiya",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/8 Scale",
                RetailPrice = 3190.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/megumi_fushiguro.png",
                AvailableStock = 14
            },            // สินค้าสำรองลำดับที่ 13: จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 13,
                Sku = "FIG-ZONG-013",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ",
                CategoryName = "ของสะสมลิมิเต็ด",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 6200.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/zhongli_morax.png",
                AvailableStock = 4
            },            // สินค้าสำรองลำดับที่ 14: อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 14,
                Sku = "FIG-SASUKE-014",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Naruto Shippuden",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 2590.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/sasuke_uchiha.png",
                AvailableStock = 19
            },            // สินค้าสำรองลำดับที่ 15: เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์ (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 15,
                Sku = "FIG-RENBK-015",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Demon Slayer",
                BrandName = "Aniplex",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/8 Scale",
                RetailPrice = 4600.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/rengoku_kyojuro.png",
                AvailableStock = 8
            },            // สินค้าสำรองลำดับที่ 16: ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 16,
                Sku = "FIG-YOR-016",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Spy x Family",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 3250.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/yor_forger.png",
                AvailableStock = 13
            },            // สินค้าสำรองลำดับที่ 17: ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด (Vocaloid)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 17,
                Sku = "FIG-MIKUS-017",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด",
                CategoryName = "ของสะสมลิมิเต็ด",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Vocaloid",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 2890.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/hatsune_miku_sakura.png",
                AvailableStock = 9
            },            // สินค้าสำรองลำดับที่ 18: โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 18,
                Sku = "FIG-ACE-018",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "One Piece",
                BrandName = "MegaHouse",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 1890.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/portgas_d_ace.png",
                AvailableStock = 18
            },            // สินค้าสำรองลำดับที่ 19: อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 19,
                Sku = "FIG-ITAD-019",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 1450.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/itadori_yuji.png",
                AvailableStock = 24
            },            // สินค้าสำรองลำดับที่ 20: กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 20,
                Sku = "FIG-GANYU-020",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 5400.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/ganyu_plenilune.png",
                AvailableStock = 5
            },            // สินค้าสำรองลำดับที่ 21: ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม (FriendHouse Studio)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 21,
                Sku = "FIG-CAT-021",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม",
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "FriendHouse Studio",
                BrandName = "FriendHouse Original",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 490.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/lucky_cat_plush.png",
                AvailableStock = 45
            },            // สินค้าสำรองลำดับที่ 22: โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 22,
                Sku = "FIG-CHOP-022",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ",
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 650.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/chopper_plush.png",
                AvailableStock = 32
            },            // สินค้าสำรองลำดับที่ 23: คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์ (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 23,
                Sku = "FIG-NOBARA-023",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 1350.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/nobara_kugisaki.png",
                AvailableStock = 23
            },            // สินค้าสำรองลำดับที่ 24: ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 24,
                Sku = "FIG-HUTAO-024",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง",
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "1/7 Scale",
                RetailPrice = 5200.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/hu_tao_ghost.png",
                AvailableStock = 7
            },            // สินค้าสำรองลำดับที่ 25: ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าและรหัส SKU ประจำโมเดล
                ProductId = 25,
                Sku = "FIG-LOID-025",
                // ชื่อโมเดลฟิกเกอร์และหมวดหมู่สินค้า
                Name = "ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส",
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัดและแบรนด์ผู้ผลิตลิขสิทธิ์
                SeriesName = "Spy x Family",
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกลและราคาจำหน่ายปลีก
                Scale = "Non-scale",
                RetailPrice = 2390.00m,
                // พาธไฟล์รูปภาพสินค้าและสต็อกพร้อมส่ง
                ImageUrl = "~/media/images/loid_forger.png",
                AvailableStock = 16
            }
        };
    }

    // READ: แสดงรายการโปรโมชั่นและสิทธิพิเศษของร้านพร้อมระบบค้นหา (GET Method)
    // WHY: ดึงรายการโปรโมชั่นที่เปิดใช้งาน แปลงรหัสประเภทและขอบเขตเป็นภาษาไทยผ่าน PromotionCatalogViewModel
    public IActionResult Promotion(string? searchTerm, string? promoType)
    {
        // ขั้นตอนที่ 1: ดึงรายการโปรโมชั่นที่เปิดใช้งานจริงจากฐานข้อมูลเท่านั้น
        var query = _db.Promotions.Where(p => p.IsActive == true);

        // ขั้นตอนที่ 2: กรองตามคำค้นหาชื่อโปรโมชั่นหรือรหัสคูปอง (รองรับการค้นหาแบบไม่ใส่ขีด -)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string term = searchTerm.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            query = query.Where(p => p.Name.ToLower().Contains(term)
                || p.VoucherCode.ToLower().Contains(term)
                || (!string.IsNullOrEmpty(termNoHyphen) && p.VoucherCode.Replace("-", "").ToLower().Contains(termNoHyphen)));
        }

        // ขั้นตอนที่ 3: กรองตามประเภทโปรโมชั่น
        if (!string.IsNullOrWhiteSpace(promoType) && promoType != "all")
        {
            query = query.Where(p => p.PromoType == promoType);
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var promotions = query.OrderBy(p => p.PromotionId).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var customerId = GetCurrentCustomerId();
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.IsLoggedIn = customerId.HasValue;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var collectedCodes = customerId.HasValue
            ? PromoAndRedemptionHelper.ReadCollectedCodes(
                // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
                HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey))
            : new List<string>();

        // ขั้นตอนที่ 4: ประกอบข้อมูลลงใน PromotionCatalogViewModel พร้อมแปลงข้อความเป็นภาษาไทยบริสุทธิ์
        var viewModel = new PromotionCatalogViewModel
        {
            SearchTerm = searchTerm,
            SelectedType = promoType ?? "all",
            CollectedCodes = collectedCodes
        };
        viewModel.AddRange(promotions);

        viewModel.PromotionCards = promotions.Select(p =>
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string tagThai = PromoAndRedemptionHelper.GetPromoTypeThaiLabel(p.PromoType);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string scopeCode = (p.ApplicableScope ?? "all").Trim().ToLowerInvariant();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string scopeThai = PromoAndRedemptionHelper.GetScopeThaiLabel(scopeCode);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string discountText = PromoAndRedemptionHelper.FormatPromoBenefitSummary(p);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string startFormatted = PromoAndRedemptionHelper.FormatThaiDate(p.StartDate);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string endFormatted = PromoAndRedemptionHelper.FormatThaiDate(p.EndDate);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string limitDesc = p.UsageLimit.HasValue
                ? $"จำกัดจำนวน {p.UsageLimit.Value:N0} สิทธิ์ (ใช้แล้ว {p.CurrentUsageCount:N0} สิทธิ์)"
                : $"ใช้ได้ไม่จำกัดสิทธิ์ (ใช้แล้ว {p.CurrentUsageCount:N0} สิทธิ์)";

            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromotionCardItemViewModel
            {
                PromotionId = p.PromotionId,
                Tag = tagThai,
                Title = p.Name,
                Description = $"ใช้ได้กับ{scopeThai} · {limitDesc}",
                Period = $"{startFormatted} - {endFormatted}",
                Condition = $"รหัสคูปอง: {p.VoucherCode} • {discountText} • ยอดขั้นต่ำ ฿{p.MinimumSpend ?? 0m:N0}",
                VoucherCode = p.VoucherCode,
                ApplicableScope = scopeCode,
                IsCollected = collectedCodes.Contains(p.VoucherCode, StringComparer.OrdinalIgnoreCase)
            };
        }).ToList();

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // ACTION: ลูกค้ากดเก็บคูปองโปรโมชั่นไว้ใช้ในหน้าตะกร้าสินค้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CollectPromoCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult CollectPromoCode(string? voucherCode, string? returnUrl)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนเก็บโค้ดโปรโมชั่น";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalized = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(normalized))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "ไม่พบรหัสคูปองที่ต้องการเก็บ";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Promotion");
        }

        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var promo = _db.Promotions.FirstOrDefault(p => p.VoucherCode.ToUpper() == normalized);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (promo == null || promo.IsActive != true)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"ไม่พบโปรโมชั่นรหัส \"{normalized}\" หรือถูกปิดใช้งานแล้ว";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Promotion");
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var today = PromoAndRedemptionHelper.TodayGregorian();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var startDate = PromoAndRedemptionHelper.NormalizeGregorianDate(promo.StartDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var endDate = PromoAndRedemptionHelper.NormalizeGregorianDate(promo.EndDate);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today < startDate)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" ยังไม่เริ่มเปิดให้เก็บ (เริ่ม {PromoAndRedemptionHelper.FormatThaiDate(startDate)})";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Promotion");
        }
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today > endDate)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Promotion");
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" ถูกใช้ครบสิทธิ์แล้ว";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Promotion");
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var collected = PromoAndRedemptionHelper.ReadCollectedCodes(
            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey));
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!collected.Contains(promo.VoucherCode, StringComparer.OrdinalIgnoreCase))
        {
            collected.Add(promo.VoucherCode.ToUpperInvariant());
            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.SetString(
                PromoAndRedemptionHelper.CollectedPromoSessionKey,
                PromoAndRedemptionHelper.SerializeCollectedCodes(collected));
            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"เก็บคูปอง \"{promo.VoucherCode}\" ({promo.Name}) เรียบร้อยแล้ว สามารถกดใช้งานได้ที่หน้าตะกร้าสินค้า";
        }
        else
        {
            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"คุณมีคูปอง \"{promo.VoucherCode}\" อยู่ในรายการที่เก็บไว้แล้ว";
        }

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return Redirect(returnUrl);
        }
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("Promotion");
    }

    // ฟังก์ชัน Customer สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Customer()
    {
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("Index");
    }

    // ฟังก์ชัน GetCurrentCustomerId สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private int? GetCurrentCustomerId()
    {
        // WHY: แยกสิทธิ์ Customer ออกจาก Staff/Owner เพื่อไม่ให้ Session พนักงานหรือเจ้าของร้านรั่วเข้ามาใช้งานตะกร้า/การจองของลูกค้า
        var customerId = HttpContext.Session.GetInt32("CustomerUserId");
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (customerId.HasValue)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return customerId.Value;
        }
        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (role == "customer")
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return HttpContext.Session.GetInt32("UserId");
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return null;
    }

    // READ: แสดงรายการสินค้าในตะกร้า (Cart)
    // WHY: ตรวจสอบสิทธิ์ผู้ใช้ แปลงข้อมูลเป็น CartViewModel คำนวณยอดเงินรวมและดึงเบอร์โทรศัพท์สำหรับแบบฟอร์มยืนยัน
    public IActionResult Cart()
    {
        // ขั้นตอนที่ 1: ดึงรหัสผู้ใช้งานปัจจุบันจาก Session เพื่อตรวจสอบสิทธิ์การเข้าสู่ระบบ
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเพื่อดูตะกร้าสินค้าของคุณ";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: ดึงข้อมูลผู้ใช้งานจริงจากฐานข้อมูลเพื่อเตรียมเบอร์โทรศัพท์ติดต่อ
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);

        // ขั้นตอนที่ 3: ดึงรายการสินค้าในตะกร้าของผู้ใช้คนนี้ พร้อมข้อมูลสินค้า หมวดหมู่ และคลังสินค้า
        var rawItems = _db.Cartitems
            .Include(c => c.Product)
                .ThenInclude(p => p.Category)
            .Include(c => c.Product)
                .ThenInclude(p => p.Inventory)
            .Where(c => c.UserId == userId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        // ขั้นตอนที่ 4: แปลงข้อมูลเป็น CartViewModel และ CartItemViewModel
        var viewModel = new CartViewModel
        {
            UserPhone = user?.Phone ?? string.Empty,
            PickupBranch = "FriendHouse HQ สาขาสยามสแควร์",
            Items = rawItems.Select(c => new CartItemViewModel
            {
                CartItemId = c.CartItemId,
                ProductId = c.ProductId,
                ProductName = c.Product?.Name ?? "สินค้าไม่มีชื่อ",
                ImageUrl = !string.IsNullOrEmpty(c.Product?.ImageUrl) ? c.Product.ImageUrl : "~/media/images/naruto_hero_fig.png",
                UnitPrice = c.Product?.RetailPrice ?? 0m,
                Quantity = c.Quantity,
                AvailableStock = (c.Product?.Inventory?.QuantityOnHand ?? 0) - (c.Product?.Inventory?.ReservedQuantity ?? 0),
                ScheduleNotice = "กำหนดเข้าร้าน: สินค้าพร้อมส่งมอบตามรอบพรีออเดอร์"
            }).ToList()
        };

        // ขั้นตอนที่ 4.1: ตรวจสอบคูปองโปรโมชั่นที่เก็บไว้และคูปองที่ใช้งานอยู่ในตะกร้าสินค้า
        var collectedCodes = PromoAndRedemptionHelper.ReadCollectedCodes(
            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey));
        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        string? appliedCode = HttpContext.Session.GetString(PromoAndRedemptionHelper.AppliedPromoSessionKey);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (collectedCodes.Any())
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var collectedPromos = _db.Promotions
                .Where(p => (p.IsActive == true || p.IsActive == null) && collectedCodes.Contains(p.VoucherCode.ToUpper()))
                .OrderBy(p => p.PromotionId)
                .ToList();

            viewModel.CollectedPromos = collectedPromos.Select(p =>
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string label = (p.PromoType == "percent" || p.PromoType == "percentage")
                    ? $"ลด {p.DiscountValue:0.#}%"
                    : p.PromoType == "double_points"
                        ? "แต้ม x2"
                        : $"ลด ฿{p.DiscountValue:N0}";
                // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                return new CollectedPromoOptionViewModel
                {
                    VoucherCode = p.VoucherCode,
                    Title = p.Name,
                    BenefitLabel = $"{label} (ขั้นต่ำ ฿{p.MinimumSpend:N0})",
                    IsApplied = !string.IsNullOrEmpty(appliedCode)
                        && string.Equals(p.VoucherCode, appliedCode, StringComparison.OrdinalIgnoreCase)
                };
            }).ToList();
        }

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(appliedCode) && rawItems.Any())
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var promo = FindPromotionByVoucherCode(appliedCode);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var eval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, rawItems);
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (eval.IsValid)
            {
                viewModel.AppliedPromoCode = eval.VoucherCode;
                viewModel.AppliedPromoName = eval.PromoName;
                viewModel.PromoBenefitSummary = eval.BenefitSummary;
                viewModel.DiscountAmount = eval.DiscountAmount;
                viewModel.IsDoublePoints = eval.IsDoublePoints;
            }
            else
            {
                viewModel.AppliedPromoCode = appliedCode;
                viewModel.PromoValidationError = eval.ErrorMessage;
            }
        }

        // ส่งเบอร์โทรศัพท์ผ่าน ViewBag เพื่อรองรับการทำงานร่วมกันกับ View
        ViewBag.UserPhone = viewModel.UserPhone;

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // ค้นหาโปรโมชั่นจากรหัสคูปอง (รองรับทั้งพิมพ์ตรงและไม่ใส่ขีด -)
    private Promotion? FindPromotionByVoucherCode(string? voucherCode)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalized = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string noHyphen = PromoAndRedemptionHelper.NormalizeCodeNoHyphen(voucherCode);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(normalized)) return null;

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return _db.Promotions.ToList().FirstOrDefault(p =>
            p.VoucherCode.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(noHyphen) && PromoAndRedemptionHelper.NormalizeCodeNoHyphen(p.VoucherCode) == noHyphen));
    }

    // ACTION: กรอกหรือเลือกใช้รหัสโปรโมชั่นกับรายการสินค้าในตะกร้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน ApplyPromoCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult ApplyPromoCode(string? voucherCode)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนใช้งานรหัสโปรโมชั่น";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cleanCode = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(cleanCode))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณากรอกรหัสคูปองโปรโมชั่นก่อนกดใช้งาน";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Cart");
        }

        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var rawItems = _db.Cartitems
            .Include(c => c.Product).ThenInclude(p => p.Category)
            .Include(c => c.Product).ThenInclude(p => p.Inventory)
            .Where(c => c.UserId == userId.Value)
            .ToList();

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!rawItems.Any())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเพิ่มสินค้าลงในตะกร้าก่อนใช้รหัสโปรโมชั่น";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Cart");
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var promo = FindPromotionByVoucherCode(cleanCode);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var eval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, rawItems);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!eval.IsValid || eval.Promotion == null)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = eval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นนี้ได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Cart");
        }

        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        HttpContext.Session.SetString(PromoAndRedemptionHelper.AppliedPromoSessionKey, eval.Promotion.VoucherCode);
        // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
        TempData["SuccessMessage"] = $"ใช้คูปอง \"{eval.Promotion.VoucherCode}\" สำเร็จ ({eval.BenefitSummary})";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("Cart");
    }

    // ACTION: ยกเลิกการใช้รหัสโปรโมชั่นในตะกร้าสินค้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน RemovePromoCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult RemovePromoCode()
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนดำเนินการ";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        HttpContext.Session.Remove(PromoAndRedemptionHelper.AppliedPromoSessionKey);
        // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
        TempData["SuccessMessage"] = "ยกเลิกการใช้รหัสโปรโมชั่นในตะกร้าเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("Cart");
    }

    // Action สำหรับเพิ่มสินค้าลงในตะกร้าของสมาชิก (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน AddToCart รับ productId (รหัสสินค้า), quantity (จำนวนชิ้น), returnUrl (URL ย้อนกลับ) และ goToCart (พาไปหน้าตะกร้าทันทีเมื่อกดซื้อตอนนี้)
    public IActionResult AddToCart(int productId, int quantity = 1, string? returnUrl = null, bool goToCart = false)
    {
        // ขั้นตอนที่ 1: ดึงรหัสผู้ใช้งานปัจจุบันจาก Session เพื่อตรวจสอบสิทธิ์การเข้าสู่ระบบ
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนให้ผู้ใช้งานเข้าสู่ระบบก่อน
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนเพื่อเพิ่มสินค้าลงในตะกร้าและเลือกวันรับสินค้า";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        string? safeReturnUrl = (!goToCart && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            ? returnUrl
            : null;

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (quantity <= 0)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "จำนวนสินค้าที่ต้องการเพิ่มลงตะกร้าต้องอย่างน้อย 1 ชิ้น";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return Redirect(safeReturnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 2: แปลงรหัสสินค้าเพื่อรองรับรหัสข้อมูลดั้งเดิม (201-205 ให้แปลงเป็น 1-5)
        if (productId >= 201 && productId <= 205)
        {
            // ลดค่ารหัสสินค้าลง 200 เพื่อให้ตรงกับรหัสจริงในฐานข้อมูล
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าที่เปิดใช้งานอยู่จากตาราง Products พร้อมดึงข้อมูลสต็อก Inventories (ไม่ Fallback ไปยังสินค้าอื่น)
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId && p.IsActive != false);

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้าหรือสินค้าปิดการขาย
            TempData["ErrorMessage"] = "ไม่พบสินค้านี้ในระบบหรือสินค้าปิดวางขายชั่วคราว";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(safeReturnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 3: คำนวณจำนวนสินค้าคงเหลือที่พร้อมให้สั่งซื้อ (QuantityOnHand ลบด้วย ReservedQuantity)
        int availableQty = product.IsPreorder
            ? 0
            : (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
        // ตรวจสอบว่ามีสินค้าพร้อมให้สั่งซื้อหรือไม่
        if (availableQty <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีสินค้าหมดชั่วคราว
            TempData["ErrorMessage"] = "ขออภัย สินค้านี้หมดชั่วคราว";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(safeReturnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 4: ค้นหาว่าผู้ใช้งานคนนี้มีสินค้านี้อยู่ในตะกร้าแล้วหรือไม่
        var existingItem = _db.Cartitems.FirstOrDefault(c => c.UserId == userId.Value && c.ProductId == product.ProductId);
        // กำหนดจำนวนสินค้าที่ต้องการสั่งเพิ่ม
        int requestedQty = quantity;
        // ตรวจสอบจำนวนเดิมที่อยู่ในตะกร้าแล้ว (หากยังไม่มีให้เป็น 0)
        int currentInCart = existingItem?.Quantity ?? 0;

        // ตรวจสอบว่าจำนวนเดิมในตะกร้ารวมกับจำนวนที่ต้องการสั่งใหม่ เกินกว่าสต็อกพร้อมจองหรือไม่
        if (currentInCart + requestedQty > availableQty)
        {
            // กำหนดข้อความแจ้งเตือนระบุจำนวนคงเหลือจริง และจำนวนที่มีในตะกร้าแล้ว
            TempData["ErrorMessage"] = $"สินค้าคงเหลือพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้น (ในตะกร้ามีแล้ว {currentInCart} ชิ้น)";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าตะกร้าสินค้า
            return Redirect(safeReturnUrl ?? Url.Action("Cart", "Home")!);
        }

        // ขั้นตอนที่ 5: ตรวจสอบเพื่อเพิ่มหรือสร้างรายการสินค้าในตะกร้า
        if (existingItem != null)
        {
            // กรณีมีสินค้านี้ในตะกร้าอยู่แล้ว ให้เพิ่มจำนวนชิ้นเดิมด้วยจำนวนใหม่
            existingItem.Quantity += requestedQty;
        }
        else
        {
            // กรณีเป็นสินค้าใหม่ในตะกร้า ให้สร้างแถวข้อมูล Cartitem ใหม่
            _db.Cartitems.Add(new Cartitem
            {
                // กำหนดรหัสผู้ใช้งานเจ้าของตะกร้า
                UserId = userId.Value,
                // กำหนดรหัสสินค้าที่เลือก
                ProductId = product.ProductId,
                // กำหนดจำนวนสินค้าที่ต้องการสั่ง
                Quantity = requestedQty,
                // บันทึกวันและเวลาปัจจุบันที่เพิ่มลงตะกร้า
                CreatedAt = DateTime.UtcNow
            });
        }

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จระบุชื่อสินค้า
        TempData["SuccessMessage"] = goToCart
            ? $"เพิ่ม \"{product.Name}\" ลงในตะกร้าแล้ว กรุณาเลือกวันที่รับสินค้าและตรวจสอบเบอร์โทรศัพท์เพื่อยืนยันคำสั่งซื้อ"
            : $"เพิ่ม \"{product.Name}\" ลงในตะกร้าเรียบร้อยแล้ว";
        // หากกด "ซื้อตอนนี้" ให้พาไปหน้าตะกร้าทันที ส่วนปุ่ม "ตะกร้า" ให้กลับไปยังหน้าเดิม
        return Redirect(safeReturnUrl ?? Url.Action("Cart", "Home")!);
    }

    // Action สำหรับปรับปรุงจำนวนสินค้าในตะกร้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน UpdateCartQuantity รับ cartItemId (รหัสรายการตะกร้า) และ quantity (จำนวนชิ้นใหม่)
    public IActionResult UpdateCartQuantity(int cartItemId, int quantity)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนแก้ไขตะกร้าสินค้า";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลรายการในตะกร้าเฉพาะของสมาชิกที่ล็อกอินอยู่
        var item = _db.Cartitems.Include(c => c.Product).ThenInclude(p => p.Inventory)
            .FirstOrDefault(c => c.CartItemId == cartItemId && c.UserId == userId.Value);
        // ตรวจสอบว่าพบรายการในตะกร้าหรือไม่
        if (item == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการสินค้านี้ในตะกร้าของคุณ";
            // เปลี่ยนเส้นทางกลับไปหน้าตะกร้า
            return RedirectToAction("Cart");
        }

        // ตรวจสอบกรณีจำนวนที่ปรับลดลงเหลือน้อยกว่าหรือเท่ากับศูนย์
        if (quantity <= 0)
        {
            // สั่งลบรายการสินค้านั้นออกจากตาราง Cartitems
            _db.Cartitems.Remove(item);
            // กำหนดข้อความแจ้งผลการลบรายการออกจากตะกร้า
            TempData["SuccessMessage"] = "ลบรายการออกจากตะกร้าเรียบร้อยแล้ว";
        }
        // กรณีจำนวนมากกว่าศูนย์
        else
        {
            // คำนวณจำนวนสต็อกคงเหลือที่พร้อมให้สั่งจอง (QuantityOnHand ลบด้วย ReservedQuantity)
            int availableQty = (item.Product?.Inventory?.QuantityOnHand ?? 0) - (item.Product?.Inventory?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าจำนวนที่ปรับ เกินกว่าสินค้าคงเหลือที่มีอยู่หรือไม่
            if (quantity > availableQty)
            {
                // กำหนดข้อความแจ้งเตือนว่าสินค้าไม่เพียงพอ
                TempData["ErrorMessage"] = $"สินค้าพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้นเท่านั้น";
                // เปลี่ยนเส้นทางกลับหน้าตะกร้า
                return RedirectToAction("Cart");
            }

            // อัปเดตจำนวนสินค้าชิ้นใหม่ในรายการตะกร้า
            item.Quantity = quantity;
            // กำหนดข้อความแจ้งผลการปรับจำนวนสำเร็จ
            TempData["SuccessMessage"] = "ปรับจำนวนสินค้าในตะกร้าเรียบร้อยแล้ว";
        }

        // บันทึกการเปลี่ยนแปลงลง MySQL Database
        _db.SaveChanges();
        // สั่งเปลี่ยนเส้นทางกลับหน้าตะกร้าเพื่อแสดงยอดเงินใหม่
        return RedirectToAction("Cart");
    }

    // Action สำหรับลบสินค้า 1 รายการออกจากตะกร้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน RemoveFromCart รับ cartItemId ที่ต้องการลบ
    public IActionResult RemoveFromCart(int cartItemId)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนลบรายการในตะกร้า";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหารายการในตาราง Cartitems เฉพาะของผู้ใช้ปัจจุบัน
        var item = _db.Cartitems.FirstOrDefault(c => c.CartItemId == cartItemId && c.UserId == userId.Value);
        // ตรวจสอบว่าพบรายการในตะกร้าหรือไม่
        if (item != null)
        {
            // สั่งลบแถวข้อมูลนั้นออกจากฐานข้อมูล
            _db.Cartitems.Remove(item);
            // บันทึกการลบลง MySQL Database
            _db.SaveChanges();
            // กำหนดข้อความแจ้งผลสำเร็จผ่าน TempData
            TempData["SuccessMessage"] = "ลบรายการออกจากตะกร้าแล้ว";
        }
        else
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "ไม่พบรายการสินค้าที่ต้องการลบในตะกร้าของคุณ";
        }

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // Action สำหรับล้างรายการสินค้าทั้งหมดในตะกร้าของผู้ใช้ (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน ClearCart ลบรายการทั้งหมดของผู้ใช้ที่ล็อกอินอยู่
    public IActionResult ClearCart()
    {
        // ดึงรหัสผู้ใช้งานปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนล้างตะกร้าสินค้า";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการสินค้าทั้งหมดในตะกร้าที่เป็นของผู้ใช้งานคนนี้
        var items = _db.Cartitems.Where(c => c.UserId == userId.Value).ToList();
        // สั่งลบรายการทั้งหมดพร้อมกันด้วย RemoveRange
        _db.Cartitems.RemoveRange(items);
        // บันทึกการเปลี่ยนแปลงลง MySQL Database
        _db.SaveChanges();
        // กำหนดข้อความแจ้งผลการล้างตะกร้าสำเร็จ
        TempData["SuccessMessage"] = "ล้างรายการสินค้าในตะกร้าเรียบร้อยแล้ว";

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // ฟังก์ชันกลางตรวจสอบความถูกต้องของวันที่รับสินค้า (ใช้ร่วมกันระหว่างตะกร้าสินค้าและการสั่งซื้อโดยตรง)
    private static bool TryResolvePickupDate(string? pickupDate, out DateOnly resolvedDate, out string? errorMessage)
    {
        resolvedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        errorMessage = null;

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(pickupDate))
        {
            errorMessage = "กรุณาเลือกวันที่สะดวกเข้ารับสินค้าที่หน้าร้าน";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!DateOnly.TryParseExact(pickupDate.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate)
            && !DateOnly.TryParse(pickupDate.Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
        {
            errorMessage = "รูปแบบวันที่รับสินค้าไม่ถูกต้อง กรุณาเลือกใหม่อีกครั้ง";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // แปลงปี พ.ศ. -> ค.ศ. ก่อนตรวจสอบเทียบกับวันปัจจุบันเสมอ (ป้องกันปี 2569 ถูกยอมรับเป็นอนาคต 543 ปี)
        parsedDate = PromoAndRedemptionHelper.NormalizeGregorianDate(parsedDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var todayGregorian = PromoAndRedemptionHelper.TodayGregorian();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (parsedDate < todayGregorian)
        {
            errorMessage = "ไม่สามารถเลือกวันที่ย้อนหลังได้ กรุณาเลือกวันปัจจุบันเป็นต้นไป";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (parsedDate > todayGregorian.AddYears(2))
        {
            errorMessage = "กรุณาเลือกวันที่นัดรับสินค้าภายในระยะเวลาไม่เกิน 2 ปีข้างหน้า";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        resolvedDate = parsedDate;
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return true;
    }

    // Action ยืนยันการสั่งจองพรีออเดอร์จากตะกร้าสินค้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน ConfirmPreorder รับเบอร์โทรติดต่อ สาขาที่ต้องการรับ วันที่สะดวกเข้ารับสินค้า และรหัสคูปอง (ถ้ามี)
    public IActionResult ConfirmPreorder(string? contactPhone, string? pickupBranch, string? pickupDate, string? voucherCode = null)
    {
        // ขั้นตอนที่ 1: ดึงรหัสลูกค้าปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนสีแดงผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนยืนยันการจอง";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของวันที่นัดรับสินค้าที่ลูกค้าเลือกจากหน้าตะกร้า
        if (!TryResolvePickupDate(pickupDate, out DateOnly estPickupDate, out string? dateError))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = dateError;
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Cart");
        }

        // เริ่มต้นฐานข้อมูล Transaction เพื่อรองรับความปลอดภัยของสต็อกและป้องกัน Race Condition
        using var transaction = _db.Database.BeginTransaction();
        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // ดึงรายการสินค้าทั้งหมดในตะกร้าของผู้ใช้คนนี้พร้อมข้อมูลสินค้า หมวดหมู่ และสต็อก
            var cartItems = _db.Cartitems
                .Include(c => c.Product).ThenInclude(p => p.Category)
                .Include(c => c.Product).ThenInclude(p => p.Inventory)
                .Where(c => c.UserId == userId.Value)
                .ToList();

            // ตรวจสอบว่าในตะกร้ามีรายการสินค้าหรือไม่
            if (!cartItems.Any())
            {
                // แจ้งเตือนว่าไม่มีสินค้าในตะกร้า
                TempData["ErrorMessage"] = "ไม่มีสินค้าในตะกร้า กรุณาเลือกสินค้าก่อนยืนยันการจอง";
                // เปลี่ยนเส้นทางกลับไปหน้ารวมสินค้า
                return RedirectToAction("Collection");
            }

            // ขั้นตอนที่ 2 (Race Condition Check): ตรวจสอบสถานะการวางขายและสต็อกคงเหลือจริงใน Transaction
            foreach (var item in cartItems)
            {
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (item.Product.IsActive == false)
                {
                    transaction.Rollback();
                    // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product.Name}\" ปิดการวางขายชั่วคราว กรุณานำออกจากตะกร้าก่อนยืนยัน";
                    // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                    return RedirectToAction("Cart");
                }
                // ค้นหาสต็อกของสินค้าแต่ละชิ้นในตาราง Inventories
                var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                // คำนวณสต็อกพร้อมจอง = จำนวนคงคลัง (QuantityOnHand) ลบด้วยยอดที่ถูกจองแล้ว (ReservedQuantity)
                int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
                // ตรวจสอบว่าสต็อกคงเหลือไม่พอต่อจำนวนที่ต้องการสั่งจอง
                if (inv == null || available < item.Quantity)
                {
                    // ยกเลิกคำสั่งทั้งหมดใน Transaction (Rollback) เพื่อไม่ให้ตัดสต็อก
                    transaction.Rollback();
                    // แจ้งเตือนข้อความว่าสินค้าไม่เพียงพอ
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น) กรุณาปรับจำนวนในตะกร้า";
                    // เปลี่ยนเส้นทางกลับหน้าตะกร้า
                    return RedirectToAction("Cart");
                }
            }

            // คำนวณยอดเงินรวมทั้งหมดของสินค้าในตะกร้าก่อนส่วนลดและตรวจสอบรหัสโปรโมชั่นจริง
            decimal originalSubtotal = cartItems.Sum(c => c.Product.RetailPrice * c.Quantity);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal discountAmount = 0m;
            PromoEvaluationResult? appliedEval = null;

            string? effectivePromoCode = !string.IsNullOrWhiteSpace(voucherCode)
                ? voucherCode
                // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
                : HttpContext.Session.GetString(PromoAndRedemptionHelper.AppliedPromoSessionKey);

            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (!string.IsNullOrWhiteSpace(effectivePromoCode))
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var promo = FindPromotionByVoucherCode(effectivePromoCode);
                appliedEval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, cartItems);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (!appliedEval.IsValid || appliedEval.Promotion == null)
                {
                    transaction.Rollback();
                    // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                    TempData["ErrorMessage"] = appliedEval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นได้ กรุณาตรวจสอบอีกครั้ง";
                    // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                    return RedirectToAction("Cart");
                }
                discountAmount = appliedEval.DiscountAmount;
                appliedEval.Promotion.CurrentUsageCount += 1;
            }

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal total = Math.Max(0m, originalSubtotal - discountAmount);
            // สร้างรหัสใบสั่งจองพรีออเดอร์รูปแบบ PO-2026-XXXX (ไม่ซ้ำกับรหัสเดิมในระบบ)
            var reservationNo = "PO-2026-" + Random.Shared.Next(1000, 9999);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                ? contactPhone.Trim()
                : (user?.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!PromoAndRedemptionHelper.IsValidPhoneNumber(resolvedPhone))
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "กรุณาระบุเบอร์โทรศัพท์ติดต่อที่ถูกต้อง (9-10 หลัก)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Cart");
            }

            // สร้างอ็อบเจกต์คำสั่งจอง Preorder ใหม่
            var preorder = new Preorder
            {
                // กำหนดหมายเลขใบจอง
                ReservationNumber = reservationNo,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                UserId = userId.Value,
                // กำหนดเบอร์โทรติดต่อ (ใช้ค่าที่กรอกหรือเบอร์สมาชิกที่ Auto-fill)
                ContactPhone = resolvedPhone,
                // กำหนดวันนัดรับสินค้าตามวันที่ลูกค้าเลือกจริงในหน้าตะกร้า
                EstimatedPickupDate = estPickupDate,
                // กำหนดสถานะเริ่มต้นเป็น พร้อมรับสินค้าที่ร้าน (ReadyForPickup)
                CurrentStatus = "ReadyForPickup",
                // กำหนดยอดเงินสุทธิรวม
                TotalFinalPrice = total,
                // กำหนดสาขาที่นัดรับสินค้า
                PickupBranch = pickupBranch ?? "FriendHouse HQ สาขาสยามสแควร์",
                // บันทึกวันเวลาที่ทำรายการสั่งจอง
                CreatedAt = DateTime.Now
            };

            // เพิ่มข้อมูลคำสั่งจองลงในตาราง Preorders
            _db.Preorders.Add(preorder);
            // บันทึกการเปลี่ยนแปลงเพื่อให้ได้ค่า PreorderId ที่สร้างขึ้น
            _db.SaveChanges();

            // วนลูปบันทึกรายการสินค้าแต่ละชิ้นลงใน Preorderitems พร้อมสำรองยอดจอง
            foreach (var item in cartItems)
            {
                // เพิ่มรายการสินค้าที่สั่งจองลงในตาราง Preorderitems
                _db.Preorderitems.Add(new Preorderitem
                {
                    // ผูกรหัสใบจอง PreorderId
                    PreorderId = preorder.PreorderId,
                    // ผูกรหัสสินค้า ProductId
                    ProductId = item.ProductId,
                    // บันทึกราคาต่อหน่วย ณ ขณะที่สั่งจอง
                    UnitPrice = item.Product.RetailPrice,
                    // บันทึกจำนวนชิ้นที่จอง
                    Quantity = item.Quantity
                });

                // สำรองยอดจองสินค้าในคลัง (Phase 1: ล็อคยอด ReservedQuantity)
                var inv = _db.Inventories.First(i => i.ProductId == item.ProductId);
                // บวกยอดจองเพิ่มขึ้นตามจำนวนชิ้นที่สั่ง
                inv.ReservedQuantity += item.Quantity;
            }

            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (appliedEval != null && appliedEval.Promotion != null)
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var promoPayload = new PreorderPromoPayload
                {
                    ReservationNumber = reservationNo,
                    VoucherCode = appliedEval.VoucherCode,
                    PromoName = appliedEval.PromoName,
                    PromoType = appliedEval.PromoType,
                    OriginalSubtotal = originalSubtotal,
                    DiscountAmount = discountAmount,
                    IsDoublePoints = appliedEval.IsDoublePoints
                };
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string logCode = PromoAndRedemptionHelper.GenerateLogCode("PRM");

                // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                _db.Systemlogs.Add(new Systemlog
                {
                    LogCode = logCode,
                    LogLevel = "INFO",
                    Module = "preorder.promo",
                    Message = $"ใช้รหัสโปรโมชั่น {appliedEval.VoucherCode} กับใบจอง #{reservationNo} ({appliedEval.BenefitSummary})",
                    DetailPayload = JsonSerializer.Serialize(promoPayload),
                    UserId = userId.Value,
                    Timestamp = DateTime.Now
                });
            }

            // ลบรายการสินค้าออกจากตะกร้าเมื่อสั่งจองสำเร็จ
            _db.Cartitems.RemoveRange(cartItems);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
            _db.SaveChanges();

            // ยืนยันคำสั่งทั้งหมดใน Transaction (Commit) เมื่อทำงานครบทุกขั้นตอน
            transaction.Commit();

            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.Remove(PromoAndRedemptionHelper.AppliedPromoSessionKey);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string promoSuccessSuffix = appliedEval != null
                ? $" (ใช้โค้ด {appliedEval.VoucherCode}: {appliedEval.BenefitSummary})"
                : string.Empty;

            // แจ้งเตือนข้อความสำเร็จพร้อมหมายเลขใบจองผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว{promoSuccessSuffix} สามารถมารับสินค้าได้ที่หน้าร้าน";
            // เปลี่ยนเส้นทางไปยังหน้าแสดงสถานะคำสั่งจองของลูกค้า
            return RedirectToAction("PreorderStatus");
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch (Exception)
        {
            // กรณีเกิดข้อผิดพลาด ให้ยกเลิกคำสั่งทั้งหมดใน Transaction ทันที
            transaction.Rollback();
            // แจ้งเตือนข้อความข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับไปที่หน้าตะกร้าสินค้า
            return RedirectToAction("Cart");
        }
    }

    // Action สั่งจองสินค้าโดยตรงจากหน้ารายละเอียดสินค้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน DirectPreorder รับ productId, quantity, contactPhone และ pickupDate
    public IActionResult DirectPreorder(int productId, int quantity = 1, string? contactPhone = null, string? pickupDate = null)
    {
        // ขั้นตอนที่ 1: ตรวจสอบการเข้าสู่ระบบของลูกค้า
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบหรือยัง
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนทำรายการสั่งจองสินค้า";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าใช้
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (quantity <= 0)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "จำนวนสินค้าที่สั่งจองต้องอย่างน้อย 1 ชิ้น";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Collection");
        }

        // แปลงรหัสสินค้ารองรับข้อมูลดั้งเดิม 201-205 ให้เป็น 1-5
        if (productId >= 201 && productId <= 205)
        {
            // หักลบค่ารหัสสินค้า 200
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าที่เปิดใช้งานอยู่จริงในตาราง Products ด้วยรหัส ProductId (ไม่ Fallback ไปยังสินค้าอื่น)
        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId && p.IsActive != false);

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // แจ้งเตือนกรณีไม่พบสินค้าหรือปิดวางขาย
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการสั่งจองหรือสินค้าปิดวางขายชั่วคราว";
            // เปลี่ยนเส้นทางกลับไปหน้ารวมสินค้า
            return RedirectToAction("Collection");
        }

        // ตรวจสอบวันนัดรับสินค้าอย่างเข้มงวด (ต้องระบุและถูกต้อง)
        if (!TryResolvePickupDate(pickupDate, out DateOnly estDate, out string? dateError))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = dateError;
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("ProductDetail", new { id = product.ProductId });
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int qty = quantity;

        // เริ่มต้นฐานข้อมูล Transaction
        using var transaction = _db.Database.BeginTransaction();
        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // ค้นหาข้อมูลสต็อกในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == product.ProductId);
            // คำนวณสต็อกพร้อมขายจริง = QuantityOnHand ลบด้วย ReservedQuantity (หากเคยตั้งเป็นพรีออเดอร์ให้นับเป็นหมด)
            int available = product.IsPreorder
                ? 0
                : (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าสินค้ามีพอให้สั่งซื้อหรือไม่
            if (inv == null || available < qty)
            {
                // ยกเลิกคำสั่งทั้งหมดใน Transaction
                transaction.Rollback();
                // แจ้งเตือนข้อความว่าสินค้าหมดหรือไม่เพียงพอ
                TempData["ErrorMessage"] = available <= 0
                    ? $"ขออภัย สินค้า \"{product.Name}\" หมดชั่วคราว"
                    : $"ขออภัย สินค้า \"{product.Name}\" มีจำนวนคงเหลือไม่เพียงพอ (เหลือ {available} ชิ้น)";
                // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้าเดิม
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                ? contactPhone.Trim()
                : (user?.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!PromoAndRedemptionHelper.IsValidPhoneNumber(resolvedPhone))
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "กรุณาระบุเบอร์โทรศัพท์ติดต่อที่ถูกต้อง (9-10 หลัก)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            // สร้างหมายเลขใบสั่งจองพรีออเดอร์
            var reservationNo = "PO-2026-" + Random.Shared.Next(1000, 9999);
            // คำนวณยอดเงินรวมสุทธิ
            decimal totalPrice = product.RetailPrice * qty;

            // สร้างอ็อบเจกต์ Preorder สำหรับบันทึกคำสั่งจอง
            var preorder = new Preorder
            {
                // กำหนดหมายเลขใบจอง
                ReservationNumber = reservationNo,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                UserId = userId.Value,
                // กำหนดเบอร์โทรศัพท์ผู้ติดต่อ
                ContactPhone = resolvedPhone,
                // กำหนดวันนัดรับสินค้า
                EstimatedPickupDate = estDate,
                // กำหนดสถานะเป็น พร้อมรับสินค้าที่ร้าน
                CurrentStatus = "ReadyForPickup",
                // กำหนดยอดเงินสุทธิ
                TotalFinalPrice = totalPrice,
                // กำหนดสาขาที่นัดรับสินค้า
                PickupBranch = "FriendHouse HQ สาขาสยามสแควร์",
                // บันทึกวันเวลาที่สร้างคำสั่งจอง
                CreatedAt = DateTime.Now
            };

            // เพิ่มคำสั่งจองลงในตาราง Preorders
            _db.Preorders.Add(preorder);
            // บันทึกการเปลี่ยนแปลงเพื่อให้ได้ค่า PreorderId
            _db.SaveChanges();

            // เพิ่มรายการสินค้าที่สั่งจองลงในตาราง Preorderitems
            _db.Preorderitems.Add(new Preorderitem
            {
                // ผูกรหัสใบจอง
                PreorderId = preorder.PreorderId,
                // ผูกรหัสสินค้า
                ProductId = product.ProductId,
                // บันทึกราคาต่อหน่วย
                UnitPrice = product.RetailPrice,
                // บันทึกจำนวนชิ้นที่จอง
                Quantity = qty
            });

            // สำรองยอดจองสินค้าในคลัง (Phase 1: ล็อคยอด ReservedQuantity)
            inv.ReservedQuantity += qty;
            // บันทึกการเปลี่ยนแปลงยอดจองลงฐานข้อมูล
            _db.SaveChanges();

            // ยืนยันคำสั่งทั้งหมดใน Transaction (Commit)
            transaction.Commit();

            // แจ้งเตือนข้อความสำเร็จผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว มารับสินค้าและชำระเงินที่หน้าร้านได้ทันที";
            // เปลี่ยนเส้นทางไปยังหน้าแสดงสถานะคำสั่งจอง
            return RedirectToAction("PreorderStatus");
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch (Exception)
        {
            // ยกเลิกคำสั่งทั้งหมดใน Transaction เมื่อมีข้อผิดพลาด
            transaction.Rollback();
            // แจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้า
            return RedirectToAction("ProductDetail", new { id = product.ProductId });
        }
    }
    

    // READ: แสดงหน้ารายละเอียดสินค้าฟิกเกอร์จากฐานข้อมูลผ่าน ProductDetailViewModel (GET Method)
    // WHY: ดึงข้อมูลสินค้า หมวดหมู่ และคลังสินค้า แปลงเป็น ProductDetailViewModel ตามมาตรฐาน MVC
    public IActionResult ProductDetail(int? id)
    {
        // ขั้นตอนที่ 1: ค้นหาข้อมูลสินค้าตามรหัส id ที่ระบุ
        Product? product = null;

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (id.HasValue && id.Value > 0)
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (id.Value >= 301 && id.Value <= 304)
            {
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // แปลงรหัสสินค้าดั้งเดิม (201-205) ให้ตรงกับรหัสจริงในฐานข้อมูล (1-5)
            int targetId = (id.Value >= 201 && id.Value <= 205) ? (id.Value - 200) : id.Value;

            // ดึงข้อมูลสินค้าที่เปิดใช้งานจริงพร้อมหมวดหมู่และสต็อกคงคลัง (ไม่ Fallback ไปยังสินค้าอื่น)
            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == targetId && p.IsActive != false);

            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (product == null)
            {
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการหรือสินค้าปิดวางขายชั่วคราว";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Collection");
            }
        }
        else
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.IsFeaturedHero && p.IsActive != false)
                // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                ?? _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .FirstOrDefault(p => p.IsActive != false);

            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (product == null)
            {
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ยังไม่มีรายการสินค้าเปิดวางขายในขณะนี้";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Collection");
            }
        }

        // ขั้นตอนที่ 2: ประกอบข้อมูลเป็น ProductDetailViewModel
        int availableStock = product.IsPreorder
            ? 0
            : Math.Max(0, (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0));

        // สร้างอินสแตนซ์ ProductDetailViewModel เพื่อส่งไปยัง View
        var detailVm = new ProductDetailViewModel
        {
            // กำหนดรหัสสินค้าหลัก
            ProductId = product.ProductId,
            // กำหนดรหัส SKU สินค้า
            Sku = product.Sku,
            // กำหนดชื่อฟิกเกอร์เต็ม
            Name = product.Name,
            // กำหนดชื่อหมวดหมู่สินค้า
            CategoryName = product.Category?.CategoryName ?? "ฟิกเกอร์สเกล",
            // กำหนดชื่อซีรีส์ของฟิกเกอร์
            SeriesName = product.SeriesName ?? "คอลเลกชัน FriendHouse",
            // กำหนดแบรนด์ผู้ผลิตลิขสิทธิ์
            BrandName = product.BrandName ?? "ผู้ผลิตลิขสิทธิ์แท้",
            // กำหนดอัตราส่วนสเกลโมเดล
            Scale = product.Scale ?? "มาตรฐาน",
            // กำหนดความสูงของฟิกเกอร์ในหน่วยเซนติเมตร
            HeightCm = product.HeightCm,
            // กำหนดวัสดุที่ใช้ผลิตโมเดล
            Material = product.Material ?? "PVC + ABS เกรดพรีเมียม",
            // กำหนดราคาขายปลีกหน้าร้าน
            RetailPrice = product.RetailPrice,
            // ราคาพรีออเดอร์ (ไม่ระบุสำหรับสินค้าพร้อมส่ง)
            PreorderPrice = null,
            // กำหนดพาธรูปภาพหลักของสินค้า
            ImageUrl = !string.IsNullOrEmpty(product.ImageUrl) ? product.ImageUrl : "~/media/images/naruto_hero_fig.png",
            // กำหนดพาธไฟล์โมเดล 3 มิติ (.glb)
            Model3DPath = product.Model3Dpath,
            // กำหนดจำนวนสินค้าคงเหลือที่พร้อมส่งมอบ
            AvailableStock = availableStock,
            // กำหนดสถานะสินค้าว่าไม่ใช่พรีออเดอร์ (แสดงเป็นสินค้าหมดเมื่อไม่มีสต็อก)
            IsPreorder = false,
            // กำหนดข้อความระบุระยะเวลาที่คาดว่าจะได้รับสินค้า
            ExpectedArrival = availableStock > 0 ? "มีสินค้าพร้อมส่งมอบหน้าร้าน" : "สินค้าหมดชั่วคราว",
            // กำหนดคำอธิบายรายละเอียดคุณลักษณะสินค้า
            Description = $"{product.Name} ผลิตโดย {product.BrandName ?? "แบรนด์ลิขสิทธิ์แท้"} ซีรีส์ {product.SeriesName ?? "ลิขสิทธิ์แท้"} สเกล {product.Scale ?? "มาตรฐาน"} วัสดุ {product.Material ?? "PVC/ABS คุณภาพสูง"} สินค้าลิขสิทธิ์แท้ รับสินค้าและชำระเงินที่หน้าร้าน FriendHouse สยามสแควร์",
            // กำหนดตารางข้อมูลจำเพาะเชิงเทคนิคของฟิกเกอร์
            Specs = new List<KeyValuePair<string, string>>
            {
                // ข้อมูลจำเพาะ: หมวดหมู่สินค้า
                new KeyValuePair<string, string>("หมวดหมู่", product.Category?.CategoryName ?? "ฟิกเกอร์สเกล"),
                // ข้อมูลจำเพาะ: ซีรีส์ต้นสังกัด
                new KeyValuePair<string, string>("ซีรีส์", product.SeriesName ?? "FriendHouse ลิขสิทธิ์แท้"),
                // ข้อมูลจำเพาะ: ค่ายผู้ผลิตโมเดล
                new KeyValuePair<string, string>("ผู้ผลิต", product.BrandName ?? "ผู้ผลิตลิขสิทธิ์แท้"),
                // ข้อมูลจำเพาะ: ขนาดสเกล
                new KeyValuePair<string, string>("สเกล", product.Scale ?? "มาตรฐาน"),
                // ข้อมูลจำเพาะ: ชนิดวัสดุ
                new KeyValuePair<string, string>("วัสดุ", product.Material ?? "PVC + ABS เกรดพรีเมียม"),
                // ข้อมูลจำเพาะ: สถานะสต็อกปัจจุบัน
                new KeyValuePair<string, string>("สถานะ", availableStock > 0 ? "มีสินค้าพร้อมส่งมอบหน้าร้าน" : "สินค้าหมดชั่วคราว")
            }
        };

        // ขั้นตอนที่ 3: ดึงสินค้าแนะนำเพิ่มเติมที่เกี่ยวข้อง (Related Products) สูงสุด 5 รายการ
        // กฎ: IsActive != false, !IsPreorder, สต็อกคงเหลือ (QuantityOnHand - ReservedQuantity) > 0 และไม่รวมสินค้าปัจจุบัน
        var inStockProducts = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.ProductId != product.ProductId
                     && p.IsActive != false
                     && !p.IsPreorder
                     && p.Inventory != null
                     && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0)
            .ToList();

        // ขั้นตอนที่ 4: ลำดับความสำคัญในการคัดเลือก (1. ซีรีส์เดียวกัน -> 2. หมวดหมู่เดียวกัน -> 3. สินค้าเข้าใหม่ล่าสุด)
        var selectedRelated = new List<Product>();

        // ลำดับที่ 1: ดึงสินค้าในซีรีส์เดียวกัน (SeriesName ตรงกัน)
        if (!string.IsNullOrWhiteSpace(product.SeriesName))
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var sameSeries = inStockProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.SeriesName) && string.Equals(p.SeriesName.Trim(), product.SeriesName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .ToList();
            selectedRelated.AddRange(sameSeries.Take(5));
        }

        // ลำดับที่ 2: ดึงสินค้าในหมวดหมู่เดียวกัน (CategoryId ตรงกัน) หากยังไม่ครบ 5 รายการ
        if (selectedRelated.Count < 5)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var sameCategory = inStockProducts
                .Where(p => p.CategoryId == product.CategoryId && !selectedRelated.Any(r => r.ProductId == p.ProductId))
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .ToList();
            selectedRelated.AddRange(sameCategory.Take(5 - selectedRelated.Count));
        }

        // ลำดับที่ 3: เติมเต็มด้วยสินค้าเข้าใหม่ล่าสุดที่มีในสต็อก จนครบ 5 รายการ
        if (selectedRelated.Count < 5)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var newestInStock = inStockProducts
                .Where(p => !selectedRelated.Any(r => r.ProductId == p.ProductId))
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .ToList();
            selectedRelated.AddRange(newestInStock.Take(5 - selectedRelated.Count));
        }

        // ขั้นตอนที่ 5: แปลงรายการสินค้าแนะนำที่คัดเลือกได้เป็น ProductCardViewModel
        detailVm.RelatedProducts = selectedRelated.Take(5).Select(p => new ProductCardViewModel
        {
            // กำหนดรหัสสินค้าแนะนำ
            ProductId = p.ProductId,
            // กำหนดรหัส SKU สินค้า
            Sku = p.Sku,
            // กำหนดชื่อฟิกเกอร์แนะนำ
            Name = p.Name,
            // กำหนดชื่อซีรีส์สินค้า
            SeriesName = p.SeriesName,
            // กำหนดชื่อหมวดหมู่สินค้า
            CategoryName = p.Category?.CategoryName,
            // กำหนดแบรนด์ผู้ผลิต
            BrandName = p.BrandName,
            // กำหนดอัตราส่วนสเกล
            Scale = p.Scale,
            // กำหนดราคาขายปลีก
            RetailPrice = p.RetailPrice,
            // กำหนดรูปภาพสินค้า หากไม่มีให้ใช้รูปสำรอง
            ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "~/media/images/naruto_hero_fig.png",
            // คำนวณสต็อกพร้อมขายจริง (ยอดคงเหลือลบยอดจอง)
            AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0),
            // กำหนดสถานะพรีออเดอร์
            IsPreorder = p.IsPreorder
        }).ToList();

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(detailVm);
    }

    // READ: แสดงรายการสถานะพรีออเดอร์ของสมาชิก พร้อมระบบค้นหาและตัวกรองสถานะ (Preorder Status)
    // WHY: ดึงรายการสถานะพรีออเดอร์ทั้งหมดของลูกค้า แปลงเป็น PreorderListViewModel พร้อม Tracking Steps
    public IActionResult PreorderStatus(string? searchTerm, string? statusFilter)
    {
        // ขั้นตอนที่ 1: ดึงรหัสลูกค้าปัจจุบันจาก Session (ห้ามนำรายการของลูกค้ารายอื่นมาแสดงให้ผู้เยี่ยมชมทั่วไป)
        var userId = GetCurrentCustomerId();
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.IsLoggedIn = userId.HasValue;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawPreorders = new List<Preorder>();

        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (userId.HasValue)
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            rawPreorders = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(pi => pi.Product)
                .Where(p => p.UserId == userId.Value)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        // กรองตามคำค้นหาหมายเลขใบจอง เบอร์โทรศัพท์ หรือชื่อสินค้า (รองรับการค้นหาโดยไม่ต้องพิมพ์ขีด - หรือ # เช่น PO20269187, 20269187, 9187)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // แปลงคำค้นหาเป็นตัวพิมพ์เล็กและสร้างตัวแปรที่ตัดเครื่องหมายขีด (-) เครื่องหมาย # และช่องว่างออกทั้งหมด
            string term = searchTerm.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");

            rawPreorders = rawPreorders.Where(p =>
            {
                // เตรียมรหัสใบจองและเบอร์โทรศัพท์แบบตัดเครื่องหมายขีด (-) และ # เพื่อเปรียบเทียบ
                string resNoLower = (p.ReservationNumber ?? "").ToLower();
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string resNoNormalized = resNoLower.Replace("-", "").Replace("#", "").Replace(" ", "");
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string phoneLower = (p.ContactPhone ?? "").ToLower();
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string phoneNormalized = phoneLower.Replace("-", "").Replace(" ", "");

                // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                return resNoLower.Contains(term)
                    || (!string.IsNullOrEmpty(termNoHyphen) && resNoNormalized.Contains(termNoHyphen))
                    || phoneLower.Contains(term)
                    || (!string.IsNullOrEmpty(termNoHyphen) && phoneNormalized.Contains(termNoHyphen))
                    || p.Preorderitems.Any(pi => pi.Product != null && (
                        pi.Product.Name.ToLower().Contains(term) ||
                        (!string.IsNullOrEmpty(termNoHyphen) && pi.Product.Sku != null && pi.Product.Sku.Replace("-", "").ToLower().Contains(termNoHyphen))
                    ));
            }).ToList();
        }

        // กรองตามสถานะใบจองพรีออเดอร์
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (statusFilter == "active")
            {
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "PendingConfirm" || p.CurrentStatus == "WaitingStock" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "Reserved").ToList();
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (statusFilter == "completed")
            {
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "Completed").ToList();
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (statusFilter == "cancelled")
            {
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "Cancelled").ToList();
            }
        }

        // ขั้นตอนที่ 2: ประกอบข้อมูล PreorderListViewModel
        var viewModel = new PreorderListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = statusFilter ?? "all"
        };
        // เพิ่มข้อมูล Preorder ดั้งเดิมเพื่อให้เข้ากันได้แบบ IEnumerable<Preorder>
        viewModel.AddRange(rawPreorders);

        // ขั้นตอนที่ 3: แปลงแต่ละใบสั่งจองเป็น PreorderSummaryViewModel พร้อมขั้นตอนการติดตาม (ดึง promoMap เพียงครั้งเดียว)
        var promoLogs = _db.Systemlogs.Where(l => l.Module == "preorder.promo").ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var promoMap = PromoAndRedemptionHelper.BuildPreorderPromoMap(promoLogs);

        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var po in rawPreorders)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string statusBadgeText = "รอดำเนินการ";
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string statusBadgeClass = "bg-amber-50/80 border-amber-200/80 text-amber-800";
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string noticeText = "รายการนี้จะแสดงสถานะอัปเดตแบบเรียลไทม์จากหน้าร้าน เมื่อมารับสินค้าแล้วสามารถแจ้งรหัสใบจองนี้ที่เคาน์เตอร์ได้ทันที";
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var steps = new List<PreorderTrackingStepViewModel>();

            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (po.CurrentStatus == "PendingConfirm" || po.CurrentStatus == "WaitingStock" || po.CurrentStatus == "ReadyForPickup" || po.CurrentStatus == "Reserved")
            {
                statusBadgeText = po.CurrentStatus == "ReadyForPickup" ? "สินค้าพร้อมรับแล้ว" : "รอดำเนินการ";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "รอชำระเงิน/รับสินค้า", Description = "รอสินค้าเข้าสู่สาขาและแจ้งรับ", IsCompleted = false, IsCurrent = true });
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (po.CurrentStatus == "Completed")
            {
                statusBadgeText = "สำเร็จเรียบร้อย";
                statusBadgeClass = "bg-emerald-50/80 border-emerald-200/80 text-emerald-800";
                noticeText = "รายการพรีออเดอร์นี้เสร็จสมบูรณ์เรียบร้อย ลูกค้าชำระเงินและรับสินค้าแล้ว ขอขอบคุณที่เลือกซื้อสินค้ากับ FriendHouse";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "ชำระเงิน/รับสินค้าแล้ว", Description = "รับสินค้าที่เคาน์เตอร์แล้ว", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 3, Title = "สำเร็จ", Description = "เสร็จสมบูรณ์", IsCompleted = true, IsCurrent = true });
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (po.CurrentStatus == "Cancelled")
            {
                statusBadgeText = "ยกเลิก / ไม่รับสินค้า";
                statusBadgeClass = "bg-rose-50/80 border-rose-200/80 text-rose-800";
                noticeText = "รายการนี้ถูกยกเลิกแล้ว เนื่องจากลูกค้ายกเลิกคำสั่งจองหรือไม่ได้มารับสินค้าตามกำหนด";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "ไม่รับสินค้า / ยกเลิก", Description = "ยกเลิกคำสั่งจอง", IsCompleted = false, IsCurrent = false });
            }

            promoMap.TryGetValue(po.ReservationNumber, out var promoMeta);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var summary = new PreorderSummaryViewModel
            {
                PreorderId = po.PreorderId,
                ReservationNumber = po.ReservationNumber,
                ContactPhone = po.ContactPhone,
                PickupBranch = po.PickupBranch ?? "FriendHouse HQ สาขาสยามสแควร์",
                CurrentStatus = po.CurrentStatus,
                StatusBadgeText = statusBadgeText,
                StatusBadgeClass = statusBadgeClass,
                NoticeText = noticeText,
                CreatedAt = po.CreatedAt,
                CreatedAtFormatted = po.CreatedAt.ToString("dd MMM yyyy HH:mm น."),
                EstimatedPickupDate = po.EstimatedPickupDate,
                EstimatedPickupDateFormatted = po.EstimatedPickupDate.ToString("dd MMMM yyyy"),
                TotalFinalPrice = po.TotalFinalPrice,
                AppliedPromoCode = promoMeta?.VoucherCode,
                DiscountAmount = promoMeta?.DiscountAmount ?? 0m,
                OriginalSubtotal = promoMeta?.OriginalSubtotal ?? po.TotalFinalPrice,
                IsDoublePoints = promoMeta?.IsDoublePoints ?? false,
                Steps = steps,
                Items = po.Preorderitems.Select(pi => new PreorderItemDetailViewModel
                {
                    ItemId = pi.ItemId,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product?.Name ?? "รายการฟิกเกอร์พรีออเดอร์",
                    ImageUrl = !string.IsNullOrEmpty(pi.Product?.ImageUrl) ? pi.Product.ImageUrl : "~/media/images/naruto_hero_fig.png",
                    UnitPrice = pi.UnitPrice,
                    Quantity = pi.Quantity
                }).ToList()
            };

            viewModel.Preorders.Add(summary);
        }

        // ขั้นตอนที่ 4: ดึงรายการจองแลกของรางวัลด้วยแต้มสะสมของสมาชิก (RWD-...) เพื่อแสดงรวมเป็นรายการจองของลูกค้า
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rewardMetaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawRedemptions = userId.HasValue
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            ? _db.Rewardredemptions
                .Include(r => r.Reward)
                .Include(r => r.User)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.RedeemedAt)
                .ToList()
            : new List<Rewardredemption>();

        // กรองรายการแลกของรางวัลตามคำค้นหา (รองรับค้นหารหัส RWD แบบไม่ใส่ขีด เบอร์โทรที่ระบุตอนแลก/บัญชี และชื่อของรางวัล)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string term = searchTerm.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            rawRedemptions = rawRedemptions.Where(r =>
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string codeLower = (r.RedemptionCode ?? "").ToLower();
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string codeNoHyphen = codeLower.Replace("-", "").Replace("#", "").Replace(" ", "");
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string accountPhone = PromoAndRedemptionHelper.NormalizePhoneDigits(r.User?.Phone);
                rewardMetaMap.TryGetValue(r.RedemptionCode ?? "", out var rMeta);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string metaPhone = PromoAndRedemptionHelper.NormalizePhoneDigits(rMeta?.ContactPhone);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string rewardTitle = (r.Reward?.Title ?? "").ToLower();

                // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                return codeLower.Contains(term)
                    || (!string.IsNullOrEmpty(termNoHyphen) && codeNoHyphen.Contains(termNoHyphen))
                    || (!string.IsNullOrEmpty(termNoHyphen) && accountPhone.Contains(termNoHyphen))
                    || (!string.IsNullOrEmpty(termNoHyphen) && metaPhone.Contains(termNoHyphen))
                    || rewardTitle.Contains(term);
            }).ToList();
        }

        // กรองรายการแลกของรางวัลตามตัวกรองสถานะให้สอดคล้องกับใบจองพรีออเดอร์
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            rawRedemptions = statusFilter switch
            {
                "active" => rawRedemptions.Where(r => r.Status == "PendingPickup").ToList(),
                "completed" => rawRedemptions.Where(r => r.Status == "Completed").ToList(),
                "cancelled" => rawRedemptions.Where(r => r.Status == "Cancelled").ToList(),
                _ => rawRedemptions
            };
        }

        viewModel.Redemptions = MapRedemptionsToViewModels(rawRedemptions, rewardMetaMap);

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // ฟังก์ชันช่วยแปลงรายการ Rewardredemption จากฐานข้อมูลเป็น ViewModel (ใช้ร่วมกันทั้งหน้า PreorderStatus และ Rewards)
    private static List<RewardRedemptionViewModel> MapRedemptionsToViewModels(
        IEnumerable<Rewardredemption> items,
        IReadOnlyDictionary<string, RedemptionMetaPayload>? metaMap = null)
    {
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return items.Select(red =>
        {
            RedemptionMetaPayload? meta = null;
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (metaMap != null && !string.IsNullOrEmpty(red.RedemptionCode))
            {
                metaMap.TryGetValue(red.RedemptionCode, out meta);
            }

            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new RewardRedemptionViewModel
            {
                RedemptionId = red.RedemptionId,
                RedemptionCode = red.RedemptionCode,
                RewardId = red.RewardId,
                RewardTitle = red.Reward?.Title ?? "ของรางวัลพรีเมียม",
                RewardBrand = red.Reward?.Brand ?? "FriendHouse Exclusive",
                ImageUrl = !string.IsNullOrEmpty(red.Reward?.ImageUrl) ? red.Reward.ImageUrl : "~/media/images/naruto_hero_fig.png",
                PointsSpent = red.PointsSpent,
                Status = red.Status,
                RedeemedAt = red.RedeemedAt,
                RedeemedAtFormatted = red.RedeemedAt.ToString("dd MMM yyyy HH:mm น."),
                CompletedAtFormatted = red.CompletedAt?.ToString("dd MMM yyyy HH:mm น."),
                PickupDateFormatted = meta != null ? PromoAndRedemptionHelper.FormatPickupDateThai(meta.PickupDate) : null,
                ContactPhone = meta?.ContactPhone ?? red.User?.Phone
            };
        }).ToList();
    }

    // DELETE/UPDATE: ยกเลิกคำสั่งจองพรีออเดอร์ของสมาชิกและคืนสต็อกสำรอง (POST Method)
    // WHY: ให้สมาชิกสามารถยกเลิกใบสั่งจองที่ยังไม่ได้รับสินค้าด้วยตนเองผ่านแบบฟอร์ม MVC มาตรฐาน พร้อมคืนค่า ReservedQuantity ให้คลังสินค้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CancelPreorder สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult CancelPreorder(int preorderId)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนยกเลิกใบสั่งจอง";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 1: ค้นหาใบสั่งจองเฉพาะของลูกค้าที่ล็อกอินอยู่เท่านั้น
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId && p.UserId == userId.Value);

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (preorder == null)
        {
            // แจ้งเตือนกรณีไม่พบใบสั่งจอง
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองพรีออเดอร์ของคุณที่ต้องการยกเลิก";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("PreorderStatus");
        }

        // ขั้นตอนที่ 2: ตรวจสอบว่าสถานะปัจจุบันสามารถยกเลิกได้หรือไม่
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "ไม่สามารถยกเลิกใบสั่งจองที่ดำเนินการเสร็จสิ้นหรือถูกยกเลิกไปแล้วได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("PreorderStatus");
        }

        // ขั้นตอนที่ 3: คืนยอดจอง (ReservedQuantity) ในตาราง Inventories สำหรับสินค้าทุกชิ้นในใบจอง
        foreach (var item in preorder.Preorderitems)
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (inv != null)
            {
                // ลดจำนวนที่สำรองไว้โดยไม่ให้ติดลบ
                inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                inv.UpdatedAt = DateTime.Now;
            }
        }

        // คืนสิทธิ์จำนวนการใช้โค้ดโปรโมชั่นหากใบจองนี้ใช้โค้ดส่วนลดไว้
        var promoLogs = _db.Systemlogs
            .Where(l => l.Module == "preorder.promo" && l.DetailPayload != null && l.DetailPayload.Contains(preorder.ReservationNumber))
            .ToList();
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var promoLog in promoLogs)
        {
            // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
            try
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var payload = JsonSerializer.Deserialize<PreorderPromoPayload>(promoLog.DetailPayload!);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (payload != null && string.Equals(payload.ReservationNumber, preorder.ReservationNumber, StringComparison.OrdinalIgnoreCase))
                {
                    // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                    var usedPromo = FindPromotionByVoucherCode(payload.VoucherCode);
                    // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                    if (usedPromo != null && usedPromo.CurrentUsageCount > 0)
                    {
                        usedPromo.CurrentUsageCount -= 1;
                    }
                }
            }
            // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
            catch
            {
            }
        }

        // ขั้นตอนที่ 4: ปรับสถานะใบสั่งจองเป็น Cancelled
        preorder.CurrentStatus = "Cancelled";

        // ขั้นตอนที่ 5: บันทึกประวัติการยกเลิกใบสั่งจองลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("PO-CAN"),
            LogLevel = "INFO",
            Module = "preorder.cancel",
            Message = $"สมาชิกยกเลิกใบสั่งจองพรีออเดอร์หมายเลข #{preorder.ReservationNumber} และคืนสต็อกสำรองเรียบร้อยแล้ว",
            UserId = userId.Value,
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนผลสำเร็จและเปลี่ยนเส้นทางกลับไปหน้าแสดงสถานะพรีออเดอร์
        TempData["SuccessMessage"] = $"ยกเลิกคำสั่งจองพรีออเดอร์ #{preorder.ReservationNumber} และคืนสิทธิ์การจองเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction("PreorderStatus");
    }

    // READ: แสดงแคตตาล็อกแลกของรางวัลด้วยแต้มสะสม (Rewards)
    // WHY: ดึงแต้มสะสมจริงของสมาชิกจากฐานข้อมูล, ดึงประวัติการแลกแต้มจริง และตรวจสอบว่าแต้มพอแลกรางวัลแต่ละชิ้นหรือไม่
    public IActionResult Rewards()
    {
        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int userPoints = 0;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string userPhone = string.Empty;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userRedemptions = new List<RewardRedemptionViewModel>();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool isLoggedIn = userId.HasValue;

        // ขั้นตอนที่ 2: หากเข้าสู่ระบบแล้ว ให้ดึงข้อมูลแต้มสะสมและประวัติการแลกจากฐานข้อมูล MySQL
        if (userId.HasValue)
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (user != null)
            {
                userPoints = user.Points;
                userPhone = user.Phone ?? string.Empty;
            }

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var rewardMetaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var rawRedemptions = _db.Rewardredemptions
                .Include(r => r.Reward)
                .Include(r => r.User)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.RedeemedAt)
                .ToList();

            userRedemptions = MapRedemptionsToViewModels(rawRedemptions, rewardMetaMap);
        }

        // ขั้นตอนที่ 3: ดึงรายการของรางวัลที่พร้อมให้แลกจากฐานข้อมูล
        var rawRewards = _db.Rewards
            .Where(r => r.IsAvailable == true || r.IsAvailable == null)
            .OrderBy(r => r.PointsRequired)
            .ToList();

        // ขั้นตอนที่ 4: แปลงเป็น RewardItemViewModel พร้อมคำนวณ CanAfford ตามแต้มจริงของสมาชิก
        var rewardItems = rawRewards.Select(r => new RewardItemViewModel
        {
            RewardId = r.RewardId,
            RewardCode = r.RewardCode,
            Title = r.Title,
            Brand = r.Brand ?? "FriendHouse Exclusive",
            CategoryTag = !string.IsNullOrEmpty(r.CategoryTag) ? r.CategoryTag : "พิเศษ",
            PointsRequired = r.PointsRequired,
            StockQuantity = r.StockQuantity,
            ImageUrl = !string.IsNullOrEmpty(r.ImageUrl) ? r.ImageUrl : "~/media/images/naruto_hero_fig.png",
            IsAvailable = (r.IsAvailable ?? true) && r.StockQuantity > 0,
            CanAfford = isLoggedIn && (userPoints >= r.PointsRequired)
        }).ToList();

        // ขั้นตอนที่ 5: ประกอบ RewardsCatalogViewModel
        var viewModel = new RewardsCatalogViewModel
        {
            IsLoggedIn = isLoggedIn,
            UserPhone = userPhone,
            UserPoints = userPoints,
            Rewards = rewardItems,
            UserRedemptions = userRedemptions
        };

        // ส่งค่าผ่าน ViewBag เพื่อความเข้ากันได้ย้อนหลัง
        ViewBag.UserPoints = userPoints;
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.IsLoggedIn = isLoggedIn;
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.UserRedemptions = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Where(r => userId.HasValue && r.UserId == userId.Value)
            .OrderByDescending(r => r.RedeemedAt)
            .ToList();

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // =========================================================================
    // โฟลว์: สมาชิกแลกของรางวัลด้วยแต้มสะสม (Redeem Reward)
    // =========================================================================
    // รองรับการส่งคำขอแลกรางวัลผ่านฟอร์ม POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชันดำเนินการแลกของรางวัลโดยรับค่ารหัสของรางวัล วันที่สะดวกรับของ และเบอร์โทรติดต่อ
    public IActionResult RedeemReward(int rewardId, string? pickupDate = null, string? contactPhone = null, string? voucherCode = null)
    {
        // ดึงรหัสสมาชิกปัจจุบันจากเซสชันล็อกอิน
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานได้เข้าสู่ระบบหรือไม่
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนสีแดงลง TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีลูกค้าก่อนทำการแลกของรางวัล";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ป้องกันการใช้รหัสโปรโมชั่นร่วมกับการแลกของรางวัลด้วยแต้มสะสม
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "รหัสคูปองโปรโมชั่นสามารถใช้กับสินค้าในตะกร้าเท่านั้น ไม่สามารถใช้กับการแลกของรางวัลด้วยแต้มสะสมได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบวันที่สะดวกมารับของรางวัลที่ลูกค้าเลือก
        if (!TryResolvePickupDate(pickupDate, out DateOnly estPickupDate, out string? dateError))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = dateError ?? "กรุณาระบุวันที่สะดวกเข้ารับของรางวัล";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Rewards");
        }

        using var transaction = _db.Database.BeginTransaction();
        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // ค้นหาข้อมูลสมาชิกในตาราง Users ด้วยรหัสสมาชิก
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // ค้นหาข้อมูลของรางวัลในตาราง Rewards ด้วยรหัสของรางวัล
            var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);

            // ตรวจสอบว่าไม่พบข้อมูลสมาชิกหรือของรางวัลในฐานข้อมูล
            if (user == null || reward == null)
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ไม่พบข้อมูลสมาชิกหรือของรางวัลที่ต้องการแลก";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                ? contactPhone.Trim()
                : (user.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!PromoAndRedemptionHelper.IsValidPhoneNumber(resolvedPhone))
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "กรุณาระบุเบอร์โทรศัพท์ติดต่อที่ถูกต้อง (9-10 หลัก)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // ตรวจสอบว่าสถานะของรางวัลถูกปิดให้บริการหรือสต็อกหมดหรือไม่
            if (reward.IsAvailable != true || reward.StockQuantity <= 0)
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ขออภัย ของรางวัลชิ้นนี้หมดสต็อกหรือปิดให้บริการชั่วคราว";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // ตรวจสอบว่าแต้มสะสมของสมาชิกน้อยกว่าแต้มที่ต้องใช้หรือไม่
            if (user.Points < reward.PointsRequired)
            {
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = $"แต้มสะสมของคุณไม่เพียงพอ (ต้องการ {reward.PointsRequired} แต้ม แต่คุณมี {user.Points} แต้ม)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // หักแต้มสะสมของสมาชิกตามจำนวนแต้มที่รางวัลต้องการ และจองสต็อกของรางวัลไว้ 1 ชิ้นทันที
            user.Points -= reward.PointsRequired;
            reward.StockQuantity = Math.Max(0, reward.StockQuantity - 1);

            // สุ่มตัวเลข 6 หลักเพื่อใช้สร้างรหัสรับของรางวัล
            int randomSuffix = Random.Shared.Next(100000, 999999);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string redemptionCode = "RWD-" + randomSuffix;

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var redemption = new Rewardredemption
            {
                RedemptionCode = redemptionCode,
                UserId = user.UserId,
                RewardId = reward.RewardId,
                PointsSpent = reward.PointsRequired,
                Status = "PendingPickup",
                RedeemedAt = DateTime.Now
            };
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Rewardredemptions.Add(redemption);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var metaPayload = new RedemptionMetaPayload
            {
                RedemptionCode = redemptionCode,
                PickupDate = estPickupDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ContactPhone = resolvedPhone
            };
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string pickupThai = estPickupDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"));

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Systemlogs.Add(new Systemlog
            {
                LogCode = PromoAndRedemptionHelper.GenerateLogCode("RWD"),
                LogLevel = "INFO",
                Module = "reward.redeem",
                Message = $"สมาชิก {user.FullName ?? user.Email} จองแลกของรางวัล \"{reward.Title}\" รหัส #{redemptionCode} ใช้ {reward.PointsRequired:N0} แต้ม (นัดรับ: {pickupThai} · โทร: {resolvedPhone})",
                DetailPayload = JsonSerializer.Serialize(metaPayload),
                UserId = user.UserId,
                Timestamp = DateTime.Now
            });

            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            transaction.Commit();

            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"จองแลกของรางวัล \"{reward.Title}\" สำเร็จ! รหัสรับของรางวัลคือ {redemptionCode} (นัดรับ {pickupThai} · โทร {resolvedPhone})";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Rewards");
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch (Exception)
        {
            transaction.Rollback();
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการจองแลกของรางวัล กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Rewards");
        }
    }

    // DELETE/UPDATE: ยกเลิกการจองแลกของรางวัลของสมาชิก พร้อมคืนแต้มสะสมและคืนสต็อกของรางวัล (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CancelRedemption สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult CancelRedemption(long redemptionId, string? returnAction)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var userId = GetCurrentCustomerId();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string targetAction = returnAction == "Rewards" ? "Rewards" : "PreorderStatus";

        // ตรวจสอบสิทธิ์ผู้ใช้งานและสถานะเซสชันปัจจุบัน
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการยกเลิกการแลกของรางวัล";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Login", "Account");
        }

        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId && r.UserId == userId.Value);

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (redemption == null)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "ไม่พบรายการจองแลกของรางวัลที่คุณต้องการยกเลิก";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction(targetAction);
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (redemption.Status != "PendingPickup")
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"ไม่สามารถยกเลิกรายการ #{redemption.RedemptionCode} ได้ เนื่องจากดำเนินการเสร็จสิ้นหรือยกเลิกไปแล้ว";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction(targetAction);
        }

        // ปรับสถานะเป็นยกเลิก พร้อมคืนแต้มให้สมาชิกและคืนสต็อกของรางวัล
        redemption.Status = "Cancelled";
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (redemption.User != null)
        {
            redemption.User.Points += redemption.PointsSpent;
        }
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (redemption.Reward != null)
        {
            redemption.Reward.StockQuantity += 1;
        }

        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("RWD-CAN"),
            LogLevel = "INFO",
            Module = "reward.cancel",
            Message = $"สมาชิกยกเลิกการจองแลกของรางวัล #{redemption.RedemptionCode} ({redemption.Reward?.Title}) คืน {redemption.PointsSpent:N0} แต้ม และคืนสต็อกของรางวัลแล้ว",
            UserId = userId.Value,
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
        _db.SaveChanges();

        // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
        TempData["SuccessMessage"] = $"ยกเลิกการจองแลกของรางวัล #{redemption.RedemptionCode} สำเร็จ ระบบคืน {redemption.PointsSpent:N0} แต้มสะสมและคืนสิทธิ์สต็อกให้เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
        return RedirectToAction(targetAction);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // ฟังก์ชัน Error สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Error()
    {
        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
