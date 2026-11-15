// นำเข้าไลบรารี Entity Framework Core สำหรับจัดการฐานข้อมูล MySQL
using Microsoft.EntityFrameworkCore;
// นำเข้าเนมสเปซโมเดลตารางฐานข้อมูลของโปรเจกต์
using Khetsophon_CSI402_8456.Models;
// นำเข้าตัวเข้ารหัสข้อความสำหรับจัดการอักขระบนหน้าเว็บ
using System.Text.Encodings.Web;
// นำเข้าช่วงรหัสอักขระ Unicode สำหรับรองรับภาษาไทย
using System.Text.Unicode;

// สร้างอินสแตนซ์ตัวสร้างเว็บแอปพลิเคชันจากพารามิเตอร์เริ่มต้น
var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียนบริการ Controllers พร้อมระบบแสดงผล Views ตามสถาปัตยกรรม MVC
builder.Services.AddControllersWithViews();
// กำหนดการเข้ารหัส HTML ให้รองรับภาษาไทยและอักขระสากลโดยไม่แปลงเป็นรหัส Escape
builder.Services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin, UnicodeRanges.Thai, UnicodeRanges.CjkUnifiedIdeographs }));
// ลงทะเบียนหน่วยความจำแคชในเครื่องสำหรับจัดเก็บข้อมูลเซสชันของผู้ใช้
builder.Services.AddDistributedMemoryCache();
// ตั้งค่าระบบ Session สำหรับจดจำสถานะการเข้าสู่ระบบของแต่ละบทบาท
builder.Services.AddSession(options =>
{
    // กำหนดเวลาหมดอายุของเซสชันเมื่อไม่มีการใช้งานนาน 30 นาที
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    // ป้องกันสคริปต์ฝั่งไคลเอนต์เข้าถึงคุกกี้เซสชันเพื่อความปลอดภัย
    options.Cookie.HttpOnly = true;
    // กำหนดให้คุกกี้เซสชันทำงานเสมอแม้ตั้งค่าความเป็นส่วนตัวเข้มงวด
    options.Cookie.IsEssential = true;
});
// ลงทะเบียนบริบทฐานข้อมูล Csi402dbContext เชื่อมต่อกับฐานข้อมูล MySQL
builder.Services.AddDbContext<Csi402dbContext>(
    options => options.UseMySql(
        // อ่านค่า Connection String จากไฟล์ appsettings.json
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // ตรวจสอบเวอร์ชันของเซิร์ฟเวอร์ MySQL โดยอัตโนมัติ
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// สร้างแอปพลิเคชันจากบริการทั้งหมดที่ลงทะเบียนไว้
var app = builder.Build();

// ตรวจสอบสภาพแวดล้อมการทำงานเพื่อกำหนดการจัดการข้อผิดพลาดบน Production
if (!app.Environment.IsDevelopment())
{
    // เปลี่ยนเส้นทางไปยังหน้าแสดงข้อผิดพลาดมาตรฐานเมื่อเกิด Exception
    app.UseExceptionHandler("/Home/Error");
    // เปิดใช้งานมาตรการความปลอดภัย HSTS บังคับใช้ HTTPS
    app.UseHsts();
}

// บังคับเปลี่ยนเส้นทางจาก HTTP เป็น HTTPS
app.UseHttpsRedirection();
// เปิดใช้งานระบบจัดการ Session ในไปป์ไลน์ของคำขอ
app.UseSession();
// เปิดใช้งานระบบจับคู่เส้นทาง URL (Routing)
app.UseRouting();

// เปิดใช้งานระบบตรวจสอบสิทธิ์การเข้าถึง (Authorization)
app.UseAuthorization();

// เปิดใช้งานการส่งมอบไฟล์สื่อคงที่ (Static Assets เช่น รูปภาพ, โมเดล 3D, CSS, JS)
app.MapStaticAssets();

// กำหนดรูปแบบเส้นทางเริ่มต้นของ MVC ชี้ไปยัง HomeController และ Action Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// เตรียมข้อมูลเริ่มต้นในฐานข้อมูล (สินค้าฟิกเกอร์, โมเดล 3D, ผู้ใช้ 5 บทบาท และหมวดหมู่)
// WHY: ดำเนินการเตรียมข้อมูลที่จำเป็น (รวมถึงโมเดล 3D) ลง MySQL ทันทีที่สตาร์ตโปรเจกต์
using (var scope = app.Services.CreateScope())
{
    // ดึงตัวจัดการบริการภายในขอบเขตการทำงานชั่วคราว
    var services = scope.ServiceProvider;
    // ดึงอินสแตนซ์ฐานข้อมูล Csi402dbContext
    var context = services.GetRequiredService<Csi402dbContext>();
    // เรียกเมธอด Initialize เพื่อซิงก์และเพิ่มข้อมูลเริ่มต้น
    Khetsophon_CSI402_8456.Data.DbInitializer.Initialize(context);
}

// เริ่มต้นรันเว็บเซิร์ฟเวอร์เพื่อรอรับคำขอจากผู้ใช้งาน
app.Run();
