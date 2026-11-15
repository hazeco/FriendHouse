using System.Text.Json;
using System.Globalization;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;
using Microsoft.VisualBasic;
using Khetsophon_CSI402_8456.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Khetsophon_CSI402_8456.Controllers;

// ตัวควบคุมระบบบัญชีผู้ใช้งาน การเข้าสู่ระบบ และโปรไฟล์
public class AccountController : Controller
{
    // อ็อบเจกต์ DbContext สำหรับเข้าถึงฐานข้อมูล MySQL
    private readonly Csi402dbContext _db;
    // อ็อบเจกต์ IWebHostEnvironment สำหรับเข้าถึงสภาพแวดล้อมและพาธโฮสต์
    private readonly IWebHostEnvironment _env;

    // เมธอด Constructor กำหนดค่า DbContext และ Hosting Environment
    public AccountController(Csi402dbContext db, IWebHostEnvironment env)
    {
        // กำหนดค่า DbContext ใช้งานภายในตัวควบคุม
        _db = db;
        // กำหนดค่า Hosting Environment ใช้งานภายในตัวควบคุม
        _env = env;
    }
    
    // CREATE: แสดงแบบฟอร์มลงทะเบียนสมาชิกใหม่ (GET Method)
    public IActionResult Register()
    {
        // แสดงผลหน้า View ที่เกี่ยวข้อง
        return View();
    }

    // CREATE: ดำเนินการสมัครสมาชิกและบันทึกผู้ใช้ใหม่ลงฐานข้อมูล (POST Method)
    // WHY: ตรวจสอบความถูกต้องของ RegisterViewModel แฮชรหัสผ่านด้วย BCrypt และกำหนดสิทธิ์เริ่มต้นเป็น customer
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับประมวลผลการสมัครสมาชิกใหม่
    public IActionResult Register(RegisterViewModel data)
    {
        // ตรวจสอบความถูกต้องของข้อมูลตามกฎ Validation
        if (ModelState.IsValid)
        {
            // ขั้นตอนที่ 1: ตรวจสอบว่ามีอีเมลนี้อยู่ในระบบฐานข้อมูลแล้วหรือไม่
            var checkUser = _db.Users.FirstOrDefault(u => u.Email == data.Email);
            // หากพบบัญชีเดิมที่ถูกระงับหรือลบแบบ Soft Delete ไปแล้ว ให้ปลดล็อกอีเมลเดิมเพื่อให้สมัครใหม่ได้ทันที
            if (checkUser != null && checkUser.IsActive == false)
            {
                // เปลี่ยนอีเมลของเรคอร์ดที่ปิดใช้งานแล้วเป็นอีเมลเก็บถาวรเพื่อคืนสิทธิ์อีเมลให้ผู้สมัครใหม่
                checkUser.Email = $"deleted_{checkUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
                // บันทึกการปลดล็อกอีเมลลงฐานข้อมูล
                _db.SaveChanges();
                // ล้างตัวแปรตรวจสอบเพื่อให้เข้าสู่ขั้นตอนสร้างบัญชีใหม่
                checkUser = null;
            }
            
            // ตรวจสอบว่าไม่พบอีเมลซ้ำในระบบ
            if (checkUser == null)
            {
                // ขั้นตอนที่ 2: สร้างออบเจกต์ผู้ใช้งานใหม่พร้อมแฮชรหัสผ่านด้วย BCrypt
                User newUser =  new User();
                // สร้างรหัสสมาชิกใหม่ตามวันเวลา
                newUser.UserCode = "U" + DateTime.Now.ToString("yyyyMMddHHmmss");
                // กำหนดอีเมลสมาชิกใหม่
                newUser.Email = data.Email;
                // กำหนดเบอร์โทรศัพท์สมาชิกใหม่
                newUser.Phone = data.Phone;
                // เข้ารหัสผ่านด้วย BCrypt ปลอดภัยสูง
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password);
                // กำหนดชื่อ-นามสกุลสมาชิก
                newUser.FullName = string.IsNullOrWhiteSpace(data.FullName) ? "สมาชิกใหม่" : data.FullName.Trim();
                // กำหนดรูปประจำตัวเริ่มต้นเป็นค่าว่าง
                newUser.AvatarUrl = null;
                // กำหนดบทบาทเริ่มต้นเป็นลูกค้าทั่วไป
                newUser.RoleName = "customer";
                // มอบแต้มสะสมเริ่มต้น 100 แต้ม
                newUser.Points = 100;
                // กำหนดสถานะเปิดใช้งานบัญชี
                newUser.IsActive = true;
                // กำหนดวันเวลาที่สมัครสมาชิก
                newUser.CreatedAt = DateTime.UtcNow;
                // เพิ่มข้อมูลสมาชิกใหม่ลงในฐานข้อมูล
                _db.Users.Add(newUser);
                // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
                _db.SaveChanges();

                // บันทึกประวัติการสมัครสมาชิกใหม่ลงตาราง Systemlogs ตามมาตรฐานระบบ
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัส Log
                    LogCode = GenerateLogCode("REG"),
                    // กำหนดระดับความสำคัญเป็น INFO
                    LogLevel = "INFO",
                    // กำหนดโมดูลการลงทะเบียน
                    Module = "auth.register",
                    // ข้อความรายละเอียดการสมัครสมาชิก
                    Message = $"สมัครสมาชิกใหม่สำเร็จ: {newUser.Email} ({newUser.FullName})",
                    // รหัสผู้ใช้งานที่เพิ่งสมัคร
                    UserId = newUser.UserId,
                    // วันเวลาที่ทำรายการ
                    Timestamp = DateTime.Now
                });
                // บันทึก Log ลงฐานข้อมูล
                _db.SaveChanges();

                // ขั้นตอนที่ 3: ส่งข้อความแจ้งเตือนสำเร็จและเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
                TempData["SuccessMessage"] = "สมัครสมาชิกสำเร็จ สามารถเข้าสู่ระบบได้ทันที";
                // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
                return RedirectToAction("Login", "Account");
            }
            // มิฉะนั้น (กรณีอีเมลซ้ำ)
            else
            {
                // กำหนดข้อความแจ้งเตือนกรณีอีเมลซ้ำ
                ViewBag.Status = "อีเมลนี้มีอยู่ในระบบแล้ว กรุณาใช้อีเมลอื่น";
                // แสดงหน้าเดิมพร้อมข้อมูลและข้อผิดพลาด
                return View(data);
            }
        }
        // แสดงหน้าเดิมพร้อมข้อมูลและข้อผิดพลาด
        return View(data);
    }

    // READ: แสดงแบบฟอร์มเข้าสู่ระบบ (GET Method)
    [HttpGet]
    // แสดงแบบฟอร์มหน้าเข้าสู่ระบบ
    public IActionResult Login()
    {
        // แสดงผลหน้า View ที่เกี่ยวข้อง
        return View();
    }

    // READ/AUTHENTICATE: ตรวจสอบข้อมูลอีเมลและรหัสผ่านเพื่อเข้าสู่ระบบ (POST Method)
    // WHY: ตรวจสอบความถูกต้องของ LoginViewModel ยืนยันรหัสผ่านด้วย BCrypt และจัดเก็บ Session ตามบทบาท
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับประมวลผลการเข้าสู่ระบบ
    public IActionResult Login(LoginViewModel data)
    {
        // ตรวจสอบความถูกต้องของข้อมูลตามกฎ Validation
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
                    // บันทึก Log ระดับ ERROR เมื่อมีความพยายามเข้าสู่ระบบด้วยบัญชีที่ถูกระงับ
                    _db.Systemlogs.Add(new Systemlog
                    {
                        // กำหนดรหัส Log
                        LogCode = GenerateLogCode("AUTH-ERR"),
                        // กำหนดระดับความสำคัญเป็น ERROR
                        LogLevel = "ERROR",
                        // กำหนดโมดูลการยืนยันตัวตน
                        Module = "auth.login",
                        // ข้อความรายละเอียดข้อผิดพลาด
                        Message = $"ปฏิเสธการเข้าสู่ระบบเนื่องจากบัญชีถูกระงับการใช้งาน: {user.Email}",
                        // รหัสผู้ใช้งานที่พยายามเข้าสู่ระบบ
                        UserId = user.UserId,
                        // วันเวลาที่เกิดเหตุการณ์
                        Timestamp = DateTime.Now
                    });
                    // บันทึก Log ลงฐานข้อมูล
                    _db.SaveChanges();
                    // แจ้งเตือนกรณีบัญชีถูกระงับการใช้งาน
                    ViewBag.Status = "บัญชีผู้ใช้งานนี้ถูกระงับหรือยกเลิกการใช้งานแล้ว กรุณาติดต่อผู้ดูแลระบบ";
                    // ส่งกลับหน้าเข้าสู่ระบบพร้อมข้อมูลเดิม
                    return View(data);
                }

                // ขั้นตอนที่ 2: ล้าง Session เก่าก่อนบันทึกสถานะการเข้าสู่ระบบใหม่เพื่อป้องกัน Role ปะปน
                ClearAllAuthSession();
                // บันทึกรหัสผู้ใช้งานกลางลงในเซสชัน
                HttpContext.Session.SetInt32("UserId", user.UserId);
                // บันทึกชื่อบทบาทกลางลงในเซสชัน
                HttpContext.Session.SetString("Role", user.RoleName);
                // บันทึกอีเมลผู้ใช้กลางลงในเซสชัน
                HttpContext.Session.SetString("UserEmail", user.Email);
                // บันทึกชื่อผู้ใช้กลางลงในเซสชัน
                HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

                // บันทึก Session เฉพาะบทบาทที่เข้าสู่ระบบ
                // แปลงชื่อบทบาทผู้ใช้งานเป็นตัวพิมพ์เล็กเพื่อตรวจสอบสิทธิ์
                string r = user.RoleName.ToLowerInvariant();
                // กรณีผู้ใช้มีบทบาทลูกค้าทั่วไป (Customer)
                if (r == "customer")
                {
                    // กำหนดรหัสผู้ใช้และชื่อลูกค้าลงใน Session
                    HttpContext.Session.SetInt32("CustomerUserId", user.UserId);
                    // บันทึกชื่อผู้ใช้เฉพาะบทบาทลูกค้า
                    HttpContext.Session.SetString("CustomerUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทพนักงานหน้าร้าน (Staff)
                else if (r == "staff")
                {
                    // กำหนดรหัสพนักงานและชื่อพนักงานลงใน Session
                    HttpContext.Session.SetInt32("StaffUserId", user.UserId);
                    // บันทึกชื่อผู้ใช้เฉพาะบทบาทพนักงาน
                    HttpContext.Session.SetString("StaffUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทเจ้าของร้าน (Owner)
                else if (r == "owner")
                {
                    // กำหนดรหัสเจ้าของร้านและชื่อเจ้าของร้านลงใน Session
                    HttpContext.Session.SetInt32("OwnerUserId", user.UserId);
                    // บันทึกชื่อผู้ใช้เฉพาะบทบาทเจ้าของร้าน
                    HttpContext.Session.SetString("OwnerUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทฝ่ายบัญชีและการเงิน (Accountant)
                else if (r == "accountant")
                {
                    // กำหนดรหัสฝ่ายบัญชีและชื่อฝ่ายบัญชีลงใน Session
                    HttpContext.Session.SetInt32("AccountantUserId", user.UserId);
                    // บันทึกชื่อผู้ใช้เฉพาะบทบาทฝ่ายบัญชี
                    HttpContext.Session.SetString("AccountantUserName", user.FullName ?? user.Email);
                }
                // กรณีผู้ใช้มีบทบาทฝ่ายสนับสนุนไอที (IT Support)
                else if (r == "itsupport")
                {
                    // กำหนดรหัสฝ่ายไอทีและชื่อฝ่ายไอทีลงใน Session
                    HttpContext.Session.SetInt32("ITSupportUserId", user.UserId);
                    // บันทึกชื่อผู้ใช้เฉพาะบทบาทไอทีซัพพอร์ต
                    HttpContext.Session.SetString("ITSupportUserName", user.FullName ?? user.Email);
                }
                
                // ขั้นตอนที่ 3: บันทึก Log ประวัติการเข้าสู่ระบบลงตาราง Systemlogs
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัส Log
                    LogCode = GenerateLogCode("AUTH"),
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

                // ขั้นตอนที่ 4: เข้าสู่ระบบสำเร็จ นำทางไปยังหน้าหลักตามบทบาทของผู้ใช้งานทันที
                var (targetAction, targetController) = GetRoleHomeRoute(r);
                // เปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทนั้น
                return RedirectToAction(targetAction, targetController);
            }
            // มิฉะนั้น (กรณีอีเมลหรือรหัสผ่านไม่ถูกต้อง)
            else
            {
                // บันทึก Log ระดับ ERROR เมื่อผู้ใช้งานกรอกอีเมลหรือรหัสผ่านผิดพลาดตามมาตรฐานความปลอดภัย
                _db.Systemlogs.Add(new Systemlog
                {
                    // กำหนดรหัส Log ข้อผิดพลาดการเข้าสู่ระบบ
                    LogCode = GenerateLogCode("AUTH-FAIL"),
                    // กำหนดระดับความสำคัญเป็น ERROR
                    LogLevel = "ERROR",
                    // กำหนดโมดูลการยืนยันตัวตน
                    Module = "auth.login",
                    // ข้อความรายละเอียดการล็อกอินผิดพลาด
                    Message = $"เข้าสู่ระบบล้มเหลว (อีเมลหรือรหัสผ่านไม่ถูกต้อง): {data.Email}",
                    // ผูกรหัสผู้ใช้หากพบอีเมลในระบบ
                    UserId = user?.UserId,
                    // วันเวลาที่เกิดข้อผิดพลาด
                    Timestamp = DateTime.Now
                });
                // บันทึก Log ลงฐานข้อมูล MySQL
                _db.SaveChanges();
                // แจ้งเตือนข้อความรหัสผ่านไม่ถูกต้องบนหน้าจอ
                ViewBag.Status = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง";
                // แสดงหน้าเดิมพร้อมข้อมูลและข้อผิดพลาด
                return View(data);
            }
        }
        // แสดงหน้าเดิมพร้อมข้อมูลและข้อผิดพลาด
        return View(data);
    }

    // AUTHENTICATE: ระบบล็อกอินด่วนสำหรับการทดสอบระบบและนำเสนองาน (GET Method)
    // WHY: ออกแบบมาเพื่อให้อาจารย์และผู้ตรวจสามารถสลับบทบาทได้ในคลิกเดียว
    // WHY SESSION: แม้จะเป็นการล็อกอินด่วนสำหรับการทดสอบ แต่จำเป็นต้องบันทึก Session จริง
    // เพื่อให้ตัวตรวจสิทธิ์ใน Controller แต่ละบทบาทอนุญาตให้เข้าใช้งานได้จริง
    [HttpGet]
    // แอ็กชันเข้าสู่ระบบด่วนสำหรับทดสอบแต่ละบทบาท
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
                // สิ้นสุดกรณีการทำงาน
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
                // สิ้นสุดกรณีการทำงาน
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
                // สิ้นสุดกรณีการทำงาน
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
                // สิ้นสุดกรณีการทำงาน
                break;

            // กรณีบทบาทลูกค้าทั่วไปหรือบทบาทอื่นๆ
            default:
                // คืนค่าบทบาทเป็นลูกค้า
                normalizedRole = "customer";
                // สิ้นสุดกรณีการทำงาน
                break;
        }

        // WHY: ค้นหาบัญชีที่ยังเปิดใช้งานในฐานข้อมูลก่อน หากถูกระงับทั้งหมดจะไม่ให้เข้าสู่ระบบ
        var dbUser = _db.Users.FirstOrDefault(u => u.RoleName.ToLower() == normalizedRole && u.IsActive != false);
        // ตรวจสอบกรณีพบบัญชีแต่ถูกปิดใช้งาน
        if (dbUser == null && _db.Users.Any(u => u.RoleName.ToLower() == normalizedRole))
        {
            // กำหนดข้อความแจ้งเตือนเมื่อไม่พบบัญชี
            TempData["ErrorMessage"] = $"ไม่พบบัญชีบทบาท {normalizedRole} ที่เปิดใช้งานอยู่";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }
        // ตรวจสอบว่าพบข้อมูลบัญชีผู้ใช้ในฐานข้อมูลหรือไม่
        if (dbUser != null)
        {
            // ดึงรหัสผู้ใช้งานจริงจากฐานข้อมูล
            mockUserId = dbUser.UserId;
            // ดึงอีเมลผู้ใช้งานจริงจากฐานข้อมูล
            email = dbUser.Email;
            // ดึงชื่อผู้ใช้งานจริงจากฐานข้อมูล
            name = dbUser.FullName ?? dbUser.Email;
        }

        // ล้าง Session บทบาทเดิมทั้งหมดก่อนเข้าสู่ระบบบทบาทใหม่ (ป้องกัน Session ปะปนข้ามบทบาท)
        ClearAllAuthSession();

        // บันทึก Session จำเป็นสำหรับใช้ตรวจสอบทุกหน้า
        HttpContext.Session.SetInt32("UserId", mockUserId);
        // บันทึกบทบาทลงในเซสชัน
        HttpContext.Session.SetString("Role", normalizedRole);
        // บันทึกอีเมลลงในเซสชัน
        HttpContext.Session.SetString("UserEmail", email);
        // บันทึกชื่อผู้ใช้ลงในเซสชัน
        HttpContext.Session.SetString("UserName", name);

        // บันทึก Session เฉพาะบทบาทที่เลือก
        if (normalizedRole == "customer")
        {
            // บันทึกรหัสผู้ใช้บทบาทลูกค้า
            HttpContext.Session.SetInt32("CustomerUserId", mockUserId);
            // บันทึกชื่อผู้ใช้บทบาทลูกค้า
            HttpContext.Session.SetString("CustomerUserName", name);
        }
        // กรณีเลือกเข้าสู่ระบบบทบาทพนักงาน
        else if (normalizedRole == "staff")
        {
            // บันทึกรหัสผู้ใช้บทบาทพนักงาน
            HttpContext.Session.SetInt32("StaffUserId", mockUserId);
            // บันทึกชื่อผู้ใช้บทบาทพนักงาน
            HttpContext.Session.SetString("StaffUserName", name);
        }
        // กรณีเลือกเข้าสู่ระบบบทบาทเจ้าของร้าน
        else if (normalizedRole == "owner")
        {
            // บันทึกรหัสผู้ใช้บทบาทเจ้าของร้าน
            HttpContext.Session.SetInt32("OwnerUserId", mockUserId);
            // บันทึกชื่อผู้ใช้บทบาทเจ้าของร้าน
            HttpContext.Session.SetString("OwnerUserName", name);
        }
        // กรณีเลือกเข้าสู่ระบบบทบาทฝ่ายบัญชี
        else if (normalizedRole == "accountant")
        {
            // บันทึกรหัสผู้ใช้บทบาทฝ่ายบัญชี
            HttpContext.Session.SetInt32("AccountantUserId", mockUserId);
            // บันทึกชื่อผู้ใช้บทบาทฝ่ายบัญชี
            HttpContext.Session.SetString("AccountantUserName", name);
        }
        // กรณีเลือกเข้าสู่ระบบบทบาทไอทีซัพพอร์ต
        else if (normalizedRole == "itsupport")
        {
            // บันทึกรหัสผู้ใช้บทบาทไอทีซัพพอร์ต
            HttpContext.Session.SetInt32("ITSupportUserId", mockUserId);
            // บันทึกชื่อผู้ใช้บทบาทไอทีซัพพอร์ต
            HttpContext.Session.SetString("ITSupportUserName", name);
        }

        // บันทึก Log การเข้าสู่ระบบด่วนลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("AUTH"),
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

        // แจ้งเตือนข้อความสำเร็จในการเข้าสู่ระบบด่วน
        TempData["SuccessMessage"] = $"เข้าสู่ระบบด่วนในบทบาท {normalizedRole} สำเร็จ";
        // เปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทที่เลือก
        return RedirectToAction(targetAction, targetController);
    }

    // ฟังก์ชันช่วยล้างค่า Session การยืนยันตัวตนทั้งหมดออกจากระบบ
    private void ClearAllAuthSession()
    {
        // รายการคีย์เซสชันทั้งหมดที่เกี่ยวข้องกับการเข้าสู่ระบบและโปรโมชั่น
        string[] keys = {
            // คีย์เซสชันกลางสำหรับการเข้าสู่ระบบ
            "UserId", "Role", "UserEmail", "UserName",
            // คีย์เซสชันสำหรับบทบาทลูกค้า
            "CustomerUserId", "CustomerUserName",
            // คีย์เซสชันสำหรับบทบาทพนักงาน
            "StaffUserId", "StaffUserName",
            // คีย์เซสชันสำหรับบทบาทเจ้าของร้าน
            "OwnerUserId", "OwnerUserName",
            // คีย์เซสชันสำหรับบทบาทฝ่ายบัญชี
            "AccountantUserId", "AccountantUserName",
            // คีย์เซสชันสำหรับบทบาทไอทีซัพพอร์ต
            "ITSupportUserId", "ITSupportUserName",
            // คีย์เซสชันโค้ดโปรโมชั่นที่ใช้งาน
            AppliedPromoSessionKey,
            // คีย์เซสชันคูปองที่เก็บสะสมไว้
            CollectedPromoSessionKey
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
            // หากไม่พบให้ดึงจากเซสชันลูกค้า
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
    // แสดงแบบฟอร์มเปลี่ยนรหัสผ่าน
    public IActionResult ResetPassword()
    {
        // ส่งคืน View พร้อม ViewModel เปล่า
        return View(new ResetPasswordViewModel());
    }

    // UPDATE: ดำเนินการเปลี่ยนรหัสผ่านของสมาชิกผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: ตรวจสอบความถูกต้องของโมเดล ยืนยันรหัสผ่านเดิมด้วย BCrypt และแฮชรหัสผ่านใหม่ก่อนบันทึกลงฐานข้อมูล
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับประมวลผลการเปลี่ยนรหัสผ่าน
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        // ขั้นตอนที่ 1: ตรวจสอบความถูกต้องตาม DataAnnotations
        if (!ModelState.IsValid)
        {
            // ส่งคืน View พร้อมข้อมูลเดิม
            return View(model);
        }

        // ขั้นตอนที่ 2: ดึงรหัสสมาชิกจาก Session เพื่อยืนยันตัวตน
        var userId = GetAuthenticatedUserId();

        // ตรวจสอบว่าผู้ใช้เข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // แจ้งเตือนข้อความเมื่อไม่ได้เข้าสู่ระบบ
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนดำเนินการเปลี่ยนรหัสผ่าน";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 3: ค้นหาข้อมูลสมาชิกในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ตรวจสอบว่าพบข้อมูลผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนข้อความเมื่อไม่พบบัญชี
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 4: ตรวจสอบรหัสผ่านปัจจุบันด้วย BCrypt.Net.BCrypt.Verify
        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            // เพิ่มข้อผิดพลาดรหัสผ่านปัจจุบันไม่ถูกต้อง
            ModelState.AddModelError("CurrentPassword", "รหัสผ่านปัจจุบันไม่ถูกต้อง");
            // ส่งคืน View พร้อมข้อมูลเดิม
            return View(model);
        }

        // ขั้นตอนที่ 5: แฮชรหัสผ่านใหม่และบันทึกลงฐานข้อมูล MySQL
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จและนำทางไปยังหน้าโปรไฟล์
        TempData["SuccessMessage"] = "เปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าโปรไฟล์ของฉัน
        return RedirectToAction("Profile");
    }

    // READ: แสดงหน้าแบบฟอร์มกู้คืนรหัสผ่านก่อนเข้าสู่ระบบ (GET Method)
    // WHY: ให้สมาชิกที่ลืมรหัสผ่านสามารถยืนยันอีเมลและเบอร์โทรศัพท์เพื่อขอรหัสยืนยัน 6 หลักและตั้งรหัสผ่านใหม่ได้โดยไม่ต้องต่อเซิร์ฟเวอร์อีเมลภายนอก
    [HttpGet]
    // ประมวลผลคำสั่งของระบบ
    public IActionResult ForgotPassword()
    {
        // ล้างค่าเซสชันรหัสกู้คืนเดิมก่อนเริ่มขั้นตอนใหม่
        HttpContext.Session.Remove("ForgotPwdCode");
        // ล้างค่าเซสชันรหัสผู้ใช้ที่ขอกู้คืนเดิม
        HttpContext.Session.Remove("ForgotPwdUserId");
        // ส่งโมเดลเริ่มต้นไปยังหน้า View
        return View(new ForgotPasswordViewModel());
    }

    // UPDATE: ตรวจสอบอีเมลและเบอร์โทรศัพท์เพื่อสร้างรหัสยืนยัน 6 หลักแสดงบนหน้าจอ (POST Method ขั้นตอนที่ 1)
    // WHY: ตรวจสอบความตรงกันของอีเมลและเบอร์โทรศัพท์ในฐานข้อมูลจริง แล้วสร้างรหัส 6 หลักเก็บใน Session เพื่อให้ผู้ใช้คัดลอกหรือกดกรอกอัตโนมัติ
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสร้างรหัสยืนยันกู้คืนรหัสผ่าน
    public IActionResult GenerateResetCode(ForgotPasswordViewModel model)
    {
        // ตรวจสอบว่าผู้ใช้กรอกอีเมลและเบอร์โทรศัพท์ครบถ้วนหรือไม่
        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Phone))
        {
            // แจ้งเตือนเมื่อกรอกข้อมูลไม่ครบถ้วน
            TempData["ErrorMessage"] = "กรุณากรอกทั้งที่อยู่อีเมลและเบอร์โทรศัพท์ที่ลงทะเบียนไว้";
            // ส่งกลับหน้าเดิมพร้อมข้อมูลที่กรอกไว้
            return View("ForgotPassword", model);
        }

        // ตัดช่องว่างของอีเมลที่กรอกเข้ามา
        string normalizedEmail = model.Email.Trim();
        // สกัดเฉพาะตัวเลขของเบอร์โทรศัพท์เพื่อรองรับทั้งแบบมีขีดและไม่มีขีด (เช่น 081-234-5678 หรือ 0812345678)
        string cleanInputPhone = new string(model.Phone.Where(char.IsDigit).ToArray());

        // ค้นหาผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail.ToLower() && u.IsActive == true);
        // สกัดเฉพาะตัวเลขจากเบอร์โทรศัพท์ในฐานข้อมูลเพื่อเปรียบเทียบ
        string cleanDbPhone = user?.Phone != null ? new string(user.Phone.Where(char.IsDigit).ToArray()) : string.Empty;

        // ตรวจสอบว่าพบบัญชีและเบอร์โทรศัพท์ตรงกันหรือไม่
        if (user == null || string.IsNullOrEmpty(cleanInputPhone) || cleanDbPhone != cleanInputPhone)
        {
            // แจ้งเตือนเมื่ออีเมลหรือเบอร์โทรศัพท์ไม่ตรงกับข้อมูลที่ลงทะเบียนไว้
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่มีอีเมลและเบอร์โทรศัพท์ตรงกัน กรุณาตรวจสอบข้อมูลอีกครั้ง";
            // ส่งกลับหน้าเดิมพร้อมข้อมูลที่กรอกไว้
            return View("ForgotPassword", model);
        }

        // สร้างรหัสยืนยันตัวเลข 6 หลักแบบสุ่ม
        string generatedCode = Random.Shared.Next(100000, 999999).ToString();
        // บันทึกรหัสยืนยันลงใน Session สำหรับตรวจสอบในขั้นตอนตั้งรหัสผ่านใหม่
        HttpContext.Session.SetString("ForgotPwdCode", generatedCode);
        // บันทึกรหัสสมาชิกที่ขอกู้คืนลงใน Session
        HttpContext.Session.SetInt32("ForgotPwdUserId", user.UserId);

        // บันทึกประวัติการขอรหัสกู้คืนรหัสผ่านลงตาราง Systemlogs เพื่อให้ฝ่าย IT Support ตรวจสอบได้
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("PWD-OTP"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "account.forgot_password",
            // ข้อความรายละเอียดการออกรหัสกู้คืน
            Message = $"ออกรหัสยืนยันกู้คืนรหัสผ่าน 6 หลัก ({generatedCode}) สำหรับบัญชี: {user.Email}",
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = user.UserId,
            // เวลาที่ทำรายการ
            Timestamp = DateTime.Now
        });
        // บันทึกข้อมูล Log ลงฐานข้อมูล
        _db.SaveChanges();

        // ตั้งค่าสถานะว่าสร้างรหัสยืนยันสำเร็จแล้วเพื่อเปิดส่วนกรอกรหัสผ่านใหม่ในหน้า View
        model.IsCodeGenerated = true;
        // ส่งรหัสยืนยัน 6 หลักไปแสดงบนหน้าจอให้ผู้ใช้คัดลอกหรือกดกรอกอัตโนมัติ
        model.GeneratedCode = generatedCode;
        // แจ้งข้อความแนะนำขั้นตอนถัดไป
        TempData["SuccessMessage"] = "ยืนยันตัวตนสำเร็จ ระบบได้สร้างรหัสกู้คืน 6 หลักให้คุณแล้ว กดปุ่มคัดลอกและกรอกอัตโนมัติเพื่อตั้งรหัสผ่านใหม่ได้ทันที";
        // แสดงหน้า ForgotPassword ในโหมดขั้นตอนที่ 2
        return View("ForgotPassword", model);
    }

    // UPDATE: ยืนยันรหัสกู้คืน 6 หลักและบันทึกรหัสผ่านใหม่ลงฐานข้อมูล (POST Method ขั้นตอนที่ 2)
    // WHY: ตรวจสอบรหัส 6 หลักกับ Session แฮชรหัสผ่านใหม่ด้วย BCrypt และบันทึกลงฐานข้อมูลจริงพร้อมเก็บ Systemlog
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันยืนยันการตั้งรหัสผ่านใหม่
    public IActionResult ConfirmForgotPassword(ForgotPasswordViewModel model)
    {
        // อ่านรหัสยืนยัน 6 หลักที่เก็บไว้ใน Session
        var sessionCode = HttpContext.Session.GetString("ForgotPwdCode");
        // อ่านรหัสสมาชิกที่ขอกู้คืนจาก Session
        var sessionUserId = HttpContext.Session.GetInt32("ForgotPwdUserId");

        // ตรวจสอบว่ามีข้อมูล Session ของการกู้คืนรหัสผ่านหรือไม่
        if (string.IsNullOrEmpty(sessionCode) || !sessionUserId.HasValue)
        {
            // แจ้งเตือนเมื่อเซสชันหมดอายุหรือยังไม่ได้ขอรหัสยืนยัน
            TempData["ErrorMessage"] = "รหัสยืนยันหมดอายุหรือยังไม่ได้ขอรหัสยืนยัน กรุณายืนยันอีเมลและเบอร์โทรศัพท์ใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับไปเริ่มต้นใหม่
            return RedirectToAction("ForgotPassword");
        }

        // คงสถานะเปิดฟอร์มขั้นตอนที่ 2 ไว้กรณีตรวจสอบไม่ผ่าน
        model.IsCodeGenerated = true;
        // คงค่ารหัสที่ระบบเจนไว้ให้แสดงบนหน้าจอ
        model.GeneratedCode = sessionCode;

        // ตรวจสอบว่ารหัสยืนยัน 6 หลักที่ผู้ใช้กรอกตรงกับรหัสที่ระบบสร้างหรือไม่
        if (string.IsNullOrWhiteSpace(model.VerificationCode) || model.VerificationCode.Trim() != sessionCode)
        {
            // แจ้งเตือนเมื่อรหัสยืนยันไม่ถูกต้อง
            TempData["ErrorMessage"] = "รหัสยืนยัน 6 หลักไม่ถูกต้อง กรุณากดปุ่มคัดลอกและกรอกอัตโนมัติอีกครั้ง";
            // ส่งกลับหน้าเดิมพร้อมคงสถานะขั้นตอนที่ 2
            return View("ForgotPassword", model);
        }

        // ตรวจสอบความยาวของรหัสผ่านใหม่
        if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
        {
            // แจ้งเตือนเมื่อรหัสผ่านใหม่สั้นเกินไป
            TempData["ErrorMessage"] = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 6 ตัวอักษร";
            // ส่งกลับหน้าเดิมพร้อมคงสถานะขั้นตอนที่ 2
            return View("ForgotPassword", model);
        }

        // ตรวจสอบว่ารหัสผ่านใหม่และการยืนยันรหัสผ่านตรงกันหรือไม่
        if (model.NewPassword != model.ConfirmNewPassword)
        {
            // แจ้งเตือนเมื่อรหัสผ่านไม่ตรงกัน
            TempData["ErrorMessage"] = "รหัสผ่านใหม่และการยืนยันรหัสผ่านไม่ตรงกัน";
            // ส่งกลับหน้าเดิมพร้อมคงสถานะขั้นตอนที่ 2
            return View("ForgotPassword", model);
        }

        // ค้นหาข้อมูลสมาชิกในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.UserId == sessionUserId.Value && u.IsActive == true);
        // ตรวจสอบว่าพบบัญชีสมาชิกหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบบัญชีผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            // เปลี่ยนเส้นทางกลับไปหน้าเข้าสู่ระบบ
            return RedirectToAction("Login");
        }

        // เข้ารหัสรหัสผ่านใหม่ด้วย BCrypt และอัปเดตลงฐานข้อมูล
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

        // บันทึกประวัติการตั้งรหัสผ่านใหม่สำเร็จลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("PWD-RST"),
            // กำหนดระดับความสำคัญ
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "account.forgot_password",
            // ข้อความรายละเอียดการตั้งรหัสผ่านใหม่สำเร็จ
            Message = $"กู้คืนและตั้งรหัสผ่านใหม่สำเร็จสำหรับบัญชี: {user.Email}",
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = user.UserId,
            // เวลาที่ทำรายการ
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // ล้างข้อมูลรหัสยืนยันใน Session หลังจากใช้งานเสร็จสิ้น
        HttpContext.Session.Remove("ForgotPwdCode");
        // ล้างข้อมูลรหัสสมาชิกใน Session
        HttpContext.Session.Remove("ForgotPwdUserId");

        // แจ้งข้อความสำเร็จเพื่อให้ผู้ใช้เข้าสู่ระบบด้วยรหัสผ่านใหม่ได้ทันที
        TempData["SuccessMessage"] = "ตั้งรหัสผ่านใหม่เรียบร้อยแล้ว สามารถเข้าสู่ระบบด้วยรหัสผ่านใหม่ได้ทันที";
        // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
        return RedirectToAction("Login");
    }

    // READ: แสดงหน้าโปรไฟล์สมาชิก (Profile)
    // WHY: ส่งผ่าน UserProfileViewModel แยกความรับผิดชอบออกจาก Domain Model ป้องกันปัญหา Over-posting
    public IActionResult Profile()
    {
        // ตรวจสอบว่าปัจจุบันเบราว์เซอร์ล็อกอินเป็นบทบาทหลังบ้านหรือไม่ (กฎ 1 เบราว์เซอร์ 1 บทบาท)
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // หากเป็นบทบาทอื่นที่ไม่ใช่ลูกค้า ให้เปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทนั้นทันที
        if (!string.IsNullOrEmpty(activeRole) && activeRole != "customer")
        {
            // ดึงเส้นทางหน้าหลักของบทบาทปัจจุบัน
            var (roleAct, roleCtrl) = GetRoleHomeRoute(activeRole);
            // เปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทนั้น
            return RedirectToAction(roleAct, roleCtrl);
        }

        // ขั้นตอนที่ 1: ดึงรหัสสมาชิกปัจจุบันจาก Session
        var userId = GetAuthenticatedUserId();

        // ตรวจสอบว่าผู้ใช้เข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // แจ้งเตือนให้เข้าสู่ระบบเพื่อดูโปรไฟล์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบเพื่อดูโปรไฟล์ของคุณ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: ดึงข้อมูลสมาชิกจากฐานข้อมูล MySQL
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ตรวจสอบว่าพบข้อมูลผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนข้อความเมื่อไม่พบบัญชี
            TempData["ErrorMessage"] = "ไม่พบข้อมูลบัญชีผู้ใช้งานในระบบ";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 3: คำนวณจำนวนพรีออเดอร์ที่รอดำเนินการ
        int pendingCount = _db.Preorders.Count(p => p.UserId == user.UserId && p.CurrentStatus != "Completed" && p.CurrentStatus != "Cancelled");

        // ขั้นตอนที่ 4: ประกอบข้อมูล UserProfileViewModel
        var viewModel = new UserProfileViewModel
        {
            // กำหนดรหัสผู้ใช้
            UserId = user.UserId,
            // กำหนดรหัสสมาชิก
            UserCode = user.UserCode,
            // กำหนดชื่อ-นามสกุล
            FullName = user.FullName,
            // กำหนดอีเมล
            Email = user.Email,
            // กำหนดเบอร์โทรศัพท์
            Phone = user.Phone,
            // กำหนดที่อยู่รูปประจำตัว
            AvatarUrl = user.AvatarUrl,
            // กำหนดคะแนนสะสม
            Points = user.Points,
            // กำหนดจำนวนพรีออเดอร์ที่รอดำเนินการ
            PendingPreorderCount = pendingCount,
            // กำหนดบทบาทผู้ใช้
            RoleName = user.RoleName
        };

        // กำหนดค่า ViewBag สำหรับความเข้ากันได้
        ViewBag.PendingPreorderCount = pendingCount;

        // ส่งคืนหน้า View โปรไฟล์พร้อมข้อมูล
        return View(viewModel);
    }

    // UPDATE: แก้ไขข้อมูลส่วนตัวของสมาชิกผ่านแบบฟอร์ม MVC มาตรฐาน (POST Method)
    // WHY: รับค่าผ่าน UserProfileViewModel อัปเดตข้อมูลชื่อ เบอร์โทรศัพท์ และรูปโปรไฟล์ลง MySQL Database โดยตรง และปรับปรุง Session
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับปรับปรุงข้อมูลส่วนตัว
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
            // กรณีระบุ URL รูปภาพใหม่
            ? updatedAvatar.Trim()
            // กรณีใช้รูปภาพเดิม
            : user.AvatarUrl;

        // บันทึกไฟล์รูปประจำตัวที่อัปโหลด
        string? savedAvatarUrl = SaveUploadedImage(avatarFile, _env.WebRootPath, "avatar", candidateAvatarUrl);
        // ตรวจสอบว่าบันทึกไฟล์รูปสำเร็จหรือไม่
        if (!string.IsNullOrWhiteSpace(savedAvatarUrl))
        {
            // กำหนด URL รูปภาพใหม่ให้กับผู้ใช้
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับลบหรือยกเลิกบัญชีผู้ใช้งาน
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
        // ตรวจสอบว่ามีรายการในตะกร้าสินค้าหรือไม่
        if (cartItems.Any())
        {
            // สั่งลบรายการในตะกร้าทั้งหมด
            _db.Cartitems.RemoveRange(cartItems);
        }

        // เก็บอีเมลเดิมไว้สำหรับบันทึก Log ก่อนทำการลบหรือปลดล็อกอีเมล
        string originalEmail = user.Email;

        // ปลดการอ้างอิง UserId ในตาราง Systemlogs เพื่อให้สามารถลบเรคอร์ดผู้ใช้ได้โดยไม่ติด Foreign Key
        var userLogs = _db.Systemlogs.Where(l => l.UserId == user.UserId).ToList();
        // วนลูปปลดรหัสผู้ใช้ออกจากบันทึกกิจกรรมเดิม
        foreach (var log in userLogs)
        {
            // กำหนด UserId เป็นค่าว่างเพื่อรักษาประวัติ Log โดยไม่บล็อกการลบผู้ใช้
            log.UserId = null;
        }

        // ตรวจสอบว่ามีข้อมูลธุรกรรมผูกพันในตารางใบจอง ใบเสร็จ การแลกของรางวัล หรือสมุดบัญชีหรือไม่
        bool hasLinkedRecords = _db.Preorders.Any(p => p.UserId == user.UserId)
            // ตรวจสอบว่ามีประวัติใบเสร็จรับเงินหรือไม่
            || _db.Receipts.Any(r => r.CustomerId == user.UserId || r.CashierUserId == user.UserId)
            // ตรวจสอบว่ามีประวัติการแลกของรางวัลหรือไม่
            || _db.Rewardredemptions.Any(r => r.UserId == user.UserId)
            // ตรวจสอบว่ามีประวัติบันทึกในสมุดบัญชีหรือไม่
            || _db.Financialledgers.Any(f => f.RecordedByUserId == user.UserId);

        // ขั้นตอนที่ 4: หากไม่มีประวัติธุรกรรมผูกพัน ให้ลบบัญชีออกจากฐานข้อมูลทันที หากมีให้ปิดสถานะและปลดล็อกอีเมลเดิม
        if (!hasLinkedRecords)
        {
            // ลบข้อมูลผู้ใช้งานออกจากตาราง Users โดยตรง
            _db.Users.Remove(user);
        }
        // มิฉะนั้น (กรณีมีประวัติการทำธุรกรรมหรือใบจองในระบบ)
        else
        {
            // ปิดการใช้งานบัญชีเพื่อรักษา Foreign Key ของใบเสร็จ/ใบจองเดิม
            user.IsActive = false;
            // เปลี่ยนอีเมลของเรคอร์ดเดิมเป็นรหัสเก็บถาวรเพื่อให้ผู้ใช้สามารถนำอีเมลเดิมไปสมัครใหม่ได้ทันที
            user.Email = $"deleted_{user.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
        }

        // ขั้นตอนที่ 5: บันทึกประวัติการยกเลิกบัญชีลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("ACC-DEL"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "account.delete",
            // ข้อความรายละเอียดการยกเลิกบัญชี
            Message = $"สมาชิกดำเนินการยกเลิกและลบบัญชีผู้ใช้งานด้วยตนเอง: {originalEmail}",
            // ไม่ผูก UserId เนื่องจากบัญชีถูกลบแล้ว
            UserId = null,
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


    // ชื่อคีย์เซสชันสำหรับจัดเก็บรายการรหัสคูปองที่ผู้ใช้เก็บสะสมไว้
    private const string CollectedPromoSessionKey = "CollectedPromoCodes";
    // ชื่อคีย์เซสชันสำหรับจัดเก็บรหัสคูปองที่เลือกใช้งานในตะกร้า
    private const string AppliedPromoSessionKey = "AppliedPromoCode";

    // ออบเจกต์ CultureInfo สำหรับการจัดรูปแบบวันเวลาภาษาไทย
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    // ฟังก์ชันคำนวณวันปัจจุบันตามปฏิทินเกรกอเรียน (ค.ศ.)
    private static DateOnly TodayGregorian() =>
        // สร้างออบเจกต์ DateOnly จากปี เดือน วันปัจจุบัน
        new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

    // ฟังก์ชัน GenerateLogCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string GenerateLogCode(string prefix)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix)
            // กรณีคำนำหน้าว่างให้ใช้ LOG เป็นค่าเริ่มต้น
            ? "LOG"
            // กรณีระบุคำนำหน้าให้ตัดช่องว่างและเครื่องหมายขีดท้าย
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
            // แปลงปี พ.ศ. เป็น ค.ศ.
            year += 543;
        }
        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
        else if (year >= 2400)
        {
            // แปลงปี พ.ศ. เกิน 2400 เป็น ค.ศ.
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

    // ฟังก์ชันจัดรูปแบบวันที่เป็นสตริง ISO yyyy-MM-dd
    private static string FormatIsoDate(DateOnly date) =>
        // แปลงวันที่เป็น ค.ศ. และจัดรูปแบบตามมาตรฐาน ISO
        NormalizeGregorianDate(date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ฟังก์ชัน FormatPromoBenefitSummary สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatPromoBenefitSummary(Promotion promo)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal val = promo.DiscountValue ?? 0m;
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return (promo.PromoType ?? "percent").Trim().ToLowerInvariant() switch
        {
            // กรณีโปรโมชั่นแบบส่วนลดเปอร์เซ็นต์
            "percent" or "percentage" => $"ลด {val:0.#}%",
            // กรณีโปรโมชั่นซื้อ 1 แถม 1
            "bogo" => val > 0 ? $"ซื้อ 1 แถม 1 (สูงสุด ฿{val:N0})" : "ซื้อ 1 แถม 1",
            // กรณีโปรโมชั่นรับคะแนนสะสมพิเศษ 2 เท่า
            "double_points" => "รับแต้มสะสม x2",
            // กรณีโปรโมชั่นชุดเซ็ตสุดคุ้ม
            "bundle" => $"ชุดเซ็ตสุดคุ้ม ลด ฿{val:N0}",
            // กรณีโปรโมชั่นส่วนลดทั่วไป
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
            // กำหนดนามสกุลไฟล์เริ่มต้นเป็น .png
            ext = ".png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var imagesDir = Path.Combine(webRootPath, "media", "images");
        // สร้างโฟลเดอร์สำหรับจัดเก็บไฟล์หากยังไม่มี
        Directory.CreateDirectory(imagesDir);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
            // ตัดช่องว่างส่วนเกินหัวท้าย
            .Trim()
            // แปลงเป็นตัวพิมพ์เล็กมาตรฐาน
            .ToLowerInvariant()
            // แทนที่ช่องว่างด้วยขีดล่าง
            .Replace(" ", "_");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var safePrefix = string.IsNullOrWhiteSpace(rawName) ? defaultPrefix : rawName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fullPath = Path.Combine(imagesDir, fileName);

        // เปิดสตรีมไฟล์เพื่อเขียนข้อมูลรูปลงในดิสก์
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            // บันทึกไฟล์รูปลงในสตรีมปลายทาง
            imageFile.CopyTo(stream);
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "/media/images/" + fileName;
    }

    // ฟังก์ชันปรับมาตรฐานรหัสคูปองหรือข้อความให้เป็นตัวพิมพ์ใหญ่ตัดช่องว่าง
    private static string NormalizeCode(string? code) =>
        // แปลงรหัสเป็นตัวพิมพ์ใหญ่
        (code ?? string.Empty).Trim().ToUpperInvariant();

    // ฟังก์ชันปรับมาตรฐานรหัสคูปองโดยตัดเครื่องหมายขีดและช่องว่าง
    private static string NormalizeCodeNoHyphen(string? code) =>
        // ตัดเครื่องหมายขีด ชาร์ป และช่องว่างออก
        NormalizeCode(code).Replace("-", "").Replace("#", "").Replace(" ", "");

    // ฟังก์ชันแปลงประเภทโปรโมชั่นเป็นป้ายชื่อภาษาไทย
    private static string GetPromoTypeThaiLabel(string? promoType) =>
        // ตรวจสอบประเภทโปรโมชั่นและส่งคืนชื่อภาษาไทย
        (promoType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ป้ายชื่อโปรโมชั่นส่วนลดเปอร์เซ็นต์
            "percent" or "percentage" => "ส่วนลดเปอร์เซ็นต์",
            // ป้ายชื่อโปรโมชั่นส่วนลดมูลค่าคงที่
            "fixed" => "ส่วนลดมูลค่าคงที่",
            // ป้ายชื่อโปรโมชั่นซื้อ 1 แถม 1
            "bogo" => "ซื้อ 1 แถม 1",
            // ป้ายชื่อโปรโมชั่นชุดเซ็ต
            "bundle" => "ชุดเซ็ตสุดคุ้ม",
            // ป้ายชื่อโปรโมชั่นแต้มสองเท่า
            "double_points" => "แต้มสะสมสองเท่า",
            // ป้ายชื่อโปรโมชั่นทั่วไป
            _ => "โปรโมชั่นพิเศษ"
        };

    // ฟังก์ชันแปลงขอบเขตสินค้าที่เข้าร่วมโปรโมชั่นเป็นป้ายชื่อภาษาไทย
    private static string GetScopeThaiLabel(string? scope) =>
        // ตรวจสอบขอบเขตสินค้าและส่งคืนชื่อภาษาไทย
        (scope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ขอบเขตสินค้าทั้งหมด
            "all" or "" => "สินค้าทุกหมวดหมู่ในร้าน",
            // ขอบเขตสินค้าหมวดฟิกเกอร์
            "figures" => "สินค้าหมวดฟิกเกอร์",
            // ขอบเขตสินค้าลดล้างสต็อก
            "clearance" => "สินค้าลดล้างสต๊อก / พร้อมส่ง",
            // ขอบเขตสินค้าตามที่ระบุ
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
                // ตรวจสอบสลักที่มีคำว่า figure
                || slug.Contains("figure")
                // ตรวจสอบหมวดหมู่ของสะสมลิมิเต็ด
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

    // ฟังก์ชันประเมินและคำนวณส่วนลดโปรโมชั่นกับรายการสินค้าในตะกร้า
    private static PromoEvaluationResult EvaluateCartPromotion(
        // พารามิเตอร์ข้อมูลโปรโมชั่นที่ต้องการประเมิน
        Promotion? promo,
        // พารามิเตอร์รายการสินค้าในตะกร้าของลูกค้า
        IReadOnlyCollection<Cartitem> cartItems,
        // พารามิเตอร์วันที่จำลองสำหรับทดสอบ
        DateOnly? todayOverride = null)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var today = todayOverride ?? TodayGregorian();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal subtotal = cartItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (promo == null)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = "ไม่พบรหัสโปรโมชั่นนี้ในระบบ กรุณาตรวจสอบตัวสะกดอีกครั้ง"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.IsActive != true)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
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
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัส \"{promo.VoucherCode}\" จะเริ่มใช้งานได้ตั้งแต่วันที่ {FormatThaiDate(normalizedStart)}"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today > normalizedEnd)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ถูกใช้ครบจำนวนสิทธิ์แล้ว"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var eligibleItems = cartItems.Where(c => MatchesScope(c.Product, promo.ApplicableScope)).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal eligibleSubtotal = eligibleItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int eligibleQty = eligibleItems.Sum(c => c.Quantity);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (eligibleItems.Count == 0 || eligibleSubtotal <= 0m)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"โค้ด \"{promo.VoucherCode}\" ใช้ได้เฉพาะ{GetScopeThaiLabel(promo.ApplicableScope)} แต่ไม่มีสินค้าที่ร่วมรายการในตะกร้า"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal minSpend = promo.MinimumSpend ?? 0m;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (subtotal < minSpend)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal diff = minSpend - subtotal;
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดยอดรวมสินค้าที่เข้าเกณฑ์โปรโมชั่น
                EligibleSubtotal = eligibleSubtotal,
                // ประมวลผลคำสั่งของระบบ
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
                // คำนวณส่วนลดเปอร์เซ็นต์จากยอดเข้าเกณฑ์
                discount = Math.Round(eligibleSubtotal * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
                // จำกัดส่วนลดไม่ให้เกินยอดเข้าเกณฑ์
                discount = Math.Clamp(discount, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลด
                summary = $"ลด {discountValue:0.#}% (ประหยัด ฿{discount:N0})";
                // สิ้นสุดกรณีการทำงาน
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bogo":
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (eligibleQty < 2)
                {
                    // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                    return new PromoEvaluationResult
                    {
                        // กำหนดสถานะความถูกต้องเป็นเท็จ
                        IsValid = false,
                        // กำหนดข้อมูลโปรโมชั่น
                        Promotion = promo,
                        // กำหนดยอดรวมราคาสินค้าในตะกร้า
                        SubtotalAmount = subtotal,
                        // กำหนดยอดรวมสินค้าที่เข้าเกณฑ์โปรโมชั่น
                        EligibleSubtotal = eligibleSubtotal,
                        // กำหนดข้อความแจ้งเตือนเงื่อนไข 2 ชิ้น
                        ErrorMessage = $"โค้ดซื้อ 1 แถม 1 ({promo.VoucherCode}) ต้องมีสินค้าที่ร่วมรายการอย่างน้อย 2 ชิ้นในตะกร้า"
                    };
                }
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal cheapestUnit = eligibleItems.Min(c => c.Product?.RetailPrice ?? 0m);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal bogoCap = discountValue > 0m ? discountValue : cheapestUnit;
                // จำกัดส่วนลดซื้อ 1 แถม 1 ไม่ให้เกินยอดเข้าเกณฑ์
                discount = Math.Clamp(bogoCap, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลดซื้อ 1 แถม 1
                summary = $"สิทธิพิเศษซื้อ 1 แถม 1 (ส่วนลด ฿{discount:N0})";
                // สิ้นสุดกรณีการทำงาน
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "double_points":
                // กำหนดสถานะรับแต้มสองเท่า
                isDoublePoints = true;
                // กำหนดข้อความสรุปสิทธิประโยชน์แต้มสองเท่า
                summary = "รับแต้มสะสม x2 เมื่อชำระเงินที่หน้าร้าน";
                // สิ้นสุดกรณีรับแต้มสองเท่า
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "fixed":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bundle":
            // กรณีส่วนลดมูลค่าคงที่หรือกรณีเริ่มต้น
            default:
                // คำนวณส่วนลดคงที่ท้ายบิล
                discount = Math.Clamp(discountValue, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลดท้ายบิล
                summary = $"ส่วนลดท้ายบิล ฿{discount:N0}";
                // สิ้นสุดกรณีส่วนลดคงที่
                break;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new PromoEvaluationResult
        {
            // กำหนดสถานะความถูกต้องเป็นจริง
            IsValid = true,
            // กำหนดข้อมูลโปรโมชั่น
            Promotion = promo,
            // กำหนดยอดรวมราคาสินค้าในตะกร้า
            SubtotalAmount = subtotal,
            // กำหนดยอดสินค้าที่เข้าเกณฑ์โปรโมชั่น
            EligibleSubtotal = eligibleSubtotal,
            // กำหนดยอดเงินส่วนลดสุทธิ
            DiscountAmount = discount,
            // กำหนดสถานะรับแต้มสะสมพิเศษสองเท่า
            IsDoublePoints = isDoublePoints,
            // กำหนดข้อความสรุปสิทธิประโยชน์ของโปรโมชั่น
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
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return JsonSerializer.Deserialize<List<string>>(rawSessionValue)?
                // กรองเฉพาะข้อความที่ไม่เป็นค่าว่าง
                .Where(s => !string.IsNullOrWhiteSpace(s))
                // ปรับมาตรฐานรูปแบบรหัสคูปอง
                .Select(NormalizeCode)
                // ตัดรายการที่ซ้ำซ้อนออก
                .Distinct(StringComparer.OrdinalIgnoreCase)
                // แปลงเป็นรายการลิสต์
                .ToList() ?? new List<string>();
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }
    }

    // ฟังก์ชันแปลงรายการรหัสคูปองเป็น JSON
    private static string SerializeCollectedCodes(IEnumerable<string> codes) =>
        // แปลงข้อมูลเป็นสตริง JSON
        JsonSerializer.Serialize(
            // กรองรหัสคูปองที่ไม่ว่าง
            codes.Where(s => !string.IsNullOrWhiteSpace(s))
                 // ปรับมาตรฐานรูปแบบรหัสคูปอง
                 .Select(NormalizeCode)
                 // ตัดรายการที่ซ้ำซ้อนออก
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 // แปลงเป็นรายการลิสต์
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
                    // เก็บเพย์โหลดลงในพจนานุกรมตามรหัสการแลกของรางวัล
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
                    // เก็บเพย์โหลดลงในพจนานุกรมตามหมายเลขใบสั่งจองพรีออเดอร์
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
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(isoDate) &&
            // แปลงวันที่ตามรูปแบบ ISO yyyy-MM-dd
            DateOnly.TryParseExact(isoDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return parsed.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "-";
    }

    // ฟังก์ชันตัดอักขระพิเศษออกจากเบอร์โทรศัพท์ให้เหลือเฉพาะตัวเลข
    private static string NormalizePhoneDigits(string? phone) =>
        // ลบเครื่องหมายขีด ช่องว่าง และตัดช่องว่างส่วนเกิน
        (phone ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();

    // ตัวแปรเก็บพาธโฟลเดอร์ wwwroot สำหรับการอัปโหลดไฟล์
    private static string? _webRootPath;

    // ฟังก์ชัน ConfigureWebRoot สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static void ConfigureWebRoot(string? webRootPath)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            // กำหนดค่าพาธ wwwroot
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

        // ดึงชื่อไฟล์โดยไม่รวมนามสกุล
        string? fileNameNoExt = Path.GetFileNameWithoutExtension(normalized);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(fileNameNoExt) || fileNameNoExt.EndsWith("_card", StringComparison.OrdinalIgnoreCase))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cardFileName = fileNameNoExt + "_card.png";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cardRelative = "/media/images/" + cardFileName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string rootDir = !string.IsNullOrWhiteSpace(_webRootPath)
            // กรณีระบุพาธ wwwroot ชัดเจน
            ? _webRootPath
            // กำหนดพาธโฟลเดอร์ wwwroot เริ่มต้น
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string physicalPath = Path.Combine(rootDir, "media", "images", cardFileName);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (System.IO.File.Exists(physicalPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed.StartsWith("~/") ? "~" + cardRelative : cardRelative;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return trimmed;
    }

    // ฟังก์ชัน EscapeCsvField สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string EscapeCsvField(string? value)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string s = value ?? string.Empty;
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
        // พารามิเตอร์รายการบันทึกกิจกรรมในระบบ
        IEnumerable<Systemlog>? logs,
        // พารามิเตอร์รหัสอ้างอิงของรายการ
        string? referenceCode,
        // พารามิเตอร์ระบุว่าเป็นรายการของรางวัลหรือไม่
        bool isReward = false)
    {
        // กำหนดข้อความเริ่มต้นกรณีลูกค้ายกเลิกทั่วไป
        string defaultTitle = isReward ? "ยกเลิกการแลกของรางวัล" : "ยกเลิกคำสั่งจอง";
        // กำหนดข้อความคำอธิบายเริ่มต้น
        string defaultNotice = isReward
            // ข้อความสาเหตุเริ่มต้นสำหรับการยกเลิกของรางวัล
            ? "รายการแลกของรางวัลนี้ถูกยกเลิกแล้ว ระบบได้คืนแต้มสะสมและคืนสต็อกของรางวัลเรียบร้อยแล้ว"
            // ข้อความสาเหตุเริ่มต้นสำหรับพรีออเดอร์
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
            // กรองบันทึกกิจกรรมที่มีข้อความและตรงตามเงื่อนไข
            .Where(l => !string.IsNullOrEmpty(l.Message) &&
                        // ตรวจสอบข้อความว่ามีรหัสอ้างอิงตรงกัน
                        l.Message.Contains(cleanCode, StringComparison.OrdinalIgnoreCase) &&
                        // ตรวจสอบโมดูลการยกเลิกที่เกี่ยวข้อง
                        (l.Module == "CounterPreorder" || l.Module == "preorder.cancel" || l.Module == "reward.cancel" ||
                         // ตรวจสอบรหัสกิจกรรมปฏิเสธรับสินค้า
                         l.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมลูกค้าไม่มารับ
                         l.LogCode.StartsWith("NOSHOW", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมยกเลิกพรีออเดอร์
                         l.LogCode.StartsWith("PO-CAN", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมยกเลิกของรางวัล
                         l.LogCode.StartsWith("RWD-CAN", StringComparison.OrdinalIgnoreCase)))
            // จัดเรียงตามวันเวลาจากล่าสุดไปเก่าสุด
            .OrderByDescending(l => l.Timestamp)
            // เลือกรายการแรกที่พบ
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
                // ตัดข้อความรายละเอียดที่ว่างเปล่าออก
                .Replace("| รายละเอียด: ไม่มี", "")
                // ตัดข้อความหมายเหตุที่ว่างเปล่าออก
                .Replace("| หมายเหตุ: ไม่มี", "")
                // ตัดช่องว่างส่วนเกิน
                .Trim();
        }

        // ตรวจสอบกรณีพนักงานกด "ไม่รับสินค้า" (REJECT)
        if (matchedLog.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
            // ตรวจสอบข้อความลูกค้าไม่รับสินค้า
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
            // ตรวจสอบข้อความลูกค้าไม่มารับตามนัด
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
            // ส่งคืนข้อความสาเหตุที่แกะได้
            ? reasonPart
            // ข้อความสาเหตุเริ่มต้นกรณีแกะไม่พบ
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
            // เส้นทางหน้าหลักพนักงานหน้าร้าน
            "staff" => ("Index", "Staff"),
            // เส้นทางหน้าหลักเจ้าของร้าน
            "owner" => ("Promotions", "Owner"),
            // เส้นทางหน้าหลักฝ่ายบัญชี
            "accountant" => ("Index", "Accountant"),
            // เส้นทางหน้าหลักฝ่ายไอทีซัพพอร์ต
            "itsupport" => ("Dashboard", "ITSupport"),
            // เส้นทางหน้าหลักลูกค้าทั่วไป
            _ => ("Index", "Home")
        };
    }
}
