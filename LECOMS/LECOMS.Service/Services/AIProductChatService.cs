using System.Net.Http;
using System.Text;
using System.Text.Json;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LECOMS.Service.Services
{
    public class AIProductChatService : IAIProductChatService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;

        public AIProductChatService(HttpClient client, IConfiguration config)
        {
            _client = client;
            _apiKey = config["Gemini:ApiKey"];
        }

        public async Task<string> GetProductAnswerAsync(Product product, string userMessage)
        {
            string systemPrompt = $@"
Bạn là trợ lý tư vấn mua hàng chuyên nghiệp.
Thông tin sản phẩm:
- Tên: {product.Name}
- Giá: {product.Price}đ
- Mô tả: {product.Description}
Hãy trả lời ngắn gọn, dễ hiểu và tự nhiên giống nhân viên bán hàng.
";

            // 🔥 Model Google Gemini mới → VALID 100%
            var model = "models/gemini-2.5-flash";

            var url =
                $"https://generativelanguage.googleapis.com/v1/{model}:generateContent?key={_apiKey}";


            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = systemPrompt + "\nKhách hỏi: " + userMessage }
                        }
                    }
                }
            };

            var jsonBody = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var res = await _client.PostAsync(url, jsonBody);
            var raw = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Nếu lỗi trả về
            if (root.TryGetProperty("error", out var err))
            {
                return $"AI gặp lỗi: {err.GetProperty("message").GetString()}";
            }

            // Parse nội dung AI trả lời
            try
            {
                string content =
                    root.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                return content ?? "Xin lỗi, tôi chưa thể trả lời câu này.";
            }
            catch
            {
                return "Xin lỗi, AI không đọc được câu trả lời.";
            }
        }
    }
}
