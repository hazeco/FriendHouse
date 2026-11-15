// นำเข้าไลบรารีสำหรับการจัดการวัฒนธรรมและรูปแบบวันที่สากล
using System.Globalization;
// นำเข้าไลบรารีสำหรับการวินิจฉัยและติดตามข้อผิดพลาด
using System.Diagnostics;
// นำเข้าไลบรารีสำหรับการแปลงข้อมูลรูปแบบ JSON
using System.Text.Json;
// นำเข้าไลบรารีหลักของ ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc;
// นำเข้าไลบรารี Entity Framework Core สำหรับการเชื่อมต่อฐานข้อมูล
using Microsoft.EntityFrameworkCore;
// นำเข้าโมเดลข้อมูลของระบบ
using Khetsophon_CSI402_8456.Models;
// นำเข้าไลบรารีส่วนขยายของ Microsoft VisualBasic
using Microsoft.VisualBasic;
// นำเข้าไลบรารีจัดการใบรับรองความปลอดภัยทางดิจิทัล
using System.Security.Cryptography.X509Certificates;
// นำเข้าโมเดลมุมมอง (ViewModels) สำหรับการแสดงผลหน้าเว็บ
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คลาส HomeController สำหรับจัดการโครงสร้างข้อมูลและการประมวลผลตามหน้าที่
public class HomeController : Controller
{
    // อ็อบเจกต์ DbContext สำหรับเข้าถึงฐานข้อมูล MySQL
    private readonly Csi402dbContext _db;
    // ฟังก์ชัน HomeController สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public HomeController (Csi402dbContext db)
    {
        // กำหนดค่า DbContext เข้าตัวแปรภายในคลาส
        _db = db;
    }

    // ตรวจสอบสิทธิ์ตามมาตรฐาน 1 เบราว์เซอร์ 1 บทบาท ก่อนรัน Action ใน HomeController
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // อ่านชื่อ Action ปัจจุบันจาก RouteData
        var actionName = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        // อ่านบทบาทปัจจุบันในเซสชันของเบราว์เซอร์
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // ยกเว้นหน้าจัดการผู้ใช้ (UserList, CreateCustomerUser, UpdateCustomerUser, DeleteCustomerUser) และหน้า Lab9-11
        bool isSharedAdminOrLab = actionName.Contains("User", StringComparison.OrdinalIgnoreCase)
            // หรือเป็นหน้าแล็บสำหรับทดสอบระบบ
            || actionName.StartsWith("Lab", StringComparison.OrdinalIgnoreCase);
        // หากเบราว์เซอร์ล็อกอินเป็นบทบาทหลังบ้าน (staff, owner, accountant, itsupport) แล้วเข้าหน้าลูกค้า ให้บังคับเปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทนั้นทันที
        if (!isSharedAdminOrLab && !string.IsNullOrEmpty(activeRole) && activeRole != "customer")
        {
            // ดึงเส้นทางหน้าหลักของบทบาทที่ล็อกอินอยู่
            var (targetAct, targetCtrl) = GetRoleHomeRoute(activeRole);
            // บังคับเปลี่ยนเส้นทางไปยังบทบาทปัจจุบันทันที
            context.Result = RedirectToAction(targetAct, targetCtrl);
            // จบการทำงานของตัวกรอง
            return;
        }
        // เรียกการทำงานพื้นฐานของคลาสแม่
        base.OnActionExecuting(context);
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
    // ป้องกันการโจมตีแบบ CSRF
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
                    // กำหนดรหัสนักศึกษา
                    s.StdId = data.StdId;
                    // กำหนดรหัสผ่าน
                    s.StdPassword = data.StdPassword;
                    // กำหนดชื่อ
                    s.StdName = data.StdName;
                    // กำหนดนามสกุล
                    s.StdLastname = data.StdLastname;
                    // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                    _db.Add(s);
                    // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
                    _db.SaveChanges();
                    // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                    return RedirectToAction("Lab9_1", "Home", new { status = "บันทึกข้อมูลสำเร็จ" });
                }
                // จัดการกรณีรหัสนักศึกษานี้มีอยู่ในระบบแล้ว
                else
                {
                    // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
                    ViewBag.Status = "รหัสนักศึกษานี้มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
                    // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
                    return View();
                }
            }
            // จัดการกรณีไม่ได้กรอกรหัสผ่าน
            else
            {
                // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
                ViewBag.Status = "กรุณากรอกรหัสผ่าน";
                // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
                return View();
            }
        }
        // จัดการกรณีข้อมูลนักศึกษาไม่ถูกต้อง
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
    // ป้องกันการโจมตีแบบ CSRF
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน Lab10 สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult Lab10(Labstudent data)
    {
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (User != null)
        {
            // กำหนดรหัสผ่านใหม่
            User.StdPassword = data.StdPassword;
            // กำหนดชื่อใหม่
            User.StdName = data.StdName;
            // กำหนดนามสกุลใหม่
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
    // ป้องกันการโจมตีแบบ CSRF
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
        // จัดการกรณีไม่พบรหัสนักศึกษานี้ในระบบ
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

        // ขั้นตอนที่ 1: สร้าง Query พื้นฐานจากตาราง Users ในฐานข้อมูล MySQL (ไม่รวมบัญชีที่ถูกลบและเก็บถาวรแล้ว)
        var query = _db.Users.Where(u => !u.Email.EndsWith("@deleted.local")).AsQueryable();

        // ขั้นตอนที่ 2: กรองตามคำค้นหา (อีเมล เบอร์โทรศัพท์ ชื่อ-นามสกุล หรือรหัสผู้ใช้ รองรับการพิมพ์แบบไม่ใส่ขีด -)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // ตัดช่องว่างหัวท้ายและสร้างคำค้นหาแบบตัดเครื่องหมายขีด (-) และ # ออก
            string term = searchQuery.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองข้อมูลที่ตรงกับคำค้นหาทั้งแบบมีขีดและไม่มีขีด
            query = query.Where(u => u.Email.ToLower().Contains(term)
                // ตรวจสอบความสอดคล้องกับหมายเลขโทรศัพท์
                || u.Phone.ToLower().Contains(term)
                // ตรวจสอบหมายเลขโทรศัพท์แบบตัดเครื่องหมายขีด
                || (!string.IsNullOrEmpty(termNoHyphen) && u.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(termNoHyphen))
                // ตรวจสอบรหัสผู้ใช้งาน
                || u.UserCode.ToLower().Contains(term)
                // ตรวจสอบรหัสผู้ใช้งานแบบตัดเครื่องหมายขีด
                || (!string.IsNullOrEmpty(termNoHyphen) && u.UserCode.Replace("-", "").ToLower().Contains(termNoHyphen))
                // ตรวจสอบชื่อนามสกุลของผู้ใช้งาน
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
            // กำหนดจำนวนรายการทั้งหมด
            TotalCount = totalCount,
            // กำหนดหมายเลขหน้าปัจจุบัน
            CurrentPage = currentPage,
            // กำหนดขนาดหน้า
            PageSize = pageSize
        };

        // เพิ่มรายการ User ของหน้าปัจจุบันลงในคอลเลกชันพื้นฐานเพื่อความเข้ากันได้
        viewModel.AddRange(pagedUsers);

        // แปลงแต่ละเรคอร์ดในหน้าปัจจุบันเป็น UserAccountItemViewModel
        viewModel.UserItems = pagedUsers.Select(u => new UserAccountItemViewModel
        {
            // กำหนดรหัสผู้ใช้งาน
            UserId = u.UserId,
            // กำหนดรหัสสมาชิก
            UserCode = u.UserCode,
            // กำหนดชื่อ-นามสกุล
            FullName = u.FullName,
            // กำหนดอีเมล
            Email = u.Email,
            // กำหนดเบอร์โทรศัพท์
            Phone = u.Phone,
            // กำหนดชื่อบทบาท
            RoleName = u.RoleName,
            // กำหนดแต้มสะสม
            Points = u.Points,
            // กำหนดสถานะเปิดใช้งาน
            IsActive = u.IsActive ?? true,
            // กำหนดวันเวลาที่สร้างบัญชี
            CreatedAt = u.CreatedAt
            // แปลงเป็นรายการ List
        }).ToList();

        // ส่ง UserManagementViewModel ไปแสดงผลที่หน้า View
        return View(viewModel);
    }

    // CREATE: เพิ่มข้อมูลสมาชิกใหม่จากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการเพิ่มข้อมูลผู้ใช้งานใหม่ (เน้นบทบาทลูกค้า) โดยแฮชรหัสผ่านด้วย BCrypt และบันทึกลงฐานข้อมูล
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
        if (!IsValidPhoneNumber(phone))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9-10 หลัก";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: ตรวจสอบว่ามีอีเมลซ้ำในระบบฐานข้อมูลหรือไม่
        var exists = _db.Users.FirstOrDefault(u => u.Email == email.Trim());
        // หากพบบัญชีเดิมที่ถูกระงับหรือลบแบบ Soft Delete ไปแล้ว ให้ปลดล็อกอีเมลเดิมเพื่อให้สร้างใหม่ได้ทันที
        if (exists != null && exists.IsActive == false)
        {
            // เปลี่ยนอีเมลของเรคอร์ดเดิมเป็นรหัสเก็บถาวรเพื่อคืนสิทธิ์อีเมล
            exists.Email = $"deleted_{exists.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
            // บันทึกการปลดล็อกอีเมลลงฐานข้อมูล
            _db.SaveChanges();
            // ล้างค่าตัวแปรเพื่อให้สร้างบัญชีใหม่ได้ทันที
            exists = null;
        }
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
        // บันทึกประวัติการสร้างบัญชีผู้ใช้งานลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("USR-ADD"),
            // กำหนดระดับความสำคัญเป็น INFO
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการสร้างบัญชี
            Message = $"สร้างบัญชีผู้ใช้งานใหม่: {newUser.Email} (สิทธิ์: {newUser.RoleName})",
            // กำหนดรหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("ITSupportUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // กำหนดวันเวลาที่ทำรายการ
            Timestamp = DateTime.Now
        });
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
            // กำหนดอีเมลใหม่
            user.Email = cleanEmail;
        }
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!IsValidPhoneNumber(phone))
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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

        // เก็บอีเมลเดิมไว้ก่อนดำเนินการลบหรือปลดล็อก
        string deletedEmail = user.Email;
        // ปลดการอ้างอิง UserId ในตาราง Systemlogs เพื่อให้สามารถลบเรคอร์ดผู้ใช้ได้โดยไม่ติด Foreign Key
        var logs = _db.Systemlogs.Where(l => l.UserId == user.UserId).ToList();
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var log in logs)
        {
            // ล้างค่ารหัสผู้ใช้งานเพื่อไม่ให้ติด Foreign Key
            log.UserId = null;
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (hasLinkedRecords)
        {
            // กรณีมีประวัติธุรกรรมผูกพัน ให้ปรับสถานะเป็นระงับการใช้งานและปลดล็อกอีเมลเดิมเพื่อให้สมัครใหม่ได้ทันที
            user.IsActive = false;
            // เปลี่ยนอีเมลของเรคอร์ดเดิมเป็นรหัสเก็บถาวร
            user.Email = $"deleted_{user.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
        }
        // จัดการกรณีไม่มีประวัติธุรกรรมผูกพัน ลบเรคอร์ดผู้ใช้ออกจากตารางได้ทันที
        else
        {
            // ลบข้อมูลผู้ใช้งานออกจากฐานข้อมูล
            _db.Users.Remove(user);
        }

        // บันทึกกิจกรรมการลบบัญชีผู้ใช้งานลง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("USR-DEL"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.users",
            // ข้อความรายละเอียดการลบบัญชี
            Message = $"ลบบัญชีผู้ใช้งานออกจากระบบ: {deletedEmail}",
            // กำหนดรหัสผู้ใช้งานเจ้าของร้านผู้ทำรายการ
            UserId = currentOwnerId,
            // กำหนดวันเวลาที่ทำรายการ
            Timestamp = DateTime.Now
        });
        // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
        _db.SaveChanges();
        // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
        TempData["SuccessMessage"] = $"ลบบัญชีสมาชิก \"{deletedEmail}\" ออกจากระบบเรียบร้อยแล้ว";

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
            // กำหนดอีเมลเข้าสู่ระบบ
            LoginEmail = data?.Email,
            // กำหนดรหัสผ่านเข้าสู่ระบบ
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
            // รวมข้อมูลหมวดหมู่สินค้า
            .Include(p => p.Category)
            // รวมข้อมูลสต็อกสินค้า
            .Include(p => p.Inventory)
            // ค้นหาสินค้าที่เป็นไฮไลท์หลัก
            .FirstOrDefault(p => p.IsFeaturedHero)
            // กรณีไม่พบ ค้นหาสินค้าที่มีโมเดล 3 มิติ
            ?? _db.Products
                // รวมข้อมูลหมวดหมู่สินค้า
                .Include(p => p.Category)
                // รวมข้อมูลสต็อกสินค้า
                .Include(p => p.Inventory)
                // ค้นหาสินค้าที่มีพาธไฟล์โมเดล 3 มิติ
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Model3Dpath))
            // กรณีไม่พบ ให้ดึงสินค้าตัวแรกที่มีในระบบ
            ?? _db.Products
                // รวมข้อมูลหมวดหมู่สินค้า
                .Include(p => p.Category)
                // รวมข้อมูลสต็อกสินค้า
                .Include(p => p.Inventory)
                // ดึงรายการแรกที่พบ
                .FirstOrDefault();

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (heroProduct != null)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int heroAvailableStock = (heroProduct.Inventory?.QuantityOnHand ?? 0) - (heroProduct.Inventory?.ReservedQuantity ?? 0);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            bool heroInStock = !heroProduct.IsPreorder && heroAvailableStock > 0;

            // กำหนดข้อมูล HeroProductViewModel สำหรับแสดงผลสินค้าไฮไลท์หน้าแรก
            viewModel.HeroProduct = new HeroProductViewModel
            {
                // กำหนดรหัสสินค้า
                ProductId = heroProduct.ProductId,
                // กำหนดชื่อสินค้า
                Name = heroProduct.Name,
                // กำหนดคำอธิบายสินค้า
                Description = $"{heroProduct.Name} แบรนด์ {heroProduct.BrandName ?? "ลิขสิทธิ์แท้"} ซีรีส์ {heroProduct.SeriesName ?? "FriendHouse"} {(string.IsNullOrEmpty(heroProduct.Material) ? "" : $"วัสดุ {heroProduct.Material} ")}หมุนชมโมเดล 3 มิติได้รอบตัวก่อนสั่งซื้อ",
                // กำหนดราคาจำหน่ายจัดรูปแบบเงินบาท
                PriceFormatted = $"฿{heroProduct.RetailPrice:N0}",
                // กำหนดที่อยู่ไฟล์โมเดล 3 มิติ
                Model3DPath = !string.IsNullOrEmpty(heroProduct.Model3Dpath) ? heroProduct.Model3Dpath : "~/media/models/naruto_kyubi_sage.glb",
                // กำหนดที่อยู่ไฟล์รูปภาพสำรองกรณีไม่รองรับ WebGL
                FallbackImageUrl = !string.IsNullOrEmpty(heroProduct.ImageUrl) ? heroProduct.ImageUrl : "~/media/images/naruto_hero_fallback.png",
                // กำหนดสถานะมีสินค้าพร้อมส่ง
                InStock = heroInStock,
                // กำหนดข้อความป้ายสถานะสต็อก
                BadgeText = heroInStock ? "มีสินค้าพร้อมส่ง" : "สินค้าหมดชั่วคราว",
                // กำหนดข้อความแจ้งเงื่อนไขการส่งมอบ
                NoticeText = heroInStock
                    // ข้อความกรณีมีสินค้าพร้อมส่งมอบ
                    ? "มีสินค้าพร้อมส่งมอบที่หน้าร้าน FriendHouse"
                    // ข้อความกรณีสินค้าหมดชั่วคราว
                    : "สินค้าหมดชั่วคราว — กรุณาตรวจสอบอีกครั้งเมื่อสินค้าเข้าสาขา",
                // กำหนดรายการข้อมูลจำเพาะของฟิกเกอร์
                Specs = new List<KeyValuePair<string, string>>
                {
                    // คุณสมบัติแบรนด์ผู้ผลิต
                    new KeyValuePair<string, string>("ผู้ผลิต", string.IsNullOrWhiteSpace(heroProduct.BrandName) ? "ลิขสิทธิ์แท้" : heroProduct.BrandName),
                    // คุณสมบัติวัสดุการผลิต
                    new KeyValuePair<string, string>("วัสดุ", string.IsNullOrWhiteSpace(heroProduct.Material) ? "PVC / ABS" : heroProduct.Material),
                    // คุณสมบัติความสูงของโมเดล
                    new KeyValuePair<string, string>("ความสูง", heroProduct.HeightCm.HasValue ? $"{heroProduct.HeightCm.Value:0.#} ซม." : "มาตรฐาน"),
                    // คุณสมบัติอัตราส่วนสเกล
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
            // รวมข้อมูลหมวดหมู่สินค้า
            .Include(p => p.Category)
            // รวมข้อมูลสต็อกสินค้า
            .Include(p => p.Inventory)
            // กรองเฉพาะสินค้าที่เปิดใช้งาน
            .Where(p => p.IsActive == true || p.IsActive == null)
            // แปลงเป็นรายการ List
            .ToList();

        // ขั้นตอนที่ 4: แปลงรายการ Product เป็น ProductCardViewModel
        viewModel.FeaturedProducts = products.Select(p => new ProductCardViewModel
        {
            // กำหนดรหัสสินค้า
            ProductId = p.ProductId,
            // กำหนดรหัส SKU
            Sku = p.Sku,
            // กำหนดชื่อสินค้า
            Name = p.Name,
            // กำหนดชื่อซีรีส์สินค้า
            SeriesName = p.SeriesName,
            // กำหนดชื่อหมวดหมู่สินค้า
            CategoryName = p.Category?.CategoryName,
            // กำหนดแบรนด์ผู้ผลิต
            BrandName = p.BrandName,
            // กำหนดอัตราส่วนสเกล
            Scale = p.Scale,
            // กำหนดราคาจำหน่ายปลีก
            RetailPrice = p.RetailPrice,
            // กำหนดที่อยู่รูปภาพสินค้า
            ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "~/media/images/naruto_hero_fig.png",
            // คำนวณจำนวนสินค้าพร้อมขายสุทธิ
            AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0),
            // กำหนดสถานะสั่งจองล่วงหน้า
            IsPreorder = p.IsPreorder
            // แปลงเป็นรายการ List
        }).ToList();

        // ขั้นตอนที่ 5: ดึงการ์ดโปรโมชั่นที่เปิดใช้งานจริงจากฐานข้อมูล (แสดงสูงสุด 5 รายการ)
        var today = TodayGregorian();
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var activePromos = _db.Promotions
            // ใช้งาน AsNoTracking เพื่อเพิ่มประสิทธิภาพการอ่านข้อมูล
            .AsNoTracking()
            // กรองเฉพาะโปรโมชั่นที่เปิดใช้งาน
            .Where(p => p.IsActive == true)
            // เรียงลำดับตามรหัสโปรโมชั่น
            .OrderBy(p => p.PromotionId)
            // แปลงเป็นรายการ List
            .ToList()
            // กรองเฉพาะโปรโมชั่นที่ยังไม่หมดอายุตามปฏิทินสุริยคติ
            .Where(p => NormalizeGregorianDate(p.EndDate) >= today)
            // เลือกสูงสุด 5 รายการ
            .Take(5)
            // แปลงเป็นรายการ List
            .ToList();

        // แปลงข้อมูลโปรโมชั่นเป็นการ์ดโปรโมชั่นสำหรับแสดงผลหน้าแรก
        viewModel.Promotions = activePromos.Select(p => new HomePromotionCardViewModel
        {
            // กำหนดป้ายกำกับประเภทโปรโมชั่นภาษาไทย
            Tag = GetPromoTypeThaiLabel(p.PromoType),
            // กำหนดชื่อแคมเปญโปรโมชั่น
            Title = p.Name,
            // กำหนดคำอธิบายสิทธิประโยชน์โปรโมชั่น
            Description = $"{FormatPromoBenefitSummary(p)} · ขั้นต่ำ ฿{p.MinimumSpend ?? 0m:N0}",
            // กำหนดรหัสคูปอง
            Code = p.VoucherCode,
            // กำหนดขอบเขตการใช้งานโปรโมชั่น
            ApplicableScope = (p.ApplicableScope ?? "all").Trim().ToLowerInvariant(),
            // กำหนดประเภทโปรโมชั่น
            PromoType = (p.PromoType ?? "percent").Trim().ToLowerInvariant(),
            // กำหนดข้อความวันหมดอายุภาษาไทย
            ExpiryText = $"ถึง {FormatThaiDate(p.EndDate)}"
            // แปลงเป็นรายการ List
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
            // กำหนดคำค้นหา
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            // กำหนดหมวดหมู่ที่เลือก
            SelectedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            // กำหนดซีรีส์ที่เลือก
            SelectedSeries = string.IsNullOrWhiteSpace(series) ? null : series.Trim(),
            // กำหนดสถานะสินค้าที่เลือก
            SelectedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            // กำหนดช่วงราคาที่เลือก
            SelectedPriceRange = string.IsNullOrWhiteSpace(priceRange) ? null : priceRange.Trim(),
            // กำหนดรหัสโปรโมชั่นที่เลือก
            SelectedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : NormalizeCode(promoCode),
            // กำหนดขอบเขตโปรโมชั่นที่เลือก
            SelectedPromoScope = string.IsNullOrWhiteSpace(promoScope) ? null : promoScope.Trim().ToLowerInvariant(),
            // กำหนดจำนวนสินค้าต่อหน้า
            PageSize = 20
        };

        // กำหนดรายการตัวเลือกช่วงราคามาตรฐานสำหรับแสดงใน Select Dropdown
        viewModel.AvailablePriceRanges = new List<string>
        {
            // ช่วงราคาทั้งหมด
            "ทั้งหมด",
            // ช่วงราคาไม่เกิน 1000 บาท
            "฿0 - ฿1,000",
            // ช่วงราคา 1001 ถึง 3000 บาท
            "฿1,001 - ฿3,000",
            // ช่วงราคา 3001 ถึง 5000 บาท
            "฿3,001 - ฿5,000",
            // ช่วงราคามากกว่า 5000 บาท
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
                    // เชื่อมโยงข้อมูลตารางหมวดหมู่สินค้า
                    .Include(p => p.Category)
                    // เชื่อมโยงข้อมูลตารางสต็อกสินค้าคงคลัง
                    .Include(p => p.Inventory)
                    // กรองเฉพาะสินค้าที่เปิดใช้งาน
                    .Where(p => p.IsActive == true || p.IsActive == null);

                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                int catalogTotal = baseQuery.Count();
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (catalogTotal > 0)
                {
                    // กำหนดสถานะการใช้งานฐานข้อมูลสำเร็จ
                    usedDatabase = true;

                    // ดึงรายการหมวดหมู่สินค้าจริงที่มีอยู่ในฐานข้อมูล
                    var dbCategories = _db.Categories
                        // กรองเฉพาะหมวดหมู่ที่เปิดใช้งาน
                        .Where(c => c.IsActive == true || c.IsActive == null)
                        // เรียงลำดับตามรหัสหมวดหมู่
                        .OrderBy(c => c.CategoryId)
                        // ดึงเฉพาะชื่อหมวดหมู่
                        .Select(c => c.CategoryName)
                        // กรองเฉพาะรายการที่ไม่ซ้ำกัน
                        .Distinct()
                        // แปลงเป็นรายการ List
                        .ToList();

                    // กำหนดค่าเริ่มต้นตัวเลือกหมวดหมู่
                    viewModel.AvailableCategories = new List<string> { "ทั้งหมด" };
                    // เพิ่มหมวดหมู่จากฐานข้อมูล
                    viewModel.AvailableCategories.AddRange(dbCategories);

                    // ดึงรายการชื่อซีรีส์จริงที่มีอยู่ในฐานข้อมูล
                    var dbSeries = _db.Products
                        // กรองเฉพาะสินค้าที่เปิดใช้งานและมีชื่อซีรีส์
                        .Where(p => (p.IsActive == true || p.IsActive == null) && !string.IsNullOrEmpty(p.SeriesName))
                        // ดึงเฉพาะชื่อซีรีส์
                        .Select(p => p.SeriesName!)
                        // กรองเฉพาะรายการที่ไม่ซ้ำกัน
                        .Distinct()
                        // จัดเรียงตามตัวอักษร
                        .OrderBy(s => s)
                        // แปลงเป็นรายการ List
                        .ToList();

                    // กำหนดค่าเริ่มต้นตัวเลือกซีรีส์
                    viewModel.AvailableSeries = new List<string> { "ทั้งหมด" };
                    // เพิ่มซีรีส์จากฐานข้อมูล
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
                        // กรองข้อมูลสินค้าตามคำค้นหาในชื่อสินค้า ซีรีส์ แบรนด์ หรือรหัสสินค้า
                        query = query.Where(p => p.Name.ToLower().Contains(term)
                            // ตรวจสอบชื่อซีรีส์สินค้า
                            || (p.SeriesName != null && p.SeriesName.ToLower().Contains(term))
                            // ตรวจสอบแบรนด์ผู้ผลิต
                            || (p.BrandName != null && p.BrandName.ToLower().Contains(term))
                            // ตรวจสอบรหัส SKU สินค้า
                            || (p.Sku != null && (p.Sku.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLower().Contains(termNoHyphen))))
                            // ตรวจสอบรหัสบาร์โค้ดสินค้า
                            || (p.Barcode != null && !string.IsNullOrEmpty(termNoHyphen) && p.Barcode.Replace("-", "").ToLower().Contains(termNoHyphen)));
                    }

                    // เงื่อนไขที่ 2: กรองตามหมวดหมู่สินค้า category
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedCategory) && viewModel.SelectedCategory != "ทั้งหมด")
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string selectedCat = viewModel.SelectedCategory;
                        // กรองข้อมูลสินค้าตามหมวดหมู่หรือสลักของหมวดหมู่
                        query = query.Where(p => p.Category != null && (
                            // ตรวจสอบชื่อหมวดหมู่
                            p.Category.CategoryName == selectedCat ||
                            // ตรวจสอบสลักหมวดหมู่
                            p.Category.Slug == selectedCat ||
                            // ตรวจสอบหมวดหมู่สเกลฟิกเกอร์
                            (selectedCat == "สเกลฟิกเกอร์" && p.Category.CategoryName == "ฟิกเกอร์สเกล") ||
                            // ตรวจสอบหมวดหมู่ตุ๊กตาผ้า
                            (selectedCat == "ตุ๊กตาผ้า" && p.Category.CategoryName == "ของสะสมและตุ๊กตา")
                        ));
                    }

                    // เงื่อนไขที่ 3: กรองตามซีรีส์ series
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedSeries) && viewModel.SelectedSeries != "ทั้งหมด")
                    {
                        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                        string selectedSeries = viewModel.SelectedSeries;
                        // กรองข้อมูลสินค้าตามชื่อซีรีส์
                        query = query.Where(p => p.SeriesName == selectedSeries);
                    }

                    // เงื่อนไขที่ 4: กรองตามสถานะสินค้า (รองรับทั้งภาษาไทยและรหัสจากหน้าแรก instock / preorder)
                    if (string.Equals(viewModel.SelectedStatus, "มีสินค้า", StringComparison.OrdinalIgnoreCase)
                        // ตรวจสอบสถานะ instock ภาษาอังกฤษ
                        || string.Equals(viewModel.SelectedStatus, "instock", StringComparison.OrdinalIgnoreCase))
                    {
                        // กำหนดสถานะแสดงผลเป็นมีสินค้า
                        viewModel.SelectedStatus = "มีสินค้า";
                        // กรองสินค้าที่มีสต็อกและไม่เป็นสินค้าพรีออเดอร์
                        query = query.Where(p => !p.IsPreorder && p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0);
                    }
                    // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                    else if (string.Equals(viewModel.SelectedStatus, "สินค้าหมด", StringComparison.OrdinalIgnoreCase)
                        // ตรวจสอบสถานะ outofstock
                        || string.Equals(viewModel.SelectedStatus, "outofstock", StringComparison.OrdinalIgnoreCase)
                        // ตรวจสอบสถานะเปิดสั่งจอง
                        || string.Equals(viewModel.SelectedStatus, "เปิดสั่งจอง", StringComparison.OrdinalIgnoreCase)
                        // ตรวจสอบสถานะ preorder
                        || string.Equals(viewModel.SelectedStatus, "preorder", StringComparison.OrdinalIgnoreCase))
                    {
                        // กำหนดสถานะแสดงผลเป็นสินค้าหมด
                        viewModel.SelectedStatus = "สินค้าหมด";
                        // กรองสินค้าที่เป็นพรีออเดอร์หรือไม่มีสต็อกพร้อมขาย
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
                            // กรองสินค้าราคาไม่เกินหนึ่งพันบาท
                            query = query.Where(p => p.RetailPrice <= 1000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                        {
                            // กรองสินค้าราคาหนึ่งพันถึงสามพันบาท
                            query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                        {
                            // กรองสินค้าราคาสามพันถึงห้าพันบาท
                            query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                        }
                        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                        else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                        {
                            // กรองสินค้าราคามากกว่าห้าพันบาทขึ้นไป
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
                            // กำหนดขอบเขตสินค้าที่ร่วมรายการโปรโมชั่น
                            viewModel.SelectedPromoScope = (matchedPromo.ApplicableScope ?? "all").Trim().ToLowerInvariant();
                            // กำหนดหัวข้อแบนเนอร์โปรโมชั่น
                            viewModel.PromoBannerTitle = $"{matchedPromo.Name} (รหัส {matchedPromo.VoucherCode})";
                            // กำหนดข้อความส่วนลดตามประเภทโปรโมชั่น
                            string discountText = (matchedPromo.PromoType == "percent" || matchedPromo.PromoType == "percentage")
                                // ข้อความส่วนลดแบบเปอร์เซ็นต์
                                ? $"ส่วนลด {matchedPromo.DiscountValue:0.#}%"
                                // ตรวจสอบโปรโมชั่นแต้มสองเท่า
                                : matchedPromo.PromoType == "double_points"
                                    // ข้อความสิทธิ์รับแต้มสะสมสองเท่า
                                    ? "แต้มสะสม x2"
                                    // ข้อความส่วนลดแบบจำนวนเงินบาท
                                    : $"ส่วนลด ฿{matchedPromo.DiscountValue:N0}";
                            // กำหนดข้อความเงื่อนไขโปรโมชั่นบนแบนเนอร์
                            viewModel.PromoBannerCondition = $"ร่วมรายการ: {GetScopeThaiLabel(viewModel.SelectedPromoScope)} • {discountText} • ขั้นต่ำ ฿{matchedPromo.MinimumSpend:N0}";
                        }
                    }
                    // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                    else if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoScope))
                    {
                        // กำหนดหัวข้อแบนเนอร์รายการสินค้าตามสิทธิ์โปรโมชั่น
                        viewModel.PromoBannerTitle = $"รายการสินค้าตามสิทธิ์โปรโมชั่น";
                        // กำหนดเงื่อนไขขอบเขตสินค้าบนแบนเนอร์
                        viewModel.PromoBannerCondition = $"ขอบเขตสินค้า: {GetScopeThaiLabel(viewModel.SelectedPromoScope)}";
                    }

                    // ดึงรายการผลลัพธ์และแปลงเป็น ProductCardViewModel (พร้อมกรองตามขอบเขตโปรโมชั่นหากระบุ)
                    var filteredProducts = query.ToList();
                    // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoScope))
                    {
                        // กรองรายการสินค้าให้ตรงกับขอบเขตสิทธิ์ของโปรโมชั่น
                        filteredProducts = filteredProducts
                            // ตรวจสอบเงื่อนไขขอบเขตโปรโมชั่น
                            .Where(p => MatchesScope(p, viewModel.SelectedPromoScope))
                            // แปลงผลลัพธ์เป็นรายการ List
                            .ToList();
                    }
                    // แปลงผลลัพธ์เป็น ProductCardViewModel
                    viewModel.Products = filteredProducts.Select(p => new ProductCardViewModel
                    {
                        // กำหนดรหัสสินค้า
                        ProductId = p.ProductId,
                        // กำหนดรหัส SKU
                        Sku = p.Sku,
                        // กำหนดชื่อสินค้า
                        Name = p.Name,
                        // กำหนดชื่อซีรีส์สินค้า
                        SeriesName = p.SeriesName,
                        // กำหนดชื่อหมวดหมู่สินค้า
                        CategoryName = p.Category?.CategoryName,
                        // กำหนดแบรนด์ผู้ผลิต
                        BrandName = p.BrandName,
                        // กำหนดอัตราส่วนสเกล
                        Scale = p.Scale,
                        // กำหนดราคาจำหน่ายปลีก
                        RetailPrice = p.RetailPrice,
                        // กำหนดที่อยู่รูปภาพสินค้า
                        ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "~/media/images/naruto_hero_fig.png",
                        // คำนวณจำนวนสินค้าพร้อมขายสุทธิ
                        AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0),
                        // กำหนดสถานะสั่งจองล่วงหน้า
                        IsPreorder = p.IsPreorder
                        // แปลงเป็นรายการ List
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
            // เพิ่มรายชื่อหมวดหมู่ที่พบในแคตตาล็อกสำรอง
            viewModel.AvailableCategories.AddRange(
                // ดึงชื่อหมวดหมู่ที่ไม่ซ้ำกัน
                fallbackProducts.Select(p => p.CategoryName).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList()!
            );

            // กำหนดค่าเริ่มต้นตัวเลือกซีรีส์
            viewModel.AvailableSeries = new List<string> { "ทั้งหมด" };
            // เพิ่มรายชื่อซีรีส์ที่พบในแคตตาล็อกสำรอง
            viewModel.AvailableSeries.AddRange(
                // ดึงชื่อซีรีส์ที่ไม่ซ้ำกันเรียงตามลำดับอักษร
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
                // กรองข้อมูลสินค้าตามคำค้นหา
                query = query.Where(p =>
                    // ตรวจสอบชื่อสินค้า
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    // ตรวจสอบชื่อซีรีส์สินค้า
                    (p.SeriesName != null && p.SeriesName.ToLower().Contains(term)) ||
                    // ตรวจสอบชื่อแบรนด์ผู้ผลิต
                    (p.BrandName != null && p.BrandName.ToLower().Contains(term)) ||
                    // ตรวจสอบรหัส SKU สินค้า
                    (p.Sku != null && (p.Sku.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLower().Contains(termNoHyphen)))));
            }

            // เงื่อนไขที่ 2: หมวดหมู่
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedCategory) && viewModel.SelectedCategory != "ทั้งหมด")
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string selectedCat = viewModel.SelectedCategory;
                // กรองข้อมูลสินค้าตามหมวดหมู่
                query = query.Where(p =>
                    // ตรวจสอบชื่อหมวดหมู่
                    p.CategoryName == selectedCat ||
                    // ตรวจสอบหมวดหมู่สเกลฟิกเกอร์
                    (selectedCat == "scale-figures" && p.CategoryName == "ฟิกเกอร์สเกล") ||
                    // ตรวจสอบหมวดหมู่แอคชั่นฟิกเกอร์
                    (selectedCat == "action-figures" && p.CategoryName == "แอคชั่นฟิกเกอร์") ||
                    // ตรวจสอบหมวดหมู่ของสะสมและตุ๊กตา
                    (selectedCat == "merchandise" && p.CategoryName == "ของสะสมและตุ๊กตา") ||
                    // ตรวจสอบหมวดหมู่ของสะสมลิมิเต็ด
                    (selectedCat == "limited-collectibles" && p.CategoryName == "ของสะสมลิมิเต็ด") ||
                    // ตรวจสอบชื่อภาษาไทยหมวดสเกลฟิกเกอร์
                    (selectedCat == "สเกลฟิกเกอร์" && p.CategoryName == "ฟิกเกอร์สเกล") ||
                    // ตรวจสอบชื่อภาษาไทยหมวดตุ๊กตาผ้า
                    (selectedCat == "ตุ๊กตาผ้า" && p.CategoryName == "ของสะสมและตุ๊กตา"));
            }

            // เงื่อนไขที่ 3: ซีรีส์
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedSeries) && viewModel.SelectedSeries != "ทั้งหมด")
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string selectedSeries = viewModel.SelectedSeries;
                // กรองข้อมูลสินค้าตามชื่อซีรีส์
                query = query.Where(p => p.SeriesName == selectedSeries);
            }

            // เงื่อนไขที่ 4: สถานะสินค้ามีสินค้าหรือสินค้าหมด
            if (string.Equals(viewModel.SelectedStatus, "มีสินค้า", StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบสถานะ instock
                || string.Equals(viewModel.SelectedStatus, "instock", StringComparison.OrdinalIgnoreCase))
            {
                // กำหนดสถานะแสดงผลเป็นมีสินค้า
                viewModel.SelectedStatus = "มีสินค้า";
                // กรองเฉพาะสินค้าที่มีสต็อกและไม่เป็นสินค้าพรีออเดอร์
                query = query.Where(p => !p.IsPreorder && p.AvailableStock > 0);
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (string.Equals(viewModel.SelectedStatus, "สินค้าหมด", StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบสถานะ outofstock
                || string.Equals(viewModel.SelectedStatus, "outofstock", StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบสถานะเปิดสั่งจอง
                || string.Equals(viewModel.SelectedStatus, "เปิดสั่งจอง", StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบสถานะ preorder
                || string.Equals(viewModel.SelectedStatus, "preorder", StringComparison.OrdinalIgnoreCase))
            {
                // กำหนดสถานะแสดงผลเป็นสินค้าหมด
                viewModel.SelectedStatus = "สินค้าหมด";
                // กรองเฉพาะสินค้าที่เปิดพรีออเดอร์หรือไม่มีสต็อกคงเหลือ
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
                    // กรองสินค้าราคาไม่เกินหนึ่งพันบาท
                    query = query.Where(p => p.RetailPrice <= 1000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                {
                    // กรองสินค้าราคาหนึ่งพันถึงสามพันบาท
                    query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                {
                    // กรองสินค้าราคาสามพันถึงห้าพันบาท
                    query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                }
                // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
                else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                {
                    // กรองสินค้าราคามากกว่าห้าพันบาทขึ้นไป
                    query = query.Where(p => p.RetailPrice > 5000m);
                }
            }

            // กำหนดรายการสินค้าที่ผ่านการกรองลงในโมเดล
            viewModel.Products = query.ToList();
        }

        // ขั้นตอนที่ 5: คำนวณการแบ่งหน้าสำหรับรายการสินค้าฟิกเกอร์ (หน้าละ 20 ชิ้น)
        viewModel.TotalCount = viewModel.Products.Count;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalProductPages = Math.Max(1, (int)Math.Ceiling((double)viewModel.TotalCount / viewModel.PageSize));
        // กำหนดหมายเลขหน้าปัจจุบันให้อยู่ในช่วงที่ถูกต้อง
        viewModel.CurrentPage = Math.Clamp(page, 1, totalProductPages);
        // ดึงรายการสินค้าเฉพาะหน้าปัจจุบันตามขนาดหน้า
        viewModel.Products = viewModel.Products
            // ข้ามรายการสินค้าของหน้าก่อนหน้า
            .Skip((viewModel.CurrentPage - 1) * viewModel.PageSize)
            // เลือกเฉพาะจำนวนรายการของหน้านี้
            .Take(viewModel.PageSize)
            // แปลงเป็นรายการ List
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
        {
            // สินค้าสำรองลำดับที่ 1: นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 1,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-NARUTO-001",
                // ชื่อโมเดลฟิกเกอร์
                Name = "นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Naruto Shippuden",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Banpresto",
                // อัตราส่วนสเกล
                Scale = "1/6 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 4290.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/naruto_hero_fig.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 13
            },
            // สินค้าสำรองลำดับที่ 2: ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 2,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-TANJIRO-002",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Demon Slayer",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 2450.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/tanjiro_kamado.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 19
            },
            // สินค้าสำรองลำดับที่ 3: ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล (Vocaloid)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 3,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-MIKU-003",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Vocaloid",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 4200.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/hatsune_miku_magical.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 5
            },
            // สินค้าสำรองลำดับที่ 4: มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 4,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-LUFFY-004",
                // ชื่อโมเดลฟิกเกอร์
                Name = "มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "One Piece",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 950.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/luffy_gear5.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 26
            },
            // สินค้าสำรองลำดับที่ 5: อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 5,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-ANYA-005",
                // ชื่อโมเดลฟิกเกอร์
                Name = "อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Spy x Family",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Megahouse",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 1850.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/anya_forger.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 10
            },
            // สินค้าสำรองลำดับที่ 6: นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 6,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-NARUTO-006",
                // ชื่อโมเดลฟิกเกอร์
                Name = "นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Naruto Shippuden",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Banpresto",
                // อัตราส่วนสเกล
                Scale = "1/8 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 1200.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/naruto_vibration_stars.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 22
            },
            // สินค้าสำรองลำดับที่ 7: โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 7,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-GOJO-007",
                // ชื่อโมเดลฟิกเกอร์
                Name = "โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Jujutsu Kaisen",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Megahouse",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 3800.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/gojo_satoru.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 8
            },
            // สินค้าสำรองลำดับที่ 8: โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 8,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-RAIDEN-008",
                // ชื่อโมเดลฟิกเกอร์
                Name = "โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Genshin Impact",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 5690.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/raiden_shogun.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 5
            },
            // สินค้าสำรองลำดับที่ 9: ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล (FriendHouse Studio)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 9,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-BEAR-009",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัด
                SeriesName = "FriendHouse Studio",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "FriendHouse Original",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 590.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/cloud_bear_plush.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 35
            },
            // สินค้าสำรองลำดับที่ 10: โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 10,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-ZORO-010",
                // ชื่อโมเดลฟิกเกอร์
                Name = "โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "One Piece",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 2790.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/roronoa_zoro.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 16
            },
            // สินค้าสำรองลำดับที่ 11: คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 11,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-NEZUKO-011",
                // ชื่อโมเดลฟิกเกอร์
                Name = "คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Demon Slayer",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Aniplex",
                // อัตราส่วนสเกล
                Scale = "1/8 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 3450.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/nezuko_kamado.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 12
            },
            // สินค้าสำรองลำดับที่ 12: ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 12,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-MEGUMI-012",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Jujutsu Kaisen",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Kotobukiya",
                // อัตราส่วนสเกล
                Scale = "1/8 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 3190.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/megumi_fushiguro.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 14
            },
            // สินค้าสำรองลำดับที่ 13: จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 13,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-ZONG-013",
                // ชื่อโมเดลฟิกเกอร์
                Name = "จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมลิมิเต็ด",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Genshin Impact",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 6200.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/zhongli_morax.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 4
            },
            // สินค้าสำรองลำดับที่ 14: อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา (Naruto Shippuden)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 14,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-SASUKE-014",
                // ชื่อโมเดลฟิกเกอร์
                Name = "อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Naruto Shippuden",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 2590.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/sasuke_uchiha.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 19
            },
            // สินค้าสำรองลำดับที่ 15: เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์ (Demon Slayer)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 15,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-RENBK-015",
                // ชื่อโมเดลฟิกเกอร์
                Name = "เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Demon Slayer",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Aniplex",
                // อัตราส่วนสเกล
                Scale = "1/8 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 4600.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/rengoku_kyojuro.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 8
            },
            // สินค้าสำรองลำดับที่ 16: ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 16,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-YOR-016",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Spy x Family",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 3250.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/yor_forger.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 13
            },
            // สินค้าสำรองลำดับที่ 17: ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด (Vocaloid)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 17,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-MIKUS-017",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมลิมิเต็ด",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Vocaloid",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 2890.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/hatsune_miku_sakura.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 9
            },
            // สินค้าสำรองลำดับที่ 18: โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 18,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-ACE-018",
                // ชื่อโมเดลฟิกเกอร์
                Name = "โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "One Piece",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "MegaHouse",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 1890.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/portgas_d_ace.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 18
            },
            // สินค้าสำรองลำดับที่ 19: อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 19,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-ITAD-019",
                // ชื่อโมเดลฟิกเกอร์
                Name = "อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Jujutsu Kaisen",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 1450.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/itadori_yuji.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 24
            },
            // สินค้าสำรองลำดับที่ 20: กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่ (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 20,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-GANYU-020",
                // ชื่อโมเดลฟิกเกอร์
                Name = "กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Genshin Impact",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 5400.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/ganyu_plenilune.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 5
            },
            // สินค้าสำรองลำดับที่ 21: ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม (FriendHouse Studio)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 21,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-CAT-021",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัด
                SeriesName = "FriendHouse Studio",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "FriendHouse Original",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 490.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/lucky_cat_plush.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 45
            },
            // สินค้าสำรองลำดับที่ 22: โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ (One Piece)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 22,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-CHOP-022",
                // ชื่อโมเดลฟิกเกอร์
                Name = "โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ของสะสมและตุ๊กตา",
                // ซีรีส์ต้นสังกัด
                SeriesName = "One Piece",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 650.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/chopper_plush.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 32
            },
            // สินค้าสำรองลำดับที่ 23: คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์ (Jujutsu Kaisen)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 23,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-NOBARA-023",
                // ชื่อโมเดลฟิกเกอร์
                Name = "คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Jujutsu Kaisen",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Bandai Spirits",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 1350.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/nobara_kugisaki.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 23
            },
            // สินค้าสำรองลำดับที่ 24: ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง (Genshin Impact)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 24,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-HUTAO-024",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "ฟิกเกอร์สเกล",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Genshin Impact",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Apex Innovation",
                // อัตราส่วนสเกล
                Scale = "1/7 Scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 5200.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/hu_tao_ghost.png",
                // จำนวนสต็อกพร้อมส่ง
                AvailableStock = 7
            },
            // สินค้าสำรองลำดับที่ 25: ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส (Spy x Family)
            new ProductCardViewModel
            {
                // รหัสสินค้าประจำโมเดล
                ProductId = 25,
                // รหัส SKU ประจำโมเดล
                Sku = "FIG-LOID-025",
                // ชื่อโมเดลฟิกเกอร์
                Name = "ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส",
                // ชื่อหมวดหมู่สินค้า
                CategoryName = "แอคชั่นฟิกเกอร์",
                // ซีรีส์ต้นสังกัด
                SeriesName = "Spy x Family",
                // แบรนด์ผู้ผลิตลิขสิทธิ์
                BrandName = "Good Smile Company",
                // อัตราส่วนสเกล
                Scale = "Non-scale",
                // ราคาจำหน่ายปลีก
                RetailPrice = 2390.00m,
                // พาธไฟล์รูปภาพสินค้า
                ImageUrl = "~/media/images/loid_forger.png",
                // จำนวนสต็อกพร้อมส่ง
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
            // กรองรายการโปรโมชั่นตามคำค้นหา
            query = query.Where(p => p.Name.ToLower().Contains(term)
                // ตรวจสอบรหัสคูปอง
                || p.VoucherCode.ToLower().Contains(term)
                // ตรวจสอบรหัสคูปองแบบตัดเครื่องหมายขีด
                || (!string.IsNullOrEmpty(termNoHyphen) && p.VoucherCode.Replace("-", "").ToLower().Contains(termNoHyphen)));
        }

        // ขั้นตอนที่ 3: กรองตามประเภทโปรโมชั่น
        if (!string.IsNullOrWhiteSpace(promoType) && promoType != "all")
        {
            // กรองตามประเภทโปรโมชั่นที่ระบุ
            query = query.Where(p => p.PromoType == promoType);
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var promotions = query.OrderBy(p => p.PromotionId).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var customerId = GetCurrentCustomerId();
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.IsLoggedIn = customerId.HasValue;
        // อ่านรายการรหัสคูปองที่ลูกค้าเก็บไว้จากเซสชัน
        var collectedCodes = customerId.HasValue
            // กรณีเข้าสู่ระบบให้อ่านรหัสคูปองจากเซสชัน
            ? ReadCollectedCodes(
                // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
                HttpContext.Session.GetString(CollectedPromoSessionKey))
            // กรณีไม่ได้เข้าสู่ระบบให้คืนรายการว่าง
            : new List<string>();

        // ขั้นตอนที่ 4: ประกอบข้อมูลลงใน PromotionCatalogViewModel พร้อมแปลงข้อความเป็นภาษาไทยบริสุทธิ์
        var viewModel = new PromotionCatalogViewModel
        {
            // คำค้นหาที่ระบุ
            SearchTerm = searchTerm,
            // ประเภทโปรโมชั่นที่เลือก
            SelectedType = promoType ?? "all",
            // รายการรหัสคูปองที่เก็บไว้
            CollectedCodes = collectedCodes
        };
        // เพิ่มรายการโปรโมชั่นทั้งหมดลงใน ViewModel
        viewModel.AddRange(promotions);

        // แปลงข้อมูลโปรโมชั่นเป็นโมเดลการ์ดแสดงผล
        viewModel.PromotionCards = promotions.Select(p =>
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string tagThai = GetPromoTypeThaiLabel(p.PromoType);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string scopeCode = (p.ApplicableScope ?? "all").Trim().ToLowerInvariant();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string scopeThai = GetScopeThaiLabel(scopeCode);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string discountText = FormatPromoBenefitSummary(p);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string startFormatted = FormatThaiDate(p.StartDate);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string endFormatted = FormatThaiDate(p.EndDate);
            // กำหนดคำอธิบายจำนวนสิทธิ์การใช้งาน
            string limitDesc = p.UsageLimit.HasValue
                // กรณีจำกัดจำนวนสิทธิ์
                ? $"จำกัดจำนวน {p.UsageLimit.Value:N0} สิทธิ์ (ใช้แล้ว {p.CurrentUsageCount:N0} สิทธิ์)"
                // กรณีไม่จำกัดจำนวนสิทธิ์
                : $"ใช้ได้ไม่จำกัดสิทธิ์ (ใช้แล้ว {p.CurrentUsageCount:N0} สิทธิ์)";

            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromotionCardItemViewModel
            {
                // รหัสโปรโมชั่น
                PromotionId = p.PromotionId,
                // ป้ายประเภทโปรโมชั่น
                Tag = tagThai,
                // ชื่อโปรโมชั่น
                Title = p.Name,
                // คำอธิบายเงื่อนไขโปรโมชั่น
                Description = $"ใช้ได้กับ{scopeThai} · {limitDesc}",
                // ระยะเวลาของโปรโมชั่น
                Period = $"{startFormatted} - {endFormatted}",
                // เงื่อนไขการรับสิทธิ์
                Condition = $"รหัสคูปอง: {p.VoucherCode} • {discountText} • ยอดขั้นต่ำ ฿{p.MinimumSpend ?? 0m:N0}",
                // รหัสคูปองส่วนลด
                VoucherCode = p.VoucherCode,
                // ขอบเขตสินค้าที่ร่วมรายการ
                ApplicableScope = scopeCode,
                // สถานะว่าลูกค้าเก็บคูปองนี้แล้วหรือไม่
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
        string normalized = NormalizeCode(voucherCode);
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
        var today = TodayGregorian();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var startDate = NormalizeGregorianDate(promo.StartDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var endDate = NormalizeGregorianDate(promo.EndDate);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today < startDate)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" ยังไม่เริ่มเปิดให้เก็บ (เริ่ม {FormatThaiDate(startDate)})";
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
        var collected = ReadCollectedCodes(
            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.GetString(CollectedPromoSessionKey));
        // ตรวจสอบเงื่อนไขว่าเก็บคูปองนี้ไว้ในเซสชันแล้วหรือยัง
        if (!collected.Contains(promo.VoucherCode, StringComparer.OrdinalIgnoreCase))
        {
            // เพิ่มรหัสคูปองตัวพิมพ์ใหญ่ลงในรายการที่เก็บไว้
            collected.Add(promo.VoucherCode.ToUpperInvariant());
            // บันทึกรายการคูปองที่เก็บไว้ลงใน Session
            HttpContext.Session.SetString(
                // คีย์สำหรับเก็บรหัสคูปองในเซสชัน
                CollectedPromoSessionKey,
                // แปลงรายการรหัสคูปองเป็นข้อความ JSON
                SerializeCollectedCodes(collected));
            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"เก็บคูปอง \"{promo.VoucherCode}\" ({promo.Name}) เรียบร้อยแล้ว สามารถกดใช้งานได้ที่หน้าตะกร้าสินค้า";
        }
        // จัดการกรณีมีคูปองนี้อยู่ในรายการที่เก็บไว้แล้ว
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
            // เชื่อมโยงข้อมูลสินค้า
            .Include(c => c.Product)
                // เชื่อมโยงข้อมูลหมวดหมู่สินค้า
                .ThenInclude(p => p.Category)
            // เชื่อมโยงข้อมูลสินค้า
            .Include(c => c.Product)
                // เชื่อมโยงข้อมูลสต็อกสินค้าคงคลัง
                .ThenInclude(p => p.Inventory)
            // กรองเฉพาะรายการของผู้ใช้ปัจจุบัน
            .Where(c => c.UserId == userId.Value)
            // เรียงลำดับจากรายการที่เพิ่มล่าสุด
            .OrderByDescending(c => c.CreatedAt)
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();

        // ขั้นตอนที่ 4: แปลงข้อมูลเป็น CartViewModel และ CartItemViewModel
        var viewModel = new CartViewModel
        {
            // เบอร์โทรศัพท์ของผู้ใช้งาน
            UserPhone = user?.Phone ?? string.Empty,
            // สาขาที่นัดรับสินค้า
            PickupBranch = "FriendHouse HQ สาขาสยามสแควร์",
            // แปลงรายการสินค้าในตะกร้าเป็นรายการ CartItemViewModel
            Items = rawItems.Select(c => new CartItemViewModel
            {
                // รหัสรายการในตะกร้า
                CartItemId = c.CartItemId,
                // รหัสสินค้า
                ProductId = c.ProductId,
                // ชื่อสินค้า
                ProductName = c.Product?.Name ?? "สินค้าไม่มีชื่อ",
                // รูปภาพสินค้า
                ImageUrl = !string.IsNullOrEmpty(c.Product?.ImageUrl) ? c.Product.ImageUrl : "~/media/images/naruto_hero_fig.png",
                // ราคาต่อหน่วย
                UnitPrice = c.Product?.RetailPrice ?? 0m,
                // จำนวนสินค้าที่สั่ง
                Quantity = c.Quantity,
                // จำนวนสินค้าคงเหลือที่พร้อมสั่ง
                AvailableStock = (c.Product?.Inventory?.QuantityOnHand ?? 0) - (c.Product?.Inventory?.ReservedQuantity ?? 0),
                // ข้อความแจ้งเตือนรอบการส่งมอบ
                ScheduleNotice = "กำหนดเข้าร้าน: สินค้าพร้อมส่งมอบตามรอบพรีออเดอร์"
            }).ToList()
        };

        // ขั้นตอนที่ 4.1: ตรวจสอบคูปองโปรโมชั่นที่เก็บไว้และคูปองที่ใช้งานอยู่ในตะกร้าสินค้า
        var collectedCodes = ReadCollectedCodes(
            // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
            HttpContext.Session.GetString(CollectedPromoSessionKey));
        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        string? appliedCode = HttpContext.Session.GetString(AppliedPromoSessionKey);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (collectedCodes.Any())
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var collectedPromos = _db.Promotions
                // กรองเฉพาะโปรโมชั่นที่เปิดใช้งานและลูกค้าเก็บไว้
                .Where(p => (p.IsActive == true || p.IsActive == null) && collectedCodes.Contains(p.VoucherCode.ToUpper()))
                // เรียงลำดับตามรหัสโปรโมชั่น
                .OrderBy(p => p.PromotionId)
                // แปลงผลลัพธ์เป็นรายการ List
                .ToList();

            // แปลงโปรโมชั่นเป็นตัวเลือกคูปองใน ViewModel
            viewModel.CollectedPromos = collectedPromos.Select(p =>
            {
                // กำหนดป้ายแสดงสิทธิประโยชน์ตามประเภทโปรโมชั่น
                string label = (p.PromoType == "percent" || p.PromoType == "percentage")
                    // กรณีลดเป็นเปอร์เซ็นต์
                    ? $"ลด {p.DiscountValue:0.#}%"
                    // ตรวจสอบกรณีแต้มสองเท่า
                    : p.PromoType == "double_points"
                        // กรณีแต้มสองเท่า
                        ? "แต้ม x2"
                        // กรณีลดเป็นจำนวนเงินบาท
                        : $"ลด ฿{p.DiscountValue:N0}";
                // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                return new CollectedPromoOptionViewModel
                {
                    // รหัสคูปองส่วนลด
                    VoucherCode = p.VoucherCode,
                    // ชื่อโปรโมชั่น
                    Title = p.Name,
                    // ป้ายข้อความสิทธิประโยชน์และยอดขั้นต่ำ
                    BenefitLabel = $"{label} (ขั้นต่ำ ฿{p.MinimumSpend:N0})",
                    // ตรวจสอบว่าคูปองนี้กำลังใช้งานอยู่ในปัจจุบันหรือไม่
                    IsApplied = !string.IsNullOrEmpty(appliedCode)
                        // ตรวจสอบความตรงกันของรหัสคูปอง
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
            var eval = EvaluateCartPromotion(promo, rawItems);
            // ตรวจสอบเงื่อนไขว่าคูปองผ่านเกณฑ์การใช้งานหรือไม่
            if (eval.IsValid)
            {
                // กำหนดรหัสคูปองที่ใช้งานสำเร็จ
                viewModel.AppliedPromoCode = eval.VoucherCode;
                // กำหนดชื่อโปรโมชั่นที่ใช้งานสำเร็จ
                viewModel.AppliedPromoName = eval.PromoName;
                // กำหนดข้อความสรุปสิทธิประโยชน์
                viewModel.PromoBenefitSummary = eval.BenefitSummary;
                // กำหนดจำนวนเงินส่วนลด
                viewModel.DiscountAmount = eval.DiscountAmount;
                // กำหนดสถานะรับแต้มสองเท่า
                viewModel.IsDoublePoints = eval.IsDoublePoints;
            }
            // จัดการกรณีคูปองไม่ผ่านเกณฑ์การใช้งาน
            else
            {
                // กำหนดรหัสคูปองที่พยายามใช้งาน
                viewModel.AppliedPromoCode = appliedCode;
                // กำหนดข้อความแจ้งข้อผิดพลาดของคูปอง
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
        string normalized = NormalizeCode(voucherCode);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string noHyphen = NormalizeCodeNoHyphen(voucherCode);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(normalized)) return null;

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return _db.Promotions.ToList().FirstOrDefault(p =>
            // ตรวจสอบความตรงกันของรหัสคูปองปกติ
            p.VoucherCode.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            // หรือตรวจสอบความตรงกันของรหัสคูปองแบบไม่ใส่ขีด
            || (!string.IsNullOrEmpty(noHyphen) && NormalizeCodeNoHyphen(p.VoucherCode) == noHyphen));
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
        string cleanCode = NormalizeCode(voucherCode);
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
            // เชื่อมโยงข้อมูลสินค้าและหมวดหมู่
            .Include(c => c.Product).ThenInclude(p => p.Category)
            // เชื่อมโยงข้อมูลสินค้าและสต็อกคงคลัง
            .Include(c => c.Product).ThenInclude(p => p.Inventory)
            // กรองเฉพาะรายการของผู้ใช้ปัจจุบัน
            .Where(c => c.UserId == userId.Value)
            // แปลงผลลัพธ์เป็นรายการ List
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
        var eval = EvaluateCartPromotion(promo, rawItems);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!eval.IsValid || eval.Promotion == null)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
            TempData["ErrorMessage"] = eval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นนี้ได้";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Cart");
        }

        // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
        HttpContext.Session.SetString(AppliedPromoSessionKey, eval.Promotion.VoucherCode);
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
        HttpContext.Session.Remove(AppliedPromoSessionKey);
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

        // กำหนดที่อยู่ปลายทางสำหรับเปลี่ยนเส้นทางหลังเพิ่มสินค้าสำเร็จ
        string? safeReturnUrl = (!goToCart && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            // กรณีเป็น URL ภายในระบบและไม่ได้เลือกไปหน้าตะกร้าทันที
            ? returnUrl
            // กรณีอื่นให้เป็นค่าว่างเพื่อไปหน้าตะกร้า
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
            // กรณีเป็นสินค้าพรีออเดอร์ให้สต็อกเป็นศูนย์
            ? 0
            // กรณีสินค้าทั่วไปคำนวณจากสต็อกในมือลบยอดจอง
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
        // จัดการกรณีเป็นสินค้าใหม่ในตะกร้า ให้สร้างแถวข้อมูล Cartitem ใหม่
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
            // ข้อความกรณีเลือกซื้อตอนนี้เพื่อนำทางไปยังหน้าตะกร้า
            ? $"เพิ่ม \"{product.Name}\" ลงในตะกร้าแล้ว กรุณาเลือกวันที่รับสินค้าและตรวจสอบเบอร์โทรศัพท์เพื่อยืนยันคำสั่งซื้อ"
            // ข้อความกรณีเพิ่มลงตะกร้าสำเร็จ
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
            // ค้นหารายการตามรหัสตะกร้าและรหัสผู้ใช้งาน
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
        // จัดการกรณีจำนวนสินค้ามากกว่าศูนย์
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
        // จัดการกรณีไม่พบรายการสินค้าที่ต้องการลบในตะกร้า
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
        // กำหนดวันที่เริ่มต้นเป็น 7 วันข้างหน้าเป็นค่าสำรอง
        resolvedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        // กำหนดข้อความแจ้งข้อผิดพลาดเริ่มต้นเป็นค่าว่าง
        errorMessage = null;

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(pickupDate))
        {
            // กำหนดข้อความแจ้งเตือนให้เลือกวันที่รับสินค้า
            errorMessage = "กรุณาเลือกวันที่สะดวกเข้ารับสินค้าที่หน้าร้าน";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // ตรวจสอบรูปแบบวันที่ตามมาตรฐาน ISO yyyy-MM-dd
        if (!DateOnly.TryParseExact(pickupDate.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate)
            // หรือตรวจสอบรูปแบบวันที่ทั่วไป
            && !DateOnly.TryParse(pickupDate.Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
        {
            // กำหนดข้อความแจ้งเตือนรูปแบบวันที่ไม่ถูกต้อง
            errorMessage = "รูปแบบวันที่รับสินค้าไม่ถูกต้อง กรุณาเลือกใหม่อีกครั้ง";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // แปลงปี พ.ศ. -> ค.ศ. ก่อนตรวจสอบเทียบกับวันปัจจุบันเสมอ (ป้องกันปี 2569 ถูกยอมรับเป็นอนาคต 543 ปี)
        parsedDate = NormalizeGregorianDate(parsedDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var todayGregorian = TodayGregorian();
        // ตรวจสอบเงื่อนไขว่าวันที่เลือกเป็นอดีตย้อนหลังหรือไม่
        if (parsedDate < todayGregorian)
        {
            // กำหนดข้อความแจ้งเตือนไม่ให้เลือกวันที่ย้อนหลัง
            errorMessage = "ไม่สามารถเลือกวันที่ย้อนหลังได้ กรุณาเลือกวันปัจจุบันเป็นต้นไป";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // ตรวจสอบเงื่อนไขว่าวันที่เลือกเกินระยะเวลา 2 ปีข้างหน้าหรือไม่
        if (parsedDate > todayGregorian.AddYears(2))
        {
            // กำหนดข้อความแจ้งเตือนให้เลือกวันที่ภายในระยะเวลาที่กำหนด
            errorMessage = "กรุณาเลือกวันที่นัดรับสินค้าภายในระยะเวลาไม่เกิน 2 ปีข้างหน้า";
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return false;
        }

        // กำหนดวันที่ที่ผ่านการตรวจสอบความถูกต้องแล้ว
        resolvedDate = parsedDate;
        // ส่งคืนค่าจริงเพื่อระบุว่าตรวจสอบวันที่ผ่านเรียบร้อย
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
                // เชื่อมโยงข้อมูลสินค้าและหมวดหมู่
                .Include(c => c.Product).ThenInclude(p => p.Category)
                // เชื่อมโยงข้อมูลสินค้าและสต็อกคงคลัง
                .Include(c => c.Product).ThenInclude(p => p.Inventory)
                // กรองเฉพาะรายการของผู้ใช้คนนี้
                .Where(c => c.UserId == userId.Value)
                // แปลงผลลัพธ์เป็นรายการ List
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
                // ตรวจสอบว่าสินค้าปิดการวางขายหรือไม่
                if (item.Product.IsActive == false)
                {
                    // ยกเลิกคำสั่งใน Transaction กรณีสินค้าปิดขาย
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
            // ประกาศตัวแปรเก็บผลการประเมินโปรโมชั่น
            PromoEvaluationResult? appliedEval = null;

            // กำหนดรหัสโปรโมชั่นที่จะใช้งานจริง
            string? effectivePromoCode = !string.IsNullOrWhiteSpace(voucherCode)
                // กรณีส่งรหัสคูปองมาในพารามิเตอร์
                ? voucherCode
                // อ่านหรือบันทึกข้อมูลสถานะผู้ใช้งานลงใน Session
                : HttpContext.Session.GetString(AppliedPromoSessionKey);

            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (!string.IsNullOrWhiteSpace(effectivePromoCode))
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var promo = FindPromotionByVoucherCode(effectivePromoCode);
                // ประเมินเงื่อนไขการใช้โปรโมชั่นกับสินค้าในตะกร้า
                appliedEval = EvaluateCartPromotion(promo, cartItems);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (!appliedEval.IsValid || appliedEval.Promotion == null)
                {
                    // ยกเลิกคำสั่งใน Transaction กรณีโปรโมชั่นไม่ถูกต้อง
                    transaction.Rollback();
                    // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                    TempData["ErrorMessage"] = appliedEval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นได้ กรุณาตรวจสอบอีกครั้ง";
                    // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                    return RedirectToAction("Cart");
                }
                // กำหนดจำนวนเงินส่วนลดจากผลการประเมิน
                discountAmount = appliedEval.DiscountAmount;
                // เพิ่มยอดการใช้สิทธิ์ของโปรโมชั่นขึ้น 1 ครั้ง
                appliedEval.Promotion.CurrentUsageCount += 1;
            }

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal total = Math.Max(0m, originalSubtotal - discountAmount);
            // สร้างรหัสใบสั่งจองพรีออเดอร์รูปแบบ PO-2026-XXXX (ไม่ซ้ำกับรหัสเดิมในระบบ)
            var reservationNo = "PO-2026-" + Random.Shared.Next(1000, 9999);
            // กำหนดหมายเลขโทรศัพท์ติดต่อ
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                // กรณีผู้ใช้กรอกหมายเลขโทรศัพท์
                ? contactPhone.Trim()
                // กรณีใช้หมายเลขโทรศัพท์จากบัญชี
                : (user?.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขความถูกต้องของหมายเลขโทรศัพท์
            if (!IsValidPhoneNumber(resolvedPhone))
            {
                // ยกเลิกคำสั่งใน Transaction กรณีเบอร์โทรศัพท์ไม่ถูกต้อง
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
                    // กำหนดหมายเลขใบสั่งจอง
                    ReservationNumber = reservationNo,
                    // กำหนดรหัสคูปอง
                    VoucherCode = appliedEval.VoucherCode,
                    // กำหนดชื่อโปรโมชั่น
                    PromoName = appliedEval.PromoName,
                    // กำหนดประเภทโปรโมชั่น
                    PromoType = appliedEval.PromoType,
                    // กำหนดยอดรวมก่อนหักส่วนลด
                    OriginalSubtotal = originalSubtotal,
                    // กำหนดจำนวนเงินส่วนลด
                    DiscountAmount = discountAmount,
                    // กำหนดสถานะรับแต้มสองเท่า
                    IsDoublePoints = appliedEval.IsDoublePoints
                };
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string logCode = GenerateLogCode("PRM");

                // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัสบันทึกระบบ
                    LogCode = logCode,
                    // กำหนดระดับความสำคัญ
                    LogLevel = "INFO",
                    // กำหนดชื่อโมดูล
                    Module = "preorder.promo",
                    // กำหนดข้อความบันทึก
                    Message = $"ใช้รหัสโปรโมชั่น {appliedEval.VoucherCode} กับใบจอง #{reservationNo} ({appliedEval.BenefitSummary})",
                    // แปลงข้อมูลเพย์โหลดเป็น JSON
                    DetailPayload = JsonSerializer.Serialize(promoPayload),
                    // กำหนดรหัสผู้ใช้งาน
                    UserId = userId.Value,
                    // กำหนดวันเวลาที่บันทึก
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
            HttpContext.Session.Remove(AppliedPromoSessionKey);

            // กำหนดข้อความต่อท้ายแจ้งเตือนการใช้โปรโมชั่นสำเร็จ
            string promoSuccessSuffix = appliedEval != null
                // ข้อความกรณีมีโปรโมชั่น
                ? $" (ใช้โค้ด {appliedEval.VoucherCode}: {appliedEval.BenefitSummary})"
                // ข้อความกรณีไม่มีโปรโมชั่น
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
                // กรณีเป็นสินค้าพรีออเดอร์ให้สต็อกเป็นศูนย์
                ? 0
                // กรณีสินค้าทั่วไปคำนวณจากสต็อกในมือลบยอดจอง
                : (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าสินค้ามีพอให้สั่งซื้อหรือไม่
            if (inv == null || available < qty)
            {
                // ยกเลิกคำสั่งทั้งหมดใน Transaction
                transaction.Rollback();
                // แจ้งเตือนข้อความว่าสินค้าหมดหรือไม่เพียงพอ
                TempData["ErrorMessage"] = available <= 0
                    // ข้อความกรณีสินค้าหมดชั่วคราว
                    ? $"ขออภัย สินค้า \"{product.Name}\" หมดชั่วคราว"
                    // ข้อความกรณีสินค้ามีไม่พอต่อจำนวนที่ต้องการ
                    : $"ขออภัย สินค้า \"{product.Name}\" มีจำนวนคงเหลือไม่เพียงพอ (เหลือ {available} ชิ้น)";
                // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้าเดิม
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // กำหนดหมายเลขโทรศัพท์ติดต่อ
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                // กรณีระบุเบอร์โทรศัพท์ในแบบฟอร์ม
                ? contactPhone.Trim()
                // กรณีใช้เบอร์โทรศัพท์จากบัญชี
                : (user?.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!IsValidPhoneNumber(resolvedPhone))
            {
                // ยกเลิกคำสั่งใน Transaction กรณีเบอร์โทรศัพท์ไม่ถูกต้อง
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
                // เชื่อมโยงข้อมูลตารางหมวดหมู่สินค้า
                .Include(p => p.Category)
                // เชื่อมโยงข้อมูลตารางสต็อกสินค้าคงคลัง
                .Include(p => p.Inventory)
                // ค้นหาตามรหัสสินค้าและต้องไม่ถูกปิดการใช้งาน
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
        // ดำเนินการกรณีไม่ได้ระบุรหัสสินค้า ให้ค้นหาสินค้าไฮไลท์ประจำร้าน
        else
        {
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            product = _db.Products
                // เชื่อมโยงข้อมูลหมวดหมู่
                .Include(p => p.Category)
                // เชื่อมโยงข้อมูลสต็อกคงคลัง
                .Include(p => p.Inventory)
                // ค้นหาสินค้าไฮไลท์ที่เปิดใช้งาน
                .FirstOrDefault(p => p.IsFeaturedHero && p.IsActive != false)
                // หรือดึงสินค้าแรกที่เปิดใช้งาน
                ?? _db.Products
                    // เชื่อมโยงข้อมูลหมวดหมู่
                    .Include(p => p.Category)
                    // เชื่อมโยงข้อมูลสต็อกคงคลัง
                    .Include(p => p.Inventory)
                    // ค้นหาสินค้ารายการแรกที่เปิดใช้งาน
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
            // กรณีเป็นสินค้าพรีออเดอร์ให้สต็อกพร้อมขายเป็นศูนย์
            ? 0
            // กรณีสินค้าทั่วไปคำนวณจากสต็อกคงคลังลบยอดจอง
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
        // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
        var inStockProducts = _db.Products
            // เชื่อมโยงข้อมูลตารางหมวดหมู่สินค้า
            .Include(p => p.Category)
            // เชื่อมโยงข้อมูลตารางสต็อกสินค้าคงคลัง
            .Include(p => p.Inventory)
            // กรองเงื่อนไขสินค้าที่มีสต็อกและไม่ใช่สินค้าปัจจุบัน
            .Where(p => p.ProductId != product.ProductId
                     // ต้องไม่ถูกปิดการใช้งาน
                     && p.IsActive != false
                     // ต้องไม่เป็นสินค้าพรีออเดอร์
                     && !p.IsPreorder
                     // ต้องมีข้อมูลสต็อกสินค้า
                     && p.Inventory != null
                     // จำนวนคงคลังต้องมากกว่ายอดที่ถูกจอง
                     && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0)
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();

        // ขั้นตอนที่ 4: ลำดับความสำคัญในการคัดเลือก (1. ซีรีส์เดียวกัน -> 2. หมวดหมู่เดียวกัน -> 3. สินค้าเข้าใหม่ล่าสุด)
        var selectedRelated = new List<Product>();

        // ลำดับที่ 1: ดึงสินค้าในซีรีส์เดียวกัน (SeriesName ตรงกัน)
        if (!string.IsNullOrWhiteSpace(product.SeriesName))
        {
            // ค้นหาสินค้าในซีรีส์เดียวกันจากสินค้าพร้อมส่ง
            var sameSeries = inStockProducts
                // กรองเฉพาะสินค้าที่ซีรีส์ตรงกัน
                .Where(p => !string.IsNullOrWhiteSpace(p.SeriesName) && string.Equals(p.SeriesName.Trim(), product.SeriesName.Trim(), StringComparison.OrdinalIgnoreCase))
                // เรียงลำดับจากวันที่สร้างล่าสุด
                .OrderByDescending(p => p.CreatedAt)
                // เรียงตามรหัสสินค้าจากมากไปน้อย
                .ThenByDescending(p => p.ProductId)
                // แปลงเป็นรายการ List
                .ToList();
            // นำสินค้าในซีรีส์เดียวกันเพิ่มลงในรายการแนะนำ
            selectedRelated.AddRange(sameSeries.Take(5));
        }

        // ลำดับที่ 2: ดึงสินค้าในหมวดหมู่เดียวกัน (CategoryId ตรงกัน) หากยังไม่ครบ 5 รายการ
        if (selectedRelated.Count < 5)
        {
            // ค้นหาสินค้าในหมวดหมู่เดียวกันจากสินค้าพร้อมส่ง
            var sameCategory = inStockProducts
                // กรองเฉพาะหมวดหมู่เดียวกันและไม่ซ้ำกับที่เลือกไว้แล้ว
                .Where(p => p.CategoryId == product.CategoryId && !selectedRelated.Any(r => r.ProductId == p.ProductId))
                // เรียงลำดับจากวันที่สร้างล่าสุด
                .OrderByDescending(p => p.CreatedAt)
                // เรียงตามรหัสสินค้าจากมากไปน้อย
                .ThenByDescending(p => p.ProductId)
                // แปลงเป็นรายการ List
                .ToList();
            // เติมเต็มสินค้าในหมวดหมู่เดียวกัน
            selectedRelated.AddRange(sameCategory.Take(5 - selectedRelated.Count));
        }

        // ลำดับที่ 3: เติมเต็มด้วยสินค้าเข้าใหม่ล่าสุดที่มีในสต็อก จนครบ 5 รายการ
        if (selectedRelated.Count < 5)
        {
            // ค้นหาสินค้าเข้าใหม่ล่าสุดที่มีในสต็อก
            var newestInStock = inStockProducts
                // กรองเฉพาะสินค้าที่ไม่ซ้ำกับที่เลือกไว้แล้ว
                .Where(p => !selectedRelated.Any(r => r.ProductId == p.ProductId))
                // เรียงลำดับจากวันที่สร้างล่าสุด
                .OrderByDescending(p => p.CreatedAt)
                // เรียงตามรหัสสินค้าจากมากไปน้อย
                .ThenByDescending(p => p.ProductId)
                // แปลงเป็นรายการ List
                .ToList();
            // เติมเต็มสินค้าเข้าใหม่ล่าสุดจนครบโควตา 5 รายการ
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
                // เชื่อมโยงรายการสินค้าในใบสั่งจอง
                .Include(p => p.Preorderitems)
                    // เชื่อมโยงข้อมูลสินค้าแต่ละรายการ
                    .ThenInclude(pi => pi.Product)
                // กรองเฉพาะรายการของลูกค้าคนนี้
                .Where(p => p.UserId == userId.Value)
                // เรียงลำดับจากวันที่สั่งจองล่าสุด
                .OrderByDescending(p => p.CreatedAt)
                // แปลงผลลัพธ์เป็นรายการ List
                .ToList();
        }

        // กรองตามคำค้นหาหมายเลขใบจอง เบอร์โทรศัพท์ หรือชื่อสินค้า
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // แปลงคำค้นหาเป็นตัวพิมพ์เล็กและสร้างตัวแปรที่ตัดเครื่องหมายขีด เครื่องหมายสี่เหลี่ยม และช่องว่างออกทั้งหมด
            string term = searchTerm!.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");

            // กรองรายการใบสั่งจองตามเงื่อนไขคำค้นหา
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

                // ตรวจสอบเงื่อนไขคำค้นหากับรหัสใบจอง เบอร์โทรศัพท์ และสินค้าในใบจอง
                return resNoLower.Contains(term)
                    // ตรวจสอบรหัสใบจองแบบตัดเครื่องหมายขีด
                    || (!string.IsNullOrEmpty(termNoHyphen) && resNoNormalized.Contains(termNoHyphen))
                    // ตรวจสอบเบอร์โทรศัพท์ติดต่อ
                    || phoneLower.Contains(term)
                    // ตรวจสอบเบอร์โทรศัพท์แบบตัดเครื่องหมายขีด
                    || (!string.IsNullOrEmpty(termNoHyphen) && phoneNormalized.Contains(termNoHyphen))
                    // ตรวจสอบรายการสินค้าในใบสั่งจอง
                    || p.Preorderitems.Any(pi => pi.Product != null && (
                        // ตรวจสอบชื่อสินค้า
                        pi.Product.Name.ToLower().Contains(term) ||
                        // ตรวจสอบรหัส SKU สินค้า
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
                // กรองใบสั่งจองที่มีสถานะยังดำเนินการอยู่
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "PendingConfirm" || p.CurrentStatus == "WaitingStock" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "Reserved").ToList();
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (statusFilter == "completed")
            {
                // กรองใบสั่งจองที่มีสถานะเสร็จสมบูรณ์
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "Completed").ToList();
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (statusFilter == "cancelled")
            {
                // กรองใบสั่งจองที่มีสถานะยกเลิก
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "Cancelled").ToList();
            }
        }

        // ขั้นตอนที่ 2: ประกอบข้อมูล PreorderListViewModel
        var viewModel = new PreorderListViewModel
        {
            // คำค้นหาที่ระบุ
            SearchTerm = searchTerm,
            // ตัวกรองสถานะที่เลือก
            StatusFilter = statusFilter ?? "all"
        };
        // เพิ่มข้อมูล Preorder ดั้งเดิมเพื่อให้เข้ากันได้แบบ IEnumerable<Preorder>
        viewModel.AddRange(rawPreorders);

        // ขั้นตอนที่ 3: แปลงแต่ละใบสั่งจองเป็น PreorderSummaryViewModel พร้อมขั้นตอนการติดตาม (ดึง promoMap และ cancelLogs เพียงครั้งเดียว)
        var promoLogs = _db.Systemlogs.Where(l => l.Module == "preorder.promo").ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var promoMap = BuildPreorderPromoMap(promoLogs);
        // ดึงบันทึกการไม่รับสินค้า ไม่มารับสินค้า และการยกเลิกคำสั่งจองจากตาราง Systemlogs
        var cancelLogs = _db.Systemlogs
            // กรองบันทึกที่เกี่ยวข้องกับเคาน์เตอร์และการยกเลิก
            .Where(l => l.Module == "CounterPreorder" || l.Module == "preorder.cancel" || l.Module == "reward.cancel")
            // เรียงตามวันเวลาล่าสุด
            .OrderByDescending(l => l.Timestamp)
            // แปลงเป็นรายการ List
            .ToList();
        // ส่งรายการบันทึกการยกเลิก/ไม่รับสินค้าไปยัง ViewBag เพื่อให้หน้า View แสดงสถานะและเหตุผลตามจริง
        ViewBag.CancelLogs = cancelLogs;

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

            // ตรวจสอบสถานะใบจองเพื่อกำหนดป้ายสถานะและขั้นตอนการติดตาม
            if (po.CurrentStatus == "PendingConfirm" || po.CurrentStatus == "WaitingStock" || po.CurrentStatus == "ReadyForPickup" || po.CurrentStatus == "Reserved")
            {
                // กำหนดข้อความป้ายสถานะ
                statusBadgeText = po.CurrentStatus == "ReadyForPickup" ? "สินค้าพร้อมรับแล้ว" : "รอดำเนินการ";
                // เพิ่มขั้นตอนที่ 1 ในการติดตาม
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                // เพิ่มขั้นตอนที่ 2 ในการติดตาม
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "รอชำระเงิน/รับสินค้า", Description = "รอสินค้าเข้าสู่สาขาและแจ้งรับ", IsCompleted = false, IsCurrent = true });
            }
            // ตรวจสอบเงื่อนไขกรณีคำสั่งจองสำเร็จสมบูรณ์
            else if (po.CurrentStatus == "Completed")
            {
                // กำหนดข้อความป้ายสถานะสำเร็จ
                statusBadgeText = "สำเร็จเรียบร้อย";
                // กำหนดคลาสตกแต่งป้ายสถานะสีเขียว
                statusBadgeClass = "bg-emerald-50/80 border-emerald-200/80 text-emerald-800";
                // กำหนดข้อความคำแนะนำ
                noticeText = "รายการพรีออเดอร์นี้เสร็จสมบูรณ์เรียบร้อย ลูกค้าชำระเงินและรับสินค้าแล้ว ขอขอบคุณที่เลือกซื้อสินค้ากับ FriendHouse";
                // เพิ่มขั้นตอนที่ 1 แจ้งจอง
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                // เพิ่มขั้นตอนที่ 2 ชำระเงินรับสินค้า
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "ชำระเงิน/รับสินค้าแล้ว", Description = "รับสินค้าที่เคาน์เตอร์แล้ว", IsCompleted = true, IsCurrent = false });
                // เพิ่มขั้นตอนที่ 3 สำเร็จ
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 3, Title = "สำเร็จ", Description = "เสร็จสมบูรณ์", IsCompleted = true, IsCurrent = true });
            }
            // ตรวจสอบเงื่อนไขกรณีคำสั่งจองถูกยกเลิก
            else if (po.CurrentStatus == "Cancelled")
            {
                // ดึงข้อมูลเหตุผลการยกเลิกจากบันทึกระบบ
                var cancelMeta = ResolveOrderCancelMeta(cancelLogs, po.ReservationNumber, isReward: false);
                // กำหนดข้อความป้ายสถานะการยกเลิก
                statusBadgeText = cancelMeta.BadgeTitle;
                // กำหนดคลาสสีแดงสำหรับสถานะยกเลิก
                statusBadgeClass = "bg-rose-50/80 border-rose-200/80 text-rose-800";
                // กำหนดข้อความแจ้งเหตุผลการยกเลิก
                noticeText = cancelMeta.NoticeText;
                // เพิ่มขั้นตอนแจ้งจองออเดอร์
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                // เพิ่มขั้นตอนสถานะการยกเลิก
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = cancelMeta.StepTitle, Description = cancelMeta.ReasonDetail, IsCompleted = false, IsCurrent = false });
            }

            // ค้นหาข้อมูลโปรโมชั่นที่ผูกกับหมายเลขใบจองนี้
            promoMap.TryGetValue(po.ReservationNumber, out var promoMeta);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            // สร้างโมเดลสรุปข้อมูลใบจองพรีออเดอร์สำหรับแสดงผล
            var summary = new PreorderSummaryViewModel
            {
                // กำหนดรหัสใบสั่งจอง
                PreorderId = po.PreorderId,
                // กำหนดหมายเลขใบสั่งจอง
                ReservationNumber = po.ReservationNumber,
                // กำหนดเบอร์โทรศัพท์ติดต่อ
                ContactPhone = po.ContactPhone,
                // กำหนดสาขาที่รับสินค้า
                PickupBranch = po.PickupBranch ?? "FriendHouse HQ สาขาสยามสแควร์",
                // กำหนดสถานะปัจจุบัน
                CurrentStatus = po.CurrentStatus,
                // กำหนดข้อความป้ายสถานะ
                StatusBadgeText = statusBadgeText,
                // กำหนดคลาสตกแต่งป้ายสถานะ
                StatusBadgeClass = statusBadgeClass,
                // กำหนดข้อความคำแนะนำ
                NoticeText = noticeText,
                // กำหนดวันเวลาที่บันทึก
                CreatedAt = po.CreatedAt,
                // กำหนดวันเวลาที่บันทึกจัดรูปแบบภาษาไทย
                CreatedAtFormatted = po.CreatedAt.ToString("dd MMM yyyy HH:mm น."),
                // กำหนดวันที่นัดรับสินค้า
                EstimatedPickupDate = po.EstimatedPickupDate,
                // กำหนดวันที่นัดรับสินค้าจัดรูปแบบภาษาไทย
                EstimatedPickupDateFormatted = po.EstimatedPickupDate.ToString("dd MMMM yyyy"),
                // กำหนดยอดเงินสุทธิรวม
                TotalFinalPrice = po.TotalFinalPrice,
                // กำหนดรหัสโปรโมชั่นที่ใช้งาน
                AppliedPromoCode = promoMeta?.VoucherCode,
                // กำหนดจำนวนเงินส่วนลด
                DiscountAmount = promoMeta?.DiscountAmount ?? 0m,
                // กำหนดยอดเงินรวมก่อนหักส่วนลด
                OriginalSubtotal = promoMeta?.OriginalSubtotal ?? po.TotalFinalPrice,
                // กำหนดสถานะแต้มสองเท่า
                IsDoublePoints = promoMeta?.IsDoublePoints ?? false,
                // กำหนดขั้นตอนการติดตามสถานะ
                Steps = steps,
                // แปลงรายการสินค้าในใบสั่งจอง
                Items = po.Preorderitems.Select(pi => new PreorderItemDetailViewModel
                {
                    // กำหนดรหัสรายการ
                    ItemId = pi.ItemId,
                    // กำหนดรหัสสินค้า
                    ProductId = pi.ProductId,
                    // กำหนดชื่อสินค้า
                    ProductName = pi.Product?.Name ?? "รายการฟิกเกอร์พรีออเดอร์",
                    // กำหนดรูปภาพสินค้า
                    ImageUrl = !string.IsNullOrEmpty(pi.Product?.ImageUrl) ? pi.Product.ImageUrl : "~/media/images/naruto_hero_fig.png",
                    // กำหนดราคาต่อหน่วย
                    UnitPrice = pi.UnitPrice,
                    // กำหนดจำนวนสินค้า
                    Quantity = pi.Quantity
                }).ToList()
            };

            // เพิ่มรายการใบจองลงใน ViewModel
            viewModel.Preorders.Add(summary);
        }

        // ขั้นตอนที่ 4: ดึงรายการจองแลกของรางวัลด้วยแต้มสะสมของสมาชิก (RWD-...) เพื่อแสดงรวมเป็นรายการจองของลูกค้า
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rewardMetaMap = BuildRedemptionMetaMap(rewardLogs);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawRedemptions = userId.HasValue
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            ? _db.Rewardredemptions
                // เชื่อมโยงข้อมูลของรางวัล
                .Include(r => r.Reward)
                // เชื่อมโยงข้อมูลผู้ใช้งาน
                .Include(r => r.User)
                // กรองเฉพาะรายการของผู้ใช้คนนี้
                .Where(r => r.UserId == userId.Value)
                // เรียงลำดับจากวันที่แลกล่าสุด
                .OrderByDescending(r => r.RedeemedAt)
                // แปลงเป็นรายการ List
                .ToList()
            // กรณีไม่มีผู้ใช้ล็อกอินให้คืนรายการว่าง
            : new List<Rewardredemption>();

        // กรองรายการแลกของรางวัลตามคำค้นหา (รองรับค้นหารหัส RWD แบบไม่ใส่ขีด เบอร์โทรที่ระบุตอนแลก/บัญชี และชื่อของรางวัล)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string term = searchTerm.Trim().ToLower();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองรายการแลกของรางวัลตามเงื่อนไขคำค้นหา
            rawRedemptions = rawRedemptions.Where(r =>
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string codeLower = (r.RedemptionCode ?? "").ToLower();
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string codeNoHyphen = codeLower.Replace("-", "").Replace("#", "").Replace(" ", "");
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string accountPhone = NormalizePhoneDigits(r.User?.Phone);
                // ดึงข้อมูลเมทาดาต้าของรหัสการแลก
                rewardMetaMap.TryGetValue(r.RedemptionCode ?? "", out var rMeta);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string metaPhone = NormalizePhoneDigits(rMeta?.ContactPhone);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string rewardTitle = (r.Reward?.Title ?? "").ToLower();

                // ตรวจสอบเงื่อนไขคำค้นหาตรงกับรหัสแลกรางวัล เบอร์โทร หรือชื่อของรางวัล
                return codeLower.Contains(term)
                    // ตรวจสอบรหัสการแลกแบบตัดเครื่องหมายขีด
                    || (!string.IsNullOrEmpty(termNoHyphen) && codeNoHyphen.Contains(termNoHyphen))
                    // ตรวจสอบเบอร์โทรศัพท์บัญชีแบบตัดเครื่องหมายขีด
                    || (!string.IsNullOrEmpty(termNoHyphen) && accountPhone.Contains(termNoHyphen))
                    // ตรวจสอบเบอร์โทรศัพท์ที่กรอกตอนแลกแบบตัดเครื่องหมายขีด
                    || (!string.IsNullOrEmpty(termNoHyphen) && metaPhone.Contains(termNoHyphen))
                    // ตรวจสอบชื่อของรางวัล
                    || rewardTitle.Contains(term);
            }).ToList();
        }

        // กรองรายการแลกของรางวัลตามตัวกรองสถานะให้สอดคล้องกับใบจองพรีออเดอร์
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            // กรองรายการตามสถานะที่เลือก
            rawRedemptions = statusFilter switch
            {
                // สถานะกำลังรอดำเนินการรับของรางวัล
                "active" => rawRedemptions.Where(r => r.Status == "PendingPickup").ToList(),
                // สถานะรับของรางวัลเรียบร้อยแล้ว
                "completed" => rawRedemptions.Where(r => r.Status == "Completed").ToList(),
                // สถานะยกเลิกการแลกของรางวัล
                "cancelled" => rawRedemptions.Where(r => r.Status == "Cancelled").ToList(),
                // กรณีอื่นให้ใช้รายการทั้งหมด
                _ => rawRedemptions
            };
        }

        // กำหนดรายการแลกของรางวัลที่แปลงเป็น ViewModel
        viewModel.Redemptions = MapRedemptionsToViewModels(rawRedemptions, rewardMetaMap);

        // ส่งข้อมูลโมเดลไปยังหน้า View เพื่อเรนเดอร์ส่วนติดต่อผู้ใช้
        return View(viewModel);
    }

    // ฟังก์ชันช่วยแปลงรายการ Rewardredemption จากฐานข้อมูลเป็น ViewModel (ใช้ร่วมกันทั้งหน้า PreorderStatus และ Rewards)
    private static List<RewardRedemptionViewModel> MapRedemptionsToViewModels(
        IEnumerable<Rewardredemption> items,
        IReadOnlyDictionary<string, RedemptionMetaPayload>? metaMap = null)
    {
        // แปลงข้อมูลการแลกของรางวัลเป็นรายการ ViewModel
        return items.Select(red =>
        {
            // ประกาศตัวแปรเก็บข้อมูลเมทาดาต้า
            RedemptionMetaPayload? meta = null;
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (metaMap != null && !string.IsNullOrEmpty(red.RedemptionCode))
            {
                // ค้นหาข้อมูลเมทาดาต้าของรหัสการแลกของรางวัล
                metaMap.TryGetValue(red.RedemptionCode, out meta);
            }

            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new RewardRedemptionViewModel
            {
                // รหัสการแลกของรางวัล
                RedemptionId = red.RedemptionId,
                // หมายเลขรหัสคูปองแลกของรางวัล
                RedemptionCode = red.RedemptionCode,
                // รหัสของรางวัล
                RewardId = red.RewardId,
                // ชื่อของรางวัล
                RewardTitle = red.Reward?.Title ?? "ของรางวัลพรีเมียม",
                // แบรนด์ของรางวัล
                RewardBrand = red.Reward?.Brand ?? "FriendHouse Exclusive",
                // รูปภาพของรางวัล
                ImageUrl = !string.IsNullOrEmpty(red.Reward?.ImageUrl) ? red.Reward.ImageUrl : "~/media/images/naruto_hero_fig.png",
                // จำนวนแต้มที่ใช้แลก
                PointsSpent = red.PointsSpent,
                // สถานะการแลกของรางวัล
                Status = red.Status,
                // วันเวลาที่แลกของรางวัล
                RedeemedAt = red.RedeemedAt,
                // วันเวลาที่แลกของรางวัลจัดรูปแบบภาษาไทย
                RedeemedAtFormatted = red.RedeemedAt.ToString("dd MMM yyyy HH:mm น."),
                // วันเวลาที่รับของรางวัลจัดรูปแบบภาษาไทย
                CompletedAtFormatted = red.CompletedAt?.ToString("dd MMM yyyy HH:mm น."),
                // วันที่นัดรับของรางวัลจัดรูปแบบภาษาไทย
                PickupDateFormatted = meta != null ? FormatPickupDateThai(meta.PickupDate) : null,
                // หมายเลขโทรศัพท์ติดต่อ
                ContactPhone = meta?.ContactPhone ?? red.User?.Phone
            };
        }).ToList();
    }

    // DELETE/UPDATE: ยกเลิกคำสั่งจองพรีออเดอร์ของสมาชิกและคืนสต็อกสำรอง พร้อมบันทึกเหตุผลการยกเลิกที่เลือก (POST Method)
    // WHY: ให้สมาชิกสามารถยกเลิกใบสั่งจองที่ยังไม่ได้รับสินค้าด้วยตนเองผ่านแบบฟอร์ม MVC มาตรฐาน พร้อมคืนค่า ReservedQuantity ให้คลังสินค้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CancelPreorder สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public IActionResult CancelPreorder(int preorderId, string? reason = null, string? remark = null)
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
            // เชื่อมโยงรายการสินค้าในใบสั่งจอง
            .Include(p => p.Preorderitems)
            // ค้นหาตามรหัสใบสั่งจองและรหัสลูกค้า
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
                // บันทึกวันเวลาที่ปรับปรุงสต็อก
                inv.UpdatedAt = DateTime.Now;
            }
        }

        // คืนสิทธิ์จำนวนการใช้โค้ดโปรโมชั่นหากใบจองนี้ใช้โค้ดส่วนลดไว้
        var promoLogs = _db.Systemlogs
            // กรองบันทึกที่เกี่ยวข้องกับโปรโมชั่นของใบสั่งจองนี้
            .Where(l => l.Module == "preorder.promo" && l.DetailPayload != null && l.DetailPayload.Contains(preorder.ReservationNumber))
            // แปลงเป็นรายการ List
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
                        // ปรับลดยอดการใช้งานคูปองลง 1 ครั้ง
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

        // จัดรูปแบบเหตุผลการยกเลิกตามที่ลูกค้าเลือกหรือกรอกเข้ามา
        string cleanCancelReason = string.IsNullOrWhiteSpace(reason)
            // กรณีไม่ได้ระบุเหตุผล
            ? "ลูกค้ากดยกเลิกคำสั่งจองด้วยตนเอง"
            // กรณีระบุเหตุผล
            : reason.Trim();
        // ต่อข้อความหมายเหตุเพิ่มเติมหากลูกค้ากรอกรายละเอียดมาด้วย
        string cancelReasonSummary = !string.IsNullOrWhiteSpace(remark)
            // กรณีมีหมายเหตุเพิ่มเติม
            ? $"สาเหตุ: {cleanCancelReason} | รายละเอียด: {remark.Trim()}"
            // กรณีไม่มีหมายเหตุเพิ่มเติม
            : $"สาเหตุ: {cleanCancelReason}";

        // ขั้นตอนที่ 5: บันทึกประวัติการยกเลิกใบสั่งจองลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log การยกเลิก
            LogCode = GenerateLogCode("PO-CAN"),
            // กำหนดระดับความสำคัญเป็นคำเตือน
            LogLevel = "WARNING",
            // กำหนดชื่อโมดูล
            Module = "preorder.cancel",
            // กำหนดข้อความรายละเอียดการยกเลิก
            Message = $"สมาชิกยกเลิกใบสั่งจองพรีออเดอร์หมายเลข #{preorder.ReservationNumber} {cancelReasonSummary}",
            // กำหนดรหัสผู้ใช้งานที่ทำรายการ
            UserId = userId.Value,
            // กำหนดวันเวลาที่ทำรายการ
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
                // กำหนดจำนวนแต้มสะสมของผู้ใช้งาน
                userPoints = user.Points;
                // กำหนดหมายเลขโทรศัพท์ของผู้ใช้งาน
                userPhone = user.Phone ?? string.Empty;
            }

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var rewardMetaMap = BuildRedemptionMetaMap(rewardLogs);

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            var rawRedemptions = _db.Rewardredemptions
                // เชื่อมโยงข้อมูลของรางวัล
                .Include(r => r.Reward)
                // เชื่อมโยงข้อมูลผู้ใช้งาน
                .Include(r => r.User)
                // กรองเฉพาะรายการของผู้ใช้คนนี้
                .Where(r => r.UserId == userId.Value)
                // เรียงลำดับจากวันที่แลกล่าสุด
                .OrderByDescending(r => r.RedeemedAt)
                // แปลงผลลัพธ์เป็นรายการ List
                .ToList();

            userRedemptions = MapRedemptionsToViewModels(rawRedemptions, rewardMetaMap);
        }

        // ขั้นตอนที่ 3: ดึงรายการของรางวัลที่พร้อมให้แลกจากฐานข้อมูล
        var rawRewards = _db.Rewards
            // กรองเฉพาะของรางวัลที่เปิดให้แลก
            .Where(r => r.IsAvailable == true || r.IsAvailable == null)
            // เรียงลำดับตามแต้มที่ต้องใช้จากน้อยไปมาก
            .OrderBy(r => r.PointsRequired)
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();

        // ขั้นตอนที่ 4: แปลงเป็น RewardItemViewModel พร้อมคำนวณ CanAfford ตามแต้มจริงของสมาชิก
        var rewardItems = rawRewards.Select(r => new RewardItemViewModel
        {
            // รหัสของรางวัล
            RewardId = r.RewardId,
            // รหัสโค้ดของรางวัล
            RewardCode = r.RewardCode,
            // ชื่อของรางวัล
            Title = r.Title,
            // แบรนด์ของรางวัล
            Brand = r.Brand ?? "FriendHouse Exclusive",
            // ป้ายหมวดหมู่ของรางวัล
            CategoryTag = !string.IsNullOrEmpty(r.CategoryTag) ? r.CategoryTag : "พิเศษ",
            // จำนวนแต้มที่ต้องใช้แลก
            PointsRequired = r.PointsRequired,
            // จำนวนคงเหลือในสต็อก
            StockQuantity = r.StockQuantity,
            // รูปภาพของรางวัล
            ImageUrl = !string.IsNullOrEmpty(r.ImageUrl) ? r.ImageUrl : "~/media/images/naruto_hero_fig.png",
            // สถานะพร้อมให้แลก
            IsAvailable = (r.IsAvailable ?? true) && r.StockQuantity > 0,
            // ตรวจสอบว่าแต้มของผู้ใช้เพียงพอหรือไม่
            CanAfford = isLoggedIn && (userPoints >= r.PointsRequired)
        }).ToList();

        // ขั้นตอนที่ 5: ประกอบ RewardsCatalogViewModel
        var viewModel = new RewardsCatalogViewModel
        {
            // สถานะการเข้าสู่ระบบ
            IsLoggedIn = isLoggedIn,
            // หมายเลขโทรศัพท์ของผู้ใช้งาน
            UserPhone = userPhone,
            // แต้มสะสมของผู้ใช้งาน
            UserPoints = userPoints,
            // รายการของรางวัลทั้งหมด
            Rewards = rewardItems,
            // รายการประวัติการแลกของรางวัล
            UserRedemptions = userRedemptions
        };

        // ส่งค่าผ่าน ViewBag เพื่อความเข้ากันได้ย้อนหลัง
        ViewBag.UserPoints = userPoints;
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.IsLoggedIn = isLoggedIn;
        // กำหนดค่าข้อมูลเสริมลงใน ViewBag/ViewData เพื่อส่งต่อไปยังหน้า View
        ViewBag.UserRedemptions = _db.Rewardredemptions
            // เชื่อมโยงข้อมูลของรางวัล
            .Include(r => r.Reward)
            // กรองเฉพาะรายการของผู้ใช้งานคนนี้
            .Where(r => userId.HasValue && r.UserId == userId.Value)
            // เรียงลำดับจากวันที่แลกล่าสุด
            .OrderByDescending(r => r.RedeemedAt)
            // แปลงเป็นรายการ List
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

        // เริ่มต้นฐานข้อมูล Transaction เพื่อความถูกต้องของแต้มและสต็อก
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
                // ยกเลิกคำสั่งใน Transaction กรณีไม่พบข้อมูล
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ไม่พบข้อมูลสมาชิกหรือของรางวัลที่ต้องการแลก";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // กำหนดหมายเลขโทรศัพท์ติดต่อ
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                // กรณีระบุเบอร์โทรศัพท์ในแบบฟอร์ม
                ? contactPhone.Trim()
                // กรณีใช้เบอร์โทรศัพท์จากบัญชี
                : (user.Phone ?? string.Empty).Trim();
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!IsValidPhoneNumber(resolvedPhone))
            {
                // ยกเลิกคำสั่งใน Transaction กรณีเบอร์โทรศัพท์ไม่ถูกต้อง
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "กรุณาระบุเบอร์โทรศัพท์ติดต่อที่ถูกต้อง (9-10 หลัก)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // ตรวจสอบว่าสถานะของรางวัลถูกปิดให้บริการหรือสต็อกหมดหรือไม่
            if (reward.IsAvailable != true || reward.StockQuantity <= 0)
            {
                // ยกเลิกคำสั่งใน Transaction กรณีของรางวัลหมด
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = "ขออภัย ของรางวัลชิ้นนี้หมดสต็อกหรือปิดให้บริการชั่วคราว";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // ตรวจสอบว่าแต้มสะสมของสมาชิกน้อยกว่าแต้มที่ต้องใช้หรือไม่
            if (user.Points < reward.PointsRequired)
            {
                // ยกเลิกคำสั่งใน Transaction กรณีแต้มสะสมไม่เพียงพอ
                transaction.Rollback();
                // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData สำหรับแสดงผลในหน้าถัดไป
                TempData["ErrorMessage"] = $"แต้มสะสมของคุณไม่เพียงพอ (ต้องการ {reward.PointsRequired} แต้ม แต่คุณมี {user.Points} แต้ม)";
                // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
                return RedirectToAction("Rewards");
            }

            // หักแต้มสะสมของสมาชิกตามจำนวนแต้มที่รางวัลต้องการ และจองสต็อกของรางวัลไว้ 1 ชิ้นทันที
            user.Points -= reward.PointsRequired;
            // ปรับลดยอดสต็อกของรางวัลลง 1 ชิ้นโดยไม่ให้ติดลบ
            reward.StockQuantity = Math.Max(0, reward.StockQuantity - 1);

            // สุ่มตัวเลข 6 หลักเพื่อใช้สร้างรหัสรับของรางวัล
            int randomSuffix = Random.Shared.Next(100000, 999999);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string redemptionCode = "RWD-" + randomSuffix;

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var redemption = new Rewardredemption
            {
                // กำหนดรหัสคูปองแลกของรางวัล
                RedemptionCode = redemptionCode,
                // กำหนดรหัสผู้ใช้งาน
                UserId = user.UserId,
                // กำหนดรหัสของรางวัล
                RewardId = reward.RewardId,
                // กำหนดแต้มที่ใช้แลก
                PointsSpent = reward.PointsRequired,
                // กำหนดสถานะเป็นรอมารับของรางวัล
                Status = "PendingPickup",
                // กำหนดวันเวลาที่ทำรายการแลก
                RedeemedAt = DateTime.Now
            };
            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Rewardredemptions.Add(redemption);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var metaPayload = new RedemptionMetaPayload
            {
                // กำหนดรหัสการแลกของรางวัล
                RedemptionCode = redemptionCode,
                // กำหนดวันที่นัดรับของรางวัล
                PickupDate = estPickupDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                // กำหนดหมายเลขโทรศัพท์ติดต่อ
                ContactPhone = resolvedPhone
            };
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string pickupThai = estPickupDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"));

            // คิวรีหรือปรับปรุงข้อมูลจากตารางในฐานข้อมูลผ่าน Entity Framework Core
            _db.Systemlogs.Add(new Systemlog
            {
                // กำหนดรหัส Log
                LogCode = GenerateLogCode("RWD"),
                // กำหนดระดับความสำคัญ
                LogLevel = "INFO",
                // กำหนดชื่อโมดูล
                Module = "reward.redeem",
                // กำหนดข้อความบันทึก
                Message = $"สมาชิก {user.FullName ?? user.Email} จองแลกของรางวัล \"{reward.Title}\" รหัส #{redemptionCode} ใช้ {reward.PointsRequired:N0} แต้ม (นัดรับ: {pickupThai} · โทร: {resolvedPhone})",
                // กำหนดเพย์โหลด JSON
                DetailPayload = JsonSerializer.Serialize(metaPayload),
                // กำหนดรหัสผู้ใช้งาน
                UserId = user.UserId,
                // กำหนดวันเวลาที่บันทึก
                Timestamp = DateTime.Now
            });

            // บันทึกการเปลี่ยนแปลงข้อมูลทั้งหมดลงในฐานข้อมูล MySQL
            _db.SaveChanges();
            // ยืนยันคำสั่งทั้งหมดใน Transaction (Commit) เมื่อทำงานครบทุกขั้นตอน
            transaction.Commit();

            // บันทึกข้อความแจ้งสถานะการทำงานลงใน TempData สำหรับแสดงผลบนหน้าเว็บ
            TempData["SuccessMessage"] = $"จองแลกของรางวัล \"{reward.Title}\" สำเร็จ! รหัสรับของรางวัลคือ {redemptionCode} (นัดรับ {pickupThai} · โทร {resolvedPhone})";
            // เปลี่ยนเส้นทางกลับไปยัง Action ที่กำหนดหลังประมวลผลเสร็จ
            return RedirectToAction("Rewards");
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch (Exception)
        {
            // ยกเลิกคำสั่งใน Transaction กรณีเกิดข้อผิดพลาด
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
    public IActionResult CancelRedemption(long redemptionId, string? returnAction, string? reason = null, string? remark = null)
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
            // เชื่อมโยงข้อมูลของรางวัล
            .Include(r => r.Reward)
            // เชื่อมโยงข้อมูลผู้ใช้งาน
            .Include(r => r.User)
            // ค้นหาตามรหัสการแลกและรหัสผู้ใช้งาน
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
            // คืนแต้มสะสมที่ใช้แลกกลับเข้าบัญชีผู้ใช้งาน
            redemption.User.Points += redemption.PointsSpent;
        }
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (redemption.Reward != null)
        {
            // คืนจำนวนสต็อกของรางวัลกลับเข้าคลัง 1 ชิ้น
            redemption.Reward.StockQuantity += 1;
        }

        // จัดรูปแบบเหตุผลการยกเลิกการแลกของรางวัลตามที่ลูกค้าเลือก
        string cleanRedeemReason = string.IsNullOrWhiteSpace(reason)
            // กรณีไม่ได้ระบุเหตุผล
            ? "ลูกค้ากดยกเลิกการแลกของรางวัลด้วยตนเอง"
            // กรณีระบุเหตุผล
            : reason.Trim();
        // ต่อข้อความรายละเอียดเพิ่มเติมหากมีระบุไว้
        string redeemReasonSummary = !string.IsNullOrWhiteSpace(remark)
            // กรณีมีหมายเหตุเพิ่มเติม
            ? $"สาเหตุ: {cleanRedeemReason} | รายละเอียด: {remark.Trim()}"
            // กรณีไม่มีหมายเหตุเพิ่มเติม
            : $"สาเหตุ: {cleanRedeemReason}";

        // บันทึกกิจกรรมการยกเลิกการแลกของรางวัลลง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log การยกเลิก
            LogCode = GenerateLogCode("RWD-CAN"),
            // กำหนดระดับความสำคัญเป็นคำเตือน
            LogLevel = "WARNING",
            // กำหนดชื่อโมดูล
            Module = "reward.cancel",
            // กำหนดข้อความรายละเอียดการยกเลิก
            Message = $"สมาชิกยกเลิกการจองแลกของรางวัล #{redemption.RedemptionCode} ({redemption.Reward?.Title}) {redeemReasonSummary} | คืน {redemption.PointsSpent:N0} แต้มและคืนสต็อกแล้ว",
            // กำหนดรหัสผู้ใช้งาน
            UserId = userId.Value,
            // กำหนดวันเวลาที่บันทึก
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




    // คีย์เซสชันสำหรับจัดเก็บรหัสคูปองส่วนลดที่ลูกค้าเก็บสะสมไว้
    // คีย์เซสชันสำหรับจัดเก็บรหัสคูปองส่วนลดที่ลูกค้าเก็บสะสมไว้
    private const string CollectedPromoSessionKey = "CollectedPromoCodes";
    // คีย์เซสชันสำหรับจัดเก็บรหัสคูปองส่วนลดที่กำลังใช้งานกับตะกร้าปัจจุบัน
    private const string AppliedPromoSessionKey = "AppliedPromoCode";

    // กำหนดวัฒนธรรมและรูปแบบภาษาไทยสำหรับแปลงและแสดงผลวันที่
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    // ฟังก์ชันคืนค่าวันที่ปัจจุบันตามปฏิทินสุริยคติสากล (ค.ศ.)
    private static DateOnly TodayGregorian() =>
        // สร้างอ็อบเจกต์ DateOnly ของวันปัจจุบัน
        new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

    // ฟังก์ชัน GenerateLogCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string GenerateLogCode(string prefix)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix)
            // กรณีคำนำหน้าว่างเปล่าให้ใช้คำนำหน้ามาตรฐาน LOG
            ? "LOG"
            // กรณีมีคำนำหน้าให้ตัดช่องว่าง ตัดขีดท้าย และแปลงเป็นตัวพิมพ์ใหญ่
            : prefix.Trim().TrimEnd('-').ToUpperInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (cleanPrefix.Length > 11) cleanPrefix = cleanPrefix[..11];
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string code = $"{cleanPrefix}-{DateTime.Now:yyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return code.Length <= 30 ? code : code[..30];
    }

    // ฟังก์ชัน IsValidPhoneNumber สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static bool IsValidPhoneNumber(string? phone)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string digits = NormalizePhoneDigits(phone);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return digits.Length >= 9 && digits.Length <= 15;
    }

    // ฟังก์ชัน NormalizeGregorianDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static DateOnly NormalizeGregorianDate(DateOnly date)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int year = date.Year;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (year > 0 && year < 1900)
        {
            // ปรับปี พ.ศ. ให้กลับเป็นปี ค.ศ.
            year += 543;
        }
        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
        else if (year >= 2400)
        {
            // หักลบปี พ.ศ. 543 ปีเพื่อให้ได้ปี ค.ศ.
            year -= 543;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new DateOnly(year, date.Month, day);
    }

    // ฟังก์ชัน FormatThaiDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatThaiDate(DateOnly date, string format = "d MMM yyyy")
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var gregorian = NormalizeGregorianDate(date);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return gregorian.ToDateTime(TimeOnly.MinValue).ToString(format, ThaiCulture);
    }

    // ฟังก์ชันแปลงวันที่ให้อยู่ในรูปแบบสากล ISO yyyy-MM-dd
    private static string FormatIsoDate(DateOnly date) =>
        // ปรับปี ค.ศ. และแปลงเป็นข้อความวันที่รูปแบบ ISO
        NormalizeGregorianDate(date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ฟังก์ชัน FormatPromoBenefitSummary สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatPromoBenefitSummary(Promotion promo)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal val = promo.DiscountValue ?? 0m;
        // ตรวจสอบประเภทโปรโมชั่นและจัดรูปแบบข้อความสรุปสิทธิประโยชน์
        return (promo.PromoType ?? "percent").Trim().ToLowerInvariant() switch
        {
            // กรณีส่วนลดคิดเป็นเปอร์เซ็นต์
            "percent" or "percentage" => $"ลด {val:0.#}%",
            // กรณีโปรโมชั่นซื้อหนึ่งแถมหนึ่ง
            "bogo" => val > 0 ? $"ซื้อ 1 แถม 1 (สูงสุด ฿{val:N0})" : "ซื้อ 1 แถม 1",
            // กรณีโปรโมชั่นรับแต้มสะสมสองเท่า
            "double_points" => "รับแต้มสะสม x2",
            // กรณีโปรโมชั่นชุดเซ็ตสุดคุ้ม
            "bundle" => $"ชุดเซ็ตสุดคุ้ม ลด ฿{val:N0}",
            // กรณีโปรโมชั่นส่วนลดรูปแบบอื่นๆ
            _ => $"ลด ฿{val:N0}"
        };
    }

    // ฟังก์ชัน SaveUploadedImage สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string? SaveUploadedImage(IFormFile? imageFile, string webRootPath, string defaultPrefix, string? fallbackUrl)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (imageFile == null || imageFile.Length <= 0 || string.IsNullOrWhiteSpace(webRootPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return fallbackUrl;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
        {
            // กำหนดนามสกุลไฟล์เริ่มต้นเป็น png
            ext = ".png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var imagesDir = Path.Combine(webRootPath, "media", "images");
        // สร้างโฟลเดอร์สำหรับจัดเก็บไฟล์หากยังไม่มี
        Directory.CreateDirectory(imagesDir);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
            // ตัดช่องว่างหน้าหลังชื่อไฟล์
            .Trim()
            // แปลงชื่อไฟล์เป็นตัวพิมพ์เล็ก
            .ToLowerInvariant()
            // แทนที่ช่องว่างด้วยเครื่องหมายขีดล่าง
            .Replace(" ", "_");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var safePrefix = string.IsNullOrWhiteSpace(rawName) ? defaultPrefix : rawName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fullPath = Path.Combine(imagesDir, fileName);

        // เปิดสตรีมไฟล์เพื่อบันทึกข้อมูลรูปภาพลงในดิสก์
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            // คัดลอกข้อมูลจากฟอร์มอัปโหลดลงสู่ไฟล์ปลายทาง
            imageFile.CopyTo(stream);
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "/media/images/" + fileName;
    }

    // ฟังก์ชันจัดรูปแบบรหัสโปรโมชั่นให้เป็นตัวพิมพ์ใหญ่และตัดช่องว่าง
    private static string NormalizeCode(string? code) =>
        // แปลงข้อความเป็นตัวพิมพ์ใหญ่และตัดช่องว่าง
        (code ?? string.Empty).Trim().ToUpperInvariant();

    // ฟังก์ชันลบเครื่องหมายขีด ช่องว่าง และชาร์ปออกจากรหัสโค้ด
    private static string NormalizeCodeNoHyphen(string? code) =>
        // ลบเครื่องหมายขีด ชาร์ป และช่องว่างออกจากรหัส
        NormalizeCode(code).Replace("-", "").Replace("#", "").Replace(" ", "");

    // ฟังก์ชันคืนค่าข้อความภาษาไทยสำหรับประเภทโปรโมชั่น
    private static string GetPromoTypeThaiLabel(string? promoType) =>
        // แปลงประเภทโปรโมชั่นเป็นป้ายชื่อภาษาไทย
        (promoType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ประเภทส่วนลดเปอร์เซ็นต์
            "percent" or "percentage" => "ส่วนลดเปอร์เซ็นต์",
            // ประเภทส่วนลดมูลค่าคงที่
            "fixed" => "ส่วนลดมูลค่าคงที่",
            // ประเภทซื้อ 1 แถม 1
            "bogo" => "ซื้อ 1 แถม 1",
            // ประเภทชุดเซ็ตสุดคุ้ม
            "bundle" => "ชุดเซ็ตสุดคุ้ม",
            // ประเภทรับแต้มสะสมสองเท่า
            "double_points" => "แต้มสะสมสองเท่า",
            // ประเภทโปรโมชั่นพิเศษอื่นๆ
            _ => "โปรโมชั่นพิเศษ"
        };

    // ฟังก์ชันคืนค่าข้อความภาษาไทยสำหรับขอบเขตสินค้าที่ร่วมรายการโปรโมชั่น
    private static string GetScopeThaiLabel(string? scope) =>
        // แปลงขอบเขตสินค้าเป็นป้ายชื่อภาษาไทย
        (scope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ใช้ได้กับสินค้าทุกหมวดหมู่
            "all" or "" => "สินค้าทุกหมวดหมู่ในร้าน",
            // ใช้ได้เฉพาะสินค้าหมวดฟิกเกอร์
            "figures" => "สินค้าหมวดฟิกเกอร์",
            // ใช้ได้เฉพาะสินค้าลดล้างสต๊อกหรือพร้อมส่ง
            "clearance" => "สินค้าลดล้างสต๊อก / พร้อมส่ง",
            // ขอบเขตอื่นๆ ให้แสดงตามชื่อเดิม
            _ => scope!
        };

    // ฟังก์ชัน MatchesScope สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static bool MatchesScope(Product? product, string? scope)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (product == null) return false;

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalizedScope = (scope ?? "all").Trim().ToLowerInvariant();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "all")
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return true;
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (normalizedScope == "figures")
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string catName = product.Category?.CategoryName ?? string.Empty;
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string slug = (product.Category?.Slug ?? string.Empty).ToLowerInvariant();
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return catName.Contains("ฟิกเกอร์")
                // ตรวจสอบว่า slug มีคำว่า figure หรือไม่
                || slug.Contains("figure")
                // ตรวจสอบว่าตรงกับหมวดของสะสมจำนวนจำกัดหรือไม่
                || slug == "limited-collectibles";
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (normalizedScope == "clearance")
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int available = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return !product.IsPreorder && available > 0;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return true;
    }

    // ฟังก์ชันคำนวณและประเมินผลสิทธิประโยชน์ของโปรโมชั่นสำหรับรายการสินค้าในตะกร้า
    private static PromoEvaluationResult EvaluateCartPromotion(
        Promotion? promo,
        IReadOnlyCollection<Cartitem> cartItems,
        DateOnly? todayOverride = null)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var today = todayOverride ?? TodayGregorian();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal subtotal = cartItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (promo == null)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากไม่พบรหัส
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = "ไม่พบรหัสโปรโมชั่นนี้ในระบบ กรุณาตรวจสอบตัวสะกดอีกครั้ง"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.IsActive != true)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากปิดการใช้งาน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ปิดการใช้งานชั่วคราวแล้ว"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var normalizedStart = NormalizeGregorianDate(promo.StartDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var normalizedEnd = NormalizeGregorianDate(promo.EndDate);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today < normalizedStart)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากยังไม่ถึงวันเริ่มใช้
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"รหัส \"{promo.VoucherCode}\" จะเริ่มใช้งานได้ตั้งแต่วันที่ {FormatThaiDate(normalizedStart)}"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today > normalizedEnd)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากหมดอายุ
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากใช้สิทธิ์เต็มแล้ว
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ถูกใช้ครบจำนวนสิทธิ์แล้ว"
            };
        }

        // กรองรายการสินค้าในตะกร้าที่ตรงตามขอบเขตของโปรโมชั่น
        var eligibleItems = cartItems
            // ตรวจสอบเงื่อนไขขอบเขตสินค้า
            .Where(c => MatchesScope(c.Product, promo.ApplicableScope))
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (eligibleItems.Count == 0)
        {
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากไม่มีสินค้าที่ร่วมรายการ
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"โค้ด \"{promo.VoucherCode}\" ใช้ได้เฉพาะ{GetScopeThaiLabel(promo.ApplicableScope)} แต่ไม่มีสินค้าที่ร่วมรายการในตะกร้า"
            };
        }

        // คำนวณยอดรวมสินค้าที่ร่วมรายการโปรโมชั่น
        decimal eligibleSubtotal = eligibleItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);
        // คำนวณจำนวนชิ้นสินค้ารวมที่ร่วมรายการโปรโมชั่น
        int eligibleQty = eligibleItems.Sum(c => c.Quantity);
        // กำหนดยอดใช้จ่ายขั้นต่ำที่ต้องถึงเกณฑ์
        decimal minSpend = promo.MinimumSpend ?? 0m;

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (eligibleSubtotal < minSpend)
        {
            // คำนวณยอดเงินที่ยังขาดอยู่เพื่อให้ถึงยอดขั้นต่ำ
            decimal diff = minSpend - eligibleSubtotal;
            // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากยอดไม่ถึงขั้นต่ำ
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องของโปรโมชั่น
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดยอดรวมสินค้าที่ร่วมรายการ
                EligibleSubtotal = eligibleSubtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = $"ยอดรวมในตะกร้ายังไม่ถึงขั้นต่ำ ฿{minSpend:N0} สำหรับโค้ด \"{promo.VoucherCode}\" (ขาดอีก ฿{diff:N0})"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string promoType = (promo.PromoType ?? "percent").Trim().ToLowerInvariant();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal discountValue = promo.DiscountValue ?? 0m;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal discount = 0m;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool isDoublePoints = false;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string summary;

        // แยกการทำงานตามค่าตัวแปรที่ระบุ
        switch (promoType)
        {
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "percent":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "percentage":
                // คำนวณมูลค่าส่วนลดตามเปอร์เซ็นต์
                discount = Math.Round(eligibleSubtotal * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
                // จำกัดมูลค่าส่วนลดไม่ให้เกินยอดรวมสินค้าที่ร่วมรายการ
                discount = Math.Clamp(discount, 0m, eligibleSubtotal);
                // จัดรูปแบบข้อความสรุปส่วนลดเปอร์เซ็นต์
                summary = $"ลด {discountValue:0.#}% (ประหยัด ฿{discount:N0})";
                // จบการทำงานของเงื่อนไขนี้
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bogo":
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (eligibleQty < 2)
                {
                    // ส่งคืนผลลัพธ์การประเมินโปรโมชั่นที่ไม่ถูกต้องเนื่องจากสินค้าไม่ครบ 2 ชิ้น
                    return new PromoEvaluationResult
                    {
                        // กำหนดสถานะความถูกต้องของโปรโมชั่น
                        IsValid = false,
                        // กำหนดข้อมูลโปรโมชั่น
                        Promotion = promo,
                        // กำหนดยอดรวมสินค้าในตะกร้า
                        SubtotalAmount = subtotal,
                        // กำหนดยอดรวมสินค้าที่ร่วมรายการ
                        EligibleSubtotal = eligibleSubtotal,
                        // กำหนดข้อความแจ้งข้อผิดพลาด
                        ErrorMessage = $"โค้ดซื้อ 1 แถม 1 ({promo.VoucherCode}) ต้องมีสินค้าที่ร่วมรายการอย่างน้อย 2 ชิ้นในตะกร้า"
                    };
                }
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal cheapestUnit = eligibleItems.Min(c => c.Product?.RetailPrice ?? 0m);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal bogoCap = discountValue > 0m ? discountValue : cheapestUnit;
                // คำนวณและจำกัดมูลค่าส่วนลดฟรีสินค้าชิ้นที่ถูกที่สุด
                discount = Math.Clamp(bogoCap, 0m, eligibleSubtotal);
                // จัดรูปแบบข้อความสรุปโปรโมชั่นซื้อหนึ่งแถมหนึ่ง
                summary = $"สิทธิพิเศษซื้อ 1 แถม 1 (ส่วนลด ฿{discount:N0})";
                // จบการทำงานของเงื่อนไขนี้
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "double_points":
                // กำหนดแฟล็กแต้มสะสมสองเท่าเป็นจริง
                isDoublePoints = true;
                // กำหนดข้อความสรุปสิทธิพิเศษแต้มสองเท่า
                summary = "รับแต้มสะสม x2 เมื่อชำระเงินที่หน้าร้าน";
                // จบการทำงานของเงื่อนไขนี้
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "fixed":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bundle":
            // กรณีประเภทโปรโมชั่นอื่นๆ
            default:
                // จำกัดมูลค่าส่วนลดตามจำนวนเงินที่กำหนด
                discount = Math.Clamp(discountValue, 0m, eligibleSubtotal);
                // จัดรูปแบบข้อความสรุปส่วนลดเงินสดท้ายบิล
                summary = $"ส่วนลดท้ายบิล ฿{discount:N0}";
                // จบการทำงานของเงื่อนไขนี้
                break;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new PromoEvaluationResult
        {
            // กำหนดสถานะความถูกต้องของโปรโมชั่น
            IsValid = true,
            // กำหนดข้อมูลโปรโมชั่น
            Promotion = promo,
            // กำหนดยอดรวมสินค้าในตะกร้า
            SubtotalAmount = subtotal,
            // กำหนดยอดรวมสินค้าที่ร่วมรายการ
            EligibleSubtotal = eligibleSubtotal,
            // กำหนดมูลค่าส่วนลดที่ได้รับ
            DiscountAmount = discount,
            // กำหนดแฟล็กแต้มสองเท่า
            IsDoublePoints = isDoublePoints,
            // กำหนดข้อความสรุปสิทธิประโยชน์
            BenefitSummary = summary
        };
    }

    // ฟังก์ชัน ReadCollectedCodes สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static List<string> ReadCollectedCodes(string? rawSessionValue)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(rawSessionValue))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }

        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // แปลงข้อมูล JSON จากเซสชันกลับเป็นรายการรหัสโค้ด
            return JsonSerializer.Deserialize<List<string>>(rawSessionValue)?
                // กรองเฉพาะข้อความที่ไม่เป็นค่าว่าง
                .Where(s => !string.IsNullOrWhiteSpace(s))
                // จัดรูปแบบรหัสโค้ดให้เป็นมาตรฐาน
                .Select(NormalizeCode)
                // ตัดรหัสโค้ดที่ซ้ำกันออก
                .Distinct(StringComparer.OrdinalIgnoreCase)
                // แปลงเป็นรายการ List หรือคืนค่า List ว่างหากผลลัพธ์เป็น null
                .ToList() ?? new List<string>();
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }
    }

    // ฟังก์ชันแปลงรายการรหัสโปรโมชั่นที่เก็บสะสมเป็นข้อความ JSON สำหรับบันทึกลงเซสชัน
    private static string SerializeCollectedCodes(IEnumerable<string> codes) =>
        // แปลงคอลเลกชันเป็น JSON สตริง
        JsonSerializer.Serialize(
            // กรองเฉพาะรหัสที่ไม่เป็นค่าว่าง
            codes.Where(s => !string.IsNullOrWhiteSpace(s))
                 // จัดรูปแบบรหัสโค้ดให้เป็นมาตรฐาน
                 .Select(NormalizeCode)
                 // ตัดรหัสโค้ดที่ซ้ำกันออก
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 // แปลงเป็น List
                 .ToList());

    // ฟังก์ชัน BuildRedemptionMetaMap สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static Dictionary<string, RedemptionMetaPayload> BuildRedemptionMetaMap(IEnumerable<Systemlog> logs)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var map = new Dictionary<string, RedemptionMetaPayload>(StringComparer.OrdinalIgnoreCase);
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
            try
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var meta = JsonSerializer.Deserialize<RedemptionMetaPayload>(log.DetailPayload);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (meta != null && !string.IsNullOrWhiteSpace(meta.RedemptionCode) && !map.ContainsKey(meta.RedemptionCode))
                {
                    // กำหนดค่าข้อมูล meta ลงใน Dictionary
                    map[meta.RedemptionCode] = meta;
                }
            }
            // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return map;
    }

    // ฟังก์ชัน BuildPreorderPromoMap สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static Dictionary<string, PreorderPromoPayload> BuildPreorderPromoMap(IEnumerable<Systemlog> logs)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var map = new Dictionary<string, PreorderPromoPayload>(StringComparer.OrdinalIgnoreCase);
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
            try
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var meta = JsonSerializer.Deserialize<PreorderPromoPayload>(log.DetailPayload);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (meta != null && !string.IsNullOrWhiteSpace(meta.ReservationNumber) && !map.ContainsKey(meta.ReservationNumber))
                {
                    // กำหนดค่าข้อมูล meta ลงใน Dictionary
                    map[meta.ReservationNumber] = meta;
                }
            }
            // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return map;
    }

    // ฟังก์ชัน FormatPickupDateThai สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatPickupDateThai(string? isoDate)
    {
        // ตรวจสอบความถูกต้องและแปลงรูปแบบวันที่รับสินค้า ISO
        if (!string.IsNullOrWhiteSpace(isoDate) &&
            // ตรวจสอบและแปลงข้อความวันที่ ISO
            DateOnly.TryParseExact(isoDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return parsed.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "-";
    }

    // ฟังก์ชันกรองเฉพาะตัวเลขจากเบอร์โทรศัพท์และตัดช่องว่าง
    private static string NormalizePhoneDigits(string? phone) =>
        // ลบขีด ช่องว่าง และตัดช่องว่างหน้าหลัง
        (phone ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();

    // ตัวแปรเก็บเส้นทางไดเรกทอรี wwwroot ในระบบ
    private static string? _webRootPath;

    // ฟังก์ชัน ConfigureWebRoot สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static void ConfigureWebRoot(string? webRootPath)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            // กำหนดค่าพาธไดเรกทอรี wwwroot ให้กับตัวแปรคงที่
            _webRootPath = webRootPath;
        }
    }

    // ฟังก์ชัน ResolveCardImageUrl สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string ResolveCardImageUrl(string? imageUrl)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return "~/media/images/naruto_hero_fig.png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string trimmed = imageUrl.Trim();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalized = trimmed.StartsWith("~/") ? trimmed[1..] : trimmed;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!normalized.StartsWith("/media/images/", StringComparison.OrdinalIgnoreCase) || normalized.Contains(".."))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ดึงชื่อไฟล์รูปภาพโดยไม่รวมนามสกุล
        string? fileNameNoExt = Path.GetFileNameWithoutExtension(normalized);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(fileNameNoExt))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ดึงนามสกุลไฟล์เดิม
        string ext = Path.GetExtension(normalized).ToLowerInvariant();
        // ตรวจสอบว่ามีนามสกุลไฟล์ที่ถูกต้องหรือไม่
        if (string.IsNullOrEmpty(ext))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // กำหนดไดเรกทอรีราก wwwroot
        string rootDir = !string.IsNullOrWhiteSpace(_webRootPath)
            // ใช้พาธ wwwroot ที่กำหนดค่าไว้
            ? _webRootPath
            // หรือใช้โฟลเดอร์ wwwroot จากไดเรกทอรีการทำงานปัจจุบัน
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string directPath = Path.Combine(rootDir, normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        // ตรวจสอบว่ามีไฟล์รูปภาพตามพาธตรงหรือไม่
        if (System.IO.File.Exists(directPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string imagesDir = Path.Combine(rootDir, "media", "images");
        // ตรวจสอบว่าไดเรกทอรี images มีอยู่จริงหรือไม่
        if (!Directory.Exists(imagesDir))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ลองค้นหาไฟล์รูปภาพนามสกุลอื่นที่ชื่อเดียวกันในโฟลเดอร์
        string[] candidateExts = ext == ".png"
            // หากเดิมเป็น png ให้ลองค้นหา webp, jpg, jpeg
            ? new[] { ".webp", ".jpg", ".jpeg" }
            // หากเดิมเป็นนามสกุลอื่น ให้ลองค้นหา png, webp, jpg, jpeg
            : new[] { ".png", ".webp", ".jpg", ".jpeg" };

        // วนลูปตรวจสอบนามสกุลไฟล์ที่เป็นไปได้
        foreach (var cExt in candidateExts)
        {
            // สร้างชื่อไฟล์พร้อมนามสกุลทางเลือก
            string altFileName = fileNameNoExt + cExt;
            // สร้างเส้นทางเต็มของไฟล์ทางเลือก
            string altPath = Path.Combine(imagesDir, altFileName);
            // ตรวจสอบว่ามีไฟล์ทางเลือกอยู่จริงหรือไม่
            if (System.IO.File.Exists(altPath))
            {
                // ส่งคืนพาธรูปภาพทางเลือกที่พบ
                return (trimmed.StartsWith("~/") ? "~" : "") + "/media/images/" + altFileName;
            }
        }

        // ส่งคืนพาธรูปภาพเดิมหากไม่พบไฟล์ทางเลือก
        return trimmed;
    }

    // ฟังก์ชัน EscapeCsvField สำหรับแปลงค่าฟิลด์ให้อยู่ในรูปแบบ CSV ที่ปลอดภัย
    private static string EscapeCsvField(string? value)
    {
        // ตรวจสอบว่าข้อมูลเป็นค่าว่างหรือไม่
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // ลบเครื่องหมายช่องว่างส่วนเกิน
        string s = value.Trim();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return s;
    }

    // ฟังก์ชันสร้างรายการหมายเลขหน้าแบบย่อพร้อมจุดไข่ปลา (-1 แทน ...) เช่น 1 2 (3) ... 27 -> 2 3 (4) ... 27
    private static List<int> GetCompactPagination(int currentPage, int totalPages)
    {
        // สร้างรายการเก็บหมายเลขหน้าที่จะแสดงผล
        var pages = new List<int>();
        // กรณีจำนวนหน้าทั้งหมดไม่เกิน 5 หน้า ให้แสดงทุกหน้าตามลำดับ
        if (totalPages <= 5)
        {
            // วนลูปเพิ่มเลขหน้าตั้งแต่ 1 ถึงหน้าสุดท้าย
            for (int i = 1; i <= Math.Max(1, totalPages); i++)
            {
                // เพิ่มเลขหน้าเข้าสู่รายการ
                pages.Add(i);
            }
            // ส่งคืนรายการเลขหน้าทั้งหมด
            return pages;
        }

        // กรณีอยู่ช่วง 3 หน้าแรก แสดง 1 2 3 ... หน้าสุดท้าย
        if (currentPage <= 3)
        {
            // เพิ่มหน้า 1
            pages.Add(1);
            // เพิ่มหน้า 2
            pages.Add(2);
            // เพิ่มหน้า 3
            pages.Add(3);
            // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
            pages.Add(-1);
            // เพิ่มหน้าสุดท้าย
            pages.Add(totalPages);
            // ส่งคืนรายการเลขหน้า
            return pages;
        }

        // กรณีอยู่ช่วง 2 หน้าสุดท้าย แสดง 1 ... (N-2) (N-1) N
        if (currentPage >= totalPages - 1)
        {
            // เพิ่มหน้าแรกสุด
            pages.Add(1);
            // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
            pages.Add(-1);
            // เพิ่มหน้าก่อนรองสุดท้าย
            pages.Add(totalPages - 2);
            // เพิ่มหน้ารองสุดท้าย
            pages.Add(totalPages - 1);
            // เพิ่มหน้าสุดท้าย
            pages.Add(totalPages);
            // ส่งคืนรายการเลขหน้า
            return pages;
        }

        // กรณีอยู่หน้ากลาง แสดง (p-2) (p-1) (p) ... หน้าสุดท้าย ตามรูปแบบ 1 2 (3) ... N -> 2 3 (4) ... N
        pages.Add(currentPage - 2);
        // เพิ่มหน้าก่อนหน้า 1 ลำดับ
        pages.Add(currentPage - 1);
        // เพิ่มหน้าปัจจุบัน
        pages.Add(currentPage);
        // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
        pages.Add(-1);
        // เพิ่มหน้าสุดท้าย
        pages.Add(totalPages);
        // ส่งคืนรายการเลขหน้าแบบย่อ
        return pages;
    }

    // ฟังก์ชันสกัดข้อมูลสถานะและเหตุผลการไม่รับสินค้า/ไม่มารับสินค้า/ยกเลิกคำสั่งจองจากตาราง Systemlogs
    private static (string BadgeTitle, string StepTitle, string NoticeText, string ReasonDetail) ResolveOrderCancelMeta(
        // ข้อมูลบันทึกระบบ
        IEnumerable<Systemlog>? logs,
        // รหัสอ้างอิงคำสั่งจองหรือการแลกของรางวัล
        string? referenceCode,
        // ตัวระบุว่าเป็นการแลกของรางวัลหรือไม่
        bool isReward = false)
    {
        // กำหนดข้อความเริ่มต้นกรณีลูกค้ายกเลิกทั่วไป
        string defaultTitle = isReward ? "ยกเลิกการแลกของรางวัล" : "ยกเลิกคำสั่งจอง";
        // กำหนดข้อความคำอธิบายเริ่มต้น
        string defaultNotice = isReward
            // ข้อความสำหรับกรณีของรางวัล
            ? "รายการแลกของรางวัลนี้ถูกยกเลิกแล้ว ระบบได้คืนแต้มสะสมและคืนสต็อกของรางวัลเรียบร้อยแล้ว"
            // ข้อความสำหรับกรณีคำสั่งจองสินค้า
            : "รายการนี้ถูกยกเลิกตามคำขอของลูกค้า และระบบได้คืนสิทธิ์สต็อกสินค้าเรียบร้อยแล้ว";

        // ตรวจสอบว่ามีรายการบันทึกและรหัสอ้างอิงหรือไม่
        if (logs == null || string.IsNullOrWhiteSpace(referenceCode))
        {
            // ส่งคืนค่าเริ่มต้นเมื่อไม่มีข้อมูลบันทึก
            return (defaultTitle, defaultTitle, defaultNotice, "ลูกค้าแจ้งยกเลิกรายการ");
        }

        // ตัดช่องว่างรหัสอ้างอิงเพื่อใช้ค้นหาในข้อความบันทึก
        string cleanCode = referenceCode.Trim();
        // ค้นหาบันทึกล่าสุดที่เกี่ยวข้องกับรหัสใบจองหรือรหัสแลกของรางวัลนี้
        var matchedLog = logs
            // กรองบันทึกที่มีข้อความและมีรหัสอ้างอิงตรงกัน
            .Where(l => !string.IsNullOrEmpty(l.Message) &&
                        // ตรวจสอบว่าข้อความมีรหัสอ้างอิงตรงกันหรือไม่
                        l.Message.Contains(cleanCode, StringComparison.OrdinalIgnoreCase) &&
                        // ตรวจสอบโมดูลหรือรหัสประเภทการยกเลิกที่เกี่ยวข้อง
                        (l.Module == "CounterPreorder" || l.Module == "preorder.cancel" || l.Module == "reward.cancel" ||
                         l.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
                         l.LogCode.StartsWith("NOSHOW", StringComparison.OrdinalIgnoreCase) ||
                         l.LogCode.StartsWith("PO-CAN", StringComparison.OrdinalIgnoreCase) ||
                         l.LogCode.StartsWith("RWD-CAN", StringComparison.OrdinalIgnoreCase)))
            // จัดเรียงตามวันเวลาจากใหม่ไปเก่า
            .OrderByDescending(l => l.Timestamp)
            // เลือกบันทึกรายการแรกสุดที่ตรงเงื่อนไข
            .FirstOrDefault();

        // หากไม่พบบันทึกเฉพาะเจาะจง ให้ส่งคืนค่าเริ่มต้น
        if (matchedLog == null)
        {
            // ส่งคืนค่าเริ่มต้น
            return (defaultTitle, defaultTitle, defaultNotice, "ลูกค้าแจ้งยกเลิกรายการ");
        }

        // อ่านข้อความเต็มจากบันทึกกิจกรรม
        string msg = matchedLog.Message ?? string.Empty;
        // สกัดส่วนข้อความหลังคำว่า "สาเหตุ:" หากมีระบุไว้
        string reasonPart = string.Empty;
        // ค้นหาตำแหน่งคำว่า "สาเหตุ:" ในข้อความ
        int reasonIdx = msg.IndexOf("สาเหตุ:", StringComparison.OrdinalIgnoreCase);
        // หากพบคำว่า "สาเหตุ:" ให้ตัดข้อความตั้งแต่ตำแหน่งนั้นมาจัดรูปแบบ
        if (reasonIdx >= 0)
        {
            // ตัดข้อความส่วนสาเหตุและรายละเอียด
            reasonPart = msg[reasonIdx..].Trim();
            // ตัดข้อความส่วนท้ายเรื่องการคืนแต้มออกจากข้อความสาเหตุเพื่อไม่ให้ซ้ำซ้อน
            int refundIdx = reasonPart.IndexOf("| คืน", StringComparison.OrdinalIgnoreCase);
            // หากพบส่วนข้อความคืนแต้มต่อท้าย ให้ตัดเฉพาะส่วนสาเหตุข้างหน้า
            if (refundIdx > 0)
            {
                // เก็บเฉพาะข้อความสาเหตุและหมายเหตุ
                reasonPart = reasonPart[..refundIdx].Trim();
            }
            // ลบข้อความหมายเหตุหรือรายละเอียดที่เป็นคำว่า "ไม่มี" ออกเพื่อให้ข้อความกระชับสวยงาม
            reasonPart = reasonPart
                // ลบข้อความรายละเอียดไม่มี
                .Replace("| รายละเอียด: ไม่มี", "")
                // ลบข้อความหมายเหตุไม่มี
                .Replace("| หมายเหตุ: ไม่มี", "")
                // ตัดช่องว่างหน้าหลัง
                .Trim();
        }

        // ตรวจสอบกรณีพนักงานกด "ไม่รับสินค้า" (REJECT)
        if (matchedLog.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
            // หรือข้อความระบุว่าลูกค้าไม่รับ
            msg.Contains("ลูกค้าไม่รับ", StringComparison.OrdinalIgnoreCase))
        {
            // กำหนดป้ายสถานะเป็นลูกค้าไม่รับสินค้า
            string title = isReward ? "ลูกค้าไม่รับของรางวัล" : "ลูกค้าไม่รับสินค้า";
            // กำหนดรายละเอียดสาเหตุ
            string detail = !string.IsNullOrWhiteSpace(reasonPart) ? reasonPart : "สาเหตุ: ลูกค้าปฏิเสธไม่รับสินค้าที่เคาน์เตอร์หน้าร้าน";
            // ประกอบข้อความแจ้งเตือนฉบับเต็ม
            string notice = $"รายการนี้{title}ที่หน้าร้าน — {detail} (คืนสต็อกเรียบร้อยแล้ว)";
            // ส่งคืนข้อมูลกรณีลูกค้าไม่รับสินค้า
            return (title, title, notice, detail);
        }

        // ตรวจสอบกรณีพนักงานกด "ไม่มารับสินค้า" (NOSHOW)
        if (matchedLog.LogCode.StartsWith("NOSHOW", StringComparison.OrdinalIgnoreCase) ||
            // หรือข้อความระบุว่าลูกค้าไม่มารับ
            msg.Contains("ลูกค้าไม่มารับ", StringComparison.OrdinalIgnoreCase))
        {
            // กำหนดป้ายสถานะเป็นลูกค้าไม่มารับสินค้า
            string title = isReward ? "ลูกค้าไม่มารับของรางวัล" : "ลูกค้าไม่มารับสินค้า";
            // กำหนดรายละเอียดสาเหตุ
            string detail = !string.IsNullOrWhiteSpace(reasonPart) ? reasonPart : "สาเหตุ: ลูกค้าไม่มารับตามกำหนดนัดหมาย";
            // ประกอบข้อความแจ้งเตือนฉบับเต็ม
            string notice = $"รายการนี้ถูกยกเลิกเนื่องจาก{title}ตามกำหนด — {detail} (คืนสต็อกเรียบร้อยแล้ว)";
            // ส่งคืนข้อมูลกรณีลูกค้าไม่มารับสินค้า
            return (title, title, notice, detail);
        }

        // กรณีลูกค้าเป็นผู้กดยกเลิกคำสั่งจองหรือการแลกของรางวัลด้วยตนเอง
        string cancelDetail = !string.IsNullOrWhiteSpace(reasonPart)
            // ใช้รายละเอียดสาเหตุที่มี
            ? reasonPart
            // หรือกำหนดสาเหตุเริ่มต้นว่าลูกค้ากดยกเลิกด้วยตนเอง
            : "สาเหตุ: ลูกค้ากดยกเลิกคำสั่งจองด้วยตนเอง";
        // ประกอบข้อความแจ้งเตือนสำหรับกรณีลูกค้ายกเลิกเอง
        string cancelNotice = $"รายการนี้ถูกยกเลิกตามคำขอของลูกค้า — {cancelDetail} (คืนสิทธิ์สต็อกเรียบร้อยแล้ว)";
        // ส่งคืนข้อมูลกรณีลูกค้ายกเลิกเอง
        return (defaultTitle, defaultTitle, cancelNotice, cancelDetail);
    }

    // ฟังก์ชันคืนค่า Action และ Controller หน้าหลักประจำบทบาทผู้ใช้งาน (สำหรับระบบ 1 เบราว์เซอร์ 1 บทบาท)
    private static (string ActionName, string ControllerName) GetRoleHomeRoute(string? role)
    {
        // แปลงชื่อบทบาทเป็นตัวพิมพ์เล็ก
        string normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
        // จับคู่บทบาทกับหน้าหลักของบทบาทนั้น
        return normalized switch
        {
            // บทบาทพนักงานหน้าร้าน
            "staff" => ("Index", "Staff"),
            // บทบาทเจ้าของร้าน
            "owner" => ("Promotions", "Owner"),
            // บทบาทฝ่ายบัญชี
            "accountant" => ("Index", "Accountant"),
            // บทบาทฝ่ายสนับสนุนด้านไอที
            "itsupport" => ("Dashboard", "ITSupport"),
            // กรณีลูกค้าทั่วไปหรือค่าเริ่มต้น
            _ => ("Index", "Home")
        };
    }

}
