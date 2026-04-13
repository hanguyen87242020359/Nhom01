using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace ShopBanHoaLyly.Controllers
{
    public class CartController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        public CartController(ShopHoaLyLyContext context)
        {
            _context = context;
        }
        
        [Authorize]
        public IActionResult Index()
        {
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null) return RedirectToAction("Login", "Home");
            var cart = _context.GioHangs
                .Where(g => g.MaKhachHang == user.MaTaiKhoan)
                .Include(g => g.MaSanPhamNavigation)
                .ThenInclude(sp => sp.HinhAnhs)
                .Select(g => new CartItemViewModel
                {
                    MaSanPham = g.MaSanPham,
                    TenSanPham = g.MaSanPhamNavigation.TenSanPham,
                    GiaBan = g.MaSanPhamNavigation.GiaBan,
                    SoLuong = g.SoLuong ?? 1,
                    HinhAnhDaiDien = g.MaSanPhamNavigation.HinhAnhs.FirstOrDefault().DuongDan,
                    SoLuongCon = g.MaSanPhamNavigation.SoLuongCon,
                    ChonMua = true
                })
                .ToList();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCartAjax(int id, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng" });
            }
            
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var cartItem = _context.GioHangs.FirstOrDefault(g => g.MaKhachHang == user.MaTaiKhoan && g.MaSanPham == id);
            if (cartItem == null)
            {
                var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.MaSanPham == id);
                if (sanPham == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });

                int tonKho = sanPham.SoLuongCon ?? 0;
                bool adjusted = false;
                if (quantity > tonKho)
                {
                    quantity = tonKho;
                    adjusted = true;
                }

                if (quantity == 0)
                {
                    return Json(new { success = false, message = "Sản phẩm đã hết hàng." });
                }

                cartItem = new GioHang
                {
                    MaKhachHang = user.MaTaiKhoan,
                    MaSanPham = id,
                    SoLuong = quantity
                };
                _context.GioHangs.Add(cartItem);
                _context.SaveChanges();
                var count = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                var msg = adjusted ? "Số lượng đã được điều chỉnh theo tồn kho." : "Đã thêm vào giỏ hàng!";
                return Json(new { success = true, message = msg, count });
            }
            else
            {
                var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.MaSanPham == id);
                int tonKho = sanPham?.SoLuongCon ?? 0;
                int newQty = (cartItem.SoLuong ?? 1) + quantity;
                if (newQty > tonKho)
                {
                    cartItem.SoLuong = tonKho;
                    _context.SaveChanges();
                    var count = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                    return Json(new { success = false, message = $"Chỉ còn {tonKho} sản phẩm trong kho.", count });
                }

                cartItem.SoLuong = newQty;
                _context.SaveChanges();
                var countOk = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                return Json(new { success = true, message = "Đã tăng số lượng sản phẩm trong giỏ hàng!", count = countOk });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCartAjax(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập để xóa sản phẩm khỏi giỏ hàng" });
            }
            
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var cartItem = _context.GioHangs.FirstOrDefault(g => g.MaKhachHang == user.MaTaiKhoan && g.MaSanPham == id);
            if (cartItem != null)
            {
                _context.GioHangs.Remove(cartItem);
                _context.SaveChanges();
                var count = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                return Json(new { success = true, message = "Đã xóa khỏi giỏ hàng!", count });
            }
            var countFail = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng.", count = countFail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantityAjax(int id, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập để cập nhật số lượng" });
            }
            
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng này." });

            var cartItem = _context.GioHangs
                .Include(g => g.MaSanPhamNavigation)
                .FirstOrDefault(g => g.MaKhachHang == user.MaTaiKhoan && g.MaSanPham == id);
                
            if (cartItem != null)
            {
                if (quantity < 1)
                {
                    cartItem.SoLuong = 1;
                    _context.SaveChanges();
                    var subtotal = cartItem.MaSanPhamNavigation.GiaBan ?? 0;
                    var total = _context.GioHangs
                        .Include(g => g.MaSanPhamNavigation)
                        .Where(g => g.MaKhachHang == user.MaTaiKhoan)
                        .Sum(g => (g.MaSanPhamNavigation.GiaBan ?? 0) * (g.SoLuong ?? 1));
                    var count = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                    return Json(new { success = false, message = "Số lượng tối thiểu là 1.", newQuantity = 1, subtotal, total, count });
                }
                // Kiểm tra tồn kho
                int tonKho = cartItem.MaSanPhamNavigation.SoLuongCon ?? 0;
                if (quantity > tonKho)
                {
                    cartItem.SoLuong = tonKho;
                    _context.SaveChanges();

                    var subtotalWarn = (cartItem.MaSanPhamNavigation.GiaBan ?? 0) * tonKho;
                    var totalWarn = _context.GioHangs
                        .Include(g => g.MaSanPhamNavigation)
                        .Where(g => g.MaKhachHang == user.MaTaiKhoan)
                        .Sum(g => (g.MaSanPhamNavigation.GiaBan ?? 0) * (g.SoLuong ?? 1));
                    var countWarn = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                    return Json(new { success = false, message = $"Chỉ còn {tonKho} sản phẩm trong kho.", newQuantity = tonKho, subtotal = subtotalWarn, total = totalWarn, count = countWarn });
                }

                cartItem.SoLuong = quantity;
                _context.SaveChanges();
                var subtotal2 = (cartItem.MaSanPhamNavigation.GiaBan ?? 0) * quantity;
                var total2 = _context.GioHangs
                    .Include(g => g.MaSanPhamNavigation)
                    .Where(g => g.MaKhachHang == user.MaTaiKhoan)
                    .Sum(g => (g.MaSanPhamNavigation.GiaBan ?? 0) * (g.SoLuong ?? 1));
                var count2 = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
                return Json(new { success = true, message = "Đã cập nhật số lượng!", newQuantity = quantity, subtotal = subtotal2, total = total2, count = count2 });
            }
            var countFail = _context.GioHangs.Where(g => g.MaKhachHang == user.MaTaiKhoan).Sum(g => g.SoLuong ?? 1);
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng.", count = countFail });
        }

        // Loại bỏ các sản phẩm đã hết hàng trong giỏ – gọi qua Ajax
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult CleanOutOfStockAjax()
        {
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null)
            {
                return StatusCode(401, new { message = "Bạn cần đăng nhập" });
            }

            var toRemove = _context.GioHangs
                .Include(g => g.MaSanPhamNavigation)
                .Where(g => g.MaKhachHang == user.MaTaiKhoan && (g.MaSanPhamNavigation.SoLuongCon ?? 0) <= 0)
                .ToList();

            int removed = toRemove.Count;
            if (removed > 0)
            {
                _context.GioHangs.RemoveRange(toRemove);
                _context.SaveChanges();
            }

            return Json(new { success = true, removedCount = removed, message = removed > 0 ? $"Đã xoá {removed} sản phẩm hết hàng khỏi giỏ hàng." : string.Empty });
        }
    }
}
