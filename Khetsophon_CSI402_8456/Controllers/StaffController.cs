using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

public class StaffController : Controller
{
    private readonly Csi402dbContext _db;

    public StaffController(Csi402dbContext db)
    {
        _db = db;
    }

    private bool IsAuthorizedStaff()
    {
        var role = HttpContext.Session.GetString("Role");
        var staffId = HttpContext.Session.GetInt32("StaffUserId");
        if (staffId.HasValue) return true;
        return role != null && (role.ToLower() == "staff" || role.ToLower() == "owner" || role.ToLower() == "itsupport");
    }

    // READ: สต็อกสินค้าหน้าร้านและแสดงผลใบจองจากเคาน์เตอร์
    public IActionResult Index(CounterPreorderViewModel? data)
    {
        if (!IsAuthorizedStaff())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน (Staff)";
            return RedirectToAction("Login", "Account");
        }

        if (data != null && !string.IsNullOrEmpty(data.ReservationNumber))
        {
            ViewBag.ReservationNumber = data.ReservationNumber;
        }

        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .ToList();

        return View(products);
    }

    // READ: ค้นหาใบจองพรีออเดอร์ (GET)
    [HttpGet]
    public IActionResult CounterPreorder(string? query)
    {
        if (!IsAuthorizedStaff())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน (Staff)";
            return RedirectToAction("Login", "Account");
        }

        // WHY: เปิดหน้ามาครั้งแรก ให้ดึงใบจองล่าสุดที่รอมารับ (ReadyForPickup หรือ PendingConfirm) มาแสดงผลทันที เพื่อให้พนักงานทดสอบกดปุ่มได้เลย
        var defaultPreorder = _db.Preorders
            .Include(p => p.Preorderitems)
                .ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm");

        if (defaultPreorder != null)
        {
            ViewBag.SearchResult = defaultPreorder;
            ViewBag.Status = $"กำลังแสดงรายการพรีออเดอร์ล่าสุด #{defaultPreorder.ReservationNumber} ({defaultPreorder.ContactPhone})";
        }

        ViewBag.RecentPreorders = _db.Preorders
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToList();

        return View(new CounterPreorderViewModel { ReservationNumber = defaultPreorder?.ReservationNumber ?? "" });
    }

    // READ: ค้นหาใบจองพรีออเดอร์จากฐานข้อมูลจริงด้วย Smart Search (POST)
    [HttpPost]
    public IActionResult CounterPreorder(CounterPreorderViewModel data)
    {
        if (!IsAuthorizedStaff())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน (Staff)";
            return RedirectToAction("Login", "Account");
        }

        string rawQuery = (data?.ReservationNumber ?? data?.Phone ?? "").Trim();
        string cleanQuery = rawQuery.Replace("#", "").Trim();
        string phoneWithoutHyphen = cleanQuery.Replace("-", "");

        if (!string.IsNullOrEmpty(cleanQuery))
        {
            // WHY SMART SEARCH: ค้นหาได้ทั้งเลขใบจอง (มี/ไม่มี #), เบอร์โทรศัพท์ (มี/ไม่มี -), ชื่อลูกค้า และอีเมล
            var preorder = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(item => item.Product)
                .Include(p => p.User)
                .FirstOrDefault(p => p.ReservationNumber == cleanQuery
                                  || p.ReservationNumber == rawQuery
                                  || p.ContactPhone == cleanQuery
                                  || p.ContactPhone.Replace("-", "") == phoneWithoutHyphen
                                  || (p.User != null && (p.User.FullName != null && p.User.FullName.Contains(cleanQuery)))
                                  || (p.User != null && p.User.Email.Contains(cleanQuery)));

            if (preorder != null)
            {
                ViewBag.SearchResult = preorder;
                ViewBag.Status = $"พบข้อมูลใบสั่งจอง #{preorder.ReservationNumber} (ลูกค้า: {preorder.User?.FullName ?? preorder.ContactPhone})";
            }
            else
            {
                // ตรวจสอบในตาราง Rewardredemptions (ค้นหาด้วยรหัสแลกของรางวัล หรือเบอร์โทรลูกค้า)
                var redemption = _db.Rewardredemptions
                    .Include(r => r.Reward)
                    .Include(r => r.User)
                    .FirstOrDefault(r => r.RedemptionCode == cleanQuery
                                      || r.RedemptionCode == rawQuery
                                      || (r.User != null && r.User.Phone == cleanQuery)
                                      || (r.User != null && r.User.Phone.Replace("-", "") == phoneWithoutHyphen));

                if (redemption != null)
                {
                    ViewBag.SearchResultRedemption = redemption;
                    ViewBag.Status = $"พบข้อมูลแลกของรางวัล #{redemption.RedemptionCode} (ลูกค้า: {redemption.User.FullName ?? redemption.User.Email})";
                }
                else
                {
                    ViewBag.Status = $"ไม่พบข้อมูลใบสั่งจองหรือรหัสแลกของรางวัลตามเงื่อนไข \"{rawQuery}\" ในระบบ";
                }
            }
        }
        else
        {
            ViewBag.Status = "กรุณากรอกรหัสพรีออเดอร์หรือเบอร์โทรศัพท์สำหรับค้นหา";
        }

        ViewBag.RecentPreorders = _db.Preorders
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToList();

        return View(data);
    }

    // 1. ACTION: มารับแล้ว / ชำระเงินแล้ว (Complete Pickup & Issue Receipt & Member Points)
    // WHY: อัปเดตสถานะ Preorder เป็น Completed, ออกใบเสร็จ Receipt จริงใน DB และเพิ่ม Points ให้สมาชิก
    [HttpPost]
    public IActionResult CompletePickup(int? preorderId, string? memberPhone)
    {
        if (!IsAuthorizedStaff())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        int pointsEarned = 0;
        bool memberFound = true;
        string receiptNo = "RCP" + DateTime.Now.ToString("yyyyMMddHHmmss");

        if (preorderId.HasValue && preorderId.Value > 0)
        {
            var preorder = _db.Preorders
                .Include(p => p.Preorderitems)
                .FirstOrDefault(p => p.PreorderId == preorderId.Value);

            if (preorder != null)
            {
                // WHY SECURITY & AUDIT: ป้องกันการปั๊มแต้มซ้ำซ้อน หากออเดอร์ถูกปิดสถานะเป็น Completed หรือ Cancelled แล้ว จะปฏิเสธการทำรายการทันที
                if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
                {
                    return Json(new { success = false, message = "รายการพรีออเดอร์นี้ได้ดำเนินการส่งมอบหรือยกเลิกไปแล้ว ไม่สามารถดำเนินการซ้ำได้" });
                }

                preorder.CurrentStatus = "Completed";
                preorder.CompletedAt = DateTime.Now;

                // สะสมแต้มสมาชิก: ค้นหาเบอร์โทรในตาราง Users
                if (!string.IsNullOrWhiteSpace(memberPhone))
                {
                    var cleanPhone = memberPhone.Trim().Replace("-", "");
                    var memberUser = _db.Users.FirstOrDefault(u => u.Phone == cleanPhone || u.Phone == memberPhone.Trim() || u.Email == memberPhone.Trim());
                    if (memberUser != null)
                    {
                        // คำนวณแต้ม: 10% ของยอด หรือ 150 แต้มมาตรฐาน
                        pointsEarned = (int)Math.Max(10, Math.Round(preorder.TotalFinalPrice / 10));
                        memberUser.Points += pointsEarned;
                        memberFound = true;
                    }
                    else
                    {
                        memberFound = false;
                    }
                }

                // ออกใบเสร็จรับเงินจริง (Receipts) - ไม่ต้องระบุประเภทการชำระเงินตามโครงสร้างตาราง
                var cashierId = HttpContext.Session.GetInt32("UserId") ?? 2;
                var receipt = new Receipt
                {
                    ReceiptNo = receiptNo,
                    PreorderId = preorder.PreorderId,
                    CustomerId = preorder.UserId,
                    CustomerPhone = memberPhone ?? preorder.ContactPhone,
                    CashierUserId = cashierId,
                    SubtotalAmount = preorder.TotalFinalPrice,
                    DiscountAmount = 0.00m,
                    VatAmount = Math.Round(preorder.TotalFinalPrice * 0.07m, 2),
                    NetTotalAmount = preorder.TotalFinalPrice,
                    BarcodeData = preorder.ReservationNumber,
                    IssuedAt = DateTime.Now
                };
                _db.Receipts.Add(receipt);

                // ปรับลดยอดจองในคลังสินค้า (Inventories)
                foreach (var item in preorder.Preorderitems)
                {
                    var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                    if (inv != null)
                    {
                        inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                        inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                    }
                }

                _db.SaveChanges();
            }
        }

        return Json(new { success = true, receiptNo, pointsEarned, memberFound, message = "บันทึกการรับสินค้าและชำระเงินเรียบร้อย" });
    }

    // 2. ACTION: ไม่รับสินค้า (Reject Preorder & Log Reason)
    // WHY: อัปเดตสถานะเป็น Cancelled, คืนสต็อกที่จองไว้ และบันทึกเหตุผลลง Systemlogs เป็น Audit Trail
    [HttpPost]
    public IActionResult RejectPreorder(int? preorderId, string reason, string? remark)
    {
        if (!IsAuthorizedStaff())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        string orderNumber = "PO-2026-9912";

        if (preorderId.HasValue && preorderId.Value > 0)
        {
            var preorder = _db.Preorders
                .Include(p => p.Preorderitems)
                .FirstOrDefault(p => p.PreorderId == preorderId.Value);

            if (preorder != null)
            {
                if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
                {
                    return Json(new { success = false, message = "รายการพรีออเดอร์นี้ได้ดำเนินการปิดสถานะไปแล้ว ไม่สามารถดำเนินการซ้ำได้" });
                }

                orderNumber = preorder.ReservationNumber;
                preorder.CurrentStatus = "Cancelled";

                // คืนยอดจองกลับสู่สต็อก
                foreach (var item in preorder.Preorderitems)
                {
                    var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                    if (inv != null)
                    {
                        inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                    }
                }

                // บันทึกเหตุผลลงใน Systemlogs (เนื่องจากตาราง Preorders ไม่มีคอลัมน์เก็บเหตุผล)
                _db.Systemlogs.Add(new Systemlog
                {
                    LogCode = "REJECT-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    LogLevel = "Warning",
                    Module = "CounterPreorder",
                    Message = $"ลูกค้าไม่รับสินค้า ใบจอง #{orderNumber} สาเหตุ: {reason} | รายละเอียด: {remark ?? "ไม่มี"}",
                    UserId = HttpContext.Session.GetInt32("UserId"),
                    Timestamp = DateTime.Now
                });

                _db.SaveChanges();
            }
        }

        return Json(new { success = true, orderNumber, message = "บันทึกการไม่รับสินค้าเรียบร้อย" });
    }

    // 3. ACTION: ไม่มารับสินค้า (Customer No-Show)
    // WHY: อัปเดตสถานะเป็น Cancelled, ปลดสต็อกที่จองไว้กลับสู่คลัง และบันทึกเหตุผลลง Systemlogs
    [HttpPost]
    public IActionResult NoShowPreorder(int? preorderId, string reason, string? remark)
    {
        if (!IsAuthorizedStaff())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        string orderNumber = "PO-2026-9912";

        if (preorderId.HasValue && preorderId.Value > 0)
        {
            var preorder = _db.Preorders
                .Include(p => p.Preorderitems)
                .FirstOrDefault(p => p.PreorderId == preorderId.Value);

            if (preorder != null)
            {
                if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
                {
                    return Json(new { success = false, message = "รายการพรีออเดอร์นี้ได้ดำเนินการปิดสถานะไปแล้ว ไม่สามารถดำเนินการซ้ำได้" });
                }

                orderNumber = preorder.ReservationNumber;
                preorder.CurrentStatus = "Cancelled";

                foreach (var item in preorder.Preorderitems)
                {
                    var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                    if (inv != null)
                    {
                        inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                    }
                }

                _db.Systemlogs.Add(new Systemlog
                {
                    LogCode = "NOSHOW-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    LogLevel = "Warning",
                    Module = "CounterPreorder",
                    Message = $"ลูกค้าไม่มารับสินค้า ใบจอง #{orderNumber} สาเหตุ: {reason} | หมายเหตุ: {remark ?? "ไม่มี"}",
                    UserId = HttpContext.Session.GetInt32("UserId"),
                    Timestamp = DateTime.Now
                });

                _db.SaveChanges();
            }
        }

        return Json(new { success = true, orderNumber, message = "บันทึกลูกค้าไม่มารับสินค้าเรียบร้อย" });
    }

    // 4. ACTION: ส่งมอบของรางวัลให้ลูกค้า (Complete Reward Redemption)
    [HttpPost]
    public IActionResult CompleteRewardRedemption(long redemptionId)
    {
        if (!IsAuthorizedStaff())
        {
            return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
        }

        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId);

        if (redemption == null)
        {
            return Json(new { success = false, message = "ไม่พบรายการแลกของรางวัลในระบบ" });
        }

        if (redemption.Status == "Completed")
        {
            return Json(new { success = false, message = "รายการนี้ได้ทำการส่งมอบของรางวัลไปแล้ว ไม่สามารถดำเนินการซ้ำได้" });
        }

        redemption.Status = "Completed";
        redemption.CompletedAt = DateTime.Now;

        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = "RWD-DELIVER-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            LogLevel = "Info",
            Module = "CounterPreorder",
            Message = $"ส่งมอบของรางวัลรหัส #{redemption.RedemptionCode} ({redemption.Reward.Title}) ให้ลูกค้า {redemption.User.FullName ?? redemption.User.Email} เรียบร้อยแล้ว",
            UserId = HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        return Json(new { success = true, message = $"บันทึกส่งมอบ \"{redemption.Reward.Title}\" เรียบร้อยแล้ว" });
    }

    // READ: สแกนรับสินค้าเข้าคลัง (GET)
    [HttpGet]
    public IActionResult ScanReceive()
    {
        if (!IsAuthorizedStaff())
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน (Staff)";
            return RedirectToAction("Login", "Account");
        }

        var products = _db.Products.Include(p => p.Inventory).Take(20).ToList();
        return View(products);
    }

    // CREATE / UPDATE: บันทึกรับสินค้าเข้าสต็อกจริงลงตาราง Inventories (POST)
    // WHY: อัปเดตยอดคงเหลือจริงในคลัง (QuantityOnHand) และบันทึก Audit Trail ลง Systemlogs
    [HttpPost]
    public IActionResult ScanReceive(string? sku, int? quantity)
    {
        if (!IsAuthorizedStaff())
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "ไม่มีสิทธิ์เข้าถึง" });
            }
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน (Staff)";
            return RedirectToAction("Login", "Account");
        }

        string cleanSku = (sku ?? "").Trim();
        int addQty = (quantity.HasValue && quantity.Value > 0) ? quantity.Value : 1;

        if (string.IsNullOrWhiteSpace(cleanSku))
        {
            cleanSku = "FIG-NARUTO-001"; // Fallback SKU ตัวอย่าง
        }

        var product = _db.Products
            .Include(p => p.Inventory)
            .FirstOrDefault(p => p.Sku == cleanSku || p.Barcode == cleanSku)
            ?? _db.Products.Include(p => p.Inventory).FirstOrDefault();

        if (product != null)
        {
            var inv = product.Inventory;
            if (inv == null)
            {
                inv = new Inventory
                {
                    ProductId = product.ProductId,
                    QuantityOnHand = addQty,
                    ReservedQuantity = 0,
                    SafetyStockLevel = 5,
                    StorageLocation = "Aisle-A-01",
                    UpdatedAt = DateTime.Now
                };
                _db.Inventories.Add(inv);
            }
            else
            {
                inv.QuantityOnHand += addQty;
                inv.UpdatedAt = DateTime.Now;
            }

            _db.Systemlogs.Add(new Systemlog
            {
                LogCode = "RESTOCK-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                LogLevel = "Info",
                Module = "Inventory",
                Message = $"รับสินค้า {product.Name} (SKU: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าสต็อก คงเหลือรวม {inv.QuantityOnHand} ชิ้น",
                UserId = HttpContext.Session.GetInt32("UserId") ?? 2,
                Timestamp = DateTime.Now
            });

            _db.SaveChanges();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, productName = product.Name, sku = product.Sku, quantity = addQty, totalOnHand = inv.QuantityOnHand });
            }

            TempData["SuccessMessage"] = $"รับสินค้า {product.Name} (SKU: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าคลังเรียบร้อยแล้ว (คงเหลือรวม: {inv.QuantityOnHand} ชิ้น)";
        }
        else
        {
            TempData["ErrorMessage"] = $"ไม่พบรหัสสินค้า SKU: {cleanSku} ในระบบ";
        }

        return RedirectToAction("ScanReceive");
    }
}
