using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Google;
using ShopBanHoaLyly.Services;

namespace ShopBanHoaLyly.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ShopHoaLyLyContext _context;


    public HomeController(ILogger<HomeController> logger, ShopHoaLyLyContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var products = _context.SanPhams
            .Include(s => s.HinhAnhs)
            .Include(s => s.DanhGia)
            .Take(12)
            .ToList();

            var categories = _context.DanhMucs
           .OrderBy(d => d.TenDanhMuc)
           .Take(12)
           .ToList();

        // Lấy danh sách tin tức mới để hiển thị block "New" trên trang chủ
        var latestNews = _context.TinTucs
            .Where(t => t.TrangThaiHienThi == true)
            .OrderByDescending(t => t.NgayCapNhat)
            .Take(3)
            .ToList();

        ViewBag.Categories = categories;
        ViewBag.News = latestNews;

        return View(products);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kiểm tra tên tài khoản đã tồn tại
        if (await _context.TaiKhoans.AnyAsync(u => u.TenTaiKhoan == model.TenTaiKhoan))
        {
            ModelState.AddModelError("TenTaiKhoan", "Tên tài khoản đã được sử dụng.");
            return View(model);
        }

        // Kiểm tra email đã tồn tại
        if (await _context.TaiKhoans.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng.");
            return View(model);
        }

        // Tạo tài khoản mới
        var taiKhoan = new TaiKhoan
        {
            TenTaiKhoan = model.TenTaiKhoan,
            MatKhau = PasswordHasher.Hash(model.MatKhau),
            HoVaTen = model.HoVaTen,
            Email = model.Email,
            MaQuyen = 3, // Khách hàng
            TrangThaiTaiKhoan = true
        };

        _context.TaiKhoans.Add(taiKhoan);
        await _context.SaveChangesAsync();

        // Đăng nhập tự động
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, taiKhoan.TenTaiKhoan),
            new Claim("FullName", taiKhoan.HoVaTen),
            new Claim(ClaimTypes.Role, "User")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
        await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult LoginGoogle()
    {
        var properties = new AuthenticationProperties { 
            RedirectUri = Url.Action("GoogleResponse", "Home")
        };
        
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleResponse()
    {
        // Xác thực với Google đã hoàn tất, lấy thông tin người dùng
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        
        if (!authenticateResult.Succeeded)
        {
            _logger.LogError("Không thể xác thực với Google");
            return RedirectToAction("Login");
        }

        var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
        var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);
        
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("Không thể lấy email từ Google");
            return RedirectToAction("Login");
        }

        // Kiểm tra tài khoản tồn tại chưa, nếu chưa thì tạo
        var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Tạo tên tài khoản từ email (loại bỏ @gmail.com)
            string username = email;
            if (email.Contains("@"))
            {
                username = email.Substring(0, email.IndexOf("@"));
            }

            // Kiểm tra xem username đã tồn tại chưa, nếu có thì thêm một số ngẫu nhiên
            if (await _context.TaiKhoans.AnyAsync(u => u.TenTaiKhoan == username))
            {
                Random rnd = new Random();
                username = username + rnd.Next(100, 999);
            }

            user = new TaiKhoan
            {
                TenTaiKhoan = username,
                HoVaTen = name ?? username,
                MatKhau = "", // Không cần vì dùng Google
                MaQuyen = 3,  // Khách hàng
                TrangThaiTaiKhoan = true,
                Email = email
            };
            _context.TaiKhoans.Add(user);
            await _context.SaveChangesAsync();
        }

        // Đăng nhập vào ứng dụng bằng cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.TenTaiKhoan),
            new Claim("FullName", user.HoVaTen),
            new Claim(ClaimTypes.Role, "User")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
        await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

        return RedirectToAction("Index", "Home");
    }

    //GET: /Home/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Nếu đã đăng nhập rồi thì chuyển về trang chủ
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }


    //POST: /Home/Login
    [HttpPost]
    public async Task<IActionResult> LoginAsync(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _context.TaiKhoans
            .Include(u => u.MaQuyenNavigation)
            .FirstOrDefault(u => u.TenTaiKhoan == model.TaiKhoan);

        if (user == null || !PasswordHasher.Verify(model.MatKhau, user.MatKhau))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
            return View(model);
        }

        // Kiểm tra trạng thái tài khoản
        if (user.TrangThaiTaiKhoan == null || user.TrangThaiTaiKhoan == false)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc không hoạt động.");
            return View(model);
        }

        _logger.LogInformation($"LoginAsync: Value of user.TenTaiKhoan before creating ClaimTypes.Name: '{user.TenTaiKhoan}'");
        _logger.LogInformation($"LoginAsync: Value of user.HoVaTen before creating FullName claim: '{user.HoVaTen}'");


        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.TenTaiKhoan),
        new Claim("FullName", user.HoVaTen),
        new Claim(ClaimTypes.Role, user.MaQuyenNavigation?.TenQuyen)
    };

        var nameClaimValueAfterCreation = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        _logger.LogInformation($"LoginAsync: Value of ClaimTypes.Name in List<Claim> before SignIn: '{nameClaimValueAfterCreation}'");

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe
        };

        await HttpContext.SignInAsync(
            "MyCookieAuth",
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        _logger.LogInformation($"LoginAsync: User {user.TenTaiKhoan} SignInAsync completed.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Điều hướng theo vai trò
        if (user.MaQuyenNavigation?.TenQuyen == "Quản trị viên")
        {
            return RedirectToAction("Index", "Account", new { area = "Admin" });
        }
        else if (user.MaQuyenNavigation?.TenQuyen == "Nhân viên")
        {
            return RedirectToAction("Index", "Category", new { area = "Employee" });
        }
        else
        {
            // Người dùng thông thường
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");

        // Chuyển hướng về trang đăng nhập
        return RedirectToAction("Login", "Home");
    }

    public async Task<IActionResult> Details(int id)
    {
        if (id == 0)
        {
            return NotFound();
        }

        var product = await _context.SanPhams
            .Include(s => s.HinhAnhs)
            .Include(s => s.MaDanhMucNavigation)
            .Include(s => s.DanhGia)
                .ThenInclude(dg => dg.ChiTietDonHang)
                    .ThenInclude(ct => ct.MaDonHangNavigation)
                        .ThenInclude(dh => dh.MaTaiKhoanNavigation)
            .FirstOrDefaultAsync(s => s.MaSanPham == id);

        if (product == null)
        {
            return NotFound();
        }

        var relatedProducts = await _context.SanPhams
            .Include(s => s.HinhAnhs)
            .Include(s => s.DanhGia)
                .ThenInclude(dg => dg.ChiTietDonHang)
                    .ThenInclude(ct => ct.MaDonHangNavigation)
                        .ThenInclude(dh => dh.MaTaiKhoanNavigation)
            .Where(s => s.MaDanhMuc == product.MaDanhMuc && s.MaSanPham != id)
            .OrderByDescending(s => s.DanhGia.Count)
            .Take(6)
            .ToListAsync();

        var averageRating = product.DanhGia.Any()
            ? (double)(product.DanhGia.Average(d => d.SoSao) ?? 0)
            : 0;

        bool userHasPurchased = false;
        bool userHasReviewed = false;

        // Kiểm tra nếu người dùng đã đăng nhập
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userName = User.Identity.Name;
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenTaiKhoan == userName);
            
            if (user != null)
            {
                // Kiểm tra xem người dùng đã mua sản phẩm này chưa
                userHasPurchased = await _context.DonHangs
                    .Where(d => d.MaTaiKhoan == user.MaTaiKhoan)
                    .Where(d => d.MaTrangThaiDonHang == 4) // Đã giao hàng
                    .Join(_context.ChiTietDonHangs.Where(ct => ct.MaSanPham == id),
                          d => d.MaDonHang,
                          ct => ct.MaDonHang,
                          (d, ct) => new { d, ct })
                    .AnyAsync();
                
                // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa
                // Lấy danh sách đơn hàng đã hoàn thành của người dùng chứa sản phẩm này
                var completedOrders = await _context.DonHangs
                    .Where(d => d.MaTaiKhoan == user.MaTaiKhoan && d.MaTrangThaiDonHang == 4)
                    .Join(_context.ChiTietDonHangs.Where(ct => ct.MaSanPham == id),
                          d => d.MaDonHang,
                          ct => ct.MaDonHang,
                          (d, ct) => d.MaDonHang)
                    .ToListAsync();
                
                // Lấy danh sách đơn hàng đã được đánh giá
                var reviewedOrders = await _context.DanhGia
                    .Where(d => d.MaSanPham == id && completedOrders.Contains(d.MaDonHang))
                    .Select(d => d.MaDonHang)
                    .ToListAsync();
                
                // Kiểm tra xem còn đơn hàng nào chưa được đánh giá không
                userHasReviewed = completedOrders.Count > 0 && 
                                 completedOrders.Count == reviewedOrders.Count;
            }
        }

        var viewModel = new ProductDetailViewModel
        {
            Product = product,
            Images = product.HinhAnhs,
            Reviews = product.DanhGia,
            AverageRating = averageRating,
            TotalReviews = product.DanhGia.Count,
            RelatedProducts = relatedProducts,
            UserHasPurchased = userHasPurchased,
            UserHasReviewed = userHasReviewed
        };

        return View(viewModel);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult Search(string? search, int cId = 0, int minPrice = -1, int maxPrice = -1, int sort = 1, int page = 1)
    {
        int pageSize = 12;
        var query = _context.SanPhams
            .Include(s => s.HinhAnhs)
            .Include(s => s.DanhGia)
            .Where(sp => string.IsNullOrEmpty(search) || sp.TenSanPham.Contains(search));

        var queryDanhmuc = query
            .Where(sp => cId == 0 || sp.MaDanhMuc == cId);

        var queryGia = queryDanhmuc
            .Where(sp => (minPrice < 0 && maxPrice < 0) || (sp.GiaBan >= minPrice && sp.GiaBan <= maxPrice));

        switch (sort)
        {
            case 2: queryGia = queryGia.OrderBy(sp => sp.GiaBan); break;
            case 3: queryGia = queryGia.OrderByDescending(sp => sp.GiaBan); break;
            default: queryGia = queryGia.OrderBy(sp => sp.TenSanPham); break;
        }

        int totalItems = queryGia.Count();
        var products = queryGia
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var categories = _context.DanhMucs;

        var model = new ProductCategoryViewModel
        {
            products = products,
            categories = categories.ToList()
        };

        ViewBag.Keyword = search;
        ViewBag.cId = cId;
        ViewBag.MinPrice = minPrice >= 0 ? minPrice : 0;
        ViewBag.MaxPrice = maxPrice >= 0 ? maxPrice : 5000000;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(model);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
