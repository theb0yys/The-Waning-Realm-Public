// Minimal native boundary doubles. Tests execute production behavior; these
// deliberately do not claim Bannerlord event delivery or save/load proof.
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.Core { public sealed class SkillObject { } }
namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { }
    public class CampaignGameStarter { }
    public abstract class CampaignBehaviorBase
    {
        public abstract void RegisterEvents();
        public abstract void SyncData(IDataStore store);
    }
    public sealed class TestEvent<T>
    {
        public Action<T> Handler;
        public void AddNonSerializedListener(object owner, Action<T> handler) { Handler += handler; }
    }
    public sealed class TestEvent
    {
        public Action Handler;
        public void AddNonSerializedListener(object owner, Action handler) { Handler += handler; }
    }
    public sealed class TestEvent<T,U>
    {
        public Action<T,U> Handler;
        public void AddNonSerializedListener(object owner, Action<T,U> handler) { Handler += handler; }
    }
    public sealed class TestEvent<T,U,V,W>
    {
        public Action<T,U,V,W> Handler;
        public void AddNonSerializedListener(object owner, Action<T,U,V,W> handler) { Handler += handler; }
    }
    public static class CampaignEvents
    {
        public static TestEvent<CampaignGameStarter> OnSessionLaunchedEvent = new TestEvent<CampaignGameStarter>();
        public static TestEvent HourlyTickEvent = new TestEvent();
        public static TestEvent<Hero,SkillObject,int,bool> HeroGainedSkill = new TestEvent<Hero,SkillObject,int,bool>();
        public static TestEvent<Hero,PerkObject> PerkOpenedEvent = new TestEvent<Hero,PerkObject>();
        public static TestEvent<Hero,bool> HeroLevelledUp = new TestEvent<Hero,bool>();
    }
    public sealed class Hero
    {
        public static Hero MainHero;
        public readonly Dictionary<SkillObject,int> Skills = new Dictionary<SkillObject,int>();
        public readonly HashSet<PerkObject> Perks = new HashSet<PerkObject>();
        public HeroDeveloper HeroDeveloper;
        public int Reads;
        public Hero() { HeroDeveloper = new HeroDeveloper(this); }
        public int GetSkillValue(SkillObject skill) { Reads++; return Skills.TryGetValue(skill, out int value) ? value : 0; }
        public bool GetPerkValue(PerkObject perk) { return Perks.Contains(perk); }
    }
}
namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
    public sealed class PerkObject
    {
        public static readonly List<PerkObject> All = new List<PerkObject>();
        public SkillObject Skill;
        public float RequiredSkillValue;
        public PerkObject AlternativePerk;
    }
    public sealed class HeroDeveloper
    {
        private readonly Hero _hero;
        public int UnspentAttributePoints;
        public int UnspentFocusPoints;
        public int Writes;
        public HeroDeveloper(Hero hero) { _hero = hero; }
        public void AddPerk(PerkObject perk)
        {
            Writes++;
            if (_hero.Perks.Add(perk)) CampaignEvents.PerkOpenedEvent.Handler?.Invoke(_hero, perk);
        }
    }
}
