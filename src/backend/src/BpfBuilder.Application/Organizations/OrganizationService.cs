using BpfBuilder.Application.Common;
using BpfBuilder.Application.Contracts.Organizations;
using BpfBuilder.Domain;

namespace BpfBuilder.Application.Organizations;

/// <summary>Each account owns at most one organization, created when section A is first saved.</summary>
public sealed class OrganizationService(IOrganizationRepository organizations, TimeProvider timeProvider)
{
    public async Task<OrganizationResponse?> GetAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);

        return organization is null ? null : ToResponse(organization);
    }

    public async Task<ServiceResult<OrganizationResponse>> SaveIdentificationAsync(
        Guid ownerId,
        OrganizationIdentificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = OrganizationValidator.Validate(request);
        if (errors.Count > 0)
        {
            return ServiceResult<OrganizationResponse>.Invalid(errors);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);
        if (organization is null)
        {
            organization = new Organization(ownerId, request.Name, now);
            organizations.Add(organization);
        }

        organization.UpdateIdentification(
            request.Name,
            request.DeclarationNumber,
            request.Siret,
            request.NafCode,
            request.LegalForm,
            request.AddressLine,
            request.PostalCode,
            request.City,
            request.PublishAddress,
            request.Phone,
            request.ContactEmail,
            now);
        await organizations.SaveChangesAsync(cancellationToken);

        return ServiceResult<OrganizationResponse>.Ok(ToResponse(organization));
    }

    /// <returns>Not found until section A has been saved.</returns>
    public async Task<ServiceResult<OrganizationResponse>> SaveSignatoryAsync(
        Guid ownerId,
        OrganizationSignatoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = OrganizationValidator.Validate(request);
        var organization = await organizations.FindByOwnerAsync(ownerId, cancellationToken);
        if (organization is null)
        {
            return ServiceResult<OrganizationResponse>.NotFound;
        }

        if (errors.Count > 0)
        {
            return ServiceResult<OrganizationResponse>.Invalid(errors);
        }

        organization.UpdateSignatory(
            request.ManagerName,
            request.ManagerTitle,
            request.SignatoryName,
            request.SignatoryTitle,
            request.SignaturePlace,
            request.SignatoryEmail,
            request.SignatoryPhone,
            timeProvider.GetUtcNow().UtcDateTime);
        await organizations.SaveChangesAsync(cancellationToken);

        return ServiceResult<OrganizationResponse>.Ok(ToResponse(organization));
    }

    private static OrganizationResponse ToResponse(Organization o) => new(
        o.Id,
        o.Name,
        o.DeclarationNumber,
        o.Siret,
        o.NafCode,
        o.LegalForm,
        o.AddressLine,
        o.PostalCode,
        o.City,
        o.PublishAddress,
        o.Phone,
        o.ContactEmail,
        o.ManagerName,
        o.ManagerTitle,
        o.SignatoryName,
        o.SignatoryTitle,
        o.SignaturePlace,
        o.SignatoryEmail,
        o.SignatoryPhone,
        o.UpdatedAtUtc);
}
