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

    // รหัสเดือนก่อนหน้าและเดือนถัดไปสำหรับปุ่มเลื่อนช่วงเวลาบนหน้ารายงาน
    public string PrevMonthKey { get; set; } = string.Empty;
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

    // ข้อความเปรียบเทียบผลประกอบการกับเดือนก่อนหน้าจากข้อมูลจริง
    public string RevenueDeltaText { get; set; } = string.Empty;
    public string StockInDeltaText { get; set; } = string.Empty;
    public string StockOutDeltaText { get; set; } = string.Empty;
    public string PreorderDeltaText { get; set; } = string.Empty;

    // ข้อมูลกราฟแท่งเปรียบเทียบรายรับและรายจ่ายย้อนหลัง 6 เดือนล่าสุด
    public List<MonthlyChartBarItem> MonthlyTrends { get; set; } = new();

    // ข้อมูลสรุปรายได้แยกตามหมวดหมู่สินค้าจริง
    public List<CategoryRevenueItem> CategorySummaries { get; set; } = new();

    // ข้อมูลตารางรายละเอียดสต็อกเข้า-ออกในหน้าปัจจุบัน (แบ่งหน้าละ 10 รายการ)
    public List<StockMovementRowItem> StockMovementRows { get; set; } = new();

    // ยอดรวมทั้งหมดทุกสินค้าในตารางสต็อกเข้า-ออก
    public int GrandTotalInQty { get; set; }
    public int GrandTotalOutQty { get; set; }
    public int GrandTotalRemainQty { get; set; }
    public decimal GrandTotalInValue { get; set; }
    public decimal GrandTotalOutValue { get; set; }

    // ข้อมูลควบคุมการแบ่งหน้าตารางสต็อกเข้า-ออก
    public int TotalProductRowCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalProductRowCount / PageSize));
}

