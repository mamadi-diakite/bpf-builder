using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BpfBuilder.Infrastructure.Persistence;

namespace BpfBuilder.Infrastructure.Tests;

/// <summary>In-memory SQLite database that lives as long as its connection stays open.</summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public BpfBuilderDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<BpfBuilderDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
