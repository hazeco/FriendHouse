using System.ComponentModel.DataAnnotations;

namespace Khetsophon_CSI402_8456.ViewModels;

// CREATE: ViewModel สำหรับรับข้อมูลจากแบบฟอร์มสมัครสมาชิกใหม่ของลูกค้า
// WHY: ตรวจสอบความถูกต้องของอีเมล เบอร์โทรศัพท์ และรหัสผ่านก่อนบันทึกลงฐานข้อมูล
public class RegisterViewModel
{
    // ชื่อ-นามสกุลของสมาชิกใหม่ (ไม่บังคับ หากไม่กรอกจะตั้งค่าเริ่มต้นเป็นสมาชิกใหม่)
    public string? FullName { get; set; }

    // อีเมลสำหรับใช้เข้าสู่ระบบ
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "กรุณากรอกอีเมลให้ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    // เบอร์โทรศัพท์สำหรับติดต่อและแจ้งเตือนรับสินค้าพรีออเดอร์
    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
    [Phone(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง")]
    public string Phone { get; set; } = string.Empty;

    // รหัสผ่านสำหรับเข้าใช้งานบัญชี
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // ยืนยันรหัสผ่านอีกครั้งเพื่อป้องกันการพิมพ์ผิดพลาด
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านอีกครั้ง")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
