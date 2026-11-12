using System;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงข้อมูลและจัดการโปรไฟล์สมาชิก (Views/Account/Profile.cshtml)
// WHY: ส่งผ่านข้อมูลสมาชิกอย่างปลอดภัย แยกความรับผิดชอบออกจาก Domain Model User โดยตรง
public class UserProfileViewModel
{
    // รหัสสมาชิกในฐานข้อมูล
    public int UserId { get; set; }

    // รหัสประจำตัวสมาชิก เช่น USR-0001
    public string UserCode { get; set; } = string.Empty;

    // ชื่อ-นามสกุลจริงของสมาชิก
    public string? FullName { get; set; }

    // ชื่อที่ใช้แสดงผลบน UI (หากไม่ได้ตั้งชื่อจะใช้อีเมลส่วนหน้าแทน)
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : (!string.IsNullOrWhiteSpace(Email) ? Email.Split('@')[0] : "สมาชิก FriendHouse");

    // ที่อยู่อีเมลของสมาชิก
    public string Email { get; set; } = string.Empty;

    // หมายเลขโทรศัพท์ติดต่อ
    public string Phone { get; set; } = string.Empty;

    // ที่อยู่พาธรูปภาพโปรไฟล์
    public string? AvatarUrl { get; set; }

    // ที่อยู่รูปโปรไฟล์ที่พร้อมใช้งาน (หากไม่มีจะใช้โลโก้ร้านเป็นค่าเริ่มต้น)
    public string EffectiveAvatarUrl => !string.IsNullOrEmpty(AvatarUrl) ? AvatarUrl : "~/logo/logo.png";

    // แต้มสะสมปัจจุบันของสมาชิก
    public int Points { get; set; }

    // ข้อความแสดงแต้มสะสมจัดรูปแบบเงิน/แต้มไทย เช่น 1,250 แต้ม
    public string PointsFormatted => $"{Points:N0} แต้ม";

    // จำนวนคำสั่งจองพรีออเดอร์ที่ยังอยู่ในสถานะรอดำเนินการ
    public int PendingPreorderCount { get; set; }

    // ข้อความแจ้งเตือนจำนวนพรีออเดอร์ที่รอดำเนินการ
    public string PendingOrderNotice => $"คุณมีพรีออเดอร์ที่รอดำเนินการอยู่ {PendingPreorderCount} รายการ";

    // บทบาทการทำงานในระบบ เช่น customer
    public string RoleName { get; set; } = "customer";
}
