# No Fall Damage 0.1.0

Eliminates standard fall damage in Bannerlord 1.4.8 singleplayer for **the player, NPCs, mounts and riders**, regardless of fall height. Active whenever this module is enabled; no configuration required.

Gravity, jumping, landing animations and other damage types are unchanged. This is a separate mod and can be used with Player Jump Height. Multiplayer and replay playback are excluded.

Requires **TAOM.Dependencies** for Harmony. Extract the ZIP's NoFallDamage folder into Modules and enable **No Fall Damage** after its dependencies in the launcher. Start the game yourself. No dependency DLLs are included.

The mod intercepts the game's dedicated fall-damage callback before a fall blow can be registered. Direct health changes or scripted kills implemented independently by another mod are outside that callback.

Offline tests exercise the patched installed callback and package checks. **Native gameplay is not yet verified.** Check player/NPC/horse/rider falls and ordinary weapon damage in a user-started game before relying on it in a campaign.

No campaign save objects or settings are added. Disable/remove the module and restart to restore normal fall damage. Build and test using `powershell -ExecutionPolicy Bypass -File ./Build-Package.ps1`.
