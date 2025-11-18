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

        public async Task TrackViewAsync(string userId, string itemId)
        {
            await _client.SendAsync(new AddDetailView(userId, itemId, cascadeCreate: true));
        }

        public async Task TrackAddToCartAsync(string userId, string itemId)
        {
            await _client.SendAsync(new AddCartAddition(userId, itemId, cascadeCreate: true));
        }

        public async Task TrackPurchaseAsync(string userId, string itemId)
        {
            await _client.SendAsync(new AddPurchase(userId, itemId, cascadeCreate: true));
        }

        public async Task TrackRatingAsync(string userId, string itemId, double rating)
        {
            await _client.SendAsync(new AddRating(userId, itemId, rating, cascadeCreate: true));
        }
    }
}
