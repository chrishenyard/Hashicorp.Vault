namespace Hashicorp.Vault.SecretManagers;

public interface ISecretManager
{
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(
        CancellationToken cancellationToken = default);
}
