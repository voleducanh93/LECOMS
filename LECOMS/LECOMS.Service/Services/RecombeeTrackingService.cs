using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class RecombeeTrackingService
    {
        private readonly RecombeeClient _client;

        public RecombeeTrackingService(RecombeeClient client)
        {
            _client = client;
        }

        // ✅ Ghi hành vi: user xem sản phẩm
        public async Task TrackViewAsync(string userId, string productId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(productId))
                return;

            await _client.SendAsync(new AddDetailView(userId, productId, cascadeCreate: true));
        }

        // ✅ Ghi hành vi: user thêm sản phẩm vào giỏ
        public async Task TrackAddToCartAsync(string userId, string productId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(productId))
                return;

            await _client.SendAsync(new AddCartAddition(userId, productId, cascadeCreate: true));
        }

        // ✅ Ghi hành vi: user mua sản phẩm
        public async Task TrackPurchaseAsync(string userId, string productId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(productId))
                return;

            await _client.SendAsync(new AddPurchase(userId, productId, cascadeCreate: true));
        }

        // ✅ (tuỳ chọn) Ghi hành vi: user đánh giá sản phẩm
        public async Task TrackRatingAsync(string userId, string productId, double rating)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(productId))
                return;

            await _client.SendAsync(new AddRating(userId, productId, rating, cascadeCreate: true));
        }
    }
}
