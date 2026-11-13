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
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    // CREATE: บันทึกโปรโมชั่นใหม่ลงในฐานข้อมูลจริง
    [HttpPost]
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

        // ตรวจสอบว่ามีการส่งข้อมูลโปรโมชั่นใหม่และระบุชื่อโปรโมชั่นเข้ามาหรือไม่
        if (data != null && !string.IsNullOrEmpty(data.Name))
        {
            // กรณีไม่ได้ระบุรหัสคูปอง ให้สร้างรหัสคูปองอัตโนมัติตามวันและเวลาปัจจุบัน
            if (string.IsNullOrEmpty(data.VoucherCode))
            {
                // กำหนดรหัสคูปองขึ้นต้นด้วย FH ตามด้วยวันเวลา
                data.VoucherCode = "FH-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            // กรณีไม่ได้ระบุประเภทโปรโมชั่น ให้กำหนดค่าเริ่มต้นเป็นส่วนลดเปอร์เซ็นต์
            if (string.IsNullOrEmpty(data.PromoType))
            {
                // กำหนดประเภทโปรโมชั่นเริ่มต้น
                data.PromoType = "percent";
            }

            // กรณีไม่ได้ระบุขอบเขตสินค้าที่ร่วมรายการ ให้กำหนดค่าเริ่มต้นเป็นสินค้าทั้งหมด
            if (string.IsNullOrEmpty(data.ApplicableScope))
            {
                // กำหนดขอบเขตสินค้าเริ่มต้น
                data.ApplicableScope = "all";
            }

            // กำหนดจำนวนครั้งที่ถูกใช้งานเริ่มต้นเป็นศูนย์
            data.CurrentUsageCount = 0;
            // กำหนดสถานะเปิดใช้งานโปรโมชั่นเริ่มต้นเป็นเปิดใช้งาน
            data.IsActive = data.IsActive ?? true;

            // เพิ่มข้อมูลโปรโมชั่นใหม่ลงในชุดข้อมูลโปรโมชั่น
            _db.Promotions.Add(data);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
            _db.SaveChanges();

            // กำหนดข้อความแจ้งเตือนผลสำเร็จพร้อมชื่อโปรโมชั่นที่เพิ่ม
            TempData["SuccessMessage"] = "เพิ่มโปรโมชั่นใหม่ \"" + data.Name + "\" เรียบร้อยแล้ว";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายการโปรโมชั่นทั้งหมด
            return RedirectToAction("Promotions");
        }

        TempData["ErrorMessage"] = "กรุณาระบุชื่อโปรโมชั่นให้ครบถ้วน";
        return View(data);
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
            // อัปเดตชื่อโปรโมชั่น
            promo.Name = data.Name;
            // อัปเดตรหัสคูปองโปรโมชั่น
            promo.VoucherCode = data.VoucherCode;
            // อัปเดตประเภทโปรโมชั่น
            promo.PromoType = data.PromoType;
            // อัปเดตมูลค่าส่วนลด
            promo.DiscountValue = data.DiscountValue;
            // อัปเดตยอดซื้อขั้นต่ำ
            promo.MinimumSpend = data.MinimumSpend;
            // อัปเดตขอบเขตสินค้าที่ร่วมรายการ
            promo.ApplicableScope = data.ApplicableScope ?? "all";
            // อัปเดตวันที่เริ่มต้นโปรโมชั่น
            promo.StartDate = data.StartDate;
            // อัปเดตวันที่สิ้นสุดโปรโมชั่น
            promo.EndDate = data.EndDate;
            // อัปเดตจำนวนสิทธิ์การใช้งานสูงสุด
            promo.UsageLimit = data.UsageLimit;
            // อัปเดตสถานะการเปิดหรือปิดใช้งานโปรโมชั่น
            promo.IsActive = data.IsActive;

            // บันทึกการแก้ไขลงฐานข้อมูลจริง
            _db.SaveChanges();

            // กำหนดข้อความแจ้งเตือนผลการแก้ไขสำเร็จผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกการแก้ไขโปรโมชั่น \"{promo.Name}\" เรียบร้อยแล้ว";
            // เปลี่ยนเส้นทางกลับไปยังหน้ารายการโปรโมชั่นทั้งหมด
            return RedirectToAction("Promotions");
        }

        // กรณีไม่พบโปรโมชั่นที่ต้องการแก้ไข ให้แจ้งเตือนข้อผิดพลาด
        TempData["ErrorMessage"] = "ไม่พบข้อมูลโปรโมชั่นที่ต้องการแก้ไข";
        // เปลี่ยนเส้นทางกลับไปยังหน้ารายการโปรโมชั่นทั้งหมด
        return RedirectToAction("Promotions");
    }

    // UPDATE: สลับสถานะเปิดหรือปิดการใช้งานโปรโมชั่นในฐานข้อมูล
    [HttpPost]
    public IActionResult TogglePromotion(int id)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // ส่งผลลัพธ์แจ้งว่าไม่มีสิทธิ์เข้าถึง
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        // ค้นหาโปรโมชั่นตามรหัสที่ระบุจากตารางโปรโมชั่นในฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        // ตรวจสอบว่าพบข้อมูลโปรโมชั่นหรือไม่
        if (promo != null)
        {
            // สลับค่าสถานะการเปิดใช้งานเป็นค่าตรงกันข้าม
            promo.IsActive = !(promo.IsActive ?? false);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
            _db.SaveChanges();
            // ส่งผลลัพธ์สถานะใหม่กลับไปยังหน้าเว็บ
            return Json(new { success = true, isActive = promo.IsActive });
        }

        // กรณีไม่พบโปรโมชั่นในฐานข้อมูล
        return Json(new { success = false, message = "ไม่พบโปรโมชั่น" });
    }

    // DELETE: ปิดการใช้งานโปรโมชั่นเพื่อรักษาประวัติการใช้งานเดิมในฐานข้อมูล
    [HttpPost]
    public IActionResult DeletePromotion(int id)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            // ส่งผลลัพธ์แจ้งว่าไม่มีสิทธิ์เข้าถึง
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        // ค้นหาโปรโมชั่นตามรหัสที่ระบุจากตารางโปรโมชั่นในฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        // ตรวจสอบว่าพบโปรโมชั่นหรือไม่
        if (promo != null)
        {
            // ปรับสถานะเป็นปิดการใช้งานเพื่อรักษาข้อมูลอ้างอิงในระบบ
            promo.IsActive = false;
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
            _db.SaveChanges();
            // ส่งผลลัพธ์แจ้งการลบโปรโมชั่นสำเร็จ
            return Json(new { success = true, message = $"ลบโปรโมชั่น {promo.Name} เรียบร้อยแล้ว" });
        }

        // กรณีไม่พบโปรโมชั่นที่ต้องการลบ
        return Json(new { success = false, message = "ไม่พบโปรโมชั่นที่ต้องการลบ" });
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

    // ฟังก์ชันช่วยอัปโหลดและบันทึกไฟล์รูปภาพสินค้าหรือของรางวัลลงโฟลเดอร์ wwwroot/media/images
    private string SaveUploadedMediaImage(IFormFile? imageFile, string? fallbackUrl)
    {
        // ตรวจสอบว่ามีการแนบไฟล์รูปภาพขนาดมากกว่าศูนย์เข้ามาหรือไม่
        if (imageFile != null && imageFile.Length > 0)
        {
            // อ่านนามสกุลไฟล์เดิมและตรวจสอบความปลอดภัยเบื้องต้น
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
            {
                ext = ".png";
            }

            // สร้างโฟลเดอร์เป้าหมายหากยังไม่มีอยู่จริง
            var imagesDir = Path.Combine(_env.WebRootPath, "media", "images");
            Directory.CreateDirectory(imagesDir);

            // ตั้งชื่อไฟล์ให้สะอาดและไม่ซ้ำซ้อนโดยอิงจากชื่อเดิมหรือเวลาปัจจุบัน
            var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_");
            var safePrefix = string.IsNullOrWhiteSpace(rawName) ? "product" : rawName;
            var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(imagesDir, fileName);

            // คัดลอกข้อมูลไฟล์ลงดิสก์ในโฟลเดอร์สื่อกลางของโปรเจกต์
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            // คืนค่าเส้นทางเว็บสำหรับเก็บลงฐานข้อมูล
            return "/media/images/" + fileName;
        }

        // กรณีไม่ได้อัปโหลดไฟล์ใหม่ ให้ใช้เส้นทางรูปภาพเดิมหรือรูปภาพตัวอย่างมาตรฐาน
        return string.IsNullOrWhiteSpace(fallbackUrl) ? "/media/images/tanjiro_kamado.png" : fallbackUrl.Trim();
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

            // กรองตามรูปแบบการจำหน่าย (เปิดขายปกติ พรีออเดอร์ หรือซ่อนจากหน้าร้าน)
            if (!string.IsNullOrWhiteSpace(saleModeFilter) && saleModeFilter != "all")
            {
                filteredProducts = saleModeFilter switch
                {
                    "sale" => filteredProducts.Where(p => p.IsActive == true && !p.IsPreorder),
                    "preorder" => filteredProducts.Where(p => p.IsActive == true && p.IsPreorder),
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
            ActiveTab = activeTab,
            SearchQuery = searchQuery,
            CategoryFilter = categoryFilter ?? "all",
            SaleModeFilter = saleModeFilter ?? "all",
            Products = pagedProducts,
            Rewards = pagedRewards,
            AllCategories = categories,
            AvailableImages = GetAvailableProductImages(),
            LinkedRewardTitles = linkedTitles,
            TotalProducts = allProducts.Count,
            ActiveSaleProducts = allProducts.Count(p => p.IsActive == true),
            PreorderProducts = allProducts.Count(p => p.IsActive == true && p.IsPreorder),
            TotalRewards = allRewards.Count,
            ActiveRewards = allRewards.Count(r => r.IsAvailable == true),
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };

        // ส่ง ViewModel ไปแสดงผลที่หน้าจัดการสินค้าและของรางวัลของเจ้าของร้าน
        return View(vm);
    }

    // CREATE: เพิ่มสินค้าใหม่เข้าสู่ตาราง Products และ Inventory (และสร้างเป็นของแลกแต้มได้ทันทีหากเลือก)
    [HttpPost]
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

        // บันทึกรูปภาพที่อัปโหลดหรือใช้รูปภาพที่เลือกจากคลังสื่อของร้าน
        string finalImageUrl = SaveUploadedMediaImage(imageFile, imageUrl);

        // สร้างอ็อบเจกต์สินค้าใหม่สำหรับบันทึกลงตาราง Products
        var product = new Product
        {
            Sku = cleanSku,
            Barcode = string.IsNullOrWhiteSpace(barcode) ? cleanSku : barcode.Trim(),
            Name = cleanName,
            CategoryId = categoryId > 0 ? categoryId : (_db.Categories.Select(c => c.CategoryId).FirstOrDefault()),
            SeriesName = string.IsNullOrWhiteSpace(seriesName) ? "ทั่วไป" : seriesName.Trim(),
            BrandName = string.IsNullOrWhiteSpace(brandName) ? "FriendHouse Select" : brandName.Trim(),
            Scale = string.IsNullOrWhiteSpace(scale) ? "มาตรฐาน" : scale.Trim(),
            HeightCm = heightCm,
            Material = string.IsNullOrWhiteSpace(material) ? "PVC / ABS" : material.Trim(),
            RetailPrice = Math.Max(0, retailPrice),
            PreorderPrice = preorderPrice,
            ExpectedArrival = string.IsNullOrWhiteSpace(expectedArrival) ? null : expectedArrival.Trim(),
            IsPreorder = isPreorder,
            IsActive = isActive,
            ImageUrl = finalImageUrl,
            IsFeaturedHero = false,
            CreatedAt = DateTime.Now
        };

        // เพิ่มสินค้าลงบริบทฐานข้อมูลและบันทึกเพื่อให้ได้รหัส ProductId จริง
        _db.Products.Add(product);
        _db.SaveChanges();

        // สร้างข้อมูลสต็อกสินค้าในตาราง Inventories เชื่อมโยงกับสินค้าใหม่
        var inventory = new Inventory
        {
            ProductId = product.ProductId,
            QuantityOnHand = Math.Max(0, initialStock),
            ReservedQuantity = 0,
            SafetyStockLevel = Math.Max(1, safetyStockLevel),
            StorageLocation = string.IsNullOrWhiteSpace(storageLocation) ? "โซน A-01" : storageLocation.Trim(),
            LastRestockedAt = initialStock > 0 ? DateTime.Now : null,
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
                    RewardCode = rewardCode,
                    Title = product.Name,
                    Brand = product.BrandName,
                    CategoryTag = catName,
                    PointsRequired = Math.Max(100, rewardPointsRequired ?? 1500),
                    StockQuantity = Math.Max(1, rewardStockQuantity ?? 5),
                    ImageUrl = finalImageUrl,
                    IsAvailable = true,
                    CreatedAt = DateTime.Now
                });
            }
        }

        // บันทึกประวัติการเพิ่มสินค้าลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "PRD-ADD-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "INFO",
            Module = "OwnerCatalog",
            Message = $"เจ้าของร้านเพิ่มสินค้าใหม่ \"{product.Name}\" (SKU: {product.Sku}) ราคา ฿{product.RetailPrice:N0} จำนวนเริ่มต้น {inventory.QuantityOnHand} ชิ้น",
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"เพิ่มสินค้าใหม่ \"{product.Name}\" ({product.Sku}) ลงฐานข้อมูลเรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // UPDATE: แก้ไขข้อมูลสินค้า รูปภาพ ราคา สต็อก และสถานะการขายในฐานข้อมูลจริง
    [HttpPost]
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
        IFormFile? imageFile)
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
        product.PreorderPrice = preorderPrice;
        product.ExpectedArrival = string.IsNullOrWhiteSpace(expectedArrival) ? null : expectedArrival.Trim();
        product.IsPreorder = isPreorder;
        product.IsActive = isActive;
        product.ImageUrl = SaveUploadedMediaImage(imageFile, string.IsNullOrWhiteSpace(imageUrl) ? product.ImageUrl : imageUrl);

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
            LogCode = "PRD-EDIT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
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
    public IActionResult ToggleProductSaleStatus(int productId, string mode, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าที่ต้องการปรับสถานะ
        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสินค้าที่ต้องการปรับสถานะ";
            return RedirectToAction("Catalog", new { tab = "products", page });
        }

        // สลับสถานะตามโหมดที่เลือก (สลับวางขายหน้าร้าน หรือ สลับโหมดพรีออเดอร์)
        if (string.Equals(mode, "preorder", StringComparison.OrdinalIgnoreCase))
        {
            product.IsPreorder = !product.IsPreorder;
            TempData["SuccessMessage"] = $"ปรับสถานะ \"{product.Name}\" เป็น " + (product.IsPreorder ? "สินค้าพรีออเดอร์" : "สินค้าพร้อมส่งปกติ");
        }
        else
        {
            product.IsActive = !(product.IsActive ?? true);
            TempData["SuccessMessage"] = $"ปรับสถานะ \"{product.Name}\" เป็น " + (product.IsActive == true ? "เปิดวางขายหน้าร้าน" : "ซ่อนจากหน้าขายชั่วคราว");
        }

        _db.SaveChanges();
        return RedirectToAction("Catalog", new { tab = "products", page });
    }

    // CREATE / UPDATE: นำสินค้าจากคลังมากำหนดเป็นของแลกแต้ม (เลือกได้ว่าจะซ่อนจากการขายหน้าร้านหรือเปิดควบคู่กัน)
    [HttpPost]
    public IActionResult ConvertProductToReward(int productId, int pointsRequired, int rewardStock, bool hideFromSale)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าต้นทางพร้อมข้อมูลหมวดหมู่
        var product = _db.Products
            .Include(p => p.Category)
            .FirstOrDefault(p => p.ProductId == productId);

        if (product == null)
        {
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการกำหนดเป็นของแลกแต้ม";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบว่ามีรายการของรางวัลสำหรับสินค้านี้อยู่แล้วหรือไม่เพื่ออัปเดตหรือสร้างใหม่
        string targetRewardCode = "RWD-" + product.Sku;
        var existingReward = _db.Rewards.FirstOrDefault(r => r.RewardCode == targetRewardCode || r.Title == product.Name);
        int validPoints = Math.Max(50, pointsRequired);
        int validStock = Math.Max(1, rewardStock);

        if (existingReward != null)
        {
            // อัปเดตแต้ม จำนวนสิทธิ์ รูปภาพ และเปิดใช้งานของแลกแต้ม
            existingReward.PointsRequired = validPoints;
            existingReward.StockQuantity = validStock;
            existingReward.ImageUrl = product.ImageUrl;
            existingReward.Brand = product.BrandName ?? existingReward.Brand;
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
            product.IsActive = false;
        }

        // บันทึกประวัติการกำหนดของแลกแต้มลงใน Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "RWD-MAP-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "INFO",
            Module = "OwnerCatalog",
            Message = $"กำหนดสินค้า \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม จำนวน {validStock} ชิ้น) " + (hideFromSale ? "และปิดการขายหน้าร้าน" : "และเปิดควบคู่หน้าขาย"),
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        TempData["SuccessMessage"] = hideFromSale
            ? $"ย้าย \"{product.Name}\" ไปเป็นของแลกแต้ม ({validPoints:N0} แต้ม) และปิดการขายหน้าร้านเรียบร้อยแล้ว"
            : $"เปิดให้ \"{product.Name}\" เป็นของแลกแต้ม ({validPoints:N0} แต้ม) ควบคู่กับการขายปกติเรียบร้อยแล้ว";

        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // DELETE: ลบสินค้าหรือซ่อนสินค้าอย่างปลอดภัยกรณีมีประวัติคำสั่งซื้อหรือใบเสร็จผูกอยู่
    [HttpPost]
    public IActionResult DeleteProduct(int productId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาสินค้าพร้อมข้อมูลคลังสินค้า
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการลบในระบบ";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // ตรวจสอบว่าสินค้านี้มีประวัติในใบพรีออเดอร์ ใบเสร็จรับเงิน หรือตะกร้าสินค้าหรือไม่
        bool hasHistory = _db.Preorderitems.Any(pi => pi.ProductId == productId)
                       || _db.Receiptitems.Any(ri => ri.ProductId == productId)
                       || _db.Cartitems.Any(ci => ci.ProductId == productId);

        if (hasHistory)
        {
            // กรณีมีประวัติอ้างอิง ให้ปรับสถานะเป็นปิดการขายเพื่อรักษาข้อมูลประวัติการขายเดิมไว้
            product.IsActive = false;
            _db.Systemlogs.Add(new Systemlog
            {
                LogCode = "PRD-HIDE-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                LogLevel = "WARNING",
                Module = "OwnerCatalog",
                Message = $"ปิดการวางขายสินค้า \"{product.Name}\" (SKU: {product.Sku}) แทนการลบถาวรเนื่องจากมีประวัติการซื้อขายผูกอยู่",
                UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
                Timestamp = DateTime.Now
            });
            _db.SaveChanges();
            TempData["SuccessMessage"] = $"ปิดการวางขาย \"{product.Name}\" แล้ว (เก็บประวัติใบสั่งจองและใบเสร็จเดิมไว้ในฐานข้อมูล)";
            return RedirectToAction("Catalog", new { tab = "products" });
        }

        // กรณีไม่มีประวัติอ้างอิง ให้ลบข้อมูลสต็อกและลบสินค้าออกจากฐานข้อมูลถาวร
        string deletedName = product.Name;
        if (product.Inventory != null)
        {
            _db.Inventories.Remove(product.Inventory);
        }
        _db.Products.Remove(product);

        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "PRD-DEL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "WARNING",
            Module = "OwnerCatalog",
            Message = $"เจ้าของร้านลบสินค้า \"{deletedName}\" ออกจากฐานข้อมูลถาวร",
            UserId = HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();
        TempData["SuccessMessage"] = $"ลบสินค้า \"{deletedName}\" ออกจากฐานข้อมูลเรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "products" });
    }

    // CREATE: เพิ่มรายการของรางวัลแลกแต้มใหม่เข้าสู่ตาราง Rewards โดยตรง
    [HttpPost]
    public IActionResult CreateReward(
        string rewardCode,
        string title,
        string? brand,
        string? categoryTag,
        int pointsRequired,
        int stockQuantity,
        bool isAvailable,
        string? imageUrl,
        IFormFile? imageFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        string cleanCode = (rewardCode ?? "").Trim().ToUpperInvariant();
        string cleanTitle = (title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanCode) || string.IsNullOrWhiteSpace(cleanTitle))
        {
            TempData["ErrorMessage"] = "กรุณาระบุรหัสของรางวัลและชื่อของแลกแต้มให้ครบถ้วน";
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        if (_db.Rewards.Any(r => r.RewardCode.ToLower() == cleanCode.ToLower()))
        {
            TempData["ErrorMessage"] = $"รหัสของรางวัล {cleanCode} มีอยู่ในระบบแล้ว";
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
            ImageUrl = SaveUploadedMediaImage(imageFile, imageUrl),
            IsAvailable = isAvailable,
            CreatedAt = DateTime.Now
        };

        _db.Rewards.Add(reward);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"เพิ่มของแลกแต้ม \"{reward.Title}\" ({reward.PointsRequired:N0} แต้ม) เรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // UPDATE: แก้ไขข้อมูลของแลกแต้ม แต้มที่ใช้ และจำนวนสิทธิ์คงเหลือในตาราง Rewards
    [HttpPost]
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
        IFormFile? imageFile)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        if (reward == null)
        {
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลที่ต้องการแก้ไข";
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        reward.RewardCode = string.IsNullOrWhiteSpace(rewardCode) ? reward.RewardCode : rewardCode.Trim().ToUpperInvariant();
        reward.Title = string.IsNullOrWhiteSpace(title) ? reward.Title : title.Trim();
        reward.Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        reward.CategoryTag = string.IsNullOrWhiteSpace(categoryTag) ? reward.CategoryTag : categoryTag.Trim();
        reward.PointsRequired = Math.Max(50, pointsRequired);
        reward.StockQuantity = Math.Max(0, stockQuantity);
        reward.IsAvailable = isAvailable;
        reward.ImageUrl = SaveUploadedMediaImage(imageFile, string.IsNullOrWhiteSpace(imageUrl) ? reward.ImageUrl : imageUrl);

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"บันทึกการแก้ไขของแลกแต้ม \"{reward.Title}\" เรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }

    // UPDATE: เปิดหรือปิดสถานะการแลกแต้มของรายการรางวัลทันที
    [HttpPost]
    public IActionResult ToggleRewardAvailability(int rewardId, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        if (reward == null)
        {
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลในระบบ";
            return RedirectToAction("Catalog", new { tab = "rewards", page });
        }

        reward.IsAvailable = !(reward.IsAvailable ?? true);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"ปรับสถานะของรางวัล \"{reward.Title}\" เป็น " + (reward.IsAvailable == true ? "เปิดให้ลูกค้าแลกแต้ม" : "ปิดชั่วคราว");
        return RedirectToAction("Catalog", new { tab = "rewards", page });
    }

    // DELETE: ลบของแลกแต้มหรือปิดสิทธิ์กรณีมีประวัติลูกค้าแลกไปแล้ว
    [HttpPost]
    public IActionResult DeleteReward(int rewardId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของเจ้าของร้าน
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
        if (reward == null)
        {
            TempData["ErrorMessage"] = "ไม่พบรายการของรางวัลที่ต้องการลบ";
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        bool hasRedemptions = _db.Rewardredemptions.Any(rr => rr.RewardId == rewardId);
        if (hasRedemptions)
        {
            reward.IsAvailable = false;
            _db.SaveChanges();
            TempData["SuccessMessage"] = $"ปิดสิทธิ์แลก \"{reward.Title}\" แล้ว (เก็บประวัติการแลกของลูกค้าเดิมไว้ในฐานข้อมูล)";
            return RedirectToAction("Catalog", new { tab = "rewards" });
        }

        string title = reward.Title;
        _db.Rewards.Remove(reward);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"ลบของแลกแต้ม \"{title}\" ออกจากฐานข้อมูลเรียบร้อยแล้ว";
        return RedirectToAction("Catalog", new { tab = "rewards" });
    }
}
