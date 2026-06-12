using Hashicorp.Vault.Options;
using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Hashicorp.Vault.Extensions;

public static class SecretManagerExtensions
{
    public static IServiceCollection AddSecretManager(
        this IServiceCollection services,
        IConfiguration config)
    {
        var provider = config
            .GetSection("HashiCorpVaultOptions")
            .Get<HashiCorpVaultOptions>()!.Provider;

        switch (provider?.ToLowerInvariant())
        {
            case "hashicorpvault":
            case "vault":
                services.AddSingleton<ISecretManager, HashiCorpVaultSecretManager>();
                break;

            case "azurekeyvault":
            case "azure":
                services.AddSingleton<ISecretManager, AzureKeyVaultSecretManager>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported secret manager provider: {provider}");
        }

        return services;
    }

    public static async Task MapOptions<T>(IServiceProvider serviceProvider) where T : class
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var secretManager = serviceProvider.GetRequiredService<ISecretManager>();
        var options = serviceProvider.GetRequiredService<IOptions<T>>().Value;
        var secrets = await secretManager.GetSecretsAsync();
        
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name);

        foreach (var pair in secrets)
        {
            if (!properties.TryGetValue(pair.Key, out var prop))
            {
                throw new InvalidOperationException($"Property '{pair.Key}' not found on type '{type.FullName}'.");
            }
            prop.SetValue(options, pair.Value);
        }
    }
}
