using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class CommunityService : ICommunityService
    {
        private readonly IUnitOfWork _uow;

        public CommunityService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CommunityPost> CreatePostAsync(string userId, string title, string body)
        {
            var post = new CommunityPost
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Title = title,
                Body = body,
                ApprovalStatus = ApprovalStatus.Pending
            };

            await _uow.CommunityPosts.AddAsync(post);
            await _uow.CompleteAsync();

            return post;
        }

        public async Task<IEnumerable<CommunityPost>> GetPublicPostsAsync()
        {
            var posts = await _uow.CommunityPosts.GetAllAsync(
                p => p.ApprovalStatus == ApprovalStatus.Approved,
                includeProperties: "User,Comments"
            );
            return posts;
        }

        public async Task<Comment> CreateCommentAsync(string userId, string postId, string body)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CommunityPostId = postId,
                Body = body
            };

            await _uow.Comments.AddAsync(comment);
            await _uow.CompleteAsync();

            return comment;
        }
    }

}
