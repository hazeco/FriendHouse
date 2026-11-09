using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khetsophon_CSI402_8456.Models;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;
using Khetsophon_CSI402_8456.ViewModels;

namespace Khetsophon_CSI402_8456.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    public HomeController (Csi402dbContext db)
    {
        _db = db;
    }

    public IActionResult Lab9()
    {
        // show form for create data 

        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lab9(Labstudent data) //create user into DB
    {
        if(ModelState.IsValid)
        {
            if(data.StdPassword != null)
            {
                var CheckID = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                //select * from Labstudents where StdId == "@StdId" 
                if(CheckID == null)
                {
                    Labstudent s = new Labstudent();
                    //table2
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return RedirectToAction("Lab9_1", "Home", new { status = "Create Success" });
                } else {
                    ViewBag.Status = "This ID is already exists";
                    return View();
                }
                
            } else
            {
                ViewBag.Status = "Please enter password";
                return View();
            }
        } else
        {
            return View();
        }
    }

    // Read
    public IActionResult Lab9_1()
    {
        // Read data from Labstudents table and return to view
        var data = _db.Labstudents.ToList();
        return View(data);
    }

    public IActionResult Lab10(string? StdId) // show form for update data
    {
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        return View(User);
    }

    [HttpPost]
    public IActionResult Lab10(Labstudent data) // update user into DB
    {
        var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
        User.StdPassword = data.StdPassword;
        User.StdName = data.StdName;
        User.StdLastname = data.StdLastname;
        _db.SaveChanges();
        return RedirectToAction("Lab9_1", "Home", new { status = "Update Success" });
    }

    [HttpPost] // delete user from DB
    public IActionResult Lab10_1(string? StdId){
        var Check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        if(Check != null)
        {
            _db.Remove(Check);
            _db.SaveChanges();
            return RedirectToAction("Lab9_1", "Home", new { status = "Delete Success" });
        }
        else {
            return RedirectToAction("Lab9_1", "Home", new { status = "This ID is not exists" });
        }
    }


    public IActionResult UserList()
    {
        var users = _db.Users.ToList();
        return View(users);
    }
    public IActionResult Index(LoginViewModel? data)
    {
        // receive data from login page by viewbag
        if (data != null)
        {
            ViewBag.Email = data.Email;
            ViewBag.Password = data.Password;
        }

        var heroProduct = _db.Products.FirstOrDefault(p => p.IsFeaturedHero)
                          ?? _db.Products.FirstOrDefault(p => !string.IsNullOrEmpty(p.Model3Dpath));

        if (heroProduct != null)
        {
            ViewBag.HeroModel3D = !string.IsNullOrEmpty(heroProduct.Model3Dpath) ? heroProduct.Model3Dpath : "~/media/models/naruto_kyubi_sage.glb";
            ViewBag.HeroName = heroProduct.Name;
            ViewBag.HeroPrice = $"฿{heroProduct.RetailPrice:N0}";
            ViewBag.HeroId = heroProduct.ProductId;
        }

        // ดึงรายการฟิกเกอร์จริงจากฐานข้อมูล พร้อมข้อมูลสต็อกคงเหลือ
        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive == true || p.IsActive == null)
            .ToList();

        return View(products);
    }

    public IActionResult Collection()
    {
        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive == true || p.IsActive == null)
            .ToList();
        return View(products);
    }

    public IActionResult Promotion()
    {
        var promotions = _db.Promotions.Where(p => p.IsActive == true || p.IsActive == null).ToList();
        return View(promotions);
    }

    public IActionResult Customer()
    {
        return RedirectToAction("Index");
    }

    private int? GetCurrentCustomerId()
    {
        // WHY: ดึง ID ของลูกค้า โดยให้ความสำคัญกับ CustomerUserId ก่อน
        // เพื่อให้สามารถเปิดทดสอบหลาย Tab สลับบทบาท (Customer vs Staff) ได้อย่างอิสระโดยไม่แย่ง Session กัน
        return HttpContext.Session.GetInt32("CustomerUserId")
            ?? (HttpContext.Session.GetString("Role") == "customer" ? HttpContext.Session.GetInt32("UserId") : null)
            ?? HttpContext.Session.GetInt32("UserId");
    }

    // READ: แสดงรายการสินค้าในตะกร้า (Cart)
    // WHY: ตรวจสอบสิทธิ์ผู้ใช้ หากยังไม่ได้ล็อกอินจะนำทางไปหน้า Login ทันที
    public IActionResult Cart()
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเพื่อดูตะกร้าสินค้าของคุณ";
            return RedirectToAction("Login", "Account");
        }

        var items = _db.Cartitems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        return View(items);
    }

    // Action สำหรับเพิ่มสินค้าลงในตะกร้าของสมาชิก (POST Method)
    [HttpPost]
    // ฟังก์ชัน AddToCart รับ productId (รหัสสินค้า), quantity (จำนวนชิ้น) และ returnUrl (URL ย้อนกลับ)
    public IActionResult AddToCart(int productId, int quantity = 1, string? returnUrl = null)
    {
        // ขั้นตอนที่ 1: ดึงรหัสผู้ใช้งานปัจจุบันจาก Session เพื่อตรวจสอบสิทธิ์การเข้าสู่ระบบ
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนให้ผู้ใช้งานเข้าสู่ระบบก่อน
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเพื่อเพิ่มสินค้าลงในตะกร้า";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าเข้าสู่ระบบใน AccountController
            return RedirectToAction("Login", "Account");
        }

        // ขั้นตอนที่ 2: แปลงรหัสสินค้าเพื่อรองรับรหัสข้อมูลดั้งเดิม (201-205 ให้แปลงเป็น 1-5)
        if (productId >= 201 && productId <= 205)
        {
            // ลดค่ารหัสสินค้าลง 200 เพื่อให้ตรงกับรหัสจริงในฐานข้อมูล
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าจากตาราง Products พร้อมดึงข้อมูลสต็อก Inventories
        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId)
            // กรณีไม่พบรหัสที่ระบุ ให้เลือกสินค้าชิ้นแรกในฐานข้อมูลเป็นค่าเริ่มต้น
            ?? _db.Products.Include(p => p.Inventory).FirstOrDefault();

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้านี้ในระบบฐานข้อมูล";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(returnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 3: คำนวณจำนวนสินค้าคงเหลือที่พร้อมให้สั่งจอง (QuantityOnHand ลบด้วย ReservedQuantity)
        int availableQty = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
        // ตรวจสอบว่ามีสินค้าพร้อมให้สั่งจองหรือไม่
        if (availableQty <= 0)
        {
            // กำหนดข้อความแจ้งเตือนกรณีสินค้าถูกจองเต็มจำนวนแล้ว
            TempData["ErrorMessage"] = "ขออภัย สินค้านี้ถูกจองเต็มจำนวนแล้ว (สินค้าหมดชั่วคราว)";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการสินค้า
            return Redirect(returnUrl ?? Url.Action("Collection", "Home")!);
        }

        // ขั้นตอนที่ 4: ค้นหาว่าผู้ใช้งานคนนี้มีสินค้านี้อยู่ในตะกร้าแล้วหรือไม่
        var existingItem = _db.Cartitems.FirstOrDefault(c => c.UserId == userId.Value && c.ProductId == product.ProductId);
        // กำหนดจำนวนสินค้าที่ต้องการสั่งเพิ่ม โดยไม่ให้น้อยกว่า 1 ชิ้น
        int requestedQty = Math.Max(1, quantity);
        // ตรวจสอบจำนวนเดิมที่อยู่ในตะกร้าแล้ว (หากยังไม่มีให้เป็น 0)
        int currentInCart = existingItem?.Quantity ?? 0;

        // ตรวจสอบว่าจำนวนเดิมในตะกร้ารวมกับจำนวนที่ต้องการสั่งใหม่ เกินกว่าสต็อกพร้อมจองหรือไม่
        if (currentInCart + requestedQty > availableQty)
        {
            // กำหนดข้อความแจ้งเตือนระบุจำนวนคงเหลือจริง และจำนวนที่มีในตะกร้าแล้ว
            TempData["ErrorMessage"] = $"สินค้าคงเหลือพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้น (ในตะกร้ามีแล้ว {currentInCart} ชิ้น)";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าตะกร้าสินค้า
            return Redirect(returnUrl ?? Url.Action("Cart", "Home")!);
        }

        // ขั้นตอนที่ 5: ตรวจสอบเพื่อเพิ่มหรือสร้างรายการสินค้าในตะกร้า
        if (existingItem != null)
        {
            // กรณีมีสินค้านี้ในตะกร้าอยู่แล้ว ให้เพิ่มจำนวนชิ้นเดิมด้วยจำนวนใหม่
            existingItem.Quantity += requestedQty;
        }
        else
        {
            // กรณีเป็นสินค้าใหม่ในตะกร้า ให้สร้างแถวข้อมูล Cartitem ใหม่
            _db.Cartitems.Add(new Cartitem
            {
                // กำหนดรหัสผู้ใช้งานเจ้าของตะกร้า
                UserId = userId.Value,
                // กำหนดรหัสสินค้าที่เลือก
                ProductId = product.ProductId,
                // กำหนดจำนวนสินค้าที่ต้องการสั่ง
                Quantity = requestedQty,
                // บันทึกวันและเวลาปัจจุบันที่เพิ่มลงตะกร้า
                CreatedAt = DateTime.UtcNow
            });
        }

        // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL Database
        _db.SaveChanges();

        // กำหนดข้อความแจ้งเตือนผลสำเร็จระบุชื่อสินค้า
        TempData["SuccessMessage"] = $"เพิ่ม \"{product.Name}\" ลงในตะกร้าเรียบร้อยแล้ว";
        // สั่งเปลี่ยนเส้นทางไปยังหน้าตะกร้าสินค้าเพื่อดูสรุปรายการ
        return Redirect(returnUrl ?? Url.Action("Cart", "Home")!);
    }

    // Action สำหรับปรับปรุงจำนวนสินค้าในตะกร้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน UpdateCartQuantity รับ cartItemId (รหัสรายการตะกร้า) และ quantity (จำนวนชิ้นใหม่)
    public IActionResult UpdateCartQuantity(int cartItemId, int quantity)
    {
        // ค้นหาข้อมูลรายการในตะกร้าจากตาราง Cartitems พร้อมเชื่อมโยง Product และ Inventory
        var item = _db.Cartitems.Include(c => c.Product).ThenInclude(p => p.Inventory).FirstOrDefault(c => c.CartItemId == cartItemId);
        // ตรวจสอบว่าพบรายการในตะกร้าหรือไม่
        if (item == null)
        {
            // กำหนดข้อความแจ้งเตือนกรณีไม่พบรายการ
            TempData["ErrorMessage"] = "ไม่พบรายการสินค้านี้ในตะกร้า";
            // เปลี่ยนเส้นทางกลับไปหน้าตะกร้า
            return RedirectToAction("Cart");
        }

        // ตรวจสอบกรณีจำนวนที่ปรับลดลงเหลือน้อยกว่าหรือเท่ากับศูนย์
        if (quantity <= 0)
        {
            // สั่งลบรายการสินค้านั้นออกจากตาราง Cartitems
            _db.Cartitems.Remove(item);
            // กำหนดข้อความแจ้งผลการลบรายการออกจากตะกร้า
            TempData["SuccessMessage"] = "ลบรายการออกจากตะกร้าเรียบร้อยแล้ว";
        }
        // กรณีจำนวนมากกว่าศูนย์
        else
        {
            // คำนวณจำนวนสต็อกคงเหลือที่พร้อมให้สั่งจอง (QuantityOnHand ลบด้วย ReservedQuantity)
            int availableQty = (item.Product?.Inventory?.QuantityOnHand ?? 0) - (item.Product?.Inventory?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าจำนวนที่ปรับ เกินกว่าสินค้าคงเหลือที่มีอยู่หรือไม่
            if (quantity > availableQty)
            {
                // กำหนดข้อความแจ้งเตือนว่าสินค้าไม่เพียงพอ
                TempData["ErrorMessage"] = $"สินค้าพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้นเท่านั้น";
                // เปลี่ยนเส้นทางกลับหน้าตะกร้า
                return RedirectToAction("Cart");
            }

            // อัปเดตจำนวนสินค้าชิ้นใหม่ในรายการตะกร้า
            item.Quantity = quantity;
            // กำหนดข้อความแจ้งผลการปรับจำนวนสำเร็จ
            TempData["SuccessMessage"] = "ปรับจำนวนสินค้าในตะกร้าเรียบร้อยแล้ว";
        }

        // บันทึกการเปลี่ยนแปลงลง MySQL Database
        _db.SaveChanges();
        // สั่งเปลี่ยนเส้นทางกลับหน้าตะกร้าเพื่อแสดงยอดเงินใหม่
        return RedirectToAction("Cart");
    }

    // Action สำหรับลบสินค้า 1 รายการออกจากตะกร้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน RemoveFromCart รับ cartItemId ที่ต้องการลบ
    public IActionResult RemoveFromCart(int cartItemId)
    {
        // ค้นหารายการในตาราง Cartitems ด้วยรหัส CartItemId
        var item = _db.Cartitems.FirstOrDefault(c => c.CartItemId == cartItemId);
        // ตรวจสอบว่าพบรายการในตะกร้าหรือไม่
        if (item != null)
        {
            // สั่งลบแถวข้อมูลนั้นออกจากฐานข้อมูล
            _db.Cartitems.Remove(item);
            // บันทึกการลบลง MySQL Database
            _db.SaveChanges();
            // กำหนดข้อความแจ้งผลสำเร็จผ่าน TempData
            TempData["SuccessMessage"] = "ลบรายการออกจากตะกร้าแล้ว";
        }

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // Action สำหรับล้างรายการสินค้าทั้งหมดในตะกร้าของผู้ใช้ (POST Method)
    [HttpPost]
    // ฟังก์ชัน ClearCart ลบรายการทั้งหมดของผู้ใช้ที่ล็อกอินอยู่
    public IActionResult ClearCart()
    {
        // ดึงรหัสผู้ใช้งานปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานล็อกอินอยู่หรือไม่
        if (userId.HasValue)
        {
            // ดึงรายการสินค้าทั้งหมดในตะกร้าที่เป็นของผู้ใช้งานคนนี้
            var items = _db.Cartitems.Where(c => c.UserId == userId.Value).ToList();
            // สั่งลบรายการทั้งหมดพร้อมกันด้วย RemoveRange
            _db.Cartitems.RemoveRange(items);
            // บันทึกการเปลี่ยนแปลงลง MySQL Database
            _db.SaveChanges();
            // กำหนดข้อความแจ้งผลการล้างตะกร้าสำเร็จ
            TempData["SuccessMessage"] = "ล้างรายการสินค้าในตะกร้าเรียบร้อยแล้ว";
        }

        // เปลี่ยนเส้นทางกลับไปยังหน้าตะกร้าสินค้า
        return RedirectToAction("Cart");
    }

    // Action ยืนยันการสั่งจองพรีออเดอร์จากตะกร้าสินค้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน ConfirmPreorder รับเบอร์โทรติดต่อและสาขาที่ต้องการรับสินค้า
    public IActionResult ConfirmPreorder(string? contactPhone, string? pickupBranch)
    {
        // ขั้นตอนที่ 1: ดึงรหัสลูกค้าปัจจุบันจาก Session
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบแล้วหรือไม่
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนสีแดงผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนยืนยันการจอง";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // เริ่มต้นฐานข้อมูล Transaction เพื่อรองรับความปลอดภัยของสต็อกและป้องกัน Race Condition
        using var transaction = _db.Database.BeginTransaction();
        try
        {
            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // ดึงรายการสินค้าทั้งหมดในตะกร้าของผู้ใช้คนนี้พร้อมข้อมูลสินค้า Product
            var cartItems = _db.Cartitems.Include(c => c.Product).Where(c => c.UserId == userId.Value).ToList();

            // ตรวจสอบว่าในตะกร้ามีรายการสินค้าหรือไม่
            if (!cartItems.Any())
            {
                // แจ้งเตือนว่าไม่มีสินค้าในตะกร้า
                TempData["ErrorMessage"] = "ไม่มีสินค้าในตะกร้า กรุณาเลือกสินค้าก่อนยืนยันการจอง";
                // เปลี่ยนเส้นทางกลับไปหน้ารวมสินค้า
                return RedirectToAction("Collection");
            }

            // ขั้นตอนที่ 2 (Race Condition Check): ตรวจสอบสต็อกคงเหลือจริงแบบสดๆ ใน Transaction
            foreach (var item in cartItems)
            {
                // ค้นหาสต็อกของสินค้าแต่ละชิ้นในตาราง Inventories
                var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                // คำนวณสต็อกพร้อมจอง = จำนวนคงคลัง (QuantityOnHand) ลบด้วยยอดที่ถูกจองแล้ว (ReservedQuantity)
                int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
                // ตรวจสอบว่าสต็อกคงเหลือไม่พอต่อจำนวนที่ต้องการสั่งจอง
                if (inv == null || available < item.Quantity)
                {
                    // ยกเลิกคำสั่งทั้งหมดใน Transaction (Rollback) เพื่อไม่ให้ตัดสต็อก
                    transaction.Rollback();
                    // แจ้งเตือนข้อความว่าสินค้าไม่เพียงพอ
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น) กรุณาปรับจำนวนในตะกร้า";
                    // เปลี่ยนเส้นทางกลับหน้าตะกร้า
                    return RedirectToAction("Cart");
                }
            }

            // คำนวณยอดเงินรวมทั้งหมดของสินค้าในตะกร้า
            decimal total = cartItems.Sum(c => c.Product.RetailPrice * c.Quantity);
            // สร้างรหัสใบสั่งจองพรีออเดอร์รูปแบบ PO-2026-XXXX
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);

            // สร้างอ็อบเจกต์คำสั่งจอง Preorder ใหม่
            var preorder = new Preorder
            {
                // กำหนดหมายเลขใบจอง
                ReservationNumber = reservationNo,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                UserId = userId.Value,
                // กำหนดเบอร์โทรติดต่อ (หากไม่กรอกให้ใช้เบอร์จากตารางผู้ใช้)
                ContactPhone = contactPhone ?? user?.Phone ?? "081-234-5678",
                // กำหนดวันนัดรับสินค้าล่วงหน้า 7 วัน
                EstimatedPickupDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                // กำหนดสถานะเริ่มต้นเป็น พร้อมรับสินค้าที่ร้าน (ReadyForPickup)
                CurrentStatus = "ReadyForPickup",
                // กำหนดยอดเงินสุทธิรวม
                TotalFinalPrice = total,
                // กำหนดสาขาที่นัดรับสินค้า
                PickupBranch = pickupBranch ?? "FriendHouse HQ สาขาสยามสแควร์",
                // บันทึกวันเวลาที่ทำรายการสั่งจอง
                CreatedAt = DateTime.Now
            };

            // เพิ่มข้อมูลคำสั่งจองลงในตาราง Preorders
            _db.Preorders.Add(preorder);
            // บันทึกการเปลี่ยนแปลงเพื่อให้ได้ค่า PreorderId ที่สร้างขึ้น
            _db.SaveChanges();

            // วนลูปบันทึกรายการสินค้าแต่ละชิ้นลงใน Preorderitems พร้อมสำรองยอดจอง
            foreach (var item in cartItems)
            {
                // เพิ่มรายการสินค้าที่สั่งจองลงในตาราง Preorderitems
                _db.Preorderitems.Add(new Preorderitem
                {
                    // ผูกรหัสใบจอง PreorderId
                    PreorderId = preorder.PreorderId,
                    // ผูกรหัสสินค้า ProductId
                    ProductId = item.ProductId,
                    // บันทึกราคาต่อหน่วย ณ ขณะที่สั่งจอง
                    UnitPrice = item.Product.RetailPrice,
                    // บันทึกจำนวนชิ้นที่จอง
                    Quantity = item.Quantity
                });

                // สำรองยอดจองสินค้าในคลัง (Phase 1: ล็อคยอด ReservedQuantity)
                var inv = _db.Inventories.First(i => i.ProductId == item.ProductId);
                // บวกยอดจองเพิ่มขึ้นตามจำนวนชิ้นที่สั่ง
                inv.ReservedQuantity += item.Quantity;
            }

            // ลบรายการสินค้าออกจากตะกร้าเมื่อสั่งจองสำเร็จ
            _db.Cartitems.RemoveRange(cartItems);
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
            _db.SaveChanges();

            // ยืนยันคำสั่งทั้งหมดใน Transaction (Commit) เมื่อทำงานครบทุกขั้นตอน
            transaction.Commit();

            // แจ้งเตือนข้อความสำเร็จพร้อมหมายเลขใบจองผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว สามารถมารับสินค้าได้ที่หน้าร้าน";
            // เปลี่ยนเส้นทางไปยังหน้าแสดงสถานะคำสั่งจองของลูกค้า
            return RedirectToAction("PreorderStatus");
        }
        catch (Exception)
        {
            // กรณีเกิดข้อผิดพลาด ให้ยกเลิกคำสั่งทั้งหมดใน Transaction ทันที
            transaction.Rollback();
            // แจ้งเตือนข้อความข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับไปที่หน้าตะกร้าสินค้า
            return RedirectToAction("Cart");
        }
    }

    // Action สั่งจองสินค้าโดยตรงจากหน้ารายละเอียดสินค้า (POST Method)
    [HttpPost]
    // ฟังก์ชัน DirectPreorder รับ productId, quantity, contactPhone และ pickupDate
    public IActionResult DirectPreorder(int productId, int quantity = 1, string? contactPhone = null, string? pickupDate = null)
    {
        // ขั้นตอนที่ 1: ตรวจสอบการเข้าสู่ระบบของลูกค้า
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าลูกค้าเข้าสู่ระบบหรือยัง
        if (!userId.HasValue)
        {
            // กำหนดข้อความแจ้งเตือนผ่าน TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำรายการสั่งจองสินค้า";
            // เปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าใช้
            return RedirectToAction("Login", "Account");
        }

        // แปลงรหัสสินค้ารองรับข้อมูลดั้งเดิม 201-205 ให้เป็น 1-5
        if (productId >= 201 && productId <= 205)
        {
            // หักลบค่ารหัสสินค้า 200
            productId = productId - 200;
        }

        // ค้นหาข้อมูลสินค้าในตาราง Products ด้วยรหัส ProductId
        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId)
            // หากไม่พบ ให้ใช้สินค้าชิ้นแรกในระบบ
            ?? _db.Products.FirstOrDefault();

        // ตรวจสอบว่าพบสินค้าในระบบหรือไม่
        if (product == null)
        {
            // แจ้งเตือนกรณีไม่พบสินค้า
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการสั่งจอง";
            // เปลี่ยนเส้นทางกลับไปหน้ารวมสินค้า
            return RedirectToAction("Collection");
        }

        // กำหนดจำนวนสินค้าที่จอง ขั้นต่ำ 1 ชิ้น
        int qty = Math.Max(1, quantity);

        // เริ่มต้นฐานข้อมูล Transaction
        using var transaction = _db.Database.BeginTransaction();
        try
        {
            // ค้นหาข้อมูลสต็อกในตาราง Inventories
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == product.ProductId);
            // คำนวณสต็อกพร้อมจองจริง = QuantityOnHand ลบด้วย ReservedQuantity
            int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
            // ตรวจสอบว่าสินค้ามีพอให้สั่งจองหรือไม่
            if (inv == null || available < qty)
            {
                // ยกเลิกคำสั่งทั้งหมดใน Transaction
                transaction.Rollback();
                // แจ้งเตือนข้อความว่าสินค้าไม่เพียงพอ
                TempData["ErrorMessage"] = $"ขออภัย สินค้า \"{product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น)";
                // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้าเดิม
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            // ค้นหาข้อมูลผู้ใช้งานในตาราง Users
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            // สร้างหมายเลขใบสั่งจองพรีออเดอร์
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);
            // คำนวณยอดเงินรวมสุทธิ
            decimal totalPrice = product.RetailPrice * qty;

            // กำหนดวันนัดรับสินค้าเริ่มต้นเป็น 7 วันนับจากวันนี้
            DateOnly estDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            // ตรวจสอบว่าผู้ใช้เลือกวันที่นัดรับสินค้ามาหรือไม่
            if (!string.IsNullOrEmpty(pickupDate) && DateOnly.TryParse(pickupDate, out var parsedDate))
            {
                // ตรวจสอบเงื่อนไขว่าห้ามเลือกวันที่ย้อนหลังจากวันปัจจุบัน
                if (parsedDate < DateOnly.FromDateTime(DateTime.Today))
                {
                    // ยกเลิก Transaction
                    transaction.Rollback();
                    // กำหนดข้อความแจ้งเตือนห้ามเลือกวันที่ย้อนหลัง
                    TempData["ErrorMessage"] = "ไม่สามารถเลือกวันที่ย้อนหลังได้ กรุณาเลือกวันปัจจุบันเป็นต้นไป";
                    // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้าเดิม
                    return RedirectToAction("ProductDetail", new { id = product.ProductId });
                }
                // กำหนดวันที่นัดรับสินค้าตามที่ลูกค้าเลือก
                estDate = parsedDate;
            }

            // สร้างอ็อบเจกต์ Preorder สำหรับบันทึกคำสั่งจอง
            var preorder = new Preorder
            {
                // กำหนดหมายเลขใบจอง
                ReservationNumber = reservationNo,
                // กำหนดรหัสลูกค้าผู้สั่งจอง
                UserId = userId.Value,
                // กำหนดเบอร์โทรศัพท์ผู้ติดต่อ
                ContactPhone = contactPhone ?? user?.Phone ?? "081-234-5678",
                // กำหนดวันนัดรับสินค้า
                EstimatedPickupDate = estDate,
                // กำหนดสถานะเป็น พร้อมรับสินค้าที่ร้าน
                CurrentStatus = "ReadyForPickup",
                // กำหนดยอดเงินสุทธิ
                TotalFinalPrice = totalPrice,
                // กำหนดสาขาที่นัดรับสินค้า
                PickupBranch = "FriendHouse HQ สาขาสยามสแควร์",
                // บันทึกวันเวลาที่สร้างคำสั่งจอง
                CreatedAt = DateTime.Now
            };

            // เพิ่มคำสั่งจองลงในตาราง Preorders
            _db.Preorders.Add(preorder);
            // บันทึกการเปลี่ยนแปลงเพื่อให้ได้ค่า PreorderId
            _db.SaveChanges();

            // เพิ่มรายการสินค้าที่สั่งจองลงในตาราง Preorderitems
            _db.Preorderitems.Add(new Preorderitem
            {
                // ผูกรหัสใบจอง
                PreorderId = preorder.PreorderId,
                // ผูกรหัสสินค้า
                ProductId = product.ProductId,
                // บันทึกราคาต่อหน่วย
                UnitPrice = product.RetailPrice,
                // บันทึกจำนวนชิ้นที่จอง
                Quantity = qty
            });

            // สำรองยอดจองสินค้าในคลัง (Phase 1: ล็อคยอด ReservedQuantity)
            inv.ReservedQuantity += qty;
            // บันทึกการเปลี่ยนแปลงยอดจองลงฐานข้อมูล
            _db.SaveChanges();

            // ยืนยันคำสั่งทั้งหมดใน Transaction (Commit)
            transaction.Commit();

            // แจ้งเตือนข้อความสำเร็จผ่าน TempData
            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว มารับสินค้าและชำระเงินที่หน้าร้านได้ทันที";
            // เปลี่ยนเส้นทางไปยังหน้าแสดงสถานะคำสั่งจอง
            return RedirectToAction("PreorderStatus");
        }
        catch (Exception)
        {
            // ยกเลิกคำสั่งทั้งหมดใน Transaction เมื่อมีข้อผิดพลาด
            transaction.Rollback();
            // แจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
            // เปลี่ยนเส้นทางกลับหน้ารายละเอียดสินค้า
            return RedirectToAction("ProductDetail", new { id = product.ProductId });
        }
    }

    // READ: หน้ารายละเอียดสินค้าจริงจาก DB (Product Detail)
    public IActionResult ProductDetail(int? id)
    {
        Product? product = null;

        if (id.HasValue && id.Value > 0)
        {
            int targetId = (id.Value >= 201 && id.Value <= 205) ? (id.Value - 200) : id.Value;

            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == targetId);
        }

        if (product == null)
        {
            product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefault();
        }

        return View(product);
    }

    public IActionResult PreorderStatus()
    {
        // WHY: ดึงรายการสถานะพรีออเดอร์ทั้งหมดของลูกค้า เพื่อให้แสดงรายการสั่งจองครบทุกชิ้น ไม่ใช่แค่ใบเดียว
        var userId = GetCurrentCustomerId();
        var preorders = new List<Preorder>();

        if (userId.HasValue)
        {
            preorders = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(pi => pi.Product)
                .Where(p => p.UserId == userId.Value)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        // กรณีเป็น Guest หรือยังไม่มีรายการใน Account ให้ดึงรายการล่าสุดในระบบมาแสดงผล
        if (!preorders.Any())
        {
            preorders = _db.Preorders
                .Include(p => p.Preorderitems)
                    .ThenInclude(pi => pi.Product)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToList();
        }

        return View(preorders);
    }

    public IActionResult Rewards()
    {
        var rewards = _db.Rewards.Where(r => r.IsAvailable == true || r.IsAvailable == null).ToList();
        return View(rewards);
    }

    // =========================================================================
    // โฟลว์: สมาชิกแลกของรางวัลด้วยแต้มสะสม (Redeem Reward)
    // =========================================================================
    // รองรับการส่งคำขอแลกรางวัลผ่านฟอร์ม POST
    [HttpPost]
    // ฟังก์ชันดำเนินการแลกของรางวัลโดยรับค่ารหัสของรางวัล rewardId
    public IActionResult RedeemReward(int rewardId)
    {
        // ดึงรหัสสมาชิกปัจจุบันจากเซสชันล็อกอิน
        var userId = GetCurrentCustomerId();
        // ตรวจสอบว่าผู้ใช้งานได้เข้าสู่ระบบหรือไม่
        if (!userId.HasValue)
        {
            // บันทึกข้อความแจ้งเตือนสีแดงลง TempData
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำการแลกของรางวัล";
            // สั่งเปลี่ยนเส้นทางไปยังหน้าลงชื่อเข้าสู่ระบบ
            return RedirectToAction("Login", "Account");
        }

        // ค้นหาข้อมูลสมาชิกในตาราง Users ด้วยรหัสสมาชิก
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        // ค้นหาข้อมูลของรางวัลในตาราง Rewards ด้วยรหัสของรางวัล
        var reward = _db.Rewards.FirstOrDefault(r => r.RewardId == rewardId);

        // ตรวจสอบว่าไม่พบข้อมูลสมาชิกหรือของรางวัลในฐานข้อมูล
        if (user == null || reward == null)
        {
            // บันทึกข้อความแจ้งเตือนลง TempData
            TempData["ErrorMessage"] = "ไม่พบข้อมูลสมาชิกหรือของรางวัลที่ต้องการแลก";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบว่าสถานะของรางวัลถูกปิดให้บริการหรือไม่
        if (reward.IsAvailable == false)
        {
            // บันทึกข้อความแจ้งเตือนว่าสินค้าหมด
            TempData["ErrorMessage"] = "ขออภัย ของรางวัลชิ้นนี้หมดสต็อกชั่วคราว";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        // ตรวจสอบว่าแต้มสะสมของสมาชิกน้อยกว่าแต้มที่ต้องใช้หรือไม่
        if (user.Points < reward.PointsRequired)
        {
            // บันทึกข้อความแจ้งเตือนพร้อมระบุแต้มที่ขาด
            TempData["ErrorMessage"] = $"แต้มสะสมของคุณไม่เพียงพอ (ต้องการ {reward.PointsRequired} แต้ม แต่คุณมี {user.Points} แต้ม)";
            // สั่งเปลี่ยนเส้นทางกลับไปยังหน้าของรางวัล
            return RedirectToAction("Rewards");
        }

        // หักแต้มสะสมของสมาชิกตามจำนวนแต้มที่รางวัลต้องการ
        user.Points -= reward.PointsRequired;

        // สุ่มตัวเลข 6 หลักเพื่อใช้สร้างรหัสรับของรางวัล
        int randomSuffix = new Random().Next(100000, 999999);
        // ประกอบรหัสรับของรางวัลในรูปแบบ RWD-XXXXXX
        string redemptionCode = "RWD-" + randomSuffix;

        // สร้างออบเจกต์บันทึกการแลกของรางวัลใหม่
        var redemption = new Rewardredemption
        {
            // กำหนดรหัสรับของรางวัลที่สุ่มขึ้นมา
            RedemptionCode = redemptionCode,
            // บันทึกรหัสสมาชิกผู้ทำการแลก
            UserId = user.UserId,
            // บันทึกรหัสของรางวัลที่ถูกแลก
            RewardId = reward.RewardId,
            // บันทึกจำนวนแต้มที่สมาชิกใช้ไป
            PointsSpent = reward.PointsRequired,
            // บันทึกสถานะเริ่มต้นเป็นรอมารับของหน้าร้าน
            Status = "PendingPickup",
            // บันทึกวันและเวลาปัจจุบันที่ทำรายการแลก
            RedeemedAt = DateTime.Now
        };
        // เพิ่มข้อมูลการแลกของรางวัลลงใน DbSet ของ Context
        _db.Rewardredemptions.Add(redemption);
        // บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล MySQL
        _db.SaveChanges();

        // บันทึกข้อความแจ้งเตือนสีเขียวพร้อมรหัสรับของรางวัลลง TempData
        TempData["SuccessMessage"] = $"แลกของรางวัล \"{reward.Title}\" สำเร็จ! รหัสรับของรางวัลคือ {redemptionCode} (นำรหัสนี้ไปแจ้งพนักงานที่เคาน์เตอร์หน้าร้าน)";
        // สั่งเปลี่ยนเส้นทางกลับไปยังหน้ารายการของรางวัล
        return RedirectToAction("Rewards");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
