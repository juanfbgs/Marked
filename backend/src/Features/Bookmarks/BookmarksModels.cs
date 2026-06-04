namespace Marked.Features.Bookmarks;

public record CreateBookmarkRequest(
    string Title,
    string Url,
    IFormFile? Image
);

public record UpdateBookmarkRequest(
    string Title,
    string Url,
    IFormFile? Image
);

public record BookmarkResponse(
    Guid Id,
    string Title,
    string Url,
    string? ImageUrl,
    DateTime CreatedAt
);