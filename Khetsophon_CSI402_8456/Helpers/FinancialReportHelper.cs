using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Helpers;

// คลาสตัวช่วยกลางสำหรับคำนวณรายงานยอดขาย รายรับ รายจ่าย และความเคลื่อนไหวสต็อกจากฐานข้อมูลจริงร่วมกันระหว่าง Owner และ Accountant
public static class FinancialReportHelper
{
    // รายชื่อเดือนภาษาไทยแบบเต็มสำหรับการแสดงผล
    private static readonly string[] ThaiMonthsFull =
    {
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
    };

    // รายชื่อเดือนภาษาไทยแบบย่อสำหรับแกนกราฟ
    private static readonly string[] ThaiMonthsShort =
    {
        "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
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
            monthOptions.Add(new MonthOptionItem
            {
                Key = m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Label = FormatThaiMonthYear(m)
            });
        }
        monthOptions.Add(new MonthOptionItem
        {
            Key = "all",
            Label = "ภาพรวมทั้งหมด"
        });

        // ตรวจสอบช่วงเวลาที่เลือก หากไม่ได้ระบุให้ใช้เดือนปัจจุบันเป็นค่าเริ่มต้นตรงกันทุกบทบาท
        string activeKey = string.IsNullOrWhiteSpace(selectedMonth)
            ? currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : selectedMonth.Trim();

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!monthOptions.Any(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase)))
        {
            activeKey = currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool isAllTime = activeKey.Equals("all", StringComparison.OrdinalIgnoreCase);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        DateTime targetMonthStart = currentMonthStart;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!isAllTime && DateTime.TryParseExact(activeKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            targetMonthStart = parsedMonth;
        }
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        DateTime targetMonthEnd = targetMonthStart.AddMonths(1);
        DateOnly targetDateStart = DateOnly.FromDateTime(targetMonthStart);
        DateOnly targetDateEnd = DateOnly.FromDateTime(targetMonthEnd);

        // คำนวณเดือนก่อนหน้า (เก่ากว่า) และเดือนถัดไป (ใหม่กว่า) โดยไม่วนกลับแบบสับสน
        int selectedIndex = monthOptions.FindIndex(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase));
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int lastMonthlyIndex = monthOptions.Count - 2; // ดัชนีสุดท้ายของเดือนจริง (ก่อน 'all')
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string prevKey = (!isAllTime && selectedIndex >= 0 && selectedIndex < lastMonthlyIndex)
            ? monthOptions[selectedIndex + 1].Key
            : string.Empty;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string nextKey = (!isAllTime && selectedIndex > 0)
            ? monthOptions[selectedIndex - 1].Key
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

        // กรองข้อมูลตามช่วงเดือนที่เลือก (หรือรวมทั้งหมดเมื่อเลือกภาพรวมทั้งหมด)
        var periodReceipts = isAllTime
            ? allReceipts
            : allReceipts.Where(r => r.IssuedAt >= targetMonthStart && r.IssuedAt < targetMonthEnd).ToList();

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var periodLedgers = isAllTime
            ? allLedgers
            : allLedgers.Where(l => l.EntryDate >= targetDateStart && l.EntryDate < targetDateEnd).ToList();

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var periodPreorders = isAllTime
            ? allPreorders
            : allPreorders.Where(p => p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd).ToList();

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var periodInventoryLogs = isAllTime
            ? allInventoryLogs
            : allInventoryLogs.Where(l => l.Timestamp >= targetMonthStart && l.Timestamp < targetMonthEnd).ToList();

        // คำนวณยอดรายได้ หน้าร้าน รายรับอื่น รายจ่าย และพรีออเดอร์ค้างรับ
        decimal storefrontRevenue = periodReceipts.Sum(r => r.NetTotalAmount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal ledgerIncome = periodLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal totalExpenses = periodLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal totalRevenue = storefrontRevenue + ledgerIncome;

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal preordersPendingTotal = allPreorders
            .Where(p => p.CurrentStatus == "Pending" || p.CurrentStatus == "รอชำระ" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
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
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal pct = Math.Round(((totalRevenue - prevRev) / prevRev) * 100m, 1);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                string arrow = pct >= 0 ? "▲ +" : "▼ ";
                revenueDeltaText = $"{arrow}{pct}% เทียบกับเดือนก่อน";
            }
            // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
            else if (totalRevenue > 0)
            {
                revenueDeltaText = $"หน้าร้าน ฿{storefrontRevenue:N0} · สมุดบัญชี ฿{ledgerIncome:N0}";
            }

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int prevPreorders = allPreorders.Count(p => p.CreatedAt >= prevStart && p.CreatedAt < prevEnd);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int diffPreorder = totalPreorders - prevPreorders;
            preorderDeltaText = diffPreorder >= 0
                ? $"▲ +{diffPreorder} ออเดอร์เทียบเดือนก่อน"
                : $"▼ {diffPreorder} ออเดอร์เทียบเดือนก่อน";
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

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal mRev = allReceipts.Where(r => r.IssuedAt >= mStart && r.IssuedAt < mEnd).Sum(r => r.NetTotalAmount)
                         + allLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal mExp = allLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);

            rawSixMonths.Add((mStart.ToString("yyyy-MM", CultureInfo.InvariantCulture), ThaiMonthsShort[mStart.Month - 1], mRev, mExp));
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal maxChartValue = rawSixMonths.SelectMany(x => new[] { x.Rev, x.Exp }).DefaultIfEmpty(0m).Max();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var chartBars = rawSixMonths.Select(x => new MonthlyChartBarItem
        {
            MonthKey = x.Key,
            MonthShort = x.Short,
            Revenue = x.Rev,
            Expense = x.Exp,
            RevenueShortLabel = FormatCompactBaht(x.Rev),
            RevenueHeightPct = maxChartValue > 0 && x.Rev > 0 ? Math.Max(8, (int)Math.Round((x.Rev / maxChartValue) * 100m)) : 0,
            ExpenseHeightPct = maxChartValue > 0 && x.Exp > 0 ? Math.Max(8, (int)Math.Round((x.Exp / maxChartValue) * 100m)) : 0,
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
                    catName = prod.Category.CategoryName;
                }
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal val = item.LineTotal ?? (item.Quantity * item.UnitPrice);
                categorySalesMap[catName] = categorySalesMap.GetValueOrDefault(catName) + val;
            }
        }

        // กรณีหมวดหมู่ใดยังไม่มีรายการขายในช่วงที่เลือก ให้เติมชื่อหมวดหมู่จริงจากฐานข้อมูลด้วยยอด 0 เพื่อไม่ให้เกิดหมวดหมู่จำลอง
        foreach (var cat in db.Categories.ToList())
        {
            // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
            if (!categorySalesMap.ContainsKey(cat.CategoryName))
            {
                categorySalesMap[cat.CategoryName] = 0m;
            }
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal maxCatRevenue = categorySalesMap.Values.DefaultIfEmpty(0m).Max();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var categorySummaries = categorySalesMap
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(5)
            .Select(kv => new CategoryRevenueItem
            {
                CategoryName = kv.Key,
                RevenueAmount = kv.Value,
                BarPercent = maxCatRevenue > 0 && kv.Value > 0 ? Math.Max(6, (int)Math.Round((kv.Value / maxCatRevenue) * 100m)) : 0
            })
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
            int loggedRestockInPeriod = periodInventoryLogs
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                .Sum(l => ExtractRestockedQty(l.Message));

            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int totalAllTimeOut = allReceiptItems.Where(ri => ri.ProductId == p.ProductId).Sum(ri => ri.Quantity);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int totalAllTimeLoggedIn = allInventoryLogs
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                .Sum(l => ExtractRestockedQty(l.Message));

            // คำนวณยอดรับเข้าตั้งต้นของสินค้าในเดือนที่สร้างรายการเพื่อให้ยอดรับเข้าลบขายออกสอดคล้องกับคงเหลือจริง
            int initialStockForCreationMonth = Math.Max(0, (remainQty + totalAllTimeOut) - totalAllTimeLoggedIn);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            bool createdInTargetPeriod = isAllTime || (p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd);
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int inQty = loggedRestockInPeriod + (createdInTargetPeriod ? initialStockForCreationMonth : 0);

            movementRows.Add(new StockMovementRowItem
            {
                ProductId = p.ProductId,
                Sku = p.Sku,
                ProductName = p.Name,
                CategoryName = p.Category?.CategoryName ?? "ทั่วไป",
                InQty = inQty,
                OutQty = outQty,
                RemainQty = remainQty,
                InValue = inQty * p.RetailPrice,
                OutValue = outVal
            });
        }

        // เรียงลำดับสินค้าที่มียอดขายออกมากที่สุดขึ้นก่อน
        var orderedRows = movementRows
            .OrderByDescending(r => r.OutValue)
            .ThenByDescending(r => r.InQty)
            .ThenBy(r => r.ProductName)
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
            SelectedMonthKey = activeKey,
            SelectedMonthLabel = monthOptions.First(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase)).Label,
            PrevMonthKey = prevKey,
            NextMonthKey = nextKey,
            AvailableMonths = monthOptions,
            TotalRevenue = totalRevenue,
            StorefrontRevenue = storefrontRevenue,
            LedgerIncome = ledgerIncome,
            RecordedExpenses = totalExpenses,
            PreordersPendingTotal = preordersPendingTotal,
            ReceiptCount = periodReceipts.Count,
            TotalStockInQty = grandInQty,
            TotalStockOutQty = grandOutQty,
            CompletedPreorderCount = completedPreorders,
            TotalPreorderCount = totalPreorders,
            RevenueDeltaText = revenueDeltaText,
            StockInDeltaText = $"คงเหลือในคลังปัจจุบัน {grandRemainQty:N0} ชิ้น",
            StockOutDeltaText = $"ยอดขายตามใบเสร็จ {periodReceipts.Count} ใบ",
            PreorderDeltaText = preorderDeltaText,
            MonthlyTrends = chartBars,
            CategorySummaries = categorySummaries,
            StockMovementRows = pagedRows,
            GrandTotalInQty = grandInQty,
            GrandTotalOutQty = grandOutQty,
            GrandTotalRemainQty = grandRemainQty,
            GrandTotalInValue = grandInVal,
            GrandTotalOutValue = grandOutVal,
            TotalProductRowCount = orderedRows.Count,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }
}

