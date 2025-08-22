using Android.Content;
using Huawei.Agconnect.Config;

namespace Mobile.Integrations.Huawei;

public static class HuaweiMainActivityHelper
{
    public static void SetConfig(Context context) => 
        AGConnectServicesConfig.FromContext(context);
}