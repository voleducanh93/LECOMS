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

        public async Task InitSchemaAsync()
        {
            await _client.SendAsync(new AddItemProperty("type", "string"));
            await _client.SendAsync(new AddItemProperty("name", "string"));
            await _client.SendAsync(new AddItemProperty("slug", "string"));
            await _client.SendAsync(new AddItemProperty("categoryId", "string"));
            await _client.SendAsync(new AddItemProperty("categoryName", "string"));
            await _client.SendAsync(new AddItemProperty("price", "double"));
            await _client.SendAsync(new AddItemProperty("thumbnailUrl", "string"));
            await _client.SendAsync(new AddItemProperty("shopId", "int"));
            await _client.SendAsync(new AddItemProperty("shopName", "string"));
            // COURSE PROPS
            await _client.SendAsync(new AddItemProperty("type", "string"));
            await _client.SendAsync(new AddItemProperty("title", "string"));
            await _client.SendAsync(new AddItemProperty("courseThumbnail", "string"));

            // USER PROPS
            await _client.SendAsync(new AddUserProperty("role", "string"));
            await _client.SendAsync(new AddUserProperty("gender", "string"));
            await _client.SendAsync(new AddUserProperty("preferredCategory", "string"));
        }
    }
}
