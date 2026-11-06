using System.Text.RegularExpressions;

namespace BpfBuilder.Domain;

/// <summary>The training organization. Profile fields are optional until the BPF is prepared.</summary>
public sealed partial class Organization
{
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 100;
    public const int AddressLineMaxLength = 200;
    public const int PostalCodeMaxLength = 10;
    public const int PhoneMaxLength = 30;
    public const int EmailMaxLength = 254;
    public const int DeclarationNumberLength = 11;
    public const int SiretLength = 14;

    private Organization()
    {
    }

    public Organization(Guid ownerId, string name, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OwnerId = Guard.NotEmpty(ownerId, nameof(ownerId));
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public User? Owner { get; private set; }

    // Section A: identification.

    public string Name { get; private set; } = string.Empty;

    /// <summary>Activity declaration number given by the State, 11 digits.</summary>
    public string? DeclarationNumber { get; private set; }

    public string? Siret { get; private set; }

    public string? NafCode { get; private set; }

    public string? LegalForm { get; private set; }

    public string? AddressLine { get; private set; }

    public string? PostalCode { get; private set; }

    public string? City { get; private set; }

    public bool PublishAddress { get; private set; }

    public string? Phone { get; private set; }

    public string? ContactEmail { get; private set; }

    // Section H: manager and signatory.

    public string? ManagerName { get; private set; }

    public string? ManagerTitle { get; private set; }

    public string? SignatoryName { get; private set; }

    public string? SignatoryTitle { get; private set; }

    public string? SignaturePlace { get; private set; }

    public string? SignatoryEmail { get; private set; }

    public string? SignatoryPhone { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateIdentification(
        string name,
        string? declarationNumber,
        string? siret,
        string? nafCode,
        string? legalForm,
        string? addressLine,
        string? postalCode,
        string? city,
        bool publishAddress,
        string? phone,
        string? contactEmail,
        DateTime nowUtc)
    {
        var updatedAt = Guard.Utc(nowUtc, nameof(nowUtc));
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        var newDeclarationNumber = Digits(declarationNumber, DeclarationNumberLength, nameof(declarationNumber));
        var newSiret = Digits(siret, SiretLength, nameof(siret));
        var newNafCode = Guard.Optional(nafCode, nameof(nafCode), TextMaxLength)?.ToUpperInvariant();
        var newLegalForm = Guard.Optional(legalForm, nameof(legalForm), TextMaxLength);
        var newAddressLine = Guard.Optional(addressLine, nameof(addressLine), AddressLineMaxLength);
        var newPostalCode = Guard.Optional(postalCode, nameof(postalCode), PostalCodeMaxLength);
        var newCity = Guard.Optional(city, nameof(city), TextMaxLength);
        var newPhone = Guard.Optional(phone, nameof(phone), PhoneMaxLength);
        var newContactEmail = OptionalEmail(contactEmail, nameof(contactEmail));

        if (newSiret is not null && !PassesLuhnCheck(newSiret))
        {
            throw new ArgumentException("SIRET check digit is not valid.", nameof(siret));
        }

        if (newNafCode is not null && !NafCodePattern().IsMatch(newNafCode))
        {
            throw new ArgumentException("NAF code must look like 85.59A.", nameof(nafCode));
        }

        Name = newName;
        DeclarationNumber = newDeclarationNumber;
        Siret = newSiret;
        NafCode = newNafCode;
        LegalForm = newLegalForm;
        AddressLine = newAddressLine;
        PostalCode = newPostalCode;
        City = newCity;
        PublishAddress = publishAddress;
        Phone = newPhone;
        ContactEmail = newContactEmail;
        UpdatedAtUtc = updatedAt;
    }

    public void UpdateSignatory(
        string? managerName,
        string? managerTitle,
        string? signatoryName,
        string? signatoryTitle,
        string? signaturePlace,
        string? signatoryEmail,
        string? signatoryPhone,
        DateTime nowUtc)
    {
        var updatedAt = Guard.Utc(nowUtc, nameof(nowUtc));
        var newManagerName = Guard.Optional(managerName, nameof(managerName), TextMaxLength);
        var newManagerTitle = Guard.Optional(managerTitle, nameof(managerTitle), TextMaxLength);
        var newSignatoryName = Guard.Optional(signatoryName, nameof(signatoryName), TextMaxLength);
        var newSignatoryTitle = Guard.Optional(signatoryTitle, nameof(signatoryTitle), TextMaxLength);
        var newSignaturePlace = Guard.Optional(signaturePlace, nameof(signaturePlace), TextMaxLength);
        var newSignatoryEmail = OptionalEmail(signatoryEmail, nameof(signatoryEmail));
        var newSignatoryPhone = Guard.Optional(signatoryPhone, nameof(signatoryPhone), PhoneMaxLength);

        ManagerName = newManagerName;
        ManagerTitle = newManagerTitle;
        SignatoryName = newSignatoryName;
        SignatoryTitle = newSignatoryTitle;
        SignaturePlace = newSignaturePlace;
        SignatoryEmail = newSignatoryEmail;
        SignatoryPhone = newSignatoryPhone;
        UpdatedAtUtc = updatedAt;
    }

    private static string? Digits(string? value, int length, string paramName)
    {
        var digits = Guard.Optional(value?.Replace(" ", string.Empty), paramName, length + 1);
        if (digits is null)
        {
            return null;
        }

        return digits.Length == length && digits.All(char.IsAsciiDigit)
            ? digits
            : throw new ArgumentException($"Value must have exactly {length} digits.", paramName);
    }

    private static string? OptionalEmail(string? value, string paramName) =>
        string.IsNullOrWhiteSpace(value) ? null : Guard.Email(value, paramName, EmailMaxLength);

    private static bool PassesLuhnCheck(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[digits.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }

    [GeneratedRegex("^[0-9]{2}\\.[0-9]{2}[A-Z]$")]
    private static partial Regex NafCodePattern();
}
