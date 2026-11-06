namespace BpfBuilder.Application.Templates;

/// <summary>Fictional activity of a small training organization for the 2025 calendar year.</summary>
public static class SampleActivity
{
    public static IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<object?>>> Rows { get; } =
        new Dictionary<string, IReadOnlyList<IReadOnlyList<object?>>>
        {
            [ActivityTemplate.Sessions.Name] =
            [
                ["S-2025-01", "Spreadsheet basics for office teams", Day(2, 3), Day(2, 5), 21m, "Direct", "None", "Other", null, "324"],
                ["S-2025-02", "Web developer certificate", Day(3, 3), Day(6, 27), 399m, "Direct", "Partial", "RNCP", "5", "326"],
                ["S-2025-03", "Workplace first aid", Day(4, 10), Day(4, 11), 14m, "Subcontracted out", "None", "RS", null, "344"],
                ["S-2025-04", "Customer service skills", Day(9, 15), Day(9, 19), 35m, "Subcontracted in", "Full", "Other", null, "312"],
                ["S-2025-05", "Career skills assessment", Day(10, 6), Day(11, 28), 24m, "Direct", "Partial", "Skills assessment", null, "415"],
            ],
            [ActivityTemplate.Learners.Name] =
            [
                ["Student 01", "S-2025-01", "Employee", 21m],
                ["Student 02", "S-2025-01", "Employee", 21m],
                ["Student 03", "S-2025-01", "Employee", 14m],
                ["Student 04", "S-2025-02", "Job seeker", 399m],
                ["Student 05", "S-2025-02", "Job seeker", 385.5m],
                ["Student 06", "S-2025-02", "Individual", 399m],
                ["Student 07", "S-2025-03", "Apprentice", 14m],
                ["Student 08", "S-2025-04", "Employee", 35m],
                ["Student 09", "S-2025-04", "Other", 28m],
                ["Student 10", "S-2025-05", "Employee", 24m],
            ],
            [ActivityTemplate.Trainers.Name] =
            [
                ["Trainer 01", "S-2025-01", "Internal", 21m],
                ["Trainer 01", "S-2025-02", "Internal", 210m],
                ["Trainer 02", "S-2025-02", "External", 189m],
                ["Trainer 03", "S-2025-03", "External", 14m],
                ["Trainer 02", "S-2025-04", "External", 35m],
                ["Trainer 04", "S-2025-05", "Internal", 24m],
            ],
            [ActivityTemplate.Revenue.Name] =
            [
                ["1", 4200m, Day(2, 28), "S-2025-01", "Invoice 2025-014"],
                ["7", 16800m, Day(7, 4), "S-2025-02", "Invoice 2025-031"],
                ["9", 6450m, Day(3, 3), "S-2025-02", "Invoice 2025-022"],
                ["2a", 980m, Day(4, 30), "S-2025-03", "Invoice 2025-027"],
                ["10", 3150m, Day(9, 30), "S-2025-04", "Invoice 2025-048"],
                ["2e", 1890m, Day(11, 28), "S-2025-05", "Invoice 2025-061"],
                ["11", 600m, Day(12, 15), null, "Course material sold"],
            ],
            [ActivityTemplate.Costs.Name] =
            [
                ["Trainer salaries", 18500m, "Internal trainer salaries"],
                ["Bought services", 9240m, "External trainer fees"],
                ["Other costs", 2310m, "Room hire and course material"],
            ],
        };

    private static DateOnly Day(int month, int day) => new(2025, month, day);
}
