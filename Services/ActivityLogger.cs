using Core.Data;
using Entities.Entities;

namespace KPGJA.Web.Services;

public class ActivityLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ActivityLogger(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string entityType, int entityId, string? details = null)
    {
        // Get the currently logged-in user's email
        var user = _http.HttpContext?.User?.Identity?.Name ?? "Anonymous";

        _db.ActivityLogEntries.Add(new ActivityLogEntry
        {
            UserEmail = user,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}