# Authored placement review

Этот документ фиксирует placement-gate этапа 3. Он не является разрешением на
worldgen: `NpcAuthoredPresetManifest` остаётся пустым до housing, protected
footprint, seed-fallback и пользовательской визуальной приёмки.

## Контракт

- `SurfaceStructure` — компактная схема с явно проверенным floor anchor.
- `TerrainScene` — здание вместе с авторским рельефом; требует органической
  terrain-mask и blending, обычный surface-planner запрещён.
- `HarborScene` — схема с берегом/водой; требует waterline anchor и проверки
  сохранения океана.
- `FloatingScene` — остров, воздушный корабль или отдельный небесный landmark;
  не привязывается к поверхности земли.
- `UndergroundPoiScene` — готовая подземная сцена; обязательны округлая граница,
  фон, переход в породу и POI lifecycle.
- `UnderworldPoiScene` — сцена нижнего мира; требует ash/lava shelf planner и
  не проходит через обычный surface-planner.

Любой private Home asset сейчас имеет `PlacementReviewed=false` и nullable
`GroundOffset`. Это намеренный fail-closed статус. Значение `0` больше не
используется как фиктивный anchor. `HomeStructureTemplate.AreaAt` и дорожный
builder требуют reviewed anchor, а surface-planner отклоняет все scene modes.

Технический candidate deployment manifest теперь отдельно хранит 61/61
назначений без разрешения runtime worldgen: 9 compact `SurfaceStructure`
используют reviewed ground row, а 52 full scenes используют typed target anchor,
protected padding, blend margin, boundary policy и seed fallback. Поле
`PlayerVisualApproved` остаётся `false` у всех 61 строк.

## Текущий инвентарь режимов

| Source group | Count | Placement status |
| --- | ---: | --- |
| Structify | 36 | `SurfaceStructure`; исходные anchors сохранены и помечены reviewed; из них 11 назначены в матрице |
| Private `TerrainScene` | 38 | classified, anchor/blending pending |
| Private `HarborScene` | 8 | classified, waterline pending |
| Private `FloatingScene` | 4 | classified, altitude/clearance pending |
| Private `UndergroundPoiScene` | 2 | classified, POI mask/lifecycle pending |
| Private `UnderworldPoiScene` | 1 | classified, ash/lava shelf pending |
| **Total** | **89** | **53 private scenes fail-closed** |

Режим источника и режим назначения NPC — разные вещи. Например, наземное
авторское здание, назначенное Underground NPC, должно быть встроено в новую
полную подземную POI-сцену; наземный crop сам по себе не даёт права вырезать
прямоугольную камеру.

## Evidence

- `obj/evidence/structify-anchor-calibration.json` — полный tile/wall row profile
  36 Structify files; SHA-256
  `15998EC5C9C1E6FE4194FFEAA0FE545FCC853AACDA8BE3F2F26EC81AE2BAE356`.
- `obj/evidence/private-home-geometry.json` — 53/53 private files have active
  geometry and per-column dense-terrain diagnostics; SHA-256
  `A3487DBA058ED78481A18F34DF1BE56C763498200B53C290D67A1C31C6E45525`.
- `obj/evidence/placement-mode-contract-build.log` — compile PASS, 0 errors;
  four private helper families all emit `PlacementReviewed=false`.
- `obj/evidence/candidate-manifest-runtime-gate.log` — runtime static initializer
  resolved 61/61 candidate mappings against both canonical NPC identities and
  the authored catalog, and verified 61/61 catalog/file dimensions; approved
  manifest remains exactly 0/61.
- Диагностические contact sheets находятся только в private intake temp и не
  входят в распространяемый мод. Автоматическая линия плотности используется
  для поиска кандидатов, но визуально доказано, что она пересекает крыши и
  многоуровневые сцены, поэтому runtime её не использует.
- Placement review отклонил исходные Ziiio crops `airship-house` и `night-club`
  из-за несвязанного прямоугольного океана/песка. Они recut из того же pinned
  source-world в `160x110` и `175x115`; новые binaries независимо прошли 2/2
  parser gate, а старые hashes сохранены в private temp evidence.
- Dedicated deployment-contract PASS на Large FTW Legendary:
  `DINGDONG_DEPLOYMENT_CONTRACT_END|verdict=PASS|profiles=61|distinct=61|compact=9|scenes=52|approved=0/61|masterMode=True|getGoodWorld=True`.
  Frozen log SHA-256:
  `D918ADEEB0F26B6B4C4C9DD33AB60EB12F47EBEE3BFAB13C610C1DEB9EF999D2`.
- Exact loader-write mask следует реальной семантике `Generate`: клетки
  `NullBlock`/`NullWall` не считаются мутацией, поэтому сохранённый natural
  background не создаёт ложный collision. Independent single-scene site gate
  локализовал один vanilla chest у
  `Terraria/PartyGirl -> R7_ScarletDevilMansion`. Lossless relocator перенёс его
  из `1496,622` в `1946,710` вместе с пятью item slots и именем; signature
  `98D02B33E744F6A7` подтверждена после reload. Повторный site gate дал `61/61`
  clean, advisory profiles 0, persistent overlaps 0. Clean-site artifact
  SHA-256:
  `FF211DBE9BC6E57BC867F4B1DCEF5B1CFF0330146C3FE3F4F4308DDFC0BBFB3A`.
  Apply/reload log SHA-256:
  `27EA33345DC409CB474F60D0591CBF4E135806A7EE1FD3EE175C71CFC1FA4159` и
  `EF7C5F3B3B7035F3D2FE3FE1778A722773E931DC229C6A920C1BCC71F11C9A67`.
- Fail-closed Scarlet preview artifact SHA-256
  `A146914D8C8CB41D531D6CC7D9A5BF358FFD0AE4D9D793E2AB83B6E4EA13DFC5`
  прошёл dedicated apply/save/reload на Large FTW Legendary: 20 валидных комнат,
  один lossless relocated chest, `loadedFromSave=True`, current/saved rooms
  `20/20`. Apply/reload log SHA-256:
  `4943ACDC0F535546E5F784E95A3185E0736C99B9B722A56E7253597E4F971D35` и
  `B42F34E1A7368D191E875C43DA47A61475F9AFB6089BF3557FE931B0A0261B60`.
  Simultaneous layout и worldgen placement явно `False`; approved остаётся 0/61.
- Первый отдельный Brewer preview дал реальный `IndexOutOfRangeException` в
  Terraria wall framing. Контекст локализовал три active клетки tile ID 726,
  хотя Greaveil exporter сообщал `tile726->empty:3`; причина — TEdit `Tile`
  передавался в compatibility helper по значению. После `ref Tile` re-export
  все 13 Greaveil assets имеют 0 active target-incompatible IDs, пять non-remap
  assets byte-identical. Текущий artifact SHA-256:
  `0FA2159F608518E5154454938624D2298CC428B892BAA71455973B39B450E336`.
  Corrected Brewer apply/reload дал 2/2 комнаты, Architect canary — 4/4; все
  остальные семь Forest full scenes также прошли apply/save/reload. Exact
  world/log hashes находятся в `PLAYER_VISUAL_ACCEPTANCE.md`.
- Semantic-runtime-identical successor SHA-256
  `6E6164AE46609D7B5EB11CB65EAEF7E020C64A65F90588A616D432E9FACDA814`
  дополнительно прошёл apply/save/reload оставшихся четырёх Snow scenes:
  Cyborg 1/1, corrected Greaveil Mechanic 5/5, Santa 1/1 и Tax Collector 3/3.
  Exact world/log hashes также находятся в `PLAYER_VISUAL_ACCEPTANCE.md`.
- Следующий semantic-runtime-identical artifact SHA-256
  `51ACC72CE865CDCB736151704204A000AB6B953E216C1023630F148830D1F3F5`
  прошёл apply/save/reload пяти Desert scenes: Bandit 6/6, corrected Greaveil
  Arms Dealer 4/4, Dye Trader 8/8, Steampunker 3/3, Desert Acolyte 8/8.
- Semantic-runtime-identical successor SHA-256
  `73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`
  прошёл пять Jungle scenes: Jharim 5/5, Dryad 3/3, Painter 3/3,
  Witch Doctor 2/2, Druid 1/1.
- Тот же функциональный artifact прошёл семь оставшихся Ocean scenes:
  corrected Greaveil Sea King 3/3, Jelly Priest 2/2, Fisherman 4/4, Pirate 2/2,
  Stylist 1/1, corrected Greaveil Diverman 2/2, Fishylady 1/1. Angler #39 не
  повторялся, поскольку exact compact PASS уже зафиксирован.
- Тот же artifact прошёл пять оставшихся Underground scenes: Golem 5/5,
  Deeps 3/3, Clothier 1/1, Demolitionist 2/2, Goblin Tinkerer 1/1. Blacksmith
  #49 не повторялся после R7 PASS; Clothier 60x105 требует owner visual verdict.
- Тот же artifact прошёл семь оставшихся Hallow scenes: Oracle 2/2, Tavern
  10/10, Nurse 3/3, Princess 4/4, Wizard 2/2, Spiritualist 1/1, Witch 4/4.
  R7 #51/#54 не повторялись.
- Последние Mushroom/Underworld scenes также PASS: Truffle 3/3, Brimstone Witch
  5/5, Abomination 4/4. Persisted technical review worlds готовы 61/61.
- Финальный docs-only successor
  `DingDongBiomeHomes-roadmap-3f-technical61-doc-smoke-5FCCBCA1D181.tmod`,
  SHA-256
  `5FCCBCA1D1811BFB0F28D5041E11BB2A1686CF86609FF63E81B2764A4BDB809D`,
  сохранил runtime semantic SHA-256
  `931D3A1A8B0650C8A1EE93B08A48F319AFEA48998B47B265488E68295F7FB363`
  по 30,169 canonical lines и прошёл отдельный dedicated Large FTW Legendary
  load-smoke. Frozen smoke-log SHA-256
  `EDED0329EB3E030B5D0762CB7860F4BA8586583F3FF3B12A8B31A0CBFBBB33D4`.
- Player-review successor
  `DingDongBiomeHomes-roadmap-3f-player-review-command-9698BEE2DFE5.tmod`,
  SHA-256
  `9698BEE2DFE514C94D24B12535B93EB52DCEC3C067EFC4EF199962C36CAC33BE`,
  добавил fail-closed `/dingdong review`. Package delta ограничен 8 ожидаемыми
  entries, а compiled semantics вне `DingDongCommand` совпали по 30,094 строкам.
  Dedicated Scarlet apply/reload сохранил тот же target и `approved=0/61`.
- Safe-focus successor
  `DingDongBiomeHomes-roadmap-3f-safe-focus-preflight-ED00AD072CB1.tmod`, SHA-256
  `ED00AD072CB169F5A63DE1ADB62B2E01116CE568AD5B486E36D92653397E7271`,
  использует общий collision-aware finder. Пять persisted Large FTW Legendary
  clones — Scarlet, compact Greenhouse, sky CloudCity, Underground Clothier и
  Underworld Castle — вернули `focusReady=True`; approval остался `0/61`.
- Functional canonical-review-id successor
  `DingDongBiomeHomes-roadmap-3f-canonical-review-id-2FC9A5D5BE77.tmod`, SHA-256
  `2FC9A5D5BE773DFC3C03A276798F8B0AF2BC1980A322BA9B5C38E903F029E36D`,
  fail-closed отвергает persisted preview, если его NPC/preset не совпадает с
  canonical candidate row. Contract verifier сопоставил 61/61 manifest,
  candidate и ledger entries; Scarlet dedicated runtime вернул `reviewId=54`,
  `ownerReply=54:PASS-or-FAIL-with-reason`, `focusReady=True`,
  `approved=0/61`, затем clean save/exit. Contract/runtime log SHA-256:
  `9D29E5DD3509409B07DB700C588D9875B2A649CCBAB358E4405C1C9B8804D13E`
  и `81F565D68B6D7C754D32A44ABC6D4387EBD5ED6B8071914D08055FD73EB59586`.
- Current packaged successor
  `DingDongBiomeHomes-roadmap-3f-canonical-review-id-doc-successor-8C0317D6BCB4.tmod`,
  SHA-256
  `8C0317D6BCB4AC93B6110823604770E409EC34B9CE9A85941A921264642EB0BB`,
  отличается от dedicated-tested `2FC9…` ровно шестью ожидаемыми package
  entries. Runtime semantic SHA-256 совпал по 30,866 canonical lines:
  `3E07845D779506EF87D3BAB1EA1E8F90F6AACE558F6C5F86602B4E757C5D421A`;
  scope log SHA-256
  `40E833B23E4B601A5D3A8534B062DAD429C31E1284BCA327DAEA3B457FAD1BF7`.
- F139 simultaneous owner review: `NEEDS_FIX`. Placement audit обнаружил 33
  пересекающиеся пары исходных asset rectangles, 35 затронутых резиденций и 15
  containment; owner screenshots отдельно подтвердили source-terrain
  rectangles, наложения и обрезанные фрагменты.
- Visual-integration replacement SHA-256
  `3506161CB3593E8C16E1107AFE52E08306E4684221F34845557D9A29E050FDF9`
  отделяет boundary-connected natural terrain/background от authored focus и
  резервирует фактический protected footprint без разрешения пересечений.
  Full-modpack dedicated Large literal `for the worthy` worldgen дал
  `structures=61/61`, `uniquePresets=61/61`, `housing=61/61`, `rooms=229`,
  `relocatedChests=1`, unresolved overlaps `0`, `masterMode=True`,
  `getGoodWorld=True`, затем `Server started`; reload прошёл без errors. Frozen
  worldgen/reload log SHA-256:
  `A5A9071F9477CF1653A97C8B31DA52D97031DA0BF55A2FD81A3968FD59C738FF`
  и `E736D08ECD79763AE7D24625FE8A1DE50B9D49B6E30768C028E49B81D9926B3F`.
  Offline full-map и 61 region crops дали технический visual-isolation PASS,
  но не owner visual PASS.
- Destination-terrain / exact-mask successor SHA-256
  `1FBAFD913B1DC9B99533D931317B2626C7526D47C8158F3F6CA4273550F9BFB1`
  (1,677,026 bytes) supersedes `3506161C…`. Exact contract сохранил authored
  focus 61/61 byte-identical и все 9 compact homes, уменьшил natural
  terrain/background mask у 39/52 scene profiles и оставил 12,224 source
  natural cells destination terrain. Строгие broad rectangles двух
  промежуточных artifacts не смогли разместить 61/61 на numeric seed
  `1965240418` и были отвергнуты до mutation. Final planner использует broad
  protected rectangle только как prefilter и окончательно проверяет пересечение
  точных mutation masks. Full-modpack dedicated Large FTW Legendary worldgen
  дал `structures=61/61`, `uniquePresets=61/61`, `housing=61/61`, `rooms=271`,
  `relocatedChests=1`, unresolved persistent overlaps `0`, `masterMode=True`,
  `getGoodWorld=True`, 60 loaded available mods, затем `Server started`;
  frozen server log SHA-256
  `BD834758F5592978B6A75E44B1C11B8E3369F46409B90D667D679960606E4E22`.
  Reload также дошёл до `Server started` без worldgen markers и errors; frozen
  reload log SHA-256
  `DF2A84DF94E93B6398CF22374FF9BC04E9ACD6555D532C1281546F345BB23EEC`.
  Offline full-map, 61 region crops и before/after contact sheet не обнаружили
  cropped authored focus; это техническая sanity check, не owner visual PASS.
- Visual-focus-clearance successor SHA-256
  `5C48D3C540B53BF7054E66F93B1B3A7425A2FB045C37BDBB9EE51DD10BF64174`
  (1,679,651 bytes) supersedes `1FBAFD91…`. Exact mutation masks по-прежнему
  разрешают безопасно накладываться только unwritten source envelopes, но
  primary authored focus каждого кандидата теперь резервируется с 12-tile
  clearance против всех уже запланированных residence focus. Full-modpack
  dedicated Large literal `for the worthy` Legendary worldgen дал
  `structures=61/61`, `uniquePresets=61/61`, `housing=61/61`, `rooms=226`,
  `relocatedChests=0`, unresolved persistent overlaps `0`, `masterMode=True`,
  `getGoodWorld=True`, затем `Server started`; frozen main log SHA-256
  `F06B35276B55D0C7BE7082EAEABCC2370A433F01261983F55C0C7E5B7F5EFA93`.
  Reload main process дошёл до `Server started`, не запустил worldgen и не имел
  DingDong/error/fatal markers; audit log SHA-256
  `9C3F9C39E7309148E486B7D66A3B12CCFB123506297ABAE8BADF3FA54E5F2117`.
  Один silently-caught `EndOfStreamException` относится только к отдельно
  порождённому Subworld Library `-subworld 0` helper и записан отдельным логом.
  Offline full-map и 61 crops подтверждают целостность больших сцен, семи R7
  replacements и девяти compact homes; это технический sanity PASS, не owner
  visual PASS.
  Отдельный exact-current-artifact contract probe выдал 61/61 `focusBounds`.
  Независимый verifier сопоставил их с 61/61 фактическими placement rows v10 и
  проверил все 1,830 пар: `focusIntersections=0`,
  `paddedIntersections=0`, `clearancePerSide=12`, `minAxisGap=24`.
  Audit log SHA-256
  `9C242F261D1797C173C956308AF2AA5DFEDFD00A2A2D7BAF3E388B87A2700CE0`;
  rectangles/pairs TSV SHA-256
  `E3147BE6BB31C2A573F8BB9BDD3F26348C94D19822529B0828D66037B478A54B`
  и `0AEA22057E6991F5020E445CD104313F4ECCA4A463A11E4BFDAF6A0026ECD750`.

## Следующий единственный gate

Технические profiles, exact mutation masks, независимые biome envelopes и
persistent-object result записаны clean для 61/61; collision gate закрыт без
потери loot. F139 получил owner `NEEDS_FIX`, поэтому единственный активный gate
— владелец повторно проверяет новый `5C48D3C5…` simultaneous мир визуально на
Large FTW Legendary. Широкие исходные envelopes могут геометрически
пересекаться только там, где actual write masks не пишут ни одной общей клетки;
primary authored focus при этом имеют обязательный 12-tile clearance. Это не
заменяет ручную проверку визуальной дистанции между резиденциями. Ни
dedicated PASS, ни offline render сами по себе не
переводят строку в `APPROVED`; runtime manifest остаётся 0/61. Порядок 61 строк,
критерии и fail-closed результаты зафиксированы в
`PLAYER_VISUAL_ACCEPTANCE.md`. Семь R7 residences, девять compact
`SurfaceStructure` и остальные full scenes входят в те же 61 назначения;
provenance и source digests не изменены. Владелец отдельно проверяет compact
набор против запрета на процедурные коробки и недостаточно уникальные малые
дома, а также intrinsic edge fragments внутри самих авторских сцен.
