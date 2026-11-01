using System;
using System.Linq.Expressions;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    // Signatures preserve the old evaluation stack; factories replace old newobj
    // instructions with calls. All metadata and settings delegates bind at startup.
    public static class LegacyApi
    {
        private static readonly Func<BasicCharacterObject, MBReadOnlyList<Equipment>> EquipmentGetter =
            (Func<BasicCharacterObject, MBReadOnlyList<Equipment>>)Delegate.CreateDelegate(
                typeof(Func<BasicCharacterObject, MBReadOnlyList<Equipment>>),
                typeof(BasicCharacterObject).GetProperty("AllEquipments", BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(true));
        private static readonly Action<CharacterObject, CultureObject> CultureSetter =
            (Action<CharacterObject, CultureObject>)Delegate.CreateDelegate(
                typeof(Action<CharacterObject, CultureObject>),
                typeof(CharacterObject).GetProperty("Culture", BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.DeclaredOnly).GetSetMethod(true));
        private static Func<object> settings;
        private static Func<object, bool> settlement, kingdom, editor, recruitment;

        public static void BindSettings(Type original)
        {
            PropertyInfo instance = original.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            settings = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Property(null, instance), typeof(object))).Compile();
            settlement = Getter(original, "SpawnAtPlayerSettlement");
            kingdom = Getter(original, "SpawnAtPlayerKingdom");
            editor = Getter(original, "FullUnitEditor");
            recruitment = Getter(original, "ClanRecruitCustomTroop");
        }

        private static Func<object, bool> Getter(Type type, string name)
        {
            ParameterExpression obj = Expression.Parameter(typeof(object));
            return Expression.Lambda<Func<object, bool>>(
                Expression.Property(Expression.Convert(obj, type), name), obj).Compile();
        }

        public static object SettingsInstance() => settings();
        public static bool SpawnAtPlayerSettlement(object obj) => settlement(obj);
        public static bool SpawnAtPlayerKingdom(object obj) => kingdom(obj);
        public static bool FullUnitEditor(object obj) => editor(obj);
        public static bool ClanRecruitCustomTroop(object obj) => recruitment(obj);
        public static MBReadOnlyList<Equipment> AllEquipments(BasicCharacterObject obj) => EquipmentGetter(obj);
        public static void SetCulture(CharacterObject obj, CultureObject culture)
        {
            CultureSetter(obj, culture);
            CultureCompatibility.Synchronize(obj);
        }
        public static bool HiddenInEncyclopedia(CharacterObject obj) => obj.HiddenInEncyclopedia;
        public static void FillCharacter(CharacterViewModel vm, BasicCharacterObject character, int equipment)
        {
            if (character is CharacterObject troop) CultureCompatibility.Synchronize(troop);
            vm.FillFrom(character, equipment);
        }
        public static GauntletLayer Layer(int order, string name, bool clear) => new GauntletLayer(name, order, clear);
        public static ImageIdentifierVM ItemVM(ItemObject item, string banner) => new ItemImageIdentifierVM(item, banner);
        public static ImageIdentifierVM CharacterVM(CharacterCode code) => new CharacterImageIdentifierVM(code);
        public static ImageIdentifierVM EmptyVM(int kind) => new GenericImageIdentifierVM(new EmptyImageIdentifier());
        public static ImageIdentifier Item(ItemObject item, string banner) => new ItemImageIdentifier(item, banner);
        public static ImageIdentifier Character(CharacterCode code) => new CharacterImageIdentifier(code);
        public static ImageIdentifier Banner(Banner banner) => new BannerImageIdentifier(banner);
        public static ImageIdentifier Empty(int kind) => new EmptyImageIdentifier();
        public static InquiryElement Inquiry(object id, string name, ImageIdentifier image) => new InquiryElement(id, name, image);
        public static InquiryElement EnabledInquiry(object id, string name, ImageIdentifier image, bool enabled, string hint) => new InquiryElement(id, name, image, enabled, hint);
        public static ItemMenuTooltipPropertyVM Tooltip(string definition, string value, int height, bool extended, HintViewModel hint) =>
            new ItemMenuTooltipPropertyVM(definition, value, height, extended, hint);
        public static ItemMenuTooltipPropertyVM ColoredTooltip(string definition, string value, int height, Color color, bool extended,
            HintViewModel hint, TooltipProperty.TooltipPropertyFlags flags) =>
            new ItemMenuTooltipPropertyVM(definition, value, height, color, extended, hint, flags);
    }
}
