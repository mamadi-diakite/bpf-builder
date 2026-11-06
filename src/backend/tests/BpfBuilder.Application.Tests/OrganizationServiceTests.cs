using BpfBuilder.Application.Common;
using BpfBuilder.Application.Organizations;

namespace BpfBuilder.Application.Tests;

public sealed class OrganizationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly InMemoryOrganizationRepository _organizations = new();
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _service = new OrganizationService(_organizations, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task First_identification_creates_the_organization()
    {
        Assert.Null(await _service.GetAsync(OwnerId));

        var result = await _service.SaveIdentificationAsync(OwnerId, OrganizationValidatorTests.Identification);

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        var organization = Assert.Single(_organizations.Organizations);
        Assert.Equal(OwnerId, organization.OwnerId);
        Assert.Equal("11999999999", result.Value?.DeclarationNumber);
        Assert.Equal("99900000000011", result.Value?.Siret);
        Assert.Equal("85.59A", result.Value?.NafCode);
        Assert.True(result.Value?.PublishAddress);
        Assert.Equal(Now.UtcDateTime, result.Value?.UpdatedAtUtc);
        Assert.Equal(result.Value, await _service.GetAsync(OwnerId));
        Assert.Equal(1, _organizations.SaveCount);
    }

    [Fact]
    public async Task Next_identification_updates_the_same_organization()
    {
        await _service.SaveIdentificationAsync(OwnerId, OrganizationValidatorTests.Identification);

        var result = await _service.SaveIdentificationAsync(
            OwnerId, OrganizationValidatorTests.Identification with { Name = "Northwind Academy", Siret = null });

        Assert.Equal("Northwind Academy", Assert.Single(_organizations.Organizations).Name);
        Assert.Null(result.Value?.Siret);
    }

    [Fact]
    public async Task Invalid_identification_creates_nothing()
    {
        var result = await _service.SaveIdentificationAsync(OwnerId, OrganizationValidatorTests.Identification with { Name = "" });

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Equal(["name"], result.Errors.Keys);
        Assert.Empty(_organizations.Organizations);
        Assert.Equal(0, _organizations.SaveCount);
    }

    [Fact]
    public async Task Signatory_needs_an_organization_first()
    {
        var result = await _service.SaveSignatoryAsync(OwnerId, OrganizationValidatorTests.Signatory);

        Assert.Equal(ServiceResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Saves_the_signatory()
    {
        await _service.SaveIdentificationAsync(OwnerId, OrganizationValidatorTests.Identification);

        var result = await _service.SaveSignatoryAsync(OwnerId, OrganizationValidatorTests.Signatory);

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        Assert.Equal("Alex Martin", result.Value?.SignatoryName);
        Assert.Equal("alex.martin@example.com", result.Value?.SignatoryEmail);
        Assert.Equal("Northwind Training", result.Value?.Name);
        Assert.Equal(2, _organizations.SaveCount);
    }

    [Fact]
    public async Task Invalid_signatory_changes_nothing()
    {
        await _service.SaveIdentificationAsync(OwnerId, OrganizationValidatorTests.Identification);

        var result = await _service.SaveSignatoryAsync(
            OwnerId, OrganizationValidatorTests.Signatory with { SignatoryEmail = "not-an-email" });

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Null(Assert.Single(_organizations.Organizations).SignatoryName);
        Assert.Equal(1, _organizations.SaveCount);
    }
}
