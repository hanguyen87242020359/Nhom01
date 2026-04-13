using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace ShopBanHoaLyly.Services
{
    public class VnpayPayment
    {
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;

        public VnpayPayment(IVnpay vnpay, IConfiguration configuration)
        {
            _vnpay = vnpay;
            _configuration = configuration;

            _vnpay.Initialize(
                _configuration["Vnpay:TmnCode"],
                _configuration["Vnpay:HashSecret"],
                _configuration["Vnpay:BaseUrl"],
                _configuration["Vnpay:ReturnUrl"]
            );
        }

        public string CreatePaymentUrl(double amount, string description, string ipAddress)
        {
            var request = new PaymentRequest
            {
                PaymentId = DateTime.Now.Ticks, // Mã đơn hàng
                Money = amount,
                Description = description,
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            return _vnpay.GetPaymentUrl(request);
        }

    }
}
