using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับจัดการงานฝั่งเจ้าของร้าน เช่น สินค้า ของแลกแต้ม โปรโมชั่น และรายงานยอดขายประจำเดือน
public class OwnerController : Controller
{
    // ประกาศตัวแปรเก็บบริบทฐานข้อมูลสำหรับเชื่อมต่อตารางในฐานข้อมูล
    private readonly Csi402dbContext _db;

    // ประกาศตัวแปรเก็บสภาพแวดล้อมของเว็บโฮสต์สำหรับจัดการบันทึกไฟล์รูปภาพลงโฟลเดอร์ wwwroot
    private readonly IWebHostEnvironment _env;

    // คอนสตรัคเตอร์รับบริบทฐานข้อมูลและสภาพแวดล้อมเว็บโฮสต์ผ่านระบบฉีดพึ่งพา
    public OwnerController(Csi402dbContext db, IWebHostEnvironment env)
    {
        // กำหนดค่าบริบทฐานข้อมูลให้กับตัวแปรภายในคลาส
        _db = db;
        // กำหนดค่าสภาพแวดล้อมเว็บโฮสต์ให้กับตัวแปรภายในคลาส
        _env = env;
    }

    // ตรวจสอบสิทธิ์ตามมาตรฐาน 1 เบราว์เซอร์ 1 บทบาท ก่อนรัน Action ใน OwnerController
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของเบราว์เซอร์
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // หากเบราว์เซอร์ล็อกอินด้วยบทบาทอื่นที่ไม่ใช่เจ้าของร้าน ให้บังคับเปลี่ยนเส้นทางไปยังบทบาทนั้นทันที
        if (!string.IsNullOrEmpty(activeRole) && activeRole != "owner")
        {
            // ดึงเส้นทางหน้าหลักของบทบาทที่ล็อกอินอยู่
            var (targetAct, targetCtrl) = GetRoleHomeRoute(activeRole);
            // บังคับเปลี่ยนเส้นทางไปยังบทบาทปัจจุบัน
            context.Result = RedirectToAction(targetAct, targetCtrl);
            // จบการทำงานของตัวกรอง
            return;
        }
        // เรียกการทำงานพื้นฐานของคลาสแม่
        base.OnActionExecuting(context);
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทเจ้าของร้าน
    private bool IsAuthorizedOwner()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของผู้ใช้งาน
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // อ่านค่ารหัสผู้ใช้งานเฉพาะเจ้าของร้านจากเซสชัน
        var ownerId = HttpContext.Session.GetInt32("OwnerUserId");
        // ตรวจสอบว่าบทบาทตรงกับเจ้าของร้านหรือมีเซสชันของเจ้าของร้าน
        return role == "owner" || ownerId.HasValue;
    }

    // แอ็กชันหน้าหลักของเจ้าของร้าน นำทางไปยังหน้าจัดการโปรโมชั่น
    public IActionResult Index()
    {
        // เปลี่ยนเส้นทางไปยังหน้ารายการโปรโมชั่นทั้งหมด
        return RedirectToAction("Promotions");
    }

    // READ: แสดงหน้ารายการโปรโมชั่นทั้งหมดจากตารางโปรโมชั่นในฐานข้อมูล
    public IActionResult Promotions()
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการโปรโมชั่นทั้งหมดจากตารางโปรโมชั่นในฐานข้อมูล
        var promotions = _db.Promotions.ToList();
        // ส่งรายการโปรโมชั่นไปแสดงผลที่หน้าจัดการโปรโมชั่น
        return View(promotions);
    }

    // CREATE: แสดงหน้าแบบฟอร์มสร้างโปรโมชั่นใหม่
    [HttpGet]
    // แอ็กชันแสดงแบบฟอร์มเพิ่มข้อมูลโปรโมชันใหม่
    public IActionResult AddPromotion()
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ส่งมุมมองหน้าสร้างโปรโมชั่นใหม่
        return View();
    }

    // ฟังก์ชันช่วยตรวจสอบและปรับแต่งค่าโปรโมชั่นให้ถูกต้องตามกฎธุรกิจ
    private string? ValidateAndNormalizePromotion(Promotion data, int? ignorePromotionId = null)
    {
        // ตรวจสอบว่ามีการระบุชื่อโปรโมชั่นหรือไม่
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            // คืนค่าข้อความแจ้งเตือนกรณีไม่ระบุชื่อ
            return "กรุณาระบุชื่อโปรโมชั่นให้ครบถ้วน";
        }
        // ตัดช่องว่างส่วนเกินของชื่อโปรโมชั่น
        data.Name = data.Name.Trim();

        // ตรวจสอบว่ามีการระบุรหัสคูปองหรือไม่ หากไม่มีให้สุ่มรหัสอัตโนมัติ
        if (string.IsNullOrWhiteSpace(data.VoucherCode))
        {
            // กำหนดรหัสคูปองอัตโนมัติขึ้นต้นด้วย FH- ตามด้วยปีเดือนวันเวลา
            data.VoucherCode = "FH-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        // ปรับแต่งรหัสคูปองเป็นตัวพิมพ์ใหญ่และตัดช่องว่าง
        data.VoucherCode = NormalizeCode(data.VoucherCode);

        // ตรวจสอบว่ารหัสคูปองซ้ำกับโปรโมชั่นอื่นในฐานข้อมูลหรือไม่
        bool duplicateCode = _db.Promotions.Any(p =>
            // ตรวจสอบว่าไม่ใช่รหัสโปรโมชันของตนเอง
            p.PromotionId != (ignorePromotionId ?? 0) &&
            // ตรวจสอบว่ารหัสคูปองซ้ำกับโปรโมชันอื่นในระบบหรือไม่
            p.VoucherCode.ToUpper() == data.VoucherCode);
        // หากพบรหัสซ้ำให้แจ้งเตือน
        if (duplicateCode)
        {
            // คืนค่าข้อความแจ้งเตือนกรณีรหัสคูปองซ้ำ
            return $"รหัสคูปอง \"{data.VoucherCode}\" มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
        }

        // ปรับแต่งประเภทโปรโมชั่นเป็นตัวพิมพ์เล็ก (ค่าเริ่มต้นคือ percent)
        data.PromoType = string.IsNullOrWhiteSpace(data.PromoType) ? "percent" : data.PromoType.Trim().ToLowerInvariant();
        // ปรับแต่งขอบเขตสินค้าเป็นตัวพิมพ์เล็ก (ค่าเริ่มต้นคือ all)
        data.ApplicableScope = string.IsNullOrWhiteSpace(data.ApplicableScope) ? "all" : data.ApplicableScope.Trim().ToLowerInvariant();

        // ตรวจสอบว่ามูลค่าส่วนลดไม่ติดลบ
        if (data.DiscountValue < 0)
        {
            // คืนค่าข้อความแจ้งเตือนกรณีส่วนลดติดลบ
            return "มูลค่าส่วนลดต้องไม่น้อยกว่า 0";
        }
        // ตรวจสอบว่ากรณีส่วนลดแบบเปอร์เซ็นต์ต้องไม่เกิน 100%
        if ((data.PromoType == "percent" || data.PromoType == "percentage") && data.DiscountValue > 100)
        {
            // คืนค่าข้อความแจ้งเตือนกรณีส่วนลดเกินร้อยละ 100
            return "ส่วนลดแบบเปอร์เซ็นต์ต้องไม่เกิน 100%";
        }
        // ตรวจสอบว่ายอดสั่งซื้อขั้นต่ำไม่ติดลบ
        if (data.MinimumSpend.HasValue && data.MinimumSpend.Value < 0)
        {
            // คืนค่าข้อความแจ้งเตือนกรณียอดขั้นต่ำติดลบ
            return "ยอดสั่งซื้อขั้นต่ำต้องไม่ติดลบ";
        }
        // ตรวจสอบจำนวนสิทธิ์การใช้งานสูงสุด
        if (data.UsageLimit.HasValue && data.UsageLimit.Value < 1)
        {
            // คืนค่าข้อความแจ้งเตือนกรณีจำนวนสิทธิ์น้อยกว่า 1
            return "จำนวนสิทธิ์การใช้งานสูงสุดต้องอย่างน้อย 1 สิทธิ์ (หรือเว้นว่างหากไม่จำกัด)";
        }

        // ปรับแต่งวันที่เริ่มต้นตามปฏิทินสากล
        data.StartDate = NormalizeGregorianDate(data.StartDate);
        // ปรับแต่งวันที่สิ้นสุดตามปฏิทินสากล
        data.EndDate = NormalizeGregorianDate(data.EndDate);
        // ตรวจสอบว่าวันสิ้นสุดต้องไม่เกิดขึ้นก่อนวันเริ่มต้น
        if (data.EndDate < data.StartDate)
        {
            // คืนค่าข้อความแจ้งเตือนกรณีช่วงวันที่ไม่ถูกต้อง
            return "วันสิ้นสุดโปรโมชั่นต้องไม่ก่อนวันเริ่มต้น";
        }

        // คืนค่าว่างกรณีข้อมูลผ่านการตรวจสอบทั้งหมด
        return null;
    }

    // CREATE: บันทึกโปรโมชั่นใหม่ลงในฐานข้อมูลจริง
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันประมวลผลการบันทึกข้อมูลโปรโมชันใหม่เข้าสู่ระบบ
    public IActionResult AddPromotion(Promotion? data)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบว่ามีข้อมูลส่งมาจากแบบฟอร์มหรือไม่
        if (data == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบข้อมูลโปรโมชั่นที่ส่งมา";
            // ส่งกลับไปยังหน้าแบบฟอร์มเดิม
            return View(data);
        }

        // ตรวจสอบความถูกต้องของข้อมูลโปรโมชั่น
        string? validationError = ValidateAndNormalizePromotion(data);
        // ตรวจสอบว่ามีข้อผิดพลาดหรือไม่
        if (!string.IsNullOrEmpty(validationError))
        {
            // กำหนดข้อความแจ้งเตือนตามข้อผิดพลาดที่พบ
            TempData["ErrorMessage"] = validationError;
            // ส่งกลับไปยังหน้าแบบฟอร์มเดิมพร้อมข้อมูลเดิม
            return View(data);
        }

        // กำหนดจำนวนครั้งที่ถูกใช้งานเริ่มต้นเป็นศูนย์
        data.CurrentUsageCount = 0;
        // กำหนดสถานะเปิดใช้งานเริ่มต้น
        data.IsActive = data.IsActive ?? false;

        // เพิ่มโปรโมชั่นใหม่ลงในบริบทฐานข้อมูล
        _db.Promotions.Add(data);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // บันทึกข้อความแจ้งเตือนผลสำเร็จ
        TempData["SuccessMessage"] = "เพิ่มโปรโมชั่นใหม่ \"" + data.Name + "\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายการโปรโมชั่นทั้งหมด
        return RedirectToAction("Promotions");
    }

    // READ: แสดงหน้าแบบฟอร์มแก้ไขข้อมูลโปรโมชั่นตามรหัสโปรโมชั่นที่ระบุ
    [HttpGet]
    // แอ็กชันแสดงแบบฟอร์มแก้ไขข้อมูลโปรโมชันตามรหัสที่ระบุ
    public IActionResult EditPromotion(int? id)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ประกาศตัวแปรเก็บข้อมูลโปรโมชั่นที่ต้องการแก้ไข
        Promotion? promo = null;
        // ตรวจสอบว่ามีการระบุรหัสโปรโมชั่นที่มากกว่าศูนย์หรือไม่
        if (id.HasValue && id.Value > 0)
        {
            // ค้นหาโปรโมชั่นจากตารางโปรโมชั่นในฐานข้อมูลตามรหัสที่ระบุ
            promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id.Value);
        }

        // ส่งข้อมูลโปรโมชั่นไปแสดงในหน้าแบบฟอร์มแก้ไข
        return View(promo);
    }

    // UPDATE: บันทึกข้อมูลการแก้ไขโปรโมชั่นลงในฐานข้อมูลจริง
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันประมวลผลการแก้ไขข้อมูลโปรโมชัน
    public IActionResult EditPromotion(Promotion data)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลโปรโมชั่นที่ต้องการแก้ไขจากรหัสโปรโมชั่นในฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == data.PromotionId);
        // ตรวจสอบว่าพบข้อมูลโปรโมชั่นในฐานข้อมูลหรือไม่
        if (promo != null)
        {
            // ตรวจสอบความถูกต้องของข้อมูลโปรโมชั่นที่ส่งมาแก้ไข
            string? validationError = ValidateAndNormalizePromotion(data, promo.PromotionId);
            // ตรวจสอบว่ามีข้อผิดพลาดหรือไม่
            if (!string.IsNullOrEmpty(validationError))
            {
                // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = validationError;
                // ส่งกลับไปยังหน้าแบบฟอร์มเดิมพร้อมข้อมูล
                return View(data);
            }

            // อัปเดตชื่อโปรโมชั่น
            promo.Name = data.Name;
            // อัปเดตรหัสคูปอง
            promo.VoucherCode = data.VoucherCode;
            // อัปเดตประเภทโปรโมชั่น
            promo.PromoType = data.PromoType;
            // อัปเดตมูลค่าส่วนลด
            promo.DiscountValue = data.DiscountValue;
            // อัปเดตยอดสั่งซื้อขั้นต่ำ
            promo.MinimumSpend = data.MinimumSpend;
            // อัปเดตขอบเขตสินค้า
            promo.ApplicableScope = data.ApplicableScope;
            // อัปเดตวันที่เริ่มต้น
            promo.StartDate = data.StartDate;
            // อัปเดตวันที่สิ้นสุด
            promo.EndDate = data.EndDate;
            // อัปเดตจำนวนสิทธิ์สูงสุด
            promo.UsageLimit = data.UsageLimit;
            // อัปเดตสถานะเปิดใช้งาน
            promo.IsActive = data.IsActive ?? false;

            // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูลจริง
            _db.SaveChanges();

            // กำหนดข้อความแจ้งเตือนความสำเร็จ
            TempData["SuccessMessage"] = $"บันทึกการแก้ไขโปรโมชั่น \"{promo.Name}\" เรียบร้อยแล้ว";
            // เปลี่ยนเส้นทางกลับไปหน้ารายการโปรโมชั่นทั้งหมด
            return RedirectToAction("Promotions");
        }

        // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูลโปรโมชั่น
        TempData["ErrorMessage"] = "ไม่พบข้อมูลโปรโมชั่นที่ต้องการแก้ไข";
        // เปลี่ยนเส้นทางกลับไปหน้ารายการโปรโมชั่น
        return RedirectToAction("Promotions");
    }

    // UPDATE: สลับสถานะเปิดหรือปิดการใช้งานโปรโมชั่นในฐานข้อมูลแบบ Post-Redirect-Get (ไม่ใช้ AJAX)
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันสลับสถานะเปิดหรือปิดการใช้งานโปรโมชัน
    public IActionResult TogglePromotion(int id)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาโปรโมชั่นตามรหัสในฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        // ตรวจสอบว่าพบโปรโมชั่นหรือไม่
        if (promo != null)
        {
            // สลับสถานะเปิดใช้งานเป็นตรงกันข้าม
            promo.IsActive = !(promo.IsActive ?? false);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
            _db.SaveChanges();
            // กำหนดข้อความป้ายสถานะภาษาไทย
            string statusLabel = promo.IsActive == true ? "เปิดใช้งาน" : "ปิดใช้งาน";
            // บันทึกข้อความแจ้งเตือนผลลัพธ์
            TempData["SuccessMessage"] = $"อัปเดตสถานะโปรโมชั่น \"{promo.Name}\" เป็น {statusLabel} เรียบร้อยแล้ว";
        }
        // มิฉะนั้น
        else
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบโปรโมชั่น
            TempData["ErrorMessage"] = "ไม่พบโปรโมชั่นที่ต้องการเปลี่ยนสถานะ";
        }

        // เปลี่ยนเส้นทางกลับไปหน้ารายการโปรโมชั่น
        return RedirectToAction("Promotions");
    }

    // DELETE: ปิดการใช้งานโปรโมชั่นเพื่อรักษาประวัติการใช้งานเดิมในฐานข้อมูลแบบ Post-Redirect-Get (ไม่ใช้ AJAX)
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันลบข้อมูลโปรโมชันออกจากระบบ
    public IActionResult DeletePromotion(int id)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาโปรโมชั่นจากรหัสในฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        // ตรวจสอบว่าพบข้อมูลหรือไม่
        if (promo != null)
        {
            // ปรับสถานะเป็นปิดใช้งานเพื่อรักษาประวัติการขายเดิม
            promo.IsActive = false;
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
            _db.SaveChanges();
            // แจ้งเตือนข้อความสำเร็จ
            TempData["SuccessMessage"] = $"ปิดการใช้งานโปรโมชั่น \"{promo.Name}\" เรียบร้อยแล้ว (คงประวัติเดิมไว้)";
        }
        // มิฉะนั้น
        else
        {
            // แจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบโปรโมชั่นที่ต้องการลบ";
        }

        // เปลี่ยนเส้นทางกลับไปหน้ารายการโปรโมชั่น
        return RedirectToAction("Promotions");
    }

    // READ: แสดงหน้ารายงานสรุปยอดขายประจำเดือนสำหรับเจ้าของร้าน โดยดึงตัวเลขจริงจากฐานข้อมูลชุดเดียวกับฝ่ายบัญชี
    public IActionResult MonthlyReport(string? selectedMonth, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างข้อมูลรายงานจากฐานข้อมูลจริงตามช่วงเดือนที่เลือกและหมายเลขหน้าปัจจุบัน
        var reportModel = FinancialReportViewModel.BuildReport(_db, selectedMonth, page);

        // ส่งข้อมูลรายงานไปยังหน้าแสดงผล
        return View(reportModel);
    }

    // READ: ส่งออกรายงานสรุปสต็อกและยอดขายประจำเดือนเป็นไฟล์ข้อความตารางภาษาไทยจากฐานข้อมูลจริง
    public IActionResult ExportMonthlyReport(string? selectedMonth)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลทั้งหมดโดยไม่แบ่งหน้าเพื่อส่งออกครบทุกรายการสินค้า
        var reportModel = FinancialReportViewModel.BuildReport(_db, selectedMonth, 1);
        // ดึงรายการสินค้าทั้งหมดพร้อมหมวดหมู่และสต็อกสำหรับทำรายงาน
        var fullRows = _db.Products
            // รวมข้อมูลตารางหมวดหมู่สินค้า
            .Include(p => p.Category)
            // รวมข้อมูลตารางสต็อกสินค้าคงคลัง
            .Include(p => p.Inventory)
            // แปลงผลลัพธ์เป็นรายการในหน่วยความจำ
            .ToList();

        // สร้างตัวประกอบข้อความสำหรับไฟล์รายงาน CSV
        var sb = new System.Text.StringBuilder();
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine($"รายงานสรุปยอดขายและสต็อกสินค้า - {reportModel.SelectedMonthLabel}");
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine($"ยอดรายได้รวม,{reportModel.TotalRevenue:F2}");
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine($"รายได้จากการขายหน้าร้าน,{reportModel.StorefrontRevenue:F2}");
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine($"รายรับอื่นในสมุดบัญชี,{reportModel.LedgerIncome:F2}");
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine($"รายจ่ายและต้นทุนที่บันทึก,{reportModel.RecordedExpenses:F2}");
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine();
        // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
        sb.AppendLine("รหัสสินค้า,ชื่อสินค้า,หมวดหมู่,คงเหลือในคลัง,ราคาขายปกติ");
        // วนลูปประมวลผลสินค้าแต่ละรายการเพื่อเขียนข้อมูลลง CSV
        foreach (var item in fullRows)
        {
            // ปรับแต่งข้อความชื่อสินค้าสำหรับไฟล์ CSV
            var safeName = (item.Name ?? "").Replace("\"", "\"\"");
            // ปรับแต่งข้อความหมวดหมู่สินค้าสำหรับไฟล์ CSV
            var safeCat = (item.Category?.CategoryName ?? "ทั่วไป").Replace("\"", "\"\"");
            // เพิ่มบรรทัดข้อมูลลงในข้อความ CSV
            sb.AppendLine($"\"{item.Sku}\",\"{safeName}\",\"{safeCat}\",{item.Inventory?.QuantityOnHand ?? 0},{item.RetailPrice:F2}");
        }

        // ดึงไบต์ UTF-8 BOM เพื่อรองรับการแสดงผลภาษาไทยในโปรแกรม Excel
        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        // แปลงข้อความรายงานเป็นชุดข้อมูลไบต์ UTF-8
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        // รวมข้อมูลไบต์ BOM และเนื้อหารายงานเข้าด้วยกัน
        var finalBytes = preamble.Concat(bodyBytes).ToArray();
        // กำหนดชื่อไฟล์รายงานตามช่วงเวลาที่เลือก
        string fileName = $"FriendHouse_Report_{(string.IsNullOrWhiteSpace(selectedMonth) ? DateTime.Today.ToString("yyyyMM") : selectedMonth)}.csv";

        // ส่งคืนไฟล์ข้อมูลให้ผู้ใช้งานดาวน์โหลด
        return File(finalBytes, "text/csv; charset=utf-8", fileName);
    }

    // ฟังก์ชันช่วยดึงรายการเส้นทางรูปภาพทั้งหมดที่มีอยู่ในโฟลเดอร์ wwwroot/media/images
    private List<string> GetAvailableProductImages()
    {
        // กำหนดเส้นทางเต็มของโฟลเดอร์เก็บรูปภาพสินค้า
        var imagesDir = Path.Combine(_env.WebRootPath, "media", "images");
        // กรณีไม่มีโฟลเดอร์รูปภาพ ให้คืนค่ารายการว่าง
        if (!Directory.Exists(imagesDir)) return new List<string>();

        // นามสกุลไฟล์รูปภาพที่อนุญาตให้นำมาแสดงผล
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
        // อ่านชื่อไฟล์ทั้งหมดในโฟลเดอร์และแปลงเป็นเส้นทางเว็บแบบสัมพัทธ์
        return Directory.GetFiles(imagesDir)
            // กรองข้อมูลตามเงื่อนไขที่กำหนด
            .Where(f => allowedExt.Contains(Path.GetExtension(f)))
            // เลือกเฉพาะข้อมูลคอลัมน์ที่ต้องการ
            .Select(f => "/media/images/" + Path.GetFileName(f))
            // จัดเรียงลำดับข้อมูลจากน้อยไปมาก
            .OrderBy(p => p)
            // แปลงผลลัพธ์เป็นรายการในหน่วยความจำ
            .ToList();
    }

    // ฟังก์ชันช่วยอัปโหลดและบันทึกไฟล์รูปภาพสินค้าหรือของรางวัล (ต้นฉบับ + ภาพการ์ด 1:1 _card.png)
    private string SaveUploadedMediaImage(IFormFile? imageFile, string? fallbackUrl, IFormFile? thumbFile = null)
    {
        // กำหนดเส้นทางโฟลเดอร์สำหรับจัดเก็บรูปภาพในระบบ
        var imagesDir = Path.Combine(_env.WebRootPath, "media", "images");
        // สร้างโฟลเดอร์จัดเก็บไฟล์หากยังไม่มีในระบบ
        Directory.CreateDirectory(imagesDir);
        // กำหนดเส้นทางรูปภาพสำรองเริ่มต้นกรณีไม่มีการระบุ
        var targetOriginalUrl = string.IsNullOrWhiteSpace(fallbackUrl)
            // ใช้รูปภาพเริ่มต้นของระบบ
            ? "/media/images/tanjiro_kamado.png"
            // หรือใช้รูปภาพสำรองเดิมที่ระบุมา
            : fallbackUrl.Trim();

        // กรณีมีการอัปโหลดไฟล์ต้นฉบับใหม่ ให้บันทึกทั้งไฟล์ต้นฉบับและไฟล์การ์ด 1:1 คู่กัน
        if (imageFile != null && imageFile.Length > 0)
        {
            // ดึงนามสกุลไฟล์รูปภาพที่อัปโหลด
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            // ตรวจสอบนามสกุลไฟล์รูปภาพที่อนุญาตให้อัปโหลด
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
            {
                // กำหนดนามสกุลไฟล์เริ่มต้นเป็น png
                ext = ".png";
            }

            // ดึงชื่อไฟล์รูปภาพเดิมและตัดนามสกุลออก
            var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
                // ตัดช่องว่างส่วนเกินหัวท้าย
                .Trim()
                // แปลงตัวอักษรเป็นพิมพ์เล็กทั้งหมด
                .ToLowerInvariant()
                // แทนที่ช่องว่างด้วยขีดล่างเพื่อความปลอดภัยของชื่อไฟล์
                .Replace(" ", "_");
            // กำหนดคำนำหน้าชื่อไฟล์รูปภาพที่ปลอดภัย
            var safePrefix = string.IsNullOrWhiteSpace(rawName) ? "product" : rawName;
            // สร้างชื่อไฟล์พื้นฐานประกอบด้วยคำนำหน้าและวันเวลา
            var baseName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmss}";
            // สร้างชื่อไฟล์รูปภาพพร้อมนามสกุล
            var fileName = $"{baseName}{ext}";
            // รวมเส้นทางโฟลเดอร์และชื่อไฟล์รูปภาพ
            var fullPath = Path.Combine(imagesDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                // คัดลอกและบันทึกไฟล์รูปภาพลงในระบบจัดเก็บ
                imageFile.CopyTo(stream);
            }

            // บันทึกไฟล์เวอร์ชันการ์ด 1:1 คู่กันหากผู้ใช้ปรับกรอบภาพจากตัวครอปมาด้วย
            if (thumbFile != null && thumbFile.Length > 0)
            {
                // กำหนดชื่อไฟล์รูปภาพขนาดย่อสำหรับการ์ดสินค้า
                var cardFileName = $"{baseName}_card.png";
                // รวมเส้นทางโฟลเดอร์และชื่อไฟล์รูปภาพขนาดย่อ
                var cardFullPath = Path.Combine(imagesDir, cardFileName);
                using var thumbStream = new FileStream(cardFullPath, FileMode.Create);
                // คัดลอกและบันทึกไฟล์รูปภาพขนาดย่อลงในระบบ
                thumbFile.CopyTo(thumbStream);
            }

            // คืนค่าเส้นทางรูปภาพที่บันทึก
            return "/media/images/" + fileName;
        }

        // กรณีแก้ไขสินค้าเดิมและปรับกรอบภาพใหม่โดยไม่ได้อัปโหลดไฟล์ต้นฉบับทับ
        if (thumbFile != null && thumbFile.Length > 0 && targetOriginalUrl.StartsWith("/media/images/", StringComparison.OrdinalIgnoreCase))
        {
            // สกัดชื่อไฟล์จากเส้นทางรูปภาพเดิม
            var relFileName = Path.GetFileName(targetOriginalUrl.Split('?', '#')[0]);
            // ดึงชื่อไฟล์โดยไม่รวมนามสกุล
            var relBase = Path.GetFileNameWithoutExtension(relFileName);
            // ตรวจสอบว่ามีชื่อไฟล์เดิมที่ถูกต้องหรือไม่
            if (!string.IsNullOrWhiteSpace(relBase))
            {
                // ตรวจสอบว่าชื่อไฟล์ลงท้ายด้วยคำว่าการ์ดหรือไม่
                if (relBase.EndsWith("_card", StringComparison.OrdinalIgnoreCase))
                {
                    // ตัดคำว่าการ์ดออกจากส่วนท้ายชื่อไฟล์
                    relBase = relBase[..^5];
                }
                // รวมเส้นทางโฟลเดอร์และชื่อไฟล์รูปภาพขนาดย่อ
                var cardFullPath = Path.Combine(imagesDir, $"{relBase}_card.png");
                using var thumbStream = new FileStream(cardFullPath, FileMode.Create);
                // คัดลอกและบันทึกไฟล์รูปภาพขนาดย่อลงในระบบ
                thumbFile.CopyTo(thumbStream);
            }
        }

        // คืนค่าเส้นทางรูปภาพที่บันทึก
        return targetOriginalUrl;
    }

    // READ: แสดงหน้าจัดการสินค้าและของแลกแต้มสำหรับเจ้าของร้าน พร้อมค้นหา กรอง และแบ่งหน้า
    public IActionResult Catalog(string? tab, string? searchQuery, string? categoryFilter, string? saleModeFilter, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // กำหนดแท็บที่กำลังเปิดใช้งานอยู่ (ค่าเริ่มต้นคือรายการสินค้าขาย)
        string activeTab = string.Equals(tab, "rewards", StringComparison.OrdinalIgnoreCase) ? "rewards" : "products";

        // ดึงข้อมูลสินค้าทั้งหมดพร้อมหมวดหมู่และสต็อกสินค้าเรียงจากรายการใหม่ไปเก่า
        var allProducts = _db.Products
            // รวมข้อมูลตารางหมวดหมู่สินค้า
            .Include(p => p.Category)
            // รวมข้อมูลตารางสต็อกสินค้าคงคลัง
            .Include(p => p.Inventory)
            // จัดเรียงลำดับจากรายการล่าสุดลงไป
            .OrderByDescending(p => p.CreatedAt)
            // แปลงผลลัพธ์เป็นรายการในหน่วยความจำ
            .ToList();

        // ดึงข้อมูลของแลกแต้มทั้งหมดเรียงจากรายการใหม่ไปเก่า
        var allRewards = _db.Rewards
            // จัดเรียงลำดับจากรายการล่าสุดลงไป
            .OrderByDescending(r => r.CreatedAt)
            // แปลงผลลัพธ์เป็นรายการในหน่วยความจำ
            .ToList();

        // ดึงรายการหมวดหมู่สินค้าทั้งหมดจากฐานข้อมูล
        var categories = _db.Categories.OrderBy(c => c.CategoryName).ToList();

        // เตรียมข้อความค้นหาแบบรองรับการพิมพ์โดยไม่ต้องใส่เครื่องหมายขีดลบ
        string cleanSearch = (searchQuery ?? "").Trim().ToLower();
        // ตัดเครื่องหมายขีดและช่องว่างเพื่อการค้นหารหัสสินค้าแบบยืดหยุ่น
        string noHyphenSearch = cleanSearch.Replace("-", "").Replace("#", "").Replace(" ", "");

        // กำหนดจำนวนรายการต่อหน้ามาตรฐานเป็น 10 รายการ
        int pageSize = 10;
        // กำหนดตัวแปรนับจำนวนรายการทั้งหมดเริ่มต้น
        int totalCount = 0;
        // กำหนดตัวแปรหมายเลขหน้าปัจจุบันเริ่มต้น
        int currentPage = 1;
        // สร้างรายการสินค้าสำหรับการแสดงผลแบบแบ่งหน้า
        var pagedProducts = new List<Product>();
        // สร้างรายการของรางวัลสำหรับการแสดงผลแบบแบ่งหน้า
        var pagedRewards = new List<Reward>();

        // ตรวจสอบว่ากำลังแสดงผลในแท็บสินค้าขายหรือแท็บของแลกแต้ม
        if (activeTab == "products")
        {
            // กรองสินค้าตามคำค้นหา (ชื่อสินค้า ซีรีส์ แบรนด์ รหัส SKU หรือบาร์โค้ด)
            IEnumerable<Product> filteredProducts = allProducts;
            // ตรวจสอบว่ามีการระบุคำค้นหาเข้ามาหรือไม่
            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                // กรองรายการสินค้าตามคำค้นหาที่ระบุ
                filteredProducts = filteredProducts.Where(p =>
                    // เงื่อนไขชื่อสินค้ามีคำค้นหาที่ระบุ
                    p.Name.ToLower().Contains(cleanSearch) ||
                    // เงื่อนไขรหัสสินค้ามีคำค้นหาที่ระบุ
                    p.Sku.ToLower().Contains(cleanSearch) ||
                    // เงื่อนไขรหัสสินค้าแบบไม่มีขีดตรงกับคำค้นหา
                    (!string.IsNullOrEmpty(noHyphenSearch) && p.Sku.Replace("-", "").ToLower().Contains(noHyphenSearch)) ||
                    // เงื่อนไขบาร์โค้ดมีคำค้นหาที่ระบุ
                    (p.Barcode != null && p.Barcode.ToLower().Contains(cleanSearch)) ||
                    // เงื่อนไขชื่อซีรีส์มีคำค้นหาที่ระบุ
                    (p.SeriesName != null && p.SeriesName.ToLower().Contains(cleanSearch)) ||
                    // เงื่อนไขชื่อแบรนด์มีคำค้นหาที่ระบุ
                    (p.BrandName != null && p.BrandName.ToLower().Contains(cleanSearch)));
            }

            // กรองตามหมวดหมู่ที่เลือก
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all" && int.TryParse(categoryFilter, out int catId))
            {
                // กรองรายการสินค้าตามคำค้นหาที่ระบุ
                filteredProducts = filteredProducts.Where(p => p.CategoryId == catId);
            }

            // กรองตามสถานะสินค้า (วางขายปกติ สินค้าหมดสต็อก หรือซ่อนจากหน้าร้าน)
            if (!string.IsNullOrWhiteSpace(saleModeFilter) && saleModeFilter != "all")
            {
                // กรองรายการสินค้าตามโหมดการวางขายที่เลือก
                filteredProducts = saleModeFilter switch
                {
                    // โหมดแสดงเฉพาะสินค้าพร้อมขายหน้าร้าน
                    "sale" => filteredProducts.Where(p => p.IsActive == true && !p.IsPreorder && p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0),
                    // โหมดแสดงสินค้าที่สินค้าหมดสต็อก
                    "outofstock" => filteredProducts.Where(p => p.IsActive == true && (p.IsPreorder || p.Inventory == null || (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) <= 0)),
                    // โหมดแสดงสินค้าที่ปิดการแสดงผล
                    "hidden" => filteredProducts.Where(p => p.IsActive != true),
                    // กรณีโหมดอื่นให้แสดงรายการทั้งหมดตามปกติ
                    _ => filteredProducts
                };
            }

            // คำนวณการแบ่งหน้าสำหรับรายการสินค้าที่ผ่านการกรองแล้ว
            var productList = filteredProducts.ToList();
            // ดึงจำนวนรายการสินค้าทั้งหมดที่ผ่านการกรอง
            totalCount = productList.Count;
            // คำนวณจำนวนหน้าทั้งหมดจากจำนวนรายการและขนาดหน้า
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            // จำกัดหมายเลขหน้าให้อยู่ในช่วงที่ถูกต้องเสมอ
            currentPage = Math.Clamp(page, 1, totalPages);
            // ตัดแบ่งรายการสินค้าตามหน้าปัจจุบันและขนาดหน้า
            pagedProducts = productList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
        }
        // มิฉะนั้น
        else
        {
            // กรองของแลกแต้มตามคำค้นหา (ชื่อของรางวัล รหัส แบรนด์ หรือหมวดหมู่)
            IEnumerable<Reward> filteredRewards = allRewards;
            // ตรวจสอบว่ามีการระบุคำค้นหาเข้ามาหรือไม่
            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                // กรองรายการของรางวัลตามคำค้นหาที่ระบุ
                filteredRewards = filteredRewards.Where(r =>
                    // เงื่อนไขชื่อของรางวัลมีคำค้นหาที่ระบุ
                    r.Title.ToLower().Contains(cleanSearch) ||
                    // เงื่อนไขรหัสของรางวัลมีคำค้นหาที่ระบุ
                    r.RewardCode.ToLower().Contains(cleanSearch) ||
                    // เงื่อนไขรหัสสินค้าแบบไม่มีขีดตรงกับคำค้นหา
                    (!string.IsNullOrEmpty(noHyphenSearch) && r.RewardCode.Replace("-", "").ToLower().Contains(noHyphenSearch)) ||
                    // เงื่อนไขแบรนด์ของรางวัลมีคำค้นหาที่ระบุ
                    (r.Brand != null && r.Brand.ToLower().Contains(cleanSearch)) ||
                    // เงื่อนไขหมวดหมู่ของรางวัลมีคำค้นหาที่ระบุ
                    r.CategoryTag.ToLower().Contains(cleanSearch));
            }

            // กรองตามสถานะการเปิดให้แลกแต้ม
            if (!string.IsNullOrWhiteSpace(saleModeFilter) && saleModeFilter != "all")
            {
                // กรองรายการของรางวัลตามโหมดสถานะที่เลือก
                filteredRewards = saleModeFilter switch
                {
                    // โหมดแสดงของรางวัลที่พร้อมแลกและมีสต็อก
                    "available" => filteredRewards.Where(r => r.IsAvailable == true && r.StockQuantity > 0),
                    // โหมดแสดงของรางวัลที่ปิดการแลก
                    "unavailable" => filteredRewards.Where(r => r.IsAvailable != true),
                    // โหมดแสดงสินค้าที่สินค้าหมดสต็อก
                    "outofstock" => filteredRewards.Where(r => r.StockQuantity <= 0),
                    // กรณีโหมดอื่นให้แสดงรายการทั้งหมดตามปกติ
                    _ => filteredRewards
                };
            }

            // คำนวณการแบ่งหน้าสำหรับรายการของแลกแต้ม
            var rewardList = filteredRewards.ToList();
            // ดึงจำนวนรายการของรางวัลทั้งหมดที่ผ่านการกรอง
            totalCount = rewardList.Count;
            // คำนวณจำนวนหน้าทั้งหมดจากจำนวนรายการและขนาดหน้า
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            // จำกัดหมายเลขหน้าให้อยู่ในช่วงที่ถูกต้องเสมอ
            currentPage = Math.Clamp(page, 1, totalPages);
            // ตัดแบ่งรายการของรางวัลตามหน้าปัจจุบันและขนาดหน้า
            pagedRewards = rewardList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
        }

        // สร้างชุดข้อมูลชื่อของรางวัลที่มีอยู่ในระบบเพื่อใช้แสดงป้ายกำกับควบคู่บนรายการสินค้าขาย
        var linkedTitles = new HashSet<string>(allRewards.Select(r => r.Title), StringComparer.OrdinalIgnoreCase);

        // ประกอบข้อมูลทั้งหมดลงใน OwnerCatalogViewModel เพื่อส่งไปยังหน้าแสดงผล
        var vm = new OwnerCatalogViewModel
        {
            // กำหนดแท็บที่กำลังเปิดใช้งานอยู่ (สินค้าขาย หรือ ของแลกแต้ม)
            ActiveTab = activeTab,
            // กำหนดคำค้นหาปัจจุบัน
            SearchQuery = searchQuery,
            // กำหนดตัวกรองหมวดหมู่สินค้า
            CategoryFilter = categoryFilter ?? "all",
            // กำหนดตัวกรองรูปแบบการขาย (ทั้งหมด / เปิดขาย / ซ่อนขาย)
            SaleModeFilter = saleModeFilter ?? "all",
            // รายการสินค้าในหน้านี้ที่ผ่านการแบ่งหน้า
            Products = pagedProducts,
            // รายการของรางวัลในหน้านี้ที่ผ่านการแบ่งหน้า
            Rewards = pagedRewards,
            // รายชื่อหมวดหมู่สินค้าทั้งหมดสำหรับกล่องตัวเลือก
            AllCategories = categories,
            // รายการไฟล์รูปภาพสินค้าที่พร้อมให้เลือกในระบบ
            AvailableImages = GetAvailableProductImages(),
            // รายการชื่อของรางวัลที่มีอยู่เพื่อตรวจสอบความสัมพันธ์
            LinkedRewardTitles = linkedTitles,
            // จำนวนสินค้าทั้งหมดในระบบ
            TotalProducts = allProducts.Count,
            // จำนวนสินค้าที่เปิดวางขายหน้าร้านและมีสต็อกพร้อมขาย
            ActiveSaleProducts = allProducts.Count(p => p.IsActive == true && !p.IsPreorder && p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0),
            // จำนวนสินค้าหมดสต็อกหรือปิดวางขาย
            PreorderProducts = allProducts.Count(p => p.IsActive == true && (p.IsPreorder || p.Inventory == null || (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) <= 0)),
            // จำนวนของรางวัลทั้งหมดในระบบ
            TotalRewards = allRewards.Count,
            // จำนวนของรางวัลที่เปิดให้แลกแต้ม
            ActiveRewards = allRewards.Count(r => r.IsAvailable == true),
            // จำนวนรายการทั้งหมดตามเงื่อนไขตัวกรอง
            TotalCount = totalCount,
            // หมายเลขหน้าปัจจุบัน
            CurrentPage = currentPage,
            // จำนวนรายการต่อหน้า
            PageSize = pageSize
        };

        // ส่ง ViewModel ไปแสดงผลที่หน้าจัดการสินค้าและของรางวัลของเจ้าของร้าน
        return View(vm);
    }

    // CREATE: เพิ่มสินค้าใหม่เข้าสู่ตาราง Products และ Inventory (และสร้างเป็นของแลกแต้มได้ทันทีหากเลือก)
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันบันทึกข้อมูลสินค้าใหม่เข้าสู่ระบบ
    public IActionResult CreateProduct(
        // พารามิเตอร์รหัสสินค้า SKU
        string sku,
        // พารามิเตอร์รหัสบาร์โค้ดสินค้า
        string? barcode,
        // พารามิเตอร์ชื่อสินค้า
        string name,
        // พารามิเตอร์รหัสหมวดหมู่สินค้า
        int categoryId,
        // พารามิเตอร์ชื่อซีรีส์สินค้า
        string? seriesName,
        // พารามิเตอร์ชื่อแบรนด์หรือผู้ผลิต
        string? brandName,
        // พารามิเตอร์ขนาดสเกลโมเดล
        string? scale,
        // พารามิเตอร์ความสูงของโมเดลเป็นเซนติเมตร
        decimal? heightCm,
        // พารามิเตอร์วัสดุที่ใช้ผลิตโมเดล
        string? material,
        // พารามิเตอร์ราคาขายปลีกหน้าร้าน
        decimal retailPrice,
        // พารามิเตอร์ราคาสำหรับสั่งจองล่วงหน้า
        decimal? preorderPrice,
        // พารามิเตอร์วันที่คาดว่าสินค้าจะมาถึง
        string? expectedArrival,
        // พารามิเตอร์สถานะเปิดให้สั่งจองล่วงหน้า
        bool isPreorder,
        // พารามิเตอร์สถานะการเปิดวางขายสินค้า
        bool isActive,
        // พารามิเตอร์จำนวนสต็อกสินค้าเริ่มต้น
        int initialStock,
        // พารามิเตอร์ระดับสต็อกปลอดภัยขั้นต่ำ
        int safetyStockLevel,
        // พารามิเตอร์ตำแหน่งจัดเก็บสินค้าในคลัง
        string? storageLocation,
        // พารามิเตอร์ลิงก์รูปภาพสินค้า
        string? imageUrl,
        // พารามิเตอร์ไฟล์รูปภาพหลักที่อัปโหลด
        IFormFile? imageFile,
        // พารามิเตอร์ไฟล์รูปภาพขนาดย่อที่อัปโหลด
        IFormFile? thumbFile,
        // พารามิเตอร์ตัวเลือกกำหนดเป็นของรางวัลแลกแต้มควบคู่
        bool alsoAsReward,
        // พารามิเตอร์แต้มสะสมที่ต้องใช้แลกของรางวัล
        int? rewardPointsRequired,
        // พารามิเตอร์จำนวนสต็อกของรางวัลที่เปิดให้แลก
        int? rewardStockQuantity)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความครบถ้วนของรหัสสินค้าและชื่อสินค้า
        string cleanSku = (sku ?? "").Trim().ToUpperInvariant();
        // ตัดช่องว่างส่วนเกินออกจากชื่อสินค้า
        string cleanName = (name ?? "").Trim();
        // ตรวจสอบว่ามีการกรอกรหัสสินค้าและชื่อสินค้าหรือไม่
        if (string.IsNullOrWhiteSpace(cleanSku) || string.IsNullOrWhiteSpace(cleanName))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = "กรุณากรอกรหัสสินค้า (SKU) และชื่อสินค้าให้ครบถ้วน";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบว่ารหัส SKU ซ้ำกับสินค้าที่มีอยู่เดิมในระบบหรือไม่
        if (_db.Products.Any(p => p.Sku.ToLower() == cleanSku.ToLower()))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = $"รหัสสินค้า {cleanSku} มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // บันทึกรูปภาพต้นฉบับและภาพการ์ด 1:1 (ถ้ามี)
        string finalImageUrl = SaveUploadedMediaImage(imageFile, imageUrl, thumbFile);

        // สร้างอ็อบเจกต์สินค้าใหม่สำหรับบันทึกลงตาราง Products
        var product = new Product
        {
            // กำหนดรหัส SKU สินค้า
            Sku = cleanSku,
            // กำหนดรหัสบาร์โค้ด
            Barcode = string.IsNullOrWhiteSpace(barcode) ? cleanSku : barcode.Trim(),
            // กำหนดชื่อสินค้าฟิกเกอร์
            Name = cleanName,
            // กำหนดรหัสหมวดหมู่สินค้า
            CategoryId = categoryId > 0 ? categoryId : (_db.Categories.Select(c => c.CategoryId).FirstOrDefault()),
            // กำหนดชื่อซีรีส์หรืออนิเมะ
            SeriesName = string.IsNullOrWhiteSpace(seriesName) ? "ทั่วไป" : seriesName.Trim(),
            // กำหนดแบรนด์ผู้ผลิต
            BrandName = string.IsNullOrWhiteSpace(brandName) ? "FriendHouse Select" : brandName.Trim(),
            // กำหนดอัตราส่วนสเกล
            Scale = string.IsNullOrWhiteSpace(scale) ? "มาตรฐาน" : scale.Trim(),
            // กำหนดความสูงในหน่วยเซนติเมตร
            HeightCm = heightCm,
            // กำหนดชนิดวัสดุที่ใช้ผลิต
            Material = string.IsNullOrWhiteSpace(material) ? "PVC / ABS" : material.Trim(),
            // กำหนดราคาขายปลีก
            RetailPrice = Math.Max(0, retailPrice),
            // ราคาพรีออเดอร์
            PreorderPrice = null,
            // กำหนดการประมาณการของเข้า
            ExpectedArrival = null,
            // สถานะสินค้าพรีออเดอร์
            IsPreorder = false,
            // สถานะเปิดใช้งานการขาย
            IsActive = isActive,
            // ที่อยู่พาธไฟล์รูปภาพ
            ImageUrl = finalImageUrl,
            // สถานะเป็นสินค้าเด่นบนแบนเนอร์
            IsFeaturedHero = false,
            // บันทึกวันเวลาที่สร้างรายการ
            CreatedAt = DateTime.Now
        };

        // เพิ่มสินค้าลงบริบทฐานข้อมูลและบันทึกเพื่อให้ได้รหัส ProductId จริง
        _db.Products.Add(product);
        // บันทึกการเปลี่ยนแปลงทั้งหมดลงในฐานข้อมูล
        _db.SaveChanges();

        // สร้างข้อมูลสต็อกสินค้าในตาราง Inventories เชื่อมโยงกับสินค้าใหม่
        var inventory = new Inventory
        {
            // ผูกรหัสสินค้าที่เพิ่งสร้าง
            ProductId = product.ProductId,
            // จำนวนสต็อกเริ่มต้นในคลัง
            QuantityOnHand = Math.Max(0, initialStock),
            // ยอดที่ถูกจองแล้วเริ่มต้นเป็น 0
            ReservedQuantity = 0,
            // ระดับสต็อกปลอดภัย
            SafetyStockLevel = Math.Max(1, safetyStockLevel),
            // ตำแหน่งจัดเก็บสินค้าในร้าน
            StorageLocation = string.IsNullOrWhiteSpace(storageLocation) ? "โซน A-01" : storageLocation.Trim(),
            // วันเวลาที่เติมสต็อกล่าสุด
            LastRestockedAt = initialStock > 0 ? DateTime.Now : null,
            // วันเวลาที่อัปเดตข้อมูลล่าสุด
            UpdatedAt = DateTime.Now
        };
        // เพิ่มข้อมูลสต็อกสินค้าคงคลังใหม่ลงในฐานข้อมูล
        _db.Inventories.Add(inventory);

        // หากเจ้าของร้านเลือกเปิดให้สินค้านี้เป็นของแลกแต้มด้วย ให้เพิ่มเข้าตาราง Rewards อัตโนมัติ
        if (alsoAsReward)
        {
            // ดึงชื่อหมวดหมู่สินค้าสำหรับกำหนดแท็กของรางวัล
            var catName = _db.Categories.FirstOrDefault(c => c.CategoryId == product.CategoryId)?.CategoryName ?? "ของสะสม";
            // กำหนดรหัสของรางวัลตามรหัสสินค้า
            var rewardCode = "RWD-" + cleanSku;
            // ตรวจสอบว่ายังไม่มีของรางวัลรหัสนี้ในระบบ
            if (!_db.Rewards.Any(r => r.RewardCode == rewardCode))
            {
                // เพิ่มข้อมูลของรางวัลแลกแต้มใหม่ลงในฐานข้อมูล
                _db.Rewards.Add(new Reward
                {
                    // กำหนดรหัสของรางวัล
                    RewardCode = rewardCode,
                    // กำหนดชื่อของรางวัล
                    Title = product.Name,
                    // กำหนดแบรนด์ผู้ผลิต
                    Brand = product.BrandName,
                    // กำหนดแท็กหมวดหมู่ของรางวัล
                    CategoryTag = catName,
                    // จำนวนแต้มที่ต้องใช้ในการแลก
                    PointsRequired = Math.Max(100, rewardPointsRequired ?? 1500),
                    // จำนวนสต็อกสำหรับแลกแต้ม
                    StockQuantity = Math.Max(1, rewardStockQuantity ?? 5),
                    // รูปภาพของรางวัล
                    ImageUrl = finalImageUrl,
                    // สถานะเปิดให้แลก
                    IsAvailable = true,
                    // บันทึกวันเวลาที่สร้าง
                    CreatedAt = DateTime.Now
                });
            }
        }

        // บันทึกประวัติการเพิ่มสินค้าลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // รหัสบันทึกกิจกรรม
            LogCode = GenerateLogCode("PRD-ADD"),
            // ระดับความสำคัญของบันทึก
            LogLevel = "INFO",
            // โมดูลที่เกี่ยวข้อง
            Module = "OwnerCatalog",
            // ข้อความรายละเอียดกิจกรรม
            Message = $"เจ้าของร้านเพิ่มสินค้าใหม่ \"{product.Name}\" (SKU: {product.Sku}) ราคา ฿{product.RetailPrice:N0} จำนวนเริ่มต้น {inventory.QuantityOnHand} ชิ้น",
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่เกิดกิจกรรม
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงในฐานข้อมูล
        _db.SaveChanges();

        // บันทึกข้อความแจ้งเตือนสำเร็จลงใน TempData
        TempData["SuccessMessage"] = $"เพิ่มสินค้าใหม่ \"{product.Name}\" ({product.Sku}) ลงฐานข้อมูลเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // UPDATE: แก้ไขข้อมูลสินค้า รูปภาพ ราคา สต็อก และสถานะการขายในฐานข้อมูลจริง
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันบันทึกการแก้ไขข้อมูลสินค้าเดิมในระบบ
    public IActionResult EditProduct(
        // พารามิเตอร์รหัสสินค้าที่ต้องการดำเนินการ
        int productId,
        // พารามิเตอร์รหัสสินค้า SKU
        string sku,
        // พารามิเตอร์รหัสบาร์โค้ดสินค้า
        string? barcode,
        // พารามิเตอร์ชื่อสินค้า
        string name,
        // พารามิเตอร์รหัสหมวดหมู่สินค้า
        int categoryId,
        // พารามิเตอร์ชื่อซีรีส์สินค้า
        string? seriesName,
        // พารามิเตอร์ชื่อแบรนด์หรือผู้ผลิต
        string? brandName,
        // พารามิเตอร์ขนาดสเกลโมเดล
        string? scale,
        // พารามิเตอร์ความสูงของโมเดลเป็นเซนติเมตร
        decimal? heightCm,
        // พารามิเตอร์วัสดุที่ใช้ผลิตโมเดล
        string? material,
        // พารามิเตอร์ราคาขายปลีกหน้าร้าน
        decimal retailPrice,
        // พารามิเตอร์ราคาสำหรับสั่งจองล่วงหน้า
        decimal? preorderPrice,
        // พารามิเตอร์วันที่คาดว่าสินค้าจะมาถึง
        string? expectedArrival,
        // พารามิเตอร์สถานะเปิดให้สั่งจองล่วงหน้า
        bool isPreorder,
        // พารามิเตอร์สถานะการเปิดวางขายสินค้า
        bool isActive,
        // พารามิเตอร์จำนวนสินค้าคงเหลือในคลังจริง
        int quantityOnHand,
        // พารามิเตอร์ระดับสต็อกปลอดภัยขั้นต่ำ
        int safetyStockLevel,
        // พารามิเตอร์ตำแหน่งจัดเก็บสินค้าในคลัง
        string? storageLocation,
        // พารามิเตอร์ลิงก์รูปภาพสินค้า
        string? imageUrl,
        // พารามิเตอร์ไฟล์รูปภาพหลักที่อัปโหลด
        IFormFile? imageFile,
        // พารามิเตอร์ไฟล์รูปภาพขนาดย่อที่อัปโหลด
        IFormFile? thumbFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลสินค้าพร้อมข้อมูลสต็อกสินค้าในฐานข้อมูล
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId);
        // ตรวจสอบว่าพบข้อมูลสินค้าในฐานข้อมูลหรือไม่
        if (product == null)
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสินค้าที่ต้องการแก้ไขในฐานข้อมูล";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบความซ้ำซ้อนของรหัส SKU กรณีมีการแก้ไขรหัสใหม่
        string cleanSku = (sku ?? product.Sku).Trim().ToUpperInvariant();
        // ตรวจสอบว่ารหัสสินค้าใหม่ไม่ซ้ำกับสินค้าอื่นในระบบ
        if (!string.Equals(cleanSku, product.Sku, StringComparison.OrdinalIgnoreCase) &&
            // ตรวจสอบและประมวลผลคำสั่ง
            _db.Products.Any(p => p.ProductId != productId && p.Sku.ToLower() == cleanSku.ToLower()))
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาดลงใน TempData
            TempData["ErrorMessage"] = $"ไม่สามารถเปลี่ยนเป็นรหัส {cleanSku} ได้ เนื่องจากมีสินค้าอื่นใช้รหัสนี้อยู่แล้ว";
            // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // อัปเดตรายละเอียดสินค้าทั้งหมดลงในอ็อบเจกต์ Product
        product.Sku = cleanSku;
        // อัปเดตรหัสบาร์โค้ดของสินค้า
        product.Barcode = string.IsNullOrWhiteSpace(barcode) ? cleanSku : barcode.Trim();
        // อัปเดตชื่อสินค้า
        product.Name = string.IsNullOrWhiteSpace(name) ? product.Name : name.Trim();
        // ตรวจสอบและอัปเดตหมวดหมู่สินค้า
        if (categoryId > 0) product.CategoryId = categoryId;
        // อัปเดตชื่อซีรีส์ของสินค้า
        product.SeriesName = string.IsNullOrWhiteSpace(seriesName) ? null : seriesName.Trim();
        // อัปเดตชื่อแบรนด์ของสินค้า
        product.BrandName = string.IsNullOrWhiteSpace(brandName) ? null : brandName.Trim();
        // อัปเดตขนาดสเกลของสินค้า
        product.Scale = string.IsNullOrWhiteSpace(scale) ? null : scale.Trim();
        // อัปเดตความสูงของสินค้า
        product.HeightCm = heightCm;
        // อัปเดตวัสดุของสินค้า
        product.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();
        // อัปเดตราคาขายปลีกหน้าร้าน
        product.RetailPrice = Math.Max(0, retailPrice);
        // ล้างค่าราคาพรีออเดอร์เมื่อเป็นสินค้าปกติ
        product.PreorderPrice = null;
        // ล้างค่าวันที่คาดว่าจะมาถึงเมื่อเป็นสินค้าปกติ
        product.ExpectedArrival = null;
        // กำหนดสถานะสินค้าปกติไม่เป็นพรีออเดอร์
        product.IsPreorder = false;
        // อัปเดตสถานะเปิดวางขายสินค้า
        product.IsActive = isActive;
        // อัปเดตรูปภาพสินค้าที่บันทึก
        product.ImageUrl = SaveUploadedMediaImage(imageFile, string.IsNullOrWhiteSpace(imageUrl) ? product.ImageUrl : imageUrl, thumbFile);

        // อัปเดตข้อมูลคลังสินค้าหรือสร้างใหม่หากยังไม่เคยมีข้อมูลในตาราง Inventories
        if (product.Inventory == null)
        {
            // เพิ่มข้อมูลสต็อกสินค้าคงคลังใหม่ลงในฐานข้อมูล
            _db.Inventories.Add(new Inventory
            {
                // กำหนดรหัสสินค้าที่ผูกกับสต็อก
                ProductId = product.ProductId,
                // กำหนดจำนวนสินค้าคงเหลือในคลัง
                QuantityOnHand = Math.Max(0, quantityOnHand),
                // กำหนดจำนวนสินค้าที่ถูกจอง
                ReservedQuantity = 0,
                // กำหนดระดับสต็อกสินค้าปลอดภัย
                SafetyStockLevel = Math.Max(1, safetyStockLevel),
                // กำหนดตำแหน่งจัดเก็บในคลังสินค้า
                StorageLocation = string.IsNullOrWhiteSpace(storageLocation) ? "โซน A-01" : storageLocation.Trim(),
                // กำหนดวันเวลาที่แก้ไขข้อมูลล่าสุด
                UpdatedAt = DateTime.Now
            });
        }
        // มิฉะนั้น
        else
        {
            // คำนวณ QuantityOnHand ให้จำนวนพร้อมขายหลังหักยอดจอง (ReservedQuantity) ตรงกับตัวเลขที่เจ้าของร้านระบุทุกประการ
            product.Inventory.QuantityOnHand = Math.Max(0, quantityOnHand) + Math.Max(0, product.Inventory.ReservedQuantity);
            // อัปเดตระดับสต็อกปลอดภัยของสินค้า
            product.Inventory.SafetyStockLevel = Math.Max(1, safetyStockLevel);
            // ตรวจสอบว่ามีการระบุตำแหน่งจัดเก็บสินค้าหรือไม่
            if (!string.IsNullOrWhiteSpace(storageLocation))
            {
                // อัปเดตตำแหน่งจัดเก็บสินค้าในคลัง
                product.Inventory.StorageLocation = storageLocation.Trim();
            }
            // อัปเดตวันเวลาแก้ไขข้อมูลสต็อกสินค้า
            product.Inventory.UpdatedAt = DateTime.Now;
        }

        // บันทึกประวัติการแก้ไขสินค้าลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกเหตุการณ์ระบบ
            LogCode = GenerateLogCode("PRD-EDIT"),
            // กำหนดระดับความสำคัญของเหตุการณ์
            LogLevel = "INFO",
            // กำหนดชื่อโมดูลที่เกิดเหตุการณ์
            Module = "OwnerCatalog",
            // กำหนดข้อความอธิบายเหตุการณ์
            Message = $"เจ้าของร้านแก้ไขข้อมูลสินค้า \"{product.Name}\" (SKU: {product.Sku})",
            // กำหนดรหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // กำหนดวันเวลาที่เกิดเหตุการณ์
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงในฐานข้อมูล
        _db.SaveChanges();

        // บันทึกข้อความแจ้งเตือนสำเร็จลงใน TempData
        TempData["SuccessMessage"] = $"บันทึกการแก้ไขสินค้า \"{product.Name}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางไปยังแอ็กชันที่กำหนด
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // UPDATE: สลับสถานะการวางขายหรือพรีออเดอร์ของสินค้าจากหน้าตารางทันที
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันสลับสถานะการวางขายสินค้าหน้าร้าน
    public IActionResult ToggleProductSaleStatus(int productId, string mode, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าที่ต้องการปรับสถานะในฐานข้อมูล
        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId);
        // ตรวจสอบว่าพบข้อมูลสินค้าหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสินค้าที่ต้องการปรับสถานะ";
            // เปลี่ยนเส้นทางกลับไปหน้าแคตตาล็อก
            return RedirectToAction("Catalog", new { tab = "products", page });
        }

        // สลับสถานะเปิดวางขายหน้าร้านหรือซ่อนจากหน้าขายชั่วคราว
        product.IsActive = !(product.IsActive ?? true);
        // กำหนดข้อความแจ้งเตือนการเปลี่ยนสถานะ
        TempData["SuccessMessage"] = $"ปรับสถานะ \"{product.Name}\" เป็น " + (product.IsActive == true ? "เปิดวางขายหน้าร้าน" : "ซ่อนจากหน้าขายชั่วคราว");

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();
        // เปลี่ยนเส้นทางกลับไปหน้ารายการสินค้า
        return RedirectToAction("Catalog", new { tab = "products", page });
    }

    // CREATE / UPDATE: นำสินค้าจากคลังมากำหนดเป็นของแลกแต้ม (เลือกได้ว่าจะซ่อนจากการขายหน้าร้านหรือเปิดควบคู่กัน)
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันแปลงสินค้าเป็นของรางวัลแลกแต้มสะสม
    public IActionResult ConvertProductToReward(int productId, int pointsRequired, int rewardStock, bool hideFromSale)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าต้นทางพร้อมข้อมูลหมวดหมู่
        var product = _db.Products
            // รวมข้อมูลตารางหมวดหมู่สินค้า
            .Include(p => p.Category)
            // ค้นหาสินค้าตามรหัสที่ระบุในฐานข้อมูล
            .FirstOrDefault(p => p.ProductId == productId);

        // ตรวจสอบว่าพบสินค้าหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการกำหนดเป็นของแลกแต้ม";
            // เปลี่ยนเส้นทางกลับไปหน้ารายการสินค้า
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // กำหนดรหัสของรางวัลเป้าหมาย
        string targetRewardCode = "RWD-" + product.Sku;
        // ตรวจสอบว่ามีรายการของรางวัลสำหรับสินค้านี้อยู่แล้วหรือไม่
        var existingReward = _db.Rewards.FirstOrDefault(r => r.RewardCode == targetRewardCode || r.Title == product.Name);
        // กำหนดแต้มขั้นต่ำ 50 แต้ม
        int validPoints = Math.Max(50, pointsRequired);
        // กำหนดจำนวนสิทธิ์ขั้นต่ำ 1 ชิ้น
        int validStock = Math.Max(1, rewardStock);

        // ตรวจสอบว่ามีของรางวัลเดิมอยู่แล้วหรือไม่
        if (existingReward != null)
        {
            // อัปเดตแต้มที่ใช้แลก
            existingReward.PointsRequired = validPoints;
            // อัปเดตจำนวนคงเหลือ
            existingReward.StockQuantity = validStock;
            // อัปเดตรูปภาพ
            existingReward.ImageUrl = product.ImageUrl;
            // อัปเดตแบรนด์
            existingReward.Brand = product.BrandName ?? existingReward.Brand;
            // เปิดให้แลกแต้ม
            existingReward.IsAvailable = true;
        }
        // มิฉะนั้น
        else
        {
            // สร้างรายการของรางวัลใหม่ในตาราง Rewards โดยอ้างอิงข้อมูลจากสินค้า
            _db.Rewards.Add(new Reward
            {
                // กำหนดรหัสของรางวัล
                RewardCode = targetRewardCode,
                // กำหนดชื่อของรางวัล
                Title = product.Name,
                // กำหนดแบรนด์ของรางวัล
                Brand = product.BrandName ?? "FriendHouse",
                // กำหนดหมวดหมู่หรือแท็กของรางวัล
                CategoryTag = product.Category?.CategoryName ?? "ของสะสม",
                // กำหนดแต้มที่ต้องใช้แลกรางวัล
                PointsRequired = validPoints,
                // กำหนดจำนวนสต็อกของรางวัล
                StockQuantity = validStock,
                // กำหนดลิงก์รูปภาพของรางวัล
                ImageUrl = product.ImageUrl,
                // กำหนดสถานะเปิดให้แลกรางวัล
                IsAvailable = true,
                // กำหนดวันเวลาที่สร้างข้อมูล
                CreatedAt = DateTime.Now
            });
        }

        // หากเจ้าของร้านเลือกให้เป็นของไว้แลกแต้มอย่างเดียว ให้ซ่อนสินค้าจากหน้าขายปกติ
        if (hideFromSale)
        {
            // ปิดสถานะการขายหน้าร้าน
            product.IsActive = false;
        }

        // บันทึกประวัติการกำหนดของแลกแต้มลงใน Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกเหตุการณ์ระบบ
            LogCode = GenerateLogCode("RWD-MAP"),
            // กำหนดระดับความสำคัญของเหตุการณ์
            LogLevel = "INFO",
            // กำหนดชื่อโมดูลที่เกิดเหตุการณ์
            Module = "OwnerCatalog",
            // กำหนดข้อความอธิบายเหตุการณ์
            Message = $"กำหนดสินค้า \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม จำนวน {validStock} ชิ้น) " + (hideFromSale ? "และปิดการขายหน้าร้าน" : "และเปิดควบคู่หน้าขาย"),
            // กำหนดรหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // กำหนดวันเวลาที่เกิดเหตุการณ์
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = hideFromSale
            // ตรวจสอบและประมวลผลคำสั่ง
            ? $"ย้าย \"{product.Name}\" ไปเป็นของแลกแต้ม ({validPoints:N0} แต้ม) และปิดการขายหน้าร้านเรียบร้อยแล้ว"
            // ตรวจสอบและประมวลผลคำสั่ง
            : $"เปิดให้ \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม) ควบคู่กับการขายปกติเรียบร้อยแล้ว";

        // เปลี่ยนเส้นทางไปยังแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // DELETE: ลบสินค้าหรือซ่อนสินค้าอย่างปลอดภัยกรณีมีประวัติคำสั่งซื้อหรือใบเสร็จผูกอยู่
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันลบหรือปิดการแสดงผลสินค้าในระบบ
    public IActionResult DeleteProduct(int productId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าพร้อมข้อมูลคลังสินค้า
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId);
        // ตรวจสอบว่าพบสินค้าหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการลบในระบบ";
            // เปลี่ยนเส้นทางกลับไปหน้ารายการสินค้า
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบว่าสินค้านี้มีประวัติในใบพรีออเดอร์ ใบเสร็จรับเงิน หรือตะกร้าสินค้าหรือไม่
        bool hasHistory = _db.Preorderitems.Any(pi => pi.ProductId == productId)
                       // ตรวจสอบว่าสินค้ามีประวัติในใบเสร็จการซื้อขายหรือไม่
                       || _db.Receiptitems.Any(ri => ri.ProductId == productId)
                       // ตรวจสอบว่าสินค้าอยู่ในตะกร้าสินค้าของผู้ใช้หรือไม่
                       || _db.Cartitems.Any(ci => ci.ProductId == productId);

        // ตรวจสอบเงื่อนไขประวัติการซื้อขาย
        if (hasHistory)
        {
            // กรณีมีประวัติอ้างอิง ให้ปรับสถานะเป็นปิดการขายเพื่อรักษาข้อมูลประวัติการขายเดิมไว้
            product.IsActive = false;
            // บันทึกกิจกรรมการซ่อนสินค้าลงในบันทึกระบบ
            _db.Systemlogs.Add(new Systemlog
            {
                // กำหนดรหัสบันทึกเหตุการณ์ระบบ
                LogCode = GenerateLogCode("PRD-HIDE"),
                // กำหนดระดับความสำคัญของเหตุการณ์
                LogLevel = "WARNING",
                // กำหนดชื่อโมดูลที่เกิดเหตุการณ์
                Module = "OwnerCatalog",
                // กำหนดข้อความอธิบายเหตุการณ์
                Message = $"ปิดการวางขายสินค้า \"{product.Name}\" (SKU: {product.Sku}) แทนการลบถาวรเนื่องจากมีประวัติการซื้อขายผูกอยู่",
                // กำหนดรหัสผู้ใช้งานที่ทำรายการ
                UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
                // กำหนดวันเวลาที่เกิดเหตุการณ์
                Timestamp = DateTime.Now
            });
            // บันทึกการเปลี่ยนแปลง
            _db.SaveChanges();
            // กำหนดข้อความแจ้งเตือน
            TempData["SuccessMessage"] = $"ปิดการวางขาย \"{product.Name}\" แล้ว (เก็บประวัติใบสั่งจองและใบเสร็จเดิมไว้ในฐานข้อมูล)";
            // เปลี่ยนเส้นทางกลับไปหน้ารายการสินค้า
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // บันทึกชื่อสินค้าไว้สำหรับข้อความแจ้งเตือน
        string deletedName = product.Name;
        // ลบข้อมูลในตารางสต็อกสินค้าหากมีอยู่
        if (product.Inventory != null)
        {
            // ลบแถวสต็อกสินค้า
            _db.Inventories.Remove(product.Inventory);
        }
        // ลบสินค้าออกจากตาราง Products
        _db.Products.Remove(product);

        // บันทึกกิจกรรมการลบสินค้าลงในบันทึกระบบ
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกเหตุการณ์ระบบ
            LogCode = GenerateLogCode("PRD-DEL"),
            // กำหนดระดับความสำคัญของเหตุการณ์
            LogLevel = "WARNING",
            // กำหนดชื่อโมดูลที่เกิดเหตุการณ์
            Module = "OwnerCatalog",
            // กำหนดข้อความอธิบายเหตุการณ์
            Message = $"เจ้าของร้านลบสินค้า \"{deletedName}\" ออกจากฐานข้อมูลถาวร",
            // กำหนดรหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // กำหนดวันเวลาที่เกิดเหตุการณ์
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();
        // กำหนดข้อความแจ้งเตือนสำเร็จ
        TempData["SuccessMessage"] = $"ลบสินค้า \"{deletedName}\" ออกจากฐานข้อมูลเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายการสินค้า
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // CREATE: เพิ่มรายการของรางวัลแลกแต้มใหม่เข้าสู่ตาราง Rewards โดยตรง
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันบันทึกข้อมูลของรางวัลแลกแต้มรายการใหม่
    public IActionResult CreateReward(
        // พารามิเตอร์รหัสของรางวัล
        string rewardCode,
        // พารามิเตอร์ชื่อของรางวัล
        string title,
        // พารามิเตอร์ชื่อแบรนด์ของรางวัล
        string? brand,
        // พารามิเตอร์แท็กหมวดหมู่ของรางวัล
        string? categoryTag,
        // พารามิเตอร์จำนวนแต้มสะสมที่ใช้แลกรางวัล
        int pointsRequired,
        // พารามิเตอร์จำนวนสต็อกของรางวัล
        int stockQuantity,
        // พารามิเตอร์สถานะเปิดให้แลกรับของรางวัล
        bool isAvailable,
        // พารามิเตอร์ลิงก์รูปภาพสินค้า
        string? imageUrl,
        // พารามิเตอร์ไฟล์รูปภาพหลักที่อัปโหลด
        IFormFile? imageFile,
        // พารามิเตอร์ไฟล์รูปภาพขนาดย่อที่อัปโหลด
        IFormFile? thumbFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตัดช่องว่างและปรับแต่งรหัสของรางวัลเป็นตัวพิมพ์ใหญ่
        string cleanCode = (rewardCode ?? "").Trim().ToUpperInvariant();
        // ตัดช่องว่างชื่อของรางวัล
        string cleanTitle = (title ?? "").Trim();
        // ตรวจสอบความครบถ้วนของข้อมูล
        if (string.IsNullOrWhiteSpace(cleanCode) || string.IsNullOrWhiteSpace(cleanTitle))
        {
            // แจ้งเตือนกรณีข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณาระบุรหัสของรางวัลและชื่อของแลกแต้มให้ครบถ้วน";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // ตรวจสอบว่ารหัสของรางวัลซ้ำหรือไม่
        if (_db.Rewards.Any(r => r.RewardCode.ToLower() == cleanCode.ToLower()))
        {
            // แจ้งเตือนกรณีรหัสซ้ำ
            TempData["ErrorMessage"] = $"รหัสของรางวัล {cleanCode} มีอยู่ในระบบแล้ว";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // สร้างและบันทึกรายการของรางวัลใหม่ลงในฐานข้อมูล
        var reward = new Reward
        {
            // กำหนดรหัสของรางวัล
            RewardCode = cleanCode,
            // กำหนดชื่อของรางวัล
            Title = cleanTitle,
            // กำหนดแบรนด์ของรางวัล
            Brand = string.IsNullOrWhiteSpace(brand) ? "FriendHouse" : brand.Trim(),
            // กำหนดหมวดหมู่หรือแท็กของรางวัล
            CategoryTag = string.IsNullOrWhiteSpace(categoryTag) ? "ของสะสมพิเศษ" : categoryTag.Trim(),
            // กำหนดแต้มที่ต้องใช้แลกรางวัล
            PointsRequired = Math.Max(50, pointsRequired),
            // กำหนดจำนวนสต็อกของรางวัล
            StockQuantity = Math.Max(0, stockQuantity),
            // กำหนดลิงก์รูปภาพของรางวัล
            ImageUrl = SaveUploadedMediaImage(imageFile, imageUrl, thumbFile),
            // กำหนดสถานะเปิดให้แลกรางวัล
            IsAvailable = isAvailable,
            // กำหนดวันเวลาที่สร้างข้อมูล
            CreatedAt = DateTime.Now
        };

        // เพิ่มรายการของรางวัลลงในฐานข้อมูล
        _db.Rewards.Add(reward);
        // บันทึกการเปลี่ยนแปลง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"เพิ่มของแลกแต้ม \"{reward.Title}\" ({reward.PointsRequired:N0} แต้ม) เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // UPDATE: แก้ไขข้อมูลของแลกแต้ม แต้มที่ใช้ และจำนวนสิทธิ์คงเหลือในตาราง Rewards
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันบันทึกการแก้ไขข้อมูลของรางวัลแลกแต้ม
    public IActionResult EditReward(
        // พารามิเตอร์รหัสของรางวัลที่ต้องการดำเนินการ
        int rewardId,
        // พารามิเตอร์รหัสของรางวัล
        string rewardCode,
        // พารามิเตอร์ชื่อของรางวัล
        string title,
        // พารามิเตอร์ชื่อแบรนด์ของรางวัล
        string? brand,
        // พารามิเตอร์แท็กหมวดหมู่ของรางวัล
        string? categoryTag,
        // พารามิเตอร์จำนวนแต้มสะสมที่ใช้แลกรางวัล
        int pointsRequired,
        // พารามิเตอร์จำนวนสต็อกของรางวัล
        int stockQuantity,
        // พารามิเตอร์สถานะเปิดให้แลกรับของรางวัล
        bool isAvailable,
        // พารามิเตอร์ลิงก์รูปภาพสินค้า
        string? imageUrl,
        // พารามิเตอร์ไฟล์รูปภาพหลักที่อัปโหลด
        IFormFile? imageFile,
        // พารามิเตอร์ไฟล์รูปภาพขนาดย่อที่อัปโหลด
        IFormFile? thumbFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาของรางวัลตามรหัสในฐานข้อมูล
        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        // ตรวจสอบว่าพบของรางวัลหรือไม่
        if (reward == null)
        {
            // แจ้งเตือนกรณีไม่พบของรางวัล
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลที่ต้องการแก้ไข";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // กำหนดรหัสของรางวัลใหม่
        string nextCode = string.IsNullOrWhiteSpace(rewardCode) ? reward.RewardCode : rewardCode.Trim().ToUpperInvariant();
        // ตรวจสอบว่ารหัสซ้ำกับรายการอื่นหรือไม่
        if (!string.Equals(nextCode, reward.RewardCode, StringComparison.OrdinalIgnoreCase) &&
            // ตรวจสอบว่ารหัสของรางวัลซ้ำกับรายการอื่นหรือไม่
            _db.Rewards.Any(r => r.RewardId != rewardId && r.RewardCode.ToLower() == nextCode.ToLower()))
        {
            // แจ้งเตือนกรณีรหัสซ้ำ
            TempData["ErrorMessage"] = $"รหัสของรางวัล {nextCode} มีอยู่ในระบบแล้ว";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // อัปเดตรหัสของรางวัล
        reward.RewardCode = nextCode;
        // อัปเดตชื่อของรางวัล
        reward.Title = string.IsNullOrWhiteSpace(title) ? reward.Title : title.Trim();
        // อัปเดตแบรนด์
        reward.Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        // อัปเดตหมวดหมู่
        reward.CategoryTag = string.IsNullOrWhiteSpace(categoryTag) ? reward.CategoryTag : categoryTag.Trim();
        // อัปเดตแต้มที่ใช้แลก
        reward.PointsRequired = Math.Max(50, pointsRequired);
        // อัปเดตจำนวนสต็อกคงเหลือ
        reward.StockQuantity = Math.Max(0, stockQuantity);
        // อัปเดตสถานะการแลกแต้ม
        reward.IsAvailable = isAvailable;
        // อัปเดตรูปภาพ
        reward.ImageUrl = SaveUploadedMediaImage(imageFile, string.IsNullOrWhiteSpace(imageUrl) ? reward.ImageUrl : imageUrl, thumbFile);

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"บันทึกการแก้ไขของแลกแต้ม \"{reward.Title}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // UPDATE: เปิดหรือปิดสถานะการแลกแต้มของรายการรางวัลทันที
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันสลับสถานะเปิดหรือปิดการแลกของรางวัล
    public IActionResult ToggleRewardAvailability(int rewardId, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาของรางวัลตามรหัสในฐานข้อมูล
        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        // ตรวจสอบว่าพบของรางวัลหรือไม่
        if (reward == null)
        {
            // แจ้งเตือนกรณีไม่พบของรางวัล
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลในระบบ";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards", page });
        }

        // สลับสถานะเปิดหรือปิดสิทธิ์แลกแต้ม
        reward.IsAvailable = !(reward.IsAvailable ?? true);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"ปรับสถานะของรางวัล \"{reward.Title}\" เป็น " + (reward.IsAvailable == true ? "เปิดให้ลูกค้าแลกแต้ม" : "ปิดชั่วคราว");
        // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards", page });
    }

    // DELETE: ลบของแลกแต้มหรือปิดสิทธิ์กรณีมีประวัติลูกค้าแลกไปแล้ว
    [HttpPost]
    // ตรวจสอบความถูกต้องของโทเค็นความปลอดภัย CSRF ก่อนประมวลผลคำขอ
    [ValidateAntiForgeryToken]
    // แอ็กชันลบรายการของรางวัลแลกแต้มออกจากระบบ
    public IActionResult DeleteReward(int rewardId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาของรางวัลตามรหัสในฐานข้อมูล
        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        // ตรวจสอบว่าพบของรางวัลหรือไม่
        if (reward == null)
        {
            // แจ้งเตือนกรณีไม่พบของรางวัล
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // ตรวจสอบว่ามีประวัติลูกค้าแลกของรางวัลนี้ไปแล้วหรือไม่
        bool hasRedemptions = _db.Rewardredemptions.Any(rr => rr.RewardId == rewardId);
        // ตรวจสอบผลการค้นหาประวัติ
        if (hasRedemptions)
        {
            // ปิดสถานะการแลกเพื่อรักษาประวัติข้อมูล
            reward.IsAvailable = false;
            // บันทึกการเปลี่ยนแปลง
            _db.SaveChanges();
            // แจ้งเตือนข้อความสำเร็จ
            TempData["SuccessMessage"] = $"ปิดสิทธิ์แลก \"{reward.Title}\" แล้ว (เก็บประวัติการแลกของลูกค้าเดิมไว้ในฐานข้อมูล)";
            // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        // บันทึกชื่อของรางวัลสำหรับข้อความแจ้งเตือน
        string title = reward.Title;
        // ลบรายการของรางวัลออกจากฐานข้อมูล
        _db.Rewards.Remove(reward);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"ลบของแลกแต้ม \"{title}\" ออกจากฐานข้อมูลเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards" });
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
