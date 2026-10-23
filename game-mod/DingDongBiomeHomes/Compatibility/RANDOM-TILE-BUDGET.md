# Optional experiment: bounded aggregate random tile updates

This own adapter changes budget operands only. It copies no game/provider method body and does not replace dimensions, tile maps, coordinate sampling, world-rate logic or authority checks. The public edition ships this experiment disabled by default; enabling it requires a live comparison of the growth tradeoff.

## Evidence and formula

The supplied ten-second profile attributes 56.77% inclusive samples to WorldGen.UpdateWorld, including 11.66% leaf samples in Calamity HandleTileGrowth and 10.42%/6.53% in vanilla overground/underground tile updates. Inclusive numbers overlap and are not additive. These observations identify a candidate CPU cost, not a complete proof of the separate HP symptom or an FPS/UPS guarantee.

Physical Large area is 8400 x 2400 = 20,160,000 tiles. The current Continental map is 28000 x 7200 = 201,600,000, exactly ten Large areas. Both pinned methods multiply the physical integer area by an existing growth-rate coefficient for each random update loop.

For those seven budget expressions only, replace area `A` with `min(A, 20,160,000)`. The existing coefficient, float/double conversions, `GetWorldUpdateRate`, loop comparison/rounding and random coordinate ranges remain in their original IL. Nonpositive native operands are also returned unchanged; this adapter is not an overflow repair for unsupported world sizes.

The server-side `RandomTileBudgetConfig.AutoNormalizeExpandedWorldGrowth` setting defaults to false in the public release and can be disabled. It uses tModLoader's existing ServerSide configuration and synchronization system; this adapter introduces no separate network protocol or permission route. Disabled returns every original area operand unchanged. Enabled is also exact identity for standard worlds and any map whose area is at most one vanilla Large world; only oversized budgets are capped. Changing this setting does not change dimensions or world time.

At native rate 1, vanilla overground uses the original float `3e-5`, ordinary underground `1.5e-5`, and remix underground `2.5e-5`. The normal Large loops execute approximately 605/303 attempts; their unbounded Continental equivalents are 6048/3024. Exact integer execution counts retain the original float rounding and integer-versus-floating loop comparison. The adapter uses the Large budget on Continental while sampling the entire physical map.

## Tradeoff

This reduces aggregate work, not just redundant computations. Each tile on a ten-area map is selected about one tenth as often per tick by these loops. Plants, grass, tile random updates and infection/growth mechanisms reached by them consequently progress more slowly per tile across the whole world, including offscreen regions; the total growth work resembles one Large world. Nothing is globally disabled and no region is excluded. Liquid simulation, other mods' independent growth loops and nonrandom world updates are unchanged. Higher UPS may partly offset the per-real-second difference, but no resulting rate or 60 UPS is promised.

## Pinned IL contract

- Engine DLL SHA256 `00D560FAA33CE25522E85792CD1B25C8D5BE95787A10AE2B03FA33AC496BDF2E`, MVID `033e87cd-cefe-4dc6-a1fb-e14aa938bbb2`. `WorldGen.UpdateWorld` is a wrapper; its direct caller target `UpdateWorld_Inner` contains three area budget expressions originally starting at IL_0207, IL_0329 and IL_0384. Their coefficient local indices are 3, 5, 4; all convert integer area to double.
- Calamity audit DLL SHA256 `02C6372141AFA3C893C93278ABBCCE1041E0F81EA956BA124677474AC27D9114`, MVID `0e63c698-ec72-446e-ac1f-8d01f0ee7ac7`. `CalamityMod.Systems.WorldMiscUpdateSystem.HandleTileGrowth` has four expressions originally starting at IL_0194, IL_02AB, IL_03BD and IL_0D6F. Coefficient locals are 1, 3, 2, 12; the last area converts to float and the others to double.
- Original offsets are audit navigation, not fragile runtime selectors. Select only adjacent integer maxTilesX/maxTilesY multiplication followed by the specified conversion, coefficient-local load and multiplication. Require exact expression count and coefficient/precision order before emitting any delegates. Insert one own integer-to-integer call after each area multiplication. Other dimension operands are untouched.
- Runtime identities are checked before hook installation. If any present provider is unsupported or a target pattern changes, dispose every installed hook and keep the original behavior with a warning. Calamity is optional; an absent provider leaves the vanilla adapter only. Unload disposes hooks in reverse order. No persistent state or save/net format is introduced.

## Regression contract before acceptance

1. Validate the seven anchors against the exact pinned methods using Cecil; clone IL only, without loading game/provider assemblies for execution. Show exactly 3 + 4 own inserted calls and no other changed original instructions, branches, constants, field operands or exception handlers. Reject a deliberately changed pattern without partial emitted edits.
2. Policy identity for Small 4200x1200, Medium 6400x1800 and Large 8400x2400, including original rate 0, fractional rate, 1 and accelerated rate; retain the original float/double evaluation and integer loop counts. Test cap boundary and Continental's effective area/attempts. This is pure arithmetic and needs no world.
3. One candidate compile after source integration; no repeated full suite. Static review must confirm no writes to Main dimensions/tilemap, no altered RNG ranges, callbacks, multiplayer authority, world-rate function, liquidation or event logic. Check cleanup on installation failure/unload.
4. Live game/server profiling, host/client NPC motion, gameplay growth and HP behavior remain NOT_RUN in this subtask. The public default leaves this experiment disabled. A separately authorized live comparison must establish actual UPS benefit and the acceptable growth tradeoff before enabling it for normal play.

## Executed bounded fixture

`PublicCandidate/RandomTileBudgetChecks` links the actual four own adapter/config source files. It uses Cecil and MonoMod ILContext on the pinned metadata models and small compile-only game API stubs. It never loads the game/provider assemblies for execution and never calls ModSystem.Load, runtime ILHook installation or game/Steam APIs.

One Release build and one execution passed on 2026-10-22, zero warnings/errors, approximately four seconds total:

- exactly 3 vanilla + 4 Calamity own calls inserted;
- every original instruction, branch target, local and exception handler preserved;
- two deliberately changed patterns rejected before any delegate insertion;
- six Small/Medium/Large enabled/disabled identity cases and 120 rate/precision cases passed;
- Continental cap, disabled restoration, area boundaries and server config scope/default/wiring passed;
- own compiled adapter has zero Main-field writes; both pinned input hashes unchanged;
- game/provider/Steam assemblies loaded for execution: zero.

Report: `PublicCandidate/RandomTileBudgetChecks/obj/verification.json`. Hook-installation rollback/unload behavior was reviewed in source; the fixture does not execute live detours, a multiplayer config handshake or native gameplay. Full integrated mod compilation and current-world performance remain **NOT_RUN** here. The successful fixture is reused; it is not rerun for documentation/readback.
