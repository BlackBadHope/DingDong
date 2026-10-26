# DingDong Public: modpack and Biome Homes

DingDong Public is a Terraria/tModLoader modpack built around a much larger
world, long-distance exploration, cooperative progression and a custom mod,
**DingDong: Biome Homes**. It combines major content mods with an integration
layer that adapts world generation, progression, inventory tools and controls
to the selected pack.

## A world built for exploration

The continental world is 28,000 tiles wide and 7,200 tiles deep: **201.6 million
tiles, ten times the area of a vanilla Large world**. This is a two-dimensional
area comparison, not a claim of ten times the width or a third spatial axis.
The expanded map needs adapted depth thresholds, coastal regions and Underworld
behaviour rather than a larger allocation alone.

Biome Homes adds 61 NPC residence assignments, discovery and quest systems,
custom equipment and integration with the pack's content providers. NPC services
are part of the exploration loop: the world is not designed to put every useful
merchant beside the starting point. The public edition uses original procedural
replacements for private Workshop buildings; those private schematics are not
distributed.

Calamity, Thorium, Secrets of the Shadows, Fargo's mods and other providers add
their own biomes, structures, enemies, bosses and progression. DingDong keeps
their native generation passes while applying the pack's compatibility rules.
The selected For the Worthy/Master-style experience can be demanding. Fargo's
enemy difficulty remains available, while its item-specific penalties and
behaviour overrides are disabled by the pack integration. This includes former
progression restrictions on weapons such as Water Bolt.

## The ordinary ocean becomes an expedition

Version 0.29.6 adds Ocean Expeditions to newly generated continental worlds.
The ordinary ocean opposite Calamity's Abyss becomes a wide, deep basin with
islands, large swimming spaces, underwater landmarks, finite caches and dry air
grottoes near the continental cliff. It reaches the lower caverns while retaining
separation from Hell.

Twelve new creatures cover several sizes and movement styles: two rare giants,
eight other swimmers and bottom-dwellers, and two dry-grotto inhabitants. They
provide shared Sea Essence, distinct materials, ordinary loot and crafting paths
for eight equipment rewards. Giant encounters require a large clear-water
envelope, so they do not belong in tiny pools. Fixed-body crustacean rigs and
continuous fish-body bending replace the rejected animations that changed
anatomy or barely moved a fin.

See [Ocean Expeditions](OCEAN-EXPEDITIONS.md) for the creature list, equipment,
geography, animation and compatibility details.

## Weapon growth and enchantments

Signature Equipment and Weapon Enchantments serve different purposes. Signature
mastery rewards weapon use with progression bonuses. Weapon Enchantments levels
provide enchantment capacity, while infusion changes the weapon's base power.
Their experience systems remain separate; they are not two names for one level.
Displayed weapon damage already reflects applicable equipment and progression
effects.

The integration limits Signature's ordinary combat-stat growth so that weapon
upgrades remain meaningful. Projectile quantity and minion-slot discounts can
continue growing without a mastery-level ceiling; penetration and projectile
lifetime are capped at twice their original values. Early mastery
is faster, while world progression and high mastery slow further experience
gain. Detailed explanations belong in DingDong Menu instead of filling every
item tooltip. Large effects from final Fargo accessories remain distinct from
weapon mastery.

## Inventory and crafting comfort

New characters receive a complete Magic Storage starter kit providing exactly
128 storage slots, with the heart, crafting access, extra access and connectors
needed to use it. Rejoining or dying does not repeatedly grant the kit.

Auto Uncraft and Auto Trash use two real inventory slots with separate switches
and editable rule lists. Uncrafting selects one basic recipe rather than offering
every furniture variant in a recipe group. This blocks conversions such as
turning an ordinary wooden furnishing into an expensive material variant.
Favourited items are protected, and a full inventory pauses automatic uncrafting.
The duplicated trash interface is disabled while the retained mod's other
features remain available.

Magic Storage, Recipe Browser, Ore Excavator and other quality-of-life mods
support the larger exploration loop. Fishing also has its own group of content
and automation mods, alongside DingDong's water habitats and depth integration.

## Controls and multiplayer

Related abilities share sensible default controls: **F** for dashes, **Y** for
armour abilities, **V/X** for rage/adrenaline and **Z** for cursor teleportation.
**F1** opens DingDong Menu, **F2** opens quests, and **F3/F4** open the relevant
boss interfaces. Default migration preserves custom assignments and keeps a
backup; it does not reset the keyboard layout on every launch.

The pack includes multiplayer integration for world data, building tools,
storage and encounter behaviour. All clients and the server must use matching
updates. A successful loading, serialization or rendering test is reported
separately from real two-client gameplay, long combat and live performance.
The [acceptance notes](../ACCEPTANCE.md) and release validation receipt describe
what was actually exercised.

## Installation, updates and saves

The public preset contains 66 mods including DingDong and the official Russian
translation dependency. The exact list is pinned in
[public-dependencies.json](../bootstrap/public-dependencies.json).

The installer obtains the project's signed bundle from GitHub, uses the installed
Steam runtime, and resolves external mods from their official Steam or author
sources. Steam entitlement to Terraria is required. Terraria binaries, full
patched game assemblies, third-party mod mirrors, private schematics and user
saves are not included in the public release.

Updates verify signatures, file hashes and the allowed package contents. The
public installation has its own profile and rollback backups. Existing
Steam/FTP worlds and characters are not imported automatically. Updating does
not regenerate an old world: Ocean Expeditions needs a new continental world.
Large maps and a full modpack can require substantial memory, especially when
hosting and playing on the same computer. Memory admission failures are handled
separately from evidence of save corruption.
