using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Helpers;

/// <summary>
/// ผลลัพธ์จากการตรวจสอบและคำนวณโปรโมชั่นกับรายการสินค้าในตะกร้า
/// </summary>
public class PromoEvaluationResult
{
    // พร็อพเพอร์ตี้ IsValid สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public bool IsValid { get; set; }
    // พร็อพเพอร์ตี้ ErrorMessage สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string? ErrorMessage { get; set; }
    // พร็อพเพอร์ตี้ Promotion สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public Promotion? Promotion { get; set; }
    public string VoucherCode => Promotion?.VoucherCode ?? string.Empty;
    public string PromoName => Promotion?.Name ?? string.Empty;
    public string PromoType => (Promotion?.PromoType ?? string.Empty).ToLowerInvariant();
    // พร็อพเพอร์ตี้ SubtotalAmount สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public decimal SubtotalAmount { get; set; }
    // พร็อพเพอร์ตี้ EligibleSubtotal สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public decimal EligibleSubtotal { get; set; }
    // พร็อพเพอร์ตี้ DiscountAmount สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public decimal DiscountAmount { get; set; }
    // พร็อพเพอร์ตี้ IsDoublePoints สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public bool IsDoublePoints { get; set; }
    // พร็อพเพอร์ตี้ BenefitSummary สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string BenefitSummary { get; set; } = string.Empty;
}

/// <summary>
/// ข้อมูลวันนัดรับและเบอร์ติดต่อของการจองแลกของรางวัล (จัดเก็บผ่าน Systemlogs.DetailPayload)
/// </summary>
public class RedemptionMetaPayload
{
    // พร็อพเพอร์ตี้ RedemptionCode สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string RedemptionCode { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PickupDate สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string PickupDate { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ ContactPhone สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string ContactPhone { get; set; } = string.Empty;
}

/// <summary>
/// ข้อมูลรหัสโปรโมชั่นและส่วนลดของใบสั่งจองพรีออเดอร์ (จัดเก็บผ่าน Systemlogs.DetailPayload)
/// </summary>
public class PreorderPromoPayload
{
    // พร็อพเพอร์ตี้ ReservationNumber สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string ReservationNumber { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ VoucherCode สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string VoucherCode { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PromoName สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string PromoName { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PromoType สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public string PromoType { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ OriginalSubtotal สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public decimal OriginalSubtotal { get; set; }
    // พร็อพเพอร์ตี้ DiscountAmount สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public decimal DiscountAmount { get; set; }
    // พร็อพเพอร์ตี้ IsDoublePoints สำหรับจัดเก็บและส่งต่อข้อมูลไปยังหน้า View
    public bool IsDoublePoints { get; set; }
}

/// <summary>
/// ศูนย์รวมตรรกะการตรวจสอบขอบเขต/เงื่อนไขโปรโมชั่น การคำนวณส่วนลด และข้อมูลเสริมของการแลกของรางวัล
/// </summary>
public static class PromoAndRedemptionHelper
{
    public const string CollectedPromoSessionKey = "CollectedPromoCodes";
    public const string AppliedPromoSessionKey = "AppliedPromoCode";

    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    public static DateOnly TodayGregorian() =>
        new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

    // ฟังก์ชัน GenerateLogCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string GenerateLogCode(string prefix)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix)
            ? "LOG"
            : prefix.Trim().TrimEnd('-').ToUpperInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (cleanPrefix.Length > 11) cleanPrefix = cleanPrefix[..11];
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string code = $"{cleanPrefix}-{DateTime.Now:yyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return code.Length <= 30 ? code : code[..30];
    }

    // ฟังก์ชัน IsValidPhoneNumber สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static bool IsValidPhoneNumber(string? phone)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string digits = NormalizePhoneDigits(phone);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return digits.Length >= 9 && digits.Length <= 15;
    }

    // ฟังก์ชัน NormalizeGregorianDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static DateOnly NormalizeGregorianDate(DateOnly date)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int year = date.Year;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (year > 0 && year < 1900)
        {
            year += 543;
        }
        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
        else if (year >= 2400)
        {
            year -= 543;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new DateOnly(year, date.Month, day);
    }

    // ฟังก์ชัน FormatThaiDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string FormatThaiDate(DateOnly date, string format = "d MMM yyyy")
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var gregorian = NormalizeGregorianDate(date);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return gregorian.ToDateTime(TimeOnly.MinValue).ToString(format, ThaiCulture);
    }

    public static string FormatIsoDate(DateOnly date) =>
        NormalizeGregorianDate(date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ฟังก์ชัน FormatPromoBenefitSummary สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string FormatPromoBenefitSummary(Promotion promo)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal val = promo.DiscountValue ?? 0m;
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return (promo.PromoType ?? "percent").Trim().ToLowerInvariant() switch
        {
            "percent" or "percentage" => $"ลด {val:0.#}%",
            "bogo" => val > 0 ? $"ซื้อ 1 แถม 1 (สูงสุด ฿{val:N0})" : "ซื้อ 1 แถม 1",
            "double_points" => "รับแต้มสะสม x2",
            "bundle" => $"ชุดเซ็ตสุดคุ้ม ลด ฿{val:N0}",
            _ => $"ลด ฿{val:N0}"
        };
    }

    // ฟังก์ชัน SaveUploadedImage สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string? SaveUploadedImage(IFormFile? imageFile, string webRootPath, string defaultPrefix, string? fallbackUrl)
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
            ext = ".png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var imagesDir = Path.Combine(webRootPath, "media", "images");
        Directory.CreateDirectory(imagesDir);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var safePrefix = string.IsNullOrWhiteSpace(rawName) ? defaultPrefix : rawName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fullPath = Path.Combine(imagesDir, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            imageFile.CopyTo(stream);
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "/media/images/" + fileName;
    }

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

    // ฟังก์ชัน MatchesScope สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static bool MatchesScope(Product? product, string? scope)
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
                || slug.Contains("figure")
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

    public static PromoEvaluationResult EvaluateCartPromotion(
        Promotion? promo,
        IReadOnlyCollection<Cartitem> cartItems,
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
                IsValid = false,
                SubtotalAmount = subtotal,
                ErrorMessage = "ไม่พบรหัสโปรโมชั่นนี้ในระบบ กรุณาตรวจสอบตัวสะกดอีกครั้ง"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.IsActive != true)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
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
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัส \"{promo.VoucherCode}\" จะเริ่มใช้งานได้ตั้งแต่วันที่ {FormatThaiDate(normalizedStart)}"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today > normalizedEnd)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
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
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
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
                IsValid = false,
                Promotion = promo,
                SubtotalAmount = subtotal,
                EligibleSubtotal = eligibleSubtotal,
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
                discount = Math.Round(eligibleSubtotal * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
                discount = Math.Clamp(discount, 0m, eligibleSubtotal);
                summary = $"ลด {discountValue:0.#}% (ประหยัด ฿{discount:N0})";
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bogo":
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (eligibleQty < 2)
                {
                    // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                    return new PromoEvaluationResult
                    {
                        IsValid = false,
                        Promotion = promo,
                        SubtotalAmount = subtotal,
                        EligibleSubtotal = eligibleSubtotal,
                        ErrorMessage = $"โค้ดซื้อ 1 แถม 1 ({promo.VoucherCode}) ต้องมีสินค้าที่ร่วมรายการอย่างน้อย 2 ชิ้นในตะกร้า"
                    };
                }
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal cheapestUnit = eligibleItems.Min(c => c.Product?.RetailPrice ?? 0m);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal bogoCap = discountValue > 0m ? discountValue : cheapestUnit;
                discount = Math.Clamp(bogoCap, 0m, eligibleSubtotal);
                summary = $"สิทธิพิเศษซื้อ 1 แถม 1 (ส่วนลด ฿{discount:N0})";
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "double_points":
                isDoublePoints = true;
                summary = "รับแต้มสะสม x2 เมื่อชำระเงินที่หน้าร้าน";
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "fixed":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bundle":
            default:
                discount = Math.Clamp(discountValue, 0m, eligibleSubtotal);
                summary = $"ส่วนลดท้ายบิล ฿{discount:N0}";
                break;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
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

    // ฟังก์ชัน ReadCollectedCodes สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static List<string> ReadCollectedCodes(string? rawSessionValue)
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
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(NormalizeCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }
    }

    public static string SerializeCollectedCodes(IEnumerable<string> codes) =>
        JsonSerializer.Serialize(
            codes.Where(s => !string.IsNullOrWhiteSpace(s))
                 .Select(NormalizeCode)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList());

    // ฟังก์ชัน BuildRedemptionMetaMap สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static Dictionary<string, RedemptionMetaPayload> BuildRedemptionMetaMap(IEnumerable<Systemlog> logs)
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
    public static Dictionary<string, PreorderPromoPayload> BuildPreorderPromoMap(IEnumerable<Systemlog> logs)
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
    public static string FormatPickupDateThai(string? isoDate)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(isoDate) &&
            DateOnly.TryParseExact(isoDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return parsed.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "-";
    }

    public static string NormalizePhoneDigits(string? phone) =>
        (phone ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();

    private static string? _webRootPath;

    // ฟังก์ชัน ConfigureWebRoot สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static void ConfigureWebRoot(string? webRootPath)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            _webRootPath = webRootPath;
        }
    }

    // ฟังก์ชัน ResolveCardImageUrl สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string ResolveCardImageUrl(string? imageUrl)
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
            ? _webRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string physicalPath = Path.Combine(rootDir, "media", "images", cardFileName);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (File.Exists(physicalPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed.StartsWith("~/") ? "~" + cardRelative : cardRelative;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return trimmed;
    }

    // ฟังก์ชัน EscapeCsvField สำหรับประมวลผลตรรกะและการทำงานของระบบ
    public static string EscapeCsvField(string? value)
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
}
