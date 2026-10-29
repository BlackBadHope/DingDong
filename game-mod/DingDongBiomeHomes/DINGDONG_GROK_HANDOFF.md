# DingDong Grok — продолжение task 2026-08-22

Открой этот ярлык `grok.exe.lnk`. Рабочая папка Grok — исходники мода, не Integrity Guardian.

Первое действие: полностью прочитай
`C:\Users\ish50\Desktop\DingDong Grok\Инструкция для Грог.txt`
затем этот handoff. Не начинай Stage 4+, damage, квесты, escort.

## Живой gate

Stage 3F: пространственное распределение 61 авторской резиденции.
Owner verdict: `NEEDS_FIX`. Visual PASS = `0/61`. Approved manifest = `0/61`.

## Пути

- source: `C:\Users\ish50\Documents\My Games\Terraria\tModLoader\ModSources\DingDongBiomeHomes`
- live tmod: `C:\Users\ish50\Documents\My Games\Terraria\tModLoader\Mods\DingDongBiomeHomes.tmod`
- tModLoader: `D:\SteamLibrary\steamapps\common\tModLoader`
- private intake: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake`
- backups: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\installed-mod-backups`

## Проверенное состояние

- branch `review-successor-2918`
- HEAD `8f341a9` (грязное дерево: distribution planner)
- live `.tmod` SHA-256 `5C48D3C540B53BF7054E66F93B1B3A7425A2FB045C37BDBB9EE51DD10BF64174` (1,679,651 bytes)
- `enabled.json` SHA-256 `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1` (67 entries) — НЕ ТРОГАТЬ
- v10 accepted worldgen 61/61 housing, но owner отклонил мегакластеры и одинаковый sky `top=64`

## Что уже в source (не в live)

- `AuthoredResidencePlacementManifest.cs`
- `AuthoredResidenceLayoutPolicy.cs`
- правки `CandidateDeploymentSitePlanner.cs` (убран packToExistingDistrict, sky bands, scoring, fail-before-mutation)
- правки `BiomeHomeWorldgenSystem.cs` (строки DISTRIBUTION)

## Последний dedicated worldgen

Кандидат (НЕ установлен live):

`DingDongBiomeHomes-distribution-layout-candidate-06C2A634360B.tmod`
SHA-256 `06C2A634360B5F31C5DB28022052C37001A8D22E038D5D6C3B249BD9E9693847`

Log: `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\evidence\terrain-v16-06c2a634-literal-ftw-server-20260822.log`
SHA-256 `B5765A8F32D2792D86EEF0F6ED85BFC5DB2B074DC09B09F5ED83E3A35E576720`

Результат: FAIL до mutation. Лучший attempt `27/61`. `mutated=0`.

Повторяющиеся blockers:

- Terraria/Princess / Aegis8_HallowCliffManor
- AlchemistNPCLite/Musician / R7_NadikiMansion
- Terraria/SantaClaus / Aegis8_SnowCastle
- Terraria/Guide / Greaveil_HillManor
- WeaponEnchantments/Witch / Aegis8_HallowRidgeVillage
- Terraria/TaxCollector / Hannibal_SnowResidence

Не возвращать `packToExistingDistrict`. Не ставить candidate live, пока нет `DINGDONG_WORLDGEN_END|verdict=PASS`. Не отмечать owner visual PASS.

## Как гонять dedicated

Один сервер. Python runner с живым stdin:
`C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\run-worldgen-v12.py`
(скопируй и смени config/log имена). Isolated harness:
`C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\fullmodpack-terrain-v6b-f8bafadb-20260822`
Live `.tmod` после `-build` сразу возвращай на `5C48D3C5…`, пока нет PASS.

Large, seed `for the worthy`, difficulty 2, masterMode, getGoodWorld.
Не запускай основной клиент. Не трогай пользовательские миры и модпак.