using System.Text;
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

    // ฟังก์ชันช่วยดึงรหัสผู้ใช้งานปัจจุบันจากเซสชัน (รองรับฝ่ายบัญชี เจ้าของร้าน และฝ่ายไอที)
    private int? GetCurrentAccountantUserId()
    {
        // ค้นหารหัสผู้ใช้งานจากเซสชันตามลำดับสิทธิ์
        return HttpContext.Session.GetInt32("AccountantUserId")
            ?? HttpContext.Session.GetInt32("OwnerUserId")
            ?? HttpContext.Session.GetInt32("ITSupportUserId")
            ?? HttpContext.Session.GetInt32("UserId");
    }

    // ฟังก์ชันช่วยสร้างคิวรีใบเสร็จพร้อมตัวกรองที่ใช้ร่วมกันระหว่างหน้าแสดงผลและส่งออก CSV
    private IQueryable<Receipt> BuildFilteredReceiptsQuery(string? searchTerm, DateOnly? startDate, DateOnly? endDate, string? promoFilter)
    {
        // เริ่มต้นสร้างคิวรีดึงข้อมูลใบเสร็จพร้อมตารางที่เกี่ยวข้อง
        var query = _db.Receipts
            .Include(r => r.Receiptitems)
            .Include(r => r.CashierUser)
            .Include(r => r.Customer)
            .AsQueryable();

        // ตรวจสอบว่ามีการระบุคำค้นหาใบเสร็จหรือไม่
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // แปลงคำค้นหาเป็นตัวพิมพ์เล็กและตัดช่องว่าง
            var term = searchTerm.Trim().ToLower();
            // ลบเครื่องหมายขีดและช่องว่างเพื่อค้นหาเลขใบเสร็จแบบยืดหยุ่น
            var termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองเงื่อนไขค้นหาตามเลขที่ใบเสร็จ ข้อมูลบาร์โค้ด หรือเบอร์โทรลูกค้า
            query = query.Where(r =>
                r.ReceiptNo.ToLower().Contains(term) ||
                (!string.IsNullOrEmpty(termNoHyphen) && r.ReceiptNo.Replace("-", "").ToLower().Contains(termNoHyphen)) ||
                (r.BarcodeData != null && (r.BarcodeData.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && r.BarcodeData.Replace("-", "").ToLower().Contains(termNoHyphen)))) ||
                (r.CustomerPhone != null && (r.CustomerPhone.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && r.CustomerPhone.Replace("-", "").Replace(" ", "").ToLower().Contains(termNoHyphen)))));
        }

        // ตรวจสอบว่ามีการระบุวันที่เริ่มต้นหรือไม่
        if (startDate.HasValue)
        {
            // แปลงวันที่เริ่มต้นเป็น DateTime ที่เวลา 00:00:00
            var startDt = startDate.Value.ToDateTime(TimeOnly.MinValue);
            // กรองเฉพาะใบเสร็จที่ออกตั้งแต่วันที่เริ่มต้น
            query = query.Where(r => r.IssuedAt >= startDt);
        }

        // ตรวจสอบว่ามีการระบุวันที่สิ้นสุดหรือไม่
        if (endDate.HasValue)
        {
            // คำนวณวันถัดไปสำหรับเงื่อนไขน้อยกว่า (ครอบคลุมทั้งวัน)
            var endExclusiveDt = endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            // กรองเฉพาะใบเสร็จที่ออกก่อนสิ้นสุดวัน
            query = query.Where(r => r.IssuedAt < endExclusiveDt);
        }

        // ตรวจสอบว่ามีการกรองตามเงื่อนไขส่วนลดโปรโมชั่นหรือไม่
        if (!string.IsNullOrWhiteSpace(promoFilter) && !promoFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // กรองเฉพาะใบเสร็จที่มีการใช้ส่วนลด
            if (promoFilter.Equals("discounted", StringComparison.OrdinalIgnoreCase))
            {
                // เงื่อนไขยอดส่วนลดมากกว่าศูนย์
                query = query.Where(r => r.DiscountAmount > 0);
            }
            // กรองเฉพาะใบเสร็จราคาปกติไม่มีส่วนลด
            else if (promoFilter.Equals("regular", StringComparison.OrdinalIgnoreCase))
            {
                // เงื่อนไขยอดส่วนลดเท่ากับหรือน้อยกว่าศูนย์
                query = query.Where(r => r.DiscountAmount <= 0);
            }
        }

        // ส่งคืนผลลัพธ์คำสั่งคิวรี
        return query;
    }

    // READ: แสดงหน้าประวัติใบเสร็จรับเงินและรายการธุรกรรมทั้งหมดพร้อมระบบค้นหา ตัวกรอง และแบ่งหน้า
    public IActionResult Receipts(string? searchTerm, DateOnly? startDate, DateOnly? endDate, string? promoFilter, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการใบเสร็จทั้งหมดตามตัวกรองและเรียงลำดับจากเวลาล่าสุด
        var allReceipts = BuildFilteredReceiptsQuery(searchTerm, startDate, endDate, promoFilter)
            .OrderByDescending(r => r.IssuedAt)
            .ToList();

        // กำหนดขนาดรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)allReceipts.Count / pageSize));
        // ตรวจสอบขอบเขตหน้าปัจจุบัน
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ตัดข้อมูลเฉพาะหน้าปัจจุบัน
        var pagedReceipts = allReceipts.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งค่าตัวแปรค้นหาและข้อมูลการแบ่งหน้าไปยังมุมมองผ่าน ViewBag
        ViewBag.SearchTerm = searchTerm;
        // ส่งวันที่เริ่มต้น
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? "";
        // ส่งวันที่สิ้นสุด
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? "";
        // ส่งตัวกรองโปรโมชั่น
        ViewBag.PromoFilter = promoFilter ?? "all";
        // ส่งจำนวนใบเสร็จทั้งหมด
        ViewBag.TotalReceiptCount = allReceipts.Count;
        // ส่งหน้าปัจจุบัน
        ViewBag.CurrentPage = currentPage;
        // ส่งจำนวนหน้าทั้งหมด
        ViewBag.TotalPages = totalPages;
        // ส่งขนาดรายการต่อหน้า
        ViewBag.PageSize = pageSize;

        // ส่งข้อมูลใบเสร็จไปแสดงผลที่หน้า Receipts
        return View(pagedReceipts);
    }

    // EXPORT: ส่งออกข้อมูลประวัติใบเสร็จจริงจากฐานข้อมูลเป็นไฟล์ CSV (รองรับภาษาไทย UTF-8 BOM)
    [HttpGet]
    public IActionResult ExportReceiptsCSV(string? searchTerm, DateOnly? startDate, DateOnly? endDate, string? promoFilter)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงรายการข้อมูลใบเสร็จตามตัวกรองเดียวกัน
        var rows = BuildFilteredReceiptsQuery(searchTerm, startDate, endDate, promoFilter)
            .OrderByDescending(r => r.IssuedAt)
            .ToList();

        // สร้างตัวสร้างข้อความ StringBuilder สำหรับเขียนข้อมูล CSV
        var sb = new StringBuilder();
        // เพิ่มแถวหัวคอลัมน์ภาษาไทย
        sb.AppendLine("เลขที่ใบเสร็จ,วันเวลาที่ออก,เบอร์โทรลูกค้า,ชื่อลูกค้า,รายการสินค้า,ยอดรวมก่อนลด,ส่วนลด,ภาษีมูลค่าเพิ่ม,ยอดชำระสุทธิ,พนักงานขาย");
        // วนลูปแปลงข้อมูลใบเสร็จแต่ละรายการลงในบรรทัด CSV
        foreach (var r in rows)
        {
            // รวมชื่อรายการสินค้าในใบเสร็จ
            var itemSummary = r.Receiptitems.Any()
                ? string.Join(" | ", r.Receiptitems.Select(i => $"{i.ItemDescription} x{i.Quantity}"))
                : "สินค้าหน้าร้าน x1";
            // กำหนดชื่อลูกค้า
            var customerName = r.Customer != null
                ? (r.Customer.FullName ?? r.Customer.Email)
                : (string.IsNullOrEmpty(r.CustomerPhone) ? "ลูกค้าทั่วไป" : "ลูกค้าสมาชิก");
            // กำหนดชื่อพนักงานที่ออกใบเสร็จ
            var staffName = r.CashierUser != null
                ? (r.CashierUser.FullName ?? r.CashierUser.Email)
                : "พนักงานหน้าร้าน";

            // จัดรูปแบบคอลัมน์ข้อมูลใบเสร็จรับเงินสำหรับไฟล์ CSV (เลขที่ใบเสร็จ, วันเวลา, เบอร์โทร, ลูกค้า, รายการ, ยอดเงิน, พนักงาน)
            sb.AppendLine(string.Join(",",
                // ข้อมูลระบุตัวตนใบเสร็จ: หมายเลขใบเสร็จและวันเวลาออกเอกสาร
                PromoAndRedemptionHelper.EscapeCsvField(r.ReceiptNo),
                PromoAndRedemptionHelper.EscapeCsvField(r.IssuedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                // ข้อมูลลูกค้า: เบอร์โทรศัพท์และชื่อลูกค้า
                PromoAndRedemptionHelper.EscapeCsvField(string.IsNullOrEmpty(r.CustomerPhone) ? "ทั่วไป" : r.CustomerPhone),
                PromoAndRedemptionHelper.EscapeCsvField(customerName),
                // ข้อมูลรายการสินค้าที่ซื้อ
                PromoAndRedemptionHelper.EscapeCsvField(itemSummary),
                // ข้อมูลตัวเลขยอดเงิน: ยอดรวม ส่วนลด ภาษี และยอดสุทธิ
                r.SubtotalAmount.ToString("0.00"),
                r.DiscountAmount.ToString("0.00"),
                r.VatAmount.ToString("0.00"),
                r.NetTotalAmount.ToString("0.00"),
                // ข้อมูลพนักงานแคชเชียร์ผู้ทำรายการ
                PromoAndRedemptionHelper.EscapeCsvField(staffName)));
        }

        // แปลงข้อความเป็นไบต์อาร์เรย์พร้อมแนบ UTF-8 BOM เพื่อให้เปิดอ่านภาษาไทยใน Excel ได้ถูกต้อง
        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        // ส่งคืนไฟล์ CSV สำหรับดาวน์โหลด
        return File(csvBytes, "text/csv; charset=utf-8", $"FriendHouse_Receipts_{DateTime.Now:yyyyMMdd_HHmm}.csv");
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
            .Include(f => f.RecordedByUser)
            .OrderByDescending(f => f.EntryDate)
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
    [HttpPost]
    [ValidateAntiForgeryToken]
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

        // ดึงรหัสผู้ใช้งานปัจจุบันจากเซสชัน
        var userId = GetCurrentAccountantUserId();
        // ตรวจสอบว่าพบข้อมูลผู้ใช้งานหรือไม่
        if (!userId.HasValue)
        {
            // แจ้งเตือนกรณีไม่พบเซสชันผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบข้อมูลผู้ใช้งานในเซสชัน กรุณาเข้าสู่ระบบใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางไปหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความครบถ้วนของข้อมูลหมวดหมู่และจำนวนเงิน
        if (string.IsNullOrWhiteSpace(CategoryName) || !Amount.HasValue || Amount.Value <= 0)
        {
            // แจ้งเตือนข้อผิดพลาดกรณีข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณาระบุหมวดหมู่และจำนวนเงินที่มากกว่า 0 บาท";
            // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
            return RedirectToAction("Ledger");
        }

        // ปรับแต่งและตรวจสอบประเภทรายการรายรับหรือรายจ่าย
        var normalizedType = string.IsNullOrWhiteSpace(EntryType) ? "expense" : EntryType.Trim().ToLowerInvariant();
        // ตรวจสอบว่าประเภทเป็น income หรือ expense เท่านั้น
        if (normalizedType != "income" && normalizedType != "expense")
        {
            // แจ้งเตือนข้อผิดพลาดกรณีประเภทรายการไม่ถูกต้อง
            TempData["ErrorMessage"] = "ประเภทรายการต้องเป็นรายรับ (income) หรือรายจ่าย (expense) เท่านั้น";
            // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
            return RedirectToAction("Ledger");
        }

        // ตัดช่องว่างของชื่อหมวดหมู่
        var cleanCategory = CategoryName.Trim();
        // สร้างออบเจ็กต์รายการบันทึกบัญชีใหม่
        var newEntry = new Financialledger
        {
            EntryType = normalizedType,
            CategoryName = cleanCategory,
            Description = Note?.Trim() ?? "",
            Amount = Amount.Value,
            EntryDate = EntryDate ?? DateOnly.FromDateTime(DateTime.Today),
            RecordedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        // เพิ่มรายการใหม่ลงในฐานข้อมูล
        _db.Financialledgers.Add(newEntry);
        // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนผลการบันทึกสำเร็จ
        TempData["SuccessMessage"] = "บันทึกรายการ \"" + cleanCategory + "\" จำนวน ฿" + Amount.Value.ToString("N2") + " เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
        return RedirectToAction("Ledger");
    }

    // UPDATE: แก้ไขรายการบัญชีรายรับ-รายจ่ายในตารางสมุดบัญชีรายวัน
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditLedgerEntry(int ledgerId, string? categoryName, string? entryType, decimal? amount, DateOnly? entryDate, string? note)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหารายการบัญชีจากฐานข้อมูลด้วยรหัส EntryId
        var entry = _db.Financialledgers.FirstOrDefault(f => f.EntryId == ledgerId);
        // ตรวจสอบว่าพบรายการในฐานข้อมูลหรือไม่
        if (entry == null)
        {
            // แจ้งเตือนกรณีไม่พบรายการที่ต้องการแก้ไข
            TempData["ErrorMessage"] = "ไม่พบรายการบัญชีที่ต้องการแก้ไข";
            // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
            return RedirectToAction("Ledger");
        }

        // ตรวจสอบว่าจำนวนเงินถูกต้องและมากกว่าศูนย์
        if (amount.HasValue && amount.Value <= 0)
        {
            // แจ้งเตือนกรณีจำนวนเงินไม่ถูกต้อง
            TempData["ErrorMessage"] = "จำนวนเงินต้องมากกว่า 0 บาท";
            // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
            return RedirectToAction("Ledger");
        }

        // ตรวจสอบและอัปเดตประเภทรายการ
        if (!string.IsNullOrWhiteSpace(entryType))
        {
            // ปรับแต่งประเภทรายการเป็นตัวพิมพ์เล็ก
            var normalizedType = entryType.Trim().ToLowerInvariant();
            // ตรวจสอบความถูกต้องของประเภทรายการ
            if (normalizedType != "income" && normalizedType != "expense")
            {
                // แจ้งเตือนกรณีประเภทรายการไม่ถูกต้อง
                TempData["ErrorMessage"] = "ประเภทรายการไม่ถูกต้อง";
                // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
                return RedirectToAction("Ledger");
            }
            // อัปเดตประเภทรายการ
            entry.EntryType = normalizedType;
        }

        // อัปเดตชื่อหมวดหมู่รายการ
        if (!string.IsNullOrWhiteSpace(categoryName)) entry.CategoryName = categoryName.Trim();
        // อัปเดตจำนวนเงิน
        if (amount.HasValue && amount.Value > 0) entry.Amount = amount.Value;
        // อัปเดตวันที่ของรายการ
        if (entryDate.HasValue) entry.EntryDate = entryDate.Value;
        // อัปเดตบันทึกเพิ่มเติม
        if (note != null) entry.Description = note.Trim();

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"แก้ไขรายการ \"{entry.CategoryName}\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
        return RedirectToAction("Ledger");
    }

    // DELETE: ลบรายการบัญชีออกจากตารางสมุดบัญชีรายวันในฐานข้อมูล
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteLedgerEntry(int ledgerId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายบัญชีและการเงิน
        if (!IsAuthorizedAccountant())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีฝ่ายบัญชีและการเงิน";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหารายการบัญชีที่ต้องการลบในฐานข้อมูล
        var entry = _db.Financialledgers.FirstOrDefault(f => f.EntryId == ledgerId);
        // ตรวจสอบว่าพบรายการในฐานข้อมูลหรือไม่
        if (entry == null)
        {
            // แจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการบัญชีที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
            return RedirectToAction("Ledger");
        }

        // บันทึกชื่อหมวดหมู่ไว้สำหรับแสดงในข้อความแจ้งเตือน
        var catName = entry.CategoryName;
        // ลบรายการออกจากบริบทฐานข้อมูล
        _db.Financialledgers.Remove(entry);
        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความว่าการลบสำเร็จ
        TempData["SuccessMessage"] = $"ลบรายการ \"{catName}\" ออกจากสมุดบัญชีเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้าสมุดบัญชี
        return RedirectToAction("Ledger");
    }
}
