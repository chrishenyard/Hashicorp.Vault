namespace Hashicorp.Vault.Options;

public sealed class SecretManagerOptions
{
    public string Provider { get; set; } = "HashiCorpVault";
}

public sealed class HashiCorpVaultOptions
{
    public string Address { get; set; } = "";
    public string AuthMethod { get; set; } = "Token";

    public string Token { get; set; } = "";

    public string RoleName { get; set; } = "";
    public string KubernetesJwtPath { get; set; } =
        "/var/run/secrets/kubernetes.io/serviceaccount/token";

    public string RoleId { get; set; } = "";
    public string SecretId { get; set; } = "";

    public string MountPoint { get; set; } = "secret";
    public string SecretPath { get; set; } = "";

    public bool AllowInvalidServerCertificate { get; set; } = false;
}
