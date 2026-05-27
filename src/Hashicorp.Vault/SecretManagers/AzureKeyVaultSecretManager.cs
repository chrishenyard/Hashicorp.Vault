namespace Hashicorp.Vault.SecretManagers;

public sealed class AzureKeyVaultSecretManager : ISecretManager
{
    public Task<string?> GetSecretAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
