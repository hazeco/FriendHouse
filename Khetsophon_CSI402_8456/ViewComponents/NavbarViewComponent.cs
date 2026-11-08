using Microsoft.AspNetCore.Mvc;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Controllers;

public class NavbarViewComponent : ViewComponent
{
    private readonly Csi402dbContext _db;

    public NavbarViewComponent(Csi402dbContext db)
    {
        _db = db;
    }

    public IViewComponentResult Invoke(string role = "guest")
    {
        int cartCount = 0;
        int memberPoints = 0;
        string avatarLetter = "K";

        var customerId = HttpContext.Session.GetInt32("CustomerUserId")
            ?? (HttpContext.Session.GetString("Role") == "customer" ? HttpContext.Session.GetInt32("UserId") : null);

        if (customerId.HasValue)
        {
            // WHY: คำนวณจำนวนชิ้นสินค้าในตะกร้าจริงจากตาราง Cartitems ของลูกค้ารายนั้น
            cartCount = _db.Cartitems
                .Where(c => c.UserId == customerId.Value)
                .Sum(c => (int?)c.Quantity) ?? 0;

            var user = _db.Users.FirstOrDefault(u => u.UserId == customerId.Value);
            if (user != null)
            {
                memberPoints = user.Points;
                if (!string.IsNullOrWhiteSpace(user.FullName))
                {
                    avatarLetter = user.FullName.Trim()[0].ToString().ToUpper();
                }
                else if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    avatarLetter = user.Email.Trim()[0].ToString().ToUpper();
                }
            }
        }

        ViewBag.CartCount = cartCount;
        ViewBag.MemberPoints = memberPoints;
        ViewBag.AvatarLetter = avatarLetter;

        string viewName = (role?.ToLowerInvariant()) switch
        {
            "staff" => "Staff",
            "owner" => "Owner",
            "itsupport" or "it" => "ITSupport",
            "accountant" or "acc" => "Accountant",
            _ => "Default"
        };
        return View(viewName, model: role);
    }
}