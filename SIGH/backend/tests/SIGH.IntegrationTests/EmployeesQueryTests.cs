using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;
using SIGH.Application.Employees.Import;
using SIGH.Domain.Employees.Enums;
using SIGH.IntegrationTests.Infrastructure;
using Xunit;
using R = SIGH.IntegrationTests.Infrastructure.HrXlsxBuilder.Row;

namespace SIGH.IntegrationTests;

public class EmployeesQueryTests : IntegrationTestBase
{
    public EmployeesQueryTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task List_MasksTheCpf_FiltersByGroup_AndDetailShowsTheFullCpf()
    {
        using var admin = await AdminAsync();
        var anaCpf = HrXlsxBuilder.Cpf(1);
        var file = new HrXlsxBuilder()
            .Add(new R("50090001", "Ana Sintética Um", anaCpf, Group: "Campo", ManagementUnit: "USGA141", DeptCode: "60002000", DeptName: "Corte"))
            .Add(new R("50090002", "Bruno Sintético Dois", HrXlsxBuilder.Cpf(2), Group: "Indústria"))
            .Build();
        await ImportAsync(admin, file);

        var all = (await admin.GetFromJsonAsync<Result<PagedResult<EmployeeListItemResponse>>>("/api/v1/employees?pageSize=50"))!.Data!;
        var ana = all.Items.Single(e => e.EmployeeNumber == "50090001");
        ana.CpfMasked.Should().Be($"***.{anaCpf[3..6]}.{anaCpf[6..9]}-**");
        ana.Group.Should().Be(EmployeeGroup.Field);
        ana.ManagementUnitCode.Should().Be("USGA141");
        ana.ManagementUnitName.Should().Be("Agrícola Rurícola");
        ana.DepartmentCode.Should().Be("60002000");
        (await admin.GetStringAsync("/api/v1/employees?pageSize=50")).Should().NotContain(anaCpf, "a lista nunca traz o CPF completo");

        var field = (await admin.GetFromJsonAsync<Result<PagedResult<EmployeeListItemResponse>>>($"/api/v1/employees?group={(int)EmployeeGroup.Field}"))!.Data!;
        field.Items.Select(e => e.EmployeeNumber).Should().Equal("50090001");

        var bySearch = (await admin.GetFromJsonAsync<Result<PagedResult<EmployeeListItemResponse>>>("/api/v1/employees?searchTerm=50090002"))!.Data!;
        bySearch.Items.Should().ContainSingle(e => e.FullName == "Bruno Sintético Dois");

        var detail = (await admin.GetFromJsonAsync<Result<GetEmployeeByIdResponse>>($"/api/v1/employees/{ana.Id}"))!.Data!;
        detail.Cpf.Should().Be(anaCpf);
        detail.Team.Should().Be("A");
        detail.JobTitleCode.Should().Be("50001234");

        var options = (await admin.GetFromJsonAsync<Result<EmployeeFilterOptionsDto>>("/api/v1/employees/filter-options"))!.Data!;
        options.ManagementUnits.Should().HaveCountGreaterOrEqualTo(9).And.Contain(m => m.Code == "USGA131" && m.Name == "Industrial");
        options.Departments.Should().Contain(d => d.Code == "60002000" && d.Name == "Corte");
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, "admin@sigh.com", CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task ImportAsync(HttpClient client, byte[] file)
    {
        async Task<HttpResponseMessage> Post(string url, bool? confirm = null)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(file), "file", "exportacao.xlsx");
            if (confirm is { } c) form.Add(new StringContent(c ? "true" : "false"), "confirmMassTermination");
            return await client.PostAsync(url, form);
        }

        var preview = (await (await Post("/api/v1/employees/import/preview")).Content.ReadFromJsonAsync<Result<EmployeeImportPreviewDto>>())!.Data!;
        (await Post($"/api/v1/employees/import/{preview.BatchId}/confirm", true)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
