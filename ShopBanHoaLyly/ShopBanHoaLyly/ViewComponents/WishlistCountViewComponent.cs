using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using System.Security.Claims;
using System.Linq;

namespace ShopBanHoaLyly.ViewComponents
{
    public class WishlistCountViewComponent : ViewComponent
    {
        private readonly ShopHoaLyLyContext _context;

        public WishlistCountViewComponent(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var userName = HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Content("0"); // Trả về chuỗi "0"
            }

            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
            {
                return Content("0");
            }

            int wishlistCount = _context.YeuThiches.Count(y => y.MaKhachHang == user.MaTaiKhoan);
            return Content(wishlistCount.ToString());
        }
    }
}