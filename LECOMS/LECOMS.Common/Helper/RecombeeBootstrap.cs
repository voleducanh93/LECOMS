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

        // ============================================================
        // SAFE HELPERS — Không bao giờ throw lỗi property đã tồn tại
        // ============================================================
        private async Task SafeAddItemProperty(string name, string type)
        {
            try
            {
                await _client.SendAsync(new AddItemProperty(name, type));
            }
            catch
            {
                // Nếu đã tồn tại → bỏ qua
            }
        }

        private async Task SafeAddUserProperty(string name, string type)
        {
            try
            {
                await _client.SendAsync(new AddUserProperty(name, type));
            }
            catch
            {
                // Nếu đã tồn tại → bỏ qua
            }
        }

        // ============================================================
        // INIT SCHEMA — gọi 1 lần, không bao giờ lỗi duplicate
        // ============================================================
        public async Task InitSchemaAsync()
        {
            // ------------------------------
            // PRODUCT PROPERTIES
            // ------------------------------
            await SafeAddItemProperty("name", "string");
            await SafeAddItemProperty("slug", "string");
            await SafeAddItemProperty("categoryId", "string");
            await SafeAddItemProperty("categoryName", "string");
            await SafeAddItemProperty("price", "double");
            await SafeAddItemProperty("thumbnailUrl", "string");
            await SafeAddItemProperty("shopId", "int");
            await SafeAddItemProperty("shopName", "string");
            await SafeAddItemProperty("status", "string");

            // ------------------------------
            // COURSE PROPERTIES
            // ------------------------------
            await SafeAddItemProperty("type", "string");
            await SafeAddItemProperty("title", "string");
            await SafeAddItemProperty("courseThumbnail", "string");

            // ------------------------------
            // USER PROPERTIES
            // ------------------------------
            await SafeAddUserProperty("role", "string");
            await SafeAddUserProperty("gender", "string");
            await SafeAddUserProperty("preferredCategory", "string");
        }
    }
}
