using FyM.Users.Application.Common;

namespace FyM.Users.Infrastructure.Identity;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
