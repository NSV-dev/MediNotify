using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Integrations.Abstractions;
using Mobile.Integrations.DTOs;
using Mobile.Integrations.Huawei.Services;

namespace MediNotify.ViewModels;

public class PulseViewModel : INotifyPropertyChanged
{
    private readonly IHealthDeviceManager _deviceManager;
    private int _currentPulse;
    private string _connectionStatus = "Disconnected";
    private bool _isConnected;
    private bool _isMonitoring;
    private string _lastUpdateTime = "Never";
    private string _deviceName = "No device";
    private PulseQuality _pulseQuality = PulseQuality.Unknown;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public PulseViewModel()
    {
        _deviceManager = new HuaweiDeviceManager();
            
        // Subscribe to events
        _deviceManager.PulseDataReceived += OnPulseDataReceived;
        _deviceManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        _deviceManager.ErrorOccurred += OnErrorOccurred;

        // Initialize commands
        ConnectCommand = new Command(async () => await ConnectAsync());
        DisconnectCommand = new Command(async () => await DisconnectAsync());
        StartMonitoringCommand = new Command(async () => await StartMonitoringAsync());
        StopMonitoringCommand = new Command(async () => await StopMonitoringAsync());
        RefreshCommand = new Command(async () => await RefreshPulseAsync());

        // Initialize the device manager
        _ = InitializeAsync();
    }

    public int CurrentPulse
    {
        get => _currentPulse;
        set => SetProperty(ref _currentPulse, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    public string LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public PulseQuality PulseQuality
    {
        get => _pulseQuality;
        set => SetProperty(ref _pulseQuality, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrEmpty(value);
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public string PulseQualityText => PulseQuality switch
    {
        PulseQuality.Excellent => "Excellent",
        PulseQuality.Good => "Good",
        PulseQuality.Fair => "Fair",
        PulseQuality.Poor => "Poor",
        _ => "Unknown"
    };

    public string PulseQualityColor => PulseQuality switch
    {
        PulseQuality.Excellent => "#4CAF50", // Green
        PulseQuality.Good => "#8BC34A",      // Light Green
        PulseQuality.Fair => "#FFC107",      // Yellow
        PulseQuality.Poor => "#FF5722",      // Red
        _ => "#9E9E9E"                       // Gray
    };

    public string PulseStatusText => IsMonitoring ? "Monitoring" : "Not Monitoring";
    public string PulseStatusColor => IsMonitoring ? "#4CAF50" : "#FF5722";

    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    public ICommand RefreshCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            var initialized = await _deviceManager.InitializeAsync();
            if (initialized)
            {
                ConnectionStatus = "Initialized";
                IsConnected = true;
            }
            else
            {
                ConnectionStatus = "Failed to initialize";
                ErrorMessage = "Failed to initialize Huawei Health service";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Initialization error: {ex.Message}";
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            var connected = await _deviceManager.ConnectToDeviceAsync("huawei_device");
            if (connected)
            {
                IsConnected = true;
                ConnectionStatus = "Connected";
            }
            else
            {
                ErrorMessage = "Failed to connect to Huawei device";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            await _deviceManager.DisconnectAsync();
            IsConnected = false;
            IsMonitoring = false;
            ConnectionStatus = "Disconnected";
            CurrentPulse = 0;
            LastUpdateTime = "Never";
            DeviceName = "No device";
            PulseQuality = PulseQuality.Unknown;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Disconnection error: {ex.Message}";
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            var started = await _deviceManager.StartPulseMonitoringAsync();
            if (started)
            {
                IsMonitoring = true;
            }
            else
            {
                ErrorMessage = "Failed to start pulse monitoring";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Start monitoring error: {ex.Message}";
        }
    }

    private async Task StopMonitoringAsync()
    {
        try
        {
            await _deviceManager.StopPulseMonitoringAsync();
            IsMonitoring = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Stop monitoring error: {ex.Message}";
        }
    }

    private async Task RefreshPulseAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            var pulseData = await _deviceManager.GetCurrentPulseAsync();
            UpdatePulseData(pulseData);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Refresh error: {ex.Message}";
        }
    }

    private void OnPulseDataReceived(object? sender, PulseDataDto pulseData)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdatePulseData(pulseData);
        });
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionStatus = status;
            IsConnected = status.Contains("Connected");
        });
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorMessage = error;
        });
    }

    private void UpdatePulseData(PulseDataDto pulseData)
    {
        CurrentPulse = pulseData.PulseRate;
        LastUpdateTime = pulseData.Timestamp.ToString("HH:mm:ss");
        DeviceName = pulseData.DeviceName;
        PulseQuality = pulseData.Quality;
            
        if (pulseData.IsRealTime)
        {
            IsMonitoring = true;
        }
    }

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}