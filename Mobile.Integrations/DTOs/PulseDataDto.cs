namespace Mobile.Integrations.DTOs;

public record PulseDataDto(
    DateTime Timestamp,
    string DeviceId,
    string DeviceName,
    HealthDataType DataType,
    int PulseRate,
    int? MinPulseRate = null,
    int? MaxPulseRate = null,
    double? AveragePulseRate = null,
    PulseQuality Quality = PulseQuality.Unknown,
    bool IsRealTime = false
) : HealthDataDto(Timestamp, DeviceId, DeviceName, DataType);

public enum PulseQuality
{
    Unknown,
    Poor,
    Fair,
    Good,
    Excellent
}
