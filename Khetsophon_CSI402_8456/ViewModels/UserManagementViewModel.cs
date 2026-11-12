using System;
using System.Collections.Generic;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ/CRUD: ViewModel สำหรับหน้าจัดการข้อมูลบัญชีผู้ใช้งานและรายชื่อสมาชิก (Views/Home/UserList.cshtml และ Views/ITSupport/Users.cshtml)
// WHY: รองรับการทำ User Full CRUD และการค้นหาข้อมูลสมาชิกตามบทบาท (เน้นบทบาทลูกค้า) โดยใช้ ViewModel ตามมาตรฐาน MVC
public class UserManagementViewModel : List<User>
{
    // คำค้นหาสำหรับค้นหาด้วยชื่อ อีเมล รหัสผู้ใช้ หรือเบอร์โทรศัพท์
    public string? SearchQuery { get; set; }

    // ตัวกรองบทบาทผู้ใช้งาน เช่น customer, staff, owner, itsupport, accountant หรือ all
    public string? RoleFilter { get; set; }

    // ตัวกรองสถานะบัญชี เช่น ปกติ, ถูกล็อก หรือ all
    public string? StatusFilter { get; set; }

    // อีเมลของบัญชีที่เพิ่งรีเซ็ตรหัสผ่านชั่วคราวสำเร็จ (สำหรับแสดงผลบนหน้าจอ)
    public string? TempResetEmail { get; set; }

    // รหัสผ่านชั่วคราวที่เพิ่งสร้างใหม่ (สำหรับแสดงผลบนหน้าจอ)
    public string? TempResetPassword { get; set; }

    // เวลาเข้าสู่ระบบล่าสุดของแต่ละ UserId
    public Dictionary<int, DateTime> LastLogins { get; set; } = new();

    // รายการข้อมูลผู้ใช้งานที่แปลงเป็น ViewModel พร้อมแสดงผลภาษาไทย
    public List<UserAccountItemViewModel> UserItems { get; set; } = new();

    // จำนวนผู้ใช้งานทั้งหมดที่ตรงตามเงื่อนไขการค้นหาและกรอง
    public int TotalCount { get; set; }

    // หมายเลขหน้าปัจจุบันสำหรับระบบแบ่งหน้า (เริ่มต้นที่ 1)
    public int CurrentPage { get; set; } = 1;

    // จำนวนแถวผู้ใช้งานที่แสดงต่อหน้า (มาตรฐานหน้าละ 10 รายการ)
    public int PageSize { get; set; } = 10;

    // จำนวนหน้าทั้งหมดของการแสดงผลตารางผู้ใช้งาน
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)(TotalCount > 0 ? TotalCount : UserItems.Count) / PageSize));
}

// READ: ViewModel สำหรับรายการบัญชีผู้ใช้งานแต่ละแถว
public class UserAccountItemViewModel
{
    // รหัสลำดับผู้ใช้งานในฐานข้อมูล
    public int UserId { get; set; }

    // รหัสประจำตัวสมาชิก เช่น U20261014001
    public string UserCode { get; set; } = string.Empty;

    // ชื่อ-นามสกุลของผู้ใช้งาน
    public string? FullName { get; set; }

    // ชื่อสำหรับแสดงผลภาษาไทย
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : "สมาชิกทั่วไป";

    // อีเมลของผู้ใช้งาน
    public string Email { get; set; } = string.Empty;

    // เบอร์โทรศัพท์ติดต่อ
    public string Phone { get; set; } = string.Empty;

    // รหัสบทบาทในระบบฐานข้อมูล
    public string RoleName { get; set; } = "Customer";

    // ชื่อบทบาทภาษาไทยสำหรับแสดงผลบน UI โดยไม่มีวงเล็บภาษาอังกฤษ
    public string RoleDisplayThai => (RoleName ?? "customer").ToLowerInvariant() switch
    {
        "customer" => "ลูกค้าทั่วไป",
        "staff" => "พนักงานหน้าร้าน",
        "owner" => "เจ้าของร้าน",
        "itsupport" => "ผู้ดูแลระบบไอที",
        "accountant" => "ฝ่ายบัญชีและการเงิน",
        _ => "ลูกค้าทั่วไป"
    };

    // แต้มสะสมของสมาชิก
    public int Points { get; set; }

    // สถานะการใช้งานบัญชี (true = ปกติ, false = ระงับการใช้งาน)
    public bool IsActive { get; set; } = true;

    // ข้อความสถานะบัญชีภาษาไทย
    public string StatusText => IsActive ? "ปกติ" : "ถูกล็อก";

    // วันที่และเวลาที่สมัครสมาชิก
    public DateTime CreatedAt { get; set; }

    // เวลาที่เข้าสู่ระบบล่าสุด (ถ้ามี)
    public DateTime? LastLoginAt { get; set; }
}

