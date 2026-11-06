namespace BpfBuilder.Application.Contracts.Organizations;

/// <summary>Section A of the BPF. Spaces in the declaration number and the SIRET are ignored.</summary>
public sealed record OrganizationIdentificationRequest(
    string Name,
    string? DeclarationNumber,
    string? Siret,
    string? NafCode,
    string? LegalForm,
    string? AddressLine,
    string? PostalCode,
    string? City,
    bool PublishAddress,
    string? Phone,
    string? ContactEmail);

/// <summary>Section H of the BPF.</summary>
public sealed record OrganizationSignatoryRequest(
    string? ManagerName,
    string? ManagerTitle,
    string? SignatoryName,
    string? SignatoryTitle,
    string? SignaturePlace,
    string? SignatoryEmail,
    string? SignatoryPhone);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string? DeclarationNumber,
    string? Siret,
    string? NafCode,
    string? LegalForm,
    string? AddressLine,
    string? PostalCode,
    string? City,
    bool PublishAddress,
    string? Phone,
    string? ContactEmail,
    string? ManagerName,
    string? ManagerTitle,
    string? SignatoryName,
    string? SignatoryTitle,
    string? SignaturePlace,
    string? SignatoryEmail,
    string? SignatoryPhone,
    DateTime UpdatedAtUtc);
