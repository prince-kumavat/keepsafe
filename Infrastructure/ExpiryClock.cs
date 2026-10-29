using System;
using Microsoft.Extensions.Configuration;

namespace KeepSafe.Infrastructure;

public interface IExpiryClock
{
    DateOnly Today { get; }
    bool IsExpired(DateOnly expirationDate);
}

public class ExpiryClock : IExpiryClock
{
    private readonly int _utcOffsetMinutes;

    public ExpiryClock(IConfiguration configuration)
    {
        // APP_UTC_OFFSET_MINUTES environment variable or configuration, defaults to 0
        if (!int.TryParse(configuration["APP_UTC_OFFSET_MINUTES"], out _utcOffsetMinutes))
        {
            _utcOffsetMinutes = 0;
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(_utcOffsetMinutes));

    public bool IsExpired(DateOnly expirationDate) => expirationDate < Today;
}
