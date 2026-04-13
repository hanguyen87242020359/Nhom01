using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using ShopBanHoaLyly.Services;

[Authorize]
public class ClientAccountController : Controller
{
    private readonly ShopHoaLyLyContext _context;
    private readonly ILogger<ClientAccountController> _logger;

    public ClientAccountController(ShopHoaLyLyContext context, ILogger<ClientAccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name;

        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("Không thể lấy tên người dùng trong ClientAccountController.Index.");
            TempData["ErrorMessage"] = "Không thể xác định người dùng. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Home");
        }

        var taiKhoan = await _context.TaiKhoans
            .Include(t => t.MaPhuongXaNavigation)
                .ThenInclude(px => px.MaQuanHuyenNavigation)
            .FirstOrDefaultAsync(t => t.TenTaiKhoan == userName);

        if (taiKhoan == null)
        {
            _logger.LogWarning($"Không tìm thấy tài khoản: {userName}.");
            TempData["ErrorMessage"] = "Tài khoản không tồn tại.";
            return RedirectToAction("Index", "Home");
        }

        // Lấy danh sách đơn hàng của người dùng
        var donHangList = await _context.DonHangs
            .Where(d => d.MaTaiKhoan == taiKhoan.MaTaiKhoan)
            .Include(d => d.MaTrangThaiDonHangNavigation)
            .Include(d => d.ChiTietDonHangs)
            .OrderByDescending(d => d.NgayDatHang)
            .ToListAsync();

        var viewModel = new AccountViewModel
        {
            MaTaiKhoan = taiKhoan.MaTaiKhoan,
            TenTaiKhoan = taiKhoan.TenTaiKhoan,
            HoVaTen = taiKhoan.HoVaTen,
            Email = taiKhoan.Email,
            SoDienThoai = taiKhoan.SoDienThoai,
            DiaChi = taiKhoan.DiaChi,
            MaPhuongXa = taiKhoan.MaPhuongXa ?? 0,
            MaQuanHuyen = taiKhoan.MaPhuongXaNavigation?.MaQuanHuyen ?? 0,
            TenPhuongXa = taiKhoan.MaPhuongXaNavigation?.TenPhuongXa,
            TenQuanHuyen = taiKhoan.MaPhuongXaNavigation?.MaQuanHuyenNavigation?.TenQuanHuyen,
            QuanHuyenList = await GetQuanHuyenListAsync(),
            DanhSachDonHang = donHangList
        };

        viewModel.PhuongXaList = viewModel.MaQuanHuyen > 0
            ? await GetPhuongXaListAsync(viewModel.MaQuanHuyen)
            : new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Chọn Phường/Xã --" } };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AccountViewModel viewModel)
    {
        var userName = User.Identity?.Name;

        if (string.IsNullOrEmpty(userName) || userName != viewModel.TenTaiKhoan)
        {
            _logger.LogWarning($"Người dùng {userName} cố cập nhật tài khoản {viewModel.TenTaiKhoan} không hợp lệ.");
            TempData["ErrorMessage"] = "Bạn không có quyền sửa tài khoản này.";
            return RedirectToAction("Index", "ClientAccount");
        }

        ModelState.Remove(nameof(viewModel.TenPhuongXa));
        ModelState.Remove(nameof(viewModel.TenQuanHuyen));

        if (string.IsNullOrEmpty(viewModel.MatKhauMoi)) // Nếu không đổi mật khẩu
        {
            ModelState.Remove(nameof(viewModel.MatKhauMoi));
            ModelState.Remove(nameof(viewModel.XacNhanMatKhauMoi));
            ModelState.Remove(nameof(viewModel.MatKhauHienTai)); // QUAN TRỌNG
        }
        else // Nếu có đổi mật khẩu
        {
            // Kiểm tra MatKhauHienTai ở đây
            if (string.IsNullOrEmpty(viewModel.MatKhauHienTai))
            {
                ModelState.AddModelError(nameof(viewModel.MatKhauHienTai), "Vui lòng nhập mật khẩu hiện tại để đổi mật khẩu mới.");
            }
            else
            {
                // ... logic kiểm tra mật khẩu hiện tại có đúng không ...
                var currentUserName = User.Identity?.Name;
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenTaiKhoan == currentUserName);
                if (taiKhoan == null || !PasswordHasher.Verify(viewModel.MatKhauHienTai, taiKhoan.MatKhau))
                {
                    ModelState.AddModelError(nameof(viewModel.MatKhauHienTai), "Mật khẩu hiện tại không đúng.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is NOT valid.");
            foreach (var modelStateKey in ModelState.Keys)
            {
                var modelStateVal = ModelState[modelStateKey];
                foreach (var error in modelStateVal.Errors)
                {
                    _logger.LogWarning($"Key: {modelStateKey}, Error: {error.ErrorMessage}");
                }
            }
            viewModel.TenTaiKhoan = userName;
            await PrepareDropdownLists(viewModel);
            return View(viewModel);
        }

        var taiKhoanToUpdate = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenTaiKhoan == viewModel.TenTaiKhoan);

        if (taiKhoanToUpdate == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản để cập nhật.";
            return RedirectToAction("Index");
        }

        taiKhoanToUpdate.HoVaTen = viewModel.HoVaTen;
        taiKhoanToUpdate.Email = viewModel.Email;
        taiKhoanToUpdate.SoDienThoai = viewModel.SoDienThoai;
        taiKhoanToUpdate.DiaChi = viewModel.DiaChi;
        taiKhoanToUpdate.MaPhuongXa = viewModel.MaPhuongXa;

        if (!string.IsNullOrEmpty(viewModel.MatKhauMoi))
        {
            taiKhoanToUpdate.MatKhau = PasswordHasher.Hash(viewModel.MatKhauMoi);
            _logger.LogWarning($"Mật khẩu tài khoản {taiKhoanToUpdate.TenTaiKhoan} đang lưu dưới dạng hash.");
        }

        try
        {
            _context.Update(taiKhoanToUpdate);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";

            if (User.FindFirstValue("FullName") != taiKhoanToUpdate.HoVaTen)
            {
                await UpdateFullNameClaimAsync(taiKhoanToUpdate.HoVaTen);
            }

            return RedirectToAction("Index");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, $"Lỗi khi cập nhật tài khoản: {userName}");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi không mong muốn khi cập nhật tài khoản: {userName}");
            TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống.";
        }

        await PrepareDropdownLists(viewModel);
        return View(viewModel);
    }

    private async Task UpdateFullNameClaimAsync(string newFullName)
    {
        if (User.Identity is ClaimsIdentity identity)
        {
            var claim = identity.FindFirst("FullName");
            if (claim != null)
                identity.RemoveClaim(claim);

            identity.AddClaim(new Claim("FullName", newFullName ?? ""));

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = User.Claims.Any(c => c.Type == ".persistent")
            };

            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity), authProperties);
        }
    }

    private async Task PrepareDropdownLists(AccountViewModel viewModel)
    {
        viewModel.QuanHuyenList = await GetQuanHuyenListAsync();

        viewModel.PhuongXaList = viewModel.MaQuanHuyen > 0
            ? await GetPhuongXaListAsync(viewModel.MaQuanHuyen)
            : new List<SelectListItem> { new SelectListItem { Value = "", Text = "-- Chọn Phường/Xã --" } };
    }

    private async Task<List<SelectListItem>> GetQuanHuyenListAsync()
    {
        return await _context.QuanHuyens
            .OrderBy(qh => qh.TenQuanHuyen)
            .Select(qh => new SelectListItem
            {
                Value = qh.MaQuanHuyen.ToString(),
                Text = qh.TenQuanHuyen
            })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetPhuongXaListAsync(int maQuanHuyen)
    {
        return await _context.PhuongXas
            .Where(px => px.MaQuanHuyen == maQuanHuyen)
            .OrderBy(px => px.TenPhuongXa)
            .Select(px => new SelectListItem
            {
                Value = px.MaPhuongXa.ToString(),
                Text = px.TenPhuongXa
            })
            .ToListAsync();
    }

    [HttpGet]
    public async Task<JsonResult> GetPhuongXaByQuanHuyen(int maQuanHuyen)
    {
        if (maQuanHuyen <= 0)
        {
            return Json(new List<SelectListItem>()); // Trả về danh sách rỗng nếu không có maQuanHuyen
        }

        var phuongXaList = await _context.PhuongXas
            .Where(px => px.MaQuanHuyen == maQuanHuyen)
            .OrderBy(px => px.TenPhuongXa)
            .Select(px => new SelectListItem
            {
                Value = px.MaPhuongXa.ToString(),
                Text = px.TenPhuongXa
            })
            .ToListAsync();

        return Json(phuongXaList);
    }
}
