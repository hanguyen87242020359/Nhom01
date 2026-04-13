using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System.Linq;

namespace ShopBanHoaLyly.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ShopHoaLyLyContext _context;

        public WishlistController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách yêu thích
        [Authorize]
        public IActionResult Index()
        {
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null) return RedirectToAction("Login", "Home");

            var wishlist = _context.YeuThiches
                .Where(y => y.MaKhachHang == user.MaTaiKhoan)
                .Include(y => y.MaSanPhamNavigation)
                .ThenInclude(sp => sp.HinhAnhs)
                .OrderByDescending(y => y.NgayThem) // Sắp xếp theo ngày thêm giảm dần
                .Select(y => new WishlistItemViewModel
                {
                    MaSanPham = y.MaSanPham,
                    TenSanPham = y.MaSanPhamNavigation.TenSanPham,
                    GiaBan = y.MaSanPhamNavigation.GiaBan,
                    HinhAnhDaiDien = y.MaSanPhamNavigation.HinhAnhs.FirstOrDefault().DuongDan,
                    NgayThem = y.NgayThem
                })
                .ToList();

            return View(wishlist);
        }

        // Thêm sản phẩm vào yêu thích
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToWishlistAjax(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập để thêm sản phẩm vào danh sách yêu thích" });
            }
            
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var exists = _context.YeuThiches.Any(y => y.MaKhachHang == user.MaTaiKhoan && y.MaSanPham == id);
            if (!exists)
            {
                var yeuThich = new YeuThich
                {
                    MaKhachHang = user.MaTaiKhoan,
                    MaSanPham = id,
                    NgayThem = DateTime.Now
                };
                _context.YeuThiches.Add(yeuThich);
                _context.SaveChanges();
                // Đếm lại số lượng wishlist
                var count = _context.YeuThiches.Count(y => y.MaKhachHang == user.MaTaiKhoan);
                return Json(new { success = true, message = "Đã thêm vào danh sách yêu thích!", count });
            }
            else
            {
                var count = _context.YeuThiches.Count(y => y.MaKhachHang == user.MaTaiKhoan);
                return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích.", count });
            }
        }

        // Xóa sản phẩm khỏi yêu thích
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromWishlistAjax(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập để xóa sản phẩm khỏi danh sách yêu thích" });
            }
            
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var item = _context.YeuThiches.FirstOrDefault(y => y.MaKhachHang == user.MaTaiKhoan && y.MaSanPham == id);
            if (item != null)
            {
                _context.YeuThiches.Remove(item);
                _context.SaveChanges();
                var count = _context.YeuThiches.Count(y => y.MaKhachHang == user.MaTaiKhoan);
                return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích!", count });
            }
            var countFail = _context.YeuThiches.Count(y => y.MaKhachHang == user.MaTaiKhoan);
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong danh sách yêu thích.", count = countFail });
        }
    }
}