using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using Microsoft.AspNetCore.Authentication;
using ShopBanHoaLyly.Services;
using System.Security.Claims;

namespace ShopBanHoaLyly.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Quản trị viên")]
    public class AccountController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        public AccountController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var accounts = _context.TaiKhoans
                .Include(tk => tk.MaQuyenNavigation)
                .ToList();
            return View(accounts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Quyens = _context.Quyens.ToList();
            return PartialView("_CreateOrEdit", new TaiKhoan() { TrangThaiTaiKhoan = true });
        }

        [HttpPost]
        public IActionResult Create(TaiKhoan model, string? ConfirmPassword = null)
        {
            ViewBag.Quyens = _context.Quyens.ToList();
            ViewBag.ConfirmPassword = ConfirmPassword;
            
            // Kiểm tra các trường bắt buộc
            // Kiểm tra tên tài khoản
            if (string.IsNullOrWhiteSpace(model.TenTaiKhoan))
            {
                ModelState.AddModelError("TenTaiKhoan", "Vui lòng nhập tên tài khoản");
            }
            else if (model.TenTaiKhoan.Length < 3)
            {
                ModelState.AddModelError("TenTaiKhoan", "Tên tài khoản phải có ít nhất 3 ký tự");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.TenTaiKhoan, @"^[a-zA-Z0-9_.]+$"))
            {
                ModelState.AddModelError("TenTaiKhoan", "Tên tài khoản chỉ được chứa chữ cái, số, dấu gạch dưới (_) hoặc dấu chấm (.), không chứa khoảng trắng.");
            }
            
            if (string.IsNullOrWhiteSpace(model.HoVaTen))
            {
                ModelState.AddModelError("HoVaTen", "Vui lòng nhập họ và tên");
            }

            if (string.IsNullOrWhiteSpace(model.HoVaTen) || model.HoVaTen.Trim().Length < 2)
            {
                ModelState.AddModelError("HoVaTen", "Họ và tên phải có ít nhất 2 ký tự");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.HoVaTen, @"^[\p{L}\s]+$"))
            {
                ModelState.AddModelError("HoVaTen", "Họ và tên chỉ được chứa chữ cái và khoảng trắng");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập email");
            }
            else if (!IsValidEmail(model.Email))
            {
                ModelState.AddModelError("Email", "Email không hợp lệ");
            }
            
            if (string.IsNullOrWhiteSpace(model.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Vui lòng nhập mật khẩu");
            }

            if (!string.IsNullOrWhiteSpace(model.MatKhau) && model.MatKhau.Length < 6)
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu phải có ít nhất 6 ký tự");
            }

            // Số điện thoại
            if (string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Vui lòng nhập số điện thoại");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.SoDienThoai, @"^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])[0-9]{7}$"))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không hợp lệ");
            }
            
            // Xóa lỗi tự động từ Model Binding
            ModelState.Remove("ConfirmPassword");
            
            // Thêm lỗi tùy chỉnh bằng tiếng Việt
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu");
            }
            else if (!string.IsNullOrWhiteSpace(model.MatKhau) && model.MatKhau != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");
            }
            
            if (!model.MaQuyen.HasValue || model.MaQuyen <= 0)
            {
                ModelState.AddModelError("MaQuyen", "Vui lòng chọn quyền");
            }
            
            // Kiểm tra trùng tên tài khoản
            if (!string.IsNullOrWhiteSpace(model.TenTaiKhoan) && 
                _context.TaiKhoans.Any(a => a.TenTaiKhoan.ToLower() == model.TenTaiKhoan.ToLower()))
            {
                ModelState.AddModelError("TenTaiKhoan", "Tên tài khoản đã tồn tại");
            }
            
            // Kiểm tra trùng email
            if (!string.IsNullOrWhiteSpace(model.Email) && 
                _context.TaiKhoans.Any(a => a.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
            }

            if (ModelState.IsValid)
            {
                model.MatKhau = PasswordHasher.Hash(model.MatKhau);
                _context.TaiKhoans.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            
            // Luôn trả về PartialView khi có lỗi
            return PartialView("_CreateOrEdit", model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var account = _context.TaiKhoans.Find(id);
            if (account == null)
                return NotFound();
            ViewBag.Quyens = _context.Quyens.ToList();
            return PartialView("_CreateOrEdit", account);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TaiKhoan model, string? ConfirmPassword = null)
        {
            ViewBag.Quyens = _context.Quyens.ToList();
            ViewBag.ConfirmPassword = ConfirmPassword;
            
            // Lấy tài khoản hiện tại
            var entity = _context.TaiKhoans.Find(model.MaTaiKhoan);
            if (entity == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản");
                return PartialView("_CreateOrEdit", model);
            }
            
            // Không cho phép đổi tên tài khoản
            model.TenTaiKhoan = entity.TenTaiKhoan;
            
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(model.HoVaTen))
            {
                ModelState.AddModelError("HoVaTen", "Vui lòng nhập họ và tên");
            }

            if (string.IsNullOrWhiteSpace(model.HoVaTen) || model.HoVaTen.Trim().Length < 2)
            {
                ModelState.AddModelError("HoVaTen", "Họ và tên phải có ít nhất 2 ký tự");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.HoVaTen, @"^[\p{L}\s]+$"))
            {
                ModelState.AddModelError("HoVaTen", "Họ và tên chỉ được chứa chữ cái và khoảng trắng");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập email");
            }
            else if (!IsValidEmail(model.Email))
            {
                ModelState.AddModelError("Email", "Email không hợp lệ");
            }
            
            // Số điện thoại
            if (string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Vui lòng nhập số điện thoại");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.SoDienThoai, @"^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])[0-9]{7}$"))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không hợp lệ");
            }
            
            // Xóa lỗi tự động từ Model Binding
            ModelState.Remove("ConfirmPassword");
            
            // Kiểm tra mật khẩu và xác nhận mật khẩu nếu có nhập mới
            if (!string.IsNullOrWhiteSpace(model.MatKhau))
            {
                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu");
                }
                else if (model.MatKhau != ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");
                }

            }

            if (!string.IsNullOrWhiteSpace(model.MatKhau) && model.MatKhau.Length < 6)
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu phải có ít nhất 6 ký tự");
            }

            if (!model.MaQuyen.HasValue || model.MaQuyen <= 0)
            {
                ModelState.AddModelError("MaQuyen", "Vui lòng chọn quyền");
            }
            
            // Kiểm tra trùng email (trừ chính nó)
            if (!string.IsNullOrWhiteSpace(model.Email) && 
                _context.TaiKhoans.Any(a => a.Email.ToLower() == model.Email.ToLower() && a.MaTaiKhoan != model.MaTaiKhoan))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
            }

            if (ModelState.IsValid)
            {
                // Lấy tên người dùng hiện tại
                var currentUserName = User.Identity.Name;
                bool isCurrentUser = entity.TenTaiKhoan == currentUserName;

                // Lưu lại quyền cũ và trạng thái cũ trước khi cập nhật
                var oldMaQuyen = entity.MaQuyen;
                var oldTrangThai = entity.TrangThaiTaiKhoan;
                var oldHoVaTen = entity.HoVaTen;

                // Cập nhật thông tin
                entity.HoVaTen = model.HoVaTen;
                entity.Email = model.Email;
                entity.SoDienThoai = model.SoDienThoai;
                // Chỉ cập nhật mật khẩu nếu có nhập mới
                if (!string.IsNullOrWhiteSpace(model.MatKhau))
                {
                    entity.MatKhau = PasswordHasher.Hash(model.MatKhau);
                }
                entity.TrangThaiTaiKhoan = model.TrangThaiTaiKhoan;
                entity.MaQuyen = model.MaQuyen;
                _context.SaveChanges();

                // Kiểm tra thay đổi quyền và trạng thái
                bool quyenChanged = oldMaQuyen != model.MaQuyen;
                bool biKhoa = oldTrangThai != model.TrangThaiTaiKhoan && model.TrangThaiTaiKhoan == false;
                bool hoVaTenChanged = oldHoVaTen != model.HoVaTen;

                if (isCurrentUser && (biKhoa || quyenChanged))
                {
                    await HttpContext.SignOutAsync();
                    return Json(new { success = true, forceLogout = true });
                }
                else if (isCurrentUser && hoVaTenChanged)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, entity.TenTaiKhoan),
                        new Claim("FullName", entity.HoVaTen ?? ""),
                        new Claim(ClaimTypes.Role, entity.MaQuyenNavigation?.TenQuyen ?? "")
                    };
                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(principal);
                    return Json(new { success = true, updateClaims = true });
                }

                return Json(new { success = true });
            }
            
            // Luôn trả về PartialView khi có lỗi
            return PartialView("_CreateOrEdit", model);
        }
        
        // Hàm kiểm tra email hợp lệ
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}