using System;
using System.Collections.Generic;
using System.Globalization;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงแคตตาล็อกแลกของรางวัลด้วยแต้มสะสม (Views/Home/Rewards.cshtml)
// WHY: รวมข้อมูลแต้มสะสมของสมาชิก, รายการของรางวัลพร้อมสถานะว่าแต้มพอแลกหรือไม่ และประวัติการแลกแต้ม
public class RewardsCatalogViewModel
{
    // สถานะการเข้าสู่ระบบของผู้ใช้งาน
    public bool IsLoggedIn { get; set; }

    // เบอร์โทรศัพท์ของสมาชิกสำหรับกรอกอัตโนมัติในหน้าต่างแลกของรางวัล
    public string UserPhone { get; set; } = string.Empty;

    // วันที่ขั้นต่ำที่อนุญาตให้เลือกนัดรับของรางวัล (รูปแบบ ค.ศ. yyyy-MM-dd)
    public string MinPickupDate => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // จำนวนแต้มสะสมปัจจุบันของผู้ใช้งาน
    public int UserPoints { get; set; }

    // แต้มสะสมจัดรูปแบบตัวเลข เช่น 1,250
    public string UserPointsFormatted => $"{UserPoints:N0}";

    // รายการของรางวัลทั้งหมดที่มีในระบบ
    public List<RewardItemViewModel> Rewards { get; set; } = new();

    // รายการแท็บหมวดหมู่ของรางวัล (ภาษาไทยล้วน ไม่มีภาษาอังกฤษในวงเล็บ)
    public List<string> Categories { get; set; } = new()
    {
        "ทั้งหมด",
        "ฟิกเกอร์สะสม",
        "ของสะสมพรีเมียม",
        "แลกได้ทันที"
    };

    // หมวดหมู่ที่เลือกแสดงผลในปัจจุบัน
    public string SelectedCategory { get; set; } = "ทั้งหมด";

    // รายการประวัติการแลกของรางวัลที่ผ่านมาของสมาชิก
    public List<RewardRedemptionViewModel> UserRedemptions { get; set; } = new();
}

// READ: ViewModel สำหรับการ์ดของรางวัลแต่ละชิ้น
public class RewardItemViewModel
{
    // รหัสของรางวัลในฐานข้อมูล
    public int RewardId { get; set; }

    // รหัสโค้ดของรางวัล เช่น RWD-FIG-001
    public string RewardCode { get; set; } = string.Empty;

    // ชื่อของรางวัล
    public string Title { get; set; } = string.Empty;

    // แบรนด์หรือผู้ผลิต
    public string Brand { get; set; } = "FriendHouse Exclusive";

    // ป้ายแท็กหมวดหมู่ เช่น ฟิกเกอร์สะสม หรือ ของสะสมพรีเมียม
    public string CategoryTag { get; set; } = "พิเศษ";

    // จำนวนแต้มสะสมที่ต้องใช้ในการแลก
    public int PointsRequired { get; set; }

    // ข้อความแสดงแต้มสะสมที่ต้องใช้ เช่น 500 แต้ม
    public string PointsText => $"{PointsRequired:N0} แต้ม";

    // จำนวนของรางวัลคงเหลือในสต็อก
    public int StockQuantity { get; set; }

    // ที่อยู่พาธไฟล์รูปภาพของรางวัล
    public string ImageUrl { get; set; } = string.Empty;

    // สถานะว่าของรางวัลเปิดให้แลกและมีสต็อกเหลือหรือไม่
    public bool IsAvailable { get; set; }

    // สถานะว่าผู้ใช้งานคนปัจจุบันมีแต้มสะสมเพียงพอที่จะแลกรางวัลชิ้นนี้หรือไม่
    public bool CanAfford { get; set; }
}

// READ: ViewModel สำหรับแสดงรายการประวัติและสถานะการจองแลกของรางวัล
public class RewardRedemptionViewModel
{
    // รหัสการทำรายการแลกในตาราง Rewardredemptions
    public long RedemptionId { get; set; }

    // รหัสรับของรางวัลสำหรับนำไปแสดงที่หน้าร้าน เช่น RWD-845612
    public string RedemptionCode { get; set; } = string.Empty;

    // รหัสของรางวัล
    public int RewardId { get; set; }

    // ชื่อของรางวัลที่แลก
    public string RewardTitle { get; set; } = string.Empty;

    // แบรนด์ของรางวัล
    public string RewardBrand { get; set; } = "FriendHouse Exclusive";

    // พาธรูปภาพของรางวัล
    public string ImageUrl { get; set; } = "~/media/images/naruto_hero_fig.png";

    // จำนวนแต้มที่ใช้ไปในการแลกครั้งนี้
    public int PointsSpent { get; set; }

    // จำนวนแต้มที่ใช้จัดรูปแบบข้อความ เช่น 500 แต้ม
    public string PointsSpentFormatted => $"{PointsSpent:N0} แต้ม";

    // สถานะการรับของรางวัลในฐานข้อมูล เช่น PendingPickup, Completed, Cancelled
    public string Status { get; set; } = "PendingPickup";

    // ข้อความสถานะภาษาไทยตามสถานะปัจจุบัน
    public string StatusText => Status switch
    {
        "Completed" => "รับของรางวัลแล้ว",
        "Cancelled" => "ยกเลิก / คืนแต้มแล้ว",
        _ => "รอรับที่ร้าน"
    };

    // วันที่และเวลาที่ทำรายการแลกของรางวัล
    public DateTime RedeemedAt { get; set; }

    // วันที่และเวลาแลกจัดรูปแบบภาษาไทย
    public string RedeemedAtFormatted { get; set; } = string.Empty;

    // วันที่และเวลาที่ส่งมอบเสร็จสิ้น (ถ้ามี)
    public string? CompletedAtFormatted { get; set; }

    // วันที่ลูกค้าสะดวกมารับของรางวัลที่หน้าร้าน (จากที่ระบุตอนแลกแต้ม)
    public string? PickupDateFormatted { get; set; }

    // เบอร์โทรศัพท์สำหรับติดต่อแจ้งรับของรางวัล
    public string? ContactPhone { get; set; }

    // สถานะว่ารอรับที่หน้าร้านอยู่หรือไม่ (สามารถยกเลิกหรือดำเนินการต่อได้)
    public bool IsPendingPickup => Status == "PendingPickup";

    // สถานะว่ารับของรางวัลเรียบร้อยแล้วหรือไม่
    public bool IsCompleted => Status == "Completed";

    // สถานะว่าถูกยกเลิกแล้วหรือไม่
    public bool IsCancelled => Status == "Cancelled";
}
