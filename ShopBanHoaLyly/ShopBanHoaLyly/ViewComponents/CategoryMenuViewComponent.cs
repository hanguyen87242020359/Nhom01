using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;

namespace ShopBanHoaLyly.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ShopHoaLyLyContext _context;

        public CategoryMenuViewComponent(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.DanhMucs.ToListAsync();
            return View(categories);
        }
    }
} 