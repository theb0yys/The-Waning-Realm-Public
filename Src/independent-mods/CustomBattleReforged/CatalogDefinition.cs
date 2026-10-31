using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CustomBattleReforged
{
    // Metadata only. Equipment and characters remain owned by the loaded TAOM game.
    internal sealed class CatalogDefinition
    {
        internal const int ArmyLimit = 9999;
        internal readonly Dictionary<string, string> CultureNames = new Dictionary<string, string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, CharacterDefinition> Characters = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
        internal int FilesRead { get; private set; }
        private long _bytesRead;

        internal static CatalogDefinition Load(string moduleDirectory)
        {
            var result = new CatalogDefinition();
            var manifest = result.Read(Path.Combine(moduleDirectory, "SubModule.xml"));
            if (manifest.SelectSingleNode("/Module/Id/@value")?.Value != "TAOM" ||
                manifest.SelectSingleNode("/Module/Version/@value")?.Value != "v2.0.29.5")
                throw new InvalidDataException("This build requires TAOM 2.0.29.5.");

            string dataRoot = Path.GetFullPath(Path.Combine(moduleDirectory, "ModuleData")) + Path.DirectorySeparatorChar;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement entry in manifest.SelectNodes("/Module/Xmls/XmlNode"))
            {
                var xml = entry.SelectSingleNode("XmlName") as XmlElement;
                if (xml == null || (xml.GetAttribute("id") != "SPCultures" && xml.GetAttribute("id") != "NPCCharacters")) continue;
                if (entry.SelectSingleNode("IncludedGameTypes") != null && entry.SelectSingleNode("IncludedGameTypes/GameType[@value='CustomGame']") == null) continue;
                string relative = xml.GetAttribute("path");
                string path = Path.GetFullPath(Path.Combine(dataRoot, relative + ".xml"));
                if (!path.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("TAOM XML path leaves ModuleData.");
                if (File.Exists(path) && visited.Add(path)) result.AddDocument(result.Read(path));
                // TAOM preserves vanilla culture IDs through an authored XSLT rename.
                if (xml.GetAttribute("id") == "SPCultures")
                {
                    string transform = Path.ChangeExtension(path, ".xslt");
                    if (File.Exists(transform) && visited.Add(transform)) result.AddCultureTransform(result.Read(transform));
                }
            }
            // Owner-requested Custom Battle exclusion. TAOM names this reused
            // vanilla culture ID Variag; removing eligibility also filters its
            // soldiers and commanders without editing TAOM or campaign data.
            result.CultureNames.Remove("battania");
            if (result.CultureNames.Count == 0 || result.Characters.Count == 0)
                throw new InvalidDataException("TAOM custom battle definitions are missing.");
            return result;
        }

        internal void AddDocument(XmlDocument document)
        {
            foreach (XmlElement culture in document.SelectNodes("/SPCultures/Culture[@is_main_culture='true']"))
                if (culture.GetAttribute("is_bandit") != "true" && culture.HasAttribute("id") && culture.HasAttribute("name"))
                    CultureNames[culture.GetAttribute("id")] = culture.GetAttribute("name");
            foreach (XmlElement character in document.SelectNodes("/NPCCharacters/NPCCharacter"))
            {
                bool soldier = character.GetAttribute("occupation").IndexOf("soldier", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hero = string.Equals(character.GetAttribute("is_hero"), "true", StringComparison.OrdinalIgnoreCase);
                string id = character.GetAttribute("id");
                string culture = character.GetAttribute("culture");
                if (id.Length == 0 || !culture.StartsWith("Culture.", StringComparison.Ordinal)) continue;
                if (!soldier && !hero) continue;
                Characters[id] = new CharacterDefinition(culture.Substring(8), soldier, hero);
            }
        }

        internal void AddCultureTransform(XmlDocument document)
        {
            var ns = new XmlNamespaceManager(document.NameTable);
            ns.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
            foreach (XmlElement template in document.SelectNodes("/xsl:stylesheet/xsl:template", ns))
            {
                string match = template.GetAttribute("match");
                const string prefix = "Culture[@id='";
                if (!match.StartsWith(prefix, StringComparison.Ordinal) || !match.EndsWith("']", StringComparison.Ordinal)) continue;
                string id = match.Substring(prefix.Length, match.Length - prefix.Length - 2);
                var name = template.SelectSingleNode("xsl:copy/xsl:attribute[@name='name']", ns);
                if (id.Length > 0 && name != null && name.InnerText.Length > 0) CultureNames[id] = name.InnerText;
            }
        }

        internal bool Accepts(string id, string culture, bool obsolete, bool hasBattleEquipment)
        {
            return !obsolete && hasBattleEquipment && id != null && culture != null &&
                CultureNames.ContainsKey(culture) && Characters.TryGetValue(id, out var definition) && definition.Culture == culture;
        }

        private XmlDocument Read(string path)
        {
            var size = new FileInfo(path).Length;
            if (size > 16 * 1024 * 1024 || (_bytesRead += size) > 64 * 1024 * 1024 || ++FilesRead > 128)
                throw new InvalidDataException("TAOM custom battle metadata exceeds the supported loading budget.");
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024 }))
            {
                var document = new XmlDocument { XmlResolver = null };
                document.Load(reader);
                return document;
            }
        }
    }

    internal sealed class CharacterDefinition
    {
        internal readonly string Culture;
        internal readonly bool Soldier;
        internal readonly bool Hero;
        internal CharacterDefinition(string culture, bool soldier, bool hero) { Culture = culture; Soldier = soldier; Hero = hero; }
    }
}
