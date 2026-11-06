namespace BpfBuilder.Api.IntegrationTests;

public sealed class ImportEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Imports_the_sample_once_and_shows_it_on_the_fiscal_year()
    {
        var client = await LoggedInClientAsync();
        var year = await FiscalYearAsync(client, 2025);
        var sample = await client.GetByteArrayAsync("/api/template/sample");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/fiscal-years/{year.Id}/import")).StatusCode);

        var response = await UploadAsync(client, year.Id, sample, "activity-2025.xlsx");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/fiscal-years/{year.Id}/import", response.Headers.Location?.AbsolutePath);
        var report = (await response.Content.ReadFromJsonAsync<ImportReportResponse>())!;
        Assert.True(report.Imported);
        Assert.Equal(new ImportCountsResponse(5, 10, 6, 7, 3), report.Counts);
        Assert.Empty(report.Issues);

        var import = await client.GetFromJsonAsync<ActivityImportResponse>(response.Headers.Location);
        Assert.Equal("activity-2025.xlsx", import!.FileName);
        Assert.Equal(report.Counts, import.Counts);
        Assert.Equal(import.ImportedAtUtc, (await FiscalYearAsync(client, 2025)).ImportedAtUtc);
        Assert.Equal(import.ImportedAtUtc, (await client.GetFromJsonAsync<FiscalYearResponse>($"/api/fiscal-years/{year.Id}"))!.ImportedAtUtc);

        var again = await UploadAsync(client, year.Id, sample, "activity-2025.xlsx");
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("This fiscal year already has imported activity.", (await again.Content.ReadFromJsonAsync<ProblemDetails>())?.Title);
    }

    [Fact]
    public async Task Returns_the_report_without_saving_when_the_file_has_errors()
    {
        var client = await LoggedInClientAsync();
        var year = await FiscalYearAsync(client, 2024);
        var sample = await client.GetByteArrayAsync("/api/template/sample");

        var response = await UploadAsync(client, year.Id, sample, "activity-2025.xlsx");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<ImportReportResponse>())!;
        Assert.False(report.Imported);
        Assert.Contains(
            new ImportIssueResponse("Sessions", 2, "Start date", "The session is entirely outside the fiscal year.", "Error"),
            report.Issues);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/fiscal-years/{year.Id}/import")).StatusCode);
        Assert.Null((await FiscalYearAsync(client, 2024)).ImportedAtUtc);
    }

    [Fact]
    public async Task Rejects_a_missing_or_unreadable_file()
    {
        var client = await LoggedInClientAsync();
        var year = await FiscalYearAsync(client, 2024);

        using var empty = new MultipartFormDataContent { { new StringContent("2024"), "note" } };
        var missing = await client.PostAsync($"/api/fiscal-years/{year.Id}/import", empty);
        var unreadable = await UploadAsync(client, year.Id, "not a workbook"u8.ToArray(), "activity.xlsx");

        await AssertFileErrorAsync(missing, "Choose a filled activity template.");
        await AssertFileErrorAsync(unreadable, "This file is not a readable Excel workbook.");
    }

    [Fact]
    public async Task Finds_no_import_for_an_unknown_fiscal_year()
    {
        var client = await LoggedInClientAsync();
        var path = $"/api/fiscal-years/{Guid.NewGuid()}/import";

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(client, Guid.NewGuid(), [1], "activity.xlsx")).StatusCode);
    }

    [Fact]
    public async Task Endpoints_need_a_token_with_a_user()
    {
        var anonymous = factory.CreateClient();
        var withoutSubject = factory.CreateClient();
        withoutSubject.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenWithoutSubject());
        var id = Guid.NewGuid();

        foreach (var client in new[] { anonymous, withoutSubject })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/fiscal-years/{id}/import")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await UploadAsync(client, id, [1], "activity.xlsx")).StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid fiscalYearId, byte[] content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(file, "file", fileName);

        return await client.PostAsync($"/api/fiscal-years/{fiscalYearId}/import", form);
    }

    private static async Task AssertFileErrorAsync(HttpResponseMessage response, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal([message], problem!.Errors["file"]);
    }

    private static async Task<FiscalYearResponse> FiscalYearAsync(HttpClient client, int year)
    {
        var years = await client.GetFromJsonAsync<List<FiscalYearResponse>>("/api/fiscal-years");

        return years!.Single(y => y.StartDate.Year == year);
    }

    private async Task<HttpClient> LoggedInClientAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("camille.durand@example.com", "TestDemo123!"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }
}
