using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace ShopBanHoaLyly.Services
{
    public class VnPayService
    {
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;

        // Giới hạn số tiền thanh toán của VNPAY
        private const double MIN_PAYMENT_AMOUNT = 5000;
        private const double MAX_PAYMENT_AMOUNT = 1000000000;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _vnpay = new Vnpay();
            _vnpay.Initialize(
                _configuration["VnPay:TmnCode"],
                _configuration["VnPay:HashSecret"],
                _configuration["VnPay:BaseUrl"],
                _configuration["VnPay:ReturnUrl"]
            );
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, IHttpContextAccessor httpContextAccessor)
        {
            var ipAddress = GetIpAddress(httpContextAccessor.HttpContext);

            // Chuyển đổi decimal sang double và kiểm tra giới hạn
            double paymentAmount = (double)amount;
            
            // Đảm bảo số tiền nằm trong khoảng cho phép
            if (paymentAmount < MIN_PAYMENT_AMOUNT)
            {
                paymentAmount = MIN_PAYMENT_AMOUNT; // Đặt giá trị tối thiểu là 5.000 VND
            }
            else if (paymentAmount > MAX_PAYMENT_AMOUNT)
            {
                paymentAmount = MAX_PAYMENT_AMOUNT; // Đặt giá trị tối đa là 1.000.000.000 VND
            }

            var request = new PaymentRequest
            {
                PaymentId = orderId,
                Money = paymentAmount,
                Description = orderInfo,
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            return _vnpay.GetPaymentUrl(request);
        }

        public PaymentResult GetPaymentResult(IQueryCollection queryCollection)
        {
            return _vnpay.GetPaymentResult(queryCollection);
        }

        private string GetIpAddress(HttpContext context)
        {
            string ipAddress;
            try
            {
                ipAddress = context.Connection.RemoteIpAddress.ToString();

                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
                {
                    ipAddress = "127.0.0.1";
                }
            }
            catch (Exception)
            {
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }
    }
} 