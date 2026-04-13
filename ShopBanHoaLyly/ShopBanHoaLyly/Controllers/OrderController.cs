    using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.AspNetCore.Authorization;
using ShopBanHoaLyly.Services;
using VNPAY.NET.Models;
using Newtonsoft.Json;

namespace ShopBanHoaLyly.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        private readonly VnPayService _vnPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;

        public OrderController(ShopHoaLyLyContext context, VnPayService vnPayService, IHttpContextAccessor httpContextAccessor, EmailService emailService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(List<int> chonMua)
        {
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null) return RedirectToAction("Login", "Home");

            if (chonMua == null || !chonMua.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán";
                return RedirectToAction("Index", "Cart");
            }
            
            Console.WriteLine($"Sản phẩm được chọn: {string.Join(", ", chonMua)}"); // Log để debug

            var cartItems = _context.GioHangs
                .Where(g => g.MaKhachHang == user.MaTaiKhoan && chonMua.Contains(g.MaSanPham))
                .Include(g => g.MaSanPhamNavigation)
                .ThenInclude(sp => sp.HinhAnhs)
                .Select(g => new CartItemViewModel
                {
                    MaSanPham = g.MaSanPham,
                    TenSanPham = g.MaSanPhamNavigation.TenSanPham,
                    GiaBan = g.MaSanPhamNavigation.GiaBan,
                    SoLuong = g.SoLuong ?? 1,
                    HinhAnhDaiDien = g.MaSanPhamNavigation.HinhAnhs.FirstOrDefault().DuongDan,
                    ChonMua = true
                })
                .ToList();

                            // Debug thông tin giỏ hàng
            Console.WriteLine($"Số sản phẩm trong giỏ hàng: {cartItems.Count}");
            foreach (var item in cartItems)
            {
                Console.WriteLine($"Sản phẩm: {item.MaSanPham} - {item.TenSanPham}, Giá: {item.GiaBan}, SL: {item.SoLuong}, Tổng: {item.TongCong}");
            }
            
            // Tạo OrderViewModel
            var orderViewModel = new OrderViewModel
            {
                NguoiNhan = user.HoVaTen,
                DiaChi = user.DiaChi,
                SoDienThoai = user.SoDienThoai,
                CartItems = cartItems,
                QuanHuyenList = _context.QuanHuyens
                    .Select(q => new SelectListItem
                    {
                        Value = q.MaQuanHuyen.ToString(),
                        Text = q.TenQuanHuyen
                    }).ToList()
            };
            
            Console.WriteLine($"TamTinh: {orderViewModel.TamTinh:C0}, PhiVanChuyen: {orderViewModel.PhiVanChuyen:C0}, TongCong: {orderViewModel.TongCong:C0}");
            
            return View(orderViewModel);
        }

        [HttpGet]
        public IActionResult GetPhuongXa(int maQuanHuyen)
        {
            var danhSachPhuongXa = _context.PhuongXas
                .Where(p => p.MaQuanHuyen == maQuanHuyen)
                .Select(p => new SelectListItem
                {
                    Value = p.MaPhuongXa.ToString(),
                    Text = p.TenPhuongXa
                })
                .ToList();
            
            return Json(danhSachPhuongXa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(OrderViewModel model, List<int> sanPhamIds, List<int> soLuongs)
        {
            bool stockError = false;
            if (sanPhamIds != null && sanPhamIds.Any())
            {
                for (int i = 0; i < sanPhamIds.Count; i++)
                {
                    var spId = sanPhamIds[i];
                    var soLuongDat = i < soLuongs.Count ? soLuongs[i] : 1;

                    var sanPham = _context.SanPhams.FirstOrDefault(s => s.MaSanPham == spId);
                    if (sanPham == null || (sanPham.SoLuongCon ?? 0) < soLuongDat)
                    {
                        stockError = true;
                        var tenSp = sanPham?.TenSanPham ?? $"Mã {spId}";
                        ModelState.AddModelError(string.Empty, $"Sản phẩm '{tenSp}' không đủ số lượng tồn kho (còn {(sanPham?.SoLuongCon ?? 0)})");
                    }
                }
            }

            if (stockError)
            {
                TempData["ErrorMessage"] = "Một hoặc nhiều sản phẩm không đủ tồn kho. Vui lòng kiểm tra lại giỏ hàng.";
            }

            if (!ModelState.IsValid)
            {
                // Nạp lại danh sách quận huyện
                model.QuanHuyenList = _context.QuanHuyens
                    .Select(q => new SelectListItem
                    {
                        Value = q.MaQuanHuyen.ToString(),
                        Text = q.TenQuanHuyen
                    }).ToList();

                // Nạp lại danh sách phường xã nếu đã chọn quận huyện
                if (model.MaQuanHuyen > 0)
                {
                    model.PhuongXaList = _context.PhuongXas
                        .Where(p => p.MaQuanHuyen == model.MaQuanHuyen)
                        .Select(p => new SelectListItem
                        {
                            Value = p.MaPhuongXa.ToString(),
                            Text = p.TenPhuongXa
                        }).ToList();
                }

                // Nạp lại danh sách sản phẩm
                if (sanPhamIds != null && sanPhamIds.Any())
                {
                    var cartItems = new List<CartItemViewModel>();
                    for (int i = 0; i < sanPhamIds.Count; i++)
                    {
                        var sanPham = _context.SanPhams
                            .Include(s => s.HinhAnhs)
                            .FirstOrDefault(s => s.MaSanPham == sanPhamIds[i]);
                        
                        if (sanPham != null)
                        {
                            cartItems.Add(new CartItemViewModel
                            {
                                MaSanPham = sanPham.MaSanPham,
                                TenSanPham = sanPham.TenSanPham,
                                GiaBan = sanPham.GiaBan,
                                SoLuong = i < soLuongs.Count ? soLuongs[i] : 1,
                                HinhAnhDaiDien = sanPham.HinhAnhs.FirstOrDefault()?.DuongDan,
                                ChonMua = true
                            });
                        }
                    }
                    model.CartItems = cartItems;
                }

                return View("Index", model);
            }

            // Lấy thông tin người dùng
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null) return RedirectToAction("Login", "Home");

            // Lấy thông tin quận huyện, phường xã
            var quanHuyen = _context.QuanHuyens.FirstOrDefault(q => q.MaQuanHuyen == model.MaQuanHuyen);
            var phuongXa = _context.PhuongXas.FirstOrDefault(p => p.MaPhuongXa == model.MaPhuongXa);

            // Tạo địa chỉ đầy đủ
            string diaChiDayDu = $"{model.DiaChi}, {phuongXa?.TenPhuongXa}, {quanHuyen?.TenQuanHuyen}, {model.TinhThanhPho}";

            // Kiểm tra phương thức thanh toán
            if (model.PhuongThucThanhToan == "VNPay")
            {
                // In danh sách sản phẩm để debug
                Console.WriteLine("Danh sách sản phẩm thanh toán:");
                var itemsDetail = "";
                var tongTienItems = 0M;
                
                if (sanPhamIds != null)
                {
                    for (int i = 0; i < sanPhamIds.Count; i++)
                    {
                        var sanPham = _context.SanPhams.FirstOrDefault(s => s.MaSanPham == sanPhamIds[i]);
                        if (sanPham != null)
                        {
                            var soLuong = i < soLuongs.Count ? soLuongs[i] : 1;
                            var giaBan = sanPham.GiaBan ?? 0;
                            var thanhTien = giaBan * soLuong;
                            tongTienItems += thanhTien;
                            
                            itemsDetail += $"{sanPham.MaSanPham} - {sanPham.TenSanPham}: {giaBan:C0} x {soLuong} = {thanhTien:C0}; ";
                        }
                    }
                }
                
                Console.WriteLine(itemsDetail);
                Console.WriteLine($"Tổng tiền sản phẩm: {tongTienItems:C0}");
                Console.WriteLine($"Phí vận chuyển: {model.PhiVanChuyen:C0}");
                Console.WriteLine($"Tổng thanh toán: {(tongTienItems + model.PhiVanChuyen):C0}");
                
                // Tính phí vận chuyển dựa trên tổng tiền sản phẩm
                decimal phiVanChuyenThucTe = ShippingCalculator.Calculate(tongTienItems);
                Console.WriteLine($"Phí vận chuyển thực tế (áp dụng quy tắc): {phiVanChuyenThucTe:C0}");
                
                // Lưu thông tin đơn hàng vào TempData để xử lý sau khi thanh toán thành công
                var orderInfo = new VNPayOrderInfo
                {
                    MaTaiKhoan = user.MaTaiKhoan,
                    NgayDatHang = DateTime.Now,
                    MaTrangThaiDonHang = 1, // Trạng thái "Hoàn tất đặt hàng"
                    DiaChiGiaoHang = diaChiDayDu,
                    NguoiNhan = model.NguoiNhan,
                    SoDienThoaiNhan = model.SoDienThoai,
                    GhiChu = model.GhiChu,
                    TongTien = tongTienItems + phiVanChuyenThucTe, // Sử dụng tổng tiền được tính chính xác và phí vận chuyển thực tế
                    PhiVanChuyen = phiVanChuyenThucTe,
                    SanPhamIds = sanPhamIds,
                    SoLuongs = soLuongs
                };

                // Lưu vào session để sử dụng khi thanh toán thành công
                HttpContext.Session.SetString("PendingOrder", JsonConvert.SerializeObject(orderInfo));

                // Tạo URL thanh toán VNPay và chuyển đến trang thanh toán
                return RedirectToAction("PaymentVNPay");
            }
            else
            {
                // Nếu là COD, tạo đơn hàng ngay
                var donHang = new DonHang
                {
                    MaTaiKhoan = user.MaTaiKhoan,
                    NgayDatHang = DateTime.Now,
                    MaTrangThaiDonHang = 1, // Trạng thái "Hoàn tất đặt hàng"
                    MaPhuongThucThanhToan = 1, // 1: COD
                    TrangThaiThanhToan = false, // COD chưa thanh toán
                    DiaChiGiaoHang = diaChiDayDu,
                    NguoiNhan = model.NguoiNhan,
                    SoDienThoaiNhan = model.SoDienThoai,
                    GhiChu = model.GhiChu,
                    PhiVanChuyen = model.PhiVanChuyen
                    // Không lưu TongTien nữa, sẽ tính toán từ chi tiết đơn hàng và phí vận chuyển
                };

                _context.DonHangs.Add(donHang);
                _context.SaveChanges();

                // Thêm chi tiết đơn hàng
                AddOrderDetails(donHang.MaDonHang, user.MaTaiKhoan, sanPhamIds, soLuongs);

                // Gửi email xác nhận đơn hàng
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        var tongTien = _context.ChiTietDonHangs
                            .Where(c => c.MaDonHang == donHang.MaDonHang)
                            .Sum(c => c.ThanhTien ?? 0) + (donHang.PhiVanChuyen ?? 0);
                            
                        await _emailService.SendOrderConfirmationAsync(
                            user.Email,
                            donHang.NguoiNhan ?? user.HoVaTen,
                            donHang.MaDonHang,
                            tongTien,
                            donHang.NgayDatHang.ToString("dd/MM/yyyy HH:mm")
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không hiển thị cho người dùng
                        Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                    }
                }

                // Chuyển đến trang cảm ơn
                return RedirectToAction("ThankYou", new { orderId = donHang.MaDonHang });
            }
        }

        // Phương thức để thêm chi tiết đơn hàng và xóa giỏ hàng
        private void AddOrderDetails(int orderId, int userId, List<int> sanPhamIds, List<int> soLuongs)
        {
            if (sanPhamIds != null && sanPhamIds.Any())
            {
                decimal tongTienHang = 0;
                
                for (int i = 0; i < sanPhamIds.Count; i++)
                {
                    var sanPhamId = sanPhamIds[i];
                    var soLuong = i < soLuongs.Count ? soLuongs[i] : 1;

                    var sanPham = _context.SanPhams.IgnoreQueryFilters().FirstOrDefault(s => s.MaSanPham == sanPhamId);
                    if (sanPham != null)
                    {
                        decimal donGia = sanPham.GiaBan ?? 0;
                        var thanhTien = donGia * soLuong;
                        tongTienHang += thanhTien;
                        
                        var chiTietDonHang = new ChiTietDonHang
                        {
                            MaDonHang = orderId,
                            MaSanPham = sanPham.MaSanPham,
                            TenSanPham = sanPham.TenSanPham,
                            DonGia = sanPham.GiaBan,
                            SoLuongDat = soLuong,
                            ThanhTien = thanhTien
                        };

                        _context.ChiTietDonHangs.Add(chiTietDonHang);

                        // Cập nhật số lượng sản phẩm
                        sanPham.SoLuongCon -= soLuong;
                        _context.Update(sanPham);

                        // Xóa sản phẩm khỏi giỏ hàng
                        var cartItem = _context.GioHangs.FirstOrDefault(g => g.MaKhachHang == userId && g.MaSanPham == sanPhamId);
                        if (cartItem != null)
                        {
                            _context.GioHangs.Remove(cartItem);
                        }
                    }
                }
                
                // Tính phí vận chuyển thống nhất bằng ShippingCalculator
                var donHang = _context.DonHangs.Find(orderId);
                if (donHang != null)
                {
                    donHang.PhiVanChuyen = ShippingCalculator.Calculate(tongTienHang);
                    _context.Update(donHang);
                }

                _context.SaveChanges();
            }
        }

        public async Task<IActionResult> ThankYou(int orderId)
        {
            var donHang = _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.MaTaiKhoanNavigation)
                .FirstOrDefault(d => d.MaDonHang == orderId);

            if (donHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(donHang);
        }
        
        // Action mới để xem chi tiết đơn hàng
        public IActionResult Details(int id)
        {
            // Lấy thông tin người dùng hiện tại
            var userName = User.Identity?.Name;
            var currentUser = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var donHang = _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.MaSanPhamNavigation)
                        .ThenInclude(s => s.HinhAnhs)
                .Include(d => d.MaTrangThaiDonHangNavigation)
                .Include(d => d.MaPhuongThucThanhToanNavigation)
                .IgnoreQueryFilters()
                .FirstOrDefault(d => d.MaDonHang == id);

            if (donHang == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không tìm thấy thông tin đơn hàng";
                return RedirectToAction("Index", "ClientAccount");
            }

            // Kiểm tra xem người dùng hiện tại có phải là chủ đơn hàng không
            if (donHang.MaTaiKhoan != currentUser.MaTaiKhoan)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Bạn không có quyền xem đơn hàng này";
                return RedirectToAction("Index", "Home");
            }

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int orderId, string cancelReason, string otherReason)
        {
            // Kiểm tra người dùng đăng nhập
            var userName = User.Identity?.Name;
            var currentUser = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Home");
            }
            
            // Tìm đơn hàng
            var donHang = _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefault(d => d.MaDonHang == orderId);
                
            if (donHang == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không tìm thấy thông tin đơn hàng";
                return RedirectToAction("Index", "ClientAccount");
            }
            
            // Kiểm tra quyền
            if (donHang.MaTaiKhoan != currentUser.MaTaiKhoan)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Bạn không có quyền hủy đơn hàng này";
                return RedirectToAction("Index", "Home");
            }
            
            // Kiểm tra trạng thái đơn hàng
            if (donHang.MaTrangThaiDonHang != 1)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Chỉ có thể hủy đơn hàng ở trạng thái Hoàn tất đặt hàng";
                return RedirectToAction("Details", new { id = orderId });
            }
            
            // Cập nhật trạng thái đơn hàng
            donHang.MaTrangThaiDonHang = 5; // Đã hủy
            
            // Lưu lý do hủy
            string lyDoHuy = cancelReason;
            if (cancelReason == "Lý do khác" && !string.IsNullOrEmpty(otherReason))
            {
                lyDoHuy = otherReason;
            }
            
            donHang.GhiChu = string.IsNullOrEmpty(donHang.GhiChu) 
                ? $"[Đơn hàng đã hủy] Lý do: {lyDoHuy}" 
                : $"{donHang.GhiChu}\n[Đơn hàng đã hủy] Lý do: {lyDoHuy}";
            
            // Hoàn trả số lượng sản phẩm
            foreach (var chiTiet in donHang.ChiTietDonHangs)
            {
                var sanPham = _context.SanPhams.IgnoreQueryFilters().FirstOrDefault(s => s.MaSanPham == chiTiet.MaSanPham);
                if (sanPham != null)
                {
                    sanPham.SoLuongCon += chiTiet.SoLuongDat ?? 0;
                    _context.Update(sanPham);
                }
            }
            
            _context.Update(donHang);
            _context.SaveChanges();
            
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đơn hàng đã được hủy thành công";
            
            return RedirectToAction("Details", new { id = orderId });
        }

        // Đặt lại đơn hàng
        [HttpGet]
        public IActionResult Reorder(int id)
        {
            // Lấy thông tin người dùng
            var userName = User.Identity?.Name;
            var user = _context.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == userName);
            if (user == null) return RedirectToAction("Login", "Home");
            
            // Tìm đơn hàng cần đặt lại
            var donHangCu = _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(c => c.MaSanPhamNavigation)
                .FirstOrDefault(d => d.MaDonHang == id && d.MaTaiKhoan == user.MaTaiKhoan);
                
            if (donHangCu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng";
                return RedirectToAction("Index", "ClientAccount");
            }
            
            // Tạo danh sách sản phẩm để thêm vào giỏ hàng
            List<int> sanPhamIds = new List<int>();
            List<int> soLuongs = new List<int>();
            
            foreach (var chiTiet in donHangCu.ChiTietDonHangs)
            {
                // Kiểm tra sản phẩm còn tồn tại và có đủ số lượng không
                var sanPham = _context.SanPhams.FirstOrDefault(s => s.MaSanPham == chiTiet.MaSanPham);
                if (sanPham != null && (sanPham.SoLuongCon ?? 0) >= (chiTiet.SoLuongDat ?? 0))
                {
                    // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                    var gioHang = _context.GioHangs.FirstOrDefault(g => g.MaKhachHang == user.MaTaiKhoan && g.MaSanPham == chiTiet.MaSanPham);
                    
                    if (gioHang != null)
                    {
                        // Cập nhật số lượng
                        gioHang.SoLuong = (gioHang.SoLuong ?? 0) + (chiTiet.SoLuongDat ?? 1);
                        _context.Update(gioHang);
                    }
                    else
                    {
                        // Thêm mới vào giỏ hàng
                        _context.GioHangs.Add(new GioHang
                        {
                            MaKhachHang = user.MaTaiKhoan,
                            MaSanPham = chiTiet.MaSanPham,
                            SoLuong = chiTiet.SoLuongDat
                        });
                    }
                    
                    sanPhamIds.Add(chiTiet.MaSanPham);
                    soLuongs.Add(chiTiet.SoLuongDat ?? 1);
                }
            }
            
            _context.SaveChanges();
            
            if (sanPhamIds.Count > 0)
            {
                TempData["SuccessMessage"] = $"Đã thêm {sanPhamIds.Count} sản phẩm vào giỏ hàng";
                return RedirectToAction("Index", "Cart");
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể thêm sản phẩm vào giỏ hàng do hết hàng";
                return RedirectToAction("Details", new { id = id });
            }
        }
        
        // Phương thức thanh toán VNPay
        [HttpGet]
        public IActionResult PaymentVNPay()
        {
            try
            {
                // Lấy thông tin đơn hàng từ session
                var orderInfoJson = HttpContext.Session.GetString("PendingOrder");
                if (string.IsNullOrEmpty(orderInfoJson))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                var orderInfo = JsonConvert.DeserializeObject<VNPayOrderInfo>(orderInfoJson);
                if (orderInfo == null)
                {
                    TempData["ErrorMessage"] = "Không thể đọc thông tin đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                // Kiểm tra nếu số tiền đơn hàng là 0 hoặc null
                if (orderInfo.TongTien <= 0)
                {
                    TempData["ErrorMessage"] = "Số tiền đơn hàng không hợp lệ";
                    return RedirectToAction("Index", "Cart");
                }
                
                // Log để debug
                Console.WriteLine($"Tổng tiền thanh toán VNPay: {orderInfo.TongTien:C0}");

                // Tạo một temporary order ID cho thanh toán
                int tempOrderId = (int)(DateTime.Now.Ticks % 1000000000);
                
                // Tạo mô tả đơn hàng
                string orderDescription = $"Thanh toan don hang - Shop Hoa Lyly";
                
                // Tạo URL thanh toán VNPay sử dụng tổng tiền đã lưu trong orderInfo
                var paymentUrl = _vnPayService.CreatePaymentUrl(tempOrderId, orderInfo.TongTien, orderDescription, _httpContextAccessor);
                
                // Chuyển hướng đến trang thanh toán VNPay
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (nếu cần)
                TempData["ErrorMessage"] = $"Lỗi khi tạo yêu cầu thanh toán: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        // Xử lý callback từ VNPay sau khi thanh toán
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var result = _vnPayService.GetPaymentResult(Request.Query);
            
            // Kiểm tra kết quả thanh toán
            if (result != null && result.IsSuccess)
            {
                // Lấy thông tin đơn hàng từ session
                var orderInfoJson = HttpContext.Session.GetString("PendingOrder");
                if (string.IsNullOrEmpty(orderInfoJson))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                var orderInfo = JsonConvert.DeserializeObject<VNPayOrderInfo>(orderInfoJson);
                if (orderInfo == null)
                {
                    TempData["ErrorMessage"] = "Không thể đọc thông tin đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                // Tạo đơn hàng mới trong cơ sở dữ liệu
                var donHang = new DonHang
                {
                    MaTaiKhoan = orderInfo.MaTaiKhoan,
                    NgayDatHang = DateTime.Now,
                    MaTrangThaiDonHang = 1, // Trạng thái "Hoàn tất đặt hàng"
                    MaPhuongThucThanhToan = 2, // 2: VNPay
                    TrangThaiThanhToan = true, // Đã thanh toán
                    DiaChiGiaoHang = orderInfo.DiaChiGiaoHang,
                    NguoiNhan = orderInfo.NguoiNhan,
                    SoDienThoaiNhan = orderInfo.SoDienThoaiNhan,
                    GhiChu = orderInfo.GhiChu,
                    PhiVanChuyen = orderInfo.PhiVanChuyen
                    // Không lưu TongTien nữa, sẽ tính toán từ chi tiết đơn hàng và phí vận chuyển
                };

                _context.DonHangs.Add(donHang);
                _context.SaveChanges();

                // Thêm chi tiết đơn hàng
                AddOrderDetails(donHang.MaDonHang, orderInfo.MaTaiKhoan, orderInfo.SanPhamIds, orderInfo.SoLuongs);

                // Xóa thông tin đơn hàng khỏi session
                HttpContext.Session.Remove("PendingOrder");
                
                // Gửi email xác nhận đơn hàng
                var userEmail = _context.TaiKhoans.FirstOrDefault(t => t.MaTaiKhoan == donHang.MaTaiKhoan)?.Email;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    try
                    {
                        var tongTien = _context.ChiTietDonHangs
                            .Where(c => c.MaDonHang == donHang.MaDonHang)
                            .Sum(c => c.ThanhTien ?? 0) + (donHang.PhiVanChuyen ?? 0);
                            
                        await _emailService.SendOrderConfirmationAsync(
                            userEmail,
                            donHang.NguoiNhan ?? "",
                            donHang.MaDonHang,
                            tongTien,
                            donHang.NgayDatHang.ToString("dd/MM/yyyy HH:mm")
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không hiển thị cho người dùng
                        Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                    }
                }
                
                // Chuyển đến trang cảm ơn
                return RedirectToAction("ThankYou", new { orderId = donHang.MaDonHang });
            }
            else
            {
                // Lấy thông tin lỗi
                string errorMessage = "Thanh toán thất bại";
                
                if (result != null && result.PaymentResponse != null)
                {
                    errorMessage = $"Thanh toán thất bại: {result.PaymentResponse.Description}";
                }
                
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Cart");
            }
        }

        // Xử lý IPN từ VNPay (webhook)
        [HttpGet]
        public IActionResult PaymentIpn()
        {
            var result = _vnPayService.GetPaymentResult(Request.Query);
            
            if (result != null && result.IsSuccess)
            {
                // Lưu ý: IPN có thể được gọi nhiều lần, cần kiểm tra xem đơn hàng đã được xử lý chưa
                // Trong trường hợp này, chúng ta không làm gì vì đơn hàng chỉ được tạo sau khi PaymentCallback thành công
                return Ok();
            }
            
            return BadRequest();
        }

        // Trang hiển thị khi thanh toán thất bại
        [HttpGet]
        public IActionResult PaymentFailed()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int maDonHang, int maSanPham, int soSao, string noiDung)
        {
            try
            {
                if (string.IsNullOrEmpty(noiDung) || soSao < 1 || soSao > 5)
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin đánh giá và chọn số sao (1-5)" });
                }

                // Kiểm tra xem đơn hàng có thuộc về người dùng hiện tại không
                var userName = User.Identity?.Name;
                var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenTaiKhoan == userName);
                if (user == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá sản phẩm" });
                }

                var donHang = await _context.DonHangs
                    .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaTaiKhoan == user.MaTaiKhoan);
                
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc bạn không có quyền đánh giá đơn hàng này" });
                }

                // Kiểm tra xem đơn hàng đã được giao hàng chưa
                if (donHang.MaTrangThaiDonHang != 4)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sản phẩm sau khi đã nhận được hàng" });
                }

                // Kiểm tra xem sản phẩm có trong đơn hàng không
                var chiTietDonHang = await _context.ChiTietDonHangs
                    .FirstOrDefaultAsync(c => c.MaDonHang == maDonHang && c.MaSanPham == maSanPham);
                
                if (chiTietDonHang == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong đơn hàng này" });
                }

                // Kiểm tra xem sản phẩm đã được đánh giá chưa
                var daDanhGia = await _context.DanhGia
                    .AnyAsync(d => d.MaDonHang == maDonHang && d.MaSanPham == maSanPham);
                
                if (daDanhGia)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi" });
                }

                // Thêm đánh giá mới
                var danhGia = new DanhGia
                {
                    MaSanPham = maSanPham,
                    MaDonHang = maDonHang,
                    NoiDung = noiDung,
                    SoSao = soSao,
                    NgayDanhGia = DateTime.Now
                };

                _context.DanhGia.Add(danhGia);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đánh giá của bạn đã được gửi thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }

    // Class để lưu thông tin đơn hàng tạm thời trong session
    public class VNPayOrderInfo
    {
        public int MaTaiKhoan { get; set; }
        public DateTime NgayDatHang { get; set; }
        public int MaTrangThaiDonHang { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public string NguoiNhan { get; set; }
        public string SoDienThoaiNhan { get; set; }
        public string GhiChu { get; set; }
        public decimal TongTien { get; set; } // Giữ lại để lưu tổng tiền thanh toán VNPay
        public decimal PhiVanChuyen { get; set; }
        public List<int> SanPhamIds { get; set; }
        public List<int> SoLuongs { get; set; }
    }
}
