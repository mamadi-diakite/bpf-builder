namespace BpfBuilder.Infrastructure.Seeding;

public sealed class DemoDataSeeder(BpfBuilderDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "TestDemo123!";
    public const string DemoEmail = "camille.durand@example.com";
    public const string DemoOrganizationName = "Horizon Skills Training";

    /// <summary>
    /// One fictional account with a fictional training organization (sections A and H filled)
    /// and its last two calendar fiscal years.
    /// </summary>
    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new User(DemoEmail, "Camille Durand", passwordHasher.Hash(DemoPassword), now);

        var organization = new Organization(user.Id, DemoOrganizationName, now);
        organization.UpdateIdentification(
            DemoOrganizationName,
            declarationNumber: "11999999999",
            siret: "99900000000011",
            nafCode: "85.59A",
            legalForm: "SAS",
            addressLine: "12 Example Street",
            postalCode: "75001",
            city: "Paris",
            publishAddress: true,
            phone: "+33 1 00 00 00 00",
            contactEmail: "contact@example.com",
            now);
        organization.UpdateSignatory(
            managerName: "Camille Durand",
            managerTitle: "Managing director",
            signatoryName: "Camille Durand",
            signatoryTitle: "Managing director",
            signaturePlace: "Paris",
            signatoryEmail: DemoEmail,
            signatoryPhone: "+33 1 00 00 00 00",
            now);

        db.Users.Add(user);
        db.Organizations.Add(organization);
        db.FiscalYears.AddRange(
            FiscalYear.CalendarYear(organization.Id, now.Year - 2, 412_500m, now),
            FiscalYear.CalendarYear(organization.Id, now.Year - 1, 468_900m, now));
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
