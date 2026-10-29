# DingDong 2.0 — systemic gameplay contract

Статус: `SPECIFIED / NOT IMPLEMENTED`. Этот документ фиксирует новый
обязательный scope 2.0, но не открывает этап 4 и не подменяет активный owner
visual retest 61 резиденции. Реализация живёт в том же `.tmod`, однако combat,
events, quests и escort остаются отдельными модулями от worldgen.

## Цель

For the Worthy должен отличаться не только географией. Тип источника урона,
стихия, способ получения оружия, морфология противника, сегмент тела, biome,
слой глубины, POI и отношения с NPC должны менять тактику. Система обязана быть
data-driven, server-authoritative, сохраняться в мире и игроке, корректно
синхронизироваться и иметь измеримый coverage всего текущего модпака.

Это не разрешает:

- безымянный neutral fallback без записи в audit;
- сотни несвязанных `if (npc.type == ...)` вместо manifests/adapters;
- глобальное событие, которое только визуально называется биомным;
- мгновенный teleport сопровождаемого NPC как скрытый fallback;
- автоматическое переселение vanilla housing ночью в обход escort;
- молча ломать баланс или immunity pipeline других модов.

## 1. World physics: базовый прыжок

- Базовый прыжок персонажа без брони, аксессуаров, mount, grapple, wings,
  liquids и внешних buffs имеет apex ровно пять tiles: 80 px между положением
  ступней на старте и верхней точкой траектории.
- Аксессуары и эффекты других модов применяются поверх новой базы; DingDong не
  обнуляет их бонусы и не переписывает скорость каждый tick.
- Server config является источником истины. Клиент получает синхронизированную
  базу до начала движения; рассинхрон prediction/server считается FAIL.
- Dedicated physics probe проверяет стартовую скорость, gravity profile,
  рассчитанный apex и повторяемость при одинаковых modifiers.

## 2. Damage ontology

Каждый damage source получает `DamageSourceProfile`, состоящий из взвешенных
namespaced tags. Строковые IDs позволяют модам добавлять новые типы без
перекомпиляции ядра.

### Оси источника

- Delivery: `kinetic_contact`, `arrow`, `bullet`, `thrown`, `rogue`,
  `magic_projectile`, `beam`, `explosion`, `minion_contact`,
  `minion_projectile`, `sentry`, `trap`, `dot`, `environment`.
- Element: `neutral`, `fire`, `frost`, `lightning`, `water`, `earth`,
  `poison`, `holy`, `shadow`, `astral`, `void`, плюс namespaced mod tags.
- Behavior: `single`, `burst`, `sustained`, `piercing`, `area`, `chain`,
  `delayed`, `rapid_multihit`.
- Acquisition: `crafted`, `creature_drop`, `boss_drop`, `biome_chest`,
  `worldgen`, `quest`, `shop`, `event`, `fishing`, `transmutation`, `unknown`.

Один источник может иметь несколько тегов одной оси с весами, сумма которых
нормализуется до 1.0. Например огненная стрела остаётся одновременно `arrow` и
`fire`; магический взрыв — `magic_projectile`, `explosion` и конкретная стихия.

### Оси цели

`TargetResistanceProfile` содержит:

- morphology: `gelatinous`, `fleshy`, `armored`, `brittle`, `spectral`,
  `mechanical`, `plant`, `aquatic`, `flying`, `segmented`;
- delivery multipliers;
- elemental multipliers;
- conditional vulnerabilities/resistances;
- segment roles: `head`, `body`, `tail`, `detached`, `core`;
- phase/state modifiers и telegraph metadata.

Внутри одной оси взвешенные multipliers объединяются геометрическим средним;
между осями перемножаются. Затем применяются condition/segment rules и общий
fail-safe clamp `0.10x..3.00x`. Множитель применяется к `FinalDamage` после
обычных weapon/ammo/enchantment modifiers, но до фактического списания life.
Каждый hit может вывести deterministic trace: source tags, target traits,
сработавшие правила, итоговый multiplier.

## 3. Hit lineage и окно multihit

- Каждый первичный use получает server-side `AttackInstanceId`.
- Дочерние projectiles наследуют lineage. Искусственные копии одного выстрела
  от `SeveralfoldProjectiles` по умолчанию считаются одной volley, если профиль
  явно не помечен `true_multistrike`.
- История хранится по target, attacker и attack lineage. Client hit packets не
  могут самостоятельно объявить vulnerability.
- Текущее проверяемое толкование идеи «три фрейма урона»: три независимых
  server-accepted hit events в окне трёх игровых ticks. Третий hit активирует
  условие и сам получает его multiplier; окно может быть продлено target rule.
  Размер окна, число hits и поведение triggering hit являются данными, не
  зашитыми константами.

### Канонический slime rule

Для vanilla slime family базовый профиль:

- `kinetic_contact = 0.10x`;
- `arrow = 0.20x`;
- `bullet = 0.50x`;
- на третьем независимом hit в окне 3 ticks срабатывает `gel_rupture = 3.00x`
  для triggering hit независимо от delivery/element;
- один размноженный volley не может сам создать три независимых события без
  явного `true_multistrike`;
- combat text/debug trace обязан объяснить `gel_rupture`, а не показывать
  игроку необъяснимый скачок.

## 4. Summon affinity по способу получения

Summon weapon получает `AcquisitionAffinity` при первом авторитетном появлении
экземпляра у игрока. Она сохраняется в `GlobalItem`, наследуется при clone,
передаётся по сети и копируется в minion/sentry projectile lineage.

- Crafting, boss drop, ordinary creature drop, biome chest, quest, shop,
  fishing, event и transmutation являются разными affinities.
- `RandomLoot` не отменяет фактический источник: если предмет реально выдан как
  случайный drop, сохраняется `creature_drop`/`boss_drop`; если источник нельзя
  доказать, используется canonical item manifest и audit отмечает fallback.
- Потомки minion projectile наследуют affinity родителя; повторный spawn не
  теряет её.
- Разные экземпляры одного summon item могут иметь разную affinity, если были
  действительно добыты разными путями. Tooltip обязан это показывать.
- Нельзя выводить стихию только из цвета sprite или имени. Manifest задаёт
  соответствие affinity -> delivery/element/behavior и provenance правила.

## 5. Segmented enemies

Сегментированная цель имеет одну `EncounterChainId`, но каждый сегмент — свою
роль и resistance profile.

- Head может быть уязвим к precision/arrow, body — к explosion/area, tail — к
  contact или статусам; конкретные правила задаются manifest.
- Piercing attack не получает полный on-hit proc на каждом соседнем сегменте,
  если profile не разрешает `segment_chain`; это исключает случайный
  многократный proc.
- Разрушение/смена состояния сегмента может открывать core, ослаблять соседей,
  менять скорость или разрывать chain.
- Boss adapters обязаны уважать собственные phase/DR механики мода. DingDong не
  выключает их и не подменяет бинарным immunity.

## 6. Coverage и compatibility adapters

Обязательный runtime audit перечисляет:

- все damage-capable Item, Projectile, NPC contact, buff/DoT, trap и
  environment sources текущего loadout;
- все hostile NPC targets и сегменты;
- долю explicit, inherited, adapter и fallback profiles;
- каждый `unknown` с canonical content key;
- конфликты lineage, immunity и post-multiplier hooks.

Релизный contract: 100% источников и целей имеют объяснимый профиль; `unknown=0`.
Fallback допустим во время разработки только как явно логируемый `1.00x`, не
как доказательство coverage.

### Текущий 67-mod build

| Мод/класс | Обязательное поведение DingDong |
| --- | --- |
| `NothingImmune` | Сначала его removal бинарных immunities, затем DingDong multipliers; DingDong не возвращает скрытую immunity |
| `SeveralfoldProjectiles`, `danmaku` | Projectile lineage/volley ID предотвращает ложный multihit; явные multistrike profiles разрешают настоящую синергию |
| `WeaponEnchantments`, `SignatureEquipment` | DingDong читает уже модифицированный hit и умножает `FinalDamage`, не перезаписывая item stats |
| `ColoredDamageTypes` | Цвет/label может отображать dominant tags, но не является источником классификации |
| `InfiniteMunitions`, `MoreEndlessAmmo` | Bullet/arrow affinity берётся из фактически выбранного ammo/projectile lineage, а не из расходования stack |
| `RandomLoot` | Реальный authoritative acquisition source имеет приоритет над canonical fallback |
| `CalamityMod` | Rogue, classless, true melee, summon, debuff и boss DR adapters; astral/brimstone/abyssal tags |
| `ThoriumMod` | Bard, healer, thrower/summon hybrids и segmented/boss adapters |
| `SOTS` | Void resource/damage, minion lineage и biome target adapters |
| `FargowiltasSouls` | Eternity/combined-AI modifiers сохраняются; DingDong не дублирует boss phase logic |
| `SummonersAssociation` | UI/management совместимы; affinity хранится в DingDong item/projectile data |

Adapters используют optional content keys и `Mod.Call`; ни один большой мод не
становится hard assembly dependency DingDong.

## 7. Локальные biome-depth события

Событие принадлежит `BiomeRegionId + DepthLayer + LocalEventId`, а не всему
миру. Depth layers: `Sky`, `Surface`, `Underground`, `Cavern`, `Underworld`.

- Region создаётся из реального biome envelope, поселения или POI и имеет
  polygon/rect bounds, activation radius и protected anchors.
- Эффекты, spawn pool, музыка/свет, hazards, quest state и loot действуют только
  для игроков внутри region и только на сервере.
- Два игрока в разных биомах могут одновременно видеть разные события.
- Выход последнего участника переводит событие в `Paused`, `Resolved` или
  bounded cleanup; глобальные invasion flags не используются как состояние.
- Каждый фактически существующий `BiomeRegion x DepthLayer` получает минимум
  одно исключительное событие. Невозможная комбинация не синтезируется
  процедурной коробкой: её может открыть только полноценный authored POI.
- События могут менять локальные resistance profiles, NPC dialogue, quests,
  сервисы и POI state, но не весь мир.

## 8. NPC quests, доверие и переселение

Для каждого из 61 canonical residents сохраняются:

- immutable `OriginResidenceId` и origin spawn anchor;
- per-player quest completions и trust;
- приглашение, escort state, reserved destination home и текущий path state;
- death/return state и последний честный failure reason.

State machine:

`Anchored -> Befriended -> Invited -> Following -> Arrived`.

- Опция «переехать со мной» появляется только после заданного для NPC числа
  quests и при наличии заранее валидированного свободного дома.
- Дом резервируется, но vanilla housing assignment не переключается до
  физического прибытия NPC.
- NPC действительно следует за пригласившим игроком. Night/home teleport,
  off-screen relocation и скрытый rescue teleport блокируются.
- Если маршрут невозможен, NPC останавливается и сообщает конкретную причину:
  пропасть, lava, закрытая область, отсутствующая платформа/дверь. Игрок должен
  подготовить путь.
- После смерти NPC всегда возрождается в `OriginResidenceId`, даже если ранее
  жил в городе игрока. Чтобы вернуть его, нужно снова пройти escort, но уже
  выполненные quests/trust не сбрасываются без отдельного сюжетного правила.
- В multiplayer trust принадлежит конкретному player UUID; приглашение и
  destination reservation серверны. Один NPC не может одновременно следовать
  за двумя игроками.

## 9. Escort navigation

Путь строится и исполняется сервером в два уровня:

1. Hierarchical route по roads, settlement/POI anchors, естественным проходам и
   загруженным waypoint sections.
2. Локальный bounded A* вокруг NPC/игрока с действиями `walk`, `step`, `jump`,
   `drop_platform`, `open_door`, `wait`, `swim_safe`.

Ограничения:

- не строить A* по всей карте 8400x2400 каждый tick;
- пересчитывать локальный путь только при изменении tiles, цели или stuck;
- NPC не ломает блоки и не строит мосты автоматически;
- stuck recovery может отступить, перепрыгнуть, открыть дверь или запросить
  помощь, но не teleport;
- при boss/event danger NPC использует authored defensive behavior или ждёт в
  безопасной точке, сохраняя escort state;
- path budget, explored nodes и failure reason входят в `/dingdong audit`.

## 10. Quests, POI и shops

- Quests привязаны к конкретному NPC, его origin residence, локальным POI и
  biome-depth events; generic global fetch RNG не считается уникальным quest.
- Completion меняет trust, dialogue, ассортимент и доступные escort действия.
- Shops используют уже существующий canonical NPC registry; unlock state
  серверно сохраняется и синхронизируется.
- POI states `Dormant`, `Discovered`, `Activated`, `Restored` могут открывать
  quests/events и менять локальную resistance ecology.
- Большие моды подключаются adapters: их boss/event/biome progression читается,
  но DingDong не переписывает их глобальные флаги.

## 11. Multiplayer и persistence

- World: origin residences, POI/event states, NPC escort/destination state,
  target ecology versions.
- Player: quest completions, trust, invitations и acquisition affinities,
  которые принадлежат игроку.
- Item/projectile: acquisition affinity, attack lineage и source tags.
- Join/rejoin получает versioned snapshot; deltas имеют monotonically
  increasing revision. Клиент не может сообщить damage multiplier, trust,
  quest completion, event resolution или NPC arrival как авторитетный факт.
- Save/reload и mod-update migrations fail-closed; неизвестная новая версия не
  сбрасывает silently origin homes или trust.

## 12. Первый вертикальный срез

Срез доказывает архитектуру, но не считается полным gameplay layer:

1. Базовый прыжок 5 tiles с dedicated physics contract.
2. Vanilla slime family с точными `0.10/0.20/0.50/3.00` правилами и hit trace.
3. Два summon examples с разным provenance: `SlimeStaff` как
   `creature_drop/gel` и `FinchStaff` как `worldgen/nature`.
4. Одно локальное `Jungle + Cavern` событие вокруг полноценного authored POI;
   игрок вне bounds не получает ни одного эффекта.
5. Один canonical resident с тремя authored quests, reserved valid house,
   server-side follow/path state и death return в origin residence.
6. Save/reload и two-player ownership simulation.

Минимальные gates: pure contract tests -> compile -> dedicated synthetic combat
probe -> disposable Large For the Worthy save/reload. Клиент нужен только для
визуальной оценки movement/telegraph после технического PASS.

## 13. Definition of Done gameplay layer

- jump apex 5 tiles и multiplayer prediction совпадают;
- source/target coverage audit `unknown=0` для зафиксированного 67-mod loadout;
- все multipliers имеют deterministic trace и bounded rule provenance;
- summon affinity переживает save/reload, clone и projectile descendants;
- segmented adapters не размножают proc по одному attack instance;
- каждое поддерживаемое biome-depth region имеет локальное исключительное
  событие, не затрагивающее игроков снаружи;
- все 61 residents имеют origin spawn, quest/trust contract и escort policy;
- ни один NPC не переселяется teleport fallback;
- shops/POI/events/quests сохраняются и серверно синхронизируются;
- полный Large For the Worthy full-modpack worldgen/load/join/rejoin gate PASS;
- owner visual/gameplay acceptance выполнен; до этого статус только technical.

