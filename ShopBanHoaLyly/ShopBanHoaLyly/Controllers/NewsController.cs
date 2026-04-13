using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ShopBanHoaLyly.Controllers
{
    public class NewsController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        private const int PageSize = 9; // Số tin tức trên mỗi trang

        public NewsController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            // Đảm bảo trang hiện tại hợp lệ
            if (page < 1)
            {
                page = 1;
            }

            // Đếm tổng số tin tức
            var totalItems = await _context.TinTucs
                .Where(t => t.TrangThaiHienThi == true)
                .CountAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Đảm bảo trang hiện tại không vượt quá tổng số trang
            if (page > totalPages && totalPages > 0)
            {
                page = totalPages;
            }

            // Lấy danh sách tin tức cho trang hiện tại
            var tinTucList = await _context.TinTucs
                .Include(t => t.MaTaiKhoanNavigation)
                .Where(t => t.TrangThaiHienThi == true)
                .OrderByDescending(t => t.NgayCapNhat)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Lưu thông tin phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(tinTucList);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
        {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs
                .Include(t => t.MaTaiKhoanNavigation)
                .FirstOrDefaultAsync(t => t.MaTinTuc == id && t.TrangThaiHienThi == true);

            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }
    }
}
