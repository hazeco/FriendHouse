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

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    public HomeController (Csi402dbContext db)
    {
        _db = db;
    }

    // CREATE: แสดงแบบฟอร์มบันทึกข้อมูลนักศึกษาเข้าสู่ระบบ (GET Method)
    // WHY: สนับสนุนการฝึกปฏิบัติการฐานข้อมูล Lab 9 สำหรับการสร้างข้อมูลนักศึกษาใหม่
    public IActionResult Lab9()
    {
        return View();
    }

    // CREATE: บันทึกข้อมูลนักศึกษาใหม่ลงในฐานข้อมูล MySQL ตาราง Labstudents (POST Method)
    // WHY: ตรวจสอบความถูกต้องของแบบฟอร์ม ตรวจสอบรหัสนักศึกษาซ้ำ และบันทึกข้อมูลลงฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lab9(Labstudent data)
    {
        if (ModelState.IsValid)
        {
            if (data.StdPassword != null)
            {
                // ขั้นตอนที่ 1: ตรวจสอบว่ามีรหัสนักศึกษานี้อยู่ในฐานข้อมูลแล้วหรือไม่
                var CheckID = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                if (CheckID == null)
                {
                    // ขั้นตอนที่ 2: สร้างออบเจกต์นักศึกษาใหม่และบันทึกข้อมูลลงตาราง Labstudents
                    Labstudent s = new Labstudent();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return RedirectToAction("Lab9_1", "Home", new { status = "บันทึกข้อมูลสำเร็จ" });
                }
                else
                {
                    ViewBag.Status = "รหัสนักศึกษานี้มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
                    return View();
                }
            }
            else
            {
                ViewBag.Status = "กรุณากรอกรหัสผ่าน";
                return View();
            }
        }
        else
        {
            return View();
        }
    }

    // READ: แสดงรายชื่อนักศึกษาทั้งหมดจากตาราง Labstudents (GET Method)
    // WHY: ดึงข้อมูลนักศึกษาทั้งหมดจากฐานข้อมูลมาแสดงผลในตารางสำหรับการฝึกปฏิบัติการ Lab 9.1
    public IActionResult Lab9_1()
    {
        var data = _db.Labstudents.ToList();
        return View(data);
    }

    // UPDATE: แสดงแบบฟอร์มแก้ไขข้อมูลนักศึกษาตามรหัส (GET Method)
    // WHY: ค้นหาข้อมูลนักศึกษาจากรหัสผ่าน LINQ เพื่อนำข้อมูลเดิมมาแสดงในแบบฟอร์มแก้ไข
    public IActionResult Lab10(string? StdId)
    {
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        return View(User);
    }

    // UPDATE: ปรับปรุงข้อมูลนักศึกษาในตาราง Labstudents (POST Method)
    // WHY: บันทึกการแก้ไขรหัสผ่าน ชื่อ และนามสกุลลงฐานข้อมูล MySQL
    [HttpPost]
    public IActionResult Lab10(Labstudent data)
    {
        var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
        if (User != null)
        {
            User.StdPassword = data.StdPassword;
            User.StdName = data.StdName;
            User.StdLastname = data.StdLastname;
            _db.SaveChanges();
            return RedirectToAction("Lab9_1", "Home", new { status = "ปรับปรุงข้อมูลสำเร็จ" });
        }
        return RedirectToAction("Lab9_1", "Home", new { status = "ไม่พบข้อมูลนักศึกษาที่ต้องการแก้ไข" });
    }

    // DELETE: ลบข้อมูลนักศึกษาออกจากตาราง Labstudents (POST Method)
    // WHY: ดำเนินการลบเรคอร์ดนักศึกษาตามรหัสที่ระบุสำหรับการฝึกปฏิบัติการ Lab 10.1
    [HttpPost]
    public IActionResult Lab10_1(string? StdId)
    {
        var Check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        if (Check != null)
        {
            _db.Remove(Check);
            _db.SaveChanges();
            return RedirectToAction("Lab9_1", "Home", new { status = "ลบข้อมูลสำเร็จ" });
        }
        else
        {
            return RedirectToAction("Lab9_1", "Home", new { status = "ไม่พบรหัสนักศึกษานี้ในระบบ" });
        }
    }

    // READ: แสดงรายชื่อผู้ใช้งานทั้งหมดในระบบจากตาราง Users พร้อมระบบค้นหา ตัวกรอง และแบ่งหน้า (GET Method)
    // WHY: รองรับการทำงาน User Full CRUD และการสืบค้นข้อมูลผู้ใช้งาน (เน้นบทบาทลูกค้า) ผ่าน UserManagementViewModel
    public IActionResult UserList(string? searchQuery, string? roleFilter, string? statusFilter, int page = 1)
    {
        // ขั้นตอนที่ 1: สร้าง Query พื้นฐานจากตาราง Users ในฐานข้อมูล MySQL
        var query = _db.Users.AsQueryable();

        // ขั้นตอนที่ 2: กรองตามคำค้นหา (อีเมล เบอร์โทรศัพท์ ชื่อ-นามสกุล หรือรหัสผู้ใช้ รองรับการพิมพ์แบบไม่ใส่ขีด -)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // ตัดช่องว่างหัวท้ายและสร้างคำค้นหาแบบตัดเครื่องหมายขีด (-) และ # ออก
            string term = searchQuery.Trim().ToLower();
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
            if (statusFilter == "ปกติ" || statusFilter.ToLower() == "active")
            {
                // กรองเฉพาะบัญชีที่เปิดใช้งานปกติ
                query = query.Where(u => u.IsActive == true);
            }
            else if (statusFilter == "ถูกล็อก" || statusFilter.ToLower() == "locked")
            {
                // กรองเฉพาะบัญชีที่ถูกระงับการใช้งาน
                query = query.Where(u => u.IsActive == false);
            }
        }

        // ขั้นตอนที่ 5: ดึงรายการผู้ใช้งานเรียงลำดับจากวันที่สมัครล่าสุด และแบ่งหน้า (หน้าละ 10 รายการ)
        var allFilteredUsers = query.OrderByDescending(u => u.CreatedAt).ToList();
        int pageSize = 10;
        int totalCount = allFilteredUsers.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        int currentPage = Math.Clamp(page, 1, totalPages);
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
    public IActionResult CreateCustomerUser(string? fullName, string email, string phone, string password, string? role = "customer", int points = 0)
    {
        // ขั้นตอนที่ 1: ตรวจสอบว่ากรอกข้อมูลบังคับครบถ้วนหรือไม่
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            // แจ้งเตือนกรณีกรอกข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณากรอกอีเมล เบอร์โทรศัพท์ และรหัสผ่านให้ครบถ้วน";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: ตรวจสอบว่ามีอีเมลซ้ำในระบบฐานข้อมูลหรือไม่
        var exists = _db.Users.FirstOrDefault(u => u.Email == email.Trim());
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
        return RedirectToAction("UserList");
    }

    // UPDATE: แก้ไขข้อมูลสมาชิกจากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการอัปเดตชื่อ-นามสกุล อีเมล เบอร์โทรศัพท์ แต้มสะสม บทบาท และสถานะการใช้งานของสมาชิก
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCustomerUser(int userId, string? fullName, string? email, string phone, string? role, int points, string? status, bool? isActive)
    {
        // ขั้นตอนที่ 1: ค้นหาข้อมูลสมาชิกในตาราง Users ตามรหัส UserId
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบข้อมูลสมาชิก
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานที่ต้องการแก้ไข";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: อัปเดตชื่อ-นามสกุล อีเมล เบอร์โทรศัพท์ บทบาท แต้มสะสม และสถานะ
        user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            user.Email = email.Trim();
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // อัปเดตเบอร์โทรศัพท์
            user.Phone = phone.Trim();
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            // อัปเดตบทบาทผู้ใช้งาน
            user.RoleName = role.Trim().ToLower();
        }
        // อัปเดตแต้มสะสมโดยไม่ให้น้อยกว่า 0
        user.Points = Math.Max(0, points);
        if (isActive.HasValue)
        {
            // อัปเดตสถานะการใช้งานบัญชีจากค่าบูลีน
            user.IsActive = isActive.Value;
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            // อัปเดตสถานะการใช้งานบัญชีจากข้อความสถานะ
            user.IsActive = (status == "ปกติ" || status.ToLower() == "active" || status.ToLower() == "true");
        }

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนผลสำเร็จและเปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
        TempData["SuccessMessage"] = $"แก้ไขข้อมูลสมาชิก \"{user.Email}\" เรียบร้อยแล้ว";
        return RedirectToAction("UserList");
    }

    // DELETE: ลบหรือระงับบัญชีสมาชิกจากหน้าจัดการรายชื่อผู้ใช้งานผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการลบข้อมูลสมาชิก (หากไม่มีประวัติธุรกรรมจะลบถาวร หากมีประวัติจะทำ Soft Delete เพื่อรักษา Foreign Key)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCustomerUser(int userId)
    {
        // ขั้นตอนที่ 1: ค้นหาข้อมูลสมาชิกจากตาราง Users
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบข้อมูลสมาชิก
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("UserList");
        }

        // ขั้นตอนที่ 2: ลบรายการในตะกร้าสินค้าของผู้ใช้งานรายนี้ก่อน
        var cartItems = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
        if (cartItems.Any())
        {
            _db.Cartitems.RemoveRange(cartItems);
        }

        // ขั้นตอนที่ 3: ตรวจสอบว่ามีข้อมูลผูกพันในตาราง Preorders, Receipts หรือ Rewardredemptions หรือไม่
        bool hasLinkedRecords = _db.Preorders.Any(p => p.UserId == user.UserId)
            || _db.Receipts.Any(r => r.CustomerId == user.UserId || r.CashierUserId == user.UserId)
            || _db.Rewardredemptions.Any(r => r.UserId == user.UserId)
            || _db.Financialledgers.Any(f => f.RecordedByUserId == user.UserId);

        if (hasLinkedRecords)
        {
            // กรณีมีประวัติธุรกรรมผูกพัน ให้ปรับสถานะเป็นระงับการใช้งาน (Soft Delete) เพื่อป้องกัน Foreign Key Error
            user.IsActive = false;
            _db.SaveChanges();
            TempData["SuccessMessage"] = $"ระงับและปิดการใช้งานบัญชี \"{user.Email}\" เรียบร้อยแล้ว";
        }
        else
        {
            // กรณีไม่มีประวัติธุรกรรมผูกพัน ลบประวัติ Log ที่อ้างอิงและลบเรคอร์ดผู้ใช้ออกจากตารางได้ทันที
            var logs = _db.Systemlogs.Where(l => l.UserId == user.UserId).ToList();
            foreach (var log in logs)
            {
                log.UserId = null;
            }
            string deletedEmail = user.Email;
            _db.Users.Remove(user);
            _db.SaveChanges();
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

        if (data != null)
        {
            ViewBag.Email = data.Email;
            ViewBag.Password = data.Password;
        }

        // ขั้นตอนที่ 2: ค้นหาฟิกเกอร์ที่เป็น Featured Hero หรือมีโมเดล 3D
        var heroProduct = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefault(p => p.IsFeaturedHero)
            ?? _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Model3Dpath))
            ?? _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault();

        if (heroProduct != null)
        {
            viewModel.HeroProduct = new HeroProductViewModel
            {
                ProductId = heroProduct.ProductId,
                Name = heroProduct.Name,
                Description = "งานปั้นรายละเอียดสูง จับจังหวะท่าโจมตีจากฉากไคลแมกซ์ในซีรีส์ หล่อจากเรซิ่นคุณภาพสูง ทาสีมือทุกชิ้น — หมุนดูโมเดล 3 มิติได้รอบตัวก่อนตัดสินใจสั่งจอง",
                PriceFormatted = $"฿{heroProduct.RetailPrice:N0}",
                Model3DPath = !string.IsNullOrEmpty(heroProduct.Model3Dpath) ? heroProduct.Model3Dpath : "~/media/models/naruto_kyubi_sage.glb",
                FallbackImageUrl = "~/media/images/naruto_hero_fallback.png",
                BadgeText = "ฟิกเกอร์คอลเลกชัน · มีสินค้า",
                NoticeText = "มีสินค้าพร้อมส่งมอบ — มารับและชำระเงินที่หน้าร้านได้ทันที",
                Specs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("น้ำหนัก", "1.2 กก."),
                    new KeyValuePair<string, string>("วัสดุ", heroProduct.Material ?? "PVC + เรซิ่น"),
                    new KeyValuePair<string, string>("ความสูง", heroProduct.HeightCm.HasValue ? $"{heroProduct.HeightCm.Value:0.#} ซม." : "32 ซม."),
                    new KeyValuePair<string, string>("สเกล", heroProduct.Scale ?? "1/6")
                }
            };

            ViewBag.HeroModel3D = viewModel.HeroProduct.Model3DPath;
            ViewBag.HeroName = viewModel.HeroProduct.Name;
            ViewBag.HeroPrice = viewModel.HeroProduct.PriceFormatted;
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
            AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)
        }).ToList();

        // ขั้นตอนที่ 5: กำหนดการ์ดโปรโมชั่นเด่นหน้าแรก
        viewModel.Promotions = new List<HomePromotionCardViewModel>
        {
            new HomePromotionCardViewModel
            {
                Tag = "ล้างสต๊อก",
                Title = "ล้างสต๊อกส่งท้ายปี",
                Description = "ลด 20–50% เฉพาะสินค้าค้างสต็อกนาน",
                ExpiryText = "หมดเขตสิ้นเดือนนี้"
            },
            new HomePromotionCardViewModel
            {
                Tag = "สะสมแต้ม",
                Title = "แต้มสะสม x2",
                Description = "สำหรับสมาชิก FriendHouse ทุกยอดจองฟิกเกอร์",
                ExpiryText = "สิทธิพิเศษสมาชิก"
            },
            new HomePromotionCardViewModel
            {
                Tag = "ลูกค้าใหม่",
                Title = "ต้อนรับสมาชิกใหม่",
                Description = "รับแต้มสะสมพิเศษทันทีเมื่อลงทะเบียนสำเร็จ",
                ExpiryText = "ไม่จำกัดเวลา"
            }
        };

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

        bool usedDatabase = false;

        try
        {
            // ขั้นตอนที่ 2: ตรวจสอบการเชื่อมต่อฐานข้อมูลจริง และตรวจสอบว่ามีข้อมูลสินค้าพร้อมใช้งานหรือไม่
            if (_db.Database.CanConnect())
            {
                var baseQuery = _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Where(p => p.IsActive == true || p.IsActive == null);

                int catalogTotal = baseQuery.Count();
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
                        string term = viewModel.SearchTerm.ToLower();
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
                        string selectedSeries = viewModel.SelectedSeries;
                        query = query.Where(p => p.SeriesName == selectedSeries);
                    }

                    // เงื่อนไขที่ 4: กรองตามสถานะสินค้าพร้อมส่ง status
                    if (viewModel.SelectedStatus == "มีสินค้า")
                    {
                        query = query.Where(p => p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0);
                    }

                    // เงื่อนไขที่ 5: กรองตามช่วงราคา priceRange
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPriceRange) && viewModel.SelectedPriceRange != "ทั้งหมด")
                    {
                        string pr = viewModel.SelectedPriceRange;
                        if (pr == "฿0 - ฿1,000" || pr == "<= 1000" || pr == "0-1000")
                        {
                            query = query.Where(p => p.RetailPrice <= 1000m);
                        }
                        else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                        {
                            query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                        }
                        else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                        {
                            query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                        }
                        else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                        {
                            query = query.Where(p => p.RetailPrice > 5000m);
                        }
                    }

                    // ตรวจสอบข้อมูลโปรโมชั่นจริงจากฐานข้อมูลเมื่อมีพารามิเตอร์ promoCode หรือ promoScope
                    if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoCode))
                    {
                        var matchedPromo = _db.Promotions.FirstOrDefault(p => p.VoucherCode.ToUpper() == viewModel.SelectedPromoCode);
                        if (matchedPromo != null)
                        {
                            viewModel.SelectedPromoScope = (matchedPromo.ApplicableScope ?? "all").Trim().ToLowerInvariant();
                            viewModel.PromoBannerTitle = $"{matchedPromo.Name} (รหัส {matchedPromo.VoucherCode})";
                            string discountText = (matchedPromo.PromoType == "percent" || matchedPromo.PromoType == "percentage")
                                ? $"ส่วนลด {matchedPromo.DiscountValue:0.#}%"
                                : matchedPromo.PromoType == "double_points"
                                    ? "แต้มสะสม x2"
                                    : $"ส่วนลด ฿{matchedPromo.DiscountValue:N0}";
                            viewModel.PromoBannerCondition = $"ร่วมรายการ: {PromoAndRedemptionHelper.GetScopeThaiLabel(viewModel.SelectedPromoScope)} • {discountText} • ขั้นต่ำ ฿{matchedPromo.MinimumSpend:N0}";
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(viewModel.SelectedPromoScope))
                    {
                        viewModel.PromoBannerTitle = $"รายการสินค้าตามสิทธิ์โปรโมชั่น";
                        viewModel.PromoBannerCondition = $"ขอบเขตสินค้า: {PromoAndRedemptionHelper.GetScopeThaiLabel(viewModel.SelectedPromoScope)}";
                    }

                    // ดึงรายการผลลัพธ์และแปลงเป็น ProductCardViewModel (พร้อมกรองตามขอบเขตโปรโมชั่นหากระบุ)
                    var filteredProducts = query.ToList();
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
                        AvailableStock = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)
                    }).ToList();
                }
            }
        }
        catch (Exception)
        {
            // หากเกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล ให้สลับไปใช้ระบบสำรองข้อมูล
            usedDatabase = false;
        }

        // ขั้นตอนที่ 4: กรณีฐานข้อมูลออฟไลน์หรือไม่พบข้อมูล ให้ใช้ข้อมูลสำรองแคตตาล็อกและกรองผ่าน LINQ
        if (!usedDatabase)
        {
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

            var query = fallbackProducts.AsEnumerable();

            // เงื่อนไขที่ 1: คำค้นหา (รองรับการค้นหารหัสสินค้าแบบไม่ใส่ขีด -)
            if (!string.IsNullOrWhiteSpace(viewModel.SearchTerm))
            {
                string term = viewModel.SearchTerm.ToLower();
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
                string selectedSeries = viewModel.SelectedSeries;
                query = query.Where(p => p.SeriesName == selectedSeries);
            }

            // เงื่อนไขที่ 4: สถานะสินค้าพร้อมส่ง
            if (viewModel.SelectedStatus == "มีสินค้า")
            {
                query = query.Where(p => p.AvailableStock > 0);
            }

            // เงื่อนไขที่ 5: ช่วงราคา
            if (!string.IsNullOrWhiteSpace(viewModel.SelectedPriceRange) && viewModel.SelectedPriceRange != "ทั้งหมด")
            {
                string pr = viewModel.SelectedPriceRange;
                if (pr == "฿0 - ฿1,000" || pr == "<= 1000" || pr == "0-1000")
                {
                    query = query.Where(p => p.RetailPrice <= 1000m);
                }
                else if (pr == "฿1,001 - ฿3,000" || pr == "1001-3000")
                {
                    query = query.Where(p => p.RetailPrice > 1000m && p.RetailPrice <= 3000m);
                }
                else if (pr == "฿3,001 - ฿5,000" || pr == "3001-5000")
                {
                    query = query.Where(p => p.RetailPrice > 3000m && p.RetailPrice <= 5000m);
                }
                else if (pr == "มากกว่า ฿5,000" || pr == "> 5000" || pr == ">5000")
                {
                    query = query.Where(p => p.RetailPrice > 5000m);
                }
            }

            viewModel.Products = query.ToList();
        }

        // ขั้นตอนที่ 5: คำนวณการแบ่งหน้าสำหรับรายการสินค้าฟิกเกอร์ (หน้าละ 20 ชิ้น)
        viewModel.TotalCount = viewModel.Products.Count;
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
        return new List<ProductCardViewModel>
        {
            new ProductCardViewModel
            {
                ProductId = 1,
                Sku = "FIG-NARUTO-001",
                Name = "นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Naruto Shippuden",
                BrandName = "Banpresto",
                Scale = "1/6 Scale",
                RetailPrice = 4290.00m,
                ImageUrl = "~/media/images/naruto_hero_fig.png",
                AvailableStock = 13
            },
            new ProductCardViewModel
            {
                ProductId = 2,
                Sku = "FIG-TANJIRO-002",
                Name = "ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Demon Slayer",
                BrandName = "Good Smile Company",
                Scale = "Non-scale",
                RetailPrice = 2450.00m,
                ImageUrl = "~/media/images/tanjiro_kamado.png",
                AvailableStock = 19
            },
            new ProductCardViewModel
            {
                ProductId = 3,
                Sku = "FIG-MIKU-003",
                Name = "ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Vocaloid",
                BrandName = "Good Smile Company",
                Scale = "1/7 Scale",
                RetailPrice = 4200.00m,
                ImageUrl = "~/media/images/hatsune_miku_magical.png",
                AvailableStock = 5
            },
            new ProductCardViewModel
            {
                ProductId = 4,
                Sku = "FIG-LUFFY-004",
                Name = "มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 950.00m,
                ImageUrl = "~/media/images/luffy_gear5.png",
                AvailableStock = 26
            },
            new ProductCardViewModel
            {
                ProductId = 5,
                Sku = "FIG-ANYA-005",
                Name = "อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด",
                CategoryName = "ของสะสมและตุ๊กตา",
                SeriesName = "Spy x Family",
                BrandName = "Megahouse",
                Scale = "1/7 Scale",
                RetailPrice = 1850.00m,
                ImageUrl = "~/media/images/anya_forger.png",
                AvailableStock = 10
            },
            new ProductCardViewModel
            {
                ProductId = 6,
                Sku = "FIG-NARUTO-006",
                Name = "นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "Naruto Shippuden",
                BrandName = "Banpresto",
                Scale = "1/8 Scale",
                RetailPrice = 1200.00m,
                ImageUrl = "~/media/images/naruto_vibration_stars.png",
                AvailableStock = 22
            },
            new ProductCardViewModel
            {
                ProductId = 7,
                Sku = "FIG-GOJO-007",
                Name = "โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Megahouse",
                Scale = "1/7 Scale",
                RetailPrice = 3800.00m,
                ImageUrl = "~/media/images/gojo_satoru.png",
                AvailableStock = 8
            },
            new ProductCardViewModel
            {
                ProductId = 8,
                Sku = "FIG-RAIDEN-008",
                Name = "โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Genshin Impact",
                BrandName = "Good Smile Company",
                Scale = "1/7 Scale",
                RetailPrice = 5690.00m,
                ImageUrl = "~/media/images/raiden_shogun.png",
                AvailableStock = 5
            },
            new ProductCardViewModel
            {
                ProductId = 9,
                Sku = "FIG-BEAR-009",
                Name = "ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล",
                CategoryName = "ของสะสมและตุ๊กตา",
                SeriesName = "FriendHouse Studio",
                BrandName = "FriendHouse Original",
                Scale = "Non-scale",
                RetailPrice = 590.00m,
                ImageUrl = "~/media/images/cloud_bear_plush.png",
                AvailableStock = 35
            },
            new ProductCardViewModel
            {
                ProductId = 10,
                Sku = "FIG-ZORO-010",
                Name = "โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 2790.00m,
                ImageUrl = "~/media/images/roronoa_zoro.png",
                AvailableStock = 16
            },
            new ProductCardViewModel
            {
                ProductId = 11,
                Sku = "FIG-NEZUKO-011",
                Name = "คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Demon Slayer",
                BrandName = "Aniplex",
                Scale = "1/8 Scale",
                RetailPrice = 3450.00m,
                ImageUrl = "~/media/images/nezuko_kamado.png",
                AvailableStock = 12
            },
            new ProductCardViewModel
            {
                ProductId = 12,
                Sku = "FIG-MEGUMI-012",
                Name = "ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Kotobukiya",
                Scale = "1/8 Scale",
                RetailPrice = 3190.00m,
                ImageUrl = "~/media/images/megumi_fushiguro.png",
                AvailableStock = 14
            },
            new ProductCardViewModel
            {
                ProductId = 13,
                Sku = "FIG-ZONG-013",
                Name = "จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ",
                CategoryName = "ของสะสมลิมิเต็ด",
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                Scale = "1/7 Scale",
                RetailPrice = 6200.00m,
                ImageUrl = "~/media/images/zhongli_morax.png",
                AvailableStock = 4
            },
            new ProductCardViewModel
            {
                ProductId = 14,
                Sku = "FIG-SASUKE-014",
                Name = "อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "Naruto Shippuden",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 2590.00m,
                ImageUrl = "~/media/images/sasuke_uchiha.png",
                AvailableStock = 19
            },
            new ProductCardViewModel
            {
                ProductId = 15,
                Sku = "FIG-RENBK-015",
                Name = "เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Demon Slayer",
                BrandName = "Aniplex",
                Scale = "1/8 Scale",
                RetailPrice = 4600.00m,
                ImageUrl = "~/media/images/rengoku_kyojuro.png",
                AvailableStock = 8
            },
            new ProductCardViewModel
            {
                ProductId = 16,
                Sku = "FIG-YOR-016",
                Name = "ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Spy x Family",
                BrandName = "Good Smile Company",
                Scale = "1/7 Scale",
                RetailPrice = 3250.00m,
                ImageUrl = "~/media/images/yor_forger.png",
                AvailableStock = 13
            },
            new ProductCardViewModel
            {
                ProductId = 17,
                Sku = "FIG-MIKUS-017",
                Name = "ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด",
                CategoryName = "ของสะสมลิมิเต็ด",
                SeriesName = "Vocaloid",
                BrandName = "Good Smile Company",
                Scale = "1/7 Scale",
                RetailPrice = 2890.00m,
                ImageUrl = "~/media/images/hatsune_miku_sakura.png",
                AvailableStock = 9
            },
            new ProductCardViewModel
            {
                ProductId = 18,
                Sku = "FIG-ACE-018",
                Name = "โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "One Piece",
                BrandName = "MegaHouse",
                Scale = "Non-scale",
                RetailPrice = 1890.00m,
                ImageUrl = "~/media/images/portgas_d_ace.png",
                AvailableStock = 18
            },
            new ProductCardViewModel
            {
                ProductId = 19,
                Sku = "FIG-ITAD-019",
                Name = "อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 1450.00m,
                ImageUrl = "~/media/images/itadori_yuji.png",
                AvailableStock = 24
            },
            new ProductCardViewModel
            {
                ProductId = 20,
                Sku = "FIG-GANYU-020",
                Name = "กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                Scale = "1/7 Scale",
                RetailPrice = 5400.00m,
                ImageUrl = "~/media/images/ganyu_plenilune.png",
                AvailableStock = 5
            },
            new ProductCardViewModel
            {
                ProductId = 21,
                Sku = "FIG-CAT-021",
                Name = "ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม",
                CategoryName = "ของสะสมและตุ๊กตา",
                SeriesName = "FriendHouse Studio",
                BrandName = "FriendHouse Original",
                Scale = "Non-scale",
                RetailPrice = 490.00m,
                ImageUrl = "~/media/images/lucky_cat_plush.png",
                AvailableStock = 45
            },
            new ProductCardViewModel
            {
                ProductId = 22,
                Sku = "FIG-CHOP-022",
                Name = "โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ",
                CategoryName = "ของสะสมและตุ๊กตา",
                SeriesName = "One Piece",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 650.00m,
                ImageUrl = "~/media/images/chopper_plush.png",
                AvailableStock = 32
            },
            new ProductCardViewModel
            {
                ProductId = 23,
                Sku = "FIG-NOBARA-023",
                Name = "คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "Jujutsu Kaisen",
                BrandName = "Bandai Spirits",
                Scale = "Non-scale",
                RetailPrice = 1350.00m,
                ImageUrl = "~/media/images/nobara_kugisaki.png",
                AvailableStock = 23
            },
            new ProductCardViewModel
            {
                ProductId = 24,
                Sku = "FIG-HUTAO-024",
                Name = "ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง",
                CategoryName = "ฟิกเกอร์สเกล",
                SeriesName = "Genshin Impact",
                BrandName = "Apex Innovation",
                Scale = "1/7 Scale",
                RetailPrice = 5200.00m,
                ImageUrl = "~/media/images/hu_tao_ghost.png",
                AvailableStock = 7
            },
            new ProductCardViewModel
            {
                ProductId = 25,
                Sku = "FIG-LOID-025",
                Name = "ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส",
                CategoryName = "แอคชั่นฟิกเกอร์",
                SeriesName = "Spy x Family",
                BrandName = "Good Smile Company",
                Scale = "Non-scale",
                RetailPrice = 2390.00m,
                ImageUrl = "~/media/images/loid_forger.png",
                AvailableStock = 16
            }
        };
    }

    // READ: แสดงรายการโปรโมชั่นและสิทธิพิเศษของร้านพร้อมระบบค้นหา (GET Method)
    // WHY: ดึงรายการโปรโมชั่นที่เปิดใช้งาน แปลงรหัสประเภทและขอบเขตเป็นภาษาไทยผ่าน PromotionCatalogViewModel
    public IActionResult Promotion(string? searchTerm, string? promoType)
    {
        // ขั้นตอนที่ 1: ดึงรายการโปรโมชั่นที่เปิดใช้งานจากฐานข้อมูล
        var query = _db.Promotions.Where(p => p.IsActive == true || p.IsActive == null);

        // ขั้นตอนที่ 2: กรองตามคำค้นหาชื่อโปรโมชั่นหรือรหัสคูปอง (รองรับการค้นหาแบบไม่ใส่ขีด -)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
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

        var promotions = query.OrderBy(p => p.PromotionId).ToList();
        var collectedCodes = PromoAndRedemptionHelper.ReadCollectedCodes(
            HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey));

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
            string tagThai = PromoAndRedemptionHelper.GetPromoTypeThaiLabel(p.PromoType);
            string scopeCode = (p.ApplicableScope ?? "all").Trim().ToLowerInvariant();
            string scopeThai = PromoAndRedemptionHelper.GetScopeThaiLabel(scopeCode);

            // จัดรูปแบบคำอธิบายส่วนลดตามประเภท
            string discountText = (p.PromoType == "percent" || p.PromoType == "percentage")
                ? $"ส่วนลด {p.DiscountValue:0.#}%"
                : p.PromoType == "double_points"
                    ? "รับแต้มสะสม x2"
                    : $"ส่วนลด ฿{p.DiscountValue:N0}";

            return new PromotionCardItemViewModel
            {
                PromotionId = p.PromotionId,
                Tag = tagThai,
                Title = p.Name,
                Description = $"ใช้ได้กับ{scopeThai} จำกัดจำนวน {p.UsageLimit ?? 100} สิทธิ์ (ใช้แล้ว {p.CurrentUsageCount:N0} สิทธิ์)",
                Period = $"{p.StartDate:dd/MM/yyyy} - {p.EndDate:dd/MM/yyyy}",
                Condition = $"รหัสคูปอง: {p.VoucherCode} • {discountText} • ยอดขั้นต่ำ ฿{p.MinimumSpend:N0}",
                VoucherCode = p.VoucherCode,
                ApplicableScope = scopeCode,
                IsCollected = collectedCodes.Contains(p.VoucherCode, StringComparer.OrdinalIgnoreCase)
            };
        }).ToList();

        return View(viewModel);
    }

    // ACTION: ลูกค้ากดเก็บคูปองโปรโมชั่นไว้ใช้ในหน้าตะกร้าสินค้า (POST Method)
    [HttpPost]
    public IActionResult CollectPromoCode(string? voucherCode, string? returnUrl)
    {
        string normalized = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        if (string.IsNullOrEmpty(normalized))
        {
            TempData["ErrorMessage"] = "ไม่พบรหัสคูปองที่ต้องการเก็บ";
            return RedirectToAction("Promotion");
        }

        var promo = _db.Promotions.FirstOrDefault(p => p.VoucherCode.ToUpper() == normalized);
        if (promo == null || promo.IsActive == false)
        {
            TempData["ErrorMessage"] = $"ไม่พบโปรโมชั่นรหัส \"{normalized}\" หรือถูกปิดใช้งานแล้ว";
            return RedirectToAction("Promotion");
        }

        var today = PromoAndRedemptionHelper.TodayGregorian();
        if (today > promo.EndDate)
        {
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว";
            return RedirectToAction("Promotion");
        }

        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            TempData["ErrorMessage"] = $"คูปอง \"{promo.VoucherCode}\" ถูกใช้ครบสิทธิ์แล้ว";
            return RedirectToAction("Promotion");
        }

        var collected = PromoAndRedemptionHelper.ReadCollectedCodes(
            HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey));
        if (!collected.Contains(promo.VoucherCode, StringComparer.OrdinalIgnoreCase))
        {
            collected.Add(promo.VoucherCode.ToUpperInvariant());
            HttpContext.Session.SetString(
                PromoAndRedemptionHelper.CollectedPromoSessionKey,
                PromoAndRedemptionHelper.SerializeCollectedCodes(collected));
            TempData["SuccessMessage"] = $"เก็บคูปอง \"{promo.VoucherCode}\" ({promo.Name}) เรียบร้อยแล้ว สามารถกดใช้งานได้ที่หน้าตะกร้าสินค้า";
        }
        else
        {
            TempData["SuccessMessage"] = $"คุณมีคูปอง \"{promo.VoucherCode}\" อยู่ในรายการที่เก็บไว้แล้ว";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Promotion");
    }

    public IActionResult Customer()
    {
        return RedirectToAction("Index");
    }

    private int? GetCurrentCustomerId()
    {
        // WHY: ดึง ID ของลูกค้า โดยให้ความสำคัญกับ CustomerUserId ก่อน
        // เพื่อให้สามารถเปิดทดสอบหลาย Tab สลับบทบาท (Customer vs Staff) ได้อย่างอิสระโดยไม่แย่ง Session กัน
        return HttpContext.Session.GetInt32("CustomerUserId")
            ?? (HttpContext.Session.GetString("Role") == "customer" ? HttpContext.Session.GetInt32("UserId") : null)
            ?? HttpContext.Session.GetInt32("UserId");
    }

    // READ: แสดงรายการสินค้าในตะกร้า (Cart)
    // WHY: ตรวจสอบสิทธิ์ผู้ใช้ แปลงข้อมูลเป็น CartViewModel คำนวณยอดเงินรวมและดึงเบอร์โทรศัพท์สำหรับแบบฟอร์มยืนยัน
    public IActionResult Cart()
    {
        // ขั้นตอนที่ 1: ดึงรหัสผู้ใช้งานปัจจุบันจาก Session เพื่อตรวจสอบสิทธิ์การเข้าสู่ระบบ
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเพื่อดูตะกร้าสินค้าของคุณ";
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
            HttpContext.Session.GetString(PromoAndRedemptionHelper.CollectedPromoSessionKey));
        string? appliedCode = HttpContext.Session.GetString(PromoAndRedemptionHelper.AppliedPromoSessionKey);

        if (collectedCodes.Any())
        {
            var collectedPromos = _db.Promotions
                .Where(p => (p.IsActive == true || p.IsActive == null) && collectedCodes.Contains(p.VoucherCode.ToUpper()))
                .OrderBy(p => p.PromotionId)
                .ToList();

            viewModel.CollectedPromos = collectedPromos.Select(p =>
            {
                string label = (p.PromoType == "percent" || p.PromoType == "percentage")
                    ? $"ลด {p.DiscountValue:0.#}%"
                    : p.PromoType == "double_points"
                        ? "แต้ม x2"
                        : $"ลด ฿{p.DiscountValue:N0}";
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

        if (!string.IsNullOrWhiteSpace(appliedCode) && rawItems.Any())
        {
            var promo = FindPromotionByVoucherCode(appliedCode);
            var eval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, rawItems);
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

        return View(viewModel);
    }

    // ค้นหาโปรโมชั่นจากรหัสคูปอง (รองรับทั้งพิมพ์ตรงและไม่ใส่ขีด -)
    private Promotion? FindPromotionByVoucherCode(string? voucherCode)
    {
        string normalized = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        string noHyphen = PromoAndRedemptionHelper.NormalizeCodeNoHyphen(voucherCode);
        if (string.IsNullOrEmpty(normalized)) return null;

        return _db.Promotions.ToList().FirstOrDefault(p =>
            p.VoucherCode.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(noHyphen) && PromoAndRedemptionHelper.NormalizeCodeNoHyphen(p.VoucherCode) == noHyphen));
    }

    // ACTION: กรอกหรือเลือกใช้รหัสโปรโมชั่นกับรายการสินค้าในตะกร้า (POST Method)
    [HttpPost]
    public IActionResult ApplyPromoCode(string? voucherCode)
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนใช้งานรหัสโปรโมชั่น";
            return RedirectToAction("Login", "Account");
        }

        string cleanCode = PromoAndRedemptionHelper.NormalizeCode(voucherCode);
        if (string.IsNullOrEmpty(cleanCode))
        {
            TempData["ErrorMessage"] = "กรุณากรอกรหัสคูปองโปรโมชั่นก่อนกดใช้งาน";
            return RedirectToAction("Cart");
        }

        var rawItems = _db.Cartitems
            .Include(c => c.Product).ThenInclude(p => p.Category)
            .Include(c => c.Product).ThenInclude(p => p.Inventory)
            .Where(c => c.UserId == userId.Value)
            .ToList();

        if (!rawItems.Any())
        {
            TempData["ErrorMessage"] = "กรุณาเพิ่มสินค้าลงในตะกร้าก่อนใช้รหัสโปรโมชั่น";
            return RedirectToAction("Cart");
        }

        var promo = FindPromotionByVoucherCode(cleanCode);
        var eval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, rawItems);
        if (!eval.IsValid || eval.Promotion == null)
        {
            TempData["ErrorMessage"] = eval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นนี้ได้";
            return RedirectToAction("Cart");
        }

        HttpContext.Session.SetString(PromoAndRedemptionHelper.AppliedPromoSessionKey, eval.Promotion.VoucherCode);
        TempData["SuccessMessage"] = $"ใช้คูปอง \"{eval.Promotion.VoucherCode}\" สำเร็จ ({eval.BenefitSummary})";
        return RedirectToAction("Cart");
    }

    // ACTION: ยกเลิกการใช้รหัสโปรโมชั่นในตะกร้าสินค้า (POST Method)
    [HttpPost]
    public IActionResult RemovePromoCode()
    {
        HttpContext.Session.Remove(PromoAndRedemptionHelper.AppliedPromoSessionKey);
        TempData["SuccessMessage"] = "ยกเลิกการใช้รหัสโปรโมชั่นในตะกร้าเรียบร้อยแล้ว";
        return RedirectToAction("Cart");
    }

    // Action สำหรับเพิ่มสินค้าลงในตะกร้าของสมาชิก (POST Method)
    [HttpPost]
    // ฟังก์ชัน AddToCart รับ productId (รหัสสินค้า), quantity (จำนวนชิ้น), returnUrl (URL ย้อนกลับ) และ goToCart (พาไปหน้าตะกร้าทันทีเมื่อกดซื้อตอนนี้)
    public IActionResult AddToCart(int productId, int quantity = 1, string? returnUrl = null, bool goToCart = false)
    {
        // ขั้นตอนที่ 1: ดึงรหัสผู้ใช้งานปัจจุบันจาก Session เพื่อตรวจสอบสิทธิ์การเข้าสู่ระบบ
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนให้ผู้ใช้งานเข้าสู่ระบบก่อน
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเพื่อเพิ่มสินค้าลงในตะกร้าและเลือกวันรับสินค้า";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        string? safeReturnUrl = (!goToCart && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            ? returnUrl
            : null;

        // ขั้นตอนที่ 2: แปลงรหัสสินค้าเพื่อรองรับรหัสข้อมูลดั้งเดิม (201-205 ให้แปลงเป็น 1-5)
        if (productId >= 201 && productId <= 205)
        {
            // ลดค่ารหัสสินค้าลง 200 เพื่อให้ตรงกับรหัสจริงในฐานข้อมูล
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าจากตาราง Products พร้อมดึงข้อมูลสต็อก Inventories
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId)
            // กรณีไม่พบรหัสที่ระบุ ให้เลือกสินค้าชิ้นแรกในฐานข้อมูลเป็นค่าเริ่มต้น
            ?? _db.Products.Include(p => p.Inventory).FirstOrDefault();

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้านี้ในระบบฐานข้อมูล";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(safeReturnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 3: คำนวณจำนวนสินค้าคงเหลือที่พร้อมให้สั่งจอง (QuantityOnHand ลบด้วย ReservedQuantity)
        int availableQty = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
        // ตรวจสอบว่ามีสินค้าพร้อมให้สั่งจองหรือไม่
        if (availableQty <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีสินค้าถูกจองเต็มจำนวนแล้ว
            TempData["ErrorMessage"] = "ขออภัย สินค้านี้ถูกจองเต็มจำนวนแล้ว (สินค้าหมดชั่วคราว)";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(safeReturnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 4: ค้นหาว่าผู้ใช้งานคนนี้มีสินค้านี้อยู่ในตะกร้าแล้วหรือไม่
        var existingItem = _db.Cartitems.FirstOrDefault(c => c.UserId == userId.Value && c.ProductId == product.ProductId);
        // กำหนดจำนวนสินค้าที่ต้องการสั่งเพิ่ม โดยไม่ให้น้อยกว่า 1 ชิ้น
        int requestedQty = Math.Max(1, quantity);
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
    // ฟังก์ชัน UpdateCartQuantity รับ cartItemId (รหัสรายการตะกร้า) และ quantity (จำนวนชิ้นใหม่)
    public IActionResult UpdateCartQuantity(int cartItemId, int quantity)
    {
        // ค้นหาข้อมูลรายการในตะกร้าจากตาราง Cartitems พร้อมเชื่อมโยง Product และ Inventory
        var item = _db.Cartitems.Include(c => c.Product).ThenInclude(p => p.Inventory).FirstOrDefault(c => c.CartItemId == cartItemId);
        // ตรวจสอบว่าพบรายการในตะกร้าหรือไม่
        if (item == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการสินค้านี้ในตะกร้า";
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
    // ฟังก์ชัน RemoveFromCart รับ cartItemId ที่ต้องการลบ
    public IActionResult RemoveFromCart(int cartItemId)
    {
        // ค้นหารายการในตาราง Cartitems ด้วยรหัส CartItemId
        var item = _db.Cartitems.FirstOrDefault(c => c.CartItemId == cartItemId);
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

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // Action สำหรับล้างรายการสินค้าทั้งหมดในตะกร้าของผู้ใช้ (POST Method)
    [HttpPost]
    // ฟังก์ชัน ClearCart ลบรายการทั้งหมดของผู้ใช้ที่ล็อกอินอยู่
    public IActionResult ClearCart()
    {
        // ดึงรหัสผู้ใช้งานปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานล็อกอินอยู่หรือไม่
        if (userId.HasValue)
        {
            // ดึงรายการสินค้าทั้งหมดในตะกร้าที่เป็นของผู้ใช้งานคนนี้
            var items = _db.Cartitems.Where(c => c.UserId == userId.Value).ToList();
            // สั่งลบรายการทั้งหมดพร้อมกันด้วย RemoveRange
            _db.Cartitems.RemoveRange(items);
            // บันทึกการเปลี่ยนแปลงลง MySQL Database
            _db.SaveChanges();
            // กำหนดข้อความแจ้งผลการล้างตะกร้าสำเร็จ
            TempData["SuccessMessage"] = "ล้างรายการสินค้าในตะกร้าเรียบร้อยแล้ว";
        }

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // ฟังก์ชันกลางตรวจสอบความถูกต้องของวันที่รับสินค้า (ใช้ร่วมกันระหว่างตะกร้าสินค้าและการสั่งซื้อโดยตรง)
    private static bool TryResolvePickupDate(string? pickupDate, out DateOnly resolvedDate, out string? errorMessage)
    {
        resolvedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(pickupDate))
        {
            errorMessage = "กรุณาเลือกวันที่สะดวกเข้ารับสินค้าที่หน้าร้าน";
            return false;
        }

        if (!DateOnly.TryParseExact(pickupDate.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate)
            && !DateOnly.TryParse(pickupDate.Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
        {
            errorMessage = "รูปแบบวันที่รับสินค้าไม่ถูกต้อง กรุณาเลือกใหม่อีกครั้ง";
            return false;
        }

        // ใช้ปี ค.ศ. จากปฏิทินสากลเปรียบเทียบเสมอ (ป้องกันระบบแปลง พ.ศ. 2569 ผิดพลาด)
        var todayGregorian = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
        if (parsedDate < todayGregorian)
        {
            errorMessage = "ไม่สามารถเลือกวันที่ย้อนหลังได้ กรุณาเลือกวันปัจจุบันเป็นต้นไป";
            return false;
        }

        resolvedDate = parsedDate;
        return true;
    }

    // Action ยืนยันการสั่งจองพรีออเดอร์จากตะกร้าสินค้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน ConfirmPreorder รับเบอร์โทรติดต่อ สาขาที่ต้องการรับ วันที่สะดวกเข้ารับสินค้า และรหัสคูปอง (ถ้ามี)
    public IActionResult ConfirmPreorder(string? contactPhone, string? pickupBranch, string? pickupDate, string? voucherCode = null)
    {
        // ขั้นตอนที่ 1: ดึงรหัสลูกค้าปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนสีแดงผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนยืนยันการจอง";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของวันที่นัดรับสินค้าที่ลูกค้าเลือกจากหน้าตะกร้า
        if (!TryResolvePickupDate(pickupDate, out DateOnly estPickupDate, out string? dateError))
        {
            TempData["ErrorMessage"] = dateError;
            return RedirectToAction("Cart");
        }

        // เริ่มต้นฐานข้อมูล Transaction เพื่อรองรับความปลอดภัยของสต็อกและป้องกัน Race Condition
        using var transaction = _db.Database.BeginTransaction();
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

            // ขั้นตอนที่ 2 (Race Condition Check): ตรวจสอบสต็อกคงเหลือจริงแบบสดๆ ใน Transaction
            foreach (var item in cartItems)
            {
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
            decimal discountAmount = 0m;
            PromoEvaluationResult? appliedEval = null;

            string? effectivePromoCode = !string.IsNullOrWhiteSpace(voucherCode)
                ? voucherCode
                : HttpContext.Session.GetString(PromoAndRedemptionHelper.AppliedPromoSessionKey);

            if (!string.IsNullOrWhiteSpace(effectivePromoCode))
            {
                var promo = FindPromotionByVoucherCode(effectivePromoCode);
                appliedEval = PromoAndRedemptionHelper.EvaluateCartPromotion(promo, cartItems);
                if (!appliedEval.IsValid || appliedEval.Promotion == null)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = appliedEval.ErrorMessage ?? "ไม่สามารถใช้รหัสโปรโมชั่นได้ กรุณาตรวจสอบอีกครั้ง";
                    return RedirectToAction("Cart");
                }
                discountAmount = appliedEval.DiscountAmount;
                appliedEval.Promotion.CurrentUsageCount += 1;
            }

            decimal total = Math.Max(0m, originalSubtotal - discountAmount);
            // สร้างรหัสใบสั่งจองพรีออเดอร์รูปแบบ PO-2026-XXXX
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);
            string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
                ? contactPhone.Trim()
                : (user?.Phone ?? "081-234-5678");

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

            if (appliedEval != null && appliedEval.Promotion != null)
            {
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
                string logCode = $"PRM-{reservationNo}-{Random.Shared.Next(100, 999)}";
                if (logCode.Length > 30) logCode = logCode[..30];

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

            HttpContext.Session.Remove(PromoAndRedemptionHelper.AppliedPromoSessionKey);

            string promoSuccessSuffix = appliedEval != null
                ? $" (ใช้โค้ด {appliedEval.VoucherCode}: {appliedEval.BenefitSummary})"
                : string.Empty;

            // แจ้งเตือนข้อความสำเร็จพร้อมหมายเลขใบจองผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว{promoSuccessSuffix} สามารถมารับสินค้าได้ที่หน้าร้าน";
            // เปลี่ยนเส้นทางไปยังหน้าแสดงสถานะคำสั่งจองของลูกค้า
            return RedirectToAction("PreorderStatus");
        }
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
    // ฟังก์ชัน DirectPreorder รับ productId, quantity, contactPhone และ pickupDate
    public IActionResult DirectPreorder(int productId, int quantity = 1, string? contactPhone = null, string? pickupDate = null)
    {
        // ขั้นตอนที่ 1: ตรวจสอบการเข้าสู่ระบบของลูกค้า
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบหรือยัง
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำรายการสั่งจองสินค้า";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าใช้
            return RedirectToAction("Login", "Account");
        }

        // แปลงรหัสสินค้ารองรับข้อมูลดั้งเดิม 201-205 ให้เป็น 1-5
        if (productId >= 201 && productId <= 205)
        {
            // หักลบค่ารหัสสินค้า 200
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าในตาราง Products ด้วยรหัส ProductId
        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId)
            // หากไม่พบ ให้ใช้สินค้าชิ้นแรกในระบบ
            ?? _db.Products.FirstOrDefault();

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // แจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการสั่งจอง";
            // เปลี่ยนเส้นทางกลับไปหน้ารวมสินค้า
            return RedirectToAction("Collection");
        }

        // กำหนดจำนวนสินค้าที่จอง ขั้นต่ำ 1 ชิ้น
        int qty = Math.Max(1, quantity);

        // เริ่มต้นฐานข้อมูล Transaction
        using var transaction = _db.Database.BeginTransaction();
        try
        {
            // ค้นหาข้อมูลสต็อกในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == product.ProductId);
            // คำนวณสต็อกพร้อมจองจริง = QuantityOnHand ลบด้วย ReservedQuantity
            int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าสินค้ามีพอให้สั่งจองหรือไม่
            if (inv == null || available < qty)
            {
                // ยกเลิกคำสั่งทั้งหมดใน Transaction
                transaction.Rollback();
                // แจ้งเตือนข้อความว่าสินค้าไม่เพียงพอ
                TempData["ErrorMessage"] = $"ขออภัย สินค้า \"{product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น)";
                // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้าเดิม
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // สร้างหมายเลขใบสั่งจองพรีออเดอร์
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);
            // คำนวณยอดเงินรวมสุทธิ
            decimal totalPrice = product.RetailPrice * qty;

            // กำหนดวันนัดรับสินค้าโดยใช้ฟังก์ชันกลางตรวจสอบวันที่ (หากไม่ได้ระบุให้ค่าเริ่มต้นเป็น 7 วัน)
            DateOnly estDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            if (!string.IsNullOrWhiteSpace(pickupDate))
            {
                if (!TryResolvePickupDate(pickupDate, out estDate, out string? dateError))
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = dateError;
                    return RedirectToAction("ProductDetail", new { id = product.ProductId });
                }
            }

            // สร้างอ็อบเจกต์ Preorder สำหรับบันทึกคำสั่งจอง
            var preorder = new Preorder
            {
                // กำหนดหมายเลขใบจอง
                ReservationNumber = reservationNo,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                UserId = userId.Value,
                // กำหนดเบอร์โทรศัพท์ผู้ติดต่อ
                ContactPhone = contactPhone ?? user?.Phone ?? "081-234-5678",
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

        if (id.HasValue && id.Value > 0)
        {
            // แปลงรหัสสินค้าดั้งเดิม (201-205) ให้ตรงกับรหัสจริงในฐานข้อมูล (1-5)
            int targetId = (id.Value >= 201 && id.Value <= 205) ? (id.Value - 200) : id.Value;

            // ดึงข้อมูลสินค้าพร้อมหมวดหมู่และสต็อกคงคลัง
            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == targetId);
        }

        // กรณีไม่ระบุรหัสและไม่ใช่รหัสของรางวัล (301-304) ให้ดึงสินค้าชิ้นแรกเป็นค่าเริ่มต้น
        if (product == null && (!id.HasValue || id.Value < 301 || id.Value > 304))
        {
            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault();
        }

        // ขั้นตอนที่ 2: หากพบสินค้าในฐานข้อมูล ให้ประกอบข้อมูลเป็น ProductDetailViewModel
        if (product != null)
        {
            // คำนวณจำนวนสต็อกที่พร้อมให้สั่งจอง
            int availableStock = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);

            var detailVm = new ProductDetailViewModel
            {
                ProductId = product.ProductId,
                Sku = product.Sku,
                Name = product.Name,
                CategoryName = product.Category?.CategoryName ?? "ฟิกเกอร์สเกล",
                SeriesName = product.SeriesName ?? "คอลเลกชัน FriendHouse",
                BrandName = product.BrandName ?? "ผู้ผลิตลิขสิทธิ์แท้",
                Scale = product.Scale ?? "มาตรฐาน",
                HeightCm = product.HeightCm,
                Material = product.Material ?? "PVC + ABS เกรดพรีเมียม",
                RetailPrice = product.RetailPrice,
                PreorderPrice = product.PreorderPrice,
                ImageUrl = !string.IsNullOrEmpty(product.ImageUrl) ? product.ImageUrl : "~/media/images/naruto_hero_fig.png",
                Model3DPath = product.Model3Dpath,
                AvailableStock = availableStock,
                ExpectedArrival = !string.IsNullOrEmpty(product.ExpectedArrival) ? product.ExpectedArrival : "มีสินค้าพร้อมส่งมอบ",
                Description = $"{product.Name} ผลิตโดย {product.BrandName ?? "แบรนด์ลิขสิทธิ์แท้"} ซีรีส์ {product.SeriesName ?? "ลิขสิทธิ์แท้"} สเกล {product.Scale ?? "มาตรฐาน"} วัสดุ {product.Material ?? "PVC/ABS คุณภาพสูง"} สินค้าลิขสิทธิ์แท้ รับสินค้าและชำระเงินที่หน้าร้าน FriendHouse สยามสแควร์",
                Specs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("หมวดหมู่", product.Category?.CategoryName ?? "ฟิกเกอร์สเกล"),
                    new KeyValuePair<string, string>("ซีรีส์", product.SeriesName ?? "FriendHouse ลิขสิทธิ์แท้"),
                    new KeyValuePair<string, string>("ผู้ผลิต", product.BrandName ?? "ผู้ผลิตลิขสิทธิ์แท้"),
                    new KeyValuePair<string, string>("สเกล", product.Scale ?? "มาตรฐาน"),
                    new KeyValuePair<string, string>("วัสดุ", product.Material ?? "PVC + ABS เกรดพรีเมียม"),
                    new KeyValuePair<string, string>("สถานะ", availableStock > 0 ? "มีสินค้าพร้อมส่งมอบหน้าร้าน" : "เปิดรับสั่งจองพรีออเดอร์")
                }
            };

            return View(detailVm);
        }

        // กรณีเป็นรหัสของรางวัล (301-304) หรือฐานข้อมูลออฟไลน์ ส่งค่า null เพื่อให้ View ใช้แคตตาล็อกสำรอง
        return View((ProductDetailViewModel?)null);
    }

    // READ: แสดงรายการสถานะพรีออเดอร์ของสมาชิก พร้อมระบบค้นหาและตัวกรองสถานะ (Preorder Status)
    // WHY: ดึงรายการสถานะพรีออเดอร์ทั้งหมดของลูกค้า แปลงเป็น PreorderListViewModel พร้อม Tracking Steps
    public IActionResult PreorderStatus(string? searchTerm, string? statusFilter)
    {
        // ขั้นตอนที่ 1: ดึงรหัสลูกค้าปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        var rawPreorders = new List<Preorder>();

        if (userId.HasValue)
        {
            rawPreorders = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(pi => pi.Product)
                .Where(p => p.UserId == userId.Value)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        // กรณีเป็น Guest หรือยังไม่มีรายการใน Account ให้ดึงรายการล่าสุดในระบบมาแสดงผล
        if (!rawPreorders.Any())
        {
            rawPreorders = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(pi => pi.Product)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToList();
        }

        // กรองตามคำค้นหาหมายเลขใบจอง เบอร์โทรศัพท์ หรือชื่อสินค้า (รองรับการค้นหาโดยไม่ต้องพิมพ์ขีด - หรือ # เช่น PO20269187, 20269187, 9187)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // แปลงคำค้นหาเป็นตัวพิมพ์เล็กและสร้างตัวแปรที่ตัดเครื่องหมายขีด (-) เครื่องหมาย # และช่องว่างออกทั้งหมด
            string term = searchTerm.Trim().ToLower();
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");

            rawPreorders = rawPreorders.Where(p =>
            {
                // เตรียมรหัสใบจองและเบอร์โทรศัพท์แบบตัดเครื่องหมายขีด (-) และ # เพื่อเปรียบเทียบ
                string resNoLower = (p.ReservationNumber ?? "").ToLower();
                string resNoNormalized = resNoLower.Replace("-", "").Replace("#", "").Replace(" ", "");
                string phoneLower = (p.ContactPhone ?? "").ToLower();
                string phoneNormalized = phoneLower.Replace("-", "").Replace(" ", "");

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
            if (statusFilter == "active")
            {
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "PendingConfirm" || p.CurrentStatus == "WaitingStock" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "Reserved").ToList();
            }
            else if (statusFilter == "completed")
            {
                rawPreorders = rawPreorders.Where(p => p.CurrentStatus == "Completed").ToList();
            }
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
        var promoMap = PromoAndRedemptionHelper.BuildPreorderPromoMap(promoLogs);

        foreach (var po in rawPreorders)
        {
            string statusBadgeText = "รอดำเนินการ";
            string statusBadgeClass = "bg-amber-50/80 border-amber-200/80 text-amber-800";
            string noticeText = "รายการนี้จะแสดงสถานะอัปเดตแบบเรียลไทม์จากหน้าร้าน เมื่อมารับสินค้าแล้วสามารถแจ้งรหัสใบจองนี้ที่เคาน์เตอร์ได้ทันที";
            var steps = new List<PreorderTrackingStepViewModel>();

            if (po.CurrentStatus == "PendingConfirm" || po.CurrentStatus == "WaitingStock" || po.CurrentStatus == "ReadyForPickup" || po.CurrentStatus == "Reserved")
            {
                statusBadgeText = po.CurrentStatus == "ReadyForPickup" ? "สินค้าพร้อมรับแล้ว" : "รอดำเนินการ";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "รอชำระเงิน/รับสินค้า", Description = "รอสินค้าเข้าสู่สาขาและแจ้งรับ", IsCompleted = false, IsCurrent = true });
            }
            else if (po.CurrentStatus == "Completed")
            {
                statusBadgeText = "สำเร็จเรียบร้อย";
                statusBadgeClass = "bg-emerald-50/80 border-emerald-200/80 text-emerald-800";
                noticeText = "รายการพรีออเดอร์นี้เสร็จสมบูรณ์เรียบร้อย ลูกค้าชำระเงินและรับสินค้าแล้ว ขอขอบคุณที่เลือกซื้อสินค้ากับ FriendHouse";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "ชำระเงิน/รับสินค้าแล้ว", Description = "รับสินค้าที่เคาน์เตอร์แล้ว", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 3, Title = "สำเร็จ", Description = "เสร็จสมบูรณ์", IsCompleted = true, IsCurrent = true });
            }
            else if (po.CurrentStatus == "Cancelled")
            {
                statusBadgeText = "ยกเลิก / ไม่รับสินค้า";
                statusBadgeClass = "bg-rose-50/80 border-rose-200/80 text-rose-800";
                noticeText = "รายการนี้ถูกยกเลิกแล้ว เนื่องจากลูกค้ายกเลิกคำสั่งจองหรือไม่ได้มารับสินค้าตามกำหนด";
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 1, Title = "แจ้งจองออเดอร์", Description = "บันทึกคำสั่งจองเรียบร้อย", IsCompleted = true, IsCurrent = false });
                steps.Add(new PreorderTrackingStepViewModel { StepNumber = 2, Title = "ไม่รับสินค้า / ยกเลิก", Description = "ยกเลิกคำสั่งจอง", IsCompleted = false, IsCurrent = false });
            }

            promoMap.TryGetValue(po.ReservationNumber, out var promoMeta);

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
        var rewardMetaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);

        var rawRedemptions = userId.HasValue
            ? _db.Rewardredemptions
                .Include(r => r.Reward)
                .Include(r => r.User)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.RedeemedAt)
                .ToList()
            : _db.Rewardredemptions
                .Include(r => r.Reward)
                .Include(r => r.User)
                .OrderByDescending(r => r.RedeemedAt)
                .Take(3)
                .ToList();

        // กรองรายการแลกของรางวัลตามคำค้นหา (รองรับค้นหารหัส RWD แบบไม่ใส่ขีด เบอร์โทรที่ระบุตอนแลก/บัญชี และชื่อของรางวัล)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            rawRedemptions = rawRedemptions.Where(r =>
            {
                string codeLower = (r.RedemptionCode ?? "").ToLower();
                string codeNoHyphen = codeLower.Replace("-", "").Replace("#", "").Replace(" ", "");
                string accountPhone = PromoAndRedemptionHelper.NormalizePhoneDigits(r.User?.Phone);
                rewardMetaMap.TryGetValue(r.RedemptionCode ?? "", out var rMeta);
                string metaPhone = PromoAndRedemptionHelper.NormalizePhoneDigits(rMeta?.ContactPhone);
                string rewardTitle = (r.Reward?.Title ?? "").ToLower();

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

        return View(viewModel);
    }

    // ฟังก์ชันช่วยแปลงรายการ Rewardredemption จากฐานข้อมูลเป็น ViewModel (ใช้ร่วมกันทั้งหน้า PreorderStatus และ Rewards)
    private static List<RewardRedemptionViewModel> MapRedemptionsToViewModels(
        IEnumerable<Rewardredemption> items,
        IReadOnlyDictionary<string, RedemptionMetaPayload>? metaMap = null)
    {
        return items.Select(red =>
        {
            RedemptionMetaPayload? meta = null;
            if (metaMap != null && !string.IsNullOrEmpty(red.RedemptionCode))
            {
                metaMap.TryGetValue(red.RedemptionCode, out meta);
            }

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
    public IActionResult CancelPreorder(int preorderId)
    {
        // ขั้นตอนที่ 1: ค้นหาใบสั่งจองพร้อมรายการสินค้าข้างใน
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId);

        if (preorder == null)
        {
            // แจ้งเตือนกรณีไม่พบใบสั่งจอง
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองพรีออเดอร์ที่ต้องการยกเลิก";
            return RedirectToAction("PreorderStatus");
        }

        // ขั้นตอนที่ 2: ตรวจสอบว่าสถานะปัจจุบันสามารถยกเลิกได้หรือไม่
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            TempData["ErrorMessage"] = "ไม่สามารถยกเลิกใบสั่งจองที่ดำเนินการเสร็จสิ้นหรือถูกยกเลิกไปแล้วได้";
            return RedirectToAction("PreorderStatus");
        }

        // ขั้นตอนที่ 3: คืนยอดจอง (ReservedQuantity) ในตาราง Inventories สำหรับสินค้าทุกชิ้นในใบจอง
        foreach (var item in preorder.Preorderitems)
        {
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (inv != null)
            {
                // ลดจำนวนที่สำรองไว้โดยไม่ให้ติดลบ
                inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                inv.UpdatedAt = DateTime.Now;
            }
        }

        // ขั้นตอนที่ 4: ปรับสถานะใบสั่งจองเป็น Cancelled
        preorder.CurrentStatus = "Cancelled";

        // ขั้นตอนที่ 5: บันทึกประวัติการยกเลิกใบสั่งจองลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "PO-CANCEL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "INFO",
            Module = "preorder.cancel",
            Message = $"สมาชิกยกเลิกใบสั่งจองพรีออเดอร์หมายเลข #{preorder.ReservationNumber} และคืนสต็อกสำรองเรียบร้อยแล้ว",
            UserId = GetCurrentCustomerId(),
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนผลสำเร็จและเปลี่ยนเส้นทางกลับไปหน้าแสดงสถานะพรีออเดอร์
        TempData["SuccessMessage"] = $"ยกเลิกคำสั่งจองพรีออเดอร์ #{preorder.ReservationNumber} และคืนสิทธิ์การจองเรียบร้อยแล้ว";
        return RedirectToAction("PreorderStatus");
    }

    // READ: แสดงแคตตาล็อกแลกของรางวัลด้วยแต้มสะสม (Rewards)
    // WHY: ดึงแต้มสะสมจริงของสมาชิกจากฐานข้อมูล, ดึงประวัติการแลกแต้มจริง และตรวจสอบว่าแต้มพอแลกรางวัลแต่ละชิ้นหรือไม่
    public IActionResult Rewards()
    {
        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        int userPoints = 0;
        string userPhone = string.Empty;
        var userRedemptions = new List<RewardRedemptionViewModel>();
        bool isLoggedIn = userId.HasValue;

        // ขั้นตอนที่ 2: หากเข้าสู่ระบบแล้ว ให้ดึงข้อมูลแต้มสะสมและประวัติการแลกจากฐานข้อมูล MySQL
        if (userId.HasValue)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            if (user != null)
            {
                userPoints = user.Points;
                userPhone = user.Phone ?? string.Empty;
            }

            var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
            var rewardMetaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);

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
        ViewBag.IsLoggedIn = isLoggedIn;
        ViewBag.UserRedemptions = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Where(r => userId.HasValue && r.UserId == userId.Value)
            .OrderByDescending(r => r.RedeemedAt)
            .ToList();

        return View(viewModel);
    }

    // =========================================================================
    // โฟลว์: สมาชิกแลกของรางวัลด้วยแต้มสะสม (Redeem Reward)
    // =========================================================================
    // รองรับการส่งคำขอแลกรางวัลผ่านฟอร์ม POST
    [HttpPost]
    // ฟังก์ชันดำเนินการแลกของรางวัลโดยรับค่ารหัสของรางวัล วันที่สะดวกรับของ และเบอร์โทรติดต่อ
    public IActionResult RedeemReward(int rewardId, string? pickupDate = null, string? contactPhone = null, string? voucherCode = null)
    {
        // ดึงรหัสสมาชิกปัจจุบันจากเซสชันล็อกอิน
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานได้เข้าสู่ระบบหรือไม่
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนสีแดงลง TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำการแลกของรางวัล";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ป้องกันการใช้รหัสโปรโมชั่นร่วมกับการแลกของรางวัลด้วยแต้มสะสม
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            TempData["ErrorMessage"] = "รหัสคูปองโปรโมชั่นสามารถใช้กับสินค้าในตะกร้าเท่านั้น ไม่สามารถใช้กับการแลกของรางวัลด้วยแต้มสะสมได้";
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบวันที่สะดวกมารับของรางวัลที่ลูกค้าเลือก
        if (!TryResolvePickupDate(pickupDate, out DateOnly estPickupDate, out string? dateError))
        {
            TempData["ErrorMessage"] = dateError ?? "กรุณาระบุวันที่สะดวกเข้ารับของรางวัล";
            return RedirectToAction("Rewards");
        }

        // ค้นหาข้อมูลสมาชิกในตาราง Users ด้วยรหัสสมาชิก
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ค้นหาข้อมูลของรางวัลในตาราง Rewards ด้วยรหัสของรางวัล
        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);

        // ตรวจสอบว่าไม่พบข้อมูลสมาชิกหรือของรางวัลในฐานข้อมูล
        if (user == null || reward == null)
        {
            // บันทึกข้อความแจ้งเตือนลง TempData
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสมาชิกหรือของรางวัลที่ต้องการแลก";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        string resolvedPhone = !string.IsNullOrWhiteSpace(contactPhone)
            ? contactPhone.Trim()
            : (user.Phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(resolvedPhone))
        {
            TempData["ErrorMessage"] = "กรุณาระบุเบอร์โทรศัพท์สำหรับติดต่อแจ้งรับของรางวัล";
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบว่าสถานะของรางวัลถูกปิดให้บริการหรือสต็อกหมดหรือไม่
        if (reward.IsAvailable == false || reward.StockQuantity <= 0)
        {
            // บันทึกข้อความแจ้งเตือนว่าสินค้าหมด
            TempData["ErrorMessage"] = "ขออภัย ของรางวัลชิ้นนี้หมดสต็อกชั่วคราว";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบว่าแต้มสะสมของสมาชิกน้อยกว่าแต้มที่ต้องใช้หรือไม่
        if (user.Points < reward.PointsRequired)
        {
            // บันทึกข้อความแจ้งเตือนพร้อมระบุแต้มที่ขาด
            TempData["ErrorMessage"] = $"แต้มสะสมของคุณไม่เพียงพอ (ต้องการ {reward.PointsRequired} แต้ม แต่คุณมี {user.Points} แต้ม)";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        // หักแต้มสะสมของสมาชิกตามจำนวนแต้มที่รางวัลต้องการ และจองสต็อกของรางวัลไว้ 1 ชิ้นทันที
        user.Points -= reward.PointsRequired;
        reward.StockQuantity = Math.Max(0, reward.StockQuantity - 1);

        // สุ่มตัวเลข 6 หลักเพื่อใช้สร้างรหัสรับของรางวัล
        int randomSuffix = new Random().Next(100000, 999999);
        // ประกอบรหัสรับของรางวัลในรูปแบบ RWD-XXXXXX
        string redemptionCode = "RWD-" + randomSuffix;

        // สร้างออบเจกต์บันทึกการจองแลกของรางวัลใหม่
        var redemption = new Rewardredemption
        {
            // กำหนดรหัสรับของรางวัลที่สุ่มขึ้นมา
            RedemptionCode = redemptionCode,
            // บันทึกรหัสสมาชิกผู้ทำการแลก
            UserId = user.UserId,
            // บันทึกรหัสของรางวัลที่ถูกแลก
            RewardId = reward.RewardId,
            // บันทึกจำนวนแต้มที่สมาชิกใช้ไป
            PointsSpent = reward.PointsRequired,
            // บันทึกสถานะเริ่มต้นเป็นรอมารับของหน้าร้าน
            Status = "PendingPickup",
            // บันทึกวันและเวลาปัจจุบันที่ทำรายการแลก
            RedeemedAt = DateTime.Now
        };
        // เพิ่มข้อมูลการแลกของรางวัลลงใน DbSet ของ Context
        _db.Rewardredemptions.Add(redemption);

        var metaPayload = new RedemptionMetaPayload
        {
            RedemptionCode = redemptionCode,
            PickupDate = estPickupDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ContactPhone = resolvedPhone
        };
        string pickupThai = estPickupDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("th-TH"));

        // บันทึกประวัติการจองแลกของรางวัล พร้อมวันนัดรับและเบอร์ติดต่อลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = $"RWD-{randomSuffix}-{Random.Shared.Next(100, 999)}",
            LogLevel = "INFO",
            Module = "reward.redeem",
            Message = $"สมาชิก {user.FullName ?? user.Email} จองแลกของรางวัล \"{reward.Title}\" รหัส #{redemptionCode} ใช้ {reward.PointsRequired:N0} แต้ม (นัดรับ: {pickupThai} · โทร: {resolvedPhone})",
            DetailPayload = JsonSerializer.Serialize(metaPayload),
            UserId = user.UserId,
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // บันทึกข้อความแจ้งเตือนสีเขียวพร้อมรหัสรับของรางวัลและวันนัดรับลง TempData
        TempData["SuccessMessage"] = $"จองแลกของรางวัล \"{reward.Title}\" สำเร็จ! รหัสรับของรางวัลคือ {redemptionCode} (นัดรับ {pickupThai} · โทร {resolvedPhone})";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการของรางวัล
        return RedirectToAction("Rewards");
    }

    // DELETE/UPDATE: ยกเลิกการจองแลกของรางวัลของสมาชิก พร้อมคืนแต้มสะสมและคืนสต็อกของรางวัล (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelRedemption(long redemptionId, string? returnAction)
    {
        var userId = GetCurrentCustomerId();
        string targetAction = returnAction == "Rewards" ? "Rewards" : "PreorderStatus";

        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการยกเลิกการแลกของรางวัล";
            return RedirectToAction("Login", "Account");
        }

        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId && r.UserId == userId.Value);

        if (redemption == null)
        {
            TempData["ErrorMessage"] = "ไม่พบรายการจองแลกของรางวัลที่คุณต้องการยกเลิก";
            return RedirectToAction(targetAction);
        }

        if (redemption.Status != "PendingPickup")
        {
            TempData["ErrorMessage"] = $"ไม่สามารถยกเลิกรายการ #{redemption.RedemptionCode} ได้ เนื่องจากดำเนินการเสร็จสิ้นหรือยกเลิกไปแล้ว";
            return RedirectToAction(targetAction);
        }

        // ปรับสถานะเป็นยกเลิก พร้อมคืนแต้มให้สมาชิกและคืนสต็อกของรางวัล
        redemption.Status = "Cancelled";
        if (redemption.User != null)
        {
            redemption.User.Points += redemption.PointsSpent;
        }
        if (redemption.Reward != null)
        {
            redemption.Reward.StockQuantity += 1;
        }

        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "RWD-CANCEL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "INFO",
            Module = "reward.cancel",
            Message = $"สมาชิกยกเลิกการจองแลกของรางวัล #{redemption.RedemptionCode} ({redemption.Reward?.Title}) คืน {redemption.PointsSpent:N0} แต้ม และคืนสต็อกของรางวัลแล้ว",
            UserId = userId.Value,
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"ยกเลิกการจองแลกของรางวัล #{redemption.RedemptionCode} สำเร็จ ระบบคืน {redemption.PointsSpent:N0} แต้มสะสมและคืนสิทธิ์สต็อกให้เรียบร้อยแล้ว";
        return RedirectToAction(targetAction);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
