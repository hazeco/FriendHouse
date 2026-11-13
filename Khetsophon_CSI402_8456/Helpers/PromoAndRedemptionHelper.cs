using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Helpers;

/// <summary>
/// ผลลัพธ์จากการตรวจสอบและคำนวณโปรโมชั่นกับรายการสินค้าในตะกร้า
/// </summary>
public class PromoEvaluationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Promotion? Promotion { get; set; }
    public string VoucherCode => Promotion?.VoucherCode ?? string.Empty;
    public string PromoName => Promotion?.Name ?? string.Empty;
    public string PromoType => (Promotion?.PromoType ?? string.Empty).ToLowerInvariant();
    public decimal SubtotalAmount { get; set; }
    public decimal EligibleSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsDoublePoints { get; set; }
    public string BenefitSummary { get; set; } = string.Empty;
}

/// <summary>
/// ข้อมูลวันนัดรับและเบอร์ติดต่อของการจองแลกของรางวัล (จัดเก็บผ่าน Systemlogs.DetailPayload)
/// </summary>
public class RedemptionMetaPayload
{
    public string RedemptionCode { get; set; } = string.Empty;
    public string PickupDate { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

/// <summary>
/// ข้อมูลรหัสโปรโมชั่นและส่วนลดของใบสั่งจองพรีออเดอร์ (จัดเก็บผ่าน Systemlogs.DetailPayload)
/// </summary>
public class PreorderPromoPayload
{
    public string ReservationNumber { get; set; } = string.Empty;
    public string VoucherCode { get; set; } = string.Empty;
    public string PromoName { get; set; } = string.Empty;
    public string PromoType { get; set; } = string.Empty;
    public decimal OriginalSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsDoublePoints { get; set; }
}

/// <summary>
/// ศูนย์รวมตรรกะการตรวจสอบขอบเขต/เงื่อนไขโปรโมชั่น การคำนวณส่วนลด และข้อมูลเสริมของการแลกของรางวัล
/// </summary>
public static class PromoAndRedemptionHelper
{
    public const string CollectedPromoSessionKey = "CollectedPromoCodes";
    public const string AppliedPromoSessionKey = "AppliedPromoCode";

    public static DateOnly TodayGregorian() =>
        new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

    public static string NormalizeCode(string? code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant();

    public static string NormalizeCodeNoHyphen(string? code) =>
        NormalizeCode(code).Replace("-", "").Replace("#", "").Replace(" ", "");

    public static string GetPromoTypeThaiLabel(string? promoType) =>
        (promoType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "percent" or "percentage" => "ส่วนลดเปอร์เซ็นต์",
            "fixed" => "ส่วนลดมูลค่าคงที่",
            "bogo" => "ซื้อ 1 แถม 1",
            "bundle" => "ชุดเซ็ตสุดคุ้ม",
            "double_points" => "แต้มสะสมสองเท่า",
            _ => "โปรโมชั่นพิเศษ"
        };

    public static string GetScopeThaiLabel(string? scope) =>
        (scope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "all" or "" => "สินค้าทุกหมวดหมู่ในร้าน",
            "figures" => "สินค้าหมวดฟิกเกอร์",
            "clearance" => "สินค้าลดล้างสต๊อก / พร้อมส่ง",
            _ => scope!
        };

    public static bool MatchesScope(Product? product, string? scope)
    {
        if (product == null) return false;

        string normalizedScope = (scope ?? "all").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "all")
        {
            return true;
        }

        if (normalizedScope == "figures")
        {
            string catName = product.Category?.CategoryName ?? string.Empty;
            string slug = (product.Category?.Slug ?? string.Empty).ToLowerInvariant();
            return catName.Contains("ฟิกเกอร์")
                || slug.Contains("figure")
                || slug == "limited-collectibles";
        }

        if (normalizedScope == "clearance")
        {
            int available = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
            return !product.IsPreorder && available > 0;
        }

        return true;
    }

    public static PromoEvaluationResult EvaluateCartPromotion(
        Promotion? promo,
        IReadOnlyCollection<Cartitem> cartItems,
        DateOnly? todayOverride = null)
    {
        var today = todayOverride ?? TodayGregorian();
        decimal subtotal = cartItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);

        if (promo == null)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                SubtotalAmount = subtotal,
                ErrorMessage = "ไม่พบรหัสโปรโมชั่นนี้ในระบบ กรุณาตรวจสอบตัวสะกดอีกครั้ง"
            };
        }

        if (promo.IsActive == false)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ปิดการใช้งานชั่วคราวแล้ว"
            };
        }

        if (today < promo.StartDate)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัส \"{promo.VoucherCode}\" จะเริ่มใช้งานได้ตั้งแต่วันที่ {promo.StartDate:dd/MM/yyyy}"
            };
        }

        if (today > promo.EndDate)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว"
            };
        }

        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ถูกใช้ครบจำนวนสิทธิ์แล้ว"
            };
        }

        var eligibleItems = cartItems.Where(c => MatchesScope(c.Product, promo.ApplicableScope)).ToList();
        decimal eligibleSubtotal = eligibleItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);
        int eligibleQty = eligibleItems.Sum(c => c.Quantity);

        if (eligibleItems.Count == 0 || eligibleSubtotal <= 0m)
        {
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"โค้ด \"{promo.VoucherCode}\" ใช้ได้เฉพาะ{GetScopeThaiLabel(promo.ApplicableScope)} แต่ไม่มีสินค้าที่ร่วมรายการในตะกร้า"
            };
        }

        decimal minSpend = promo.MinimumSpend ?? 0m;
        if (subtotal < minSpend)
        {
            decimal diff = minSpend - subtotal;
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                EligibleSubtotal = eligibleSubtotal,
                ErrorMessage = $"ยอดรวมในตะกร้ายังไม่ถึงขั้นต่ำ ฿{minSpend:N0} สำหรับโค้ด \"{promo.VoucherCode}\" (ขาดอีก ฿{diff:N0})"
            };
        }

        string promoType = (promo.PromoType ?? "percent").Trim().ToLowerInvariant();
        decimal discountValue = promo.DiscountValue ?? 0m;
        decimal discount = 0m;
        bool isDoublePoints = false;
        string summary;

        switch (promoType)
        {
            case "percent":
            case "percentage":
                discount = Math.Round(eligibleSubtotal * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
                discount = Math.Clamp(discount, 0m, eligibleSubtotal);
                summary = $"ลด {discountValue:0.#}% (ประหยัด ฿{discount:N0})";
                break;

            case "bogo":
                if (eligibleQty < 2)
                {
                    return new PromoEvaluationResult
                    {
                        IsValid = false,
                        Promotion = promo,
                        SubtotalAmount = subtotal,
                        EligibleSubtotal = eligibleSubtotal,
                        ErrorMessage = $"โค้ดซื้อ 1 แถม 1 ({promo.VoucherCode}) ต้องมีสินค้าที่ร่วมรายการอย่างน้อย 2 ชิ้นในตะกร้า"
                    };
                }
                decimal cheapestUnit = eligibleItems.Min(c => c.Product?.RetailPrice ?? 0m);
                decimal bogoCap = discountValue > 0m ? discountValue : cheapestUnit;
                discount = Math.Clamp(bogoCap, 0m, eligibleSubtotal);
                summary = $"สิทธิพิเศษซื้อ 1 แถม 1 (ส่วนลด ฿{discount:N0})";
                break;

            case "double_points":
                isDoublePoints = true;
                summary = "รับแต้มสะสม x2 เมื่อชำระเงินที่หน้าร้าน";
                break;

            case "fixed":
            case "bundle":
            default:
                discount = Math.Clamp(discountValue, 0m, eligibleSubtotal);
                summary = $"ส่วนลดท้ายบิล ฿{discount:N0}";
                break;
        }

        return new PromoEvaluationResult
        {
            IsValid = true,
            Promotion = promo,
            SubtotalAmount = subtotal,
            EligibleSubtotal = eligibleSubtotal,
            DiscountAmount = discount,
            IsDoublePoints = isDoublePoints,
            BenefitSummary = summary
        };
    }

    public static List<string> ReadCollectedCodes(string? rawSessionValue)
    {
        if (string.IsNullOrWhiteSpace(rawSessionValue))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(rawSessionValue)?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static string SerializeCollectedCodes(IEnumerable<string> codes) =>
        JsonSerializer.Serialize(
            codes.Where(s => !string.IsNullOrWhiteSpace(s))
                 .Select(NormalizeCode)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList());

    public static Dictionary<string, RedemptionMetaPayload> BuildRedemptionMetaMap(IEnumerable<Systemlog> logs)
    {
        var map = new Dictionary<string, RedemptionMetaPayload>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            try
            {
                var meta = JsonSerializer.Deserialize<RedemptionMetaPayload>(log.DetailPayload);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.RedemptionCode) && !map.ContainsKey(meta.RedemptionCode))
                {
                    map[meta.RedemptionCode] = meta;
                }
            }
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        return map;
    }

    public static Dictionary<string, PreorderPromoPayload> BuildPreorderPromoMap(IEnumerable<Systemlog> logs)
    {
        var map = new Dictionary<string, PreorderPromoPayload>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            try
            {
                var meta = JsonSerializer.Deserialize<PreorderPromoPayload>(log.DetailPayload);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.ReservationNumber) && !map.ContainsKey(meta.ReservationNumber))
                {
                    map[meta.ReservationNumber] = meta;
                }
            }
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        return map;
    }

    public static string FormatPickupDateThai(string? isoDate)
    {
        if (!string.IsNullOrWhiteSpace(isoDate) &&
            DateOnly.TryParseExact(isoDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
        }
        return "-";
    }

    public static string NormalizePhoneDigits(string? phone) =>
        (phone ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();
}
