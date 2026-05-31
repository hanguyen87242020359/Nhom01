# Chatbot Shop Hoa Lyly sử dụng Gemini AI

## Hướng dẫn thiết lập API key

### Bước 1: Đăng ký tài khoản Google AI Studio
1. Truy cập [Google AI Studio](https://aistudio.google.com/)
2. Đăng nhập bằng tài khoản Google của bạn
3. Chấp nhận các điều khoản dịch vụ

### Bước 2: Tạo API key Gemini
1. Trong Google AI Studio, nhấp vào nút "Get API key" ở góc trên bên phải
2. Chọn "Create API key"
3. Đặt tên cho API key (ví dụ: "ShopHoaLyly-Chatbot")
4. Sao chép API key được tạo ra

### Bước 3: Thêm API key vào ứng dụng
1. Mở file appsettings.json trong dự án
2. Tìm phần cấu hình "Gemini"
3. Thay thế "YOUR_API_KEY_HERE" bằng API key bạn đã sao chép
```json
"Gemini": {
    "ApiKey": "YOUR_API_KEY_HERE"
}
```

### Bước 4: Đăng ký dịch vụ trong Program.cs
Đảm bảo rằng bạn đăng ký dịch vụ Gemini AI đúng cách trong Program.cs:
```csharp
// Đăng ký Gemini AI
builder.Services.AddSingleton<IGenerativeAI>(provider => 
{
    var apiKey = builder.Configuration["Gemini:ApiKey"];
    return new GoogleAI(apiKey);
});
```

### Bước 5: Lưu ý về xung đột tên model
Để tránh xung đột với tên model của thư viện Mscc.GenerativeAI, project sử dụng:
- `AppChatMessage` thay vì `ChatMessage`
- `AppChatSession` thay vì `ChatSession`

Khi sử dụng dịch vụ AI trong ChatService:
```csharp
// Khởi tạo model Gemini
var genModel = _generativeAI.GenerativeModel(Model.Gemini20Flash);

// Khởi tạo chat session của Gemini
var geminiChat = genModel.StartChat();

// Thêm system instruction
geminiChat.AddSystemInstruction("Bạn là trợ lý ảo của...");

// Gửi và nhận tin nhắn
var response = await geminiChat.SendMessage(message);
```

### Bước 6: Khởi động ứng dụng
1. Khởi động lại ứng dụng để áp dụng cấu hình mới
2. Chatbot sẽ xuất hiện ở góc dưới bên phải của trang web

## Chức năng chính của chatbot
- Hỗ trợ khách hàng tìm kiếm sản phẩm phù hợp
- Tư vấn về ý nghĩa các loại hoa
- Giới thiệu các bó hoa phù hợp cho từng dịp/sự kiện
- Hỗ trợ thông tin về đơn hàng và chính sách cửa hàng

## Tùy chỉnh chatbot
Bạn có thể tùy chỉnh vai trò và cách phản hồi của chatbot bằng cách sửa system instruction trong file `ChatService.cs`:
```csharp
chat.AddSystemInstruction("Bạn là trợ lý ảo của Shop Hoa Lyly. Nhiệm vụ của bạn là giúp khách hàng tìm kiếm sản phẩm hoa phù hợp, tư vấn về ý nghĩa các loại hoa, hỗ trợ đặt hàng và trả lời các câu hỏi về chính sách của cửa hàng. Luôn trả lời bằng tiếng Việt và thân thiện.");
``` 

### Thẻ để thanh toán vnpay 

Ngân hàng:	    NCB
Số thẻ:	        9704198526191432198
Tên chủ thẻ:	NGUYEN VAN A
Ngày phát hành:	07/15
Mật khẩu OTP:	123456

### Tài khoản admin
nguyenvana
123456
