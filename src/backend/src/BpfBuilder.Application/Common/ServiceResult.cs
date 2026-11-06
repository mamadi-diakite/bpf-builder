namespace BpfBuilder.Application.Common;

public enum ServiceResultStatus
{
    Ok = 0,
    NotFound = 1,
    Invalid = 2,
}

/// <param name="Errors">Messages by request field, in camelCase, when the status is <see cref="ServiceResultStatus.Invalid"/>.</param>
public sealed record ServiceResult<T>(ServiceResultStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    private static readonly Dictionary<string, string[]> NoErrors = [];

    public static ServiceResult<T> NotFound { get; } = new(ServiceResultStatus.NotFound, default, NoErrors);

    public static ServiceResult<T> Ok(T value) => new(ServiceResultStatus.Ok, value, NoErrors);

    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(ServiceResultStatus.Invalid, default, errors);
}
