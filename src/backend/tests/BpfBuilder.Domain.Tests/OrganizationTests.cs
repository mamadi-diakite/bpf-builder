namespace BpfBuilder.Domain.Tests;

public sealed class OrganizationTests
{
    private const string ValidSiret = "99900000000011";
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTime Later = TestDates.Now.AddDays(1);

    [Fact]
    public void New_organization_has_a_name_and_an_empty_profile()
    {
        var organization = new Organization(OwnerId, " Northwind Training ", TestDates.Now);

        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.Equal(OwnerId, organization.OwnerId);
        Assert.Equal("Northwind Training", organization.Name);
        Assert.Null(organization.DeclarationNumber);
        Assert.Null(organization.Siret);
        Assert.False(organization.PublishAddress);
        Assert.Null(organization.SignatoryName);
        Assert.Equal(TestDates.Now, organization.CreatedAtUtc);
        Assert.Equal(TestDates.Now, organization.UpdatedAtUtc);
    }

    [Fact]
    public void Rejects_empty_owner_blank_name_and_local_date()
    {
        Assert.Equal("ownerId", Assert.Throws<ArgumentException>(
            () => new Organization(Guid.Empty, "Northwind", TestDates.Now)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(
            () => new Organization(OwnerId, " ", TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new Organization(OwnerId, "Northwind", DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local))).ParamName);
    }

    [Fact]
    public void Updates_the_identification_with_normalized_values()
    {
        var organization = NewOrganization();

        organization.UpdateIdentification(
            "Northwind Learning", "119 9999 9999", "999 000 000 00011", "85.59a", "SAS",
            "12 Example Street", "75001", "Paris", true, "+33 1 00 00 00 00", "Contact@Example.com", Later);

        Assert.Equal("Northwind Learning", organization.Name);
        Assert.Equal("11999999999", organization.DeclarationNumber);
        Assert.Equal(ValidSiret, organization.Siret);
        Assert.Equal("85.59A", organization.NafCode);
        Assert.Equal("SAS", organization.LegalForm);
        Assert.Equal("12 Example Street", organization.AddressLine);
        Assert.Equal("75001", organization.PostalCode);
        Assert.Equal("Paris", organization.City);
        Assert.True(organization.PublishAddress);
        Assert.Equal("+33 1 00 00 00 00", organization.Phone);
        Assert.Equal("contact@example.com", organization.ContactEmail);
        Assert.Equal(Later, organization.UpdatedAtUtc);
    }

    [Fact]
    public void Identification_fields_can_be_cleared()
    {
        var organization = NewOrganization();
        organization.UpdateIdentification(
            "Northwind", "11999999999", ValidSiret, "85.59A", "SAS", "Street", "75001", "Paris", true, "01", "a@example.com", Later);

        organization.UpdateIdentification("Northwind", null, " ", null, null, null, null, null, false, null, " ", Later);

        Assert.Null(organization.DeclarationNumber);
        Assert.Null(organization.Siret);
        Assert.Null(organization.NafCode);
        Assert.Null(organization.ContactEmail);
        Assert.False(organization.PublishAddress);
    }

    [Theory]
    [InlineData("1199999999")]
    [InlineData("119999999999")]
    [InlineData("1199999999A")]
    [InlineData("11999999999999999999")]
    public void Rejects_invalid_declaration_number(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => UpdateIdentification(NewOrganization(), declarationNumber: value));

        Assert.Equal("declarationNumber", error.ParamName);
    }

    [Theory]
    [InlineData("9990000000001")]
    [InlineData("99900000000012")]
    [InlineData("9990000000001A")]
    public void Rejects_invalid_siret(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => UpdateIdentification(NewOrganization(), siret: value));

        Assert.Equal("siret", error.ParamName);
    }

    [Theory]
    [InlineData(ValidSiret, true)]
    [InlineData("99900000000012", false)]
    [InlineData("9990000000001A", false)]
    [InlineData("999 000 000 00011", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Checks_a_siret(string? value, bool expected)
    {
        Assert.Equal(expected, Organization.IsValidSiret(value));
    }

    [Theory]
    [InlineData("85.59A", true)]
    [InlineData("85.59a", false)]
    [InlineData("8559A", false)]
    [InlineData(null, false)]
    public void Checks_a_naf_code(string? value, bool expected)
    {
        Assert.Equal(expected, Organization.IsValidNafCode(value));
    }

    [Theory]
    [InlineData("8559A")]
    [InlineData("85.5A")]
    [InlineData("85.591")]
    public void Rejects_invalid_naf_code(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => UpdateIdentification(NewOrganization(), nafCode: value));

        Assert.Equal("nafCode", error.ParamName);
    }

    [Fact]
    public void Rejects_invalid_contact_email()
    {
        var error = Assert.Throws<ArgumentException>(() => UpdateIdentification(NewOrganization(), contactEmail: "contact@"));

        Assert.Equal("contactEmail", error.ParamName);
    }

    [Fact]
    public void Failed_identification_update_changes_nothing()
    {
        var organization = NewOrganization();

        Assert.Throws<ArgumentException>(() => UpdateIdentification(organization, name: "Other", siret: "99900000000012"));

        Assert.Equal("Northwind Training", organization.Name);
        Assert.Equal(TestDates.Now, organization.UpdatedAtUtc);
    }

    [Fact]
    public void Identification_update_rejects_local_date()
    {
        var error = Assert.Throws<ArgumentException>(() => NewOrganization().UpdateIdentification(
            "Northwind", null, null, null, null, null, null, null, false, null, null,
            DateTime.SpecifyKind(Later, DateTimeKind.Local)));

        Assert.Equal("nowUtc", error.ParamName);
    }

    [Fact]
    public void Updates_the_signatory()
    {
        var organization = NewOrganization();

        organization.UpdateSignatory(
            " Alex Morgan ", "Director", "Sam Lee", "Training manager", "Lyon", "Sam.Lee@Example.com", "04 00 00 00 00", Later);

        Assert.Equal("Alex Morgan", organization.ManagerName);
        Assert.Equal("Director", organization.ManagerTitle);
        Assert.Equal("Sam Lee", organization.SignatoryName);
        Assert.Equal("Training manager", organization.SignatoryTitle);
        Assert.Equal("Lyon", organization.SignaturePlace);
        Assert.Equal("sam.lee@example.com", organization.SignatoryEmail);
        Assert.Equal("04 00 00 00 00", organization.SignatoryPhone);
        Assert.Equal(Later, organization.UpdatedAtUtc);
    }

    [Fact]
    public void Signatory_rejects_invalid_email_and_local_date()
    {
        var organization = NewOrganization();

        Assert.Equal("signatoryEmail", Assert.Throws<ArgumentException>(
            () => organization.UpdateSignatory(null, null, "Sam", null, null, "sam@", null, Later)).ParamName);
        Assert.Equal("nowUtc", Assert.Throws<ArgumentException>(
            () => organization.UpdateSignatory(null, null, null, null, null, null, null,
                DateTime.SpecifyKind(Later, DateTimeKind.Local))).ParamName);
        Assert.Null(organization.SignatoryName);
    }

    private static Organization NewOrganization() => new(OwnerId, "Northwind Training", TestDates.Now);

    private static void UpdateIdentification(
        Organization organization,
        string name = "Northwind",
        string? declarationNumber = null,
        string? siret = null,
        string? nafCode = null,
        string? contactEmail = null) =>
        organization.UpdateIdentification(
            name, declarationNumber, siret, nafCode, null, null, null, null, false, null, contactEmail, Later);
}
