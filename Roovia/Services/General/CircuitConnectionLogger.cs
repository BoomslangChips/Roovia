using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;

namespace Roovia.Services.General
{
    /// <summary>
    /// Records dropped live connections so connectivity problems can be diagnosed from the server logs.
    /// Closing or reloading a tab also drops the connection, so only drops that recover (or expire) are reported.
    /// </summary>
    public class CircuitConnectionLogger : CircuitHandler
    {
        private readonly ILogger<CircuitConnectionLogger> _logger;
        private readonly TimeSpan _retentionPeriod;
        private DateTime? _connectionLostAtUtc;

        public CircuitConnectionLogger(ILogger<CircuitConnectionLogger> logger, IOptions<CircuitOptions> circuitOptions)
        {
            _logger = logger;
            _retentionPeriod = circuitOptions.Value.DisconnectedCircuitRetentionPeriod;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _connectionLostAtUtc = DateTime.UtcNow;
            _logger.LogDebug("Circuit {CircuitId} lost its connection", circuit.Id);
            return Task.CompletedTask;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (_connectionLostAtUtc is { } lostAt)
            {
                _logger.LogInformation("Circuit {CircuitId} reconnected after {Seconds:0.0}s offline", circuit.Id, (DateTime.UtcNow - lostAt).TotalSeconds);
                _connectionLostAtUtc = null;
            }

            return Task.CompletedTask;
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (_connectionLostAtUtc is { } lostAt && DateTime.UtcNow - lostAt >= _retentionPeriod)
            {
                _logger.LogWarning("Circuit {CircuitId} expired after the browser failed to reconnect within {Minutes:0.#} minutes", circuit.Id, _retentionPeriod.TotalMinutes);
            }
            else
            {
                _logger.LogDebug("Circuit {CircuitId} closed", circuit.Id);
            }

            return Task.CompletedTask;
        }
    }
}
