using FluentValidation;
using Hashicorp.Vault.Options;
using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hashicorp.Vault.Extensions;

public static class SecretManagerServiceCollectionExtensions
{
    public static IServiceCollection AddSecretManager(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();

        services.AddOptions<HashiCorpVaultOptions>()
            .Bind(configuration.GetSection("HashiCorpVault"))
            .Validate(options =>
            {
                var validator = serviceProvider.GetRequiredService<IValidator<HashiCorpVaultOptions>>();
                var result = validator.Validate(options);
                if (!result.IsValid)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                    throw new InvalidOperationException($"Invalid HashiCorpVaultOptions: {errors}");
                }
                return true;
            });

        var options = serviceProvider.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value;
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
}
