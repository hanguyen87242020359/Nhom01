using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ShopBanHoaLyly.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Quản trị viên")]
    public class NewsController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public NewsController(ShopHoaLyLyContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/News
        public async Task<IActionResult> Index()
        {
            var tinTucs = await _context.TinTucs
                .Include(t => t.MaTaiKhoanNavigation)
                .OrderByDescending(t => t.NgayCapNhat)
                .ToListAsync();
            return View(tinTucs);
        }

        // GET: Admin/News/Create
        public IActionResult Create()
        {
            var tinTuc = new TinTuc();
            return PartialView("_CreateOrEdit", tinTuc);
        }

        // POST: Admin/News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create([Bind("TieuDe,TomTat,NoiDung,TrangThaiHienThi")] TinTuc tinTuc, IFormFile? HinhAnhFile)
        {
            // Bỏ qua validation cho navigation không có trong form
            ModelState.Remove("MaTaiKhoanNavigation");
            
            // Bỏ qua validation mặc định của các trường mà chúng ta tự thêm validation
            ModelState.Remove("TieuDe");
            ModelState.Remove("TomTat");
            ModelState.Remove("NoiDung");

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(tinTuc.TieuDe))
            {
                ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề tin tức");
            }
            else if (tinTuc.TieuDe.Length > 200)
            {
                ModelState.AddModelError("TieuDe", "Tiêu đề không được vượt quá 200 ký tự");
            }

            if (string.IsNullOrEmpty(tinTuc.TomTat))
            {
                ModelState.AddModelError("TomTat", "Vui lòng nhập tóm tắt tin tức");
            }

            if (string.IsNullOrEmpty(tinTuc.NoiDung))
            {
                ModelState.AddModelError("NoiDung", "Vui lòng nhập nội dung tin tức");
            }

            if (HinhAnhFile == null || HinhAnhFile.Length == 0)
            {
                ModelState.AddModelError("HinhAnhFile", "Vui lòng chọn hình ảnh đại diện");
            }
            else
            {
                // Kiểm tra định dạng file
                var extension = Path.GetExtension(HinhAnhFile.FileName).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                {
                    ModelState.AddModelError("HinhAnhFile", "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif");
                }
                
                // Kiểm tra kích thước file (tối đa 5MB)
                if (HinhAnhFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("HinhAnhFile", "Kích thước ảnh không được vượt quá 5MB");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy MaTaiKhoan từ người dùng đăng nhập (có thể không có claim UserId)
                    int? maTaiKhoan = null;
                    var claim = User.FindFirst("UserId");
                    if (claim != null && int.TryParse(claim.Value, out var parsedId))
                    {
                        maTaiKhoan = parsedId;
                    }
                    else if (!string.IsNullOrEmpty(User.Identity?.Name))
                    {
                        maTaiKhoan = _context.TaiKhoans
                                    .Where(t => t.TenTaiKhoan == User.Identity.Name)
                                    .Select(t => (int?)t.MaTaiKhoan)
                                    .FirstOrDefault();
                    }

                    if (!maTaiKhoan.HasValue)
                    {
                        return Json(new { success = false, message = "Không xác định được tài khoản đăng nhập" });
                    }
                    
                    tinTuc.MaTaiKhoan = maTaiKhoan.Value;
                    tinTuc.NgayCapNhat = DateTime.Now;

                    // Xử lý tải lên hình ảnh
                    if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Path.GetFileNameWithoutExtension(HinhAnhFile.FileName);
                        string extension = Path.GetExtension(HinhAnhFile.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        string path = Path.Combine(wwwRootPath, "images", "blog", fileName);
                        
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await HinhAnhFile.CopyToAsync(fileStream);
                        }
                        
                        tinTuc.HinhAnhDaiDien = fileName;
                    }

                    _context.Add(tinTuc);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Thêm tin tức thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
            
            // Tạo danh sách lỗi để trả về cho client
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToList() }
                );
            
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
        }

        // GET: Admin/News/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs.FindAsync(id);
            if (tinTuc == null)
            {
                return NotFound();
            }
            
            return PartialView("_CreateOrEdit", tinTuc);
        }

        // POST: Admin/News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Edit(int id, [Bind("MaTinTuc,TieuDe,TomTat,NoiDung,MaTaiKhoan,NgayCapNhat,TrangThaiHienThi,HinhAnhDaiDien")] TinTuc tinTuc, IFormFile? HinhAnhFile)
        {
            // Bỏ qua validation cho navigation không bind từ form
            ModelState.Remove("MaTaiKhoanNavigation");
            
            // Bỏ qua validation mặc định của các trường mà chúng ta tự thêm validation
            ModelState.Remove("TieuDe");
            ModelState.Remove("TomTat");
            ModelState.Remove("NoiDung");

            if (id != tinTuc.MaTinTuc)
            {
                return Json(new { success = false, message = "ID không khớp" });
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(tinTuc.TieuDe))
            {
                ModelState.AddModelError("TieuDe", "Vui lòng nhập tiêu đề tin tức");
            }
            else if (tinTuc.TieuDe.Length > 200)
            {
                ModelState.AddModelError("TieuDe", "Tiêu đề không được vượt quá 200 ký tự");
            }

            if (string.IsNullOrEmpty(tinTuc.TomTat))
            {
                ModelState.AddModelError("TomTat", "Vui lòng nhập tóm tắt tin tức");
            }

            if (string.IsNullOrEmpty(tinTuc.NoiDung))
            {
                ModelState.AddModelError("NoiDung", "Vui lòng nhập nội dung tin tức");
            }

            // Kiểm tra ảnh nếu có upload
            if (HinhAnhFile != null && HinhAnhFile.Length > 0)
            {
                // Kiểm tra định dạng file
                var extension = Path.GetExtension(HinhAnhFile.FileName).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                {
                    ModelState.AddModelError("HinhAnhFile", "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif");
                }
                
                // Kiểm tra kích thước file (tối đa 5MB)
                if (HinhAnhFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("HinhAnhFile", "Kích thước ảnh không được vượt quá 5MB");
                }
            }
            // Kiểm tra nếu không có ảnh cũ và không upload ảnh mới
            else if (string.IsNullOrEmpty(tinTuc.HinhAnhDaiDien))
            {
                ModelState.AddModelError("HinhAnhFile", "Vui lòng chọn hình ảnh đại diện");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật NgayCapNhat
                    tinTuc.NgayCapNhat = DateTime.Now;

                    // Xử lý tải lên hình ảnh mới
                    if (HinhAnhFile != null && HinhAnhFile.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(tinTuc.HinhAnhDaiDien))
                        {
                            string oldImagePath = Path.Combine(wwwRootPath, "images", "blog", tinTuc.HinhAnhDaiDien);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Lưu ảnh mới
                        string fileName = Path.GetFileNameWithoutExtension(HinhAnhFile.FileName);
                        string extension = Path.GetExtension(HinhAnhFile.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        string path = Path.Combine(wwwRootPath, "images", "blog", fileName);
                        
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await HinhAnhFile.CopyToAsync(fileStream);
                        }
                        
                        tinTuc.HinhAnhDaiDien = fileName;
                    }

                    _context.Update(tinTuc);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = "Cập nhật tin tức thành công!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TinTucExists(tinTuc.MaTinTuc))
                    {
                        return Json(new { success = false, message = "Không tìm thấy tin tức" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Lỗi cập nhật dữ liệu" });
                    }
                }
            }
            
            // Tạo danh sách lỗi để trả về cho client
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToList() }
                );
            
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
        }

        // GET: Admin/News/DeleteConfirm/5
        public async Task<IActionResult> DeleteConfirm(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs
                .Include(t => t.MaTaiKhoanNavigation)
                .FirstOrDefaultAsync(m => m.MaTinTuc == id);
                
            if (tinTuc == null)
            {
                return NotFound();
            }

            return PartialView("_Delete", tinTuc);
        }

        // POST: Admin/News/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var tinTuc = await _context.TinTucs.FindAsync(id);
            
            if (tinTuc != null)
            {
                try 
                {
                    // Xóa file ảnh nếu có
                    if (!string.IsNullOrEmpty(tinTuc.HinhAnhDaiDien))
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string oldImagePath = Path.Combine(wwwRootPath, "images", "blog", tinTuc.HinhAnhDaiDien);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    _context.TinTucs.Remove(tinTuc);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = "Xóa tin tức thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
                }
            }
            
            return Json(new { success = false, message = "Không tìm thấy tin tức" });
        }

        // GET: Admin/News/Preview/5
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var tinTuc = await _context.TinTucs
                .Include(t => t.MaTaiKhoanNavigation)
                .FirstOrDefaultAsync(t => t.MaTinTuc == id);
            if (tinTuc == null)
            {
                return NotFound();
            }
            // Thêm flag để biết đang xem từ trang admin
            ViewBag.IsAdminPreview = true;
            // Tái sử dụng view client để hiển thị chi tiết
            return View("~/Views/News/Details.cshtml", tinTuc);
        }

        private bool TinTucExists(int id)
        {
            return _context.TinTucs.Any(e => e.MaTinTuc == id);
        }
    }
} 