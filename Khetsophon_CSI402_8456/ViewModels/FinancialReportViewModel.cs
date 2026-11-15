using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using System;
using System.Collections.Generic;

namespace Khetsophon_CSI402_8456.ViewModels;

// โครงสร้างข้อมูลตัวเลือกช่วงเดือนสำหรับแสดงในรายการเลือกบนหน้ารายงานและหน้าการเงิน
public class MonthOptionItem
{
    // รหัสช่วงเดือนรูปแบบ yyyy-MM หรือ all สำหรับภาพรวมทั้งหมด
    public string Key { get; set; } = string.Empty;

    // ชื่อช่วงเดือนภาษาไทยปีพุทธศักราชสำหรับแสดงบนหน้าเว็บ
    public string Label { get; set; } = string.Empty;
}

// โครงสร้างข้อมูลแท่งกราฟรายรับและรายจ่ายย้อนหลัง 6 เดือนจากฐานข้อมูลจริง
public class MonthlyChartBarItem
{
    // รหัสเดือนรูปแบบ yyyy-MM
    public string MonthKey { get; set; } = string.Empty;

    // ชื่อย่อเดือนภาษาไทยสำหรับแกนแนวนอนของกราฟ
    public string MonthShort { get; set; } = string.Empty;

    // ยอดรายรับรวมจริงของเดือนนั้น
    public decimal Revenue { get; set; }

    // ยอดรายจ่ายหรือต้นทุนจริงของเดือนนั้น
    public decimal Expense { get; set; }

    // ข้อความย่อยอดเงินสำหรับแสดงบนหัวแท่งกราฟ
    public string RevenueShortLabel { get; set; } = "฿0";

    // ความสูงคิดเป็นร้อยละของแท่งรายรับเทียบกับเดือนที่มียอดสูงสุด
    public int RevenueHeightPct { get; set; }

    // ความสูงคิดเป็นร้อยละของแท่งรายจ่ายเทียบกับยอดสูงสุดในช่วง 6 เดือน
    public int ExpenseHeightPct { get; set; }

    // สถานะระบุว่าเป็นเดือนที่กำลังเลือกดูอยู่ในปัจจุบันหรือไม่
    public bool IsCurrent { get; set; }
}

// โครงสร้างข้อมูลสรุปรายได้แยกตามหมวดหมู่สินค้าจริงจากใบเสร็จรับเงิน
public class CategoryRevenueItem
{
    // ชื่อหมวดหมู่สินค้าภาษาไทยในร้าน
    public string CategoryName { get; set; } = string.Empty;

    // ยอดรายได้รวมจริงของหมวดหมู่นั้น
    public decimal RevenueAmount { get; set; }

    // สัดส่วนร้อยละเทียบกับหมวดหมู่ที่มียอดสูงสุดสำหรับแถบความคืบหน้า
    public int BarPercent { get; set; }
}

// โครงสร้างข้อมูลตารางรายละเอียดการรับเข้า ขายออก และคงเหลือรายสินค้าจากฐานข้อมูลจริง
public class StockMovementRowItem
{
    // รหัสอ้างอิงสินค้าในฐานข้อมูล
    public int ProductId { get; set; }

    // รหัสสินค้า
    public string Sku { get; set; } = string.Empty;

    // ชื่อสินค้าฟิกเกอร์หรือโมเดล
    public string ProductName { get; set; } = string.Empty;

    // ชื่อหมวดหมู่ของสินค้า
    public string CategoryName { get; set; } = string.Empty;

    // จำนวนสินค้าที่รับเข้าคลังในช่วงเวลาที่เลือก
    public int InQty { get; set; }

    // จำนวนสินค้าที่ขายออกไปจริงตามใบเสร็จรับเงินในช่วงเวลาที่เลือก
    public int OutQty { get; set; }

    // จำนวนสินค้าคงเหลือปัจจุบันในคลังสินค้า
    public int RemainQty { get; set; }

    // มูลค่าสินค้าที่รับเข้าคำนวณจากราคาขายมาตรฐาน
    public decimal InValue { get; set; }

    // มูลค่าขายออกจริงตามรายการใบเสร็จรับเงิน
    public decimal OutValue { get; set; }
}

// ViewModel กลางสำหรับหน้าสรุปรายงานเจ้าของร้านและหน้าภาพรวมการเงินฝ่ายบัญชีให้ใช้ตัวเลขจากฐานข้อมูลชุดเดียวกัน
public class FinancialReportViewModel
{
    // รหัสช่วงเวลาที่กำลังแสดงผลอยู่ เช่น 2026-11 หรือ all
    public string SelectedMonthKey { get; set; } = string.Empty;

    // ชื่อช่วงเวลาที่กำลังแสดงผลเป็นภาษาไทย เช่น พฤศจิกายน 2569
    public string SelectedMonthLabel { get; set; } = string.Empty;

    // รหัสเดือนก่อนหน้าสำหรับปุ่มเลื่อนช่วงเวลาบนหน้ารายงาน
    public string PrevMonthKey { get; set; } = string.Empty;

    // รหัสเดือนถัดไปสำหรับปุ่มเลื่อนช่วงเวลาบนหน้ารายงาน
    public string NextMonthKey { get; set; } = string.Empty;

    // รายการตัวเลือกช่วงเดือนทั้งหมดสำหรับแสดงในเมนูแบบเลื่อนลง
    public List<MonthOptionItem> AvailableMonths { get; set; } = new();

    // ยอดรายได้รวมทั้งหมด (รายได้หน้าร้านตามใบเสร็จ + รายรับในสมุดบัญชี)
    public decimal TotalRevenue { get; set; }

    // ยอดรายได้จากการขายหน้าร้านและรับพรีออเดอร์ตามใบเสร็จจริง
    public decimal StorefrontRevenue { get; set; }

    // ยอดรายรับเพิ่มเติมที่บันทึกในสมุดบัญชีรายวัน
    public decimal LedgerIncome { get; set; }

    // ยอดรายจ่ายและต้นทุนที่บันทึกในสมุดบัญชีรายวัน
    public decimal RecordedExpenses { get; set; }

    // กำไรขั้นต้น (รายได้รวม - รายจ่ายที่บันทึก)
    public decimal GrossProfit => TotalRevenue - RecordedExpenses;

    // อัตรากำไรขั้นต้นเป็นร้อยละ
    public decimal GrossMarginPct => TotalRevenue > 0 ? Math.Round((GrossProfit / TotalRevenue) * 100m, 1) : 0m;

    // ยอดคำสั่งซื้อพรีออเดอร์ที่ยังค้างรับสินค้าและรอชำระเงิน
    public decimal PreordersPendingTotal { get; set; }

    // จำนวนใบเสร็จทั้งหมดในช่วงเวลาที่เลือก
    public int ReceiptCount { get; set; }

    // จำนวนสินค้ารับเข้าคลังรวมในช่วงเวลาที่เลือก
    public int TotalStockInQty { get; set; }

    // จำนวนสินค้าขายออกจริงรวมตามใบเสร็จในช่วงเวลาที่เลือก
    public int TotalStockOutQty { get; set; }

    // จำนวนใบพรีออเดอร์ที่เสร็จสิ้นในช่วงเวลาที่เลือก
    public int CompletedPreorderCount { get; set; }

    // จำนวนใบพรีออเดอร์ทั้งหมดในช่วงเวลาที่เลือก
    public int TotalPreorderCount { get; set; }

    // ข้อความเปรียบเทียบยอดรายได้กับเดือนก่อนหน้าจากข้อมูลจริง
    public string RevenueDeltaText { get; set; } = string.Empty;

    // ข้อความสรุปจำนวนสินค้ารับเข้าคลังสินค้า
    public string StockInDeltaText { get; set; } = string.Empty;

    // ข้อความสรุปจำนวนสินค้าขายออกตามใบเสร็จ
    public string StockOutDeltaText { get; set; } = string.Empty;

    // ข้อความเปรียบเทียบจำนวนออเดอร์พรีออเดอร์กับเดือนก่อนหน้า
    public string PreorderDeltaText { get; set; } = string.Empty;

    // ข้อมูลกราฟแท่งเปรียบเทียบรายรับและรายจ่ายย้อนหลัง 6 เดือนล่าสุด
    public List<MonthlyChartBarItem> MonthlyTrends { get; set; } = new();

    // ข้อมูลสรุปรายได้แยกตามหมวดหมู่สินค้าจริง
    public List<CategoryRevenueItem> CategorySummaries { get; set; } = new();

    // ข้อมูลตารางรายละเอียดสต็อกเข้า-ออกในหน้าปัจจุบัน (แบ่งหน้าละ 10 รายการ)
    public List<StockMovementRowItem> StockMovementRows { get; set; } = new();

    // ยอดรวมจำนวนสินค้ารับเข้าคลังทั้งหมด
    public int GrandTotalInQty { get; set; }

    // ยอดรวมจำนวนสินค้าขายออกทั้งหมด
    public int GrandTotalOutQty { get; set; }

    // ยอดรวมจำนวนสินค้าคงเหลือทั้งหมด
    public int GrandTotalRemainQty { get; set; }

    // มูลค่ารวมสินค้ารับเข้าคลังทั้งหมด
    public decimal GrandTotalInValue { get; set; }

    // มูลค่ารวมสินค้าขายออกทั้งหมด
    public decimal GrandTotalOutValue { get; set; }

    // ข้อมูลควบคุมจำนวนแถวสินค้าทั้งหมดในตารางสต็อกเข้า-ออก
    public int TotalProductRowCount { get; set; }

    // หมายเลขหน้าปัจจุบันของการแบ่งหน้า
    public int CurrentPage { get; set; } = 1;

    // จำนวนรายการที่แสดงต่อหน้า
    public int PageSize { get; set; } = 10;

    // จำนวนหน้าทั้งหมดที่คำนวณจากจำนวนแถวและขนาดหน้า
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalProductRowCount / PageSize));


    // รายชื่อเดือนภาษาไทยแบบเต็มสำหรับการแสดงผล
    private static readonly string[] ThaiMonthsFull =
    {
        // รายชื่อเดือนมกราคมถึงมิถุนายน
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        // รายชื่อเดือนกรกฎาคมถึงธันวาคม
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
    };

    // รายชื่อเดือนภาษาไทยแบบย่อสำหรับแกนกราฟ
    private static readonly string[] ThaiMonthsShort =
    {
        // รายชื่อเดือนย่อ ม.ค. ถึง มิ.ย.
        "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
        // รายชื่อเดือนย่อ ก.ค. ถึง ธ.ค.
        "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."
    };

    // ฟังก์ชันแปลงวันที่เป็นข้อความเดือนปีพุทธศักราชภาษาไทย
    public static string FormatThaiMonthYear(DateTime date)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int thaiYear = date.Year + 543;
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return $"{ThaiMonthsFull[date.Month - 1]} {thaiYear}";
    }

    // ฟังก์ชันแปลงจำนวนเงินเป็นข้อความสั้นสำหรับแสดงบนหัวแท่งกราฟ
    private static string FormatCompactBaht(decimal amount)
    {
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (amount <= 0) return "฿0";
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (amount >= 1000000m) return $"฿{(amount / 1000000m):N1}M";
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (amount >= 1000m) return $"฿{(amount / 1000m):N0}K";
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return $"฿{amount:N0}";
    }

    // ฟังก์ชันแกะจำนวนชิ้นที่รับเข้าคลังจากข้อความบันทึกกิจกรรมระบบโมดูลคลังสินค้า
    private static int ExtractRestockedQty(string? message)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(message)) return 0;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var match = Regex.Match(message, @"\+(\d+)\s*ชิ้น");
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (match.Success && int.TryParse(match.Groups[1].Value, out int qty))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return qty;
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return 0;
    }

    // ฟังก์ชันหลักสร้าง ViewModel รายงานการเงินและสต็อกจากฐานข้อมูลจริงสำหรับช่วงเดือนที่เลือก
    public static FinancialReportViewModel BuildReport(Csi402dbContext db, string? selectedMonth, int page = 1)
    {
        // ใช้วันที่ปัจจุบันเป็นฐานในการสร้างตัวเลือก 6 เดือนล่าสุด
        var now = DateTime.Today;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);

        // สร้างรายการตัวเลือก 6 เดือนล่าสุดเรียงจากเดือนปัจจุบันย้อนหลัง และเพิ่มตัวเลือกภาพรวมทั้งหมด
        var monthOptions = new List<MonthOptionItem>();
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        for (int i = 0; i < 6; i++)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var m = currentMonthStart.AddMonths(-i);
            // เพิ่มรายการตัวเลือกเดือนลงในคอลเลกชัน
            monthOptions.Add(new MonthOptionItem
            {
                // กำหนดรหัสเดือนในรูปแบบปีและเดือน ค.ศ.
                Key = m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                // กำหนดป้ายชื่อเดือนและปี พ.ศ. ภาษาไทย
                Label = FormatThaiMonthYear(m)
            });
        }
        // เพิ่มตัวเลือกภาพรวมทั้งหมดลงในคอลเลกชัน
        monthOptions.Add(new MonthOptionItem
        {
            // กำหนดรหัสคีย์พิเศษสำหรับภาพรวมทั้งหมด
            Key = "all",
            // กำหนดชื่อป้ายกำกับสำหรับแสดงภาพรวมทุกช่วงเวลา
            Label = "ภาพรวมทั้งหมด"
        });

        // กำหนดรหัสเดือนที่กำลังเลือก หากว่างให้ใช้เดือนปัจจุบัน
        string activeKey = string.IsNullOrWhiteSpace(selectedMonth)
            // กรณีไม่ได้ระบุช่วงเดือน ให้ใช้เดือนปัจจุบัน
            ? currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            // กรณีระบุช่วงเดือน ให้ตัดช่องว่างส่วนเกินออก
            : selectedMonth.Trim();

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!monthOptions.Any(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase)))
        {
            // กำหนดค่าเริ่มต้นกลับเป็นเดือนปัจจุบันเมื่อไม่พบตัวเลือกที่ระบุ
            activeKey = currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool isAllTime = activeKey.Equals("all", StringComparison.OrdinalIgnoreCase);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        DateTime targetMonthStart = currentMonthStart;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!isAllTime && DateTime.TryParseExact(activeKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            // กำหนดวันเริ่มต้นของเดือนที่แปลงได้สำเร็จ
            targetMonthStart = parsedMonth;
        }
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        DateTime targetMonthEnd = targetMonthStart.AddMonths(1);
        // แปลงวันเริ่มต้นของเดือนเป้าหมายเป็น DateOnly สำหรับเปรียบเทียบในสมุดบัญชี
        DateOnly targetDateStart = DateOnly.FromDateTime(targetMonthStart);
        // แปลงวันสิ้นสุดของเดือนเป้าหมายเป็น DateOnly สำหรับเปรียบเทียบในสมุดบัญชี
        DateOnly targetDateEnd = DateOnly.FromDateTime(targetMonthEnd);

        // คำนวณเดือนก่อนหน้า (เก่ากว่า) และเดือนถัดไป (ใหม่กว่า) โดยไม่วนกลับแบบสับสน
        int selectedIndex = monthOptions.FindIndex(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase));
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int lastMonthlyIndex = monthOptions.Count - 2; // ดัชนีสุดท้ายของเดือนจริง (ก่อน 'all')
        // คำนวณรหัสเดือนก่อนหน้าสำหรับปุ่มนำทาง
        string prevKey = (!isAllTime && selectedIndex >= 0 && selectedIndex < lastMonthlyIndex)
            // หากมีเดือนก่อนหน้า ให้ดึงรหัสเดือนถัดไปในลิสต์
            ? monthOptions[selectedIndex + 1].Key
            // หากไม่มีเดือนก่อนหน้า ให้เป็นค่าว่าง
            : string.Empty;
        // คำนวณรหัสเดือนถัดไปสำหรับปุ่มนำทาง
        string nextKey = (!isAllTime && selectedIndex > 0)
            // หากมีเดือนถัดไป ให้ดึงรหัสเดือนก่อนหน้าในลิสต์
            ? monthOptions[selectedIndex - 1].Key
            // หากไม่มีเดือนถัดไป ให้เป็นค่าว่าง
            : string.Empty;

        // ดึงข้อมูลจริงทั้งหมดที่จำเป็นจากฐานข้อมูล MySQL
        var allReceipts = db.Receipts.Include(r => r.Receiptitems).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var allLedgers = db.Financialledgers.ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var allPreorders = db.Preorders.ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var allProducts = db.Products.Include(p => p.Category).Include(p => p.Inventory).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var allInventoryLogs = db.Systemlogs.Where(l => l.Module == "Inventory").ToList();

        // กรองข้อมูลใบเสร็จตามช่วงเดือนที่เลือกหรือภาพรวมทั้งหมด
        var periodReceipts = isAllTime
            // กรณีเลือกภาพรวมทั้งหมด ให้ใช้ข้อมูลใบเสร็จทั้งหมด
            ? allReceipts
            // กรณีเลือกเฉพาะเดือน ให้กรองตามช่วงวันที่ออกใบเสร็จ
            : allReceipts.Where(r => r.IssuedAt >= targetMonthStart && r.IssuedAt < targetMonthEnd).ToList();

        // กรองข้อมูลสมุดบัญชีตามช่วงเดือนที่เลือกหรือภาพรวมทั้งหมด
        var periodLedgers = isAllTime
            // กรณีเลือกภาพรวมทั้งหมด ให้ใช้ข้อมูลสมุดบัญชีทั้งหมด
            ? allLedgers
            // กรณีเลือกเฉพาะเดือน ให้กรองตามช่วงวันที่บันทึกบัญชี
            : allLedgers.Where(l => l.EntryDate >= targetDateStart && l.EntryDate < targetDateEnd).ToList();

        // กรองข้อมูลพรีออเดอร์ตามช่วงเดือนที่เลือกหรือภาพรวมทั้งหมด
        var periodPreorders = isAllTime
            // กรณีเลือกภาพรวมทั้งหมด ให้ใช้ข้อมูลพรีออเดอร์ทั้งหมด
            ? allPreorders
            // กรณีเลือกเฉพาะเดือน ให้กรองตามช่วงวันที่สั่งจอง
            : allPreorders.Where(p => p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd).ToList();

        // กรองข้อมูลบันทึกกิจกรรมสต็อกตามช่วงเดือนที่เลือกหรือภาพรวมทั้งหมด
        var periodInventoryLogs = isAllTime
            // กรณีเลือกภาพรวมทั้งหมด ให้ใช้บันทึกกิจกรรมสต็อกทั้งหมด
            ? allInventoryLogs
            // กรณีเลือกเฉพาะเดือน ให้กรองตามช่วงเวลาของบันทึกกิจกรรม
            : allInventoryLogs.Where(l => l.Timestamp >= targetMonthStart && l.Timestamp < targetMonthEnd).ToList();

        // คำนวณยอดรายได้ หน้าร้าน รายรับอื่น รายจ่าย และพรีออเดอร์ค้างรับ
        decimal storefrontRevenue = periodReceipts.Sum(r => r.NetTotalAmount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal ledgerIncome = periodLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal totalExpenses = periodLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal totalRevenue = storefrontRevenue + ledgerIncome;

        // คำนวณยอดเงินรวมของคำสั่งซื้อพรีออเดอร์ที่ยังค้างดำเนินการ
        decimal preordersPendingTotal = allPreorders
            // กรองเฉพาะพรีออเดอร์ที่มีสถานะรอชำระเงินหรือรอรับสินค้า
            .Where(p => p.CurrentStatus == "Pending" || p.CurrentStatus == "รอชำระ" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
            // รวมยอดราคาจำหน่ายสุทธิทั้งหมด
            .Sum(p => p.TotalFinalPrice);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int completedPreorders = periodPreorders.Count(p => p.CurrentStatus == "Completed" || p.CurrentStatus == "PickedUp");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalPreorders = periodPreorders.Count;

        // คำนวณยอดเปรียบเทียบกับเดือนก่อนหน้าเมื่อดูแบบรายเดือน
        string revenueDeltaText = $"จากใบเสร็จ {periodReceipts.Count} ใบ";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string preorderDeltaText = $"ทั้งหมด {totalPreorders} ออเดอร์ในช่วงนี้";
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!isAllTime)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var prevStart = targetMonthStart.AddMonths(-1);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var prevEnd = targetMonthStart;
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var prevDateStart = DateOnly.FromDateTime(prevStart);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var prevDateEnd = DateOnly.FromDateTime(prevEnd);

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal prevStorefront = allReceipts.Where(r => r.IssuedAt >= prevStart && r.IssuedAt < prevEnd).Sum(r => r.NetTotalAmount);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal prevLedgerInc = allLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= prevDateStart && l.EntryDate < prevDateEnd).Sum(l => l.Amount);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal prevRev = prevStorefront + prevLedgerInc;

            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (prevRev > 0)
            {
                // คำนวณสัดส่วนร้อยละของการเปลี่ยนแปลงยอดรายได้เทียบกับเดือนก่อน
                decimal pct = Math.Round(((totalRevenue - prevRev) / prevRev) * 100m, 1);
                // กำหนดเครื่องหมายตามทิศทางการเติบโตของรายได้
                string arrow = pct >= 0 ? "+" : "";
                // สร้างข้อความแสดงผลการเปรียบเทียบยอดรายได้
                revenueDeltaText = $"{arrow}{pct}% เทียบกับเดือนก่อน";
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (totalRevenue > 0)
            {
                // กำหนดข้อความแยกยอดรายได้หน้าร้านและสมุดบัญชีเมื่อไม่มีข้อมูลเดือนก่อน
                revenueDeltaText = $"หน้าร้าน ฿{storefrontRevenue:N0} · สมุดบัญชี ฿{ledgerIncome:N0}";
            }

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int prevPreorders = allPreorders.Count(p => p.CreatedAt >= prevStart && p.CreatedAt < prevEnd);
            // คำนวณส่วนต่างจำนวนพรีออเดอร์เทียบกับเดือนก่อนหน้า
            int diffPreorder = totalPreorders - prevPreorders;
            // กำหนดข้อความเปรียบเทียบจำนวนออเดอร์ตามทิศทางการเปลี่ยนแปลง
            preorderDeltaText = diffPreorder >= 0
                // กรณีจำนวนออเดอร์เพิ่มขึ้นหรือเท่าเดิม
                ? $"+{diffPreorder} ออเดอร์เทียบเดือนก่อน"
                // กรณีจำนวนออเดอร์ลดลง
                : $"{diffPreorder} ออเดอร์เทียบเดือนก่อน";
        }

        // สร้างข้อมูลกราฟแท่งย้อนหลัง 6 เดือนล่าสุดจากฐานข้อมูลจริง (เรียงจากเดือนเก่าไปเดือนล่าสุด)
        var rawSixMonths = new List<(string Key, string Short, decimal Rev, decimal Exp)>();
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        for (int i = 5; i >= 0; i--)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var mStart = currentMonthStart.AddMonths(-i);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var mEnd = mStart.AddMonths(1);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var dStart = DateOnly.FromDateTime(mStart);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var dEnd = DateOnly.FromDateTime(mEnd);

            // คำนวณรายรับรวมประจำเดือนจากใบเสร็จและสมุดบัญชีรายรับ
            decimal mRev = allReceipts.Where(r => r.IssuedAt >= mStart && r.IssuedAt < mEnd).Sum(r => r.NetTotalAmount)
                         // รวมรายรับเพิ่มเติมจากสมุดบัญชีการเงิน
                         + allLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);
            // คำนวณรายจ่ายรวมประจำเดือนจากสมุดบัญชีรายจ่าย
            decimal mExp = allLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);

            // บันทึกสถิติประจำเดือนลงในคอลเลกชัน 6 เดือน
            rawSixMonths.Add((mStart.ToString("yyyy-MM", CultureInfo.InvariantCulture), ThaiMonthsShort[mStart.Month - 1], mRev, mExp));
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal maxChartValue = rawSixMonths.SelectMany(x => new[] { x.Rev, x.Exp }).DefaultIfEmpty(0m).Max();
        // แปลงข้อมูลสถิติ 6 เดือนเป็นรายการแท่งกราฟ
        var chartBars = rawSixMonths.Select(x => new MonthlyChartBarItem
        {
            // กำหนดรหัสเดือนของแท่งกราฟ
            MonthKey = x.Key,
            // กำหนดชื่อย่อเดือนภาษาไทย
            MonthShort = x.Short,
            // กำหนดยอดรายรับรวม
            Revenue = x.Rev,
            // กำหนดยอดรายจ่ายรวม
            Expense = x.Exp,
            // กำหนดข้อความย่อยอดเงินสำหรับหัวแท่งกราฟ
            RevenueShortLabel = FormatCompactBaht(x.Rev),
            // คำนวณความสูงร้อยละของแท่งรายรับ
            RevenueHeightPct = maxChartValue > 0 && x.Rev > 0 ? Math.Max(8, (int)Math.Round((x.Rev / maxChartValue) * 100m)) : 0,
            // คำนวณความสูงร้อยละของแท่งรายจ่าย
            ExpenseHeightPct = maxChartValue > 0 && x.Exp > 0 ? Math.Max(8, (int)Math.Round((x.Exp / maxChartValue) * 100m)) : 0,
            // ระบุว่าเป็นเดือนที่กำลังเลือกดูอยู่หรือไม่
            IsCurrent = isAllTime ? x.Key == currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture) : x.Key == activeKey
        }).ToList();

        // คำนวณสรุปรายได้แยกตามหมวดหมู่สินค้าจริงจากรายการในใบเสร็จรับเงิน
        var productLookup = allProducts.ToDictionary(p => p.ProductId, p => p);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var categorySalesMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var rcpt in periodReceipts)
        {
            // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
            foreach (var item in rcpt.Receiptitems)
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string catName = "สินค้าทั่วไป";
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (item.ProductId.HasValue && productLookup.TryGetValue(item.ProductId.Value, out var prod) && prod.Category != null)
                {
                    // กำหนดชื่อหมวดหมู่จากข้อมูลสินค้าจริง
                    catName = prod.Category.CategoryName;
                }
                // คำนวณมูลค่ายอดขายของแถวรายการใบเสร็จ
                decimal val = item.LineTotal ?? (item.Quantity * item.UnitPrice);
                // สะสมยอดขายรวมของหมวดหมู่สินค้านี้
                categorySalesMap[catName] = categorySalesMap.GetValueOrDefault(catName) + val;
            }
        }

        // กรณีหมวดหมู่ใดยังไม่มีรายการขายในช่วงที่เลือก ให้เติมชื่อหมวดหมู่จริงจากฐานข้อมูลด้วยยอด 0 เพื่อไม่ให้เกิดหมวดหมู่จำลอง
        foreach (var cat in db.Categories.ToList())
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!categorySalesMap.ContainsKey(cat.CategoryName))
            {
                // กำหนดยอดขายเริ่มต้นเป็น 0 สำหรับหมวดหมู่ที่ยังไม่มียอดขาย
                categorySalesMap[cat.CategoryName] = 0m;
            }
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal maxCatRevenue = categorySalesMap.Values.DefaultIfEmpty(0m).Max();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var categorySummaries = categorySalesMap
            // จัดเรียงหมวดหมู่ตามยอดขายจากมากไปน้อย
            .OrderByDescending(kv => kv.Value)
            // จัดเรียงลำดับรองตามชื่อหมวดหมู่สินค้า
            .ThenBy(kv => kv.Key)
            // เลือกเฉพาะ 5 หมวดหมู่แรกที่มียอดขายสูงสุด
            .Take(5)
            // แปลงข้อมูลยอดขายแต่ละหมวดหมู่เป็น ViewModel สรุปรายได้
            .Select(kv => new CategoryRevenueItem
            {
                // กำหนดชื่อหมวดหมู่สินค้า
                CategoryName = kv.Key,
                // กำหนดยอดรายได้รวมของหมวดหมู่
                RevenueAmount = kv.Value,
                // คำนวณความยาวร้อยละของแถบความคืบหน้า
                BarPercent = maxCatRevenue > 0 && kv.Value > 0 ? Math.Max(6, (int)Math.Round((kv.Value / maxCatRevenue) * 100m)) : 0
            })
            // แปลงผลลัพธ์เป็นรายการลิสต์
            .ToList();

        // คำนวณตารางความเคลื่อนไหวสต็อกเข้า-ออกรายสินค้าจากฐานข้อมูลจริง
        var periodReceiptItems = periodReceipts.SelectMany(r => r.Receiptitems).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var allReceiptItems = allReceipts.SelectMany(r => r.Receiptitems).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var movementRows = new List<StockMovementRowItem>();

        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var p in allProducts)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int remainQty = p.Inventory?.QuantityOnHand ?? 0;
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            var soldInPeriod = periodReceiptItems.Where(ri => ri.ProductId == p.ProductId).ToList();
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int outQty = soldInPeriod.Sum(ri => ri.Quantity);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal outVal = soldInPeriod.Sum(ri => ri.LineTotal ?? (ri.Quantity * ri.UnitPrice));

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            // คำนวณจำนวนชิ้นที่รับเข้าคลังตามบันทึกกิจกรรมในช่วงเวลาที่เลือก
            int loggedRestockInPeriod = periodInventoryLogs
                // กรองบันทึกกิจกรรมสต็อกที่ระบุรหัสสินค้า
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                // แกะจำนวนชิ้นรับเข้าและหาผลรวม
                .Sum(l => ExtractRestockedQty(l.Message));

            // คำนวณยอดขายออกสะสมตลอดกาลของสินค้านี้
            int totalAllTimeOut = allReceiptItems.Where(ri => ri.ProductId == p.ProductId).Sum(ri => ri.Quantity);
            // คำนวณจำนวนรับเข้าสะสมตลอดกาลตามบันทึกกิจกรรม
            int totalAllTimeLoggedIn = allInventoryLogs
                // กรองบันทึกกิจกรรมสต็อกทั้งหมดที่ระบุรหัสสินค้า
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                // แกะจำนวนชิ้นรับเข้าสะสมและหาผลรวม
                .Sum(l => ExtractRestockedQty(l.Message));

            // คำนวณยอดรับเข้าตั้งต้นของสินค้าในเดือนที่สร้างรายการเพื่อให้ยอดรับเข้าลบขายออกสอดคล้องกับคงเหลือจริง
            int initialStockForCreationMonth = Math.Max(0, (remainQty + totalAllTimeOut) - totalAllTimeLoggedIn);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            bool createdInTargetPeriod = isAllTime || (p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int inQty = loggedRestockInPeriod + (createdInTargetPeriod ? initialStockForCreationMonth : 0);

            // เพิ่มแถวข้อมูลความเคลื่อนไหวสต็อกสินค้าลงในรายการ
            movementRows.Add(new StockMovementRowItem
            {
                // กำหนดรหัสอ้างอิงสินค้า
                ProductId = p.ProductId,
                // กำหนดรหัสสินค้า SKU
                Sku = p.Sku,
                // กำหนดชื่อสินค้า
                ProductName = p.Name,
                // กำหนดชื่อหมวดหมู่สินค้า
                CategoryName = p.Category?.CategoryName ?? "ทั่วไป",
                // กำหนดจำนวนรับเข้าคลัง
                InQty = inQty,
                // กำหนดจำนวนขายออกตามใบเสร็จ
                OutQty = outQty,
                // กำหนดจำนวนสินค้าคงเหลือปัจจุบัน
                RemainQty = remainQty,
                // คำนวณมูลค่ารวมของสินค้าที่รับเข้า
                InValue = inQty * p.RetailPrice,
                // กำหนดมูลค่ารวมของสินค้าที่ขายออก
                OutValue = outVal
            });
        }

        // เรียงลำดับสินค้าที่มียอดขายออกมากที่สุดขึ้นก่อน
        var orderedRows = movementRows
            // จัดเรียงตามมูลค่าขายออกจริงจากมากไปน้อย
            .OrderByDescending(r => r.OutValue)
            // จัดเรียงลำดับรองตามจำนวนรับเข้าคลังจากมากไปน้อย
            .ThenByDescending(r => r.InQty)
            // จัดเรียงลำดับสุดท้ายตามชื่อสินค้า
            .ThenBy(r => r.ProductName)
            // แปลงผลลัพธ์เป็นรายการแถวข้อมูล
            .ToList();

        // คำนวณยอดรวมทั้งหมดของทุกสินค้าก่อนแบ่งหน้า
        int grandInQty = orderedRows.Sum(r => r.InQty);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int grandOutQty = orderedRows.Sum(r => r.OutQty);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int grandRemainQty = orderedRows.Sum(r => r.RemainQty);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal grandInVal = orderedRows.Sum(r => r.InValue);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal grandOutVal = orderedRows.Sum(r => r.OutValue);

        // แบ่งหน้าตารางสต็อกเข้า-ออกหน้าละ 10 รายการ
        int pageSize = 10;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)orderedRows.Count / pageSize));
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var pagedRows = orderedRows.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new FinancialReportViewModel
        {
            // กำหนดรหัสเดือนที่กำลังแสดงผล
            SelectedMonthKey = activeKey,
            // กำหนดชื่อเดือนภาษาไทยปี พ.ศ. ที่กำลังแสดงผล
            SelectedMonthLabel = monthOptions.First(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase)).Label,
            // กำหนดรหัสเดือนก่อนหน้าสำหรับปุ่มนำทาง
            PrevMonthKey = prevKey,
            // กำหนดรหัสเดือนถัดไปสำหรับปุ่มนำทาง
            NextMonthKey = nextKey,
            // กำหนดรายการตัวเลือกช่วงเดือนทั้งหมด
            AvailableMonths = monthOptions,
            // กำหนดยอดรายได้รวมทั้งหมด
            TotalRevenue = totalRevenue,
            // กำหนดยอดรายได้จากการขายหน้าร้าน
            StorefrontRevenue = storefrontRevenue,
            // กำหนดยอดรายรับจากสมุดบัญชี
            LedgerIncome = ledgerIncome,
            // กำหนดยอดรายจ่ายที่บันทึกในสมุดบัญชี
            RecordedExpenses = totalExpenses,
            // กำหนดยอดพรีออเดอร์ค้างรับสินค้า
            PreordersPendingTotal = preordersPendingTotal,
            // กำหนดจำนวนใบเสร็จรับเงินทั้งหมด
            ReceiptCount = periodReceipts.Count,
            // กำหนดจำนวนสินค้ารับเข้ารวม
            TotalStockInQty = grandInQty,
            // กำหนดจำนวนสินค้าขายออกรวม
            TotalStockOutQty = grandOutQty,
            // กำหนดจำนวนออเดอร์พรีออเดอร์ที่เสร็จสิ้น
            CompletedPreorderCount = completedPreorders,
            // กำหนดจำนวนออเดอร์พรีออเดอร์ทั้งหมด
            TotalPreorderCount = totalPreorders,
            // กำหนดข้อความเปรียบเทียบยอดรายได้
            RevenueDeltaText = revenueDeltaText,
            // กำหนดข้อความสรุปจำนวนสินค้ารับเข้าคลัง
            StockInDeltaText = $"คงเหลือในคลังปัจจุบัน {grandRemainQty:N0} ชิ้น",
            // กำหนดข้อความสรุปยอดขายตามใบเสร็จ
            StockOutDeltaText = $"ยอดขายตามใบเสร็จ {periodReceipts.Count} ใบ",
            // กำหนดข้อความเปรียบเทียบจำนวนออเดอร์พรีออเดอร์
            PreorderDeltaText = preorderDeltaText,
            // กำหนดรายการแท่งกราฟสถิติ 6 เดือน
            MonthlyTrends = chartBars,
            // กำหนดรายการสรุปรายได้ตามหมวดหมู่สินค้า
            CategorySummaries = categorySummaries,
            // กำหนดแถวข้อมูลสต็อกสินค้าตามการแบ่งหน้า
            StockMovementRows = pagedRows,
            // กำหนดยอดรวมสินค้ารับเข้าทั้งหมด
            GrandTotalInQty = grandInQty,
            // กำหนดยอดรวมสินค้าขายออกทั้งหมด
            GrandTotalOutQty = grandOutQty,
            // กำหนดยอดรวมสินค้าคงเหลือทั้งหมด
            GrandTotalRemainQty = grandRemainQty,
            // กำหนดมูลค่ารวมสินค้ารับเข้าทั้งหมด
            GrandTotalInValue = grandInVal,
            // กำหนดมูลค่ารวมสินค้าขายออกทั้งหมด
            GrandTotalOutValue = grandOutVal,
            // กำหนดจำนวนแถวสินค้าทั้งหมด
            TotalProductRowCount = orderedRows.Count,
            // กำหนดหมายเลขหน้าปัจจุบัน
            CurrentPage = currentPage,
            // กำหนดขนาดหน้าต่อรายการ
            PageSize = pageSize
        };
    }

}
