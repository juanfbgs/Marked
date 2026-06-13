using Marked.Data;
using Marked.Domain;
using Marked.Features.Extensions;
using Marked.Features.Uploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marked.Features.Bookmarks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BookmarksController(AppDbContext context, IS3Service s3, IConfiguration config) : ControllerBase
{
    private string BucketName => config["S3:BucketName"] ?? throw new InvalidOperationException("S3 Bucket Name is not configured.");

    private int PresignedUrlExpiryMinutes => config.GetValue("S3:PresignedUrlExpiryMinutes", 15);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookmarkResponse>>> GetBookmarks()
    {
        var userId = User.GetCurrentUserId();

        var bookmarks = await context.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Title, b.Url, b.ImageUrl, b.CreatedAt })
            .ToListAsync();

        // 3. Modern .NET parallel execution optimization
        var responseTasks = bookmarks.Select(async b => new BookmarkResponse(
            b.Id,
            b.Title,
            b.Url,
            b.ImageUrl is not null
                ? await s3.GeneratePresignedUrlAsync(BucketName, b.ImageUrl, PresignedUrlExpiryMinutes)
                : null,
            b.CreatedAt
        ));

        var result = await Task.WhenAll(responseTasks);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookmarkResponse>> GetBookmark(Guid id)
    {
        var userId = User.GetCurrentUserId();

        var bookmark = await context.Bookmarks
            .AsNoTracking()
            .Where(b => b.Id == id && b.UserId == userId)
            .Select(b => new { b.Id, b.Title, b.Url, b.ImageUrl, b.CreatedAt })
            .FirstOrDefaultAsync();

        if (bookmark is null)
            return Problem(detail: "Bookmark not found.", statusCode: StatusCodes.Status404NotFound);

        string? presignedUrl = bookmark.ImageUrl is not null
            ? await s3.GeneratePresignedUrlAsync(BucketName, bookmark.ImageUrl, PresignedUrlExpiryMinutes)
            : null;

        return Ok(new BookmarkResponse(bookmark.Id, bookmark.Title, bookmark.Url, presignedUrl, bookmark.CreatedAt));
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<CreatedAtActionResult> CreateBookmark([FromForm] CreateBookmarkRequest request)
    {
        var userId = User.GetCurrentUserId();
        string? imageUrl = null;

        if (request.Image is not null)
        {
            imageUrl = await s3.UploadAsync(
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

        context.Bookmarks.Add(bookmark);
        await context.SaveChangesAsync();

        string? presignedUrl = bookmark.ImageUrl is not null
            ? await s3.GeneratePresignedUrlAsync(BucketName, bookmark.ImageUrl, PresignedUrlExpiryMinutes)
            : null;

        var response = new BookmarkResponse(bookmark.Id, bookmark.Title, bookmark.Url, presignedUrl, bookmark.CreatedAt);
        return CreatedAtAction(nameof(GetBookmark), new { id = bookmark.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBookmark(Guid id, [FromForm] UpdateBookmarkRequest request)
    {
        var userId = User.GetCurrentUserId();

        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (bookmark is null)
            return Problem(detail: "Bookmark not found.", statusCode: StatusCodes.Status404NotFound);

        bookmark.Title = request.Title;
        bookmark.Url = request.Url;

        if (request.Image is not null)
        {
            if (bookmark.ImageUrl is not null)
                await s3.DeleteAsync(bookmark.ImageUrl);

            bookmark.ImageUrl = await s3.UploadAsync(
                request.Image.OpenReadStream(),
                request.Image.FileName,
                request.Image.ContentType
            );
        }

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        var userId = User.GetCurrentUserId();

        var bookmark = await context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (bookmark is null)
            return Problem(detail: "Bookmark not found.", statusCode: StatusCodes.Status404NotFound);

        if (bookmark.ImageUrl is not null)
            await s3.DeleteAsync(bookmark.ImageUrl);

        context.Bookmarks.Remove(bookmark);
        await context.SaveChangesAsync();

        return NoContent();
    }
}