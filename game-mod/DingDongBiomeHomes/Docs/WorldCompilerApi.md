# Ding Dong World Compiler API v2

The API uses tModLoader `Mod.Call` and BCL-only return values, so a provider does
not need to reference Ding Dong types.

```csharp
Mod dingDong = ModLoader.GetMod("DingDongBiomeHomes");
var capabilities = (Dictionary<string, object>)dingDong.Call(
    "Worldgen.GetCapabilities");
```

## Queries

- `Worldgen.GetCapabilities` returns the API version, operation names,
  extension phases, worker budget and plan availability.
- `Worldgen.GetPlanSummary` returns dimensions, numeric seed, fingerprint,
  chunk count, cave-node count and structure-room count.
- `Worldgen.GetBiomeBands` returns `object[]`; every entry is a
  `Dictionary<string, object>` containing `biome`, `left` and `right`.
- `Worldgen.GetSurfaceY`, `int x` returns the compiled surface coordinate.
- `Worldgen.GetContractSnapshot` returns one BCL-only dictionary containing the
  plan summary, a copied `int[]` surface profile, biome bands and chunk/read-halo
  bounds. Prefer this single call when a provider needs many coordinates; it
  avoids repeated `Mod.Call` and any second `Main.tile` discovery scan.

Plan queries are valid after the Ding Dong compiler has prepared the current
world. Calling one without an active plan fails explicitly instead of returning
guessed coordinates.

## Deterministic extension actions

Register during mod loading or content setup, before world generation starts:

```csharp
bool admitted = (bool)dingDong.Call(
    "Worldgen.RegisterAction",
    Name,
    "place-my-structure-v1",
    "AfterWorldContracts",
    (Action)(() => PlaceMyStructure()));
```

Supported phases:

- `AfterBaseTerrain`
- `AfterCavernGraph`
- `AfterResources`
- `AfterHydrology`
- `AfterStructures`
- `AfterWorldContracts`
- `AfterCompatibility`
- `AfterNaturalDecoration`
- `BeforeFinalize`

Actions run sequentially on the world-generation thread, once per phase,
ordered by provider name and action key. Duplicate identities and late
registrations are rejected. An exception aborts generation with the exact
provider, key and phase; partial silent success is forbidden.

Use the plan queries inside an action instead of scanning all `Main.tile`
columns merely to rediscover biome boundaries or surface coordinates.
