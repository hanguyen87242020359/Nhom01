using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ShopBanHoaLyly.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            var emailConfig = configuration.GetSection("EmailSettings");
            _smtpHost = emailConfig["SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
            _smtpUsername = emailConfig["SmtpUsername"] ?? "";
            _smtpPassword = emailConfig["SmtpPassword"] ?? "";
            _senderEmail = emailConfig["SenderEmail"] ?? _smtpUsername;
            _senderName = emailConfig["SenderName"] ?? "Shop Hoa Lyly";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
            }
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string orderDate)
        {
            string subject = $"Xác nhận đơn hàng #{orderId} - Shop Hoa Lyly";
            string message = $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        color: #333;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                        border: 1px solid #ddd;
                        border-radius: 5px;
                    }}
                    .header {{
                        background-color: #E72463;
                        color: white;
                        padding: 10px 20px;
                        text-align: center;
                        border-radius: 5px 5px 0 0;
                    }}
                    .content {{
                        padding: 20px;
                    }}
                    .footer {{
                        text-align: center;
                        margin-top: 20px;
                        font-size: 12px;
                        color: #777;
                    }}
                    .button {{
                        display: inline-block;
                        padding: 10px 20px;
                        background-color: #E72463;
                        color: white !important;
                        text-decoration: none;
                        border-radius: 5px;
                        margin-top: 15px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Đơn hàng của bạn đã được xác nhận!</h2>
                    </div>
                    <div class='content'>
                        <p>Kính gửi <strong>{customerName}</strong>,</p>
                        <p>Cảm ơn bạn đã đặt hàng tại Shop Hoa Lyly. Đơn hàng của bạn đã được xác nhận và đang được xử lý.</p>
                        
                        <p><strong>Thông tin đơn hàng:</strong></p>
                        <ul>
                            <li>Mã đơn hàng: #{orderId}</li>
                            <li>Ngày đặt: {orderDate}</li>
                            <li>Tổng giá trị: {string.Format("{0:C0}", totalAmount)}</li>
                        </ul>
                        
                        <p>Bạn có thể theo dõi tình trạng đơn hàng trong tài khoản của mình trên trang web của chúng tôi.</p>
                        
                        <div style='text-align: center;'>
                            <a href='https://localhost:7155/Order/Details/{orderId}' class='button'>Xem chi tiết đơn hàng</a>
                        </div>
                        
                        <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại hỗ trợ trên website.</p>
                        
                        <p>Trân trọng,<br>Shop Hoa Lyly</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 Shop Hoa Lyly.</p>
                        <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                    </div>
                </div>
            </body>
            </html>
            ";

            await SendEmailAsync(toEmail, subject, message);
        }
    }
} 