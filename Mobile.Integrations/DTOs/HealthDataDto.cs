namespace Mobile.Integrations.DTOs;

public record HealthDataDto(
    DateTime Timestamp,
    string DeviceId,
    string DeviceName,
    HealthDataType DataType
);

public enum HealthDataType
{
    Pulse,
    HeartRate,
    BloodPressure,
    Steps,
    Sleep,
    Calories,
    Distance
}
