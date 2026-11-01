using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TAOM_CheatPanel.Settings
{
    public sealed partial class CheatPanelSettings
    {
        private static CheatPanelSettings _instance;
        public static CheatPanelSettings Instance
        {
            get { return _instance ?? (_instance = Load()); }
            internal set { _instance = value; }
        }

        public static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "TAOM_CheatPanel", "settings.xml");

        private static CheatPanelSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 };
                    using (var reader = XmlReader.Create(SettingsPath, options))
                        return (CheatPanelSettings)new XmlSerializer(typeof(CheatPanelSettings)).Deserialize(reader);
                }
            }
            catch (Exception error) { Runtime.Log.ErrorOnce("settings.load", error); }
            return new CheatPanelSettings();
        }

        public static bool Save()
        {
            try
            {
                var path = SettingsPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var temporary = path + ".tmp";
                using (var writer = new StreamWriter(temporary, false))
                    new XmlSerializer(typeof(CheatPanelSettings)).Serialize(writer, Instance);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception error)
            {
                Runtime.Log.ErrorOnce("settings.save", error);
                return false;
            }
        }
    }
}
