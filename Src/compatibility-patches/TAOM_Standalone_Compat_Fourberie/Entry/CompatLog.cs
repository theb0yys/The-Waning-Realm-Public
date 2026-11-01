using System;
using System.Collections.Generic;
using System.IO;

namespace Fourberie_CompatPatch
{
    internal static class CompatLog
    {
        private const string TestLogPathEnvironmentVariable = "TAOM_FOURBERIE_COMPAT_LOG_PATH";
        private static readonly object Sync = new object();
        private static readonly HashSet<string> EmittedKeys = new HashSet<string>(StringComparer.Ordinal);
        private static readonly string LogPath = ResolveLogPath();

        public static void Info(string message)
        {
            Write(message);
        }

        public static void Once(string key, string message)
        {
            if (string.IsNullOrEmpty(key))
                key = "<empty>";

            lock (Sync)
            {
                if (!EmittedKeys.Add(key))
                    return;
            }

            Write(message);
        }

        private static string ResolveLogPath()
        {
            string overridePath = Environment.GetEnvironmentVariable(TestLogPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
                return overridePath;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Logs",
                "fourberie_compatpatch.log");
        }

        private static void Write(string message)
        {
            try
            {
                string directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(
                    LogPath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
