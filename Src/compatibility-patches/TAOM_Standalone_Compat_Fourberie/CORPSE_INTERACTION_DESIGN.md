# Fourberie corpse skeleton correction

Scope: this independent compatibility owner only. No external mod, native
assembly, save/config contract, module ID or dependency changes. Not deployed.

## Research authority

ILSpy 10.1.0.8386; installed Native v1.4.8, Fourberie DLL 1.4.8.1.
Read-only inspection outputs are local under Artifacts/Fourberie-race-investigation.

- SandBox.dll SHA256 587C6E94C425745C1478F632F3B66226FF8298666CB27E8E6D9A62488DF5956A:
  CorpseDraggingMissionLogic.OnAgentInteraction obtains HumanBone indices and
  parent hierarchy from userAgent.AgentVisuals. The selected bone came from
  a corpse limb raycast and is later used on that corpse by SetDraggedCorpse
  and OnFixedMissionTick. Different skeleton layouts invalidate that assumption.
- Fourberie.dll SHA256 2240105A9AD42413DFD750BE519D386CBB55F841BD2069A3136CCB9F56412353:
  FStealthMissionLogic.GetFocusableObjectInteractionInfoTexts sets its loot
  target only for an interactable inactive agent. Disguise additionally checks
  input, distance, suspicion and alarms. No race check was found in that path.
- Agent.CanInteractWithAgent requires an offered mission action, reachability
  and ability to use a machine. Native corpse logic offers an action for killed
  or unconscious agents when the player is not already dragging.

## Implemented policy

Substitute the corpse as skeleton source only for humanoid pairs in a mission
containing the exact Fourberie.FStealthMissionLogic type. Preserve native
eligibility, bone filtering, physics and state transitions. Preserve native
mapping for other missions and non-humanoid pairs. Null inputs fall back.
The transpiler requires exactly one original carrier-visual lookup and rejects
missing, ambiguous or branch/exception-entry lookups rather than guessing.

One mission-behavior traversal occurs per grab event; no new tick work,
reflection lookup, IO, logging, LINQ or global scans occur during interaction.
This is a correction of a demonstrated mapping defect, not proof of the cause
of the reported missing prompt or disguise failure. Native raycasts, custom
skeletons and other installed patches remain unverified runtime dependencies.

## Validation and next stop

Release/x64 build passed against installed libraries. Test-CorpseSkeletonPatch.ps1
passes 17 checks against the actual installed method: exact target, IL insertion,
argument order, preservation of original instructions/labels/exception blocks,
missing and ambiguous layout rejection, null fallback and static hot-path audit.
These are managed/static checks, not Harmony detour or native gameplay proof.
The older Tests/native-adapters/Fourberie_CompatPatch regression script stops on
its Native 1.4.7 requirement; current Native is 1.4.8. That regression lane remains
unqualified and was not changed as part of this focused correction.

Next gate requires separately authorized owner-only deployment, then a user-started
game. As an elf: drag killed/unconscious orc, goblin and human guards; release and
re-grab; note whether any failure is before the prompt or after grab. With guards
calm, loot and confirm disguise, prison access/rescue and loyalty activity. Verify
alarm/suspicion rejection and unchanged ordinary mission/non-humanoid behavior.
All native cases are NOT_RUN. No game was started and no live files were changed.
