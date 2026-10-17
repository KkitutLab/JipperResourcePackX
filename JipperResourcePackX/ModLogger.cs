namespace JipperResourcePackX;

public static class ModLogger {
    private static string Clean(string message) {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        if (message.StartsWith("[JipperResourcePackX] ")) return message.Substring(22);
        if (message.StartsWith("[JipperResourcePackX]")) return message.Substring(21);
        return message;
    }

    public static void Log(string message) {
#if ML
        MelonLoader.MelonLogger.Msg(Clean(message));
#else
        UnityModManagerNet.UnityModManager.Logger.Log(Clean(message));
#endif
    }

    public static void Warn(string message) {
#if ML
        MelonLoader.MelonLogger.Warning(Clean(message));
#else
        UnityModManagerNet.UnityModManager.Logger.Log("[WARN] " + Clean(message));
#endif
    }

    public static void Error(string message) {
#if ML
        MelonLoader.MelonLogger.Error(Clean(message));
#else
        UnityModManagerNet.UnityModManager.Logger.Error(Clean(message));
#endif
    }

    public static void LogException(System.Exception ex) {
        Error(ex.ToString());
    }

    public static void LogException(string message, System.Exception ex) {
        Error($"{message}\n{ex}");
    }
}
