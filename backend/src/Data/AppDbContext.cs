using Microsoft.EntityFrameworkCore;

namespace Marked.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}