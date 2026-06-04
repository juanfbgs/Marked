using System.Security.Claims;
using Marked.Data;
using Marked.Domain;
using Marked.Features.Shared;
using Marked.Features.Uploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marked.Features.Bookmarks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BookmarksController : BaseController
{
    private readonly AppDbContext _context;

    private readonly IS3Service _s3;

    private readonly IConfiguration _config;


    public BookmarksController(AppDbContext context, IS3Service s3, IConfiguration config)
    {
        _context = context;
        _s3 = s3;
        _config = config;
    }

    private string BucketName => _config["S3:BucketName"]!;


    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookmarkResponse>>> GetBookmarks()
    {
        var userId = GetCurrentUserId();

        var bookmarks = await _context.Bookmarks
        .Where(b => b.UserId == userId)
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.Id,
            b.Title,
            b.Url,
            b.ImageUrl,
            b.CreatedAt,
        })
        .ToListAsync();

        var result = bookmarks.Select(b => new BookmarkResponse(
            b.Id,
            b.Title,
            b.Url,
            b.ImageUrl is not null
                ? Url.Action(nameof(GetBookmarkImage), new { id = b.Id })
                : null,
            b.CreatedAt
        ));

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookmarkResponse>> GetBookmark(Guid id)
    {
        var userId = GetCurrentUserId();
        var bookmark = await _context.Bookmarks
        .Where(b => b.Id == id && b.UserId == userId)
        .Select(b => new { b.Id, b.Title, b.Url, b.ImageUrl, b.CreatedAt })
        .FirstOrDefaultAsync();

        if (bookmark is null)
            return NotFound("Bookmark not found.");

        return Ok(new BookmarkResponse(
            bookmark.Id,
            bookmark.Title,
            bookmark.Url,
            bookmark.ImageUrl is not null
                ? Url.Action(nameof(GetBookmarkImage), new { id = bookmark.Id })
                : null,
            bookmark.CreatedAt
        ));
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<ActionResult<BookmarkResponse>> CreateBookmark([FromForm] CreateBookmarkRequest request)
    {
        var userId = GetCurrentUserId();

        string? imageUrl = null;

        if (request.Image is not null)
        {
            imageUrl = await _s3.UploadAsync(
                request.Image.OpenReadStream(),
                request.Image.FileName,
                request.Image.ContentType
            );
        }

        var bookmark = new Bookmark
        {
            Title = request.Title,
            Url = request.Url,
            ImageUrl = imageUrl,
            UserId = userId
        };


        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        var bookmarkResponse = new BookmarkResponse(
        bookmark.Id,
        bookmark.Title,
        bookmark.Url,
        bookmark.ImageUrl is not null
            ? Url.Action(nameof(GetBookmarkImage), new { id = bookmark.Id })
            : null,
        bookmark.CreatedAt
    );

        return CreatedAtAction(nameof(GetBookmark), new { id = bookmark.Id }, bookmarkResponse);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBookmark(Guid id, [FromForm] UpdateBookmarkRequest request)
    {
        var userId = GetCurrentUserId();
        var bookmark = await _context.Bookmarks
        .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (bookmark == null) return NotFound("Bookmark not found.");

        bookmark.Title = request.Title;
        bookmark.Url = request.Url;

        if (request.Image is not null)
        {
            if (bookmark.ImageUrl is not null)
                await _s3.DeleteAsync(bookmark.ImageUrl);

            bookmark.ImageUrl = await _s3.UploadAsync(
                request.Image.OpenReadStream(),
                request.Image.FileName,
                request.Image.ContentType
            );
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        var userId = GetCurrentUserId();
        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (bookmark is null)
            return NotFound("Bookmark not found.");

        if (bookmark.ImageUrl is not null)
            await _s3.DeleteAsync(bookmark.ImageUrl);

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetBookmarkImage(Guid id)
    {
        var userId = GetCurrentUserId();

        var imageUrl = await _context.Bookmarks
            .Where(b => b.Id == id && b.UserId == userId)
            .Select(b => b.ImageUrl)
            .FirstOrDefaultAsync();

        if (imageUrl is null)
            return NotFound();

        var imageData = await _s3.GetObjectAsync(BucketName, imageUrl);

        return File(imageData.ResponseStream, imageData.Headers.ContentType);
    }
}