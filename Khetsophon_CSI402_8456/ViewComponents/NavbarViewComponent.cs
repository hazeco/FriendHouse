// นำเข้าไลบรารีจัดการวัฒนธรรมและหน่วยอักขระภาษาไทย
using System.Globalization;
// นำเข้าไลบรารี ASP.NET Core MVC สำหรับสร้าง ViewComponent
using Microsoft.AspNetCore.Mvc;
// นำเข้า Entity Framework Core สำหรับคิวรีข้อมูลจากฐานข้อมูล
using Microsoft.EntityFrameworkCore;
// นำเข้าโมเดลข้อมูลหลักของระบบ FriendHouse
using Khetsophon_CSI402_8456.Models;

// กำหนดเนมสเปซสำหรับส่วนประกอบการแสดงผล
namespace Khetsophon_CSI402_8456.Controllers;

// คอมโพเนนต์แถบนำทางส่วนหัว (Navbar) รองรับการแสดงผลแยกตามบทบาทผู้ใช้ทั้ง 5 บทบาท
public class NavbarViewComponent : ViewComponent
{
    // ตัวแปรบริบทฐานข้อมูลสำหรับดึงยอดตะกร้า แต้มสะสม และการแจ้งเตือน
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับค่าบริบทฐานข้อมูลผ่าน Dependency Injection
    public NavbarViewComponent(Csi402dbContext db)
    {
        // กำหนดค่าบริบทฐานข้อมูลให้กับฟิลด์ภายในคลาส
        _db = db;
    }

    // เมธอดหลักสำหรับประมวลผลและเลือกเรนเดอร์แถบนำทางตามบทบาทที่ระบุ
    public IViewComponentResult Invoke(string role = "guest")
    {
        // กำหนดค่าเริ่มต้นจำนวนสินค้าในตะกร้าเป็น 0 ชิ้น
        int cartCount = 0;
        // กำหนดค่าเริ่มต้นแต้มสะสมของสมาชิกเป็น 0 แต้ม
        int memberPoints = 0;
        // กำหนดตัวอักษรย่อโปรไฟล์เริ่มต้นสำหรับสมาชิก
        string avatarLetter = "ค";

        // ดึงรหัสลูกค้าที่เข้าสู่ระบบจาก Session
        var customerId = HttpContext.Session.GetInt32("CustomerUserId")
            ?? (HttpContext.Session.GetString("Role") == "customer" ? HttpContext.Session.GetInt32("UserId") : null);

        // หากพบว่ามีลูกค้าเข้าสู่ระบบอยู่ ให้ดึงข้อมูลจริงจากฐานข้อมูล
        if (customerId.HasValue)
        {
            // คำนวณจำนวนชิ้นสินค้าในตะกร้าจริงจากตาราง Cartitems ของลูกค้ารายนั้น
            cartCount = _db.Cartitems
                .Where(c => c.UserId == customerId.Value)
                .Sum(c => (int?)c.Quantity) ?? 0;

            // ค้นหาข้อมูลบัญชีผู้ใช้เพื่อดึงแต้มสะสมและชื่อสำหรับสร้างตัวอักษรย่อ
            var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.UserId == customerId.Value);
            // ตรวจสอบว่าพบข้อมูลผู้ใช้ในฐานข้อมูลหรือไม่
            if (user != null)
            {
                // กำหนดแต้มสะสมปัจจุบันของสมาชิก
                memberPoints = user.Points;
                // หากมีชื่อ-นามสกุล ให้ดึงอักษรตัวแรกของชื่อมาแสดงบนไอคอนโปรไฟล์
                if (!string.IsNullOrWhiteSpace(user.FullName))
                {
                    avatarLetter = GetInitials(user.FullName, 1);
                }
                // หากไม่มีชื่อ ให้ใช้อักษรตัวแรกของอีเมลแทน
                else if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    avatarLetter = GetInitials(user.Email, 1).ToUpperInvariant();
                }
            }
        }

        // ส่งค่าจำนวนสินค้าในตะกร้าไปยัง View ผ่าน ViewBag
        ViewBag.CartCount = cartCount;
        // ส่งค่าแต้มสะสมของสมาชิกไปยัง View ผ่าน ViewBag
        ViewBag.MemberPoints = memberPoints;
        // ส่งตัวอักษรย่อโปรไฟล์ไปยัง View ผ่าน ViewBag
        ViewBag.AvatarLetter = avatarLetter;

        // ปรับรูปแบบชื่อบทบาทเป็นตัวพิมพ์เล็กและตัดช่องว่างเพื่อเปรียบเทียบอย่างแม่นยำ
        string normalizedRole = (role ?? "guest").Trim().ToLowerInvariant();
        // แปลงชื่อย่อ it ให้เป็นชื่อบทบาทมาตรฐาน itsupport
        if (normalizedRole == "it") normalizedRole = "itsupport";
        // แปลงชื่อย่อ acc ให้เป็นชื่อบทบาทมาตรฐาน accountant
        if (normalizedRole == "acc") normalizedRole = "accountant";

        // ดึงชื่อผู้ใช้ตามบทบาทจาก Session หรือฐานข้อมูลจริง
        string roleUserName = ResolveRoleUserName(normalizedRole);
        // ส่งชื่อเต็มของผู้ใช้ตามบทบาทไปยัง ViewBag
        ViewBag.RoleUserName = roleUserName;
        // ส่งตัวอักษรย่อ 2 ตัวของชื่อผู้ใช้ตามบทบาทไปยัง ViewBag
        ViewBag.RoleUserInitials = GetInitials(roleUserName, 2);

        // คำนวณการแจ้งเตือนจริงสำหรับพนักงานหน้าร้าน
        if (normalizedRole == "staff")
        {
            // ดึงข้อมูลคลังสินค้าของสินค้าที่ยังเปิดใช้งานทั้งหมดจากฐานข้อมูล
            var activeInventories = _db.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                .Where(i => i.Product != null && i.Product.IsActive != false)
                .ToList();

            // นับจำนวนสินค้าที่สต็อกคงเหลือเป็นศูนย์หรือหมดสต็อก
            int outOfStockCount = activeInventories.Count(i => i.QuantityOnHand <= 0);
            // นับจำนวนสินค้าที่สต็อกใกล้หมด (ต่ำกว่าหรือเท่ากับเกณฑ์ขั้นต่ำ)
            int lowStockCount = activeInventories.Count(i => i.QuantityOnHand > 0 && i.QuantityOnHand <= i.SafetyStockLevel);

            // นับจำนวนใบสั่งจองพรีออเดอร์ที่รอดำเนินการหรือรอส่งมอบหน้าร้าน
            int pendingPreorders = _db.Preorders
                .AsNoTracking()
                .Count(p => p.CurrentStatus == "Pending" || p.CurrentStatus == "ReadyForPickup");

            // นับจำนวนรายการแลกของรางวัลที่รอส่งมอบหน้าร้าน
            int pendingRewards = _db.Rewardredemptions
                .AsNoTracking()
                .Count(r => r.Status == "Pending" || r.Status == "ReadyForPickup");

            // คำนวณจำนวนหมวดหมู่แจ้งเตือนที่มีรายการค้างอยู่สำหรับแสดงจุดแจ้งเตือน
            int badgeCount = 0;
            // เพิ่มจำนวนแจ้งเตือนหากมีสินค้าหมดสต็อก
            if (outOfStockCount > 0) badgeCount++;
            // เพิ่มจำนวนแจ้งเตือนหากมีสินค้าใกล้หมดสต็อก
            if (lowStockCount > 0) badgeCount++;
            // เพิ่มจำนวนแจ้งเตือนหากมีใบจองพรีออเดอร์รอมารับ
            if (pendingPreorders > 0) badgeCount++;
            // เพิ่มจำนวนแจ้งเตือนหากมีรายการแลกของรางวัลรอมารับ
            if (pendingRewards > 0) badgeCount++;

            // ส่งจำนวนหมวดแจ้งเตือนทั้งหมดไปยังแถบนำทางพนักงาน
            ViewBag.StaffNotificationCount = badgeCount;
            // ส่งจำนวนสินค้าหมดสต็อกไปยังแถบนำทางพนักงาน
            ViewBag.StaffOutOfStockCount = outOfStockCount;
            // ส่งจำนวนสินค้าใกล้หมดสต็อกไปยังแถบนำทางพนักงาน
            ViewBag.StaffLowStockCount = lowStockCount;
            // ส่งจำนวนใบจองรอมารับไปยังแถบนำทางพนักงาน
            ViewBag.StaffPendingPreorders = pendingPreorders;
            // ส่งจำนวนของรางวัลรอมารับไปยังแถบนำทางพนักงาน
            ViewBag.StaffPendingRewards = pendingRewards;
        }

        // เลือกชื่อไฟล์ View ของแถบนำทางให้ตรงกับบทบาทของผู้ใช้งาน
        string viewName = normalizedRole switch
        {
            "staff" => "Staff",
            "owner" => "Owner",
            "itsupport" => "ITSupport",
            "accountant" => "Accountant",
            _ => "Default"
        };
        // ส่งคืนผลลัพธ์ ViewComponent พร้อมส่งค่าบทบาทเป็น Model
        return View(viewName, model: role);
    }

    // ฟังก์ชันช่วยค้นหาชื่อผู้ใช้งานตามบทบาทจาก Session หรือฐานข้อมูล
    private string ResolveRoleUserName(string normalizedRole)
    {
        // อ่านชื่อผู้ใช้งานที่บันทึกไว้ใน Session ตามบทบาท
        string? sessionName = normalizedRole switch
        {
            "staff" => HttpContext.Session.GetString("StaffUserName"),
            "owner" => HttpContext.Session.GetString("OwnerUserName"),
            "accountant" => HttpContext.Session.GetString("AccountantUserName"),
            "itsupport" => HttpContext.Session.GetString("ITSupportUserName"),
            _ => null
        };
        // หากมีชื่อใน Session ให้ตัดช่องว่างแล้วส่งคืนทันที
        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            return sessionName.Trim();
        }

        // อ่านรหัสผู้ใช้งานจาก Session ตามบทบาทเพื่อนำไปค้นหาในฐานข้อมูล
        int? roleUserId = normalizedRole switch
        {
            "staff" => HttpContext.Session.GetInt32("StaffUserId"),
            "owner" => HttpContext.Session.GetInt32("OwnerUserId"),
            "accountant" => HttpContext.Session.GetInt32("AccountantUserId"),
            "itsupport" => HttpContext.Session.GetInt32("ITSupportUserId"),
            _ => null
        };

        // หากรหัสผู้ใช้มีค่า ให้คิวรีชื่อเต็มจากตาราง Users
        if (roleUserId.HasValue)
        {
            var dbUser = _db.Users.AsNoTracking().FirstOrDefault(u => u.UserId == roleUserId.Value);
            // หากพบชื่อเต็มในฐานข้อมูล ให้ส่งคืนค่าชื่อนั้น
            if (!string.IsNullOrWhiteSpace(dbUser?.FullName))
            {
                return dbUser.FullName.Trim();
            }
        }

        // กรณีสำรอง: ดึงผู้ใช้คนแรกที่เปิดใช้งานในบทบาทนั้นจากฐานข้อมูล
        var fallbackUser = _db.Users.AsNoTracking()
            .FirstOrDefault(u => u.RoleName.ToLower() == normalizedRole && u.IsActive != false);
        // หากพบชื่อผู้ใช้สำรองในฐานข้อมูล ให้ส่งคืนชื่อนั้น
        if (!string.IsNullOrWhiteSpace(fallbackUser?.FullName))
        {
            return fallbackUser.FullName.Trim();
        }

        // กรณีไม่พบในฐานข้อมูล ให้คืนค่าชื่อตำแหน่งภาษาไทยเริ่มต้นตามบทบาท
        return normalizedRole switch
        {
            "staff" => "พนักงานหน้าร้าน",
            "owner" => "เจ้าของร้าน",
            "accountant" => "ฝ่ายบัญชี",
            "itsupport" => "ไอทีซัพพอร์ต",
            _ => "ผู้ใช้งาน"
        };
    }

    // ฟังก์ชันช่วยดึงตัวอักษรย่อจากชื่อภาษาไทยหรือภาษาอังกฤษอย่างถูกต้องตามอักขระผสม
    private static string GetInitials(string input, int maxClusters)
    {
        // หากข้อความว่างเปล่า ให้คืนค่าอักษรย่อเริ่มต้น FH
        if (string.IsNullOrWhiteSpace(input)) return "FH";
        // แยกคำด้วยช่องว่างเพื่อดึงอักษรตัวแรกของชื่อและนามสกุล
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // สร้างรายการเก็บตัวอักษรย่อที่สกัดได้
        var result = new List<string>();
        // วนลูปอ่านแต่ละคำที่แยกได้
        foreach (var part in parts)
        {
            // ใช้ TextElementEnumerator เพื่อไม่อ่านสระลอยหรือวรรณยุกต์แยกจากพยัญชนะ
            var enumerator = StringInfo.GetTextElementEnumerator(part);
            // เลื่อนไปยังหน่วยอักขระแรกของคำ
            if (enumerator.MoveNext())
            {
                // เพิ่มอักขระตัวแรกเข้าสู่รายการผลลัพธ์
                result.Add(enumerator.GetTextElement());
            }
            // หยุดวนลูปเมื่อได้จำนวนตัวอักษรย่อครบตามที่กำหนด
            if (result.Count >= maxClusters) break;
        }
        // รวมตัวอักษรย่อเป็นสตริงเดียว หรือคืนค่า FH หากไม่พบอักขระ
        return result.Count > 0 ? string.Join("", result) : "FH";
    }
}