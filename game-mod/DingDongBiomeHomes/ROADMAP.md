# DingDong: Biome Homes 2.0 roadmap

Цель проекта — превратить огромный For the Worthy мир DingDong в сеть разных,
устойчиво генерируемых биомных городов. Сервер является источником истины;
основной клиент нужен только для визуальной приёмки игроком.

## Обязательный контракт

- Сначала детерминированно планируется весь набор построек, затем мир изменяется.
- Один поддерживаемый NPC получает отдельную авторскую постройку в подходящем
  биоме. Изменение ширины, крыши, краски или набора опор не считается новой
  постройкой.
- Геометрия жилых и общественных зданий берётся из лицензированных авторских
  схем. Процедурный код допустим только для дорог, мягкой подготовки рельефа и
  защитных переходов между готовыми схемами.
- Любая подземная генерация считается POI-сценой. Отдельная схема дома без
  авторского окружения не размещается под землёй: обязательны естественный
  округлый силуэт пещеры, переходы в породу, фоновые стены и тематический декор.
- Запрещены raised-tier районы на длинных опорах, прямоугольные вырезы пещер и
  однотипные коробки как fallback. Если биом отсутствует или занят, применяется
  явно описанный seed-профиль, перенос этапа или честный skip в отчёте.
- Нельзя затирать защищённые структуры, TileEntity и чужие точки интереса.
- Отсутствующий или изменённый биом обрабатывается профилем seed, а не случайным
  переносом дома в неподходящее место.
- Генерация считается PASS только после headless Large For the Worthy worldgen,
  загрузки мира, `Server started` и нулевого числа ошибок мода.
- Для byte-identical билда полный gate повторно не запускается: используется SHA-256
  предыдущего PASS и короткий smoke-check.

## Этапы

1. **Стабильность worldgen (v0.7.11, технический PASS).** 61 назначенный
   житель и 61 валидное жильё на dedicated server. Визуальный результат этого
   поколения отклонён: параметрические коробки не являются авторскими домами.
2. **Наблюдаемое ядро (готово технически).** Server config, сохраняемый в мире отчёт,
   multiplayer sync и `/dingdong audit`.
3. **Авторский preset-каталог (перезапущено после visual FAIL v0.9.0).** Импорт
   лицензированных `.shstruct`, явное соответствие NPC -> авторская схема,
   корректные размеры/якоря и полный отказ от TownHouseBuilder/CivicHubBuilder.
   Runtime DingDong не зависит от `StructureHelper`: поддерживаемое подмножество
   формата читается собственным fail-closed loader, а NBT/custom data требуют
   отдельного явного импортера. Это исключает конфликт библиотек с Fargo.
4. **Биомные города.** Civic center, рынок, таверна, мастерская, площадь, дороги,
   декоративные переходы и безопасный pylon hub для каждого биома.
5. **Развитие города.** Ассортимент, декор и сервисы открываются по боссам,
   населению, Hardmode и локальному уровню поселения.
6. **Расширяемый NPC registry.** Vanilla/mod NPC adapters, blueprint packs и
   отчёт о неподдержанных NPC без процедурных fallback-домов.
7. **POI-фреймворк.** Авторские надземные и подземные сцены: руины, шахты,
   лаборатории, святилища, лагеря, арены, тайники и другие non-NPC POI. Сцена
   включает основную схему, background/terrain-маску, декор и защищённую зону.
8. **Живые поселения и POI.** Состояния `Dormant`, `Discovered`, `Activated`,
   `Restored`; условия активации, изменения декора/сервисов/спавнов, сохранение
   в мире и серверно-авторитетная синхронизация в multiplayer.
9. **Системный игровой слой.** Базовый прыжок 5 tiles; data-driven damage и
   resistance ecology для всех источников/целей модпака; summon affinity по
   способу получения; особые правила сегментированных противников; локальные
   `biome x depth` события; Town Atlas, POI/NPC quests, trust, shops и физическое
   escort-переселение без teleport fallback. Origin residence каждого NPC
   остаётся его единственной точкой respawn. Полный контракт и compatibility
   matrix находятся в `GAMEPLAY_SYSTEMS_2_0.md`.
10. **Приёмка 2.0.** Multiplayer join/rejoin, сохранение/перезагрузка, Large FTW
   seed matrix, конфликты структур, audit invariants и релизный headless gate.

## Живой checkpoint

Обновлено: 2026-08-22. Одновременно активен только один этап. Следующий этап не
начинается, пока текущий не достиг Definition of Done и его evidence не записан
здесь или в связанном intake-документе.

| Roadmap item | Статус | Definition of Done | Evidence / gate |
| --- | --- | --- | --- |
| Visual FAIL v0.9.0 cleanup | DONE | В исходниках отсутствуют процедурные жилые/civic коробки, raised-tier и прямоугольный underground fallback | Старые builders удалены; генератор fail-closed при неполном manifest и при отсутствии полной POI-сцены |
| 3. Авторский preset-каталог | **IN PROGRESS — единственный активный этап; sub-gate 3F (owner visual retest)** | 61 визуально различимая авторская постройка; для каждой записаны NPC, биом, автор, источник, право использования, digest, anchor, footprint и runtime-совместимость; ни одна palette/size-вариация не считается отдельной постройкой | `3E-R7 DONE`: candidate mapping и housing закрыты 61/61. F139 получил owner `NEEDS_FIX`; последующие `3506161C…` и `1FBAFD91…` устранили source-terrain rectangles и exact mutation-mask collisions. Текущий live successor `5C48D3C5…` дополнительно запрещает пересечение основных authored focus с 12-tile clearance, не запрещая безопасное пересечение широких unwritten source envelopes. Exact artifact прошёл full-modpack dedicated Large literal `for the worthy` Legendary worldgen: 61/61 residences/presets/housing, 226 rooms, 0 relocated chests, 0 unresolved overlaps, `masterMode=True`, `getGoodWorld=True`; main-world reload дошёл до `Server started` без DingDong/error/fatal markers. Offline full-map и 61 crops подтверждают целые authored scenes и визуальное разнесение, но не заменяют owner PASS. Artifact установлен live как `READY_FOR_OWNER_RETEST`; approved manifest остаётся 0/61. См. `AUTHORED_HOUSING_SERVER_BASELINE.md`, `AUTHORED_PLACEMENT_REVIEW.md` и `PLAYER_VISUAL_ACCEPTANCE.md` |
| Workshop private Home intake | IN PROGRESS — PRIVATE_ONLY | Нужные `.wld` загружены без основного клиента, разобраны headless и визуально проверены; публикация запрещена без разрешения автора | R7 добавил восемь независимых крупных резиденций из восьми Workshop-миров; tight crops совместимы с Terraria 1.4.4.9, экспорт проверен по SHA, housing 8/8 PASS. Семь закрывают replacement slots, `Mansion(almost)` остаётся резервом. Все SHA-256, размеры и права записаны в `ASSET_INTAKE.md` |
| 9. Системный gameplay layer | SPECIFIED — BLOCKED BY CURRENT GATE | 5-tile jump; объяснимая rock-paper-scissors damage matrix с `unknown=0`; summon acquisition affinity; segmented rules; локальные biome-depth events; 61 quest/trust/origin/escort policies; multiplayer persistence | Новый обязательный scope зафиксирован в `GAMEPLAY_SYSTEMS_2_0.md`. Текущий 67-mod compatibility включает `NothingImmune`, `SeveralfoldProjectiles`, `RandomLoot`, `WeaponEnchantments`, `SignatureEquipment`, Calamity, Thorium, SOTS и Fargo. Реализация не открывается до завершения предшествующих roadmap gates |
| 4–10 | BLOCKED BY CURRENT GATE | Соответствующий предыдущий этап имеет PASS | Реализация городов, POI-state и worldgen не начинается на фиктивном каталоге |

### Активный следующий gate

1. **3A–3D DONE:** source/rights registry, headless binary intake, compatibility
   remap audit и уникальное 61/61 NPC/biome/preset назначение. Одинаковая
   геометрия не учитывается дважды; private Home assets не готовятся к публикации.
2. **3E BASELINE DONE:** для каждого назначения измерить deployment anchor, housing capacity, protected
   footprint и допустимый biome/seed fallback. Подземные два Aegis8 candidates
   и четыре наземных здания, назначенных Underground NPC, проходят отдельный
   full-POI-scene gate с естественным background/terrain. Harbor/Floating/
   Underworld scenes имеют отдельные anchor references. Offline room-needs
   screening завершён, но его предположение о furniture-only ремонте опровергнуто
   реальным сервером. `WorldGen.StartRoomCheck` принял 45/61; 16/61 не имеют
   замкнутой room shell, а reserve дал лишь три тюремные камеры и три малых
   village-infill здания, непригодных как showcase residences.
3. **3E-R7 DONE:** восемь новых standalone residences из восьми независимых
   PRIVATE_ONLY Workshop-источников прошли tight visual crop,
   compatibility/digest, `.shstruct` export и реальный housing probe на
   сохранённом disposable Large For the Worthy Legendary мире. Результат 8/8,
   loader errors 0; семь закрыли оставшиеся replacement slots, один оставлен
   резервом. Candidate mapping теперь 61/61 distinct server-PASS geometries.
4. **3F CLEAN-SITE PASS:** `AuthoredCandidateDeploymentManifest` построил 61/61
   distinct deployment profiles. Dedicated contract на Large FTW Legendary
   подтвердил 9 compact / 52 scene masks и approved 0/61. Exact
   NullBlock/NullWall write-mask site gate первоначально локализовал один vanilla
   chest в protected footprint Scarlet. `PersistentChestRelocator` сначала
   создал destination `1946,710`, полностью перенёс пять item slots и имя,
   подтвердил signature `98D02B33E744F6A7`, и только затем освободил source
   `1496,622`. Save/reload readback подтвердил source absent, destination present;
   повторный independent site gate дал `cleanProfiles=61`, overlaps 0. Артефакт
   clean-site proof SHA-256:
   `FF211DBE9BC6E57BC867F4B1DCEF5B1CFF0330146C3FE3F4F4308DDFC0BBFB3A`;
   apply/site log SHA-256:
   `27EA33345DC409CB474F60D0591CBF4E135806A7EE1FD3EE175C71CFC1FA4159`;
   reload/site log SHA-256:
   `EF7C5F3B3B7035F3D2FE3FE1778A722773E931DC229C6A920C1BCC71F11C9A67`.
5. **3F PREVIEW PREDECESSOR PASS:** fail-closed one-scene preview артефакта
   SHA-256
   `A146914D8C8CB41D531D6CC7D9A5BF358FFD0AE4D9D793E2AB83B6E4EA13DFC5`
   реально разместил `R7_ScarletDevilMansion` на disposable Large FTW Legendary,
   получил 20 валидных комнат, lossless перенёс один сундук и сохранил marker в
   `.twld`. Reload-status подтвердил `loadedFromSave=True` и `20/20` комнат.
   Apply/reload log SHA-256 соответственно:
   `4943ACDC0F535546E5F784E95A3185E0736C99B9B722A56E7253597E4F971D35` и
   `B42F34E1A7368D191E875C43DA47A61475F9AFB6089BF3557FE931B0A0261B60`.
6. **3F FOREST TECHNICAL PASS:** после семи R7 и девяти compact worlds
   подготовлены ещё девять отдельных Forest full-scene worlds. Brewer впервые
   дал реальный framing FAIL: compatibility exporter сообщал
   `tile726->empty:3`, но `Tile` передавался по значению и клетки оставались
   active. Exporter исправлен через `ref`; все 13 Greaveil assets переэкспортированы
   из pinned world, пять non-remap assets остались byte-identical, восемь remap
   assets ожидаемо сменили digest, active out-of-range IDs = 0. Текущий
   dedicated-tested artifact:
   `DingDongBiomeHomes-roadmap-3f-greaveil-ref-fix-forest25-pass-0FA2159F6085.tmod`,
   SHA-256
   `0FA2159F608518E5154454938624D2298CC428B892BAA71455973B39B450E336`.
   Все девять Forest scenes дали apply/save/reload PASS; точные world/log hashes
   записаны в `PLAYER_VISUAL_ACCEPTANCE.md`.
7. **3F SNOW TECHNICAL PASS:** artifact
   `DingDongBiomeHomes-roadmap-3f-forest25-doc-smoke-6E6164AE4660.tmod`,
   SHA-256
   `6E6164AE46609D7B5EB11CB65EAEF7E020C64A65F90588A616D432E9FACDA814`,
   сначала доказан semantic-runtime-identical Forest25 predecessor, затем
   функционально проверен четырьмя новыми Snow scenes. Cyborg, corrected
   Greaveil Mechanic, Santa и Tax Collector дали соответственно 1/1, 5/5, 1/1
   и 3/3 комнат после save/reload; exact world/log hashes записаны в
   `PLAYER_VISUAL_ACCEPTANCE.md`.
8. **3F DESERT TECHNICAL PASS:** artifact
   `DingDongBiomeHomes-roadmap-3f-snow29-doc-smoke-51ACC72CE865.tmod`, SHA-256
   `51ACC72CE865CDCB736151704204A000AB6B953E216C1023630F148830D1F3F5`,
   функционально проверен пятью Desert scenes. Bandit, corrected Greaveil Arms
   Dealer, Dye Trader, Steampunker и Desert Acolyte дали соответственно 6/6,
   4/4, 8/8, 3/3 и 8/8 комнат после save/reload; exact world/log hashes
   записаны в `PLAYER_VISUAL_ACCEPTANCE.md`.
9. **3F JUNGLE TECHNICAL PASS:** artifact
   `DingDongBiomeHomes-roadmap-3f-desert34-doc-smoke-73D3EA7F825F.tmod`, SHA-256
   `73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`,
   функционально проверен пятью Jungle scenes. Jharim, Dryad, Painter, Witch
   Doctor и Druid дали соответственно 5/5, 3/3, 3/3, 2/2 и 1/1 комнат после
   save/reload; exact world/log hashes записаны в `PLAYER_VISUAL_ACCEPTANCE.md`.
10. **3F OCEAN TECHNICAL PASS:** тот же функциональный artifact SHA-256
    `73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`
    проверен семью оставшимися Ocean scenes. Corrected Greaveil Sea King,
    Jelly Priest, Fisherman, Pirate, Stylist, corrected Greaveil Diverman и
    Fishylady дали соответственно 3/3, 2/2, 4/4, 2/2, 1/1, 2/2 и 1/1 комнат
    после save/reload; exact world/log hashes записаны в
    `PLAYER_VISUAL_ACCEPTANCE.md`. Angler #39 уже был доказан compact gate и не
    тестировался повторно.
11. **3F UNDERGROUND TECHNICAL PASS:** тот же функциональный artifact SHA-256
    `73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`
    проверен пятью оставшимися Underground scenes. Golem, Deeps, Clothier,
    Demolitionist и Goblin Tinkerer дали соответственно 5/5, 3/3, 1/1, 2/2 и
    1/1 комнат после save/reload; exact world/log hashes записаны в
    `PLAYER_VISUAL_ACCEPTANCE.md`. Blacksmith #49 уже был доказан R7 gate и не
    тестировался повторно. Clothier 60x105 остаётся предметом owner visual gate.
12. **3F HALLOW TECHNICAL PASS:** тот же artifact проверен семью оставшимися
    Hallow scenes. Oracle, Tavern, corrected Greaveil Nurse, Princess, corrected
    Greaveil Wizard, Spiritualist и Witch дали 2/2, 10/10, 3/3, 4/4, 2/2, 1/1
    и 4/4 комнат после save/reload. R7 #51/#54 не повторялись.
13. **3F MUSHROOM/UNDERWORLD TECHNICAL PASS:** тот же artifact проверен тремя
    последними scenes: corrected Greaveil Truffle 3/3, Brimstone Witch 5/5,
    corrected Greaveil Abomination 4/4 после save/reload. Технические persisted
    review worlds теперь готовы 61/61; exact hashes находятся в ledger.
14. **3F TECHNICAL-61 PACKAGE PASS:** docs-only successor
    `DingDongBiomeHomes-roadmap-3f-technical61-doc-smoke-5FCCBCA1D181.tmod`,
    SHA-256
    `5FCCBCA1D1811BFB0F28D5041E11BB2A1686CF86609FF63E81B2764A4BDB809D`,
    имеет тот же runtime semantic SHA-256
    `931D3A1A8B0650C8A1EE93B08A48F319AFEA48998B47B265488E68295F7FB363`
    по 30,169 canonical lines, что и функционально проверенный `73D3…`.
    Отдельный bounded dedicated load-smoke на Large FTW Legendary дошёл до
    `Server started` и штатного save/exit; frozen log SHA-256
    `EDED0329EB3E030B5D0762CB7860F4BA8586583F3FF3B12A8B31A0CBFBBB33D4`.
15. **3F PLAYER-REVIEW TOOL PASS:** fail-closed successor
    `DingDongBiomeHomes-roadmap-3f-player-review-command-9698BEE2DFE5.tmod`,
    SHA-256
    `9698BEE2DFE514C94D24B12535B93EB52DCEC3C067EFC4EF199962C36CAC33BE`,
    добавил `/dingdong review` и packaged owner runbook. Exact package delta —
    8 ожидаемых source/doc/PDB/DLL entries; все 30,094 compiled semantic lines
    вне `DingDongCommand` неизменны. На disposable Scarlet Large FTW Legendary
    clone dedicated apply/reload дважды вернул один target
    `Terraria/PartyGirl/R7_ScarletDevilMansion`, area `1264,400,650,350`,
    `playerVisualApproved=False`, `approved=0/61`. Клиентская teleport-ветка не
    запускалась Codex и остаётся предметом owner visual use.
16. **3F SAFE-FOCUS PREFLIGHT PASS:** successor
    `DingDongBiomeHomes-roadmap-3f-safe-focus-preflight-ED00AD072CB1.tmod`,
    SHA-256
    `ED00AD072CB169F5A63DE1ADB62B2E01116CE568AD5B486E36D92653397E7271`,
    использует один collision-aware finder в dedicated preflight и
    single-player teleport. Exact scope: 181 entries, семь ожидаемых изменений,
    30,094 semantic lines вне `DingDongCommand` неизменны. Пять разнородных
    persisted Large FTW Legendary clones дали `focusReady=True`: Scarlet large,
    Greenhouse compact, CloudCity sky/ocean, Clothier underground и Underworld
    Castle; каждый сохранил `playerVisualApproved=False`, `approved=0/61`.
17. **3F CANONICAL REVIEW-ID PASS:** functional successor
    `DingDongBiomeHomes-roadmap-3f-canonical-review-id-2FC9A5D5BE77.tmod`,
    SHA-256
    `2FC9A5D5BE773DFC3C03A276798F8B0AF2BC1980A322BA9B5C38E903F029E36D`,
    fail-closed связывает persisted preview NPC/preset с канонической строкой
    visual ledger. Static contract доказал все 61 identity/candidate/ledger
    соответствия. Dedicated Scarlet Large FTW Legendary вернул `reviewId=54`,
    `ownerReply=54:PASS-or-FAIL-with-reason`, target `1266,408`,
    `focusReady=True`, `approved=0/61`, затем clean save/exit. Contract/runtime
    log SHA-256: `9D29E5DD3509409B07DB700C588D9875B2A649CCBAB358E4405C1C9B8804D13E`
    и `81F565D68B6D7C754D32A44ABC6D4387EBD5ED6B8071914D08055FD73EB59586`.
    Current packaged successor
    `DingDongBiomeHomes-roadmap-3f-canonical-review-id-doc-successor-8C0317D6BCB4.tmod`,
    SHA-256
    `8C0317D6BCB4AC93B6110823604770E409EC34B9CE9A85941A921264642EB0BB`,
    отличается ровно шестью ожидаемыми doc/DLL package entries и сохранил
    normalized runtime semantic SHA-256 по 30,866 lines:
    `3E07845D779506EF87D3BAB1EA1E8F90F6AACE558F6C5F86602B4E757C5D421A`.
    Scope log SHA-256:
    `40E833B23E4B601A5D3A8534B062DAD429C31E1284BCA327DAEA3B457FAD1BF7`.
18. **3F FULL-MODPACK REVIEW-REGISTRY PASS — SUPERSEDED FOR SEED ROBUSTNESS:** от dedicated-tested baseline
    `2918B134F65CAB331CEC98985EDF5D30BE114D409FDAC620AB207C35485AD43B`
    собран узкий successor
    `C159E32D359EC3430444EA0C8DC5B309DE4C787F390E5A6C5C2590EC9F42695F`.
    Package delta ограничен DLL/PDB, `DingDongCommand.cs`,
    `SettlementWorldState.cs` и `PLAYER_VISUAL_REVIEW.md`; все 125
    `.shstruct`, loader, planner и builder байт-в-байт совпадают с baseline.
    Static semantic gate разрешил изменения только в review command и
    report-v3 residence registry. На disposable Large FTW Legendary с точной
    копией пользовательского `enabled.json` (67 entries, SHA-256
    `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`)
    full-modpack server дал `structures=61/61`, `uniquePresets=61/61`,
    `housing=61/61`, `rooms=269`, `reviewTargets=61/61`, 14 lossless chest
    relocations и 0 unresolved overlaps. Reload сохранил audit PASS и
    `/dingdong review 1`/`61` вернули `READY`, `loadedFromSave=True`,
    `focusReady=True`. Exact PASS artifact установлен live без изменения
    `enabled.json`; основной клиент Codex не запускал. Это simultaneous
    residence-placement PASS, но не этап-4 city/road/civic acceptance и не
    owner visual PASS. Последующая реальная генерация владельца с UI seed `123`
    (numeric seed `106748011`) доказала, что этот PASS был seed-specific:
    planner нашёл 61/61, но поздно отверг stale/unsupported `Main.chest` record
    на tile type `2797`; остальные три bounded layout attempts исчерпали
    поверхность. Ошибка была до mutation (`mutated=0`). Артефакт `C159…` больше
    не считается текущим working-world доказательством.
19. **3F EXACT-SEED FULL-MODPACK REGRESSION PASS:** planner теперь использует
    `Main.ActiveWorldFileData.Seed`, то есть тот же numeric seed, которым создан
    terrain, а не произвольный UI `SeedText`. Unsupported/non-2x2 chest records
    отбрасываются при выборе candidate site и не могут обрушить уже полный
    61-site relocation batch. Новый artifact SHA-256
    `F139103D029C42DDA3C92559AAEDE00D1952C1E8EB665529B176FEAA72985308`
    проверен с теми же 67 enabled-модами на disposable Large FTW Legendary,
    exact numeric seed `106748011`: `structures=61/61`, `uniquePresets=61/61`,
    `housing=61/61`, `rooms=259`, `relocatedChests=8`, unresolved overlaps `0`,
    errors `0`, затем `Server started`. Reload audit подтвердил
    `houses/themes/residents/housing=61/61`, `reviewTargets=61/61`. Frozen
    worldgen/reload server log SHA-256:
    `380CD6F7941576A117E0A87162C2BA951BFF497A34C505012CDFC48E0F24CC9F` и
    `EAF1A516C156F07A8C0F3A63E0B2996D2058DEA65A3E169E98F66ACE5A067FA1`;
    reload audit transcript SHA-256:
    `AE617AA14C72CA1D3FAC1028D7B769DB6EC8390966E541068EB750E16F313BA4`.
    Artifact установлен live; `enabled.json` остался byte-identical SHA-256
    `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
20. **3F OWNER VISUAL VERDICT — NEEDS_FIX (F139):** просмотр реального
    simultaneous мира выявил системные дефекты интеграции: жёсткие
    прямоугольные куски source terrain/background, наложения самостоятельных
    резиденций и обрезанные дальние части сцен. Независимый разбор 61 placement
    rectangles подтвердил 33 пары asset-area intersections, 35 затронутых
    резиденций и 15 полных containment. Поэтому технический PASS F139 не принят
    визуально и не может переводить ни одну строку в `APPROVED`.
21. **3F ACTIVE — VISUAL-INTEGRATION SUCCESSOR READY FOR OWNER RETEST:** runtime
    loader теперь отделяет boundary-connected natural terrain/background от
    архитектуры, выбирает основной furnished residence focus и мутирует только
    его bounded footprint. Planner резервирует интегрированный protected
    footprint и безусловно отклоняет любое пересечение. Artifact SHA-256
    `3506161CB3593E8C16E1107AFE52E08306E4684221F34845557D9A29E050FDF9`
    (1,664,173 bytes) прошёл full-modpack dedicated worldgen с 67 enabled-модами
    на Large literal `for the worthy`: `structures=61/61`,
    `uniquePresets=61/61`, `housing=61/61`, `rooms=229`, `relocatedChests=1`,
    unresolved overlaps `0`, `masterMode=True`, `getGoodWorld=True`, затем
    `Server started`. Reload снова дошёл до `Server started` без errors.
    Frozen worldgen/reload log SHA-256:
    `A5A9071F9477CF1653A97C8B31DA52D97031DA0BF55A2FD81A3968FD59C738FF`
    и `E736D08ECD79763AE7D24625FE8A1DE50B9D49B6E30768C028E49B81D9926B3F`.
    Offline render полной карты и 61 отдельных region crops не показывает
    прежних гигантских DingDong terrain-rectangles или пересекающихся protected
    footprints, но это только техническая проверка. Единственный активный gate
    остаётся owner visual retest нового мира; `NpcAuthoredPresetManifest`
    остаётся 0/61. Artifact установлен live byte-identical; предыдущий F139
    сохранён в backup, а live `enabled.json` остался byte-identical SHA-256
    `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
    Stage 4 не открыт.
22. **3F ACTIVE — DESTINATION-TERRAIN / EXACT-MASK SUCCESSOR READY FOR OWNER
    RETEST:** `3506161C…` superseded artifact SHA-256
    `1FBAFD913B1DC9B99533D931317B2626C7526D47C8158F3F6CA4273550F9BFB1`
    (1,677,026 bytes). Loader suppresses source natural tile/wall cells only for
    52 terrain-scene profiles: exact contract preserved authored focus 61/61
    byte-identical and all 9 compact homes, reduced 39/52 scene masks and left
    12,224 natural source cells to destination terrain. Two intermediate
    artifacts (`F8BAFADB…`, `5D9EE3FA…`) were rejected before any mutation when
    strict broad rectangles could not place all 61 on deterministic numeric
    seed `1965240418`; neither was installed live. Final planner uses broad
    protected rectangles only as a cheap prefilter and rejects placement on an
    exact mutation-mask intersection. Full-modpack dedicated Large FTW
    Legendary worldgen for `1FBAFD91…` passed:
    `structures=61/61`, `uniquePresets=61/61`, `housing=61/61`, `rooms=271`,
    `relocatedChests=1`, unresolved persistent overlaps `0`,
    `masterMode=True`, `getGoodWorld=True`, 60 loaded available mods, then
    `Server started`; frozen server log SHA-256
    `BD834758F5592978B6A75E44B1C11B8E3369F46409B90D667D679960606E4E22`.
    Saved-world reload reached `Server started` with zero worldgen markers and
    zero errors; frozen reload log SHA-256
    `DF2A84DF94E93B6398CF22374FF9BC04E9ACD6555D532C1281546F345BB23EEC`.
    Final disposable WLD/TWLD SHA-256 after reload:
    `FBE76E46E772C61BE9D671DCC0CDB4B74C2A08850CF92AC30AD019FC93A784B7`
    and `EBECEE4D75CA95863E59AAC58A9E386031E5CB295C2AC8D48B28A656B295DF93`.
    Offline full-map, 61 region crops and before/after contact sheet are a
    technical visual sanity check only: broad source envelopes may overlap in
    cells that are intentionally not written, while actual write-mask
    intersections are zero. Artifact installed live byte-identical; prior
    `3506161C…` is backed up, `enabled.json` remains byte-identical SHA-256
    `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
    Owner visual retest is still the only active gate, approved manifest is
    0/61, and Stage 4 remains closed.
23. **3F ACTIVE — VISUAL-FOCUS CLEARANCE SUCCESSOR READY FOR OWNER RETEST:**
    `1FBAFD91…` superseded artifact SHA-256
    `5C48D3C540B53BF7054E66F93B1B3A7425A2FB045C37BDBB9EE51DD10BF64174`
    (1,679,651 bytes). Planner сохраняет exact mutation-mask collision gate и
    дополнительно запрещает пересечение primary authored focus rectangles с
    12-tile clearance. Это разносит самостоятельные резиденции визуально, но не
    превращает широкие unwritten source envelopes в ложные занятые области и не
    обрезает сами авторские сцены. Exact artifact прошёл full-modpack dedicated
    Large literal `for the worthy` Legendary worldgen:
    `structures=61/61`, `uniquePresets=61/61`, `housing=61/61`, `rooms=226`,
    `relocatedChests=0`, unresolved persistent overlaps `0`,
    `masterMode=True`, `getGoodWorld=True`, 60 locally available mods, затем
    `Server started`. Frozen worldgen server log SHA-256
    `F06B35276B55D0C7BE7082EAEABCC2370A433F01261983F55C0C7E5B7F5EFA93`.
    Saved-world reload main process снова дошёл до `Server started`, не запускал
    worldgen, не имел error/fatal markers и завершился code 0; main log SHA-256
    `9A63B9C3A82AA08947F30837B9FC8B90AC1F88B40B29ABD06C34555232DC558B`.
    Subworld Library отдельно запустил `-subworld 0` helper, где один
    `EndOfStreamException` был silently caught до загрузки мира; helper не имеет
    DingDong в failure path и зафиксирован отдельно SHA-256
    `CF0790AF78D352CDCF8C26B2BDDBD310D7447C62473CED01EAC015274D077447`.
    Offline full-map и 61 region crops проверены малыми группами: крупные сцены,
    семь R7 replacements, спорные пары и девять compact homes присутствуют
    целиком, без взаимного authored-focus overlap. Это технический sanity PASS,
    не owner visual PASS. Независимый exact-current-artifact audit сопоставил
    61 contract focus bounds с 61 фактическими v10 placements и исчерпывающе
    проверил все 1,830 пар: authored-focus intersections `0`, padded-focus
    intersections `0`, minimum axis gap `24 tiles` при clearance 12 с каждой
    стороны. Audit log SHA-256
    `9C242F261D1797C173C956308AF2AA5DFEDFD00A2A2D7BAF3E388B87A2700CE0`;
    61-row rectangles TSV и 1,830-row pairs TSV SHA-256 соответственно
    `E3147BE6BB31C2A573F8BB9BDD3F26348C94D19822529B0828D66037B478A54B`
    и `0AEA22057E6991F5020E445CD104313F4ECCA4A463A11E4BFDAF6A0026ECD750`.
    Один предшествующий v9 запуск с diagnostic seed text
    дал `getGoodWorld=False` и формально исключён из acceptance; окончательный
    v10 использовал literal `for the worthy`. Candidate установлен live
    byte-identical; prior `1FBAFD91…` сохранён в evidence backup, `enabled.json`
    остался byte-identical SHA-256
    `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
    Единственный активный gate — owner visual retest, approved manifest 0/61,
    Stage 4 закрыт.

## Ближайший acceptance gate: preset intake

- Ни один путь генерации не вызывает `TownHouseBuilder` или `CivicHubBuilder`.
- Каждая размещённая постройка ссылается на конкретный preset, автора, источник,
  лицензию и digest.
- На техническом тестовом мире нет raised-tier опор и прямоугольных очищенных
  камер, созданных DingDong.
- Подземная схема не проходит intake без отдельной POI-scene metadata и
  проверенной background/terrain-маски.
- После dedicated-server PASS кандидат передаётся игроку для отдельного visual
  PASS; технический PASS не заменяет визуальную приёмку.
- Clean site-envelope и preview PASS не разрешают runtime placement без player
  visual PASS; simultaneous district/city layout проходит отдельную проверку на
  этапе 4.
