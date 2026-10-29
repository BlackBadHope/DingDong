# Authored housing dedicated-server baseline

This is the authoritative technical baseline for the 61 candidate residences.
The main tModLoader client was not launched. All commands ran on a disposable
Large For the Worthy world named `DingDongHousingProbe-20260821` with numeric
seed `284875780`.

## World gate

- exact full-worldgen artifact SHA-256:
  `061A8B8AE1073A301FBBA08520E24032911B4D681A466D8256D3F8F675D3BBC1`
- size: Large (`8400 x 2400`)
- For the Worthy override: enabled
- world saved: PASS
- dedicated server reached `Server started`: PASS
- DingDong authored generation remained fail-closed at `0/61` approved presets:
  expected PASS behavior, not a generation error

The saved disposable world is reused for targeted probe commands. Full worldgen
must not be repeated for a byte-identical artifact.

## Terraria housing authority

The probe places each authored schematic in a cleared disposable envelope and
calls Terraria's actual `WorldGen.StartRoomCheck`, `RoomNeeds` and `ScoreRoom`.

`DINGDONG_HOUSING_PROBE_END|verdict=FAIL|actualPass=45/61|expectedRepair=12/18|regressions=4|errors=0`

- actual housing PASS: 45/61
- actual housing FAIL: 16/61
- schematic loader/runtime errors: 0
- archived full log: `tModLoader-Logs/Old/2026-08-21-24.zip`, entry `server.log`

The exact 16 replacement inputs are:

1. `Aegis8_ForestHillManor`
2. `Ziiio_ForestLakeCottage`
3. `Aegis8_ForestWindmill`
4. `Ziiio_CliffTreeLodge`
5. `Aegis8_SnowSpire`
6. `Ziiio_SnowLodge`
7. `Ziiio_DesertPavilion`
8. `Ziiio_VerticalTreehouse`
9. `Ship1`
10. `WoB_WestGalleon`
11. `Ziiio_WaterLaboratory`
12. `Ziiio_IndustrialFoundry`
13. `Ziiio_JunglePortalHouse`
14. `Greenhouse2`
15. `Ziiio_NightClub`
16. `Ziiio_CrimsonSpire`

## Reserve and shell diagnostics

Reserve probe:

`DINGDONG_RESERVE_HOUSING_PROBE_END|verdict=BASELINE_PASS|actualPass=6/18|actualFail=12/18|errors=0`

The six valid reserves are three prison cells and three small Ziiio village
infill buildings. They remain useful for later districts but are rejected as
replacements for large NPC showcase residences. Archived full log:
`tModLoader-Logs/Old/2026-08-21-25.zip`, entry `server.log`.

Targeted shell probe:

`DINGDONG_FAILED_SHELL_PROBE_END|verdict=BASELINE_PASS|withShells=0/16|withoutShells=16/16|errors=0`

All 16 failures lack a closed Terraria room shell. Furniture injection is not
a valid repair. The only accepted path is a different authored residential
building or an explicitly authored closed interior that is part of the visible
design. Current log: `tModLoader-Logs/server.log`.

## Active roadmap decision

Stage `3E-R` sources non-overlapping authored replacements from the private
Workshop intake, exports them with provenance and digest, and admits only
candidates that pass the same dedicated-server housing authority. The approved
manifest stays at `0/61` until per-preset anchor, biome, housing and visual gates
are complete.

## Replacement intake checkpoint 3E-R5

The first visually strong replacement sets were tested by the same Terraria
housing authority on the saved disposable Large For the Worthy world.

- Aegis8 showcase: `3/5` housing PASS, `0` loader errors. Snow Harbor
  Settlement has 4 valid rooms, Moon Citadel 4 and Cloud City 2. East Airship
  and Ocean Galleon have no valid room and are retained only as POI assets.
- Updated huadiao world: `4/4` selected showcase residences PASS, `0` loader
  errors. Elemental Sky House has 2 valid rooms, Fire Sky Village 5, Crescent
  Village 2 and Jungle Gothic Manor 3.
- Barton Sky Treehouse: `0/1`; `HoleInWallIsTooBig`. It is reclassified as a
  landmark/POI rather than silently patched.
- Gnorp Builder: `14/16` technically valid housing, but visual review classifies
  the passing set as a related village-infill family. It does not fill any of
  the sixteen showcase replacement slots.

Current large-showcase replacement coverage is therefore `7/16`. At least nine
additional visually distinct authored residences are still required before NPC
mapping and placement acceptance. The approved manifest intentionally remains
`0/61`.

Evidence:

- Aegis8 server log SHA-256:
  `874EBFD6AFBFDC767A0A16F0F7AA46B2D77C5AA168930C8945B9A65A04CB979B`
- Huadiao/Barton server log SHA-256:
  `73EDE4DC66EF70AFFB54B11F3161AB84AFD54DE28CE206AC881BEDACAFBC8E63`
- Huadiao/Barton package SHA-256:
  `5B77055049AC1D2485A859D64875E6E48D65468C4760C6EE77E2BC9DE9BBD543`
- machine assertion: `dingdong-residence-intake-housing-log-v1` PASS

## Replacement intake checkpoint 3E-R6

HANNIBAL Snow Residence and Magoozy Palace added two independent large
residences. The saved disposable Large For the Worthy server reported 3 and 8
valid rooms respectively, with `2/2` PASS and no loader errors. Tested artifact
SHA-256: `42BBF5D3525922B0A879D65B3EAAC163716564A4422E41FFA056D9EBB2098262`.
This brought large-showcase replacement coverage to `9/16`.

## Replacement intake checkpoint 3E-R7 and final acceptance

Eight additional standalone authored residences from eight separate private
Workshop worlds passed the same Terraria housing authority:

| Preset | Valid rooms |
| --- | ---: |
| `R7_NadikiMansion` | 6 |
| `R7_SnowMansion` | 8 |
| `R7_PrincessCastle` | 11 |
| `R7_CastleCoolResidence` | 18 |
| `R7_EsonoraHome` | 10 |
| `R7_ScarletDevilMansion` | 19 |
| `R7_UnrealDeltaCastle` | 13 |
| `R7_MansionAlmost` | 9 |

The final artifact includes the distinct `61/61` candidate mapping and was
accepted again on the saved disposable world. Server runtime evidence:

`DINGDONG_ROADMAP_R7_HOUSING_PROBE_BEGIN|world=DingDongHousingProbe-20260821|presets=8|envelope=7100,40,740,490|masterMode=True|getGoodWorld=True`

`DINGDONG_ROADMAP_R7_HOUSING_PROBE_END|verdict=BASELINE_PASS|actualPass=8/8|actualFail=0/8|errors=0`

- final tested `.tmod` SHA-256:
  `E428C5BECBA0BCFA4A1B431E7A95FF2C9B94E511FAF17122CC89AC3F8CDBC3F3`
- final frozen server log SHA-256:
  `136E642A818D1BAA0AE1E2EE1522EC1BB48FE7AC5F4CBCBB672EA7A64ECDE8DB`
- isolated save root:
  `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\build-save`
- loader/probe errors: `0`

Seven R7 presets fill the remaining replacement slots; the incomplete
`R7_MansionAlmost` remains a tested reserve. The technical housing set is now
`45 original PASS + 16 replacement PASS = 61/61`, with no duplicated geometry.
This closes mapping coverage only. The runtime-approved manifest remains
`0/61` until per-assignment biome, anchor, protected-footprint and player visual
acceptance are complete.
