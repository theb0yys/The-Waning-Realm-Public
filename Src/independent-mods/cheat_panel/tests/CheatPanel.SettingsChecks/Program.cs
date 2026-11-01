using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TAOM_CheatPanel.Runtime;
using TAOM_CheatPanel.Settings;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        _checks++;
    }
    public static void Main(string[] args)
    {
        var settings = new CheatPanelSettings();
        CheatPanelSettings.Instance = settings;
        Check(settings.Id == "TAOM_CheatPanel_Settings" && settings.FolderName == "TAOM_CheatPanel", "Separate settings identity");
        Check(!CheatPanelSettingsAccess.EnablePassiveCheatAdjustments, "New installations must default passive cheats off");
        settings.EnablePassiveCheatAdjustments = true;
        Check(CheatPanelSettingsAccess.EnablePassiveCheatAdjustments, "Explicit enable should work");
        settings.Enabled = false;
        Check(!CheatPanelSettingsAccess.EnablePassiveCheatAdjustments, "Panel master switch must override passive enable");
        settings.Enabled = true;
        foreach (var row in new[] {
            ("DailyGoldStipend", 1000000), ("DailyInfluenceStipend", 5000),
            ("DailyRenownStipend", 10000), ("OwnedSettlementLoyaltyFloor", 100),
            ("OwnedSettlementSecurityFloor", 100), ("OwnedSettlementProsperityFloor", 20000),
            ("DailyOwnedGarrisonReinforcement", 200) })
        {
            var input = typeof(CheatPanelSettings).GetProperty(row.Item1);
            var output = typeof(CheatPanelSettingsAccess).GetProperty(row.Item1);
            input.SetValue(settings, -1);
            Check((int)output.GetValue(null) == 0, row.Item1 + " lower clamp");
            input.SetValue(settings, int.MaxValue);
            Check((int)output.GetValue(null) == row.Item2, row.Item1 + " upper clamp");
        }
        settings.DailyGoldStipend = 750;
        var serializer = new XmlSerializer(typeof(CheatPanelSettings));
        var writer = new StringWriter();
        serializer.Serialize(writer, settings);
        var restored = (CheatPanelSettings)serializer.Deserialize(new StringReader(writer.ToString()));
        Check(restored.DailyGoldStipend == 750 && restored.EnablePassiveCheatAdjustments, "Settings XML round-trip");
        var defaults = (CheatPanelSettings)serializer.Deserialize(new StringReader("<CheatPanelSettings />"));
        Check(!defaults.EnablePassiveCheatAdjustments && defaults.OwnedSettlementLoyaltyFloor == 75, "Missing fields retain safe defaults");
        RecentActionsLog.Clear();
        for (int i = 0; i < 100; i++) RecentActionsLog.Record(i.ToString());
        var history = RecentActionsLog.GetSnapshot();
        Check(history.Count == 50 && history[0].Description == "99" && history[49].Description == "50", "Bounded newest-first history");
        history.Clear();
        Check(RecentActionsLog.GetSnapshot().Count == 50, "Snapshot isolation");
        RecentActionsLog.Clear();
        Log.Reset();
        Log.ErrorOnce("same", new Exception("test"));
        Log.ErrorOnce("same", new Exception("test"));
        Check(RecentActionsLog.GetSnapshot().Count == 1, "Duplicate error suppression");
        if (args.Length != 1) throw new Exception("Pass the built Cheat Panel DLL.");
        var module = Assembly.ReflectionOnlyLoadFrom(Path.GetFullPath(args[0]));
        var names = module.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        Check(names.All(n => n == "mscorlib" || n == "System" || n == "MCMv5" || n == "0Harmony" || n == "SandBox.View" || n.StartsWith("System.", StringComparison.Ordinal) || n.StartsWith("TaleWorlds.", StringComparison.Ordinal)), "Unexpected reference: " + string.Join(", ", names));
        Check(names.Contains("MCMv5") && names.Contains("0Harmony"), "Requested MCM and XP integrations compiled");
        ProgressionChecks.Run(Check);
        HotkeyChecks.Run(Check);
        Console.WriteLine("References: " + string.Join(", ", names));
        Console.WriteLine($"PASS: {_checks} settings, progression simulation, history and compiled-dependency checks; no game runtime.");
    }
}
