namespace BpfBuilder.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file.</summary>
public class BpfBuilderApiFactory : WebApplicationFactory<Program>
{
    public static readonly string TestSigningKey =
        Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), "bpf-builder-tests", $"{Guid.NewGuid():N}.db");

    protected virtual string EnvironmentName => "Development";

    protected virtual bool SeedDemoData => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData.ToString());
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
    }

    /// <summary>A valid token that names no user.</summary>
    public string TokenWithoutSubject()
    {
        var settings = Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}

public sealed class DemoDataApiFactory : BpfBuilderApiFactory
{
    protected override bool SeedDemoData => true;
}

public sealed class ProductionDemoDataApiFactory : BpfBuilderApiFactory
{
    protected override string EnvironmentName => "Production";

    protected override bool SeedDemoData => true;
}
