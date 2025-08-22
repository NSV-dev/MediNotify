using Mobile.Integrations.Abstractions;
using Mobile.Integrations.DTOs;
using Huawei.Hms.Hihealth;
using Huawei.Hms.Hihealth.Data;
using Huawei.Hms.Hihealth.Options;
using Huawei.Hms.Support.Hwid;
using Huawei.Hms.Support.Hwid.Request;
using Java.Util.Concurrent;

namespace Mobile.Integrations.Huawei.Services;

public class HuaweiHealthDataService : IHealthDataService
{
    private bool _isMonitoring;
    private string _deviceId = string.Empty;
    private string _deviceName = string.Empty;
    private DataController? _dataController;

    public string ProviderName => "Huawei Health";
    public bool IsConnected { get; private set; }

    public event EventHandler<PulseDataDto>? PulseDataReceived;
    public event EventHandler<string>? ConnectionStatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public HuaweiHealthDataService()
    {
        _isMonitoring = false;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // Initialize Huawei HMS Core first
            var initialized = HuaweiHealthInitializer.InitializeAsync();
            if (!initialized)
            {
                ErrorOccurred?.Invoke(this, "Failed to initialize Huawei HMS Core");
                return false;
            }
            
            // Get Huawei ID for authentication
            var authParams = new HuaweiIdAuthParamsHelper().CreateParams();
            var huaweiIdAuthManager = HuaweiIdAuthManager.GetService(Application.Context, authParams);
            var auth = await huaweiIdAuthManager.SilentSignInAsync();
            
            // Create DataController with authenticated Huawei ID
            _dataController = new DataController(auth);
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, "Connected to Huawei Health");
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_isMonitoring)
            {
                await StopPulseMonitoringAsync();
            }

            _dataController?.Dispose();
            IsConnected = false;
            ConnectionStatusChanged?.Invoke(this, "Disconnected from Huawei Health");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Disconnection error: {ex.Message}");
        }
    }

    public async Task<bool> StartPulseMonitoringAsync()
    {
        if (!IsConnected || _dataController == null)
        {
            ErrorOccurred?.Invoke(this, "Not connected to Huawei Health");
            return false;
        }

        try
        {
// #if ANDROID
//             // Subscribe to real-time heart rate data
//             var dataType = DataType.DtInstantaneousHeartRate;
//             
//             var result = await _dataController.SubscribeAsync(dataType);
//             
//             if (result.IsSuccess())
//             {
//                 _isMonitoring = true;
//                 
//                 // Start reading heart rate data
//                 _ = Task.Run(async () =>
//                 {
//                     await ReadHeartRateDataAsync();
//                 });
//                 
//                 return true;
//             }
//             else
//             {
//                 ErrorOccurred?.Invoke(this, "Failed to subscribe to heart rate data");
//                 return false;
//             }
// #else
//             ErrorOccurred?.Invoke(this, "Huawei Health is only available on Android devices");
//             return false;
//             return true;
// #endif
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Start monitoring error: {ex.Message}");
            return false;
        }
    }

    public async Task StopPulseMonitoringAsync()
    {
        if (!_isMonitoring || _dataController == null)
            return;

        try
        {
#if ANDROID
            // var dataType = DataType.DtInstantaneousHeartRate;
            // await _dataController.UnsubscribeAsync(dataType);
            _isMonitoring = false;
#endif
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Stop monitoring error: {ex.Message}");
        }
    }

    public async Task<PulseDataDto> GetCurrentPulseAsync()
    {
        if (!IsConnected || _dataController == null)
        {
            throw new InvalidOperationException("Not connected to Huawei Health");
        }

        try
        {
#if ANDROID
            // Read the latest heart rate data
            var dataType = DataType.DtInstantaneousHeartRate;
            var endTime = DateTime.Now;
            var startTime = endTime.AddMinutes(-Configuration.HuaweiHealthConfig.DataRetentionMinutes);
            
            var readOptions = new ReadOptions.Builder()
                .Read(dataType)
                .SetTimeRange(startTime.Minute, endTime.Minute, TimeUnit.Minutes)
                .Build();

            var result = await _dataController.ReadAsync(readOptions);
            
            if (result.Status.IsSuccess)
            {
                var dataSet = result.GetSampleSet(dataType);
                var latestData = dataSet.SamplePoints.FirstOrDefault();
                
                if (latestData != null)
                {
                    // var heartRate = latestData.GetFieldValue(Field.HeartRateField.HeartRate);
                    var timestamp = latestData.GetStartTime(TimeUnit.Milliseconds);
                    
                    return new PulseDataDto(
                        Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime,
                        DeviceId: _deviceId ?? "Huawei Device",
                        DeviceName: _deviceName ?? "Huawei Watch",
                        DataType: HealthDataType.Pulse,
                        PulseRate: /*(int)heartRate*/0,
                        IsRealTime: true,
                        Quality: DeterminePulseQuality(/*(int)heartRate*/0)
                    );
                }
            }
            
            // Fallback to mock data if no real data available
            return new PulseDataDto(
                Timestamp: DateTime.Now,
                DeviceId: _deviceId ?? "Huawei Device",
                DeviceName: _deviceName ?? "Huawei Watch",
                DataType: HealthDataType.Pulse,
                PulseRate: 74,
                IsRealTime: false,
                Quality: PulseQuality.Good
            );
#else
            throw new InvalidOperationException("Huawei Health is only available on Android devices");
#endif
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Get current pulse error: {ex.Message}");
            throw;
        }
    }

    private async Task ReadHeartRateDataAsync()
    {
        while (_isMonitoring)
        {
            try
            {
                var pulseData = await GetCurrentPulseAsync();
                PulseDataReceived?.Invoke(this, pulseData);
                
                // Wait before next reading
                await Task.Delay(Configuration.HuaweiHealthConfig.PulseReadingIntervalMs);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Heart rate reading error: {ex.Message}");
                break;
            }
        }
    }

    private static PulseQuality DeterminePulseQuality(int heartRate) =>
        heartRate switch
        {
            < Configuration.HuaweiHealthConfig.MinNormalHeartRate => PulseQuality.Poor,
            <= Configuration.HuaweiHealthConfig.MaxNormalHeartRate => PulseQuality.Good,
            <= Configuration.HuaweiHealthConfig.MaxAcceptableHeartRate => PulseQuality.Fair,
            > Configuration.HuaweiHealthConfig.MaxAcceptableHeartRate => PulseQuality.Poor
        };
}
