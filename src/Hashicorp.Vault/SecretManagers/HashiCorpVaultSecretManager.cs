using Hashicorp.Vault.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace Hashicorp.Vault.SecretManagers;

public sealed class HashiCorpVaultSecretManager(
    IOptions<HashiCorpVaultOptions> options,
    IHostEnvironment environment) : ISecretManager
{
    private readonly HashiCorpVaultOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private Task<IVaultClient>? _clientTask;

    public async Task<string?> GetSecretAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var secrets = await GetSecretsAsync(cancellationToken);
        return secrets.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        var client = await GetOrCreateClientAsync(cancellationToken);

        var result = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: _options.SecretPath,
            mountPoint: _options.MountPoint);

        var data = result?.Data?.Data;
        if (data is null || data.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return data.ToDictionary(
            pair => pair.Key,
            pair => pair.Value?.ToString() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IVaultClient> GetOrCreateClientAsync(CancellationToken cancellationToken)
    {
        if (_clientTask is { IsCompletedSuccessfully: true })
        {
            return _clientTask.Result;
        }

        await _clientLock.WaitAsync(cancellationToken);
        try
        {
            if (_clientTask is null || _clientTask.IsFaulted || _clientTask.IsCanceled)
            {
                _clientTask = CreateClientAsync(cancellationToken);
            }

            return await _clientTask;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    private async Task<IVaultClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var authMethod = await CreateAuthMethodInfoAsync(cancellationToken);
        return CreateVaultClient(authMethod);
    }

    private async Task<IAuthMethodInfo> CreateAuthMethodInfoAsync(CancellationToken cancellationToken)
    {
        return _options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => new KubernetesAuthMethodInfo(
                roleName: _options.RoleName,
                jwt: await File.ReadAllTextAsync(_options.KubernetesJwtPath, cancellationToken)),

            "approle" => new AppRoleAuthMethodInfo(
                roleId: _options.RoleId,
                secretId: _options.SecretId),

            "token" => new TokenAuthMethodInfo(_options.Token),

            _ => throw new InvalidOperationException(
                $"Unsupported Vault auth method: {_options.AuthMethod}")
        };
    }

    private VaultClient CreateVaultClient(IAuthMethodInfo authMethod)
    {
        var settings = new VaultClientSettings(_options.Address, authMethod);

        if (_environment.IsDevelopment() && _options.AllowInvalidServerCertificate)
        {
            settings.PostProcessHttpClientHandlerAction = handler =>
            {
                if (handler is HttpClientHandler clientHandler)
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
            };
        }

        return new VaultClient(settings);
    }
}
