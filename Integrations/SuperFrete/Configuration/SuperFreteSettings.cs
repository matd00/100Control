namespace Integrations.SuperFrete.Configuration;

public class SuperFreteSettings
{
    public const string SectionName = "SuperFrete";

    /// <summary>
    /// Token de autenticação da API SuperFrete
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// URL base da API (produção: https://api.superfrete.com)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.superfrete.com";

    /// <summary>
    /// CEP de origem padrão para cotações
    /// </summary>
    public string DefaultOriginPostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Timeout em segundos para requisições
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
