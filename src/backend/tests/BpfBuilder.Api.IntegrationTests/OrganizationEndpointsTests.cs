using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using BpfBuilder.Api.Auth;
using BpfBuilder.Application.Contracts.Auth;
using BpfBuilder.Application.Contracts.FiscalYears;
using BpfBuilder.Application.Contracts.Organizations;

namespace BpfBuilder.Api.IntegrationTests;

public sealed class OrganizationEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoPassword = "TestDemo123!";

    private static readonly OrganizationIdentificationRequest Identification = new(
        "Northwind Training", "11 888 888 888", null, "85.59a", "SARL", "4 Sample Road", "69001", "Lyon", false, null, null);

    private static readonly OrganizationSignatoryRequest Signatory =
        new("Alex Martin", "Manager", "Alex Martin", "Manager", "Lyon", "alex.martin@example.com", null);

    [Fact]
    public async Task Demo_account_sees_its_organization_and_fiscal_years()
    {
        var client = await LoggedInClientAsync("camille.durand@example.com");

        var organization = await client.GetFromJsonAsync<OrganizationResponse>("/api/organization");
        var years = await client.GetFromJsonAsync<List<FiscalYearResponse>>("/api/fiscal-years");

        Assert.Equal("Horizon Skills Training", organization?.Name);
        Assert.Equal("Camille Durand", organization?.SignatoryName);
        Assert.Equal(2, years!.Count);
        Assert.True(years[0].StartDate > years[1].StartDate);
    }

    [Fact]
    public async Task New_account_fills_its_profile_then_creates_a_fiscal_year()
    {
        var client = await RegisteredClientAsync("alex.martin@example.com");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/organization")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<FiscalYearResponse>>("/api/fiscal-years"))!);
        await AssertNeedsIdentificationAsync(await client.PutAsJsonAsync("/api/organization/signatory", Signatory));
        await AssertNeedsIdentificationAsync(await client.PostAsJsonAsync("/api/fiscal-years", new CreateFiscalYearRequest(null, null, 0m)));

        var identification = await client.PutAsJsonAsync("/api/organization/identification", Identification);
        Assert.Equal(HttpStatusCode.OK, identification.StatusCode);
        Assert.Equal("11888888888", (await identification.Content.ReadFromJsonAsync<OrganizationResponse>())?.DeclarationNumber);

        var signatory = await client.PutAsJsonAsync("/api/organization/signatory", Signatory);
        Assert.Equal(HttpStatusCode.OK, signatory.StatusCode);
        Assert.Equal("Alex Martin", (await client.GetFromJsonAsync<OrganizationResponse>("/api/organization"))?.SignatoryName);

        var created = await client.PostAsJsonAsync("/api/fiscal-years", new CreateFiscalYearRequest(null, null, 98_000m));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var fiscalYear = await created.Content.ReadFromJsonAsync<FiscalYearResponse>();
        Assert.Equal($"/api/fiscal-years/{fiscalYear!.Id}", created.Headers.Location?.AbsolutePath);
        Assert.Equal(fiscalYear, await client.GetFromJsonAsync<FiscalYearResponse>(created.Headers.Location));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/fiscal-years/{Guid.NewGuid()}")).StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/fiscal-years", new CreateFiscalYearRequest(null, null, 98_000m));
        await AssertInvalidAsync(duplicate, "startDate");
    }

    [Fact]
    public async Task Invalid_sections_return_every_error()
    {
        var client = await RegisteredClientAsync("lou.perrin@example.com");

        await AssertInvalidAsync(
            await client.PutAsJsonAsync("/api/organization/identification", Identification with { Name = "", Siret = "123" }),
            "name",
            "siret");

        await client.PutAsJsonAsync("/api/organization/identification", Identification);
        await AssertInvalidAsync(
            await client.PutAsJsonAsync("/api/organization/signatory", Signatory with { SignatoryEmail = "lou" }),
            "signatoryEmail");
    }

    [Theory]
    [InlineData("GET", "/api/organization")]
    [InlineData("PUT", "/api/organization/identification")]
    [InlineData("PUT", "/api/organization/signatory")]
    [InlineData("GET", "/api/fiscal-years")]
    [InlineData("GET", "/api/fiscal-years/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/fiscal-years")]
    public async Task Endpoints_need_a_token_with_a_user(string method, string path)
    {
        var anonymous = factory.CreateClient();
        var withoutSubject = factory.CreateClient();
        withoutSubject.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject());

        foreach (var client in new[] { anonymous, withoutSubject })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Content = method switch
            {
                "POST" => JsonContent.Create(new CreateFiscalYearRequest(null, null, 0m)),
                "PUT" when path.EndsWith("identification", StringComparison.Ordinal) => JsonContent.Create(Identification),
                "PUT" => JsonContent.Create(Signatory),
                _ => null,
            };

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
        }
    }

    private static async Task AssertNeedsIdentificationAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Save the organization's identification first.", problem?.Title);
    }

    private static async Task AssertInvalidAsync(HttpResponseMessage response, params string[] fields)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(fields, problem!.Errors.Keys.Order());
    }

    private async Task<HttpClient> RegisteredClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Test User", email, DemoPassword, DemoPassword));

        return await AuthorizeAsync(client, response);
    }

    private async Task<HttpClient> LoggedInClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DemoPassword));

        return await AuthorizeAsync(client, response);
    }

    private static async Task<HttpClient> AuthorizeAsync(HttpClient client, HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private string TokenWithoutSubject()
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }
}
