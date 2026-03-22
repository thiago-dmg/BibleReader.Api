using BibleReader.Api.Data;
using BibleReader.Api.Extensions;
using BibleReader.Api.Models;
using BibleReader.Api.ViewModels;
using BibleReader.Api.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibleReader.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/community")]
public class CommunityController : ControllerBase
{
    [HttpGet("feed")]
    public async Task<IActionResult> Feed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromServices] AppDbContext db = null!)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = db.CommunityPosts
            .AsNoTracking()
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt);

        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Content,
                p.CreatedAt,
                author = new { p.User.DisplayName, p.User.Id },
                likes = p.Likes.Count,
                comments = p.Comments.Count
            })
            .ToListAsync();

        return Ok(new ResultViewModel<object>(new { total, page, pageSize, items }));
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostViewModel model, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var post = new CommunityPost
        {
            UserId = userId.Value,
            Content = model.Content.Trim()
        };
        db.CommunityPosts.Add(post);
        await db.SaveChangesAsync();

        return Ok(new ResultViewModel<object>(new { post.Id }));
    }

    [HttpGet("posts/{id:guid}")]
    public async Task<IActionResult> GetPost(Guid id, [FromServices] AppDbContext db)
    {
        var post = await db.CommunityPosts
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound(new ResultViewModel<string>("Post não encontrado"));

        var likes = await db.CommunityPostLikes.CountAsync(l => l.PostId == id);
        var comments = await db.CommunityComments.CountAsync(c => c.PostId == id);

        return Ok(new ResultViewModel<object>(new
        {
            post.Id,
            post.Content,
            post.CreatedAt,
            author = new { post.User.DisplayName, post.User.Id },
            likes,
            comments
        }));
    }

    [HttpPost("posts/{postId:guid}/like")]
    public async Task<IActionResult> Like(Guid postId, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!await db.CommunityPosts.AnyAsync(p => p.Id == postId))
            return NotFound(new ResultViewModel<string>("Post não encontrado"));

        if (await db.CommunityPostLikes.AnyAsync(l => l.PostId == postId && l.UserId == userId.Value))
            return Ok(new ResultViewModel<string>("Já curtido"));

        db.CommunityPostLikes.Add(new CommunityPostLike { PostId = postId, UserId = userId.Value });
        await db.SaveChangesAsync();
        return Ok(new ResultViewModel<string>("Curtido"));
    }

    [HttpDelete("posts/{postId:guid}/like")]
    public async Task<IActionResult> Unlike(Guid postId, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var like = await db.CommunityPostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId.Value);
        if (like == null)
            return Ok(new ResultViewModel<string>("Sem curtida"));

        db.CommunityPostLikes.Remove(like);
        await db.SaveChangesAsync();
        return Ok(new ResultViewModel<string>("Curtida removida"));
    }

    [HttpGet("posts/{postId:guid}/comments")]
    public async Task<IActionResult> Comments(Guid postId, [FromServices] AppDbContext db)
    {
        var list = await db.CommunityComments
            .AsNoTracking()
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                author = new { c.User.DisplayName, c.User.Id }
            })
            .ToListAsync();

        return Ok(new ResultViewModel<object>(list));
    }

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentViewModel model, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!await db.CommunityPosts.AnyAsync(p => p.Id == postId))
            return NotFound(new ResultViewModel<string>("Post não encontrado"));

        var c = new CommunityComment
        {
            PostId = postId,
            UserId = userId.Value,
            Content = model.Content.Trim()
        };
        db.CommunityComments.Add(c);
        await db.SaveChangesAsync();

        return Ok(new ResultViewModel<object>(new { c.Id }));
    }

    [HttpPost("posts/{postId:guid}/save")]
    public async Task<IActionResult> SavePost(Guid postId, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!await db.CommunityPosts.AnyAsync(p => p.Id == postId))
            return NotFound(new ResultViewModel<string>("Post não encontrado"));

        if (await db.CommunitySavedPosts.AnyAsync(s => s.PostId == postId && s.UserId == userId.Value))
            return Ok(new ResultViewModel<string>("Já salvo"));

        db.CommunitySavedPosts.Add(new CommunitySavedPost { PostId = postId, UserId = userId.Value });
        await db.SaveChangesAsync();
        return Ok(new ResultViewModel<string>("Salvo"));
    }

    [HttpDelete("posts/{postId:guid}/save")]
    public async Task<IActionResult> UnsavePost(Guid postId, [FromServices] AppDbContext db)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var s = await db.CommunitySavedPosts.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId.Value);
        if (s == null)
            return Ok(new ResultViewModel<string>("Não estava salvo"));

        db.CommunitySavedPosts.Remove(s);
        await db.SaveChangesAsync();
        return Ok(new ResultViewModel<string>("Removido dos salvos"));
    }
}
