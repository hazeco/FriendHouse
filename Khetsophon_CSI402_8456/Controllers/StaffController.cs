using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Helpers;
using Khetsophon_CSI402_8456.Models;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

// คอนโทรลเลอร์สำหรับงานเคาน์เตอร์พนักงานหน้าร้าน
public class StaffController : Controller
{
    // ตัวแปรเชื่อมต่อฐานข้อมูล CSI402DB ผ่าน Entity Framework Core
    private readonly Csi402dbContext _db;

    // คอนสตรัคเตอร์รับ DbContext ผ่านการฉีด Dependency Injection
    public StaffController(Csi402dbContext db)
    {
        // กำหนดค่าตัวแปรเชื่อมต่อฐานข้อมูลให้กับฟิลด์ภายในคลาส
        _db = db;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินด้วยบทบาทพนักงานหรือผู้มีอำนาจหรือไม่
    private bool IsAuthorizedStaff()
    {
        // อ่านค่าบทบาท (Role) ของผู้ใช้งานปัจจุบันที่บันทึกไว้ใน Session
        var role = HttpContext.Session.GetString("Role");
        // อ่านค่ารหัสพนักงาน StaffUserId จาก Session
        var staffId = HttpContext.Session.GetInt32("StaffUserId");
        // หากมีรหัส StaffUserId ใน Session แสดงว่าผ่านการตรวจสอบสิทธิ์
        if (staffId.HasValue) return true;
        // ตรวจสอบว่าบทบาทตรงกับ staff, owner หรือ itsupport หรือไม่
        return role != null && (role.ToLower() == "staff" || role.ToLower() == "owner" || role.ToLower() == "itsupport");
    }

    // Action แสดงรายการสต็อกสินค้าและค้นหาใบจองเบื้องต้นสำหรับพนักงาน พร้อมระบบค้นหา กรอง และแบ่งหน้า (GET Method)
    public IActionResult Index(CounterPreorderViewModel? data, int page = 1, bool? openReceive = null, string? stockFilter = null, string? search = null, string? category = null)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้ล็อกอินเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบว่ามีข้อมูลค้นหารหัสใบจองส่งมาหรือไม่
        if (data != null && !string.IsNullOrEmpty(data.ReservationNumber))
        {
            // ส่งหมายเลขใบจองไปยัง View ผ่านตัวแปร ViewBag
            ViewBag.ReservationNumber = data.ReservationNumber;
        }

        // ดึงรายการสินค้าทั้งหมดจากตาราง Products พร้อมข้อมูลหมวดหมู่และสต็อกสินค้า
        var allProducts = _db.Products
            // รวมข้อมูลหมวดหมู่สินค้า Category
            .Include(p => p.Category)
            // รวมข้อมูลจำนวนสต็อกสินค้าในคลัง Inventory
            .Include(p => p.Inventory)
            // เรียงลำดับจากสินค้าใหม่ไปเก่า
            .OrderByDescending(p => p.ProductId)
            // แปลงผลลัพธ์เป็น List รายการสินค้า
            .ToList();

        // คำนวณยอดสรุปสต็อกสินค้าทั้งหมดในคลังด้วยเกณฑ์เดียวกันทั้งตารางและการ์ดสรุป
        ViewBag.TotalProductCount = allProducts.Count;
        // คำนวณจำนวนสินค้าที่มีสต็อกปกติ (พร้อมขายมากกว่าระดับสต็อกปลอดภัย)
        ViewBag.NormalStockCount = allProducts.Count(p =>
        {
            var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
            var safety = p.Inventory?.SafetyStockLevel ?? 5;
            return avail > safety;
        });
        // คำนวณจำนวนสินค้าที่สต็อกใกล้หมด (พร้อมขายมากกว่า 0 แต่ไม่เกินระดับสต็อกปลอดภัย)
        ViewBag.LowStockCount = allProducts.Count(p =>
        {
            var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
            var safety = p.Inventory?.SafetyStockLevel ?? 5;
            return avail > 0 && avail <= safety;
        });
        // คำนวณจำนวนสินค้าที่หมดสต็อกชั่วคราว
        ViewBag.OutOfStockCount = allProducts.Count(p => ((p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)) <= 0);

        // ดึงรายชื่อหมวดหมู่สินค้าทั้งหมดที่ไม่ซ้ำกันเพื่อแสดงในตัวเลือกกรอง
        ViewBag.AllCategories = allProducts
            .Select(p => p.Category?.CategoryName ?? "ทั่วไป")
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // เริ่มต้นการกรองข้อมูลสินค้า
        var filtered = allProducts.AsEnumerable();

        // ตรวจสอบและกรองตามคำค้นหา (ค้นหาจากชื่อ, SKU, บาร์โค้ด หรือชื่อซีรีส์)
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            filtered = filtered.Where(p =>
                (!string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(term)) ||
                (!string.IsNullOrEmpty(p.Sku) && (p.Sku.ToLowerInvariant().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLowerInvariant().Contains(termNoHyphen)))) ||
                (!string.IsNullOrEmpty(p.Barcode) && (p.Barcode.ToLowerInvariant().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Barcode.Replace("-", "").ToLowerInvariant().Contains(termNoHyphen)))) ||
                (!string.IsNullOrEmpty(p.SeriesName) && p.SeriesName.ToLowerInvariant().Contains(term)));
        }

        // ปรับแต่งและตรวจสอบค่าตัวกรองหมวดหมู่สินค้า
        string normalizedCategory = (string.IsNullOrWhiteSpace(category) ||
            string.Equals(category.Trim(), "all", StringComparison.OrdinalIgnoreCase) ||
            category.Trim() == "ทุกหมวดหมู่")
            ? "all"
            : category.Trim();
        // หากเลือกหมวดหมู่เฉพาะให้กรองตามหมวดหมู่นั้น
        if (normalizedCategory != "all")
        {
            filtered = filtered.Where(p => string.Equals(p.Category?.CategoryName ?? "ทั่วไป", normalizedCategory, StringComparison.OrdinalIgnoreCase));
        }

        // ตรวจสอบและกรองตามสถานะสต็อกสินค้า (ปกติ / ใกล้หมด / หมดสต็อก)
        string normalizedFilter = (stockFilter ?? "all").Trim().ToLowerInvariant();
        if (normalizedFilter == "normal")
        {
            // กรองเฉพาะสินค้าสต็อกปกติ
            filtered = filtered.Where(p =>
            {
                var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
                var safety = p.Inventory?.SafetyStockLevel ?? 5;
                return avail > safety;
            });
        }
        else if (normalizedFilter == "low")
        {
            // กรองเฉพาะสินค้าสต็อกใกล้หมด
            filtered = filtered.Where(p =>
            {
                var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
                var safety = p.Inventory?.SafetyStockLevel ?? 5;
                return avail > 0 && avail <= safety;
            });
        }
        else if (normalizedFilter == "out")
        {
            // กรองเฉพาะสินค้าหมดสต็อก
            filtered = filtered.Where(p => ((p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)) <= 0);
        }
        else
        {
            // ค่าเริ่มต้นแสดงสินค้าทุกระดับสต็อก
            normalizedFilter = "all";
        }

        // แปลงผลการกรองเป็นรายการข้อมูล List
        var filteredList = filtered.ToList();

        // ส่งรายชื่อสินค้าทั้งหมดและประวัติการรับเข้าคลังล่าสุดจากฐานข้อมูลจริงไปยังหน้าต่างรับสินค้า
        ViewBag.AllProductsForModal = allProducts.OrderBy(p => p.Name).ToList();
        ViewBag.RecentRestockLogs = _db.Systemlogs
            .Where(l => l.Module == "Inventory")
            .OrderByDescending(l => l.Timestamp)
            .Take(6)
            .ToList();
        ViewBag.OpenReceiveModal = openReceive ?? false;

        // คำนวณการแบ่งหน้าสำหรับตารางสต็อกสินค้า (หน้าละ 10 รายการ)
        int pageSize = 10;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredList.Count / pageSize));
        int currentPage = Math.Clamp(page, 1, totalPages);
        var pagedProducts = filteredList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งข้อมูลตัวกรองและการแบ่งหน้าไปยัง View
        ViewBag.SelectedStockFilter = normalizedFilter;
        ViewBag.SelectedSearch = search?.Trim() ?? string.Empty;
        ViewBag.SelectedCategory = normalizedCategory;
        ViewBag.FilteredCount = filteredList.Count;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        // ส่งข้อมูลรายการสินค้าของหน้าปัจจุบันไปยังหน้า Index ของ Staff
        return View(pagedProducts);
    }

    // Action สำหรับเปิดหน้าเคาน์เตอร์พรีออเดอร์ (GET Method)
    [HttpGet]
    // ฟังก์ชัน CounterPreorder รับค่า query ที่ส่งมาทาง URL เพื่อค้นหาข้อมูลอัตโนมัติ
    public IActionResult CounterPreorder(string? query)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินด้วยบทบาทพนักงาน (Staff) หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData ไปแสดงผลที่หน้า Login
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // สั่งเปลี่ยนเส้นทาง (Redirect) ไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        // ประกาศตัวแปรเก็บข้อมูลใบจองพรีออเดอร์และรายการแลกของรางวัลที่ค้นหาพบ
        Preorder? foundPreorder = null;
        Rewardredemption? foundRedemption = null;

        // ตรวจสอบว่าผู้ใช้ส่งข้อความค้นหา (query) เข้ามาหรือไม่ (รองรับการค้นหาโดยไม่ต้องพิมพ์ขีด - หรือ #)
        if (!string.IsNullOrWhiteSpace(query))
        {
            (foundPreorder, foundRedemption) = ResolveCounterSearch(query);
        }

        // กรณีไม่ได้ระบุคำค้นหา ให้แสดงใบจองล่าสุดเป็นตัวอย่าง
        if (foundPreorder == null && foundRedemption == null && string.IsNullOrWhiteSpace(query))
        {
            var defaultPreorder = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(item => item.Product)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault(p => p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
                ?? _db.Preorders
                    .Include(p => p.Preorderitems)
                        .ThenInclude(item => item.Product)
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

            if (defaultPreorder != null)
            {
                ViewBag.SearchResult = defaultPreorder;
                ViewBag.Status = $"กำลังแสดงรายการพรีออเดอร์ล่าสุด #{defaultPreorder.ReservationNumber} ({defaultPreorder.ContactPhone})";
            }
        }

        // เตรียมข้อมูลค้นหาด่วน (ทั้ง Preorder และ RewardRedemption) และเบอร์สมาชิก
        PopulateCounterRecentData();

        return View(new CounterPreorderViewModel { ReservationNumber = foundPreorder?.ReservationNumber ?? foundRedemption?.RedemptionCode ?? query ?? "" });
    }

    // Action สำหรับค้นหาใบจองพรีออเดอร์จากช่องค้นหา (POST Method)
    [HttpPost]
    // ฟังก์ชัน CounterPreorder รับข้อมูลโมเดล data จากฟอร์มค้นหา
    public IActionResult CounterPreorder(CounterPreorderViewModel data)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์เป็นพนักงานเคาน์เตอร์หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        string rawQuery = (data?.ReservationNumber ?? data?.Phone ?? "").Trim();

        if (!string.IsNullOrEmpty(rawQuery))
        {
            ResolveCounterSearch(rawQuery);
        }
        else
        {
            ViewBag.Status = "กรุณากรอกรหัสพรีออเดอร์ รหัสแลกของรางวัล หรือเบอร์โทรศัพท์สำหรับค้นหา";
        }

        PopulateCounterRecentData();

        return View(data);
    }

    // ฟังก์ชันช่วยค้นหาใบสั่งจอง (Preorders) และรายการแลกของรางวัล (Rewardredemptions) ร่วมกันระหว่าง GET และ POST
    private (Preorder? Preorder, Rewardredemption? Redemption) ResolveCounterSearch(string rawQuery)
    {
        // ตัดเครื่องหมาย # และช่องว่างหัวท้ายออกจากคำค้นหา
        string cleanQuery = rawQuery.Replace("#", "").Trim();
        // แปลงคำค้นหาเป็นตัวพิมพ์เล็ก
        string lowerQuery = cleanQuery.ToLower();
        // ปรับแต่งคำค้นหาโดยตัดขีดและช่องว่างออกเพื่อรองรับการพิมพ์แบบไม่มีขีด
        string normalizedQuery = lowerQuery.Replace("-", "").Replace(" ", "");

        // ตรวจสอบว่าคำค้นหาขึ้นต้นด้วยรหัสของรางวัล (RWD) หรือไม่
        bool preferRedemption = normalizedQuery.StartsWith("rwd");

        // หากเป็นรหัสของรางวัล ให้ค้นหาจากตารางของรางวัลก่อน
        if (preferRedemption)
        {
            // ค้นหาข้อมูลการแลกของรางวัลตามคำค้นหา
            var matchedRedemption = FindRedemptionByQuery(cleanQuery, lowerQuery, normalizedQuery);
            // ตรวจสอบว่าพบข้อมูลการแลกของรางวัลหรือไม่
            if (matchedRedemption != null)
            {
                // ส่งผลลัพธ์ของรางวัลไปยัง ViewBag เพื่อแสดงผล
                ViewBag.SearchResultRedemption = matchedRedemption;
                // กำหนดข้อความสถานะการค้นหาสำเร็จ
                ViewBag.Status = $"พบข้อมูลแลกของรางวัล #{matchedRedemption.RedemptionCode} ลูกค้า: {matchedRedemption.User?.FullName ?? matchedRedemption.User?.Email}";
                // ส่งคืนผลลัพธ์ของรางวัล
                return (null, matchedRedemption);
            }
        }

        // ค้นหาข้อมูลใบสั่งจองพรีออเดอร์ตามคำค้นหา
        var matchedPreorder = FindPreorderByQuery(cleanQuery, lowerQuery, normalizedQuery);
        // ตรวจสอบว่าพบข้อมูลใบสั่งจองหรือไม่
        if (matchedPreorder != null)
        {
            // ส่งผลลัพธ์ใบสั่งจองไปยัง ViewBag เพื่อแสดงผล
            ViewBag.SearchResult = matchedPreorder;
            // กำหนดข้อความสถานะการค้นหาสำเร็จ
            ViewBag.Status = $"พบข้อมูลใบสั่งจอง #{matchedPreorder.ReservationNumber} ลูกค้า: {matchedPreorder.User?.FullName ?? matchedPreorder.ContactPhone}";
            // ส่งคืนผลลัพธ์ใบสั่งจอง
            return (matchedPreorder, null);
        }

        // หากยังไม่พบและไม่ได้ค้นหาด้วยรหัสของรางวัลในรอบแรก ให้ลองค้นหาของรางวัลเป็นตัวสำรอง
        if (!preferRedemption)
        {
            // ค้นหาข้อมูลการแลกของรางวัลตามคำค้นหา
            var matchedRedemption = FindRedemptionByQuery(cleanQuery, lowerQuery, normalizedQuery);
            // ตรวจสอบว่าพบข้อมูลการแลกของรางวัลหรือไม่
            if (matchedRedemption != null)
            {
                // ส่งผลลัพธ์ของรางวัลไปยัง ViewBag เพื่อแสดงผล
                ViewBag.SearchResultRedemption = matchedRedemption;
                // กำหนดข้อความสถานะการค้นหาสำเร็จ
                ViewBag.Status = $"พบข้อมูลแลกของรางวัล #{matchedRedemption.RedemptionCode} ลูกค้า: {matchedRedemption.User?.FullName ?? matchedRedemption.User?.Email}";
                // ส่งคืนผลลัพธ์ของรางวัล
                return (null, matchedRedemption);
            }
        }

        // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูลทั้งสองตาราง
        ViewBag.Status = $"ไม่พบข้อมูลใบสั่งจองหรือรหัสแลกของรางวัลตามเงื่อนไข \"{rawQuery}\" ในระบบ";
        // ส่งคืนผลลัพธ์ว่าง
        return (null, null);
    }

    // ค้นหาใบสั่งจองพรีออเดอร์รองรับการค้นหาแบบไม่ใส่ขีด - และค้นหาบางส่วน
    private Preorder? FindPreorderByQuery(string cleanQuery, string lowerQuery, string normalizedQuery)
    {
        var allPreorders = _db.Preorders
            .Include(p => p.Preorderitems)
                .ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        return allPreorders.FirstOrDefault(p =>
            p.ReservationNumber.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(normalizedQuery) && p.ReservationNumber.Replace("-", "").Replace("#", "").Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(normalizedQuery) && p.ReservationNumber.Replace("-", "").Replace("#", "").ToLower().Contains(normalizedQuery))
            || p.ContactPhone.ToLower().Contains(lowerQuery)
            || (!string.IsNullOrEmpty(normalizedQuery) && p.ContactPhone.Replace("-", "").Replace(" ", "").ToLower().Contains(normalizedQuery))
            || (p.User != null && p.User.Phone != null && p.User.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(normalizedQuery))
            || (p.User != null && p.User.FullName != null && p.User.FullName.ToLower().Contains(lowerQuery))
            || (p.User != null && p.User.Email.ToLower().Contains(lowerQuery)));
    }

    // ค้นหารายการแลกของรางวัลรองรับรหัส RWD แบบไม่ใส่ขีด - เบอร์โทรที่ระบุตอนแลก/บัญชี และชื่อลูกค้า
    private Rewardredemption? FindRedemptionByQuery(string cleanQuery, string lowerQuery, string normalizedQuery)
    {
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        var metaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);

        var allRedemptions = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .OrderByDescending(r => r.RedeemedAt)
            .ToList();

        return allRedemptions.FirstOrDefault(r =>
        {
            metaMap.TryGetValue(r.RedemptionCode, out var rMeta);
            string metaPhone = PromoAndRedemptionHelper.NormalizePhoneDigits(rMeta?.ContactPhone);

            return r.RedemptionCode.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(normalizedQuery) && r.RedemptionCode.Replace("-", "").Replace("#", "").ToLower().Contains(normalizedQuery))
                || (!string.IsNullOrEmpty(normalizedQuery) && metaPhone.Contains(normalizedQuery))
                || (r.User != null && r.User.Phone != null && r.User.Phone.Replace("-", "").Replace(" ", "").ToLower().Contains(normalizedQuery))
                || (r.User != null && r.User.FullName != null && r.User.FullName.ToLower().Contains(lowerQuery))
                || (r.User != null && r.User.Email.ToLower().Contains(lowerQuery));
        });
    }

    // เตรียมข้อมูลทางลัดค้นหาด่วน (พรีออเดอร์ + แลกของรางวัล) เบอร์สมาชิก และรายละเอียดโปรโมชั่น/วันนัดรับ
    private void PopulateCounterRecentData()
    {
        // ดึงรายการคำสั่งจองพรีออเดอร์ล่าสุด 6 รายการสำหรับแสดงทางลัดค้นหาด่วน
        ViewBag.RecentPreorders = _db.Preorders
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToList();

        // ดึงรายการขอแลกของรางวัลล่าสุด 6 รายการสำหรับแสดงทางลัดค้นหาด่วน
        ViewBag.RecentRedemptions = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .OrderByDescending(r => r.RedeemedAt)
            .Take(6)
            .ToList();

        // ดึงบันทึก Log ข้อมูลเพิ่มเติมสำหรับการแลกของรางวัลและโปรโมชั่น
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        // ดึงบันทึก Log ข้อมูลโปรโมชั่นที่ผูกกับคำสั่งจองพรีออเดอร์
        var promoLogs = _db.Systemlogs.Where(l => l.Module == "preorder.promo").ToList();
        // แปลงข้อมูลเมตาเดตาสำหรับวันนัดรับและเบอร์ติดต่อลงในพจนานุกรม
        ViewBag.RedemptionMetaMap = PromoAndRedemptionHelper.BuildRedemptionMetaMap(rewardLogs);
        // แปลงข้อมูลโปรโมชั่นส่วนลดลงในพจนานุกรมเพื่อนำไปคำนวณยอดเงินที่เคาน์เตอร์
        ViewBag.PreorderPromoMap = PromoAndRedemptionHelper.BuildPreorderPromoMap(promoLogs);
    }

    // เมธอดสำหรับคืนสิทธิ์จำนวนการใช้งานโค้ดโปรโมชั่นกรณีคำสั่งจองถูกยกเลิกหรือไม่สำเร็จ
    private void RestorePromoUsageForPreorder(string reservationNumber)
    {
        // ค้นหาบันทึก Log โปรโมชั่นที่มีเลขที่ใบจองตรงกัน
        var promoLogs = _db.Systemlogs
            .Where(l => l.Module == "preorder.promo" && l.DetailPayload != null && l.DetailPayload.Contains(reservationNumber))
            .ToList();
        // วนลูปตรวจสอบและปรับลดยอดการใช้งานของแต่ละโปรโมชั่นที่บันทึกไว้
        foreach (var promoLog in promoLogs)
        {
            try
            {
                // แปลงข้อมูล Payload จาก JSON เป็นอ็อบเจกต์ PreorderPromoPayload
                var payload = System.Text.Json.JsonSerializer.Deserialize<PreorderPromoPayload>(promoLog.DetailPayload!);
                // ตรวจสอบว่าหมายเลขใบจองตรงกันทุกประการ
                if (payload != null && string.Equals(payload.ReservationNumber, reservationNumber, StringComparison.OrdinalIgnoreCase))
                {
                    // ค้นหาข้อมูลโปรโมชั่นจากฐานข้อมูลตามโค้ด Voucher
                    var promo = _db.Promotions.FirstOrDefault(p => p.VoucherCode == payload.VoucherCode);
                    // ปรับลดยอดการใช้งานปัจจุบันลง 1 ครั้งหากมียอดค้างอยู่
                    if (promo != null && promo.CurrentUsageCount > 0)
                    {
                        promo.CurrentUsageCount -= 1;
                    }
                }
            }
            catch
            {
                // ละเว้นกรณีแปลงข้อมูล Payload ล้มเหลวเพื่อไม่ให้กระทบการทำงานหลัก
            }
        }
    }

    // Action สำหรับบันทึกการมารับสินค้า ชำระเงิน ตัดสต็อก และสะสมแต้ม (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CompletePickup รับ preorderId (รหัสใบจอง), memberPhone (เบอร์โทรสมาชิก) และ forceWithoutMember (ยืนยันดำเนินการต่อแบบลูกค้าทั่วไปเมื่อไม่พบเบอร์สมาชิก)
    public IActionResult CompletePickup(int? preorderId, string? memberPhone, bool? forceWithoutMember)
    {
        // ขั้นตอนที่ 0: ตรวจสอบสิทธิ์ว่าผู้ใช้งานคือพนักงานเคาน์เตอร์หรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาดกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง กรุณาเข้าสู่ระบบด้วยบัญชีพนักงาน";
            // เปลี่ยนเส้นทางกลับไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบจอง ว่ามีค่าและมากกว่าศูนย์หรือไม่
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัสใบจองที่ส่งมา
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจองที่ต้องการทำรายการ";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาข้อมูลใบสั่งจองในฐานข้อมูล พร้อมดึงรายการสินค้าที่จองไว้ข้างในและชื่อสินค้า (Preorderitems -> Product)
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบข้อมูลใบสั่งจองในฐานข้อมูลจริงหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีค้นหาไม่พบข้อมูลในระบบ
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองนี้ในระบบฐานข้อมูล";
            // เปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบสถานะก่อน: ถ้าออเดอร์เป็น Completed หรือ Cancelled ไปแล้ว จะสั่งบล็อกทันทีเพื่อป้องกันพนักงานกดซ้ำเพื่อปั๊มแต้ม
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่ารายการนี้ถูกปิดสถานะไปแล้ว และห้ามทำรายการซ้ำ
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} นี้ได้ดำเนินการปิดสถานะเป็น {preorder.CurrentStatus} ไปแล้ว ไม่สามารถดำเนินการซ้ำได้เพื่อป้องกันการปั๊มแต้ม";
            // สั่ง Redirect กลับไปที่หน้าเคาน์เตอร์พร้อมส่งรหัสใบจองเพื่อแสดงข้อมูลปัจจุบัน
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ตรวจสอบข้อมูลโปรโมชั่นที่ใช้กับใบจองนี้ (ถ้ามี เช่น ส่วนลด หรือ แต้มคูณสอง)
        var promoLogs = _db.Systemlogs.Where(l => l.Module == "preorder.promo").ToList();
        var promoMap = PromoAndRedemptionHelper.BuildPreorderPromoMap(promoLogs);
        promoMap.TryGetValue(preorder.ReservationNumber, out var preorderPromo);

        // ประกาศตัวแปรเก็บจำนวนแต้มสะสมที่สมาชิกจะได้รับ (เริ่มต้นที่ 0)
        int pointsEarned = 0;
        // ประกาศตัวแปรเก็บข้อความรายละเอียดการสะสมแต้มสำหรับนำไปแสดงผล
        string memberLogMsg = "";

        // ตรวจสอบว่าพนักงานได้กรอกเบอร์โทรศัพท์สมาชิกเข้ามาหรือไม่
        if (!string.IsNullOrWhiteSpace(memberPhone))
        {
            // ทำความสะอาดเบอร์โทรศัพท์โดยตัดช่องว่างและเครื่องหมายขีดลบ (-) ออก
            string cleanPhone = memberPhone.Trim().Replace("-", "").Replace(" ", "");
            // ค้นหาข้อมูลสมาชิกในตาราง Users ด้วยเบอร์โทรศัพท์ (รองรับทั้งแบบมีขีดและไม่มีขีด) หรืออีเมล
            var memberUser = _db.Users
                .ToList()
                .FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(u.Phone) && u.Phone.Replace("-", "").Replace(" ", "").Equals(cleanPhone, StringComparison.OrdinalIgnoreCase))
                    || u.Email.Equals(memberPhone.Trim(), StringComparison.OrdinalIgnoreCase));

            // ตรวจสอบว่าพบข้อมูลสมาชิกในระบบหรือไม่
            if (memberUser != null)
            {
                // คำนวณแต้มสะสม: 10% ของยอดเงินรวม (ขั้นต่ำ 10 แต้ม) และคูณ 2 หากใช้โปรโมชั่นแต้มสะสมสองเท่า
                pointsEarned = (int)Math.Max(10, Math.Round(preorder.TotalFinalPrice / 10));
                if (preorderPromo?.IsDoublePoints == true)
                {
                    pointsEarned *= 2;
                }
                // คำนวณแต้มสะสมจากยอดเงิน แล้วนำไปบวกให้สมาชิก memberUser.Points += pointsEarned;
                memberUser.Points += pointsEarned;
                string doubleTag = preorderPromo?.IsDoublePoints == true ? " [โปรโมชั่นแต้ม x2]" : "";
                // กำหนดข้อความรายละเอียดการสะสมแต้มสำเร็จ พร้อมระบุชื่อสมาชิกและแต้มคงเหลือรวม
                memberLogMsg = $" (สะสมแต้มให้คุณ {memberUser.FullName ?? memberUser.Email}: +{pointsEarned} แต้ม{doubleTag} รวมปัจจุบัน {memberUser.Points} แต้ม)";
            }
            // กรณีพนักงานกรอกเบอร์โทรเข้ามาแต่ไม่พบข้อมูลสมาชิกในระบบ และยังไม่ได้ยืนยันข้ามการสะสมแต้ม
            else if (forceWithoutMember != true)
            {
                // แจ้งเตือนกลับไปยังหน้าต่างสะสมแต้มเพื่อให้พนักงานเลือกดำเนินการต่อแบบทั่วไปหรือแก้ไขเบอร์โทร
                TempData["MemberNotFoundPhone"] = memberPhone.Trim();
                return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
            }
            // กรณีพนักงานยืนยันดำเนินการต่อแบบลูกค้าทั่วไปเมื่อไม่พบเบอร์สมาชิก
            else
            {
                // บันทึกข้อความแจ้งว่าไม่พบเบอร์สมาชิก และดำเนินการต่อแบบลูกค้าทั่วไป
                memberLogMsg = " (ไม่พบเบอร์สมาชิก ดำเนินการต่อแบบลูกค้าทั่วไป)";
            }
        }

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            // ตรวจสอบสต็อกจริงในคลังก่อนส่งมอบและตัดยอดขาย
            foreach (var item in preorder.Preorderitems)
            {
                var invCheck = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (invCheck == null || invCheck.QuantityOnHand < item.Quantity)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product?.Name ?? $"#{item.ProductId}"}\" มีสต็อกคงเหลือในคลังไม่เพียงพอส่งมอบ (คงเหลือ {invCheck?.QuantityOnHand ?? 0} ชิ้น)";
                    return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
                }
            }

            // ปรับเปลี่ยนสถานะใบสั่งจองใน Entity ให้เป็น "Completed"
            preorder.CurrentStatus = "Completed";
            // บันทึกวันและเวลาปัจจุบันที่ลูกค้ามารับสินค้าจริง (CompletedAt)
            preorder.CompletedAt = DateTime.Now;

            // สร้างเลขที่ใบเสร็จรับเงินไม่ซ้ำกัน
            string receiptNo = "RCP" + DateTime.Now.ToString("yyMMddHHmmssfff");
            int cashierUserId = HttpContext.Session.GetInt32("StaffUserId")
                ?? HttpContext.Session.GetInt32("OwnerUserId")
                ?? HttpContext.Session.GetInt32("UserId")
                ?? 2;

            var receipt = new Receipt
            {
                ReceiptNo = receiptNo,
                PreorderId = preorder.PreorderId,
                CustomerId = preorder.UserId,
                CustomerPhone = !string.IsNullOrWhiteSpace(memberPhone) ? memberPhone.Trim() : preorder.ContactPhone,
                CashierUserId = cashierUserId,
                SubtotalAmount = preorderPromo != null && preorderPromo.OriginalSubtotal > preorder.TotalFinalPrice
                    ? preorderPromo.OriginalSubtotal
                    : preorder.TotalFinalPrice,
                DiscountAmount = preorderPromo?.DiscountAmount ?? 0.00m,
                VatAmount = Math.Round(preorder.TotalFinalPrice * 0.07m, 2),
                NetTotalAmount = preorder.TotalFinalPrice,
                BarcodeData = preorder.ReservationNumber,
                IssuedAt = DateTime.Now
            };

            _db.Receipts.Add(receipt);
            _db.SaveChanges();

            // วนลูปสร้าง Receiptitems และตัดสต็อกสินค้าทีละรายการตามสินค้าที่อยู่ในใบสั่งจอง (ให้ MonthlyReport และคลังตรงกัน 100%)
            foreach (var item in preorder.Preorderitems)
            {
                _db.Receiptitems.Add(new Receiptitem
                {
                    ReceiptId = receipt.ReceiptId,
                    ProductId = item.ProductId,
                    ItemDescription = item.Product?.Name ?? $"สินค้า #{item.ProductId}",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.UnitPrice * item.Quantity
                });

                var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (inv != null)
                {
                    inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                    inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                    inv.UpdatedAt = DateTime.Now;
                }
            }

            _db.SaveChanges();
            transaction.Commit();

            TempData["SuccessMessage"] = $"บันทึกการส่งมอบสินค้าและชำระเงินสำเร็จ! ออกใบเสร็จจริงเลขที่ #{receiptNo}{memberLogMsg}";
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }
        catch (Exception)
        {
            transaction.Rollback();
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกส่งมอบสินค้าและออกใบเสร็จ กรุณาลองใหม่อีกครั้ง";
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }
    }

    // Action สำหรับบันทึกลูกค้าไม่มารับสินค้า (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน NoShowPreorder รับ preorderId, reason (สาเหตุ) และ remark (หมายเหตุ)
    public IActionResult NoShowPreorder(int? preorderId, string reason, string? remark)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบสั่งจอง
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัส
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจอง";
            // เปลี่ยนเส้นทางกลับไปหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาใบสั่งจองในฐานข้อมูลพร้อมรายการสินค้าที่สั่งจอง
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบใบสั่งจองหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบป้องกันการทำรายการซ้ำหากออเดอร์ปิดสถานะไปแล้ว
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่าออเดอร์นี้ปิดไปแล้ว
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} ได้ดำเนินการปิดสถานะไปแล้ว";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ปรับเปลี่ยนสถานะใบสั่งจองเป็น Cancelled (ยกเลิกเนื่องจากไม่มารับ)
        preorder.CurrentStatus = "Cancelled";

        // วนลูปปลดสต็อกที่จองไว้กลับสู่ร้าน
        foreach (var item in preorder.Preorderitems)
        {
            // ค้นหาสต็อกสินค้าในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            // ตรวจสอบว่าพบข้อมูลสต็อกหรือไม่
            if (inv != null)
            {
                // ปลดภาระยอดจองออก (ลด ReservedQuantity โดยไม่ลด QuantityOnHand)
                inv.ReservedQuantity -= item.Quantity;
                // ป้องกันค่ายอดจองติดลบ
                if (inv.ReservedQuantity < 0) inv.ReservedQuantity = 0;
            }
        }

        RestorePromoUsageForPreorder(preorder.ReservationNumber);

        // บันทึกประวัติและสาเหตุลงตาราง Systemlogs สำหรับเป็นหลักฐานการตรวจสอบ (Audit Trail)
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("NOSHOW"),
            LogLevel = "Warning",
            Module = "CounterPreorder",
            Message = $"ลูกค้าไม่มารับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason} | หมายเหตุ: {remark ?? "ไม่มี"}",
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกลูกค้าไม่มารับสินค้าเรียบร้อยแล้ว (ใบจอง #{preorder.ReservationNumber}) ปลดสินค้ากลับสู่สต็อกสำเร็จ";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสใบจอง
        return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
    }

    // Action สำหรับบันทึกลูกค้าปฏิเสธไม่รับสินค้าหน้าร้าน (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน RejectPreorder รับ preorderId, reason (เหตุผลการปฏิเสธ) และ remark (หมายเหตุ)
    public IActionResult RejectPreorder(int? preorderId, string reason, string? remark)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสใบสั่งจอง
        if (!preorderId.HasValue || preorderId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัส
            TempData["ErrorMessage"] = "ไม่พบรหัสใบสั่งจอง";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหาใบสั่งจองในฐานข้อมูลพร้อมรายการสินค้าข้างใน
        var preorder = _db.Preorders
            .Include(p => p.Preorderitems)
            .FirstOrDefault(p => p.PreorderId == preorderId.Value);

        // ตรวจสอบว่าพบข้อมูลในระบบหรือไม่
        if (preorder == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบข้อมูล
            TempData["ErrorMessage"] = "ไม่พบข้อมูลใบสั่งจองในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบว่ารายการนี้ปิดสถานะไปแล้วหรือไม่
        if (preorder.CurrentStatus == "Completed" || preorder.CurrentStatus == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่าไม่สามารถทำรายการซ้ำได้
            TempData["ErrorMessage"] = $"รายการพรีออเดอร์ #{preorder.ReservationNumber} ได้ดำเนินการปิดสถานะไปแล้ว";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }

        // ปรับเปลี่ยนสถานะใบสั่งจองเป็น Cancelled
        preorder.CurrentStatus = "Cancelled";

        // วนลูปปลดสต็อกที่จองไว้กลับสู่ร้าน
        foreach (var item in preorder.Preorderitems)
        {
            // ค้นหาสต็อกสินค้าในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
            // ตรวจสอบว่าพบข้อมูลสต็อกหรือไม่
            if (inv != null)
            {
                // ปลดภาระยอดจองออก (ลด ReservedQuantity เพื่อคืนของให้ผู้อื่นสั่งจองได้)
                inv.ReservedQuantity -= item.Quantity;
                // ป้องกันค่ายอดจองติดลบ
                if (inv.ReservedQuantity < 0) inv.ReservedQuantity = 0;
            }
        }

        RestorePromoUsageForPreorder(preorder.ReservationNumber);

        // บันทึกประวัติและสาเหตุการปฏิเสธลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("REJECT"),
            LogLevel = "Warning",
            Module = "CounterPreorder",
            Message = $"ลูกค้าไม่รับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason} | รายละเอียด: {remark ?? "ไม่มี"}",
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกการไม่รับสินค้าเรียบร้อยแล้ว (ใบจอง #{preorder.ReservationNumber}) ปลดสินค้ากลับสู่สต็อกแล้ว";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสใบจอง
        return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
    }

    // Action สำหรับบันทึกการส่งมอบของรางวัลให้ลูกค้าที่เคาน์เตอร์ (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // ฟังก์ชัน CompleteRewardRedemption รับ redemptionId (รหัสรายการแลกของรางวัล)
    public IActionResult CompleteRewardRedemption(long redemptionId)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางไปยังหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ดึงข้อมูลรายการแลกของรางวัลจากตาราง Rewardredemptions พร้อมข้อมูล Reward และ User
        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId);

        // ตรวจสอบว่าพบรายการแลกของรางวัลในระบบหรือไม่
        if (redemption == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการแลกของรางวัลในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบว่ารายการนี้ถูกส่งมอบหรือยกเลิกไปแล้วหรือไม่ เพื่อป้องกันการทำซ้ำ
        if (redemption.Status == "Completed" || redemption.Status == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนกรณีปิดสถานะไปแล้ว
            TempData["ErrorMessage"] = $"รายการแลกของรางวัล #{redemption.RedemptionCode} ได้ดำเนินการปิดสถานะไปแล้ว ไม่สามารถทำซ้ำได้";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสของรางวัล
            return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
        }

        // ปรับเปลี่ยนสถานะรายการแลกเป็น Completed
        redemption.Status = "Completed";
        // บันทึกวันและเวลาที่ส่งมอบของรางวัลสำเร็จ
        redemption.CompletedAt = DateTime.Now;

        // บันทึกประวัติการส่งมอบลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode("RWD-DLV"),
            LogLevel = "Info",
            Module = "CounterPreorder",
            Message = $"ส่งมอบของรางวัลรหัส #{redemption.RedemptionCode} ({redemption.Reward.Title}) ให้ลูกค้า {redemption.User.FullName ?? redemption.User.Email} เรียบร้อยแล้ว",
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึกส่งมอบ \"{redemption.Reward.Title}\" ให้ลูกค้าเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสของรางวัล
        return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
    }

    // Action สำหรับบันทึกลูกค้าไม่มารับของรางวัลตามกำหนด พร้อมคืนแต้มและคืนสต็อก (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NoShowRewardRedemption(long? redemptionId, string reason, string? remark)
    {
        return CancelRewardRedemptionCore(redemptionId, reason, remark, isNoShow: true);
    }

    // Action สำหรับบันทึกลูกค้าปฏิเสธไม่รับของรางวัลหน้าร้าน พร้อมคืนแต้มและคืนสต็อก (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectRewardRedemption(long? redemptionId, string reason, string? remark)
    {
        return CancelRewardRedemptionCore(redemptionId, reason, remark, isNoShow: false);
    }

    // ฟังก์ชันกลางสำหรับยกเลิกรายการแลกของรางวัลทั้งกรณีไม่รับสินค้าและไม่มารับสินค้า คืนแต้ม + คืนสต็อก + บันทึก Systemlog
    private IActionResult CancelRewardRedemptionCore(long? redemptionId, string reason, string? remark, bool isNoShow)
    {
        if (!IsAuthorizedStaff())
        {
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            return RedirectToAction("Login", "Account");
        }

        if (!redemptionId.HasValue || redemptionId.Value <= 0)
        {
            TempData["ErrorMessage"] = "ไม่พบรหัสรายการแลกของรางวัล";
            return RedirectToAction("CounterPreorder");
        }

        var redemption = _db.Rewardredemptions
            .Include(r => r.Reward)
            .Include(r => r.User)
            .FirstOrDefault(r => r.RedemptionId == redemptionId.Value);

        if (redemption == null)
        {
            TempData["ErrorMessage"] = "ไม่พบรายการแลกของรางวัลในระบบ";
            return RedirectToAction("CounterPreorder");
        }

        if (redemption.Status == "Completed" || redemption.Status == "Cancelled")
        {
            TempData["ErrorMessage"] = $"รายการแลกของรางวัล #{redemption.RedemptionCode} ได้ดำเนินการปิดสถานะไปแล้ว";
            return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
        }

        // เปลี่ยนสถานะเป็น Cancelled และคืนทั้งแต้มสะสมกับสต็อกของรางวัลให้ลูกค้าและคลังสินค้า
        redemption.Status = "Cancelled";
        if (redemption.User != null)
        {
            redemption.User.Points += redemption.PointsSpent;
        }
        if (redemption.Reward != null)
        {
            redemption.Reward.StockQuantity += 1;
        }

        string actionLabel = isNoShow ? "ลูกค้าไม่มารับของรางวัล" : "ลูกค้าไม่รับของรางวัล";
        string logPrefix = isNoShow ? "NOSHOW-R" : "REJECT-R";

        _db.Systemlogs.Add(new Systemlog
        {
            LogCode = PromoAndRedemptionHelper.GenerateLogCode(logPrefix),
            LogLevel = "Warning",
            Module = "CounterPreorder",
            Message = $"{actionLabel} รหัสแลก #{redemption.RedemptionCode} ({redemption.Reward?.Title}) สาเหตุ: {reason} | รายละเอียด: {remark ?? "ไม่มี"} | คืน {redemption.PointsSpent:N0} แต้มและคืนสต็อกแล้ว",
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            Timestamp = DateTime.Now
        });

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"บันทึก{actionLabel}เรียบร้อยแล้ว (#{redemption.RedemptionCode}) ระบบคืน {redemption.PointsSpent:N0} แต้มสะสมและคืนสต็อกของรางวัลเรียบร้อยแล้ว";
        return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
    }

    // Action เปลี่ยนเส้นทางจากเส้นทางเดิมไปยังหน้าสต็อกสินค้าพร้อมเปิดหน้าต่างรับสินค้าเข้าสต็อก
    [HttpGet]
    public IActionResult ScanReceive()
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // นำทางไปยังหน้าสต็อกสินค้าหลักและสั่งเปิดหน้าต่างรับสินค้าเข้าสต็อกโดยตรง
        return RedirectToAction("Index", new { openReceive = true });
    }

    // Action สำหรับบันทึกการรับสินค้าเข้าสต็อกจริงลงตาราง Inventories และ Systemlogs (POST Method)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ScanReceive(string? sku, int? quantity, int? currentPage)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานเป็นพนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบด้วยบัญชีพนักงานประจำสาขา";
            // เปลี่ยนเส้นทางกลับหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตัดช่องว่างหัวท้ายของรหัสสินค้าหรือรหัสบาร์โค้ด
        string cleanSku = (sku ?? "").Trim();

        // ตรวจสอบว่าพนักงานได้ระบุรหัสสินค้าหรือบาร์โค้ดเข้ามาหรือไม่
        if (string.IsNullOrWhiteSpace(cleanSku))
        {
            TempData["ErrorMessage"] = "กรุณาสแกนบาร์โค้ดหรือเลือกรหัสสินค้าที่ต้องการรับเข้าคลัง";
            return RedirectToAction("Index", new { page = currentPage ?? 1, openReceive = true });
        }

        if (!quantity.HasValue || quantity.Value <= 0)
        {
            TempData["ErrorMessage"] = "กรุณาระบุจำนวนสินค้าที่รับเข้าอย่างน้อย 1 ชิ้น";
            return RedirectToAction("Index", new { page = currentPage ?? 1, openReceive = true });
        }

        int addQty = quantity.Value;

        // ค้นหาข้อมูลสินค้าในตาราง Products ด้วยรหัสสินค้าหรือบาร์โค้ด (รองรับการพิมพ์รหัสแบบไม่ใส่ขีดลบ)
        string normalizedSku = cleanSku.Replace("-", "").Replace("#", "").Replace(" ", "").ToLower();
        var product = _db.Products
            .Include(p => p.Inventory)
            .ToList()
            .FirstOrDefault(p => p.Sku.Equals(cleanSku, StringComparison.OrdinalIgnoreCase)
                              || (p.Barcode != null && p.Barcode.Equals(cleanSku, StringComparison.OrdinalIgnoreCase))
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Sku.Replace("-", "").ToLower() == normalizedSku)
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Barcode != null && p.Barcode.Replace("-", "").ToLower() == normalizedSku)
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Sku.Replace("-", "").ToLower().Contains(normalizedSku)));

        // ตรวจสอบว่าพบข้อมูลสินค้าตรงกับรหัสที่สแกนหรือกรอกหรือไม่
        if (product != null)
        {
            // ดึงข้อมูลสต็อกคงคลังของสินค้า
            var inv = product.Inventory;
            // กรณีสินค้านี้ยังไม่มีเรคคอร์ดในตาราง Inventories
            if (inv == null)
            {
                inv = new Inventory
                {
                    ProductId = product.ProductId,
                    QuantityOnHand = addQty,
                    ReservedQuantity = 0,
                    SafetyStockLevel = 5,
                    StorageLocation = "โซน A-01",
                    LastRestockedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.Inventories.Add(inv);
            }
            // กรณีมีข้อมูลสต็อกเดิมอยู่แล้วในระบบ
            else
            {
                inv.QuantityOnHand += addQty;
                inv.LastRestockedAt = DateTime.Now;
                inv.UpdatedAt = DateTime.Now;
            }

            // บันทึกประวัติการรับสินค้าเข้าสต็อกลงในตาราง Systemlogs
            _db.Systemlogs.Add(new Systemlog
            {
                LogCode = PromoAndRedemptionHelper.GenerateLogCode("RESTOCK"),
                LogLevel = "INFO",
                Module = "Inventory",
                Message = $"รับสินค้า {product.Name} (รหัส: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าสต็อก คงเหลือรวม {inv.QuantityOnHand} ชิ้น",
                UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId") ?? 2,
                Timestamp = DateTime.Now
            });

            // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
            _db.SaveChanges();

            // กำหนดข้อความแจ้งผลสำเร็จผ่าน TempData เพื่อแสดงผลให้พนักงานทราบ
            TempData["SuccessMessage"] = $"รับสินค้า {product.Name} ({product.Sku}) จำนวน +{addQty} ชิ้น เข้าคลังเรียบร้อยแล้ว (คงเหลือรวม: {inv.QuantityOnHand} ชิ้น)";
        }
        // กรณีค้นหาไม่พบรหัสสินค้าหรือบาร์โค้ดในฐานข้อมูลจริง
        else
        {
            TempData["ErrorMessage"] = $"ไม่พบรหัสสินค้าหรือบาร์โค้ด \"{cleanSku}\" ในระบบ กรุณาตรวจสอบความถูกต้อง";
        }

        // โหลดกลับมาที่หน้าสต็อกสินค้าหลักพร้อมคงเลขหน้าเดิมไว้ (ฟอร์ม MVC มาตรฐาน)
        return RedirectToAction("Index", new { page = currentPage ?? 1 });
    }
}
