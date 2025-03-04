using Microsoft.Maui.Storage;

public static class GlobalConString
{
    public static string ConnStr => SecureStorage.GetAsync("dbConnectionString").Result ?? "server=localhost;user=root;database=olympos;password=default;sslmode=none;";

    public static async Task Initialize()
    {
        if (SecureStorage.GetAsync("dbConnectionString").Result == null)
        {
            await SecureStorage.SetAsync("dbConnectionString", "server=10.0.2.2;user=root;database=olympos;password=pa0k0l31;sslmode=none;");
        }
    }
}
