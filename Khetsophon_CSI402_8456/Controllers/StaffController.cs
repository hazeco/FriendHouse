using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // ตรวจสอบสิทธิ์ตามมาตรฐาน 1 เบราว์เซอร์ 1 บทบาท ก่อนรัน Action ใน StaffController
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // อ่านค่าบทบาทปัจจุบันของเบราว์เซอร์จากเซสชัน
        var activeRole = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // หากเบราว์เซอร์ล็อกอินด้วยบทบาทอื่นที่ไม่ใช่พนักงาน (เช่น ลูกค้า เจ้าของร้าน บัญชี หรือไอที) ให้บังคับเปลี่ยนหน้าไปยังบทบาทนั้นทันที
        if (!string.IsNullOrEmpty(activeRole) && activeRole != "staff")
        {
            // ดึงเส้นทางหน้าหลักของบทบาทที่ล็อกอินอยู่
            var (targetAct, targetCtrl) = GetRoleHomeRoute(activeRole);
            // บังคับเปลี่ยนเส้นทางไปยังบทบาทปัจจุบัน
            context.Result = RedirectToAction(targetAct, targetCtrl);
            // จบการทำงานของตัวกรอง
            return;
        }
        // เรียกการทำงานพื้นฐานของคลาสแม่
        base.OnActionExecuting(context);
    }

    // ฟังก์ชันตรวจสอบสิทธิ์ว่าผู้ใช้งานล็อกอินด้วยบทบาทพนักงานประจำสาขาหรือไม่
    private bool IsAuthorizedStaff()
    {
        // อ่านค่าบทบาท (Role) ของผู้ใช้งานปัจจุบันที่บันทึกไว้ใน Session
        var role = HttpContext.Session.GetString("Role")?.ToLowerInvariant();
        // อ่านค่ารหัสพนักงาน StaffUserId จาก Session
        var staffId = HttpContext.Session.GetInt32("StaffUserId");
        // ตรวจสอบว่าบทบาทตรงกับ staff และมีรหัสพนักงานในเซสชัน
        return role == "staff" || staffId.HasValue;
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
            // คำนวณจำนวนสต็อกพร้อมขายสุทธิ
            var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
            // กำหนดระดับสต็อกปลอดภัยขั้นต่ำ
            var safety = p.Inventory?.SafetyStockLevel ?? 5;
            // ตรวจสอบว่าพร้อมขายมากกว่าระดับปลอดภัย
            return avail > safety;
        });
        // คำนวณจำนวนสินค้าที่สต็อกใกล้หมด (พร้อมขายมากกว่า 0 แต่ไม่เกินระดับสต็อกปลอดภัย)
        ViewBag.LowStockCount = allProducts.Count(p =>
        {
            // คำนวณจำนวนสต็อกพร้อมขายสุทธิ
            var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
            // กำหนดระดับสต็อกปลอดภัยขั้นต่ำ
            var safety = p.Inventory?.SafetyStockLevel ?? 5;
            // ตรวจสอบว่าพร้อมขายมากกว่า 0 แต่ไม่เกินระดับปลอดภัย
            return avail > 0 && avail <= safety;
        });
        // คำนวณจำนวนสินค้าที่หมดสต็อกชั่วคราว
        ViewBag.OutOfStockCount = allProducts.Count(p => ((p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)) <= 0);

        // ดึงรายชื่อหมวดหมู่สินค้าทั้งหมดที่ไม่ซ้ำกันเพื่อแสดงในตัวเลือกกรอง
        ViewBag.AllCategories = allProducts
            // ดึงชื่อหมวดหมู่สินค้าหรือค่าเริ่มต้นกรณีเป็นค่าว่าง
            .Select(p => p.Category?.CategoryName ?? "ทั่วไป")
            // กรองรายการหมวดหมู่ที่ไม่ซ้ำกัน
            .Distinct()
            // จัดเรียงตามตัวอักษร
            .OrderBy(c => c)
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();

        // เริ่มต้นการกรองข้อมูลสินค้า
        var filtered = allProducts.AsEnumerable();

        // ตรวจสอบและกรองตามคำค้นหา (ค้นหาจากชื่อ, SKU, บาร์โค้ด หรือชื่อซีรีส์)
        if (!string.IsNullOrWhiteSpace(search))
        {
            // ตัดช่องว่างและแปลงคำค้นหาเป็นตัวพิมพ์เล็ก
            string term = search.Trim().ToLowerInvariant();
            // ตัดเครื่องหมายขีด ชาร์ป และช่องว่างออกสำหรับการค้นหาแบบยืดหยุ่น
            string termNoHyphen = term.Replace("-", "").Replace("#", "").Replace(" ", "");
            // กรองรายการสินค้าตามเงื่อนไขคำค้นหา
            filtered = filtered.Where(p =>
                // ค้นหาตรงกับชื่อสินค้า
                (!string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(term)) ||
                // ค้นหาตรงกับรหัส SKU
                (!string.IsNullOrEmpty(p.Sku) && (p.Sku.ToLowerInvariant().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Sku.Replace("-", "").ToLowerInvariant().Contains(termNoHyphen)))) ||
                // ค้นหาตรงกับบาร์โค้ดสินค้า
                (!string.IsNullOrEmpty(p.Barcode) && (p.Barcode.ToLowerInvariant().Contains(term) || (!string.IsNullOrEmpty(termNoHyphen) && p.Barcode.Replace("-", "").ToLowerInvariant().Contains(termNoHyphen)))) ||
                // ค้นหาตรงกับชื่อซีรีส์สินค้า
                (!string.IsNullOrEmpty(p.SeriesName) && p.SeriesName.ToLowerInvariant().Contains(term)));
        }

        // ปรับแต่งและตรวจสอบค่าตัวกรองหมวดหมู่สินค้า
        string normalizedCategory = (string.IsNullOrWhiteSpace(category) ||
            // ตรวจสอบว่าเลือกตัวเลือกทั้งหมดหรือไม่
            string.Equals(category.Trim(), "all", StringComparison.OrdinalIgnoreCase) ||
            // ตรวจสอบข้อความภาษาไทยทุกหมวดหมู่
            category.Trim() == "ทุกหมวดหมู่")
            // กำหนดค่าตัวกรองเป็น all
            ? "all"
            // กำหนดชื่อหมวดหมู่ตามที่เลือก
            : category.Trim();
        // หากเลือกหมวดหมู่เฉพาะให้กรองตามหมวดหมู่นั้น
        if (normalizedCategory != "all")
        {
            // กรองรายการสินค้าตามชื่อหมวดหมู่ที่ระบุ
            filtered = filtered.Where(p => string.Equals(p.Category?.CategoryName ?? "ทั่วไป", normalizedCategory, StringComparison.OrdinalIgnoreCase));
        }

        // ตรวจสอบและกรองตามสถานะสต็อกสินค้า (ปกติ / ใกล้หมด / หมดสต็อก)
        string normalizedFilter = (stockFilter ?? "all").Trim().ToLowerInvariant();
        // ตรวจสอบตัวกรองสต็อกปกติ
        if (normalizedFilter == "normal")
        {
            // กรองเฉพาะสินค้าสต็อกปกติ
            filtered = filtered.Where(p =>
            {
                // คำนวณจำนวนสต็อกพร้อมขายสุทธิ
                var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
                // กำหนดระดับสต็อกปลอดภัยขั้นต่ำ
                var safety = p.Inventory?.SafetyStockLevel ?? 5;
                // ส่งคืนผลการตรวจสอบสต็อกปกติ
                return avail > safety;
            });
        }
        // ตรวจสอบตัวกรองสต็อกใกล้หมด
        else if (normalizedFilter == "low")
        {
            // กรองเฉพาะสินค้าสต็อกใกล้หมด
            filtered = filtered.Where(p =>
            {
                // คำนวณจำนวนสต็อกพร้อมขายสุทธิ
                var avail = (p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0);
                // กำหนดระดับสต็อกปลอดภัยขั้นต่ำ
                var safety = p.Inventory?.SafetyStockLevel ?? 5;
                // ส่งคืนผลการตรวจสอบสต็อกใกล้หมด
                return avail > 0 && avail <= safety;
            });
        }
        // ตรวจสอบตัวกรองหมดสต็อก
        else if (normalizedFilter == "out")
        {
            // กรองเฉพาะสินค้าหมดสต็อก
            filtered = filtered.Where(p => ((p.Inventory?.QuantityOnHand ?? 0) - (p.Inventory?.ReservedQuantity ?? 0)) <= 0);
        }
        // กรณีเงื่อนไขอื่นให้แสดงสินค้าทั้งหมด
        else
        {
            // ค่าเริ่มต้นแสดงสินค้าทุกระดับสต็อก
            normalizedFilter = "all";
        }

        // แปลงผลการกรองเป็นรายการข้อมูล List
        var filteredList = filtered.ToList();

        // ส่งรายชื่อสินค้าทั้งหมดและประวัติการรับเข้าคลังล่าสุดจากฐานข้อมูลจริงไปยังหน้าต่างรับสินค้า
        ViewBag.AllProductsForModal = allProducts.OrderBy(p => p.Name).ToList();
        // ดึงประวัติการรับสินค้าเข้าคลังล่าสุด
        ViewBag.RecentRestockLogs = _db.Systemlogs
            // กรองเฉพาะโมดูลคลังสินค้า
            .Where(l => l.Module == "Inventory")
            // เรียงลำดับจากล่าสุด
            .OrderByDescending(l => l.Timestamp)
            // ดึง 6 รายการล่าสุด
            .Take(6)
            // แปลงผลลัพธ์เป็นรายการ List
            .ToList();
        // กำหนดสถานะเปิดหน้าต่างรับสินค้าเข้าคลัง
        ViewBag.OpenReceiveModal = openReceive ?? false;

        // คำนวณการแบ่งหน้าสำหรับตารางสต็อกสินค้า (หน้าละ 10 รายการ)
        int pageSize = 10;
        // คำนวณจำนวนหน้าทั้งหมดจากจำนวนรายการที่กรองได้
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredList.Count / pageSize));
        // กำหนดหมายเลขหน้าปัจจุบันให้อยู่ในช่วงที่ถูกต้อง
        int currentPage = Math.Clamp(page, 1, totalPages);
        // ดึงข้อมูลสินค้าเฉพาะหน้าปัจจุบันตามขนาดหน้า
        var pagedProducts = filteredList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        // ส่งข้อมูลตัวกรองและการแบ่งหน้าไปยัง View
        ViewBag.SelectedStockFilter = normalizedFilter;
        // ส่งค่าคำค้นหาไปยัง View
        ViewBag.SelectedSearch = search?.Trim() ?? string.Empty;
        // ส่งค่าหมวดหมู่ที่เลือกไปยัง View
        ViewBag.SelectedCategory = normalizedCategory;
        // ส่งจำนวนรายการที่กรองได้ทั้งหมดไปยัง View
        ViewBag.FilteredCount = filteredList.Count;
        // ส่งหมายเลขหน้าปัจจุบันไปยัง View
        ViewBag.CurrentPage = currentPage;
        // ส่งจำนวนหน้าทั้งหมดไปยัง View
        ViewBag.TotalPages = totalPages;
        // ส่งขนาดหน้าไปยัง View
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

        // ประกาศตัวแปรเก็บข้อมูลใบจองพรีออเดอร์ที่ค้นพบ
        Preorder? foundPreorder = null;
        // ประกาศตัวแปรเก็บข้อมูลการแลกของรางวัลที่ค้นพบ
        Rewardredemption? foundRedemption = null;

        // ตรวจสอบว่าผู้ใช้ส่งข้อความค้นหา (query) เข้ามาหรือไม่ (รองรับการค้นหาโดยไม่ต้องพิมพ์ขีด - หรือ #)
        if (!string.IsNullOrWhiteSpace(query))
        {
            // ค้นหาใบจองพรีออเดอร์หรือของรางวัลจากคำค้นหา
            (foundPreorder, foundRedemption) = ResolveCounterSearch(query);
        }

        // กรณีไม่ได้ระบุคำค้นหา ให้แสดงใบจองล่าสุดเป็นตัวอย่าง
        if (foundPreorder == null && foundRedemption == null && string.IsNullOrWhiteSpace(query))
        {
            // ดึงข้อมูลใบสั่งจองล่าสุดเป็นตัวอย่าง
            var defaultPreorder = _db.Preorders
                // รวมรายการสินค้าในใบสั่งจอง
                .Include(p => p.Preorderitems)
                    // รวมข้อมูลสินค้าแต่ละรายการ
                    .ThenInclude(item => item.Product)
                // รวมข้อมูลผู้ใช้งานที่สั่งจอง
                .Include(p => p.User)
                // เรียงลำดับจากวันที่สร้างล่าสุด
                .OrderByDescending(p => p.CreatedAt)
                // ค้นหาใบจองที่พร้อมรับสินค้าหรือรอยืนยัน
                .FirstOrDefault(p => p.CurrentStatus == "ReadyForPickup" || p.CurrentStatus == "PendingConfirm")
                // กรณีไม่พบ ให้ดึงใบจองรายการล่าสุดที่มีในระบบ
                ?? _db.Preorders
                    // รวมรายการสินค้าในใบสั่งจอง
                    .Include(p => p.Preorderitems)
                        // รวมข้อมูลสินค้าแต่ละรายการ
                        .ThenInclude(item => item.Product)
                    // รวมข้อมูลผู้ใช้งานที่สั่งจอง
                    .Include(p => p.User)
                    // เรียงลำดับจากวันที่สร้างล่าสุด
                    .OrderByDescending(p => p.CreatedAt)
                    // ดึงรายการแรกที่พบ
                    .FirstOrDefault();

            // ตรวจสอบว่าพบใบจองเริ่มต้นหรือไม่
            if (defaultPreorder != null)
            {
                // ส่งข้อมูลใบจองเริ่มต้นไปยัง ViewBag
                ViewBag.SearchResult = defaultPreorder;
                // กำหนดข้อความสถานะการแสดงผลใบจองล่าสุด
                ViewBag.Status = $"กำลังแสดงรายการพรีออเดอร์ล่าสุด #{defaultPreorder.ReservationNumber} ({defaultPreorder.ContactPhone})";
            }
        }

        // เตรียมข้อมูลค้นหาด่วน (ทั้ง Preorder และ RewardRedemption) และเบอร์สมาชิก
        PopulateCounterRecentData();

        // ส่งคืนผลลัพธ์ View พร้อมโมเดลเคาน์เตอร์พรีออเดอร์
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

        // ดึงข้อความค้นหาจากรหัสใบจองหรือเบอร์โทรศัพท์
        string rawQuery = (data?.ReservationNumber ?? data?.Phone ?? "").Trim();

        // ตรวจสอบว่ามีการระบุข้อความค้นหาหรือไม่
        if (!string.IsNullOrEmpty(rawQuery))
        {
            // ดำเนินการค้นหาใบจองหรือของรางวัล
            ResolveCounterSearch(rawQuery);
        }
        // กรณีไม่ได้ระบุคำค้นหา
        else
        {
            // แจ้งเตือนให้กรอกรหัสหรือเบอร์โทรศัพท์
            ViewBag.Status = "กรุณากรอกรหัสพรีออเดอร์ รหัสแลกของรางวัล หรือเบอร์โทรศัพท์สำหรับค้นหา";
        }

        // เตรียมข้อมูลค้นหาด่วนและประวัติล่าสุด
        PopulateCounterRecentData();

        // ส่งคืนผลลัพธ์ View พร้อมข้อมูลโมเดลเดิม
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

    // ค้นหาใบสั่งจองพรีออเดอร์ด้วยรหัสใบจองเต็ม รหัส 4 หลักท้าย เบอร์โทรศัพท์ หรือชื่อลูกค้าอย่างแม่นยำ (ไม่จับคำค้นหาตัวอักษรเดี่ยวเช่น P)
    private Preorder? FindPreorderByQuery(string cleanQuery, string lowerQuery, string normalizedQuery)
    {
        // ดึงรายการคำสั่งจองพรีออเดอร์ทั้งหมดพร้อมรายการสินค้าและผู้จองเรียงจากล่าสุด
        var allPreorders = _db.Preorders
            // รวมรายการสินค้าในใบสั่งจอง
            .Include(p => p.Preorderitems)
                // รวมข้อมูลรายละเอียดสินค้าแต่ละรายการ
                .ThenInclude(item => item.Product)
            // รวมข้อมูลผู้ใช้งานที่สั่งจอง
            .Include(p => p.User)
            // เรียงลำดับจากวันที่สร้างล่าสุด
            .OrderByDescending(p => p.CreatedAt)
            // แปลงเป็นรายการ List
            .ToList();

        // ตรวจสอบว่าเป็นตัวเลขล้วนหรือไม่สำหรับการเทียบเลขท้ายหรือเบอร์โทร
        bool isDigitsOnly = !string.IsNullOrEmpty(normalizedQuery) && normalizedQuery.All(char.IsDigit);

        // ค้นหาใบสั่งจองที่ตรงตามเงื่อนไขรหัสใบจองเต็ม เลขท้าย 4 หลัก เบอร์โทรศัพท์ครบหลัก หรือชื่อลูกค้า
        return allPreorders.FirstOrDefault(p =>
        {
            // รหัสใบจองแบบตัดขีดและเครื่องหมาย #
            string codeNoHyphen = p.ReservationNumber.Replace("-", "").Replace("#", "").ToLowerInvariant();
            // เบอร์โทรศัพท์ที่ระบุในใบจองแบบตัดขีด
            string contactDigits = NormalizePhoneDigits(p.ContactPhone);
            // เบอร์โทรศัพท์ในบัญชีสมาชิกแบบตัดขีด
            string userDigits = NormalizePhoneDigits(p.User?.Phone);

            // 1. ตรงกับรหัสใบจองเต็ม (ทั้งแบบมีขีดและไม่มีขีด เช่น PO-2026-3304 หรือ PO20263304)
            if (p.ReservationNumber.Equals(cleanQuery.TrimStart('#'), StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบรหัสใบจองแบบตัดขีด
                || (!string.IsNullOrEmpty(normalizedQuery) && codeNoHyphen.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับรหัสใบจองเต็ม
                return true;
            }

            // 2. ตรงกับเลขท้าย 4 หลักของรหัสใบจอง (เช่น พิมพ์ 3304 เพื่อหา PO-2026-3304 แต่ไม่จับปี 2026)
            if (isDigitsOnly && normalizedQuery.Length == 4 &&
                // ตรวจสอบว่าลงท้ายด้วยขีดตามด้วยเลข 4 หลัก
                p.ReservationNumber.EndsWith("-" + normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับเลขท้าย 4 หลัก
                return true;
            }

            // 3. ตรงกับเบอร์โทรศัพท์ติดต่อ (ต้องพิมพ์ตัวเลขอย่างน้อย 9 หลัก หรือตรงทั้งเบอร์)
            if (isDigitsOnly && normalizedQuery.Length >= 9 &&
                // ตรวจสอบเบอร์โทรศัพท์ติดต่อหรือเบอร์ผู้ใช้งาน
                (contactDigits.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                 userDigits.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับเบอร์โทรศัพท์
                return true;
            }

            // 4. ตรงกับชื่อ-นามสกุลลูกค้า (ต้องพิมพ์อย่างน้อย 2 ตัวอักษร และไม่ใช่ตัวเลขล้วน)
            if (!isDigitsOnly && lowerQuery.Length >= 2 &&
                // ตรวจสอบชื่อลูกค้าว่ามีคำค้นหาอยู่ภายในหรือไม่
                p.User != null && !string.IsNullOrWhiteSpace(p.User.FullName) &&
                p.User.FullName.ToLowerInvariant().Contains(lowerQuery))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับชื่อลูกค้า
                return true;
            }

            // 5. ตรงกับอีเมลเต็มหรือชื่อหน้า @ ของอีเมลลูกค้า (ต้องพิมพ์อย่างน้อย 3 ตัวอักษร)
            if (!isDigitsOnly && lowerQuery.Length >= 3 && p.User != null && !string.IsNullOrWhiteSpace(p.User.Email))
            {
                // แยกส่วนชื่อผู้ใช้หน้าเครื่องหมาย @
                string emailLocal = p.User.Email.Split('@')[0].ToLowerInvariant();
                // ตรวจสอบว่าตรงกับอีเมลเต็มหรือชื่อหน้า @
                if (p.User.Email.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase) || emailLocal.Equals(lowerQuery, StringComparison.OrdinalIgnoreCase))
                {
                    // ส่งคืนค่าจริงเมื่อตรงกับอีเมลลูกค้า
                    return true;
                }
            }

            // หากไม่ตรงกับเงื่อนไขใดเลยให้คืนค่าเท็จ
            return false;
        });
    }

    // ค้นหารายการแลกของรางวัลด้วยรหัส RWD เต็ม เลขท้าย 4 หลัก เบอร์โทรศัพท์ หรือชื่อลูกค้าอย่างแม่นยำ
    private Rewardredemption? FindRedemptionByQuery(string cleanQuery, string lowerQuery, string normalizedQuery)
    {
        // ดึงบันทึกข้อมูลวันนัดรับและเบอร์ติดต่อของการแลกของรางวัล
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        // สร้างพจนานุกรมข้อมูลเมตาของการแลกของรางวัล
        var metaMap = BuildRedemptionMetaMap(rewardLogs);

        // ดึงรายการแลกของรางวัลทั้งหมดพร้อมข้อมูลของรางวัลและสมาชิก
        var allRedemptions = _db.Rewardredemptions
            // รวมข้อมูลของรางวัลที่แลก
            .Include(r => r.Reward)
            // รวมข้อมูลผู้ใช้งานที่แลก
            .Include(r => r.User)
            // เรียงลำดับจากวันที่แลกล่าสุด
            .OrderByDescending(r => r.RedeemedAt)
            // แปลงเป็นรายการ List
            .ToList();

        // ตรวจสอบว่าเป็นตัวเลขล้วนหรือไม่
        bool isDigitsOnly = !string.IsNullOrEmpty(normalizedQuery) && normalizedQuery.All(char.IsDigit);

        // ค้นหารายการแลกของรางวัลที่ตรงตามเงื่อนไขอย่างแม่นยำ
        return allRedemptions.FirstOrDefault(r =>
        {
            // ดึงข้อมูลเมตาของรหัสแลกของรางวัลนี้
            metaMap.TryGetValue(r.RedemptionCode, out var rMeta);
            // แปลงเบอร์โทรศัพท์ที่ระบุตอนแลกเป็นตัวเลขล้วน
            string metaPhone = NormalizePhoneDigits(rMeta?.ContactPhone);
            // แปลงเบอร์โทรศัพท์ของบัญชีสมาชิกเป็นตัวเลขล้วน
            string userPhone = NormalizePhoneDigits(r.User?.Phone);
            // รหัสแลกของรางวัลแบบตัดขีดและเครื่องหมาย #
            string codeNoHyphen = r.RedemptionCode.Replace("-", "").Replace("#", "").ToLowerInvariant();

            // 1. ตรงกับรหัสแลกของรางวัลเต็ม (เช่น RWD-2026-1234 หรือ RWD20261234)
            if (r.RedemptionCode.Equals(cleanQuery.TrimStart('#'), StringComparison.OrdinalIgnoreCase)
                // ตรวจสอบรหัสของรางวัลแบบตัดขีด
                || (!string.IsNullOrEmpty(normalizedQuery) && codeNoHyphen.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับรหัสแลกของรางวัลเต็ม
                return true;
            }

            // 2. ตรงกับเลขท้าย 4 หลักของรหัสแลกของรางวัล
            if (isDigitsOnly && normalizedQuery.Length == 4 &&
                // ตรวจสอบว่าลงท้ายด้วยขีดตามด้วยเลข 4 หลัก
                r.RedemptionCode.EndsWith("-" + normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับเลขท้าย 4 หลัก
                return true;
            }

            // 3. ตรงกับเบอร์โทรศัพท์ติดต่อ (ต้องพิมพ์ตัวเลขอย่างน้อย 9 หลัก)
            if (isDigitsOnly && normalizedQuery.Length >= 9 &&
                // ตรวจสอบเบอร์โทรศัพท์ติดต่อหรือเบอร์ผู้ใช้งาน
                (metaPhone.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                 userPhone.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับเบอร์โทรศัพท์
                return true;
            }

            // 4. ตรงกับชื่อ-นามสกุลลูกค้า (อย่างน้อย 2 ตัวอักษร)
            if (!isDigitsOnly && lowerQuery.Length >= 2 &&
                // ตรวจสอบชื่อลูกค้าว่ามีคำค้นหาอยู่ภายในหรือไม่
                r.User != null && !string.IsNullOrWhiteSpace(r.User.FullName) &&
                r.User.FullName.ToLowerInvariant().Contains(lowerQuery))
            {
                // ส่งคืนค่าจริงเมื่อตรงกับชื่อลูกค้า
                return true;
            }

            // 5. ตรงกับอีเมลเต็มหรือชื่อหน้า @ ของลูกค้า (อย่างน้อย 3 ตัวอักษร)
            if (!isDigitsOnly && lowerQuery.Length >= 3 && r.User != null && !string.IsNullOrWhiteSpace(r.User.Email))
            {
                // แยกส่วนชื่อผู้ใช้หน้าเครื่องหมาย @
                string emailLocal = r.User.Email.Split('@')[0].ToLowerInvariant();
                // ตรวจสอบว่าตรงกับอีเมลเต็มหรือชื่อหน้า @
                if (r.User.Email.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase) || emailLocal.Equals(lowerQuery, StringComparison.OrdinalIgnoreCase))
                {
                    // ส่งคืนค่าจริงเมื่อตรงกับอีเมลผู้แลก
                    return true;
                }
            }

            // หากไม่ตรงกับเงื่อนไขใดเลยให้คืนค่าเท็จ
            return false;
        });
    }

    // เตรียมข้อมูลทางลัดค้นหาด่วน (พรีออเดอร์ + แลกของรางวัล) เบอร์สมาชิก และรายละเอียดโปรโมชั่น/วันนัดรับ/สาเหตุการยกเลิก
    private void PopulateCounterRecentData()
    {
        // ดึงรายการคำสั่งจองพรีออเดอร์ล่าสุด 6 รายการสำหรับแสดงทางลัดค้นหาด่วน
        ViewBag.RecentPreorders = _db.Preorders
            // รวมรายการสินค้าและข้อมูลสินค้า
            .Include(p => p.Preorderitems).ThenInclude(item => item.Product)
            // รวมข้อมูลผู้ใช้งานที่สั่งจอง
            .Include(p => p.User)
            // เรียงลำดับจากวันที่สร้างล่าสุด
            .OrderByDescending(p => p.CreatedAt)
            // เลือก 6 รายการล่าสุด
            .Take(6)
            // แปลงเป็นรายการ List
            .ToList();

        // ดึงรายการขอแลกของรางวัลล่าสุด 6 รายการสำหรับแสดงทางลัดค้นหาด่วน
        ViewBag.RecentRedemptions = _db.Rewardredemptions
            // รวมข้อมูลของรางวัล
            .Include(r => r.Reward)
            // รวมข้อมูลผู้ใช้งาน
            .Include(r => r.User)
            // เรียงลำดับจากวันที่ทำรายการล่าสุด
            .OrderByDescending(r => r.RedeemedAt)
            // เลือก 6 รายการล่าสุด
            .Take(6)
            // แปลงเป็นรายการ List
            .ToList();

        // ดึงบันทึก Log ข้อมูลเพิ่มเติมสำหรับการแลกของรางวัลและโปรโมชั่น
        var rewardLogs = _db.Systemlogs.Where(l => l.Module == "reward.redeem").ToList();
        // ดึงบันทึก Log ข้อมูลโปรโมชั่นที่ผูกกับคำสั่งจองพรีออเดอร์
        var promoLogs = _db.Systemlogs.Where(l => l.Module == "preorder.promo").ToList();
        // ดึงบันทึก Log ข้อมูลการไม่รับสินค้า ไม่มารับสินค้า และการยกเลิกคำสั่งจอง
        var cancelLogs = _db.Systemlogs
            // กรองโมดูลที่เกี่ยวข้องกับการยกเลิก
            .Where(l => l.Module == "CounterPreorder" || l.Module == "preorder.cancel" || l.Module == "reward.cancel")
            // เรียงลำดับจากวันที่ล่าสุด
            .OrderByDescending(l => l.Timestamp)
            // แปลงเป็นรายการ List
            .ToList();
        // แปลงข้อมูลเมตาเดตาสำหรับวันนัดรับและเบอร์ติดต่อลงในพจนานุกรม
        ViewBag.RedemptionMetaMap = BuildRedemptionMetaMap(rewardLogs);
        // แปลงข้อมูลโปรโมชั่นส่วนลดลงในพจนานุกรมเพื่อนำไปคำนวณยอดเงินที่เคาน์เตอร์
        ViewBag.PreorderPromoMap = BuildPreorderPromoMap(promoLogs);
        // ส่งรายการบันทึกการไม่รับสินค้า/ไม่มารับสินค้า/ยกเลิกไปยัง ViewBag เพื่อแสดงเหตุผลตามจริง
        ViewBag.CancelLogs = cancelLogs;
    }

    // เมธอดสำหรับคืนสิทธิ์จำนวนการใช้งานโค้ดโปรโมชั่นกรณีคำสั่งจองถูกยกเลิกหรือไม่สำเร็จ
    private void RestorePromoUsageForPreorder(string reservationNumber)
    {
        // ค้นหาบันทึก Log โปรโมชั่นที่มีเลขที่ใบจองตรงกัน
        var promoLogs = _db.Systemlogs
            // กรองโมดูลโปรโมชั่นที่ระบุเลขที่ใบจองในเพย์โหลด
            .Where(l => l.Module == "preorder.promo" && l.DetailPayload != null && l.DetailPayload.Contains(reservationNumber))
            // แปลงเป็นรายการ List
            .ToList();
        // วนลูปตรวจสอบและปรับลดยอดการใช้งานของแต่ละโปรโมชั่นที่บันทึกไว้
        foreach (var promoLog in promoLogs)
        {
            // เริ่มต้นบล็อกการทำงานพร้อมดักจับข้อผิดพลาด
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
                        // ปรับลดยอดการใช้งานลง 1 ครั้งเพื่อคืนสิทธิ์
                        promo.CurrentUsageCount -= 1;
                    }
                }
            }
            // ดักจับข้อผิดพลาดกรณีแปลงข้อมูลเพย์โหลดล้มเหลว
            catch
            {
                // ละเว้นกรณีแปลงข้อมูล Payload ล้มเหลวเพื่อไม่ให้กระทบการทำงานหลัก
            }
        }
    }

    // Action สำหรับบันทึกการมารับสินค้า ชำระเงิน ตัดสต็อก และสะสมแต้ม (POST Method)
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
            // รวมรายการสินค้าในใบสั่งจอง
            .Include(p => p.Preorderitems)
                // รวมข้อมูลรายละเอียดสินค้าแต่ละรายการ
                .ThenInclude(pi => pi.Product)
            // ดึงข้อมูลใบสั่งจองที่ตรงกับรหัส
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
        // ตรวจสอบและดึงข้อมูลการจับคู่โปรโมชั่นของใบสั่งจอง
        var promoMap = BuildPreorderPromoMap(promoLogs);
        // ค้นหาข้อมูลโปรโมชั่นของใบจองปัจจุบัน
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
                // แปลงเป็นรายการ List เพื่อประมวลผลเงื่อนไข
                .ToList()
                // ค้นหาสมาชิกที่ตรงตามเบอร์โทรศัพท์หรืออีเมล
                .FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(u.Phone) && u.Phone.Replace("-", "").Replace(" ", "").Equals(cleanPhone, StringComparison.OrdinalIgnoreCase))
                    || u.Email.Equals(memberPhone.Trim(), StringComparison.OrdinalIgnoreCase));

            // ตรวจสอบว่าพบข้อมูลสมาชิกในระบบหรือไม่
            if (memberUser != null)
            {
                // คำนวณแต้มสะสม: 10% ของยอดเงินรวม (ขั้นต่ำ 10 แต้ม) และคูณ 2 หากใช้โปรโมชั่นแต้มสะสมสองเท่า
                pointsEarned = (int)Math.Max(10, Math.Round(preorder.TotalFinalPrice / 10));
                // ตรวจสอบว่าโปรโมชั่นให้คะแนนสะสมพิเศษสองเท่าหรือไม่
                if (preorderPromo?.IsDoublePoints == true)
                {
                    // คูณคะแนนสะสมเป็นสองเท่า
                    pointsEarned *= 2;
                }
                // คำนวณแต้มสะสมจากยอดเงิน แล้วนำไปบวกให้สมาชิก memberUser.Points += pointsEarned;
                memberUser.Points += pointsEarned;
                // กำหนดข้อความกำกับกรณีได้รับแต้มสองเท่า
                string doubleTag = preorderPromo?.IsDoublePoints == true ? " [โปรโมชั่นแต้ม x2]" : "";
                // กำหนดข้อความรายละเอียดการสะสมแต้มสำเร็จ พร้อมระบุชื่อสมาชิกและแต้มคงเหลือรวม
                memberLogMsg = $" (สะสมแต้มให้คุณ {memberUser.FullName ?? memberUser.Email}: +{pointsEarned} แต้ม{doubleTag} รวมปัจจุบัน {memberUser.Points} แต้ม)";
            }
            // กรณีพนักงานกรอกเบอร์โทรเข้ามาแต่ไม่พบข้อมูลสมาชิกในระบบ และยังไม่ได้ยืนยันข้ามการสะสมแต้ม
            else if (forceWithoutMember != true)
            {
                // บันทึกเบอร์โทรศัพท์ที่ไม่พบลงใน TempData
                TempData["MemberNotFoundPhone"] = memberPhone.Trim();
                // นำทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
                return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
            }
            // กรณีพนักงานยืนยันดำเนินการต่อแบบลูกค้าทั่วไปเมื่อไม่พบเบอร์สมาชิก
            else
            {
                // บันทึกข้อความแจ้งว่าไม่พบเบอร์สมาชิก และดำเนินการต่อแบบลูกค้าทั่วไป
                memberLogMsg = " (ไม่พบเบอร์สมาชิก ดำเนินการต่อแบบลูกค้าทั่วไป)";
            }
        }

        // เริ่มต้นการทำธุรกรรมฐานข้อมูล
        using var transaction = _db.Database.BeginTransaction();
        // เริ่มต้นบล็อกการทำงานพร้อมดักจับข้อผิดพลาด
        try
        {
            // ตรวจสอบสต็อกจริงในคลังก่อนส่งมอบและตัดยอดขาย
            foreach (var item in preorder.Preorderitems)
            {
                // ค้นหาข้อมูลสต็อกสินค้าในคลัง
                var invCheck = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                // ตรวจสอบว่ามีสต็อกเพียงพอสำหรับการส่งมอบหรือไม่
                if (invCheck == null || invCheck.QuantityOnHand < item.Quantity)
                {
                    // ยกเลิกธุรกรรมฐานข้อมูลทันทีเมื่อสต็อกไม่พอ
                    transaction.Rollback();
                    // แจ้งเตือนข้อผิดพลาดสต็อกไม่เพียงพอ
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product?.Name ?? $"#{item.ProductId}"}\" มีสต็อกคงเหลือในคลังไม่เพียงพอส่งมอบ (คงเหลือ {invCheck?.QuantityOnHand ?? 0} ชิ้น)";
                    // นำทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
                    return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
                }
            }

            // ปรับเปลี่ยนสถานะใบสั่งจองใน Entity ให้เป็น "Completed"
            preorder.CurrentStatus = "Completed";
            // บันทึกวันและเวลาปัจจุบันที่ลูกค้ามารับสินค้าจริง (CompletedAt)
            preorder.CompletedAt = DateTime.Now;

            // สร้างเลขที่ใบเสร็จรับเงินไม่ซ้ำกัน
            string receiptNo = "RCP" + DateTime.Now.ToString("yyMMddHHmmssfff");
            // ดึงรหัสพนักงานแคชเชียร์จากเซสชันของผู้ใช้งาน
            int cashierUserId = HttpContext.Session.GetInt32("StaffUserId")
                // กรณีไม่มีสิทธิ์พนักงานให้ตรวจสอบสิทธิ์เจ้าของร้าน
                ?? HttpContext.Session.GetInt32("OwnerUserId")
                // กรณีไม่มีให้ตรวจสอบรหัสผู้ใช้ทั่วไป
                ?? HttpContext.Session.GetInt32("UserId")
                // กำหนดค่าเริ่มต้นเป็นรหัส 2
                ?? 2;

            // สร้างออบเจกต์ใบเสร็จรับเงินใหม่
            var receipt = new Receipt
            {
                // กำหนดเลขที่ใบเสร็จรับเงิน
                ReceiptNo = receiptNo,
                // กำหนดรหัสใบสั่งจองที่เกี่ยวข้อง
                PreorderId = preorder.PreorderId,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                CustomerId = preorder.UserId,
                // กำหนดเบอร์โทรศัพท์ติดต่อของลูกค้า
                CustomerPhone = !string.IsNullOrWhiteSpace(memberPhone) ? memberPhone.Trim() : preorder.ContactPhone,
                // กำหนดรหัสพนักงานแคชเชียร์ผู้ออกใบเสร็จ
                CashierUserId = cashierUserId,
                // กำหนดมูลค่ายอดรวมก่อนหักส่วนลด
                SubtotalAmount = preorderPromo != null && preorderPromo.OriginalSubtotal > preorder.TotalFinalPrice
                    ? preorderPromo.OriginalSubtotal
                    : preorder.TotalFinalPrice,
                // กำหนดจำนวนเงินส่วนลดจากโปรโมชั่น
                DiscountAmount = preorderPromo?.DiscountAmount ?? 0.00m,
                // คำนวณภาษีมูลค่าเพิ่ม 7% จากยอดชำระสุทธิ
                VatAmount = Math.Round(preorder.TotalFinalPrice * 0.07m, 2),
                // กำหนดยอดชำระสุทธิหลังหักส่วนลด
                NetTotalAmount = preorder.TotalFinalPrice,
                // กำหนดข้อมูลบาร์โค้ดใบเสร็จจากรหัสใบสั่งจอง
                BarcodeData = preorder.ReservationNumber,
                // กำหนดวันเวลาที่ออกใบเสร็จรับเงิน
                IssuedAt = DateTime.Now
            };

            // เพิ่มข้อมูลใบเสร็จรับเงินลงในฐานข้อมูล
            _db.Receipts.Add(receipt);
            // บันทึกข้อมูลใบเสร็จรับเงินเพื่อนำรหัส ReceiptId ไปใช้งานต่อ
            _db.SaveChanges();

            // วนลูปสร้าง Receiptitems และตัดสต็อกสินค้าทีละรายการตามสินค้าที่อยู่ในใบสั่งจอง (ให้ MonthlyReport และคลังตรงกัน 100%)
            foreach (var item in preorder.Preorderitems)
            {
                // เพิ่มรายการสินค้าในใบเสร็จรับเงิน
                _db.Receiptitems.Add(new Receiptitem
                {
                    // ผูกรหัสใบเสร็จรับเงิน
                    ReceiptId = receipt.ReceiptId,
                    // ผูกรหัสสินค้า
                    ProductId = item.ProductId,
                    // กำหนดรายละเอียดชื่อสินค้า
                    ItemDescription = item.Product?.Name ?? $"สินค้า #{item.ProductId}",
                    // กำหนดจำนวนสินค้าที่ส่งมอบ
                    Quantity = item.Quantity,
                    // กำหนดราคาต่อหน่วย
                    UnitPrice = item.UnitPrice,
                    // คำนวณยอดรวมของรายการสินค้านี้
                    LineTotal = item.UnitPrice * item.Quantity
                });

                // ค้นหาข้อมูลสต็อกสินค้าในคลัง
                var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                // ตรวจสอบว่าพบข้อมูลสต็อกสินค้าหรือไม่
                if (inv != null)
                {
                    // ตัดยอดสินค้าคงคลังจริง
                    inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                    // ปรับลดยอดสินค้าที่ถูกจองไว้
                    inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - item.Quantity);
                    // ปรับปรุงวันเวลาที่แก้ไขข้อมูลล่าสุด
                    inv.UpdatedAt = DateTime.Now;
                }
            }

            // บันทึกการเปลี่ยนแปลงรายการสินค้าและการตัดสต็อกลงฐานข้อมูล
            _db.SaveChanges();
            // ยืนยันธุรกรรมฐานข้อมูลสำเร็จ
            transaction.Commit();

            // กำหนดข้อความแจ้งเตือนผลสำเร็จการส่งมอบและออกใบเสร็จ
            TempData["SuccessMessage"] = $"บันทึกการส่งมอบสินค้าและชำระเงินสำเร็จ! ออกใบเสร็จจริงเลขที่ #{receiptNo}{memberLogMsg}";
            // เปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }
        // ดักจับข้อผิดพลาดระหว่างการทำธุรกรรมฐานข้อมูล
        catch (Exception)
        {
            // ยกเลิกธุรกรรมฐานข้อมูลย้อนกลับทั้งหมด
            transaction.Rollback();
            // กำหนดข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกส่งมอบสินค้าและออกใบเสร็จ กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับไปยังหน้าเคาน์เตอร์พรีออเดอร์
            return RedirectToAction("CounterPreorder", new { query = preorder.ReservationNumber });
        }
    }

    // Action สำหรับบันทึกลูกค้าไม่มารับสินค้า (POST Method)
    [HttpPost]
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
            // รวมรายการสินค้าในใบสั่งจอง
            .Include(p => p.Preorderitems)
            // ค้นหาใบจองที่ตรงกับรหัส
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

        // คืนสิทธิ์การใช้งานโปรโมชั่นสำหรับใบสั่งจองนี้
        RestorePromoUsageForPreorder(preorder.ReservationNumber);

        // บันทึกประวัติและสาเหตุลงตาราง Systemlogs สำหรับเป็นหลักฐานการตรวจสอบ (Audit Trail)
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("NOSHOW"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "CounterPreorder",
            // ข้อความรายละเอียดลูกค้าไม่มารับสินค้า
            Message = $"ลูกค้าไม่มารับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason}" + (!string.IsNullOrWhiteSpace(remark) ? $" | หมายเหตุ: {remark.Trim()}" : ""),
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำรายการ
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
            // รวมรายการสินค้าในใบสั่งจอง
            .Include(p => p.Preorderitems)
            // ค้นหาใบจองที่ตรงกับรหัส
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

        // คืนสิทธิ์การใช้งานโปรโมชั่นสำหรับใบสั่งจองนี้
        RestorePromoUsageForPreorder(preorder.ReservationNumber);

        // บันทึกประวัติและสาเหตุการปฏิเสธลงตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("REJECT"),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "CounterPreorder",
            // ข้อความรายละเอียดลูกค้าปฏิเสธไม่รับสินค้า
            Message = $"ลูกค้าไม่รับสินค้า ใบจอง #{preorder.ReservationNumber} สาเหตุ: {reason}" + (!string.IsNullOrWhiteSpace(remark) ? $" | รายละเอียด: {remark.Trim()}" : ""),
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำรายการ
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
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
            // รวมข้อมูลของรางวัล
            .Include(r => r.Reward)
            // รวมข้อมูลผู้ใช้งาน
            .Include(r => r.User)
            // ค้นหารายการแลกที่ตรงกับรหัส
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
            // กำหนดรหัส Log
            LogCode = GenerateLogCode("RWD-DLV"),
            // กำหนดระดับความสำคัญเป็น INFO
            LogLevel = "INFO",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "CounterPreorder",
            // ข้อความรายละเอียดการส่งมอบของรางวัล
            Message = $"ส่งมอบของรางวัลรหัส #{redemption.RedemptionCode} ({redemption.Reward.Title}) ให้ลูกค้า {redemption.User.FullName ?? redemption.User.Email} เรียบร้อยแล้ว",
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำรายการ
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
    // ป้องกันการโจมตีแบบ CSRF
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับบันทึกกรณีลูกค้าไม่มารับของรางวัล
    public IActionResult NoShowRewardRedemption(long? redemptionId, string reason, string? remark)
    {
        // เรียกฟังก์ชันกลางยกเลิกรายการแลกของรางวัลโดยระบุสถานะไม่มารับ
        return CancelRewardRedemptionCore(redemptionId, reason, remark, isNoShow: true);
    }

    // Action สำหรับบันทึกลูกค้าปฏิเสธไม่รับของรางวัลหน้าร้าน พร้อมคืนแต้มและคืนสต็อก (POST Method)
    [HttpPost]
    // ป้องกันการโจมตีแบบ CSRF
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับบันทึกกรณีลูกค้าปฏิเสธไม่รับของรางวัล
    public IActionResult RejectRewardRedemption(long? redemptionId, string reason, string? remark)
    {
        // เรียกฟังก์ชันกลางยกเลิกรายการแลกของรางวัลโดยระบุสถานะปฏิเสธไม่รับ
        return CancelRewardRedemptionCore(redemptionId, reason, remark, isNoShow: false);
    }

    // ฟังก์ชันกลางสำหรับยกเลิกรายการแลกของรางวัลทั้งกรณีไม่รับสินค้าและไม่มารับสินค้า คืนแต้ม + คืนสต็อก + บันทึก Systemlog
    private IActionResult CancelRewardRedemptionCore(long? redemptionId, string reason, string? remark, bool isNoShow)
    {
        // ตรวจสอบสิทธิ์ว่าผู้ใช้งานมีสิทธิ์พนักงานหรือไม่
        if (!IsAuthorizedStaff())
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่มีสิทธิ์
            TempData["ErrorMessage"] = "ไม่มีสิทธิ์เข้าถึง";
            // เปลี่ยนเส้นทางกลับหน้าล็อกอิน
            return RedirectToAction("Login", "Account");
        }

        // ตรวจสอบความถูกต้องของรหัสรายการแลกของรางวัล
        if (!redemptionId.HasValue || redemptionId.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัส
            TempData["ErrorMessage"] = "ไม่พบรหัสรายการแลกของรางวัล";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ค้นหารายการแลกของรางวัลในฐานข้อมูลพร้อมข้อมูล Reward และ User
        var redemption = _db.Rewardredemptions
            // รวมข้อมูลของรางวัล
            .Include(r => r.Reward)
            // รวมข้อมูลผู้ใช้งาน
            .Include(r => r.User)
            // ค้นหารายการที่ตรงกับรหัส
            .FirstOrDefault(r => r.RedemptionId == redemptionId.Value);

        // ตรวจสอบว่าพบรายการแลกของรางวัลหรือไม่
        if (redemption == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการแลกของรางวัลในระบบ";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์
            return RedirectToAction("CounterPreorder");
        }

        // ตรวจสอบว่ารายการนี้ปิดสถานะไปแล้วหรือไม่
        if (redemption.Status == "Completed" || redemption.Status == "Cancelled")
        {
            // กำหนดข้อความแจ้งเตือนว่ารายการนี้ปิดไปแล้ว
            TempData["ErrorMessage"] = $"รายการแลกของรางวัล #{redemption.RedemptionCode} ได้ดำเนินการปิดสถานะไปแล้ว";
            // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัส
            return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
        }

        // เปลี่ยนสถานะเป็น Cancelled และคืนทั้งแต้มสะสมกับสต็อกของรางวัลให้ลูกค้าและคลังสินค้า
        redemption.Status = "Cancelled";
        // ตรวจสอบว่ามีข้อมูลผู้ใช้งานเพื่อคืนแต้มสะสมหรือไม่
        if (redemption.User != null)
        {
            // คืนแต้มสะสมที่ใช้แลกของรางวัลให้สมาชิก
            redemption.User.Points += redemption.PointsSpent;
        }
        // ตรวจสอบว่ามีข้อมูลของรางวัลเพื่อคืนสต็อกหรือไม่
        if (redemption.Reward != null)
        {
            // เพิ่มสต็อกของรางวัลคืนคลัง 1 ชิ้น
            redemption.Reward.StockQuantity += 1;
        }

        // กำหนดป้ายข้อความการทำงานตามกรณีไม่มารับหรือปฏิเสธไม่รับ
        string actionLabel = isNoShow ? "ลูกค้าไม่มารับของรางวัล" : "ลูกค้าไม่รับของรางวัล";
        // กำหนดคำนำหน้ารหัสบันทึกกิจกรรม
        string logPrefix = isNoShow ? "NOSHOW-R" : "REJECT-R";

        // บันทึกประวัติการยกเลิกรายการแลกของรางวัลลงในตาราง Systemlogs
        _db.Systemlogs.Add(new Systemlog
        {
            // กำหนดรหัส Log
            LogCode = GenerateLogCode(logPrefix),
            // กำหนดระดับความสำคัญเป็น WARNING
            LogLevel = "WARNING",
            // กำหนดโมดูลที่เกี่ยวข้อง
            Module = "CounterPreorder",
            // ข้อความรายละเอียดการยกเลิกของรางวัล
            Message = $"{actionLabel} รหัสแลก #{redemption.RedemptionCode} ({redemption.Reward?.Title}) สาเหตุ: {reason}" + (!string.IsNullOrWhiteSpace(remark) ? $" | รายละเอียด: {remark.Trim()}" : "") + $" | คืน {redemption.PointsSpent:N0} แต้มและคืนสต็อกแล้ว",
            // รหัสผู้ใช้งานที่ทำรายการ
            UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId"),
            // วันเวลาที่ทำรายการ
            Timestamp = DateTime.Now
        });

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จผ่าน TempData
        TempData["SuccessMessage"] = $"บันทึก{actionLabel}เรียบร้อยแล้ว (#{redemption.RedemptionCode}) ระบบคืน {redemption.PointsSpent:N0} แต้มสะสมและคืนสต็อกของรางวัลเรียบร้อยแล้ว";
        // เปลี่ยนเส้นทางกลับหน้าเคาน์เตอร์พร้อมระบุรหัสของรางวัล
        return RedirectToAction("CounterPreorder", new { query = redemption.RedemptionCode });
    }

    // Action เปลี่ยนเส้นทางจากเส้นทางเดิมไปยังหน้าสต็อกสินค้าพร้อมเปิดหน้าต่างรับสินค้าเข้าสต็อก
    [HttpGet]
    // แอ็กชันสำหรับเปิดหน้าต่างรับสินค้าเข้าสต็อก
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
    // ป้องกันการโจมตีแบบ Cross-Site Request Forgery (CSRF)
    [ValidateAntiForgeryToken]
    // แอ็กชันสำหรับประมวลผลการรับสินค้าเข้าสต็อก
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
            // กำหนดข้อความแจ้งเตือนให้ระบุรหัสสินค้า
            TempData["ErrorMessage"] = "กรุณาสแกนบาร์โค้ดหรือเลือกรหัสสินค้าที่ต้องการรับเข้าคลัง";
            // เปลี่ยนเส้นทางกลับหน้าคลังสินค้าพร้อมเปิดหน้าต่างรับสินค้า
            return RedirectToAction("Index", new { page = currentPage ?? 1, openReceive = true });
        }

        // ตรวจสอบว่าจำนวนสินค้าที่ระบุถูกต้องและมากกว่าศูนย์หรือไม่
        if (!quantity.HasValue || quantity.Value <= 0)
        {
            // กำหนดข้อความแจ้งเตือนให้ระบุจำนวนอย่างน้อย 1 ชิ้น
            TempData["ErrorMessage"] = "กรุณาระบุจำนวนสินค้าที่รับเข้าอย่างน้อย 1 ชิ้น";
            // เปลี่ยนเส้นทางกลับหน้าคลังสินค้าพร้อมเปิดหน้าต่างรับสินค้า
            return RedirectToAction("Index", new { page = currentPage ?? 1, openReceive = true });
        }

        // กำหนดจำนวนสินค้าที่ต้องการรับเข้า
        int addQty = quantity.Value;

        // ค้นหาข้อมูลสินค้าในตาราง Products ด้วยรหัสสินค้าหรือบาร์โค้ด (รองรับการพิมพ์รหัสแบบไม่ใส่ขีดลบ)
        string normalizedSku = cleanSku.Replace("-", "").Replace("#", "").Replace(" ", "").ToLower();
        // ดึงรายการสินค้าที่ตรงกับรหัส SKU หรือบาร์โค้ด
        var product = _db.Products
            // รวมข้อมูลสต็อกสินค้า
            .Include(p => p.Inventory)
            // แปลงเป็นรายการ List
            .ToList()
            // ค้นหาสินค้าที่ตรงตามเงื่อนไขรหัส SKU หรือบาร์โค้ด
            .FirstOrDefault(p => p.Sku.Equals(cleanSku, StringComparison.OrdinalIgnoreCase)
                              || (p.Barcode != null && p.Barcode.Equals(cleanSku, StringComparison.OrdinalIgnoreCase))
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Sku.Replace("-", "").ToLower() == normalizedSku)
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Barcode != null && p.Barcode.Replace("-", "").ToLower() == normalizedSku)
                              || (!string.IsNullOrEmpty(normalizedSku) && p.Sku.Replace("-", "").ToLower().Contains(normalizedSku)));

        // ตรวจสอบว่าพบข้อมูลสินค้าตรงกับรหัสที่สแกนหรือกรอกหรือไม่
        if (product != null)
        {
            // ปลดสถานะพรีออเดอร์เดิมเพื่อให้สินค้าพร้อมขายตรงกันทั้งฝั่งลูกค้า พนักงาน และเจ้าของร้าน
            product.IsPreorder = false;
            // ดึงข้อมูลสต็อกคงคลังของสินค้า
            var inv = product.Inventory;
            // กรณีสินค้านี้ยังไม่มีเรคคอร์ดในตาราง Inventories
            if (inv == null)
            {
                // สร้างออบเจกต์สต็อกสินค้าใหม่
                inv = new Inventory
                {
                    // ผูกรหัสสินค้า
                    ProductId = product.ProductId,
                    // กำหนดจำนวนสินค้าในคลัง
                    QuantityOnHand = addQty,
                    // กำหนดจำนวนที่ถูกจองเริ่มต้นเป็นศูนย์
                    ReservedQuantity = 0,
                    // กำหนดระดับสต็อกปลอดภัยขั้นต่ำ
                    SafetyStockLevel = 5,
                    // กำหนดตำแหน่งจัดเก็บในคลัง
                    StorageLocation = "โซน A-01",
                    // บันทึกวันเวลาที่รับเข้าล่าสุด
                    LastRestockedAt = DateTime.Now,
                    // บันทึกวันเวลาที่ปรับปรุงข้อมูล
                    UpdatedAt = DateTime.Now
                };
                // เพิ่มข้อมูลสต็อกใหม่ลงในฐานข้อมูล
                _db.Inventories.Add(inv);
            }
            // กรณีมีข้อมูลสต็อกเดิมอยู่แล้วในระบบ
            else
            {
                // เพิ่มจำนวนสต็อกสินค้าในคลัง
                inv.QuantityOnHand += addQty;
                // ปรับปรุงวันเวลาที่รับเข้าสต็อกล่าสุด
                inv.LastRestockedAt = DateTime.Now;
                // ปรับปรุงวันเวลาที่แก้ไขข้อมูล
                inv.UpdatedAt = DateTime.Now;
            }

            // คำนวณจำนวนสต็อกพร้อมขายสุทธิหลังหักยอดจองเพื่อให้ตรงกันทุกบทบาท
            int availableAfterRestock = Math.Max(0, inv.QuantityOnHand - inv.ReservedQuantity);

            // บันทึกประวัติการรับสินค้าเข้าสต็อกลงในตาราง Systemlogs
            _db.Systemlogs.Add(new Systemlog
            {
                // กำหนดรหัส Log
                LogCode = GenerateLogCode("RESTOCK"),
                // กำหนดระดับความสำคัญเป็น INFO
                LogLevel = "INFO",
                // กำหนดโมดูลที่เกี่ยวข้อง
                Module = "Inventory",
                // ข้อความรายละเอียดการรับสินค้าเข้าคลัง
                Message = $"รับสินค้า {product.Name} (รหัส: {product.Sku}) จำนวน +{addQty} ชิ้น เข้าสต็อก คงเหลือพร้อมขาย {availableAfterRestock} ชิ้น",
                // รหัสผู้ใช้งานที่ทำรายการ
                UserId = HttpContext.Session.GetInt32("StaffUserId") ?? HttpContext.Session.GetInt32("OwnerUserId") ?? HttpContext.Session.GetInt32("UserId") ?? 2,
                // วันเวลาที่ทำรายการ
                Timestamp = DateTime.Now
            });

            // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
            _db.SaveChanges();

            // กำหนดข้อความแจ้งผลสำเร็จผ่าน TempData เพื่อแสดงผลให้พนักงานทราบ
            TempData["SuccessMessage"] = $"รับสินค้า {product.Name} ({product.Sku}) จำนวน +{addQty} ชิ้น เข้าคลังเรียบร้อยแล้ว (คงเหลือพร้อมขาย: {availableAfterRestock} ชิ้น)";
        }
        // กรณีค้นหาไม่พบรหัสสินค้าหรือบาร์โค้ดในฐานข้อมูลจริง
        else
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรหัสสินค้า
            TempData["ErrorMessage"] = $"ไม่พบรหัสสินค้าหรือบาร์โค้ด \"{cleanSku}\" ในระบบ กรุณาตรวจสอบความถูกต้อง";
        }

        // โหลดกลับมาที่หน้าสต็อกสินค้าหลักพร้อมคงเลขหน้าเดิมไว้ (ฟอร์ม MVC มาตรฐาน)
        return RedirectToAction("Index", new { page = currentPage ?? 1 });
    }


    // ชื่อคีย์เซสชันสำหรับจัดเก็บรายการรหัสคูปองที่ผู้ใช้เก็บสะสมไว้
    private const string CollectedPromoSessionKey = "CollectedPromoCodes";
    // ชื่อคีย์เซสชันสำหรับจัดเก็บรหัสคูปองที่เลือกใช้งานในตะกร้า
    private const string AppliedPromoSessionKey = "AppliedPromoCode";

    // ออบเจกต์ CultureInfo สำหรับการจัดรูปแบบวันเวลาภาษาไทย
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    // ฟังก์ชันคำนวณวันปัจจุบันตามปฏิทินเกรกอเรียน (ค.ศ.)
    private static DateOnly TodayGregorian() =>
        // สร้างออบเจกต์ DateOnly จากปี เดือน วันปัจจุบัน
        new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);

    // ฟังก์ชัน GenerateLogCode สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string GenerateLogCode(string prefix)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix)
            // กรณีคำนำหน้าว่างให้ใช้ LOG เป็นค่าเริ่มต้น
            ? "LOG"
            // กรณีระบุคำนำหน้าให้ตัดช่องว่างและเครื่องหมายขีดท้าย
            : prefix.Trim().TrimEnd('-').ToUpperInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (cleanPrefix.Length > 11) cleanPrefix = cleanPrefix[..11];
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string code = $"{cleanPrefix}-{DateTime.Now:yyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return code.Length <= 30 ? code : code[..30];
    }

    // ฟังก์ชัน IsValidPhoneNumber สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static bool IsValidPhoneNumber(string? phone)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string digits = NormalizePhoneDigits(phone);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return digits.Length >= 9 && digits.Length <= 15;
    }

    // ฟังก์ชัน NormalizeGregorianDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static DateOnly NormalizeGregorianDate(DateOnly date)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int year = date.Year;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (year > 0 && year < 1900)
        {
            // แปลงปี พ.ศ. เป็น ค.ศ.
            year += 543;
        }
        // ตรวจสอบเงื่อนไขกรณีถัดไปเมื่อไม่ตรงกับเงื่อนไขก่อนหน้า
        else if (year >= 2400)
        {
            // แปลงปี พ.ศ. เกิน 2400 เป็น ค.ศ.
            year -= 543;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new DateOnly(year, date.Month, day);
    }

    // ฟังก์ชัน FormatThaiDate สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatThaiDate(DateOnly date, string format = "d MMM yyyy")
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var gregorian = NormalizeGregorianDate(date);
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return gregorian.ToDateTime(TimeOnly.MinValue).ToString(format, ThaiCulture);
    }

    // ฟังก์ชันจัดรูปแบบวันที่เป็นสตริง ISO yyyy-MM-dd
    private static string FormatIsoDate(DateOnly date) =>
        // แปลงวันที่เป็น ค.ศ. และจัดรูปแบบตามมาตรฐาน ISO
        NormalizeGregorianDate(date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ฟังก์ชัน FormatPromoBenefitSummary สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatPromoBenefitSummary(Promotion promo)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal val = promo.DiscountValue ?? 0m;
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return (promo.PromoType ?? "percent").Trim().ToLowerInvariant() switch
        {
            // กรณีโปรโมชั่นแบบส่วนลดเปอร์เซ็นต์
            "percent" or "percentage" => $"ลด {val:0.#}%",
            // กรณีโปรโมชั่นซื้อ 1 แถม 1
            "bogo" => val > 0 ? $"ซื้อ 1 แถม 1 (สูงสุด ฿{val:N0})" : "ซื้อ 1 แถม 1",
            // กรณีโปรโมชั่นรับคะแนนสะสมพิเศษ 2 เท่า
            "double_points" => "รับแต้มสะสม x2",
            // กรณีโปรโมชั่นชุดเซ็ตสุดคุ้ม
            "bundle" => $"ชุดเซ็ตสุดคุ้ม ลด ฿{val:N0}",
            // กรณีโปรโมชั่นส่วนลดทั่วไป
            _ => $"ลด ฿{val:N0}"
        };
    }

    // ฟังก์ชัน SaveUploadedImage สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string? SaveUploadedImage(IFormFile? imageFile, string webRootPath, string defaultPrefix, string? fallbackUrl)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (imageFile == null || imageFile.Length <= 0 || string.IsNullOrWhiteSpace(webRootPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return fallbackUrl;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
        {
            // กำหนดนามสกุลไฟล์เริ่มต้นเป็น .png
            ext = ".png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var imagesDir = Path.Combine(webRootPath, "media", "images");
        // สร้างโฟลเดอร์สำหรับจัดเก็บไฟล์หากยังไม่มี
        Directory.CreateDirectory(imagesDir);

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var rawName = Path.GetFileNameWithoutExtension(imageFile.FileName)
            // ตัดช่องว่างส่วนเกินหัวท้าย
            .Trim()
            // แปลงเป็นตัวพิมพ์เล็กมาตรฐาน
            .ToLowerInvariant()
            // แทนที่ช่องว่างด้วยขีดล่าง
            .Replace(" ", "_");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var safePrefix = string.IsNullOrWhiteSpace(rawName) ? defaultPrefix : rawName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var fullPath = Path.Combine(imagesDir, fileName);

        // เปิดสตรีมไฟล์เพื่อเขียนข้อมูลรูปลงในดิสก์
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            // บันทึกไฟล์รูปลงในสตรีมปลายทาง
            imageFile.CopyTo(stream);
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "/media/images/" + fileName;
    }

    // ฟังก์ชันปรับมาตรฐานรหัสคูปองหรือข้อความให้เป็นตัวพิมพ์ใหญ่ตัดช่องว่าง
    private static string NormalizeCode(string? code) =>
        // แปลงรหัสเป็นตัวพิมพ์ใหญ่
        (code ?? string.Empty).Trim().ToUpperInvariant();

    // ฟังก์ชันปรับมาตรฐานรหัสคูปองโดยตัดเครื่องหมายขีดและช่องว่าง
    private static string NormalizeCodeNoHyphen(string? code) =>
        // ตัดเครื่องหมายขีด ชาร์ป และช่องว่างออก
        NormalizeCode(code).Replace("-", "").Replace("#", "").Replace(" ", "");

    // ฟังก์ชันแปลงประเภทโปรโมชั่นเป็นป้ายชื่อภาษาไทย
    private static string GetPromoTypeThaiLabel(string? promoType) =>
        // ตรวจสอบประเภทโปรโมชั่นและส่งคืนชื่อภาษาไทย
        (promoType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ป้ายชื่อโปรโมชั่นส่วนลดเปอร์เซ็นต์
            "percent" or "percentage" => "ส่วนลดเปอร์เซ็นต์",
            // ป้ายชื่อโปรโมชั่นส่วนลดมูลค่าคงที่
            "fixed" => "ส่วนลดมูลค่าคงที่",
            // ป้ายชื่อโปรโมชั่นซื้อ 1 แถม 1
            "bogo" => "ซื้อ 1 แถม 1",
            // ป้ายชื่อโปรโมชั่นชุดเซ็ต
            "bundle" => "ชุดเซ็ตสุดคุ้ม",
            // ป้ายชื่อโปรโมชั่นแต้มสองเท่า
            "double_points" => "แต้มสะสมสองเท่า",
            // ป้ายชื่อโปรโมชั่นทั่วไป
            _ => "โปรโมชั่นพิเศษ"
        };

    // ฟังก์ชันแปลงขอบเขตสินค้าที่เข้าร่วมโปรโมชั่นเป็นป้ายชื่อภาษาไทย
    private static string GetScopeThaiLabel(string? scope) =>
        // ตรวจสอบขอบเขตสินค้าและส่งคืนชื่อภาษาไทย
        (scope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            // ขอบเขตสินค้าทั้งหมด
            "all" or "" => "สินค้าทุกหมวดหมู่ในร้าน",
            // ขอบเขตสินค้าหมวดฟิกเกอร์
            "figures" => "สินค้าหมวดฟิกเกอร์",
            // ขอบเขตสินค้าลดล้างสต็อก
            "clearance" => "สินค้าลดล้างสต๊อก / พร้อมส่ง",
            // ขอบเขตสินค้าตามที่ระบุ
            _ => scope!
        };

    // ฟังก์ชัน MatchesScope สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static bool MatchesScope(Product? product, string? scope)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (product == null) return false;

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalizedScope = (scope ?? "all").Trim().ToLowerInvariant();
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrEmpty(normalizedScope) || normalizedScope == "all")
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return true;
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (normalizedScope == "figures")
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string catName = product.Category?.CategoryName ?? string.Empty;
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            string slug = (product.Category?.Slug ?? string.Empty).ToLowerInvariant();
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return catName.Contains("ฟิกเกอร์")
                // ตรวจสอบสลักที่มีคำว่า figure
                || slug.Contains("figure")
                // ตรวจสอบหมวดหมู่ของสะสมลิมิเต็ด
                || slug == "limited-collectibles";
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (normalizedScope == "clearance")
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            int available = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return !product.IsPreorder && available > 0;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return true;
    }

    // ฟังก์ชันประเมินและคำนวณส่วนลดโปรโมชั่นกับรายการสินค้าในตะกร้า
    private static PromoEvaluationResult EvaluateCartPromotion(
        // พารามิเตอร์ข้อมูลโปรโมชั่นที่ต้องการประเมิน
        Promotion? promo,
        // พารามิเตอร์รายการสินค้าในตะกร้าของลูกค้า
        IReadOnlyCollection<Cartitem> cartItems,
        // พารามิเตอร์วันที่จำลองสำหรับทดสอบ
        DateOnly? todayOverride = null)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var today = todayOverride ?? TodayGregorian();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal subtotal = cartItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);

        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (promo == null)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดข้อความแจ้งข้อผิดพลาด
                ErrorMessage = "ไม่พบรหัสโปรโมชั่นนี้ในระบบ กรุณาตรวจสอบตัวสะกดอีกครั้ง"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.IsActive != true)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ปิดการใช้งานชั่วคราวแล้ว"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var normalizedStart = NormalizeGregorianDate(promo.StartDate);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var normalizedEnd = NormalizeGregorianDate(promo.EndDate);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today < normalizedStart)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัส \"{promo.VoucherCode}\" จะเริ่มใช้งานได้ตั้งแต่วันที่ {FormatThaiDate(normalizedStart)}"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (today > normalizedEnd)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" หมดอายุการใช้งานแล้ว"
            };
        }

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (promo.UsageLimit.HasValue && promo.CurrentUsageCount >= promo.UsageLimit.Value)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"รหัสโปรโมชั่น \"{promo.VoucherCode}\" ถูกใช้ครบจำนวนสิทธิ์แล้ว"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var eligibleItems = cartItems.Where(c => MatchesScope(c.Product, promo.ApplicableScope)).ToList();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal eligibleSubtotal = eligibleItems.Sum(c => (c.Product?.RetailPrice ?? 0m) * c.Quantity);
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        int eligibleQty = eligibleItems.Sum(c => c.Quantity);

        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (eligibleItems.Count == 0 || eligibleSubtotal <= 0m)
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"โค้ด \"{promo.VoucherCode}\" ใช้ได้เฉพาะ{GetScopeThaiLabel(promo.ApplicableScope)} แต่ไม่มีสินค้าที่ร่วมรายการในตะกร้า"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal minSpend = promo.MinimumSpend ?? 0m;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (subtotal < minSpend)
        {
            // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
            decimal diff = minSpend - subtotal;
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new PromoEvaluationResult
            {
                // กำหนดสถานะความถูกต้องเป็นเท็จ
                IsValid = false,
                // กำหนดข้อมูลโปรโมชั่น
                Promotion = promo,
                // กำหนดยอดรวมราคาสินค้าในตะกร้า
                SubtotalAmount = subtotal,
                // กำหนดยอดรวมสินค้าที่เข้าเกณฑ์โปรโมชั่น
                EligibleSubtotal = eligibleSubtotal,
                // ประมวลผลคำสั่งของระบบ
                ErrorMessage = $"ยอดรวมในตะกร้ายังไม่ถึงขั้นต่ำ ฿{minSpend:N0} สำหรับโค้ด \"{promo.VoucherCode}\" (ขาดอีก ฿{diff:N0})"
            };
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string promoType = (promo.PromoType ?? "percent").Trim().ToLowerInvariant();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal discountValue = promo.DiscountValue ?? 0m;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        decimal discount = 0m;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        bool isDoublePoints = false;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string summary;

        // แยกการทำงานตามค่าตัวแปรที่ระบุ
        switch (promoType)
        {
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "percent":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "percentage":
                // คำนวณส่วนลดเปอร์เซ็นต์จากยอดเข้าเกณฑ์
                discount = Math.Round(eligibleSubtotal * (discountValue / 100m), 2, MidpointRounding.AwayFromZero);
                // จำกัดส่วนลดไม่ให้เกินยอดเข้าเกณฑ์
                discount = Math.Clamp(discount, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลด
                summary = $"ลด {discountValue:0.#}% (ประหยัด ฿{discount:N0})";
                // สิ้นสุดกรณีการทำงาน
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bogo":
                // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
                if (eligibleQty < 2)
                {
                    // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
                    return new PromoEvaluationResult
                    {
                        // กำหนดสถานะความถูกต้องเป็นเท็จ
                        IsValid = false,
                        // กำหนดข้อมูลโปรโมชั่น
                        Promotion = promo,
                        // กำหนดยอดรวมราคาสินค้าในตะกร้า
                        SubtotalAmount = subtotal,
                        // กำหนดยอดรวมสินค้าที่เข้าเกณฑ์โปรโมชั่น
                        EligibleSubtotal = eligibleSubtotal,
                        // กำหนดข้อความแจ้งเตือนเงื่อนไข 2 ชิ้น
                        ErrorMessage = $"โค้ดซื้อ 1 แถม 1 ({promo.VoucherCode}) ต้องมีสินค้าที่ร่วมรายการอย่างน้อย 2 ชิ้นในตะกร้า"
                    };
                }
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal cheapestUnit = eligibleItems.Min(c => c.Product?.RetailPrice ?? 0m);
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                decimal bogoCap = discountValue > 0m ? discountValue : cheapestUnit;
                // จำกัดส่วนลดซื้อ 1 แถม 1 ไม่ให้เกินยอดเข้าเกณฑ์
                discount = Math.Clamp(bogoCap, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลดซื้อ 1 แถม 1
                summary = $"สิทธิพิเศษซื้อ 1 แถม 1 (ส่วนลด ฿{discount:N0})";
                // สิ้นสุดกรณีการทำงาน
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "double_points":
                // กำหนดสถานะรับแต้มสองเท่า
                isDoublePoints = true;
                // กำหนดข้อความสรุปสิทธิประโยชน์แต้มสองเท่า
                summary = "รับแต้มสะสม x2 เมื่อชำระเงินที่หน้าร้าน";
                // สิ้นสุดกรณีรับแต้มสองเท่า
                break;

            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "fixed":
            // กรณีเงื่อนไขตรงกับค่าที่กำหนด
            case "bundle":
            // กรณีส่วนลดมูลค่าคงที่หรือกรณีเริ่มต้น
            default:
                // คำนวณส่วนลดคงที่ท้ายบิล
                discount = Math.Clamp(discountValue, 0m, eligibleSubtotal);
                // กำหนดข้อความสรุปส่วนลดท้ายบิล
                summary = $"ส่วนลดท้ายบิล ฿{discount:N0}";
                // สิ้นสุดกรณีส่วนลดคงที่
                break;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return new PromoEvaluationResult
        {
            // กำหนดสถานะความถูกต้องเป็นจริง
            IsValid = true,
            // กำหนดข้อมูลโปรโมชั่น
            Promotion = promo,
            // กำหนดยอดรวมราคาสินค้าในตะกร้า
            SubtotalAmount = subtotal,
            // กำหนดยอดสินค้าที่เข้าเกณฑ์โปรโมชั่น
            EligibleSubtotal = eligibleSubtotal,
            // กำหนดยอดเงินส่วนลดสุทธิ
            DiscountAmount = discount,
            // กำหนดสถานะรับแต้มสะสมพิเศษสองเท่า
            IsDoublePoints = isDoublePoints,
            // กำหนดข้อความสรุปสิทธิประโยชน์ของโปรโมชั่น
            BenefitSummary = summary
        };
    }

    // ฟังก์ชัน ReadCollectedCodes สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static List<string> ReadCollectedCodes(string? rawSessionValue)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(rawSessionValue))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }

        // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
        try
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return JsonSerializer.Deserialize<List<string>>(rawSessionValue)?
                // กรองเฉพาะข้อความที่ไม่เป็นค่าว่าง
                .Where(s => !string.IsNullOrWhiteSpace(s))
                // ปรับมาตรฐานรูปแบบรหัสคูปอง
                .Select(NormalizeCode)
                // ตัดรายการที่ซ้ำซ้อนออก
                .Distinct(StringComparer.OrdinalIgnoreCase)
                // แปลงเป็นรายการลิสต์
                .ToList() ?? new List<string>();
        }
        // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
        catch
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return new List<string>();
        }
    }

    // ฟังก์ชันแปลงรายการรหัสคูปองเป็น JSON
    private static string SerializeCollectedCodes(IEnumerable<string> codes) =>
        // แปลงข้อมูลเป็นสตริง JSON
        JsonSerializer.Serialize(
            // กรองรหัสคูปองที่ไม่ว่าง
            codes.Where(s => !string.IsNullOrWhiteSpace(s))
                 // ปรับมาตรฐานรูปแบบรหัสคูปอง
                 .Select(NormalizeCode)
                 // ตัดรายการที่ซ้ำซ้อนออก
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 // แปลงเป็นรายการลิสต์
                 .ToList());

    // ฟังก์ชัน BuildRedemptionMetaMap สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static Dictionary<string, RedemptionMetaPayload> BuildRedemptionMetaMap(IEnumerable<Systemlog> logs)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var map = new Dictionary<string, RedemptionMetaPayload>(StringComparer.OrdinalIgnoreCase);
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
            try
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var meta = JsonSerializer.Deserialize<RedemptionMetaPayload>(log.DetailPayload);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (meta != null && !string.IsNullOrWhiteSpace(meta.RedemptionCode) && !map.ContainsKey(meta.RedemptionCode))
                {
                    // เก็บเพย์โหลดลงในพจนานุกรมตามรหัสการแลกของรางวัล
                    map[meta.RedemptionCode] = meta;
                }
            }
            // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return map;
    }

    // ฟังก์ชัน BuildPreorderPromoMap สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static Dictionary<string, PreorderPromoPayload> BuildPreorderPromoMap(IEnumerable<Systemlog> logs)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        var map = new Dictionary<string, PreorderPromoPayload>(StringComparer.OrdinalIgnoreCase);
        // วนลูปประมวลผลข้อมูลแต่ละรายการในคอลเลกชัน
        foreach (var log in logs.OrderByDescending(l => l.Timestamp))
        {
            // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
            if (string.IsNullOrWhiteSpace(log.DetailPayload)) continue;
            // เริ่มบล็อกดักจับข้อผิดพลาดระหว่างประมวลผลคำสั่ง
            try
            {
                // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
                var meta = JsonSerializer.Deserialize<PreorderPromoPayload>(log.DetailPayload);
                // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
                if (meta != null && !string.IsNullOrWhiteSpace(meta.ReservationNumber) && !map.ContainsKey(meta.ReservationNumber))
                {
                    // เก็บเพย์โหลดลงในพจนานุกรมตามหมายเลขใบสั่งจองพรีออเดอร์
                    map[meta.ReservationNumber] = meta;
                }
            }
            // จัดการกรณีเกิดข้อผิดพลาดเพื่อป้องกันระบบหยุดทำงาน
            catch
            {
                // ข้าม log ที่ไม่ใช่ JSON meta
            }
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return map;
    }

    // ฟังก์ชัน FormatPickupDateThai สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string FormatPickupDateThai(string? isoDate)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(isoDate) &&
            // แปลงวันที่ตามรูปแบบ ISO yyyy-MM-dd
            DateOnly.TryParseExact(isoDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return parsed.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("th-TH"));
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return "-";
    }

    // ฟังก์ชันตัดอักขระพิเศษออกจากเบอร์โทรศัพท์ให้เหลือเฉพาะตัวเลข
    private static string NormalizePhoneDigits(string? phone) =>
        // ลบเครื่องหมายขีด ช่องว่าง และตัดช่องว่างส่วนเกิน
        (phone ?? string.Empty).Replace("-", "").Replace(" ", "").Trim().ToLowerInvariant();

    // ตัวแปรเก็บพาธโฟลเดอร์ wwwroot สำหรับการอัปโหลดไฟล์
    private static string? _webRootPath;

    // ฟังก์ชัน ConfigureWebRoot สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static void ConfigureWebRoot(string? webRootPath)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            // กำหนดค่าพาธ wwwroot
            _webRootPath = webRootPath;
        }
    }

    // ฟังก์ชัน ResolveCardImageUrl สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string ResolveCardImageUrl(string? imageUrl)
    {
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return "~/media/images/naruto_hero_fig.png";
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string trimmed = imageUrl.Trim();
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string normalized = trimmed.StartsWith("~/") ? trimmed[1..] : trimmed;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (!normalized.StartsWith("/media/images/", StringComparison.OrdinalIgnoreCase) || normalized.Contains(".."))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ดึงชื่อไฟล์โดยไม่รวมนามสกุล
        string? fileNameNoExt = Path.GetFileNameWithoutExtension(normalized);
        // ตรวจสอบความถูกต้องและค่าว่างของข้อมูลก่อนดำเนินการต่อ
        if (string.IsNullOrWhiteSpace(fileNameNoExt) || fileNameNoExt.EndsWith("_card", StringComparison.OrdinalIgnoreCase))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed;
        }

        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cardFileName = fileNameNoExt + "_card.png";
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string cardRelative = "/media/images/" + cardFileName;
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string rootDir = !string.IsNullOrWhiteSpace(_webRootPath)
            // กรณีระบุพาธ wwwroot ชัดเจน
            ? _webRootPath
            // กำหนดพาธโฟลเดอร์ wwwroot เริ่มต้น
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string physicalPath = Path.Combine(rootDir, "media", "images", cardFileName);
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (System.IO.File.Exists(physicalPath))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return trimmed.StartsWith("~/") ? "~" + cardRelative : cardRelative;
        }

        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return trimmed;
    }

    // ฟังก์ชัน EscapeCsvField สำหรับประมวลผลตรรกะและการทำงานของระบบ
    private static string EscapeCsvField(string? value)
    {
        // ประกาศและกำหนดค่าตัวแปรสำหรับใช้คำนวณและประมวลผลในขั้นตอนถัดไป
        string s = value ?? string.Empty;
        // ตรวจสอบเงื่อนไขทางตรรกะเพื่อแยกการทำงานตามกรณี
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
        {
            // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        // ส่งคืนค่าผลลัพธ์จากการประมวลผลของฟังก์ชัน
        return s;
    }

    // ฟังก์ชันสร้างรายการหมายเลขหน้าแบบย่อพร้อมจุดไข่ปลา (-1 แทน ...) เช่น 1 2 (3) ... 27 -> 2 3 (4) ... 27
    private static List<int> GetCompactPagination(int currentPage, int totalPages)
    {
        // สร้างรายการเก็บหมายเลขหน้าที่จะแสดงผล
        var pages = new List<int>();
        // กรณีจำนวนหน้าทั้งหมดไม่เกิน 5 หน้า ให้แสดงทุกหน้าตามลำดับ
        if (totalPages <= 5)
        {
            // วนลูปเพิ่มเลขหน้าตั้งแต่ 1 ถึงหน้าสุดท้าย
            for (int i = 1; i <= Math.Max(1, totalPages); i++)
            {
                // เพิ่มเลขหน้าเข้าสู่รายการ
                pages.Add(i);
            }
            // ส่งคืนรายการเลขหน้าทั้งหมด
            return pages;
        }

        // กรณีอยู่ช่วง 3 หน้าแรก แสดง 1 2 3 ... หน้าสุดท้าย
        if (currentPage <= 3)
        {
            // เพิ่มหน้า 1
            pages.Add(1);
            // เพิ่มหน้า 2
            pages.Add(2);
            // เพิ่มหน้า 3
            pages.Add(3);
            // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
            pages.Add(-1);
            // เพิ่มหน้าสุดท้าย
            pages.Add(totalPages);
            // ส่งคืนรายการเลขหน้า
            return pages;
        }

        // กรณีอยู่ช่วง 2 หน้าสุดท้าย แสดง 1 ... (N-2) (N-1) N
        if (currentPage >= totalPages - 1)
        {
            // เพิ่มหน้าแรกสุด
            pages.Add(1);
            // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
            pages.Add(-1);
            // เพิ่มหน้าก่อนรองสุดท้าย
            pages.Add(totalPages - 2);
            // เพิ่มหน้ารองสุดท้าย
            pages.Add(totalPages - 1);
            // เพิ่มหน้าสุดท้าย
            pages.Add(totalPages);
            // ส่งคืนรายการเลขหน้า
            return pages;
        }

        // กรณีอยู่หน้ากลาง แสดง (p-2) (p-1) (p) ... หน้าสุดท้าย ตามรูปแบบ 1 2 (3) ... N -> 2 3 (4) ... N
        pages.Add(currentPage - 2);
        // เพิ่มหน้าก่อนหน้า 1 ลำดับ
        pages.Add(currentPage - 1);
        // เพิ่มหน้าปัจจุบัน
        pages.Add(currentPage);
        // เพิ่มสัญลักษณ์จุดไข่ปลา (-1)
        pages.Add(-1);
        // เพิ่มหน้าสุดท้าย
        pages.Add(totalPages);
        // ส่งคืนรายการเลขหน้าแบบย่อ
        return pages;
    }

    // ฟังก์ชันสกัดข้อมูลสถานะและเหตุผลการไม่รับสินค้า/ไม่มารับสินค้า/ยกเลิกคำสั่งจองจากตาราง Systemlogs
    private static (string BadgeTitle, string StepTitle, string NoticeText, string ReasonDetail) ResolveOrderCancelMeta(
        // พารามิเตอร์รายการบันทึกกิจกรรมในระบบ
        IEnumerable<Systemlog>? logs,
        // พารามิเตอร์รหัสอ้างอิงของรายการ
        string? referenceCode,
        // พารามิเตอร์ระบุว่าเป็นรายการของรางวัลหรือไม่
        bool isReward = false)
    {
        // กำหนดข้อความเริ่มต้นกรณีลูกค้ายกเลิกทั่วไป
        string defaultTitle = isReward ? "ยกเลิกการแลกของรางวัล" : "ยกเลิกคำสั่งจอง";
        // กำหนดข้อความคำอธิบายเริ่มต้น
        string defaultNotice = isReward
            // ข้อความสาเหตุเริ่มต้นสำหรับการยกเลิกของรางวัล
            ? "รายการแลกของรางวัลนี้ถูกยกเลิกแล้ว ระบบได้คืนแต้มสะสมและคืนสต็อกของรางวัลเรียบร้อยแล้ว"
            // ข้อความสาเหตุเริ่มต้นสำหรับพรีออเดอร์
            : "รายการนี้ถูกยกเลิกตามคำขอของลูกค้า และระบบได้คืนสิทธิ์สต็อกสินค้าเรียบร้อยแล้ว";

        // ตรวจสอบว่ามีรายการบันทึกและรหัสอ้างอิงหรือไม่
        if (logs == null || string.IsNullOrWhiteSpace(referenceCode))
        {
            // ส่งคืนค่าเริ่มต้นเมื่อไม่มีข้อมูลบันทึก
            return (defaultTitle, defaultTitle, defaultNotice, "ลูกค้าแจ้งยกเลิกรายการ");
        }

        // ตัดช่องว่างรหัสอ้างอิงเพื่อใช้ค้นหาในข้อความบันทึก
        string cleanCode = referenceCode.Trim();
        // ค้นหาบันทึกล่าสุดที่เกี่ยวข้องกับรหัสใบจองหรือรหัสแลกของรางวัลนี้
        var matchedLog = logs
            // กรองบันทึกกิจกรรมที่มีข้อความและตรงตามเงื่อนไข
            .Where(l => !string.IsNullOrEmpty(l.Message) &&
                        // ตรวจสอบข้อความว่ามีรหัสอ้างอิงตรงกัน
                        l.Message.Contains(cleanCode, StringComparison.OrdinalIgnoreCase) &&
                        // ตรวจสอบโมดูลการยกเลิกที่เกี่ยวข้อง
                        (l.Module == "CounterPreorder" || l.Module == "preorder.cancel" || l.Module == "reward.cancel" ||
                         // ตรวจสอบรหัสกิจกรรมปฏิเสธรับสินค้า
                         l.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมลูกค้าไม่มารับ
                         l.LogCode.StartsWith("NOSHOW", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมยกเลิกพรีออเดอร์
                         l.LogCode.StartsWith("PO-CAN", StringComparison.OrdinalIgnoreCase) ||
                         // ตรวจสอบรหัสกิจกรรมยกเลิกของรางวัล
                         l.LogCode.StartsWith("RWD-CAN", StringComparison.OrdinalIgnoreCase)))
            // จัดเรียงตามวันเวลาจากล่าสุดไปเก่าสุด
            .OrderByDescending(l => l.Timestamp)
            // เลือกรายการแรกที่พบ
            .FirstOrDefault();

        // หากไม่พบบันทึกเฉพาะเจาะจง ให้ส่งคืนค่าเริ่มต้น
        if (matchedLog == null)
        {
            // ส่งคืนค่าเริ่มต้น
            return (defaultTitle, defaultTitle, defaultNotice, "ลูกค้าแจ้งยกเลิกรายการ");
        }

        // อ่านข้อความเต็มจากบันทึกกิจกรรม
        string msg = matchedLog.Message ?? string.Empty;
        // สกัดส่วนข้อความหลังคำว่า "สาเหตุ:" หากมีระบุไว้
        string reasonPart = string.Empty;
        // ค้นหาตำแหน่งคำว่า "สาเหตุ:" ในข้อความ
        int reasonIdx = msg.IndexOf("สาเหตุ:", StringComparison.OrdinalIgnoreCase);
        // หากพบคำว่า "สาเหตุ:" ให้ตัดข้อความตั้งแต่ตำแหน่งนั้นมาจัดรูปแบบ
        if (reasonIdx >= 0)
        {
            // ตัดข้อความส่วนสาเหตุและรายละเอียด
            reasonPart = msg[reasonIdx..].Trim();
            // ตัดข้อความส่วนท้ายเรื่องการคืนแต้มออกจากข้อความสาเหตุเพื่อไม่ให้ซ้ำซ้อน
            int refundIdx = reasonPart.IndexOf("| คืน", StringComparison.OrdinalIgnoreCase);
            // หากพบส่วนข้อความคืนแต้มต่อท้าย ให้ตัดเฉพาะส่วนสาเหตุข้างหน้า
            if (refundIdx > 0)
            {
                // เก็บเฉพาะข้อความสาเหตุและหมายเหตุ
                reasonPart = reasonPart[..refundIdx].Trim();
            }
            // ลบข้อความหมายเหตุหรือรายละเอียดที่เป็นคำว่า "ไม่มี" ออกเพื่อให้ข้อความกระชับสวยงาม
            reasonPart = reasonPart
                // ตัดข้อความรายละเอียดที่ว่างเปล่าออก
                .Replace("| รายละเอียด: ไม่มี", "")
                // ตัดข้อความหมายเหตุที่ว่างเปล่าออก
                .Replace("| หมายเหตุ: ไม่มี", "")
                // ตัดช่องว่างส่วนเกิน
                .Trim();
        }

        // ตรวจสอบกรณีพนักงานกด "ไม่รับสินค้า" (REJECT)
        if (matchedLog.LogCode.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase) ||
            // ตรวจสอบข้อความลูกค้าไม่รับสินค้า
            msg.Contains("ลูกค้าไม่รับ", StringComparison.OrdinalIgnoreCase))
        {
            // กำหนดป้ายสถานะเป็นลูกค้าไม่รับสินค้า
            string title = isReward ? "ลูกค้าไม่รับของรางวัล" : "ลูกค้าไม่รับสินค้า";
            // กำหนดรายละเอียดสาเหตุ
            string detail = !string.IsNullOrWhiteSpace(reasonPart) ? reasonPart : "สาเหตุ: ลูกค้าปฏิเสธไม่รับสินค้าที่เคาน์เตอร์หน้าร้าน";
            // ประกอบข้อความแจ้งเตือนฉบับเต็ม
            string notice = $"รายการนี้{title}ที่หน้าร้าน — {detail} (คืนสต็อกเรียบร้อยแล้ว)";
            // ส่งคืนข้อมูลกรณีลูกค้าไม่รับสินค้า
            return (title, title, notice, detail);
        }

        // ตรวจสอบกรณีพนักงานกด "ไม่มารับสินค้า" (NOSHOW)
        if (matchedLog.LogCode.StartsWith("NOSHOW", StringComparison.OrdinalIgnoreCase) ||
            // ตรวจสอบข้อความลูกค้าไม่มารับตามนัด
            msg.Contains("ลูกค้าไม่มารับ", StringComparison.OrdinalIgnoreCase))
        {
            // กำหนดป้ายสถานะเป็นลูกค้าไม่มารับสินค้า
            string title = isReward ? "ลูกค้าไม่มารับของรางวัล" : "ลูกค้าไม่มารับสินค้า";
            // กำหนดรายละเอียดสาเหตุ
            string detail = !string.IsNullOrWhiteSpace(reasonPart) ? reasonPart : "สาเหตุ: ลูกค้าไม่มารับตามกำหนดนัดหมาย";
            // ประกอบข้อความแจ้งเตือนฉบับเต็ม
            string notice = $"รายการนี้ถูกยกเลิกเนื่องจาก{title}ตามกำหนด — {detail} (คืนสต็อกเรียบร้อยแล้ว)";
            // ส่งคืนข้อมูลกรณีลูกค้าไม่มารับสินค้า
            return (title, title, notice, detail);
        }

        // กรณีลูกค้าเป็นผู้กดยกเลิกคำสั่งจองหรือการแลกของรางวัลด้วยตนเอง
        string cancelDetail = !string.IsNullOrWhiteSpace(reasonPart)
            // ส่งคืนข้อความสาเหตุที่แกะได้
            ? reasonPart
            // ข้อความสาเหตุเริ่มต้นกรณีแกะไม่พบ
            : "สาเหตุ: ลูกค้ากดยกเลิกคำสั่งจองด้วยตนเอง";
        // ประกอบข้อความแจ้งเตือนสำหรับกรณีลูกค้ายกเลิกเอง
        string cancelNotice = $"รายการนี้ถูกยกเลิกตามคำขอของลูกค้า — {cancelDetail} (คืนสิทธิ์สต็อกเรียบร้อยแล้ว)";
        // ส่งคืนข้อมูลกรณีลูกค้ายกเลิกเอง
        return (defaultTitle, defaultTitle, cancelNotice, cancelDetail);
    }

    // ฟังก์ชันคืนค่า Action และ Controller หน้าหลักประจำบทบาทผู้ใช้งาน (สำหรับระบบ 1 เบราว์เซอร์ 1 บทบาท)
    private static (string ActionName, string ControllerName) GetRoleHomeRoute(string? role)
    {
        // แปลงชื่อบทบาทเป็นตัวพิมพ์เล็ก
        string normalized = (role ?? string.Empty).Trim().ToLowerInvariant();
        // จับคู่บทบาทกับหน้าหลักของบทบาทนั้น
        return normalized switch
        {
            // เส้นทางหน้าหลักพนักงานหน้าร้าน
            "staff" => ("Index", "Staff"),
            // เส้นทางหน้าหลักเจ้าของร้าน
            "owner" => ("Promotions", "Owner"),
            // เส้นทางหน้าหลักฝ่ายบัญชี
            "accountant" => ("Index", "Accountant"),
            // เส้นทางหน้าหลักฝ่ายไอทีซัพพอร์ต
            "itsupport" => ("Dashboard", "ITSupport"),
            // เส้นทางหน้าหลักลูกค้าทั่วไป
            _ => ("Index", "Home")
        };
    }

}
