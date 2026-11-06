namespace BpfBuilder.Domain.Activity;

/// <summary>One allowed value of a coded column, with what it means.</summary>
public sealed record ReferenceItem(string Code, string Label);

/// <summary>The allowed values of a coded column of the Excel template.</summary>
public sealed class ReferenceList
{
    public ReferenceList(string name, IReadOnlyList<ReferenceItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (items.Count == 0)
        {
            throw new ArgumentException("A reference list needs at least one item.", nameof(items));
        }

        if (items.DistinctBy(item => item.Code, StringComparer.OrdinalIgnoreCase).Count() != items.Count)
        {
            throw new ArgumentException("Codes must be unique.", nameof(items));
        }

        Name = name;
        Items = items;
    }

    public string Name { get; }

    public IReadOnlyList<ReferenceItem> Items { get; }

    /// <summary>Finds an item by code, ignoring case and surrounding spaces.</summary>
    public ReferenceItem? Find(string? code)
    {
        var trimmed = code?.Trim();

        return Items.FirstOrDefault(item => string.Equals(item.Code, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
