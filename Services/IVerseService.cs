using Prayer.Models;

namespace Prayer.Services;

public interface IVerseService
{
    Task<DailyVerse> GetTodayVerseAsync(DateTime date, CancellationToken cancellationToken = default);
}
