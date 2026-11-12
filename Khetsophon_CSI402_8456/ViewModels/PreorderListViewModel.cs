using System;
using System.Collections.Generic;
using System.Linq;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงรายการประวัติและสถานะพรีออเดอร์ของสมาชิก (Views/Home/PreorderStatus.cshtml)
// WHY: รวบรวมข้อมูลใบสั่งจองพร้อมสถานะขั้นตอนการติดตาม เพื่อให้ View นำไปแสดงผลได้ทันที
// หมายเหตุ: สืบทอดจาก List<Preorder> เพื่อรองรับทั้งการแสดงผลแบบ Strongly Typed ViewModel และการเข้ากันได้แบบ IEnumerable<Preorder>
public class PreorderListViewModel : List<Preorder>
{
    // คำค้นหาสำหรับค้นหาด้วยหมายเลขใบจองหรือชื่อสินค้า
    public string? SearchTerm { get; set; }

    // ตัวกรองสถานะพรีออเดอร์ เช่น all, active, completed, cancelled
    public string? StatusFilter { get; set; }

    // รายการใบสั่งจองพรีออเดอร์ทั้งหมดของสมาชิกแบบ ViewModel พร้อมใช้งาน
    public List<PreorderSummaryViewModel> Preorders { get; set; } = new();

    // จำนวนใบสั่งจองพรีออเดอร์ทั้งหมด
    public int TotalOrders => Preorders.Count > 0 ? Preorders.Count : this.Count;
}

// READ: ViewModel สรุปรายละเอียดของใบสั่งจองพรีออเดอร์แต่ละใบ
public class PreorderSummaryViewModel
{
    // รหัสใบสั่งจองพรีออเดอร์ในฐานข้อมูล
    public int PreorderId { get; set; }

    // หมายเลขใบสั่งจอง เช่น FH-PRE-2026-0001
    public string ReservationNumber { get; set; } = string.Empty;

    // เบอร์โทรศัพท์ติดต่อสำหรับรับ SMS
    public string ContactPhone { get; set; } = string.Empty;

    // สาขาหน้าร้านที่นัดรับสินค้า
    public string PickupBranch { get; set; } = "FriendHouse HQ สาขาสยามสแควร์";

    // รหัสสถานะปัจจุบันในฐานข้อมูล เช่น Reserved, ReadyForPickup, Completed, Cancelled
    public string CurrentStatus { get; set; } = "Reserved";

    // ข้อความป้ายกำกับสถานะภาษาไทย เช่น ยืนยันการจองแล้ว, สินค้ามาถึงแล้ว, รับของแล้ว
    public string StatusBadgeText { get; set; } = "ยืนยันการจองแล้ว";

    // คลาสสี Tailwind CSS สำหรับป้ายกำกับสถานะ
    public string StatusBadgeClass { get; set; } = "bg-blue-100 text-[#2F39A9]";

    // ข้อความคำแนะนำสำหรับลูกค้าตามสถานะปัจจุบัน
    public string NoticeText { get; set; } = "คำสั่งจองของท่านได้รับการยืนยันแล้ว รอสินค้าเข้าสู่คลังและแจ้งเตือนผ่าน SMS";

    // วันที่และเวลาที่ทำรายการสั่งจอง
    public DateTime CreatedAt { get; set; }

    // วันที่และเวลาที่สั่งจองจัดรูปแบบภาษาไทย
    public string CreatedAtFormatted { get; set; } = string.Empty;

    // วันที่นัดหมายประมาณการรับสินค้า
    public DateOnly EstimatedPickupDate { get; set; }

    // วันที่นัดหมายประมาณการจัดรูปแบบภาษาไทย
    public string EstimatedPickupDateFormatted { get; set; } = string.Empty;

    // ยอดรวมราคาสุทธิของใบสั่งจอง (บาท)
    public decimal TotalFinalPrice { get; set; }

    // ยอดรวมราคาสุทธิจัดรูปแบบเงินบาทไทย เช่น ฿4,290
    public string TotalFinalPriceFormatted => $"฿{TotalFinalPrice:N0}";

    // จำนวนชิ้นสินค้ารวมในใบสั่งจองนี้
    public int TotalQuantity => Items.Sum(i => i.Quantity);

    // รายการสินค้าแต่ละชิ้นในใบสั่งจอง
    public List<PreorderItemDetailViewModel> Items { get; set; } = new();

    // ขั้นตอนการติดตามสถานะคำสั่งจอง
    public List<PreorderTrackingStepViewModel> Steps { get; set; } = new();
}

// READ: ViewModel สำหรับรายการสินค้าในใบสั่งจอง
public class PreorderItemDetailViewModel
{
    // รหัสแถวรายการสินค้าในตาราง Preorderitems
    public int ItemId { get; set; }

    // รหัสสินค้า
    public int ProductId { get; set; }

    // ชื่อสินค้าฟิกเกอร์
    public string ProductName { get; set; } = string.Empty;

    // ที่อยู่พาธไฟล์รูปภาพสินค้า
    public string ImageUrl { get; set; } = string.Empty;

    // ราคาต่อหน่วย (บาท)
    public decimal UnitPrice { get; set; }

    // ราคาต่อหน่วยจัดรูปแบบเงินบาทไทย
    public string UnitPriceFormatted => $"฿{UnitPrice:N0}";

    // จำนวนชิ้นที่สั่งจอง
    public int Quantity { get; set; }

    // ยอดรวมของแถวสินค้า (ราคาต่อหน่วย * จำนวน)
    public decimal LineTotal => UnitPrice * Quantity;

    // ยอดรวมของแถวสินค้าจัดรูปแบบเงินบาทไทย
    public string LineTotalFormatted => $"฿{LineTotal:N0}";
}

// READ: ViewModel สำหรับแสดงขั้นตอนการติดตามสถานะ
public class PreorderTrackingStepViewModel
{
    // ลำดับขั้นตอน (1, 2, 3, 4)
    public int StepNumber { get; set; }

    // ชื่อขั้นตอน เช่น สั่งจองสำเร็จ, สินค้าเข้าร้าน, รอรับสินค้า, ส่งมอบเรียบร้อย
    public string Title { get; set; } = string.Empty;

    // คำอธิบายเพิ่มเติมของขั้นตอน
    public string Description { get; set; } = string.Empty;

    // สถานะว่าขั้นตอนนี้ผ่านไปแล้วหรือไม่
    public bool IsCompleted { get; set; }

    // สถานะว่าเป็นขั้นตอนปัจจุบันที่กำลังดำเนินการอยู่หรือไม่
    public bool IsCurrent { get; set; }
}
