namespace BpfBuilder.Domain.Activity;

/// <summary>Reference lists used by the Excel template, based on form version 10443*17.</summary>
public static class ReferenceLists
{
    public const string RncpGoal = "RNCP";

    public static ReferenceList FormLines { get; } = new("Form lines", [
        new("1", "Companies, for training their own employees"),
        new("2a", "Skills operators: apprenticeship contracts"),
        new("2b", "Skills operators: professionalisation contracts"),
        new("2c", "Skills operators: work-study promotion or retraining (Pro-A)"),
        new("2d", "Career transition leave"),
        new("2e", "Personal training account (CPF)"),
        new("2f", "Specific schemes for job seekers"),
        new("2g", "Specific schemes for self-employed workers"),
        new("2h", "Skills development plan and other schemes"),
        new("3", "Public employers, for training their own staff"),
        new("4", "European bodies"),
        new("5", "The State"),
        new("6", "Regional councils"),
        new("7", "France Travail"),
        new("8", "Other public funding"),
        new("9", "Individuals paying for their own training"),
        new("10", "Other training organizations"),
        new("11", "Other vocational training income"),
    ]);

    public static ReferenceList DeliveryModes { get; } = new("Delivery modes", [
        new("Direct", "Delivered by the organization itself"),
        new("Subcontracted out", "Given to another organization to deliver"),
        new("Subcontracted in", "Delivered for another organization that sold the training"),
    ]);

    public static ReferenceList DistanceLearning { get; } = new("Distance learning", [
        new("None", "All hours on site"),
        new("Partial", "Part of the hours at a distance"),
        new("Full", "All hours at a distance"),
    ]);

    public static ReferenceList TrainingGoals { get; } = new("Training goals", [
        new(RncpGoal, "Certification registered in the RNCP"),
        new("RS", "Certification registered in the specific directory (RS)"),
        new("CQP", "Professional qualification certificate not registered in the RNCP"),
        new("Other", "Other vocational training"),
        new("Skills assessment", "Skills assessment (bilan de compétences)"),
        new("VAE", "Support for the validation of prior experience"),
    ]);

    public static ReferenceList RncpLevels { get; } = new("RNCP levels", [
        new("1", "Level 1: basic knowledge"),
        new("2", "Level 2: simple work tasks"),
        new("3", "Level 3: CAP, BEP"),
        new("4", "Level 4: baccalaureate"),
        new("5", "Level 5: two years after the baccalaureate"),
        new("6", "Level 6: bachelor's degree"),
        new("7", "Level 7: master's degree"),
        new("8", "Level 8: doctorate"),
    ]);

    public static ReferenceList LearnerCategories { get; } = new("Learner categories", [
        new("Employee", "Private-sector employee"),
        new("Apprentice", "Apprentice"),
        new("Job seeker", "Job seeker"),
        new("Individual", "Individual paying for their own training"),
        new("Other", "Other learner"),
    ]);

    public static ReferenceList TrainerTypes { get; } = new("Trainer types", [
        new("Internal", "Trainer employed by the organization"),
        new("External", "External trainer or subcontractor"),
    ]);

    public static ReferenceList CostCategories { get; } = new("Cost categories", [
        new("Trainer salaries", "Salaries of the trainers"),
        new("Bought services", "Training services bought and trainer fees"),
        new("Other costs", "Other training costs"),
    ]);

    public static ReferenceList NsfCodes { get; } = new("NSF codes", [
        new("100", "General education"),
        new("110", "Multidisciplinary sciences"),
        new("111", "Physics and chemistry"),
        new("112", "Chemistry and biology, biochemistry"),
        new("113", "Natural sciences"),
        new("114", "Mathematics"),
        new("115", "Physics"),
        new("116", "Chemistry"),
        new("117", "Earth sciences"),
        new("118", "Life sciences"),
        new("120", "Multidisciplinary humanities and law"),
        new("121", "Geography"),
        new("122", "Economics"),
        new("123", "Social sciences"),
        new("124", "Psychology"),
        new("125", "Linguistics"),
        new("126", "History"),
        new("127", "Philosophy, ethics and theology"),
        new("128", "Law, political science"),
        new("130", "Multidisciplinary literature and arts"),
        new("131", "French, literature and civilization"),
        new("132", "Fine arts"),
        new("133", "Music, performing arts"),
        new("134", "Other artistic disciplines"),
        new("135", "Classical languages and civilizations"),
        new("136", "Modern languages and civilizations"),
        new("200", "Core industrial technologies"),
        new("201", "Industrial process control technologies"),
        new("210", "Multidisciplinary agronomy and agriculture"),
        new("211", "Crop production"),
        new("212", "Animal production"),
        new("213", "Forestry, natural areas, wildlife, fishing"),
        new("214", "Landscaping"),
        new("220", "Multidisciplinary processing technologies"),
        new("221", "Food processing"),
        new("222", "Chemical processing"),
        new("223", "Metallurgy"),
        new("224", "Building materials, glass, ceramics"),
        new("225", "Plastics processing"),
        new("226", "Paper, cardboard"),
        new("227", "Energy, heating and cooling"),
        new("230", "Multidisciplinary civil engineering, construction, wood"),
        new("231", "Mines and quarries, civil engineering"),
        new("232", "Building: construction and roofing"),
        new("233", "Building: finishing work"),
        new("234", "Woodwork"),
        new("240", "Multidisciplinary flexible materials"),
        new("241", "Textiles"),
        new("242", "Clothing"),
        new("243", "Leather and hides"),
        new("250", "Multidisciplinary mechanics and electricity"),
        new("251", "General and precision mechanics, machining"),
        new("252", "Engines and vehicle mechanics"),
        new("253", "Aeronautical and space mechanics"),
        new("254", "Metal structures"),
        new("255", "Electricity, electronics"),
        new("300", "Multidisciplinary services"),
        new("310", "Multidisciplinary trade and management"),
        new("311", "Transport, handling, warehousing"),
        new("312", "Sales, commerce"),
        new("313", "Finance, banking, insurance, real estate"),
        new("314", "Accounting, management"),
        new("315", "Human resources, staff management, employment"),
        new("320", "Multidisciplinary communication and information"),
        new("321", "Journalism and communication"),
        new("322", "Printing and publishing"),
        new("323", "Image and sound techniques, performing arts"),
        new("324", "Office work, secretarial work"),
        new("325", "Documentation, libraries, administration of data"),
        new("326", "Computing, data processing, networks"),
        new("330", "Multidisciplinary health and social services"),
        new("331", "Health"),
        new("332", "Social work"),
        new("333", "Teaching, training"),
        new("334", "Hospitality, hotels, tourism"),
        new("335", "Cultural, sports and leisure activities"),
        new("336", "Hairdressing, beauty care"),
        new("341", "Land planning, urban development"),
        new("342", "Heritage protection and development"),
        new("343", "Cleaning, sanitation, environmental protection"),
        new("344", "Safety of property and people, police, surveillance"),
        new("345", "Application of rights and personal status"),
        new("346", "Military specialties"),
        new("410", "Multiple personal skills"),
        new("411", "Sports practice"),
        new("412", "Development of mental skills"),
        new("413", "Development of behavioral skills"),
        new("414", "Development of personal organization skills"),
        new("415", "Development of career guidance skills"),
        new("421", "Games and leisure activities"),
        new("422", "Domestic economy and activities"),
        new("423", "Family and social life"),
    ]);

    public static IReadOnlyList<ReferenceList> All { get; } =
    [
        FormLines, DeliveryModes, DistanceLearning, TrainingGoals, RncpLevels, NsfCodes,
        LearnerCategories, TrainerTypes, CostCategories,
    ];
}
