using System;
using System.Collections.Generic;
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

    // ค่าธรรมเนียมการให้บริการระบบ (ร้าน FriendHouse ให้บริการสั่งจองฟรี)
    public decimal PlatformFee => 0m;

    // ข้อความแสดงค่าธรรมเนียมระบบ
    public string PlatformFeeFormatted => "ฟรี (฿0)";

    // ยอดรวมสุทธิที่ต้องชำระที่หน้าร้านเมื่อมารับสินค้า
    public decimal GrandTotalAmount => SubtotalAmount + PlatformFee;

    // ยอดรวมสุทธิจัดรูปแบบเงินบาทไทย
    public string GrandTotalFormatted => $"฿{GrandTotalAmount:N0}";

    // เบอร์โทรศัพท์ของผู้ใช้งานที่เข้าสู่ระบบ สำหรับใส่เป็นค่าเริ่มต้นในฟอร์มยืนยัน
    public string? UserPhone { get; set; }

    // สาขาหน้าร้านที่กำหนดให้มารับสินค้า
    public string PickupBranch { get; set; } = "FriendHouse HQ สาขาสยามสแควร์";

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
