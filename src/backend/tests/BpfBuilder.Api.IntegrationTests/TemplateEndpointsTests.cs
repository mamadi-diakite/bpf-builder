namespace BpfBuilder.Api.IntegrationTests;

public sealed class TemplateEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Theory]
    [InlineData("/api/template", "bpf-activity-template-v1.xlsx", 1)]
    [InlineData("/api/template/sample", "bpf-activity-sample-v1.xlsx", 6)]
    public async Task Downloads_the_workbook(string url, string fileName, int sessionRows)
    {
        var client = await LoggedInClientAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fileName, response.Content.Headers.ContentDisposition?.FileName);
        using var workbook = new XLWorkbook(await response.Content.ReadAsStreamAsync());
        Assert.Equal(sessionRows, workbook.Worksheet("Sessions").LastRowUsed()!.RowNumber());
    }

    [Theory]
    [InlineData("/api/template")]
    [InlineData("/api/template/sample")]
    public async Task Requires_a_signed_in_user(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
