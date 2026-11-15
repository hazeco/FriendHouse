using Khetsophon_CSI402_8456.ViewModels;
using System.Text.Json;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // ตรวจสอบสิทธิ์ตามมาตรฐาน 1 เบราว์เซอร์ 1 บทบาท ก่อนรัน Action ใน AccountantController
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของเบราว์เซอร์
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // หากเบราว์เซอร์ล็อกอินด้วยบทบาทอื่นที่ไม่ใช่ฝ่ายบัญชี ให้บังคับเปลี่ยนเส้นทางไปยังบทบาทนั้นทันที
        if (!string.IsNullOrEmpty(activeRole) && activeRole != "accountant")
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

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทฝ่ายบัญชีและการเงิน
    private bool IsAuthorizedAccountant()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชันของผู้ใช้งาน
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // อ่านค่ารหัสผู้ใช้งานเฉพาะฝ่ายบัญชีจากเซสชัน
        var accountantId = HttpContext.Session.GetInt32("AccountantUserId");
        // ตรวจสอบว่าบทบาทตรงกับฝ่ายบัญชีหรือมีเซสชันเฉพาะของฝ่ายบัญชี
        return role == "accountant" || accountantId.HasValue;
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
        var reportModel = FinancialReportViewModel.BuildReport(_db, selectedMonth, 1);

        // ส่งข้อมูล ViewModel ไปแสดงผลที่หน้าภาพรวมการเงินร้าน
        return View(reportModel);
    }

    // ฟังก์ชันช่วยดึงรหัสผู้ใช้งานปัจจุบันจากเซสชัน (รองรับฝ่ายบัญชี เจ้าของร้าน และฝ่ายไอที)
    private int? GetCurrentAccountantUserId()
    {
        // ค้นหารหัสผู้ใช้งานจากเซสชันตามลำดับสิทธิ์
        return HttpContext.Session.GetInt32("AccountantUserId")
            // หากไม่พบ ให้ดึงจากเซสชันเจ้าของร้าน
            ?? HttpContext.Session.GetInt32("OwnerUserId")
            // หากไม่พบ ให้ดึงจากเซสชันไอทีซัพพอร์ต
            ?? HttpContext.Session.GetInt32("ITSupportUserId")
            // หากไม่พบ ให้ดึงจากเซสชันผู้ใช้ทั่วไป
            ?? HttpContext.Session.GetInt32("UserId");
    }

    // ฟังก์ชันช่วยสร้างคิวรีใบเสร็จพร้อมตัวกรองที่ใช้ร่วมกันระหว่างหน้าแสดงผลและส่งออก CSV
    private IQueryable<Receipt> BuildFilteredReceiptsQuery(string? searchTerm, DateOnly? startDate, DateOnly? endDate, string? promoFilter)
    {
        // เริ่มต้นสร้างคิวรีดึงข้อมูลใบเสร็จพร้อมตารางที่เกี่ยวข้อง
        var query = _db.Receipts
            // โหลดรายการสินค้าในใบเสร็จ
            .Include(r => r.Receiptitems)
            // โหลดข้อมูลพนักงานแคชเชียร์
            .Include(r => r.CashierUser)
            // โหลดข้อมูลลูกค้า
            .Include(r => r.Customer)
            // แปลงเป็น IQueryable สำหรับสร้างเงื่อนไขต่อ
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
                // กรองจากเลขที่ใบเสร็จ
                r.ReceiptNo.ToLower().Contains(term) ||
                // กรองจากเลขที่ใบเสร็จแบบตัดขีด
                (!string.IsNullOrEmpty(termNoHyphen) && r.ReceiptNo.Replace("-", "").ToLower().Contains(termNoHyphen)) ||
                // กรองจากข้อมูลบาร์โค้ด
                (r.BarcodeData != null && (r.BarcodeData.ToLower().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && r.BarcodeData.Replace("-", "").ToLower().Contains(termNoHyphen)))) ||
                // กรองจากเบอร์โทรศัพท์ลูกค้า
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
            // จัดเรียงตามวันเวลาที่ออกใบเสร็จจากล่าสุดไปเก่าสุด
            .OrderByDescending(r => r.IssuedAt)
            // แปลงผลลัพธ์เป็นรายการลิสต์
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
    // แอ็กชันสำหรับส่งออกข้อมูลใบเสร็จรับเงินเป็นไฟล์ CSV
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
            // จัดเรียงตามวันเวลาที่ออกใบเสร็จจากล่าสุดไปเก่าสุด
            .OrderByDescending(r => r.IssuedAt)
            // แปลงผลลัพธ์เป็นรายการลิสต์
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
                // รวบรวมชื่อรายการสินค้าและจำนวนชิ้น
                ? string.Join(" | ", r.Receiptitems.Select(i => $"{i.ItemDescription} x{i.Quantity}"))
                // กรณีไม่มีรายการสินค้าให้ระบุสินค้าหน้าร้านเริ่มต้น
                : "สินค้าหน้าร้าน x1";
            // กำหนดชื่อลูกค้า
            var customerName = r.Customer != null
                // ใช้ชื่อเต็มหรืออีเมลของลูกค้าสมาชิก
                ? (r.Customer.FullName ?? r.Customer.Email)
                // ตรวจสอบเบอร์โทรสำหรับลูกค้าทั่วไป
                : (string.IsNullOrEmpty(r.CustomerPhone) ? "ลูกค้าทั่วไป" : "ลูกค้าสมาชิก");
            // กำหนดชื่อพนักงานที่ออกใบเสร็จ
            var staffName = r.CashierUser != null
                // ใช้ชื่อเต็มหรืออีเมลของพนักงานแคชเชียร์
                ? (r.CashierUser.FullName ?? r.CashierUser.Email)
                // กรณีไม่พบข้อมูลพนักงานให้ระบุพนักงานหน้าร้าน
                : "พนักงานหน้าร้าน";

            // จัดรูปแบบคอลัมน์ข้อมูลใบเสร็จรับเงินสำหรับไฟล์ CSV (เลขที่ใบเสร็จ, วันเวลา, เบอร์โทร, ลูกค้า, รายการ, ยอดเงิน, พนักงาน)
            sb.AppendLine(string.Join(",",
                // ข้อมูลระบุตัวตนใบเสร็จ: หมายเลขใบเสร็จและวันเวลาออกเอกสาร
                EscapeCsvField(r.ReceiptNo),
                // ฟิลด์วันเวลาที่ออกใบเสร็จ
                EscapeCsvField(r.IssuedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                // ข้อมูลลูกค้า: เบอร์โทรศัพท์และชื่อลูกค้า
                EscapeCsvField(string.IsNullOrEmpty(r.CustomerPhone) ? "ทั่วไป" : r.CustomerPhone),
                // ฟิลด์ชื่อลูกค้า
                EscapeCsvField(customerName),
                // ข้อมูลรายการสินค้าที่ซื้อ
                EscapeCsvField(itemSummary),
                // ข้อมูลตัวเลขยอดเงิน: ยอดรวม ส่วนลด ภาษี และยอดสุทธิ
                r.SubtotalAmount.ToString("0.00"),
                // ฟิลด์ยอดส่วนลด
                r.DiscountAmount.ToString("0.00"),
                // ฟิลด์ภาษีมูลค่าเพิ่ม
                r.VatAmount.ToString("0.00"),
                // ฟิลด์ยอดสุทธิ
                r.NetTotalAmount.ToString("0.00"),
                // ข้อมูลพนักงานแคชเชียร์ผู้ทำรายการ
                EscapeCsvField(staffName)));
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
            // โหลดข้อมูลผู้บันทึกรายการบัญชี
            .Include(f => f.RecordedByUser)
            // จัดเรียงตามวันที่บันทึกจากล่าสุดไปเก่าสุด
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
        // กำหนดหมายเลขหน้าปัจจุบัน
        ViewBag.CurrentPage = currentPage;
        // กำหนดจำนวนหน้าทั้งหมด
        ViewBag.TotalPages = totalPages;
        // กำหนดขนาดหน้ารายการ
        ViewBag.PageSize = pageSize;

        // ส่งรายการบันทึกบัญชีของหน้าปัจจุบันไปแสดงผลที่หน้าสมุดบันทึกรายรับ-รายจ่าย
        return View(pagedEntries);
    }

    // CREATE: บันทึกรายการรายรับ-รายจ่ายใหม่ลงในตารางสมุดบัญชีรายวัน
    // รองรับคำขอแบบ POST สำหรับเพิ่มรายการบัญชี
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับเพิ่มรายการใหม่ในสมุดบัญชี
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
            // กำหนดประเภทรายการบัญชี
            EntryType = normalizedType,
            // กำหนดชื่อหมวดหมู่รายการบัญชี
            CategoryName = cleanCategory,
            // กำหนดคำอธิบายรายละเอียด
            Description = Note?.Trim() ?? "",
            // กำหนดยอดเงินรายการบัญชี
            Amount = Amount.Value,
            // กำหนดวันที่ทำรายการบัญชี
            EntryDate = EntryDate ?? DateOnly.FromDateTime(DateTime.Today),
            // กำหนดรหัสผู้ใช้ผู้บันทึกรายการ
            RecordedByUserId = userId.Value,
            // กำหนดวันเวลาที่บันทึกลงระบบ
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
    // รองรับคำขอแบบ POST สำหรับแก้ไขรายการบัญชี
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับแก้ไขรายการในสมุดบัญชี
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
    // รองรับคำขอแบบ POST สำหรับลบรายการบัญชี
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับลบรายการในสมุดบัญชี
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
