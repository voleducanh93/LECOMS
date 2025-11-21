using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ICommunityService
    {
        Task<CommunityPost> CreatePostAsync(string userId, string title, string body);
        Task<IEnumerable<CommunityPost>> GetPublicPostsAsync();
        Task<Comment> CreateCommentAsync(string userId, string postId, string body);
    }

}
