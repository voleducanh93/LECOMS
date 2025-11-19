using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using System;
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

        // ===========================================================================
        // 1️⃣ TRACK: User View Item (DetailView)
        // ===========================================================================
        public async Task TrackViewAsync(string userId, string itemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                    return;

                await _client.SendAsync(
                    new AddDetailView(userId, itemId, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // You may log the error if needed
            }
        }

        // ===========================================================================
        // 2️⃣ TRACK: User Adds Item To Cart
        // ===========================================================================
        public async Task TrackAddToCartAsync(string userId, string itemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                    return;

                await _client.SendAsync(
                    new AddCartAddition(userId, itemId, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // log if needed
            }
        }

        // ===========================================================================
        // 3️⃣ TRACK: User Purchase Item
        // ===========================================================================
        public async Task TrackPurchaseAsync(string userId, string itemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                    return;

                await _client.SendAsync(
                    new AddPurchase(userId, itemId, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // log
            }
        }

        // ===========================================================================
        // 4️⃣ TRACK: User Rating Item (optional)
        // ===========================================================================
        public async Task TrackRatingAsync(string userId, string itemId, double rating)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                    return;

                await _client.SendAsync(
                    new AddRating(userId, itemId, rating, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // log
            }
        }

        // ===========================================================================
        // 5️⃣ TRACK: User Bookmark / Favorite Item  (optional)
        // ===========================================================================
        public async Task TrackBookmarkAsync(string userId, string itemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                    return;

                // Recombee không có API "bookmark" chính thức,
                // nên ta dùng AddDetailView + AddPurchase weight = 0
                // hoặc một events custom mapping.
                await _client.SendAsync(
                    new AddDetailView(userId, itemId, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // log
            }
        }

        // ===========================================================================
        // 6️⃣ TRACK: COURSE ENROLL (custom event)
        // ===========================================================================
        public async Task TrackCourseEnrollAsync(string userId, string courseId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId))
                    return;

                // Ta giả lập Enroll = Purchase để ưu tiên recommend course
                await _client.SendAsync(
                    new AddPurchase(userId, courseId, cascadeCreate: true)
                );
            }
            catch (Exception)
            {
                // log
            }
        }
    }
}
