// นำเข้าไลบรารี ASP.NET Core MVC สำหรับสร้าง ViewComponent
using Microsoft.AspNetCore.Mvc;

// กำหนดเนมสเปซสำหรับส่วนประกอบการแสดงผลของโปรเจกต์
namespace Khetsophon_CSI402_8456.Controllers;

// คอมโพเนนต์สำหรับแสดงส่วนท้ายของหน้าเว็บ (Footer)
public class FooterViewComponent : ViewComponent
{
    // เมธอดหลักสำหรับเรนเดอร์ View ส่วนท้ายของเว็บไซต์ (Views/Shared/Components/Footer/Default.cshtml)
    public IViewComponentResult Invoke()
    {
        // ส่งคืนผลลัพธ์หน้า View เริ่มต้นของ Footer
        return View();
    }
}