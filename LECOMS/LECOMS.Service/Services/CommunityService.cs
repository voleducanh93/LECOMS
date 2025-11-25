using LECOMS.Data.DTOs.Community;
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

        public async Task<IEnumerable<CommunityPostDTO>> GetPublicPostsAsync()
        {
            var posts = await _uow.CommunityPosts.GetAllAsync(
                p => p.ApprovalStatus == ApprovalStatus.Approved,
                includeProperties: "User,Comments.User"
            );

            return posts.Select(p => new CommunityPostDTO
            {
                Id = p.Id,
                Title = p.Title,
                Body = p.Body,
                CreatedAt = p.CreatedAt,

                User = new UserSimpleDTO
                {
                    Id = p.User.Id,
                    UserName = p.User.UserName,
                    Avatar = p.User.ImageUrl
                },

                // ⭐ lấy 2 comment mới nhất
                Comments = p.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(2)
                    .Select(c => new CommentDTO
                    {
                        Id = c.Id,
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        User = new UserSimpleDTO
                        {
                            Id = c.User.Id,
                            UserName = c.User.UserName,
                            Avatar = c.User.ImageUrl
                        }
                    }).ToList()
            });
        }


        public async Task<CommentDTO> CreateCommentAsync(string userId, string postId, string body)
        {
            var user = await _uow.Users.GetAsync(u => u.Id == userId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy người dùng.");

            var post = await _uow.CommunityPosts.GetAsync(p => p.Id == postId);
            if (post == null)
                throw new InvalidOperationException("Không tìm thấy bài đăng.");

            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CommunityPostId = postId,
                Body = body
            };

            await _uow.Comments.AddAsync(comment);
            await _uow.CompleteAsync();

            // Load lại kèm user
            var loaded = await _uow.Comments.GetAsync(
                c => c.Id == comment.Id,
                includeProperties: "User"
            );

            return new CommentDTO
            {
                Id = loaded.Id,
                Body = loaded.Body,
                CreatedAt = loaded.CreatedAt,
                User = new UserSimpleDTO
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Avatar = user.ImageUrl
                }
            };
        }
        public async Task<CommunityPostDTO> GetPostByIdAsync(string postId)
        {
            var post = await _uow.CommunityPosts.GetAsync(
                p => p.Id == postId && p.ApprovalStatus == ApprovalStatus.Approved,
                includeProperties: "User,Comments.User"
            );

            if (post == null)
                throw new KeyNotFoundException("Không tìm thấy bài đăng.");

            return new CommunityPostDTO
            {
                Id = post.Id,
                Title = post.Title,
                Body = post.Body,
                CreatedAt = post.CreatedAt,

                User = new UserSimpleDTO
                {
                    Id = post.User.Id,
                    UserName = post.User.UserName,
                    Avatar = post.User.ImageUrl
                },

                // ⭐ Lấy ALL comment
                Comments = post.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommentDTO
                    {
                        Id = c.Id,
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        User = new UserSimpleDTO
                        {
                            Id = c.User.Id,
                            UserName = c.User.UserName,
                            Avatar = c.User.ImageUrl
                        }
                    }).ToList()
            };
        }

    }

}
