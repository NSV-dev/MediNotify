using Huawei.Hms.Api;

namespace Mobile.Integrations.Huawei.Services;

public static class HuaweiHealthInitializer
{
    private static bool _isInitialized;
    
    public static bool InitializeAsync()
    {
        if (_isInitialized)
            return true;
            
        try
        {
#if ANDROID
            var result = HuaweiApiAvailability.Instance.IsHuaweiMobileServicesAvailable(Application.Context);

            if (result != ConnectionResult.Success) return false;
            _isInitialized = true;
            return true;
#else
            return false;
#endif
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static bool IsInitialized => _isInitialized;
}
