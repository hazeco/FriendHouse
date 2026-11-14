using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Helpers;
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

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทเจ้าของร้านหรือผู้ดูแลระบบไอที
    private bool IsAuthorizedOwner()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของผู้ใช้งาน
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสผู้ใช้งานเฉพาะเจ้าของร้านจากเซสชัน
        var ownerId = HttpContext.Session.GetInt32("OwnerUserId");
        // หากมีเซสชันเฉพาะของเจ้าของร้านให้อนุญาตเข้าใช้งานได้ทันที
        if (ownerId.HasValue) return true;
        // ตรวจสอบว่าบทบาทตรงกับเจ้าของร้านหรือผู้ดูแลระบบไอทีหรือไม่
        return role != null && (role.ToLower() == "owner" || role.ToLower() == "itsupport");
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
        data.VoucherCode = PromoAndRedemptionHelper.NormalizeCode(data.VoucherCode);

        // ตรวจสอบว่ารหัสคูปองซ้ำกับโปรโมชั่นอื่นในฐานข้อมูลหรือไม่
        bool duplicateCode = _db.Promotions.Any(p =>
            p.PromotionId != (ignorePromotionId ?? 0) &&
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
        data.StartDate = PromoAndRedemptionHelper.NormalizeGregorianDate(data.StartDate);
        // ปรับแต่งวันที่สิ้นสุดตามปฏิทินสากล
        data.EndDate = PromoAndRedemptionHelper.NormalizeGregorianDate(data.EndDate);
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
        var reportModel = FinancialReportHelper.BuildReport(_db, selectedMonth, page);

        // ส่งข้อมูลรายงานไปยังหน้าแสดงผล
        return View(reportModel);
    }

    // READ: ส่งออกรายงานสรุปสต็อกและยอดขายประจำเดือนเป็นไฟล์ข้อความตารางภาษาไทยจากฐานข้อมูลจริง
    public IActionResult ExportMonthlyReport(string? selectedMonth)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลทั้งหมดโดยไม่แบ่งหน้าเพื่อส่งออกครบทุกรายการสินค้า
        var reportModel = FinancialReportHelper.BuildReport(_db, selectedMonth, 1);
        var fullRows = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"รายงานสรุปยอดขายและสต็อกสินค้า - {reportModel.SelectedMonthLabel}");
        sb.AppendLine($"ยอดรายได้รวม,{reportModel.TotalRevenue:F2}");
        sb.AppendLine($"รายได้จากการขายหน้าร้าน,{reportModel.StorefrontRevenue:F2}");
        sb.AppendLine($"รายรับอื่นในสมุดบัญชี,{reportModel.LedgerIncome:F2}");
        sb.AppendLine($"รายจ่ายและต้นทุนที่บันทึก,{reportModel.RecordedExpenses:F2}");
        sb.AppendLine();
        sb.AppendLine("รหัสสินค้า,ชื่อสินค้า,หมวดหมู่,คงเหลือในคลัง,ราคาขายปกติ");
        foreach (var item in fullRows)
        {
            var safeName = (item.Name ?? "").Replace("\"", "\"\"");
            var safeCat = (item.Category?.CategoryName ?? "ทั่วไป").Replace("\"", "\"\"");
            sb.AppendLine($"\"{item.Sku}\",\"{safeName}\",\"{safeCat}\",{item.Inventory?.QuantityOnHand ?? 0},{item.RetailPrice:F2}");
        }

        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var finalBytes = preamble.Concat(bodyBytes).ToArray();
        string fileName = $"FriendHouse_Report_{(string.IsNullOrWhiteSpace(selectedMonth) ? DateTime.Today.ToString("yyyyMM") : selectedMonth)}.csv";

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
            .Where(f => allowedExt.Contains(Path.GetExtension(f)))
            .Select(f => "/media/images/" + Path.GetFileName(f))
            .OrderBy(p => p)
            .ToList();
    }

    // ฟังก์ชันช่วยอัปโหลดและบันทึกไฟล์รูปภาพสินค้าหรือของรางวัล (ต้นฉบับ + ภาพการ์ด 1:1 _card.png)
    private string SaveUploadedMediaImage(IFormFile? imageFile, string? fallbackUrl, IFormFile? thumbFile = null)
    {
        var imagesDir = Path.Combine(_env.WebRootPath, "media", "images");
        Directory.CreateDirectory(imagesDir);
        var targetOriginalUrl = string.IsNullOrWhiteSpace(fallbackUrl)
            ? "/media/images/tanjiro_kamado.png"
            : fallbackUrl.Trim();

        // กรณีมีการอัปโหลดไฟล์ต้นฉบับใหม่ ให้บันทึกทั้งไฟล์ต้นฉบับและไฟล์การ์ด 1:1 คู่กัน
        if (imageFile != null && imageFile.Length > 0)
        {
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
            {
                ext = ".png";
            }

            var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_");
            var safePrefix = string.IsNullOrWhiteSpace(rawName) ? "product" : rawName;
            var baseName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmss}";
            var fileName = $"{baseName}{ext}";
            var fullPath = Path.Combine(imagesDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            // บันทึกไฟล์เวอร์ชันการ์ด 1:1 คู่กันหากผู้ใช้ปรับกรอบภาพจากตัวครอปมาด้วย
            if (thumbFile != null && thumbFile.Length > 0)
            {
                var cardFileName = $"{baseName}_card.png";
                var cardFullPath = Path.Combine(imagesDir, cardFileName);
                using var thumbStream = new FileStream(cardFullPath, FileMode.Create);
                thumbFile.CopyTo(thumbStream);
            }

            return "/media/images/" + fileName;
        }

        // กรณีแก้ไขสินค้าเดิมและปรับกรอบภาพใหม่โดยไม่ได้อัปโหลดไฟล์ต้นฉบับทับ
        if (thumbFile != null && thumbFile.Length > 0 && targetOriginalUrl.StartsWith("/media/images/", StringComparison.OrdinalIgnoreCase))
        {
            var relFileName = Path.GetFileName(targetOriginalUrl.Split('?', '#')[0]);
            var relBase = Path.GetFileNameWithoutExtension(relFileName);
            if (!string.IsNullOrWhiteSpace(relBase))
            {
                if (relBase.EndsWith("_card", StringComparison.OrdinalIgnoreCase))
                {
                    relBase = relBase[..^5];
                }
                var cardFullPath = Path.Combine(imagesDir, $"{relBase}_card.png");
                using var thumbStream = new FileStream(cardFullPath, FileMode.Create);
                thumbFile.CopyTo(thumbStream);
            }
        }

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
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        // ดึงข้อมูลของแลกแต้มทั้งหมดเรียงจากรายการใหม่ไปเก่า
        var allRewards = _db.Rewards
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        // ดึงรายการหมวดหมู่สินค้าทั้งหมดจากฐานข้อมูล
        var categories = _db.Categories.OrderBy(c => c.CategoryName).ToList();

        // เตรียมข้อความค้นหาแบบรองรับการพิมพ์โดยไม่ต้องใส่เครื่องหมายขีดลบ
        string cleanSearch = (searchQuery ?? "").Trim().ToLower();
        string noHyphenSearch = cleanSearch.Replace("-", "").Replace("#", "").Replace(" ", "");

        // กำหนดจำนวนรายการต่อหน้ามาตรฐานเป็น 10 รายการ
        int pageSize = 10;
        int totalCount = 0;
        int currentPage = 1;
        var pagedProducts = new List<Product>();
        var pagedRewards = new List<Reward>();

        // ตรวจสอบว่ากำลังแสดงผลในแท็บสินค้าขายหรือแท็บของแลกแต้ม
        if (activeTab == "products")
        {
            // กรองสินค้าตามคำค้นหา (ชื่อสินค้า ซีรีส์ แบรนด์ รหัส SKU หรือบาร์โค้ด)
            IEnumerable<Product> filteredProducts = allProducts;
            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                filteredProducts = filteredProducts.Where(p =>
                    p.Name.ToLower().Contains(cleanSearch) ||
                    p.Sku.ToLower().Contains(cleanSearch) ||
                    (!string.IsNullOrEmpty(noHyphenSearch) && p.Sku.Replace("-", "").ToLower().Contains(noHyphenSearch)) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(cleanSearch)) ||
                    (p.SeriesName != null && p.SeriesName.ToLower().Contains(cleanSearch)) ||
                    (p.BrandName != null && p.BrandName.ToLower().Contains(cleanSearch)));
            }

            // กรองตามหมวดหมู่ที่เลือก
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all" && int.TryParse(categoryFilter, out int catId))
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == catId);
            }

            // กรองตามสถานะสินค้า (วางขายปกติ สินค้าหมดสต็อก หรือซ่อนจากหน้าร้าน)
            if (!string.IsNullOrWhiteSpace(saleModeFilter) && saleModeFilter != "all")
            {
                filteredProducts = saleModeFilter switch
                {
                    "sale" => filteredProducts.Where(p => p.IsActive == true && !p.IsPreorder && p.Inventory != null && (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) > 0),
                    "outofstock" => filteredProducts.Where(p => p.IsActive == true && (p.IsPreorder || p.Inventory == null || (p.Inventory.QuantityOnHand - p.Inventory.ReservedQuantity) <= 0)),
                    "hidden" => filteredProducts.Where(p => p.IsActive != true),
                    _ => filteredProducts
                };
            }

            // คำนวณการแบ่งหน้าสำหรับรายการสินค้าที่ผ่านการกรองแล้ว
            var productList = filteredProducts.ToList();
            totalCount = productList.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            currentPage = Math.Clamp(page, 1, totalPages);
            pagedProducts = productList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
        }
        else
        {
            // กรองของแลกแต้มตามคำค้นหา (ชื่อของรางวัล รหัส แบรนด์ หรือหมวดหมู่)
            IEnumerable<Reward> filteredRewards = allRewards;
            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                filteredRewards = filteredRewards.Where(r =>
                    r.Title.ToLower().Contains(cleanSearch) ||
                    r.RewardCode.ToLower().Contains(cleanSearch) ||
                    (!string.IsNullOrEmpty(noHyphenSearch) && r.RewardCode.Replace("-", "").ToLower().Contains(noHyphenSearch)) ||
                    (r.Brand != null && r.Brand.ToLower().Contains(cleanSearch)) ||
                    r.CategoryTag.ToLower().Contains(cleanSearch));
            }

            // กรองตามสถานะการเปิดให้แลกแต้ม
            if (!string.IsNullOrWhiteSpace(saleModeFilter) && saleModeFilter != "all")
            {
                filteredRewards = saleModeFilter switch
                {
                    "available" => filteredRewards.Where(r => r.IsAvailable == true && r.StockQuantity > 0),
                    "unavailable" => filteredRewards.Where(r => r.IsAvailable != true),
                    "outofstock" => filteredRewards.Where(r => r.StockQuantity <= 0),
                    _ => filteredRewards
                };
            }

            // คำนวณการแบ่งหน้าสำหรับรายการของแลกแต้ม
            var rewardList = filteredRewards.ToList();
            totalCount = rewardList.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            currentPage = Math.Clamp(page, 1, totalPages);
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
    [ValidateAntiForgeryToken]
    public IActionResult CreateProduct(
        string sku,
        string? barcode,
        string name,
        int categoryId,
        string? seriesName,
        string? brandName,
        string? scale,
        decimal? heightCm,
        string? material,
        decimal retailPrice,
        decimal? preorderPrice,
        string? expectedArrival,
        bool isPreorder,
        bool isActive,
        int initialStock,
        int safetyStockLevel,
        string? storageLocation,
        string? imageUrl,
        IFormFile? imageFile,
        IFormFile? thumbFile,
        bool alsoAsReward,
        int? rewardPointsRequired,
        int? rewardStockQuantity)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความครบถ้วนของรหัสสินค้าและชื่อสินค้า
        string cleanSku = (sku ?? "").Trim().ToUpperInvariant();
        string cleanName = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanSku) || string.IsNullOrWhiteSpace(cleanName))
        {
            TempData["ErrorMessage"] = "กรุณากรอกรหัสสินค้า (SKU) และชื่อสินค้าให้ครบถ้วน";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบว่ารหัส SKU ซ้ำกับสินค้าที่มีอยู่เดิมในระบบหรือไม่
        if (_db.Products.Any(p => p.Sku.ToLower() == cleanSku.ToLower()))
        {
            TempData["ErrorMessage"] = $"รหัสสินค้า {cleanSku} มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น";
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
        _db.Inventories.Add(inventory);

        // หากเจ้าของร้านเลือกเปิดให้สินค้านี้เป็นของแลกแต้มด้วย ให้เพิ่มเข้าตาราง Rewards อัตโนมัติ
        if (alsoAsReward)
        {
            var catName = _db.Categories.FirstOrDefault(c => c.CategoryId == product.CategoryId)?.CategoryName ?? "ของสะสม";
            var rewardCode = "RWD-" + cleanSku;
            if (!_db.Rewards.Any(r => r.RewardCode == rewardCode))
            {
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
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("PRD-ADD"),
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

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"เพิ่มสินค้าใหม่ \"{product.Name}\" ({product.Sku}) ลงฐานข้อมูลเรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // UPDATE: แก้ไขข้อมูลสินค้า รูปภาพ ราคา สต็อก และสถานะการขายในฐานข้อมูลจริง
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditProduct(
        int productId,
        string sku,
        string? barcode,
        string name,
        int categoryId,
        string? seriesName,
        string? brandName,
        string? scale,
        decimal? heightCm,
        string? material,
        decimal retailPrice,
        decimal? preorderPrice,
        string? expectedArrival,
        bool isPreorder,
        bool isActive,
        int quantityOnHand,
        int safetyStockLevel,
        string? storageLocation,
        string? imageUrl,
        IFormFile? imageFile,
        IFormFile? thumbFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลสินค้าพร้อมข้อมูลสต็อกสินค้าในฐานข้อมูล
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสินค้าที่ต้องการแก้ไขในฐานข้อมูล";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบความซ้ำซ้อนของรหัส SKU กรณีมีการแก้ไขรหัสใหม่
        string cleanSku = (sku ?? product.Sku).Trim().ToUpperInvariant();
        if (!string.Equals(cleanSku, product.Sku, StringComparison.OrdinalIgnoreCase) &&
            _db.Products.Any(p => p.ProductId != productId && p.Sku.ToLower() == cleanSku.ToLower()))
        {
            TempData["ErrorMessage"] = $"ไม่สามารถเปลี่ยนเป็นรหัส {cleanSku} ได้ เนื่องจากมีสินค้าอื่นใช้รหัสนี้อยู่แล้ว";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // อัปเดตรายละเอียดสินค้าทั้งหมดลงในอ็อบเจกต์ Product
        product.Sku = cleanSku;
        product.Barcode = string.IsNullOrWhiteSpace(barcode) ? cleanSku : barcode.Trim();
        product.Name = string.IsNullOrWhiteSpace(name) ? product.Name : name.Trim();
        if (categoryId > 0) product.CategoryId = categoryId;
        product.SeriesName = string.IsNullOrWhiteSpace(seriesName) ? null : seriesName.Trim();
        product.BrandName = string.IsNullOrWhiteSpace(brandName) ? null : brandName.Trim();
        product.Scale = string.IsNullOrWhiteSpace(scale) ? null : scale.Trim();
        product.HeightCm = heightCm;
        product.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();
        product.RetailPrice = Math.Max(0, retailPrice);
        product.PreorderPrice = null;
        product.ExpectedArrival = null;
        product.IsPreorder = false;
        product.IsActive = isActive;
        product.ImageUrl = SaveUploadedMediaImage(imageFile, string.IsNullOrWhiteSpace(imageUrl) ? product.ImageUrl : imageUrl, thumbFile);

        // อัปเดตข้อมูลคลังสินค้าหรือสร้างใหม่หากยังไม่เคยมีข้อมูลในตาราง Inventories
        if (product.Inventory == null)
        {
            _db.Inventories.Add(new Inventory
            {
                ProductId = product.ProductId,
                QuantityOnHand = Math.Max(0, quantityOnHand),
                ReservedQuantity = 0,
                SafetyStockLevel = Math.Max(1, safetyStockLevel),
                StorageLocation = string.IsNullOrWhiteSpace(storageLocation) ? "โซน A-01" : storageLocation.Trim(),
                UpdatedAt = DateTime.Now
            });
        }
        else
        {
            product.Inventory.QuantityOnHand = Math.Max(0, quantityOnHand);
            product.Inventory.SafetyStockLevel = Math.Max(1, safetyStockLevel);
            if (!string.IsNullOrWhiteSpace(storageLocation))
            {
                product.Inventory.StorageLocation = storageLocation.Trim();
            }
            product.Inventory.UpdatedAt = DateTime.Now;
        }

        // บันทึกประวัติการแก้ไขสินค้าลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("PRD-EDIT"),
            LogLevel = "INFO",
            Module = "OwnerCatalog",
            Message = $"เจ้าของร้านแก้ไขข้อมูลสินค้า \"{product.Name}\" (SKU: {product.Sku})",
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"บันทึกการแก้ไขสินค้า \"{product.Name}\" เรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // UPDATE: สลับสถานะการวางขายหรือพรีออเดอร์ของสินค้าจากหน้าตารางทันที
    [HttpPost]
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
            .Include(p => p.Category)
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
        else
        {
            // สร้างรายการของรางวัลใหม่ในตาราง Rewards โดยอ้างอิงข้อมูลจากสินค้า
            _db.Rewards.Add(new Reward
            {
                RewardCode = targetRewardCode,
                Title = product.Name,
                Brand = product.BrandName ?? "FriendHouse",
                CategoryTag = product.Category?.CategoryName ?? "ของสะสม",
                PointsRequired = validPoints,
                StockQuantity = validStock,
                ImageUrl = product.ImageUrl,
                IsAvailable = true,
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
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("RWD-MAP"),
            LogLevel = "INFO",
            Module = "OwnerCatalog",
            Message = $"กำหนดสินค้า \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม จำนวน {validStock} ชิ้น) " + (hideFromSale ? "และปิดการขายหน้าร้าน" : "และเปิดควบคู่หน้าขาย"),
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = hideFromSale
            ? $"ย้าย \"{product.Name}\" ไปเป็นของแลกแต้ม ({validPoints:N0} แต้ม) และปิดการขายหน้าร้านเรียบร้อยแล้ว"
            : $"เปิดให้ \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม) ควบคู่กับการขายปกติเรียบร้อยแล้ว";

        // เปลี่ยนเส้นทางไปยังแท็บของรางวัล
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // DELETE: ลบสินค้าหรือซ่อนสินค้าอย่างปลอดภัยกรณีมีประวัติคำสั่งซื้อหรือใบเสร็จผูกอยู่
    [HttpPost]
    [ValidateAntiForgeryToken]
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
                       || _db.Receiptitems.Any(ri => ri.ProductId == productId)
                       || _db.Cartitems.Any(ci => ci.ProductId == productId);

        // ตรวจสอบเงื่อนไขประวัติการซื้อขาย
        if (hasHistory)
        {
            // กรณีมีประวัติอ้างอิง ให้ปรับสถานะเป็นปิดการขายเพื่อรักษาข้อมูลประวัติการขายเดิมไว้
            product.IsActive = false;
            // บันทึกกิจกรรมการซ่อนสินค้าลงในบันทึกระบบ
            _db.Systemlogs.Add(new Systemlog
            {
                LogCode = PromoAndRedemptionHelper.GenerateLogCode("PRD-HIDE"),
                LogLevel = "WARNING",
                Module = "OwnerCatalog",
                Message = $"ปิดการวางขายสินค้า \"{product.Name}\" (SKU: {product.Sku}) แทนการลบถาวรเนื่องจากมีประวัติการซื้อขายผูกอยู่",
                UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
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
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("PRD-DEL"),
            LogLevel = "WARNING",
            Module = "OwnerCatalog",
            Message = $"เจ้าของร้านลบสินค้า \"{deletedName}\" ออกจากฐานข้อมูลถาวร",
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
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
    [ValidateAntiForgeryToken]
    public IActionResult CreateReward(
        string rewardCode,
        string title,
        string? brand,
        string? categoryTag,
        int pointsRequired,
        int stockQuantity,
        bool isAvailable,
        string? imageUrl,
        IFormFile? imageFile,
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
            RewardCode = cleanCode,
            Title = cleanTitle,
            Brand = string.IsNullOrWhiteSpace(brand) ? "FriendHouse" : brand.Trim(),
            CategoryTag = string.IsNullOrWhiteSpace(categoryTag) ? "ของสะสมพิเศษ" : categoryTag.Trim(),
            PointsRequired = Math.Max(50, pointsRequired),
            StockQuantity = Math.Max(0, stockQuantity),
            ImageUrl = SaveUploadedMediaImage(imageFile, imageUrl, thumbFile),
            IsAvailable = isAvailable,
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
    [ValidateAntiForgeryToken]
    public IActionResult EditReward(
        int rewardId,
        string rewardCode,
        string title,
        string? brand,
        string? categoryTag,
        int pointsRequired,
        int stockQuantity,
        bool isAvailable,
        string? imageUrl,
        IFormFile? imageFile,
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
}
