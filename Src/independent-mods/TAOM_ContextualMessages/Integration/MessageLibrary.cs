using TaleWorlds.Core;

namespace ContextualMessages
{
    /// <summary>
    /// All contextual message line pools. Static arrays, random selection with
    /// last-used avoidance. Vanilla Bannerlord compatible — no mod dependencies.
    /// </summary>
    public static class MessageLibrary
    {
        private static readonly System.Collections.Generic.Dictionary<int, int> _lastPicked
            = new System.Collections.Generic.Dictionary<int, int>();

        /// <summary>Pick a random line, avoiding the most recently used one.</summary>
        public static string Pick(string[] pool)
        {
            if (pool == null || pool.Length == 0) return "";
            if (pool.Length == 1) return pool[0];
            int key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pool);
            int last = _lastPicked.ContainsKey(key) ? _lastPicked[key] : -1;
            int idx;
            int tries = 0;
            do { idx = MBRandom.RandomInt(0, pool.Length); tries++; }
            while (idx == last && tries < 10);
            _lastPicked[key] = idx;
            return pool[idx];
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — VICTORY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleWin = {
            "The field is yours. The enemy breaks and flees.",
            "Victory! The banner holds. Let the enemy carry word of this defeat back to their masters.",
            "They came in great numbers and still they could not stand. The line held.",
            "The battle is won. Count your men and tend the wounded — the road does not end here.",
            "Another host scattered. For a time, the threat retreats.",
            "Your company fought with honour. The dead lie thick upon the field.",
            "The enemy is routed. This victory will be remembered in the halls of your allies.",
            "You have driven them back. But do not mistake a battle won for a war ended.",
            "The foe breaks before you. Even the boldest must yield when courage stands against them.",
            "A hard fight, but the day is yours. The will of your people endures.",
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
        };

        // ═══════════════════════════════════════════════════════════════════════
        // BATTLE — DEFEAT
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] BattleLoss = {
            "The line breaks. Fall back — regroup — there is still a tomorrow to fight for.",
            "This day belongs to the enemy. Learn from it. Do not let them have the next.",
            "Defeat stings, but it need not be the final word. Survivors can still turn the tide.",
            "They were too many, or you were too few. Either way, the ground behind you must hold.",
            "A dark hour. But even the oldest trees once stood as saplings — patience and strength will return.",
            "The cost was high and the ground is lost. Rally what remains and find a defensible place.",
            "The enemy's strength is greater than counted. This intelligence must reach your allies.",
            "There is no shame in retreat when it preserves the force that will fight another day.",
            "You could not hold them here. Perhaps elsewhere — perhaps tomorrow — the answer lies.",
            "The men are shaken. They will need rest, reassurance, and a commander who does not despair.",
            "Not every fight can be won. Some must be survived. You survived this one.",
            "The enemy gains ground today. Tomorrow they will not go unopposed.",
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
            "Even the greatest fortresses were besieged before the tide turned.",
            "The road ahead is harder now. But it remains a road, and you can still walk it.",
            "Loss is the price of hubris — or of necessity. Today, perhaps it was the latter.",
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
            "Screams from a distant village. The war is also fought in places like this.",
            "Plumes of smoke mark the progress of a raid through farmland that will not recover quickly.",
            "No army required — a handful of raiders can unmake a year of peace in an afternoon.",
            "War does not always come with great armies. Sometimes it comes in smaller, quieter horrors.",
            "Those who cannot defend their villages must watch them burn. A truth that never stops being bitter.",
            "The crackling of burning thatch, the sound of a raid in progress — both linger long after the riders are gone.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — COMPLETED (village looted)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidCompleted = {
            "The village is stripped bare. Supplies taken, shelters burned, people scattered. A grim accounting.",
            "What took years to build has been undone in an afternoon. The raiders are gone — the damage remains.",
            "Ash and ruin where a settlement stood. This is what war costs in human terms.",
            "They came, took what they wanted, and left the rest burning. A familiar pattern in these dark days.",
            "The hamlet will not recover quickly. Its people will remember this long after the embers cool.",
            "No battle fought here — only cruelty and theft, and villagers who could do nothing but run.",
            "A community unmade by an afternoon's work. Raiding grows bolder when it goes unanswered.",
            "Grain stores burned. Livestock taken or slaughtered. Wells fouled. The raiders were thorough.",
            "Every village raided makes the next one more fearful. Fear is currency the enemy spends freely.",
            "The harvest that would have fed a hundred is gone. Hunger follows in the wake of every raid.",
            "When you find the force that did this, do not mistake victory over them for justice.",
            "An empty village under a smoke-stained sky. This is the price of land left undefended.",
            "The earth here will be cold before what was lost is rebuilt. And that is if it is rebuilt at all.",
            "Raids like this are not random — they are deliberate. The enemy knows that broken villages break morale.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — REPELLED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidRepelled = {
            "The village stands. The raiders found resistance they did not count on and paid for it.",
            "They came to pillage and found steel instead. A lesson the surviving raiders will not forget.",
            "A settlement defended. For once, the enemy left empty-handed.",
            "The militia held. Sometimes a few brave people with farm tools and old blades can turn the tide.",
            "The raid is broken. Smoke from enemy bodies instead of enemy torches — a welcome reversal.",
            "They thought it would be easy. It was not. The village endures.",
            "Not every hamlet is defenceless. Today, one reminded raiders of that at great cost to the raiders.",
            "The attackers flee with fewer numbers than they came with. The village lives to see tomorrow.",
            "When raiding is answered immediately, it does not always grow. Today, it was answered.",
            "A small victory — one village, one raid repelled — but it matters to the people who live there.",
            "Word will spread that this settlement bit back. That alone will deter the next raid.",
            "The fields still stand. The harvest still has a chance. One more reason to hold the line.",
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
            "War trumpets sound below the walls. The besiegers announce themselves to those who already knew they were coming.",
            "A siege is a battle measured in weeks, not hours. Hunger and despair are the enemy's best siege weapons.",
            "The drawbridge is up. The world outside is now divided into defenders and those who wish them harm.",
            "Walls that have stood for ages face another test. Stone is strong — but it cannot fight back.",
            "The enemy surrounds the fortress on all sides. Relief must come before the garrison's nerve breaks.",
            "They have chosen patience over storm. That patience will cost the defenders dearly if no help comes.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT CAPTURED
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
            "Walls, stores, garrison — all yours. Also the responsibility for the lives within them. Both can feel heavy.",
            "The siege is over. The settlement is yours. The next army that comes will make it theirs if you are not careful.",
            "You have taken what was defended. Now defend what you have taken.",
            "A fortress earned in blood and patience. Neither tends to make a good foundation for peace.",
            "The old banners are struck down. New ones are raised in the wind of a changed world.",
            "Every castle taken is also a line extended — another place to supply, another place to defend.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SIEGE — SETTLEMENT LOST
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
        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — PLAYER SUCCEEDED AS RAIDER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidSucceeded = {
            "The village is taken. The supplies are yours — as is the cost of what was done here.",
            "A swift raid and a full haul. The villagers will rebuild. Eventually.",
            "The operation is complete. Your soldiers carry what they came for.",
            "Ruthless and efficient. The village offered little resistance. Resupply the party from the spoils.",
            "The raid yields what you needed. Whether it was worth it is a question for later.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // RAID — PLAYER REPELLED AS ATTACKER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] RaidRepelledByDefenders = {
            "The villagers held. Your party is driven back without the supplies you came for.",
            "A repelled raid — they were better prepared than expected. Regroup and reconsider.",
            "The defenders gave more ground than expected — none. The raid yields nothing.",
            "Turned back. The village stands and your company takes the loss.",
        };


        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyHeroKilled = {
            "A foe of consequence is no more. Their soldiers will feel this loss more than any battle.",
            "The enemy's strength is diminished by more than just one. Leadership is not easily replaced.",
            "A commander falls. The enemy will feel this absence in their next engagement.",
            "One fewer enemy to plan against. The campaign is reshaped by their absence.",
            "A significant kill. Let the news travel — the enemy should know what it cost them.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO DEATH — ALLY KILLED
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
            "They were part of the story being written here. Their chapter ends. The story continues.",
            "Loss like this cannot be measured in gold or strategy. Take a moment — then keep moving.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // WAR DECLARED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WarDeclared = {
            "War is declared. The drumbeats of conflict travel faster than any rider.",
            "Swords are drawn and old grievances are given fresh edges. The war begins.",
            "What diplomacy failed to hold, war will now decide. A poor trade, always.",
            "The banners of two factions face each other across a line that will cost blood to cross.",
            "War has come again to Calradia. As if it had ever truly left.",
            "Heralds and ultimatums have run their course. Now it is the soldiers' turn.",
            "A war declared is never as easy as those who declared it imagined.",
            "The last letters are sent, the last arguments made. Now there is only war.",
            "In the histories they will call this the beginning. For those who must fight it, it already feels old.",
            "Two causes take the field. Both believe they are the righteous one. Wars tend to disabuse both.",
            "Alliances are tested, borders redrawn, and ordinary people pay the ordinary price of extraordinary decisions.",
            "Let those who called for this war be the first to explain it to the families of the fallen.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // PEACE MADE
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
        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — DAWN
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DawnMessages = {
            "Dawn breaks over the land. The world is new for exactly long enough to give you hope.",
            "The sun rises over the mountains. Another day to make something of.",
            "First light touches the peaks. The darkness retreats, as it always does — for now.",
            "Morning comes. The campaign does not pause for it, but it is beautiful all the same.",
            "A new day on the long road. The birds do not know or care what the night brought.",
            "The light returns. In the old stories, dawn always brings a turning — hold on to that.",
            "Grey gives way to gold. Another morning in the long march of this age.",
            "The watch changes at dawn. Fresh eyes look out over a world that has not changed — but the light has.",
            "Silver light on the morning hills. On a day like today, it is possible to believe that things may yet be well.",
            "Another sunrise. Another chance. The land is patient that way.",
            "The morning mist lies thick on the valleys. Out of it, anything might come — and often does.",
            "Sunrise finds your company on the road. There are worse places to greet the morning.",
            "The world wakes slowly. Your company does not have that luxury — but pause, just for a moment.",
            "First light on the river, on the road, on the distant mountains. A moment worth noticing.",
            "The horizon catches fire before the sun clears it. Another day begins whether you are ready or not.",
            "The cocks crow in a dozen villages at once. Even in wartime, the mornings come.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — DUSK
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] DuskMessages = {
            "The sun sets behind the mountains. Long shadows reach across the land.",
            "Dusk comes early here. Light your fires before the dark makes assumptions.",
            "Evening falls. Whatever was not done today waits for tomorrow — if tomorrow comes.",
            "The last light fades from the western sky. Somewhere behind those hills, warm hearths still burn.",
            "The sky turns the colour of embers. Beautiful in a way that battle never is.",
            "Dusk. The hour when plans are reviewed and regrets begin their nightly accounting.",
            "Stars appear one by one as the light fails. The night watch will have company.",
            "Long shadows lie across the road. The world is neither day nor night — a moment in between.",
            "Campfires dot the world at dusk — some yours, some not. In the dark, they look the same.",
            "Sunset behind the mountains paints the sky in impossible colours. War has not taken everything.",
            "The end of another day. Count those who began it with you and still stand at the end.",
            "The light fades from the peaks last, as if the mountains are reluctant to let the day go.",
            "A red sky at evening. The sailors' saying comes with different meaning in this country.",
            "The last birds take their final pass and settle. The night world begins to stir.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // TIME OF DAY — NIGHT
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] NightMessages = {
            "Deep night. The stars are clear and cold. The watch is doubled.",
            "Silence, broken only by the wind and the sound of something moving beyond the firelight.",
            "The night is old and the fire is low. Even courage needs to sleep sometimes.",
            "Stars wheel overhead. The darkness seems to know every path you do not.",
            "In the deep hours of the night, the world feels very wide and very empty.",
            "The shadows are at their deepest before the dawn. The watch knows this better than anyone.",
            "Something calls from the dark beyond the camp — animal, wind, or something else. The sentries do not answer.",
            "The land at night belongs to different things than it does by day. Ride carefully if you must ride.",
            "The fire crackles. Stories are told in hushed voices. The night is long.",
            "Even the land sleeps, in its way. But not everything that moves in the dark is sleeping.",
            "The moon is hidden tonight. Stars are older comfort — colder, but more reliable.",
            "Night on the campaign road is never quite still. There is always something to listen for.",
            "Deep in the night, the campaign continues in people's dreams — which are not always peaceful.",
            "The hour before dawn is the longest. The watch knows it well.",
            "Night lies heavy on the land. Somewhere in it, plans are being made against you. And by you.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — SPRING
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonSpring = {
            "Spring returns to the land. The world did not forget how to renew itself, despite everything.",
            "Green comes back to the valleys, to the riverbanks, to the foothills of the mountains.",
            "The armies stir with the spring. Campaigns begin again as the roads dry and the passes clear.",
            "New growth everywhere — even in places the war tried to claim. Life is stubborn.",
            "Spring rains and the smell of earth turning. Even soldiers notice it, sometimes.",
            "Birds return to forests that were silent all winter. Not all winters end — but this one did.",
            "The world opens up again. Passes that were closed to you are open, and so are the roads.",
            "Spring is the season of campaigns and births — life and war resuming together, as they always have.",
            "The flowers return to the fields. They did not ask who fought there.",
            "New life pushes through the grey earth. Even in the shadow of fortress walls, something green finds its way.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — SUMMER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonSummer = {
            "High summer. The heat presses down on soldiers and strategy alike.",
            "Long days — a campaigning army can cover more ground with more light to march by.",
            "The crops stand tall in the fields. If the raids do not reach them, there will be food this winter.",
            "Summer light on the great river — broad and bright. It looks peaceful from a distance.",
            "The heat of summer weighs on armoured soldiers. Morale and water both need careful tending.",
            "Full summer — the world at its most alive, and the armies at their most active.",
            "Dust rises from every road in the long dry heat. Armies leave clouds of it behind them.",
            "Midsummer and the war goes on. The seasons do not negotiate with armies.",
            "The land is green and full and beautiful. It is a good thing to remember what you are fighting for.",
            "The long summer days favour the army that marches early. Be that army.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — AUTUMN
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonAutumn = {
            "Autumn comes to the land. The hills turn gold and red — beautiful, and a little melancholy.",
            "The harvest is in, or it is not. The armies know which, and it shapes everything about the winter ahead.",
            "Leaves fall on the road to war. The trees do not care about strategy.",
            "Autumn mist on the river, on the valleys, on the faces of soldiers who have been at it too long.",
            "The year is dying gently. The campaign must resolve something before the roads close for winter.",
            "Smoke from burning villages and burning leaves — a season that always mingles fire and beauty.",
            "The passes will close soon. Plans made for autumn need to be executed before the snow does the planning instead.",
            "Golden and amber — the world makes its peace with dying beautifully every year.",
            "Autumn rains begin. The roads are going to get harder before they get easier.",
            "In the villages they would be preparing the harvest feast. Out here, there is only the march.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // SEASONS — WINTER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] SeasonWinter = {
            "Winter descends. The cold is not an enemy — but it is an obstacle.",
            "Snow on the passes, frost on the road, ice on the rivers. War slows but does not stop.",
            "The mountain passes are closed. Whatever was north of them must stay north for now.",
            "A cold wind blows from the north, from places where things older than memory sleep in ice.",
            "Winter is its own siege. Supply lines are the walls — let them break and the army breaks too.",
            "The dark and the cold together test a company's will. Keep the fires burning and the purpose clear.",
            "Short days and long nights. More time for the night watch to wonder about things they cannot see.",
            "Midwinter. Even the enemy slows down in the cold, though they slow down less than you would wish.",
            "The world is quieter under snow. Sound travels further. Keep the noise of the camp low.",
            "Winter campaigns are hard on men and horses. Move carefully, supply generously, do not rush.",
            "Even the mountains look more formidable with their white crowns restored. A reminder that some things are bigger than armies.",
            "Ice on the river. The great water slows but does not sleep — and neither does the war.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // TRAVEL
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] TravelOnRoute = {
            "The road stretches ahead. Each step is a commitment to the plan laid before the march began.",
            "Keeping to the road costs discipline. It also saves the kind of time that gets soldiers killed when wasted.",
            "The old roads were built to last. They have outlasted empires. They will outlast this war.",
            "A road followed with purpose is an anchor. The army that marches with direction arrives — the one that wanders often doesn't.",
            "The march is easier on old stone than on mud. The builders of these roads understood that.",
            "Your company follows the road — each mile brings the destination closer.",
            "The road ahead is clear. That is worth more than most soldiers think, until the roads are not clear.",
            "Travel by road is the backbone of campaigning. The generals who forget it pay in time and blood.",
            "A known route is a safe route — or at least a safer one. Stay on it.",
            "The miles pass beneath your company's boots. There is a rhythm to marching that soldiers learn to trust.",
            "These roads remember every army that marched them. Yours is one more line in a very long history.",
            "On the open road, the world seems larger than it does behind walls. Both views have their uses.",
            "The landscape passes in silence. Each rider lost in the same thought — how much further, and what waits there.",
            "A long road teaches patience. It also teaches that patience alone is not enough.",
            "Villages on either side of the road watch the company pass. Some with fear, some with hope, some with neither.",
            "The pace holds. A company that marches steadily arrives — a company that rushes often doesn't.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // MORALE — HIGH
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] MoraleHigh = {
            "The company is in fine spirits. High morale is a force multiplier — guard it carefully.",
            "Your soldiers march with their heads up. Whatever is ahead, it faces a confident company.",
            "Song carries through the camp tonight. Not the bravado of untested recruits — the quiet satisfaction of soldiers who have done hard things and survived them.",
            "High morale. The troops trust the mission and trust each other. That is worth more than numbers.",
            "Your company would follow you into the teeth of any fortress tonight. Try not to ask that of them.",
            "There is something rare and valuable in a unit whose morale is genuinely high — protect it from the things that erode it.",
            "Even the warhorse is stepping lively. Morale reaches the animals too.",
            "The company sings old songs on the march. It is a good sign — only soldiers who believe in tomorrow sing about today.",
            "High spirits in the ranks. Victories, good food, and a commander who does not waste lives — three things that build morale.",
            "The work is hard and the road is long, but the company is with you. That matters beyond measure.",
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
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — LOW FOOD WARNING
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] LowFood = {
            "The supply wagons are nearly empty. A hungry company marches slower and fights worse.",
            "Rations are running low. The soldiers have not complained yet — but they have noticed.",
            "Food is almost gone. Resupply or forage — the latter is slower and damages the villages you need on your side.",
            "The cook fires are smaller than they should be. An army marches on its stomach, and this one is going hungry.",
            "Three days of food, maybe less. This needs to be resolved before the soldiers start solving it themselves.",
            "The quartermaster's report is not good. Supplies are running out faster than the road is running out.",
            "Hunger is a slow siege. It does not announce itself until it is already doing damage.",
            "The men are counting their rations. When soldiers start counting, there is not enough.",
            "Empty saddlebags on the supply horses. Resupply the company before it becomes a problem that cannot be solved quickly.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — STARVING (food at zero)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] Starving = {
            "The company is out of food. Morale will break before the soldiers do — find supplies immediately.",
            "Nothing left in the wagons. Nothing left in the saddlebags. The march cannot continue like this.",
            "Starvation sets in. A starving army is a liability — it will raid, desert, or collapse. Resupply now.",
            "The fires are cold and the rations are gone. Whatever the plan was, it must include food or it will fail.",
            "An army with no food has no options. Find provisions before the company finds its own solutions.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — PARTY HEAVILY WOUNDED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PartyHeavilyWounded = {
            "More than a third of the company is wounded. They fight on — but they fight slower, weaker, and with less will.",
            "The injured are slowing the column. Find a surgeon, find a town, or accept that the next fight will be harder.",
            "Heavy casualties among the ranks. Wounds not tended become something worse. The company needs time to recover.",
            "The wounded outnumber the fit. Whatever campaign objective waits ahead, it waits for a reduced force.",
            "Bandages and field dressings only go so far. The company needs rest, supplies, and time — none of which are free.",
            "The men are hurting. A company that fights through pain fights well — until it doesn't. Watch for the threshold.",
            "Many hurt, fewer whole. The company can still march — but every engagement carries greater risk until the wounded recover.",
            "The toll of recent fighting is visible in every face. Rest would cost time. Pressing on may cost more.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — HERO LEVEL UP
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] HeroLevelUp = {
            "Experience is a hard teacher but a thorough one. You have grown in ways that will matter.",
            "The campaign sharpens everyone it does not break. You remain unbroken, and sharper.",
            "Something in the way you handle the sword — or the council table — is different now. Better.",
            "Every battle learned something, even the ones that were won too easily to notice. You are accumulating that knowledge.",
            "The great captains were not born great. They were made great, the same way you are making yourself.",
            "Another step on the long road of your capabilities. The road does not end here.",
            "What you were when you first took up the sword and what you are now are different things. That gap is called experience.",
            "The company notices. They always notice when a commander gets better at being a commander.",
            "Growth is its own reward — and its own preparation. You are being prepared for something.",
            "Years of campaigning have honed what raw talent once started. The difference shows.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — TOURNAMENT VICTORY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] TournamentVictory = {
            "The crowd roars. You fought well and the prize belongs to you — along with the renown that comes with it.",
            "Victory in the arena. Somewhere out there, someone will hear the name of the champion and wonder.",
            "You have earned the tournament prize and everything that follows it. Reputation is currency in Calradia.",
            "The last opponent is down. Skill, endurance, or fortune — the champion does not always know which carried them.",
            "A tournament win is more than a prize. It is a claim, staked in the sight of lords and commons alike.",
            "The banners of the tournament come down as the champion stands. Yours is the name they will carry home.",
            "Every noble who watched you fight today has updated their assessment of you. Some upward, some with concern.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — TOURNAMENT DEFEAT
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] TournamentDefeat = {
            "The arena falls silent for you. There will be other tournaments — and the one who beat you is not invincible.",
            "Eliminated. You fought well enough to learn something. Find out what it was.",
            "Not every contest ends in victory. The ones that don't are sometimes worth more than the ones that do.",
            "The prize goes to another. The experience goes to you. One of those has more lasting value.",
            "Knocked out of the tournament. There is no shame in it — only in not trying again.",
            "Someone fought better than you today. That is a fact, not a verdict on your character.",
            "You pushed through the rounds and fell short of the prize. The crowd will remember how you fought, not just the outcome.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — FUGITIVE HERO: ENEMY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyFugitive = {
            "A fugitive is still dangerous — perhaps more so. Those with nothing to lose have nothing to hold them back.",
            "The enemy has lost their standing, but not their grudge. A dispossessed commander can still cause harm.",
            "Outlawed and running. Expect them to reappear where they are least convenient.",
            "Some who fall from power simply disappear. Others make it their business to remind you they exist.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — FUGITIVE HERO: ALLY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] AllyFugitive = {
            "A companion is now an outlaw. Whether through misfortune or mistake, they need help — or distance.",
            "One of your own is now a fugitive. That is a problem that does not stay small if left alone.",
            "They are running. Loyalty is tested differently when the law itself becomes the threat.",
            "An ally stripped of their standing. Whether you stand with them will say something about the kind of leader you are.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — WEEKLY AMBIENT (broad world observations, low frequency)
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] WeeklyAmbient = {
            "The war grinds on. Maps are redrawn in blood, and the cartographers can barely keep pace.",
            "A week on the road. Calradia does not grow smaller — only more familiar, and more dangerous in its familiarity.",
            "Somewhere out there, plans are being made for you. Plans by people who have not yet decided if you are an obstacle or an asset.",
            "The world turns. Factions rise and fade and the people caught between them simply try to survive.",
            "Calradia has been at war for as long as anyone can remember. The question is not if it ends — it is who is still standing when it does.",
            "The roads are long and the distances between allies greater than they appear on any map.",
            "Rumours travel faster than armies. By the time you hear something, half of Calradia already has.",
            "The old order is breaking down. What fills the space it leaves will depend on who builds it first.",
            "Another week of marching and fighting and counting the days. Those who endure will inherit what those who don't were fighting over.",
            "The land has known a hundred wars. It will know a hundred more. The question is what you do in the middle of this one.",
            "Somewhere a fire has gone out that will not be relit. Somewhere else, a new one is starting.",
            "Power changes hands in Calradia the way water changes course — slowly, then all at once.",
            "The campaign is long and the goals are clear, but the distance between the two has a way of lengthening overnight.",
            "History is being made here. Not all of it will be recorded. Some of it will only be remembered by those who lived it.",
            "Every war has a moment when the ending becomes visible — not close, but visible. Pay attention when it arrives.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // CONTEXTUAL DYNAMIC — format string pools
        // ═══════════════════════════════════════════════════════════════════════

        public static readonly string[] BattleVictoryNearSettlement = {
            "A victory near {0}. The people behind those walls will breathe easier tonight.",
            "The enemy is driven back from the approaches of {0}. The settlement is safer — for now.",
            "The battle near {0} is won. The threat recedes from this part of the road.",
            "Your victory echoes to the walls of {0}. Let the garrison know — morale is a resource.",
            "The field near {0} belongs to you. What you do with it next will define the next chapter.",
        };

        public static readonly string[] BattleDefeatNearSettlement = {
            "The loss near {0} will be felt within its walls. The people will have seen what happened.",
            "Defeat near {0} — the settlement is now more exposed than it was this morning.",
            "The battle near {0} is lost. The enemy presses closer. Reinforce what you still hold.",
            "Your retreat from the approaches of {0} changes the strategic picture significantly.",
            "The enemy grows nearer to {0} today. Do not let them grow nearer still.",
        };

        public static readonly string[] HeroDeathNamed = {
            "{0} has fallen. The company is poorer for it, and the cause feels the loss.",
            "The death of {0} will be felt for longer than this campaign.",
            "{0} will not see the end of this road. That is the truth, and it should be said plainly.",
            "A name worth remembering: {0}. See that it is.",
            "{0} is gone. That gap in the company will not be filled easily.",
        };

        public static readonly string[] WarDeclaredNamed = {
            "{0} and {1} are at war. The roads between them have become battle lines.",
            "War between {0} and {1}. Whatever grievance started it, the soldiers will pay the price.",
            "The conflict between {0} and {1} is now official. It was already real.",
            "{0} raises the banner against {1}. Two forces that might have stood together, now stand apart.",
            "Declaration of war: {0} versus {1}. The diplomats have finished their work. Now the soldiers begin theirs.",
        };

        public static readonly string[] PeaceMadeNamed = {
            "{0} and {1} have made peace. The roads between them open again.",
            "A treaty between {0} and {1}. May it hold longer than the last one.",
            "The war between {0} and {1} is over. Count the cost when the smoke clears.",
            "{0} and {1} sheathe their swords. Whether by exhaustion or by wisdom is not always clear.",
            "Peace is declared between {0} and {1}. The soldiers on both sides are grateful, whatever their lords say.",
        };

        public static readonly string[] SiegeStartedNamed = {
            "A host besieges {0}. The walls will be tested before this is over.",
            "The siege of {0} begins. Let the garrison hold until relief can arrive.",
            "{0} is surrounded. The clock begins counting down from this moment.",
            "Siege engines before {0}. The enemy means to take it by patience if not by storm.",
            "The walls of {0} face the patience of a besieging army. Time is now a weapon.",
        };

        public static readonly string[] SettlementCapturedNamed = {
            "{0} is yours. The gates are open and the banners are changed.",
            "The walls of {0} are taken. Now comes the harder work of holding them.",
            "Your standard flies above {0}. The siege is over — the responsibility is not.",
            "{0} has fallen to your forces. Tend the garrison and secure the walls.",
            "The keys of {0} change hands. What was defended yesterday is yours to defend today.",
        };

        public static readonly string[] SettlementTakenNamed = {
            "{0} has fallen. The banners are changed. The strategic map is redrawn.",
            "The gates of {0} are open — to the wrong side. A serious loss.",
            "{0} is taken. Every hand it passes through costs blood to reclaim.",
            "The fall of {0} reverberates through the region. Its defenders did what they could.",
            "A new flag flies over {0}. What flies next depends on you.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — TOURNAMENT format strings
        // ═══════════════════════════════════════════════════════════════════════

        public static readonly string[] TournamentVictoryNamed = {
            "The tournament at {0} is yours. The crowd will carry the story home.",
            "You stand victorious in the arena at {0}. The prize and the reputation are both yours.",
            "Champion of {0}. The title means something — in the halls of nobles and in the stories of common soldiers.",
            "The tourney at {0} ends with your banner raised highest. Let the news travel.",
            "{0} will remember this tournament. So will those who watched you win it.",
        };

        public static readonly string[] TournamentDefeatNamed = {
            "The tournament at {0} ends without the prize. {1} fought better on the day.",
            "Eliminated at {0}. {1} takes the prize this time.",
            "The arena at {0} has a different champion today. {1} earns what you came for.",
            "You fought through the rounds at {0} and fell short. {1} was the better fighter today.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // NEW — FUGITIVE format strings
        // ═══════════════════════════════════════════════════════════════════════

        public static readonly string[] EnemyFugitiveNamed = {
            "{0} is now a fugitive. Their standing is gone — but they are not.",
            "The outlaw {0} is on the run. Dispossessed, not destroyed.",
            "{0} loses everything except the capacity for trouble. Watch for them.",
            "Word reaches you: {0} has been declared a fugitive. The law has caught up — but not the person.",
        };

        public static readonly string[] AllyFugitiveNamed = {
            "{0} is declared a fugitive. This is a problem that will not solve itself.",
            "One of your own — {0} — is now an outlaw. Whether through their doing or another's, the consequence is the same.",
            "{0} is on the run. The company owes them a decision: stand with them or distance from them.",
            "The name of {0} is now spoken in the same breath as outlaw. That changes things.",
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
            "The forces are combined. There is strength in numbers — and vulnerability in the supply lines that numbers demand.",
            "A great company answers your call. The road ahead will know the weight of them.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // ARMY — ENEMY MOBILISED
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyArmyMobilised = {
            "A hostile force assembles on the horizon. Their drums carry further than their banners can be seen.",
            "Enemy forces are massing. Wherever they march, the land will feel it before the people do.",
            "A great army gathers under hostile banners. Riders should be sent ahead to warn what lies in its path.",
            "The scouts report a host in formation — more than a raiding party, less than a world-ending tide. Not reassuring either way.",
            "An enemy army takes the field. The question is not whether they are coming — it is when, and where.",
            "Dark banners gather. Many swords and one purpose: to take what is yours.",
            "Their forces consolidate. Scattered raids and skirmishes are replaced by something more organised. Worse, in its way.",
            "The enemy musters. Intelligence is now more valuable than bravery — find out where they are going.",
            "A considerable force answers the enemy's call. Prepare accordingly — or prepare to yield ground.",
            "The drums of a great army carry on the wind. They are not friendly drums.",
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
        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO PRISONER: PLAYER
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] PlayerCaptured = {
            "You are taken. The chains are real, and the road ahead leads somewhere you did not plan to go.",
            "Captured. Whatever comes next, the campaign cannot continue from a cell. Find a way out.",
            "The battle is lost and you with it. Now begins a different kind of fight — to survive captivity.",
            "A prisoner now. The enemy will decide what you are worth — make sure the answer is your freedom.",
            "Even the greatest commanders have known captivity. Endure, and find the way back.",
        };

        public static readonly string[] PlayerReleased = {
            "Free again. The road back to the campaign begins with the first step out of this place.",
            "The cell opens and the world opens with it. Take a moment — then get back to work.",
            "Released. Whatever was agreed to secure your freedom, honour it. Then make sure it was worth the cost.",
            "Back in the field. The time in captivity was not wasted if it was spent planning the return.",
            "Freedom is a better feeling when it has been taken away. Do not let it happen again easily.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO PRISONER: ENEMY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] EnemyHeroCaptured = {
            "A prisoner of war is a lever. Use them wisely — ransom, exchange, or information.",
            "The enemy loses a commander. A prisoner in your hands has more strategic value than a dead one.",
            "Taken alive. Their worth to you depends on who wants them back, and how badly.",
            "An enemy captain in chains. What they know may be as valuable as any ransom.",
            "The living prisoner is an opportunity the dead enemy never is. Handle this one carefully.",
        };

        public static readonly string[] EnemyCapturedNamed = {
            "{0} is your prisoner. The enemy will notice the absence.",
            "You have taken {0}. Their side will want them back — at a price.",
            "{0} falls into your hands alive. That is worth more than their death would have been.",
            "The capture of {0} will be felt in their command structure. Use the advantage.",
            "{0} is in chains. What comes next is a negotiation.",
        };

        public static readonly string[] EnemyHeroReleased = {
            "The enemy's prisoner walks free. Whether wisely or not, what is done is done.",
            "A released enemy returns to their cause with knowledge of your methods. That is the cost of mercy.",
            "They go back to the people who sent them. Whatever freed them had better be worth it.",
        };

        public static readonly string[] EnemyReleasedNamed = {
            "{0} is released. Ensure you are prepared for {0} to return to the field.",
            "The enemy regains {0}. Whether by ransom, exchange, or choice — the consequence is the same.",
            "{0} walks free. The enemy is stronger for it by exactly one.",
        };

        // ═══════════════════════════════════════════════════════════════════════
        // HERO PRISONER: ALLY
        // ═══════════════════════════════════════════════════════════════════════
        public static readonly string[] AllyHeroCaptured = {
            "A companion is taken. Their safety is now the enemy's to grant or deny.",
            "One of your own is in enemy hands. The price of their return will not be cheap.",
            "A trusted ally is captured. Find out where they are held and what it will cost to bring them back.",
            "The enemy has gained a prisoner worth more to your cause than their own. Act accordingly.",
            "An ally in captivity. The campaign grows harder without them — and the ransom will make it harder still.",
        };

        public static readonly string[] AllyCapturedNamed = {
            "{0} has been taken prisoner. Find out what it will take to get them back.",
            "The enemy holds {0} now. That changes things.",
            "{0} is a prisoner of war. Their fate depends on how quickly you act.",
            "They took {0}. The loss is felt in the company and on the field.",
            "{0} is in captivity. Do not leave them there long.",
        };

        public static readonly string[] AllyHeroReleased = {
            "A companion returns. The captivity ends and the campaign resumes — one piece of good news in a long war.",
            "Back in the field and back in the fight. The time lost to captivity will not be recovered, but the person is.",
            "The return of a trusted ally is worth marking. Not all who are taken come back.",
        };

        public static readonly string[] AllyReleasedNamed = {
            "{0} is free. Whatever secured the release, it was worth it.",
            "The gates open and {0} walks out. The company is fuller for it.",
            "{0} returns from captivity. The absence made the gap visible — their return fills it.",
            "{0} is released. Mark the occasion — not all stories of captivity end this way.",
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
            "Not every kingdom that falls stays fallen. History is full of things that endured after they should have ended.",
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
            "{0} is destroyed. Another name removed from the map of Calradia.",
            "The kingdom of {0} is no more. Its people will remember, even when the kingdom itself is forgotten.",
            "{0} ends. The war consumes what it touches, and now it has touched {0}.",
        };

        public static readonly string[] KingdomDestroyedNeutralFlavor = {
            "Every kingdom that falls makes the survivors more desperate — on all sides.",
            "The destruction of states is the background music of this age. It does not make it easier to hear.",
            "One fewer banner in the world. The balance of power shifts where it falls.",
            "History does not pause for fallen kingdoms. Neither does the campaign.",
        };
    }
}
