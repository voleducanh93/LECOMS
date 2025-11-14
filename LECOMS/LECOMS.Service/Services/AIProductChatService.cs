using System.Text;
using System.Text.Json;
using LECOMS.Data.Entities;
using LECOMS.ServiceContract.Interfaces;

namespace LECOMS.Service.Services
{
    public class AIProductChatService : IAIProductChatService
    {
        private readonly HttpClient _client;

        public AIProductChatService(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> GetProductAnswerAsync(Product product, string userMessage)
        {
            string systemPrompt = $@"
Bạn là trợ lý tư vấn mua hàng thân thiện.
Thông tin sản phẩm:
- Tên: {product.Name}
- Giá: {product.Price}
- Mô tả: {product.Description}
- Shop: {product.Shop.Name}

Hãy trả lời tự nhiên, ngắn gọn, giống nhân viên bán hàng thật.
";

            var payload = new
            {
                model = "openai/gpt-oss-20b",
                messages = new[]
                {
        new { role = "system", content = systemPrompt },
        new { role = "user", content = userMessage }
    }
            };


            var jsonBody = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var res = await _client.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                jsonBody
            );

            var raw = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Nếu lỗi
            if (root.TryGetProperty("error", out var err))
            {
                return $"AI gặp lỗi: {err.GetProperty("message").GetString()}";
            }

            // Parse content
            string content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "Xin lỗi, tôi chưa thể trả lời câu này.";
        }
    }
}
