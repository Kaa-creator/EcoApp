namespace EcoAppMobile.Helpers
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
#if ANDROID
            "http://10.0.2.2:5287";      // Эмулятор Android
#elif IOS
            "http://localhost:5287";      // iOS симулятор
#else
            "http://172.20.10.3:5287";    // Windows (твой IP)
#endif
    }
}