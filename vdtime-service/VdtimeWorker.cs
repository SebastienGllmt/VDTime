using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class VdtimeWorker : BackgroundService
{
    private readonly ILogger<VdtimeWorker> _logger;
    private readonly ServiceOptions _options;

    public VdtimeWorker(ILogger<VdtimeWorker> logger, IOptions<ServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("vdtime-service starting with Mode={Mode}", _options.Mode);

        var stateManager = new StateManager();
        stateManager.listen();

        try
        {
            if (string.Equals(_options.Mode, "rest", StringComparison.OrdinalIgnoreCase))
            {
                var port = _options.Port ?? 5059;
                _logger.LogInformation("Starting REST adaptor on port {Port}", port);
                await RestAdaptor.RunAsync(stateManager, port, stoppingToken);
            }
            else
            {
                var pipe = string.IsNullOrWhiteSpace(_options.Pipe) ? "vdtime-core" : _options.Pipe!;
                _logger.LogInformation("Starting NamedPipe adaptor on pipe {Pipe}", pipe);
                await NamedPipeAdaptor.RunAsync(stateManager, pipe, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on stop
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "vdtime-service encountered an error");
            throw;
        }

        _logger.LogInformation("vdtime-service stopping");
    }
}

