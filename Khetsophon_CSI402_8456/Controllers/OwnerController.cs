using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Controllers;

public class OwnerController : Controller
{
    private readonly Csi402dbContext _db;

    public OwnerController(Csi402dbContext db)
    {
        _db = db;
    }

    private bool IsAuthorizedOwner()
    {
        var role = HttpContext.Session.GetString("Role");
        var ownerId = HttpContext.Session.GetInt32("OwnerUserId");
        if (ownerId.HasValue) return true;
        return role != null && (role.ToLower() == "owner" || role.ToLower() == "itsupport");
    }

    public IActionResult Index()
    {
        return RedirectToAction("Promotions");
    }

    // READ: รายการโปรโมชั่นทั้งหมดจากตาราง Promotions
    public IActionResult Promotions()
    {
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        var promotions = _db.Promotions.ToList();
        return View(promotions);
    }

    // CREATE: หน้าแบบฟอร์มและการบันทึกโปรโมชั่นใหม่
    public IActionResult AddPromotion(Promotion? data)
    {
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        if (data != null && !string.IsNullOrEmpty(data.Name))
        {
            if (string.IsNullOrEmpty(data.VoucherCode))
            {
                data.VoucherCode = "FH-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            if (string.IsNullOrEmpty(data.PromoType))
            {
                data.PromoType = "percent";
            }

            if (string.IsNullOrEmpty(data.ApplicableScope))
            {
                data.ApplicableScope = "all";
            }

            data.CurrentUsageCount = 0;
            data.IsActive = data.IsActive ?? true;

            _db.Promotions.Add(data);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "เพิ่มโปรโมชั่นใหม่ \"" + data.Name + "\" เรียบร้อยแล้ว";
            return RedirectToAction("Promotions");
        }

        return View();
    }

    // READ/UPDATE: หน้าฟอร์มแก้ไขโปรโมชั่น (GET)
    public IActionResult EditPromotion(int? id)
    {
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        Promotion? promo = null;
        if (id.HasValue && id.Value > 0)
        {
            promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id.Value);
        }

        return View(promo);
    }

    // UPDATE: บันทึกข้อมูลการแก้ไขโปรโมชั่นลงฐานข้อมูลจริง (POST)
    [HttpPost]
    public IActionResult EditPromotion(Promotion data)
    {
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == data.PromotionId);
        if (promo != null)
        {
            promo.Name = data.Name;
            promo.VoucherCode = data.VoucherCode;
            promo.PromoType = data.PromoType;
            promo.DiscountValue = data.DiscountValue;
            promo.MinimumSpend = data.MinimumSpend;
            promo.ApplicableScope = data.ApplicableScope ?? "all";
            promo.StartDate = data.StartDate;
            promo.EndDate = data.EndDate;
            promo.UsageLimit = data.UsageLimit;
            promo.IsActive = data.IsActive;

            _db.SaveChanges();
            TempData["SuccessMessage"] = $"บันทึกการแก้ไขโปรโมชั่น \"{promo.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction("Promotions");
        }

        TempData["ErrorMessage"] = "ไม่พบข้อมูลโปรโมชั่นที่ต้องการแก้ไข";
        return RedirectToAction("Promotions");
    }

    // UPDATE: สลับสถานะเปิด/ปิดโปรโมชั่นแบบ AJAX (POST)
    [HttpPost]
    public IActionResult TogglePromotion(int id)
    {
        if (!IsAuthorizedOwner())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        if (promo != null)
        {
            promo.IsActive = !(promo.IsActive ?? false);
            _db.SaveChanges();
            return Json(new { success = true, isActive = promo.IsActive });
        }

        return Json(new { success = false, message = "ไม่พบโปรโมชั่น" });
    }

    // DELETE: ลบโปรโมชั่น (Soft Delete: ปิดการใช้งาน IsActive = false เพื่อรักษาประวัติการใช้งานเดิม)
    [HttpPost]
    public IActionResult DeletePromotion(int id)
    {
        if (!IsAuthorizedOwner())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == id);
        if (promo != null)
        {
            promo.IsActive = false;
            _db.SaveChanges();
            return Json(new { success = true, message = $"ลบโปรโมชั่น {promo.Name} เรียบร้อยแล้ว" });
        }

        return Json(new { success = false, message = "ไม่พบโปรโมชั่นที่ต้องการลบ" });
    }

    // READ: หน้ารายงานสรุปยอดประจำเดือน
    public IActionResult MonthlyReport()
    {
        if (!IsAuthorizedOwner())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีเจ้าของร้าน";
            return RedirectToAction("Login", "Account");
        }

        // ดึงยอดรวมจากใบเสร็จ
        var receipts = _db.Receipts.ToList();
        ViewBag.TotalSales = receipts.Sum(r => r.NetTotalAmount);
        ViewBag.TotalReceiptCount = receipts.Count;

        return View();
    }
}
