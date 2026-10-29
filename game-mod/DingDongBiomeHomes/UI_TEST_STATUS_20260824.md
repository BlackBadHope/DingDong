# DingDong UI test handoff — 2026-08-24

Статус: отдельный headless-compiled кандидат готов к ручной визуальной проверке.
Main/live не изменён.

## Исправление в исходниках

- Изменён только `DingDongHubUi.cs`.
- SHA-256 файла после изменения:
  `3323DECDD9B5FE5F6DE4DABF815B416DE154B4089FDC8016DBB4DD97288BB6AF`.
- Окно ограничено `980x690`, уменьшены шрифты/иконки/кнопки, освобождена
  ширина вкладок, карточки учитывают длинный русский перенос, combat title не
  дублируется.

## Отдельный тестовый build

- Исходники build-copy:
  `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\ui-test-20260824-2230\source\DingDongBiomeHomes`
- Для test-only RU fallback файл `ru-RU_Mods.DingDongBiomeHomes.hjson` был
  byte-identical скопирован поверх `en-US_Mods.DingDongBiomeHomes.hjson` только
  в build-copy. Основной en-US source не менялся.
- Display/version build-copy: `DingDong: Biome Homes [UI TEST RU]`, `0.20.1`.
- Headless build: PASS, exit 0, error markers 0, warning markers 0.
- Build log:
  `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\ui-test-20260824-2230\ui-test-headless-build.log`
- Build log SHA-256:
  `1CBAC58FE03B8E827DE88704F38AAB517E1A44CDABA0352027DD14CECFD84FC9`.
- Тестовый `.tmod`:
  `C:\Users\ish50\Desktop\DingDong-UI-TEST-20260824-2230\DingDongBiomeHomes.tmod`
- SHA-256:
  `62FE0D3492DD4FBF22E773A4966ABD87BFF930B2C941BE80D08327AF4DF528CC`.

## Что ещё не проверено

- Графический клиент Codex не запускал.
- Реальный render вкладок при разных UI Scale не проверен.
- Пользователь должен вручную проверить четыре вкладки по README рядом с
  тестовым `.tmod`.
- Dedicated worldgen не повторялся: изменение UI-only, а test artifact не
  предназначен для замены main/live до визуального verdict владельца.

## Неизменённый live baseline

- `DingDongBiomeHomes.tmod` SHA-256:
  `EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`.
- `enabled.json` SHA-256:
  `1EAD649811E10EE3E37DF51F5467AE4F74C73375307942CB506B35225B32B8A1`.

## Ручной семейный ZIP

- Архив:
  `C:\Users\ish50\Desktop\DingDong-Modpack-0.2-RU-Manual-20260824-2230.zip`
- SHA-256:
  `02577127390091133FB231D86D3926248C634CAECACCE12F2A78126821DF2784`.
- Размер: `539,973,998` bytes.
- Состав: 67 `.tmod`, 67 enabled entries, русская пошаговая инструкция,
  ModConfigs/RootConfig и notices.
- Verification: 87/87 checksum entries PASS, copied source files 84/84
  byte-identical, forbidden scripts/installers 0, Worlds/Players paths 0.
- Build log SHA-256:
  `41267E612C1CA0870C8DDA84E500500C29AF4A59D5F6E5B34A4128BF9C0865F9`.
- Verify log SHA-256:
  `13B79B7965F6FCA7378C92ABB98B6927C2724D157CA57F9CCE25FA5B65B89D23`.

## Кэшированное worldgen-доказательство exact-live

- Неизменённый live artifact SHA-256
  `EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`
  byte-identical артефакту из изолированного запуска `hub-v1-worldgen-profile`.
- Замороженный server log:
  `C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake\hub-v1-worldgen-evidence\exact-live-EB1C-worldgen-server.log`
- SHA-256 server log:
  `ACCCCD654B590FA57EB30DE85C2CA81CDFE825CEE57D7D435DC7595847006781`.
- Проверены маркеры: gameplay contract PASS, ru-RU localization 29/29 PASS,
  Large master/get-good-world generation, `DINGDONG_WORLDGEN_END|verdict=PASS`,
  structures 61/61, unique presets 61/61, housing 61/61, rooms 212,
  unresolved persistent overlaps 0, generation complete, server started.
- Оговорка: server log содержит два нефатальных
  `System.IO.IOException: The handle is invalid`, связанных с перенаправленным
  show-server-console handle. Генерация продолжилась и сервер стартовал;
  этот лог нельзя описывать как полностью свободный от exceptions.

## Follow-up TEST3 — integrated combat + UI candidate

Source changes are now part of the real source tree rather than only a build
copy:

- `DingDongHubUi.cs` SHA-256:
  `273C64E9A7DBDE785E95AEC903BAE0FDD7441BD30C312C69A0CC3E92A6A76EC6`.
- `DamageGuideUi.cs` SHA-256:
  `B3FDD80FC7163C211EF872CD61A0831CC23239F7A4844CE453EB5D0E97DB4F0E`.
- `ru-RU` and `en-US` localization files are intentionally byte-identical for
  forced Russian TEST3 fallback; SHA-256:
  `185CFFEE5BA07FD8E36697DA98EA1AD07ED9CB08C4A0F32D51B184342CEB7A85`.

Layout changes:

- regular-size shell title instead of `large` Terraria font;
- navigation widened from 172 to 196 logical pixels;
- smaller tab/content fonts and compact navigation buttons;
- separate header/list geometry with no bottom footer collision;
- dynamic card height accounts for wrapped heading plus wrapped body;
- combat overview reduced from five cards to three;
- Russian text is used even when Terraria itself is set to English.

Integrated artifact:

- Display/version: `DingDong: Biome Homes [MULTIPLAYER COMBAT UI TEST 3]`, `0.20.4`.
- `.tmod` SHA-256: `3B1DBD6BBAF03F94DDD1054E562C6E33CB9D04406DDB020ACC8EC96BCAFFCDAC`.
- Patch ZIP:
  `C:\Users\ish50\Desktop\DingDong-MULTIPLAYER-COMBAT-UI-TEST3-20260825.zip`.
- Patch ZIP SHA-256:
  `EEAFA3AA1C565D87F8C57D06D652FC877E6446D9DAE05DECAA9F309BDAA89AD1`.
- Full manual ZIP:
  `C:\Users\ish50\Desktop\DingDong-Modpack-0.2.1-RU-Manual-TEST3-20260825.zip`.
- Full ZIP SHA-256:
  `15F656FA18056F84D0F9D4DA4624572A9F028CE4AA77C79E2B5F6DF97123C746`.

Verification:

- static layout/localization/combat preflight PASS;
- headless build PASS, 0 errors, 0 warnings;
- full ZIP content and embedded DingDong/enabled hashes PASS;
- no Worlds, Players, PowerShell, BAT, CMD, EXE, MSI, installer, or
  `NothingImmune.tmod` in the archive;
- graphical render at the owner's actual UI scale remains untested by Codex.

Main/live remains unchanged at SHA-256
`EB1C5467BBF2A51B0FB80274E3714C31791BAE582BEE42C8A0884EECF8CA5359`.
