using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับงานเคาน์เตอร์พนักงานหน้าร้าน
public class StaffController : Controller
{
    // ตัวแปรเชื่อมต่อฐานข้อมูล CSI402DB ผ่าน Entity Framework Core
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับ DbContext ผ่านการฉีด Dependency Injection
    public StaffController(Csi402dbContext db)
    {
        // กำหนดค่าตัวแปรเชื่อมต่อฐานข้อมูลให้กับฟิลด์ภายในคลาส
        _db = db;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินด้วยบทบาทพนักงานหรือผู้มีอำนาจหรือไม่
    private bool IsAuthorizedStaff()
    {
        // อ่านค่าบทบาท (Role) ของผู้ใช้งานปัจจุบันที่บันทึกไว้ใน Session
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสพนักงาน StaffUserId จาก Session
        var staffId = HttpContext.Session.GetInt32("StaffUserId");
        // หากมีรหัส StaffUserId ใน Session แสดงว่าผ่านการตรวจสอบสิทธิ์
        if (staffId.HasValue) return true;
        // ตรวจสอบว่าบทบาทตรงกับ staff, owner หรือ itsupport หรือไม่
        return role != null && (role.ToLower() == "staff" || role.ToLower() == "owner" || role.ToLower() == "itsupport");
    }

    // Action แสดงรายการสต็อกสินค้าและค้นหาใบจองเบื้องต้นสำหรับพนักงาน (GET Method)
    public IActionResult Index(CounterPreorderViewModel? data)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้ล็อกอินเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบว่ามีข้อมูลค้นหารหัสใบจองส่งมาหรือไม่
        if (data != null && !string.IsNullOrEmpty(data.ReservationNumber))
        {
            // ส่งหมายเลขใบจองไปยัง View ผ่านตัวแปร ViewBag
            ViewBag.ReservationNumber = data.ReservationNumber;
        }

        // ดึงรายการสินค้าทั้งหมดจากตาราง Products พร้อมข้อมูลหมวดหมู่และสต็อกสินค้า
        var products = _db.Products
            // รวมข้อมูลหมวดหมู่สินค้า Category
            .Include(p => p.Category)
            // รวมข้อมูลจำนวนสต็อกสินค้าในคลัง Inventory
            .Include(p => p.Inventory)
            // แปลงผลลัพธ์เป็น List รายการสินค้า
            .ToList();

        // ส่งข้อมูลรายการสินค้าทั้งหมดไปยังหน้า Index ของ Staff
        return View(products);
    }

    // Action สำหรับเปิดหน้าเคาน์เตอร์พรีออเดอร์ (GET Method)
    [HttpGet]
    // ฟังก์ชัน CounterPreorder รับค่า query ที่ส่งมาทาง URL เพื่อค้นหาข้อมูลอัตโนมัติ
    public IActionResult CounterPreorder(string? query)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินด้วยบทบาทพนักงาน (Staff) หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData ไปแสดงผลที่หน้า Login
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // สั่งเปลี่ยนเส้นทาง (Redirect) ไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        // ประกาศตัวแปรเก็บข้อมูลใบจองพรีออเดอร์ที่ค้นหาพบ (เริ่มต้นเป็นค่าว่าง null)
        Preorder? foundPreorder = null;
        // ประกาศตัวแปรเก็บข้อมูลการแลกของรางวัลที่ค้นหาพบ (เริ่มต้นเป็นค่าว่าง null)
        Rewardredemption? foundRedemption = null;

        // ตรวจสอบว่าผู้ใช้ส่งข้อความค้นหา (query) เข้ามาหรือไม่
        if (!string.IsNullOrWhiteSpace(query))
        {
            // ตัดช่องว่างหัวท้ายและตัดเครื่องหมาย # ออกจากข้อความค้นหา
            string cleanQuery = query.Trim().Replace("#", "");
            // ตัดเครื่องหมายขีดลบ (-) ออกเพื่อเตรียมค้นหาเบอร์โทรศัพท์ที่ไม่มีขีด
            string phoneWithoutHyphen = cleanQuery.Replace("-", "");

            // ดึงข้อมูลใบสั่งจองจากตาราง Preorders โดยโหลดตาราง Preorderitems, Product และ User ร่วมด้วย
            foundPreorder = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(item => item.Product)
                .Include(p => p.User)
                // ตรวจสอบเงื่อนไขว่าตรงกับรหัสใบจอง, เบอร์โทรผู้ติดต่อ หรือชื่อลูกค้าในฐานข้อมูล
                .FirstOrDefault(p => p.ReservationNumber == cleanQuery
                                  || p.ReservationNumber == query.Trim()
                                  || p.ContactPhone == cleanQuery
                                  || p.ContactPhone.Replace("-", "") == phoneWithoutHyphen
                                  || (p.User != null && p.User.Phone == cleanQuery)
                                  || (p.User != null && p.User.FullName != null && p.User.FullName.Contains(cleanQuery)));

            // ตรวจสอบว่าค้นพบข้อมูลใบสั่งจองในฐานข้อมูลหรือไม่
            if (foundPreorder != null)
            {
                // ส่งข้อมูลใบจองที่พบไปยัง View ผ่านตัวแปร ViewBag.SearchResult
                ViewBag.SearchResult = foundPreorder;
                // กำหนดข้อความแจ้งผลการค้นหาใบจองสำเร็จเพื่อแสดงบนหน้าจอ
                ViewBag.Status = $"พบข้อมูลใบสั่งจอง #{foundPreorder.ReservationNumber} (ลูกค้า: {foundPreorder.User?.FullName ?? foundPreorder.ContactPhone})";
            }
            // หากไม่พบใบจองในตาราง Preorders ให้ตรวจสอบต่อในตารางแลกของรางวัล
            else
            {
                // ดึงข้อมูลการแลกของรางวัลจากตาราง Rewardredemptions พร้อมข้อมูล Reward และ User
                foundRedemption = _db.Rewardredemptions
                    .Include(r => r.Reward)
                    .Include(r => r.User)
                    // ตรวจสอบว่าตรงกับรหัสแลกของรางวัล หรือเบอร์โทรสมาชิกหรือไม่
                    .FirstOrDefault(r => r.RedemptionCode == cleanQuery
                                      || r.RedemptionCode == query.Trim()
                                      || (r.User != null && r.User.Phone == cleanQuery));

                // ตรวจสอบว่าพบข้อมูลการแลกของรางวัลในฐานข้อมูลหรือไม่
                if (foundRedemption != null)
                {
                    // ส่งข้อมูลการแลกของรางวัลที่พบไปยัง View ผ่าน ViewBag.SearchResultRedemption
                    ViewBag.SearchResultRedemption = foundRedemption;
                    // กำหนดข้อความแจ้งผลการค้นหารหัสของรางวัลเพื่อแสดงบนหน้าจอ
                    ViewBag.Status = $"พบข้อมูลแลกของรางวัล #{foundRedemption.RedemptionCode} (ลูกค้า: {foundRedemption.User?.FullName ?? foundRedemption.User?.Email})";
                }
            }
        }

        // กรณีไม่ได้ระบุคำค้นหา หรือค้นหาแล้วไม่พบข้อมูล ให้แสดงใบจองล่าสุดเป็นตัวอย่าง
        if (foundPreorder == null && foundRedemption == null)
        {
            // ดึงข้อมูลใบสั่งจองรายการล่าสุดในระบบที่สถานะยังรอมารับสินค้า
            var defaultPreorder = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(item => item.Product)
                .Include(p => p.User)
                // จัดเรียงตามวันเวลาที่สั่งจองจากใหม่ไปเก่า
                .OrderByDescending(p => p.CreatedAt)
                // เลือกใบจองที่สถานะเป็น ReadyForPickup หรือ PendingConfirm เป็นลำดับแรก
                .FirstOrDefault(p => p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
                // หากไม่มีรายการรอมารับ ให้ดึงใบจองล่าสุดที่มีในระบบไม่ว่าสถานะใด
                ?? _db.Preorders
                    .Include(p => p.Preorderitems)
                        .ThenInclude(item => item.Product)
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

            // ตรวจสอบว่ามีใบสั่งจองสำหรับแสดงผลเริ่มต้นหรือไม่
            if (defaultPreorder != null)
            {
                // ส่งข้อมูลใบจองเริ่มต้นไปยัง View ผ่าน ViewBag.SearchResult
                ViewBag.SearchResult = defaultPreorder;
                // กำหนดข้อความแจ้งว่ากำลังแสดงข้อมูลใบสั่งจองล่าสุดในระบบ
                ViewBag.Status = $"กำลังแสดงรายการพรีออเดอร์ล่าสุด #{defaultPreorder.ReservationNumber} ({defaultPreorder.ContactPhone})";
            }
        }

        // ดึงรายการประวัติการจอง 6 รายการล่าสุดจากฐานข้อมูลสำหรับสร้างปุ่มค้นหาด่วน
        ViewBag.RecentPreorders = _db.Preorders
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToList();

        // ส่ง ViewModel พร้อมรหัสใบจองที่ค้นพบไปยังหน้า View เพื่อแสดงผล
        return View(new CounterPreorderViewModel { ReservationNumber = foundPreorder?.ReservationNumber ?? query ?? "" });
    }

    // Action สำหรับค้นหาใบจองพรีออเดอร์จากช่องค้นหา (POST Method)
    [HttpPost]
    // ฟังก์ชัน CounterPreorder รับข้อมูลโมเดล data จากฟอร์มค้นหา
    public IActionResult CounterPreorder(CounterPreorderViewModel data)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์เป็นพนักงานเคาน์เตอร์หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อความค้นหาจากช่องรหัสใบจองหรือช่องเบอร์โทรศัพท์ พร้อมตัดช่องว่าง
        string rawQuery = (data?.ReservationNumber ?? data?.Phone ?? "").Trim();
        // ตัดเครื่องหมาย # ออกจากข้อความค้นหา
        string cleanQuery = rawQuery.Replace("#", "").Trim();
        // ตัดเครื่องหมายขีด (-) ออกเพื่อรองรับเบอร์โทรศัพท์ทั้งสองรูปแบบ
        string phoneWithoutHyphen = cleanQuery.Replace("-", "");

        // ตรวจสอบว่าผู้ใช้งานกรอกข้อความค้นหาเข้ามาหรือไม่
        if (!string.IsNullOrEmpty(cleanQuery))
        {
            // ค้นหาข้อมูลใบสั่งจองในตาราง Preorders ด้วยรหัสใบจอง เบอร์โทรศัพท์ หรือชื่อลูกค้า
            var preorder = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(item => item.Product)
                .Include(p => p.User)
                // ตรวจสอบเงื่อนไขว่าตรงกับข้อความค้นหาจุดใดจุดหนึ่งหรือไม่
                .FirstOrDefault(p => p.ReservationNumber == cleanQuery
                                  || p.ReservationNumber == rawQuery
                                  || p.ContactPhone == cleanQuery
                                  || p.ContactPhone.Replace("-", "") == phoneWithoutHyphen
                                  || (p.User != null && (p.User.FullName != null && p.User.FullName.Contains(cleanQuery)))
                                  || (p.User != null && p.User.Email.Contains(cleanQuery)));

            // ตรวจสอบว่าพบข้อมูลใบสั่งจองหรือไม่
            if (preorder != null)
            {
                // นำใบสั่งจองที่พบส่งต่อไปยัง View
                ViewBag.SearchResult = preorder;
                // กำหนดข้อความแจ้งผลการค้นหาใบจองสำเร็จ
                ViewBag.Status = $"พบข้อมูลใบสั่งจอง #{preorder.ReservationNumber} (ลูกค้า: {preorder.User?.FullName ?? preorder.ContactPhone})";
            }
            // หากไม่พบใบสั่งจอง ให้ค้นหาต่อในตารางของรางวัล
            else
            {
                // ค้นหาข้อมูลในตาราง Rewardredemptions ด้วยรหัสแลกของรางวัลหรือเบอร์โทร
                var redemption = _db.Rewardredemptions
                    .Include(r => r.Reward)
                    .Include(r => r.User)
                    // ตรวจสอบว่าตรงกับรหัสของรางวัลหรือเบอร์โทรสมาชิกหรือไม่
                    .FirstOrDefault(r => r.RedemptionCode == cleanQuery
                                      || r.RedemptionCode == rawQuery
                                      || (r.User != null && r.User.Phone == cleanQuery)
                                      || (r.User != null && r.User.Phone.Replace("-", "") == phoneWithoutHyphen));

                // ตรวจสอบว่าพบข้อมูลการแลกของรางวัลหรือไม่
                if (redemption != null)
                {
                    // นำข้อมูลของรางวัลที่พบส่งไปยัง View
                    ViewBag.SearchResultRedemption = redemption;
                    // กำหนดข้อความแจ้งผลการค้นหาของรางวัลสำเร็จ
                    ViewBag.Status = $"พบข้อมูลแลกของรางวัล #{redemption.RedemptionCode} (ลูกค้า: {redemption.User.FullName ?? redemption.User.Email})";
                }
                // กรณีค้นหาไม่พบทั้งใบจองและของรางวัล
                else
                {
                    // กำหนดข้อความแจ้งผู้ใช้ว่าไม่พบข้อมูลตามเงื่อนไขที่ระบุ
                    ViewBag.Status = $"ไม่พบข้อมูลใบสั่งจองหรือรหัสแลกของรางวัลตามเงื่อนไข \"{rawQuery}\" ในระบบ";
                }
            }
        }
        // กรณีผู้ใช้กดค้นหาโดยไม่ได้กรอกข้อความใดๆ
        else
        {
            // กำหนดข้อความแจ้งเตือนให้ผู้ใช้งานกรอกข้อมูลก่อนค้นหา
            ViewBag.Status = "กรุณากรอกรหัสพรีออเดอร์หรือเบอร์โทรศัพท์สำหรับค้นหา";
        }

        // ดึงรายการประวัติการจองล่าสุด 6 รายการเพื่อแสดงเป็นปุ่มทางลัด
        ViewBag.RecentPreorders = _db.Preorders
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToList();

        // ส่ง ViewModel กลับไปแสดงผลที่ View
        return View(data);
    }

    // Action สำหรับบันทึกการมารับสินค้า ชำระเงิน ตัดสต็อก และสะสมแต้ม (POST Method)
    [HttpPost]
    // ฟังก์ชัน CompletePickup รับ preorderId (รหัสใบจอง) และ memberPhone (เบอร์โทรสมาชิก)
    public IActionResult CompletePickup(int? preorderId, string? memberPhone)
    {
        // ขั้นตอนที่ 0: ตรวจสอบสิทธิ์ว่าผู้ใช้งานคือพนักงานเคาน์เตอร์หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบจอง ว่ามีค่าและมากกว่าศูนย์หรือไม่
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัสใบจองที่ส่งมา
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจองที่ต้องการทำรายการ";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาข้อมูลใบสั่งจองในฐานข้อมูล พร้อมดึงรายการสินค้าที่จองไว้ข้างใน (Preorderitems)
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            // เลือกแถวที่มี PreorderId ตรงกับค่าที่ส่งเข้ามา
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบข้อมูลใบสั่งจองในฐานข้อมูลจริงหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีค้นหาไม่พบข้อมูลในระบบ
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองนี้ในระบบฐานข้อมูล";
            // เปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบสถานะก่อน: ถ้าออเดอร์เป็น Completed หรือ Cancelled ไปแล้ว จะสั่งบล็อกทันทีเพื่อป้องกันพนักงานกดซ้ำเพื่อปั๊มแต้ม
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่ารายการนี้ถูกปิดสถานะไปแล้ว และห้ามทำรายการซ้ำ
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} นี้ได้ดำเนินการปิดสถานะเป็น {preorder.CurrentStatus} ไปแล้ว ไม่สามารถดำเนินการซ้ำได้เพื่อป้องกันการปั๊มแต้ม";
            // สั่ง Redirect กลับไปที่หน้าเคาน์เตอร์พร้อมส่งรหัสใบจองเพื่อแสดงข้อมูลปัจจุบัน
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ปรับเปลี่ยนสถานะใบสั่งจองใน Entity ให้เป็น "Completed"
        preorder.CurrentStatus = "Completed";
        // บันทึกวันและเวลาปัจจุบันที่ลูกค้ามารับสินค้าจริง (CompletedAt)
        preorder.CompletedAt = DateTime.Now;

        // ประกาศตัวแปรเก็บจำนวนแต้มสะสมที่สมาชิกจะได้รับ (เริ่มต้นที่ 0)
        int pointsEarned = 0;
        // ประกาศตัวแปรเก็บข้อความรายละเอียดการสะสมแต้มสำหรับนำไปแสดงผล
        string memberLogMsg = "";

        // ตรวจสอบว่าพนักงานได้กรอกเบอร์โทรศัพท์สมาชิกเข้ามาหรือไม่
        if (!string.IsNullOrWhiteSpace(memberPhone))
        {
            // ทำความสะอาดเบอร์โทรศัพท์โดยตัดช่องว่างและเครื่องหมายขีดลบ (-) ออก
            string cleanPhone = memberPhone.Trim().Replace("-", "");
            // ค้นหาข้อมูลสมาชิกในตาราง Users ด้วยเบอร์โทรศัพท์ หรืออีเมล
            var memberUser = _db.Users.FirstOrDefault(u => u.Phone == cleanPhone || u.Phone == memberPhone.Trim() || u.Email == memberPhone.Trim());

            // ตรวจสอบว่าพบข้อมูลสมาชิกในระบบหรือไม่
            if (memberUser != null)
            {
                // คำนวณแต้มสะสม: 10% ของยอดเงินรวม (ขั้นต่ำ 10 แต้ม)
                pointsEarned = (int)Math.Max(10, Math.Round(preorder.TotalFinalPrice / 10));
                // คำนวณแต้มสะสมจากยอดเงิน แล้วนำไปบวกให้สมาชิก memberUser.Points += pointsEarned;
                memberUser.Points += pointsEarned;
                // กำหนดข้อความรายละเอียดการสะสมแต้มสำเร็จ พร้อมระบุชื่อสมาชิกและแต้มคงเหลือรวม
                memberLogMsg = $" (สะสมแต้มให้คุณ {memberUser.FullName ?? memberUser.Email}: +{pointsEarned} แต้ม รวมปัจจุบัน {memberUser.Points} แต้ม)";
            }
            // กรณีพนักงานกรอกเบอร์โทรเข้ามาแต่ไม่พบข้อมูลสมาชิกในระบบ
            else
            {
                // บันทึกข้อความแจ้งว่าไม่พบเบอร์สมาชิก และจะดำเนินการต่อแบบลูกค้าทั่วไป
                memberLogMsg = " (ไม่พบเบอร์สมาชิก ดำเนินการต่อแบบลูกค้าทั่วไป)";
            }
        }

        // สร้างเลขที่ใบเสร็จรับเงินอัตโนมัติขึ้นต้นด้วย RCP ตามด้วยวันเวลาปัจจุบัน
        string receiptNo = "RCP" + DateTime.Now.ToString("yyyyMMddHHmmss");
        // ดึงรหัสพนักงานแคชเชียร์ที่กำลังล็อกอินจาก Session (หากไม่มีให้ใช้รหัสสำรอง 2)
        int cashierUserId = HttpContext.Session.GetInt32("UserId") ?? 2;

        // สร้างอ็อบเจกต์ Receipt เพื่อเตรียมบันทึกลงตาราง receipts ในฐานข้อมูล
        var receipt = new Receipt
        {
            // กำหนดเลขที่ใบเสร็จรับเงิน
            ReceiptNo = receiptNo,
            // เชื่อมโยงรหัสใบสั่งจองพรีออเดอร์
            PreorderId = preorder.PreorderId,
            // เชื่อมโยงรหัสลูกค้าผู้สั่งจอง
            CustomerId = preorder.UserId,
            // บันทึกเบอร์โทรศัพท์ลูกค้าที่มารับสินค้า
            CustomerPhone = !string.IsNullOrWhiteSpace(memberPhone) ? memberPhone : preorder.ContactPhone,
            // บันทึกรหัสพนักงานแคชเชียร์ผู้ออกใบเสร็จ
            CashierUserId = cashierUserId,
            // กำหนดยอดเงินรวมก่อนหักส่วนลด
            SubtotalAmount = preorder.TotalFinalPrice,
            // กำหนดยอดส่วนลด (0.00 บาท)
            DiscountAmount = 0.00m,
            // คำนวณภาษีมูลค่าเพิ่ม 7% จากยอดรวม
            VatAmount = Math.Round(preorder.TotalFinalPrice * 0.07m, 2),
            // กำหนดยอดเงินสุทธิที่ลูกค้าต้องชำระจริง
            NetTotalAmount = preorder.TotalFinalPrice,
            // บันทึกรหัสบาร์โค้ดใบจองลงในใบเสร็จ
            BarcodeData = preorder.ReservationNumber,
            // บันทึกวันและเวลาที่ออกใบเสร็จรับเงิน
            IssuedAt = DateTime.Now
        };

        // ออกใบเสร็จจริงลงตาราง receipts ด้วยคำสั่ง _db.Receipts.Add(receipt);
        _db.Receipts.Add(receipt);

        // วนลูปตัดสต็อกสินค้าทีละรายการตามสินค้าที่อยู่ในใบสั่งจอง
        foreach (var item in preorder.Preorderitems)
        {
            // ค้นหาข้อมูลสต็อกสินค้าในตาราง Inventories ด้วยรหัสสินค้า ProductId
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);

            // ตรวจสอบว่าพบข้อมูลสต็อกของสินค้านั้นในคลังหรือไม่
            if (inv != null)
            {
                // Phase 2 (ตัดของจริงออกจากร้าน): หักของจริงออกจากร้าน
                inv.QuantityOnHand -= item.Quantity;
                // Phase 2 (ตัดของจริงออกจากร้าน): ปลดภาระยอดจองออก
                inv.ReservedQuantity -= item.Quantity;

                // ตรวจสอบป้องกันไม่ให้จำนวนสินค้าคงเหลือติดลบในฐานข้อมูล
                if (inv.QuantityOnHand < 0) inv.QuantityOnHand = 0;
                // ตรวจสอบป้องกันไม่ให้ยอดจองติดลบในฐานข้อมูล
                if (inv.ReservedQuantity < 0) inv.ReservedQuantity = 0;
            }
        }

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL ด้วย _db.SaveChanges(); เป็นอันปิดยอดเสร็จสมบูรณ์
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนสำเร็จผ่าน TempData เพื่อแสดงผลให้พนักงานทราบ
        TempData["SuccessMessage"] = $"บันทึกการส่งมอบสินค้าและชำระเงินสำเร็จ! ออกใบเสร็จจริงเลขที่ #{receiptNo}{memberLogMsg}";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์พร้อมแสดงข้อมูลใบสั่งจองที่เพิ่งปิดยอด
        return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
    }

    // Action สำหรับบันทึกลูกค้าไม่มารับสินค้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน NoShowPreorder รับ preorderId, reason (สาเหตุ) และ remark (หมายเหตุ)
    public IActionResult NoShowPreorder(int? preorderId, string reason, string? remark)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบสั่งจอง
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัส
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจอง";
            // เปลี่ยนเส้นทางกลับไปหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาใบสั่งจองในฐานข้อมูลพร้อมรายการสินค้าที่สั่งจอง
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบใบสั่งจองหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบป้องกันการทำรายการซ้ำหากออเดอร์ปิดสถานะไปแล้ว
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่าออเดอร์นี้ปิดไปแล้ว
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} ได้ดำเนินการปิดสถานะไปแล้ว";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ปรับเปลี่ยนสถานะใบสั่งจองเป็น Cancelled (ยกเลิกเนื่องจากไม่มารับ)
        preorder.CurrentStatus = "Cancelled";

        // วนลูปปลดสต็อกที่จองไว้กลับสู่ร้าน
        foreach (var item in preorder.Preorderitems)
        {
            // ค้นหาสต็อกสินค้าในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            // ตรวจสอบว่าพบข้อมูลสต็อกหรือไม่
            if (inv != null)
            {
                // ปลดภาระยอดจองออก (ลด ReservedQuantity โดยไม่ลด QuantityOnHand)
                inv.ReservedQuantity -= item.Quantity;
                // ป้องกันค่ายอดจองติดลบ
                if (inv.ReservedQuantity < 0) inv.ReservedQuantity = 0;
            }
        }

        // บันทึกประวัติและสาเหตุลงตาราง Systemlogs สำหรับเป็นหลักฐานการตรวจสอบ (Audit Trail)
        _db.Systemlogs.Add(new Systemlog
        {
            // รหัสโค้ดบันทึกเริ่มต้นด้วย NOSHOW ตามด้วยวันเวลา
            LogCode = "NOSHOW-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // ระดับความสำคัญของ Log
            LogLevel = "Warning",
            // โมดูลที่เกิดการบันทึก
            Module = "CounterPreorder",
            // ข้อความรายละเอียดระบุหมายเลขใบจอง สาเหตุ และหมายเหตุเพิ่มเติม
            Message = $"ลูกค้าไม่มารับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason} | หมายเหตุ: {remark ?? "ไม่มี"}",
            // รหัสพนักงานผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่บันทึก
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกลูกค้าไม่มารับสินค้าเรียบร้อยแล้ว (ใบจอง #{preorder.ReservationNumber}) ปลดสินค้ากลับสู่สต็อกสำเร็จ";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสใบจอง
        return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
    }

    // Action สำหรับบันทึกลูกค้าปฏิเสธไม่รับสินค้าหน้าร้าน (POST Method)
    [HttpPost]
    // ฟังก์ชัน RejectPreorder รับ preorderId, reason (เหตุผลการปฏิเสธ) และ remark (หมายเหตุ)
    public IActionResult RejectPreorder(int? preorderId, string reason, string? remark)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบสั่งจอง
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัส
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจอง";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาใบสั่งจองในฐานข้อมูลพร้อมรายการสินค้าข้างใน
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบข้อมูลในระบบหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบว่ารายการนี้ปิดสถานะไปแล้วหรือไม่
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่าไม่สามารถทำรายการซ้ำได้
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} ได้ดำเนินการปิดสถานะไปแล้ว";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ปรับเปลี่ยนสถานะใบสั่งจองเป็น Cancelled
        preorder.CurrentStatus = "Cancelled";

        // วนลูปปลดสต็อกที่จองไว้กลับสู่ร้าน
        foreach (var item in preorder.Preorderitems)
        {
            // ค้นหาสต็อกสินค้าในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            // ตรวจสอบว่าพบข้อมูลสต็อกหรือไม่
            if (inv != null)
            {
                // ปลดภาระยอดจองออก (ลด ReservedQuantity เพื่อคืนของให้ผู้อื่นสั่งจองได้)
                inv.ReservedQuantity -= item.Quantity;
                // ป้องกันค่ายอดจองติดลบ
                if (inv.ReservedQuantity < 0) inv.ReservedQuantity = 0;
            }
        }

        // บันทึกประวัติและสาเหตุการปฏิเสธลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // รหัสโค้ดบันทึกเริ่มต้นด้วย REJECT ตามด้วยวันเวลา
            LogCode = "REJECT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // ระดับความสำคัญของ Log
            LogLevel = "Warning",
            // โมดูลที่เกิดการบันทึก
            Module = "CounterPreorder",
            // ข้อความรายละเอียดระบุหมายเลขใบจอง สาเหตุ และรายละเอียดเพิ่มเติม
            Message = $"ลูกค้าไม่รับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason} | รายละเอียด: {remark ?? "ไม่มี"}",
            // รหัสพนักงานผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่บันทึก
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกการไม่รับสินค้าเรียบร้อยแล้ว (ใบจอง #{preorder.ReservationNumber}) ปลดสินค้ากลับสู่สต็อกแล้ว";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสใบจอง
        return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
    }

    // Action สำหรับบันทึกการส่งมอบของรางวัลให้ลูกค้าที่เคาน์เตอร์ (POST Method)
    [HttpPost]
    // ฟังก์ชัน CompleteRewardRedemption รับ redemptionId (รหัสรายการแลกของรางวัล)
    public IActionResult CompleteRewardRedemption(long redemptionId)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลรายการแลกของรางวัลจากตาราง Rewardredemptions พร้อมข้อมูล Reward และ User
        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId);

        // ตรวจสอบว่าพบรายการแลกของรางวัลในระบบหรือไม่
        if (redemption == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการแลกของรางวัลในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบว่ารายการนี้ถูกส่งมอบไปแล้วหรือไม่ เพื่อป้องกันการทำซ้ำ
        if (redemption.Status == "Completed")
        {
            // กำหนดข้อความแจ้งเตือนกรณีส่งมอบไปแล้ว
            TempData["ErrorMessage"] = "รายการนี้ได้ทำการส่งมอบของรางวัลไปแล้ว ไม่สามารถดำเนินการซ้ำได้";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสของรางวัล
            return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
        }

        // ปรับเปลี่ยนสถานะรายการแลกเป็น Completed
        redemption.Status = "Completed";
        // บันทึกวันและเวลาที่ส่งมอบของรางวัลสำเร็จ
        redemption.CompletedAt = DateTime.Now;

        // บันทึกประวัติการส่งมอบลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // รหัสโค้ดบันทึกเริ่มต้นด้วย RWD-DELIVER ตามด้วยวันเวลา
            LogCode = "RWD-DELIVER-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            // ระดับความสำคัญของ Log
            LogLevel = "Info",
            // โมดูลที่เกิดการบันทึก
            Module = "CounterPreorder",
            // ข้อความรายละเอียดระบุรหัสของรางวัล ชื่อของรางวัล และชื่อลูกค้า
            Message = $"ส่งมอบของรางวัลรหัส #{redemption.RedemptionCode} ({redemption.Reward.Title}) ให้ลูกค้า {redemption.User.FullName ?? redemption.User.Email} เรียบร้อยแล้ว",
            // รหัสพนักงานผู้ทำรายการ
            UserId = HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่บันทึก
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกส่งมอบ \"{redemption.Reward.Title}\" ให้ลูกค้าเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสของรางวัล
        return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
    }

    // Action แสดงหน้ารายการสินค้าสำหรับสแกนรับเข้าคลัง (GET Method)
    [HttpGet]
    // ฟังก์ชัน ScanReceive สำหรับเปิดหน้าสแกนรับสินค้า
    public IActionResult ScanReceive()
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการสินค้า 20 รายการแรกพร้อมข้อมูลสต็อกสินค้าคงคลัง
        var products = _db.Products
            // โหลดข้อมูล Inventory ของสินค้าแต่ละชิ้น
            .Include(p => p.Inventory)
            // จำกัดจำนวน 20 รายการ
            .Take(20)
            // แปลงเป็น List
            .ToList();

        // ส่งรายการสินค้าไปยัง View หน้า ScanReceive
        return View(products);
    }

    // Action สำหรับบันทึกการรับสินค้าเข้าสต็อกจริงลงตาราง Inventories (POST Method)
    [HttpPost]
    // ฟังก์ชัน ScanReceive รับรหัสสินค้า (sku) และจำนวนที่รับเข้า (quantity)
    public IActionResult ScanReceive(string? sku, int? quantity)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตัดช่องว่างหัวท้ายของรหัส SKU
        string cleanSku = (sku ?? "").Trim();
        // กำหนดจำนวนสินค้าที่รับเข้า หากไม่ระบุให้เริ่มต้นที่ 1 ชิ้น
        int addQty = (quantity.HasValue && quantity.Value > 0) ? quantity.Value : 1;

        // ตรวจสอบว่าผู้ใช้กรอกรหัส SKU หรือไม่ หากไม่กรอกให้ใช้รหัสตัวอย่าง
        if (string.IsNullOrWhiteSpace(cleanSku))
        {
            // กำหนดรหัส SKU สำรองตัวอย่าง
            cleanSku = "FIG-NARUTO-001";
        }

        // ค้นหาข้อมูลสินค้าในตาราง Products ด้วยรหัส SKU หรือบาร์โค้ด
        var product = _db.Products
            // โหลดข้อมูลสต็อกคงคลัง Inventory ร่วมด้วย
            .Include(p => p.Inventory)
            // เปรียบเทียบรหัส SKU หรือ บาร์โค้ด
            .FirstOrDefault(p => p.Sku == cleanSku || p.Barcode == cleanSku)
            // หากไม่พบ ให้เลือกสินค้าชิ้นแรกในระบบเป็นตัวอย่าง
            ?? _db.Products.Include(p => p.Inventory).FirstOrDefault();

        // ตรวจสอบว่าพบข้อมูลสินค้าหรือไม่
        if (product != null)
        {
            // ดึงข้อมูลสต็อกคงคลังของสินค้า
            var inv = product.Inventory;
            // กรณีสินค้านี้ยังไม่มีเรคคอร์ดในตาราง Inventories
            if (inv == null)
            {
                // สร้างอ็อบเจกต์ Inventory ใหม่
                inv = new Inventory
                {
                    // กำหนดรหัสสินค้า ProductId
                    ProductId = product.ProductId,
                    // กำหนดจำนวนสินค้าคงคลังเท่ากับยอดที่รับเข้า
                    QuantityOnHand = addQty,
                    // กำหนดยอดที่ถูกจองเป็น 0
                    ReservedQuantity = 0,
                    // กำหนดระดับ Safety Stock ขั้นต่ำเป็น 5 ชิ้น
                    SafetyStockLevel = 5,
                    // กำหนดตำแหน่งจัดเก็บในคลังสินค้า
                    StorageLocation = "Aisle-A-01",
                    // บันทึกวันและเวลาปัจจุบัน
                    UpdatedAt = DateTime.Now
                };
                // เพิ่มเรคคอร์ดสต็อกใหม่ลงตาราง Inventories
                _db.Inventories.Add(inv);
            }
            // กรณีมีข้อมูลสต็อกเดิมอยู่แล้วในระบบ
            else
            {
                // บวกจำนวนสินค้าที่รับเข้าเพิ่มเข้าไปใน QuantityOnHand
                inv.QuantityOnHand += addQty;
                // อัปเดตวันและเวลาแก้ไขล่าสุด
                inv.UpdatedAt = DateTime.Now;
            }

            // บันทึกประวัติการรับสินค้าเข้าสต็อกลงในตาราง Systemlogs
            _db.Systemlogs.Add(new Systemlog
            {
                // รหัสโค้ดบันทึกเริ่มต้นด้วย RESTOCK ตามด้วยวันเวลา
                LogCode = "RESTOCK-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                // ระดับความสำคัญของ Log
                LogLevel = "Info",
                // โมดูลสต็อกสินค้า
                Module = "Inventory",
                // ข้อความระบุชื่อสินค้า รหัส SKU จำนวนที่รับเข้า และยอดคงเหลือรวม
                Message = $"รับสินค้า {product.Name} (SKU: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าสต็อก คงเหลือรวม {inv.QuantityOnHand} ชิ้น",
                // รหัสผู้ใช้งานที่ทำรายการ
                UserId = HttpContext.Session.GetInt32("UserId") ?? 2,
                // วันเวลาที่บันทึก
                Timestamp = DateTime.Now
            });

            // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
            _db.SaveChanges();

            // กำหนดข้อความแจ้งผลสำเร็จผ่าน TempData เพื่อแสดงผลให้พนักงานทราบ
            TempData["SuccessMessage"] = $"รับสินค้า {product.Name} (SKU: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าคลังเรียบร้อยแล้ว (คงเหลือรวม: {inv.QuantityOnHand} ชิ้น)";
        }
        // กรณีค้นหาไม่พบรหัสสินค้า SKU ในฐานข้อมูล
        else
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดว่าไม่พบรหัสสินค้า
            TempData["ErrorMessage"] = $"ไม่พบรหัสสินค้า SKU: {cleanSku} ในระบบ";
        }

        // โหลดกลับมาที่หน้าสแกนรับสินค้าเดิม (Pure Form Navigation ไม่ใช้ AJAX)
        return RedirectToAction("ScanReceive");
    }
}
