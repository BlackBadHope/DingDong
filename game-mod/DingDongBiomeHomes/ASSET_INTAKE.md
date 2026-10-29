# Authored asset intake

Этот документ отделяет наличие файла от визуальной приёмки. Схема не попадает
в `NpcAuthoredPresetManifest` только потому, что она технически читается.

## Structify snapshot

- Source: https://github.com/Valks-Terraria-Mods/Structify
- Commit: `eea570727f9b4037be02782911407d68915516c2`
- License: MIT
- Embedded catalog digest: `AF06A212BAE01B1D6764396987F9BDF6AA282D7813FE44DBBA995B40E20BEFA7`
- Binary intake: 36/36 dimensions and metadata match.
- Runtime loader: 26 vanilla-data structures accepted; 10 NBT structures
  fail closed until the explicit TileEntity importer is implemented.
- Visual intake: all 17 full-size Workshop catalog screenshots reviewed on
  2026-08-21. The set contains genuine authored geometry, but it is not a
  61-building NPC catalog and is dominated by one forest/medieval style.

## Candidate authored buildings

These are distinct authored components worth preserving and testing. They are
not automatically approved for an NPC or biome.

- `House1_Expination` — medieval front house, two rooms and attic.
- `House2_Expination` — medieval side house, two rooms.
- `House3_Expination` — medieval double house.
- `House4_Expination` — blacksmith; NBT-blocked at intake.
- `SmallHouse1`, `MediumHouse1`, `LargeHouse1` — one coherent house family;
  visually distinct sizes, but not three unrelated biome identities.
- `Castle1` — civic/storage anchor rather than an ordinary NPC house.
- `FishingPond1` — two huts plus authored water/terrain composition.
- `Greenhouse1` — furnished greenhouse residence.
- `Greenhouse2` — overgrown empty civic/residence shell.
- `Ship1` — harbor component; needs a complete shoreline scene.

## City-scene components

- `TowerGate1`, `ForestTree1/2`, `PineTree1/2`, `JungleTree1/2` and
  `JungleFarm` can enrich an authored district but do not count as NPC homes.
- Pylons are TileEntity-bearing infrastructure, not buildings. All nine pylon
  schemes remain NBT-blocked.

## POI-only candidates

- `UndergroundHouse1`, `UnderWaterDome` and `PrisonCell1/2/3` are rejected as
  standalone underground placements. They require a reviewed POI scene with
  natural terrain/background masks, entrances, decay and decoration.
- `BossArena` and `WallOfFleshArena` are non-NPC POI components. They require a
  purpose, discovery/activation state and authored surrounding scene.

## Workshop private-source queue

These worlds are strong visual/source candidates, but their Workshop pages do
not grant redistribution rights. Until explicit permission is obtained, any
captured schemes are private Home assets and must not be published.

- NPC Themed Houses World by Ziiio:
  https://steamcommunity.com/sharedfiles/filedetails/?id=2441486368
- The Pylons Journey by Khaios:
  https://steamcommunity.com/sharedfiles/filedetails/?id=2439942566
- Greaveil by TUNA DASH:
  https://steamcommunity.com/sharedfiles/filedetails/?id=3669025349
- The world of buildings by KIRA and Zilot:
  https://steamcommunity.com/sharedfiles/filedetails/?id=2441025960
- Aegis8 by TerraMonolith:
  https://steamcommunity.com/sharedfiles/filedetails/?id=2789072123

Обе пары миров получены 2026-08-21 через официальный SteamCMD anonymous без
запуска основного tModLoader. Master/Journey внутри каждой пары имеют одинаковую
геометрию и учитываются как один источник, а не как дополнительные постройки.
Схемы из этих миров разрешены только для private Home build.

### Workshop payload evidence

| Workshop ID | World | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| `2441486368` | NPC Themed Houses, Master | 12,554,258 | `D1AB3865D130180BB1AFC5F5F4E6595B9958D3D985B9F4C4C838CC90328C1D72` |
| `2441771534` | NPC Themed Houses, Journey | 12,554,276 | `72C7CFEC4104BE8D2B85884268FEF5FB353451FB9B952A3EA7BFA745AFC8CD29` |
| `2439942566` | The Pylons, Journey | 6,667,929 | `D17531EFD47069E522EDAFC5579C6B821F5D0F51ED8C13891C31BC86F9D5276E` |
| `2439941830` | The Pylons, Master | 6,667,927 | `E5270D4F42652FB226E64DC3DB8FFB455D494500F23834B94B4F1EAB9849A7D8` |
| `3669025349` | Greaveil | 12,844,758 | `B5738D5DA7C8065B069F7EC7DB076D3712E5F9584594F582F1FD7AE10B3FB418` |
| `2441025960` | The world of buildings | 6,367,521 | `86C0A4BFCE01A8C39E9C4E9095FE5AFEE62B823DF4AABC2DC83D978D4669919E` |
| `2789072123` | Aegis8 v1.13 | 13,229,350 | `F14FE6860EB3811E74A4E7657EBDE3B3EAC819311F6614DEA85C16736FA6008C` |
| `2458975834` | Ethnessa Town | 10,557,137 | `76C2DC4034E84ECD521C4736B821C29CC442BD951ECC3C985DD3CA3B6D11D27E` |
| `2814796435` | Giant NPC tower | 6,914,998 | `5036E2D8144ADCDFCAA42D3703006FCDF206ADAC448C1CBA508FD68A04C306EF` |
| `2865484106` | Barton 1.4.4.9 | 14,113,346 | `F0D09B348AB0AC8938ADF0D4D1F60CD464A1D3D6906B91394F2EEA6C9BB48251` |
| `2883756316` | updated huadiao world | 11,512,645 | `54BB112A3A9FDF6E8F948662B6A12AFB7352D30E4A4CF542BC315B16BE339D3E` |
| `3352920517` | RBW + All Items v1.4.4.9 | 12,317,878 | `425A1E94598F84E48EDC5F541F507DA8A9DD9982789ED10DF70A5DB53676D1FF` |
| `3358990936` | HANNIBAL'S HOUSE | 11,504,746 | `5B7F4B18182429FA69CC4155CF5A146771BD87FF1E276D026126474200CBC00E` |
| `2459200280` | Magoozy Palace | 11,364,891 | `53F688539A9AC7139AEF1878F5C462EE77B4438A79775B9841857641B91C750F` |

Headless TEdit-core intake loaded both unique worlds read-only. NPC Themed
Houses: version 234, `8400x2400`, 565 chests, 51 signs. The Pylons: version 235,
`6400x1800`, 288 chests, 4 signs. Full-map render digests are respectively
`CA91130C7F3D8236D56AB574D6059EAFB3CFF0415558AD0F33D80BB74CCF57BF`
and `DD703F613DF9EDC5D44ED3583676D2DC00F3B78B477B91AE902A2B912385ED48`.

Greaveil также загружен read-only: version 318, `8400x2400`, 36 NPC,
629 сундуков, 7 знаков и 36 TileEntity. Его Workshop-описание заявляет
авторский архив NPC-построек по биомам и поселения вокруг pylons; Cavern,
Hallow и Aether самим автором отмечены как отсутствующие. Это сильный private
источник поверхности, Underworld и городских композиций, но version 318 требует
отдельного gate совместимости с локальной Terraria 1.4.4.9 до экспорта.

The world of buildings загружен read-only: version 236, `6400x1800`, 23 NPC,
336 сундуков, 28 знаков и 25 TileEntity. Авторы страницы прямо указывают, что
постройки сделаны KIRA и Zilot. Мир визуально содержит разные надземные и
подземные композиции; подземные участки рассматриваются только как POI-сцены,
а не как готовые NPC-дома.

Aegis8 загружен read-only: version 248, `8400x2400`, 11 NPC, 600 сундуков,
33 знака и 165 TileEntity. Страница принадлежит TerraMonolith и называет мир
архивом всех его построек. Шесть surface-полос и шестнадцать финальных областей
не содержат unknown IDs или IDs за пределами локальной Terraria 1.4.4.9.

Ethnessa Town загружен read-only: version 236, `8400x2400`, 2 NPC, 552 сундука,
10 знаков и 15 TileEntity. Его сказочный spawn-town сохранён как резервный
district source, но не понадобился для закрытия residence-пула. Giant NPC tower
также загружен read-only (version 279, `6400x1800`, 36 NPC), но визуально
отклонён: тематические комнаты составляют один вертикальный монолит из малых
соединённых боксов, поэтому разделять их и искусственно увеличивать счёт нельзя.

## Source registry

| Source | Rights | Binary intake | Visual intake | Decision |
| --- | --- | --- | --- | --- |
| Structify, commit `eea5707` | MIT, notice embedded | 36/36 parsed; 26 runtime-safe, 10 NBT-blocked | 17 catalog images reviewed; authored but heavily forest/medieval | `ACCEPT_PARTIAL` |
| NPC Themed Houses World by Ziiio, IDs `2441486368` / `2441771534` | Author identifies the builds as their creations; Workshop page grants no redistribution right | Both worlds loaded; two difficulty variants, one geometry | Full map and seven surface bands reviewed; numerous distinct themed buildings | `PRIVATE_ONLY`; region extraction in progress |
| The Pylons by Khaios, IDs `2439942566` / `2439941830` | Author identifies the builds as hand-crafted; Workshop page grants no redistribution right | Both worlds loaded; two difficulty variants, one geometry | Full map reviewed; strong biome district/pylon anchors | `PRIVATE_ONLY`; city/POI source, not a 61-house catalog |
| Greaveil by TUNA DASH, ID `3669025349` | Author presents the world as their building archive; Workshop page grants no redistribution right | v318 loaded; explicit 1.4.4.9 whitelist; 13/13 export and parse PASS | Thirteen non-overlapping biome buildings/ensembles retained | `PRIVATE_ONLY`; accepted as candidates |
| The world of buildings by KIRA and Zilot, ID `2441025960` | Page states the buildings were created by KIRA and Zilot; Workshop page grants no redistribution right | v236 loaded; 5/5 export and parse PASS | Five standalone residence candidates retained | `PRIVATE_ONLY`; accepted as candidates |
| Aegis8 v1.13 by TerraMonolith, ID `2789072123` | Original author identifies it as the world containing all of their creations; no redistribution grant found | v248 loaded; 16/16 export and independent parse PASS; no compatibility remap | Sixteen visually distinct residences/complexes retained; tutorial copies and neighbours not split | `PRIVATE_ONLY`; accepted as candidates |
| Ethnessa Town by Samtaro, ID `2458975834` | Page presents an authored NPC village; no redistribution grant found | v236 loaded read-only; source digest recorded | Spawn town reviewed; distinct fairy-tale district | `PRIVATE_ONLY`; reserve district source |
| Giant NPC tower by ferdel3612, ID `2814796435` | Author states each NPC room is themed; no redistribution grant found | v279 loaded read-only; target IDs clean | One connected tower dominated by small box rooms | `REJECT_AS_61_CATALOG`; do not split |
| Barton 1.4.4.9 by XORandom, ID `2865484106` | Workshop page identifies XORandom; no redistribution grant found | loaded read-only; screened regions use target-compatible IDs | Central museum is one mega-structure; tree metropolis is a settlement; Sky Treehouse is visually standalone but housing FAIL | `PRIVATE_ONLY`; settlement/POI source |
| updated huadiao world, ID `2883756316` | Workshop page grants no redistribution right | v276 loaded read-only; selected regions use target-compatible IDs | Four standalone showcase residences retained; bottles, ship, blimp and small temple are POI/decor only | `PRIVATE_ONLY`; 4/4 housing PASS candidates |
| 51s TEdit Schematics, commit `c42d0a7` | CC0 | Repository inspected | Housing contains one basic marble room | `REJECT_AS_CATALOG` |
| Spirit Mod, commit `58f59e7` | MIT | 13 binary scene assets identified | Bismite caverns, shrine, pagoda, grave, memorial and other authored scenes | `ACCEPT_POI_CANDIDATE`; not NPC housing |
| TEdit sample schematics, commit `84e3fc3` | MS-PL | Castle and SampleHouse found | Two samples only | `REJECT_AS_CATALOG`; possible isolated candidates |
| Unrelated removed Aegis8 TEdit pack | Uploader stated the builds came from another map; download removed | Unavailable | Not reviewable; distinct from TerraMonolith's Workshop world above | `UNAVAILABLE_PRIVATE_ONLY` |

Следующий gate — поштучный region intake. Ни один прямоугольник с TileEntity,
сундуком, знаком или неизвестными модовыми тайлами не экспортируется молча:
он либо получает явный безопасный importer, либо отклоняется fail-closed.

## First private region batch

Пять подписанных Ziiio-построек выделены и визуально приняты как кандидаты:
Steampunker Workshop, Witch Doctor's Hut, Barber, Tavern и Night Club. Все
активные tile/wall IDs распознаны как vanilla. Экспортёр применил boundary
flood-fill: только связанное с краем пустое пространство стало прозрачной
NullBlock/NullWall-маской, поэтому asset не вырезает прямоугольник в мире.

Сундуки, знаки и TileEntity-тайлы санитизированы целыми multitile-объектами.
Пять результатов независимо разобраны по бинарному контракту: правильные
dimensions, `NBT=false`, 5/5 ожидаемых data blocks и точные block lengths.
Файлы и полная provenance-таблица находятся в
`Structures/PrivateHome/ZiiioNpcThemed` и
`ThirdPartyNotices/PRIVATE_HOME_WORKSHOP.md`.

Второй surface batch добавил 14 технически валидных геометрий: 11 residence
candidates и 3 малых village-infill компонента, которые намеренно не считаются
отдельными NPC-домами. Итого private Ziiio catalog: **19 assets, 16 residence
candidates, 0/61 APPROVED**. Housing capacity, anchor, biome assignment,
protected placement и пользовательский visual gate ещё не пройдены. Ничего не
добавлено в `NpcAuthoredPresetManifest`.

## World of Buildings residence batch

Поверхность v236 была проверена шестнадцатью неперекрывающимися полосами:
`16/16` не содержат unknown IDs и IDs за пределами локальных
`TileID.Count=693` / `WallID.Count=347`. Детальная визуальная нарезка оставила
пять самостоятельных residence candidates: западный галеон, два разных дома
портовой улицы, jungle treehouse и desert palace. Пустая роща и cliff shelter
не засчитаны как дома; Trost оставлен единым district-компонентом.

Пять `.shstruct` прошли независимый бинарный parse `5/5 PASS` и добавлены в
`Structures/PrivateHome/WorldOfBuildings`. Итого private catalog теперь:
**24 assets, 21 residence candidates, 3 village-infill components, 0/61
APPROVED**. Полная provenance-таблица находится в
`ThirdPartyNotices/PRIVATE_HOME_WORKSHOP.md`.

## Greaveil building batch

Девять исходных settlement-регионов визуально проверены, после чего
перекрывающиеся части улиц были объединены, а не засчитаны как разные дома.
Финальный batch содержит 13 крупных авторских residence/civic candidates по
Ocean, Desert, Snow, Forest/Hill, Mushroom, Jungle и Underworld.

Обычный exporter дал ожидаемый negative PASS: отказ на новых IDs v318.
Отдельный 1.4.4.9 compatibility exporter применяет только документированный
whitelist замен и падает на любом другом ID. Экспорт `13/13` и независимый
binary parse `13/13` прошли. Последующий реальный Brewer preview выявил дефект,
который parser не мог увидеть: TEdit `Tile` передавался в remap helper по
значению, поэтому отчёт считал `tile726->empty`, но три клетки оставались active
и падали в Terraria wall framing. Helper исправлен на `ref Tile`, все 13 assets
переэкспортированы из того же pinned world/manifest: пять non-remap binaries
остались byte-identical, восемь remap binaries ожидаемо получили новые hashes,
active out-of-range tile IDs и incompatible walls = 0. Corrected byte-identical
copies находятся в `Structures/PrivateHome/Greaveil`; актуальные hashes записаны
в `ThirdPartyNotices/PRIVATE_HOME_WORKSHOP.md`.

Private catalog теперь содержит **37 assets, 34 residence candidates и 3
village-infill components; 0/61 APPROVED**. Вместе с 11 Structify residence
candidates текущий потенциальный пул равен **45**, но это ещё не housing,
anchor, biome или пользовательский visual PASS.

## Aegis8 authored residence batch

Шесть широких surface-полос и шестнадцать уточнённых прямоугольников прошли
визуальный и ID-аудит. В batch вошли разные архитектурные сцены: desert oasis
settlement и gothic manor, mountain lodge, floating pagoda, forest hill manor,
две естественно встроенные cavern lake scenes, snow castle и spire, два Hallow
комплекса, jungle tree-city, desert sky palace, steampunk orb, treehouse и
windmill. Это не palette/size-варианты одной геометрии.

Обычный exporter использован без compatibility-remap: все регионы уже находятся
в пределах `TileID.Count=693` / `WallID.Count=347`. Stateful мебель,
сундуки, знаки и TileEntity санитизированы; boundary flood-fill сохранил
естественный terrain/background силуэт. Независимый parser дал `16/16 PASS`, а
копии в `Structures/PrivateHome/Aegis8` совпали с экспортом `16/16` по SHA-256.

Private catalog теперь содержит **53 assets, 50 residence candidates и 3
village-infill components; 0/61 APPROVED**. Вместе с 11 Structify residence
candidates потенциальный пул достиг **61**, но `NpcAuthoredPresetManifest`
остаётся пустым до per-NPC biome mapping, anchor, housing и пользовательского
visual PASS.

## Large-showcase replacement intake

Magic Builder был разобран как `.tmod`: внутри нет готовых `.shstruct`/world
schematics, а assembly строит Small/Medium/LargeHouse через `LayoutBox`,
`LayoutFilledBox` и `LayoutTriangle`. Источник отклонён как процедурный и в
модпак не устанавливался.

Gnorp Builder дал `14/16` Terraria housing PASS, но визуально это связанное
семейство небольших village-infill домов. Оно сохранено для будущих улиц, но не
засчитывается как крупные персональные дома.

Из Aegis8 дополнительно экспортированы пять непересекающихся showcase-сцен.
Snow Harbor Settlement, Moon Citadel и Cloud City прошли housing (`4`, `4`, `2`
комнаты); East Airship и Ocean Galleon имеют `0` комнат и переведены в POI.

Barton проверен широкими полосами и tight crops. Центральные observatory/
palace/court являются частями одной музейной мегаструктуры, Tree Metropolis —
готовый settlement asset. Sky Treehouse визуально самостоятельный, но сервер
вернул `HoleInWallIsTooBig`, поэтому это POI, а не скрыто исправленный дом.

В updated huadiao world четыре tight-crop showcase-резиденции не содержат
unknown или target-incompatible IDs. Экспорт и dedicated-server housing дали
`4/4 PASS`: Elemental Sky House — 2 комнаты, Fire Sky Village — 5, Crescent
Village — 2, Jungle Gothic Manor — 3. Бутылочные диорамы, корабль, blimp,
маленький desert shrine и temple не считаются домами; они оставлены в POI queue.

Итог sub-gate `3E-R5`: **7/16** крупных replacement slots имеют визуально
отобранный и серверно жилой кандидат; ещё минимум 9 уникальных домов требуется.
Approved manifest остаётся `0/61` до NPC/biome/anchor и пользовательского
in-world visual gate.

## Roadmap R6 private intake

Три мира загружены одним SteamCMD-вызовом и разобраны read-only без запуска
основного tModLoader. RBW содержит авторские именованные работы, но его верхняя
галерея состоит преимущественно из all-items/storage залов, а нижние здания
слишком компактны для роли крупных персональных резиденций. RBW не дал ни одного
residence slot; его tavern, hospital, crane и train сохранены только в civic/POI
очереди и не копировались в runtime мод.

HANNIBAL'S HOUSE и Magoozy Palace прошли tight visual crop, ID-совместимость,
экспорт и реальный dedicated-server housing probe на disposable Large For the
Worthy world. Полный Magoozy palace maze не импортирован: прямоугольный
underground-фрагмент отделён в POI queue, где потребует естественной cave mask.
Magoozy Snow Village также не разрезан на несколько домов и сохранён как один
settlement asset.

| Asset | Source | Source rectangle | SHA-256 | Housing result |
| --- | --- | --- | --- | --- |
| `Hannibal_SnowResidence.shstruct` | `3358990936` | `4125,415,290,120` | `EC4191560B2897AF30E61D9F0B790F868341F606FE51CF52761823BF37140692` | PASS, 3 rooms |
| `Magoozy_Palace.shstruct` | `2459200280` | `4500,300,405,225` | `9E6CAD8630C7F44220F45CC59A526762E24D17FE19CD6C2C7D09349509DCE2E1` | PASS, 8 rooms |

Проверенный `.tmod` SHA-256:
`42BBF5D3525922B0A879D65B3EAAC163716564A4422E41FFA056D9EBB2098262`.
Архивный server log SHA-256:
`C8F1A53BD91A392AC4819D6AF576C3322487430892DDE15585FABD205527DBC3`.
Итог активного sub-gate `3E-R6`: **9/16** крупных replacement slots; требуется
ещё минимум 7 визуально самостоятельных авторских резиденций.

## Roadmap R7 private intake and Legendary acceptance

Восемь финальных tight crops взяты из восьми независимых Workshop-миров. Все
исходники загружены вне основного tModLoader-клиента, полные карты и crops
просмотрены отдельно, а export выполнен без compatibility remap: unknown и
target-incompatible tile/wall IDs отсутствуют. Ни palette-вариации, ни части
одного поселения, ни процедурная геометрия в R7 не засчитывались.

| Asset | Workshop source | Source world SHA-256 | Source rectangle | Asset SHA-256 | Dedicated housing |
| --- | --- | --- | --- | --- | --- |
| `R7_NadikiMansion.shstruct` | `2456777286` | `B7CCF635CA9F7C52340327AB5C13CEF1227FA6C263BBC381393CC9223976C651` | `1980,190,300,220` | `1E07B629FC2839177031F2314D230B90C85BBFCD90D609AA95E920D550DF92DC` | PASS, 6 rooms |
| `R7_SnowMansion.shstruct` | `2461816371` | `9C0BE7720DB17E00741B6713720F86803EE29956C95C470B61FC0DA2F64D1C93` | `2600,300,450,350` | `C44AF81FA8E6A1B6FFDB63D8B1A6D452B3F6EEAFE96948DAC54E1DD76036F995` | PASS, 8 rooms |
| `R7_PrincessCastle.shstruct` | `3129951489` | `FEF13461385019CE85F55320A505DD9B02B9A4FD18CE646894714AFD9F8FFF2F` | `5500,150,600,250` | `4154B54EB1B8FA7943B5708CEFA474B6A742366C3E6B0A43426741897FEC86FA` | PASS, 11 rooms |
| `R7_CastleCoolResidence.shstruct` | `2580275865` | `C80F5EECD763FF03B67B20778DFA647BFECFF2E05945B68CF56E1D431286160A` | `3900,250,700,450` | `1BF4EB868411425C0E5317E72563E9D0856A9B4297A12274A71F39B3C5DD4ACA` | PASS, 18 rooms |
| `R7_EsonoraHome.shstruct` | `2848278947` | `A12FA73C9C52B04A6880414C80C8EB720FA57F3762DDF045126F66381C1A0881` | `4050,350,500,300` | `D105F24EC0CBDDB596DACC9123A46549CA177A4149B75EEA0AF6A7E3183AD35F` | PASS, 10 rooms |
| `R7_ScarletDevilMansion.shstruct` | `2445047728` | `5A689C25DBF6EF97FCB1C7A298EDD3ECE06B36EDC14224E1F91C4F9A510E3086` | `2150,50,650,350` | `E34F27364B4522A60ED3C25E122CDEF51D604F294820FDA3D11D3DACE33FB016` | PASS, 19 rooms |
| `R7_UnrealDeltaCastle.shstruct` | `3004032818` | `3D6C08D781DE4FB10B3E7913751BF242E2CE0EDF0BDEAC63D45CDB15A91710A8` | `1900,150,600,250` | `006E5CBA3D68D4190081C301A2D335F1533D45BC17762EEAD6C628BC7AA4B667` | PASS, 13 rooms |
| `R7_MansionAlmost.shstruct` | `2493801540` | `FF0ADFF5EE33E7A80E51FD549F8189AE3E53B48B5BAAD64453AB0447CB8007BB` | `3950,250,600,300` | `ECC4E105710A0F8F53969BE9AFF61EF7614B533545E89A0BA7BE6782CA1620DC` | PASS, 9 rooms; reserve |

Dedicated-server acceptance выполнялся на сохранённом disposable Large For the
Worthy мире. Runtime marker подтвердил Legendary: `masterMode=True` и
`getGoodWorld=True`. Финальный результат:

`DINGDONG_ROADMAP_R7_HOUSING_PROBE_END|verdict=BASELINE_PASS|actualPass=8/8|actualFail=0/8|errors=0`

Финальный server-tested `.tmod` SHA-256:
`E428C5BECBA0BCFA4A1B431E7A95FF2C9B94E511FAF17122CC89AC3F8CDBC3F3`.
Финальный frozen server log SHA-256:
`136E642A818D1BAA0AE1E2EE1522EC1BB48FE7AC5F4CBCBB672EA7A64ECDE8DB`.

Семь R7 assets вошли в distinct candidate mapping, а незавершённый
`R7_MansionAlmost` сохранён резервом. Вместе с 3 Aegis8 showcase, 4 Huadiao и
2 R6 это закрывает все **16/16** replacement slots и технический candidate
mapping **61/61**. Approved manifest остаётся **0/61** до пользовательского
in-world visual PASS. Все восемь источников имеют статус
`PRIVATE_HOME_ONLY`; права на распространение не зафиксированы.

## Stage 3F technical deployment checkpoint

Candidate mapping не менялся после R7. Для всех 61 назначений добавлены
технические deployment profiles: source/target anchor, boundary policy,
protected padding, blend margin и seed fallback. Отдельный contract gate на
сохранённом disposable Large For the Worthy Legendary мире подтвердил 61/61
distinct profiles, 9 compact structures, 52 full scenes и approved manifest
0/61.

Read-only site planner проверяет каждую сцену независимо, а не изображает
одновременную раскладку города. Collision-анализ использует exact loader-write
mask: экспортные `NullBlock`/`NullWall` сохраняют boundary-connected natural
background и не считаются перезаписью. Полный bounded surface search и отдельные
cavern/mushroom/underworld strategies нашли biome envelope для 61/61. Один
vanilla chest в protected footprint
`Terraria/PartyGirl -> R7_ScarletDevilMansion` был разрешён lossless: destination
создан до удаления source, перенесены все пять непустых item slots и имя,
signature `98D02B33E744F6A7` совпала до и после save/reload. Повторный independent
site gate дал `cleanProfiles=61`, `persistentObjectOverlaps=0`.

Clean-site server-tested 3F artifact:
`C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\installed-mod-backups\DingDongBiomeHomes-roadmap-3f-relocation-clean-pass-FF211DBE9BC6.tmod`,
SHA-256
`FF211DBE9BC6E57BC867F4B1DCEF5B1CFF0330146C3FE3F4F4308DDFC0BBFB3A`.
Relocation apply/site и reload/site frozen log SHA-256:
`27EA33345DC409CB474F60D0591CBF4E135806A7EE1FD3EE175C71CFC1FA4159` и
`EF7C5F3B3B7035F3D2FE3FE1778A722773E931DC229C6A920C1BCC71F11C9A67`.

Следующий preview/persistence artifact:
`C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\installed-mod-backups\DingDongBiomeHomes-roadmap-3f-scarlet-preview-pass-A146914D8C8C.tmod`,
SHA-256
`A146914D8C8CB41D531D6CC7D9A5BF358FFD0AE4D9D793E2AB83B6E4EA13DFC5`.
На disposable Large FTW Legendary Scarlet preview дал 20 валидных комнат,
lossless relocation одного сундука, persisted `.twld` marker и после reload
`loadedFromSave=True`, `savedValidRooms=20`, `currentValidRooms=20`.
Preview apply/reload frozen log SHA-256:
`4943ACDC0F535546E5F784E95A3185E0736C99B9B722A56E7253597E4F971D35` и
`B42F34E1A7368D191E875C43DA47A61475F9AFB6089BF3557FE931B0A0261B60`.

Collision blocker закрыт, но технический preview не является player visual
approval. `worldgenPlacementApproved=False`, `PlayerVisualApproved=false` и
runtime `NpcAuthoredPresetManifest` остаётся 0/61. Simultaneous city layout не
проверялся и относится к следующему roadmap-этапу. Каноническая владельческая
очередь 61/61 находится в `PLAYER_VISUAL_ACCEPTANCE.md`; её начальное состояние
fail-closed: `PASS=0`, `FAIL=0`, `PENDING=61`. Все семь вошедших в mapping R7
residences подготовлены отдельными disposable preview worlds и независимо дали
apply/save/reload PASS. Все девять compact `SurfaceStructure` и девять остальных
Forest full scenes также имеют отдельные persisted review worlds. Следующий
artifact после Greaveil correction и Forest gate имел SHA-256
`0FA2159F608518E5154454938624D2298CC428B892BAA71455973B39B450E336`;
Brewer reload сохранил 2/2 комнаты, byte-identical Architect canary — 4/4.
Его semantic-runtime-identical package-successor SHA-256
`6E6164AE46609D7B5EB11CB65EAEF7E020C64A65F90588A616D432E9FACDA814`
дополнительно прошёл apply/save/reload четырёх остальных Snow full scenes:
Cyborg 1/1, corrected Greaveil Mechanic 5/5, Santa 1/1, Tax Collector 3/3.
Его semantic-runtime-identical successor SHA-256
`51ACC72CE865CDCB736151704204A000AB6B953E216C1023630F148830D1F3F5`
затем прошёл пять Desert full scenes: Bandit 6/6, corrected Greaveil Arms
Dealer 4/4, Dye Trader 8/8, Steampunker 3/3, Desert Acolyte 8/8.
Его semantic-runtime-identical successor SHA-256
`73D3EA7F825FA7AC91FC05F7D66407161A96F8142196FA703FBAA2BAF923343F`
прошёл пять Jungle full scenes: Jharim 5/5, Dryad 3/3, Painter 3/3,
Witch Doctor 2/2, Druid 1/1. Тот же функциональный artifact затем прошёл семь
оставшихся Ocean full scenes: corrected Greaveil Sea King 3/3, Jelly Priest
2/2, Fisherman 4/4, Pirate 2/2, Stylist 1/1, corrected Greaveil Diverman 2/2,
Fishylady 1/1. Angler #39 не повторялся: его compact apply/reload 2/2 уже был
зафиксирован ранее. Тот же artifact прошёл пять оставшихся Underground full
scenes: Golem 5/5, Deeps 3/3, Clothier 1/1, Demolitionist 2/2, Goblin Tinkerer
1/1; Blacksmith #49 не повторялся после R7 PASS. Clothier area 60x105 оставлена
на явный owner visual verdict. Затем artifact прошёл семь оставшихся Hallow
scenes: Oracle 2/2, Tavern 10/10, Nurse 3/3, Princess 4/4, Wizard 2/2,
Spiritualist 1/1, Witch 4/4; R7 #51/#54 не повторялись. Последние Truffle,
Brimstone Witch и Abomination дали 3/3, 5/5 и 4/4 после save/reload.
Exact world/log hashes записаны в acceptance-ledger; технически готово к
просмотру 61/61, player results по-прежнему 0 PASS и 61 PENDING.
Финальный docs-only package-successor
`DingDongBiomeHomes-roadmap-3f-technical61-doc-smoke-5FCCBCA1D181.tmod`,
SHA-256
`5FCCBCA1D1811BFB0F28D5041E11BB2A1686CF86609FF63E81B2764A4BDB809D`,
runtime-identical `73D3…`: normalized semantic SHA-256
`931D3A1A8B0650C8A1EE93B08A48F319AFEA48998B47B265488E68295F7FB363`
совпал по 30,169 canonical lines. Он отдельно загрузился на dedicated Large FTW
Legendary и штатно сохранился; frozen smoke-log SHA-256
`EDED0329EB3E030B5D0762CB7860F4BA8586583F3FF3B12A8B31A0CBFBBB33D4`.
Текущий review successor
`DingDongBiomeHomes-roadmap-3f-player-review-command-9698BEE2DFE5.tmod`,
SHA-256
`9698BEE2DFE514C94D24B12535B93EB52DCEC3C067EFC4EF199962C36CAC33BE`,
добавляет только fail-closed `/dingdong review` и owner runbook. Compiled
semantic delta изолирован типом `DingDongCommand`; все 30,094 строки вне него
совпали с `5FCC…`. Dedicated Scarlet apply/reload вернул один и тот же persisted
target при `approved=0/61`; это usability evidence, не visual PASS.
Текущий safe-focus successor
`DingDongBiomeHomes-roadmap-3f-safe-focus-preflight-ED00AD072CB1.tmod`, SHA-256
`ED00AD072CB169F5A63DE1ADB62B2E01116CE568AD5B486E36D92653397E7271`,
добавил общий collision-aware finder для dedicated preflight и single-player.
Scope снова изолирован `DingDongCommand`; 30,094 прочих semantic lines
неизменны. Scarlet, compact Forest, sky/ocean, underground и underworld clones
дали `focusReady=True`, не меняя `approved=0/61`.
Текущий functional canonical-review-id successor
`DingDongBiomeHomes-roadmap-3f-canonical-review-id-2FC9A5D5BE77.tmod`, SHA-256
`2FC9A5D5BE773DFC3C03A276798F8B0AF2BC1980A322BA9B5C38E903F029E36D`,
добавил fail-closed NPC/preset-to-ledger resolution. Статический contract связал
61/61 manifest identities, candidate presets и visual ledger rows; Scarlet
dedicated preflight вернул канонический `reviewId=54`, `ownerReply`,
`focusReady=True`, `approved=0/61` и завершился clean save/exit. Contract/runtime
log SHA-256: `9D29E5DD3509409B07DB700C588D9875B2A649CCBAB358E4405C1C9B8804D13E`
и `81F565D68B6D7C754D32A44ABC6D4387EBD5ED6B8071914D08055FD73EB59586`.
Текущий packaged successor
`DingDongBiomeHomes-roadmap-3f-canonical-review-id-doc-successor-8C0317D6BCB4.tmod`,
SHA-256
`8C0317D6BCB4AC93B6110823604770E409EC34B9CE9A85941A921264642EB0BB`,
содержит 181/181 entries и ровно шесть ожидаемых doc/DLL changes. Его normalized
runtime semantic SHA-256 совпал с dedicated-tested `2FC9…` по 30,866 lines:
`3E07845D779506EF87D3BAB1EA1E8F90F6AACE558F6C5F86602B4E757C5D421A`;
scope log SHA-256
`40E833B23E4B601A5D3A8534B062DAD429C31E1284BCA327DAEA3B457FAD1BF7`.
Full-modpack review-registry successor
`DingDongBiomeHomes-review-C159E32D359E-fullmodpack-pass.tmod`, SHA-256
`C159E32D359EC3430444EA0C8DC5B309DE4C787F390E5A6C5C2590EC9F42695F`,
проверен на disposable Large FTW Legendary с точной копией текущего
`enabled.json` (67 entries, SHA-256
`1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`).
Worldgen дал 61/61 structures, 61/61 unique presets, 61/61 housing, 269 rooms,
14 lossless chest relocations, 0 unresolved overlaps, `masterMode=True`,
`getGoodWorld=True`; reload сохранил `reviewTargets=61/61`, а review IDs 1 и
61 вернули `READY` из persisted report-v3 registry. Artifact установлен live;
его predecessor `2918…AD43B` сохранён отдельно. Player visual results остаются
`0/61`, поэтому это не меняет approval/provenance status.

Этот C159 checkpoint позже был **superseded** реальным owner-client regression:
UI seed `123` создал numeric terrain seed `106748011`, после чего DingDong
planner нашёл 61/61 sites, но batch relocation отверг unsupported/stale chest
record на tile type `2797`; последующие bounded attempts не разложили две
крупные R7 surface scenes. Сбой произошёл до любой mutation (`mutated=0`).
Корень расхождения с предыдущим dedicated PASS: DingDong layout hash использовал
`SeedText`, тогда как Terraria terrain использовал numeric `Seed`.

Текущий exact-seed regression artifact SHA-256:
`F139103D029C42DDA3C92559AAEDE00D1952C1E8EB665529B176FEAA72985308`.
Planner теперь солит раскладку numeric seed и допускает persistent advisory site
только если все collisions являются поддерживаемыми vanilla/tML 2x2
`BasicChest`; unsupported records сохраняются и их площадка пропускается.
Full-modpack dedicated на Large FTW Legendary, numeric seed `106748011`, с той
же byte-identical копией 67-entry `enabled.json` дал 61/61 structures, 61/61
unique presets, 61/61 housing, 259 rooms, 8 lossless chest relocations,
0 unresolved overlaps и 0 errors. Reload загрузил persisted registry и audit
вернул 61/61 houses/themes/residents/housing и 61/61 review targets.

Frozen worldgen/reload server log SHA-256:
`380CD6F7941576A117E0A87162C2BA951BFF497A34C505012CDFC48E0F24CC9F` и
`EAF1A516C156F07A8C0F3A63E0B2996D2058DEA65A3E169E98F66ACE5A067FA1`.
Reload audit transcript SHA-256:
`AE617AA14C72CA1D3FAC1028D7B769DB6EC8390966E541068EB750E16F313BA4`.
Artifact установлен live вместо C159; старый `.tmod` сохранён в отдельном
backup, а live `enabled.json` не менялся и сохранил SHA-256
`1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
Player visual results остаются `0/61`.

Workshop provenance и binaries остаются `PRIVATE_ONLY`; checkpoint не даёт
права публикации или перераспространения.

## Stage 3F visual-integration correction

Owner review simultaneous F139 world получил verdict `NEEDS_FIX`. Скриншоты
показали три системных класса дефектов: жёсткие прямоугольные вставки исходного
terrain/background, наложения самостоятельных резиденций и обрезанные дальние
фрагменты сцен. Независимый разбор 61 F139 placement rectangles нашёл 33 пары
asset-area intersections, затронувшие 35 резиденций, включая 15 containment.

Исходные `.shstruct`, их digests, авторы и provenance не менялись. Исправление
находится только в runtime integration layer:

- boundary-connected natural terrain и небезопасный natural background больше
  не копируются из source scene поверх destination world;
- основной authored focus выбирается по связной furnished house-wall области,
  включая room-needs (door/table/chair/torch), а mutation ограничивается её
  bounded footprint;
- surface anchor рассчитывается относительно интегрированного terrain anchor;
- protected area строится по фактическому mutation footprint с padding, а
  planner безусловно запрещает пересечения protected rectangles.

Successor artifact SHA-256
`3506161CB3593E8C16E1107AFE52E08306E4684221F34845557D9A29E050FDF9`
(1,664,173 bytes) сохранил mapping `61/61` и dedicated simultaneous layout
`61/61` с нулём protected-footprint intersections. Full-modpack dedicated
worldgen с неизменным 67-entry `enabled.json` на Large literal
`for the worthy` дал `structures=61/61`, `uniquePresets=61/61`,
`housing=61/61`, `rooms=229`, `relocatedChests=1`, unresolved overlaps `0`,
`masterMode=True`, `getGoodWorld=True` и `Server started`. Reload существующего
мира также дошёл до `Server started` без errors. Frozen worldgen/reload log
SHA-256:
`A5A9071F9477CF1653A97C8B31DA52D97031DA0BF55A2FD81A3968FD59C738FF`
и `E736D08ECD79763AE7D24625FE8A1DE50B9D49B6E30768C028E49B81D9926B3F`.

Offline render полной карты SHA-256
`8979E1177B29B1C8F202B4CA9272E53B968024F333283AB93F0974EBD163C7C2`
и 61 отдельных region crops подтверждают отсутствие прежних гигантских
DingDong source-terrain rectangles и protected-footprint overlaps. Это не
player visual PASS: новый artifact имеет только `READY_FOR_OWNER_RETEST`, все
61 строки ledger остаются `PENDING`, runtime approved manifest — `0/61`.
Artifact установлен live byte-identical; F139 сохранён как
`DingDongBiomeHomes-F139103D-pre-visual350616.tmod`. Live `enabled.json` не
изменён и сохранил SHA-256
`1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.
Workshop provenance и binaries остаются `PRIVATE_ONLY`.
