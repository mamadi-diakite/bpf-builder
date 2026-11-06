namespace BpfBuilder.Domain.Tests;

public sealed class ReferenceListTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_a_blank_name(string name)
    {
        Assert.Throws<ArgumentException>(() => new ReferenceList(name, [new("A", "Alpha")]));
    }

    [Fact]
    public void Rejects_an_empty_list()
    {
        var error = Assert.Throws<ArgumentException>(() => new ReferenceList("Letters", []));

        Assert.Equal("items", error.ParamName);
    }

    [Fact]
    public void Rejects_codes_that_differ_only_by_case()
    {
        var error = Assert.Throws<ArgumentException>(() => new ReferenceList("Letters", [new("a", "Alpha"), new("A", "Again")]));

        Assert.Equal("items", error.ParamName);
    }

    [Theory]
    [InlineData("2a")]
    [InlineData("2A")]
    [InlineData(" 2a ")]
    public void Finds_a_code_ignoring_case_and_spaces(string code)
    {
        Assert.Equal("2a", ReferenceLists.FormLines.Find(code)?.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2z")]
    public void Returns_null_for_an_unknown_code(string? code)
    {
        Assert.Null(ReferenceLists.FormLines.Find(code));
    }

    [Fact]
    public void Lists_every_reference_list_once_with_distinct_names()
    {
        Assert.Equal(9, ReferenceLists.All.Count);
        Assert.Equal(ReferenceLists.All.Count, ReferenceLists.All.Select(list => list.Name).Distinct().Count());
        Assert.All(ReferenceLists.All, list => Assert.All(list.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Label))));
    }

    [Fact]
    public void Form_lines_follow_the_revenue_section_of_the_form()
    {
        Assert.Equal(
            ["1", "2a", "2b", "2c", "2d", "2e", "2f", "2g", "2h", "3", "4", "5", "6", "7", "8", "9", "10", "11"],
            ReferenceLists.FormLines.Items.Select(item => item.Code));
    }

    [Fact]
    public void Nsf_codes_are_three_digits()
    {
        Assert.All(ReferenceLists.NsfCodes.Items, item => Assert.Matches("^[1-4][0-9]{2}$", item.Code));
        Assert.NotNull(ReferenceLists.TrainingGoals.Find(ReferenceLists.RncpGoal));
    }
}
