using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using ShopBanHoaLyly.Services;

namespace ShopBanHoaLyly.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Quản trị viên")]
    public class OrderController : Controller
    {
        private readonly ShopHoaLyLyContext _context;

        public OrderController(ShopHoaLyLyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.DonHangs
                .Include(d => d.MaTaiKhoanNavigation)
                .Include(d => d.MaTrangThaiDonHangNavigation)
                .Include(d => d.MaPhuongThucThanhToanNavigation)
                .OrderByDescending(d => d.NgayDatHang)
                .ToListAsync();

            var trangThaiDonHangs = await _context.TrangThaiDonHangs.ToListAsync();
            ViewBag.TrangThaiDonHangs = trangThaiDonHangs;
            
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.MaTaiKhoanNavigation)
                .Include(d => d.MaTrangThaiDonHangNavigation)
                .Include(d => d.MaPhuongThucThanhToanNavigation)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.MaSanPhamNavigation)
                        .ThenInclude(s => s.HinhAnhs)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null)
            {
                return NotFound();
            }

            ViewBag.TrangThaiDonHangs = await _context.TrangThaiDonHangs.ToListAsync();
            return View(donHang);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách sản phẩm
            ViewBag.Products = await _context.SanPhams
                .Include(p => p.MaDanhMucNavigation)
                .Include(p => p.HinhAnhs)
                .Where(p => p.SoLuongCon > 0)
                .ToListAsync();

            // Lấy danh sách phương thức thanh toán
            ViewBag.PaymentMethods = await _context.PhuongThucThanhToans.ToListAsync();

            // Lấy danh sách trạng thái đơn hàng
            ViewBag.OrderStatuses = await _context.TrangThaiDonHangs.ToListAsync();
            
            // Lấy danh sách quận/huyện
            ViewBag.QuanHuyens = await _context.QuanHuyens
                .AsNoTracking()
                .ToListAsync();
            
            // Lấy danh sách phường/xã với thông tin quận huyện
            ViewBag.PhuongXas = await _context.PhuongXas
                .AsNoTracking()
                .Select(p => new { 
                    p.MaPhuongXa, 
                    p.TenPhuongXa, 
                    p.MaQuanHuyen 
                })
                .ToListAsync();

            // Tạo đơn hàng mới mặc định
            var donHang = new DonHang
            {
                NgayDatHang = DateTime.Now,
                MaTrangThaiDonHang = 1, // Mặc định là "Đang chờ xác nhận"
                TrangThaiThanhToan = false,
                PhiVanChuyen = 30000 // Phí vận chuyển mặc định 30,000 đồng
            };

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonHang donHang, List<int> productIds, List<int> quantities, List<decimal> prices, int QuanHuyen, int PhuongXa, bool IsPickup)
        {
            if (string.IsNullOrEmpty(donHang.DiaChiGiaoHang))
            {
                ModelState.AddModelError("DiaChiGiaoHang", "Vui lòng nhập địa chỉ giao hàng");
            }

            if (QuanHuyen <= 0)
            {
                ModelState.AddModelError("QuanHuyen", "Vui lòng chọn Quận/Huyện");
            }

            if (PhuongXa <= 0)
            {
                ModelState.AddModelError("PhuongXa", "Vui lòng chọn Phường/Xã");
            }

            if (string.IsNullOrEmpty(donHang.NguoiNhan))
            {
                ModelState.AddModelError("NguoiNhan", "Vui lòng nhập tên người nhận");
            }

            if (string.IsNullOrEmpty(donHang.SoDienThoaiNhan))
            {
                ModelState.AddModelError("SoDienThoaiNhan", "Vui lòng nhập số điện thoại người nhận");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(donHang.SoDienThoaiNhan, @"^(0|\+84)\d{9,10}$"))
            {
                ModelState.AddModelError("SoDienThoaiNhan", "Số điện thoại không hợp lệ");
            }

            // Kiểm tra xem có sản phẩm nào được chọn không
            if (productIds == null || productIds.Count == 0 || quantities == null || quantities.Count == 0)
            {
                // Không thêm lỗi vào ModelState để tránh hiển thị ở đầu form
                TempData["ProductError"] = "Vui lòng chọn ít nhất một sản phẩm";
                
                // Lấy lại dữ liệu cho view
                ViewBag.Products = await _context.SanPhams
                    .Include(p => p.MaDanhMucNavigation)
                    .Include(p => p.HinhAnhs)
                    .Where(p => p.SoLuongCon > 0)
                    .ToListAsync();
                ViewBag.PaymentMethods = await _context.PhuongThucThanhToans.ToListAsync();
                ViewBag.OrderStatuses = await _context.TrangThaiDonHangs.ToListAsync();
                ViewBag.QuanHuyens = await _context.QuanHuyens
                    .AsNoTracking()
                    .ToListAsync();
                ViewBag.PhuongXas = await _context.PhuongXas
                    .AsNoTracking()
                    .Select(p => new { 
                        p.MaPhuongXa, 
                        p.TenPhuongXa, 
                        p.MaQuanHuyen 
                    })
                    .ToListAsync();
                
                return View(donHang);
            }

            // Kiểm tra số lượng sản phẩm
            bool hasInvalidQuantity = false;
            for (int i = 0; i < productIds.Count; i++)
            {
                if (quantities[i] <= 0)
                {
                    hasInvalidQuantity = true;
                    break;
                }

                // Kiểm tra số lượng tồn kho
                var product = await _context.SanPhams.FindAsync(productIds[i]);
                if (product == null || (product.SoLuongCon ?? 0) < quantities[i])
                {
                    TempData["ProductError"] = $"Sản phẩm '{product?.TenSanPham}' không đủ số lượng trong kho";
                    
                    // Lấy lại dữ liệu cho view
                    ViewBag.Products = await _context.SanPhams
                        .Include(p => p.MaDanhMucNavigation)
                        .Include(p => p.HinhAnhs)
                        .Where(p => p.SoLuongCon > 0)
                        .ToListAsync();
                    ViewBag.PaymentMethods = await _context.PhuongThucThanhToans.ToListAsync();
                    ViewBag.OrderStatuses = await _context.TrangThaiDonHangs.ToListAsync();
                    ViewBag.QuanHuyens = await _context.QuanHuyens
                        .AsNoTracking()
                        .ToListAsync();
                    ViewBag.PhuongXas = await _context.PhuongXas
                        .AsNoTracking()
                        .Select(p => new { 
                            p.MaPhuongXa, 
                            p.TenPhuongXa, 
                            p.MaQuanHuyen 
                        })
                        .ToListAsync();
                    
                    return View(donHang);
                }
            }

            if (hasInvalidQuantity)
            {
                TempData["ProductError"] = "Số lượng sản phẩm phải lớn hơn 0";
                
                // Lấy lại dữ liệu cho view
                ViewBag.Products = await _context.SanPhams
                    .Include(p => p.MaDanhMucNavigation)
                    .Include(p => p.HinhAnhs)
                    .Where(p => p.SoLuongCon > 0)
                    .ToListAsync();
                ViewBag.PaymentMethods = await _context.PhuongThucThanhToans.ToListAsync();
                ViewBag.OrderStatuses = await _context.TrangThaiDonHangs.ToListAsync();
                ViewBag.QuanHuyens = await _context.QuanHuyens
                    .AsNoTracking()
                    .ToListAsync();
                ViewBag.PhuongXas = await _context.PhuongXas
                    .AsNoTracking()
                    .Select(p => new { 
                        p.MaPhuongXa, 
                        p.TenPhuongXa, 
                        p.MaQuanHuyen 
                    })
                    .ToListAsync();
                
                return View(donHang);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cài đặt ngày đặt hàng
                    donHang.NgayDatHang = DateTime.Now;
                    
                    // Nếu nhận tại cửa hàng thì set trạng thái đã giao và ngày giao hàng
                    if (IsPickup)
                    {
                        donHang.MaTrangThaiDonHang = 4; // 4 = Đã giao
                        donHang.NgayGiaoHang = DateTime.Now;
                    }
                    
                    // Lấy tên quận/huyện và phường/xã
                    var quanHuyenInfo = await _context.QuanHuyens.FindAsync(QuanHuyen);
                    var phuongXaInfo = await _context.PhuongXas.FindAsync(PhuongXa);
                    
                    // Cập nhật địa chỉ đầy đủ
                    donHang.DiaChiGiaoHang = $"{donHang.DiaChiGiaoHang}, {phuongXaInfo?.TenPhuongXa}, {quanHuyenInfo?.TenQuanHuyen}";
                    
                    // Tính tổng tiền đơn hàng
                    decimal subtotal = 0;
                    for (int i = 0; i < productIds.Count; i++)
                    {
                        subtotal += prices[i] * quantities[i];
                    }
                    
                    // Cập nhật phí vận chuyển theo lựa chọn
                    donHang.PhiVanChuyen = IsPickup ? 0 : ShippingCalculator.Calculate(subtotal);
                    
                    // Lưu đơn hàng
                    _context.DonHangs.Add(donHang);
                    await _context.SaveChangesAsync();
                    
                    // Tạo chi tiết đơn hàng
                    for (int i = 0; i < productIds.Count; i++)
                    {
                        var product = await _context.SanPhams.FindAsync(productIds[i]);
                        if (product != null)
                        {
                            // Tạo chi tiết đơn hàng
                            var chiTietDonHang = new ChiTietDonHang
                            {
                                MaDonHang = donHang.MaDonHang,
                                MaSanPham = productIds[i],
                                TenSanPham = product.TenSanPham,
                                DonGia = prices[i],
                                SoLuongDat = quantities[i],
                                ThanhTien = prices[i] * quantities[i]
                            };
                            
                            _context.ChiTietDonHangs.Add(chiTietDonHang);
                            
                            // Cập nhật số lượng trong kho
                            product.SoLuongCon -= quantities[i];
                            _context.SanPhams.Update(product);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tạo đơn hàng thành công!";
                    
                    return RedirectToAction(nameof(Details), new { id = donHang.MaDonHang });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi tạo đơn hàng: {ex.Message}");
                }
            }
            
            // Nếu có lỗi, lấy lại dữ liệu cho view
            ViewBag.Products = await _context.SanPhams
                .Include(p => p.MaDanhMucNavigation)
                .Include(p => p.HinhAnhs)
                .Where(p => p.SoLuongCon > 0)
                .ToListAsync();
            ViewBag.PaymentMethods = await _context.PhuongThucThanhToans.ToListAsync();
            ViewBag.OrderStatuses = await _context.TrangThaiDonHangs.ToListAsync();
            ViewBag.QuanHuyens = await _context.QuanHuyens
                .AsNoTracking()
                .ToListAsync();
            ViewBag.PhuongXas = await _context.PhuongXas
                .AsNoTracking()
                .Select(p => new { 
                    p.MaPhuongXa, 
                    p.TenPhuongXa, 
                    p.MaQuanHuyen 
                })
                .ToListAsync();
            
            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int maDonHang, int maTrangThaiDonHang, DateTime? thoiGianGiaoHang, bool trangThaiThanhToan, bool daXacNhanThanhToan = false)
        {
            var donHang = await _context.DonHangs.FindAsync(maDonHang);
            if (donHang == null)
            {
                return NotFound();
            }

            int trangThaiHienTai = donHang.MaTrangThaiDonHang ?? 1;

            // Không cho thay đổi nếu đơn đã hủy
            if (trangThaiHienTai == 5)
            {
                TempData["ErrorMessage"] = "Đơn hàng đã hủy, không thể thay đổi trạng thái.";
                return RedirectToAction(nameof(Details), new { id = maDonHang });
            }

            // Chỉ cho phép tiến tới trạng thái mới (lớn hơn) hoặc hủy (5)
            if (maTrangThaiDonHang != 5 && maTrangThaiDonHang <= trangThaiHienTai)
            {
                TempData["ErrorMessage"] = "Không thể chuyển về trạng thái trước đó.";
                return RedirectToAction(nameof(Details), new { id = maDonHang });
            }
            
            // Kiểm tra đặc biệt khi chuyển từ "Đang giao" (3) sang "Đã giao" (4)
            if (trangThaiHienTai == 3 && maTrangThaiDonHang == 4)
            {
                // Nếu đơn hàng chưa thanh toán và người dùng chưa xác nhận
                if (donHang.TrangThaiThanhToan == false && !daXacNhanThanhToan)
                {
                    // Chuyển về trang chi tiết với thông báo cần xác nhận
                    TempData["WarningMessage"] = "Đơn hàng chưa thanh toán. Để chuyển sang trạng thái \"Đã giao\", đơn hàng phải được đánh dấu là \"Đã thanh toán\".";
                    TempData["PendingStatus"] = maTrangThaiDonHang;
                    TempData["PendingDeliveryTime"] = thoiGianGiaoHang?.ToString("yyyy-MM-ddTHH:mm") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                    return RedirectToAction(nameof(Details), new { id = maDonHang });
                }
                
                // Khi chuyển sang trạng thái "Đã giao", luôn đánh dấu là đã thanh toán
                trangThaiThanhToan = true;
            }

            donHang.MaTrangThaiDonHang = maTrangThaiDonHang;

            // Cập nhật trạng thái thanh toán
            donHang.TrangThaiThanhToan = trangThaiThanhToan;

            // Nếu trạng thái là "Đã giao" (mã 4)
            if (maTrangThaiDonHang == 4)
            {
                // Cập nhật ngày giao hàng nếu chưa có
                if (!donHang.NgayGiaoHang.HasValue)
            {
                donHang.NgayGiaoHang = thoiGianGiaoHang ?? DateTime.Now;
                }
                
                // Đảm bảo đơn hàng được đánh dấu là đã thanh toán
                donHang.TrangThaiThanhToan = true;
            }
            
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật đơn hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật đơn hàng: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = maDonHang });
        }
    }
}
