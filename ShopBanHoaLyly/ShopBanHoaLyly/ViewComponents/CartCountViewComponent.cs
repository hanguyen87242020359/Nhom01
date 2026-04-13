using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using System.Linq;

namespace ShopBanHoaLyly.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ShopHoaLyLyContext _context;

        public CartCountViewComponent(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var userName = HttpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Content("0");
            }

            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
            {
                return Content("0");
            }

            int cartCount = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
            return Content(cartCount.ToString());
        }
    }
} 