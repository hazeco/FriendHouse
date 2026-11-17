using Khetsophon_CSI402_8456.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงรายการสินค้าในตะกร้าสั่งจอง (Views/Home/Cart.cshtml)
// WHY: คำนวณยอดเงินรวม สรุปจำนวนชิ้น และเตรียมข้อมูลเบอร์โทรศัพท์สำหรับแบบฟอร์มยืนยันพรีออเดอร์
public class CartViewModel
{
    // รายการสินค้าแต่ละแถวในตะกร้าของสมาชิก
    public List<CartItemViewModel> Items { get; set; } = new();

    // จำนวนชิ้นสินค้ารวมทั้งหมดในตะกร้า
    public int TotalItemCount => Items.Sum(i => i.Quantity);

    // ยอดรวมราคาสินค้าทั้งหมดก่อนหักส่วนลดหรือค่าธรรมเนียม
    public decimal SubtotalAmount => Items.Sum(i => i.LineTotal);

    // ยอดรวมราคาสินค้าจัดรูปแบบเงินบาทไทย เช่น ฿8,580
    public string SubtotalFormatted => $"฿{SubtotalAmount:N0}";

    // ข้อมูลโค้ดโปรโมชั่นที่ใช้งานอยู่กับรายการสินค้าในตะกร้า
    public string? AppliedPromoCode { get; set; }

    // ชื่อแคมเปญโปรโมชั่นที่กำลังใช้งาน
    public string? AppliedPromoName { get; set; }

    // ข้อความสรุปสิทธิประโยชน์ของโปรโมชั่นที่ใช้งาน
    public string? PromoBenefitSummary { get; set; }

    // ข้อความแจ้งเตือนข้อผิดพลาดในการตรวจสอบโปรโมชั่น
    public string? PromoValidationError { get; set; }

    // ยอดเงินส่วนลดที่คำนวณได้จากโปรโมชั่น
    public decimal DiscountAmount { get; set; }

    // ยอดเงินส่วนลดจัดรูปแบบเงินบาทไทย
    public string DiscountFormatted => $"-฿{DiscountAmount:N0}";

    // สถานะระบุว่าโปรโมชั่นให้คะแนนสะสมพิเศษสองเท่าหรือไม่
    public bool IsDoublePoints { get; set; }

    // สถานะว่ากำลังใช้โปรโมชั่นซื้อ 1 แถม 1 ในตะกร้าหรือไม่
    public bool IsBogoApplied { get; set; }

    // โหมดของแถมสำหรับโปรโมชั่นซื้อ 1 แถม 1 (same = แถมตัวมันเอง, item = แถมสินค้าที่ระบุ, pick = ลูกค้าเลือกได้เอง)
    public string BogoGiftMode { get; set; } = string.Empty;

    // รหัสสินค้าของแถมที่ได้รับหรือเลือกไว้ (0 หมายถึงแถมตัวมันเองรุ่นเดียวกับที่ซื้อ)
    public int? SelectedFreeGiftProductId { get; set; }

    // ชื่อสินค้าของแถมที่ได้รับจากโปรโมชั่นซื้อ 1 แถม 1
    public string? SelectedFreeGiftProductName { get; set; }

    // รูปภาพของสินค้าของแถม
    public string? SelectedFreeGiftImageUrl { get; set; }

    // มูลค่าปกติของสินค้าของแถมที่ได้รับฟรี
    public decimal SelectedFreeGiftPrice { get; set; }

    // รายการตัวเลือกสินค้าของแถมสำหรับโหมดที่ให้ลูกค้าเลือกของแถมได้เองในตะกร้า
    public List<BogoGiftOptionViewModel> AvailableBogoGifts { get; set; } = new();

    // รายการคูปองโปรโมชั่นทั้งหมดที่สมาชิกเก็บสะสมไว้
    public List<CollectedPromoOptionViewModel> CollectedPromos { get; set; } = new();

    // ค่าธรรมเนียมการให้บริการระบบ (ร้าน FriendHouse ให้บริการสั่งจองฟรี)
    public decimal PlatformFee => 0m;

    // ข้อความแสดงค่าธรรมเนียมระบบ
    public string PlatformFeeFormatted => "ฟรี (฿0)";

    // ยอดรวมสุทธิที่ต้องชำระที่หน้าร้านเมื่อมารับสินค้า (หักส่วนลดโปรโมชั่นแล้ว)
    public decimal GrandTotalAmount => Math.Max(0m, SubtotalAmount - DiscountAmount) + PlatformFee;

    // ยอดรวมสุทธิจัดรูปแบบเงินบาทไทย
    public string GrandTotalFormatted => $"฿{GrandTotalAmount:N0}";

    // เบอร์โทรศัพท์ของผู้ใช้งานที่เข้าสู่ระบบ สำหรับใส่เป็นค่าเริ่มต้นในฟอร์มยืนยัน
    public string? UserPhone { get; set; }

    // สาขาหน้าร้านที่กำหนดให้มารับสินค้า
    public string PickupBranch { get; set; } = "FriendHouse HQ สาขาสยามสแควร์";

    // วันที่ขั้นต่ำที่อนุญาตให้เลือกนัดรับสินค้า (วันปัจจุบันในรูปแบบ ISO yyyy-MM-dd แบบ ค.ศ. เสมอ)
    public string MinPickupDate => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // หัวข้อคำชี้แจงการรับสินค้าหน้าร้าน
    public string StoreNoticeTitle { get; set; } = "รับและชำระเงินที่หน้าร้านเท่านั้น";

    // รายละเอียดคำชี้แจงการรับสินค้าหน้าร้าน
    public string StoreNoticeDesc { get; set; } = "การสั่งจองพรีออเดอร์ไม่ต้องชำระเงินออนไลน์ ชำระยอดเต็มหรือมัดจำเมื่อมารับสินค้าที่สาขา FriendHouse HQ";
}

// READ: ViewModel สำหรับแสดงรายการสินค้าแต่ละชิ้นในตะกร้า
public class CartItemViewModel
{
    // รหัสรายการตะกร้าสินค้าในฐานข้อมูล
    public int CartItemId { get; set; }

    // รหัสสินค้า
    public int ProductId { get; set; }

    // ชื่อสินค้าฟิกเกอร์
    public string ProductName { get; set; } = string.Empty;

    // ที่อยู่พาธไฟล์รูปภาพสินค้า
    public string ImageUrl { get; set; } = string.Empty;

    // ราคาต่อหน่วย (บาท)
    public decimal UnitPrice { get; set; }

    // ราคาต่อหน่วยจัดรูปแบบเงินบาทไทย เช่น ฿4,290
    public string UnitPriceFormatted => $"฿{UnitPrice:N0}";

    // จำนวนชิ้นที่สั่ง
    public int Quantity { get; set; }

    // ยอดเงินรวมของแถวสินค้านี้ (ราคาต่อหน่วย * จำนวน)
    public decimal LineTotal => UnitPrice * Quantity;

    // ยอดเงินรวมของแถวสินค้าจัดรูปแบบเงินบาทไทย
    public string LineTotalFormatted => $"฿{LineTotal:N0}";

    // จำนวนสินค้าคงเหลือในคลังที่พร้อมจอง
    public int AvailableStock { get; set; }

    // ข้อความแจ้งกำหนดการเข้าร้าน
    public string ScheduleNotice { get; set; } = "กำหนดเข้าร้าน: สินค้าพร้อมส่งมอบตามรอบพรีออเดอร์";
}

// READ: ViewModel แสดงตัวเลือกคูปองที่ลูกค้าเก็บไว้จากหน้าโปรโมชั่นสำหรับกดใช้ในตะกร้า
public class CollectedPromoOptionViewModel
{
    // รหัสคูปองส่วนลดสำหรับอ้างอิง
    public string VoucherCode { get; set; } = string.Empty;
    // ชื่อหัวข้อโปรโมชั่น
    public string Title { get; set; } = string.Empty;
    // ป้ายข้อความระบุสิทธิประโยชน์ของคูปอง
    public string BenefitLabel { get; set; } = string.Empty;
    // สถานะระบุว่าคูปองนี้ถูกเลือกใช้งานอยู่หรือไม่
    public bool IsApplied { get; set; }
}

// โครงสร้างผลลัพธ์จากการตรวจสอบและคำนวณโปรโมชั่นกับรายการสินค้าในตะกร้า
public class PromoEvaluationResult
{
    // พร็อพเพอร์ตี้ IsValid สำหรับจัดเก็บสถานะความถูกต้องของโปรโมชั่น
    public bool IsValid { get; set; }
    // พร็อพเพอร์ตี้ ErrorMessage สำหรับจัดเก็บข้อความแจ้งข้อผิดพลาด
    public string? ErrorMessage { get; set; }
    // พร็อพเพอร์ตี้ Promotion สำหรับจัดเก็บเอนทิตีโปรโมชั่นที่ผ่านการตรวจสอบ
    public Promotion? Promotion { get; set; }
    // รหัสคูปองส่วนลดที่ได้รับจากโปรโมชั่น
    public string VoucherCode => Promotion?.VoucherCode ?? string.Empty;
    // ชื่อโปรโมชั่นที่ผ่านการประเมิน
    public string PromoName => Promotion?.Name ?? string.Empty;
    // ประเภทโปรโมชั่นในรูปแบบตัวพิมพ์เล็ก
    public string PromoType => (Promotion?.PromoType ?? string.Empty).ToLowerInvariant();
    // พร็อพเพอร์ตี้ SubtotalAmount สำหรับจัดเก็บยอดรวมราคาสินค้าในตะกร้า
    public decimal SubtotalAmount { get; set; }
    // พร็อพเพอร์ตี้ EligibleSubtotal สำหรับจัดเก็บยอดราคาสินค้าที่เข้าเกณฑ์โปรโมชั่น
    public decimal EligibleSubtotal { get; set; }
    // พร็อพเพอร์ตี้ DiscountAmount สำหรับจัดเก็บยอดเงินส่วนลดสุทธิ
    public decimal DiscountAmount { get; set; }
    // พร็อพเพอร์ตี้ IsDoublePoints สำหรับจัดเก็บสถานะแต้มสะสมพิเศษสองเท่า
    public bool IsDoublePoints { get; set; }
    // พร็อพเพอร์ตี้ BenefitSummary สำหรับจัดเก็บข้อความสรุปสิทธิประโยชน์
    public string BenefitSummary { get; set; } = string.Empty;
    // โหมดของแถมสำหรับโปรโมชั่นซื้อ 1 แถม 1 (same, item, pick)
    public string BogoGiftMode { get; set; } = string.Empty;
    // รหัสสินค้าของแถมที่ได้รับฟรี (0 = แถมตัวมันเองรุ่นเดียวกัน)
    public int? FreeGiftProductId { get; set; }
    // ชื่อสินค้าของแถมที่ได้รับฟรี
    public string? FreeGiftProductName { get; set; }
    // พาธรูปภาพสินค้าของแถม
    public string? FreeGiftImageUrl { get; set; }
    // มูลค่าปกติของสินค้าของแถม
    public decimal FreeGiftPrice { get; set; }
}

// โครงสร้างตัวเลือกสินค้าของแถมสำหรับโปรโมชั่นซื้อ 1 แถม 1 ในหน้าตะกร้าสินค้า
public class BogoGiftOptionViewModel
{
    // รหัสสินค้าของแถม (0 = แถมตัวมันเองรุ่นเดียวกับสินค้าที่ซื้อ)
    public int ProductId { get; set; }
    // ชื่อแสดงผลของสินค้าของแถม
    public string Name { get; set; } = string.Empty;
    // พาธรูปภาพสินค้าของแถม
    public string ImageUrl { get; set; } = string.Empty;
    // ราคาขายปกติของสินค้าของแถม
    public decimal RetailPrice { get; set; }
    // สถานะว่าตัวเลือกนี้กำลังถูกเลือกเป็นของแถมอยู่หรือไม่
    public bool IsSelected { get; set; }
}

// ข้อมูลวันนัดรับและเบอร์ติดต่อของการจองแลกของรางวัล จัดเก็บผ่าน DetailPayload ในบันทึกกิจกรรม
public class RedemptionMetaPayload
{
    // พร็อพเพอร์ตี้ RedemptionCode สำหรับจัดเก็บรหัสการแลกของรางวัล
    public string RedemptionCode { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PickupDate สำหรับจัดเก็บวันนัดรับของรางวัล
    public string PickupDate { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ ContactPhone สำหรับจัดเก็บเบอร์ติดต่อผู้แลกของรางวัล
    public string ContactPhone { get; set; } = string.Empty;
}

// ข้อมูลรหัสโปรโมชั่นและส่วนลดของใบสั่งจองพรีออเดอร์ จัดเก็บผ่าน DetailPayload ในบันทึกกิจกรรม
public class PreorderPromoPayload
{
    // พร็อพเพอร์ตี้ ReservationNumber สำหรับจัดเก็บหมายเลขใบสั่งจองพรีออเดอร์
    public string ReservationNumber { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ VoucherCode สำหรับจัดเก็บรหัสคูปองโปรโมชั่น
    public string VoucherCode { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PromoName สำหรับจัดเก็บชื่อโปรโมชั่น
    public string PromoName { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ PromoType สำหรับจัดเก็บประเภทโปรโมชั่น
    public string PromoType { get; set; } = string.Empty;
    // พร็อพเพอร์ตี้ OriginalSubtotal สำหรับจัดเก็บยอดรวมเดิมก่อนหักส่วนลด
    public decimal OriginalSubtotal { get; set; }
    // พร็อพเพอร์ตี้ DiscountAmount สำหรับจัดเก็บยอดส่วนลดที่ได้รับ
    public decimal DiscountAmount { get; set; }
    // พร็อพเพอร์ตี้ IsDoublePoints สำหรับจัดเก็บสถานะแต้มสะสมพิเศษสองเท่า
    public bool IsDoublePoints { get; set; }
    // รหัสสินค้าของแถมจากโปรโมชั่นซื้อ 1 แถม 1 (ถ้ามี)
    public int? FreeGiftProductId { get; set; }
    // ชื่อสินค้าของแถมจากโปรโมชั่นซื้อ 1 แถม 1 (สำหรับแสดงในใบจองและหน้าเคาน์เตอร์พนักงาน)
    public string FreeGiftProductName { get; set; } = string.Empty;
    // รูปภาพของสินค้าของแถม
    public string FreeGiftImageUrl { get; set; } = string.Empty;
    // มูลค่าปกติของสินค้าของแถมที่ได้รับฟรี
    public decimal FreeGiftValue { get; set; }
}
