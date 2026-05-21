using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace Hashicorp.Vault;

public sealed class VaultOptions
{
    public const string SectionName = "VaultOptions";

    public string Address { get; set; } = "";
    public string AuthMethod { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string RoleId { get; set; } = "";
    public string SecretId { get; set; } = "";
    public string Token { get; set; } = "";
    public string MountPoint { get; set; } = "secret";
    public string SecretPath { get; set; } = "";
}

public static class VaultClientFactory
{
    public static async Task<IVaultClient> CreateAsync(VaultOptions options)
    {
        IAuthMethodInfo authMethod = options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => new KubernetesAuthMethodInfo(
                roleName: options.RoleName,
                jwt: await File.ReadAllTextAsync(
                    "/var/run/secrets/kubernetes.io/serviceaccount/token")),

            "approle" => new AppRoleAuthMethodInfo(
                roleId: options.RoleId,
                secretId: options.SecretId),

            "token" => new TokenAuthMethodInfo(options.Token),

            _ => throw new InvalidOperationException(
                $"Unsupported Vault auth method: {options.AuthMethod}")
        };

        var settings = new VaultClientSettings(options.Address, authMethod);

        return new VaultClient(settings);
    }
}
