using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>
/// Mensagens que o próprio framework gera (validação de modelo, JSON inválido, FluentValidation padrão)
/// chegam ao usuário em português.
/// </summary>
public class PortugueseMessagesTests : IntegrationTestBase
{
    public PortugueseMessagesTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static readonly string[] English =
        ["One or more validation errors occurred", "is required", "could not be converted", "Not Found", "Bad Request", "must not be empty"];

    [Fact]
    public async Task InvalidJson_ReturnsValidationProblemInPortuguese()
    {
        using var admin = await AccessTestHelpers.FixtureAdminAsync(Factory);
        var response = await admin.PostAsync("/api/v1/disciplinary-records",
            new StringContent("{\"employeeId\": \"nao-e-guid\", \"measureType\": \"x\"}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(body).RootElement;
        root.GetProperty("title").GetString().Should().Be("Um ou mais erros de validação ocorreram.");
        body.Should().NotContainAny(English);
    }

    [Fact]
    public async Task MissingBody_ReturnsPortuguese()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsync("/api/v1/auth/login", new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainAny(English);
    }

    [Fact]
    public void FluentValidationDefaults_AreInPortuguese()
    {
        _ = Factory.CreateClient(); // AddApplication configura a cultura e os nomes dos campos.
        var validator = new InlineValidator<Sample>();
        validator.RuleFor(x => x.Description).NotEmpty();
        validator.RuleFor(x => x.EmployeeNumber).MaximumLength(3);

        var errors = validator.Validate(new Sample("", "12345")).Errors.Select(e => e.ErrorMessage).ToList();
        errors.Should().Contain("'Descrição' deve ser informado.");
        errors.Should().Contain(e => e.StartsWith("'Matrícula' deve ser menor ou igual a 3 caracteres"));
    }

    public sealed record Sample(string Description, string EmployeeNumber);
}
