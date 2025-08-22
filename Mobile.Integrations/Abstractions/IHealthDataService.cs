using Mobile.Integrations.DTOs;

namespace Mobile.Integrations.Abstractions;

public interface IHealthDataService
{
    string ProviderName { get; }
    bool IsConnected { get; }
    
    event EventHandler<PulseDataDto> PulseDataReceived;
    event EventHandler<string> ConnectionStatusChanged;
    event EventHandler<string> ErrorOccurred;
    
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> StartPulseMonitoringAsync();
    Task StopPulseMonitoringAsync();
    Task<PulseDataDto> GetCurrentPulseAsync();
}
