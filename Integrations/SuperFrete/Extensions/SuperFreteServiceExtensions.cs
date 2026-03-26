using Integrations.SuperFrete.Configuration;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integrations.SuperFrete.Extensions;

public static class SuperFreteServiceExtensions
{
    /// <summary>
    /// Adiciona os serviços do SuperFrete ao container de DI
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="configuration">IConfiguration para ler as configurações</param>
    /// <returns>IServiceCollection para encadeamento</returns>
    public static IServiceCollection AddSuperFrete(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(SuperFreteSettings.SectionName).Get<SuperFreteSettings>()
            ?? throw new InvalidOperationException("Configuração SuperFrete não encontrada no appsettings.");

        services.AddSingleton<SuperFreteSettings>(settings);

        services.AddHttpClient<ISuperFreteService, SuperFreteService>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }

    /// <summary>
    /// Adiciona os serviços do SuperFrete com configuração manual
    /// </summary>
    public static IServiceCollection AddSuperFrete(this IServiceCollection services, Action<SuperFreteSettings> configure)
    {
        var settings = new SuperFreteSettings();
        configure(settings);

        services.AddSingleton<SuperFreteSettings>(settings);

        services.AddHttpClient<ISuperFreteService, SuperFreteService>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }
}
