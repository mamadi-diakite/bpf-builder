using BpfBuilder.Application.Contracts.Organizations;
using BpfBuilder.Application.Organizations;

namespace BpfBuilder.Application.Tests;

public sealed class OrganizationValidatorTests
{
    internal static readonly OrganizationIdentificationRequest Identification = new(
        "Northwind Training",
        "11 999 999 999",
        "999 000 000 00011",
        "85.59a",
        "SAS",
        "12 Example Street",
        "75001",
        "Paris",
        PublishAddress: true,
        "+33 1 00 00 00 00",
        "contact@example.com");

    internal static readonly OrganizationSignatoryRequest Signatory = new(
        "Alex Martin", "Managing director", "Alex Martin", "Managing director", "Paris", "alex.martin@example.com", "+33 1 00 00 00 01");

    [Fact]
    public void Accepts_a_complete_identification_with_spaces_and_lower_case()
    {
        Assert.Empty(OrganizationValidator.Validate(Identification));
    }

    [Fact]
    public void Accepts_an_identification_with_only_a_name()
    {
        var request = new OrganizationIdentificationRequest("Northwind", null, " ", null, null, null, null, null, false, null, "");

        Assert.Empty(OrganizationValidator.Validate(request));
    }

    [Theory]
    [InlineData(null, "Name is required.")]
    [InlineData("  ", "Name is required.")]
    [InlineData("long", "Use at most 200 characters.")]
    public void Checks_the_name(string? name, string message)
    {
        name = name == "long" ? new string('n', 201) : name;

        var errors = OrganizationValidator.Validate(Identification with { Name = name! });

        Assert.Equal([message], errors["name"]);
    }

    [Theory]
    [InlineData("1199999999")]
    [InlineData("1199999999A")]
    public void Checks_the_declaration_number(string value)
    {
        var errors = OrganizationValidator.Validate(Identification with { DeclarationNumber = value });

        Assert.Equal(["Enter the 11 digits of the declaration number."], errors["declarationNumber"]);
    }

    [Fact]
    public void Reports_every_identification_error()
    {
        var request = Identification with
        {
            Siret = "99900000000012",
            NafCode = "8559A",
            LegalForm = new string('l', 101),
            AddressLine = new string('a', 201),
            PostalCode = "75001-75002",
            City = new string('c', 101),
            Phone = new string('1', 31),
            ContactEmail = "contact.example.com",
        };

        var errors = OrganizationValidator.Validate(request);

        Assert.Equal(
            ["addressLine", "city", "contactEmail", "legalForm", "nafCode", "phone", "postalCode", "siret"],
            errors.Keys.Order());
        Assert.Equal(["Enter a valid 14-digit SIRET."], errors["siret"]);
        Assert.Equal(["Use the NAF code format, for example 85.59A."], errors["nafCode"]);
        Assert.Equal(["Enter a valid email address."], errors["contactEmail"]);
        Assert.Equal(["Use at most 10 characters."], errors["postalCode"]);
    }

    [Fact]
    public void Accepts_a_signatory_section_complete_or_empty()
    {
        Assert.Empty(OrganizationValidator.Validate(Signatory));
        Assert.Empty(OrganizationValidator.Validate(new OrganizationSignatoryRequest(null, null, null, null, null, null, null)));
    }

    [Fact]
    public void Reports_every_signatory_error()
    {
        var tooLong = new string('x', 101);
        var request = new OrganizationSignatoryRequest(tooLong, tooLong, tooLong, tooLong, tooLong, "nobody@", new string('1', 31));

        var errors = OrganizationValidator.Validate(request);

        Assert.Equal(
            ["managerName", "managerTitle", "signatoryEmail", "signatoryName", "signatoryPhone", "signatoryTitle", "signaturePlace"],
            errors.Keys.Order());
    }

    [Fact]
    public void Rejects_missing_requests()
    {
        Assert.Throws<ArgumentNullException>(() => OrganizationValidator.Validate((OrganizationIdentificationRequest)null!));
        Assert.Throws<ArgumentNullException>(() => OrganizationValidator.Validate((OrganizationSignatoryRequest)null!));
    }
}
