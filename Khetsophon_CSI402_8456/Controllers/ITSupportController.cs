using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับฝ่ายผู้ดูแลระบบไอที
public class ITSupportController : Controller
{
    // ประกาศตัวแปรเก็บบริบทฐานข้อมูลสำหรับเชื่อมต่อตารางใน MySQL
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับบริบทฐานข้อมูลผ่านระบบฉีดพึ่งพา
    public ITSupportController(Csi402dbContext db)
    {
        // กำหนดค่าบริบทฐานข้อมูลให้กับตัวแปรภายในคลาส
        _db = db;
    }

    // เซ็ตข้อมูลสิทธิ์บทบาทผู้ใช้งานที่ได้รับอนุญาตในระบบ
    // กำหนดชุดข้อมูลบทบาททั้งหมดที่ระบบอนุญาต
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        // รายชื่อบทบาททั้ง 5 บทบาทในระบบ FriendHouse
        "customer", "staff", "accountant", "itsupport", "owner"
    };

    // ฟังก์ชันช่วยดึงรหัสผู้ใช้งานปัจจุบันของฝ่ายไอทีจากเซสชัน
    private int? GetCurrentITUserId()
    {
        // ค้นหารหัสผู้ใช้งานจากเซสชันตามลำดับสิทธิ์
        return HttpContext.Session.GetInt32("ITSupportUserId")
            // หากไม่พบ ให้ดึงจากเซสชันเจ้าของร้าน
            ?? HttpContext.Session.GetInt32("OwnerUserId")
            // หากไม่พบ ให้ดึงจากเซสชันผู้ใช้ทั่วไป
            ?? HttpContext.Session.GetInt32("UserId");
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าถึงเฉพาะบทบาทผู้ดูแลระบบไอที
    private bool IsAuthorizedIT()
    {
        // อ่านค่าบทบาทปัจจุบันจากเซสชัน
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // อ่านค่ารหัสผู้ใช้งานฝ่ายไอทีจากเซสชัน
        var itId = HttpContext.Session.GetInt32("ITSupportUserId");
        // หากมีบทบาทหลักในเซสชัน ต้องเป็นบทบาทผู้ดูแลระบบไอทีเท่านั้น
        if (!string.IsNullOrEmpty(role))
        {
            // ส่งคืนผลการเปรียบเทียบบทบาทผู้ดูแลระบบไอที
            return role == "itsupport";
        }
        // ตรวจสอบว่ามีเซสชันเฉพาะของฝ่ายไอทีหรือไม่
        return itId.HasValue;
    }

    // ฟังก์ชันคืนค่าชื่อ Action และ Controller หน้าหลักตามบทบาทที่กำลังเข้าสู่ระบบ
    private static (string ActionName, string ControllerName) GetRoleHomeRoute(string? role)
    {
        // ปรับแต่งชื่อบทบาทเป็นตัวพิมพ์เล็ก
        string normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
        // คืนค่าเส้นทางหน้าหลักตามบทบาทผู้ใช้งาน
        return normalized switch
        {
            // กำหนดเส้นทางหน้าหลักสำหรับบทบาทพนักงาน
            "staff" => ("Index", "Staff"),
            // กำหนดเส้นทางหน้าหลักสำหรับบทบาทเจ้าของร้านไปยังหน้ารายการโปรโมชั่น
            "owner" => ("Promotions", "Owner"),
            // กำหนดเส้นทางหน้าหลักสำหรับบทบาทนักบัญชีไปยังหน้าหลักภาพรวมบัญชี
            "accountant" => ("Index", "Accountant"),
            // กำหนดเส้นทางหน้าหลักสำหรับบทบาทฝ่ายไอทีไปยังแดชบอร์ดระบบ
            "itsupport" => ("Dashboard", "ITSupport"),
            // กำหนดเส้นทางเริ่มต้นสำหรับบทบาทลูกค้าและผู้ใช้ทั่วไป
            _ => ("Index", "Home")
        };
    }

    // ฟังก์ชันสร้างรหัสบันทึกกิจกรรมระบบพร้อมเวลาและเลขสุ่ม
    private static string GenerateLogCode(string prefix)
    {
        // ปรับแต่งคำนำหน้ารหัสบันทึก
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "LOG" : prefix.Trim().TrimEnd('-').ToUpperInvariant();
        // จำกัดความยาวคำนำหน้าไม่ให้เกิน 11 ตัวอักษร
        if (cleanPrefix.Length > 11) cleanPrefix = cleanPrefix[..11];
        // สร้างรหัสบันทึกพร้อมเวลาปัจจุบันและเลขสุ่ม
        string code = $"{cleanPrefix}-{DateTime.Now:yyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
        // คืนค่ารหัสบันทึกความยาวไม่เกิน 30 ตัวอักษร
        return code.Length <= 30 ? code : code[..30];
    }

    // ฟังก์ชันตรวจสอบความถูกต้องของเบอร์โทรศัพท์ (ตัวเลข 9 ถึง 15 หลัก)
    private static bool IsValidPhoneNumber(string? phone)
    {
        // คัดกรองเฉพาะตัวเลขออกจากข้อความเบอร์โทรศัพท์
        string digits = string.IsNullOrWhiteSpace(phone) ? string.Empty : new string(phone.Where(char.IsDigit).ToArray());
        // ตรวจสอบจำนวนหลักของตัวเลขเบอร์โทรศัพท์
        return digits.Length >= 9 && digits.Length <= 15;
    }

    // ฟังก์ชันจัดรูปแบบข้อความสำหรับส่งออกไฟล์ CSV
    private static string EscapeCsvField(string? value)
    {
        // ตรวจสอบกรณีข้อความว่างเปล่า
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // แทนที่เครื่องหมายคำพูดซ้อนและครอบด้วยเครื่องหมายคำพูด
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    // ฟังก์ชันคำนวณสัดส่วนความสูงสีน้ำเงิน (Info), สีส้ม (Warning), และสีแดง (Error) ภายในแท่งกราฟเดียวกันตามจำนวนจริง
    private static (int InfoPct, int WarnPct, int ErrPct) CalculateStackedSegments(int info, int warn, int err)
    {
        // คำนวณจำนวนรวมทั้งหมดของแท่งกราฟ
        int total = info + warn + err;
        // หากไม่มีข้อมูลเลยให้คืนค่า 0 ทั้งหมด
        if (total <= 0) return (0, 0, 0);
        // คำนวณสัดส่วนเปอร์เซ็นต์ของ Error (หากมีอย่างน้อย 1 รายการ ให้แสดงขั้นต่ำ 8% เพื่อให้มองเห็นขีดสีแดงบนยอดแท่ง)
        int errPct = err > 0 ? Math.Max(8, (int)Math.Round((double)err / total * 100)) : 0;
        // คำนวณสัดส่วนเปอร์เซ็นต์ของ Warning (หากมีอย่างน้อย 1 รายการ ให้แสดงขั้นต่ำ 8% เพื่อให้มองเห็นขีดสีส้ม)
        int warnPct = warn > 0 ? Math.Max(8, (int)Math.Round((double)warn / total * 100)) : 0;
        // ป้องกันผลรวมของ Error และ Warning เกินสัดส่วนจริงเมื่อมี Info อยู่ด้วย
        if (info > 0 && (errPct + warnPct) > 85)
        {
            // ปรับลดตามสัดส่วนเพื่อให้ยังเห็นสีน้ำเงินของ Info
            errPct = err > 0 ? Math.Max(10, (int)Math.Round((double)err / total * 80)) : 0;
            // คำนวณสัดส่วนความสูงของแท่งกราฟเตือนภัย
            warnPct = warn > 0 ? Math.Max(10, (int)Math.Round((double)warn / total * 80)) : 0;
        }
        // คำนวณสัดส่วนเปอร์เซ็นต์ของ Info ที่เหลือทั้งหมด (ฐานสีน้ำเงิน)
        int infoPct = Math.Max(0, 100 - errPct - warnPct);
        // ส่งคืนสัดส่วนทั้ง 3 สีรวมกันได้ 100%
        return (infoPct, warnPct, errPct);
    }

    // ฟังก์ชันตรวจสอบและบังคับให้ 1 เบราว์เซอร์ใช้งานได้เพียง 1 บทบาทตามเซสชันล่าสุด
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // อ่านค่าบทบาทปัจจุบันของเบราว์เซอร์จากเซสชัน
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // หากล็อกอินเป็นบทบาทอื่นที่ไม่ใช่ฝ่ายไอที ให้บังคับเปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทนั้นทันที
        if (!string.IsNullOrEmpty(activeRole) && activeRole != "itsupport")
        {
            // ดึงเส้นทางหน้าหลักของบทบาทที่กำลังใช้งานอยู่
            var (actionName, controllerName) = GetRoleHomeRoute(activeRole);
            // บังคับเปลี่ยนเส้นทางไปยังหน้าหลักของบทบาทปัจจุบัน
            context.Result = RedirectToAction(actionName, controllerName);
            // จบการทำงานของตัวกรองทันที
            return;
        }
        // เรียกใช้งานฟังก์ชันพื้นฐานของคลาสแม่ตามปกติ
        base.OnActionExecuting(context);
    }

    // แอ็กชันหน้าหลักของฝ่ายไอที นำทางไปยังหน้าภาพรวมสถานะระบบ
    public IActionResult Index()
    {
        // เปลี่ยนเส้นทางไปยังหน้าภาพรวมสถานะระบบ
        return RedirectToAction("Dashboard");
    }

    // READ: ภาพรวมสถานะระบบของฝ่ายผู้ดูแลระบบไอที พร้อมกราฟแท่งซ้อนตามสัดส่วนจริงจากฐานข้อมูล
    public IActionResult Dashboard(string? range = "30d")
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // นับจำนวนผู้ใช้งานทั้งหมดในระบบโดยไม่รวมบัญชีที่ถูกลบไปแล้ว
        ViewBag.TotalUsers = _db.Users.Count(u => !u.Email.EndsWith("@deleted.local"));
        // นับจำนวนบันทึกกิจกรรมและข้อผิดพลาดทั้งหมดจากตารางบันทึกกิจกรรมระบบ
        ViewBag.TotalLogs = _db.Systemlogs.Count();
        // นับจำนวนข้อผิดพลาดระดับร้ายแรงจากตารางบันทึกกิจกรรมระบบ
        ViewBag.ErrorLogs = _db.Systemlogs.Count(l => l.LogLevel.ToUpper() == "ERROR");
        // นับจำนวนรายการแจ้งเตือนจากตารางบันทึกกิจกรรมระบบ
        ViewBag.WarningLogs = _db.Systemlogs.Count(l => l.LogLevel.ToUpper() == "WARNING");
        // นับจำนวนรายการปกติจากตารางบันทึกกิจกรรมระบบ
        ViewBag.InfoLogs = _db.Systemlogs.Count(l => l.LogLevel.ToUpper() == "INFO");
        // นับจำนวนสินค้าทั้งหมดในระบบจากตารางสินค้า
        ViewBag.TotalProducts = _db.Products.Count();
        // ดึงรายการบันทึกประวัติการทำงานล่าสุด 5 รายการจากฐานข้อมูลจริง
        ViewBag.RecentLogs = _db.Systemlogs.OrderByDescending(l => l.Timestamp).Take(5).ToList();

        // ปรับแต่งค่าตัวกรองช่วงเวลาของกราฟบันทึกกิจกรรมระบบ (1d = รายวัน, 30d = 30 วัน, 90d = 90 วัน)
        var activeRange = string.IsNullOrWhiteSpace(range) ? "30d" : range.Trim().ToLowerInvariant();
        // ตรวจสอบความถูกต้องของค่าช่วงเวลา หากไม่อยู่ในเกณฑ์ให้ใช้ค่าเริ่มต้น 30 วัน
        if (activeRange != "1d" && activeRange != "30d" && activeRange != "90d")
        {
            // กำหนดค่าเริ่มต้นเป็น 30 วันล่าสุด
            activeRange = "30d";
        }

        // อ่านเวลาปัจจุบันตามเวลาท้องถิ่นสำหรับคำนวณช่วงกราฟ
        var nowLocal = DateTime.Now;
        // สร้างวัฒนธรรมภาษาไทยสำหรับจัดรูปแบบวันที่บนแกนกราฟ
        var thaiCulture = new System.Globalization.CultureInfo("th-TH");
        // เตรียมรายการข้อมูลแท่งกราฟแบบซ้อนสัดส่วนจริงสำหรับส่งไปยังหน้า View
        var chartBars = new List<(string Label, int InfoCount, int WarningCount, int ErrorCount, int TotalCount, int HeightPct, int InfoPct, int WarningPct, int ErrorPct)>();

        // กรณีเลือกดูกราฟรายวัน (24 ชั่วโมงล่าสุด แบ่งเป็น 12 ช่วง ช่วงละ 2 ชั่วโมง)
        if (activeRange == "1d")
        {
            // คำนวณเวลาเริ่มต้นย้อนหลัง 24 ชั่วโมงในรูปแบบ UTC
            var sinceUtc = DateTime.UtcNow.AddHours(-24);
            // ดึงบันทึกกิจกรรมในรอบ 24 ชั่วโมงจากฐานข้อมูลจริง
            var rawLogs = _db.Systemlogs.Where(l => l.Timestamp >= sinceUtc).ToList();
            // เตรียมรายการเก็บผลลัพธ์ชั่วคราวของแต่ละช่วงเวลา
            var tempBuckets = new List<(string Label, int Info, int Warn, int Err, int Total)>();
            // วนลูปสร้างข้อมูล 12 ช่วงเวลา (ช่วงละ 2 ชั่วโมง)
            for (int i = 11; i >= 0; i--)
            {
                // คำนวณเวลาสิ้นสุดของแท่งกราฟในรูปแบบ UTC
                var slotEndUtc = DateTime.UtcNow.AddHours(-(i * 2));
                // คำนวณเวลาเริ่มต้นของแท่งกราฟในรูปแบบ UTC
                var slotStartUtc = slotEndUtc.AddHours(-2);
                // กรองรายการบันทึกที่อยู่ในช่วงเวลานี้ (สำหรับแท่งปัจจุบันให้รวมรายการที่มีเวลา Local ด้วย)
                var slotLogs = rawLogs.Where(l =>
                    // ตรวจสอบเวลาเริ่มต้นของสล็อต
                    l.Timestamp >= slotStartUtc &&
                    // ตรวจสอบเวลาสิ้นสุดของสล็อต
                    (i == 0 ? l.Timestamp <= slotEndUtc.AddHours(8) : l.Timestamp <= slotEndUtc)).ToList();
                // นับจำนวนรายการระดับปกติ
                int infoCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "INFO");
                // นับจำนวนรายการระดับแจ้งเตือน
                int warnCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "WARNING");
                // นับจำนวนรายการระดับข้อผิดพลาด
                int errCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "ERROR");
                // สร้างป้ายกำกับช่วงเวลาเป็นชั่วโมง:นาที
                var slotLabel = $"{slotStartUtc.ToLocalTime():HH:mm}-{slotEndUtc.ToLocalTime():HH:mm} น.";
                // เพิ่มข้อมูลลงในรายการชั่วคราว
                tempBuckets.Add((slotLabel, infoCnt, warnCnt, errCnt, slotLogs.Count));
            }
            // คำนวณค่าสูงสุดของจำนวนบันทึกเพื่อใช้ปรับสัดส่วนความสูงแท่งกราฟ
            int maxTotal = Math.Max(1, tempBuckets.Max(b => b.Total));
            // วนลูปแปลงข้อมูลเป็นรูปแบบสำหรับแสดงผลบนหน้า View
            foreach (var b in tempBuckets)
            {
                // คำนวณความสูงของแท่งกราฟเป็นเปอร์เซ็นต์
                int heightPct = b.Total == 0 ? 12 : Math.Clamp((int)Math.Round((double)b.Total / maxTotal * 100), 28, 100);
                // คำนวณสัดส่วนสีน้ำเงิน สีส้ม และสีแดงภายในแท่งกราฟ
                var (infoPct, warnPct, errPct) = CalculateStackedSegments(b.Info, b.Warn, b.Err);
                // เพิ่มข้อมูลแท่งกราฟลงในรายการผลลัพธ์
                chartBars.Add((b.Label, b.Info, b.Warn, b.Err, b.Total, heightPct, infoPct, warnPct, errPct));
            }
            // กำหนดชื่อหัวข้อกราฟสำหรับมุมมองรายวัน
            ViewBag.ChartTitle = "สถิติบันทึกกิจกรรมระบบรายวัน (24 ชั่วโมงล่าสุด)";
            // กำหนดป้ายกำกับแกน X ด้านซ้าย
            ViewBag.ChartStartLabel = nowLocal.AddHours(-24).ToString("HH:mm น.");
            // กำหนดป้ายกำกับแกน X ตรงกลาง
            ViewBag.ChartMidLabel = nowLocal.AddHours(-12).ToString("HH:mm น.");
            // กำหนดป้ายกำกับแกน X ด้านขวา
            ViewBag.ChartEndLabel = nowLocal.ToString("HH:mm น. (ปัจจุบัน)");
        }
        // กรณีเลือกดูกราฟ 90 วันล่าสุด (แบ่งเป็น 30 ช่วง ช่วงละ 3 วัน)
        else if (activeRange == "90d")
        {
            // คำนวณวันเริ่มต้นย้อนหลัง 90 วันในรูปแบบ UTC
            var sinceUtc = DateTime.UtcNow.Date.AddDays(-89);
            // ดึงบันทึกกิจกรรมในรอบ 90 วันจากฐานข้อมูลจริง
            var rawLogs = _db.Systemlogs.Where(l => l.Timestamp >= sinceUtc).ToList();
            // เตรียมรายการเก็บผลลัพธ์ชั่วคราวของแต่ละช่วงเวลา
            var tempBuckets = new List<(string Label, int Info, int Warn, int Err, int Total)>();
            // วนลูปสร้างข้อมูล 30 ช่วง (ช่วงละ 3 วัน)
            for (int i = 29; i >= 0; i--)
            {
                // คำนวณวันสิ้นสุดของช่วงเวลา
                var periodEndDate = DateTime.UtcNow.Date.AddDays(-(i * 3));
                // คำนวณวันเริ่มต้นของช่วงเวลา
                var periodStartDate = periodEndDate.AddDays(-2);
                // กรองรายการบันทึกที่อยู่ในช่วง 3 วันนี้
                var slotLogs = rawLogs.Where(l => l.Timestamp.Date >= periodStartDate && l.Timestamp.Date <= periodEndDate).ToList();
                // นับจำนวนรายการระดับปกติ
                int infoCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "INFO");
                // นับจำนวนรายการระดับแจ้งเตือน
                int warnCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "WARNING");
                // นับจำนวนรายการระดับข้อผิดพลาด
                int errCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "ERROR");
                // สร้างป้ายกำกับช่วงวันที่ภาษาไทย
                var slotLabel = $"{periodStartDate.ToString("d MMM", thaiCulture)} - {periodEndDate.ToString("d MMM", thaiCulture)}";
                // เพิ่มข้อมูลลงในรายการชั่วคราว
                tempBuckets.Add((slotLabel, infoCnt, warnCnt, errCnt, slotLogs.Count));
            }
            // คำนวณค่าสูงสุดของจำนวนบันทึกเพื่อใช้ปรับสัดส่วนความสูงแท่งกราฟ
            int maxTotal = Math.Max(1, tempBuckets.Max(b => b.Total));
            // วนลูปแปลงข้อมูลเป็นรูปแบบสำหรับแสดงผลบนหน้า View
            foreach (var b in tempBuckets)
            {
                // คำนวณความสูงของแท่งกราฟเป็นเปอร์เซ็นต์
                int heightPct = b.Total == 0 ? 12 : Math.Clamp((int)Math.Round((double)b.Total / maxTotal * 100), 28, 100);
                // คำนวณสัดส่วนสีน้ำเงิน สีส้ม และสีแดงภายในแท่งกราฟ
                var (infoPct, warnPct, errPct) = CalculateStackedSegments(b.Info, b.Warn, b.Err);
                // เพิ่มข้อมูลแท่งกราฟลงในรายการผลลัพธ์
                chartBars.Add((b.Label, b.Info, b.Warn, b.Err, b.Total, heightPct, infoPct, warnPct, errPct));
            }
            // กำหนดชื่อหัวข้อกราฟสำหรับมุมมอง 90 วันล่าสุด
            ViewBag.ChartTitle = "สถิติบันทึกกิจกรรมระบบ (90 วันล่าสุด)";
            // กำหนดป้ายกำกับแกน X ด้านซ้าย
            ViewBag.ChartStartLabel = nowLocal.AddDays(-89).ToString("dd MMM", thaiCulture);
            // กำหนดป้ายกำกับแกน X ตรงกลาง
            ViewBag.ChartMidLabel = nowLocal.AddDays(-45).ToString("dd MMM", thaiCulture);
            // กำหนดป้ายกำกับแกน X ด้านขวา
            ViewBag.ChartEndLabel = nowLocal.ToString("dd MMM", thaiCulture);
        }
        // กรณีค่าเริ่มต้น: เลือกดูกราฟ 30 วันล่าสุด (แบ่งเป็นรายวัน 30 แท่ง)
        else
        {
            // คำนวณวันเริ่มต้นย้อนหลัง 30 วันในรูปแบบ UTC
            var sinceUtc = DateTime.UtcNow.Date.AddDays(-29);
            // ดึงบันทึกกิจกรรมในรอบ 30 วันจากฐานข้อมูลจริง
            var rawLogs = _db.Systemlogs.Where(l => l.Timestamp >= sinceUtc).ToList();
            // เตรียมรายการเก็บผลลัพธ์ชั่วคราวของแต่ละวัน
            var tempBuckets = new List<(string Label, int Info, int Warn, int Err, int Total)>();
            // วนลูปสร้างข้อมูลรายวันจำนวน 30 วัน
            for (int i = 29; i >= 0; i--)
            {
                // คำนวณวันที่ของแท่งกราฟ
                var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                // กรองรายการบันทึกที่เกิดขึ้นในวันนั้น
                var slotLogs = rawLogs.Where(l => l.Timestamp.Date == targetDate).ToList();
                // นับจำนวนรายการระดับปกติ
                int infoCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "INFO");
                // นับจำนวนรายการระดับแจ้งเตือน
                int warnCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "WARNING");
                // นับจำนวนรายการระดับข้อผิดพลาด
                int errCnt = slotLogs.Count(l => (l.LogLevel ?? "").ToUpperInvariant() == "ERROR");
                // สร้างป้ายกำกับวันที่ภาษาไทย
                var slotLabel = targetDate.ToString("d MMM yyyy", thaiCulture);
                // เพิ่มข้อมูลลงในรายการชั่วคราว
                tempBuckets.Add((slotLabel, infoCnt, warnCnt, errCnt, slotLogs.Count));
            }
            // คำนวณค่าสูงสุดของจำนวนบันทึกเพื่อใช้ปรับสัดส่วนความสูงแท่งกราฟ
            int maxTotal = Math.Max(1, tempBuckets.Max(b => b.Total));
            // วนลูปแปลงข้อมูลเป็นรูปแบบสำหรับแสดงผลบนหน้า View
            foreach (var b in tempBuckets)
            {
                // คำนวณความสูงของแท่งกราฟเป็นเปอร์เซ็นต์
                int heightPct = b.Total == 0 ? 12 : Math.Clamp((int)Math.Round((double)b.Total / maxTotal * 100), 28, 100);
                // คำนวณสัดส่วนสีน้ำเงิน สีส้ม และสีแดงภายในแท่งกราฟ
                var (infoPct, warnPct, errPct) = CalculateStackedSegments(b.Info, b.Warn, b.Err);
                // เพิ่มข้อมูลแท่งกราฟลงในรายการผลลัพธ์
                chartBars.Add((b.Label, b.Info, b.Warn, b.Err, b.Total, heightPct, infoPct, warnPct, errPct));
            }
            // กำหนดชื่อหัวข้อกราฟสำหรับมุมมอง 30 วันล่าสุด
            ViewBag.ChartTitle = "สถิติบันทึกกิจกรรมระบบรายวัน (30 วันล่าสุด)";
            // กำหนดป้ายกำกับแกน X ด้านซ้าย
            ViewBag.ChartStartLabel = nowLocal.AddDays(-29).ToString("dd MMM", thaiCulture);
            // กำหนดป้ายกำกับแกน X ตรงกลาง
            ViewBag.ChartMidLabel = nowLocal.AddDays(-15).ToString("dd MMM", thaiCulture);
            // กำหนดป้ายกำกับแกน X ด้านขวา
            ViewBag.ChartEndLabel = nowLocal.ToString("dd MMM", thaiCulture);
        }

        // ส่งค่าช่วงเวลาที่กำลังเลือกไปยังหน้า View
        ViewBag.ChartRange = activeRange;
        // ส่งรายการแท่งกราฟทั้งหมดไปยังหน้า View
        ViewBag.ChartBars = chartBars;

        // ส่งการทำงานไปแสดงผลที่หน้าภาพรวมสถานะระบบ
        return View();
    }

    // READ: จัดการบัญชีผู้ใช้งานทั้งหมดพร้อมค้นหา กรองข้อมูล และแบ่งหน้าผ่าน UserManagementViewModel
    public IActionResult Users(string? searchQuery, string? statusFilter, string? roleFilter, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // บันทึกข้อความแจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // สร้างคำสั่งคิวรีพื้นฐานสำหรับดึงข้อมูลผู้ใช้งานโดยไม่รวมบัญชีที่ถูกลบไปแล้ว
        var query = _db.Users.Where(u => !u.Email.EndsWith("@deleted.local")).AsQueryable();

        // ตรวจสอบว่ามีการกรอกคำค้นหาหรือไม่ (รองรับการค้นหาเบอร์โทรและรหัสผู้ใช้โดยไม่ต้องพิมพ์ขีด -)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // ทำความสะอาดและแปลงคำค้นหาเป็นตัวพิมพ์เล็ก
            var cleanKeyword = searchQuery.Trim().ToLower();
            // ลบเครื่องหมายขีดและช่องว่างเพื่อการค้นหาแบบยืดหยุ่น
            var keywordNoHyphen = cleanKeyword.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองผู้ใช้งานด้วยอีเมล เบอร์โทรศัพท์ รหัสผู้ใช้ หรือชื่อ-นามสกุล ทั้งแบบมีขีดและไม่มีขีด
            query = query.Where(u =>
                // กรองจากอีเมล
                u.Email.ToLower().Contains(cleanKeyword) ||
                // กรองจากเบอร์โทรศัพท์
                u.Phone.ToLower().Contains(cleanKeyword) ||
                // กรองจากเบอร์โทรศัพท์แบบตัดขีดและเว้นวรรค
                (!string.IsNullOrEmpty(keywordNoHyphen) && u.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(keywordNoHyphen)) ||
                // กรองจากรหัสสมาชิก
                u.UserCode.ToLower().Contains(cleanKeyword) ||
                // กรองจากรหัสสมาชิกแบบตัดขีด
                (!string.IsNullOrEmpty(keywordNoHyphen) && u.UserCode.Replace("-", "").ToLower().Contains(keywordNoHyphen)) ||
                // กรองจากชื่อ-นามสกุล
                (u.FullName != null && u.FullName.ToLower().Contains(cleanKeyword)));
        }

        // ตรวจสอบว่ามีการกรองตามบทบาทผู้ใช้งานหรือไม่
        if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "all")
        {
            // ปรับแต่งบทบาทที่ต้องการกรองให้เป็นตัวพิมพ์เล็ก
            var normalizedRole = roleFilter.Trim().ToLowerInvariant();
            // กรองเฉพาะผู้ใช้งานที่มีบทบาทตรงตามเงื่อนไข
            query = query.Where(u => u.RoleName != null && u.RoleName.ToLower() == normalizedRole);
        }

        // ตรวจสอบว่ามีการกรองตามสถานะบัญชีหรือไม่
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            // ตรวจสอบเงื่อนไขสถานะปกติ
            if (statusFilter == "ปกติ")
            {
                // กรองเฉพาะบัญชีที่เปิดใช้งานปกติ
                query = query.Where(u => u.IsActive == true);
            }
            // ตรวจสอบเงื่อนไขสถานะถูกล็อก
            else if (statusFilter == "ถูกล็อก")
            {
                // กรองเฉพาะบัญชีที่ถูกระงับการใช้งาน
                query = query.Where(u => u.IsActive == false);
            }
        }

        // ดึงรายการผู้ใช้งานทั้งหมดเรียงลำดับจากวันที่สมัครล่าสุด และคำนวณการแบ่งหน้า (หน้าละ 10 รายการ)
        var allUsers = query.OrderByDescending(u => u.CreatedAt).ToList();
        // กำหนดขนาดรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // นับจำนวนผู้ใช้งานทั้งหมดตามเงื่อนไข
        int totalCount = allUsers.Count;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        // ตรวจสอบขอบเขตหน้าปัจจุบัน
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงข้อมูลเฉพาะรายการของหน้าปัจจุบัน
        var pagedUsers = allUsers.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ดึงข้อมูลเวลาเข้าสู่ระบบล่าสุดของผู้ใช้งานแต่ละรายจากตารางบันทึกกิจกรรมระบบ
        var lastLogins = _db.Systemlogs
            // กรองเฉพาะบันทึกกิจกรรมที่มีรหัสผู้ใช้และเป็นโมดูลล็อกอิน
            .Where(l => l.UserId != null && l.Module == "auth.login")
            // จัดกลุ่มตามรหัสผู้ใช้
            .GroupBy(l => l.UserId!.Value)
            // เลือกเวลาล็อกอินล่าสุดของแต่ละกลุ่ม
            .Select(g => new { UserId = g.Key, LastLogin = g.Max(l => l.Timestamp) })
            // แปลงเป็นพจนานุกรม
            .ToDictionary(x => x.UserId, x => x.LastLogin);

        // ส่งพจนานุกรมเวลาเข้าสู่ระบบล่าสุดและค่าการค้นหาไปยัง ViewBag เพื่อความเข้ากันได้
        ViewBag.LastLogins = lastLogins;
        // ส่งคำค้นหา
        ViewBag.SearchQuery = searchQuery;
        // ส่งตัวกรองสถานะ
        ViewBag.StatusFilter = statusFilter;
        // ส่งตัวกรองบทบาท
        ViewBag.RoleFilter = roleFilter;

        // ประกอบข้อมูลลงใน UserManagementViewModel ตามมาตรฐานสถาปัตยกรรม MVC
        var vm = new UserManagementViewModel
        {
            // กำหนดคำค้นหาและตัวกรองสถานะ/บทบาทผู้ใช้
            SearchQuery = searchQuery,
            // กำหนดตัวกรองสถานะ
            StatusFilter = statusFilter ?? "all",
            // กำหนดตัวกรองบทบาท
            RoleFilter = roleFilter ?? "all",
            // ข้อมูลการเข้าสู่ระบบล่าสุดและรหัสผ่านชั่วคราว
            LastLogins = lastLogins,
            // กำหนดอีเมลที่มีการรีเซ็ตรหัสผ่านชั่วคราว
            TempResetEmail = TempData["ResetEmail"] as string,
            // กำหนดรหัสผ่านชั่วคราวสำหรับแสดงแจ้งเตือน
            TempResetPassword = TempData["TempPassword"] as string,
            // ข้อมูลการแบ่งหน้าแสดงผล
            TotalCount = totalCount,
            // กำหนดหมายเลขหน้าปัจจุบัน
            CurrentPage = currentPage,
            // กำหนดขนาดหน้า
            PageSize = pageSize,
            // แปลงรายการผู้ใช้จาก Entity เป็น UserAccountItemViewModel
            UserItems = pagedUsers.Select(u => new UserAccountItemViewModel
            {
                // ข้อมูลรหัสผู้ใช้
                UserId = u.UserId,
                // กำหนดรหัสสมาชิก
                UserCode = u.UserCode,
                // กำหนดชื่อ-นามสกุล
                FullName = u.FullName,
                // กำหนดอีเมล
                Email = u.Email,
                // กำหนดเบอร์โทรศัพท์
                Phone = u.Phone,
                // กำหนดบทบาทผู้ใช้
                RoleName = u.RoleName ?? "customer",
                // กำหนดคะแนนสะสม
                Points = u.Points,
                // กำหนดสถานะเปิดใช้งาน
                IsActive = u.IsActive ?? true,
                // กำหนดวันเวลาที่สร้าง
                CreatedAt = u.CreatedAt,
                // วันเวลาเข้าสู่ระบบครั้งล่าสุด
                LastLoginAt = lastLogins.ContainsKey(u.UserId) ? lastLogins[u.UserId] : null
            // แปลงผลลัพธ์เป็นรายการลิสต์
            }).ToList()
        };
        // เพิ่มรายการลงใน ViewModel
        vm.AddRange(pagedUsers);

        // ส่ง ViewModel ไปยังหน้าแสดงผลรายชื่อผู้ใช้งาน
        return View(vm);
    }

    // CREATE: เพิ่มผู้ใช้งานใหม่เข้าสู่ระบบฐานข้อมูล
    // รองรับคำขอแบบ POST สำหรับสร้างผู้ใช้ใหม่
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับสร้างผู้ใช้งานใหม่ในระบบ
    public IActionResult CreateUser(string role, string? fullName, string email, string phone, string password)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความครบถ้วนของข้อมูลที่จำเป็น
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phone))
        {
            // แจ้งเตือนกรณีข้อมูลไม่ครบ
            TempData["ErrorMessage"] = "กรุณากรอกอีเมล เบอร์โทรศัพท์ และรหัสผ่านให้ครบถ้วน";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตัดช่องว่างอีเมล
        var normalizedEmail = email.Trim();
        // ตัดช่องว่างเบอร์โทรศัพท์
        var cleanPhone = phone.Trim();
        // ตรวจสอบความถูกต้องของรูปแบบเบอร์โทรศัพท์
        if (!IsValidPhoneNumber(cleanPhone))
        {
            // แจ้งเตือนเมื่อเบอร์โทรศัพท์ผิดรูปแบบ
            TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9–15 หลัก";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบความยาวของรหัสผ่าน
        if (password.Trim().Length < 6)
        {
            // แจ้งเตือนเมื่อรหัสผ่านสั้นเกินไป
            TempData["ErrorMessage"] = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ปรับแต่งบทบาทผู้ใช้งานให้อยู่ในรูปแบบตัวพิมพ์เล็ก
        var targetRole = string.IsNullOrWhiteSpace(role) ? "customer" : role.Trim().ToLowerInvariant();
        // ตรวจสอบว่าบทบาทตรงตามสิทธิ์ที่อนุญาตหรือไม่
        if (!AllowedRoles.Contains(targetRole))
        {
            // แจ้งเตือนกรณีบทบาทไม่ถูกต้อง
            TempData["ErrorMessage"] = "ไม่พบสิทธิ์การใช้งานที่ระบุในระบบ";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ตรวจสอบว่าอีเมลซ้ำกับในระบบหรือไม่
        var checkUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail.ToLower());
        // หากเป็นบัญชีเดิมที่เคยถูกระงับไว้ ให้ปลดล็อกอีเมลเดิมเพื่อสร้างบัญชีใหม่ได้ทันที
        if (checkUser != null && checkUser.IsActive == false)
        {
            // เปลี่ยนชื่ออีเมลของบัญชีที่ถูกระงับเดิมเพื่อคืนสิทธิ์อีเมล
            checkUser.Email = $"deleted_{checkUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
            // บันทึกการปลดล็อกอีเมลเดิมลงฐานข้อมูล
            _db.SaveChanges();
            // ล้างค่าตัวแปรเพื่ออนุญาตให้สร้างบัญชีใหม่ต่อได้
            checkUser = null;
        }
        // ตรวจสอบผลการค้นหาว่ายังมีบัญชีที่ใช้งานอยู่ซ้ำหรือไม่
        if (checkUser != null)
        {
            // แจ้งเตือนกรณีอีเมลซ้ำ
            TempData["ErrorMessage"] = "อีเมลนี้มีอยู่ในระบบแล้ว";
            // สลับกลับไปหน้ารายการผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // สร้างออบเจ็กต์ผู้ใช้งานใหม่พร้อมแฮชรหัสผ่านด้วย BCrypt
        // สร้างออบเจกต์ผู้ใช้ใหม่
        var newUser = new User
        {
            // กำหนดรหัสสมาชิกใหม่แบบสุ่ม
            UserCode = "U" + DateTime.Now.ToString("yyMMddHHmmss") + RandomNumberGenerator.GetInt32(10, 99),
            // กำหนดบทบาทผู้ใช้
            RoleName = targetRole,
            // กำหนดชื่อ-นามสกุล
            FullName = string.IsNullOrWhiteSpace(fullName) ? "สมาชิกใหม่" : fullName.Trim(),
            // กำหนดอีเมลที่ปรับรูปแบบมาตรฐานแล้ว
            Email = normalizedEmail,
            // กำหนดเบอร์โทรศัพท์
            Phone = cleanPhone,
            // เข้ารหัสผ่านด้วย BCrypt
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim()),
            // กำหนดแต้มสะสมเริ่มต้นตามบทบาท
            Points = string.Equals(targetRole, "customer", StringComparison.OrdinalIgnoreCase) ? 100 : 0,
            // กำหนดสถานะเปิดใช้งาน
            IsActive = true,
            // กำหนดวันเวลาที่สร้างบัญชี
            CreatedAt = DateTime.UtcNow
        };

        // เพิ่มผู้ใช้งานใหม่ลงในฐานข้อมูล
        _db.Users.Add(newUser);

        // บันทึกกิจกรรมการสร้างผู้ใช้งานลงในตารางบันทึกระบบ
        // บันทึกกิจกรรมการสร้างผู้ใช้ใหม่ลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = GenerateLogCode("LOG"),
            // กำหนดระดับเป็น INFO
            LogLevel = "INFO",
            // กำหนดโมดูลเป็น auth.users
            Module = "auth.users",
            // กำหนดข้อความรายละเอียดกิจกรรม
            Message = $"ผู้ดูแลระบบไอทีสร้างผู้ใช้งานใหม่: {normalizedEmail} สิทธิ์การใช้งาน: {newUser.RoleName}",
            // กำหนดรหัสผู้ใช้ไอทีที่เป็นผู้สร้าง
            UserId = GetCurrentITUserId(),
            // กำหนดวันเวลาที่ทำกิจกรรม
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนผลการสร้างบัญชีสำเร็จ
        TempData["SuccessMessage"] = "สร้างบัญชีผู้ใช้งาน \"" + normalizedEmail + "\" เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: แก้ไขข้อมูลผู้ใช้งานและสถานะบัญชีตามรูปแบบฟอร์มมาตรฐาน MVC
    // รองรับคำขอแบบ POST สำหรับแก้ไขข้อมูลผู้ใช้
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับแก้ไขข้อมูลผู้ใช้งาน
    public IActionResult EditUser(string email, string? phone, string? role, string? status, string? fullName, int? points)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบผู้ใช้งานนี้ในระบบ";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ดึงรหัสผู้ใช้งานฝ่ายไอทีปัจจุบัน
        var currentUserId = GetCurrentITUserId();

        // ตรวจสอบและอัปเดตเบอร์โทรศัพท์
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // ตัดช่องว่างเบอร์โทรศัพท์
            var cleanPhone = phone.Trim();
            // ตรวจสอบความถูกต้องของเบอร์โทรศัพท์
            if (!IsValidPhoneNumber(cleanPhone))
            {
                // แจ้งเตือนกรณีเบอร์โทรศัพท์ไม่ถูกต้อง
                TempData["ErrorMessage"] = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง กรุณากรอกตัวเลข 9–15 หลัก";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดเบอร์โทรศัพท์ใหม่
            user.Phone = cleanPhone;
        }

        // อัปเดตชื่อ-นามสกุล
        if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName.Trim();

        // ตรวจสอบและอัปเดตบทบาทผู้ใช้งาน
        if (!string.IsNullOrWhiteSpace(role))
        {
            // ปรับแต่งบทบาทเป็นตัวพิมพ์เล็ก
            var normalizedRole = role.Trim().ToLowerInvariant();
            // ตรวจสอบว่าบทบาทที่ระบุได้รับอนุญาตหรือไม่
            if (!AllowedRoles.Contains(normalizedRole))
            {
                // แจ้งเตือนกรณีบทบาทไม่ถูกต้อง
                TempData["ErrorMessage"] = "ไม่พบสิทธิ์การใช้งานที่ระบุในระบบ";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // ป้องกันการลดสิทธิ์บัญชีตนเองที่กำลังล็อกอินอยู่
            if (currentUserId.HasValue && currentUserId.Value == user.UserId && normalizedRole != "itsupport" && normalizedRole != "owner")
            {
                // แจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่สามารถลดสิทธิ์บัญชีผู้ดูแลที่กำลังใช้งานอยู่ของตนเองได้";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดบทบาทใหม่
            user.RoleName = normalizedRole;
        }

        // อัปเดตคะแนนสะสม
        if (points.HasValue && points.Value >= 0) user.Points = points.Value;

        // ตรวจสอบและอัปเดตสถานะบัญชี
        if (!string.IsNullOrWhiteSpace(status))
        {
            // คำนวณสถานะเปิดใช้งาน
            bool willBeActive = status.Equals("active", StringComparison.OrdinalIgnoreCase) || status == "ปกติ";
            // ป้องกันการระงับบัญชีตนเองที่กำลังใช้งานอยู่
            if (!willBeActive && currentUserId.HasValue && currentUserId.Value == user.UserId)
            {
                // แจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่สามารถระงับการใช้งานบัญชีที่กำลังล็อกอินอยู่ของตนเองได้";
                // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
                return RedirectToAction("Users");
            }
            // กำหนดสถานะบัญชีใหม่
            user.IsActive = willBeActive;
        }

        // บันทึกกิจกรรมการแก้ไขข้อมูลผู้ใช้งานลงในตารางบันทึกระบบ
        // บันทึกกิจกรรมการแก้ไขข้อมูลผู้ใช้ลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = GenerateLogCode("EDIT"),
            // กำหนดระดับเป็น INFO
            LogLevel = "INFO",
            // กำหนดโมดูลเป็น auth.users
            Module = "auth.users",
            // กำหนดข้อความรายละเอียดกิจกรรม
            Message = $"ผู้ดูแลระบบไอทีแก้ไขข้อมูลผู้ใช้: {user.Email} สถานะการใช้งาน: {(user.IsActive == true ? "ปกติ" : "ถูกล็อก")}",
            // กำหนดรหัสผู้ใช้ไอทีที่เป็นผู้แก้ไข
            UserId = currentUserId,
            // กำหนดวันเวลาที่ทำกิจกรรม
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"อัปเดตข้อมูลบัญชี {user.Email} เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // UPDATE: รีเซ็ตรหัสผ่านชั่วคราวด้วยฟอร์มมาตรฐาน MVC
    // รองรับคำขอแบบ POST สำหรับรีเซ็ตรหัสผ่าน
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับรีเซ็ตรหัสผ่านผู้ใช้งาน
    public IActionResult ResetPassword(int userId)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานตามรหัส UserId
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบบัญชี
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการรีเซ็ตรหัสผ่าน";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // สุ่มตัวเลข 4 หลักเพื่อสร้างรหัสผ่านชั่วคราว
        var randomNum = RandomNumberGenerator.GetInt32(1000, 10000);
        // สร้างข้อความรหัสผ่านชั่วคราว
        var tempPassword = "FH-Temp-" + randomNum;
        // แฮชรหัสผ่านชั่วคราวด้วย BCrypt
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // บันทึกกิจกรรมการรีเซ็ตรหัสผ่านลงในตารางบันทึกระบบ
        // บันทึกกิจกรรมการรีเซ็ตรหัสผ่านลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = GenerateLogCode("RESET"),
            // กำหนดระดับเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลเป็น auth.security
            Module = "auth.security",
            // กำหนดข้อความรายละเอียดกิจกรรม
            Message = $"ผู้ดูแลระบบไอทีรีเซ็ตรหัสผ่านชั่วคราวให้ผู้ใช้: {user.Email}",
            // กำหนดรหัสผู้ใช้ไอทีที่เป็นผู้รีเซ็ต
            UserId = GetCurrentITUserId(),
            // กำหนดวันเวลาที่ทำกิจกรรม
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // ส่งอีเมลและรหัสผ่านชั่วคราวไปยัง TempData เพื่อแสดงผล
        TempData["ResetEmail"] = user.Email;
        // ส่งรหัสผ่านชั่วคราว
        TempData["TempPassword"] = tempPassword;
        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"สร้างรหัสผ่านชั่วคราวสำหรับบัญชี {user.Email} เรียบร้อยแล้ว: {tempPassword}";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // DELETE: ลบหรือระงับการใช้งานบัญชีผู้ใช้งานเพื่อรักษาความสมบูรณ์ของข้อมูลอ้างอิงในฐานข้อมูล
    // รองรับคำขอแบบ POST สำหรับลบหรือระงับผู้ใช้งาน
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับลบหรือระงับบัญชีผู้ใช้งาน
    public IActionResult DeleteUser(string email)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาผู้ใช้งานจากอีเมลในฐานข้อมูล
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        // ตรวจสอบว่าพบผู้ใช้งานหรือไม่
        if (user == null)
        {
            // แจ้งเตือนกรณีไม่พบผู้ใช้งาน
            TempData["ErrorMessage"] = "ไม่พบบัญชีผู้ใช้งานที่ต้องการลบ";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ดึงรหัสผู้ใช้งานฝ่ายไอทีปัจจุบัน
        var currentUserId = GetCurrentITUserId();
        // ป้องกันการลบบัญชีที่กำลังล็อกอินอยู่ของตนเอง
        if (currentUserId.HasValue && currentUserId.Value == user.UserId)
        {
            // แจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่สามารถลบหรือระงับบัญชีที่กำลังเข้าสู่ระบบอยู่ของตนเองได้";
            // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
            return RedirectToAction("Users");
        }

        // ปลดการเชื่อมโยงบันทึกกิจกรรมระบบออกจากรหัสผู้ใช้งานที่จะลบ เพื่อให้สามารถลบบัญชีได้โดยไม่ติด Foreign Key
        var userLogs = _db.Systemlogs.Where(l => l.UserId == user.UserId).ToList();
        // วนลูปปลดรหัสผู้ใช้ในบันทึกกิจกรรมระบบ
        foreach (var log in userLogs)
        {
            // กำหนดรหัสผู้ใช้เป็นค่าว่างเพื่อเก็บประวัติข้อความไว้โดยไม่ล็อกการลบบัญชี
            log.UserId = null;
        }

        // ตรวจสอบประวัติการสั่งจองพรีออเดอร์ของผู้ใช้งาน
        bool hasPreorders = _db.Preorders.Any(p => p.UserId == user.UserId);
        // ตรวจสอบประวัติการแลกแต้มของผู้ใช้งาน
        bool hasRedemptions = _db.Rewardredemptions.Any(r => r.UserId == user.UserId);
        // ตรวจสอบประวัติใบเสร็จรับเงินของผู้ใช้งาน
        bool hasReceipts = _db.Receipts.Any(r => r.CustomerId == user.UserId || r.CashierUserId == user.UserId);
        // ตรวจสอบประวัติในสมุดบัญชีของผู้ใช้งาน
        bool hasLedgers = _db.Financialledgers.Any(f => f.RecordedByUserId == user.UserId);

        // หากไม่มีข้อมูลธุรกรรมการเงินหรือใบจองเชื่อมโยงในระบบเลย สามารถลบออกจากฐานข้อมูลได้โดยตรง
        if (!hasPreorders && !hasRedemptions && !hasReceipts && !hasLedgers)
        {
            // ค้นหารายการสินค้าในตะกร้าของผู้ใช้
            var carts = _db.Cartitems.Where(c => c.UserId == user.UserId).ToList();
            // ลบสินค้าในตะกร้าหากมีอยู่
            if (carts.Any()) _db.Cartitems.RemoveRange(carts);
            // ลบบัญชีผู้ใช้งานออกจากฐานข้อมูลจริง
            _db.Users.Remove(user);
        }
        // กรณีผู้ใช้มีประวัติธุรกรรม ให้ระงับการใช้งานแทนการลบ
        else
        {
            // หากมีประวัติธุรกรรมให้ทำการระงับการใช้งานและคืนสิทธิ์อีเมลเดิมให้สามารถสมัครหรือสร้างใหม่ได้ทันที
            user.IsActive = false;
            // เปลี่ยนอีเมลในระเบียนประวัติเดิมไม่ให้ชนกับอีเมลที่จะสมัครใหม่
            user.Email = $"deleted_{user.UserId}_{DateTime.Now:yyyyMMddHHmmss}@deleted.local";
        }

        // บันทึกกิจกรรมการระงับหรือลบบัญชีลงในตารางบันทึกระบบ
        // บันทึกกิจกรรมการลบหรือระงับบัญชีลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัสบันทึกกิจกรรม
            LogCode = GenerateLogCode("DEL"),
            // กำหนดระดับเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลเป็น auth.users
            Module = "auth.users",
            // กำหนดข้อความรายละเอียดกิจกรรม
            Message = $"ผู้ดูแลระบบไอทีระงับและลบบัญชีผู้ใช้: {email}",
            // กำหนดรหัสผู้ใช้ไอทีที่ดำเนินการ
            UserId = currentUserId,
            // กำหนดวันเวลาที่ทำกิจกรรม
            Timestamp = DateTime.UtcNow
        });

        // บันทึกการเปลี่ยนแปลงลงฐานข้อมูลจริง
        _db.SaveChanges();

        // แจ้งเตือนข้อความสำเร็จ
        TempData["SuccessMessage"] = $"ดำเนินการลบบัญชีผู้ใช้งาน {email} เรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับไปหน้ารายชื่อผู้ใช้งาน
        return RedirectToAction("Users");
    }

    // ฟังก์ชันช่วยสร้างคิวรีบันทึกกิจกรรมระบบตามระดับความสำคัญและช่วงเวลา
    private IQueryable<Systemlog> BuildFilteredLogsQuery(string? level, string? timeRange)
    {
        // เริ่มต้นสร้างคิวรีบันทึกกิจกรรมระบบ
        var query = _db.Systemlogs.AsQueryable();

        // ตรวจสอบการกรองตามระดับความสำคัญ
        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // ปรับแต่งระดับความสำคัญเป็นตัวพิมพ์ใหญ่
            var upperLevel = level.Trim().ToUpperInvariant();
            // กรองเฉพาะบันทึกที่มีระดับตรงตามเงื่อนไข
            query = query.Where(l => l.LogLevel.ToUpper() == upperLevel);
        }

        // ดึงเวลาปัจจุบันในรูปแบบ UTC
        var nowUtc = DateTime.UtcNow;
        // ตรวจสอบเงื่อนไขกรอง 24 ชั่วโมงล่าสุด
        if (string.Equals(timeRange, "24h", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 24 ชั่วโมง
            var since = nowUtc.AddHours(-24);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }
        // ตรวจสอบเงื่อนไขกรอง 7 วันล่าสุด
        else if (string.Equals(timeRange, "7d", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 7 วัน
            var since = nowUtc.AddDays(-7);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }
        // ตรวจสอบเงื่อนไขกรอง 30 วันล่าสุด
        else if (string.Equals(timeRange, "30d", StringComparison.OrdinalIgnoreCase))
        {
            // คำนวณเวลาย้อนหลัง 30 วัน
            var since = nowUtc.AddDays(-30);
            // กรองบันทึกที่เกิดขึ้นตั้งแต่เวลานั้น
            query = query.Where(l => l.Timestamp >= since);
        }

        // ส่งคืนผลลัพธ์คำสั่งคิวรี
        return query;
    }

    // READ: บันทึกกิจกรรมและข้อผิดพลาดของระบบพร้อมระบบแบ่งหน้าและกรองช่วงเวลา
    public IActionResult Logs(string? level, string? timeRange, int page = 1)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // กำหนดค่าช่วงเวลาที่ใช้งาน
        var activeRange = string.IsNullOrWhiteSpace(timeRange) ? "all" : timeRange;
        // ดึงรายการบันทึกทั้งหมดตามตัวกรองและเรียงตามเวลาล่าสุด
        var allLogs = BuildFilteredLogsQuery(level, activeRange)
            // จัดเรียงตามวันเวลาจากล่าสุดไปเก่าสุด
            .OrderByDescending(l => l.Timestamp)
            // แปลงผลลัพธ์เป็นรายการลิสต์
            .ToList();

        // กำหนดจำนวนรายการต่อหน้าเป็น 10 รายการ
        int pageSize = 10;
        // นับจำนวนบันทึกทั้งหมด
        int totalCount = allLogs.Count;
        // คำนวณจำนวนหน้าทั้งหมด
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        // ตรวจสอบขอบเขตหน้าปัจจุบัน
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงเฉพาะบันทึกของหน้าปัจจุบัน
        var pagedLogs = allLogs.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งค่าตัวแปรและข้อมูลการแบ่งหน้าไปยังมุมมองผ่าน ViewBag
        ViewBag.SelectedLevel = string.IsNullOrWhiteSpace(level) ? "all" : level.ToUpperInvariant();
        // ส่งช่วงเวลาที่เลือก
        ViewBag.SelectedTimeRange = activeRange;
        // ส่งจำนวนบันทึกทั้งหมด
        ViewBag.TotalLogCount = totalCount;
        // ส่งหน้าปัจจุบัน
        ViewBag.CurrentPage = currentPage;
        // ส่งจำนวนหน้าทั้งหมด
        ViewBag.TotalPages = totalPages;
        // ส่งขนาดรายการต่อหน้า
        ViewBag.PageSize = pageSize;

        // ส่งรายการบันทึกไปแสดงผลที่หน้า Logs
        return View(pagedLogs);
    }

    // EXPORT: ส่งออกบันทึกกิจกรรมระบบจากฐานข้อมูลจริงเป็นไฟล์ .CSV
    [HttpGet]
    // แอ็กชันสำหรับส่งออกข้อมูลบันทึกกิจกรรมเป็นไฟล์ CSV
    public IActionResult ExportLogsCSV(string? level, string? timeRange)
    {
        // ตรวจสอบสิทธิ์การเข้าใช้งานของฝ่ายไอที
        if (!IsAuthorizedIT())
        {
            // แจ้งเตือนกรณีไม่มีสิทธิ์เข้าถึง
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีผู้ดูแลระบบไอที";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลบันทึกตามตัวกรองและเรียงจากเวลาล่าสุด
        var rows = BuildFilteredLogsQuery(level, string.IsNullOrWhiteSpace(timeRange) ? "all" : timeRange)
            // จัดเรียงตามวันเวลาจากล่าสุดไปเก่าสุด
            .OrderByDescending(l => l.Timestamp)
            // แปลงผลลัพธ์เป็นรายการลิสต์
            .ToList();

        // สร้างตัวสร้างข้อความ StringBuilder สำหรับเขียนข้อมูล CSV
        var sb = new StringBuilder();
        // เพิ่มแถวหัวคอลัมน์ภาษาไทย
        sb.AppendLine("รหัสบันทึก,เวลา (UTC),ระดับ,โมดูล,รายละเอียดข้อความ");
        // วนลูปแปลงข้อมูลบันทึกแต่ละรายการลงในบรรทัด CSV
        foreach (var log in rows)
        {
            // เขียนข้อมูลบันทึกทีละแถวพร้อมตัดอักขระพิเศษ
            sb.AppendLine(string.Join(",",
                // ฟิลด์รหัสบันทึก
                EscapeCsvField(log.LogCode),
                // ฟิลด์วันเวลา
                EscapeCsvField(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                // ฟิลด์ระดับความสำคัญ
                EscapeCsvField(log.LogLevel),
                // ฟิลด์ชื่อโมดูล
                EscapeCsvField(log.Module),
                // ฟิลด์ข้อความกิจกรรม
                EscapeCsvField(log.Message)));
        }

        // แปลงข้อความเป็นไบต์อาร์เรย์พร้อมแนบ UTF-8 BOM เพื่อให้เปิดอ่านภาษาไทยใน Excel ได้ถูกต้อง
        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        // ส่งคืนไฟล์ CSV สำหรับดาวน์โหลด
        return File(csvBytes, "text/csv; charset=utf-8", $"FriendHouse_SystemLogs_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }
}
