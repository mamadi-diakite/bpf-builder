using BpfBuilder.Application.Contracts.Organizations;
using BpfBuilder.Domain;

namespace BpfBuilder.Application.Organizations;

/// <summary>Checks every field at once, with messages meant for the form.</summary>
public static class OrganizationValidator
{
    public static Dictionary<string, string[]> Validate(OrganizationIdentificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }
        else
        {
            MaxLength(errors, "name", request.Name, Organization.NameMaxLength);
        }

        var declarationNumber = WithoutSpaces(request.DeclarationNumber);
        if (declarationNumber is not null
            && (declarationNumber.Length != Organization.DeclarationNumberLength || !declarationNumber.All(char.IsAsciiDigit)))
        {
            errors["declarationNumber"] = [$"Enter the {Organization.DeclarationNumberLength} digits of the declaration number."];
        }

        var siret = WithoutSpaces(request.Siret);
        if (siret is not null && !Organization.IsValidSiret(siret))
        {
            errors["siret"] = ["Enter a valid 14-digit SIRET."];
        }

        if (!string.IsNullOrWhiteSpace(request.NafCode) && !Organization.IsValidNafCode(request.NafCode.Trim().ToUpperInvariant()))
        {
            errors["nafCode"] = ["Use the NAF code format, for example 85.59A."];
        }

        MaxLength(errors, "legalForm", request.LegalForm, Organization.TextMaxLength);
        MaxLength(errors, "addressLine", request.AddressLine, Organization.AddressLineMaxLength);
        MaxLength(errors, "postalCode", request.PostalCode, Organization.PostalCodeMaxLength);
        MaxLength(errors, "city", request.City, Organization.TextMaxLength);
        MaxLength(errors, "phone", request.Phone, Organization.PhoneMaxLength);
        Email(errors, "contactEmail", request.ContactEmail);

        return errors;
    }

    public static Dictionary<string, string[]> Validate(OrganizationSignatoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        MaxLength(errors, "managerName", request.ManagerName, Organization.TextMaxLength);
        MaxLength(errors, "managerTitle", request.ManagerTitle, Organization.TextMaxLength);
        MaxLength(errors, "signatoryName", request.SignatoryName, Organization.TextMaxLength);
        MaxLength(errors, "signatoryTitle", request.SignatoryTitle, Organization.TextMaxLength);
        MaxLength(errors, "signaturePlace", request.SignaturePlace, Organization.TextMaxLength);
        Email(errors, "signatoryEmail", request.SignatoryEmail);
        MaxLength(errors, "signatoryPhone", request.SignatoryPhone, Organization.PhoneMaxLength);

        return errors;
    }

    private static string? WithoutSpaces(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(" ", string.Empty).Trim();

    private static void MaxLength(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            errors[field] = [$"Use at most {maxLength} characters."];
        }
    }

    private static void Email(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !User.IsValidEmail(value))
        {
            errors[field] = ["Enter a valid email address."];
        }
    }
}
