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
            string systemPrompt = @$"
Bạn là trợ lý tư vấn mua hàng chuyên nghiệp.
Thông tin sản phẩm:
- Tên: {product.Name}
- Giá: {product.Price}đ
- Mô tả: {product.Description}
Hãy trả lời ngắn gọn, dễ hiểu và tự nhiên giống nhân viên bán hàng.
";

            string model = "models/gemini-2.5-flash";
            string url = $"https://generativelanguage.googleapis.com/v1beta/{model}:generateContent?key={_apiKey}";

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

            var body = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            // ========================================
            // 🔥 Retry logic khi bị quota / rate limit
            // ========================================
            int retries = 0;

        RETRY_LABEL:

            var response = await _client.PostAsync(url, body);
            var raw = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // ❌ Nếu API trả về lỗi
                if (root.TryGetProperty("error", out var err))
                {
                    string message = err.GetProperty("message").GetString() ?? "";

                    // ------------------------------------------------
                    // 🔥 Quota exceeded → Retry sau 15s (tối đa 2 lần)
                    // ------------------------------------------------
                    if (message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("rate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (retries < 2)
                        {
                            retries++;
                            await Task.Delay(15000); // chờ 15 giây
                            goto RETRY_LABEL;
                        }

                        // fallback sau khi retry hết
                        return "🚫 AI đang quá tải hoặc vượt quota. Vui lòng thử lại sau 1 phút.";
                    }

                    return $"AI gặp lỗi: {message}";
                }

                // ------------------------------------------------
                // 🔥 Parse content hợp lệ
                // ------------------------------------------------
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
