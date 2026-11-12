using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Helpers;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับจัดการงานฝ่ายบัญชีและการเงินของร้าน
public class AccountantController : Controller
{
    // ประกาศตัวแปรเก็บบริบทฐานข้อมูลสำหรับเชื่อมต่อตารางในฐานข้อมูล
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับบริบทฐานข้อมูลผ่านระบบฉีดพึ่งพา
    public AccountantController(Csi402dbContext db)
    {
        // กำหนดค่าบริบทฐานข้อมูลให้กับตัวแปรภายในคลาส
        _db = db;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทฝ่ายบัญชี เจ้าของร้าน หรือผู้ดูแลระบบไอที
    private bool IsAuthorizedAccountant()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของผู้ใช้งาน
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสผู้ใช้งานเฉพาะฝ่ายบัญชีจากเซสชัน
        var accountantId = HttpContext.Session.GetInt32("AccountantUserId");
        // หากมีเซสชันเฉพาะของฝ่ายบัญชีให้อนุญาตเข้าใช้งานได้ทันที
        if (accountantId.HasValue) return true;
        // ตรวจสอบว่าบทบาทตรงกับฝ่ายบัญชี เจ้าของร้าน หรือผู้ดูแลระบบไอทีหรือไม่
        return role != null && (role.ToLower() == "accountant" || role.ToLower() == "owner" || role.ToLower() == "itsupport");
    }

    // READ: แสดงหน้าแดชบอร์ดสรุปภาพรวมการเงินของร้าน โดยใช้ตัวเลขจริงจากฐานข้อมูลชุดเดียวกับเจ้าของร้าน
    public IActionResult Index(string? selectedMonth)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างข้อมูลรายงานการเงินจากฐานข้อมูลจริงตามช่วงเดือนที่เลือกให้ตรงกับฝั่งเจ้าของร้าน
        var reportModel = FinancialReportHelper.BuildReport(_db, selectedMonth, 1);

        // ส่งข้อมูล ViewModel ไปแสดงผลที่หน้าภาพรวมการเงินร้าน
        return View(reportModel);
    }

    // READ: แสดงหน้าประวัติใบเสร็จรับเงินและรายการธุรกรรมทั้งหมดพร้อมระบบค้นหาและแบ่งหน้า
    public IActionResult Receipts(string? searchTerm, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างคำสั่งคิวรีดึงข้อมูลใบเสร็จพร้อมรายการสินค้าในใบเสร็จ พนักงานแคชเชียร์ และข้อมูลลูกค้า
        var query = _db.Receipts
            // ดึงข้อมูลรายการสินค้าในใบเสร็จแต่ละใบ
            .Include(r => r.Receiptitems)
            // ดึงข้อมูลพนักงานแคชเชียร์ผู้ออกใบเสร็จ
            .Include(r => r.CashierUser)
            // ดึงข้อมูลบัญชีลูกค้าผู้รับใบเสร็จ
            .Include(r => r.Customer)
            // เตรียมเป็นคิวรีสำหรับกรองเงื่อนไขเพิ่มเติม
            .AsQueryable();

        // กรองรายการใบเสร็จตามคำค้นหา เลขที่ใบเสร็จ รหัสใบจองพรีออเดอร์ หรือเบอร์โทรศัพท์ลูกค้า โดยรองรับการพิมพ์แบบไม่ใส่ขีด
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // ตัดช่องว่างหัวท้ายและแปลงคำค้นหาเป็นตัวพิมพ์เล็ก
            var term = searchTerm.Trim().ToLower();
            // ตัดเครื่องหมายขีด เครื่องหมายชาร์ป และช่องว่างออกเพื่อใช้เปรียบเทียบแบบไม่ใส่ขีด
            var termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองใบเสร็จที่ตรงกับเลขที่ใบเสร็จ รหัสบาร์โค้ดใบจอง หรือเบอร์โทรศัพท์ลูกค้า
            query = query.Where(r =>
                r.ReceiptNo.ToLower().Contains(term) ||
                (!string.IsNullOrEmpty(termNoHyphen) && r.ReceiptNo.Replace("-", "").ToLower().Contains(termNoHyphen)) ||
                (r.BarcodeData != null && (r.BarcodeData.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && r.BarcodeData.Replace("-", "").ToLower().Contains(termNoHyphen)))) ||
                (r.CustomerPhone != null && (r.CustomerPhone.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && r.CustomerPhone.Replace("-", "").Replace(" ", "").ToLower().Contains(termNoHyphen)))));
        }

        // ดึงรายการใบเสร็จทั้งหมดเรียงลำดับจากวันและเวลาที่ออกใบเสร็จล่าสุดไปยังเก่าสุด
        var allReceipts = query.OrderByDescending(r => r.IssuedAt).ToList();
        // กำหนดจำนวนรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)allReceipts.Count / pageSize));
        // ตรวจสอบและกำหนดหมายเลขหน้าปัจจุบันให้อยู่ในช่วงที่ถูกต้อง
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงเฉพาะรายการใบเสร็จของหน้าปัจจุบัน
        var pagedReceipts = allReceipts.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งคำค้นหาและข้อมูลการแบ่งหน้าไปยังหน้าแสดงผล
        ViewBag.SearchTerm = searchTerm;
        ViewBag.TotalReceiptCount = allReceipts.Count;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        // ส่งรายการใบเสร็จของหน้าปัจจุบันไปแสดงผลที่หน้าประวัติใบเสร็จ
        return View(pagedReceipts);
    }

    // READ: แสดงหน้าสมุดบันทึกรายรับ-รายจ่ายประจำร้านพร้อมระบบแบ่งหน้า
    public IActionResult Ledger(int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการบันทึกรายรับ-รายจ่ายทั้งหมดพร้อมข้อมูลผู้บันทึก เรียงจากวันที่ล่าสุด
        var entries = _db.Financialledgers
            // เชื่อมโยงข้อมูลผู้ใช้งานที่เป็นผู้บันทึกรายการ
            .Include(f => f.RecordedByUser)
            // เรียงลำดับตามวันที่ของรายการจากใหม่ไปเก่า
            .OrderByDescending(f => f.EntryDate)
            // แปลงผลลัพธ์เป็นรายการลิสต์
            .ToList();

        // คำนวณผลรวมของรายการที่เป็นรายรับทั้งหมดในสมุดบัญชี
        var totalIncome = entries.Where(e => e.EntryType.ToLower() == "income").Sum(e => e.Amount);
        // คำนวณผลรวมของรายการที่เป็นรายจ่ายทั้งหมดในสมุดบัญชี
        var totalExpense = entries.Where(e => e.EntryType.ToLower() == "expense").Sum(e => e.Amount);

        // กำหนดจำนวนรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // คำนวณจำนวนหน้าทั้งหมดของสมุดบัญชี
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)entries.Count / pageSize));
        // ตรวจสอบและกำหนดหมายเลขหน้าปัจจุบันให้อยู่ในช่วงที่ถูกต้อง
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงเฉพาะรายการบัญชีของหน้าปัจจุบัน
        var pagedEntries = entries.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งยอดรวมรายรับไปยังหน้าแสดงผล
        ViewBag.TotalIncome = totalIncome;
        // ส่งยอดรวมรายจ่ายไปยังหน้าแสดงผล
        ViewBag.TotalExpense = totalExpense;
        // ส่งยอดคงเหลือสุทธิจากการนำรายรับลบรายจ่ายไปยังหน้าแสดงผล
        ViewBag.NetBalance = totalIncome - totalExpense;
        // ส่งข้อมูลการแบ่งหน้าและจำนวนรายการทั้งหมดไปยังหน้าแสดงผล
        ViewBag.TotalEntryCount = entries.Count;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        // ส่งรายการบันทึกบัญชีของหน้าปัจจุบันไปแสดงผลที่หน้าสมุดบันทึกรายรับ-รายจ่าย
        return View(pagedEntries);
    }

    // CREATE: บันทึกรายการรายรับ-รายจ่ายใหม่ลงในตารางสมุดบัญชีรายวัน
    public IActionResult AddLedgerEntry(string? EntryType, string? CategoryName, string? Note, decimal? Amount, DateOnly? EntryDate)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบว่ามีการระบุจำนวนเงินที่มากกว่าศูนย์และเลือกหมวดหมู่เรียบร้อยแล้วหรือไม่
        if (Amount.HasValue && Amount.Value > 0 && !string.IsNullOrEmpty(CategoryName))
        {
            // ดึงรหัสผู้ใช้งานฝ่ายบัญชีจากเซสชันปัจจุบัน
            var userId = HttpContext.Session.GetInt32("AccountantUserId") ?? HttpContext.Session.GetInt32("UserId");
            // กรณีไม่พบรหัสผู้ใช้งานในเซสชัน ให้ดึงผู้ใช้งานคนแรกในระบบเป็นค่าสำรอง
            if (!userId.HasValue)
            {
                // ค้นหาผู้ใช้งานคนแรกจากตารางผู้ใช้งานในฐานข้อมูล
                var defaultUser = _db.Users.FirstOrDefault();
                // กำหนดรหัสผู้ใช้งานเริ่มต้น
                userId = defaultUser != null ? defaultUser.UserId : 1;
            }

            // กำหนดประเภทรายการเป็นตัวพิมพ์เล็ก หากไม่ได้ระบุให้ใช้ค่ารายจ่ายเป็นค่าเริ่มต้น
            var type = string.IsNullOrEmpty(EntryType) ? "expense" : EntryType.ToLower();

            // สร้างอ็อบเจกต์รายการบัญชีใหม่สำหรับบันทึกลงฐานข้อมูล
            var newEntry = new Financialledger
            {
                // กำหนดประเภทรายการว่าเป็นรายรับหรือรายจ่าย
                EntryType = type,
                // กำหนดชื่อหมวดหมู่ของรายการบัญชี
                CategoryName = CategoryName,
                // กำหนดรายละเอียดหมายเหตุเพิ่มเติม
                Description = Note ?? "",
                // กำหนดจำนวนเงินของรายการ
                Amount = Amount.Value,
                // กำหนดวันที่ของรายการ หากไม่ระบุให้ใช้วันที่ปัจจุบัน
                EntryDate = EntryDate ?? DateOnly.FromDateTime(DateTime.Today),
                // กำหนดรหัสผู้ใช้งานที่เป็นผู้บันทึกรายการ
                RecordedByUserId = userId.Value,
                // บันทึกวันและเวลาที่สร้างรายการ
                CreatedAt = DateTime.UtcNow
            };

            // เพิ่มรายการบัญชีใหม่ลงในชุดข้อมูลสมุดบัญชีรายวัน
            _db.Financialledgers.Add(newEntry);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
            _db.SaveChanges();

            // กำหนดข้อความแจ้งเตือนผลสำเร็จพร้อมชื่อหมวดหมู่และจำนวนเงิน
            TempData["SuccessMessage"] = "บันทึกรายการ \"" + CategoryName + "\" จำนวน ฿" + Amount.Value.ToString("N2") + " เรียบร้อยแล้ว";
            // เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
            return RedirectToAction("Ledger");
        }

        // กรณีข้อมูลไม่ครบถ้วน ให้เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
        return RedirectToAction("Ledger");
    }

    // UPDATE: แก้ไขรายการบัญชีรายรับ-รายจ่ายในตารางสมุดบัญชีรายวัน
    [HttpPost]
    public IActionResult EditLedgerEntry(int ledgerId, string? categoryName, string? entryType, decimal? amount, DateOnly? entryDate, string? note)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหารายการบัญชีที่ต้องการแก้ไขจากรหัสรายการในฐานข้อมูล
        var entry = _db.Financialledgers.FirstOrDefault(f => f.EntryId == ledgerId);
        // ตรวจสอบว่าพบรายการบัญชีที่ต้องการแก้ไขหรือไม่
        if (entry == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการที่ต้องการแก้ไข
            TempData["ErrorMessage"] = "ไม่พบรายการบัญชีที่ต้องการแก้ไข";
            // เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
            return RedirectToAction("Ledger");
        }

        // อัปเดตชื่อหมวดหมู่หากมีการส่งค่าใหม่เข้ามา
        if (!string.IsNullOrWhiteSpace(categoryName)) entry.CategoryName = categoryName.Trim();
        // อัปเดตประเภทรายการรายรับหรือรายจ่ายหากมีการส่งค่าใหม่เข้ามา
        if (!string.IsNullOrWhiteSpace(entryType)) entry.EntryType = entryType.ToLower();
        // อัปเดตจำนวนเงินหากมีค่ามากกว่าศูนย์
        if (amount.HasValue && amount.Value > 0) entry.Amount = amount.Value;
        // อัปเดตวันที่ของรายการหากมีการระบุค่าใหม่
        if (entryDate.HasValue) entry.EntryDate = entryDate.Value;
        // อัปเดตหมายเหตุเพิ่มเติมของรายการ
        if (note != null) entry.Description = note.Trim();

        // บันทึกการแก้ไขลงฐานข้อมูลจริง
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลการแก้ไขสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"แก้ไขรายการ \"{entry.CategoryName}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
        return RedirectToAction("Ledger");
    }

    // DELETE: ลบรายการบัญชีออกจากตารางสมุดบัญชีรายวันในฐานข้อมูล
    [HttpPost]
    public IActionResult DeleteLedgerEntry(int ledgerId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหารายการบัญชีที่ต้องการลบจากรหัสรายการในฐานข้อมูล
        var entry = _db.Financialledgers.FirstOrDefault(f => f.EntryId == ledgerId);
        // ตรวจสอบว่าพบรายการบัญชีที่ต้องการลบหรือไม่
        if (entry == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการที่ต้องการลบ
            TempData["ErrorMessage"] = "ไม่พบรายการบัญชีที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
            return RedirectToAction("Ledger");
        }

        // เก็บชื่อหมวดหมู่ไว้สำหรับแสดงในข้อความแจ้งเตือนหลังลบสำเร็จ
        var catName = entry.CategoryName;
        // สั่งลบรายการบัญชีออกจากชุดข้อมูลสมุดบัญชีรายวัน
        _db.Financialledgers.Remove(entry);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลการลบสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"ลบรายการ \"{catName}\" ออกจากสมุดบัญชีเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปยังหน้าสมุดบันทึกรายรับ-รายจ่าย
        return RedirectToAction("Ledger");
    }
}
