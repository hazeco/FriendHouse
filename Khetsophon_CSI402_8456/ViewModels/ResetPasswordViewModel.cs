using System.ComponentModel.DataAnnotations;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแบบฟอร์มเปลี่ยนรหัสผ่านของสมาชิก (Views/Account/ResetPassword.cshtml)
// WHY: ใช้ DataAnnotations ในการตรวจสอบความถูกต้องของรหัสผ่านใหม่และการยืนยันรหัสผ่านตามแบบแผน MVC
public class ResetPasswordViewModel
{
    // รหัสผ่านเดิมปัจจุบันที่ใช้งานอยู่
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; } = string.Empty;

    // รหัสผ่านใหม่ที่ต้องการตั้ง
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(6, ErrorMessage = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านใหม่")]
    public string NewPassword { get; set; } = string.Empty;

    // การยืนยันรหัสผ่านใหม่ ต้องตรงกับรหัสผ่านใหม่
    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
    [Compare("NewPassword", ErrorMessage = "รหัสผ่านใหม่และการยืนยันรหัสผ่านไม่ตรงกัน")]
    [DataType(DataType.Password)]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
