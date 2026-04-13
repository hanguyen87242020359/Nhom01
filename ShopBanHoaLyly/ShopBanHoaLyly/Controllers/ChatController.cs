using Microsoft.AspNetCore.Mvc;
using ShopBanHoaLyly.Models;
using ShopBanHoaLyly.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShopBanHoaLyly.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        
        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            var chatHistory = _chatService.GetChatHistory();
            return View(chatHistory);
        }
        
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Tin nhắn không được để trống");
            }
            
            var response = await _chatService.GetResponseAsync(request.Message);
            return Json(new { message = response });
        }
        
        [HttpPost]
        public IActionResult ClearChat()
        {
            _chatService.ClearChat();
            return RedirectToAction("Index");
        }
    }
    
    public class ChatRequest
    {
        public string Message { get; set; }
    }
} 