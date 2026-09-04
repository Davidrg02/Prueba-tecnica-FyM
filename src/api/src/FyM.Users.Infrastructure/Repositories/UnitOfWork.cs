using FyM.Users.Application.Abstractions;
using FyM.Users.Infrastructure.Persistence;

namespace FyM.Users.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
