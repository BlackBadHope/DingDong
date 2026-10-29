# DingDong gameplay doctrine

This document is normative. A feature is not complete because a class exists; it is complete only when its multiplayer acceptance gate passes.

## Core loop

1. Explore the enlarged world and discover authored resident homes and other points of interest.
2. Accept resident quests. Location quests mark a bounded search area and the exact current objective on the map.
3. Build resident-specific trust. The baseline is three quests; later-biome residents require progressively more, and requirements are doubled after Hardmode begins.
4. At 100% trust, invite the resident to follow. The server allows one active escort per player.
5. Lead the resident on foot to the immutable world spawn. Beds and temporary personal spawn points do not change this anchor.
6. After both player and resident reach world spawn, select a complete, vacant, suitable NPC house with a Residence Flag.
7. The selected house must be within 1000 tiles of the remembered world spawn. The resident then walks the second leg and is assigned only after physically arriving.
8. Vanilla housing reassignment is not an alternate path: until arrival, the server restores the resident's doctrine-controlled home anchor.

## World and combat

- Rare loot keeps its low global rate and does not gain extra rolls from stacked vanilla or mod difficulty/luck branches.
- At night, surface spawn pressure is 3x and underground spawn pressure is 1.5x. Hostile contact damage is 1.5x.
- Unmounted base jump apex is one half of vanilla. Accessory and mod bonuses remain additive above the replaced base contribution.
- On a 28000x7200 x10-area world, repeatable underground POI targets retain the vanilla-large density across the full area: width-scaled targets gain the missing depth factor, while targets already scaled by world area are not multiplied twice. Surface and sky targets scale only with usable width. Unique progression structures and the 61 authored resident homes remain canonical rather than being duplicated ten times.
- Provider biome clamps that define one local feature (for example Calamity Sunken Sea) remain at provider scale; continuation adds coverage or independent placements and never stretches one feature into a map-scale dome.

## Multiplayer authority

- Quest progress is per persistent player identity and world.
- Trust does not reserve a world NPC. The first qualified player who explicitly starts the escort becomes its server-owned escort owner; another player's trust completion cannot steal that ownership.
- Acceptance, completion, rewards, trust transitions, flag claims, escort transitions and final housing assignment are server-authoritative.
- Client packets are requests only and are range-, identity-, suitability-, occupancy- and state-validated by the server.
- Disconnects never transfer an escort to another player identity.

## Acceptance gates

- Trust: early/late biome requirements, Hardmode doubling, 100% display and persistence.
- Escort: no preclaimed-home shortcut, world-spawn arrival, one escort per player, 1000-tile bound, suitable/vacant room, two physical walking legs, disconnect/reload continuity.
- Anti-bypass: bed near the source NPC, vanilla housing UI, remote flag request, occupied room and out-of-radius house all fail.
- Night pressure: server-observed surface 3x, underground 1.5x, daytime 1x and hostile contact damage 1.5x.
- Jump: measured unobstructed apex equals 50% of the vanilla-base simulation before equipment bonuses.
- World: POI density receipt, 61 authored homes, map readability, accessible entrances and absence of artificial map-scale seams or solid slabs.
