# Ding Dong architecture

The mod is organized by gameplay responsibility. A source module must describe one cohesive operation or phase; a partial type may span several modules, but numbered shards such as `Behavior2` or `Phase3` are forbidden.

## Runtime boundaries

- `BiomeHomeWorldgenSystem*` owns the world-generation pipeline. It asks the planning layer for placements and applies accepted residences atomically.
- `CandidateDeploymentSitePlanner*` owns candidate collection, terrain sampling, hard-safety repair, capacity sectors, cross-layer repair, and final layout selection. Each phase has an explicit semantic filename.
- `AuthoredResidenceLayoutPolicy*` owns acceptance rules and diagnostics. The deterministic solver families own exact search, repair/min-conflicts, beam search, and arc consistency; they do not mutate the world.
- `DingDongAuthoredStructureLoader*` owns the reviewed structure format, source-cell classification, write planning, and the structure cache.
- `NpcQuest*`, `NpcResidenceWorldSystem*`, and `QuestContentRegistry*` own quest state, escort state, persistence, packet handling, and cross-mod target discovery.
- `Damage*` owns damage classification, authority, receipts, difficulty context, UI content, and exhaustive probes.
- `Items/AiArsenal`, `Items/MobRelics`, `Items/SynergyArmor`, and `Items/AccessoryEnchanting` own generated content and its runtime mechanics. Audit types validate materialized items, assets, persistence, and server-authoritative transactions.
- `DingDongHubUiSystem` is the single UI owner for Quests, Combat, Residences, and Help. Commands select a hub tab directly; retired standalone journal/guide states must not return.
- `AllModEndToEndProbeSystem*` is the fail-closed exact-mod-set acceptance boundary. It invokes production hooks and aggregates damage, quests, multiplayer isolation, generated content, and persistence results.

## Source invariants

1. Every production C# file is at most 5,000 Unicode characters.
2. Filenames and members describe their responsibility; mechanical numbering (`Behavior2`, `Search1`, `Phase3`, and similar) is rejected by tests.
3. A renamed primary file matches its declared top-level type. Partial suffixes describe operations such as `PacketDispatch`, `HomeAssignment`, or `ClassificationMatrix`.
4. Network mutations are server-authoritative. Inventory fusion, quest progress, teleports, and damage receipts must preserve their existing validation and replay guards.
5. World planning is read-only; tile mutation occurs only after an accepted plan reaches the world-generation/application boundary.

The structural guard is `Tests/test_csharp_complexity_budget.py`. Feature contracts aggregate all semantic partial modules through `Tests/source_modules.py` so splitting a type cannot silently remove assertions from the test surface.

## Evidence ladder

- Run the structural guard and the contracts closest to a changed subsystem first.
- Run a Release build after all semantic modules are stable.
- Run the exact-66 dedicated prepare/reload E2E once for the final artifact digest.
- A byte-identical copy to the live Mods directory requires digest verification and smoke only; it does not justify another full E2E run.
