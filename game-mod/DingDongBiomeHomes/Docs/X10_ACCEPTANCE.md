# Ding Dong x10 acceptance

Scope: a 28000 x 7200 ForTheWorthy world (10 times Large by tile area),
the pinned 66-mod pack, world creation and persistence, required native
progression, biome climate and two real multiplayer clients. A passing
subsystem receipt is evidence for that subsystem, not overall completion.

## Required checkpoints

1. Harness behavior: saved successful and failing logs exercise the real
   validators. Expected compatibility adapters are versioned. Every already
   observed fatal/save failure is reported, with duplicates removed. All
   executable dependencies, runtime and staged configs bind the evidence.
2. Lifecycle: the generated-world copy loads, accepts console exit, completes
   a new save after that command, exits zero and leaves no child server.
   The full and targeted paths use the same lifecycle validator.
3. World: rooms and corridors are physically connected in final tiles;
   dungeon and temple have accessible entrances appropriate to progression.
   Temple altar/door, evil altars, hive/larva, hellforge and biome loot have
   actual placements. Counts copied from the generation plan do not suffice.
4. Persistence: one full generation and independent reload/resave per exact
   artifact/environment. Post-load invariants recheck the generated mandatory
   structures, chests and tile entities. Structural file checks supplement
   rather than replace game loading.
5. Multiplayer: two graphical clients load the exact world; planned placement
   is separate from normal-control movement across section boundaries.
   Match server/client terrain samples; observe no unexpected discontinuity,
   death or disconnect. Exercise climate on active players in the relevant
   biome. Existing isolated damage/quest hook probes retain their narrower
   description; they are not mouse-driven gameplay evidence.
6. Loot: exercise Player.OpenBossBag on registered bags and inspect Item.NewItem
   emissions, including known guaranteed rewards. Run an optional-rate sample
   with nonzero reduced output and preserve the 1/4000 rare floor. Report
   static classification and observed outcomes separately.
7. Deployment: back up main's prior artifact, install the exact verified bytes
   and required config changes, verify hashes and perform bounded smoke.
   A byte-identical deployment does not need another full world generation.

## Execution boundaries

- No expensive worldgen while a known mandatory blocker remains.
- Independent work may proceed concurrently; only one runtime orchestrator
  owns tModLoader processes and shared logs at a time.
- Never modify existing player/world saves during acceptance. The main mod
  and enabled list remain hash-guarded until deliberate deployment.
- Stop tests on a concrete failure and inspect all captured evidence before
  changing the candidate. Do not remove requirements to produce PASS.
- Visual/player-facing quality and observed multiplayer behavior remain
  separate from pure compilation and metadata assertions.

Current checkpoints and results must come from run receipts; this document
does not claim any checkpoint passed.
