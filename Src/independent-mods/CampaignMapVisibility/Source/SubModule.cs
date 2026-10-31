using System;
using System.IO;
using System.Security.Cryptography;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CampaignMapVisibility
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "CampaignMapVisibility.1.4.8";
        internal const string SupportedViewModelHash = "8C4B2CA448A3BC8789A6F8D7B4427E7A9832A31C45DD79E9034173535EBAEBF4";
        private string _startupError;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony(HarmonyId);
            try
            {
                using (var stream = File.OpenRead(typeof(PartyNameplateVM).Assembly.Location))
                using (var sha = SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                    if (!string.Equals(hash, SupportedViewModelHash, StringComparison.Ordinal))
                        throw new NotSupportedException("This game build has not been verified. Expected the supported Bannerlord 1.4.8 map UI.");
                }
                PartyNameplateSession.ValidateBindings();
                harmony.PatchAll(typeof(SubModule).Assembly);
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(HarmonyId);
                _startupError = "Campaign Map Visibility is disabled: " + ex.Message;
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_startupError != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(_startupError));
                _startupError = null;
            }
        }
    }
}
