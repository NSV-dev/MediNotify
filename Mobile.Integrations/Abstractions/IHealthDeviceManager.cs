using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mobile.Integrations.DTOs;

namespace Mobile.Integrations.Abstractions;

public interface IHealthDeviceManager
{
    event EventHandler<PulseDataDto>? PulseDataReceived;
    event EventHandler<string>? ConnectionStatusChanged;
    event EventHandler<string>? ErrorOccurred;
    
    IReadOnlyList<IHealthDataService> AvailableServices { get; }
    IHealthDataService? CurrentService { get; }
    
    Task<bool> InitializeAsync();
    Task<bool> ConnectToDeviceAsync(string deviceId);
    Task DisconnectAsync();
    Task<bool> StartPulseMonitoringAsync();
    Task StopPulseMonitoringAsync();
    Task<PulseDataDto> GetCurrentPulseAsync();
}
