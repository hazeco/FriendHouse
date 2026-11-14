using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.Helpers;
using Microsoft.VisualBasic;
using Khetsophon_CSI402_8456.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Khetsophon_CSI402_8456.Controllers;

public class AccountController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IWebHostEnvironment _env;

    public AccountController(Csi402dbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }
    
    // CREATE: แสดงแบบฟอร์มลงทะเบียนสมาชิกใหม่ (GET Method)
    public IActionResult Register()
    {
        return View();
    }

    // CREATE: ดำเนินการสมัครสมาชิกและบันทึกผู้ใช้ใหม่ลงฐานข้อมูล (POST Method)
    // WHY: ตรวจสอบความถูกต้องของ RegisterViewModel แฮชรหัสผ่านด้วย BCrypt และกำหนดสิทธิ์เริ่มต้นเป็น customer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel data)
    {
        if (ModelState.IsValid)
        {
            // ขั้นตอนที่ 1: ตรวจสอบว่ามีอีเมลนี้อยู่ในระบบฐานข้อมูลแล้วหรือไม่
            var checkUser = _db.Users.FirstOrDefault(u => u.Email == data.Email);
            
            if (checkUser == null)
            {
                // ขั้นตอนที่ 2: สร้างออบเจกต์ผู้ใช้งานใหม่พร้อมแฮชรหัสผ่านด้วย BCrypt
                User newUser =  new User();
                newUser.UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss");
                newUser.Email = data.Email;
                newUser.Phone = data.Phone;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password);
                newUser.FullName = string.IsNullOrWhiteSpace(data.FullName) ? "สมาชิกใหม่" : data.FullName.Trim();
                newUser.AvatarUrl = null;
                newUser.RoleName = "customer";
                newUser.Points = 100;
                newUser.IsActive = true;
                newUser.CreatedAt = DateTime.UtcNow;
                _db.Users.Add(newUser);
                _db.SaveChanges();

                // ขั้นตอนที่ 3: ส่งข้อความแจ้งเตือนสำเร็จและเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
                TempData["SuccessMessage"] = "สมัครสมาชิกสำเร็จ สามารถเข้าสู่ระบบได้ทันที";
                return RedirectToAction("Login", "Account");
            } else
            {
                ViewBag.Status = "อีเมลนี้มีอยู่ในระบบแล้ว กรุณาใช้อีเมลอื่น";
                return View(data);
            }
        }
        return View(data);
    }

    // READ: แสดงแบบฟอร์มเข้าสู่ระบบ (GET Method)
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // READ/AUTHENTICATE: ตรวจสอบข้อมูลอีเมลและรหัสผ่านเพื่อเข้าสู่ระบบ (POST Method)
    // WHY: ตรวจสอบความถูกต้องของ LoginViewModel ยืนยันรหัสผ่านด้วย BCrypt และจัดเก็บ Session ตามบทบาท
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel data)
    {
        if (ModelState.IsValid)
        {
            // ขั้นตอนที่ 1: ค้นหาผู้ใช้จาก Email ในฐานข้อมูล
            var user = _db.Users.FirstOrDefault(u => u.Email == data.Email);
            
            // ตรวจสอบรหัสผ่านด้วย BCrypt
            if (user != null && BCrypt.Net.BCrypt.Verify(data.Password, user.PasswordHash))
            {
                // ตรวจสอบว่าบัญชีผู้ใช้งานถูกระงับหรือยกเลิกการใช้งานหรือไม่
                if (user.IsActive == false)
                {
                    // แจ้งเตือนกรณีบัญชีถูกระงับการใช้งาน
                    ViewBag.Status = "บัญชีผู้ใช้งานนี้ถูกระงับหรือยกเลิกการใช้งานแล้ว กรุณาติดต่อผู้ดูแลระบบ";
                    // ส่งกลับหน้าเข้าสู่ระบบพร้อมข้อมูลเดิม
                    return View(data);
                }

                // ขั้นตอนที่ 2: ล้าง Session เก่าก่อนบันทึกสถานะการเข้าสู่ระบบใหม่เพื่อป้องกัน Role ปะปน
                ClearAllAuthSession();
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Role", user.RoleName);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

                // บันทึก Session เฉพาะบทบาทที่เข้าสู่ระบบ
                // แปลงชื่อบทบาทผู้ใช้งานเป็นตัวพิมพ์เล็กเพื่อตรวจสอบสิทธิ์
                string r = user.RoleName.ToLowerInvariant();
                // กรณีผู้ใช้มีบทบาทลูกค้าทั่วไป (Customer)
                if (r == "customer")
                {
                    // กำหนดรหัสผู้ใช้และชื่อลูกค้าลงใน Session
                    HttpContext.Session.SetInt32("CustomerUserId", user.UserId);
                    HttpContext.Session.SetString("CustomerUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทพนักงานหน้าร้าน (Staff)
                else if (r == "staff")
                {
                    // กำหนดรหัสพนักงานและชื่อพนักงานลงใน Session
                    HttpContext.Session.SetInt32("StaffUserId", user.UserId);
                    HttpContext.Session.SetString("StaffUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทเจ้าของร้าน (Owner)
                else if (r == "owner")
                {
                    // กำหนดรหัสเจ้าของร้านและชื่อเจ้าของร้านลงใน Session
                    HttpContext.Session.SetInt32("OwnerUserId", user.UserId);
                    HttpContext.Session.SetString("OwnerUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทฝ่ายบัญชีและการเงิน (Accountant)
                else if (r == "accountant")
                {
                    // กำหนดรหัสฝ่ายบัญชีและชื่อฝ่ายบัญชีลงใน Session
                    HttpContext.Session.SetInt32("AccountantUserId", user.UserId);
                    HttpContext.Session.SetString("AccountantUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทฝ่ายสนับสนุนไอที (IT Support)
                else if (r == "itsupport")
                {
                    // กำหนดรหัสฝ่ายไอทีและชื่อฝ่ายไอทีลงใน Session
                    HttpContext.Session.SetInt32("ITSupportUserId", user.UserId);
                    HttpContext.Session.SetString("ITSupportUserName", user.FullName ?? user.Email);
                }
                
                // ขั้นตอนที่ 3: บันทึก Log ประวัติการเข้าสู่ระบบลงตาราง Systemlogs
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัส Log
                    LogCode = PromoAndRedemptionHelper.GenerateLogCode("AUTH"),
                    // กำหนดระดับความสำคัญเป็น INFO
                    LogLevel = "INFO",
                    // กำหนดโมดูลที่เกี่ยวข้อง
                    Module = "auth.login",
                    // ข้อความรายละเอียดการเข้าสู่ระบบ
                    Message = $"ผู้ใช้งานเข้าสู่ระบบสำเร็จ: {user.Email} สิทธิ์การใช้งาน: {user.RoleName}",
                    // รหัสผู้ใช้งานที่เข้าสู่ระบบ
                    UserId = user.UserId,
                    // วันเวลาที่ทำการเข้าสู่ระบบ
                    Timestamp = DateTime.Now
                });
                // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
                _db.SaveChanges();

                // ขั้นตอนที่ 4: เข้าสู่ระบบสำเร็จ นำทางไปยังหน้าแรกของเว็บไซต์
                return RedirectToAction("Index", "Home" );
            }
            else
            {
                ViewBag.Status = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง";
                return View(data);
            }
        }
        return View(data);
    }

    // AUTHENTICATE: ระบบล็อกอินด่วนสำหรับการทดสอบระบบและนำเสนองาน (GET Method)
    // WHY: ออกแบบมาเพื่อให้อาจารย์และผู้ตรวจสามารถสลับบทบาทได้ในคลิกเดียว
    // WHY SESSION: แม้จะเป็นการล็อกอินด่วนสำหรับการทดสอบ แต่จำเป็นต้องบันทึก Session จริง
    // เพื่อให้ตัวตรวจสิทธิ์ใน Controller แต่ละบทบาทอนุญาตให้เข้าใช้งานได้จริง
    [HttpGet]
    public IActionResult FastLogin(string role)
    {
        // ปรับบทบาทผู้ใช้งานเป็นตัวพิมพ์เล็กเพื่อความปลอดภัย
        string normalizedRole = (role ?? "customer").ToLowerInvariant();
        
        // กำหนดค่าจำลองตั้งต้นสำหรับบทบาทลูกค้า
        int mockUserId = 1;
        // กำหนดอีเมลจำลองตั้งต้น
        string email = "customer@friendhouse.com";
        // กำหนดชื่อผู้ใช้งานจำลองตั้งต้น
        string name = "ลูกค้าทดสอบ";
        // กำหนดแอ็กชันปลายทาง
        string targetAction = "Customer";
        // กำหนดคอนโทรลเลอร์ปลายทาง
        string targetController = "Home";

        // ตรวจสอบและตั้งค่าผู้ใช้งานจำลองตามบทบาทที่เลือก
        switch (normalizedRole)
        {
            // กรณีบทบาทพนักงานหน้าร้าน
            case "staff":
                // รหัสผู้ใช้พนักงาน
                mockUserId = 2;
                // อีเมลพนักงาน
                email = "staff@friendhouse.com";
                // ชื่อพนักงาน
                name = "พนักงานหน้าร้าน";
                // คอนโทรลเลอร์ปลายทางฝ่ายพนักงาน
                targetController = "Staff";
                // หน้าเริ่มต้นฝ่ายพนักงาน
                targetAction = "Index";
                break;

            // กรณีบทบาทเจ้าของร้าน
            case "owner":
                // รหัสผู้ใช้เจ้าของร้าน
                mockUserId = 3;
                // อีเมลเจ้าของร้าน
                email = "owner@friendhouse.com";
                // ชื่อเจ้าของร้าน
                name = "เจ้าของร้าน";
                // คอนโทรลเลอร์ปลายทางฝ่ายเจ้าของร้าน
                targetController = "Owner";
                // หน้าเริ่มต้นฝ่ายเจ้าของร้าน
                targetAction = "Promotions";
                break;

            // กรณีบทบาทผู้ดูแลระบบไอที
            case "itsupport":
                // รหัสผู้ใช้ฝ่ายไอที
                mockUserId = 4;
                // อีเมลฝ่ายไอที
                email = "it@friendhouse.com";
                // ชื่อฝ่ายไอที
                name = "เจ้าหน้าที่ไอที";
                // คอนโทรลเลอร์ปลายทางฝ่ายไอที
                targetController = "ITSupport";
                // หน้าเริ่มต้นฝ่ายไอที
                targetAction = "Dashboard";
                break;

            // กรณีบทบาทนักบัญชีและการเงิน
            case "accountant":
                // รหัสผู้ใช้นักบัญชี
                mockUserId = 5;
                // อีเมลนักบัญชี
                email = "account@friendhouse.com";
                // ชื่อนักบัญชี
                name = "นักบัญชี";
                // คอนโทรลเลอร์ปลายทางฝ่ายบัญชี
                targetController = "Accountant";
                // หน้าเริ่มต้นฝ่ายบัญชี
                targetAction = "Index";
                break;

            // กรณีบทบาทลูกค้าทั่วไปหรือบทบาทอื่นๆ
            default:
                // คืนค่าบทบาทเป็นลูกค้า
                normalizedRole = "customer";
                break;
        }

        // WHY: ค้นหาบัญชีที่ยังเปิดใช้งานในฐานข้อมูลก่อน หากถูกระงับทั้งหมดจะไม่ให้เข้าสู่ระบบ
        var dbUser = _db.Users.FirstOrDefault(u => u.RoleName.ToLower() == normalizedRole && u.IsActive != false);
        if (dbUser == null && _db.Users.Any(u => u.RoleName.ToLower() == normalizedRole))
        {
            TempData["ErrorMessage"] = $"ไม่พบบัญชีบทบาท {normalizedRole} ที่เปิดใช้งานอยู่";
            return RedirectToAction("Login", "Account");
        }
        if (dbUser != null)
        {
            mockUserId = dbUser.UserId;
            email = dbUser.Email;
            name = dbUser.FullName ?? dbUser.Email;
        }

        // ล้าง Session บทบาทเดิมทั้งหมดก่อนเข้าสู่ระบบบทบาทใหม่ (ป้องกัน Session ปะปนข้ามบทบาท)
        ClearAllAuthSession();

        // บันทึก Session จำเป็นสำหรับใช้ตรวจสอบทุกหน้า
        HttpContext.Session.SetInt32("UserId", mockUserId);
        HttpContext.Session.SetString("Role", normalizedRole);
        HttpContext.Session.SetString("UserEmail", email);
        HttpContext.Session.SetString("UserName", name);

        // บันทึก Session เฉพาะบทบาทที่เลือก
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
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("AUTH"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "auth.login",
            // ข้อความรายละเอียด
            Message = $"ผู้ใช้งานเข้าสู่ระบบด่วนสำเร็จ: {email} สิทธิ์การใช้งาน: {normalizedRole}",
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

    // ฟังก์ชันช่วยล้างค่า Session การยืนยันตัวตนทั้งหมดออกจากระบบ
    private void ClearAllAuthSession()
    {
        // รายการคีย์เซสชันทั้งหมดที่เกี่ยวข้องกับการเข้าสู่ระบบและโปรโมชั่น
        string[] keys = {
            "UserId", "Role", "UserEmail", "UserName",
            "CustomerUserId", "CustomerUserName",
            "StaffUserId", "StaffUserName",
            "OwnerUserId", "OwnerUserName",
            "AccountantUserId", "AccountantUserName",
            "ITSupportUserId", "ITSupportUserName",
            PromoAndRedemptionHelper.AppliedPromoSessionKey,
            PromoAndRedemptionHelper.CollectedPromoSessionKey
        };
        // วนลูปเพื่อลบแต่ละคีย์ออกจาก Session ของผู้ใช้งาน
        foreach (var k in keys)
        {
            // ลบค่าเซสชันตามชื่อคีย์
            HttpContext.Session.Remove(k);
        }
    }

    // ฟังก์ชันช่วยดึงรหัสผู้ใช้งานปัจจุบันที่ผ่านการยืนยันตัวตน
    private int? GetAuthenticatedUserId()
    {
        // อ่านรหัสผู้ใช้ทั่วไปหรือรหัสลูกค้าจากเซสชัน
        return HttpContext.Session.GetInt32("UserId")
            ?? HttpContext.Session.GetInt32("CustomerUserId");
    }

    // Action สำหรับออกจากระบบ (Logout) และล้าง Session ทั้งหมดอย่างสะอาด
    public IActionResult Logout(string? role)
    {
        // ล้างข้อมูลทั้งหมดใน Session
        HttpContext.Session.Clear();
        // ตรวจสอบว่าผู้ใช้งานออกจากระบบในบทบาทลูกค้าหรือไม่
        if (string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
        {
            // เปลี่ยนเส้นทางกลับไปหน้าแรกสำหรับลูกค้า
            return RedirectToAction("Index", "Home");
        }
        // เปลี่ยนเส้นทางกลับไปหน้าเข้าสู่ระบบสำหรับเจ้าหน้าที่
        return RedirectToAction("Login", "Account");
    }

    // READ: แสดงหน้าแบบฟอร์มเปลี่ยนรหัสผ่านของสมาชิก (GET Method)
    [HttpGet]
    public IActionResult ResetPassword()
    {
        return View(new ResetPasswordViewModel());
    }

    // UPDATE: ดำเนินการเปลี่ยนรหัสผ่านของสมาชิกผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: ตรวจสอบความถูกต้องของโมเดล ยืนยันรหัสผ่านเดิมด้วย BCrypt และแฮชรหัสผ่านใหม่ก่อนบันทึกลงฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        // ขั้นตอนที่ 1: ตรวจสอบความถูกต้องตาม DataAnnotations
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // ขั้นตอนที่ 2: ดึงรหัสสมาชิกจาก Session เพื่อยืนยันตัวตน
        var userId = GetAuthenticatedUserId();

        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการเปลี่ยนรหัสผ่าน";
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 3: ค้นหาข้อมูลสมาชิกในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        if (user == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 4: ตรวจสอบรหัสผ่านปัจจุบันด้วย BCrypt.Net.BCrypt.Verify
        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "รหัสผ่านปัจจุบันไม่ถูกต้อง");
            return View(model);
        }

        // ขั้นตอนที่ 5: แฮชรหัสผ่านใหม่และบันทึกลงฐานข้อมูล MySQL
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        _db.SaveChanges();

        // ขั้นตอนที่ 6: แจ้งเตือนข้อความสำเร็จและนำทางไปยังหน้าโปรไฟล์
        TempData["SuccessMessage"] = "เปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
        return RedirectToAction("Profile");
    }

    // READ: แสดงหน้าโปรไฟล์สมาชิก (Profile)
    // WHY: ส่งผ่าน UserProfileViewModel แยกความรับผิดชอบออกจาก Domain Model ป้องกันปัญหา Over-posting
    public IActionResult Profile()
    {
        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetAuthenticatedUserId();

        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบเพื่อดูโปรไฟล์ของคุณ";
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: ดึงข้อมูลสมาชิกจากฐานข้อมูล MySQL
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        if (user == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 3: คำนวณจำนวนพรีออเดอร์ที่รอดำเนินการ
        int pendingCount = _db.Preorders.Count(p => p.UserId == user.UserId && p.CurrentStatus != "Completed" && p.CurrentStatus != "Cancelled");

        // ขั้นตอนที่ 4: ประกอบข้อมูล UserProfileViewModel
        var viewModel = new UserProfileViewModel
        {
            UserId = user.UserId,
            UserCode = user.UserCode,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Points = user.Points,
            PendingPreorderCount = pendingCount,
            RoleName = user.RoleName
        };

        // กำหนดค่า ViewBag สำหรับความเข้ากันได้
        ViewBag.PendingPreorderCount = pendingCount;

        return View(viewModel);
    }

    // UPDATE: แก้ไขข้อมูลส่วนตัวของสมาชิกผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รับค่าผ่าน UserProfileViewModel อัปเดตข้อมูลชื่อ เบอร์โทรศัพท์ และรูปโปรไฟล์ลง MySQL Database โดยตรง และปรับปรุง Session
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(UserProfileViewModel model, string? fullName = null, string? phone = null, string? avatarUrl = null, IFormFile? avatarFile = null)
    {
        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetAuthenticatedUserId();

        // ตรวจสอบว่าผู้ใช้งานเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่ได้เข้าสู่ระบบ
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: ค้นหาข้อมูลสมาชิกในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ตรวจสอบว่าพบข้อมูลผู้ใช้งานในระบบหรือไม่
        if (user == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานในระบบ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // อ่านค่าชื่อ-นามสกุลจาก ViewModel หรือพารามิเตอร์ฟอร์ม
        var updatedFullName = !string.IsNullOrWhiteSpace(model?.FullName) ? model.FullName : fullName;
        // อ่านค่าเบอร์โทรศัพท์จาก ViewModel หรือพารามิเตอร์ฟอร์ม
        var updatedPhone = !string.IsNullOrWhiteSpace(model?.Phone) ? model.Phone : phone;
        // อ่านค่ารูปโปรไฟล์จาก ViewModel หรือพารามิเตอร์ฟอร์ม
        var updatedAvatar = !string.IsNullOrWhiteSpace(model?.AvatarUrl) ? model.AvatarUrl : avatarUrl;

        // ขั้นตอนที่ 3: ปรับปรุงข้อมูลชื่อ-นามสกุล
        if (!string.IsNullOrWhiteSpace(updatedFullName))
        {
            // กำหนดชื่อ-นามสกุลใหม่โดยตัดช่องว่างหัวท้าย
            user.FullName = updatedFullName.Trim();
            // อัปเดตชื่อผู้ใช้งานใน Session หลัก
            HttpContext.Session.SetString("UserName", user.FullName);
            // อัปเดตชื่อผู้ใช้งานใน Session ของลูกค้า
            HttpContext.Session.SetString("CustomerUserName", user.FullName);
        }

        // ขั้นตอนที่ 4: ปรับปรุงหมายเลขโทรศัพท์ติดต่อ
        if (!string.IsNullOrWhiteSpace(updatedPhone))
        {
            // กำหนดเบอร์โทรศัพท์ใหม่โดยตัดช่องว่างหัวท้าย
            user.Phone = updatedPhone.Trim();
        }

        // ขั้นตอนที่ 5: บันทึกไฟล์รูปโปรไฟล์จริงลง wwwroot/media/images/ (ไม่อิง Base64)
        string? candidateAvatarUrl = (!string.IsNullOrWhiteSpace(updatedAvatar) && !updatedAvatar.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            ? updatedAvatar.Trim()
            : user.AvatarUrl;

        string? savedAvatarUrl = PromoAndRedemptionHelper.SaveUploadedImage(avatarFile, _env.WebRootPath, "avatar", candidateAvatarUrl);
        if (!string.IsNullOrWhiteSpace(savedAvatarUrl))
        {
            user.AvatarUrl = savedAvatarUrl;
        }

        // บันทึกประวัติการแก้ไขโปรไฟล์ลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = $"PROF-{DateTime.Now:yyyyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}",
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "account.profile",
            // ข้อความรายละเอียดการแก้ไขข้อมูลส่วนตัว
            Message = $"สมาชิกแก้ไขข้อมูลโปรไฟล์ส่วนตัว: {user.Email}",
            // รหัสสมาชิกผู้ทำรายการ
            UserId = user.UserId,
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จและเปลี่ยนเส้นทางกลับไปยังหน้าโปรไฟล์
        TempData["SuccessMessage"] = "บันทึกข้อมูลส่วนตัวเรียบร้อยแล้ว";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าโปรไฟล์ของฉัน
        return RedirectToAction("Profile");
    }

    // DELETE: ยกเลิกและลบบัญชีสมาชิกของตนเองผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รองรับการทำงานครบวงจร User Full CRUD สำหรับบทบาทลูกค้า โดยทำการลบตะกร้าสินค้าและปิดสถานะบัญชีเพื่อรักษาความสมบูรณ์ของ Foreign Key
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAccount()
    {
        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetAuthenticatedUserId();

        // ตรวจสอบว่าผู้ใช้งานเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // แจ้งเตือนกรณีไม่ได้เข้าสู่ระบบ
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: ค้นหาข้อมูลสมาชิกในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ตรวจสอบว่าพบข้อมูลผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบบัญชี
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 3: ลบรายการสินค้าในตะกร้าของสมาชิกคนนี้ออกทั้งหมด
        var cartItems = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
        if (cartItems.Any())
        {
            // สั่งลบรายการในตะกร้าทั้งหมด
            _db.Cartitems.RemoveRange(cartItems);
        }

        // ขั้นตอนที่ 4: ปิดการใช้งานบัญชี (Soft Delete เพื่อไม่ให้กระทบประวัติใบจองและใบเสร็จในฐานข้อมูล)
        user.IsActive = false;

        // ขั้นตอนที่ 5: บันทึกประวัติการยกเลิกบัญชีลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("ACC-DEL"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "account.delete",
            // ข้อความรายละเอียดการยกเลิกบัญชี
            Message = $"สมาชิกดำเนินการยกเลิกและลบบัญชีผู้ใช้งาน: {user.Email}",
            // รหัสสมาชิกผู้ทำรายการ
            UserId = user.UserId,
            // วันเวลาที่ทำการบันทึก
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // ขั้นตอนที่ 6: ล้างข้อมูลเซสชันของสมาชิกและนำทางไปยังหน้าเข้าสู่ระบบ
        HttpContext.Session.Clear();
        // กำหนดข้อความแจ้งเตือนผลสำเร็จ
        TempData["SuccessMessage"] = "ลบบัญชีผู้ใช้งานของคุณเรียบร้อยแล้ว ขอบคุณที่ใช้บริการ FriendHouse";
        // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
        return RedirectToAction("Login", "Account");
    }
}
