using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TAOM_CheatPanel.Runtime
{
    internal sealed class RecentActionEntry
    {
        public string Timestamp { get; set; }
        public string Description { get; set; }
    }

    internal static class RecentActionsLog
    {
        private static readonly List<RecentActionEntry> Entries = new List<RecentActionEntry>();
        public static void Record(string message)
        {
            lock (Entries)
            {
                Entries.Insert(0, new RecentActionEntry { Timestamp = DateTime.Now.ToString("HH:mm:ss"), Description = message ?? string.Empty });
                if (Entries.Count > 50) Entries.RemoveAt(Entries.Count - 1);
            }
        }
        public static List<RecentActionEntry> GetSnapshot() { lock (Entries) return new List<RecentActionEntry>(Entries); }
        public static void Clear() { lock (Entries) Entries.Clear(); }
    }

    internal static class Log
    {
        private static readonly HashSet<string> Errors = new HashSet<string>(StringComparer.Ordinal);
        public static void Info(string message) { Trace.WriteLine("[TAOM Cheat Panel] " + message); }
        public static void Warn(string message) { Info(message); }
        public static void Reset() { lock (Errors) Errors.Clear(); }
        public static void ErrorOnce(string key, Exception error)
        {
            lock (Errors)
            {
                if (Errors.Count >= 128 || !Errors.Add(key ?? string.Empty)) return;
            }
            RecentActionsLog.Record("Error: " + (error == null ? key : error.Message));
            Info(key + ": " + error);
        }
    }
}
