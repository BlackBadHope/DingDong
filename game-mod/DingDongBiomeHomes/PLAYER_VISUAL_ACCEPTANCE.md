# DingDong 2.0 — player visual acceptance ledger

Этот файл — каноническая очередь единственного активного gate 3F. Он не является
runtime manifest и сам по себе ничего не разрешает. Только явный ответ владельца
`PASS` или `FAIL` после просмотра сцены в основном tModLoader-клиенте может
изменить поле `Player result`. Codex основной клиент не запускает.

## Текущий owner verdict и replacement candidate

- Simultaneous F139 world: `NEEDS_FIX`. Owner screenshots зафиксировали жёсткие
  прямоугольные вставки source terrain/background, взаимные наложения
  самостоятельных резиденций и обрезанные фрагменты. Offline placement audit
  подтвердил 33 пары asset-area intersections, 35 затронутых резиденций и 15
  containment. Этот artifact не считается визуально принятым.
- Current replacement artifact SHA-256:
  `5C48D3C540B53BF7054E66F93B1B3A7425A2FB045C37BDBB9EE51DD10BF64174`
  (1,679,651 bytes); он supersedes `1FBAFD91…`. Loader оставляет source natural
  terrain/background destination-миру, сохраняя authored focus 61/61 и 9/9
  compact homes. Planner допускает пересечение только широких исходных
  envelopes в клетках, которые не записываются, и fail-closed отклоняет любую
  фактическую mutation-mask коллизию; primary authored focus разных резиденций
  дополнительно не могут пересекаться с 12-tile clearance. Full-modpack
  dedicated Large literal `for the worthy`
  Legendary worldgen/reload прошёл 61/61 structures, 61/61 unique presets,
  61/61 housing, 226 rooms, 0 relocated chests, 0 unresolved persistent
  overlaps, `masterMode=True`, `getGoodWorld=True`; основной reload-server не
  имел DingDong/error/fatal markers. Отдельный Subworld Library helper выдал
  один silently-caught `EndOfStreamException`, не связанный с DingDong.
- Статус replacement: `READY_FOR_OWNER_RETEST`, не `PASS`. Offline full-map и
  61 region renders подтверждают исчезновение прежних гигантских DingDong
  terrain-rectangles, отсутствие обрезки authored focus и разнесение спорных
  пар, но только владелец может принять визуальный результат в основном
  клиенте. Все 61 строки ниже остаются `PENDING`, approved manifest — `0/61`.
  Дополнительный pairwise audit текущего artifact сопоставил 61 exact contract
  focus с 61 фактическими v10 placements: все 1,830 пар проверены,
  `focusIntersections=0`, `paddedIntersections=0`, минимальный axis gap ровно
  24 tiles при 12-tile clearance с каждой стороны.

Для retest не нужны `/dingdong audit` или `/dingdong review`: достаточно
создать новый Large ForTheWorthy Legendary мир обычным способом, открыть карту
вашими привычными cheat-menu/map-reveal/cursor-teleport предметами и проверить
четыре вещи: жёсткие прямоугольники source terrain/background, реальную
перезапись одной резиденции другой, обрезанные крылья/края и визуальную
самостоятельность девяти compact homes. Скриншот и имя/номер проблемной
резиденции достаточны для следующей итерации.

## Зафиксированный технический baseline

- Режим: dedicated server, Large `8400x2400`, ForTheWorthy, Legendary
  (`masterMode=True`, `getGoodWorld=True`).
- Candidate mapping/housing: `61/61`; independent clean-site: `61/61`, overlaps 0.
- Текущий packaged canonical-review-id successor:
  `DingDongBiomeHomes-roadmap-3f-canonical-review-id-doc-successor-8C0317D6BCB4.tmod`,
  SHA-256
  `8C0317D6BCB4AC93B6110823604770E409EC34B9CE9A85941A921264642EB0BB`,
  1,634,704 bytes. Package scope относительно dedicated-tested predecessor:
  181/181 entries, ровно шесть ожидаемых doc/DLL изменений; normalized runtime
  semantic SHA-256 совпал по 30,866 lines:
  `3E07845D779506EF87D3BAB1EA1E8F90F6AACE558F6C5F86602B4E757C5D421A`.
  Scope log SHA-256:
  `40E833B23E4B601A5D3A8534B062DAD429C31E1284BCA327DAEA3B457FAD1BF7`.
- Functional canonical-review-id predecessor:
  `DingDongBiomeHomes-roadmap-3f-canonical-review-id-2FC9A5D5BE77.tmod`,
  SHA-256
  `2FC9A5D5BE773DFC3C03A276798F8B0AF2BC1980A322BA9B5C38E903F029E36D`,
  1,633,179 bytes. `/dingdong review` fail-closed сверяет persisted NPC/preset с
  canonical manifest и печатает однозначный `reviewId`/`ownerReply`, но не
  записывает verdict. Статический contract сопоставил все 61 identity/candidate/
  ledger rows; log SHA-256
  `9D29E5DD3509409B07DB700C588D9875B2A649CCBAB358E4405C1C9B8804D13E`.
  Dedicated Scarlet Large FTW Legendary вернул `reviewId=54`, canonical
  `Terraria/PartyGirl/R7_ScarletDevilMansion`, target `1266,408`,
  `focusReady=True`, `teleported=False`, `approved=0/61`, затем clean save/exit;
  frozen log SHA-256
  `81F565D68B6D7C754D32A44ABC6D4387EBD5ED6B8071914D08055FD73EB59586`.
- Safe-focus predecessor:
  `DingDongBiomeHomes-roadmap-3f-safe-focus-preflight-ED00AD072CB1.tmod`,
  SHA-256
  `ED00AD072CB169F5A63DE1ADB62B2E01116CE568AD5B486E36D92653397E7271`,
  1,631,469 bytes. Dedicated и single-player теперь используют один
  collision-aware safe-focus finder; dedicated рассчитывает target для hitbox
  `20x42`, но не телепортирует. Scope PASS: 181 entries, семь ожидаемых
  source/doc/PDB/DLL изменений, все 30,094 semantic lines вне
  `DingDongCommand` неизменны. Runtime semantic SHA-256:
  `4EE356F112EB242C90FC082DEB91152E5CC06A557DB279A99F00CFC709FE6B1D`.
  `focusReady=True` подтверждён на пяти persisted Large FTW Legendary clones:
  Scarlet `1266,408`, compact Greenhouse `6991,608`, sky CloudCity `7611,211`,
  Underground Clothier `4557,1100`, Underworld Castle `4135,2070`; все сохранили
  `playerVisualApproved=False`, `approved=0/61`.
- Player-review command predecessor:
  `DingDongBiomeHomes-roadmap-3f-player-review-command-9698BEE2DFE5.tmod`,
  SHA-256
  `9698BEE2DFE514C94D24B12535B93EB52DCEC3C067EFC4EF199962C36CAC33BE`,
  1,630,171 bytes. Он добавляет только fail-closed `/dingdong review` и
  `PLAYER_VISUAL_REVIEW.md` поверх technical-61 predecessor `5FCC…`; package
  scope и compiled semantic isolation PASS, все 30,094 semantic lines вне
  `DingDongCommand` неизменны. Новый runtime semantic SHA-256:
  `EBAB4ED36244E13603F3811F4887923AC05D18C97D64F8121A6AFEB588EA484C`.
  На сохранённом Scarlet Large FTW Legendary clone команда дважды, до и после
  save/reload, вернула тот же NPC/preset/area, `teleported=False` на dedicated,
  `playerVisualApproved=False`, `approved=0/61`.
- Technical-61 predecessor
  `5FCCBCA1D1811BFB0F28D5041E11BB2A1686CF86609FF63E81B2764A4BDB809D`
  runtime-identical непосредственно функционально проверенному predecessor
  `73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`:
  normalized semantic SHA-256
  `931D3A1A8B0650C8A1EE93B08A48F319AFEA48998B47B265488E68295F7FB363`
  совпал по 30,169 IL/metadata/resource lines. Successor отдельно прошёл один
  bounded dedicated load-smoke на Large FTW Legendary; frozen log SHA-256
  `EDED0329EB3E030B5D0762CB7860F4BA8586583F3FF3B12A8B31A0CBFBBB33D4`.
  Functional predecessor `73D3…` прошёл apply/reload всех Jungle, Ocean, Underground,
  Hallow, Mushroom и Underworld scenes ниже.
  Предыдущие R7/compact worlds были доказаны на predecessor
  `A146914D8C8CB41D531D6CC7D9A5BF358FFD0AE4D9D793E2AB83B6E4EA13DFC5`;
  узкая Greaveil asset-правка не меняла их geometry, а byte-identical Forest
   Architect прошёл apply/reload canary на функциональном artifact этой цепочки.
- Подготовлены семь отдельных single-use disposable worlds для всех семи R7
  residences, вошедших в mapping. Пути и hashes перечислены ниже.
- Все семь technical previews имеют apply/save/reload PASS и
  `loadedFromSave=True`; reload-room counts: Nadiki 6, Snow Mansion 6,
  Princess Castle 11, Castle Cool 18, Esonora 10, Scarlet 20, Unreal Delta 13.
  Это всё ещё не visual PASS.
- Все девять compact `SurfaceStructure` также подготовлены отдельными мирами и
  имеют apply/save/reload PASS; именно владелец решает, не являются ли они
  визуально слишком малыми, простыми или похожими друг на друга.
- Все девять оставшихся Forest full-scene residences также имеют отдельные
  apply/save/reload PASS worlds. Первый Brewer запуск обнаружил настоящий
  compatibility-exporter defect (`tile 726` оставался active из-за передачи
  TEdit `Tile` по значению); exporter исправлен через `ref`, все 13 Greaveil
  assets переэкспортированы fail-closed, а corrected Brewer дал 2/2 комнаты.
- Все четыре оставшиеся Snow residences также подготовлены отдельными мирами и
  имеют apply/save/reload PASS: Cyborg 1/1, corrected Greaveil Mechanic 5/5,
  Santa 1/1, Tax Collector 3/3.
- Все пять Desert residences также подготовлены отдельными мирами и имеют
  apply/save/reload PASS: Bandit 6/6, corrected Greaveil Arms Dealer 4/4,
  Dye Trader 8/8, Steampunker 3/3, Desert Acolyte 8/8.
- Все пять Jungle residences также подготовлены отдельными мирами и имеют
  apply/save/reload PASS: Jharim 5/5, Dryad 3/3, Painter 3/3,
  Witch Doctor 2/2, Druid 1/1.
- Все семь оставшихся Ocean residences также подготовлены отдельными мирами и
  имеют apply/save/reload PASS: Sea King 3/3, Jelly Priest 2/2,
  Fisherman 4/4, Pirate 2/2, Stylist 1/1, Diverman 2/2, Fishylady 1/1.
- Все пять оставшихся Underground residences также подготовлены отдельными
  мирами и имеют apply/save/reload PASS: Golem 5/5, Deeps 3/3, Clothier 1/1,
  Demolitionist 2/2, Goblin Tinkerer 1/1. Технический PASS не снимает отдельный
  визуальный вопрос о масштабе Clothier-сцены 60x105.
- Все семь оставшихся Hallow residences также имеют отдельные apply/save/reload
  PASS worlds: Oracle 2/2, Tavern 10/10, Nurse 3/3, Princess 4/4, Wizard 2/2,
  Spiritualist 1/1, Witch 4/4. R7 #51/#54 не повторялись.
- Последние Mushroom Truffle и две Underworld residences также имеют отдельные
  apply/save/reload PASS worlds: Truffle 3/3, Brimstone Witch 5/5,
  Abomination 4/4.
- Workshop sources и binaries: `PRIVATE_ONLY`.

## Правило приёмки

Для каждой строки владелец проверяет в игре как минимум: целостность внешнего
силуэта; отсутствие прямоугольной procedural clearing; естественность перехода
в terrain/background; доступность и целостность комнат; отсутствие обрезанных
важных объектов; соответствие NPC и целевому биому; визуальное отличие от всех
прочих принятых резиденций. При сомнении результат `FAIL` с короткой причиной.
Пошаговый безопасный workflow и клиентская команда `/dingdong review` описаны в
`PLAYER_VISUAL_REVIEW.md`.

Допустимые значения `Player result`: `PENDING`, `PASS`, `FAIL`. Значение
`READY_FOR_OWNER_REVIEW` в колонке `Preview` означает только наличие уже
подготовленного disposable мира. После явного решения владельца обновляются
ledger/evidence; `NpcAuthoredPresetManifest` остаётся fail-closed, пока весь
активный gate не закрыт.

## Подготовленный R7 review-set

Все миры — отдельные копии одного baseline Large FTW Legendary. В основном
клиенте внутреннее имя может отображаться как `DingDongHousingProbe-20260821`;
ориентироваться нужно по имени файла. `WLD/TWLD SHA-256` фиксируют сохранённое
состояние после reload-status и штатного выхода сервера.

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 5 | `R7_NadikiMansion` | `server-preview-r7-nadiki-probe\DingDongPreviewProbe-R7Nadiki-20260822.wld` | 6 | `1409C3E8A5D7DC19030606B59F11D7FE2AC06613C0A9E47D9D3F302AD484E210` | `6B645FFF83C318224E0F7D8645D7026F5D674CDDD3000701ED697B233C317C08` | `5A79266797903403EF6160C6CFD106E79151EF2D784AD2BD4A884CA8481E7EB8` | `24843E049D6C8405CC22CAE5347873A17A6374B74A69E21E933E06D7AC797DFF` |
| 8 | `R7_EsonoraHome` | `server-preview-r7-esonora-probe\DingDongPreviewProbe-R7Esonora-20260822.wld` | 10 | `9B79F398B908E610A97C49722967E6666494E8029A25BF3BB7B37CA50678B7E5` | `CE6EF788CA88B73EFB7815B280EC51BF104F4F9828B0DAB4B49061C353580A36` | `4386821BDCF18C8E3013D0121E64475C4FCA4F50DF888A220FE637624CB0AA3B` | `69BDE12F204D446D96177E2E76FBD962606C43BC9B1A67B0A3C057FFB7663AE8` |
| 9 | `R7_CastleCoolResidence` | `server-preview-r7-castlecool-probe\DingDongPreviewProbe-R7CastleCool-20260822.wld` | 18 | `C350DD7437E79DF5A1023AD5F4217D82E68CB9B2D15204F0AF1BBC3EB4AAE7E1` | `556FD557AE9E96E12A3BA14CA8051ED24006B9FBBE2CC07FBD8AFDB7CF9E340A` | `1C5416DB48514C72E66042425D84668B0D06F6D62767EDEA30C38C177A4D356B` | `196E43E772BA4F4EAAA09061587EFDD20E31DC72E6333249991FA71C4C032486` |
| 21 | `R7_SnowMansion` | `server-preview-r7-snowmansion-probe\DingDongPreviewProbe-R7SnowMansion-20260822.wld` | 6 | `201CFD2EEFA359E8CF319A97F074D72D2A968F19E0C8997392A1B926F87288E5` | `C2EC700ADE1AE73AC851424F8BA6FE7C7B78B4706C4DC29742809567FA51868D` | `8BEED23BA534EBFB3ECA6ED33E4AA4389DFD0FE4091F8332E7DFDCD8F0CF0BF0` | `2FE3DDE61D341772311044011594A77CE1741F9653D5788981D930939F4D8E19` |
| 49 | `R7_UnrealDeltaCastle` | `server-preview-r7-unrealdelta-probe\DingDongPreviewProbe-R7UnrealDelta-20260822.wld` | 13 | `17AB395D2D47FCCF4E223229E741FF7F3B7DF001983A938098B29515F7C3C4D0` | `C13F3423F91CA3909AE39AD41D03629D3DE7F76CDFBC3FA7A8376F6CD4E9C2B1` | `A65256F57FF8C33ADBCE3D4C01B9EC94E8C74CB7921ADDA0D3B6D068A1D9B2B1` | `6B4D0501FF9238CC5B430069E533A6887868E2343E23A6B61DBCFA0D3C2D9D35` |
| 51 | `R7_PrincessCastle` | `server-preview-r7-princesscastle-probe\DingDongPreviewProbe-R7PrincessCastle-20260822.wld` | 11 | `EDC3C7F3B684BB5D771613B137E0D6573E849713D300584548027C7B9106CAF1` | `31C2DAC7A8515648B70B8D10D8AA6EF84DB90FDCCAE6AA6A8948F4B2795A7D81` | `CE41E5F965A4887B279FEE62C6CFE7BD20B69A82F2E103A0D889892495B2814A` | `8155AB3C9632D818DFD4FC9D54DB41C029BB422B5B4674A38897F2B6BEB442B2` |
| 54 | `R7_ScarletDevilMansion` | `server-preview-scarlet-final-probe\DingDongPreviewProbe-ScarletFinal-20260822.wld` | 20 | `E2C793614295E02F9325AB3768FD148402C4FB0ACA3B769C15F627932A33A197` | `DE06E66196EAB36FF4C98D342D159998AC104E01BFCC1B6192E22B258734DB1E` | `4943ACDC0F535546E5F784E95A3185E0736C99B9B722A56E7253597E4F971D35` | `B42F34E1A7368D191E875C43DA47A61475F9AFB6089BF3557FE931B0A0261B60` |

## Подготовленный compact review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 1 | `Greenhouse1` | `server-preview-compact-greenhouse-probe\DingDongPreviewProbe-CompactGreenhouse-20260822.wld` | 1 | `C10834DA3A781B4FF6CB58CB7E34C01F45D9D1BEE8586EDC40655B2B983B6D35` | `61CEB1D69E06194F54F9E9EF8FB0317CA66022068B2F2FDFFCD9D51C5DC4EA30` | `F1ED145A18EEB65371B98BA2CF23FCE092801B04B6D090C2F7E9F792270C4D17` | `692FD1C1F2DE32A631F17E9267786AE11024987302866197483A23A1433BDD30` |
| 4 | `House3_Expination` | `server-preview-compact-house3-probe\DingDongPreviewProbe-CompactHouse3-20260822.wld` | 3 | `C9FD207DE36F10A49D66542BAE869B0EDAE6F7FE663AEA3A75512A3DD4DBC0FC` | `1F30B856B24F5B8F7C00588175D7C26125AEE10D388568AFE7C4D438A70A8213` | `0520B16BD2B1E8325897AF51B71FFB34DE345F5A47D8770B48797DA6A9EF13C8` | `B3B898850AC40398CF962B5D5E8EF2D9F72A66DF0E6B29CB38BC6A91A9817E80` |
| 6 | `MediumHouse1` | `server-preview-compact-mediumhouse-probe\DingDongPreviewProbe-CompactMediumHouse-20260822.wld` | 2 | `DD936D0E9C0758A786D5EC22DE28E5454D49634E8ABB277935B9BB524E7B34E8` | `97486BD984DF53A15D477A1648ED6A05E33F83FE3859DECCD91236DDF2BFF9A0` | `297346C4060E61494DF5A9E6F04854390BF3941A3F7C76D96545466522145135` | `4FD43B74D7E39AE273CB4691F144D95E7DFD002A66C829C7699222B452944EC8` |
| 7 | `SmallHouse1` | `server-preview-compact-smallhouse-probe\DingDongPreviewProbe-CompactSmallHouse-20260822.wld` | 1 | `AC53598F93A8040D77E5928E1FDCB24BA9851A7D54C95A16803B10498697CEAF` | `2C2B0737F33C0986053A594B2C36B14441EA64007A1F139EFBAC97F162AB4536` | `B1FC920E619BB44C8106ECE92390EF54EF0DDAAFB1DFC37D05C57965F3A94448` | `49CB602510A727E77B1E6A2FC70C0C701F6F6942FC7E5CDD05C638A8BE6029FA` |
| 15 | `House1_Expination` | `server-preview-compact-house1-probe\DingDongPreviewProbe-CompactHouse1-20260822.wld` | 2 | `8730BE0492711B88A7BACE3E26CB1D1409D849BF0ACF650B464D0BCA4B927DB3` | `0F6D35CFCA7374DBF9832C3B7EC1D764BD791033C10B3F2E8F459A396EC6255B` | `8E2FB6045DBF58DD60E1067CD46BAC568128B313737A39A10565DEC09E22786C` | `8B0BF8A7FD2E6D007B004B24B298B26EC09A13DAA8DB92EA8046697CD978399B` |
| 16 | `House2_Expination` | `server-preview-compact-house2-probe\DingDongPreviewProbe-CompactHouse2-20260822.wld` | 2 | `78447FD2A13AEF7AE5BE31CD7DABED4524ED4BC85CAFE271FCD227D11594351B` | `C01C1B34FC302A4EDBEE95B1753DB1BD377E0DFFBE2A43A97F9A77D36B75B069` | `05B1E5E143A4124B9F593F7DD12ECBAB696002BD8632F3857361F17BE2E4FA95` | `351CCED85FA811B1EEED86EA32DBE1220522DB2D53FAF8DC2CDAEC1A85B38034` |
| 18 | `LargeHouse1` | `server-preview-compact-largehouse-probe\DingDongPreviewProbe-CompactLargeHouse-20260822.wld` | 3 | `E59BAA2834634B061A1102AF592FDC7457E851CBBA26A91510E6328521B3AB5F` | `D71C35625833201A5B84447966927B65CA63BD472261D181CF6DE006F602EAF4` | `E7BD519D2C2E0B0BC0ED9676796448FF67A4ED3515FB4F3DD8D591F3BB2BD62D` | `53DCE60234472EE25A6EF4A95916946746399634E31F7BBDA151131B4E4BA634` |
| 20 | `Castle1` | `server-preview-compact-castle1-probe\DingDongPreviewProbe-CompactCastle1-20260822.wld` | 4 | `DB48F8BBB0EC51F652BBD14A815A4AE5B9E7FE9B37E9CB0E26B67047EA4CE94E` | `8984B23BD2A40E867B7CF338DADF1AAF991E969A421C5EC8E589BEFD418A8382` | `02F9E20DE5BF0DC7D7A48FF1DC0941337B89251D25B4A571666C5F0F402501A9` | `42E6EAC60638302CFC1C21E240B36B9A5E2B60CA84524531E1A781DA9038095E` |
| 39 | `FishingPond1` | `server-preview-compact-fishingpond-probe\DingDongPreviewProbe-CompactFishingPond-20260822.wld` | 2 | `9DDFE6819C0C8F2BC40D862061B3A97AAB09BB11DD9BA2A050E6B1AF0344A9F8` | `53DA5870EE100467B4C6FFCE33D87DD13B0E19DA5D969DE57F020CDA0C0F58B5` | `ED346A9BB226EA525AF307F1794F1C27BE1CDF31BCCC7A46EC920F33732FB6A6` | `CC9FF696114813D887B1D1E31631784784EB3250FCF4A0BEEFFD360F69F1EB1E` |

## Подготовленный Forest full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 2 | `Greaveil_SpawnForestStreet` | `server-preview-forest-architect-ref-fix-canary-probe\DingDongPreviewProbe-ForestArchitectRefFixCanary-20260822.wld` | 4 | `B7E5A4958C2FE6217BA101B48234B063BF241C36614BC6D7067ECE7AC10F6002` | `CAB4BAA87238C0DF6E5B658619FF297183FED39AA16A1771F8A4877BB19F96F4` | `B63BAAB0958FB00F0C06C066BF6FE435EE2B2F1DA23E1BACAFB5E25AB6BFA582` | `137A3DF7B9949BB3659399B6958C2B734C7F3328C5287EE0FA6A3E0AD3D7E6FE` |
| 3 | `Greaveil_WestBeachCottage` | `server-preview-forest-brewer-ref-fix-probe\DingDongPreviewProbe-ForestBrewerRefFix-20260822.wld` | 2 | `25726921F191CE6FA1663AFA56E53B9A3D3A80AD45BBA11ADEC4ABDBD1419774` | `50053D925E3F7DF1FB4CD3D6B1BFB0E5EAA9FECD0E0AA8559FD4172B2BDBECD2` | `4E12E69CB59FDF3CF8161EA5877FA4012245703A88FB3151B6F324D424462ACC` | `225F67799AEEAAD3BE2E5ADC45AA5F8EEB940F0205CDA0CD578E6D5D0C5F9DF8` |
| 10 | `Aegis8_SteampunkOrb` | `server-preview-forest-mutant-probe\DingDongPreviewProbe-ForestMutant-20260822.wld` | 3 | `0BE6A2E76316F091B1B08FD30F239503F09F164FE4BF41353FAAB5346AD99156` | `CDBF6BF3CF7C4A1EE7295A82E16760BBE743E50F8CB9B1030A48FB52F6B9E66E` | `2A0A0B391C592CCDBD67375A28E67F694F8CCD5560EF8FEF357E1CE17F95C745` | `CFCFF842BDD502C6EC98ED711291B35C164837E7FB326146C9A476DB60C33DDD` |
| 11 | `Aegis8_ForestTreehouse` | `server-preview-forest-squirrel-probe\DingDongPreviewProbe-ForestSquirrel-20260822.wld` | 3 | `F1186DDD0A9DB26F864131C128DD56771F326715B4B7A2D5ECCCFD871CC246E3` | `6DB0DE59BAE403DCBF692EAEE379408926BE8B2D8EF0C50FE93B24B78E4A2065` | `6B677DFB2EDD3B38E6B2A1040E5997E5392399CE2B77741F2B9C859CD6EE2069` | `E6B2DEE9D4D8DC98504B7117BA99D8235011946940393ECF8E40401BC5A21C11` |
| 12 | `Huadiao_ElementalSkyHouse` | `server-preview-forest-bestiary-probe\DingDongPreviewProbe-ForestBestiary-20260822.wld` | 2 | `1A8A996A0484E8BAD29ED1E5E3996D7030127C22A9557D9C7813E710FE793D44` | `B47652E670119FAD7D8693D1EE2E14F49DB5ABD21D89C175DECA96CF00976217` | `23D1E07989CEF8CA7CE6B556A0873090623E7EFA9E3A830D823AA2EB4BD3C3B0` | `00B5A3BDDBB9D31313E6A33C124CB14E09A4693920F8794E9392243F5E797A9E` |
| 13 | `Aegis8_MountainLodge` | `server-preview-forest-golfer-probe\DingDongPreviewProbe-ForestGolfer-20260822.wld` | 1 | `69412C6C879F125478ADE999234419933D312C9D3197FA86874B5590EBFA55C8` | `848EE76686217D13DA52CC9EBE1C34FB3E0237031C66D636EA02A2B9707E7036` | `7FC58984CBA224AD60F38B35030C856E6018867E23440646AAD69F4EC3F55669` | `1ABBEC7539251B61C12C6946565C5A3D7BED706D23C47B51A67F1BDED4F3063A` |
| 14 | `Greaveil_HillManor` | `server-preview-forest-guide-probe\DingDongPreviewProbe-ForestGuide-20260822.wld` | 3 | `E7626864C9903DC7B9C82330C01C0B614024E661B143E7CDD0E15ED649AD8C08` | `3EB6C42724F44163F2D2197AED72617211B8502D7B0A00F0EB3A4577CEE5A909` | `576D1FD381818E99530D08720CB2BCBDD3881020A0404FAADCFF7C4D9966F50C` | `8A9D270AB9ADD339408A06D6B855055CAA9B2CBFB564A780E3D49232B81A0AE0` |
| 17 | `Ziiio_JungleWatchtower` | `server-preview-forest-confused-probe\DingDongPreviewProbe-ForestConfusedZombie-20260822.wld` | 1 | `97EEF795DDD21DE380D98DFF75970B9663B3241BF56CA7CA3E88F94538732BB4` | `1756DCBF902AE058143DA9689CEF96FF0F973B685D0E0D6B2E60AD9892918621` | `2ACF1298EB0D5ADEA2D56F8021968618B2D159C4658AD243AE8C8A65790EBA40` | `847D85C98C52A1D955DC28E6D9D975B973BB03D91262075CBC1D751A79FBD00F` |
| 19 | `Greaveil_JungleEastHouse` | `server-preview-forest-tracker-probe\DingDongPreviewProbe-ForestTracker-20260822.wld` | 1 | `D1610BC0D2BCF9B2A6D89AF4962040B985371358069B5EB6B5B49CB6D1F5D280` | `3F94AC42BD65FB7087497E06129F9AC61C010105E5E31FD02F6B610D58C28A91` | `83FA858EAB35338EA3A572E727063A5A9B6579B4D609BEC8EC70B4E62DE7F2DD` | `36D88A76323431CAFB3D9F03BB0E4C69D7845C676DA5082FD2A82A95AAFF6EDE` |

## Подготовленный Snow full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 22 | `Ziiio_AirshipHouse` | `server-preview-snow-cyborg-probe\DingDongPreviewProbe-SnowCyborg-20260822.wld` | 1 | `B3CF1AB86EB18D62BA7A26D9A06065F196AD897650B6B771837DD4BF846B3BF4` | `09EDD5C7CD3E05715AE3E8DE606032EFE314CD08219B28460191546A78EB687E` | `95CE1BA1C25C4F76C5221DAC8C4EACB39111B602A1463E1296165B2F072DCDBC` | `1BA8618DD758A9E325EF33505CC2C05083E262A8DDD0D7202448A43C890C1555` |
| 23 | `Greaveil_SnowSteampunkManor` | `server-preview-snow-mechanic-probe\DingDongPreviewProbe-SnowMechanic-20260822.wld` | 5 | `6BB7E703C84C0CEFAB0407FA6CDA22F80757B438EA06C43A2F8181ECD4CAD9EE` | `08BA4AE025C81384CBA1B0BC523D8D50F84E0047E98136FC230D71A421AE9D5C` | `44C5466495F08C7E27B7A65FF8CCCE03348203EB4D6A35587742CECC575E40E7` | `3A85B4AD48358B76D501A7A94CD83ABEEC5E31415B85F3D9460DF2277C1F7904` |
| 24 | `Aegis8_SnowCastle` | `server-preview-snow-santa-probe\DingDongPreviewProbe-SnowSanta-20260822.wld` | 1 | `0599F3545ADBD2D22A5D4FD38B501E89BC1D89FAD241ED210FC51453C75570E1` | `03438AEFA7314F372FD668A51539153FA107703C7BBC2F4C4F7A115E4B2053EA` | `BA78BFD8345CA812C3E11E66232EDC3DBCEC1BF0908147C42A8F375161E400DC` | `DC8CE64F8ABA473649D0DA6627A045E961C13FB393172725AAF113BE3ABE16CD` |
| 25 | `Hannibal_SnowResidence` | `server-preview-snow-taxcollector-probe\DingDongPreviewProbe-SnowTaxCollector-20260822.wld` | 3 | `CAD30D36C921E79B554611DD8E39DAA4E3E2DEA85E712672C498E022F3DDD61A` | `900F822CD10FD8843AFC17D52E6546C761DF7D73DBF18036A773684190393DDB` | `CD9F1F6BE9FE923DEFD66E3043A97BA8872A46F6D285AF3C14E9F5B7F216AD3A` | `FCADE976BCCF81CE3331429F3DB2FFE5AB533A42B9AA524E702410226E211D12` |

## Подготовленный Desert full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 26 | `Aegis8_DesertOasisSettlement` | `server-preview-desert-bandit-probe\DingDongPreviewProbe-DesertBandit-20260822.wld` | 6 | `C76C807C40703E3F0FB274A4A97769A2D122909E79A6D54DBCF2E65DFAFC0D24` | `7369CD5AEF1B16FF1852F3B3CEFAE55BBBA00AB0DAA079B5C0F12D16B2E9B462` | `8742A07D7D8C9B123CF9023BC76F1F4F4D308FF5148975B91B9E0FA267E65103` | `EA81D804817C5F6EAA945ABB9CB09F8F0EF6510BF45D9B59B9CE8D4EAFF7D11C` |
| 27 | `Greaveil_DesertPalace` | `server-preview-desert-armsdealer-probe\DingDongPreviewProbe-DesertArmsDealer-20260822.wld` | 4 | `B1DF654041EBEDD427CBDC42542973D6E5BCD0CD6E2FD0E0CCAD96B3657FCA37` | `7179C420755E57FC1546AE05D9E138E02276AF3176BA65A2C0B291653618F7AD` | `F35BBE38799081D1181FDE7699B2BEEC9620633A31318B9B759BEBD40FC0DCF5` | `21976C92FA64E38987F2DB12F3B0E9DC55D3B352678F6D41482ADAAE43698F0A` |
| 28 | `Magoozy_Palace` | `server-preview-desert-dyetrader-probe\DingDongPreviewProbe-DesertDyeTrader-20260822.wld` | 8 | `66911C99654F234CE829F08E419750601F6D0BDE3A3D885629AC293FEB2B5E5F` | `E53E97F086B627C8C12E617C9F59C84BA06667644139F5EC20C9CFA37BA42973` | `A3A1F0EE2B2D8FE443B6765411D695367337B7615833C46789F064C69ED61EA3` | `E6C46DFAE94638BE8A7F617949E38EFECF29B97ACCA87BF48A3D35444EDD58F1` |
| 29 | `WoB_DesertPalace` | `server-preview-desert-steampunker-probe\DingDongPreviewProbe-DesertSteampunker-20260822.wld` | 3 | `45E30C9298D9DE8536A04CEF174E16F82672993951C6E16A005D68E5997DFBF4` | `81A21DE7D09E7B436A2CC8B6D884B53E90BF4F1E9E376674C457B303C1A785B5` | `154CADF57881CCCDEB4989CFAF64B21A653A6B5AF64847A845CF2887E52A39E5` | `0B435DB80CE9EDEA6CE1276C286A76E259EC958ACE2215D1D951407FDB92E428` |
| 30 | `Aegis8_DesertSkyPalace` | `server-preview-desert-acolyte-probe\DingDongPreviewProbe-DesertAcolyte-20260822.wld` | 8 | `1392F650FDEA9FE3802EDD8C1BB5ECA81436167FAE8E87B2ADCD431ED0FC8B7A` | `3FC134BA338CF714D35656665BDDEAF9D757579E274D3775484E777690D4C833` | `A6F10E43A5FE8FDCFC69E04F85C19E8B83EFBDA5FF4001CFE8E7678AECEB28CD` | `4A3A1CAD2F2DC7D1BCFCF1E661EDF7D6C8FA6C0F786F38026DEC4CE110A0B5A4` |

## Подготовленный Jungle full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 31 | `Aegis8_JungleTreeCity` | `server-preview-jungle-jharim-probe\DingDongPreviewProbe-JungleJharim-20260822.wld` | 5 | `7F02D8B0941B7B34461416A59D5D5A05F91EADB6F83144C8995ACB756C4C54EC` | `BBB879762CA036078420B9E13AE935125D7E99B7EBDDA348B076715EECF9D39C` | `A96D2775970527A149EB682EE49491FC678159A26059F074311EEB25EDF50225` | `2C660E3030B6269C0998972B3F8F806EC348A3219985945E28B64989843AB1BD` |
| 32 | `Huadiao_JungleGothicManor` | `server-preview-jungle-dryad-probe\DingDongPreviewProbe-JungleDryad-20260822.wld` | 3 | `DFDDF89769B917D3A6B4EECDF25CAB81BE0380454FAC6B820254B0C569CB1762` | `60E3863A65B7483270A9FA299DFC827F838655E9097269A8321A13F65F70D8AA` | `B56032B55BA0E846E91CCAAE8908A0911E5A1AED55D86DF22F14228DE7EC8D7C` | `FACDF78CE6CCBD4BB4122EEFDE4A71D322076F6CD0F50F136C6434B0A03A9AFC` |
| 33 | `Greaveil_JungleWestHouse` | `server-preview-jungle-painter-probe\DingDongPreviewProbe-JunglePainter-20260822.wld` | 3 | `4E2C77F97B7A151BE3CC7047F32BE0459AD4881F47D47732B203051FCF58CA9D` | `D39033385F14EFC7E8AA748A6783AFC6BAD061F917C13D98380E8A09D8FE0195` | `3E4B7CD1EA65267A196AFE9F5005E50A3B007D60C1203DD7F1899ED910E1F52B` | `F22AE15DAEB378A97EF459CAE30E5C99555430F2D399FBF031DF71C9F0E30B04` |
| 34 | `Ziiio_WitchDoctorHut` | `server-preview-jungle-witchdoctor-probe\DingDongPreviewProbe-JungleWitchDoctor-20260822.wld` | 2 | `88270F072D55F1290494DC742DE34F9EAA7FE5AD49563B7755AD5BD9BFD6EF4E` | `FF5D459666E8BB3E2BF252DD872CD22DA0C7E1D78B929432B65D9D30DC0477AA` | `B2EC99A9ED6FCE60857384DE8FEE5C8A5D6F12FA25077AED4983B1332BD1AA75` | `A1F8843E6112ADDFB68A18CA448AAC35050DE98553064564B2ACC2AD87EACE02` |
| 35 | `WoB_JungleTreehouse` | `server-preview-jungle-druid-probe\DingDongPreviewProbe-JungleDruid-20260822.wld` | 1 | `8348DF1F795D4DA74E4CA64DC076E0E2431F376ED1686E0A5460D4092AA36B6E` | `ACCE38B045890FF6123F7368EC5293943C952753AE75120DC29E1C51CB0EA2C4` | `A1F7D3AC6E9D92B89222F9BA0BABD8144BDDAD1DD4B9EF747600360FEA278E46` | `E05EABDCB9540F24BEF8E089DBAD67666F8AD012960238C090D73BB7D361A248` |

## Подготовленный Ocean full-scene review-set

Строка #39 Angler уже находилась в compact review-set и здесь не повторяется.

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 36 | `Greaveil_WestGalleon` | `server-preview-ocean-seaking-probe\DingDongPreviewProbe-OceanSeaKing-20260822.wld` | 3 | `65291A0C7F21C7C4CE43339A61861977338FB0C7E7584ED56753CC04F3B5099F` | `46B372052004280830E4D97878DD35780FADCD300CFEFA68A24D7E448E87886B` | `E06B088000E722BED599C713946BE1CE5A0A385A4A8B1C8CC6B8BCE19AF0D8A7` | `CA8F86B3A8305203C8B434CD9C026371906F0D40A1A777245B7750CE667FA6A3` |
| 37 | `WoB_HarborCenterHouse` | `server-preview-ocean-jellypriest-probe\DingDongPreviewProbe-OceanJellyPriest-20260822.wld` | 2 | `B3002428016C3E9EB728D3609CE9AAE833A2392FA467594AA0404B3892498D90` | `5A6F66862A4AE4440FC1D1FFEC2C265376F8F47C8D32F3795C5D9DD1CB4FB652` | `C41484B17DC043D049BD167B7CDE886255DB8E05960DE1BF68523EBB073C6506` | `D3CA3E9B1C6A37178C94D01B084A182F3AB257BF16F9BA92BB5FECEF2350CBCF` |
| 38 | `Aegis8Showcase_SnowHarborSettlement` | `server-preview-ocean-fisherman-probe\DingDongPreviewProbe-OceanFisherman-20260822.wld` | 4 | `39B6689CFD35FE361362BDA777798FF55ED5B1F4C259005F02BF5E3DF977EDE7` | `FE4A7AB91A8B062C13A17C5E2565B9E228C8AF8F30F01774F60FAABE06C16A90` | `0AE43DF592CA17DBF535A6F148C76FD1FE18691683C607504066367BD8F1CF79` | `CA59D4BDD696BD52A2DD97671930B10773AD131E2C9EE3EF265A1D7B2CBF9DF4` |
| 40 | `Aegis8Showcase_CloudCity` | `server-preview-ocean-pirate-probe\DingDongPreviewProbe-OceanPirate-20260822.wld` | 2 | `F086962DCF8C51A9B350B68B2A8E9207BE95F3976C791577C5791B15E9E56B6F` | `06E79B761D116CB23F983AC295AC2D96AA5A3831B598C83A4089208159FBCF17` | `96CD2482D05C8C169BA138775C496BC2EA2C97F4A83B8A171BF46C304FE2A1AF` | `4EB0EB168D08191DCE2F6FAACCB8AC0F6AFAB0414DCC3CE980A3BD8076BC2577` |
| 41 | `Ziiio_BarberShop` | `server-preview-ocean-stylist-probe\DingDongPreviewProbe-OceanStylist-20260822.wld` | 1 | `D812D378FB0535F6634479C2271F947FA4C6B87B4AEBF719898CDDD29D0ADA53` | `D9BD3F70DF81683C53378FB24FC4C64FFD56D82E32486371E746C8A1485FFC7C` | `62FD84DE076A5AC6BB1046A446FAF72124CF20D0F4505529B05F9CD85A18BDFB` | `19046AD7E96C9E4B1CFA9BB844200D335C933FB65230D62F5E0A10968F35E50B` |
| 42 | `Greaveil_WestLighthouse` | `server-preview-ocean-diverman-probe\DingDongPreviewProbe-OceanDiverman-20260822.wld` | 2 | `59E2F7D4027812C8B7D63FB82F409B8F981A67FB124E0D355A48330B74180DED` | `0901303A24F5452F309603D48CF276DBC7967F68DFD67453CA855203FB6E390E` | `B7EC24B758AB1620BD94D26C6BB2839BFE6D004404D2D6106ACF1B4E4C760497` | `7A84C6055ACE06C467F3C466EA00D57E1DB28A2F08A977A89325139FBEDA41C5` |
| 43 | `WoB_HarborWestHouse` | `server-preview-ocean-fishylady-probe\DingDongPreviewProbe-OceanFishylady-20260822.wld` | 1 | `390DD074D4265617FF20F5380D90C87926161FEB75B2502E308DC93E603863DB` | `1DF5737257B73948EF013593CACD519D7C7511D1F1A9FA53F6C5AA729DC1E443` | `93F2C613E6D9D185B84038A429D2AAC813B0627D84A8F7C68C1BBA8DD3BE59A7` | `1C1AF623FFBD35FE4A23B49FCFC7B7DDBFBFF1883389813EE8035F6DA2D37150` |

## Подготовленный Underground full-scene review-set

Строка #49 Blacksmith уже находится в R7 review-set и здесь не повторяется.

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 44 | `Aegis8Showcase_MoonCitadel` | `server-preview-underground-golem-probe\DingDongPreviewProbe-UndergroundGolem-20260822.wld` | 5 | `A327AA32E379AAA2573E0B758409A4959D4B91D5082AE6ADCD9315CBF2EF2DAF` | `43C351872F18E9663896AF43DF97A0BEA09D079AB746061813B35115491DCAD0` | `CCA9E996301F0D80CD3A5365FC2DDAFE5A3C919D4B8F675F9F736C33C2FE394C` | `5F5D90BC7C712B4C0DA27B074202BDE9C838C77BDBD586D4F1381945B4AA4DCB` |
| 45 | `Aegis8_CavernLakeManor` | `server-preview-underground-deeps-probe\DingDongPreviewProbe-UndergroundDeeps-20260822.wld` | 3 | `CE6CAE1738F6902E27F06098569AA7E2562A35293B2A935D6E0A257E22F605D5` | `23BFF8A1FF9097CA9049E7712A7803080F83F94369BCBCEB9166B221CCDB6405` | `57BDE56CE7A17DAAB6FADC5F4D701CC6F11DBC16A390B426D347FA43C2D95BEB` | `6B7506550185C168EAE631D8FCD11E39BADA5DD245452796987D2F13F7D7B147` |
| 46 | `Aegis8_DesertGothicManor` | `server-preview-underground-clothier-probe\DingDongPreviewProbe-UndergroundClothier-20260822.wld` | 1 | `C0E112EF5FDBBA0F96F48BCEC8FA266CFDD5CD2FDCD03DF58499F2B40D45E3AF` | `9CF21E02DAB098830CE4288ED5217EB055E8F6123C765D9B8060CB8677B6CCA4` | `7D65A26C8FF123CB2D301A54CB7BDD52651552413CCD798C170A0AD9C7F588C1` | `743314A6571F92876A2ECF8D079E6D7DE5BFFF54956911C3A2021D90DE11B0A6` |
| 47 | `Aegis8_CavernLakeSmithy` | `server-preview-underground-demolitionist-probe\DingDongPreviewProbe-UndergroundDemolitionist-20260822.wld` | 2 | `D422DD6243959B8F2149C05CEC9440B8AFA9E8DBB2D45EEDF964C74DC7E4510B` | `643EFEBB3C0CDA697ECF3776C486833C89749600FD0DBF2118756F8A70D681B9` | `F53AD9E01F349D9215192CB220DD66B9C4F5D1CA1AABB50B23873A36A3806823` | `9AD3CC7D5776807AB0E33A1A4DBC3507229CA746D0A9236168463D84EA126261` |
| 48 | `Ziiio_SteampunkerWorkshop` | `server-preview-underground-goblin-probe\DingDongPreviewProbe-UndergroundGoblin-20260822.wld` | 1 | `305E3B1EF357F67D9E9E07C8A8A3764AF89F6630F400A84F10809FDB7BF2C233` | `6EC43D82FC1D417BAAA4CBAC1CE0A7080BBBB8D3DE7D0372FDA0DDC39143E150` | `9C1B5B9BF477D2D88AF87DEDE899CD0D51C46475409230E45C9250551A0142CB` | `BD213715D7DF6D130BD6304B0FC4000B8DCD243BBCC2A343C4B27E5FB9668712` |

## Подготовленный Hallow full-scene review-set

Строки #51 Deviantt и #54 Party Girl уже находятся в R7 review-set и здесь не
повторяются.

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 50 | `Huadiao_CrescentVillage` | `server-preview-hallow-oracle-probe\DingDongPreviewProbe-HallowOracle-20260822.wld` | 2 | `B04AC9065DE871EE81A6CE920983AD1FD29375880DB319BDB2D0425E82AC1CC9` | `01A04FC64651254AF692F5970E31816A4273E0AD3311CECA1EA49598A858AD52` | `67700994936ECAF6B323AF0C6BE27781C7330E803FF03CBF4ACAFF05F3A98C6F` | `EAC77AC1BD03643E6C37CDD317EC1855C5F6F1E415EA7B76354F0BAE9F8C5391` |
| 52 | `Ziiio_Tavern` | `server-preview-hallow-bartender-probe\DingDongPreviewProbe-HallowBartender-20260822.wld` | 10 | `173A4C99432B67CBCAC52DA16EB307725A50F88E7F903F8BC876FDC50DFF31C9` | `F835899FFF032C3F00A2389FAD5F8ECEE332DD5E8090F280D33336603B3F9B66` | `BCF0427B08F46CE9CAE4743B52AF95B9B62E1BCB8553AE535E059A97FBFCC534` | `0EDE375479DD8BBEC5AAE3769721500B74D708B13758F78FA5FD75B2F3D6CAE7` |
| 53 | `Greaveil_SnowLodge` | `server-preview-hallow-nurse-probe\DingDongPreviewProbe-HallowNurse-20260822.wld` | 3 | `2510DAD4F52362737C05DE1FD2EE8289F160A920707875C7802C35AC5750586C` | `CDF76081B924BD2CEDC31F6E40C4A882F657B5FBDB9F68006DC0328F35DC5DE5` | `D3EDD2C0A01067F3D13AE9DF83216D7AF1A028F17C5F5989E9B24B3D8CE3D837` | `FC1053771274EF52E4611D0A3233EE08D21CFCAE84F2D4700CE1B45466ABDDA6` |
| 55 | `Aegis8_HallowCliffManor` | `server-preview-hallow-princess-probe\DingDongPreviewProbe-HallowPrincess-20260822.wld` | 4 | `61D38A01F1C3BA64094426781EDDADD0CCF00EE0048905065FD2B23F223D2040` | `6F634B473C26360BF79F850A06CA43A5FE29FE536338D68C184A08205F991857` | `A95F4F211C633C4BBD410415EDB5DA01611D648AF57309C4432101B6A90D254B` | `AC984914B6A63BDCC3C271A488EB0DE571CC66F8D726F9FF7A03688584B9B1BA` |
| 56 | `Greaveil_SpawnTreeTower` | `server-preview-hallow-wizard-probe\DingDongPreviewProbe-HallowWizard-20260822.wld` | 2 | `A89A1F4E5CC36010507265D6E983D8D05BF07332555AEC8E0E53C3C8A9D18846` | `3A7B4E10FEFC409C12E6486C93204F1551EA500238461A47CD71B1E87135A829` | `1730A519948DF3316AD2DAF2F44F84491E032718A91D93009352320B27E9ECF0` | `E001324A93A373B059A1513899FB34E9D87FED8C09B34C938CCBB6F98CF7BBF8` |
| 57 | `Aegis8_FloatingPagoda` | `server-preview-hallow-spiritualist-probe\DingDongPreviewProbe-HallowSpiritualist-20260822.wld` | 1 | `E56243298743E33652427AB20CBB22D6A7EB58259FF750BFCF9BFB02648DA6B3` | `8F9E79819F150762E9EC9115F0CB5D8932CF8680D7C62AEE014DAC9EE890213E` | `DF2448564453CE472F952E5DA3363FD6F2D3A6B5FFB8E2E1BF6AA2E3245E345B` | `62271F38758D9F923399D7E5D589344994388FF5674597ECDDB9BC9DC06ED53C` |
| 58 | `Aegis8_HallowRidgeVillage` | `server-preview-hallow-witch-probe\DingDongPreviewProbe-HallowWitch-20260822.wld` | 4 | `F779D0F565D989DA25EA1FCA46B82F17A6DA208226E80E36C74ABEABEBB9E93F` | `8F9CC27918B20B6B462432E67E6D21DDEE6F311C1A4560931DB24E8A48FD6AFB` | `FCC146401BF31259EEA1074C01CD807A0FE92EE1FFF8937E7EC2ABE05074A8DB` | `7A619B607314684F35E17850B66B380F83C35FA8EF6108D11042AC81FECC8041` |

## Подготовленный Mushroom full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 59 | `Greaveil_MushroomSpire` | `server-preview-mushroom-truffle-probe\DingDongPreviewProbe-MushroomTruffle-20260822.wld` | 3 | `43D519E2288DEAC08816AD1BA07A5763D26D3F9859ECF42B4C82121BDAD1CB5E` | `C242748C8C93ADF29FDEC1A51A363B8392135630D82B728B325FA7AC2E00FD9F` | `DAE56F8E1D0BEF53F885AF4BF6918312257675A56174A08FD378BD7A84457B3B` | `29E4C2C4F0B2212CEA3938371622914DD3FB5EB91A5E93C2697F65EB3F2C90CD` |

## Подготовленный Underworld full-scene review-set

| # | Preset | World file | Rooms after reload | WLD SHA-256 | TWLD SHA-256 | Apply log SHA-256 | Reload log SHA-256 |
| ---: | --- | --- | ---: | --- | --- | --- | --- |
| 60 | `Huadiao_FireSkyVillage` | `server-preview-underworld-brimstone-probe\DingDongPreviewProbe-UnderworldBrimstone-20260822.wld` | 5 | `D63796592EC2C33A3EC6649AF22EB4B4667D0CA976CBAC0D5C0E4AEEB17D1EF5` | `DD51C4B5821A595C212FD38E7C62238A95B6AEE82D1C5FFE6848F2E75BBB1D08` | `EB32CD05D0751BB9E27A2AE48384ADC432AAA19D9F0796941680824FF7961BB7` | `4BE705E08B5F28F2415DF80A84A296089CD6CFD6E9BEB02BBC15507B4EC2A2DA` |
| 61 | `Greaveil_UnderworldCastle` | `server-preview-underworld-abomination-probe\DingDongPreviewProbe-UnderworldAbomination-20260822.wld` | 4 | `823C8DD9C3B4A6CDC9D1899A5DFE0B857E7814C51054DC33A9BAF53FDE80B110` | `845786060914F1A919FD869B54F25347F92F5754086133440C713E71B3E3AC61` | `9B8E1C313F4FCCE3C1687E3BB03A58E226A4E77CC9E0446B8729D078E54E5F39` | `38E1A76329D43A332E6AEE9085D47215D84FBA71829DAF3089649FB94FDB0E55` |

## Очередь 61/61

| # | NPC key | Preset | Biome | Theme | Preview | Player result | Notes |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 1 | `AlchemistNPCLite/Alchemist` | `Greenhouse1` | Forest | Glasshouse Apothecary | READY_FOR_OWNER_REVIEW | PENDING | compact reload 1/1 |
| 2 | `AlchemistNPCLite/Architect` | `Greaveil_SpawnForestStreet` | Forest | Drafting Guildhall | READY_FOR_OWNER_REVIEW | PENDING | reload 4/4 |
| 3 | `AlchemistNPCLite/Brewer` | `Greaveil_WestBeachCottage` | Forest | Copper Kettle Brewery | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 2/2 |
| 4 | `AlchemistNPCLite/Jeweler` | `House3_Expination` | Forest | Gemcutter Atelier | READY_FOR_OWNER_REVIEW | PENDING | compact reload 3/3 |
| 5 | `AlchemistNPCLite/Musician` | `R7_NadikiMansion` | Forest | Woodland Concert Hall | READY_FOR_OWNER_REVIEW | PENDING | reload 6/6 |
| 6 | `AlchemistNPCLite/Operator` | `MediumHouse1` | Forest | Telegraph Exchange | READY_FOR_OWNER_REVIEW | PENDING | compact reload 2/2 |
| 7 | `AlchemistNPCLite/Tinkerer` | `SmallHouse1` | Forest | Clockwork Shed | READY_FOR_OWNER_REVIEW | PENDING | compact reload 1/1 |
| 8 | `AlchemistNPCLite/YoungBrewer` | `R7_EsonoraHome` | Forest | Young Brewer Herb Cottage | READY_FOR_OWNER_REVIEW | PENDING | reload 10/10 |
| 9 | `Fargowiltas/LumberJack` | `R7_CastleCoolResidence` | Forest | Timber Mill Lodge | READY_FOR_OWNER_REVIEW | PENDING | reload 18/18 |
| 10 | `Fargowiltas/Mutant` | `Aegis8_SteampunkOrb` | Forest | Mutagen Observatory | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 11 | `Fargowiltas/Squirrel` | `Aegis8_ForestTreehouse` | Forest | Acorn Canopy Lodge | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 12 | `Terraria/BestiaryGirl` | `Huadiao_ElementalSkyHouse` | Forest | Wildlife Sanctuary | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 13 | `Terraria/Golfer` | `Aegis8_MountainLodge` | Forest | Forest Clubhouse | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 14 | `Terraria/Guide` | `Greaveil_HillManor` | Forest | Guide's Library Lodge | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil wall remap; reload 3/3 |
| 15 | `Terraria/Merchant` | `House1_Expination` | Forest | Old Forest Trading Post | READY_FOR_OWNER_REVIEW | PENDING | compact reload 2/2 |
| 16 | `ThoriumMod/Cobbler` | `House2_Expination` | Forest | Cobbler Rowhouse | READY_FOR_OWNER_REVIEW | PENDING | compact reload 2/2 |
| 17 | `ThoriumMod/ConfusedZombie` | `Ziiio_JungleWatchtower` | Forest | Reclaimed Ivy Ruin | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 18 | `ThoriumMod/Cook` | `LargeHouse1` | Forest | Great Hearth Inn | READY_FOR_OWNER_REVIEW | PENDING | compact reload 3/3 |
| 19 | `ThoriumMod/Tracker` | `Greaveil_JungleEastHouse` | Forest | Ranger Watch Lodge | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 20 | `ThoriumMod/WeaponMaster` | `Castle1` | Forest | Weapon Master's Dojo | READY_FOR_OWNER_REVIEW | PENDING | compact reload 4/4 |
| 21 | `CalamityMod/Archmage` | `R7_SnowMansion` | Snow | Permafrost Observatory | READY_FOR_OWNER_REVIEW | PENDING | reload 6/6 |
| 22 | `Terraria/Cyborg` | `Ziiio_AirshipHouse` | Snow | Cryogenic Laboratory | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 23 | `Terraria/Mechanic` | `Greaveil_SnowSteampunkManor` | Snow | Boreal Gearworks | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 5/5 |
| 24 | `Terraria/SantaClaus` | `Aegis8_SnowCastle` | Snow | Northwind Toy Workshop | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 25 | `Terraria/TaxCollector` | `Hannibal_SnowResidence` | Snow | Frost Ledger Hall | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 26 | `CalamityMod/Bandit` | `Aegis8_DesertOasisSettlement` | Desert | Smuggler Casbah | READY_FOR_OWNER_REVIEW | PENDING | reload 6/6 |
| 27 | `Terraria/ArmsDealer` | `Greaveil_DesertPalace` | Desert | Dune Gunsmith Fort | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 4/4 |
| 28 | `Terraria/DyeTrader` | `Magoozy_Palace` | Desert | Prismatic Dye Souk | READY_FOR_OWNER_REVIEW | PENDING | reload 8/8 |
| 29 | `Terraria/Steampunker` | `WoB_DesertPalace` | Desert | Brass Sand Foundry | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 30 | `ThoriumMod/DesertAcolyte` | `Aegis8_DesertSkyPalace` | Desert | Sun Acolyte Temple | READY_FOR_OWNER_REVIEW | PENDING | reload 8/8 |
| 31 | `CalValEX/Jharim` | `Aegis8_JungleTreeCity` | Jungle | Jharim Jungle Shrine | READY_FOR_OWNER_REVIEW | PENDING | reload 5/5 |
| 32 | `Terraria/Dryad` | `Huadiao_JungleGothicManor` | Jungle | Living Grove House | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 33 | `Terraria/Painter` | `Greaveil_JungleWestHouse` | Jungle | Canopy Artist Studio | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 34 | `Terraria/WitchDoctor` | `Ziiio_WitchDoctorHut` | Jungle | Mahogany Spirit House | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 35 | `ThoriumMod/Druid` | `WoB_JungleTreehouse` | Jungle | Ancient Druid Conservatory | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 36 | `CalamityMod/SeaKing` | `Greaveil_WestGalleon` | Ocean | Tide King's Palace | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 3/3 |
| 37 | `CalValEX/JellyPriestNPC` | `WoB_HarborCenterHouse` | Ocean | Jellyfish Coral Chapel | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 38 | `FishermanNPC/Fisherman` | `Aegis8Showcase_SnowHarborSettlement` | Ocean | Old Fisherman's Smokehouse | READY_FOR_OWNER_REVIEW | PENDING | reload 4/4 |
| 39 | `Terraria/Angler` | `FishingPond1` | Ocean | Bright Bait Shack | READY_FOR_OWNER_REVIEW | PENDING | compact reload 2/2 |
| 40 | `Terraria/Pirate` | `Aegis8Showcase_CloudCity` | Ocean | Corsair Galleon House | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 41 | `Terraria/Stylist` | `Ziiio_BarberShop` | Ocean | Seabreeze Salon | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 42 | `ThoriumMod/Diverman` | `Greaveil_WestLighthouse` | Ocean | Diving Bell Laboratory | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 2/2 |
| 43 | `UnuBattleRods/Fishylady` | `WoB_HarborWestHouse` | Ocean | Mermaid Grotto House | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 44 | `MagicStorage/Golem` | `Aegis8Showcase_MoonCitadel` | Underground | Arcane Storage Vault | READY_FOR_OWNER_REVIEW | PENDING | reload 5/5 |
| 45 | `miningcracks_take_on_luiafk/Deeps` | `Aegis8_CavernLakeManor` | Underground | Deep Mine Station | READY_FOR_OWNER_REVIEW | PENDING | reload 3/3 |
| 46 | `Terraria/Clothier` | `Aegis8_DesertGothicManor` | Underground | Catacomb Tailor Hall | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1; owner visual size verdict required |
| 47 | `Terraria/Demolitionist` | `Aegis8_CavernLakeSmithy` | Underground | Cavern Blastworks | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 48 | `Terraria/GoblinTinkerer` | `Ziiio_SteampunkerWorkshop` | Underground | Goblin Mechanist Burrow | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 49 | `ThoriumMod/Blacksmith` | `R7_UnrealDeltaCastle` | Underground | Basalt Grand Forge | READY_FOR_OWNER_REVIEW | PENDING | reload 13/13 |
| 50 | `CalValEX/OracleNPC` | `Huadiao_CrescentVillage` | Hallow | Crystal Oracle Rotunda | READY_FOR_OWNER_REVIEW | PENDING | reload 2/2 |
| 51 | `Fargowiltas/Deviantt` | `R7_PrincessCastle` | Hallow | Prismatic Curio Boutique | READY_FOR_OWNER_REVIEW | PENDING | reload 11/11 |
| 52 | `Terraria/DD2Bartender` | `Ziiio_Tavern` | Hallow | Aether Defender Tavern | READY_FOR_OWNER_REVIEW | PENDING | reload 10/10 |
| 53 | `Terraria/Nurse` | `Greaveil_SnowLodge` | Hallow | Hallowed Sanctuary Clinic | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 3/3 |
| 54 | `Terraria/PartyGirl` | `R7_ScarletDevilMansion` | Hallow | Confetti Pavilion | READY_FOR_OWNER_REVIEW | PENDING | reload 20/20; one lossless relocated chest |
| 55 | `Terraria/Princess` | `Aegis8_HallowCliffManor` | Hallow | Rose Crystal Palace | READY_FOR_OWNER_REVIEW | PENDING | reload 4/4 |
| 56 | `Terraria/Wizard` | `Greaveil_SpawnTreeTower` | Hallow | Arcane Star Tower | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 2/2 |
| 57 | `ThoriumMod/Spiritualist` | `Aegis8_FloatingPagoda` | Hallow | Spirit Lantern Chapel | READY_FOR_OWNER_REVIEW | PENDING | reload 1/1 |
| 58 | `WeaponEnchantments/Witch` | `Aegis8_HallowRidgeVillage` | Hallow | Enchanter Coven House | READY_FOR_OWNER_REVIEW | PENDING | reload 4/4 |
| 59 | `Terraria/Truffle` | `Greaveil_MushroomSpire` | Mushroom | Luminous Spore Dome | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 3/3 |
| 60 | `CalamityMod/BrimstoneWitch` | `Huadiao_FireSkyVillage` | Underworld | Brimstone Witch Spire | READY_FOR_OWNER_REVIEW | PENDING | reload 5/5 |
| 61 | `Fargowiltas/Abominationn` | `Greaveil_UnderworldCastle` | Underworld | Ashen Abomination Monastery | READY_FOR_OWNER_REVIEW | PENDING | corrected Greaveil export; reload 4/4 |

Итог текущего checkpoint: `READY_FOR_OWNER_REVIEW=61`, `ON_DEMAND=0`,
`PASS=0`, `FAIL=0`, `PENDING=61`,
`NpcAuthoredPresetManifest=0/61`, `worldgenPlacementApproved=False`.
