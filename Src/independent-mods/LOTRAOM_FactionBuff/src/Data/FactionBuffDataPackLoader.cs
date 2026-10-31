using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using TaleWorlds.Library;

namespace LOTRAOM.FactionBuff.Data
{
    public static class FactionBuffDataPackLoader
    {
        private const string DataPackFileName = "FactionBuff_DataPacks.xml";
        private static bool _loaded;

        public static int LoadAll()
        {
            if (_loaded)
                return 0;

            _loaded = true;
            int applied = 0;
            foreach (var path in FindDataPackFiles())
            {
                applied += ApplyFile(path);
            }

            if (applied > 0)
            {
                Debug.Print("[LOTRAOM_FactionBuff] Applied " + applied + " culture data-pack profile(s).");
            }

            return applied;
        }

        private static int ApplyFile(string path)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);

                var nodes = doc.SelectNodes("/FactionBuffDataPacks/Culture");
                if (nodes == null)
                    return 0;

                int applied = 0;
                foreach (XmlNode node in nodes)
                {
                    if (ApplyCulture(node))
                        applied++;
                }

                return applied;
            }
            catch (Exception ex)
            {
                Debug.Print("[LOTRAOM_FactionBuff] Data-pack load failed for " + path + ": " + ex.Message);
                return 0;
            }
        }

        private static bool ApplyCulture(XmlNode node)
        {
            string id = FirstAttr(node, "cultureId", "id");
            if (string.IsNullOrWhiteSpace(id))
                return false;

            id = id.Trim();
            string cloneFrom = FirstAttr(node, "cloneFrom", "base");
            if (string.IsNullOrWhiteSpace(cloneFrom))
                cloneFrom = id;

            if (!FactionData.Profiles.TryGetValue(cloneFrom.Trim(), out var source))
                return false;

            Alignment alignment = EnumAttr(node, "alignment", source.Alignment);
            string foodItem = Attr(node, "foodItem");
            if (string.IsNullOrWhiteSpace(foodItem))
                foodItem = source.Food.ItemId;

            var food = new FoodProfile(
                foodItem.Trim(),
                FloatAttr(node, "foodBonus", source.Food.BaseBonus),
                EnumAttr(node, "peakSeason", source.Food.PeakSeason),
                Keys(node, "foodScarce", source.Food.ScarceKeys),
                Keys(node, "foodStocked", source.Food.StockedKeys),
                Keys(node, "foodSurplus", source.Food.SurplusKeys));

            var prisoner = new PrisonerProfile(
                Keys(node, "prisonerHigh", source.Prisoner.HighKeys),
                Keys(node, "prisonerNormal", source.Prisoner.NormalKeys),
                Keys(node, "prisonerLow", source.Prisoner.LowKeys));

            var combined = new CombinedProfile(
                Keys(node, "combinedPeak", source.Combined.PeakKeys),
                Keys(node, "combinedDire", source.Combined.DireKeys));

            var season = new SeasonProfile(
                Keys(node, "seasonSpring", source.Season.SpringKeys),
                Keys(node, "seasonSummer", source.Season.SummerKeys),
                Keys(node, "seasonAutumn", source.Season.AutumnKeys),
                Keys(node, "seasonWinter", source.Season.WinterKeys));

            var streak = new StreakProfile(
                Keys(node, "streakPeak", source.Streak.PeakKeys),
                Keys(node, "streakDire", source.Streak.DireKeys));

            FactionData.Profiles[id] = new CultureProfile(
                id,
                alignment,
                food,
                prisoner,
                combined,
                season,
                streak,
                source.Terrain);

            string extrasFrom = FirstAttr(node, "extrasCloneFrom", "extrasBase");
            if (string.IsNullOrWhiteSpace(extrasFrom))
                extrasFrom = cloneFrom;

            if (FactionExtrasData.Extras.TryGetValue(extrasFrom.Trim(), out var extras))
                FactionExtrasData.Extras[id] = extras;

            return true;
        }

        private static IEnumerable<string> FindDataPackFiles()
        {
            string? moduleRoot = GetModuleRoot();
            if (string.IsNullOrEmpty(moduleRoot))
                yield break;

            string moduleData = Path.Combine(moduleRoot, "ModuleData");
            string directFile = Path.Combine(moduleData, DataPackFileName);
            if (File.Exists(directFile))
                yield return directFile;

            string packDir = Path.Combine(moduleData, "FactionBuff");
            if (!Directory.Exists(packDir))
                yield break;

            foreach (var file in Directory.GetFiles(packDir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }

        private static string? GetModuleRoot()
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string? binDir = Path.GetDirectoryName(assemblyPath);
                string? clientDir = binDir == null ? null : Directory.GetParent(binDir)?.FullName;
                return clientDir == null ? null : Directory.GetParent(clientDir)?.FullName;
            }
            catch
            {
                return null;
            }
        }

        private static string[] Keys(XmlNode node, string bucket, string[] fallback)
        {
            var keyNode = node.SelectSingleNode("Keys[@bucket='" + bucket + "']") ??
                          node.SelectSingleNode("Keys[@name='" + bucket + "']");
            if (keyNode == null || string.IsNullOrWhiteSpace(keyNode.InnerText))
                return fallback;

            var parts = keyNode.InnerText.Split(new[] { ',', ';', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var keys = new List<string>();
            foreach (var part in parts)
            {
                string value = part.Trim();
                if (value.Length > 0)
                    keys.Add(value);
            }

            return keys.Count == 0 ? fallback : keys.ToArray();
        }

        private static string FirstAttr(XmlNode node, string first, string second)
        {
            string value = Attr(node, first);
            return string.IsNullOrWhiteSpace(value) ? Attr(node, second) : value;
        }

        private static string Attr(XmlNode node, string name)
        {
            return node.Attributes?[name]?.Value ?? string.Empty;
        }

        private static float FloatAttr(XmlNode node, string name, float fallback)
        {
            string value = Attr(node, name);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        private static T EnumAttr<T>(XmlNode node, string name, T fallback) where T : struct
        {
            string value = Attr(node, name);
            return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
        }
    }
}
