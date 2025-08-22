using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mobile.Integrations.Abstractions;
using Mobile.Integrations.DTOs;

namespace Mobile.Integrations.Huawei.Services;

public class HuaweiDeviceManager : IHealthDeviceManager
{
    private readonly List<IHealthDataService> _availableServices;
    private IHealthDataService? _currentService;

    public event EventHandler<PulseDataDto>? PulseDataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public IReadOnlyList<IHealthDataService> AvailableServices => _availableServices.AsReadOnly();
    public IHealthDataService? CurrentService => _currentService;

    public HuaweiDeviceManager()
    {
        _availableServices = new List<IHealthDataService>();
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Create Huawei health service
            var huaweiService = new HuaweiHealthDataService();
            
            // Subscribe to events
            huaweiService.PulseDataReceived += OnPulseDataReceived;
            huaweiService.ConnectionStatusChanged += OnConnectionStatusChanged;
            huaweiService.ErrorOccurred += OnErrorOccurred;

            _availableServices.Add(huaweiService);

            // Try to connect to Huawei Health
            var connected = await huaweiService.ConnectAsync();
            if (connected)
            {
                _currentService = huaweiService;
            }

            return _availableServices.Any(s => s.IsConnected);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Initialization error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConnectToDeviceAsync(string deviceId)
    {
        try
        {
            var service = _availableServices.FirstOrDefault(s => s.ProviderName.Contains("Huawei"));
            if (service == null)
            {
                ErrorOccurred?.Invoke(this, "Huawei Health service not available");
                return false;
            }

            var connected = await service.ConnectAsync();
            if (connected)
            {
                _currentService = service;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Connect to device error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_currentService != null)
        {
            await _currentService.DisconnectAsync();
            _currentService = null;
        }
    }

    public async Task<bool> StartPulseMonitoringAsync()
    {
        if (_currentService == null)
        {
            ErrorOccurred?.Invoke(this, "No device connected");
            return false;
        }

        return await _currentService.StartPulseMonitoringAsync();
    }

    public async Task StopPulseMonitoringAsync()
    {
        if (_currentService != null)
        {
            await _currentService.StopPulseMonitoringAsync();
        }
    }

    public async Task<PulseDataDto> GetCurrentPulseAsync()
    {
        if (_currentService == null)
        {
            throw new InvalidOperationException("No device connected");
        }

        return await _currentService.GetCurrentPulseAsync();
    }

    private void OnPulseDataReceived(object? sender, PulseDataDto pulseData)
    {
        PulseDataReceived?.Invoke(this, pulseData);
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        ConnectionStatusChanged?.Invoke(this, status);
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }
}
