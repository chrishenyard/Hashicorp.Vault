using Hashicorp.Vault.Options;
using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hashicorp.Vault.Extensions;

public static class SecretManagerServiceCollectionExtensions
{
    public static IServiceCollection AddSecretManager(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SecretManagerOptions>(
            configuration.GetSection("SecretManager"));

        services.Configure<HashiCorpVaultOptions>(
            configuration.GetSection("SecretManager:HashiCorpVault"));

        var provider = configuration["SecretManager:Provider"];

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
}
