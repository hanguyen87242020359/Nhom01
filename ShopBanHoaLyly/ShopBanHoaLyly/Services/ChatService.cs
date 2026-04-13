using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI;
using ShopBanHoaLyly.Models;

namespace ShopBanHoaLyly.Services
{
    public class ChatService
    {
        private readonly IGenerativeAI _generativeAI;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ShopHoaLyLyContext _dbContext;
        
        private const string SYSTEM_INSTRUCTION = @"Bạn là trợ lý ảo của Shop Hoa Lyly, một cửa hàng bán hoa trực tuyến. Nhiệm vụ của bạn CHỈ giới hạn trong hai việc sau:

1. Tìm kiếm thông tin về sản phẩm hoa khi khách hàng hỏi về một loại hoa cụ thể
2. Gợi ý sản phẩm hoa phù hợp dựa trên nhu cầu, dịp, và ngân sách của khách hàng

Quy tắc quan trọng:
- CHỈ trả lời các câu hỏi liên quan đến việc tìm kiếm hoặc gợi ý sản phẩm hoa
- KHÔNG trả lời bất kỳ câu hỏi nào khác không liên quan đến sản phẩm hoa
- KHÔNG hiển thị mã sản phẩm (SP...) trong câu trả lời của bạn
- Nếu được hỏi câu hỏi ngoài lề hoặc không liên quan đến tìm kiếm/gợi ý sản phẩm, hãy lịch sự từ chối và đề nghị khách hàng hỏi về sản phẩm hoa
- Luôn trả lời bằng tiếng Việt, lịch sự và thân thiện
- Hãy định dạng câu trả lời bằng Markdown để tăng tính thẩm mỹ (ví dụ: **in đậm** cho tên sản phẩm, *in nghiêng* cho giá)
- Khi đề cập đến giá, hãy luôn sử dụng định dạng tiền tệ (ví dụ: 250.000₫)
- Khi giới thiệu một sản phẩm, LUÔN thêm link xem chi tiết có dạng: [Xem chi tiết Tên Sản Phẩm] (thay 'Tên Sản Phẩm' bằng tên thực của sản phẩm, ví dụ: [Xem chi tiết Bó Hoa Baby Trắng])

QUAN TRỌNG - FUNCTION CALLING:
Bạn có thể sử dụng 2 function sau đây để hỗ trợ khách hàng:

1. SEARCH_PRODUCT - Dùng khi khách hỏi về thông tin sản phẩm cụ thể:
[FUNCTION_CALL: SEARCH_PRODUCT]
tên_sản_phẩm: xxx
loại_hoa: xxx (nếu được đề cập)
[END_FUNCTION_CALL]

2. RECOMMEND_PRODUCTS - Dùng khi khách muốn gợi ý sản phẩm phù hợp:
[FUNCTION_CALL: RECOMMEND_PRODUCTS]
dịp: xxx (ví dụ: sinh nhật, tỏ tình, khai trương...)
loại_hoa: xxx (nếu khách có sở thích)
ngân_sách: xxx (khoảng giá nếu được đề cập)
yêu_cầu_khác: xxx (yêu cầu đặc biệt khác)
[END_FUNCTION_CALL]

Ví dụ:
- Nếu khách hỏi 'Hoa hồng đỏ giá bao nhiêu?', hãy dùng SEARCH_PRODUCT
- Nếu khách nói 'Tư vấn cho tôi loại hoa phù hợp để tỏ tình', hãy dùng RECOMMEND_PRODUCTS

Với mọi yêu cầu khác không liên quan đến việc tìm sản phẩm hoặc gợi ý sản phẩm, hãy lịch sự từ chối và hướng dẫn khách hàng hỏi về sản phẩm hoa.";

        // Thêm hằng số dành cho function call
        private const string SEARCH_FUNCTION_PATTERN = @"\[FUNCTION_CALL:\s*SEARCH_PRODUCT\](.*?)\[END_FUNCTION_CALL\]";
        private const string RECOMMEND_FUNCTION_PATTERN = @"\[FUNCTION_CALL:\s*RECOMMEND_PRODUCTS\](.*?)\[END_FUNCTION_CALL\]";
        
        public ChatService(IGenerativeAI generativeAI, IHttpContextAccessor httpContextAccessor, ShopHoaLyLyContext dbContext)
        {
            _generativeAI = generativeAI;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }
        
        public async Task<string> GetResponseAsync(string message)
        {
            // Chỉ định model khi gọi GenerativeModel
            var genModel = _generativeAI.GenerativeModel(Model.Gemini20Flash);
            
            // Lấy thông tin chat session từ session của người dùng
            var appChatSession = GetOrCreateChatSession();
            
            // Thêm tin nhắn của người dùng vào danh sách tin nhắn
            appChatSession.Messages.Add(new AppChatMessage 
            { 
                Content = message, 
                IsUser = true 
            });
            
            try
            {
                // Khởi tạo chat session mới của Gemini cho mỗi lần trả lời
                var geminiChat = genModel.StartChat();
                
                // Luôn gửi system instruction đầu tiên cho mọi cuộc hội thoại
                await geminiChat.SendMessage("Hãy đóng vai: " + SYSTEM_INSTRUCTION);
                
                // Thêm lịch sử hội thoại vào chat session Gemini
                // Chỉ gửi tối đa 5 cặp tin nhắn gần nhất để tránh quá tải context window
                int maxPairsToSend = 5;
                int startIdx = Math.Max(0, appChatSession.Messages.Count - (maxPairsToSend * 2) - 1);
                
                for (int i = startIdx; i < appChatSession.Messages.Count - 1; i++)
                {
                    await geminiChat.SendMessage(appChatSession.Messages[i].Content);
                }
                
                // Gửi tin nhắn mới nhất của người dùng và nhận phản hồi
                var userResponse = await geminiChat.SendMessage(message);
                var botResponse = userResponse.Text;
                
                // Kiểm tra function call
                var searchMatch = Regex.Match(botResponse, SEARCH_FUNCTION_PATTERN, RegexOptions.Singleline);
                var recommendMatch = Regex.Match(botResponse, RECOMMEND_FUNCTION_PATTERN, RegexOptions.Singleline);
                
                if (searchMatch.Success)
                {
                    // Phát hiện function call tìm kiếm sản phẩm cụ thể
                    var functionContent = searchMatch.Groups[1].Value;
                    
                    // Phân tích nội dung function call
                    string productName = ExtractValueFromFunctionCall(functionContent, "tên_sản_phẩm");
                    string flowerType = ExtractValueFromFunctionCall(functionContent, "loại_hoa");
                    
                    // Tìm kiếm sản phẩm dựa trên thông tin
                    var productInfo = await SearchProductAsync(productName, flowerType);
                    
                    // Loại bỏ function call từ câu trả lời
                    botResponse = botResponse.Replace(searchMatch.Value, "");
                    
                    // Nếu tìm thấy sản phẩm, gửi lại thông tin cho Gemini để xử lý
                    if (!string.IsNullOrEmpty(productInfo))
                    {
                        // Gửi thông tin sản phẩm cho AI để tạo câu trả lời mới
                        var finalInstruction = "Dựa vào thông tin tìm kiếm sau đây, hãy tạo câu trả lời cho người dùng. " +
                            "Hãy định dạng câu trả lời thân thiện và hữu ích, bao gồm thông tin sản phẩm chi tiết. " +
                            "Đừng tiết lộ rằng bạn đang dùng thông tin này, hãy trả lời tự nhiên như thể bạn biết thông tin này. " +
                            "Đừng trình bày dưới dạng bảng, hãy diễn đạt như đang trò chuyện. " +
                            "QUAN TRỌNG: Cho mỗi sản phẩm, sử dụng mẫu [Xem chi tiết TÊN_SẢN_PHẨM] (thay TÊN_SẢN_PHẨM bằng tên thực của sản phẩm). " +
                            "\n\nThông tin sản phẩm:\n" + productInfo;
                            
                        var formattedResponse = await geminiChat.SendMessage(finalInstruction);
                        botResponse = formattedResponse.Text;
                    }
                    else
                    {
                        // Không tìm thấy sản phẩm, trả về thông báo
                        botResponse += "\n\nXin lỗi, hiện tại Shop Hoa Lyly không có sản phẩm phù hợp với yêu cầu của bạn. Bạn có thể mô tả loại hoa hoặc dịp bạn cần để tôi gợi ý sản phẩm khác không?";
                    }
                }
                else if (recommendMatch.Success)
                {
                    // Phát hiện function call gợi ý sản phẩm
                    var functionContent = recommendMatch.Groups[1].Value;
                    
                    // Phân tích nội dung function call
                    string occasion = ExtractValueFromFunctionCall(functionContent, "dịp");
                    string flowerType = ExtractValueFromFunctionCall(functionContent, "loại_hoa");
                    string budget = ExtractValueFromFunctionCall(functionContent, "ngân_sách");
                    string otherRequirements = ExtractValueFromFunctionCall(functionContent, "yêu_cầu_khác");
                    
                    // Tìm kiếm gợi ý sản phẩm dựa trên thông tin
                    var recommendationsInfo = await RecommendProductsAsync(occasion, flowerType, budget, otherRequirements);
                    
                    // Loại bỏ function call từ câu trả lời
                    botResponse = botResponse.Replace(recommendMatch.Value, "");
                    
                    // Nếu tìm thấy gợi ý sản phẩm, gửi lại thông tin cho Gemini để xử lý
                    if (!string.IsNullOrEmpty(recommendationsInfo))
                    {
                        // Gửi thông tin sản phẩm cho AI để tạo câu trả lời mới
                        var finalInstruction = "Dựa vào các sản phẩm gợi ý sau đây, hãy tạo câu trả lời cho người dùng. " +
                            "Hãy định dạng câu trả lời thân thiện và hữu ích, giới thiệu các sản phẩm gợi ý. " +
                            "Nên giới thiệu nhiều sản phẩm để người dùng có lựa chọn, và giải thích tại sao sản phẩm đó phù hợp với nhu cầu. " +
                            "Đừng tiết lộ rằng bạn đang dùng thông tin này, hãy trả lời tự nhiên như thể bạn biết thông tin này. " +
                            "QUAN TRỌNG: Cho mỗi sản phẩm, sử dụng mẫu [Xem chi tiết TÊN_SẢN_PHẨM] (thay TÊN_SẢN_PHẨM bằng tên thực của từng sản phẩm). " +
                            "\n\nSản phẩm gợi ý:\n" + recommendationsInfo;
                            
                        var formattedResponse = await geminiChat.SendMessage(finalInstruction);
                        botResponse = formattedResponse.Text;
                    }
                    else
                    {
                        // Không tìm thấy gợi ý phù hợp, trả về thông báo
                        botResponse += "\n\nXin lỗi, hiện tại Shop Hoa Lyly không tìm thấy sản phẩm phù hợp với yêu cầu của bạn. Bạn có thể mô tả chi tiết hơn hoặc thử yêu cầu với tiêu chí khác không?";
                    }
                }
                
                // Xử lý phản hồi cuối cùng - thay thế mẫu link với link thật và loại bỏ mã sản phẩm
                string processedResponse = ProcessBotResponse(botResponse);
                
                // Lưu phản hồi đã xử lý vào app chat session
                appChatSession.Messages.Add(new AppChatMessage 
                { 
                    Content = processedResponse, 
                    IsUser = false 
                });
                
                // Lưu chat session vào session của người dùng
                SaveChatSession(appChatSession);
                
                return processedResponse;
            }
            catch (Exception ex)
            {
                // Trường hợp có lỗi, trả về thông báo lỗi
                return "Xin lỗi, có lỗi xảy ra khi xử lý tin nhắn của bạn: " + ex.Message;
            }
        }
        
        // Phương thức trích xuất giá trị từ function call
        private string ExtractValueFromFunctionCall(string content, string key)
        {
            var match = Regex.Match(content, $@"{key}:\s*(.*?)($|\n)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
        
        // Phương thức tìm kiếm sản phẩm cụ thể
        private async Task<string> SearchProductAsync(string productName, string flowerType)
        {
            if (string.IsNullOrEmpty(productName) && string.IsNullOrEmpty(flowerType))
                return string.Empty;
                
            try
            {
                // Tạo query tìm kiếm
                var query = _dbContext.SanPhams.AsQueryable();
                
                if (!string.IsNullOrEmpty(productName))
                {
                    query = query.Where(p => p.TenSanPham.Contains(productName));
                }
                
                if (!string.IsNullOrEmpty(flowerType))
                {
                    query = query.Where(p => p.TenSanPham.Contains(flowerType) || 
                                            (p.MoTa != null && p.MoTa.Contains(flowerType)));
                }
                
                // Thực thi query
                var products = await query
                    .Include(p => p.MaDanhMucNavigation)
                    .Where(p => p.SoLuongCon > 0)
                    .Take(3)
                    .Select(p => new
                    {
                        p.MaSanPham,
                        p.TenSanPham,
                        p.GiaBan,
                        DanhMuc = p.MaDanhMucNavigation.TenDanhMuc,
                        MoTa = p.MoTa
                    })
                    .ToListAsync();
                
                if (!products.Any())
                    return string.Empty;
                
                // Format thông tin sản phẩm cho AI
                var productInfo = new System.Text.StringBuilder();
                foreach (var product in products)
                {
                    productInfo.AppendLine($"- Tên: {product.TenSanPham}");
                    productInfo.AppendLine($"  Mã: SP{product.MaSanPham}");
                    productInfo.AppendLine($"  Danh mục: {product.DanhMuc}");
                    productInfo.AppendLine($"  Giá: {string.Format("{0:C0}", product.GiaBan)}");
                    
                    if (!string.IsNullOrEmpty(product.MoTa))
                    {
                        // Rút gọn mô tả nếu quá dài
                        string description = product.MoTa.Length > 200 
                            ? product.MoTa.Substring(0, 200) + "..." 
                            : product.MoTa;
                        productInfo.AppendLine($"  Mô tả: {description}");
                    }
                    
                    productInfo.AppendLine();
                }
                
                return productInfo.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        
        // Phương thức gợi ý sản phẩm
        private async Task<string> RecommendProductsAsync(string occasion, string flowerType, string budget, string otherRequirements)
        {
            try
            {
                // Tạo query gợi ý
                var query = _dbContext.SanPhams
                    .Include(p => p.MaDanhMucNavigation)
                    .Where(p => p.SoLuongCon > 0)
                    .AsQueryable();
                
                // Lọc theo dịp nếu có
                if (!string.IsNullOrEmpty(occasion))
                {
                    query = query.Where(p => (p.MoTa != null && p.MoTa.Contains(occasion)) || 
                                             p.TenSanPham.Contains(occasion));
                }
                
                // Lọc theo loại hoa
                if (!string.IsNullOrEmpty(flowerType))
                {
                    query = query.Where(p => p.TenSanPham.Contains(flowerType) || 
                                            (p.MoTa != null && p.MoTa.Contains(flowerType)));
                }
                
                // Lọc theo ngân sách
                if (!string.IsNullOrEmpty(budget))
                {
                    // Cố gắng trích xuất các con số từ chuỗi ngân sách
                    var numberMatches = Regex.Matches(budget, @"\d+");
                    if (numberMatches.Count > 0)
                    {
                        // Xử lý trường hợp có một khoảng giá (ví dụ: từ 200.000 đến 500.000)
                        if (numberMatches.Count >= 2)
                        {
                            decimal minPrice = 0, maxPrice = decimal.MaxValue;
                            
                            if (decimal.TryParse(numberMatches[0].Value, out decimal price1) && 
                                decimal.TryParse(numberMatches[1].Value, out decimal price2))
                            {
                                minPrice = Math.Min(price1, price2);
                                maxPrice = Math.Max(price1, price2);
                                
                                // Điều chỉnh nếu giá trị quá nhỏ (có thể là đơn vị nghìn/triệu)
                                if (minPrice < 1000 && budget.Contains("nghìn")) minPrice *= 1000;
                                if (minPrice < 1000 && budget.Contains("triệu")) minPrice *= 1000000;
                                if (maxPrice < 1000 && budget.Contains("nghìn")) maxPrice *= 1000;
                                if (maxPrice < 1000 && budget.Contains("triệu")) maxPrice *= 1000000;
                                
                                query = query.Where(p => p.GiaBan >= minPrice && p.GiaBan <= maxPrice);
                            }
                        }
                        else
                        {
                            // Xử lý trường hợp chỉ có một con số (ví dụ: khoảng 300.000)
                            if (decimal.TryParse(numberMatches[0].Value, out decimal basePrice))
                            {
                                // Điều chỉnh nếu giá trị quá nhỏ
                                if (basePrice < 1000 && budget.Contains("nghìn")) basePrice *= 1000;
                                if (basePrice < 1000 && budget.Contains("triệu")) basePrice *= 1000000;
                                
                                decimal lowerBound = basePrice * 0.7m;
                                decimal upperBound = basePrice * 1.3m;
                                
                                // Nếu ngân sách có từ "dưới", "ít hơn", "nhỏ hơn" thì lọc các sản phẩm có giá nhỏ hơn
                                if (budget.Contains("dưới") || budget.Contains("ít hơn") || budget.Contains("nhỏ hơn"))
                                {
                                    query = query.Where(p => p.GiaBan <= basePrice);
                                }
                                // Nếu ngân sách có từ "trên", "lớn hơn", "nhiều hơn" thì lọc các sản phẩm có giá lớn hơn
                                else if (budget.Contains("trên") || budget.Contains("lớn hơn") || budget.Contains("nhiều hơn"))
                                {
                                    query = query.Where(p => p.GiaBan >= basePrice);
                                }
                                // Nếu không có từ khóa đặc biệt, lọc các sản phẩm có giá trong khoảng +/- 30%
                                else
                                {
                                    query = query.Where(p => p.GiaBan >= lowerBound && p.GiaBan <= upperBound);
                                }
                            }
                        }
                    }
                }
                
                // Lọc theo yêu cầu khác nếu có
                if (!string.IsNullOrEmpty(otherRequirements))
                {
                    query = query.Where(p => (p.MoTa != null && p.MoTa.Contains(otherRequirements)) || 
                                              p.TenSanPham.Contains(otherRequirements));
                }
                
                // Lấy danh sách sản phẩm phù hợp
                var recommendations = await query
                    .OrderByDescending(p => p.ChiTietDonHangs.Count) // Sắp xếp theo độ phổ biến
                    .Take(5) // Lấy tối đa 5 sản phẩm gợi ý
                    .Select(p => new
                    {
                        p.MaSanPham,
                        p.TenSanPham,
                        p.GiaBan,
                        DanhMuc = p.MaDanhMucNavigation.TenDanhMuc,
                        MoTaNgan = p.MoTa != null ? (p.MoTa.Length > 150 ? p.MoTa.Substring(0, 150) + "..." : p.MoTa) : ""
                    })
                    .ToListAsync();
                
                if (!recommendations.Any())
                {
                    // Nếu không tìm thấy sản phẩm phù hợp với các điều kiện, thử tìm sản phẩm bán chạy
                    recommendations = await _dbContext.SanPhams
                        .Where(p => p.SoLuongCon > 0)
                        .Include(p => p.MaDanhMucNavigation)
                        .OrderByDescending(p => p.ChiTietDonHangs.Count)
                        .Take(5)
                        .Select(p => new
                        {
                            p.MaSanPham,
                            p.TenSanPham,
                            p.GiaBan,
                            DanhMuc = p.MaDanhMucNavigation.TenDanhMuc,
                            MoTaNgan = p.MoTa != null ? (p.MoTa.Length > 150 ? p.MoTa.Substring(0, 150) + "..." : p.MoTa) : ""
                        })
                        .ToListAsync();
                }
                
                if (!recommendations.Any())
                    return string.Empty;
                
                // Format danh sách gợi ý
                var recommendationsInfo = new System.Text.StringBuilder();
                foreach (var product in recommendations)
                {
                    recommendationsInfo.AppendLine($"- Tên: {product.TenSanPham}");
                    recommendationsInfo.AppendLine($"  Mã: SP{product.MaSanPham}");
                    recommendationsInfo.AppendLine($"  Danh mục: {product.DanhMuc}");
                    recommendationsInfo.AppendLine($"  Giá: {string.Format("{0:C0}", product.GiaBan)}");
                    
                    if (!string.IsNullOrEmpty(product.MoTaNgan))
                    {
                        recommendationsInfo.AppendLine($"  Mô tả: {product.MoTaNgan}");
                    }
                    
                    recommendationsInfo.AppendLine();
                }
                
                return recommendationsInfo.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        
        private string ProcessBotResponse(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return responseText;
                
            // Lọc bỏ các mã sản phẩm (SPxxx hoặc SP xxx)
            responseText = Regex.Replace(responseText, @"\bSP\s*\d+\b", "");
            
            // Tìm tất cả các mẫu [Xem chi tiết XXX]
            var productLinkMatches = Regex.Matches(responseText, @"\[Xem chi tiết ([^\]]+)\]", RegexOptions.IgnoreCase);
            
            if (productLinkMatches.Count == 0)
                return responseText;

            // Danh sách các sản phẩm tìm thấy để tránh trùng lặp
            var processedProducts = new HashSet<int>();
            
            // Xử lý từng mẫu link
            foreach (Match match in productLinkMatches)
            {
                string fullMatch = match.Value; // [Xem chi tiết XXX]
                string productName = match.Groups[1].Value; // XXX (tên sản phẩm)
                
                // Tìm sản phẩm trong database dựa trên tên
                var product = _dbContext.SanPhams
                    .Where(p => p.TenSanPham.Contains(productName) || productName.Contains(p.TenSanPham))
                    .FirstOrDefault();
                    
                if (product != null && !processedProducts.Contains(product.MaSanPham))
                {
                    // Thêm vào danh sách đã xử lý
                    processedProducts.Add(product.MaSanPham);
                    
                    // Tạo link tùy chỉnh cho sản phẩm này
                    string productLink = $"/Home/Details/{product.MaSanPham}";
                    string productLinkHtml = $"<a href='{productLink}' target='_blank' style='font-weight: bold; color: #0d6efd'>Xem chi tiết {product.TenSanPham}</a>";
                    
                    // Thay thế mẫu link bằng HTML thực
                    responseText = responseText.Replace(fullMatch, productLinkHtml);
                }
            }
            
            return responseText;
        }
        
        private AppChatSession GetOrCreateChatSession()
        {
            var sessionJson = _httpContextAccessor.HttpContext.Session.GetString("ChatSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return new AppChatSession();
            }
            
            return JsonSerializer.Deserialize<AppChatSession>(sessionJson);
        }
        
        private void SaveChatSession(AppChatSession chatSession)
        {
            // Giới hạn số lượng tin nhắn để tránh quá tải session
            if (chatSession.Messages.Count > 20)
            {
                chatSession.Messages = chatSession.Messages.GetRange(
                    chatSession.Messages.Count - 20,
                    20
                );
            }
            
            var sessionJson = JsonSerializer.Serialize(chatSession);
            _httpContextAccessor.HttpContext.Session.SetString("ChatSession", sessionJson);
        }
        
        public void ClearChat()
        {
            _httpContextAccessor.HttpContext.Session.Remove("ChatSession");
        }
        
        public List<AppChatMessage> GetChatHistory()
        {
            var chatSession = GetOrCreateChatSession();
            return chatSession.Messages;
        }
    }
} 