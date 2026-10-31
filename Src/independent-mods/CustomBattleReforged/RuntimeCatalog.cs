using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.CustomBattle;

namespace CustomBattleReforged
{
    internal static class RuntimeCatalog
    {
        private static Game _game;
        internal static bool Ready { get; private set; }
        internal static bool Active => Ready && ReferenceEquals(_game, Game.Current) && Game.Current?.GameType is CustomGame;
        internal static readonly List<BasicCultureObject> Factions = new List<BasicCultureObject>();
        internal static readonly List<BasicCharacterObject> Commanders = new List<BasicCharacterObject>();
        internal static readonly HashSet<BasicCharacterObject> Soldiers = new HashSet<BasicCharacterObject>();
        internal static readonly Dictionary<BasicCharacterObject, Equipment> BattleEquipment = new Dictionary<BasicCharacterObject, Equipment>();
        internal static readonly Dictionary<string, TextObject> Names = new Dictionary<string, TextObject>(StringComparer.Ordinal);
        internal static readonly Dictionary<string, List<BasicCharacterObject>> CommandersByCulture = new Dictionary<string, List<BasicCharacterObject>>(StringComparer.Ordinal);

        internal static void Clear()
        {
            Ready = false;
            _game = null;
            Factions.Clear(); Commanders.Clear(); Soldiers.Clear(); BattleEquipment.Clear(); Names.Clear(); CommandersByCulture.Clear();
        }

        internal static void Prepare()
        {
            if (!(Game.Current?.GameType is CustomGame) || ReferenceEquals(_game, Game.Current)) return;
            Clear();
            _game = Game.Current;
            try
            {
                CatalogDefinition definition = CatalogDefinition.Load(ModuleHelper.GetModuleFullPath("TAOM"));
                var troops = new Dictionary<string, List<BasicCharacterObject>>(StringComparer.Ordinal);
                var heroes = new Dictionary<string, List<BasicCharacterObject>>(StringComparer.Ordinal);
                foreach (BasicCharacterObject character in _game.ObjectManager.GetObjectTypeList<BasicCharacterObject>())
                {
                    string culture = character.Culture?.StringId;
                    if (!definition.Characters.ContainsKey(character.StringId)) continue;
                    Equipment battle = character.FirstBattleEquipment;
                    if (!definition.Accepts(character.StringId, culture, character.IsObsolete, battle != null && !battle.IsEmpty())) continue;
                    CharacterDefinition entry = definition.Characters[character.StringId];
                    if (entry.Soldier && character.IsSoldier) Add(troops, culture, character);
                    if (entry.Hero && character.IsHero) Add(heroes, culture, character);
                }
                // Sorting is once per menu/game, not per agent, frame, or property getter.
                var cultureIds = new List<string>(definition.CultureNames.Keys);
                cultureIds.Sort(StringComparer.Ordinal);
                foreach (string id in cultureIds)
                {
                    if (!troops.TryGetValue(id, out var soldiers) || soldiers.Count == 0) continue;
                    BasicCultureObject culture = _game.ObjectManager.GetObject<BasicCultureObject>(id);
                    if (culture == null || culture.IsBandit) continue;
                    soldiers.Sort(CompareCharacters);
                    var commanders = heroes.TryGetValue(id, out var candidates) ? candidates : new List<BasicCharacterObject>();
                    commanders.Sort(CompareCharacters);
                    foreach (BasicCharacterObject soldier in soldiers)
                    {
                        if (commanders.Count >= 2) break;
                        if (!commanders.Contains(soldier)) commanders.Add(soldier);
                    }
                    if (commanders.Count < 2) continue; // Both sides must have a valid distinct selection.
                    Factions.Add(culture);
                    Names[id] = new TextObject(definition.CultureNames[id]);
                    CommandersByCulture[id] = commanders;
                    Commanders.AddRange(commanders);
                    foreach (BasicCharacterObject soldier in soldiers) { Soldiers.Add(soldier); BattleEquipment[soldier] = soldier.FirstBattleEquipment; }
                    foreach (BasicCharacterObject commander in commanders) BattleEquipment[commander] = commander.FirstBattleEquipment;
                }
                if (Factions.Count == 0 || Commanders.Count < 2) throw new InvalidOperationException("No equipped TAOM custom-battle roster could be resolved.");
                Ready = true;
                TaleWorlds.Library.Debug.Print($"[Custom Battle Reforged] Ready: {Factions.Count} factions, {Soldiers.Count} soldiers, {Commanders.Count} commanders; {definition.FilesRead} metadata files.");
            }
            catch (Exception error)
            {
                Clear();
                _game = Game.Current; // Do not repeatedly parse/retry the same failed game.
                SubModule.ReportUnavailable(error.Message);
            }
        }

        private static int CompareCharacters(BasicCharacterObject left, BasicCharacterObject right)
        {
            int level = right.Level.CompareTo(left.Level);
            return level != 0 ? level : StringComparer.Ordinal.Compare(left.StringId, right.StringId);
        }

        private static void Add(Dictionary<string, List<BasicCharacterObject>> target, string culture, BasicCharacterObject character)
        {
            if (!target.TryGetValue(culture, out var list)) target[culture] = list = new List<BasicCharacterObject>();
            list.Add(character);
        }
    }
}
