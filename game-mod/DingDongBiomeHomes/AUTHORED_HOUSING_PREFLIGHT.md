# Authored housing preflight

This gate screens the 61 authored NPC candidates without launching the main
tModLoader client. It reads `.shstruct` tile/wall data and uses the official
`TileID.Sets.RoomNeeds` chair, table, torch and door sets. It does **not** claim
Terraria housing validity; dedicated-server placement plus
`WorldGen.StartRoomCheck` remains the acceptance authority.

Analyzer source SHA-256:
`A431A9E56E7AECB5F7414B7BC84F8F92C8F28EEADABED36B7CA6BCBF437A1F75`.

## Calibration gate

| Control preset | Catalog capacity | Detected candidates | Result |
| --- | ---: | ---: | --- |
| `SmallHouse1` | 1 | 1 | PASS |
| `MediumHouse1` | 2 | 2 | PASS |
| `LargeHouse1` | 3 | 3 | PASS |
| `Greenhouse1` | 1 | 1 | PASS |
| `Ship1` | 0 | 0 | PASS (negative control) |

## 61-candidate result

- candidates: 61
- with at least one component containing all four room-needs: 43
- requiring a furniture supplement or reassignment before server acceptance: 18
- room candidates detected across the complete matrix: 124
- distribution: `0:18, 1:14, 2:11, 3:4, 4:5, 5:4, 6:1, 7:2, 8:2`

This result is only a cheap offline screen. The later authoritative dedicated
server baseline accepted 45/61 candidates and rejected 16/61. A targeted shell
probe then found `withShells=0/16`: all 16 rejected candidates lack a closed
Terraria housing shell, so a furniture supplement cannot repair them. The
offline connected-component heuristic produced false positives and false
negatives on large authored scenes. Those 16 presets must be replaced by other
authored residential buildings; automatic boxes, invisible rooms, rectangular
cave cuts and generic fallback rooms remain forbidden.

| Preset | Biome | Placement mode | Best diagnostic component | Missing room-needs |
| --- | --- | --- | --- | --- |
| `Aegis8_ForestHillManor` | Forest | TerrainScene | `26,69-55,72` | table |
| `Aegis8_ForestWindmill` | Forest | TerrainScene | `0,135-2,143` | chair, table |
| `Aegis8_MountainLodge` | Forest | TerrainScene | `42,23-48,28` | chair, table |
| `Aegis8_SteampunkOrb` | Forest | FloatingScene | `129,139-136,146` | chair |
| `Ziiio_CliffTreeLodge` | Forest | TerrainScene | `95,174-110,178` | table, light |
| `Ziiio_ForestLakeCottage` | Forest | TerrainScene | `93,69-101,78` | table |
| `Aegis8_HallowCliffManor` | Hallow | TerrainScene | `76,186-84,205` | chair |
| `Greenhouse2` | Hallow | SurfaceStructure | `9,25-45,42` | chair, table, door |
| `Ziiio_JunglePortalHouse` | Hallow | TerrainScene | `35,83-67,100` | chair, table |
| `Ziiio_VerticalTreehouse` | Jungle | TerrainScene | `105,187-144,239` | table, light |
| `Ship1` | Ocean | SurfaceStructure | `7,23-33,29` | chair, table, light |
| `WoB_HarborCenterHouse` | Ocean | HarborScene | `29,55-37,65` | chair, table, light |
| `WoB_HarborWestHouse` | Ocean | HarborScene | `38,47-44,54` | chair |
| `WoB_WestGalleon` | Ocean | HarborScene | `25,6-55,18` | chair, light |
| `Ziiio_SnowLodge` | Snow | TerrainScene | `150,56-166,63` | table |
| `Aegis8_DesertGothicManor` | Underground | TerrainScene | `6,59-17,77` | chair |
| `Ziiio_IndustrialFoundry` | Underground | TerrainScene | `102,109-134,125` | chair |
| `Ziiio_WaterLaboratory` | Underground | HarborScene | `32,123-60,145` | chair, table |

Coordinates above are historical diagnostics only. They are not approved
placement points and must not be used for furniture injection. The authoritative
result and exact repair set are recorded in `AUTHORED_HOUSING_SERVER_BASELINE.md`.
