using Hashicorp.Vault.Options;
using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hashicorp.Vault.Extensions;

public static class SecretManagerService
{
    public static IServiceCollection AddSecretManager(
        this IServiceCollection services,
        IConfiguration config)
    {
        var options = config
            .GetSection("HashiCorpVaultOptions")
            .Get<HashiCorpVaultOptions>()!;
        var provider = options.Provider;

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

    public static async Task MapOptions(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var secretManager = serviceProvider.GetRequiredService<ISecretManager>();
        var options = serviceProvider.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value;
        var secrets = await secretManager.GetSecretsAsync();
        var type = options.GetType();

        foreach (var pair in secrets)
        {
            var prop = type.GetProperty(pair.Key)
                ?? throw new InvalidOperationException($"Property '{pair.Key}' not found on type '{type.FullName}'.");
            prop.SetValue(options, pair.Value);
        }
    }
}
