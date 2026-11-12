using System;
using System.Collections.Generic;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงแคตตาล็อกแลกของรางวัลด้วยแต้มสะสม (Views/Home/Rewards.cshtml)
// WHY: รวมข้อมูลแต้มสะสมของสมาชิก, รายการของรางวัลพร้อมสถานะว่าแต้มพอแลกหรือไม่ และประวัติการแลกแต้ม
public class RewardsCatalogViewModel
{
    // สถานะการเข้าสู่ระบบของผู้ใช้งาน
    public bool IsLoggedIn { get; set; }

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

// READ: ViewModel สำหรับแสดงรายการประวัติการแลกของรางวัล
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

    // จำนวนแต้มที่ใช้ไปในการแลกครั้งนี้
    public int PointsSpent { get; set; }

    // จำนวนแต้มที่ใช้จัดรูปแบบข้อความ เช่น 500 แต้ม
    public string PointsSpentFormatted => $"{PointsSpent:N0} แต้ม";

    // สถานะการรับของรางวัลในฐานข้อมูล เช่น PendingPickup หรือ Completed
    public string Status { get; set; } = "PendingPickup";

    // ข้อความสถานะภาษาไทย เช่น รอรับที่ร้าน หรือ รับของแล้ว
    public string StatusText => Status == "Completed" ? "รับของแล้ว" : "รอรับที่ร้าน";

    // วันที่และเวลาที่ทำรายการแลกของรางวัล
    public DateTime RedeemedAt { get; set; }

    // วันที่และเวลาแลกจัดรูปแบบภาษาไทย
    public string RedeemedAtFormatted { get; set; } = string.Empty;

    // สถานะว่ารับของรางวัลเรียบร้อยแล้วหรือไม่
    public bool IsCompleted => Status == "Completed";
}
