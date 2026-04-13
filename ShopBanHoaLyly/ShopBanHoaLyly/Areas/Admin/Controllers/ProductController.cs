using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace ShopBanHoaLyly.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Quản trị viên")]
    public class ProductController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        public ProductController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.SanPhams
                .IgnoreQueryFilters()
                .Include(p => p.MaDanhMucNavigation)
                .Include(p => p.HinhAnhs)
                .ToList();
            var categories = _context.DanhMucs.ToList();
            ViewBag.Categories = categories;
            return View(products);
        }

        [HttpPost]
        public IActionResult ToggleVisibility(int id)
        {
            var product = _context.SanPhams.IgnoreQueryFilters().FirstOrDefault(p => p.MaSanPham == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            try
            {
                // Dùng cờ DaXoa làm trạng thái ẩn/hiện: true = ẩn, false = hiển thị
                product.DaXoa = !product.DaXoa;
                _context.SaveChanges();
                return Json(new { success = true, hidden = product.DaXoa });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.DanhMucs.ToList();
            return PartialView("_CreateOrEdit", new SanPham() { SoLuongCon = 0 });
        }

        [HttpPost]
        public IActionResult Create(SanPham model, List<IFormFile> productImages)
        {
            ViewBag.Categories = _context.DanhMucs.ToList();
            
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Vui lòng nhập tên sản phẩm");
            }
            
            if (model.MaDanhMuc <= 0 || model.MaDanhMuc == null)
            {
                ModelState.AddModelError("MaDanhMuc", "Vui lòng chọn danh mục sản phẩm");
            }
            
            if (model.GiaBan <= 0 || model.GiaBan == null)
            {
                ModelState.AddModelError("GiaBan", "Vui lòng nhập giá bán và giá phải lớn hơn 0");
            }
            
            if (model.SoLuongCon == null)
            {
                ModelState.AddModelError("SoLuongCon", "Vui lòng nhập số lượng");
            }
            else if (model.SoLuongCon < 0)
            {
                ModelState.AddModelError("SoLuongCon", "Số lượng không được âm");
            }
            
            // Kiểm tra ảnh sản phẩm (bắt buộc khi thêm mới)
            if (productImages == null || productImages.Count == 0 || productImages.All(f => f.Length == 0))
            {
                ModelState.AddModelError("productImages", "Vui lòng chọn ít nhất một ảnh cho sản phẩm");
            }
            else
            {
                // Đếm số lượng ảnh hợp lệ
                int validImageCount = 0;
                foreach (var file in productImages)
                {
                    if (file.Length > 0)
                    {
                        // Kiểm tra định dạng file
                        var extension = Path.GetExtension(file.FileName).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif")
                        {
                            // Kiểm tra kích thước file (tối đa 5MB)
                            if (file.Length <= 5 * 1024 * 1024)
                            {
                                validImageCount++;
                            }
                        }
                    }
                }
                
                if (validImageCount == 0)
                {
                    ModelState.AddModelError("productImages", "Vui lòng chọn ít nhất một ảnh hợp lệ cho sản phẩm (JPG, PNG, GIF, tối đa 5MB)");
                }
            }

            // Kiểm tra tên sản phẩm đã tồn tại chưa
            if (!string.IsNullOrEmpty(model.TenSanPham) && 
                _context.SanPhams.Any(p => p.TenSanPham.ToLower() == model.TenSanPham.ToLower()))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm này đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.SanPhams.Add(model);
                    _context.SaveChanges();
                    
                    // Xử lý lưu ảnh mới
                    if (productImages != null && productImages.Count > 0)
                    {
                        foreach (var file in productImages)
                        {
                            if (file.Length > 0)
                            {
                                // Kiểm tra định dạng file
                                var extension = Path.GetExtension(file.FileName).ToLower();
                                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                                {
                                    continue; // Bỏ qua các file không phải ảnh
                                }
                                
                                // Kiểm tra kích thước file (tối đa 5MB)
                                if (file.Length > 5 * 1024 * 1024)
                                {
                                    continue; // Bỏ qua các file lớn hơn 5MB
                                }
                                
                                var ext = Path.GetExtension(file.FileName);
                                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                var newFileName = $"{fileName}_{DateTime.Now.Ticks}{ext}";
                                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product", newFileName);
                                
                                using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    file.CopyTo(stream);
                                }
                                
                                var img = new HinhAnh
                                {
                                    MaSanPham = model.MaSanPham,
                                    DuongDan = newFileName
                                };
                                _context.HinhAnhs.Add(img);
                            }
                        }
                        _context.SaveChanges();
                    }
                    
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi thêm sản phẩm: {ex.Message}");
                    return PartialView("_CreateOrEdit", model);
                }
            }
            
            return PartialView("_CreateOrEdit", model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _context.SanPhams
                .Include(p => p.HinhAnhs)
                .FirstOrDefault(p => p.MaSanPham == id);
            if (product == null)
                return NotFound();
            ViewBag.Categories = _context.DanhMucs.ToList();
            return PartialView("_CreateOrEdit", product);
        }

        [HttpPost]
        public IActionResult Edit(SanPham model, List<IFormFile> productImages, string? RemoveImageIds = null)
        {
            ViewBag.Categories = _context.DanhMucs.ToList();
            
            // Lấy thông tin sản phẩm hiện tại
            var entity = _context.SanPhams.Include(p => p.HinhAnhs).FirstOrDefault(p => p.MaSanPham == model.MaSanPham);
            if (entity == null)
            {
                ModelState.AddModelError("", "Không tìm thấy sản phẩm");
                return PartialView("_CreateOrEdit", model);
            }
            
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Vui lòng nhập tên sản phẩm");
            }
            
            if (model.MaDanhMuc <= 0)
            {
                ModelState.AddModelError("MaDanhMuc", "Vui lòng chọn danh mục sản phẩm");
            }
            
            if (model.GiaBan <= 0 || model.GiaBan == null)
            {
                ModelState.AddModelError("GiaBan", "Vui lòng nhập giá bán và giá phải lớn hơn 0");
            }
            
            if (model.SoLuongCon == null)
            {
                ModelState.AddModelError("SoLuongCon", "Vui lòng nhập số lượng");
            }
            else if (model.SoLuongCon < 0)
            {
                ModelState.AddModelError("SoLuongCon", "Số lượng không được âm");
            }
            
            // Kiểm tra tên sản phẩm đã tồn tại chưa (trừ chính nó)
            if (!string.IsNullOrEmpty(model.TenSanPham) && 
                _context.SanPhams.Any(p => p.TenSanPham.ToLower() == model.TenSanPham.ToLower() && p.MaSanPham != model.MaSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm này đã tồn tại");
            }
            
            // Kiểm tra nếu xóa hết ảnh cũ và không có ảnh mới
            bool willHaveNoImages = false;
            
            // Đếm số lượng ảnh sẽ còn lại sau khi xóa
            int remainingImagesCount = entity.HinhAnhs.Count();
            
            if (!string.IsNullOrEmpty(RemoveImageIds))
            {
                var ids = RemoveImageIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToList();
                
                // Trừ đi số lượng ảnh sẽ xóa
                remainingImagesCount -= ids.Count;
            }
            
            // Cộng thêm số lượng ảnh mới (nếu có)
            int newImagesCount = 0;
            if (productImages != null && productImages.Count > 0)
            {
                foreach (var file in productImages)
                {
                    if (file.Length > 0)
                    {
                        // Kiểm tra định dạng file
                        var extension = Path.GetExtension(file.FileName).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif")
                        {
                            // Kiểm tra kích thước file (tối đa 5MB)
                            if (file.Length <= 5 * 1024 * 1024)
                            {
                                newImagesCount++;
                            }
                        }
                    }
                }
            }
            
            // Tổng số ảnh sau khi cập nhật
            int totalImagesAfterUpdate = remainingImagesCount + newImagesCount;
            
            // Nếu không còn ảnh nào, báo lỗi
            if (totalImagesAfterUpdate <= 0)
            {
                willHaveNoImages = true;
            }

            if (willHaveNoImages)
            {
                ModelState.AddModelError("productImages", "Sản phẩm phải có ít nhất một ảnh");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    entity.TenSanPham = model.TenSanPham;
                    entity.GiaBan = model.GiaBan;
                    entity.SoLuongCon = model.SoLuongCon;
                    entity.MoTa = model.MoTa;
                    entity.MaDanhMuc = model.MaDanhMuc;
                    
                    // Xử lý xóa ảnh cũ
                    if (!string.IsNullOrEmpty(RemoveImageIds))
                    {
                        var ids = RemoveImageIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse).ToList();
                        var imagesToRemove = entity.HinhAnhs.Where(h => ids.Contains(h.MaAnh)).ToList();
                        
                        foreach (var img in imagesToRemove)
                        {
                            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product", img.DuongDan);
                            if (System.IO.File.Exists(imgPath))
                                System.IO.File.Delete(imgPath);
                            _context.HinhAnhs.Remove(img);
                        }
                    }
                    
                    // Xử lý lưu ảnh mới
                    if (productImages != null && productImages.Count > 0)
                    {
                        foreach (var file in productImages)
                        {
                            if (file.Length > 0)
                            {
                                // Kiểm tra định dạng file
                                var extension = Path.GetExtension(file.FileName).ToLower();
                                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                                {
                                    continue; // Bỏ qua các file không phải ảnh
                                }
                                
                                // Kiểm tra kích thước file (tối đa 5MB)
                                if (file.Length > 5 * 1024 * 1024)
                                {
                                    continue; // Bỏ qua các file lớn hơn 5MB
                                }
                                
                                var ext = Path.GetExtension(file.FileName);
                                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                var newFileName = $"{fileName}_{DateTime.Now.Ticks}{ext}";
                                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product", newFileName);
                                
                                using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    file.CopyTo(stream);
                                }
                                
                                var img = new HinhAnh
                                {
                                    MaSanPham = entity.MaSanPham,
                                    DuongDan = newFileName
                                };
                                _context.HinhAnhs.Add(img);
                            }
                        }
                    }
                    
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi cập nhật sản phẩm: {ex.Message}");
                    // Gán lại danh sách ảnh để hiển thị lại trong form
                    model.HinhAnhs = entity.HinhAnhs;
                    return PartialView("_CreateOrEdit", model);
                }
            }
            
            // Gán lại danh sách ảnh hiện tại để hiển thị lại trong form
            model.HinhAnhs = entity.HinhAnhs;
            return PartialView("_CreateOrEdit", model);
        }

        [HttpGet]
        public IActionResult DeleteConfirm(int id)
        {
            var product = _context.SanPhams.Find(id);
            if (product == null)
                return NotFound();
            return PartialView("_Delete", product);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            try
            {
                // Thực hiện xoá mềm (đánh dấu đã xoá) ngay cả khi sản phẩm đã nằm trong đơn hàng
                product.DaXoa = true;
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = "Lỗi khi xóa sản phẩm: " + ex.Message
                });
            }
        }
    }
}
