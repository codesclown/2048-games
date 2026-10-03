# Expedition E1 — Rules and session lifecycle

E1 provides the rules/service foundation. Mission gameplay screens and campaign entry buttons ship in E4; the polished Home mode selector ships in E6. No incomplete campaign is exposed in the current Classic UI.

## Ownership

- `IGameSession`: mode, dimension, numbered cells and score, independent of Unity.
- `BoardMovement`: one movement/merge implementation used by Classic `Board` and `ExpeditionSession`. Processes directional lines as segments separated by terrain. Emits original tile motions, merge destinations and score gain for presentation.
- `GameController`: existing gesture owner; no second input controller.
- `GameNavigation`: mode route and animation, selection, pause and background gates. Classic uses `GameScreen.Game`; Expedition reserves `GameScreen.Expedition` for its E4 presenter. The Classic presenter ignores Expedition change events.
- `GameManager`: owns both sessions; `ActiveSession` selects the interface for the active mode. `CanResumeClassic` and `CanResumeExpedition` are independent resume entries. `Resume(GameMode)` selects the route. Starting a mission does not replace `Session`, alter Classic stock or count a Classic game played.

## Lifecycle

Classic: start → slide → random spawn → save → animation → target/result → resume or next level. Its existing 2048/4096/8192 target validation, tools, score records and continuation remain unchanged.

Expedition foundation: validate and copy an authored mission → start its own session → snapshot → shared slide → scripted spawn → save → animation gate → Home/resume. Definition copies include stable ID and content version; changes to caller-owned arrays cannot change an active mission. Failed slides consume neither actions nor spawn steps. Each successful slide increments actions; E3 will enforce the authored action limit, evaluate mission goals, finalize results and add mission tools.

The snapshot carries cells, terrain, score, spawn cursor and latched goal IDs. Action/tool spending belongs to the live session, so E3 undo must not refund it. History is bounded at 15 snapshots. Typed turn events reserve damage/open/objective/result events for E2/E3; E1 emits slide, merge and spawn events only. A mission never uses Classic `won` or `Board.target`.

## Deterministic spawning

Spawn step N uses its authored value and first empty, unlocked priority cell. If priorities are occupied, use ascending row-major cell order. After the script ends, spawn 2 using the same fallback order. No random generator, clock or animation frame is involved. Only an actual spawn advances the cursor. Newly opened-cell exclusions and lock damage are E2 work.

## Save isolation

Classic keeps the existing checksum-protected two-slot `.save.v2.*` payload unchanged. Expedition uses independent checksum-protected two-slot `.expedition.save.v1.*` native files. Store keys include the same application/test prefix. Both reload separately; corruption falls back to the preceding committed record. Future versions are read-only. Failure in one namespace does not stop saving the other. E3 must explicitly migrate these records if it introduces a unified v3 transaction; this foundation does not silently change the Classic schema.

`ExpeditionSave` contains its session and campaign records. Campaign records/objectives are data contracts in E1; star calculation, campaign unlocks and content are later phases. Classic clear-scores does not delete Expedition progress.

## Validation

Mission dimensions 4–6, power-of-two cells, terrain durability 0–2, no tile inside a lock, valid spawn coordinates/values, positive action limit, tool array length three, bounded/noncyclic goal trees and nonempty stable ID. Runtime and snapshot grids validate against the copied mission. Snapshot cursors and scores cannot exceed live state.

Run plain rules:

```sh
mcs -out:/tmp/premium-expedition.exe Assets/Scripts/Board.cs Assets/Scripts/BoardMovement.cs Assets/Scripts/IGameSession.cs Assets/Scripts/ExpeditionSession.cs Tests/ExpeditionRulesTests.cs
mono /tmp/premium-expedition.exe
```

Native `-premiumSmoke` runs E1 service checks before the existing Classic UI suites, with separate memory storage and a temporary native-file directory. No APK is required.
