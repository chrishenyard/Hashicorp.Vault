using Hashicorp.Vault.Console;
using Hashicorp.Vault.SecretManagers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hashicorp.Vault.Services;

internal sealed class VaultService(
    ISecretManager secretManager,
    IHostApplicationLifetime lifeTime,
    ILogger<VaultService> logger) : IHostedService
{
    private readonly ISecretManager _secretManager = secretManager;
    private readonly IHostApplicationLifetime _lifeTime = lifeTime;
    private readonly ILogger<VaultService> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => VaultConsole.ExecuteAsync(
            _secretManager, _lifeTime, _logger, cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Console chat service is stopping.");
        return Task.CompletedTask;
    }
}