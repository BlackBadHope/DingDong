# DingDong v0.20 — stable gameplay checkpoint

Version 0.20 keeps the existing 61-resident authored-settlement baseline and
stabilizes the gameplay systems layered on top of it. It does not change the
owner visual-approval ledger: private Workshop assets remain private and the
runtime approval count remains `0/61` until the owner performs the visual gate.

## Resident Quest System 3.0

- Every canonical resident can hold one active quest per player, so up to 61
  resident quests can be completed and turned in in any order.
- The loaded-content catalog still supplies hundreds of boss, biome, banner
  and mod-progression targets, but selection now uses deterministic shuffled
  category bags. Local, supply, defense, biome, banner and progression work
  cannot disappear inside the much larger banner catalog.
- The first three quests for a resident form a readable onboarding sequence:
  inspect the residence, deliver supplies and defend it. Later category bags
  are independently shuffled per player, resident and cycle. Each six-entry
  half-bag contains one boss or mod-progression slot, bounding the gap between
  progression-moving offers to at most eleven completed quests.
- Generic mod-progression contracts only select mods that still have an
  undefeated indexed boss. Active frozen targets are rebound by stable content
  key after a modpack update; an unavailable unfinished target is replaced
  without deleting completed trust.
- Boss participation credit is server-authoritative and includes every active
  player recorded by Terraria as a participant, not only the final hitter.

## Rewards

- Every completion grants difficulty- and world-progression-scaled coins.
- Every completion also grants one supply cache for the accepted world tier:
  Classic, Expert, Master or Legendary.
- Every fifth total resident-quest completion grants a second cache.
- Boss base rewards scale with the indexed target's health and contact threat,
  with a bounded cap. This makes later world/mod bosses materially more
  valuable without duplicating their treasure bags.
- Banner contracts consume one submitted banner, preventing one item from
  completing an unlimited number of resident contracts.

## Multiplayer and saves

- Quest snapshots use protocol version 2, a deterministic loaded-catalog
  fingerprint and a monotonically increasing per-player revision.
- Clients reject mismatched catalogs, malformed resident keys, duplicate rows,
  invalid content IDs, oversized strings and stale revisions.
- Active quest definitions remain frozen in player/world data, so catalog
  reordering cannot silently change an accepted objective or reward.
- The quest journal refreshes when a new authoritative revision arrives and
  periodically refreshes inventory-backed supply/banner readiness while open.

## Release gates

The stable package requires a warning-free compile, exact 67-mod load,
Quest System 3.0/localization markers, one new disposable Large literal
`for the worthy` world generation, reload, archive manifest audit and proof
that live mods, enabled configuration and user worlds/players were untouched.
