# Player Speed Control 0.1.0

For this workspace's Bannerlord 1.4.8 installation, using MCM v5 and Harmony from
TAOM.Dependencies. Enable Player Speed Control after TAOM.Dependencies and Sandbox.
Open Mod Options -> Player Speed Control. Running and campaign map travel have
independent enable switches and 0.5x-3x multipliers; both default to normal 1x.
Use MCM's default/reset action to restore the defaults. Settings persist globally.

Running applies only to the player on foot on land while not in walking mode.
Walking, riding, swimming and NPC movement are excluded. Map travel affects only
the main party's land movement, with a named speed-tooltip adjustment. Attached
army followers continue to follow their leader. Sea travel is excluded.

Settings refresh within 0.2 seconds plus the next mission tick/normal campaign speed
refresh. Scripted speed limits may still constrain running. Models from other mods
that replace the default party speed calculation may bypass the travel adjustment.

Build and validate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./Build-Package.ps1
```

Output: `Release/PlayerSpeedControl-0.1.0.zip`; extract the PlayerSpeedControl folder
under Modules. The package contains only this owner's DLL, manifest and README.
It does not bundle or update dependencies. No campaign save data is added.

Offline tests are not native gameplay proof. See DESIGN.md and VALIDATION.json
for exact evidence and remaining user-started gameplay checks. The build script
does not start the game or deploy files to the game installation.
