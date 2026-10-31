using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TAOM_ContextualMessages
{
    /// <summary>
    /// All contextual message line pools. Static arrays, random selection with
    /// last-used avoidance. All text is Middle-earth / Tales from the Age of Men lore-appropriate.
    ///
    /// EXPANSION v2 — pools roughly tripled, faction-specific sets added for Gondor, Rohan,
    /// Elves, Dwarves, and the Shire; new pools added for expanded event hooks
    /// (tournaments, companions, caravans, weather, bandit hideouts, births, marriages).
    /// </summary>
    public static class MessageLibrary
    {
        private static readonly System.Collections.Generic.Dictionary<int, int> _lastPicked
            = new System.Collections.Generic.Dictionary<int, int>();
        private static readonly Dictionary<int, string> _poolNames =
            new Dictionary<int, string>();
        private static readonly Dictionary<string, List<string>> _extraLines =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static bool _dataPacksLoaded;

        static MessageLibrary()
        {
            RegisterPools();
        }

        /// <summary>Pick a random line, avoiding the most recently used one.</summary>
        public static string Pick(string[] pool)
        {
            EnsureDataPacksLoaded();
            if (pool == null || pool.Length == 0) return "";
            int key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pool);
            List<string> extras = TryGetExtraLines(key);
            int total = pool.Length + (extras?.Count ?? 0);
            if (total <= 0) return "";
            if (total == 1) return pool.Length == 1 ? pool[0] : extras[0];

            int last = _lastPicked.ContainsKey(key) ? _lastPicked[key] : -1;
            int idx;
            int tries = 0;
            do { idx = MBRandom.RandomInt(0, total); tries++; }
            while (idx == last && tries < 10);
            _lastPicked[key] = idx;
            return idx < pool.Length ? pool[idx] : extras[idx - pool.Length];
        }

        private static void RegisterPools()
        {
            var fields = typeof(MessageLibrary).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(string[]))
                    continue;

                var pool = field.GetValue(null) as string[];
                if (pool == null)
                    continue;

                int key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pool);
                _poolNames[key] = field.Name;
            }
        }

        private static List<string> TryGetExtraLines(int poolKey)
        {
            string poolName;
            if (!_poolNames.TryGetValue(poolKey, out poolName))
                return null;

            List<string> lines;
            return _extraLines.TryGetValue(poolName, out lines) ? lines : null;
        }

        private static void EnsureDataPacksLoaded()
        {
            if (_dataPacksLoaded)
                return;

            _dataPacksLoaded = true;
            int loaded = 0;
            foreach (var path in FindDataPackFiles())
            {
                loaded += LoadDataPack(path);
            }

            if (loaded > 0)
            {
                Debug.Print("[ContextualMessages] Loaded " + loaded + " data-pack message line(s).");
            }
        }

        private static int LoadDataPack(string path)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);

                var nodes = doc.SelectNodes("/ContextualMessageDataPacks/Pool");
                if (nodes == null)
                    return 0;

                int loaded = 0;
                foreach (XmlNode poolNode in nodes)
                {
                    string name = poolNode.Attributes?["name"]?.Value;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    List<string> lines;
                    if (!_extraLines.TryGetValue(name.Trim(), out lines))
                    {
                        lines = new List<string>();
                        _extraLines[name.Trim()] = lines;
                    }

                    foreach (XmlNode lineNode in poolNode.SelectNodes("Line|Message"))
                    {
                        string text = lineNode.InnerText?.Trim();
                        if (string.IsNullOrEmpty(text))
                            continue;

                        lines.Add(text);
                        loaded++;
                    }
                }

                return loaded;
            }
            catch (Exception ex)
            {
                Debug.Print("[ContextualMessages] Data-pack load failed for " + path + ": " + ex.Message);
                return 0;
            }
        }

        private static IEnumerable<string> FindDataPackFiles()
        {
            string moduleRoot = GetModuleRoot();
            if (string.IsNullOrEmpty(moduleRoot))
                yield break;

            string moduleData = Path.Combine(moduleRoot, "ModuleData");
            string directFile = Path.Combine(moduleData, "ContextualMessages_DataPacks.xml");
            if (File.Exists(directFile))
                yield return directFile;

            string packDir = Path.Combine(moduleData, "ContextualMessages");
            if (!Directory.Exists(packDir))
                yield break;

            foreach (var file in Directory.GetFiles(packDir, "*.xml", SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }

        private static string GetModuleRoot()
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string binDir = Path.GetDirectoryName(assemblyPath);
                string clientDir = binDir == null ? null : Directory.GetParent(binDir)?.FullName;
                return clientDir == null ? null : Directory.GetParent(clientDir)?.FullName;
            }
            catch
            {
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CAMPAIGN LOAD - CULTURE SIGNALS
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] CampaignLoadMood = {
            "The campaign record opens. Scouts report in, captains argue over maps, and the road chooses its first problem.",
            "The day's orders are fresh. The old troubles have not become more polite overnight.",
            "Campfires are being kicked out across the map. Another day begins with mud on its boots.",
            "Messengers are already late somewhere. That is how campaigns announce themselves.",
            "Supplies, scouts, morale, weather: the quiet forces begin their count before the first blade leaves its sheath.",
            "The map is awake again. It has opinions.",
            "A good campaign begins by listening before moving. The reports are ready.",
            "Somewhere, a quartermaster is right and a captain is ignoring them. History has range.",
            "The road has reopened under your command. Expect it to ask for payment.",
            "There is always a first choice after loading the campaign. Make it deliberately.",
        };

        public static readonly string[] CampaignLoadUnknownCulture = {
            "The realm stirs around your banner. Culture, road, and danger will declare themselves soon enough.",
            "Your company wakes to a world already in motion. Read the signals before the signals become casualties.",
        };

        public static readonly string[] CampaignLoadGondor = {
            "Gondor wakes with its helms on the wall and its ledgers open. The day will ask for both courage and order.",
            "From white stone to river road, Gondor's captains count the hours before the next summons.",
        };

        public static readonly string[] CampaignLoadRohan = {
            "In Rohan, a horse saddled before sunrise is almost a promise. The plains are listening.",
            "The riders of Rohan measure trouble by distance, wind, and how quickly a horn can carry.",
        };

        public static readonly string[] CampaignLoadRivendell = {
            "Rivendell's bells do not ring loudly, but their counsel travels farther than iron.",
            "The hidden valley wakes quietly. Old maps are unrolled, and old debts remember their names.",
        };

        public static readonly string[] CampaignLoadLothlorien = {
            "Lothlorien keeps its watch beneath golden leaves. Every footstep has a memory here.",
            "The Golden Wood stirs without haste. The border hears what the road has not yet said.",
        };

        public static readonly string[] CampaignLoadMirkwood = {
            "Mirkwood wakes under branches that make daylight earn its way in. Keep scouts close.",
            "The forest roads of Mirkwood are never just roads. They are questions with shadows on both sides.",
        };

        public static readonly string[] CampaignLoadErebor = {
            "Erebor's forges answer the morning before the court does. Steel has a language of its own.",
            "Under the Mountain, every gate report becomes part of a deeper accounting.",
        };

        public static readonly string[] CampaignLoadDale = {
            "Dale opens its markets with one eye on the road and one on the old scars of war.",
            "The bells of Dale carry across trade streets and training yards alike. Prosperity needs guards.",
        };

        public static readonly string[] CampaignLoadShire = {
            "The Shire starts with breakfast, gossip, and fences that would prefer the world stay sensible.",
            "Quiet fields do not mean empty courage. The Shire keeps its strength tucked behind ordinary doors.",
        };

        public static readonly string[] CampaignLoadMordor = {
            "Mordor does not sleep so much as wait. Ash settles, orders move, and fear is made practical.",
            "The black roads stir with marching feet. Mordor counts obedience before it counts lives.",
        };

        public static readonly string[] CampaignLoadIsengard = {
            "Isengard begins the day by measuring what can be cut, forged, moved, or broken.",
            "The ring of hammers at Isengard is not industry alone. It is intention given a rhythm.",
        };

        public static readonly string[] CampaignLoadGundabad = {
            "Gundabad wakes in the cold dark with old grudges sharpened and ready.",
            "From the northern deeps, Gundabad sends out scouts who know hunger as a map.",
        };

        public static readonly string[] CampaignLoadDolGuldur = {
            "Dol Guldur's shadows move like servants with orders. Light should be treated as contested ground.",
            "The hill of sorcery keeps poor company. Even silence there feels assigned to a purpose.",
        };

        public static readonly string[] CampaignLoadUmbar = {
            "Umbar reads the wind before the ink dries on any order. Sails are policy made visible.",
            "Corsair captains wake to tide tables, grudges, and the price of fear on foreign coasts.",
        };

        public static readonly string[] CampaignLoadHarad = {
            "Harad's banners lift in dry heat. The long road south remembers every bargain and betrayal.",
            "The host of Harad measures distance in water, shade, and discipline. Waste none of them.",
        };

        public static readonly string[] CampaignLoadRhun = {
            "Rhun's riders follow horizons that make small maps look arrogant.",
            "In the east, a muster can begin as a rumor and arrive as a storm before noon.",
        };

        public static readonly string[] CampaignLoadDunland = {
            "Dunland wakes with hill smoke, hard bread, and grievances old enough to have grandchildren.",
            "The clans of Dunland do not forget a slight. They just wait for better footing.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY (generic)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWin = {
            "The field is yours. The enemy breaks and flees into the dark.",
            "Victory! The banner holds. Let the enemy carry word of this defeat back to their masters.",
            "They came in great numbers and still they could not stand. The line held.",
            "The battle is won. Count your men and tend the wounded — the road does not end here.",
            "Another host scattered. For a time, the shadow retreats.",
            "Your company fought with honour. The dead lie thick upon the field.",
            "The enemy is routed. This victory will be remembered in the halls of your allies.",
            "You have driven them back. But do not mistake a battle won for a war ended.",
            "The foe breaks before you. Even darkness must yield when courage stands against it.",
            "A hard fight, but the day is yours. The will of the Free Peoples endures.",
            "Well struck, captain. The enemy leaves their dead behind and asks for no quarter given.",
            "The tide turns. Those who fled will spread fear among those who sent them.",
            "A clean victory — swift and decisive. The realm is safer for it this day.",
            "Your standard is still flying. Today, that is enough.",
            "The enemy came with certainty of conquest. They leave with neither treasure nor pride.",
            "Fortune favoured the resolute. Your soldiers' courage carried the day.",
            "The clash of battle fades. Smoke drifts across a field that is yours to hold.",
            "Even the bravest enemy has limits. Yours found theirs today.",
            "The grass will remember this fight long after the names are forgotten.",
            "They will need to find more soldiers before they trouble you again.",
            "A testament to your command. The enemy expected weakness and found iron.",
            "The line held where others might have broken. This is what separates armies from mobs.",
            "One more victory to mark on the long march of this war.",
            "Their retreat echoes in the hills. Let it carry a warning far and wide.",
            "Blood and dust — but the outcome is not in doubt. The field is yours.",
            // --- new lines ---
            "The enemy's charge broke on your shield wall like water on rock. A good day's work.",
            "They fought well enough. Not well enough. The field says so plainly.",
            "Your captains deserve the credit for this. Pass it along before the moment passes.",
            "A battle won cleanly is a gift. Most are messier. Savour this one.",
            "The warhorses are still breathing hard, but the outcome was never really in doubt.",
            "Smoke, screams, and then silence — the particular silence that means victory.",
            "They broke earlier than expected. Either your reputation precedes you, or their morale was already fragile.",
            "A swift and decisive action. The scribes will make it sound more dramatic. It was already dramatic enough.",
            "The last of them scatter into the tree-line. Pursuit is up to you.",
            "Some battles are won in the first charge. This was one of them.",
            "The enemy left their supply train when they ran. Check it before you move on.",
            "A field held, a force broken, a road kept open. Three victories in one.",
            "The fighting spirit of your company grows with each victory. Build on this.",
            "Their commander fled before the last of the line broke. That tells you something useful.",
            "Carry the wounded. Leave the enemy theirs — they can collect them under truce if they dare come back.",
            "One more name to add to the tally of won fields. The tally is growing.",
            "The enemies of the Free Peoples fight in many shapes. Today's shape was breakable.",
            "The ranks held and the enemy did not. That is the whole story.",
            "Well fought. The kind of victory that costs little and teaches both sides something.",
            "Whatever sent them against you today will have to answer for the result.",
            "A reminder that even in dark ages, light-bearers can prevail. You prevailed.",
            "The field is quiet now. It will not stay quiet — but for this hour, savour the stillness.",
            "Your soldiers will speak of this one around fires for a while. Let them.",
            "Even the most disciplined enemy can be broken if you find the right pressure. You found it.",
            "The messenger who carries news of this defeat will not be welcomed warmly.",
            // ── expansion ──
            "The last of them scatter before the sun is fully down. A good day's accounting.",
            "When the charge broke, everything else followed. Clean and decisive.",
            "Victory — not the heroic kind, just the thorough kind. Both count.",
            "The field is yours. So is the work of what to do with it.",
            "They came for a fight and found a rout. Those are different things, and they know it now.",
            "The company held its formation when others would have bent. That is why you won.",
            "Outnumbered, perhaps. Outfought, no.",
            "The enemy's dead are on your side of the line. That is the relevant geography.",
            "Three hours of fighting and the issue was never really in doubt after the first half-hour.",
            "Rout them thoroughly enough and they won't reassemble before you are ready for them again.",
            "Your flanks held. That is what decided it.",
            "A decisive engagement. The campaign is measurably shorter for it.",
            "Whatever they expected to find here, they found something harder.",
            "They had numbers. You had position, timing, and a company that does not panic.",
            "The field is silent now except for the wind. Add this one to the ledger and keep moving.",
            "Not every victory teaches you something. This one did. Remember it.",
            "The enemy's rear collapsed before their front knew the centre had broken. That is what coordination looks like.",
            "A well-executed battle. Credit the sergeants as much as the plan.",
            "They will come back. Not today, and not with the same assumptions.",
            "The fight was harder than it looked from the command position. Ask the infantry about it.",
            "Victory bought at a price your enemies think they can afford to pay next time. Make sure they cannot.",
            "The company deserves to know what they did today was well done. Tell them.",
            "The road ahead opened the moment the last of them stopped running.",
            "Even a small advantage pressed early enough becomes a decisive one. You pressed it.",
            "The battle is done. Begin counting what it cost before pride inflates the number.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — DEFEAT
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleLoss = {
            "The line breaks. Fall back — regroup — there is still a tomorrow to fight for.",
            "This day belongs to the enemy. Learn from it. Do not let them have the next.",
            "Defeat stings, but it need not be the final word. Survivors can still turn the tide.",
            "They were too many, or you were too few. Either way, the ground behind you must hold.",
            "A dark hour. But even the Ents once stood still until the moment came to move.",
            "The cost was high and the ground is lost. Rally what remains and find a defensible place.",
            "The enemy's strength is greater than counted. This intelligence must reach your allies.",
            "There is no shame in retreat when it preserves the force that will fight another day.",
            "You could not hold them here. Perhaps elsewhere — perhaps tomorrow — the answer lies.",
            "The men are shaken. They will need rest, reassurance, and a commander who does not despair.",
            "Not every fight can be won. Some must be survived. You survived this one.",
            "The shadow gains ground today. Tomorrow it will not be unopposed.",
            "Your lines bent and broke. Mend both before the next engagement.",
            "A bitter outcome. The enemy is emboldened — expect them to press harder.",
            "The field is theirs. Your company's survival is the only victory you can claim today.",
            "Even the greatest captains have known this feeling. It is what you do with it that matters.",
            "The retreat costs you ground and morale. Spend what gold and time you can on both.",
            "Some battles cannot be won with courage alone. Today the numbers were not in your favour.",
            "A wound to your force and a wound to your pride. Tend both before pressing on.",
            "When the darkness seems greatest, it is sometimes only waiting for you to stand again.",
            "The enemy cheers on the field you abandoned. Do not let them cheer long.",
            "This loss will echo. Find allies, find resources, find the will to come back stronger.",
            "Even Helm Hammerhand was besieged in his own keep before the tide turned.",
            "The road ahead is harder now. But it remains a road, and you can still walk it.",
            "Loss is the price of hubris — or of necessity. Today, perhaps it was the latter.",
            // --- new lines ---
            "The retreat was orderly. That is the only grace note in an otherwise poor day.",
            "Assess what went wrong before you assess the enemy's strength. One you can change.",
            "Defeat here does not end the campaign. It merely slows it. Move accordingly.",
            "The dead cannot be brought back. But lessons can be learned from the way they fell.",
            "A harder enemy than anticipated. Update your estimates and plan accordingly.",
            "The soldiers know when a fight was close and when it was not. This one was not close. Address it.",
            "Regroup at the nearest defensible position and count what remains. Then decide on next steps.",
            "A setback. Not a catastrophe. The line between the two is whether you act quickly enough.",
            "The campaign absorbs losses when they are managed. The question is how you manage this one.",
            "Pride is expensive. Today's bill has been delivered. Pay it and move forward.",
            "The enemy pressed an advantage. Find their disadvantage and press it back.",
            "There are battles that look like defeats from inside them and victories from outside. This was not one of those.",
            "Every army loses sometimes. Great armies lose and come back. That is the standard to hold yourself to.",
            "Your soldiers are looking to you. What they see in your face right now shapes what happens next.",
            "Withdraw, tend the wounded, and do not make decisions about strategy while the blood is still drying.",
            "A defeat absorbed thoughtfully is preparation for the next engagement. Do the thinking.",
            "The enemy is stronger than they looked before. They always are, once you have fought them once.",
            "What was lost in this battle can be retaken. What was learned cannot be unlearned.",
            "A day to forget, insofar as forgetting battles is possible. Then a day to act on what it taught you.",
            "The army survives. The mission survives. The war goes on. So do you.",
            "Sauron's forces broke many greater hosts before and after they broke yours. The breaking need not be permanent.",
            "Ground can be retaken. Dead soldiers cannot be replaced. Preserve the latter and the former becomes possible.",
            "Do not despair before nightfall. Sleep first. Strategic clarity requires rest.",
            "The hardest battle is the one fought against your own impulse to press on when retreat is wiser.",
            "Another lesson from the longest and cruelest teacher in Middle-earth: war itself.",
            // ── expansion ──
            "The defeat is real. The question is what is done with it.",
            "Every captain who has led armies long enough has a defeat like this one. Most of them recovered.",
            "Retreat with the wounded. Do not count anyone else as lost yet.",
            "The line broke at the flank. That is something that can be fixed. Fix it.",
            "The enemy won today. That gives them information about you. Use the reverse of it.",
            "A defeat that could have been worse if the withdrawal had not been managed well.",
            "The cost was high. The lesson was expensive. Do not waste either.",
            "Recover the instruments of command before the instruments of war. Decision-making capacity first.",
            "Hold your ground long enough to call it a fighting retreat, not a rout. There is a difference.",
            "Your soldiers need to see composure from the top before they can find it in themselves.",
            "The campaign is not over. The battle was. Two different endings.",
            "The enemy pressed an advantage. Find their next advantage before they do.",
            "A defeat on a bad field. Choose better ground next time and the equation changes.",
            "Count the wounded before deciding anything else. The decision should be made with real numbers.",
            "The moment will come when this defeat is the thing you pointed to and said: that is where it turned.",
            "Not all defeats are equal. Some are catastrophes; some are corrections. Determine which this is.",
            "Every soldier who got off that field alive is a soldier who can fight the next engagement.",
            "The enemy has momentum now. You need to either arrest it or redirect it.",
            "Spare no grief on the ground lost. Spare it only on the people.",
            "The measure of this defeat will be taken in what follows it.",
            "They were better positioned, better supplied, or better commanded. Find which one and address it.",
            "Withdraw in order. Disorder in retreat costs more than disorder in advance.",
            "A loss that will haunt the next council meeting. Do not let that haunt the next field decision.",
            "The enemy will interpret this as a signal about your capability. Correct that interpretation quickly.",
            "Whatever failed today — timing, supply, intelligence, nerve — find it and solve it before the next engagement.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs MORDOR
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinMordor = {
            "The Orcs of the Dark Lord scatter before your banners. The Eye sees you — and blinks.",
            "Mordor's host is broken. Even fell creatures bred for war have learned to fear you.",
            "The dark flame is quenched on this field. Sauron's reach has limits — you found one.",
            "Uruks and wargs and nameless things, driven back by steel and stubborn will.",
            "The servants of the Shadow flee. Let them report this defeat to the Tower of Barad-dûr.",
            "You have driven the darkness back a few steps. It will return. But today, it retreats.",
            "A victory over the Dark Lord's legions. The Free Peoples of Middle-earth stand witness.",
            "Even the iron will of Mordor yields to a blade in a courageous hand. Today proved it.",
            "The black banners are torn and trampled in the mud. A worthy sight.",
            "Sauron sends many and counts the losses lightly. Today he must count a few more.",
            "His armies are vast — but they are not unbeatable. You have proved that again.",
            "The Orcs howl as they run. Let them carry their fear back through the passes of Mordor.",
            "No fortress is built that cannot be broken. No army raised that cannot be beaten.",
            "The Eye of Sauron is fixed on many things at once. Today it blinked.",
            "Darkness was never permanent — only patient. Your patience outlasted his today.",
            // --- new lines ---
            "The Nazgûl are not here today. On the fields where they are absent, courage is still enough.",
            "The black gate is far from here. But every Orc you scatter is one fewer thing it can send.",
            "Mordor breeds soldiers the way the smith breeds iron: in fire, in great quantities, and without care for which ones survive.",
            "You have broken a wave of the dark tide. The tide will roll again. Be ready.",
            "Not the last of Sauron's armies. Not by a long way. But today, one less host to worry about.",
            "The Orcs fear flame, and rout, and a line that does not break. Give them all three whenever possible.",
            "Sauron does not mourn his losses. That is both his strength and, in the long run, his weakness.",
            "The stench of Mordor's host lingers even after the fighting is done. Victory over it never smells clean.",
            "Fell beasts and dark iron and the will of an ancient evil — and still they broke and ran. Good.",
            "A victory that will be noted in Rivendell, in Minas Tirith, in every hall where the Shadow is taken seriously.",
            "The servants of the Eye are numerous. They are not, it turns out, innumerable. Keep testing that.",
            "Even creatures bred to know nothing but conquest can learn what it feels like to lose. You taught them today.",
            "Mordor's supply of willing soldiers is vast — but every host broken makes the next one slightly more reluctant.",
            "Let the Orc-runners carry the news back. Fear of you is a weapon that costs nothing to resupply.",
            "The darkness retreats one field at a time. This is one more field reclaimed for the living.",
            "Orcs in daylight fight at a disadvantage. They know it. Force every engagement at noon if you can.",
            "Barad-dûr stands far to the East. But its soldiers fall right here, and that is where the war is won.",
            "The Eye sees all — except, apparently, its own defeat coming.",
            "Mordor's captains are not fools. They will change tactics. So must you.",
            "A victory over the Shadow, which is also a message to every village that wondered if resistance was possible: it is.",
            // ── expansion ──
            "The Eye is watching, but watching is not winning. It watched this defeat happen and did nothing.",
            "Mordor's soldiers do not mourn their dead. That means they do not learn from them either. You can.",
            "Dark iron, dark breeding, dark purpose — and still they fell. Take that as data.",
            "The servants of the Shadow fight to win or die. Today they did the latter.",
            "Mordor's armies are shaped by fear from above and hatred from within. That combination can break.",
            "You broke a piece of Sauron's war machine. He will replace it. Break the replacement too.",
            "The dark host is scattered. Their scouts will have better information next time. Make sure you do too.",
            "The Eye does not need to see everything to send more armies. But it needs to send fewer after days like today.",
            "A victory over Mordor is also a message to every wavering settlement watching: resistance is possible.",
            "The black host lies broken. The ground is yours and the road it commanded is now a free road.",
            "Fell creatures fall when met with sufficient courage and sufficient preparation. Both were present today.",
            "The servants of the Dark Lord are numerous and renewed. What they are not is unconquerable.",
            "A victory that the histories will record, if the histories survive to be written.",
            "Mordor does not feel pride. It does not feel shame. What it feels is impediment, and today you were that.",
            "The dark tide rolled in and you were the shore. It broke. The shore remains.",
            "More of the Enemy's host lies on this field than marched away from it. That proportion matters.",
            "Sauron's forces obey through fear. A fear-obedient army breaks when the fear fails. Find its failing points.",
            "A battle won against Mordor announces itself beyond this field, through every settlement in reach.",
            "The servants of the Eye are not soldiers in the way your company is soldiers. That distinction can be used.",
            "A victory against the darkest force in the world. The scale of the cause is visible from this field.",
            "You met the face of ancient evil in the field today and it was not enough to defeat you.",
            "Whatever Sauron sends next will know what happened here. That knowledge changes what they think you are.",
            "The dark army is broken. Some things that break cannot be repaired before the next engagement. Find out if this is one.",
            "A victory that each settlement in this region will hear of before nightfall. Let them hear it clearly.",
            "The shadow recoils. Not permanently. But enough.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — DEFEAT vs MORDOR
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleLossMordor = {
            "Mordor's strength is not mere numbers — it is will, and shadow, and ancient malice. Respect it.",
            "The Dark Lord's servants are bred for war. Today, that breeding tells. Regroup and prepare.",
            "A dark tide overwhelmed your lines. The shadow presses ever forward — so must you.",
            "You have felt the weight of Mordor today. Do not let it become familiar.",
            "They drove you back with flame and fury. The darkness grows bolder with each gain.",
            "Sauron's armies have broken many greater hosts. Your survival is something to build on.",
            "Defeat before Mordor is not an ending — unless you let it be. Stand again.",
            "The shadow gains a footing here. It must be contested before it becomes a fortress.",
            "Dark wings beat overhead. Your cause is not lost, but it has grown harder today.",
            "Even Gandalf fled from Mordor's servants in his time. Wisdom knows when to withdraw.",
            "The Eye sees your defeat and is pleased. Deny it that pleasure — recover and strike back.",
            "What Mordor takes, it holds with iron. You must be harder than iron to take it back.",
            // --- new lines ---
            "The creatures of the Dark Lord do not tire as men do. That is the lesson of today. Plan for it.",
            "Fell weapons and ancient hatred drove you from the field. Neither is a surprise. Both must be prepared for.",
            "Mordor does not need to be defeated everywhere. It needs to be defeated where you stand. You did not manage it today.",
            "The shadow lands another foothold. Every foothold is a staging point for something worse. Deny them the next one.",
            "You fought against the will of an age. Today it was stronger. Find where it is weaker.",
            "Sauron's forces fight without doubt or hesitation. Fear does that to an army — from inside as much as from outside.",
            "A costly defeat. Count the survivors and begin the task of understanding how it happened before it happens again.",
            "The black tide advances. It has been advancing for longer than you have been fighting it. Do not let that discourage you.",
            "Retreat was the right choice. A routed army serves no one. An army that escapes can fight again.",
            "Mordor's host did not pursue hard. That is strange. Find out why before you assume good fortune.",
            "The darkness wins today. The morning will look different — not better, but different enough to plan from.",
            "These things took the field against you: ancient malice, unceasing numbers, and no care for the fallen. Remember all three.",
            "What Sauron breaks, he does not rebuild — he does not need to. There is always more. Deny him the pattern of victory.",
            "A day to take seriously. Not to despair over — seriously. There is a difference.",
            "The servants of the Eye advanced past your position. Take it back before they have time to fortify it.",
            "Even the Rohirrim broke before the Witch-king. It is not dishonour to be beaten. It is dishonour to stay beaten.",
            // ── expansion ──
            "A defeat before the servants of the Dark Lord is not the end. The end is when you stop getting up.",
            "Mordor won today because it always wins eventually if you do not find the specific way to stop it.",
            "The shadow presses its advantage. Stop it from pressing a second one in the same direction.",
            "The Enemy's host did not break and run. They advanced, methodically. That tells you something.",
            "You felt what it means to face something that does not tire and does not despair. Prepare for the next encounter.",
            "The darkness gained ground. Every step it keeps is a step your force must eventually retake at greater cost.",
            "Retreat was right. A shattered force serves no one. This force is not shattered.",
            "The fell captain who led them today had advantages you did not. Identify them before the next engagement.",
            "Mordor's best against your company in imperfect conditions. A defeat in these circumstances is a lesson, not a verdict.",
            "Count your dead and count what they taught you. Both numbers need to be used.",
            "The servants of the Eye pressed until you gave ground. Do not give them the same ground twice.",
            "A dark defeat. The kind that reaches you in the chest before it reaches you in the strategy.",
            "Withdraw to ground you can hold, not ground you happen to be on. Defensible is a specific word.",
            "The enemy's force was larger than reported. Fix your intelligence sources before the next contact.",
            "Even the bravest soldiers can be overwhelmed by sheer weight of ancient evil pressing from one direction.",
            "The darkness won this field. It will not win the next if you learn the correct lessons.",
            "Sauron does not count his dead. Count yours and understand what each death bought.",
            "A retreat well executed is a position preserved. A rout throws away both.",
            "The black banners stand where yours were. Reclaim them before they become a permanent fixture.",
            "There is no shame in being beaten by Mordor's finest. There is shame in failing to learn from it.",
            "Mordor beat you today in a specific way. Understanding that specificity is what makes the difference next time.",
            "The retreat buys time. Time buys preparation. Preparation is the only thing that changes the outcome.",
            "The shadow does not need to be defeated everywhere at once. Only where you are standing. Stand better.",
            "This loss feeds the shadow's confidence. The next victory starves it.",
            "A defeat against Mordor is one more entry in a very long ledger. The ledger can still close in your favour.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs ISENGARD / URUK-HAI
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinIsengard = {
            "Saruman's Uruk-hai lie broken. The White Hand is crossed out on the field today.",
            "The machines of Orthanc are formidable — but they are not the equal of free men with purpose.",
            "Isengard's soldiers fall. Somewhere in his tower, a wizard counts the cost of his ambition.",
            "The Uruk-hai came swiftly and in good order. You broke their order. That broke them.",
            "The White Hand bleeds today. Saruman's supply of unbreakable soldiers is not infinite.",
            "Bred for war and yet defeated by it. Isengard's finest proved less than their reputation.",
            "A victory over Orthanc's best. Even treachery and cunning have their limits.",
            "The Uruk drums fall silent on this field. A sound you will not weary of.",
            "Saruman promised his masters an unstoppable force. You stopped it.",
            "The fires of Isengard built these soldiers. The fires of battle have broken them.",
            "Another blow against the White Hand. Hammer long enough at a wall and it falls.",
            "Well done. Saruman's hosts are mighty, but pride has always been his weakness — and ours to exploit.",
            // --- new lines ---
            "Orthanc breeds its soldiers fast and hard. Today that was not fast or hard enough.",
            "The White Hand — torn now, bloodied, and driven from this field.",
            "Saruman's industrial war machine requires results. You have denied it one today.",
            "The Uruk-hai do not fear pain the way men do. But they fear routing just the same.",
            "Isengard's science of war is formidable. Your art of war proved the superior thing today.",
            "A wizard who breaks from the order of his kind sets a limit on the friends he can call on. Saruman's limit shows.",
            "The White Hand's reach extended here. You cut it back. Keep cutting.",
            "Bred without mercy and defeated without it — that is the Uruk-hai's lot on this field.",
            "Each Uruk-hai that falls is one fewer product of Saruman's foul process. That is good arithmetic.",
            "The crossbowmen of Isengard are the most dangerous element of their line. That you overcame them speaks well.",
            "Their war-drums beat in the morning. They do not beat now. Note the difference.",
            "Saruman coveted what others built. Today he finds that what he built does not measure up.",
            "The Uruk-hai hold ground with discipline. It takes discipline to break them. Yours was sufficient.",
            "Treachery and manufacture are Saruman's tools of war. Steel and courage are yours. Yours won today.",
            // ── expansion ──
            "Saruman's finest, and they still came apart under sufficient pressure. That is useful to know.",
            "The Uruk-hai expected a broken line. They found an intact one. The surprise worked in your favour.",
            "Isengard breeds soldiers, not commanders. Their tactical rigidity showed today.",
            "The White Hand is crossed out in the mud of this field.",
            "An Uruk-hai unit that has never been defeated before was defeated today. The precedent is set.",
            "Saruman's army fights without honour but with considerable efficiency. Defeating it requires matching the efficiency.",
            "A victory over Orthanc's creations. They are formidable enough that this is worth acknowledging.",
            "The Uruk-hai retreat in better order than most. They will be back in better formation. Prepare.",
            "Isengard's losses are replenished from the breeding pits. Your losses are not. Win efficiently.",
            "The White Hand bleeds. Saruman will blame the commander of this host. That is his way.",
            "Orthanc's soldiers carry Saruman's contempt for the free peoples. Today that contempt was incorrect.",
            "They came in darkness and were met by torchlight and ready steel. A coordinated response to a coordinated threat.",
            "The Uruk-hai's strength is their ferocity combined with discipline. You broke both by targeting the officers first.",
            "A victory over Isengard's finest. Whatever Saruman says about the western armies, revise it downward.",
            "The drums of Orthanc beat before the charge. They do not beat now. The silence is a kind of answer.",
            "Bred to be unbreakable and proven otherwise. Good. They need to know that is possible.",
            "Saruman's experiment in war-making met its counter-experiment today. Yours proved more effective.",
            "The Uruk-hai fell back. They rarely fall back. Remember what made them do it.",
            "A Saruman campaign without a reliable heavy infantry core is a different campaign. You just changed the calculation.",
            "The White Hand's reach was cut back today. Each inch matters on a hand that would encompass everything.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — DEFEAT vs ISENGARD / URUK-HAI
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleLossIsengard = {
            "Saruman's Uruk-hai do not tire as men do. Today that difference decided the field.",
            "Isengard's forces drove you back. The machines of Orthanc have been given more material to work with.",
            "The Uruk-hai came in formation and left in formation. You did not match them today.",
            "A defeat at Isengard's hands. Whatever Saruman lacks in honour, he makes up for in efficiency.",
            "The White Hand presses forward. Your lines broke before soldiers bred to break them.",
            "Uruk-hai do not break easily. Today it was your line that bent first.",
            "Isengard gains ground. Every defeat here is material for Saruman's next argument to his masters.",
            "You underestimated what Orthanc's forges have produced. Do not do so again.",
            // --- new lines ---
            "The Uruk-hai are fast and they hit hard and they do not flinch. Factor all three into the next plan.",
            "Saruman's engineering of war is not just metaphor. His soldiers are engineered. Respect the craft even as you oppose it.",
            "Isengard wins today. The White Hand plants itself one step closer to what it wants.",
            "Their crossbows held the range advantage and they used it. Deny them that in the next engagement.",
            "You gave ground to a force you should have been able to hold. Learn why before you meet them again.",
            "Saruman will not celebrate this victory. He will simply use it. That is the most dangerous response to winning.",
            "A Uruk-hai advance is not a rout — it is a controlled pressure. They are methodical. So must your defence be.",
            "The White Hand stretches further today. Cut it back tomorrow.",
            "These were not Orcs in the old style — undisciplined and prone to breaking. These were soldiers. Treat them as soldiers.",
            "Isengard's army has a purpose and a plan. Today, theirs was better than yours.",
            "Even Saruman the betrayer is capable of producing capable instruments. You felt that today.",
            "Retreat to defensible ground. Uruk-hai do not pursue recklessly, but they do pursue.",
            // ── expansion ──
            "The Uruk-hai identified your weakest flank and hit it. They are not just strong — they are observant.",
            "Isengard's forces were better coordinated today than your intelligence suggested. Update the estimates.",
            "A defeat by Saruman's armies. Whatever Orthanc knows about your methods, it knows more today.",
            "The Uruk-hai's crossbow volley held your cavalry at range. Next time, suppress the crossbows first.",
            "Saruman's forces fought without chivalry and without mercy. They were more efficient for both.",
            "The White Hand pressed the centre and the centre could not hold. Reinforce it or abandon the approach.",
            "Isengard won because they prepared for your tactics. You must prepare for their adaptations.",
            "Uruk-hai in night operations are particularly dangerous. Today proved that again.",
            "The defeat hurts both in terms of force and in terms of what Saruman will now assume he can do.",
            "A fighting retreat from Isengard's host. One of the less shameful varieties of retreat.",
            "The Uruk-hai's coordination comes from Saruman's direction. Disrupt the direction if you cannot break the soldiers.",
            "Orthanc adds today's victory to its record. Ensure it does not add a second one from the same cause.",
            "The White Hand won today. That hand is now reaching further than it did this morning.",
            "Even experienced commanders are beaten by Isengard's forces when the conditions favour the enemy.",
            "The defeat was real and its causes are knowable. Know them before the next engagement.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs HARADRIM / EASTERLINGS
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinHarad = {
            "The riders of Harad are scattered. Even the great mûmakil cannot stand against a resolute line.",
            "Far from their sun-scorched homes, the Haradrim find the North unforgiving today.",
            "The Eastern allies of Sauron break and run. They fight for gold and fear — neither compels them to die.",
            "Easterlings and Southrons, driven far enough from their homelands that they lose heart in a rout.",
            "A long way to march, only to be beaten back. Let the survivors carry that lesson home.",
            "The Children of Rhûn and Harad are fierce warriors — but today, fiercer ones stood opposite them.",
            "The dark allies of Mordor fall. They came willingly into Sauron's service — let them take the price.",
            "Victory over the South. Their great beasts and curving blades could not decide the field.",
            "The Corsairs' oars will find nothing to claim here. The coast holds.",
            "They came from distant lands to fight someone else's war. They are going home lighter.",
            // --- new lines ---
            "The Haradrim cavalcade broke against your line. Even mûmakil can be turned, if you know the ground.",
            "Southrons are not simple enemies — they are experienced, proud, and well-armed. That you beat them says something.",
            "Far from Harad's blazing sun and familiar desert, these soldiers found harder going than they expected.",
            "The curved blades of the Easterlings are excellent in the charge. They met something that did not break in the charge.",
            "Harad sends soldiers south of the Poros and west of Khand to fight Sauron's wars. Today you made that arrangement costly.",
            "The Haradrim retreat in better order than most — that is worth noting for when they come back.",
            "A victory over the South and East. Let it carry back through their homelands. Fear of you is free intelligence.",
            "The golden war-masks of the Haradrim gleam on a field that is now yours. A strange and gaudy sight.",
            "They are brave fighters and loyal to whatever banner they serve. Today their banner chose the wrong side.",
            "Sauron's southern coalition is not invincible. Today was evidence.",
            "The Corsairs of Umbar bring ships and fire and long-rehearsed cruelty. They bring those things somewhere else now.",
            "Far-travelled enemies in strange armour, driven back nonetheless. Distance did not make them unbeatable.",
            "The mûmakil are terrible in open ground. Find them in broken ground and they are half the problem.",
            "Sauron's alliance is vast — in the South and East as much as in the Shadow-lands. Each piece of it you break matters.",
            "A victory worth carrying to council. The South is not impregnable. You have proved it.",
            // ── expansion ──
            "The Haradrim are not broken — they are pulled back. There is a difference worth tracking.",
            "Far-travelled soldiers with strange weapons and battle-craft shaped by a desert we have never seen. You beat them anyway.",
            "The Southrons fought for Sauron's gold and Sauron's fear. Neither compelled them to die on this specific field.",
            "The mûmakil handlers fled when the beasts panicked. Target the handlers next time.",
            "A victory over Harad is a complicated thing — they are not evil, only allied to evil. Remember the difference.",
            "The Easterlings held their formation longer than expected. They are professionals. Treat them as such.",
            "Victory over Sauron's southern allies. The distance they came to fight here makes your victory a statement.",
            "The Corsairs' swords are not at your throat today. That is worth noting.",
            "A defeat for Harad's forces that will travel back south along the same roads their army marched north on.",
            "The curved blades and layered armour of Harad are excellent equipment. They did not compensate for superior positioning.",
            "The children of the Sun found the North cold and the enemy resolute. Both work in your favour.",
            "The Easterlings were flanked when they expected to be the ones flanking. Turn their own methods against them.",
            "Sauron's southern alliance includes some of the best fighters in the world. Today you beat some of them.",
            "A victory against professional soldiers fighting someone else's war. Find a way to make them not want the contract.",
            "Whatever the Haradrim were promised for this campaign, they have not received it yet. That tension can be exploited.",
            "The mûmakil are dangerous in open ground. They are expensive to maintain and irreplaceable once lost. They lost them.",
            "The Southrons will think harder about the next march north. Give them more reasons to.",
            "A victory that demonstrates the Haradrim's limits. Those limits are worth mapping.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — DEFEAT vs HARADRIM / EASTERLINGS
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleLossHarad = {
            "The Haradrim fight with a ferocity born of a lifetime in harsh lands. Today it outweighed yours.",
            "Easterlings in good order — disciplined and hard. You gave ground before them today.",
            "The warriors of Harad know a different kind of war. Today that knowledge served them better than your own.",
            "Far from home and still they drove you back. The South fights harder than you counted on.",
            "The mûmakil and the curved blades and the war-drums — it was more than the line could hold today.",
            "A defeat against Sauron's southern allies. They will carry the victory home as proof that the West can be broken.",
            "The children of Rhûn are not soft. They proved it today at your expense.",
            "You have learned something about how the South fights. The lesson was expensive.",
            // --- new lines ---
            "The mûmakil broke your cavalry. Next time, meet the great beasts with spears and prepared ground.",
            "Haradrim archers are among the best in Middle-earth. Your line felt that today.",
            "The Easterlings flanked you before you knew they were moving. They have done this before. They knew what they were doing.",
            "Sauron's southern alliance is not merely a matter of threat and coercion — these are capable armies. Respect them.",
            "A retreat south of the river was the only option. It was the right one. Honour it.",
            "The golden war-masks of Harad are a deliberate choice — to intimidate as much as protect. Did they work today? Adjust.",
            "The Southrons push deeper into territory they should not hold. Take it back before they build anything there.",
            "A hard lesson from proud warriors. They will not be easier to fight the second time, but you will be less surprised.",
            "The South is vast and Sauron has had centuries to cultivate its enmity toward the West. Today that cultivation bore fruit.",
            "Defeat before the Easterlings. Wherever they march, the shadow is given legs. Slow those legs.",
            "Costly. But survivable. Rebuild what was lost before the Haradrim take another step West.",
            "The drums of Harad carry on the wind. Learn their rhythms — they precede the charge by forty heartbeats.",
            // ── expansion ──
            "The mûmakil did what they were brought to do. Factor them into every plan that involves open ground.",
            "Haradrim cavalry at full gallop is a different problem from infantry. Your flanks were not ready for it.",
            "The Easterlings fought with a precision that your company did not match today.",
            "A defeat by Sauron's southern allies. The shadow now has a foothold in this direction.",
            "They knew the land better than you did. Never fight on ground you have not scouted.",
            "The South sends fighters who are experienced at exactly this kind of engagement. Study their methods.",
            "Retreating from the Haradrim is one thing. Letting them hold ground is another. Address the distinction.",
            "The defeat was partly terrain, partly intelligence failure, partly execution. All three need improvement.",
            "Sauron's southern arm is strong today and emboldened. Ensure it does not push further while you regroup.",
            "The Corsairs at the flank were the deciding factor. Coastal threats need naval or rapid-response counterparts.",
            "The Haradrim came prepared for a longer fight than you gave them. They used that preparation well.",
            "A loss to the East and South. The war is being fought in directions that need more attention.",
            "Recover what you can and revisit the intelligence picture. It was incomplete in at least one critical way.",
            "The Easterlings do not announce their flanking movements. They move and then they are there. Account for this.",
            "A costly defeat. Rebuild what was lost before the Haradrim have an opportunity to follow it up.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs CIVIL WAR / ALLIED FACTIONS
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinCivilWar = {
            "A victory over those who should have been allies. There are no celebrations in this.",
            "Brother against brother — the cruelest of wars. But the line held, and that is what matters now.",
            "Men of honour on both sides today. The outcome changes nothing of what they were.",
            "This fight should never have been fought. That does not change who won it.",
            "A hard victory over a worthy enemy. Mourn the dead before counting the gains.",
            "When the real threat stands to the East, wars like this are luxuries neither side can afford.",
            "The field is yours. The cost of it will be felt for years.",
            "Civil strife tears at the fabric of the West. Sauron requires no help — and yet is helped.",
            "Brave men died here for reasons that history will judge harshly. You survived. That will have to be enough.",
            "These were not your enemies by choice, but by necessity. That changes the taste of the victory.",
            // --- new lines ---
            "A war between those who share more than they differ — Sauron's shadow benefits more than anyone here today.",
            "The fallen on both sides carried the same traditions, the same songs, the same faith in the West. That should haunt this victory.",
            "Wars between allies hollow out the very strength that the shadow seeks to destroy. Remember that at the peace table.",
            "The men you fought today were not evil. They were wrong, or they were led badly, or they were afraid.",
            "This victory is clean in the tactical sense and a wound in every other sense.",
            "Gondor does not need enemies from the East when it has conflicts like this. And yet here we are.",
            "Both sides paid the same price for the same field. Whoever wins a civil war, the real winner is never at the table.",
            "Let there be no cruelty to the defeated. They were not your enemies in the oldest sense.",
            "Allies who become enemies seldom become allies again. Remember that at every decision that led here.",
            "The victory is yours. The shame of the necessity is shared. Do not pretend otherwise.",
            "Even in winning, take stock of what was lost: the trust, the alliance, the common cause.",
            "Peace, when it comes, must be made carefully. Today's defeated cannot become tomorrow's wound that never closes.",
            "This is not the kind of victory to carry proudly. Carry it carefully instead.",
            "When the free peoples fight each other, the darkness does not even need to march.",
            "The enemy you defeated today would have stood beside you against the true Shadow. Try to find a way back to that.",
            // ── expansion ──
            "A civil victory is always a larger loss than the battle itself implies.",
            "You won the fight. No one won the war that produced it.",
            "The defeated today will be needed tomorrow against the true enemy. Treat them accordingly.",
            "When the shadow watches free peoples fight each other, it needs do nothing but wait.",
            "A necessary outcome, given the necessity — but the necessity itself should be questioned at every available council.",
            "The fallen on both sides were capable soldiers. The cause that needed them did not benefit from their death.",
            "Honour the defeated. They were not your enemies in any deep sense.",
            "A field victory in a civil conflict is a political wound that will take longer to heal than any battlefield injury.",
            "The cost of internal conflict compounds: it costs soldiers, it costs supply, and it costs the credibility that comes from unity.",
            "The outcome is yours. Whether it was worth the cost is a question that will be answered over years, not days.",
            "Even in victory, do not allow cruelty. The people you defeated will be neighbours again.",
            "The real enemy observes these civil battles with satisfaction. Give them no more reason for satisfaction.",
            "A civil war that ends in a field decision has only delayed its political resolution. Handle that resolution carefully.",
            "Whatever divided these factions is not resolved by who held the field. Resolution is political, not military.",
            "The victory is real and the grief is also real. Both can be held at the same time.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs GONDOR forces (if specifically targeted)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinGondor = {
            "The Men of Gondor fall back. To beat the heirs of Númenor is no small thing — and no cause for pride.",
            "Gondor's soldiers are among the finest in Middle-earth. That they lost today speaks to what you have become.",
            "The white tree banner is lowered on this field. It should not have come to this.",
            "A Gondorian force, defeated. The stone city will hear of it before nightfall.",
            "The Tower Guard did not break easily. They never do. That you broke them means something — decide what.",
            "Men of the South, trained in the old ways of Númenor, and still they fell. A sobering thought.",
            "The silver and black retreat in good order. They will be back, and they will not make the same mistake twice.",
            "Gondor's captains are proud and capable. Today, yours were more so. Use the advantage well.",
            "A victory over the White City's soldiers. There is grief in it, whatever the politics demanded.",
            "Minas Tirith will hear of this and draw its own conclusions. Make sure your version reaches them first.",
            // ── expansion ──
            "The silver and black have retreated. They were not the enemy in the oldest sense. Handle the aftermath accordingly.",
            "Gondor's forces are among the most disciplined in the world. That they broke speaks to your company's quality.",
            "The Tower Guard does not simply break. They were outmanoeuvred, not overwhelmed. Note the distinction.",
            "A victory over the heirs of Númenor. The history of this field is already complicated.",
            "Gondor's captains will have a full account of this battle before nightfall. Expect a response.",
            "Even the greatest martial tradition in the West has losses. Today it had this one.",
            "The white tree banner is lowered and must not be treated as merely another captured flag.",
            "Gondor's soldiers know the old traditions of honourable conduct toward prisoners and wounded. Mirror them.",
            "A bitter victory. The men you faced were the defenders of the same world you are trying to protect.",
            "The stone city will not take this lightly. Be prepared to explain yourself at a council table.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs ROHAN forces
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinRohan = {
            "The Rohirrim are driven from the field. Cavalry routed is cavalry that takes time to reassemble.",
            "The horse-lords of Rohan fell back today. That has not happened often in the history of the Riddermark.",
            "Even Éomer's heirs ride hard and fight harder. They fought hard today — and still you held.",
            "The thundering of Rohirrim hooves fades into the distance. You stood against it. That is worth knowing.",
            "The Mark is proud, and its pride is usually earned. Today, it was not enough.",
            "Rohan's cavalry charge is the most feared thing in the western armies. You broke it. Remember how.",
            "A victory over the horse-lords. Let it be bloodless where possible — they are not the enemy of the world.",
            "The riders of Rohan fight with passion that matches their horsemanship. Today passion met something colder.",
            "The Riddermark will mourn its dead from this field. So will many others.",
            "Their éored was broken but not destroyed. They will regroup and return. Be ready.",
            // ── expansion ──
            "The thunder of Rohan's cavalry is one of the most formidable sounds in the West. You silenced it today.",
            "An éored broken and scattered — that is an achievement that the Riddermark will not quickly digest.",
            "The horse-lords of Rohan fell back. In the history of this war, that does not happen often.",
            "They came in the old way — full charge, knee to knee, banners forward. You met them with something that held.",
            "Rohan's cavalry charge is the most feared manoeuvre in the West. You broke it. Remember every detail of how.",
            "The riders of Rohan are brave to the point of being dangerous to themselves. Today that courage was not enough.",
            "A victory over the Riddermark that their songs will record and debate for generations.",
            "The Mark will come again, better prepared. They always come again. Be ready.",
            "Even Théoden's heirs are not invincible. Today proved it.",
            "Rohan fell back in good order. They are hurt but not destroyed. Watch for the return.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs ELVEN forces
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinElves = {
            "An Elven host, defeated. In another age, that would have seemed impossible. This is not that age.",
            "The Firstborn retreat — and they do so without haste, and without forgetting a single face on this field.",
            "To beat the Elves in open battle requires more than numbers. You had more than numbers today.",
            "The arrows of the Galadhrim are never wasted. That they were not enough today is a strange thing to reckon with.",
            "The Elves remember everything. They will remember this, and in a thousand years it will still be a grievance.",
            "An Elven war-band does not shout as Men do. Their silence in retreat is somehow more unnerving than their advance.",
            "The silver helms withdraw into the tree-line. Do not pursue. The forest is still theirs.",
            "Even the grace of the Firstborn bends before sufficient force and sufficient will. Today you provided both.",
            "There is no pride in this. The Elves are diminishing — fighting them hastens what should not be hastened.",
            "A victory over those who once lit the world. However necessary, it is a diminishment for all of us.",
            // ── expansion ──
            "The grace and precision of an Elven war-band could not compensate for being outflanked.",
            "To defeat the Firstborn in open battle is to have achieved something that will be remembered in Rivendell for an age.",
            "The Elves withdraw without haste and without forgetting. You have earned a grudge that will outlast empires.",
            "An Elven defeat is not simply a military event. It is a moment in the history of the world.",
            "The silver helms are gone into the trees. Do not follow — the forest remains theirs.",
            "The Firstborn do not retreat carelessly. Each step of their withdrawal is observed and reported.",
            "Whatever reduced this conflict to the point where Elves and your company faced each other — it will not be easily undone.",
            "A victory over the Galadhrim or the Noldor carries a weight that victories over mortal armies do not.",
            "The Elves will have already sent word. Whatever council receives it will react in ways you should anticipate.",
            "There is grief in this victory. Let it be felt. It is the appropriate response.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY vs DWARVEN forces
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWinDwarves = {
            "Dwarven infantry broken — which is to say, pushed back with great difficulty and greater loss.",
            "The axes of Durin's folk fell silent on this field, but only after leaving their mark on it.",
            "Dwarves do not rout easily. What you achieved today is a tactical withdrawal against an enemy that almost never retreats.",
            "The Deep-folk give ground only when giving ground is the final option. Today it was.",
            "A Dwarven line held to the last man who could hold it. Those who remain left with their heads high.",
            "Even the stubbornness of Khazad-dûm could not hold here. It was close, though. It was very close.",
            "The sons of Mahal fight for honour and kin and the memory of halls lost. They fought hard today.",
            "Dwarven armour is the finest in Middle-earth. It was not enough. Nothing ever quite is, if the enemy has sufficient will.",
            "The axes are silent. The grudge is written. Dwarves do not forget, and they do not forgive. Be ready for the return.",
            "To defeat Dwarven infantry, you must match their stubbornness. You did. It cost you nearly as much as it cost them.",
            // ── expansion ──
            "Dwarven infantry does not rout. It retires. There is a meaningful tactical difference.",
            "The axes fell silent after being asked to do what axes can do — and finding something harder on the other end.",
            "A Dwarven line defeated is a grudge inscribed. They will not forgive, and they will not forget the specifics.",
            "The sons of Mahal fought with the stubbornness of their mountain halls. It was nearly sufficient.",
            "Defeating Dwarven heavy infantry takes patience, positioning, and more losses than you would like.",
            "The grudge book is being updated right now, in accurate detail, with names and dates and the exact cost.",
            "A victory over Durin's folk. Whatever brought it to this point, it was a poor series of decisions by someone.",
            "The Deep-folk left the field with their dead and their dignity. Both are Dwarven rights.",
            "Even the hardest things break when sufficient pressure is applied to the right point. You found the point.",
            "The axes are quiet. The memory of those who swung them is not, and never will be.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — VILLAGE BEING RAIDED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] VillageUnderRaid = {
            "Smoke rises from a hamlet on the horizon. Riders with dark banners are already at work.",
            "Cries carry on the wind from a village that cannot defend itself. The raiders are swift.",
            "A settlement burns. There are those who feed on fear — and they feast freely today.",
            "Raiders strike before the sun is fully up. By the time word reaches anyone, the harm is done.",
            "The enemy does not always fight to hold ground. Sometimes it burns it, instead.",
            "Screams from a distant village. The war against the darkness is also fought in places like this.",
            "Plumes of smoke mark the progress of a raid through farmland that will not recover quickly.",
            "No army required — a handful of raiders can unmake a year of peace in an afternoon.",
            "The shadow does not always come with great armies. Sometimes it comes in smaller, quieter horrors.",
            "Those who cannot defend their villages must watch them burn. A truth that never stops being bitter.",
            "The crackling of burning thatch, the sound of a raid in progress — both linger long after the riders are gone.",
            // --- new lines ---
            "Dark riders at full gallop toward a settlement that has no walls and no warning. Move if you can.",
            "The harvest season makes villages soft targets — the food is there, the defences are not.",
            "Every raided village is a message to the others: no one is coming to help.",
            "The enemy chooses their targets carefully. Isolated, undefended, and full of things worth taking.",
            "A raid underway in the farmlands. The army that could stop it is always somewhere else.",
            "Torches thrown into rooftops at night — a coward's weapon that works as well as any other.",
            "The shadow uses fear strategically. A raided village tells the next village to give in before the riders arrive.",
            "Livestock driven off, storehouses emptied, wells fouled. The damage is calculated, not frenzied.",
            "The village militia — if there is one — will not hold without help. They never do.",
            "It takes a generation to build a farming community. It takes an afternoon to make it uninhabitable.",
            "The enemy does not always announce itself with drums and banners. Sometimes it announces itself with smoke.",
            "A settlement under raid is a settlement being told what the future looks like if no one fights back.",
            "The people there did nothing to deserve this. That is not unusual. It is the nature of the shadow.",
            "If this raid is allowed to succeed, the next one will be larger and further into your territory.",
            "Even if you cannot intercept them, learn where they came from. The source of a raid matters.",
            // ── expansion ──
            "The raid is underway and you are not close enough to stop it. That is the first problem. Solve the second one.",
            "A village burning means more than the buildings — it means the people had no protection and knew it.",
            "The enemy chose this village because it could be reached and because no one would stop them. Both of those facts need changing.",
            "A raid that goes unanswered invites the next one. That is not a moral point. It is a tactical one.",
            "The smoke column from a village tells you several things: the direction, the size of the raid, and how long ago it started.",
            "Speed is the only useful response to a raid in progress. What comes after is containment and retribution.",
            "The village did not have walls. Villages without walls depend on patrols. The patrol was not there.",
            "Every farmstead burning is a winter without food for someone. The raid has consequences that outlast the flames.",
            "Riders with torches do more damage per soldier than almost any other formation. They know this. So should your patrols.",
            "The shriek of burning thatch is the sound of someone's entire livelihood being destroyed in an afternoon.",
            "The enemy raids systematically. This village was chosen, not random. Understand the selection logic.",
            "A village under raid is also a warning to every village within earshot. Fear is part of what is being burned.",
            "The livestock will be driven off — harder to defend than buildings and more valuable to the raiders.",
            "Get between them and their extraction route. That is the only reliable way to intercept a raid.",
            "The well is always targeted. Fouling the water source turns a raid into a months-long problem.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — COMPLETED (village looted)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidCompleted = {
            "The village is stripped bare. Supplies taken, shelters burned, people scattered. A grim accounting.",
            "What took years to build has been undone in an afternoon. The raiders are gone — the damage remains.",
            "Ash and ruin where a settlement stood. This is what the darkness costs in human terms.",
            "They came, took what they wanted, and left the rest burning. A familiar pattern in these dark days.",
            "The hamlet will not recover quickly. Its people will remember this long after the embers cool.",
            "No battle fought here — only cruelty and theft, and villagers who could do nothing but run.",
            "A community unmade by an afternoon's work. The shadow grows bolder when it goes unanswered.",
            "Grain stores burned. Livestock taken or slaughtered. Wells fouled. The raiders were thorough.",
            "Every village raided makes the next one more fearful. Fear is currency the enemy spends freely.",
            "The harvest that would have fed a hundred is gone. Hunger follows in the wake of every raid.",
            "When you find the force that did this, do not mistake victory over them for justice.",
            "The shadow takes in silence as often as in battle. Today it took much.",
            "An empty village under a smoke-stained sky. This is the price of land left undefended.",
            "The earth here will be cold before what was lost is rebuilt. And that is if it is rebuilt at all.",
            "Raids like this are not random — they are deliberate. The enemy knows that broken villages break morale.",
            // --- new lines ---
            "The survivors will need food, shelter, and the knowledge that someone is doing something. Provide at least one of those.",
            "A raided village seldom rebuilds in full. Some of the people do not come back.",
            "The well is poisoned. The stores are ash. The livestock is gone. It will be a hard winter in this valley.",
            "Whatever crop was in the fields is trampled or burning. That loss will multiply through winter.",
            "The hearthstones still stand. The people who warmed themselves at them do not.",
            "Count the cost and remember it. Wars are won in the grand sweep of armies; they are also lost one village at a time.",
            "Children who watch their homes burn do not grow up with soft politics. This raid has consequences decades long.",
            "The enemy does not merely want resources. They want exhaustion. They want despair. A raided village produces both.",
            "A settlement reduced to a waypoint on a road through ash. The darkness is thorough when it works.",
            "The smoke will carry to the next settlement. Their gates will close early tonight.",
            "The people scatter to the hills or the nearest town. Some carry only what they stood in.",
            "It costs the enemy very little to do this. It costs the region enormously. That asymmetry is deliberate.",
            "What stands is what could not be lifted or burned. In most villages, that is not much.",
            "Make them pay for this. Not out of vengeance — out of the strategic truth that unanswered raids invite more raids.",
            "Add this to the tally. The tally will be settled eventually — not today, but eventually.",
            // ── expansion ──
            "A completed raid is a completed argument: no one was watching, no one responded, nothing stood against it.",
            "The raiders left before anyone arrived to stop them. Both of those things should change.",
            "Whatever was worth burning, they burned. Whatever was worth taking, they took. Whatever was worth ruining, they ruined.",
            "The village is not gone — but it will take a full season to recover what was lost in an afternoon.",
            "The cost of this raid is mostly invisible. Fear. Loss of confidence. The decision not to replant next year.",
            "The families who fled will not all come back. Some losses are measured in people, not property.",
            "Add this to the territory that requires active defence rather than passive presence. The distinction is now clear.",
            "The raid succeeded because the factors that prevent raids were absent. Fix those factors.",
            "Ashes and empty storehouses and a road that leads nowhere useful now. That is the aftermath.",
            "What the enemy took was food and goods. What they destroyed was the belief that the region is safe.",
            "A raid that completes without opposition is an invitation. The raiders have already issued the next one.",
            "The people of this village did everything that was asked of them. The failure is elsewhere.",
            "A raid completed sends a message across every settlement in the region. What the message says depends on your response.",
            "Even the grain stores that were not taken are compromised — some burned, some fouled. The winter count is down.",
            "The next raid will be larger, if this one was not answered. That is how successful raiding campaigns develop.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — REPELLED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidRepelled = {
            "The village stands. The raiders found resistance they did not count on and paid for it.",
            "They came to pillage and found steel instead. A lesson the surviving raiders will not forget.",
            "A settlement defended. For once, the shadow left empty-handed.",
            "The militia held. Sometimes a few brave people with farm tools and old blades can turn the tide.",
            "The raid is broken. Smoke from enemy bodies instead of enemy torches — a welcome reversal.",
            "They thought it would be easy. It was not. The village endures.",
            "Not every hamlet is defenceless. Today, one reminded raiders of that at great cost to the raiders.",
            "The attackers flee with fewer numbers than they came with. The village lives to see tomorrow.",
            "When the shadow is answered immediately, it does not always grow. Today, it was answered.",
            "A small victory — one village, one raid repelled — but it matters to the people who live there.",
            "Word will spread that this settlement bit back. That alone will deter the next raid.",
            "The fields still stand. The harvest still has a chance. One more reason to hold the line.",
            // --- new lines ---
            "The village held. Sometimes the most important victories are the quiet ones that protect ordinary lives.",
            "Unarmed farmers are not always defenceless — not when they know the land and have had time to prepare.",
            "The raiders retreated empty-handed. That is an unusual outcome, and it will be talked about.",
            "A defended settlement is a statement. It says: this is where the shadow stops.",
            "The village will feast tonight — in relief as much as celebration. Let them.",
            "When ordinary people refuse to yield, it costs the enemy something more than soldiers. It costs them certainty.",
            "News of a repelled raid travels as far as news of a successful one — in a different direction.",
            "The militia stood their ground. Train them better, arm them better, and the next raid will fare no better.",
            "A raid that fails discourages the next one. Not always. But often enough to matter.",
            "The children of this village will grow up knowing that resistance worked here. That is not a small thing.",
            "The village is intact, the harvest is intact, the people are intact. A day when all three can be said is a good day.",
            "The shadow retreated before something it did not expect — organised resistance and the refusal to run.",
            "The village sent riders to warn the neighbours. That kind of cooperation is how the shadow is kept out of a region.",
            "Repelled with fewer losses than the attackers. The economics of the raid did not work out in the enemy's favour.",
            "Leave a garrison if you can. A repelled raid followed by an undefended village is still a raid waiting to happen.",
            // ── expansion ──
            "Repelled at cost, but repelled. The settlement stands. Count what the defence cost and consider it well-spent.",
            "The raiders found resistance and found it sufficient to make them leave. That is the only equation that matters.",
            "They turned back at the first real opposition. Some raids are like that — probes that become retreats under pressure.",
            "A defended village is proof that the shadow's plan is not infallible.",
            "The militia stood. When militia stands, it becomes something more than militia.",
            "Word will carry from this village. The raiders will carry it themselves — that this one fights back.",
            "The defence was improvised and sufficient. Formalise it before the next raid assumes otherwise.",
            "Every repelled raid makes the region slightly harder to operate in from the enemy's perspective.",
            "The village stood because someone prepared it to stand. Credit that preparation.",
            "A raid repelled is a tactical victory and a strategic signal. Send it loudly.",
            "The attackers retreated with fewer numbers and a worse plan for next time.",
            "The cost of defending this village was lower than the cost of losing it. Keep that accounting in mind.",
            "Leave something here. A patrol, a promise of regular attention, a message that the settlement is watched.",
            "The people of this village discovered they can defend themselves. That discovery has its own value.",
            "Repelled. The village is intact. The harvest is intact. The people are intact. Three good numbers.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — STARTED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SiegeStarted = {
            "The gates are shut and the walls are manned. The siege has begun.",
            "Engines of war creak into position. Whatever is inside must hold — or fall.",
            "A great host has surrounded the walls. The defenders look out at a sea of hostile banners.",
            "The siege lines form with grim efficiency. No food in, no word out — the ancient method of breaking wills.",
            "Catapults and siege towers. The old patient way of taking fortresses — wait, and starve, and batter.",
            "The walls have seen sieges before. Whether they will see another dawn depends on what happens next.",
            "Banners encircle the walls and campfires ring the perimeter. A settlement is now a fortress.",
            "The defenders have only their walls, their supplies, and their courage. Two of those can run out.",
            "The shadow draws close around stone walls. Every day the siege holds is a day something vital is denied.",
            "War trumpets sound below the walls. The besiegers announce themselves to those who already knew they were coming.",
            "A siege is a battle measured in weeks, not hours. Hunger and despair are the enemy's best siege weapons.",
            "The drawbridge is up. The world outside is now divided into defenders and those who wish them harm.",
            "Walls that have stood for ages face another test. Stone is strong — but it cannot fight back.",
            "The enemy surrounds the fortress on all sides. Relief must come before the garrison's nerve breaks.",
            "They have chosen patience over storm. That patience will cost the defenders dearly if no help comes.",
            // --- new lines ---
            "The supply lines are cut. From this moment, the garrison fights on what it already has.",
            "Siege towers are being built from the trees of the surrounding forest. The defenders can hear the axes.",
            "A thousand campfires ring the walls. The defenders see the light of them from the battlements.",
            "The siege engineer knows their trade. The defenders on those walls had better know theirs just as well.",
            "Defenders under siege fight a different war than defenders in the field — slower, more internal, more exhausting.",
            "Once the siege lines are set, the world outside becomes rumour. That uncertainty is its own weapon.",
            "The garrison commander must balance rations against morale against the probability of relief. None of those calculations are easy.",
            "Walls buy time. Time buys the chance for relief. Relief requires someone out there willing to march.",
            "The besieging army has chosen their position well. The defenders have no easy sally point.",
            "Siege is the most patient form of warfare. It asks the same of both sides — but only one side is hungry.",
            "The enemy builds their circumvallation with professional efficiency. They have done this before.",
            "Whatever food is inside those walls now is all the food the garrison will have. Count it carefully.",
            "Stone holds. But stone cannot hold against time and hunger and despair without help from outside.",
            "The sound of siege engines being assembled carries through the night. The garrison hears it.",
            "The siege begins as all sieges begin — with confidence from the outside and resolve from within.",
            // ── expansion ──
            "The circumvallation is being built. Once it is complete, the garrison is on its own.",
            "The garrison has supplies for a fixed number of days. That number is now counting down.",
            "A besieging force can afford patience. A besieged garrison cannot. That asymmetry drives everything.",
            "Siege warfare is a test of who runs out of something first — food, water, morale, or ammunition.",
            "The defenders see campfires in every direction. That sight tests soldiers in a particular way.",
            "The siege lines are set and the roads sealed. What was a settlement is now a fortress — whether it wanted to be.",
            "The relieving force — if one exists — has its own timeline. The garrison's timeline has to match it.",
            "A siege declared in autumn is a siege that the winter joins as an ally — not always for the besieger.",
            "The garrison commander's first decision is always the same: count the food and do not lie about the number.",
            "From the walls, the encircling army looks larger than it is. From inside the walls, everything looks larger than it is.",
            "The besiegers have established their camp in positions that make a direct sortie expensive.",
            "Whatever the garrison knows about relief forces will degrade over time as the siege cuts communications.",
            "The siege is a political as well as a military event. Those who did not send relief will need to explain why.",
            "Attrition is the besiegers' weapon and the garrison's enemy. Track it honestly.",
            "The first day of a siege is the easiest. The garrison knows that. The question is how they handle the later ones.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — STARTED (named line — {0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SiegeStartedNamed = {
            "A host besieges {0}. The walls will be tested before this is over.",
            "The siege of {0} begins. Let the garrison hold until relief can arrive.",
            "{0} is surrounded. The clock begins counting down from this moment.",
            "Siege engines before {0}. The enemy means to take it by patience if not by storm.",
            "The walls of {0} face the patience of a besieging army. Time is now a weapon.",
            // --- new lines ---
            "The enemy's lines close around {0}. Every road in or out is now contested.",
            "{0} is encircled. Whatever is inside must last until help arrives — or until it cannot.",
            "The siege of {0} is declared. Its people are now on the inside of a very specific kind of war.",
            "Catapults take aim at {0}. The walls have seen sieges before. Whether they survive this one is the question.",
            "{0} stands ringed by hostile campfires. The garrison watches from the walls and counts them.",
            "The fall of {0} would change the shape of this campaign significantly. Hold it.",
            "Engines of war creak into position before {0}. The defenders can hear them being assembled.",
            "{0} is under siege. The supply count begins now — from this moment, every day costs the garrison.",
            "A great host has made {0} its objective. Relief must come before the walls or the will gives out.",
            "The roads to {0} are cut. The garrison is on its own until someone opens them again.",
            // ── expansion ──
            "The army surrounds {0} completely. Every road in is a road they command.",
            "{0}'s gates are closed and will not open to friends until the siege is broken.",
            "Catapults are being aligned against {0}. The defenders are watching them being assembled.",
            "The fall of {0} would be a strategic blow of the first order. Hold it.",
            "{0} is under siege. The supply count started the moment the last road was cut.",
            "The walls of {0} are old and strong. They are also, like all walls, finite.",
            "{0} stands surrounded. Relief must come before the garrison runs out of whatever it has the least of.",
            "The siege of {0} is a commitment by the besieging force. It also commits every ally of {0} to respond.",
            "{0} under siege changes the campaign picture significantly. Act accordingly.",
            "From inside {0}, the fires of the besieging army ring the horizon. From outside, a clock is ticking.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT CAPTURED (named line — {0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SettlementCapturedNamed = {
            "{0} is yours. The gates are open and the banners are changed.",
            "The walls of {0} are taken. Now comes the harder work of holding them.",
            "Your standard flies above {0}. The siege is over — the responsibility is not.",
            "{0} has fallen to your forces. Tend the garrison and secure the walls.",
            "The keys of {0} change hands. What was defended yesterday is yours to defend today.",
            // --- new lines ---
            "The fall of {0} marks a turning point. Do not let the momentum stop here.",
            "{0} opens its gates to your forces. The people inside are watching carefully.",
            "The siege of {0} is ended. Begin the work of making it a fortress rather than a prize.",
            "{0} is secured. Garrison it, supply it, and do not treat its people as conquered subjects.",
            "The banners of {0} are changed. May the new ones mean something better for those who live there.",
            "Your forces hold {0}. That holding will be tested — prepare for the test.",
            "{0} is yours by force of arms. The harder achievement will be holding it by force of justice.",
            "The garrison of {0} has yielded. Treat them with whatever mercy the situation allows.",
            "Victory at {0}. The campaign continues — but this is a position worth keeping.",
            "Every stone of {0} now belongs to your answer for it. Use them wisely.",
            // ── expansion ──
            "The walls of {0} are yours. The responsibility that comes with them is yours as well.",
            "{0} is secured. Begin the work of making it a position rather than a prize.",
            "The banner over {0} has changed. The people inside it are watching to understand what that means.",
            "Your forces hold {0}. Hold it well — what was taken can be retaken.",
            "{0} falls to your control after a siege that cost both sides. Neither the cost nor the outcome should be forgotten.",
            "The keys of {0} are in your hands. So is the obligation to the people those walls protect.",
            "{0} is yours. Now defend it better than it was defended against you.",
            "The fall of {0} reshapes the strategic map. What was their stronghold is now your forward position.",
            "{0} belongs to your cause. Make sure your cause deserves it.",
            "The garrison of {0} yields. Treat them with the dignity that the traditions of honourable war require.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT CAPTURED (generic flavor)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SettlementCaptured = {
            "The gates have fallen. The settlement is yours — for as long as you can hold it.",
            "Your banners fly from walls that belonged to another this morning. War is rarely tidy.",
            "The last defenders yielded or fled. The fortress is secured. Now comes the harder work of holding it.",
            "A stronghold taken. What was your enemy's becomes your responsibility to protect.",
            "The walls are yours. Tend the injured, restrain the soldiers, and make clear what is expected of the garrison.",
            "Victory at the walls. The keys of the fortress change hands — along with every problem that comes with it.",
            "The settlement has fallen. Its people are watching to see what kind of conqueror you are.",
            "The flag is raised. Every soldier who fought here will remember this day differently.",
            "Another hold secured against the darkness — or taken from those who were not yet the darkness.",
            "Walls, stores, garrison — all yours. Also the responsibility for the lives within them. Both can feel heavy.",
            "The siege is over. The settlement is yours. The next army that comes will make it theirs if you are not careful.",
            "You have taken what was defended. Now defend what you have taken.",
            "A fortress earned in blood and patience. Neither tends to make a good foundation for peace.",
            "The old banners are struck down. New ones are raised in the wind of a changed world.",
            "Every castle taken is also a line extended — another place to supply, another place to defend, another reason to keep moving.",
            // --- new lines ---
            "The walls stand. What stood behind them — hope, fear, ordinary life — now depends on you.",
            "Fortifications taken, garrison absorbed, records seized. Now make it worth what it cost.",
            "A captured fortress is a doubled burden — the cost of taking it, and the cost of keeping it.",
            "The fighting is done. The politics begin immediately. Move carefully.",
            "The settlement's people have seen armies come and go. Show them you are something different.",
            "The flag changes. The fields outside the walls do not. The crops still need tending.",
            "A strategic position secured. The road it commands is now yours to control.",
            "Victory here reshapes the supply picture significantly. Protect those advantages.",
            "The defenders fought with honour. Their defeat is not an excuse to treat them without it.",
            "A hard prize. Hold it or lose what the taking of it cost — there is no middle option.",
            // ── expansion ──
            "The fortress is yours. It came at a price. The price of keeping it will be similar.",
            "A captured settlement is a doubled obligation — the obligation to secure it and the obligation to govern it.",
            "What was defended by one side is now defended by another. The walls do not care about the banner.",
            "The people inside those walls did not ask to be involved in this campaign. Handle them accordingly.",
            "A stronghold taken. Every castle taken extends your supply problem by the distance it sits from your base.",
            "The old defences are yours to improve or to exploit. Do not leave them exactly as you found them.",
            "The taking of this fortress changes the local strategic picture more than any field engagement would have.",
            "Walls, garrison, stores — all present and now accounted to your side. So are all the liabilities.",
            "A settlement captured is a settlement that will require a garrison. Plan for that before celebrating.",
            "The fighting is done. The governing begins. Both are hard. The governing is harder.",
            "Every stone of this fortress was placed by hands that expected it to defend something. Respect that expectation.",
            "The defenders are disarmed, not dishonoured. The distinction matters for what comes after.",
            "A campaign milestone: a major stronghold passes into your hands. The momentum is real — use it carefully.",
            "Take the fortress cleanly. Whatever pillaging happens in the name of your company reflects on you personally.",
            "The old banner comes down. The new one goes up. Everything in between those two moments was the price of the change.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT LOST (named line — {0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SettlementTakenNamed = {
            "{0} has fallen. The banners are changed. The strategic map is redrawn.",
            "The gates of {0} are open — to the wrong side. A serious loss.",
            "{0} is taken. Every hand it passes through costs blood to reclaim.",
            "The fall of {0} reverberates through the region. Its defenders did what they could.",
            "A new flag flies over {0}. What flies next depends on you.",
            // --- new lines ---
            "The walls of {0} belong to the enemy now. Get them back before they make those walls their own.",
            "{0} is in hostile hands. The people inside it are no longer yours to protect — for now.",
            "The loss of {0} opens this whole region to the enemy. Do not let that opening become a rout.",
            "{0} falls. The garrison either fled or was overwhelmed — find out which before you judge them.",
            "What was built over generations in {0} now serves whoever took it. That must be corrected.",
            "The banner above {0} is wrong. Fix it as soon as the force is available.",
            "{0} is taken. The road it sat astride is now the enemy's road. Think carefully about what that means.",
            "The enemy holds {0}. Every day they hold it, they fortify it. Move soon.",
            "The fall of {0} is a wound in your position. Treat it with the urgency a wound deserves.",
            "A flag lowered over {0} that took years to earn. A sharp and particular kind of grief.",
            // ── expansion ──
            "{0} is in hostile hands. Every day it remains so, it becomes more hostile.",
            "The walls of {0} serve the wrong side now. That needs correcting.",
            "{0} has fallen. The news will travel faster than any relief force.",
            "The garrison of {0} either fled or was overwhelmed. Both outcomes need understanding.",
            "What was yours in {0} is now the enemy's. What was the enemy's in terms of vulnerability is now their strength.",
            "The fall of {0} is a strategic wound. Treat it with appropriate urgency.",
            "Everyone who relied on {0} for safety just lost that safety. They need to know someone is coming.",
            "{0} is gone. The road it commanded is the enemy's road now.",
            "The banner above {0} is the wrong one. Correcting it will cost more than keeping it would have.",
            "{0} falls and the campaign adjusts around the gap it leaves. Adjust quickly.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT LOST (generic flavor)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SettlementLost = {
            "The walls have fallen. A stronghold is lost and must be retaken — or mourned.",
            "They broke through. What was yours is theirs now, and the people inside with it.",
            "The gates could not hold forever. The question is how long you can afford to let the enemy have this.",
            "A fortress taken from you — the garrison either fled or fell. Neither is good news.",
            "The loss of this settlement changes the shape of the war. The enemy grows stronger with each gain.",
            "Stone walls were not enough. They rarely are, against sufficient determination and time.",
            "Your hold on this land is diminished. Whatever was inside — people, supplies, strategic value — is gone.",
            "The banner is pulled down. Somewhere a defender watches it fall and does not know where to go.",
            "A castle does not fall once. It falls, and then must be won back, and that costs more than keeping it would have.",
            "This loss will ripple outward. The people nearby will hear and fear accordingly.",
            "Another foothold for the enemy. The map of this war looks a little darker today.",
            "They broke what you had built. That is the nature of this kind of war — everything is provisional.",
            // --- new lines ---
            "The walls that kept them out for this long kept them out as long as walls can. They cannot hold forever.",
            "A strategic loss. Measure the cost in territory, supply lines, and the morale of those who depended on those walls.",
            "The enemy is inside. That changes every calculation about this region. Begin the adjustment immediately.",
            "What was held is now lost. That is painful and it is also data — about the enemy, about your defences.",
            "A fortress once lost is harder to take back than it was to hold. That is the mathematics of siegecraft.",
            "The garrison did what it could. Do not spend grief on blame when the enemy is still moving.",
            "The supply lines through this settlement are now hostile. Reroute before the consequences compound.",
            "The people inside those walls are living under a banner they did not choose. Recover the settlement before that becomes permanent.",
            "A loss. Absorb it, understand it, plan the recovery. That is the only useful response.",
            "Even the strongest walls eventually yield to sufficient patience. Today, the enemy had sufficient patience.",
            // ── expansion ──
            "A fortress lost. The equation of this campaign just changed by more than the stones of those walls.",
            "Somewhere in there, people are waiting to find out what kind of conqueror just arrived. They will find out soon.",
            "The garrison did not hold. The reasons for that need to be understood before the next garrison is posted.",
            "A strategic position surrendered. Every other position that depended on it is now re-evaluated.",
            "The walls stand. The question is when you will stand inside them again.",
            "A loss that will ripple through supply lines, morale, and the decisions made at every council for the next month.",
            "Even a fortress temporarily lost is a fortress that the enemy is fortifying with every passing hour.",
            "The settlement is lost. Begin the process of taking it back without beginning the process before the army is ready.",
            "A defeat measured in stone and gates and supply lines and the morale of everyone who counted on those walls.",
            "They are inside now. Inside changes everything about how hard it will be to get them out.",
            "The flag has changed. The people inside it are living under it. Move before it becomes their normal.",
            "A position lost is a leverage point given away. The enemy now controls what you no longer control.",
            "Retreat in order. The settlement can be retaken. A scattered army cannot retake anything.",
            "Every stronghold that falls makes the next one harder to hold. The momentum is the enemy's.",
            "The cost of losing this settlement will be calculated over months. Begin calculating it now.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO KILLED — named line ({0} = hero name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] HeroDeathNamed = {
            "{0} has fallen. The company is poorer for it, and the cause feels the loss.",
            "The death of {0} will be felt for longer than this campaign.",
            "{0} will not see the end of this road. That is the truth, and it should be said plainly.",
            "A name worth remembering: {0}. See that it is.",
            "{0} is gone. That gap in the company will not be filled easily.",
            // --- new lines ---
            "The field took {0} today. Mark it.",
            "{0} died as they lived — in the thick of it.",
            "When the roll is called tonight, {0}'s name will be answered by silence.",
            "The world is smaller without {0}. That is not a poetic sentiment — it is a tactical fact.",
            "{0} is dead. Whatever they carried — knowledge, loyalty, skill — is lost with them.",
            "The campaign continues. {0} does not. That is the shape of this war.",
            "Say the name: {0}. Do not let the tempo of war make it too quick to speak.",
            "{0} will not see the outcome. Remember what they would have wanted it to be.",
            "Another name for the reckoning. {0}. Let it not be forgotten.",
            "The fallen: {0}. One more entry in a list that grows too fast.",
            // ── expansion ──
            "{0} fell where the fighting was hardest. That was always where they would be found.",
            "There will be a gap in the company named {0} that no one will quite fill.",
            "The name {0} should be spoken before this day is done and not forgotten after.",
            "{0} is gone. Say it plainly before the campaign moves on and takes the moment with it.",
            "The roll call will have a silence in it tonight where {0}'s answer should be.",
            "{0} was the kind of person the company built itself around. That kind of absence is structural.",
            "The field took {0}. The field takes what it takes, without preference or justice.",
            "{0}: dead in service of the cause. That is the whole of the record. It should be more.",
            "The campaign continues without {0}. That is the hardest sentence in any after-action.",
            "Write {0}'s name somewhere it will be read. The campaign should not be the only record.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO KILLED — ENEMY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyHeroKilled = {
            "A commander of the enemy falls. Their death is a wound in their ranks that will not heal quickly.",
            "One of their captains is dead. The soldiers who followed that name will need time to find another.",
            "A notable enemy is slain. The cause they served will find someone else — it always does.",
            "The enemy's champion is cut down. Let the enemy count what that cost them in more than numbers.",
            "A dark leader falls. Those who feared them will find a different fear now — of serving whoever comes next.",
            "Their great warrior is dead. Armies do not simply replace what they lose in the field.",
            "One who served the shadow falls. Good riddance to them — and good work to whoever dealt the blow.",
            "A name that will not be spoken in fear much longer. The enemy's leadership thins.",
            "A blow struck that will echo through the enemy's chain of command for some time.",
            "The field remembers every death, great and small. This one mattered.",
            "A commander removed. Wars turn on moments like this — sometimes slowly, but they turn.",
            "Death comes for all, even the great. On a day like today, it came for the right one.",
            "The enemy loses a leader and gains a vacancy that will be filled with someone less experienced.",
            "One fewer banner in the enemy's ranks. One fewer voice giving orders to those who would harm you.",
            "Their hero has fallen. Let it carry to every ear that serves against you — they are not invincible.",
            // --- new lines ---
            "The enemy captain will not be easily replaced. Good captains never are.",
            "A notable death on the other side of the line. The soldiers who served them know something has changed.",
            "Their greatest asset in this region is gone. Push the advantage before they recover.",
            "The commander who gave them cohesion is dead. That cohesion will fray before it is rebuilt.",
            "One of the enemy's best is off the field permanently. That is a different kind of victory than taking ground.",
            "The morale damage from a captain's death can outlast the tactical impact. Both are significant.",
            "A dangerous mind removed from the enemy's councils. The decisions they make will be worse for the absence.",
            "The enemy's chain of command has a gap in it now. Intelligence on who fills it is worth seeking.",
            "A death that changes the character of the opposing force. The new commander will not be the same.",
            "Good news from a dark field — one of the enemy's finest will trouble the cause no further.",
            "A name the enemy gathered around is gone. Watch how they reorganise around the absence.",
            "The campaign shortened itself today by removing the best of what opposed you.",
            "Their great captain falls. Take note of who steps forward to lead in the immediate aftermath.",
            "The death of a leader is always a crisis for the followers. Monitor the enemy's response.",
            "An adversary worth defeating has been defeated, permanently. Something to mark before moving on.",
            // ── expansion ──
            "The enemy's captain is dead. Captains are replaced — but not instantly, and not with the same experience.",
            "A field death of consequence. The soldiers who followed that leader will take orders from someone new tomorrow.",
            "The enemy's finest falls. Whatever made them effective dies with them.",
            "A noted enemy, removed from the field permanently. The campaign shortened itself.",
            "The soldiers who believed in that commander will have to find something else to believe in.",
            "The enemy lost something irreplaceable today — not a soldier, but the judgment that made soldiers effective.",
            "One of their best is gone. Track who steps forward to fill the role.",
            "A captain's death is a hole in a force that takes time to fill and is never quite filled the same way.",
            "The enemy lost someone today whose absence will shape every engagement until a new leader proves themselves.",
            "That particular threat to your campaign has been permanently addressed.",
            "A death on the enemy's side that will be felt differently than a numbers casualty.",
            "Their best captain fell doing what captains do. The cost to them is strategic as well as personal.",
            "News of this death will reach the enemy's councils before nightfall. Watch how they respond to it.",
            "An adversary worth defeating has been defeated permanently. Note it.",
            "Their champion falls. The void this creates in their command will be imperfectly filled.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO KILLED — COMPANION / ALLY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] AllyHeroKilled = {
            "A companion has fallen. The company is diminished by more than numbers today.",
            "They died in service of something greater than themselves. That is not comfort enough, but it is something.",
            "The field has taken one of your own. Their name should be spoken before the day is done.",
            "A comrade lost. Let their death have meaning by making sure it was not the last one necessary.",
            "One who rode with you will ride no further. The wound of it will be carried by all who knew them.",
            "Death comes without warning and without fairness. Today it proved both.",
            "A trusted ally is gone. The trust must be rebuilt in someone new — and that takes time.",
            "Not every death on the field is anonymous. This one had a name you knew, and now it is past tense.",
            "Those who fall alongside you deserve to be remembered. See that they are.",
            "A good soldier, a worthy companion, lost in the work they chose. There is grief in that, and honour.",
            "They stood where they were needed and did not step back. That is what it meant to know them.",
            "The world does not pause for grief. But for a moment, let yourself mark the loss.",
            "A gap in the company that cannot be filled by finding someone of the same name.",
            "They were part of the story being written here. Their chapter ends here. The story continues.",
            "Loss like this cannot be measured in gold or strategy. Take a moment — then keep moving.",
            // --- new lines ---
            "One of your own is gone. The grief is appropriate. So is the necessity of continuing without them.",
            "A trusted face will not be at the fire tonight. That particular emptiness takes a long time to fill.",
            "The company knows what was lost today. It does not need to be explained to them.",
            "A companion who made the hard road easier is on a different road now.",
            "The bonds between people who fight together are not easily made and not easily replaced. This one is severed.",
            "The duty they carried does not die with them. Someone else will carry it forward.",
            "Mourn them, honour them, and then do what they would have wanted — keep going.",
            "The campaign continues under a sky that is a little darker for the absence of someone who deserved to see its end.",
            "Those who fell beside you will be remembered with a clarity that those who lived never quite achieve.",
            "The hole left by a genuine ally is not filled by recruiting another — it is carried until the end.",
            "There is no shame in grief. There is only the question of what you do with it afterward.",
            "They are gone, and you must go on, and both of those things are true at the same time.",
            "A companion who fought with you, argued with you, carried their weight without complaint — gone.",
            "Their name will be in the records of this campaign. Make sure the records survive to be read.",
            "The light of a person you relied on has gone out. The road is darker for it. Walk it anyway.",
            // ── expansion ──
            "The loss is real and the grief is appropriate. Both can coexist with the necessity of continuing.",
            "They died doing what they came to do. That is not comfort — it is context.",
            "The company has a hole in it that has a name. That name should be spoken.",
            "Not every death in a campaign is anonymous. This one had a face you knew.",
            "A companion lost on the road that was, until today, shared.",
            "Whatever they carried — knowledge, skill, presence — it is gone with them.",
            "The fire they used to sit beside will be quieter tonight. That is the first of many places their absence will be felt.",
            "A trusted face is gone. Trust like that is built slowly and cannot be reconstructed by simply finding another person.",
            "The duty they carried does not die with them. Someone will carry it forward. Find who.",
            "Mourn them properly. Then do what they would have wanted done.",
            "The company knows its losses before the officers announce them. They already know.",
            "A death that will be felt in this company for longer than anyone currently expects.",
            "They chose to be here. Honour that choice by making their service count for something.",
            "Gone before the story they were part of reached its end. That is the thing about stories told in the middle of living them.",
            "One less person who believed the cause was worth fighting for. The cause still needs to be worth fighting for.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // WAR DECLARED — named ({0} = faction 1, {1} = faction 2)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WarDeclaredNamed = {
            "{0} and {1} are at war. The roads between them have become battle lines.",
            "War between {0} and {1}. Whatever grievance started it, the soldiers will pay the price.",
            "The conflict between {0} and {1} is now official. It was already real.",
            "{0} raises the banner against {1}. Two forces that might have stood together, now stand apart.",
            "Declaration of war: {0} versus {1}. The diplomats have finished their work. Now the soldiers begin theirs.",
            // --- new lines ---
            "{0} and {1} have chosen the sword over the table. The table will still be there after.",
            "The war between {0} and {1} will be decided on fields that neither their lords nor their heralds will walk.",
            "{0} marches on {1}. The first move of a game that ends in exhaustion or victory — rarely in wisdom.",
            "Heralds have carried the declaration from {0} to {1}. The heralds come back. The soldiers do not always.",
            "The last message between {0} and {1} was the declaration. What comes after is not words.",
            // ── expansion ──
            "The war between {0} and {1} is declared. It was already felt before anyone said it out loud.",
            "{0} and {1} exchange declarations. The documents are formal; the intentions were never in doubt.",
            "War: {0} against {1}. The outcome will be decided by soldiers who had no voice in the decision.",
            "The conflict between {0} and {1} becomes official. Whether it becomes decisive depends on what follows.",
            "{0} raises arms against {1}. A fork in the road that cannot be un-walked.",
            "{0} and {1} are at war. The people on both sides will learn what that means at different speeds.",
            "The declaration from {0} reaches {1}. The response is already being written in army orders.",
            "War between {0} and {1}. Every settlement between them is now in the potential path of something.",
            "{0} and {1} could not find terms. Now the finding will be done on a battlefield.",
            "Heralds have made the war between {0} and {1} official. The soldiers needed no herald.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // WAR DECLARED — generic flavor
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WarDeclared = {
            "War is declared. The drumbeats of conflict travel faster than any rider.",
            "Swords are drawn and old grievances are given fresh edges. The war begins.",
            "What diplomacy failed to hold, war will now decide. A poor trade, always.",
            "The banners of two factions face each other across a line that will cost blood to cross.",
            "War has come again to Middle-earth. As if it had ever truly left.",
            "Heralds and ultimatums have run their course. Now it is the soldiers' turn.",
            "A war declared is never as easy as those who declared it imagined.",
            "The last letters are sent, the last arguments made. Now there is only war.",
            "In the histories they will call this the beginning. For those who must fight it, it already feels old.",
            "Two causes take the field. Both believe they are the righteous one. Wars tend to disabuse both.",
            "Alliances are tested, borders redrawn, and ordinary people pay the ordinary price of extraordinary decisions.",
            "Let those who called for this war be the first to explain it to the families of the fallen.",
            // --- new lines ---
            "The peace was always fragile. Now it is shattered and the pieces are being gathered into weapons.",
            "A declaration of war is the loudest possible admission that every other option was refused or failed.",
            "War: the continuation of argument by other means, and always at greater cost than the argument warranted.",
            "The map changes when wars are declared. The people on the map change whether they want to or not.",
            "Those who chose this war will have reasons. The dead will have none.",
            "War declared — the ancient and efficient way of demonstrating that diplomacy ran out of ideas first.",
            "There were a hundred moments before this one where it could have been avoided. It was not avoided.",
            "Armies begin moving before the ink of the declaration is dry. Plans laid in secret are now public.",
            "The drumbeat of war carries to the villages, where people begin counting their food and mending their fences.",
            "A new war in an age already defined by too many wars. Middle-earth is patient — its people are not.",
            "The heralds are recalled. The ambassadors pack their trunks. The soldiers prepare their kits.",
            "When the talking ends, the marching begins. The marching has begun.",
            // ── expansion ──
            "Another war in an age that did not need one. And yet.",
            "The declaration is made. Everything downstream of it will cost someone something.",
            "War again. The machinery of it grinds forward with a momentum that declarations merely release.",
            "A war declared in haste is a peace made in exhaustion. Both sides should know that before they begin.",
            "The armies will march before the ink dries. Plans made in secret become orders made in daylight.",
            "The cause for this war will be debated for years. The dying starts now.",
            "War declared. The people who decided it will not be the people who die in it. Note the gap.",
            "Another chapter of conflict begins. The historians will name it something. The soldiers will name it something else.",
            "The diplomatic phase ends. The phase that burns things begins.",
            "When wars are declared, the first resource consumed is not men or gold — it is the assumption of ordinary life.",
            "A declaration of war is a statement of failure. Not always — sometimes it is the only option. But always a failure of something.",
            "The borders close. The roads between become contested. What was trade becomes intelligence.",
            "Two causes convinced of their righteousness face each other. The righteousness will be sorted out at the end, if anyone remembers to.",
            "The decision was made at a table. The results will be delivered on a field.",
            "War declared. Begin thinking about peace immediately — not to avoid the war, but to ensure it achieves something.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // PEACE MADE — named ({0} = faction 1, {1} = faction 2)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PeaceMadeNamed = {
            "{0} and {1} have made peace. The roads between them open again.",
            "A treaty between {0} and {1}. May it hold longer than the last one.",
            "The war between {0} and {1} is over. Count the cost when the smoke clears.",
            "{0} and {1} sheathe their swords. Whether by exhaustion or by wisdom is not always clear.",
            "Peace is declared between {0} and {1}. The soldiers on both sides are grateful, whatever their lords say.",
            // --- new lines ---
            "The war between {0} and {1} ends here. What comes next depends on how both sides behave in the first months.",
            "{0} and {1} find terms. The terms will be tested before they are trusted.",
            "The killing stops between {0} and {1}. Now the harder work of making peace durable begins.",
            "A peace signed between {0} and {1}. The ink dries on something fragile and necessary.",
            "Whatever divided {0} and {1} has been folded into a treaty. Fold it carefully — it opens back up easily.",
            // ── expansion ──
            "{0} and {1} reach terms. The terms will be tested over the next several years.",
            "Peace between {0} and {1}. Fragile, like all peace. Necessary, like all peace.",
            "The war between {0} and {1} closes here. What comes after depends on the terms and the will to keep them.",
            "{0} and {1} lay down their quarrel. May both sides find ways to make the laying-down permanent.",
            "The killing stops between {0} and {1}. The grievances do not — but the killing stops.",
            "{0} and {1} find a settlement. Whether it holds depends on whether the underlying issue was settled or just deferred.",
            "A peace between {0} and {1} that neither side fully wanted is still a peace. Build on it.",
            "The treaty of {0} and {1} is signed. It will last as long as both sides find it cheaper than war.",
            "{0} and {1} step back from the edge. That is not heroic. It is necessary.",
            "The war between {0} and {1} ends as wars should — at a table, with a document, without anyone dying.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // PEACE MADE — generic flavor
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PeaceMade = {
            "A peace is made. The swords are sheathed — for now. Let it last longer than the last one.",
            "The killing stops. Whether the reasons for it do is another matter.",
            "Peace is declared. Those still standing on both sides breathe a little easier.",
            "A treaty is signed. May it hold long enough to be worth the ink.",
            "The war is over. What it cost to end it will be counted for years.",
            "Silence returns to the field. An uneasy silence, but silence nonetheless.",
            "Both sides find it easier to agree now that the bleeding has made the argument clear.",
            "A peace purchased at great price. Guard it carefully — it is not free.",
            "The banners lower. May the next generation not have to raise them against each other.",
            "Swords beaten back into ploughshares. A proverb that is easier to say than to live.",
            // --- new lines ---
            "Peace. Not the end of grievance — the end of the organised violence of it, which is a start.",
            "The treaty is signed. The dead are not consulted, but they have opinions.",
            "An agreement that both sides can live with — literally, in this case, which is not always guaranteed.",
            "Peace made in exhaustion is still peace. Tend it before it becomes war made in frustration.",
            "The heralds ride again, this time with better news. The gates will open.",
            "Two armies told to stop fighting. Most of them are relieved. Some of them are not.",
            "A fragile thing, this peace. It needs tending and feeding and the active choice not to break it.",
            "The argument that the war was supposed to settle will not stay settled without effort on both sides.",
            "Peace declared. Begin rebuilding immediately — peace is expensive in its own way.",
            "Roads that were battlefields are roads again. Merchants notice these things before soldiers do.",
            "The fighting stops. The forgetting — which is the harder part of peace — takes much longer.",
            "A treaty. A beginning. Not a conclusion — a beginning. Make something of it.",
            "Middle-earth has seen many wars end in treaties that became the cause of the next war. Write this one more carefully.",
            // ── expansion ──
            "The swords are sheathed. The scabbards will be needed again. For now, they are sheathed.",
            "Peace made in the shadow of exhaustion is still peace. Rest, then prepare.",
            "The treaty is real and the relief is real and neither will last without effort.",
            "The arguments that caused this war have not been resolved by the peace. They have been adjourned.",
            "Peace. A state that requires active maintenance, like a fire in rain.",
            "The fighting stops and the counting begins — of the dead, the cost, the distance between what was wanted and what was achieved.",
            "A peace declared is a problem deferred and also an opportunity. Which it becomes depends on how it is managed.",
            "Both sides said enough, at approximately the same time. That convergence is what peace usually looks like.",
            "The peace is fragile in the way that peace always is — one misunderstanding, one opportunistic raid, one bad season away from resumption.",
            "The soldiers on both sides are eating the same meal tonight for the first time since the war began.",
            "An imperfect peace is still a peace. Perfect peaces exist only in histories written after the fact.",
            "The trade roads reopen. Commerce is the first and most reliable indicator of whether peace is holding.",
            "A peace that costs both sides is sometimes stronger than one that costs only one of them.",
            "The council that made the war is the council that made the peace. That is the one useful thing about councils.",
            "Make the peace work. The effort it takes is a fraction of the effort the war cost.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — DAWN
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DawnMessages = {
            "Dawn breaks over Middle-earth. The world is new for exactly long enough to give you hope.",
            "The sun rises over the mountains of the East. Another day to make something of.",
            "First light touches the peaks of Gondor. The darkness retreats, as it always does — for now.",
            "Morning comes. The campaign does not pause for it, but it is beautiful all the same.",
            "A new day on the long road. The birds do not know or care what the night brought.",
            "The light returns. In the old stories, dawn always brings a turning — hold on to that.",
            "Grey gives way to gold. Another morning in the long march of this age.",
            "The watch changes at dawn. Fresh eyes look out over a world that has not changed — but the light has.",
            "Mithril light on the morning hills. On a day like today, it is possible to believe that things may yet be well.",
            "The stars fade one by one as the sun claims the sky. Even Eärendil yields to the common dawn.",
            "Another sunrise. Another chance. Middle-earth is patient that way.",
            "The morning mist lies thick on the valleys. Out of it, anything might come — and often does.",
            "Sunrise finds your company on the road. There are worse places to greet the morning.",
            "The world wakes slowly. Your company does not have that luxury — but pause, just for a moment.",
            "First light on the river, on the road, on the distant mountains. A moment worth noticing.",
            // --- new lines ---
            "Dawn over the Pelennor — the light finds the stones of the city before it finds the valleys.",
            "The cock-crow at dawn. An ordinary sound that ordinary life is built on. Hold it in mind.",
            "The eastern horizon brightens before the sun clears it. A daily promise, kept without fail.",
            "Morning in the camp. The smell of cook-fires and dew and wet grass and the day beginning.",
            "The eastern light picks out the hilltops first, then works its way down into the dark places.",
            "Dawn — when the night's fears have names in the daylight, and half of them shrink away.",
            "The soldiers wake to birdsong and the distant sound of the world continuing as if nothing were wrong. It is sustaining.",
            "Light on the White Tower. Light on the river. Light on the roads that go everywhere the eye can reach.",
            "The sun rises over the shoulders of the mountains and reminds everything in the valleys that it is still there.",
            "A golden hour before the march begins — use it to eat, to plan, and to notice that the world is still worth defending.",
            "The first light carries no news of what yesterday cost. It simply arrives, as it always does.",
            "Dawn on a campaign road looks different from dawn in peacetime. But it is still dawn.",
            "The sky turns from black to grey to rose. The world was here all night. It is here again.",
            "First light on the march. However hard the day ahead, it begins with this — and that is not nothing.",
            "The birds of Middle-earth know nothing of the war. Their morning songs are proof the world has not entirely changed.",
            // ── expansion ──
            "Dawn over the Pelennor. A light that belongs to the age before the shadow, still keeping its promises.",
            "The cook-fires wake before the soldiers. Someone always wakes before everyone else, and it is usually in service of everyone else.",
            "A grey dawn — no gold this morning, just the flat light of a world getting on with it.",
            "The morning watch ends. The day watch takes the walls. Another transition in the long series of them.",
            "Dawn and the sound of the camp stirring. This is the everyday texture of a campaign.",
            "The sun finds the peaks first, then works its way down. The valleys are still dark when the mountains are lit.",
            "Morning in the field has a particular quality — dew on the grass, cold air, the smell of horses.",
            "Every dawn survived is a dawn that might be the last one before something changes. It is worth the pause.",
            "The light grows. With it, the problems of yesterday become the problems of today, under better visibility.",
            "A cold clear dawn — the kind that makes the world look sharply itself.",
            "The birds were singing before the sentries were relieved. An ordinary thing that ordinary life is made of.",
            "First light and the world's daily lie: that everything is fine and the day is just beginning.",
            "The east lightens and the stars surrender one by one, the old ones last.",
            "The morning of another campaign day. The work waits for the light as it always does.",
            "Dawn. The hour when what was imagined in the dark becomes either a plan or a fear.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — DUSK
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DuskMessages = {
            "The sun sets behind the White Mountains. Long shadows reach across the land.",
            "Dusk comes early here. Light your fires before the dark makes assumptions.",
            "Evening falls on Middle-earth. Whatever was not done today waits for tomorrow — if tomorrow comes.",
            "The last light fades from the western sky. Somewhere behind those hills, the Shire still has hobbit-holes with warm hearths.",
            "The sky turns the colour of embers. Beautiful in a way that battle never is.",
            "Dusk. The hour when plans are reviewed and regrets begin their nightly accounting.",
            "Stars appear one by one as the light fails. The night watch will have company.",
            "Long shadows lie across the road. The world is neither day nor night for this hour — a moment in between.",
            "The evening star rises. Those who know the old stories know its name and what it carried.",
            "Campfires dot the world at dusk — some yours, some not. In the dark, they look the same.",
            "Sunset behind the mountains paints the sky in impossible colours. War has not taken everything.",
            "The end of another day. Count those who began it with you and still stand at the end.",
            "The light fades from the peaks last, as if the mountains are reluctant to let the day go.",
            "A red sky at evening. The sailors' saying comes with different meaning in this country.",
            "The last birds take their final pass and settle. The night world begins to stir.",
            // --- new lines ---
            "Evening falls. The long light of a campaign day — the kind where everything that could happen, did — is over.",
            "The dusk watch is the hardest. The eyes tire and the mind drifts in the last of the failing light.",
            "Dusk over Gondor — the stone of Minas Tirith catches the last sun differently than any other stone.",
            "The horizon swallows the light slowly. There is mercy in how long the evening lasts.",
            "Fires lit against the dark. Every camp in this war, from Rohan to the borders of Mordor, does the same.",
            "The shadows of the eastern mountains fall west in the evening. They reach further than you would expect.",
            "Supper, sentries, and the quiet conversation of a camp settling in for the night.",
            "Evening on the road. The dust settles, the heat fades, the stars find their places one by one.",
            "The western light lingers long on the face of the White Mountains. Beautiful and indifferent.",
            "Dusk in the borderlands is a different quality of dark than dusk in the interior. Keep that in mind.",
            "The long shadows of late afternoon have merged and become evening. The world is smaller in the dark.",
            "Somewhere across this darkening land, the enemy is also making camp. Both sides sleep; only the war does not.",
            "The last of the light. Make your plans before it is gone — maps are harder by firelight.",
            "Evening — the daily reminder that the world has a rhythm that war cannot quite disturb.",
            "The day's work is done or it is not. Either way, the dark comes.",
            // ── expansion ──
            "The last light. Use it or lose it — both the daylight and what it revealed.",
            "The sun falls behind the mountains and the temperature drops by more than the number suggests.",
            "Evening in the camp. The logistics problems of the day are either resolved or deferred to the morning.",
            "The dusk watch is neither the hardest nor the easiest. It is the one that sees the light go.",
            "Campfire smoke begins to rise from every settlement in view. Both sides of this war do the same thing at dusk.",
            "The last light on the river. The river does not care about the war. It simply continues.",
            "Supper, sentries, the sound of tired soldiers settling into whatever rest the night allows.",
            "Dusk in the eastern sky is different from dusk in the west — darker, longer, less interested in beauty.",
            "The evening star rises at the same time each night. Something that has not changed and will not change.",
            "The long-light fades. Whatever was left undone today is now part of tomorrow's problem.",
            "The heat of the day leaves with the light. The cold of the night arrives with the same indifference.",
            "Night comes and both sides of this war disappear from each other's visibility, which is not the same as safety.",
            "The last birds settle. The first sentries of the night post begin their watch.",
            "Evening takes the colour out of things. An army at dusk looks the same as any other group of exhausted people.",
            "The sky turns and the stars appear. The war continues in the dark as it does in the light.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — NIGHT
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] NightMessages = {
            "Deep night. The stars of Middle-earth are clear and cold. The watch is doubled.",
            "Silence, broken only by the wind and the sound of something moving beyond the firelight.",
            "The night is old and the fire is low. Even courage needs to sleep sometimes.",
            "Stars wheel overhead. The old Elves knew their names. The darkness knows their light.",
            "In the deep hours of the night, the world feels very wide and very empty.",
            "The shadows are at their deepest before the dawn. The watch knows this better than anyone.",
            "Something calls from the dark beyond the camp — animal, wind, or something else. The sentries do not answer.",
            "Middle-earth at night belongs to different things than it does by day. Ride carefully if you must ride.",
            "The fire crackles. Stories are told in hushed voices. The night is long.",
            "Even the land sleeps, in its way. But not everything that moves in the dark is sleeping.",
            "The moon is hidden tonight. Stars are older comfort — colder, but more reliable.",
            "Night on the campaign road is never quite still. There is always something to listen for.",
            "Deep in the night, the campaign continues in people's dreams — which are not always peaceful.",
            "The hour before dawn is the longest. The watch knows it well.",
            "Night lies heavy on Middle-earth. Somewhere in it, plans are being made against you. And by you.",
            // --- new lines ---
            "The watch fire burns low. The sentry stares into a dark that does not stare back — not visibly, anyway.",
            "Night sounds carry differently in a war zone. Every twig-snap has implications.",
            "The camp breathes in sleep, ten thousand lungs finding a different rhythm in the dark.",
            "The stars of Varda wheel overhead, indifferent to the small wars of mortal peoples.",
            "Midnight. The deepest part of the night, when decisions made seem worse and hopes seem further away.",
            "The second watch. The hardest shift — too late to be fresh, too early to see dawn.",
            "Cold, dark, and the sound of the wind through the camp. The war continues even when no one is watching.",
            "Night falls on both sides of the line. Whatever intelligence was gathered today is being acted on in the dark.",
            "The stars were old when Men first walked Middle-earth. They will be older when the war ends.",
            "A scout returns in the dark, moving carefully through your sentries with something worth reporting.",
            "Deep night — when everything that was pushed down during the day has a chance to rise.",
            "The owls call from the treeline. In some traditions, a good sign. In others, the opposite.",
            "Fires dot the hillside like fallen stars. Some of them are yours. Know which ones.",
            "The night watch ends when the sky begins to lighten in the east. That lightening takes a long time when you are waiting for it.",
            "Even the war has a night shift. It is not restful for anyone.",
            // ── expansion ──
            "The deepest part of the night. The decisions made now feel larger than they are and the distances feel greater.",
            "Night in a war zone is not the same as night in peacetime. The sounds have different implications.",
            "The moon tracks west. The sentries track everything.",
            "Midnight on a campaign. The hour when the mind invents threats the morning will make ordinary.",
            "Something moves beyond the firelight. The sentries report it. It may be nothing. It may not be.",
            "The night smells different here than it did at the beginning of the campaign. A subtle thing, noticed only after weeks.",
            "Both armies are quieter in the deep hours. Both are still watching.",
            "The hour before dawn is when the body is coldest and the mind is weakest. The watch knows this.",
            "Night fog on the river. Every scout delayed, every patrol range halved.",
            "The camp fire burns to coals and no one adds wood. The cold comes anyway.",
            "Somewhere across the dark, the enemy is doing the same things: guarding, worrying, sleeping in rotation.",
            "The stars are the oldest navigators in Middle-earth. Elves named them. Men steered by them. Tonight, sentries use them.",
            "Deep night and the sound of something in the distance that could be many things. The watch notes it.",
            "Two hours before dawn — when the first birds begin and the last shift of the watch is at its most tested.",
            "The night will pass. Not quickly, in the way of nights on a campaign. But it will pass.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — SPRING
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonSpring = {
            "Spring returns to Middle-earth. The world did not forget how to renew itself, despite everything.",
            "Green comes back to the Pelennor, to the banks of Anduin, to the foothills of Ered Nimrais.",
            "The armies stir with the spring. Campaigns begin again as the roads dry and the passes clear.",
            "New growth everywhere — even in places the darkness tried to claim. Life is stubborn.",
            "Spring rains and the smell of earth turning. Even soldiers notice it, sometimes.",
            "The flowers return to the Fields of the Pelennor. They did not ask who fought there.",
            "Birds return to forests that were silent all winter. Not all winters end — but this one did.",
            "The world opens up again. Passes that were closed to you are open, and so are the roads.",
            "Spring is the season of campaigns and births — life and war resuming together, as they always have.",
            "Even in the shadow of Mount Doom, something green finds its way up through the grey ash. Eventually.",
            // --- new lines ---
            "The first warm day of spring is a unanimous, wordless argument that survival was worth it.",
            "The rains of spring turn the march roads to mud. A tactical inconvenience and a beautiful thing.",
            "Lambs in the Riddermark, blossoms in Ithilien, and the distant sound of the world waking up.",
            "The rivers run high with snowmelt. The fords are tricky. Plan the march accordingly.",
            "Spring in the White Mountains — the snowline retreats up the slopes and the passes breathe again.",
            "Wildflowers on the roadsides. The world is not only ash and iron, even in its worst years.",
            "The equinox passes and the days lengthen. More daylight to march, to fight, to see what's coming.",
            "Spring mud is the campaign commander's oldest enemy. The army slows; plans bend; supply strains.",
            "The smell of spring in the Shire would break hearts if anyone from these parts could compare notes.",
            "The season of green and growing. Take from it what sustenance you can — literal and otherwise.",
            // ── expansion ──
            "The passes clear. Armies that were frozen in place can move again.",
            "Spring turns the mud to passable road and the passable road to invitation.",
            "The first warm rain of spring falls and the grass begins immediately. Middle-earth is impatient to be green.",
            "The melt-water runs off the mountains and the rivers run fast. The fords are difficult until mid-spring.",
            "Spring brings new problems: disease spreads faster, supply lines strain with the campaign pace resuming.",
            "The trees come into leaf and the ambush cover with them. Note that as you plan the spring advance.",
            "Longer days mean longer marching windows. Use them before the summer heat makes afternoon marching expensive.",
            "The equinox passed and the year tips toward warmth. Even the soldiers breathe differently.",
            "Spring in the Ithilien gardens — wildflowers between the stone walls and the smell of new growth.",
            "The roads are drying. The scouts who have been waiting since winter are already moving.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — SUMMER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonSummer = {
            "High summer in Middle-earth. The heat presses down on soldiers and strategy alike.",
            "Long days — a campaigning army can cover more ground with more light to march by.",
            "The crops of Gondor and Rohan stand tall. If the raids do not reach them, there will be food this winter.",
            "Summer light on the Anduin — great and broad and bright. It looks peaceful from a distance.",
            "The heat of summer weighs on armoured soldiers. Morale and water both need careful tending.",
            "Full summer — the world at its most alive, and the armies at their most active.",
            "Dust rises from every road in the long dry heat. Armies leave clouds of it behind them.",
            "The Haradrim march better in this heat than Gondorians. Keep that in mind when choosing ground.",
            "Midsummer and the war goes on. The seasons do not negotiate with armies.",
            "The land is green and full and beautiful. It is a good thing to remember what you are fighting for.",
            // --- new lines ---
            "The heat of summer turns the Pelennor into a shimmering oven. Armouring up takes on new meaning.",
            "Long marching days and short, bright nights. Make use of the hours — the season will not last.",
            "Water becomes a strategic resource in summer. Mark every well, every spring, every river ford.",
            "Midsummer in Rohan — the grass is tall and golden and the horses are at their peak.",
            "The heat shimmer makes it hard to count distant forces accurately. Factor that into your intelligence.",
            "Summer in the Ithilien forests is as close to paradise as anywhere in the southern lands. It is worth fighting for.",
            "The long summer days press on armoured men harder than on lightly-dressed raiders. That is a tactical reality.",
            "Flies on the march in summer. One of the many reasons soldiers develop strong opinions about seasons.",
            "Midsummer bonfires in the villages, where peace holds. A reminder of what all the fighting is for.",
            "The sky is vast and blue and empty of everything but heat. Good days to move. Hard days to stand still.",
            // ── expansion ──
            "The high summer sun beats down on every march. Heat casualties are real. Manage them.",
            "Summer in the flatlands means dust. Dust means your column can be seen from a great distance.",
            "Long days give the army more working hours, but the heat of afternoon is a genuine physical cost.",
            "The summer drought in some regions makes water a tactical resource. Know where it is.",
            "Midsummer and the Anduin is lower than it is in spring. The fords are easier. Note both sides of that fact.",
            "Summer campaigns move faster but cost more in water and heat-related attrition.",
            "The crops are ripening. Raiders know the harvest timing as well as the farmers do.",
            "Heat shimmer on the southern plains makes reconnaissance estimates unreliable. Factor the uncertainty.",
            "The flies of summer do not care about the war except as a source of their particular interests.",
            "High summer means high campaign pace for everyone. The enemy is also moving faster.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — AUTUMN
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonAutumn = {
            "Autumn comes to Middle-earth. The hills turn gold and red — beautiful, and a little melancholy.",
            "The harvest is in, or it is not. The armies know which, and it shapes everything about the winter ahead.",
            "Leaves fall on the road to war. The trees do not care about strategy.",
            "Autumn mist on the river, on the valleys, on the faces of soldiers who have been at it too long.",
            "The year is dying gently. The campaign must resolve something before the roads close for winter.",
            "In the Shire they would be preparing the harvest feast. Out here, there is only the march.",
            "Smoke from burning villages and burning leaves — a season that always mingles fire and beauty.",
            "The passes will close soon. Plans made for autumn need to be executed before the snow does the planning instead.",
            "Golden and amber — the world makes its peace with dying beautifully every year.",
            "Autumn rains begin. The roads are going to get harder before they get easier.",
            "Mordor has no autumn — just varying degrees of ash and shadow. All the more reason to fight for the lands that do.",
            // --- new lines ---
            "The harvest is in the barns or it is burning in the fields. Both matter enormously to the winter campaign.",
            "The smell of autumn — damp leaves, woodsmoke, apples, and the first cold coming down from the peaks.",
            "The last campaigning season before the mountain passes close. Make it count.",
            "Autumn in Rivendell. Even the valley of Imladris cannot entirely resist the season.",
            "The geese fly south in long V-formations. They know something about the coming weather. Take note.",
            "The days shorten. Each one is a little less time to finish what was started in spring.",
            "Autumn rain in the Grey Havens country — steady, salt-smelling, and cold enough to matter.",
            "The Ents would call this a slow season, if Ents could be persuaded to notice seasons particularly.",
            "Harvest time in the villages that are still intact. Go and see one, if you can. It is why the war exists.",
            "The last of the summer warmth is traded for long amber evenings and the knowledge that winter is keeping its own schedule.",
            // ── expansion ──
            "The last campaigning window of the year. Whatever is to be achieved before winter must be achieved now.",
            "Autumn light is low and beautiful and irrelevant to the march schedule.",
            "The harvest is being completed in every village that still has people to complete it. The army's winter depends on it.",
            "The trees lose their cover. What was hidden in summer is visible in autumn. That applies to both sides.",
            "The rains start. The roads that were hard are becoming soft. Plan for slower going.",
            "Autumn mud in the lowlands is a different consistency from spring mud. Heavier.",
            "The passes are still clear. They will close in another month. Plan accordingly.",
            "Autumn in Rohan — the grass turns gold and the horses fat. A good season for cavalry.",
            "The last days of warmth are finite. Make decisions that account for the winter arriving on schedule.",
            "The leaves provide cover in odd ways — fallen leaves make noise; remaining leaves provide shade.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — WINTER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonWinter = {
            "Winter descends on Middle-earth. The cold is not an enemy — but it is an obstacle.",
            "Snow on the passes, frost on the road, ice on the rivers. War slows but does not stop.",
            "The Misty Mountains are closed. Whatever was north of them must stay north for now.",
            "A cold wind blows from the north, from the Forodwaith where things older than Sauron sleep in ice.",
            "Winter is its own siege. Supply lines are the walls — let them break and the army breaks too.",
            "The dark and the cold together test a company's will. Keep the fires burning and the purpose clear.",
            "Short days and long nights. More time for the night watch to wonder about things they cannot see.",
            "Ice on the Anduin. The great river slows but does not sleep — and neither does the war.",
            "Midwinter. Even the Orcs slow down in the cold, though they slow down less than you would wish.",
            "The world is quieter under snow. Sound travels further. Keep the noise of the camp low.",
            "Winter campaigns are hard on men and horses. Move carefully, supply generously, do not rush.",
            "Even the mountains look more formidable with their white crowns restored. A reminder that some things are bigger than armies.",
            // --- new lines ---
            "The frost bites at night and does not fully release by noon. Both horse and soldier feel it in their bones.",
            "Winter in the White Mountains — the world of ice and silence that makes the warm valleys below seem miraculous.",
            "Supply lines in winter are fragile things. A snowstorm can isolate a position as effectively as a siege.",
            "Short days mean shorter marching windows. Adjust the column's ambitions accordingly.",
            "The silence of a snowfall over a camp. Brief, white, and complete.",
            "Frostbite is a battlefield casualty as real as any wound. Tend cold soldiers as you would tend any others.",
            "The dark is not the enemy's natural state exclusively. In winter, it belongs equally to both sides.",
            "Cold makes the enemy slower. It makes you slower too. The question is which side has planned better for it.",
            "Ice on the roads means horses fall and wagons overturn. The army's pace must acknowledge the ice.",
            "Winter in Mirkwood — the forest is darker in winter, and it was never particularly welcoming in summer.",
            "The cold tests what is made of in a way that comfort never does. The company will know itself better by spring.",
            "Midwinter — when the distance to both the beginning and the end of the campaign feels equally great.",
            // ── expansion ──
            "Winter fortifies positions that summer left open. The season is its own wall.",
            "The passes are closed. Whatever was north of the mountains will stay north until the thaw.",
            "Cold iron and cold fingers do not work together. Winter maintenance of equipment is critical.",
            "Supply lines in winter are half the capacity of summer lines. Plan the garrison provisions accordingly.",
            "Winter in the Misty Mountains is not winter in the lowlands. Both are serious. One is lethal without preparation.",
            "The enemy also slows in winter. Use the relative quiet for what it is — preparation.",
            "The short days mean the army spends a third of every day in darkness. Adapt the schedule.",
            "Frost on the ground means noise on patrol. What you cannot hear in summer, you can hear in winter.",
            "A midwinter siege is a particular kind of brutality — for the besieger as much as the besieged.",
            "The cold is neutral between enemies. The side that prepares better for it gains the advantage.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SHADOW & DREAD INTEGRATION — TIER: UNEASY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DreadUneasy = {
            "There is a nervousness here that the people cannot quite name. It is the shadow, felt before it is seen.",
            "The taverns are quieter than they should be. People speak in lower voices. The dread is beginning.",
            "Children are kept indoors later in the evening. The elders know what that means.",
            "A low, persistent unease has settled on this place. Not panic — not yet — but the beginning of it.",
            "Trade has slowed. Merchants feel it before soldiers do — the shadow on the road.",
            "The walls feel less solid than they did a month ago. Nothing has changed in the stone. Everything has changed in the people.",
            "The night watch reports nothing unusual. Yet something has changed in the quality of the nothing.",
            "Rumours travel faster than facts in a fearful town. The dread breeds stories.",
            "Small signs — a militia patrol that didn't return, a wagon that arrived late, a gate closed an hour early.",
            "The people sense the shadow even when they cannot see it. Animals feel it first.",
            "Dread takes root in small things — a dark rider seen on the horizon, a fire in a distant village.",
            "The mood of a town can turn in a week. Something has been turning here for longer than a week.",
            // --- new lines ---
            "The marketplace is not empty — but the laughter has gone out of it.",
            "Innkeepers ask what road you came by before they ask your business. They want to know what is behind you.",
            "The shrines at the crossroads have been visited more than usual. People are praying for things they cannot name.",
            "The mill still runs. The forge still rings. But both millers and smiths now keep one eye on the road.",
            "Dogs bark without cause at the perimeter of the settlement, several times a day, at nothing visible.",
            "The first sign of dread is not fear — it is quiet. The quiet that replaces ordinary conversation when everyone is listening.",
            "The road in is busy; the road out is busier. Migration is the early warning sign of settlements in trouble.",
            "A subtle wrongness pervades the settlement. The kind that is hardest to explain because it is hardest to prove.",
            // ── expansion ──
            "A particular quality of hush has settled over this place. Not quiet — hushed. There is a difference.",
            "The livestock are restless. Animals read the mood of a place before people can articulate it.",
            "The elders are quieter than usual. The young do not know what to make of it yet.",
            "The roads in and out are busier than normal. Not flight — not yet. Just people moving, watching, calculating.",
            "Children are not playing outside as late as they used to. Parents make decisions in aggregate.",
            "The watchman said nothing unusual in the night report. But they delivered it in a lower voice than last week.",
            "A low-grade wrongness that everyone feels and no one has named yet.",
            "The mill still runs. The market still functions. But the smiles have gone stiff.",
            "The tavern fills earlier and empties earlier. People are not lingering.",
            "The dread begins as an adjustment to ordinary routines — small, private, plausibly deniable.",
            "The gates are not closing early yet. But someone checks them every evening, which they did not do before.",
            "There is a quality in the air of this settlement that has changed. It is measurable in the particular way people look at strangers.",
            "The first sign of dread in any settlement is the modification of ordinary behaviour. This settlement is modifying.",
            "A low-level unease that has no specific object. That is often more persistent than fear of a named thing.",
            "The town crier's voice is lower. A small thing. But small things add up in settlements gripped by dread.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SHADOW & DREAD INTEGRATION — TIER: FEARFUL
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DreadFearful = {
            "Fear has taken hold here. The militia drills with less conviction and the gates close earlier each evening.",
            "The shadow does not need an army to break a people. Fear alone, tended carefully, can do the same work.",
            "Shopfronts are boarded in the afternoons now. The people are making decisions about what they are willing to risk.",
            "Militia desertion is beginning. Those with elsewhere to go are quietly finding their way there.",
            "Children whisper about dark things in the streets. Not all of it is imagination.",
            "The shadow has a name here now — the people call it something. When fear earns a name, it has become real.",
            "A fearful town is a town that will not hold if the actual enemy arrives. This must be addressed.",
            "Morale in the garrison is visibly declining. Orders are followed, but the edge is gone from the obedience.",
            "People are leaving. Not in a flood — not yet — but in ones and twos and the occasional family.",
            "Patrols return with reports of shadows on the road, of fires in the distance, of sounds in the dark.",
            "The shadow is not here yet. But the fear of it is — and sometimes fear does more damage than the thing itself.",
            "Whatever has been happening in the surrounding villages has reached the ears of this settlement's people.",
            "Food stores are being hidden by some households. They do not trust the walls as much as they used to.",
            "The watch is doubled but the watchmen themselves are not confident. That is a poor combination.",
            "When people are this afraid, they begin to look for someone to blame. Ensure it is not someone innocent.",
            // --- new lines ---
            "The garrison drills every day, which is good. Their hearts are not in it, which is the problem.",
            "Even the bravest people in a fearful town begin to recalculate their personal risks. Watch for it.",
            "Rumours fill the space that information should occupy. Send good intelligence to the people here.",
            "The settlement's physician is busier than usual with ailments that have fear as a root cause.",
            "People here no longer look strangers in the eye. That particular change is a reliable measure of how bad it has gotten.",
            "The wealthy are the first to send their families away. The middle sort follow. The poor have nowhere to send anyone.",
            "The blacksmith works double shifts. Not in optimism, but in contingency.",
            "A settlement gripped by fear is already half-surrendered — not in battle, but in will.",
            // ── expansion ──
            "The settlement is afraid and beginning to make the practical decisions that afraid settlements make.",
            "Stockpiling, boarding, sending children away — the quiet arithmetic of a community preparing for the worst.",
            "The fear has moved from private to collective. Shared fear changes the character of a place.",
            "The garrison can see it in the faces of the people they are supposed to be protecting.",
            "The merchants have already begun requesting escorts for deliveries that used to run unprotected.",
            "The patrols have been requested to double their visibility. The people need to see that someone is watching.",
            "A fearful town makes bad decisions in aggregate — panic buying, rumour-spreading, leadership challenges.",
            "The people here are doing the calculus of risk every waking hour. The calculus is coming out badly.",
            "When fear becomes collective, the mood of the settlement becomes self-reinforcing. Break the loop.",
            "Fear is the enemy's advance scout. It goes ahead of their armies and softens the target.",
            "The walls feel shorter to the people inside them when those people are afraid.",
            "The garrison commander is holding the line in terms of discipline. It is costing them significant effort.",
            "The people who have somewhere to go are beginning to have quiet conversations about whether to go.",
            "The blacksmith has received requests for knives from people who are not in the habit of carrying them.",
            "A fearful settlement feeds its own fear. Each bad report amplifies the interpretation of the next one.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SHADOW & DREAD INTEGRATION — TIER: TERRORISED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DreadTerrorised = {
            "Terror has gripped this place. The garrison is hollow-eyed and the streets are nearly empty by midday.",
            "Whatever hope remained here is burning very low. The people are very close to breaking.",
            "The shadow has won here — not through battle, but through patient, grinding fear. Act before it becomes permanent.",
            "Militia has abandoned posts. Those that remain do so without confidence that it will matter.",
            "This settlement is on the edge. One more raid, one more rumour, one more night of sounds in the dark, and it may simply empty.",
            "The dark tide of dread has nearly submerged this place. Hold it up or watch it go under.",
            "Even the garrison commander speaks in terms of 'when they come,' not 'if they come.' That is not good.",
            "A town in terror does the enemy's work for them. Each fearful citizen helps no one.",
            "The shadow does not even need to send an army here anymore. It has already sent its vanguard — and the vanguard is fear.",
            "Children who grew up knowing war are different from their parents. The ones here are growing up knowing dread.",
            "Something must be done. Not eventually. Not when the army is free. Now.",
            "The walls are technically manned. But the men on them are not watching outward with confidence — they are waiting for something they already believe is coming.",
            // --- new lines ---
            "There is a threshold where fear becomes self-fulfilling. This settlement is close to it.",
            "The settlement has stopped sending its young men to the militia. That is an irreversible kind of surrender.",
            "The streets empty at dusk now — hours before they should. The dark has learned to come early here.",
            "Every message out of this settlement is a cry for help. Every message going unanswered increases the dread.",
            "The priest or elder who maintained morale here is gone or has given up. That is the most dangerous loss.",
            "When people stop investing in tomorrow — stop repairing, stop planting, stop planning — the settlement is already lost in spirit.",
            "A town in this state cannot defend itself. It cannot even adequately feed and organise itself. Intervene now.",
            "The shadow has been making this investment for months. It is about to collect.",
            // ── expansion ──
            "Terror has a specific signature: the absence of future planning. This settlement has stopped planning.",
            "The people here have stopped investing in tomorrow's ordinary life. Gardens not tended. Roofs not mended.",
            "A settlement under terror is already partly surrendered in the way that matters most — in the will of its people.",
            "The garrison is manned but hollow. The bodies are present; the resolve is elsewhere.",
            "The shadow won something here before it ever arrived in force. It won the decision to resist.",
            "The only thing more dangerous than a terrorised settlement is a terrorised settlement that has decided resistance is pointless.",
            "When the market stops operating and the gates close at noon, the settlement has already made its decision.",
            "People here are not planning to fight. They are planning to survive whatever comes. Those plans look very different.",
            "The community that was here six months ago has been replaced by something that has the same buildings but a different spirit.",
            "Terror is the shadow's cheapest weapon and its most effective preparation for occupation.",
            "The garrison is present. The garrison is also frightened. That combination is a specific kind of problem.",
            "Children here are being told stories to help them sleep. The stories are not working.",
            "The threshold between a terrorised settlement and a collapsed one is crossed by a single significant event.",
            "Whatever presence you show here must be genuine and sustained. Anything temporary will accelerate the collapse.",
            "This settlement needs someone to believe in it before it can believe in itself again.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SHADOW & DREAD INTEGRATION — TIER: BROKEN
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DreadBroken = {
            "The shadow has broken this place. Not with fire or sword — with relentless, accumulated dread. The cost of rebuilding will be enormous.",
            "This settlement is lost in spirit even if it still stands in stone. The people have given up.",
            "The garrison has crumbled. Those who remain are going through motions nobody believes in anymore.",
            "You have allowed the shadow to win here without a battle. That is, in its way, the worst kind of defeat.",
            "Even if the enemy never arrives, the damage is done. A broken town is no better than a ruined one.",
            "The people have stopped speaking of the future. That is the surest sign that they no longer believe they have one.",
            "What takes years to build in trust and security can be destroyed in weeks by the right application of fear. You have watched it happen.",
            "A town in this state will not contribute to the war effort. It will drain from it.",
            "Even the buildings look different when the people inside them have given up. The walls are the same. The weight of them is not.",
            "Sauron does not need to conquer what has already surrendered to despair.",
            // --- new lines ---
            "The settlement has crossed the line from fearful to compliant. When the enemy arrives, the gates may open without a fight.",
            "A broken settlement is a strategic liability. Every resource spent maintaining it is resource not spent elsewhere.",
            "The people here have made their peace with a future they did not choose. That acceptance is a kind of death.",
            "There is nothing left here that hope can catch on. It needs something to hold before it can grow again.",
            "The kind of rebuilding this place needs is not measured in walls or supplies — it is measured in years of patient presence.",
            "Even the most broken settlement has people in it worth saving. Do not write it off while they are still there.",
            "The shadow achieved here what armies could not: the voluntary surrender of the will to resist.",
            "To rebuild this, you need more than soldiers. You need teachers, healers, people who can restore the idea of a future.",
            // ── expansion ──
            "The will of this settlement has been broken by something that never fired an arrow or drew a blade.",
            "A broken settlement does not resist the enemy's arrival. It accommodates it.",
            "The people here will cooperate with whoever governs them next. That is not collaboration — it is the end of choice.",
            "The shadow won here without a siege because sieges are expensive. Fear is cheap.",
            "What you see in this settlement is what the shadow is doing to every settlement it can reach — with patience and with purpose.",
            "The long investment of fear has compounded into something that changes the character of the place.",
            "To rebuild this, the work is not in the walls or the supply lines. It is in the people.",
            "A broken settlement shows you the endpoint of a process that begins with unease. Every other settlement is somewhere on that line.",
            "The garrison walks the walls but the people do not look at them with confidence. That gap has become permanent.",
            "What the shadow breaks, it intends to hold. A broken people are easier to hold than a defiant one.",
            "The silence here is the silence of people who have decided what the future holds and have stopped arguing with it.",
            "Even the youngest people here carry something in their faces that should not be there at their age.",
            "This settlement can be physically rebuilt. The community that once lived in it cannot be rebuilt the same way.",
            "The shadow's achievement here is not military. It is psychological. The remedies are correspondingly different.",
            "It is not too late for the people here — but it is later than it should be, and the window is smaller.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // SHADOW & DREAD INTEGRATION — DREAD FALLING / RELIEF
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DreadFalling = {
            "The shadow lifts a little. The streets are fractionally fuller, the faces fractionally lighter.",
            "Something has shifted here — the dread is receding, slowly, as dread always does when given less to feed on.",
            "The garrison stands a little straighter. Small signs, but signs nonetheless.",
            "The fear that settled here is not gone — but it is no longer growing. That is a beginning.",
            "Children are playing in the afternoon again. The simplest sign that things are improving.",
            "The marketstalls are opening earlier. People vote with their routines — and the routines are returning.",
            "The shadow recedes where the light is tended. Feast, patrol, presence — these things work.",
            "The watch reports a quieter night. Not quiet — but quieter. Progress.",
            "A collective breath out. The dread is still there, in the walls and in people's eyes, but it is loosening its grip.",
            "The road feels less dangerous than it did last week. That feeling is contagious — and valuable.",
            // --- new lines ---
            "A song in the market — the first in some weeks. It sounds tentative, but it is there.",
            "The militia drills with a little more conviction today. Something has changed in the quality of the morning.",
            "People are staying in the taverns past dark again. Not many, and not late, but it is happening.",
            "The road out of town is quieter than it was. The departure of frightened people has slowed.",
            "Word of your presence is having the expected effect on local morale. Keep the presence visible.",
            "The dread was fed by absence and rumour. Your arrival has addressed one and begun to counter the other.",
            "Recovery is slower than collapse, always. But it follows the same path in reverse — small signs, accumulating.",
            "Someone re-opened the shrine at the crossroads. Someone else left a flower there. Small gestures toward an ordinary future.",
            // ── expansion ──
            "The particular quality of fear that had settled here is lifting slightly. The market opened an hour later than it closes now.",
            "The change is small and the people will not remark on it today. But the direction of it has reversed.",
            "The dread is receding — measurably. People are staying out later. Doors are opening earlier.",
            "The livestock have calmed. That is usually the last sign — it means the people's anxiety is visible to the animals, and is now lower.",
            "A watchman reported hearing laughter near the well this afternoon. An insignificant detail. Note it anyway.",
            "The market is full an extra half hour each day. That is not accidental. It reflects a choice being made.",
            "Children are playing in the street again. That particular signal is the most reliable of all.",
            "The fear has not gone. But it has stopped growing. That is the beginning of the second half of recovery.",
            "Something has shifted in the balance between what is feared and what is hoped for. Hope is winning a little.",
            "Dread ebbs the same way it flows — slowly, unevenly, and in ways that are visible only after the fact.",
            "The inn is noisier than it was two weeks ago. Not by much. By enough.",
            "The settlement is beginning to believe that tomorrow exists and will be navigable. That belief is everything.",
            "The dread was fed by absence of protection and presence of threat. Both are being addressed. The dread reflects it.",
            "Recovery from dread looks like ordinary life returning in small increments. It is the most valuable thing that can happen here.",
            "The settlement is not well. It is better. In this context, better is significant.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // DREAD — SETTLEMENT CONTEXT ({0} = settlement name, {1} = dread value, {2} = tier label)
        // ═══════════════════════════════════════════════════════════════════════
        // {0} = settlement name, {1} = DreadTierLabel word (Uneasy / Fearful / Terrorised / Broken)
        public static readonly string[] DreadSettlementContext = {
            "The shadow lies heavy upon {0}. The mood there has turned {1}.",
            "{0} trembles under a burden the soldiers cannot name. Whatever has taken root there is {1}.",
            "The people of {0} are afraid, and have been for some time. The place has gone {1}.",
            "In {0}, the darkness has taken hold in something less visible than an army. The mood is {1}.",
            "Fear spreads through {0} like winter. It has grown {1} in there.",
            "{0} needs attention before the shadow deepens further. The dread there has turned {1}.",
            "The mood inside {0} has shifted — it is {1} now. A different kind of problem than open resistance.",
            "Something is wrong in {0}. Not on the walls — inside them. The dread there is {1}.",
            "The fear in {0} has reached a {1} state. Every decision made within those walls carries that weight.",
            "{0} has gone {1}. The shadow has been working there quietly for some time.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — NEAR SETTLEMENT ({0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleVictoryNearSettlement = {
            "A victory near {0}. The people behind those walls will breathe easier tonight.",
            "The enemy is driven back from the approaches of {0}. The settlement is safer — for now.",
            "The battle near {0} is won. The shadow recedes from this part of the road.",
            "Your victory echoes to the walls of {0}. Let the garrison know — morale is a resource.",
            "The field near {0} belongs to you. What you do with it next will define the next chapter.",
            // --- new lines ---
            "Fought and won within sight of {0}'s walls. The people watching from the battlements saw what courage can do.",
            "A battle hard won in the shadow of {0}. Send word in — the garrison deserves to know.",
            "The approaches to {0} are cleared. The road in and out is open. Use it.",
            "Victory near {0} means one less threat on its doorstep. The settlement has more room to breathe.",
            "The field belongs to you, and {0} is safer for it. That double accounting is worth marking.",
        };

        public static readonly string[] BattleDefeatNearSettlement = {
            "The loss near {0} will be felt within its walls. The people will have seen what happened.",
            "Defeat near {0} — the settlement is now more exposed than it was this morning.",
            "The battle near {0} is lost. The enemy presses closer. Reinforce what you still hold.",
            "Your retreat from the approaches of {0} changes the strategic picture significantly.",
            "The shadow grows nearer to {0} today. Do not let it grow nearer still.",
            // --- new lines ---
            "Driven back past the walls of {0}. The garrison saw it happen. Address morale inside before it compounds.",
            "The defeat near {0} opens the road to its gates. That road must be re-closed.",
            "A loss in {0}'s shadow means {0} is now within the enemy's reach. Act accordingly.",
            "Whatever confidence the people of {0} had in the field defences took a blow today.",
            "The enemy is now between you and {0}. Recovering that position is the first priority.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // ROADS OF THE KING INTEGRATION — ROUTE ACTIVE
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] TravelOnRoute = {
            "The ancient roads hold. Even in these dark days, the old stonework of Númenor carries its freight faithfully.",
            "Your company follows the route — each step a commitment to the plan laid before the march began.",
            "The roads of the old kingdom still know how to be roads. Trust them.",
            "Keeping to the planned route costs discipline. It also saves the kind of time that gets soldiers killed when wasted.",
            "The great roads of Middle-earth were built to last. They have outlasted empires. They will outlast this war.",
            "On the planned road, logistics are manageable. Off it, they are a nightmare. Stay on it.",
            "The old way-markers still stand. Those who built them expected someone to follow them — here you are.",
            "A route planned in advance is a route with time to prepare supplies, allies, and alternatives.",
            "Even in chaos, a known route is an anchor. The army that marches with purpose arrives — the one that wanders often doesn't.",
            "These roads remember every army that marched them. Yours is one more line in a very long history.",
            "Follow the ancient roads and the land itself seems to want to help you. The roads were built that way.",
            "The march is easier on old stone than on mud. The builders of the Númenórean roads understood that.",
            // --- new lines ---
            "The route holds. Another league of old road behind you, another ahead.",
            "The King's Road was built for exactly this — armies moving with purpose across great distances.",
            "The stones of the ancient road speak to feet that know how to listen. Keep moving.",
            "On the old road, the distances make sense. Off it, every league seems to stretch.",
            "Each milestone is a small confirmation that the plan holds and the route is sound.",
            "The logistical advantage of a planned route compounds daily. Today you feel it in the pace.",
            "Númenórean engineers built for permanence. The road is in better shape than anything built since.",
            "The route to the next waypoint is clear and known. That alone is worth three days of planning.",
            "Old roads, new purpose. The Númenóreans who built these would recognise what you are doing with them.",
            "A march on the King's Road is a march with the history of the realm beneath your boots.",
            "The route holds. No deviations, no surprises. This is the ideal and it is being achieved.",
            "Trust the road. It was built by people who trusted that those who came after would need it.",
            // ── expansion ──
            "The planned road is the road being followed. Each league on schedule is a tactical success.",
            "Old roads know how to be roads. They also know something about patience.",
            "The march follows the route. Supply is meeting demand at the right waypoints. The plan is working.",
            "The roads of Númenor were built for exactly this — armies moving at campaign pace across known distances.",
            "On the planned route, the logistical picture is legible. Off it, everything has to be re-estimated.",
            "The route holds. Each day on the old road is a day that does not require improvisation.",
            "Keeping the column to the planned road is a discipline that pays dividends at the end of a long march.",
            "The old stones underfoot were placed by hands that intended them for this exact use.",
            "The march covers the expected ground. The supply wagons are where the map said they would be.",
            "Following the King's Road means your supply chain has landmarks. That makes it defensible.",
            "The planned route carries its own intelligence — waypoints speak to previous commanders' knowledge of the land.",
            "The army moves as its route intended. That intention was laid down by a careful commander. Trust it.",
            "Another league on the old road. Another league that does not need to be guessed at.",
            "The route marks the fastest path that is also the safest one. Both qualities are being tested today.",
            "On the road, the campaign has rhythm. Off it, the campaign has improvisation. This is the road.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ROADS OF THE KING — ROUTE CONTEXT ({0} = next stop name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RouteActiveContext = {
            "Your company follows the planned road — next stop: {0}. The route holds.",
            "The march continues. {0} waits at the end of this leg. Stay on the road.",
            "Roads of the King: the route is active. {0} is your next waypoint. Press on.",
            "The planned road to {0} stretches ahead. Every step on an old road is a step on solid ground.",
            "Your route marks {0} as the next halt. Rest, resupply, and move on.",
            // --- new lines ---
            "The road ahead leads to {0}. Keep the column together and the pace steady.",
            "Next waypoint: {0}. The road is clear and the route is sound.",
            "Following the King's Road — {0} is the next stop and the camp is already being planned.",
            "{0} awaits at the next halt. The road between here and there is in the hands of your vanguard.",
            "The march to {0} continues without incident. The old roads are earning their reputation.",
            // ── expansion ──
            "The route proceeds. {0} waits at the end of the next leg.",
            "{0} is the next waypoint and the road to it is clear.",
            "Following the King's Road toward {0}. The march continues as planned.",
            "The company moves toward {0} on the planned road. Progress is on schedule.",
            "{0} ahead. The next halt will be well earned.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ROADS OF THE KING — ARRIVED AT STOP
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] TravelArrived = {
            "A waypoint reached. The road behind you is behind you. Rest, resupply, and press on.",
            "Your company arrives on schedule. The discipline of the march has served you well.",
            "Another stop on the long road. Take what you need and remember where you are going.",
            "The route advances. Each stop brings the final destination closer, which is all a road can promise.",
            "You have arrived where the plan said to arrive. Not every step of a campaign can say that.",
            "The company makes its stop. Feed the soldiers, check the horses, and brief the march-masters for the next leg.",
            "A planned arrival is worth more than an unplanned one. Here you are, exactly where you intended to be.",
            "Rest the feet, mend the gear, and remember: the road home runs through everything ahead of you.",
            "Another waypoint in the long march. The Ranger's Satchel could do with checking before the next leg.",
            "The stop is reached. See to resupply while the market is still open.",
            // --- new lines ---
            "Arrived. The march-masters are already counting what needs replenishing and what can be sourced here.",
            "The waypoint stands as the map said it would. Take the rest — the next leg is longer.",
            "A halt well earned by an army that kept its discipline on the road.",
            "Here. Exactly here, exactly when planned. The route continues to perform.",
            "The stop is reached in good order. What comes next is already marked on the chart.",
            // ── expansion ──
            "Arrived at waypoint. The plan remains intact.",
            "The company makes its halt on schedule. Rest, resupply, and continue.",
            "A waypoint reached is a promise kept to the plan that laid it.",
            "Here, exactly where the route said to be. That is a kind of achievement that disappears in retrospect.",
            "Rest the horses first. Then the soldiers. Then the officers.",
            "The stop is achieved in good order. The next leg begins when the company is ready.",
            "A halt on the King's Road. Take what is needed and leave room for the army behind you.",
            "Arrived. The march-master is already assessing what the next leg requires.",
            "The halt is reached. Supply parties out, rest parties posted, sentry schedule established.",
            "Another waypoint achieved. The route continues to justify the faith placed in it.",
            // ── expansion ──
            "The route delivered the company where it said it would. The plan is working.",
            "Arrived. The halt is being managed well. Press on when the company is ready.",


        };

        // ═══════════════════════════════════════════════════════════════════════
        // MORALE — HIGH
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] MoraleHigh = {
            "The company is in fine spirits. High morale is a force multiplier — guard it carefully.",
            "Your soldiers march with their heads up. Whatever is ahead, it faces a confident company.",
            "Song carries through the camp tonight. Not the bravado of untested recruits — the quiet satisfaction of soldiers who have done hard things and survived them.",
            "High morale. The troops trust the mission and trust each other. That is worth more than numbers.",
            "Your company would follow you into the teeth of Barad-dûr tonight. Try not to ask that of them.",
            "There is something rare and valuable in a unit whose morale is genuinely high — protect it from the things that erode it.",
            "Even the warhorse is stepping lively. Morale reaches the animals too.",
            "The company sings old songs on the march. It is a good sign — only soldiers who believe in tomorrow sing about today.",
            "High spirits in the ranks. Victories, good food, and a commander who does not waste lives — three things that build morale.",
            "The work is hard and the road is long, but the company is with you. That matters beyond measure.",
            // --- new lines ---
            "A company with high morale is faster, hits harder, and breaks less easily. All three will matter soon.",
            "The soldiers are joking again. That specific tone — not bitter, not forced — means genuine confidence.",
            "When morale is high, the whole company moves differently. Even the supply wagons seem to move faster.",
            "This is the company at its best. Remember what brought it here — and repeat those things.",
            "Pride is a dangerous thing in excess. But earned pride, in a company that has worked for it, is a tactical asset.",
            "The volunteers are standing taller. The veterans are smiling without irony. Something good is happening in the ranks.",
            "Good morale is not just about happiness — it is about the shared belief that this is worthwhile and that it can be won.",
            "A confident company draws others to it. High morale is a kind of recruitment.",
            "The fire tonight is loud and the stories are good. Tomorrow the march will be faster for it.",
            "Morale this strong means soldiers who will do more than ordered when the moment requires it.",
            "The company has found what makes it work. Hold onto it.",
            "High spirits are both an outcome and a preparation. Whatever comes next, this company is ready for it.",
            "When soldiers trust the mission, the mission gains a quality it cannot otherwise have.",
            "This is what fighting for something looks like from the inside.",
            "The company believes in itself and in the cause. Both together are a formidable thing.",
            // ── expansion ──
            "When morale is this high, the company does more than it is ordered to. That is the thing to build on.",
            "The soldiers believe in the mission and in each other. Both are required. Both are present.",
            "High morale compresses time — the march feels shorter, the camp feels better, the challenge feels smaller.",
            "A company at peak morale is a different instrument from the same company at any other state.",
            "The veterans are proud. The new recruits are inspired. The combination is the best possible training ground.",
            "Morale this strong is partly the result of good leadership and partly the result of genuine belief in what is being done.",
            "Even the supply problems feel smaller when the company is in good spirits. They are not smaller. But the tolerance for them is higher.",
            "The company's confidence is earned and visible. That confidence is a tactical asset in itself.",
            "When soldiers are proud of their company, they defend its reputation as fiercely as they defend its position.",
            "High morale means soldiers who will find a way when there is no obvious one. That quality will be needed.",
            "A company in this state sets the standard that other companies want to reach. It should be kept this way.",
            "Earned pride and shared purpose are the components of morale that last. Both are present.",
            "The work is hard and the cause is long and the company is committed to both. That is rare.",
            "When morale is this high, the captain's hardest job is maintaining rather than building.",
            "Whatever produced this morale — victory, purpose, supply, leadership — do more of it.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // MORALE — LOW
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] MoraleLow = {
            "The company is tired and it shows. Low morale is a wound — it needs tending before it becomes fatal.",
            "Mumbling in the ranks. When soldiers stop complaining out loud, it means they have stopped expecting things to get better.",
            "Morale is eroding. Victories are needed, or rest, or both. The current situation provides neither.",
            "The march is heavy and the soldiers know it. Address what you can — the rest will have to wait.",
            "Food, pay, and something to believe in — the three things armies run on. Check which one is missing.",
            "Desertions are beginning in the margins of the company. They always start at the margins.",
            "Even the bravest soldiers have a limit. The question is whether you can see where that limit is before you reach it.",
            "Low morale in a battle means soldiers who do not take the initiative — and battles are often won or lost on initiative.",
            "The shadow within is sometimes more dangerous than the shadow without. Tend to this.",
            "Find out what is wrong and fix it, or find out what is wrong and explain why it cannot be fixed. Either is better than silence.",
            // --- new lines ---
            "The company is making calculations about survival versus mission that you should be making for them.",
            "Low morale has a smell and a sound and a particular quality of silence. You should be able to identify all three.",
            "Something has broken in the confidence of the company. Find it and fix it before it breaks further.",
            "Soldiers who do not believe in the outcome fight only for survival. That is a diminished thing.",
            "The march is slow and the faces are set and nobody is talking. That combination means something specific.",
            "The veterans know when a campaign has gone wrong. They are performing that knowledge now.",
            "When morale falls this low, the gap between ordered and performed begins to widen. Watch for it.",
            "Rest, food, a victory, a speech, a letter from home — find whichever of these is available and apply it.",
            "The cost of low morale compounds. Every day it is unaddressed, it gets harder to address.",
            "A company that has lost faith in itself fights at a significant disadvantage even against inferior forces.",
            "The soldiers are looking for a reason to believe this is still worth it. Give them one if you can.",
            "Whatever happened to bring the company to this state — address it, or the company will address it without you.",
            "Low morale is the enemy inside the camp. It must be fought as seriously as any enemy outside it.",
            "The distance between a low-morale company and a broken company is shorter than it looks.",
            "The company needs something good to happen. Engineer it if necessary.",
            // ── expansion ──
            "When soldiers stop grumbling out loud, it means they have stopped expecting improvement. Listen for the quiet.",
            "The gap between ordered and performed is widening. That is what low morale looks like in action.",
            "The company needs something to change. Either the situation or their understanding of why it is what it is.",
            "Low morale is a compound problem — it makes everything else harder, which makes morale lower.",
            "Find the root of it before treating the symptom. Morale has specific causes.",
            "The veterans' opinions spread faster than orders. Find out what the veterans are saying.",
            "Address it in person. Morale problems addressed by memo do not recover.",
            "Desertions at the margin are the early warning. They have begun.",
            "The company is going through the motions. Going through the motions is a different army than the one you had.",
            "Morale this low means soldiers who follow orders to the letter and no further. That is not enough when conditions get hard.",
            "Find out what the company thinks is true about the mission. Correct what is wrong.",
            "Low morale in a veteran unit means something specific went wrong. In a new unit it may be inexperience. Know which.",
            "The cause is real. Make sure the company knows you believe that.",
            "Morale is not about happiness. It is about the shared belief that the thing being done is worth doing and can be done.",
            "The recovery curve for morale is slower than the decay curve. Start recovery now.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO LEVEL UP
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] HeroLevelUp = {
            "Experience is a hard teacher but a thorough one. You have grown in ways that will matter.",
            "The campaign sharpens everyone it does not break. You remain unbroken, and sharper.",
            "Something in the way you handle the sword — or the council table — is different now. Better.",
            "Every battle learned something, even the ones that were won too easily to notice. You are accumulating that knowledge.",
            "The great captains of Middle-earth were not born great. They were made great, the same way you are making yourself.",
            "Another step on the long road of your capabilities. The road does not end here.",
            "Even Aragorn spent decades learning to be who he became. You are doing the same, faster.",
            "What you were when you first took up the sword and what you are now are different things. That gap is called experience.",
            "The company notices. They always notice when a commander gets better at being a commander.",
            "Growth is its own reward — and its own preparation. You are being prepared for something.",
            // --- new lines ---
            "The person who entered this campaign and the person you are now would have different conversations about what is possible.",
            "Each hard lesson absorbed is a harder opponent tomorrow. Keep absorbing them.",
            "Competence accumulates. What you could not do last season, you can do now. What you cannot do now, you will do next season.",
            "The growth is not just in the sword arm. It is in the eyes — the way you read a field, a camp, a face.",
            "The hard road makes something of whoever walks it. You are being made.",
            "In Rivendell they speak of the education of heroes as if it were curriculum. It is not. It is this.",
            "Something crystallised in the last battle — understanding that was not there before, that will not leave now.",
            "You are becoming capable of things you could not have attempted when the campaign began. Use that.",
            "The gap between who you are and who the cause needs you to be is closing. Keep walking toward it.",
            "Experience in command is the one thing that cannot be inherited, purchased, or given. It can only be earned.",
            "Whatever was uncertain in you is a little more certain now. Not everything — but something important.",
            "Growth is quiet. You may not have felt it happening. The results are visible to those who are watching.",
            "A better soldier, a better captain, a better version of who you need to be. Keep going.",
            "Even without a crown or a prophecy, the long road of earned experience builds something remarkable.",
            "You are not the same as you were. That is neither loss nor gain — it is the process. Trust it.",
            // ── expansion ──
            "The person who first took the road and the person who walks it now are related by commitment more than circumstance.",
            "Growth in command capacity is invisible to the person growing. It becomes visible in the quality of the decisions.",
            "A better captain, by a measurable degree. The company will notice before you do.",
            "The road of long practice produces what talent alone cannot.",
            "The depth of understanding that comes from doing the hard thing — repeatedly, in earnest — is visible now.",
            "What was uncertainty is becoming confidence. What was confidence is becoming competence.",
            "Each campaign season converts experience into a kind of judgment that cannot be taught.",
            "You are becoming capable of things the person who started this campaign could not have attempted.",
            "The difference between a captain and a commander is accumulated experience of exactly this kind.",
            "Growth happens in the field and the council room and the difficult conversation at midnight. You have had all three.",
            "The company looks to you differently than it did. Something in what you have become shows.",
            "A long campaign makes different things from whoever it does not break.",
            "The qualities that matter in a leader — judgment, composure, timing — are being built.",
            "Whatever test comes next will be met by someone better prepared than the person who met the last one.",
            "Even Thorin did not begin knowing what he became. The learning was the process.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ARMY — PLAYER MOBILISED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] ArmyMobilised = {
            "Your host assembles. An army marching with purpose is a different thing entirely from men walking the same road.",
            "The banners are called in. The army gathers — each company a piece of something larger than any of them.",
            "A great host forms beneath your standard. Feed it, supply it, and do not waste what it cost to assemble.",
            "The companies fall into line. The march of an army always sounds different from the march of a party.",
            "An army is a living thing — it eats, it drinks, it needs direction and rest. Care for it accordingly.",
            "The war host is assembled. What you do with it will be the story told after.",
            "Soldiers from across the realm answer the call. This is the force you have built. Use it well.",
            "The muster is complete. The combined strength of your allies marches under one banner: yours.",
            "An army moves. Slower than a small party, but with a weight that changes what is possible.",
            "Your host is considerable. Even Mordor's captains will need to take notice of it.",
            "The forces are combined. There is strength in numbers — and vulnerability in the supply lines that numbers demand.",
            "A great company answers your call. The road ahead will know the weight of them.",
            // --- new lines ---
            "The host assembles like a river gathering its tributaries. Each addition makes the whole greater.",
            "Banners from across the realm converge. The sight of them together is itself a statement.",
            "An army of this size changes the political as well as the military picture. Both need managing.",
            "The muster is a success. Now the much harder work of keeping the army fed, moving, and purposeful.",
            "Coordinated armies are rare. That this one answered the call at all speaks to what has been built.",
            "The weight of an army's footfall on the march is unlike anything else. The ground remembers it.",
            "Supply lines are already the primary concern. An army this size consumes more per day than a town.",
            "The host is assembled. Now it must be pointed in the right direction before it loses cohesion.",
            // ── expansion ──
            "The host gathers. Feed it, move it, point it at the right thing. All three are harder than they sound.",
            "An army assembled is an army that needs to be used before it begins consuming itself.",
            "Every clan that answered the call adds to the strength — and to the supply demand.",
            "The combined force is more than its parts, if the parts are coordinated. That is the condition.",
            "A great host on the march changes what is possible and what is dangerous simultaneously.",
            "The logistics of a combined force require full-time management. Appoint a march-master immediately.",
            "Banners from across the realm converge under your standard. The sight of it is its own intelligence operation.",
            "The army is assembled. The next question is: for how long can it stay that way before supply becomes critical.",
            "Combined forces have greater striking power and greater vulnerability to supply disruption. Plan for both.",
            "The host takes the road. The earth remembers the weight of it.",
            "A mobilised army is also a political statement. Every faction that sent soldiers is now committed.",
            "The muster is a success. Hold the momentum — combined forces can disperse faster than they assemble.",
            "An army on the march at this scale changes the strategic picture by existing, before it fights anything.",
            "The combined march is a communication to every party watching: this is what commitment looks like.",
            "The host is ready. The question of where and when to commit it is now the only remaining decision.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ARMY — ENEMY MOBILISED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyArmyMobilised = {
            "A dark host assembles on the horizon. Their drums carry further than their banners can be seen.",
            "Enemy forces are massing. Wherever they march, the land will feel it before the people do.",
            "A great army gathers under hostile banners. Riders should be sent ahead to warn what lies in its path.",
            "The scouts report a host in formation — more than a raiding party, less than a world-ending tide. Not reassuring either way.",
            "An enemy army takes the field. The question is not whether they are coming — it is when, and where.",
            "Dark banners gather. Many swords and one purpose: to take what is yours.",
            "Their forces consolidate. Scattered raids and skirmishes are replaced by something more organised. Worse, in its way.",
            "The enemy musters. Intelligence is now more valuable than bravery — find out where they are going.",
            "A hostile host marches. The Shadow rarely sends one army when two would be more instructive.",
            "A considerable force answers the enemy's call. Prepare accordingly — or prepare to yield ground.",
            "The drums of a great army carry on the wind. They are not friendly drums.",
            "Mordor's legions are never truly still. When they organise, pay close attention.",
            // --- new lines ---
            "The enemy's army is large enough to be seen from distance. That is a specific kind of bad news.",
            "An organised enemy host is qualitatively different from scattered opposition. Revise your plans accordingly.",
            "The scouts return early and fast. The enemy host is larger than the last estimate.",
            "A host in motion has a target. Finding that target before they reach it is the immediate priority.",
            "The enemy army flies banners of purpose. Whatever that purpose is, it is not nothing.",
            "Whatever scraps of intelligence you have about the enemy's movements need to be collated now.",
            "The consolidation of the enemy's forces reduces the advantage of interior lines. Plan for a harder fight.",
            "An enemy army assembles. Consider what it is assembling for before it tells you by arriving.",
            // ── expansion ──
            "An enemy force of this size has a specific target. Intelligence on that target is the immediate priority.",
            "The dark host assembles and the ground under it matters less than the direction it faces.",
            "A large enemy force in motion is also a logistics operation. Find where their supply comes from.",
            "The enemy has committed to something. A commitment of this size is not easily reversed.",
            "A mobilised enemy army is a threat with momentum. Interrupt that momentum or match it.",
            "Their host will arrive somewhere before yours can. Determine where before they do.",
            "Intelligence on enemy troop disposition is now the most valuable resource available.",
            "The enemy army is large and organised and has a plan. Finding the plan before it executes is necessary.",
            "A hostile host of this scale changes every defensive calculation in the region.",
            "The drums of the enemy's march carry further than their flags. Listen for both.",
            "An enemy mobilisation of this scale requires intelligence, response planning, and nerve. Begin all three.",
            "The host in motion will reach a target before you can react unless you move now.",
            "Counter-mobilisation may be necessary. Weigh the cost against the cost of allowing the enemy to move unopposed.",
            "The enemy commits a large force and the campaign becomes something other than what it was this morning.",
            "Track the enemy host and understand its objectives before committing to a response.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ARMY — PLAYER ARMY DISBANDED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] ArmyDisbanded = {
            "The army disperses. Each company returns to its own road — the combined force is gone.",
            "What was an army is now separate parties again. The work of assembling it must begin again if needed.",
            "The host disbands. It cost much to gather and costs nothing to lose. That asymmetry is worth remembering.",
            "Your soldiers scatter to their own concerns. The army is no more, until you need it again.",
            "The march ends here. The soldiers who rode together go separate ways.",
            "Cohesion dissolves when armies disband. Reassembly takes time — time the enemy may not give you.",
            "The combined host returns to its components. You are a captain again, not a general.",
            "What gathered is now scattered. The strength of numbers is no longer yours for the moment.",
            "The army is let go. Perhaps wisely — perhaps not. The road will tell.",
            "Dispersal. The great movement of the host breaks into its component streams.",
            // --- new lines ---
            "The combined force separates back into its constituent parts. Something is lost in the separation.",
            "An army disbands faster than it assembles. Remember that ratio when next considering the call.",
            "The host is gone. The relationships between the companies that formed it are not — hold onto those.",
            "Dispersal is sometimes the right strategic choice. Sometimes it is the one that allows the next disaster.",
            "The companies ride for their own halls. The flags that flew together are furled separately.",
            // ── expansion ──
            "The force disperses. The strategic capability it represented disperses with it.",
            "Reassembly will be possible. It will take time that the enemy may not give.",
            "An army disbanded is not an army lost — it is an army deferred. Note the difference when planning.",
            "The combined force returns to its constituent parts. Something is lost in the partition.",
            "The flags that marched together are furled separately. The connections that made them march together should be maintained.",
            "The host disbands in good order. The next muster will be faster for the practice.",
            "Cohesion built on the march does not fully survive disbandment. Some of it will need to be rebuilt.",
            "The army is gone for now. The strategic picture has changed accordingly.",
            "The dispersal was orderly. The relationships between the companies that made it work are intact.",
            "An army exists as long as it is fed and purposeful. Feed it and give it purpose, or it disperses itself.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO PRISONER — PLAYER CAPTURED / RELEASED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PlayerCaptured = {
            "You are taken. The chains are real, and the road ahead leads somewhere you did not plan to go.",
            "Captured. Whatever comes next, the campaign cannot continue from a cell. Find a way out.",
            "The battle is lost and you with it. Now begins a different kind of fight — to survive captivity.",
            "A prisoner now. The enemy will decide what you are worth — make sure the answer is your freedom.",
            "Even the greatest commanders have known this. Aragorn walked dark roads too. Endure, and find the way back.",
            // --- new lines ---
            "The enemy has you. The campaign has not stopped — only you have.",
            "Captivity is a problem with solutions. Find the most direct one.",
            "A cell is just a room with a locked door. Doors can be unlocked.",
            "The enemy's prisoner is still a person with choices. Choose carefully.",
            "What happens in captivity does not define you. What you do afterward does.",
            "The chains are a temporary state. Focus on the when and the how, not the if.",
            "Your company will not wait forever. Nor should they. Find the way out.",
            "The hardest part of captivity is patience. But it is also the most useful skill to practice here.",
            "Even in a cell, intelligence can be gathered. Listen. Watch. Remember.",
            "The enemy keeps prisoners for reasons. Understanding theirs is the first step to countering them.",
            // ── expansion ──
            "Captured. The campaign has a new constraint and it is you.",
            "The campaign does not stop when you are taken. Someone else will carry it until you return.",
            "A prisoner's first concern is information security. Speak carefully to anyone who listens too attentively.",
            "The cell is real and so is the way out of it. Find both in that order.",
            "Captivity has a timeline. Make yours shorter by being useful to the people holding you.",
            "The circumstances that led here need to be examined and fixed. After the escape.",
            "Being captured is a setback, not an ending. The ending depends on what happens next.",
            "A prisoner of war has rights under the old conventions. Know them. Invoke them.",
            "The company will learn of your capture and adjust. Trust that they are adjusting.",
            "Patience and observation. Both are tools that a prisoner can use without requiring anything from the captors.",

        };

        public static readonly string[] PlayerReleased = {
            "Free again. The road back to the campaign begins with the first step out of this place.",
            "The cell opens and the world opens with it. Take a moment — then get back to work.",
            "Released. Whatever was agreed to secure your freedom, honour it. Then make sure it was worth the cost.",
            "Back in the field. The time in captivity was not wasted if it was spent planning the return.",
            "Freedom is a better feeling when it has been taken away. Do not let it happen again easily.",
            // --- new lines ---
            "Out. The air is different on this side of the door.",
            "Freedom returned. Begin the process of returning to usefulness immediately.",
            "The campaign waited. Now it needs you back.",
            "Whatever the captivity cost — in time, in intelligence, in the appearance of vulnerability — recover it.",
            "Free. The rest of the work remains. Start with what matters most.",
            // ── expansion ──
            "Free. Begin the work of returning to effectiveness before the relief of it passes.",
            "The cell opens. The road back is the same road in a different direction.",
            "Released — and now the campaign needs you back in it.",
            "The captivity ended. What was learned in it may be useful.",
            "Back in the field. The company needs to see you before they believe it.",
            "The time in captivity is accounted for. The time from this moment is not.",
            "Freedom after captivity has a quality that freedom-by-default never achieves.",
            "Out. Begin the accounting of what changed while you were held.",
            "Released into a campaign that has continued without you. It needs you in it.",
            "The door is open. Walk through it and do not look back at it.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO PRISONER — ENEMY CAPTURED / RELEASED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyHeroCaptured = {
            "A prisoner of war is a lever. Use them wisely — ransom, exchange, or information.",
            "The enemy loses a commander. A prisoner in your hands has more strategic value than a dead one.",
            "Taken alive. Their worth to you depends on who wants them back, and how badly.",
            "An enemy captain in chains. What they know may be as valuable as any ransom.",
            "The living prisoner is an opportunity the dead enemy never is. Handle this one carefully.",
            // --- new lines ---
            "A prisoner taken. More valuable than their weight — which in armour is considerable.",
            "Alive and secured. The question of what to do with them is now yours to answer.",
            "The enemy has one fewer commander in the field and one more reason to negotiate.",
            "A captured enemy is a form of intelligence, a form of leverage, and a form of mercy. Use all three.",
            "The chains of war have purposes beyond cruelty. Find the best one and apply it.",
            // ── expansion ──
            "The enemy's commander is your prisoner. Treat them with the protocols that honourable war requires.",
            "A captured enemy captain is worth more alive than dead — in information, in exchange, in the moral argument.",
            "The chains of war have purposes that extend beyond the immediate battle.",
            "Find out what they know before deciding what they are worth.",
            "A living prisoner has agency. That is either a problem or an opportunity depending on how the situation is managed.",
            "The enemy will want them back. The terms of that return are now yours to set.",
            "Captivity turns a field commander into a diplomatic asset. Use that asset with patience.",
            "A captured enemy who was honourably treated is sometimes more valuable as a living argument for how you conduct war.",
            "The prisoner knows things. The things they know have a shelf life.",
            "Handle this with the care that a valuable and volatile thing requires.",

        };

        public static readonly string[] EnemyCapturedNamed = {
            "{0} is your prisoner. The enemy will notice the absence.",
            "You have taken {0}. Their side will want them back — at a price.",
            "{0} falls into your hands alive. That is worth more than their death would have been.",
            "The capture of {0} will be felt in their command structure. Use the advantage.",
            "{0} is in chains. What comes next is a negotiation.",
            // --- new lines ---
            "{0} is secured. Find out what they know before deciding what they are worth.",
            "The enemy lost {0} to your forces today. They will not forget that loss.",
            "{0} in captivity is a different problem for the enemy than {0} dead.",
            "You hold {0}. That card is worth playing carefully.",
            "A prisoner of consequence: {0}. Handle accordingly.",
        };

        public static readonly string[] AllyHeroCaptured = {
            "A companion is taken. Their safety is now the enemy's to grant or deny.",
            "One of your own is in enemy hands. The price of their return will not be cheap.",
            "A trusted ally is captured. Find out where they are held and what it will cost to bring them back.",
            "The enemy has gained a prisoner worth more to your cause than their own. Act accordingly.",
            "An ally in captivity. The campaign grows harder without them — and the ransom will make it harder still.",
            // --- new lines ---
            "One of yours is in chains somewhere you cannot see. That situation demands action.",
            "The enemy holds someone who matters. Find them.",
            "An ally captured is a decision tree with a short window. Begin working through it.",
            "The absence of a trusted companion is felt in ways that numbers cannot capture.",
            "They are alive — for now. That window needs to be used.",
            // ── expansion ──
            "One of your company is in hostile hands. The process of getting them back begins now.",
            "A companion captured changes the company's psychology in ways that will become visible under pressure.",
            "Find where they are held. Find what it costs to recover them. Begin both processes simultaneously.",
            "The enemy holds a card that they will play at the moment most useful to them.",
            "An ally in captivity is both a loss and a motivation. Use the latter carefully.",
            "The absence of a trusted companion affects decisions in ways that are hard to account for in plans.",
            "They are alive — for now. That condition is temporary in either direction. Act on it.",
            "A companion in chains is a companion who needs you to be making good decisions in their absence.",
            "The recovery process has a cost. Pay it as soon as the cost is known.",
            "Until they return, the company carries the weight of knowing they are out there.",

        };

        public static readonly string[] AllyCapturedNamed = {
            "{0} has been taken prisoner. Find out what it will take to get them back.",
            "The enemy holds {0} now. That changes things.",
            "{0} is a prisoner of war. Their fate depends on how quickly you act.",
            "They took {0}. The loss is felt in the company and on the field.",
            "{0} is in captivity. Do not leave them there long.",
            // --- new lines ---
            "{0} is in enemy hands. Begin the recovery effort now.",
            "The absence of {0} will be felt before it is explained.",
            "Find {0}. What comes after depends on what you find.",
            "{0} captured. A wound in the company that needs tending.",
            "The enemy has {0}. Make it cost them to keep them.",
        };

        public static readonly string[] AllyHeroReleased = {
            "A companion returns. The captivity ends and the campaign resumes — one piece of good news in a long war.",
            "Back in the field and back in the fight. The time lost to captivity will not be recovered, but the person is.",
            "The return of a trusted ally is worth marking. Not all who are taken come back.",
            // --- new lines ---
            "The company is made whole again in the most important way.",
            "They return changed, perhaps, but returned. Welcome them and give them space.",
            "Good news is rare enough to stop for. This is good news.",
            "Whatever secured the release, the result is what matters: they are back.",
        };

        public static readonly string[] AllyReleasedNamed = {
            "{0} is free. Whatever secured the release, it was worth it.",
            "The gates open and {0} walks out. The company is fuller for it.",
            "{0} returns from captivity. The absence made the gap visible — their return fills it.",
            "{0} is released. Mark the occasion — not all stories of captivity end this way.",
            // --- new lines ---
            "{0} walks free. Note it, and note that it needed to be made to happen.",
            "The company gets {0} back. What that cost will be evaluated later.",
            "{0} returns. The story of this captivity is over — the story of this campaign is not.",
            "Released: {0}. The recovery can now begin in earnest.",
        };

        public static readonly string[] EnemyHeroReleased = {
            "The enemy's prisoner walks free. Whether wisely or not, what is done is done.",
            "A released enemy returns to their cause with knowledge of your methods. That is the cost of mercy.",
            "They go back to the people who sent them. Whatever freed them had better be worth it.",
            // --- new lines ---
            "The enemy regains a capable person. The decision to release them has a cost — ensure the benefit is worth it.",
            "Mercy shown. Track the outcome. Not all mercy is wasted, but this kind should be verified.",
        };

        public static readonly string[] EnemyReleasedNamed = {
            "{0} is released. Ensure you are prepared for {0} to return to the field.",
            "The enemy regains {0}. Whether by ransom, exchange, or choice — the consequence is the same.",
            "{0} walks free. The enemy is stronger for it by exactly one.",
            // --- new lines ---
            "{0} returns to their cause. Whatever was gained by the release, hold it against what was given back.",
            "The enemy has {0} again. They will not waste the return.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // CLAN CHANGES KINGDOM
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PlayerClanChangedKingdom = {
            "Your clan departs from {0} and pledges to {1}. Allegiances are not permanent — but they are not cheap either.",
            "A new banner. Your clan leaves {0} behind and rides under the colours of {1}.",
            "The change is made. {0} is the past and {1} is the future. Build something worth the change.",
            "From {0} to {1} — the transition is complete. Old allies may take it personally. Be ready for that.",
        };

        public static readonly string[] ClanDefection = {
            "A defection. The kingdom is weaker by one clan — and wherever they went, that side is stronger.",
            "The departure of an ally always leaves a gap. Political and strategic both.",
            "When clans leave, they take soldiers, influence, and credibility. All three will need replacing.",
            "A defection is a message. It is worth reading carefully to understand what it says about your kingdom.",
            "They were never fully committed. Perhaps it is better to know now than when it mattered more.",
        };

        public static readonly string[] ClanDefectedFrom = {
            "{0} has left and pledged to {1}. The map of this war just shifted.",
            "The clan of {0} defects to {1}. Another banner turns its back on yours.",
            "{0} abandons the alliance and rides for {1}. Count the soldiers that ride with them.",
            "The banner of {0} is taken down and carried to {1}. A blow that did not require a battle.",
        };

        public static readonly string[] ClanJoined = {
            "A new clan joins the banner. Every addition makes the alliance harder to break.",
            "More swords under your colours. Welcome the addition — and make clear what is expected of them.",
            "An ally gained. The work of building a coalition is slow and the rewards are real. This is one of them.",
            "Another clan committed to the cause. Keep faith with them and they will keep it with you.",
        };

        public static readonly string[] ClanJoinedYour = {
            "{0} leaves {1} behind and pledges to your banner. A worthwhile gain.",
            "The clan of {0} joins your kingdom from {1}. They bring swords and a reason for leaving.",
            "{0} rides under your colours now. Whatever drove them from {1}, treat it as intelligence.",
            "A defection runs both ways. {0} comes from {1} — find out why and what it costs the enemy.",
        };

        public static readonly string[] ClanChangedAllegiance = {
            "{0} leaves {1} and joins {2}. The balance of the war adjusts accordingly.",
            "The clan of {0} changes allegiance — from {1} to {2}. The map shifts.",
            "{0} pledges to {2} after leaving {1}. Wars are also fought in throne rooms and with ink.",
            "A realignment: {0} moves from {1} to {2}. Neither side will pretend it doesn't matter.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // KINGDOM DESTROYED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PlayerKingdomDestroyed = {
            "{0} is no more. The banners are struck and the kingdom ends here.",
            "The fall of {0}. Everything built, everything defended, gone — but you remain. That is something to build from.",
            "{0} has fallen. The enemy has achieved what they set out to do. What happens next is up to you.",
        };

        public static readonly string[] KingdomDestroyedPlayerFlavor = {
            "Not every kingdom that falls stays fallen. The history of Middle-earth is full of things that endured after they should have ended.",
            "The kingdom is gone. The cause, if it was worth having, is not gone with it.",
            "When everything falls, what remains is the question of what you do with the rubble.",
        };

        public static readonly string[] EnemyKingdomDestroyed = {
            "{0} has fallen. The enemy loses a structure that will not easily be rebuilt.",
            "The destruction of {0}. What took generations to build is unmade — that is what war costs on the losing side.",
            "{0} is finished as a power. Its clans scatter and its banners come down.",
            "An enemy kingdom destroyed. The war reshapes itself around the gap where {0} used to stand.",
        };

        public static readonly string[] KingdomDestroyedEnemyFlavor = {
            "The destruction of a kingdom does not end a war. The scattered pieces become raiders, outlaws, desperate men.",
            "Victory of this kind is measurable. It is also a reminder of the cost of losing.",
            "When your enemies lose not just battles but the whole structure that sent them — that is a different kind of gain.",
            "What falls today may rise tomorrow under a different name. Be alert for it.",
        };

        public static readonly string[] NeutralKingdomDestroyed = {
            "{0} is destroyed. Another name removed from the map of Middle-earth.",
            "The kingdom of {0} is no more. Its people will remember, even when the kingdom itself is forgotten.",
            "{0} ends. The war consumes what it touches, and now it has touched {0}.",
        };

        public static readonly string[] KingdomDestroyedNeutralFlavor = {
            "Every kingdom that falls makes the survivors more desperate — on all sides.",
            "The destruction of states is the background music of this age. It does not make it easier to hear.",
            "One fewer banner in the world. The balance of power shifts where it falls.",
            "History does not pause for fallen kingdoms. Neither does the campaign.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════════════
        //  NEW POOLS — for expanded event hooks
        // ══════════════════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════════════════

        // ── TOURNAMENT STARTED ─────────────────────────────────────────────────
        public static readonly string[] TournamentStarted = {
            "A tournament is announced. Even in war, the tradition of the contest holds — and with it, the hope that skill still matters.",
            "The lists are set and the challengers named. A tournament: the one kind of organised violence that ends with a feast.",
            "Heralds ride out to spread word of the tournament. The peace of the field, at least, will be observed here.",
            "Banners of competing houses line the tournament ground. The shadow is far enough away today for this.",
            "A tournament called. The crowds will come, the champions will be tested, and someone will go home with the prize.",
            "Even in the worst years, the people need tournaments — proof that not everything is decided by armies.",
            "The tournament grounds are marked, the rules set, and the challengers registered. May the best sword win.",
            "Blunted lances and agreed rules: the civilised face of a very old art. The tournament begins.",
            "Word of the tournament spreads through the camp. The competitive edge in every soldier finds an approved outlet.",
            "A tournament — where the same skills that decide battles are channelled into something that does not burn villages.",
            "The stands fill and the air smells of sawdust and anticipation. War has a pause today.",
            "Even Gondor's finest competed in the old tournaments. The tradition connects you to something older than this campaign.",
            "The tournament is a kind of test — not just of weapons, but of character and composure under pressure.",
            "Enter, observe, or move on — the tournament is what it is, a brief interval of structured glory.",
            "The rules of the tournament are older than the current kings. They survive because people need them to.",
        };

        // ── TOURNAMENT WON ─────────────────────────────────────────────────────
        public static readonly string[] TournamentWon = {
            "The tournament is yours. The crowd remembers the name of the winner — make sure it is one worth remembering.",
            "Victory in the lists. The prize is real, but the reputation won here travels further.",
            "The final challenger is defeated. The banner is raised and the tournament ends with your name on it.",
            "Champion. The word carries weight in places this tournament's report will travel to.",
            "The tournament is won. The soldiers who watched will march a little harder for having seen it.",
            "A clean victory in the contest. What you proved here, you can prove on the field of real battle.",
            "The tournament ends with your standard raised. A good day — and the feasting is not nothing.",
            "The crowd is on their feet. Whatever brought them here, they leave having seen something worth the journey.",
            "Champion of the tournament. In better days this would be unremarkable. In these days, it carries additional weight.",
            "The prize is yours. More valuable: the message it sends about the quality of this company's fighters.",
            "The tournament champion. It is a title, and titles matter more than they should, and less than the sword that earned them.",
            "Won fairly, before witnesses, with skill. That is the only way to win a tournament worth winning.",
            "The final opponent fell with good grace. Honour them for it.",
            "Victory in the lists. The feast tonight will be louder for it.",
            "The tournament yields its prize to your hand. Wear it with the right amount of pride — which is: visible but not excessive.",
        };

        // ── TOURNAMENT LOST ────────────────────────────────────────────────────
        public static readonly string[] TournamentLost = {
            "The tournament goes to another. There is no shame in losing to a better swordsman on a better day.",
            "Eliminated from the contest. Not every tournament ends the way you intend — but every one teaches something.",
            "The prize goes elsewhere. Learn the winner's technique before the next tournament.",
            "A defeat in the lists. The blade was sharp and the opponent sharper. That is the honest accounting.",
            "You did not win today's tournament. Tomorrow's battle is decided by different things — some of which you can still control.",
            "The crown of the tournament goes to a worthier hand today. Watch how they carry it.",
            "Eliminated. The crowd moves on quickly — but you should move on more slowly, and more thoughtfully.",
            "Lost in the tournament. Not a strategic defeat. But no one should pretend it does not sting.",
            "The tournament runs on without you in the final rounds. Observe carefully — the winner may be a future opponent.",
            "A tournament defeat. The best use of it is as information: here is what still needs work.",
            "The best competitor today was not you. That is a fact to hold and use, not to despair over.",
            "Eliminated from the contest before the final. The next tournament is an opportunity to change that.",
            "The defeat is clean and the opponent is worthy. Acknowledge both and find what can be learned.",
            "No tournament prize today. The feast is still available to those who know how to lose graciously.",
            "Lost. There is data in a tournament defeat — technical, tactical, personal. Mine it.",
        };

        // ── COMPANION RECRUITED ────────────────────────────────────────────────
        public static readonly string[] CompanionRecruited = {
            "A new face joins the company. Every person who chooses this road chooses it for a reason — find out what it is.",
            "Someone new at the camp fire tonight. The company grows by one, and that one carries a story worth hearing.",
            "A companion recruited. They come with skills, with history, and with whatever drove them to seek you out.",
            "The company is larger by one worthy addition. That is not a small thing on a long campaign.",
            "A new voice in the council, a new sword at the battle. Companions are rare — tend to this one.",
            "They found you, or you found them. Either way, the company is different now, and mostly for the better.",
            "A recruit who chose to come rather than being pressed. That distinction matters — it means they brought their whole will with them.",
            "The new companion has the look of someone with things to prove. That energy, properly directed, is useful.",
            "Every companion you gain adds not just a sword but a set of eyes that see things differently. Listen to them.",
            "Welcome to the company. May the road treat you with the respect you bring to it.",
            "A new companion joins the march. They are unknown quantities until the first real test. Keep that in mind — in a good way.",
            "The door of the company opens. Another person walks through it with something to contribute.",
            "They came from somewhere and were going somewhere else before they found you. That journey has meaning.",
            "The company's character is partly the sum of its members. This addition changes the sum.",
            "A companion joins. Skills noted, history filed, value to be determined in the field.",
        };

        // ── COMPANION LEFT / DEPARTED ──────────────────────────────────────────
        public static readonly string[] CompanionLeft = {
            "A companion has left the company. Their road diverges here. Part on good terms if you can.",
            "The company is smaller by one departure. Whatever drove them away, understand it before the next person considers the same road.",
            "A companion gone. The camp fire feels it before the strategy does.",
            "Some companions stay for the duration. Some have their own destinations. This one found theirs.",
            "The departure was coming. Watch the signs of it — they are always visible in retrospect.",
            "A companion moves on. Their time with the company added something. Let the addition remain, even after the person has left.",
            "Departure. The road splits here and takes one of your company with it.",
            "Not every companion who joins the march finishes it. This one's chapter in the story closes here.",
            "They leave for their own reasons, which are their own. Let it be what it is.",
            "The company adjusts. Loss of a companion is not the loss of an army — but it is a kind of loss.",
            "A parting of ways. The road has many branches; this one took another.",
            "The companion's departure changes things in ways that will not all be visible immediately.",
            "Someone who shared the fire and the road is gone. That is not nothing.",
            "The door the companion came through — they took it back. The company adapts.",
            "Farewell to someone who was part of the story for a time. Their absence will be noticed.",
        };

        // ── CARAVAN SENT OUT ───────────────────────────────────────────────────
        public static readonly string[] CaravanDeparted = {
            "The caravan departs on the trade road. A calculated risk — but commerce does not stop for war if it can help it.",
            "Wagons loaded and guards posted, the caravan moves out. Prosperity follows those who can keep the roads open.",
            "A trade caravan, sent into a world where trade roads are also war roads. Watch the reports.",
            "The merchant's gamble: send the caravan and profit, or keep it home and stagnate. The decision is made.",
            "A caravan on the road is a small act of confidence in the stability of things. It is worth something beyond the profit.",
            "The caravan rolls out onto roads that were not entirely safe last week. Calculated risk is still risk.",
            "Merchants are the first to test the peace of a road. Your caravan joins the test.",
            "The wagons creak out into the morning. Their safe return will tell you something about the state of the road.",
            "A caravan moves. The economy of this war depends on caravans like it moving without incident.",
            "Commerce: the first casualty of instability and the first sign of recovery. This caravan is both.",
            "The guards on the caravan are as important as the goods it carries. Both require investment.",
            "Every caravan that travels safely is an argument that the region is under control. Make that argument.",
        };

        // ── CARAVAN RAIDED ─────────────────────────────────────────────────────
        public static readonly string[] CaravanRaided = {
            "The caravan is attacked. Raiders know that supply lines are worth more than strongholds in some situations.",
            "Riders hit the caravan on the road. Whatever it was carrying is now in hostile hands — or burning.",
            "The caravan is under attack. The roads are not safe enough. That is the message, and it costs more than goods.",
            "A raided caravan is a broken supply line. That is rarely a coincidence — someone chose this target.",
            "The merchants' worst fear: raiders on the road with no patrol close enough to help.",
            "The caravan's guards fought. Whether well enough depends on what is recovered afterward.",
            "Bandits or enemy agents — either way, the caravan has been hit and the supplies are compromised.",
            "The road swallowed the caravan and what came back was the news of its loss.",
            "A targeted raid on a supply caravan. Someone is paying attention to your logistics.",
            "The caravan is gone. Reconstruct the supply chain and find out who sent the raiders.",
            "A hit on the supply line disguised as brigandry. Or actual brigandry. The result is the same either way.",
            "The caravan's loss ripples outward. Where supplies were expected, absence arrives instead.",
        };

        // ── CARAVAN ARRIVED SAFELY ─────────────────────────────────────────────
        public static readonly string[] CaravanArrived = {
            "The caravan arrives intact. The road held and the guards were sufficient. A quiet victory.",
            "Safe arrival. The goods are delivered and the road has been proven navigable.",
            "The caravan returns without incident. Good news, and useful intelligence about the state of the road.",
            "Intact, on time, and uncontested. A caravan's safe arrival is not nothing in these times.",
            "The merchants breathe out. The caravan arrives and the gamble paid off.",
            "Supplies delivered, guards returned, route confirmed. A successful caravan is a working supply line.",
            "The road proved friendlier than feared today. Build on that before it changes.",
            "Arrival. The contents are logged and the guards are stood down. A small, necessary success.",
            "Safe delivery. The commerce of this war continues to function.",
            "The caravan made it. One more data point for the safety of the road.",
            "No raids. No bandits. No delays. The caravan arrives to the satisfaction of everyone who sent it.",
            "A clean run. The trade road is open for now — use it while it is.",
        };

        // ── HERO MARRIED ───────────────────────────────────────────────────────
        public static readonly string[] HeroMarried = {
            "A marriage in the company. Even in war, the old ceremonies hold their meaning.",
            "Vows exchanged in a world that offers no certainties. That is what makes them worth making.",
            "A wedding. The company stops, the swords are put aside for an afternoon, and something permanent is made.",
            "Two people choose each other in a season that offers few guarantees. That is a kind of courage.",
            "The old words are spoken and the compact is made. Something worth protecting has just grown by one relationship.",
            "A marriage — the most personal kind of alliance, and one that no treaty can replicate.",
            "The ceremony is brief and the world does not pause for it. That does not diminish what was said.",
            "Two people bound by choice, in a time when many things are bound by necessity. Mark the difference.",
            "The marriage is made. Whatever comes next, it will be faced together.",
            "An alliance of a different kind — personal, permanent, and chosen. Honour it accordingly.",
            "The ring is given and the words are said. Even the camp is quiet for it, for a moment.",
            "A wedding on campaign. It is not what anyone planned, but it is what the moment offered.",
            // ── expansion ──
            "The old words spoken in a world that does not always honour old things. They still mean what they have always meant.",
            "Two people choosing each other is the most persistent argument for something worth protecting.",
            "A wedding on campaign is a political event as well as a personal one. Both aspects deserve acknowledgment.",
            "The ceremony was brief and real and the company was quiet for it.",
            "In times of war, people make the enduring choices faster. The urgency shows in the faces of everyone watching.",
            "A marriage made in the shadow of conflict is tested differently than one made in peace. That is not a disadvantage.",
            "The old compact, renewed in a new context. It was made for exactly this kind of context.",
            "Even soldiers need reminding that the future they are fighting for includes things like this.",
            "A permanent choice made in temporary conditions. The permanence is what matters.",
            "Two people who decided that the uncertainty of the present did not change the certainty of the choice.",

        };

        // ── CHILD BORN ─────────────────────────────────────────────────────────
        public static readonly string[] ChildBorn = {
            "A child is born. The world gains a future and a reason to think beyond the current campaign.",
            "Birth. The most uncomplicated argument that the future is worth fighting for.",
            "A new life enters the world. The politics of its birth do not diminish what it is: a beginning.",
            "Born in the middle of a war, which is not unusual. The world does not wait for peace to begin populating.",
            "The child arrives, indifferent to the timing. New lives never choose their moments, and are no lesser for it.",
            "A birth — proof that even in dark times, the ordinary miracles continue on their own schedule.",
            "Someone new. Whatever this age asks of them, it will ask later. For now, they simply are.",
            "A child born in war will know only war, until war ends. Give them a reason to see it end.",
            "The heir is born. Whatever that means for the succession, for the moment it means something simpler and larger: a person.",
            "Life finds its way into the world regardless of what armies are doing elsewhere. The child does not know or care.",
            "A new person joins the world. Everything that comes after this moment is, to them, the whole world.",
            "Born into a story already well underway. May the later chapters give them better material to live in.",
            // ── expansion ──
            "A new person. Whatever the world asks of them, it is asking from a beginning.",
            "Born into a campaign. The world does not wait for peace to populate itself.",
            "The birth of a child in wartime is a statement about the future — that one exists and that it will be inhabited.",
            "A life beginning where others are ending. That is the arithmetic of the world, performed without sentiment.",
            "The child does not know what the age demands. They will learn. For now, they are only a beginning.",
            "A person added to the cause without having chosen it. What they choose later is their own.",
            "Births during campaigns go unremarked in histories. The children do not know that. The parents do not care.",
            "The new life knows nothing of shadow or army or campaign. Give it the chance to stay that way as long as possible.",
            "An heir born in the field. The succession question has an answer. The campaign continues.",
            "The oldest proof that the world intends to continue: another person arriving to be part of it.",

        };

        // ── BANDIT HIDEOUT CLEARED ─────────────────────────────────────────────
        public static readonly string[] BanditHideoutCleared = {
            "The hideout is cleared. The roads nearby will be safer for it — at least until another group fills the vacancy.",
            "The bandits' nest is rooted out. What they took is partly recovered; what they did cannot be undone.",
            "The hideout falls. However ordinary the fight, the villages it had been preying on will notice the absence.",
            "A bandit camp cleared — one less hazard on the road, one more confirmation that the region is not entirely lawless.",
            "The nest is empty and those who filled it are accounted for. A modest victory, but the people nearby will not call it modest.",
            "Bandits scattered or captured. The work of cleaning up the edges of a war is never glamorous — but it is necessary.",
            "The roads are a little safer today. That matters more than the size of the engagement.",
            "The hideout was not Mordor's work — just opportunism in the chaos of war. Cleared either way.",
            "Every hideout cleared is a village that sleeps better tonight. That accounting is worth making.",
            "The bandit leadership is dealt with. The rest will disperse or be absorbed into the chaos of the region.",
            "Cleared. The cave, the camp, the network of threats extending from it — all dealt with.",
            "The last of them run when the first of them fall. The hideout is ours to dismantle.",
            "A small action with disproportionate local significance. The people here will remember who cleared that particular road.",
            "The roads of this region just became more navigable. Not safe — but more navigable.",
            "The bandits thought themselves secure in their nest. They were mistaken.",
            // ── expansion ──
            "The nest is gone. The roads adjacent to it are passable again without the premium of armed escort.",
            "Bandits cleared — which means roads re-opened, which means supply improved, which means the villages can function.",
            "Even small victories have compounding returns if they are in the right location.",
            "The hideout is done. Someone will eventually fill the vacuum. Post a patrol presence before that happens.",
            "A local threat addressed with local effect. The villagers who used this road will notice the difference.",
            "The clearance was clean. The people who needed it done will hear about it before nightfall.",
            "One fewer hazard on the roads. The cumulative effect of many such clearances changes a region.",
            "Cleared without unnecessary force. The reputation of how you handle these operations travels as far as the news of it.",
            "The bandit network that supported this hideout will look for another base. Watch for it.",
            "Whatever the bandits knew about the region is now known to you. Use that intelligence.",
            "The road is cleaner. That is not nothing — it is literally what keeps caravans moving.",
            "The hideout cleared and the access route to the region's supplies re-opened. A good return on the effort.",
            "The local militia can now patrol this road without reinforcement. That change in capability matters.",
            "Small force, specific objective, achieved outcome. The definition of a well-managed local operation.",
            "The people in those villages are not safe — nothing in this region is safe. But they are safer.",

        };

        // ── WEATHER — RAIN ─────────────────────────────────────────────────────
        public static readonly string[] WeatherRain = {
            "Rain on the march. The roads turn to mud and the column slows. The campaign does not pause for weather.",
            "A persistent rain falls across the land. The archers curse their strings; the infantry curse their boots.",
            "Grey skies and steady rain — the campaign season's familiar companion. Dress for it and march through it.",
            "The rain that began in the night has not stopped. The rivulets on the road are becoming streams.",
            "Rain comes down from the clouds above the Misty Mountains and turns the valleys into bogs.",
            "Wet armour, wet supplies, wet soldiers — the rain costs no lives but extracts its toll another way.",
            "The mud of a wet march has defeated more campaigns than any enemy. Plan for it.",
            "Rain on the White Mountains means runoff in the lowlands. The river crossings will be harder this week.",
            "A grey curtain of rain obscures the distances. Intelligence becomes unreliable in weather like this.",
            "The sentries stand watch in the rain without complaint — at least without loud complaint. They are good soldiers.",
            "Every soldier on this march is wet. The difference between those who handle it and those who don't is character.",
            "Rain means mud means slow going. Add half a day to every estimate and plan accordingly.",
            // ── expansion ──
            "The rain fell all night and shows no sign of stopping. The march pace adjusts.",
            "Wet bowstrings are a specific problem that archery-heavy formations cannot ignore.",
            "The rain makes the scouts' job harder — tracks wash out and visibility halves.",
            "A cold rain in the mountains is a different experience from a warm rain in the lowlands. Both slow the march.",
            "The camp is waterlogged and the morale is following. Fire-keeping becomes a priority.",
            "Rain in the passes of the Misty Mountains is rain with attitude. Plan accordingly.",
            "The rivers are rising. Every ford needs to be reassessed.",
            "Rain levels the field in some ways — dust disappears, tracks appear, visibility decreases for both sides.",
            "The sick-call grows in wet weather. That is a constant of campaign life and should be planned for.",
            "Supply wagons in the mud are a mathematics problem. Half the load, twice the horses, double the time.",
            "The rain fell steadily throughout the night watch and the day watch and shows no preference for stopping.",
            "Wet armour rusts faster than any action damages it. Maintenance after rain weather is essential.",

        };

        // ── WEATHER — FOG ──────────────────────────────────────────────────────
        public static readonly string[] WeatherFog = {
            "A heavy fog lies across the land. The world shrinks to thirty paces in every direction.",
            "The fog that settled in the night valley has not lifted. Move with care — every enemy army loves fog.",
            "Thick fog on the march. The scouts are pulled in; the column tightens. Noise discipline becomes critical.",
            "Mist from the river has rolled across the flatlands. In weather like this, flanks are invisible until they are upon you.",
            "The fog muffles sound as much as sight. The watch listens as hard as it looks.",
            "Grey fog over grey land. On a day like this, the world looks the way the war feels.",
            "Fog favours the ambusher and the local. Your enemy knows these fields; your soldiers may not.",
            "The fog came down from the mountains last night and has settled into every valley. Visibility: poor.",
            "On a foggy morning, armies have been broken by forces a third their size. Proceed carefully.",
            "The mist of Middle-earth has a quality that does not feel entirely natural. Perhaps it is. Stay alert.",
            "Fog does not stop an enemy that knows the ground. It only stops the one trying to read it.",
            "A white blanket over everything that should be visible. Update your intelligence once the fog lifts.",
            // ── expansion ──
            "Fog that rolls in from the marshes has a texture that fog from the mountains does not. Both obscure vision equally.",
            "The fog is so thick the sentry cannot see the next post from their own position. Protocols are tested.",
            "In thick fog, armies have marched into each other by accident and by design.",
            "Fog lifts unpredictably. Do not make irreversible decisions while it is still present.",
            "The enemy could be thirty paces away. The fog does not distinguish between reassurance and threat.",
            "Signal horns become the primary communication method in fog. Ensure everyone knows the codes.",
            "The experienced campaigner treats fog as an enemy formation — present, purposeful, and requiring response.",
            "Move slowly in fog or not at all. The cost of moving into something unexpected is always higher than the cost of delay.",
            "Fog over the Anduin valley is thicker than it looks from the hillside above it.",
            "Intelligence reports are unreliable in fog conditions. Hold decisions until visibility returns.",
            "A fog advance is something some commanders attempt. Most of them remember it afterward for the wrong reasons.",
            "The fog belongs to whoever knows the ground better. Make sure that is you.",

        };

        // ── WEATHER — STORM ────────────────────────────────────────────────────
        public static readonly string[] WeatherStorm = {
            "A storm breaks over the campaign area. Operations halt and the wind carries orders away before they can be heard.",
            "Lightning over the mountains — the storm that has been building for three days finally arrives.",
            "The storm grounds everything. Marching, scouting, fighting — all paused by something larger than any of them.",
            "The tent ropes strain and the fires gutter. A serious storm is coming in from the west.",
            "Hail on armour is an experience that gets remembered. The sentries are having it now.",
            "Gales from the north flatten whatever was not staked down. The camp weathers it as best it can.",
            "A proper mountain storm — the kind that makes you understand why the old songs treated weather as a character.",
            "The storm passes with the thoroughness of something that wanted to make a point. What it leaves is wet, cold, and intact.",
            "Operations suspended by weather. Even the most urgent campaign has to yield to this kind of sky.",
            "The Misty Mountains in storm produce weather that does not care about your timeline.",
            "A bad storm on the road is a good time to hold position and check the supply wagons.",
            "The storm passed through like an argument that settled nothing but left everything rearranged.",
            // ── expansion ──
            "The storm hit the camp with full force in the second hour of the night watch.",
            "Operations are suspended by the scale of this weather system. There is no alternative.",
            "Lightning on the high passes means the mountain routes are closed until the storm passes.",
            "The storm passed through like something with a grudge. The camp is in disorder.",
            "After the storm, the count begins — supply wagons overturned, horses spooked, tents destroyed.",
            "Storm weather also covers enemy movements. The storm passes and the intelligence picture needs reassessing.",
            "The storm that builds over the Misty Mountains takes days to arrive and hours to finish. This is one of those arrivals.",
            "Operations interrupted by weather should be resumed with full intelligence review — the enemy also moved during the storm.",
            "Even the most urgent campaign yields to weather of this scale. Yield gracefully and prepare to resume.",
            "A proper mountain storm — the kind that makes the armies of men look like exactly what they are.",
            "The storm has passed but its effects have not. Roads damaged, rivers risen, camp degraded.",
            "After this storm, every estimate of march time needs to be revised upward.",

        };

        // ── NOTABLE KILLED (non-hero) ──────────────────────────────────────────
        public static readonly string[] NotableKilled = {
            "A notable person has died. Not every significant death happens on a battlefield.",
            "The death of someone of consequence — felt in the settlement and the councils that depended on them.",
            "A notable falls. Whether in battle, by disease, or by misfortune, the gap they leave is real.",
            "Someone the region depended on is gone. The practical consequences will follow the grief.",
            "A death that matters beyond the family that mourns it. Note it and its implications.",
            "The notable is dead. Whatever leverage their presence represented is now unclaimed — and competing interests will notice.",
            "A person of influence is gone. The power that gathered around them begins dispersing before the funeral.",
            "The death of a notable is also the beginning of a succession argument. Watch carefully.",
            "Not a soldier, not a captain — but someone whose absence from the settlement will be felt differently than a battle casualty.",
            "The notable's death changes the local politics in ways that will take time to become visible.",
            // ── expansion ──
            "The notable is dead and the local politics are now in motion around the vacancy.",
            "Not every significant death is a battle death. This one was not, and it matters the same way.",
            "The person who held certain threads of local relationship and influence is gone. Those threads are loose now.",
            "A notable's death creates a succession vacuum that is filled by competition, not appointment.",
            "The people who relied on this notable now have to find a different arrangement.",
            "Deaths of consequence ripple through the political structure in ways that are not always visible immediately.",
            "The notable fell — to disease, to violence, to age, to the campaign itself. The cause is secondary to the consequence.",
            "Watch who moves to fill the space. That movement tells you more about the current political landscape than most reports.",
            "A person of local consequence is gone. Establish contact with their successors before the vacuum fills itself badly.",
            "The notable's death changes the local council dynamics in ways that need active management.",

        };

        // ── QUEST COMPLETED ────────────────────────────────────────────────────
        public static readonly string[] QuestCompleted = {
            "The task is done. Whatever was promised can now be delivered, and whatever was owed can now be claimed.",
            "Completed. The work required was not simple; the outcome is not nothing.",
            "The errand is run, the promise kept, the obligation discharged. That is how trust is built — one completed task at a time.",
            "A task finished in a world that rarely lets tasks be finished cleanly. Mark the occasion.",
            "Done. The people who needed this done will know it was done — and they will remember who did it.",
            "The quest ends here in completion, which is the best way for quests to end.",
            "Whatever was asked has been answered. Whatever was needed has been provided.",
            "A commitment made and a commitment kept. In a war that depends on trust, these are not small things.",
            "The work is done. Time to return, report, and receive what was promised in exchange.",
            "Finished. The outcome was the one intended. That is not always how things go — take the success for what it is.",
            // ── expansion ──
            "The task is done and the people who needed it done know it. Both matter.",
            "An obligation met. The relationship that required it is stronger for it.",
            "Completed on time and on terms. That combination is rarer than it should be.",
            "The errand is finished and the result is what was promised. Mark it.",
            "A completed task is a completed trust deposit. The account is in better standing.",
            "Done. The specifics of how it was done will matter later. They were done correctly.",
            "The commitment was made and the commitment was kept. That is the entire record.",
            "Whatever was promised has been delivered. The people who doubted you will note the delivery.",
            "A task finished and finished well. The standard has been set for the ones that follow.",
            "The obligation is discharged and the relationship that carried it is intact.",

        };

        // ── QUEST FAILED ───────────────────────────────────────────────────────
        public static readonly string[] QuestFailed = {
            "The task was not completed in time. The consequences of that will arrive before any explanation of why.",
            "The errand failed. The people who needed it done still need it done, and now they have less reason to trust you to do it.",
            "A commitment was made and not kept. That has a cost, and the cost is always partly the relationship.",
            "The quest ends here in failure, which is the worst way for quests to end and the most instructive.",
            "Something went wrong with the task. Find out what, before the same thing goes wrong with the next one.",
            "The work was not done. That is a simple sentence with complicated implications.",
            "Failed. Not unusual — failure is part of the tally of anyone who attempts enough. What matters is the response.",
            "The obligation was not met. The person who set it will not forget. Neither should you.",
            "A task failed. Add it to the account and begin working on how it can be addressed.",
            "Not every errand comes back with success. This one did not. Learn, adjust, try again.",
            // ── expansion ──
            "The task failed. The reasons for it need to be understood before the relationship can be repaired.",
            "Failed. The person who needed this done still needs it done, and trusts you less than before.",
            "An unmet obligation is a completed trust withdrawal. The account is in worse standing.",
            "The failure is real. The explanation is owed. Deliver the explanation before the consequences deliver themselves.",
            "Not done. That is the simplest version of a complicated outcome.",
            "The quest failed and with it something in the relationship with the person who set it.",
            "Failures should be acknowledged honestly and promptly. Delay increases the cost of both.",
            "The mission was not completed. Learning why is the most useful thing you can do with the failure.",
            "An unmet commitment is also an unasked question: what needs to change to prevent the next one.",
            "Failed. Not the last word on the matter — but the current word, and it needs to be reckoned with.",

        };
        // ═══════════════════════════════════════════════════════════════════════
        // WEIGHT OF RULE
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WeightStrainHigh = {
            "The burden of administration is beginning to crack the foundation of governance here.",
            "The clerks and stewards are overextended. The machinery of rule is grinding rather than turning.",
            "Too many decrees, too few hands to carry them out. The strain is showing in the quality of decisions made.",
            "Administrative strain has reached a level where mistakes become likely. Something must be delegated or shed.",
            "The halls of governance are busy in the wrong way — busy managing failure rather than progress.",
            "The weight of edicts and obligations has compounded into something that will break if not lightened.",
            // ── expansion ──
            "The administrative machinery is producing errors at a rate that reflects the strain.",
            "Too many obligations, insufficient staff, and decisions being made too far from the information that should inform them.",
            "Administrative strain at this level is self-compounding — errors create more work, which creates more strain.",
            "The Weight of Rule is grinding here. Something must be shed, delegated, or simplified.",
            "Governance capacity is below the governance demand. That gap produces outcomes that satisfy no one.",
            "The clerks are overworked, the records are behind, and the decisions are late. Fix the underlying structure.",
            "Rule strain this high means the cost of governing here is approaching the value of governing here.",
            "The administrative compact between the holding and its obligations is at risk of collapsing under its own complexity.",

        };

        public static readonly string[] WeightLegitimacyLow = {
            "The people governed here no longer fully believe in the authority that governs them. That is a slow crisis.",
            "Rule Legitimacy is failing — not in battle, but in the daily practice of being obeyed.",
            "When legitimacy falls this far, orders begin to require persuasion. That is expensive and unsustainable.",
            "The governed do not openly resist. They simply fail to comply, quietly and persistently.",
            "A governance crisis does not always look like a crisis. It looks like nothing getting done.",
            "The authority of the ruling house here has thinned to the point where it must be rebuilt, not assumed.",
            // ── expansion ──
            "The governed do not believe in the authority's right to govern. That belief cannot be coerced into existence.",
            "Legitimacy is not what is claimed — it is what the governed grant. The grant here is withdrawing.",
            "Orders are followed reluctantly and partially. That is the texture of governance under low legitimacy.",
            "When people stop believing the authority is legitimate, they find creative ways to not comply with it.",
            "The governing house here has done something — or failed to do something — that the people noticed.",
            "Legitimacy this low is harder to rebuild than to maintain. Begin rebuilding before it reaches the floor.",
            "The people are not hostile. They are simply not convinced. Unconvinced people are a different kind of problem.",
            "Rule without legitimacy is administration by friction. It works until it doesn't, and then it stops completely.",

        };

        public static readonly string[] WeightLegitimacyHigh = {
            "Governance here is working as intended. The people are not happy — they are simply satisfied, which is better.",
            "Rule Legitimacy holds. Edicts are carried out without the friction of constant resistance.",
            "The administrative compact is healthy. This is the kind of stability that lets harder decisions be made.",
            "Legitimacy this strong is an asset — it can absorb one or two unpopular decisions without fracturing.",
            "The governed here expect competent rule and receive it. That expectation is its own kind of governance.",
            // ── expansion ──
            "Governance here has earned the trust it operates with. That trust is now a governance asset.",
            "The legitimacy of rule here means edicts are carried out without significant resistance.",
            "When legitimacy is this strong, governance can absorb unpopular decisions without fracturing.",
            "The governed here cooperate not because they must but because they believe in the compact. Maintain it.",
            "Rule legitimacy this high is the result of consistent good governance over time. It is also its own incentive to continue.",
            "The governed are not happy about everything — they are satisfied with the arrangement. That is the better state.",
            "Strong legitimacy here means the hold can ask more of its subjects in difficult times and be answered.",
            "This is what governance is supposed to achieve. Protect it from the decisions that erode it.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // WATCH OF THE MARCHES
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WatchReadinessLow = {
            "The watch is under-strength and under-prepared. If something comes, the response will be slow.",
            "Readiness has dropped. The garrison knows it, and so does anyone watching from outside the walls.",
            "The sentries are present but not sharp. Fatigue, reduced rotations, stretched cover — the signs are there.",
            "A watch at low readiness is an invitation. Whether anyone accepts it depends on what they can see from a distance.",
            "The patrols have thinned. Roads that were watched are not watched. That information travels.",
            // ── expansion ──
            "The watch is under-manned for the territory it is supposed to cover.",
            "Fatigue has compounded in the garrison to the point where the watch rotation is not recovering between shifts.",
            "A watch below readiness is a watch that cannot respond to what it is supposed to catch.",
            "The gap between posted strength and effective strength in this garrison is growing.",
            "The patrols are shorter and less frequent than they should be. The ground they skip is now essentially unmonitored.",
            "Watch readiness this low sends a signal to anyone observing the patrol patterns from outside.",
            "The sentries are present but they are the kind of present that happens when soldiers know the watch is undermanned.",
            "Readiness cannot be sustained below a minimum investment. This watch is below that minimum.",

        };

        public static readonly string[] WatchBurdenHigh = {
            "The civic cost of maintaining the watch is beginning to consume what the watch is supposed to protect.",
            "Watch burden is high. The villages that support the garrison are feeling it in their stores and their labour.",
            "The people are paying for their protection in ways that make them feel less protected.",
            "The watch sustains itself at the cost of the things it was raised to defend. That equation has a limit.",
            // ── expansion ──
            "The villages are paying for the garrison in ways they were not designed to sustain.",
            "The watch burden has become a resentment. Resentment is the precursor to noncompliance.",
            "The people supporting this garrison are doing so at a cost that approaches the cost of the threat the garrison protects them from.",
            "Civic burden this high means the watch is consuming the thing it was raised to protect.",

        };

        public static readonly string[] WatchReadinessGood = {
            "The watch is rotation-fit and positioned well. Nothing will arrive here unannounced.",
            "Readiness is solid. The garrison runs its routines without prompting — that is the standard to maintain.",
            "The local watch has its eyes open and its supply stores adequate. A good picture on a quiet day.",
            // ── expansion ──
            "The garrison is at the level it should be. Every route into this settlement is covered.",
            "Readiness is maintained without prompting. That is the marker of a professional watch.",
            "The watch schedule holds and the soldiers are rested within it. A good picture.",
            "Effective coverage at acceptable burden — the combination that watch readiness actually means.",
            "The garrison here knows what it is watching for and has the capacity to watch for it.",
            "A watch in good order is a watch that can respond rather than just observe.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BONDS OF THE HOLDING
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BondsStrainHigh = {
            "The compact between the holding and its villages is under pressure. Labour is thin and the grain count is falling.",
            "Village strain is growing. The smaller settlements that support this holding are approaching their limit.",
            "The bonds that hold a holding together are under stress — too much taken out and not enough put back.",
            "The villages nearby are strained. When the fields are short-handed, the granaries follow.",
            "Compact legitimacy is eroding. The people of the villages see the holding as a burden, not a protector.",
            // ── expansion ──
            "The compact between this holding and its villages is bending under the weight of uneven obligation.",
            "The villages have been giving more than the holding has been returning. The accounting shows.",
            "Village strain this high is approaching the point where the villages stop being an asset and become a liability.",
            "The bonds are strained because the terms of the compact are not being met by one or both sides.",
            "When village strain is high and compact legitimacy is low, the holding loses its agricultural foundation.",
            "The relationship between holding and village is a reciprocal one. The reciprocity has broken down.",
            "Village labour is declining along with village confidence that the holding is worth supporting.",
            "The bonds that make a holding functional are fraying. They do not need to break before they cause damage.",

        };

        public static readonly string[] BondsGrainLow = {
            "Grain flow from the villages has fallen short. The holding's stores depend on a supply that is thinning.",
            "The harvest was lean, or the requisition was heavy, or both. Either way the grain account does not balance.",
            "Food security here depends on roads and trust between holding and village. At least one of those is weakening.",
            // ── expansion ──
            "The grain flow is below the level the holding's stores need to survive the winter.",
            "The harvest shortfall ripples forward — lower stores mean tighter rationing and higher strain on everything downstream.",
            "Grain flow this low means decisions are being made about who eats and who does not.",
            "The agricultural foundation of this holding is weakened. The effects will be felt through the cold season.",
            "Low grain means the holding cannot support the garrison it has or the ambitions it holds.",
            "The harvest did not meet the holding's needs this season. Adjust plans before the gap becomes a crisis.",

        };

        public static readonly string[] BondsHealthy = {
            "The compact is holding. Villages supply what was agreed, and the holding protects what the villages need.",
            "Grain is moving, labour is present, and the villages trust the hold. That is the whole foundation of a functioning estate.",
            "The bond between holding and village is in good health. It will not stay that way without maintenance.",
            // ── expansion ──
            "The compact between holding and village is functioning as it was designed to.",
            "Grain flows, labour arrives, and the holding returns protection and governance. A working relationship.",
            "The bonds are healthy here because someone has maintained them with consistent attention.",
            "A healthy holding compact is the foundation everything else is built on. This one is solid.",
            "The villages trust the holding. The holding is responding to that trust. The compact is alive.",
            "Healthy bonds mean this holding can weather disruption without immediate collapse.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // THE LONG MENDING
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] LongMendingWounded = {
            "This settlement carries wounds that do not show on the walls. The recovery process will be long.",
            "The damage here is deeper than stone and timber. It will take time, resources, and sustained attention.",
            "A hurt settlement recovers at its own pace — faster with investment, slower with neglect.",
            "The Long Mending has begun here, as it must after what this place has been through.",
            "Wounds of this kind are not healed by a single season of effort. Plan for years, not weeks.",
            // ── expansion ──
            "The healing process here is real but slow. Slow is the right pace — rushing it compounds the damage.",
            "A settlement in recovery is more vulnerable than one in crisis. The crisis created momentum; recovery requires patience.",
            "The wounds here are the kind that outlast the events that caused them.",
            "Recovery from deep damage requires sustained investment. Not dramatic intervention — sustained, ordinary attention.",
            "The mending has begun. The timeline is measured in seasons.",
            "A settlement carrying this level of damage needs consistent presence from someone who understands recovery.",
            "The Long Mending is the unglamorous work that follows the dramatic events. It is also the necessary work.",

        };

        public static readonly string[] LongMendingRecovering = {
            "Recovery is progressing here. Not quickly, and not without setbacks — but progressing.",
            "The mending is underway. What was broken is being rebuilt, slowly and without ceremony.",
            "The settlement is pulling itself back from what happened to it. The process is visible to those who watch carefully.",
            "Progress in the Long Mending does not always look like progress. Sometimes it just looks like less damage.",
            // ── expansion ──
            "The mending is visible to those who have been watching. Progress compounds on progress.",
            "Recovery is uneven — some things heal faster, others slower. The overall direction is the metric.",
            "The settlement is healing from the inside out. External investment accelerates what is already underway.",
            "Progress in the Long Mending is visible in the quality of the decisions being made here.",
            "The recovery continues. It requires less intervention now than it did at the start.",
            "The worst is past. The mending has achieved the phase where it sustains itself with modest support.",

        };

        public static readonly string[] LongMendingMended = {
            "This place has come through its recovery and is the stronger for having survived it.",
            "The Long Mending is complete here. What was rebuilt is not identical to what was lost — sometimes it is better.",
            "Recovery complete. The chronicle of this settlement includes what it endured and what it became after.",
            // ── expansion ──
            "The mending is complete. What was broken has been rebuilt into something functional, if not identical.",
            "The settlement has come through its recovery stronger in the ways that matter for what comes next.",
            "The chronicle of this place includes what it endured and what it did afterward. The afterward is good.",
            "Mended. Not the same as before — changed, in ways that reflect the work of recovery.",
            "The work of the Long Mending is finished here. The settlement stands as evidence that recovery is possible.",
            "A recovered settlement is an argument for the value of sustained investment in the aftermath of damage.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // BURDENED MARCHES  ({0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] MarchPressureHigh = {
            "The frontier burden around {0} has risen to a level that demands active response.",
            "March pressure near {0} is building. The pattern of raids and siege-threats has compounded.",
            "The roads around {0} are beginning to feel dangerous in a way that maps cannot fully capture.",
            "Frontier conditions near {0} are deteriorating. The holding's outlying positions are exposed.",
            "What began as occasional pressure on the marches around {0} has become a sustained condition.",
            // ── expansion ──
            "The frontier condition around {0} has become a sustained problem rather than an intermittent one.",
            "The holding's outlying positions near {0} are no longer tenable without active reinforcement.",
            "March pressure near {0} is affecting the settlement's ability to function normally.",
            "The pattern of frontier threats around {0} has become systematic. Address the pattern, not just individual incidents.",
            "Everything beyond the walls of {0} is now contested in a way that changes what the holding can do.",
            "The roads out of {0} carry increasing risk. Trade and communication are both affected.",
            "Frontier pressure near {0} has reached the level where it changes the garrison's posture permanently.",

        };

        public static readonly string[] MarchPressureEasing = {
            "The frontier pressure near {0} is lower than it was. That can change, but today it is quiet.",
            "March conditions around {0} have improved. The border is not secure — it is merely less threatened.",
            "The pressure on the marches near {0} has eased. Use the breathing room to reinforce.",
            // ── expansion ──
            "The pressure near {0} has reduced. Use the breathing room before it closes again.",
            "March conditions around {0} are temporarily improved. Do not let temporary become assumed.",
            "The frontier near {0} is quieter than it was. Reinforce the position before the quiet ends.",
            "Pressure easing near {0} is an opportunity to consolidate and repair what the sustained pressure damaged.",
            "The situation near {0} has stabilised. Stability requires maintenance to persist.",
            "A quieter period on the marches near {0}. The work of this period should be preparation for the next escalation.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // FOUNDING OF HOLDS  ({0} = settlement name, {1} = stage number)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] FoundingProgress = {
            "The founding of {0} advances. Each stage completed is a commitment that cannot easily be undone.",
            "{0} is taking shape — stage {1} reached. The hold is becoming real in ways that matter politically.",
            "Progress on {0} is visible to those watching. Word of a new hold travels to people who care about such things.",
            "Stage {1} of the founding at {0}. Charter obligations are being met and the structure is growing.",
            // ── expansion ──
            "{0} advances to stage {1}. The founding is becoming difficult to reverse, which is the goal.",
            "The charter obligations at {0} are being met. Each obligation met is a brick in the political edifice.",
            "Stage {1} reached at {0}. The hold is now real enough to be contested, which means it is real enough to matter.",
            "The founding at {0} progresses. Other powers are watching and recalculating.",
            "{0} is developing in ways that will require formal recognition from neighbouring holdings.",
            "The work at {0} is compounding. What was incipient is becoming established.",

        };

        public static readonly string[] FoundingRecognition = {
            "The founding at {0} has reached a stage where other powers will need to acknowledge it.",
            "What began as an incubating hold at {0} is now a political fact. Recognition must follow.",
            "The hold at {0} has grown past the point where it can be ignored by those with interests in this region.",
            // ── expansion ──
            "The hold at {0} has grown past the threshold of deniability for those with interests in this region.",
            "Recognition for {0} is not optional for long. The political reality demands acknowledgment.",
            "What began at {0} has become something the regional power structure must account for.",
            "The founding at {0} has earned its place on the map. Whether it stays there depends on what comes next.",
            "The hold at {0} is now a political fact with the weight of established fact.",
            "{0} is past the point of being a founding. It is a settlement with legitimacy and position.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // HOSTS IN THE FIELD
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] HostsCohesionLow = {
            "The host is losing cohesion. Soldiers are present but the discipline that makes them an army is thinning.",
            "Supply gaps and march strain have begun to show in the way the host moves and responds to orders.",
            "A host below cohesion threshold is an army in name. The distance between name and capability is growing.",
            "Cohesion is the thing armies lose quietly before they break loudly. Watch the trend here.",
            // ── expansion ──
            "The host moves together but no longer thinks together. Cohesion is not just physical proximity.",
            "Supply problems are translating into discipline problems. That translation is always fast.",
            "The march has exposed the gaps in the host's composition and morale. Both need addressing.",
            "A host losing cohesion is a host that will not perform to its order of battle under pressure.",
            "The soldiers are present and armed. The army those soldiers should compose is somewhere between here and where they were assembled.",
            "Cohesion is the invisible structure that makes an army more than a group of armed people. That structure is weakening.",
            "Fix the supply and the cohesion often follows. If the supply is fixed and the cohesion does not follow, the problem is deeper.",
            "A host at low cohesion needs a rest period, a supply restoration, and a clear objective. All three, not one.",

        };

        public static readonly string[] HostsRequisitionHigh = {
            "Requisition pressure from the host is falling heavily on the settlements it passes through.",
            "The army's supply demands are beginning to create a burden that the nearby holdings will feel for months.",
            "What the host takes from the land it passes through costs more than the goods. It costs trust.",
            "Heavy requisition near a settlement is a tax on the compact between the army and the people it protects.",
            // ── expansion ──
            "The army's needs are being extracted from the land it moves through at a rate that will be remembered.",
            "Heavy requisition is fast credit against a slow account. The settlements will not forget the debt.",
            "The holding's relationship with the army depends on the army not consuming what makes the holding viable.",
            "Requisition this high is a strategic cost that will show up in the political accounts after the campaign.",
            "The army takes what it needs. What it is taking is also what the people need to survive the winter.",
            "Heavy requisition near any settlement changes how that settlement feels about the army it is feeding.",
            "The burden of supply falling on one area is a political as well as a logistical problem.",
            "Requisition pressure this high will cause defection of support, not of soldiers.",

        };

        public static readonly string[] HostsCohesionGood = {
            "The host is in good order. Supply lines are holding and the soldiers are answering the march well.",
            "Cohesion is strong. The host moves like an army and not like a collection of armed men going the same direction.",
            "Field discipline is sound. The kind of army you can ask things of and expect answers from.",
            // ── expansion ──
            "The host moves with the discipline of a formation that knows where it is going and why.",
            "Good cohesion means the host performs above the level of its individual components.",
            "The army is functioning as designed. That is harder than it sounds and worth maintaining.",
            "Field cohesion at this level means the host will fight in the formation it was built to fight in.",
            "The supply lines are holding and the soldiers' confidence in them is holding. Both are required.",
            "A cohesive host is one that can be trusted to execute complicated orders.",
            "Good order in the host is the visible product of invisible management. Maintain both.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // OATHS AND HOUSES
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] OathsFeudheatHigh = {
            "Feud heat is rising between houses. Unaddressed grievances compound until they stop being political and start being military.",
            "The heat between these houses has built to a point where a spark will find no shortage of fuel.",
            "Feud heat this high means the oaths that should bind the houses together are being tested at their weakest points.",
            "When houses grieve each other to this degree, the lord who lets it continue will eventually pay for both sides of it.",
            // ── expansion ──
            "Feud heat between houses is approaching the level where incident becomes inevitability.",
            "The grievances have been accumulating without resolution. Each unresolved one adds to the heat.",
            "Houses in this state do not wait for permission to act on their grievances. Intervene before intervention becomes irrelevant.",
            "The oath structure cannot hold indefinitely against the pressure of feud heat this intense.",
            "A feud that is allowed to reach this temperature will require blood to cool. Prevent it.",
            "The houses are watching each other for openings. Any significant event will provide one.",
            "Feud heat this high means the lord who controls both houses is running out of the commodity called restraint.",
            "The houses are in a state that has its own momentum. That momentum runs toward conflict unless redirected.",

        };

        public static readonly string[] OathsStrengthHigh = {
            "The oath compact between the houses is solid. Obligations are being met and trust is accumulating.",
            "Strong oath-keeping is its own politics. The houses that honour their word attract the houses worth having.",
            "The oath structure is sound here. When the pressure comes, that soundness will matter enormously.",
            // ── expansion ──
            "The oath compact is strong here. When the test comes — and it will come — the structure will hold.",
            "Houses that keep their oaths attract other oath-keeping houses. The compact is self-reinforcing.",
            "Strong oath practice is its own reward in the political economy of the realm.",
            "The oaths here are kept because the parties believe in them, not because they fear the consequences of breaking them. That is the stronger kind.",
            "Oath strength this high means the lord's requests are answered before they become demands.",
            "The compact holds and the houses believe it holds. Both together are what makes it durable.",
            "When the pressure comes, the houses that have been keeping their oaths will be the first to know they can call on the lord's response.",

        };

        public static readonly string[] OathsStrengthLow = {
            "Oaths here have thinned to the point where they are obligations in form but not in spirit.",
            "The compact between lord and house is weakening. That weakening is visible to the houses watching from outside it.",
            "Oath strength this low is an early warning. What it warns about is a political rupture that could have been prevented.",
            // ── expansion ──
            "The oath compact is thin here. What is owed is technically met and meaningfully neglected.",
            "Houses in this condition are looking for better compacts. They will find them if you do not provide them.",
            "An oath kept in form but not in spirit is a diplomatic failure wearing a legal costume.",
            "The houses have registered the weakness of the compact. They are adjusting their behaviour accordingly.",
            "Low oath strength is the precursor to defection. The houses are not defecting yet — they are preparing to.",
            "The compact's weakness is visible. What is visible to you is visible to rivals who might offer better terms.",
            "Oath strength this low means the lord's requests are received as suggestions.",
            "Repair the oath compact before the houses make the repair unnecessary by finding someone else to swear to.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // RECOVERY LEGACY TRAITS  ({0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] LegacyTraitEarned = {
            "{0} has earned a legacy identity through repeated good practice. That identity will endure beyond individual rulers.",
            "The chronicle of {0} now carries a legacy mark. It is a record of what this settlement became through sustained effort.",
            "A settlement that earns legacy traits has done something harder than surviving. It has consistently chosen the right path.",
            "{0}'s legacy has been scored and found worthy. The recognition is durable — but so is the responsibility it implies.",
            // ── expansion ──
            "{0} has earned its legacy mark through the sustained practice of good governance and recovery.",
            "A legacy trait at {0} is the chronicle acknowledging what has been consistently done right.",
            "The recognition is durable. So is the obligation it implies.",
            "{0}'s chronicle now carries something that most settlements do not earn. Guard what produced it.",
            "The legacy of {0} is written in practice rather than declaration. That is the legitimate kind.",
            "What {0} has earned here took time and consistency. Neither can be manufactured.",

        };

        public static readonly string[] LegacyScoreBuilding = {
            "The legacy score at {0} is accumulating. Good recovery, good watch, good support — the evidence is compounding.",
            "Consistent good practice at {0} is being recorded. The chronicle sees what passes quickly in the present.",
            // ── expansion ──
            "The good practice at {0} is accumulating in ways that the chronicle records and the realm eventually recognises.",
            "Consistent right action at {0} is building toward something that will outlast the current administration.",
            "The score is building. The people doing the work that earns it may never fully see the recognition.",
            "{0}'s legacy is being written one day of good practice at a time. That is the only way it is written.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // COURT / COUNCIL  ({0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] CourtPetitionsPending = {
            "Petitions are accumulating at {0}. Unaddressed, they become grievances. Grievances become politics.",
            "The court at {0} has pending petitions that will not wait indefinitely. Someone should hear them.",
            "Governance requires listening as much as deciding. The petition queue at {0} is reminding you of that.",
            "Unheard petitions at {0} are a signal about what the people need that has not yet been received at court.",
            // ── expansion ──
            "The petition queue at {0} is a measure of what the people need that the court has not yet addressed.",
            "Unheard petitions become public grievances. The public is already aware of the queue.",
            "The court at {0} needs to hear these petitions before they become the substance of a more significant political problem.",
            "Petitions represent the last formal channel people use before they find informal ones.",
            "Unaddressed petitions at {0} are compounding. Each week they wait, the narrative around them changes.",
            "The people do not petition courts they do not believe in. That they petition yours means something — respond to it.",
            "The court's queue is a governance barometer. What is in it tells you what the people need most.",
            "Pending petitions are pending decisions. Decide them before they decide themselves.",

        };

        public static readonly string[] CourtPrestigeHigh = {
            "The court at {0} carries genuine prestige. Petitions are brought in good faith and decisions are respected.",
            "A prestigious court is one that has earned the reputation of being worth appealing to. {0} has that.",
            "Court prestige at {0} is a governance asset. It makes administration easier in ways that are hard to see until it is gone.",
            // ── expansion ──
            "Court prestige at {0} means the court is used for its intended purpose by people who believe in it.",
            "A prestigious court attracts substantive petitions and deters frivolous ones. Both are good.",
            "The reputation of {0}'s court is making governance easier in ways that are hard to see but real.",
            "Court prestige at this level is an asset that needs protecting from the decisions that would erode it.",
            "The court at {0} commands respect that makes its decisions more likely to be implemented fully.",
            "Prestige here means the court functions as a genuine resolution mechanism, not just a formal one.",
            "A court that is believed in is a court that does not need to enforce its decisions as often.",
            "The prestige of {0}'s court reflects something real about how it has conducted itself.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ROADS OF MIDDLE-EARTH  ({0} = settlement name)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RoadsMEDangerous = {
            "The roads near {0} have become unsafe. Caravans are taking longer routes or not running at all.",
            "Corridor danger near {0} has reached a level where travel is a calculated risk rather than a routine.",
            "The roads around {0} are compromised. Everything that moves on them — goods, news, soldiers — moves in danger.",
            "Road safety near {0} has deteriorated to the point where it is shaping decisions made inside the walls.",
            // ── expansion ──
            "The roads around {0} are now a factor in every logistical decision made here.",
            "Travel through the corridor near {0} requires armed escort for anything worth protecting.",
            "Corridor danger near {0} has reached the level where it is shaping the campaign's supply options.",
            "The roads around {0} are compromised by a combination of threat and uncertainty.",
            "What moved freely through the corridor near {0} six months ago moves at risk or does not move at all.",
            "Road safety near {0} has become a command problem, not just a patrol problem.",
            "The roads around {0} are telling everyone who uses them that the region is not under control.",
            "Corridor danger near {0} needs a dedicated response, not just an adjusted patrol schedule.",

        };

        public static readonly string[] RoadsMEGood = {
            "The roads near {0} are clear and in good condition. Movement of goods and people is normal.",
            "Corridor health near {0} is solid. The old road does its work without requiring constant attention.",
            "The roads around {0} are safe enough that travellers use them without calculating the risk. That is the standard.",
            // ── expansion ──
            "The roads near {0} are passable, patrolled, and functioning. That combination enables everything else.",
            "The corridor near {0} is clear. Use the clarity — it may not last.",
            "Trade, communication, and military movement through {0}'s road network are all operating normally.",
            "Safe roads near {0} mean the holding's economy and the campaign's logistics are both functioning.",
            "The road condition near {0} reflects consistent patrol investment. Maintain it.",
            "A clear corridor near {0} is a competitive advantage that should be advertised and protected.",
            "Safe roads near {0}: the quiet achievement that enables every other achievement in the region.",
            "The roads are good and the people know it. That knowledge changes how the region functions.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // ARMY COMPOSITION / DOCTRINE
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] ArmyDriftHigh = {
            "Composition drift has taken the host away from its doctrine profile. The army fights differently than it was designed to.",
            "The roster has drifted from its intended shape. Reinforcement gaps have accumulated into a strategic problem.",
            "Doctrine drift is expensive in ways that do not show on casualty lists. It shows when the wrong fight happens.",
            "The army's composition no longer matches its doctrine. That mismatch will matter when the battle requires what the doctrine assumed.",
            // ── expansion ──
            "The army's composition has drifted from its doctrine profile. It will not fight the way it was built to fight.",
            "Drift between doctrine and composition is the gap that matters in a specific engagement at a specific time.",
            "The roster reinforcement has not maintained the balance the doctrine required.",
            "Composition drift at this level means the army is functionally a different force than the doctrine assumed.",
            "Reinforcing without regard to composition balance produces armies that surprise their own commanders.",
            "The army will not fight the battle that was planned for it — it will fight the battle its current composition allows.",
            "Doctrine drift is an invisible problem until it is a visible disaster. Correct it while it is still invisible.",
            "The host's composition and its doctrine are telling different stories. One of them will be believed in battle.",

        };

        public static readonly string[] ArmyDriftLow = {
            "The host is near its doctrine profile. Reinforcement has kept the composition close to what was intended.",
            "Composition discipline is being maintained. The army will fight the way it was built to fight.",
            // ── expansion ──
            "Composition discipline maintained. The army will fight as designed.",
            "The host matches its doctrine profile. What was planned for is what the army can deliver.",
            "Close to doctrine profile — reinforcement is working as intended.",
            "The roster holds its designed shape. The battle plan written for this host remains valid.",
            "Drift is low. The army is the army that was built.",
            "Good composition discipline means the host's commanders can rely on what they assume.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // RANGER'S SATCHEL
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RangerCampPoor = {
            "Camp readiness is low. Rest, foraging, and preparation routines are not keeping pace with the march.",
            "The camp is not in good order. That will show tomorrow in the rate of march and the state of the soldiers.",
            "A poorly-kept camp is the beginning of a poorly-kept campaign. The correlation is not coincidental.",
            // ── expansion ──
            "The camp has fallen behind on the basic routines that sustain a company on a long march.",
            "Poor camp readiness shows up in march performance before it shows up in battle performance.",
            "The health of the camp is a leading indicator of everything that follows.",
            "A camp that is not in order is a company that is not in order. Fix the camp.",
            "Camp discipline is the unglamorous foundation of effective campaigning.",
            "The camp is behind on maintenance. That will be felt in the march pace by tomorrow.",
            "Poor camp readiness compounds — a tired company makes a worse camp, which makes a more tired company.",
            "The Ranger's Satchel is not well stocked and the camp reflects it.",

        };

        public static readonly string[] RangerWeatherSevere = {
            "The weather is severe enough to affect everything the company does — movement, morale, foraging, health.",
            "Conditions are bad enough that the plan of march should account for them rather than push through them.",
            "Severe weather in Middle-earth is not merely inconvenient. Mountains and marshes and passes are less forgiving than roads.",
            // ── expansion ──
            "The weather is severe enough that it changes what is possible today.",
            "Conditions this difficult require explicit planning decisions, not just adjusted morale.",
            "Severe weather is the campaign planner's constant variable. Today it has become a constant problem.",
            "Adjust the plan for the weather or adjust to the cost of not adjusting.",
            "The company is operating under conditions that test equipment, health, and morale simultaneously.",
            "Severe weather is an objective tactical factor. Plan for it.",
            "The mountains make their own weather and it has no interest in your schedule.",
            "Operating in severe weather is a different kind of soldiering. The company needs to know how.",

        };

        public static readonly string[] RangerCampGood = {
            "The camp is well set. Rest has done its work and the company is ready to move on schedule.",
            "Good camp discipline is invisible when it works. Today it is working.",
            "A well-prepared camp is a force multiplier that never appears on the order of battle. It shows in everything else.",
            // ── expansion ──
            "The camp is in order. The company will march well tomorrow because of what was done today.",
            "Good camp discipline is the invisible force multiplier that never appears on the order of battle.",
            "The camp is prepared. That preparation will compound forward through the next leg of the march.",
            "A well-run camp is not glamorous work. It is the most reliable kind.",
            "Camp readiness is good. The company's performance tomorrow is being secured tonight.",
            "The Ranger's Satchel is stocked and the camp reflects it.",
            "Good camp discipline means the march resumes tomorrow at the pace it should, not the pace exhaustion allows.",
            "The company rested well. That is an investment that pays out on the road.",

        };

        // ═══════════════════════════════════════════════════════════════════════
        // PALANTIR  ({0} = health tier label)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PalantirHealthPoor = {
            "The Palantír reports that the campaign's save-health has fallen to a {0} state. Attention is warranted.",
            "Campaign integrity is showing strain. The Palantír reads {0}. Consider a snapshot before proceeding.",
            "A {0} save-health reading from the Palantír is worth treating seriously. The stone does not exaggerate.",
            // ── expansion ──
            "The Palantír's reading is {0}. Attention to campaign health is warranted now.",
            "Campaign integrity has dipped to {0} state. The stone sees the pattern even when the commander does not.",
            "A {0} reading from the Palantír should be acted on before it reaches the next tier.",
            "The campaign record shows {0} health. Consider a snapshot and a review before proceeding.",
            "The Palantír does not speculate. A {0} reading is the stone's specific assessment.",
            "Save-health at {0} means the chronicle has identified something that needs attention.",

        };

        public static readonly string[] PalantirHealthGood = {
            "The Palantír shows the campaign in sound health. The record is clean and the chronicle is intact.",
            "Campaign health is solid. The stone sees clearly and what it sees is stable.",
            // ── expansion ──
            "The Palantír shows the campaign in clean order. The record is intact.",
            "Campaign health is solid across the metrics the stone is measuring.",
            "A good reading from the Palantír. The chronicle is coherent and the campaign is sound.",
            "The stone sees clearly and what it sees is a campaign in order.",
            "Campaign integrity is high. Whatever is being done to maintain it, continue doing it.",
            "The Palantír confirms what good management produces: a campaign that is still its intended shape.",

        };

    }
}
