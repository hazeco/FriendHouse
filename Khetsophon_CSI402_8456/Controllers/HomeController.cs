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

        var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
        ViewBag.UserPhone = user?.Phone ?? "";

        var items = _db.Cartitems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        return View(items);
    }

    // CREATE: เพิ่มสินค้าลงในตะกร้า (Add To Cart)
    // WHY: ตรวจสอบสิทธิ์ล็อกอิน หากยังไม่ได้ล็อกอินจะส่ง requireLogin แจ้งเตือนและส่งไปยังหน้า Login
    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity = 1)
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, requireLogin = true, message = "กรุณาเข้าสู่ระบบก่อนเพื่อเพิ่มสินค้าลงในตะกร้า", redirectUrl = "/Account/Login" });
        }

        // รองรับรหัสสินค้าดั้งเดิมจาก Mock (201 -> 1, 202 -> 2, เป็นต้น)
        if (productId >= 201 && productId <= 205)
        {
            productId = productId - 200;
        }

        var product = _db.Products.Include(p => p.Inventory).FirstOrDefault(p => p.ProductId == productId)
            ?? _db.Products.Include(p => p.Inventory).FirstOrDefault();

        if (product == null)
        {
            return Json(new { success = false, message = "ไม่พบสินค้านี้ในระบบ" });
        }

        int availableQty = (product.Inventory?.QuantityOnHand ?? 0) - (product.Inventory?.ReservedQuantity ?? 0);
        if (availableQty <= 0)
        {
            return Json(new { success = false, message = "ขออภัย สินค้านี้ถูกจองเต็มจำนวนแล้ว (สินค้าหมดชั่วคราว)" });
        }

        var existingItem = _db.Cartitems.FirstOrDefault(c => c.UserId == userId.Value && c.ProductId == product.ProductId);
        int requestedQty = Math.Max(1, quantity);
        int currentInCart = existingItem?.Quantity ?? 0;
        if (currentInCart + requestedQty > availableQty)
        {
            return Json(new { success = false, message = $"สินค้าคงเหลือพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้น (ในตะกร้ามีแล้ว {currentInCart} ชิ้น)" });
        }

        if (existingItem != null)
        {
            existingItem.Quantity += requestedQty;
        }
        else
        {
            _db.Cartitems.Add(new Cartitem
            {
                UserId = userId.Value,
                ProductId = product.ProductId,
                Quantity = requestedQty,
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.SaveChanges();
        return Json(new { success = true, message = $"เพิ่ม \"{product.Name}\" ลงในตะกร้าเรียบร้อยแล้ว" });
    }

    // UPDATE: อัปเดตจำนวนสินค้าในตะกร้า (Update Cart Quantity)
    [HttpPost]
    public IActionResult UpdateCartQuantity(int cartItemId, int quantity)
    {
        var item = _db.Cartitems.Include(c => c.Product).ThenInclude(p => p.Inventory).FirstOrDefault(c => c.CartItemId == cartItemId);
        if (item == null)
        {
            return Json(new { success = false, message = "ไม่พบรายการในตะกร้า" });
        }

        if (quantity <= 0)
        {
            _db.Cartitems.Remove(item);
        }
        else
        {
            int availableQty = (item.Product?.Inventory?.QuantityOnHand ?? 0) - (item.Product?.Inventory?.ReservedQuantity ?? 0);
            if (quantity > availableQty)
            {
                return Json(new { success = false, message = $"สินค้าพร้อมให้สั่งจองมีเพียง {availableQty} ชิ้นเท่านั้น", quantity = item.Quantity });
            }
            item.Quantity = quantity;
        }

        _db.SaveChanges();
        return Json(new { success = true, quantity = item.Quantity });
    }

    // DELETE: ลบสินค้าออกจากตะกร้า (Remove From Cart)
    [HttpPost]
    public IActionResult RemoveFromCart(int cartItemId)
    {
        var item = _db.Cartitems.FirstOrDefault(c => c.CartItemId == cartItemId);
        if (item != null)
        {
            _db.Cartitems.Remove(item);
            _db.SaveChanges();
            return Json(new { success = true, message = "ลบรายการออกจากตะกร้าแล้ว" });
        }

        return Json(new { success = false, message = "ไม่พบรายการ" });
    }

    // DELETE: ล้างตะกร้าทั้งหมด (Clear Cart)
    [HttpPost]
    public IActionResult ClearCart()
    {
        var userId = GetCurrentCustomerId();
        if (userId.HasValue)
        {
            var items = _db.Cartitems.Where(c => c.UserId == userId.Value).ToList();
            _db.Cartitems.RemoveRange(items);
            _db.SaveChanges();
        }

        return Json(new { success = true });
    }

    // CREATE: ยืนยันการสั่งจองพรีออเดอร์จากตะกร้า (Confirm Preorder)
    [HttpPost]
    public IActionResult ConfirmPreorder(string? contactPhone, string? pickupBranch)
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนยืนยันการจอง";
            return RedirectToAction("Login", "Account");
        }

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            var cartItems = _db.Cartitems.Include(c => c.Product).Where(c => c.UserId == userId.Value).ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "ไม่มีสินค้าในตะกร้า กรุณาเลือกสินค้าก่อนยืนยันการจอง";
                return RedirectToAction("Collection");
            }

            // Race Condition Check: ตรวจสอบสต็อกพร้อมจองแบบสดๆ ใน Transaction
            foreach (var item in cartItems)
            {
                var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
                if (inv == null || available < item.Quantity)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น) กรุณาปรับจำนวนในตะกร้า";
                    return RedirectToAction("Cart");
                }
            }

            decimal total = cartItems.Sum(c => c.Product.RetailPrice * c.Quantity);
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);

            var preorder = new Preorder
            {
                ReservationNumber = reservationNo,
                UserId = userId.Value,
                ContactPhone = !string.IsNullOrWhiteSpace(contactPhone) ? contactPhone.Trim() : (!string.IsNullOrWhiteSpace(user?.Phone) ? user.Phone.Trim() : "081-234-5678"),
                EstimatedPickupDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                CurrentStatus = "ReadyForPickup",
                TotalFinalPrice = total,
                PickupBranch = pickupBranch ?? "FriendHouse HQ สาขาสยามสแควร์",
                CreatedAt = DateTime.Now
            };

            _db.Preorders.Add(preorder);
            _db.SaveChanges();

            foreach (var item in cartItems)
            {
                _db.Preorderitems.Add(new Preorderitem
                {
                    PreorderId = preorder.PreorderId,
                    ProductId = item.ProductId,
                    UnitPrice = item.Product.RetailPrice,
                    Quantity = item.Quantity
                });

                // สำรองยอดจองในคลัง (Phase 1)
                var inv = _db.Inventories.First(i => i.ProductId == item.ProductId);
                inv.ReservedQuantity += item.Quantity;
            }

            // เคลียร์ตะกร้าเมื่อสั่งจองสำเร็จ
            _db.Cartitems.RemoveRange(cartItems);
            _db.SaveChanges();

            transaction.Commit();

            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว สามารถมารับสินค้าได้ที่หน้าร้าน";
            return RedirectToAction("PreorderStatus");
        }
        catch (Exception)
        {
            transaction.Rollback();
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
            return RedirectToAction("Cart");
        }
    }

    // CREATE: สั่งจองสินค้าโดยตรงจากหน้า Product Detail (Direct Preorder)
    [HttpPost]
    public IActionResult DirectPreorder(int productId, int quantity = 1, string? contactPhone = null, string? pickupDate = null)
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำรายการสั่งจองสินค้า";
            return RedirectToAction("Login", "Account");
        }

        if (productId >= 201 && productId <= 205)
        {
            productId = productId - 200;
        }

        var product = _db.Products.FirstOrDefault(p => p.ProductId == productId)
            ?? _db.Products.FirstOrDefault();

        if (product == null)
        {
            TempData["ErrorMessage"] = "ไม่พบสินค้าที่ต้องการสั่งจอง";
            return RedirectToAction("Collection");
        }

        int qty = Math.Max(1, quantity);

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            var inv = _db.Inventories.FirstOrDefault(i => i.ProductId == product.ProductId);
            int available = (inv?.QuantityOnHand ?? 0) - (inv?.ReservedQuantity ?? 0);
            if (inv == null || available < qty)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = $"ขออภัย สินค้า \"{product.Name}\" มีจำนวนคงเหลือพร้อมจองไม่เพียงพอ (เหลือ {Math.Max(0, available)} ชิ้น)";
                return RedirectToAction("ProductDetail", new { id = product.ProductId });
            }

            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            var reservationNo = "PO-2026-" + new Random().Next(1000, 9999);
            decimal totalPrice = product.RetailPrice * qty;

            DateOnly estDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            if (!string.IsNullOrEmpty(pickupDate) && DateOnly.TryParse(pickupDate, out var parsedDate))
            {
                if (parsedDate < DateOnly.FromDateTime(DateTime.Today))
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "ไม่สามารถเลือกวันที่ย้อนหลังได้ กรุณาเลือกวันปัจจุบันเป็นต้นไป";
                    return RedirectToAction("ProductDetail", new { id = product.ProductId });
                }
                estDate = parsedDate;
            }

            var preorder = new Preorder
            {
                ReservationNumber = reservationNo,
                UserId = userId.Value,
                ContactPhone = !string.IsNullOrWhiteSpace(contactPhone) ? contactPhone.Trim() : (!string.IsNullOrWhiteSpace(user?.Phone) ? user.Phone.Trim() : "081-234-5678"),
                EstimatedPickupDate = estDate,
                CurrentStatus = "ReadyForPickup",
                TotalFinalPrice = totalPrice,
                PickupBranch = "FriendHouse HQ สาขาสยามสแควร์",
                CreatedAt = DateTime.Now
            };

            _db.Preorders.Add(preorder);
            _db.SaveChanges();

            _db.Preorderitems.Add(new Preorderitem
            {
                PreorderId = preorder.PreorderId,
                ProductId = product.ProductId,
                UnitPrice = product.RetailPrice,
                Quantity = qty
            });

            // สำรองยอดจองในคลัง (Phase 1)
            inv.ReservedQuantity += qty;
            _db.SaveChanges();

            transaction.Commit();

            TempData["SuccessMessage"] = $"บันทึกคำสั่งจองพรีออเดอร์ #{reservationNo} เรียบร้อยแล้ว มารับสินค้าและชำระเงินที่หน้าร้านได้ทันที";
            return RedirectToAction("PreorderStatus");
        }
        catch (Exception)
        {
            transaction.Rollback();
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกคำสั่งจอง กรุณาลองใหม่อีกครั้ง";
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
        var customerId = GetCurrentCustomerId();
        var user = customerId.HasValue ? _db.Users.Find(customerId.Value) : null;
        ViewBag.UserPoints = user?.Points ?? 0;
        ViewBag.IsLoggedIn = customerId.HasValue;

        if (customerId.HasValue)
        {
            ViewBag.UserRedemptions = _db.Rewardredemptions
                .Include(r => r.Reward)
                .Where(r => r.UserId == customerId.Value)
                .OrderByDescending(r => r.RedeemedAt)
                .ToList();
        }

        var rewards = _db.Rewards
            .Where(r => r.IsAvailable == true || r.IsAvailable == null)
            .OrderBy(r => r.PointsRequired)
            .ToList();
        return View(rewards);
    }

    // CREATE: แลกของรางวัลด้วยแต้มสะสม (Redeem Reward with Member Points)
    [HttpPost]
    public IActionResult RedeemReward(int rewardId)
    {
        var userId = GetCurrentCustomerId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, requireLogin = true, message = "กรุณาเข้าสู่ระบบก่อนทำการแลกของรางวัล", redirectUrl = "/Account/Login" });
        }

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            var user = _db.Users.Find(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "ไม่พบบัญชีผู้ใช้ในระบบ" });
            }

            var reward = _db.Rewards.Find(rewardId);
            if (reward == null || (reward.IsAvailable == false))
            {
                return Json(new { success = false, message = "ไม่พบของรางวัลที่เลือก หรือของรางวัลถูกปิดการใช้งาน" });
            }

            if (reward.StockQuantity <= 0)
            {
                return Json(new { success = false, message = "ขออภัย ของรางวัลชิ้นนี้หมดชั่วคราว" });
            }

            if (user.Points < reward.PointsRequired)
            {
                return Json(new { success = false, message = $"แต้มสะสมของคุณไม่เพียงพอ (ต้องการ {reward.PointsRequired:N0} แต้ม แต่คุณมี {user.Points:N0} แต้ม)" });
            }

            // ตัดแต้มสมาชิก และลดยอดสต็อกของรางวัล
            user.Points -= reward.PointsRequired;
            reward.StockQuantity = Math.Max(0, reward.StockQuantity - 1);

            // สร้างรหัสแลกของรางวัล (Unique Redemption Code)
            string redemptionCode = "RWD-2026-" + new Random().Next(1000, 9999);

            var redemption = new Rewardredemption
            {
                RedemptionCode = redemptionCode,
                UserId = user.UserId,
                RewardId = reward.RewardId,
                PointsSpent = reward.PointsRequired,
                Status = "PendingPickup",
                RedeemedAt = DateTime.Now
            };

            _db.Rewardredemptions.Add(redemption);
            _db.SaveChanges();
            transaction.Commit();

            return Json(new
            {
                success = true,
                redemptionCode = redemptionCode,
                rewardTitle = reward.Title,
                remainingPoints = user.Points,
                message = $"แลกรับ \"{reward.Title}\" สำเร็จ! รหัสรับของรางวัลคือ {redemptionCode} กรุณาแจ้งรหัสนี้ที่เคาน์เตอร์หน้าร้าน"
            });
        }
        catch (Exception)
        {
            transaction.Rollback();
            return Json(new { success = false, message = "เกิดข้อผิดพลาดในการทำรายการแลกของรางวัล กรุณาลองใหม่อีกครั้ง" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
