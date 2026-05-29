using FluentValidation;

namespace Hashicorp.Vault.Options;

public sealed class HashiCorpVaultOptions
{
    public string Provider { get; set; } = "HashiCorpVault";
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

public class HashiCorpVaultOptionsValidator : AbstractValidator<HashiCorpVaultOptions>
{
    public HashiCorpVaultOptionsValidator()
    {
        RuleFor(x => x.Address).NotEmpty().WithMessage("SecretManager:HashiCorpVault:Address is required.");
        RuleFor(x => x.MountPoint).NotEmpty().WithMessage("SecretManager:HashiCorpVault:MountPoint is required.");
        RuleFor(x => x.SecretPath).NotEmpty().WithMessage("SecretManager:HashiCorpVault:SecretPath is required.");
        When(x => x.AuthMethod.Equals("Token", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("Token auth requires SecretManager:HashiCorpVault:Token.");
        });
        When(x => x.AuthMethod.Equals("AppRole", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.RoleId).NotEmpty().WithMessage("AppRole auth requires RoleId.");
            RuleFor(x => x.SecretId).NotEmpty().WithMessage("AppRole auth requires SecretId.");
        });
        When(x => x.AuthMethod.Equals("Kubernetes", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.RoleName).NotEmpty().WithMessage("Kubernetes auth requires RoleName.");
            RuleFor(x => x.KubernetesJwtPath).NotEmpty().WithMessage("Kubernetes auth requires KubernetesJwtPath.");
        });
    }
}
