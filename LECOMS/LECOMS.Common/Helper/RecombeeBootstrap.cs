using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using System.Threading.Tasks;

namespace LECOMS.Common.Helper
{
    public class RecombeeBootstrap
    {
        private readonly RecombeeClient _client;

        public RecombeeBootstrap(RecombeeClient client)
        {
            _client = client;
        }

        /// <summary>
        /// ✅ Khởi tạo schema trong Recommbee (chạy 1 lần duy nhất)
        /// </summary>
        public async Task InitSchemaAsync()
        {
            // --- ITEM (Product) PROPERTIES ---
            await _client.SendAsync(new AddItemProperty("name", "string"));
            await _client.SendAsync(new AddItemProperty("slug", "string"));
            await _client.SendAsync(new AddItemProperty("categoryId", "string"));
            await _client.SendAsync(new AddItemProperty("categoryName", "string"));
            await _client.SendAsync(new AddItemProperty("price", "double"));
            await _client.SendAsync(new AddItemProperty("thumbnailUrl", "string"));
            await _client.SendAsync(new AddItemProperty("shopId", "int"));
            await _client.SendAsync(new AddItemProperty("shopName", "string"));
            await _client.SendAsync(new AddItemProperty("status", "string"));

            // --- USER PROPERTIES (nếu cần cá nhân hoá sâu hơn) ---
            await _client.SendAsync(new AddUserProperty("role", "string"));
            await _client.SendAsync(new AddUserProperty("gender", "string"));
            await _client.SendAsync(new AddUserProperty("preferredCategory", "string"));
        }
    }
}
