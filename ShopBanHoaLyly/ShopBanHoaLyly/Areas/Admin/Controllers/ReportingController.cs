using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShopBanHoaLyly.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Quản trị viên")]
    public class ReportingController : Controller
    {
        private readonly ShopHoaLyLyContext _context;
        public ReportingController(ShopHoaLyLyContext context)
        {
            _context = context;
        }
        public IActionResult Index(string kieuThongKe = "ngay", DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Gán mặc định nếu không có ngày
            if (fromDate == null && toDate == null)
            {
                toDate = DateTime.Today;
                fromDate = toDate.Value.AddDays(-30);
            }
            else if (fromDate == null)
            {
                fromDate = toDate.Value.AddDays(-30);
            }
            else if (toDate == null)
            {
                toDate = DateTime.Today;
            }

            // Xác định kiểu thống kê nếu không có
            if (string.IsNullOrEmpty(kieuThongKe))
            {
                kieuThongKe = "ngay";
            }

            // Kiểm tra ràng buộc fromDate <= toDate theo kiểu thống kê
            bool isValidDateRange = true;
            
            switch (kieuThongKe.ToLower())
            {
                case "ngay":
                    if (fromDate > toDate)
                    {
                        ViewBag.DateError = "Từ ngày phải nhỏ hơn hoặc bằng đến ngày!";
                        isValidDateRange = false;
                    }
                    break;
                    
                case "thang":
                    // Lấy năm và tháng để so sánh
                    var fromYearMonth = new DateTime(fromDate.Value.Year, fromDate.Value.Month, 1);
                    var toYearMonth = new DateTime(toDate.Value.Year, toDate.Value.Month, 1);
                    
                    if (fromYearMonth > toYearMonth)
                    {
                        ViewBag.DateError = "Từ tháng phải nhỏ hơn hoặc bằng đến tháng!";
                        isValidDateRange = false;
                    }
                    break;
                    
                case "nam":
                    if (fromDate.Value.Year > toDate.Value.Year)
                    {
                        ViewBag.DateError = "Từ năm phải nhỏ hơn hoặc bằng đến năm!";
                        isValidDateRange = false;
                    }
                    break;
            }
            
            // Nếu không hợp lệ, đặt lại giá trị mặc định
            if (!isValidDateRange)
            {
                // Giữ lại giá trị người dùng đã nhập để hiển thị lại trên form
                ViewBag.FromDateInput = fromDate.Value.ToString("yyyy-MM-dd");
                ViewBag.ToDateInput = toDate.Value.ToString("yyyy-MM-dd");
                
                // Sử dụng giá trị mặc định an toàn cho tính toán
                DateTime tempToDate = DateTime.Today;
                DateTime tempFromDate = tempToDate.AddDays(-30);
                
                // Lấy dữ liệu thống kê với giá trị mặc định an toàn
                var revenueSummary = GetRevenueSummary(kieuThongKe, tempFromDate, tempToDate);
                var labels = revenueSummary.Select(x => x.Label).ToList();
                var data = revenueSummary.Select(x => x.TongTien).ToList();
                
                // Truyền dữ liệu qua ViewBag
                ViewBag.Labels = labels;
                ViewBag.Data = data;
                ViewBag.KieuThongKe = kieuThongKe;
                
                // Lấy danh sách đơn hàng trong khoảng thời gian mặc định
                var orders = GetOrdersByPeriod(kieuThongKe, tempFromDate, tempToDate);
                ViewBag.Orders = orders;
                
                // Lấy sản phẩm bán chạy theo số lượng (mặc định)
                var bestSellersByQuantity = GetBestSellingProductsData(tempFromDate, tempToDate, "quantity");
                
                // Lấy sản phẩm bán chạy theo doanh thu
                var bestSellersByRevenue = GetBestSellingProductsData(tempFromDate, tempToDate, "revenue");
                ViewBag.BestSellersByRevenue = bestSellersByRevenue;
                
                // Tính tổng doanh thu
                var totalRevenue = orders.Sum(o => o.TongTien);
                ViewBag.TongDoanhThu = totalRevenue;
                ViewBag.TongDonHang = orders.Count;
                ViewBag.TrungBinhDonHang = orders.Count > 0 ? totalRevenue / orders.Count : 0;
                
                return View(bestSellersByQuantity);
            }

            // Nếu hợp lệ, lấy dữ liệu thống kê doanh thu
            var validRevenueSummary = GetRevenueSummary(kieuThongKe, fromDate.Value, toDate.Value);

            // Tạo labels và data cho biểu đồ
            var validLabels = validRevenueSummary.Select(x => x.Label).ToList();
            var validData = validRevenueSummary.Select(x => x.TongTien).ToList();

            // Truyền dữ liệu qua ViewBag
            ViewBag.Labels = validLabels;
            ViewBag.Data = validData;
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd"); // format cho input type="date"
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.KieuThongKe = kieuThongKe;

            // Lấy danh sách đơn hàng trong khoảng thời gian
            var validOrders = GetOrdersByPeriod(kieuThongKe, fromDate.Value, toDate.Value);
            ViewBag.Orders = validOrders;

            // Lấy sản phẩm bán chạy theo số lượng (mặc định)
            var validBestSellersByQuantity = GetBestSellingProductsData(fromDate.Value, toDate.Value, "quantity");
            
            // Lấy sản phẩm bán chạy theo doanh thu
            var validBestSellersByRevenue = GetBestSellingProductsData(fromDate.Value, toDate.Value, "revenue");
            ViewBag.BestSellersByRevenue = validBestSellersByRevenue;
            
            // Tính tổng doanh thu - lưu ý tổng tiền đã được tính trong OrderSummaryViewModel
            var validTotalRevenue = validOrders.Sum(o => o.TongTien);
            ViewBag.TongDoanhThu = validTotalRevenue;
            ViewBag.TongDonHang = validOrders.Count;
            ViewBag.TrungBinhDonHang = validOrders.Count > 0 ? validTotalRevenue / validOrders.Count : 0;
            
            return View(validBestSellersByQuantity);
        }

        public List<RevenueSummaryViewModel> GetRevenueSummary(string kieuThongKe, DateTime fromDate, DateTime toDate)
        {
            // Điều chỉnh toDate để bao gồm cả ngày cuối cùng
            toDate = toDate.Date.AddDays(1).AddTicks(-1); // Đặt thành 23:59:59.9999999 của ngày cuối
            
            // Truy vấn các đơn hàng trong khoảng thời gian
            var orders = _context.DonHangs
                .Where(dh => dh.NgayDatHang >= fromDate && dh.NgayDatHang <= toDate && dh.MaTrangThaiDonHang == 4)
                .Include(dh => dh.ChiTietDonHangs)
                .ToList();

            // Tính tổng tiền
            var result = new List<RevenueSummaryViewModel>();

            switch (kieuThongKe.ToLower())
            {
                case "ngay":
                    // Tạo danh sách tất cả các ngày trong khoảng
                    var allDates = new List<DateTime>();
                    for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
                    {
                        allDates.Add(day);
                    }
                    
                    // Gom nhóm đơn hàng theo ngày
                    var ordersByDate = orders
                        .GroupBy(dh => dh.NgayDatHang.Date)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(dh => (dh.ChiTietDonHangs.Sum(ct => ct.ThanhTien) ?? 0) + (dh.PhiVanChuyen ?? 0))
                        );
                    
                    // Tạo kết quả cho tất cả các ngày, với giá trị 0 cho ngày không có đơn hàng
                    result = allDates.Select(date => new RevenueSummaryViewModel
                    {
                        NgayThongKe = date,
                        Label = date.ToString("dd/MM/yyyy"),
                        TongTien = ordersByDate.ContainsKey(date) ? ordersByDate[date] : 0
                    })
                    .OrderBy(x => x.NgayThongKe)
                    .ToList();
                    break;

                case "thang":
                    // Tạo danh sách tất cả các tháng trong khoảng
                    var allMonths = new List<DateTime>();
                    DateTime currentMonth = new DateTime(fromDate.Year, fromDate.Month, 1);
                    DateTime lastMonth = new DateTime(toDate.Year, toDate.Month, 1);
                    while (currentMonth <= lastMonth)
                    {
                        allMonths.Add(currentMonth);
                        currentMonth = currentMonth.AddMonths(1);
                    }
                    
                    // Gom nhóm đơn hàng theo tháng
                    var ordersByMonth = orders
                        .GroupBy(dh => new { dh.NgayDatHang.Year, dh.NgayDatHang.Month })
                        .ToDictionary(
                            g => new DateTime(g.Key.Year, g.Key.Month, 1),
                            g => g.Sum(dh => (dh.ChiTietDonHangs.Sum(ct => ct.ThanhTien) ?? 0) + (dh.PhiVanChuyen ?? 0))
                        );
                    
                    // Tạo kết quả cho tất cả các tháng, với giá trị 0 cho tháng không có đơn hàng
                    result = allMonths.Select(month => new RevenueSummaryViewModel
                    {
                        NgayThongKe = month,
                        Label = $"{month.Month}/{month.Year}",
                        TongTien = ordersByMonth.ContainsKey(month) ? ordersByMonth[month] : 0
                    })
                    .OrderBy(x => x.NgayThongKe)
                    .ToList();
                    break;

                case "nam":
                    // Tạo danh sách tất cả các năm trong khoảng
                    var allYears = new List<int>();
                    for (int year = fromDate.Year; year <= toDate.Year; year++)
                    {
                        allYears.Add(year);
                    }
                    
                    // Gom nhóm đơn hàng theo năm
                    var ordersByYear = orders
                        .GroupBy(dh => dh.NgayDatHang.Year)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(dh => (dh.ChiTietDonHangs.Sum(ct => ct.ThanhTien) ?? 0) + (dh.PhiVanChuyen ?? 0))
                        );
                        
                    // Tạo kết quả cho tất cả các năm, với giá trị 0 cho năm không có đơn hàng
                    result = allYears.Select(year => new RevenueSummaryViewModel
                    {
                        NgayThongKe = new DateTime(year, 1, 1),
                        Label = year.ToString(),
                        TongTien = ordersByYear.ContainsKey(year) ? ordersByYear[year] : 0
                    })
                    .OrderBy(x => x.NgayThongKe)
                    .ToList();
                    break;
            }

            return result;
        }

        public List<OrderSummaryViewModel> GetOrdersByPeriod(string kieuThongKe, DateTime fromDate, DateTime toDate, string specificPeriod = null)
        {
            // Điều chỉnh toDate để bao gồm cả ngày cuối cùng
            toDate = toDate.Date.AddDays(1).AddTicks(-1); // Đặt thành 23:59:59.9999999 của ngày cuối
            
            // Bắt đầu truy vấn
            IQueryable<DonHang> query = _context.DonHangs
                .Where(dh => dh.NgayDatHang >= fromDate && dh.NgayDatHang <= toDate && dh.MaTrangThaiDonHang == 4);

            // Thêm các Include để lấy dữ liệu liên quan
            query = query.Include(dh => dh.ChiTietDonHangs);
            query = query.Include(dh => dh.MaTrangThaiDonHangNavigation);

            // Nếu có specificPeriod (được chọn từ biểu đồ), lọc thêm
            if (!string.IsNullOrEmpty(specificPeriod))
            {
                // Kiểm tra kiểu thống kê để biết cách lọc
                switch (kieuThongKe.ToLower())
                {
                    case "ngay":
                        // specificPeriod sẽ có định dạng dd/MM/yyyy
                        DateTime selectedDate;
                        if (DateTime.TryParseExact(specificPeriod, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
                        {
                            query = query.Where(dh => dh.NgayDatHang.Date == selectedDate.Date);
                        }
                        break;

                    case "thang":
                        // specificPeriod sẽ có định dạng MM/yyyy
                        var parts = specificPeriod.Split('/');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                        {
                            query = query.Where(dh => dh.NgayDatHang.Month == month && dh.NgayDatHang.Year == year);
                        }
                        break;

                    case "nam":
                        // specificPeriod sẽ là năm
                        if (int.TryParse(specificPeriod, out int selectedYear))
                        {
                            query = query.Where(dh => dh.NgayDatHang.Year == selectedYear);
                        }
                        break;
                }
            }

            // Chuyển đổi kết quả thành OrderSummaryViewModel
            var result = query.Select(dh => new OrderSummaryViewModel
            {
                MaDonHang = dh.MaDonHang,
                NgayDatHang = dh.NgayDatHang,
                TrangThaiDonHang = dh.MaTrangThaiDonHangNavigation.TenTrangThaiDonHang,
                TongTien = (dh.ChiTietDonHangs.Sum(ct => ct.ThanhTien) ?? 0) + (dh.PhiVanChuyen ?? 0),
                SoDienThoaiNhan = dh.SoDienThoaiNhan,
                NguoiNhan = dh.NguoiNhan
            })
            .OrderByDescending(o => o.NgayDatHang)
            .ToList();

            return result;
        }

        [HttpGet]
        public IActionResult GetOrdersBySpecificPeriod(string kieuThongKe, DateTime fromDate, DateTime toDate, string specificPeriod)
        {
            var orders = GetOrdersByPeriod(kieuThongKe, fromDate, toDate, specificPeriod);
            return PartialView("_OrderList", orders);
        }

        // Đổi tên method này để tránh xung đột với action method bên dưới
        private List<BestSellProductViewModel> GetBestSellingProductsData(DateTime fromDate, DateTime toDate, string sortBy = "quantity")
        {
            // Điều chỉnh toDate để bao gồm cả ngày cuối cùng
            toDate = toDate.Date.AddDays(1).AddTicks(-1); // Đặt thành 23:59:59.9999999 của ngày cuối
            
            // Lọc chi tiết đơn hàng trong khoảng thời gian
            var orderDetails = _context.ChiTietDonHangs
                .Where(ct => ct.MaDonHangNavigation.NgayDatHang >= fromDate && 
                             ct.MaDonHangNavigation.NgayDatHang <= toDate &&
                             ct.MaDonHangNavigation.MaTrangThaiDonHang == 4)
                .GroupBy(od => od.MaSanPham)
                .Select(group => new
                {
                    MaSanPham = group.Key,
                    SoLuongDat = group.Sum(g => g.SoLuongDat ?? 0),
                    DoanhThu = group.Sum(g => g.ThanhTien ?? 0)
                })
                .ToList();

            var productIds = orderDetails.Select(x => x.MaSanPham).ToList();

            var products = _context.SanPhams
                .Where(p => productIds.Contains(p.MaSanPham))
                .Select(p => new
                {
                    p.MaSanPham,
                    p.TenSanPham,
                    p.GiaBan,
                    p.MaDanhMuc,
                }).ToList();

            var categories = _context.DanhMucs.ToDictionary(d => d.MaDanhMuc, d => d.TenDanhMuc);

            var bestSellingProducts = orderDetails.Select(od =>
            {
                var product = products.FirstOrDefault(p => p.MaSanPham == od.MaSanPham);
                return new BestSellProductViewModel
                {
                    TenSanPham = product?.TenSanPham,
                    DanhMuc = product != null && product.MaDanhMuc.HasValue && categories.ContainsKey(product.MaDanhMuc.Value)
                        ? categories[product.MaDanhMuc.Value]
                        : "Không rõ",
                    GiaBan = product?.GiaBan ?? 0,
                    SoLuongDat = od.SoLuongDat,
                    ThanhTien = od.DoanhThu
                };
            });
            
            // Sắp xếp theo tiêu chí được chọn
            IEnumerable<BestSellProductViewModel> sortedProducts;
            if (sortBy.ToLower() == "revenue")
            {
                sortedProducts = bestSellingProducts.OrderByDescending(x => x.ThanhTien);
            }
            else // mặc định sắp xếp theo số lượng
            {
                sortedProducts = bestSellingProducts.OrderByDescending(x => x.SoLuongDat);
            }

            return sortedProducts.ToList();
        }
    }
}
