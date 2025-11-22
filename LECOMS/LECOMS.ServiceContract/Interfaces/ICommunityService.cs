using LECOMS.Data.DTOs.Community;
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
        Task<IEnumerable<CommunityPostDTO>> GetPublicPostsAsync();
        Task<CommentDTO> CreateCommentAsync(string userId, string postId, string body);
        Task<CommunityPostDTO> GetPostByIdAsync(string postId);

    }

}
