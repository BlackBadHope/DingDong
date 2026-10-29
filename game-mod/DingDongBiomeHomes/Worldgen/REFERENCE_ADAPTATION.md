# Native x10 reference adaptation (2026-09-08, r323)

Scope: keep Terraria + the installed providers as terrain owners. No Remnants or
TitanWorlds mod was enabled, no assets or implementation bodies were copied.
The worktree remains main. This is not a new x10 generation acceptance receipt.

## References inspected

- Mundos Titanicos / TitanWorlds, downloaded reference `TitanWorlds.dll`, SHA256
  `A8CFCA0E33C2E63D0F49E54DBF47515BBC8E07A8693A2A68CD27AFD492B2BC73`.
  Decompiled `TitanRemnantsCompatibility` and `TitanMajorModCompatibilitySystem`:
  final-consumer buffer release, verified pass anchors, atomic movement of a
  dependent pass chain, and pressure-gated collection. `TitanWorldPerformanceSystem`
  checks actual heap pressure before blocking collection.
  Reference listing: https://steamcommunity.com/sharedfiles/filedetails/?id=3792079909
- Remnants source commit `9c2cbf9cd2edcd8ae18a297357c4bcdc2870a389`,
  https://github.com/lazy-wombat/Remnants/blob/9c2cbf9cd2edcd8ae18a297357c4bcdc2870a389/Content/World/Structures.cs
  `Microdungeons`: biome, ground and entrance checks before reservation, room
  variants drawn from a shrinking list. `GemCaves`: count based on usable cavern
  area; `SkyGardens`: separate horizontal and vertical scale, original local pieces.
  The inspected tree did not expose a license file. Use ideas, not source/assets.
- The author's compatibility listing explicitly marks SOTS incompatible:
  https://steamcommunity.com/sharedfiles/filedetails/?id=3696148536

## Implemented in our code

1. **Temporary owned data.** `DingDongAuthoredStructureLoader.Lifetime.cs` releases
   decoded asset and integrated write-plan cache references when the last scoped
   consumer finishes. The whole residence transaction owns one scope, including
   planning, placement and integrity checks. Return/failure also releases it.
   Diagnostic consumers are scoped; PostWorldGen/world unload/mod unload provide
   idle cleanup. Eviction never zeroes arrays: retained masks remain valid and
   later requests reload the same resource. Logs report distinct cache payload
   bytes, not actual reclaimed RAM. No forced GC or global GC mode change.

2. **Dependency boundary.** After every provider has finished ModifyWorldGenTasks,
   locate our exact scheduled residence pass by object identity, validate a unique
   name and Final Cleanup anchor, then put only that pass last. This allows the
   planner to see late foreign structures before committing homes. Foreign pass
   objects, enabled state, weights and relative order remain unchanged. Unknown
   or duplicate anchors stop before mutation. This boundary covers GenPass, not
   arbitrary providers' PostWorldGen hooks; pyramid integrity remains checked
   separately. The native x10 receipt is armed only after this coordination.

3. **Finite placement search.** `SurfaceCandidateSampling` retains local scene
   size and all biome/relief/reservation/entrance constraints. It fixes the old
   `(seed + attempt * 37) % count` cycle, which missed most columns for counts
   divisible by 37. A coprime stride visits each column once. Coarse salted scans
   now include very narrow ranges; if the admissible domain is empty, a bounded
   refinement visits omitted columns. StructureMap admission, not a rejected
   high-scoring candidate, determines whether refinement is needed. No foreign
   reservation is removed to fit a home. This adopts finite non-repeating
   candidate selection, not Remnants' whole-world generator or unbounded loops.

## Existing density policy retained

NativePoiDensityPolicy extends every admitted width-scaled repeatable
underground target by height/2400 (x3 at height 7200), but does not multiply
WorldArea/None overrides again. The fail-closed target manifest covers jungle
huts, sky lakes, marble, granite, cave/desert houses, cave/Underworld chests,
trapped chests, sword shrines, thin ice, campsites, explosive traps, living
trees and both minecart-track families. Surface/sky counts use width. Unique
progression structures and the 61 distinct NPC residence assignments are not
duplicated ten times. Provider-local biomes remain covered by their exact
adapters; this native receipt is not proof that an unknown provider's private
POI loop scales correctly.

## Deliberately not transplanted

- Remnants terrain/biome replacement, assets and room generator: SOTS conflict.
- TitanWorlds fixed 16800x4800 assumptions, fixed Abyss protection width, foreign
  private arrays or flat tile-index loops: our 28000x7200 padded tile map and
  expanded coast require separate proofs of geometry and future consumers.
- Forced GC after every pass, global throughput mode, hardmode event hooks,
  content reduction or smaller POI to save time. No acceleration claim without
  a same-fixture benchmark; generation quality remains the priority.

## Acceptance boundary

### r324 continuation (not full acceptance)

Separated residence spacing from the actual writer's bounding rectangle plus an
twelve-tile native-framing halo. Foreign StructureMap rectangles retain their original
padding; sparse holes cannot admit foreign structures. Framing now runs once at
import, over the written bounding rectangle, rather than on every housing query
over the full source canvas. A local-lifetime five-plane snapshot guard rejects
any changed foreign protected tile after placement. Chest relocation destinations
also exclude foreign reservations. Synthetic negative controls detected changes
in each of the five planes; read-only housing queries preserved the control area.

Native mainland/support-survey contacts are no longer rejected solely for being
more than 192 tiles above the global worldSurface marker. Depth, relief and asset
height constraints remain. Empty native surface domains additionally consider
supported quarter-width contacts across the footprint, without resizing scenes
or relaxing biome/relief/reservation gates.

The saved Large fixture improved from 58 nonempty profile domains to 61, but the
global simultaneous layout still fails. This is NOT 61 built homes and not a
mathematical proof of unsatisfiability. The target x10 is being tested separately
for its real available capacity. Main-profile installation remains withheld.

Targeted policy and real dedicated cache probes are not full-world acceptance.
Evidence and remaining placement failures belong in
`D:/SteamLibrary/CodexWork/dingdong-x10-world/adaptation-r323/RESULT.md`.
No live installation until the required dedicated Large FTW Legendary generation
reports `DINGDONG_WORLDGEN_END|verdict=PASS`, followed by the relevant x10 checks.
Player visual approval remains separate and has not been granted by this work.

### r327 continuation

The unsupported-pair refinement runs only for an exhaustively proven conflicting
pair of current protected-rectangle domains, never because a timer elapsed.
Budget: 250,000 pair checks, eight surface profiles, 8,192 contact samples/profile,
32,768 samples total. It keeps all old candidates and gates. A partial pair scan
is Unknown. On the saved Large fixture, 6,360 samples added 24 alternatives in
0.64 seconds of refinement; this is not a whole-world generation speed claim.
The former nine unsupported pairs are now three (Bandit/ArmsDealer/DyeTrader).
Their limited left-side terrain and protected pyramid remain real constraints.
Do not rerun an unchanged full solver or shrink the source buildings.

Greaveil's tree/capsule is restored from the exact pinned original world, not
invented geometry: see `../authored-source-context.md`. Its 120x100 original
core remains identical in five planes inside a 156x122 canvas. The original
ground contact is translated to Y95; biome/ground X sampling follows that core.
The restored vanilla dresser is registered with Chest.CreateChest using a new
empty slot. Scoped registration ownership rolls back failed previews/probes,
including nested imports. It never rewrites an existing container or adds loot.
Native framing/house checks passed for this scene (two rooms, 1,103 tree cells
and 58 bed/bookcase/dresser cells), with all original probe state restored.

Aegis's gray source rectangle includes authored BlueDungeonBrick41,
AdamantiteBeam150, DiamondGemspark155 and housing wall SandstoneBrick34. It is
not evidence that ordinary natural terrain should be added to the omission
filter. Deleting these materials globally would destroy other constructions.
The source-context seam and owner visual acceptance remain open.

Current evidence: `D:/SteamLibrary/CodexWork/dingdong-x10-world/adaptation-r325/RESULT.md`.
The fresh r327 x10 generation is a separate post-settling gate; diagnostic PASS
or source framing alone must never be reported as all-world completion.

The fresh r327 native world subsequently exposed a mixed-depth Underworld
candidate range6770..6710. The search and cavern ceiling now consume the direct
generating depth policy instead of the potentially inlined Main getter. Thirteen
bounds checks and the real x10 castle domain PASS (19 sites rather than zero).
The follow-up placement replay found missing transient SOTS origin on load;
paired-snapshot capture rehydration is implemented and compile-verified only.
Owner requested wrap-up before that runtime retry. Work is paused, final61-house
post-settling and Large gates remain open, live artifact is unchanged. See the
terminal checkpoint in adaptation-r325/RESULT.md; do not read earlier candidates
or stage-level PASS receipts as current release acceptance.
