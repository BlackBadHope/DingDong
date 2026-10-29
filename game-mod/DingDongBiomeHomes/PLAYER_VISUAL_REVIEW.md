# DingDong 2.0 — owner visual review runbook

Этот runbook относится к активному gate 3F. Рабочий technical-candidate build
размещает все 61 авторские сцены одновременно в **новом Large For the Worthy
Master/Legendary мире**, но это не меняет `NpcAuthoredPresetManifest`: до явных
решений владельца formal visual manifest остаётся `0/61`.

## Безопасная граница

- Codex не запускает основной tModLoader-клиент, не создаёт player profiles и
  не меняет пользовательские миры или состав `enabled.json`. После dedicated
  acceptance Codex может заменить только установленный `DingDongBiomeHomes.tmod`
  точным проверенным артефактом.
- Существующие миры не ретрофитятся. Мир, созданный сборкой, которая записала
  `generation withheld`, не содержит DingDong-резиденций и должен оставаться
  нетронутым.
- Workshop sources и binaries остаются `PRIVATE_ONLY`.
- Команда review никогда не записывает `PASS`/`FAIL`, не меняет tiles и не
  переводит candidate в approved manifest.

## Основной workflow: один мир с 61 сценой

1. Полностью перезапустите tModLoader после установки проверенного `.tmod`.
2. Создайте новый мир: `Large` → `Master` → seed `for the worthy`.
3. Зайдите в него в single-player и выполните `/dingdong audit`. Для текущего
   формата ожидаются `houses=61/61`, `housing=61`, `reviewTargets=61/61`.
4. Выполните `/dingdong review 1`. Команда берёт точную область из сохранённого
   world-state v3, повторно сверяет `reviewId`/NPC/preset/biome с каноническими
   manifests и переносит персонажа в безопасную точку внутри сцены.
5. Для следующей или предыдущей строки используйте `/dingdong review next` и
   `/dingdong review prev`. Прямой переход всегда доступен как
   `/dingdong review <1..61>`.
6. Проверьте внешний силуэт, отсутствие procedural clearing, естественный
   terrain/background transition, целостность комнат и важных объектов,
   соответствие NPC/биому и отличие от всех уже принятых резиденций.
7. Верните решение в форме `# PASS` или `# FAIL — короткая причина`, где `#` —
   показанный командой `reviewId`. При сомнении используется `FAIL`.

Рекомендуемый порядок: сначала восемь крупных R7 scenes из R7 ledger; затем
остальные full scenes по биомам; девять compact structures — в конце, поскольку
для них отдельно оценивается запрет на маленькие/похожие коробки.

## Legacy independent preview workflow

61 отдельная persisted preview-пара из
`C:\Users\ish50\AppData\Local\Temp\DingDongAssetIntake` остаётся техническим
evidence-set. В мире `DingDongPreviewProbe-*` команда `/dingdong review`
по-прежнему открывает единственную сохранённую сцену. Числовой selector допустим
только если он совпадает с каноническим `reviewId` этой preview-пары.

## Fail-closed поведение команды

- В обычном мире без полного v3 registry команда возвращает
  `complete-residence-registry-not-loaded`, а не пытается угадать координаты.
- На dedicated server прямой `/dingdong review <id>` только проверяет target и
  печатает `teleported=False`; `next/prev` требуют активного single-player.
- В multiplayer teleport запрещён до отдельного MP-review контракта.
- Повреждённая, дублированная или неканоническая запись registry игнорируется,
  после чего полный gate `61/61` не проходит.
- Ни один вариант команды не выставляет визуальный verdict автоматически.
