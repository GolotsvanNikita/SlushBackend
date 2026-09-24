using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Community;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class CommunityService : ICommunityService
{
    private readonly IUnitOfWork _uow;

    public CommunityService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResultDto<CommunityPostDto>> GetGamePostsAsync(
        string gameId,
        CommunityPostType? postType,
        PostSortOption sortOption,
        int page,
        int pageSize,
        Guid? currentUserId = null)
    {
        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Where(p => p.GameId == gameId);

        if (postType.HasValue)
        {
            query = query.Where(p => p.PostType == postType.Value);
        }

        query = sortOption switch
        {
            PostSortOption.ByRating => query
                .OrderByDescending(p => p.LikesCount)
                .ThenByDescending(p => p.CreatedAt),

            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new CommunityPostDto
            {
                Id = p.Id.ToString(),
                AuthorUsername = p.User.Username,
                AuthorAvatarUrl = p.User.AvatarUrl,
                PostType = p.PostType.ToString(),
                Title = p.Title,
                Content = p.Content,
                ShortDescription = p.ShortDescription,
                MediaUrl = p.MediaUrl,
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                IsLiked = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value),
                CreatedAt = p.CreatedAt.ToString("dd.MM.yyyy")
            })
            .ToListAsync();

        return new PagedResultDto<CommunityPostDto>(posts, totalCount, page, pageSize);
    }

    public async Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostDto request)
    {
        var post = new CommunityPost
        {
            UserId = userId,
            GameId = request.GameId,
            PostType = request.PostType,
            Title = request.Title,
            Content = request.Content,
            ShortDescription = request.ShortDescription,
            MediaUrl = request.MediaUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<CommunityPost>().AddAsync(post);
        await _uow.SaveChangesAsync();

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        return new CommunityPostDto
        {
            Id = post.Id.ToString(),
            AuthorUsername = user!.Username,
            AuthorAvatarUrl = user.AvatarUrl,
            PostType = post.PostType.ToString(),
            Title = post.Title,
            Content = post.Content,
            ShortDescription = post.ShortDescription,
            MediaUrl = post.MediaUrl,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            CreatedAt = post.CreatedAt.ToString("dd.MM.yyyy")
        };
    }

    public async Task ToggleLikeAsync(Guid userId, Guid postId)
    {
        var postRepo = _uow.Repository<CommunityPost>();
        var post = await postRepo.GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        var likeRepo = _uow.Repository<PostLike>();
        var existingLike = await likeRepo.AsQueryable()
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike == null)
        {
            await likeRepo.AddAsync(new PostLike { PostId = postId, UserId = userId });
            post.LikesCount++;
        }
        else
        {
            likeRepo.Remove(existingLike);
            post.LikesCount--;
        }

        postRepo.Update(post);
        await _uow.SaveChangesAsync();
    }

    public async Task<PostCommentDto> AddCommentAsync(Guid userId, Guid postId, CreateCommentDto request)
    {
        var post = await _uow.Repository<CommunityPost>().GetByIdAsync(postId);
        if (post == null) throw new Exception("Post not found.");

        var comment = new PostComment
        {
            UserId = userId,
            PostId = postId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<PostComment>().AddAsync(comment);

        post.CommentsCount++;
        _uow.Repository<CommunityPost>().Update(post);

        await _uow.SaveChangesAsync();

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        return new PostCommentDto
        {
            Id = comment.Id,
            AuthorUsername = user!.Username,
            AuthorAvatarUrl = user.AvatarUrl,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt.ToString("dd.MM.yyyy HH:mm")
        };
    }

    public async Task<PagedResultDto<PostCommentDto>> GetPostCommentsAsync(Guid postId, int page, int pageSize)
    {
        var query = _uow.Repository<PostComment>().AsQueryable()
            .Include(c => c.User)
            .Include(c => c.Replies).ThenInclude(r => r.User)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new PostCommentDto
            {
                Id = c.Id,
                AuthorUsername = c.User.Username,
                AuthorAvatarUrl = c.User.AvatarUrl,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("dd.MM.yyyy HH:mm"),

                Replies = c.Replies.OrderBy(r => r.CreatedAt).Select(r => new PostCommentDto
                {
                    Id = r.Id,
                    AuthorUsername = r.User.Username,
                    AuthorAvatarUrl = r.User.AvatarUrl,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                }).ToList()
            })
            .ToListAsync();

        return new PagedResultDto<PostCommentDto>(comments, totalCount, page, pageSize);
    }

    public async Task UpdatePostAsync(Guid userId, Guid postId, UpdateCommunityPostDto request)
    {
        var repo = _uow.Repository<CommunityPost>();
        var post = await repo.GetByIdAsync(postId);

        if (post == null) throw new Exception("Post not found.");

        if (post.UserId != userId) throw new UnauthorizedAccessException("You can only edit your own posts.");

        post.Title = request.Title;
        post.Content = request.Content;
        post.ShortDescription = request.ShortDescription;

        repo.Update(post);
        await _uow.SaveChangesAsync();
    }

    public async Task DeletePostAsync(Guid userId, Guid postId)
    {
        var repo = _uow.Repository<CommunityPost>();
        var post = await repo.GetByIdAsync(postId);

        if (post == null) throw new Exception("Post not found.");
        if (post.UserId != userId) throw new UnauthorizedAccessException("You can only delete your own posts.");

        repo.Remove(post);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentDto request)
    {
        var repo = _uow.Repository<PostComment>();
        var comment = await repo.GetByIdAsync(commentId);

        if (comment == null) throw new Exception("Comment not found.");
        if (comment.UserId != userId) throw new UnauthorizedAccessException("You can only edit your own comments.");

        comment.Content = request.Content;

        repo.Update(comment);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid userId, Guid commentId)
    {
        var commentRepo = _uow.Repository<PostComment>();
        var postRepo = _uow.Repository<CommunityPost>();

        var comment = await commentRepo.GetByIdAsync(commentId);

        if (comment == null) throw new Exception("Comment not found.");
        if (comment.UserId != userId) throw new UnauthorizedAccessException("You can only delete your own comments.");

        var post = await postRepo.GetByIdAsync(comment.PostId);
        if (post != null)
        {
            post.CommentsCount--;
            postRepo.Update(post);
        }

        commentRepo.Remove(comment);
        await _uow.SaveChangesAsync();
    }

    public async Task<CommunityTabCountsDto> GetGameTabCountsAsync(string gameId, Guid? currentUserId = null) // <-- Добавили параметр
    {
        var counts = await _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => p.GameId == gameId)
            .GroupBy(p => p.PostType)
            .Select(g => new { PostType = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new CommunityTabCountsDto();

        foreach (var item in counts)
        {
            result.All += item.Count;

            switch (item.PostType)
            {
                case CommunityPostType.Discussion: result.Discussions = item.Count; break;
                case CommunityPostType.Screenshot: result.Screenshots = item.Count; break;
                case CommunityPostType.Video: result.Videos = item.Count; break;
                case CommunityPostType.Guide: result.Guides = item.Count; break;
                case CommunityPostType.News: result.News = item.Count; break;
            }
        }

        var subRepo = _uow.Repository<GameSubscription>().AsQueryable().Where(s => s.GameId == gameId);

        result.SubscribersCount = await subRepo.CountAsync();
        result.IsSubscribed = currentUserId.HasValue && await subRepo.AnyAsync(s => s.UserId == currentUserId.Value);

        return result;
    }

    public async Task ToggleSubscribeAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<GameSubscription>();
        var existingSub = await repo.AsQueryable()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GameId == gameId);

        if (existingSub == null)
        {
            await repo.AddAsync(new GameSubscription { UserId = userId, GameId = gameId });
        }
        else
        {
            repo.Remove(existingSub);
        }

        await _uow.SaveChangesAsync();
    }
}