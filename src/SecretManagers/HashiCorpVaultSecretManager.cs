using Hashicorp.Vault.Options;
using Microsoft.Extensions.Hosting;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace Hashicorp.Vault.SecretManagers;

public sealed class HashiCorpVaultSecretManager : ISecretManager
{
    private readonly HashiCorpVaultOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly Lazy<Task<IVaultClient>> _client;

    public HashiCorpVaultSecretManager(
        Microsoft.Extensions.Options.IOptions<HashiCorpVaultOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
        _client = new Lazy<Task<IVaultClient>>(CreateClientAsync);
    }

    public async Task<string?> GetSecretAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var secrets = await GetSecretsAsync(cancellationToken);

        return secrets.TryGetValue(key, out var value)
            ? value
            : null;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        var client = await _client.Value;

        var result = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: _options.SecretPath,
            mountPoint: _options.MountPoint);

        return result.Data.Data
            .ToDictionary(
                x => x.Key,
                x => x.Value?.ToString() ?? "");
    }

    private async Task<IVaultClient> CreateClientAsync()
    {
        IAuthMethodInfo authMethod = _options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => new KubernetesAuthMethodInfo(
                roleName: _options.RoleName,
                jwt: await File.ReadAllTextAsync(_options.KubernetesJwtPath)),

            "approle" => new AppRoleAuthMethodInfo(
                roleId: _options.RoleId,
                secretId: _options.SecretId),

            "token" => new TokenAuthMethodInfo(_options.Token),

            _ => throw new InvalidOperationException(
                $"Unsupported Vault auth method: {_options.AuthMethod}")
        };

        if (_environment.IsDevelopment())
            return GetDevelopmentClient(authMethod);

        return GetClient(authMethod);
    }

    private VaultClient GetDevelopmentClient(IAuthMethodInfo authMethod)
    {
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };

        var settings = new VaultClientSettings(
            _options.Address,
            authMethod);

        settings.PostProcessHttpClientHandlerAction = handler =>
        {
            if (handler is HttpClientHandler clientHandler)
            {
                clientHandler.ServerCertificateCustomValidationCallback = httpClientHandler.ServerCertificateCustomValidationCallback;
            }
        };

        return new VaultClient(settings);
    }

    private IVaultClient GetClient(IAuthMethodInfo authMethod)
    {
        var settings = new VaultClientSettings(
            _options.Address,
            authMethod);

        return new VaultClient(settings);
    }
}
