using SpookVooper.Server.Config;
using Valour.Api.Client;

namespace SpookVooper.Server.Workers;

public class ValourWorker : IHostedService
{
    private readonly ILogger<ValourWorker> _logger;
    private readonly HttpClient _http = new();
    
    public ValourWorker(ILogger<ValourWorker> logger)
    {
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting Valour Worker...");
        var result = await ValourClient.InitializeBot(ValourConfig.Instance.BotEmail, ValourConfig.Instance.BotPassword, _http);

        if (result.Success)
        {
            _logger.LogDebug("Valour Worker started successfully!");
        }
        else
        {
            _logger.LogError("Valour Worker failed to start!");
            _logger.LogError(result.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}