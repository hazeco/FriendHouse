using System.ComponentModel.DataAnnotations;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับรับข้อมูลจากแบบฟอร์มเข้าสู่ระบบของสมาชิกและเจ้าหน้าที่
// WHY: ตรวจสอบความครบถ้วนของอีเมลและรหัสผ่านก่อนนำไปตรวจสอบกับฐานข้อมูล
public class LoginViewModel
{
    // อีเมลของผู้ใช้งานที่ต้องการเข้าสู่ระบบ
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "กรุณากรอกอีเมลให้ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    // เบอร์โทรศัพท์ของผู้ใช้งาน (ตัวเลือกเสริม)
    public string? Phone { get; set; }

    // รหัสผ่านสำหรับยืนยันตัวตน
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
