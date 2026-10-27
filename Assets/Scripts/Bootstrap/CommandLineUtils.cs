using System;

namespace TanksKombat2.Bootstrap
{
    public static class CommandLineUtils
    {
        public static bool TryGetCommandLineValueFromKey(string key, out string value)
        {
            string[] args = Environment.GetCommandLineArgs ();
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Equals($"-{key}", StringComparison.OrdinalIgnoreCase)) {
                    value = args[i + 1];
                    return  true;
                }
            }

            value = string.Empty;
            return false;
        }

        public static bool TryGetCommandLineValueFromKey<T>(string key, out T value) where T : struct
        {
            value = default;
            return TryGetCommandLineValueFromKey(key, out string arg) && Enum.TryParse(arg, true, out value);
        }
        
        public static bool TryGetCommandLineValueFromKey(string key, out int value)
        {
            value = -1;
            return TryGetCommandLineValueFromKey(key, out string arg) && int.TryParse(arg, out value);
        }
    }
}
