# Ocean Expeditions — 0.29.6

Ocean Expeditions expands the ordinary ocean on the side opposite Calamity's
Abyss. It is generated as part of a **new continental world**, rather than being
carved into an existing save. Older worlds without its layout record keep their
terrain and do not activate the new ocean encounter zones.

## Geography and exploration

In a 28,000 × 7,200 world, the ocean occupies a 4,666-tile coastal region. A short
continental shelf drops into broad open water and lower cavern depths close to
the Underworld. Seven islands interrupt the surface. Deep water provides room
for large creatures, while the continental cliff contains separate dry grottoes.
The basin's bottom retains a protective margin above Hell.

Three of the islands reserve their ground for coastal NPC residences. Exploration
caches are placed on the other islands and underwater landmarks, so a cache cannot
consume the only usable building site for a lighthouse or shore house. Harbor
islands have shallow keels; at least 72% of the surface remains open water.

The generator reads native structure reservations and preserves important
objects, foreign building materials and walls. It permits ordinary terrain and
ambient decorations to be replaced, so individual stalactites do not leave huge
plugs of rock throughout the basin. Authored structures can remain as protected
features inside the water. Placement checks can omit or resize a proposed point
of interest when existing content occupies its space.

Island and underwater landmarks contain finite exploration caches. Air grottoes
use walk-through liquid-blocking bubble entrances, allowing players to move
between the flooded ocean and dry interiors. Grottoes have their own encounter
zone and two inhabitants. The generator requires at least two accepted grottoes
and a minimum open-water share; it rejects generation if the ocean cannot meet
those conditions.

## Twelve new creatures

| Creature | Behaviour and habitat | Distinct material |
| --- | --- | --- |
| Pelagic Titan | Rare giant in a large clear volume of open/deep water; telegraphed charge | Pelagic Scale |
| Hadal Maw | Hardmode giant in hadal water; charge and spine attacks | Hadal Lens |
| Coral Pincher | Small bottom-scuttling crustacean in shelf/open water | Coral Claw |
| Glassfin Darter | Small fish with bursts of speed in shelf/open water | Glass Fin |
| Ribbon Skate | Medium swimmer that circles its target in open/deep water | Ribbon Gill |
| Lantern Jelly | Pulsing luminous swimmer in deep/hadal water | Luminous Gel |
| Brine Puffer | Alternates mobile and inflated defensive phases in open/deep water | Brine Bladder |
| Silt Ribbon | Long, undulating swimmer in deep/hadal water | Silt Whisker |
| Sailfin Hunter | Large fish with a warning phase followed by a committed charge | Sailbone |
| Vent Nautilus | Hardmode hadal creature with guarded and firing phases | Vent Core |
| Saltback Custodian | Walking and hopping crustacean in dry grottoes | Salt Crystal and Grotto Geode |
| Glasswing Skimmer | Flying grotto inhabitant | Glass Wing and Grotto Geode |

Sea Essence is a shared material. Creatures also have ordinary Terraria loot
appropriate to their type. Giants require a sampled clear-water envelope of
3,072 × 1,536 pixels in addition to body collision checks. They are rare and
limited to one of each type at a time; their segmented appearance remains a
single NPC for damage and collision.

Three banner families cover reef fauna, grotto fauna and the two giants. Their
members share each family's native kill counter and banner association.

## Equipment and rewards

The expansion registers 25 items: 14 materials, eight equipment rewards and
three banners. Exploration and combat materials feed these rewards:

- **Island Navigator:** compass and depth readouts, plus movement speed in water.
- **Pelagic Harness:** extra breath capacity, flipper movement and water freedom.
- **Grotto Lantern:** personal light; night vision and regeneration in air grottoes.
- **Pressure Ward:** damage reduction and generic damage while wet; its recipe
  requires a Mythril Anvil and Hardmode materials.
- **Reefwalker Boots:** swimming assistance and movement speed while wet.
- **Sounding Pendant:** depth readout and treasure detection while wet.
- **Tidebreaker:** a thrown melee lance with limited penetration and lifetime.
- **Sailbone Bow:** an arrow-using ranged weapon crafted from ocean materials.

The item icons reference installed Terraria resources at runtime. Those game
images are not included in the downloadable ocean art.

## Animation

Coral Pincher and Saltback Custodian use a fixed torso, six articulated walking
legs and two claw arms. Movement changes limb transforms rather than replacing
the creature with frames that change its anatomy. Glassfin Darter and Sailfin
Hunter keep their heads anchored while the body and tail bend continuously.
Long creatures use segmented rendering; the skate, jelly, puffer, nautilus and
skimmer also use purpose-made frame sequences for their characteristic motion.

## Compatibility and updating

The expansion stores accepted grottoes and landmarks in the world's mod data
and sends that layout to multiplayer clients. Enemy decisions run on the server
and synchronize their phases. Every player and the server need the same update.
Two-client gameplay acceptance is reported separately from registration, network
serialization and rendering checks; see the release validation receipt.

The public edition integrates its original world-generation guards directly.
It does not require the private `PackWorldgenFix` package. External terrain
providers retain their version/fingerprint checks. Existing worlds and players
are not migrated or rewritten by installing this update.
