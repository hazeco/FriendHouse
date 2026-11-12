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
        int thaiYear = date.Year + 543;
        return $"{ThaiMonthsFull[date.Month - 1]} {thaiYear}";
    }

    // ฟังก์ชันแปลงจำนวนเงินเป็นข้อความสั้นสำหรับแสดงบนหัวแท่งกราฟ
    private static string FormatCompactBaht(decimal amount)
    {
        if (amount <= 0) return "฿0";
        if (amount >= 1000000m) return $"฿{(amount / 1000000m):N1}M";
        if (amount >= 1000m) return $"฿{(amount / 1000m):N0}K";
        return $"฿{amount:N0}";
    }

    // ฟังก์ชันแกะจำนวนชิ้นที่รับเข้าคลังจากข้อความบันทึกกิจกรรมระบบโมดูลคลังสินค้า
    private static int ExtractRestockedQty(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return 0;
        var match = Regex.Match(message, @"\+(\d+)\s*ชิ้น");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int qty))
        {
            return qty;
        }
        return 0;
    }

    // ฟังก์ชันหลักสร้าง ViewModel รายงานการเงินและสต็อกจากฐานข้อมูลจริงสำหรับช่วงเดือนที่เลือก
    public static FinancialReportViewModel BuildReport(Csi402dbContext db, string? selectedMonth, int page = 1)
    {
        // ใช้วันที่ปัจจุบันเป็นฐานในการสร้างตัวเลือก 6 เดือนล่าสุด
        var now = DateTime.Today;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);

        // สร้างรายการตัวเลือก 6 เดือนล่าสุดเรียงจากเดือนปัจจุบันย้อนหลัง และเพิ่มตัวเลือกภาพรวมทั้งหมด
        var monthOptions = new List<MonthOptionItem>();
        for (int i = 0; i < 6; i++)
        {
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

        if (!monthOptions.Any(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase)))
        {
            activeKey = currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        bool isAllTime = activeKey.Equals("all", StringComparison.OrdinalIgnoreCase);
        DateTime targetMonthStart = currentMonthStart;
        if (!isAllTime && DateTime.TryParseExact(activeKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            targetMonthStart = parsedMonth;
        }
        DateTime targetMonthEnd = targetMonthStart.AddMonths(1);
        DateOnly targetDateStart = DateOnly.FromDateTime(targetMonthStart);
        DateOnly targetDateEnd = DateOnly.FromDateTime(targetMonthEnd);

        // คำนวณเดือนก่อนหน้าและเดือนถัดไปสำหรับปุ่มเลื่อนช่วงเวลา
        int selectedIndex = monthOptions.FindIndex(m => m.Key.Equals(activeKey, StringComparison.OrdinalIgnoreCase));
        string prevKey = monthOptions[(selectedIndex + 1) % monthOptions.Count].Key;
        string nextKey = monthOptions[(selectedIndex - 1 + monthOptions.Count) % monthOptions.Count].Key;

        // ดึงข้อมูลจริงทั้งหมดที่จำเป็นจากฐานข้อมูล MySQL
        var allReceipts = db.Receipts.Include(r => r.Receiptitems).ToList();
        var allLedgers = db.Financialledgers.ToList();
        var allPreorders = db.Preorders.ToList();
        var allProducts = db.Products.Include(p => p.Category).Include(p => p.Inventory).ToList();
        var allInventoryLogs = db.Systemlogs.Where(l => l.Module == "Inventory").ToList();

        // กรองข้อมูลตามช่วงเดือนที่เลือก (หรือรวมทั้งหมดเมื่อเลือกภาพรวมทั้งหมด)
        var periodReceipts = isAllTime
            ? allReceipts
            : allReceipts.Where(r => r.IssuedAt >= targetMonthStart && r.IssuedAt < targetMonthEnd).ToList();

        var periodLedgers = isAllTime
            ? allLedgers
            : allLedgers.Where(l => l.EntryDate >= targetDateStart && l.EntryDate < targetDateEnd).ToList();

        var periodPreorders = isAllTime
            ? allPreorders
            : allPreorders.Where(p => p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd).ToList();

        var periodInventoryLogs = isAllTime
            ? allInventoryLogs
            : allInventoryLogs.Where(l => l.Timestamp >= targetMonthStart && l.Timestamp < targetMonthEnd).ToList();

        // คำนวณยอดรายได้ หน้าร้าน รายรับอื่น รายจ่าย และพรีออเดอร์ค้างรับ
        decimal storefrontRevenue = periodReceipts.Sum(r => r.NetTotalAmount);
        decimal ledgerIncome = periodLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        decimal totalExpenses = periodLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
        decimal totalRevenue = storefrontRevenue + ledgerIncome;

        decimal preordersPendingTotal = allPreorders
            .Where(p => p.CurrentStatus == "Pending" || p.CurrentStatus == "รอชำระ" || p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
            .Sum(p => p.TotalFinalPrice);

        int completedPreorders = periodPreorders.Count(p => p.CurrentStatus == "Completed" || p.CurrentStatus == "PickedUp");
        int totalPreorders = periodPreorders.Count;

        // คำนวณยอดเปรียบเทียบกับเดือนก่อนหน้าเมื่อดูแบบรายเดือน
        string revenueDeltaText = $"จากใบเสร็จ {periodReceipts.Count} ใบ";
        string preorderDeltaText = $"ทั้งหมด {totalPreorders} ออเดอร์ในช่วงนี้";
        if (!isAllTime)
        {
            var prevStart = targetMonthStart.AddMonths(-1);
            var prevEnd = targetMonthStart;
            var prevDateStart = DateOnly.FromDateTime(prevStart);
            var prevDateEnd = DateOnly.FromDateTime(prevEnd);

            decimal prevStorefront = allReceipts.Where(r => r.IssuedAt >= prevStart && r.IssuedAt < prevEnd).Sum(r => r.NetTotalAmount);
            decimal prevLedgerInc = allLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= prevDateStart && l.EntryDate < prevDateEnd).Sum(l => l.Amount);
            decimal prevRev = prevStorefront + prevLedgerInc;

            if (prevRev > 0)
            {
                decimal pct = Math.Round(((totalRevenue - prevRev) / prevRev) * 100m, 1);
                string arrow = pct >= 0 ? "▲ +" : "▼ ";
                revenueDeltaText = $"{arrow}{pct}% เทียบกับเดือนก่อน";
            }
            else if (totalRevenue > 0)
            {
                revenueDeltaText = $"หน้าร้าน ฿{storefrontRevenue:N0} · สมุดบัญชี ฿{ledgerIncome:N0}";
            }

            int prevPreorders = allPreorders.Count(p => p.CreatedAt >= prevStart && p.CreatedAt < prevEnd);
            int diffPreorder = totalPreorders - prevPreorders;
            preorderDeltaText = diffPreorder >= 0
                ? $"▲ +{diffPreorder} ออเดอร์เทียบเดือนก่อน"
                : $"▼ {diffPreorder} ออเดอร์เทียบเดือนก่อน";
        }

        // สร้างข้อมูลกราฟแท่งย้อนหลัง 6 เดือนล่าสุดจากฐานข้อมูลจริง (เรียงจากเดือนเก่าไปเดือนล่าสุด)
        var rawSixMonths = new List<(string Key, string Short, decimal Rev, decimal Exp)>();
        for (int i = 5; i >= 0; i--)
        {
            var mStart = currentMonthStart.AddMonths(-i);
            var mEnd = mStart.AddMonths(1);
            var dStart = DateOnly.FromDateTime(mStart);
            var dEnd = DateOnly.FromDateTime(mEnd);

            decimal mRev = allReceipts.Where(r => r.IssuedAt >= mStart && r.IssuedAt < mEnd).Sum(r => r.NetTotalAmount)
                         + allLedgers.Where(l => l.EntryType.Equals("income", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);
            decimal mExp = allLedgers.Where(l => l.EntryType.Equals("expense", StringComparison.OrdinalIgnoreCase) && l.EntryDate >= dStart && l.EntryDate < dEnd).Sum(l => l.Amount);

            rawSixMonths.Add((mStart.ToString("yyyy-MM", CultureInfo.InvariantCulture), ThaiMonthsShort[mStart.Month - 1], mRev, mExp));
        }

        decimal maxChartValue = rawSixMonths.SelectMany(x => new[] { x.Rev, x.Exp }).DefaultIfEmpty(0m).Max();
        var chartBars = rawSixMonths.Select(x => new MonthlyChartBarItem
        {
            MonthKey = x.Key,
            MonthShort = x.Short,
            Revenue = x.Rev,
            Expense = x.Exp,
            RevenueShortLabel = FormatCompactBaht(x.Rev),
            RevenueHeightPct = maxChartValue > 0 && x.Rev > 0 ? Math.Max(8, (int)Math.Round((x.Rev / maxChartValue) * 100m)) : 4,
            ExpenseHeightPct = maxChartValue > 0 && x.Exp > 0 ? Math.Max(8, (int)Math.Round((x.Exp / maxChartValue) * 100m)) : 4,
            IsCurrent = isAllTime ? x.Key == currentMonthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture) : x.Key == activeKey
        }).ToList();

        // คำนวณสรุปรายได้แยกตามหมวดหมู่สินค้าจริงจากรายการในใบเสร็จรับเงิน
        var productLookup = allProducts.ToDictionary(p => p.ProductId, p => p);
        var categorySalesMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var rcpt in periodReceipts)
        {
            foreach (var item in rcpt.Receiptitems)
            {
                string catName = "สินค้าทั่วไป";
                if (item.ProductId.HasValue && productLookup.TryGetValue(item.ProductId.Value, out var prod) && prod.Category != null)
                {
                    catName = prod.Category.CategoryName;
                }
                decimal val = item.LineTotal ?? (item.Quantity * item.UnitPrice);
                categorySalesMap[catName] = categorySalesMap.GetValueOrDefault(catName) + val;
            }
        }

        // กรณีหมวดหมู่ใดยังไม่มีรายการขายในช่วงที่เลือก ให้เติมชื่อหมวดหมู่จริงจากฐานข้อมูลด้วยยอด 0 เพื่อไม่ให้เกิดหมวดหมู่จำลอง
        foreach (var cat in db.Categories.ToList())
        {
            if (!categorySalesMap.ContainsKey(cat.CategoryName))
            {
                categorySalesMap[cat.CategoryName] = 0m;
            }
        }

        decimal maxCatRevenue = categorySalesMap.Values.DefaultIfEmpty(0m).Max();
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
        var allReceiptItems = allReceipts.SelectMany(r => r.Receiptitems).ToList();
        var movementRows = new List<StockMovementRowItem>();

        foreach (var p in allProducts)
        {
            int remainQty = p.Inventory?.QuantityOnHand ?? 0;
            var soldInPeriod = periodReceiptItems.Where(ri => ri.ProductId == p.ProductId).ToList();
            int outQty = soldInPeriod.Sum(ri => ri.Quantity);
            decimal outVal = soldInPeriod.Sum(ri => ri.LineTotal ?? (ri.Quantity * ri.UnitPrice));

            int loggedRestockInPeriod = periodInventoryLogs
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                .Sum(l => ExtractRestockedQty(l.Message));

            int totalAllTimeOut = allReceiptItems.Where(ri => ri.ProductId == p.ProductId).Sum(ri => ri.Quantity);
            int totalAllTimeLoggedIn = allInventoryLogs
                .Where(l => !string.IsNullOrEmpty(l.Message) && l.Message.Contains(p.Sku, StringComparison.OrdinalIgnoreCase))
                .Sum(l => ExtractRestockedQty(l.Message));

            // คำนวณยอดรับเข้าตั้งต้นของสินค้าในเดือนที่สร้างรายการเพื่อให้ยอดรับเข้าลบขายออกสอดคล้องกับคงเหลือจริง
            int initialStockForCreationMonth = Math.Max(0, (remainQty + totalAllTimeOut) - totalAllTimeLoggedIn);
            bool createdInTargetPeriod = isAllTime || (p.CreatedAt >= targetMonthStart && p.CreatedAt < targetMonthEnd);
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
        int grandOutQty = orderedRows.Sum(r => r.OutQty);
        int grandRemainQty = orderedRows.Sum(r => r.RemainQty);
        decimal grandInVal = orderedRows.Sum(r => r.InValue);
        decimal grandOutVal = orderedRows.Sum(r => r.OutValue);

        // แบ่งหน้าตารางสต็อกเข้า-ออกหน้าละ 10 รายการ
        int pageSize = 10;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)orderedRows.Count / pageSize));
        int currentPage = Math.Clamp(page, 1, totalPages);
        var pagedRows = orderedRows.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

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

