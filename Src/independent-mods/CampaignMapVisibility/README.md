# Campaign Map Visibility 0.1.0

Keeps settlement names, moving party banners/counts, and battle markers visible
when the campaign camera is zoomed out. Normal scouting is the default.

In **Mod Options -> Campaign Map Visibility**:

- **Keep map markers visible when zoomed out**: on by default. Also hides the
  floating settlement scouting-eye icons; scouting itself is unchanged.
- **Reveal entire map**: off by default. Displays moving party labels and battle
  markers outside scouting range; switching it off restores normal scouting.

The reveal option does not discover hidden hideouts/retreats, enlarge 3D models,
display individual battle-scene soldiers, or change AI/scouting/save data.
Native army grouping and labels for parties inside settlements remain unchanged.

## Build and install

Requires the verified Bannerlord **1.4.8** map UI assembly and **TAOM.Dependencies**,
which supplies Harmony and MCM (verified with its MCM 5.12.1).
An unverified map UI build disables the mod with a startup message.
Load this module after TAOM.Dependencies, Native, SandBoxCore and Sandbox.
The separate Bannerlord.Harmony/MBOptionScreen alias modules are not required.

Run `powershell -ExecutionPolicy Bypass -File Build-Package.ps1` from this folder.
The script builds Release, runs owner checks and creates
`Release/CampaignMapVisibility-0.1.0.zip`, containing only this module's DLL and
manifest. Extract its `CampaignMapVisibility` folder into the game's `Modules`
folder after ensuring TAOM.Dependencies is installed and enabled, then enable it in
the launcher. This package does not include or overwrite those dependencies.

No game startup is automated. Only the user starts/restarts Bannerlord.

## Validation

The offline checks validate policy boundaries/defaults and install/remove every
Harmony patch against the supported native assembly in an isolated test process.
They do not run a campaign, render labels, exercise native lifecycle/save/load,
or establish frame-rate performance. Follow the acceptance checklist in
`DESIGN.md` in a user-started campaign before treating this as runtime-qualified.
Full-map reveal can create dense labels and increase rendering cost.
