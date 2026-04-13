using System;
using System.Collections.Generic;

namespace ShopBanHoaLyly.Models
{
    public class AppChatMessage
    {
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    public class AppChatSession
    {
        public List<AppChatMessage> Messages { get; set; } = new List<AppChatMessage>();
        public string ChatId { get; set; } = Guid.NewGuid().ToString();
    }
} 