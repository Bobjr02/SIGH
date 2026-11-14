namespace SIGH.Api.Demo;

/// <summary>
/// Dados de demonstração (seção "Seed"): Seed:DemoData=true cria, pelos serviços reais, funcionários,
/// usuários e medidas FICTÍCIOS. Só é aceito em Development ou Demo: em qualquer outro ambiente a API
/// recusa subir (ValidateOnStart).
/// </summary>
public sealed class DemoDataOptions
{
    public const string SectionName = "Seed";

    /// <summary>Seed:DemoData.</summary>
    public bool DemoData { get; set; }

    /// <summary>Seed:DemoPassword — senha dos usuários de demonstração (vem do .env.local).</summary>
    public string? DemoPassword { get; set; }

    /// <summary>Seed:DemoDataPath — pasta com os arquivos fictícios do pacote (SIGH/docs/referencias).</summary>
    public string? DemoDataPath { get; set; }
}

public static class DemoDataGuard
{
    public const string DemoEnvironment = "Demo";

    public const string RefusedMessage =
        "Seed:DemoData=true só é permitido com ASPNETCORE_ENVIRONMENT=Development ou Demo. " +
        "Dados de demonstração nunca podem ser criados em outro ambiente: desligue Seed:DemoData.";

    /// <summary>Ambientes em que os dados fictícios e a faixa "AMBIENTE DE DEMONSTRAÇÃO" são permitidos.</summary>
    public static bool IsDemoEnvironment(string environmentName) =>
        string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(environmentName, DemoEnvironment, StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowed(DemoDataOptions options, string environmentName) =>
        !options.DemoData || IsDemoEnvironment(environmentName);
}
