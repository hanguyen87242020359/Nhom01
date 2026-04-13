using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ShopBanHoaLyly.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Nhân viên")]
    public class CategoryController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        public CategoryController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.DanhMucs.ToList();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_CreateOrEdit", new DanhMuc());
        }

        [HttpPost]
        public IActionResult Create(DanhMuc model)
        {
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
            {
                ModelState.AddModelError("TenDanhMuc", "Vui lòng nhập tên danh mục");
            }
            else if (model.TenDanhMuc.Length > 100)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục không được vượt quá 100 ký tự");
            }
            
                // Kiểm tra tên danh mục đã tồn tại chưa
            if (!string.IsNullOrWhiteSpace(model.TenDanhMuc) && 
                _context.DanhMucs.Any(c => c.TenDanhMuc.ToLower() == model.TenDanhMuc.ToLower()))
                {
                    ModelState.AddModelError("TenDanhMuc", "Tên danh mục này đã tồn tại");
                }
                
            if (ModelState.IsValid)
            {
                _context.DanhMucs.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            
            // Luôn trả về PartialView khi có lỗi
            return PartialView("_CreateOrEdit", model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.DanhMucs.Find(id);
            if (category == null)
                return NotFound();
            return PartialView("_CreateOrEdit", category);
        }

        [HttpPost]
        public IActionResult Edit(DanhMuc model)
        {
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
            {
                ModelState.AddModelError("TenDanhMuc", "Vui lòng nhập tên danh mục");
            }
            else if (model.TenDanhMuc.Length > 100)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục không được vượt quá 100 ký tự");
            }
            
                var entity = _context.DanhMucs.Find(model.MaDanhMuc);
            if (entity == null)
                {
                ModelState.AddModelError("", "Không tìm thấy danh mục");
                return PartialView("_CreateOrEdit", model);
            }
            
                    // Kiểm tra tên danh mục đã tồn tại chưa (không tính trùng với chính nó)
            if (!string.IsNullOrWhiteSpace(model.TenDanhMuc) && 
                _context.DanhMucs.Any(c => c.TenDanhMuc.ToLower() == model.TenDanhMuc.ToLower() && c.MaDanhMuc != model.MaDanhMuc))
                    {
                        ModelState.AddModelError("TenDanhMuc", "Tên danh mục này đã tồn tại");
                    }
                    
            if (ModelState.IsValid)
            {
                    entity.TenDanhMuc = model.TenDanhMuc;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                
            // Luôn trả về PartialView khi có lỗi
            return PartialView("_CreateOrEdit", model);
        }

        [HttpGet]
        public IActionResult DeleteConfirm(int id)
        {
            var category = _context.DanhMucs.Find(id);
            if (category == null)
                return NotFound();
            return PartialView("_Delete", category);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var category = _context.DanhMucs.Find(id);
            if (category != null)
            {
                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
                var hasProducts = _context.SanPhams.Any(p => p.MaDanhMuc == id);
                
                if (hasProducts)
                {
                    return Json(new { 
                        success = false, 
                        message = "Không thể xóa danh mục này vì có sản phẩm liên quan. Vui lòng chuyển các sản phẩm sang danh mục khác trước khi xóa." 
                    });
                }
                
                try
                {
                    _context.DanhMucs.Remove(category);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { 
                        success = false, 
                        message = "Lỗi khi xóa danh mục: " + ex.Message 
                    });
                }
            }
            return Json(new { success = false, message = "Không tìm thấy danh mục" });
        }
    }
}
