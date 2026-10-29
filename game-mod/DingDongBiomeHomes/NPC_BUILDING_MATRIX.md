# NPC -> authored building matrix

Это единственный счётчик покрытия этапа 3. Строка считается `APPROVED` только
после legal, binary и visual intake конкретной геометрии. Поля `Source asset`
и `Preset ID` должны указывать на один проверяемый источник; перекраска,
масштабирование или замена крыши не создают новую строку покрытия.

Текущий итог: **61/61 MAPPED_PENDING_GATES, 0/61 APPROVED**.
`MAPPED_PENDING_GATES` означает только уникальное NPC/biome назначение после
legal, visual-source, binary intake и dedicated-server housing PASS; anchor,
protected footprint, biome/seed fallback и пользовательский visual PASS ещё
обязательны.
`NpcAuthoredPresetManifest` остаётся пустым.

| NPC key | Biome | Requested authored identity | Role | Status | Source asset | Preset ID |
| --- | --- | --- | --- | --- | --- | --- |
| AlchemistNPCLite/Alchemist | Forest | Glasshouse Apothecary | Healer | MAPPED_PENDING_GATES | `Structures/Structify/Greenhouse1.shstruct` | `Greenhouse1` |
| AlchemistNPCLite/Architect | Forest | Drafting Guildhall | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/spawn-forest-street.shstruct` | `Greaveil_SpawnForestStreet` |
| AlchemistNPCLite/Brewer | Forest | Copper Kettle Brewery | Healer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/west-beach-cottage.shstruct` | `Greaveil_WestBeachCottage` |
| AlchemistNPCLite/Jeweler | Forest | Gemcutter Atelier | Trader | MAPPED_PENDING_GATES | `Structures/Structify/House3_Expination.shstruct` | `House3_Expination` |
| AlchemistNPCLite/Musician | Forest | Woodland Concert Hall | Entertainer | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_NadikiMansion.shstruct` | `R7_NadikiMansion` |
| AlchemistNPCLite/Operator | Forest | Telegraph Exchange | Engineer | MAPPED_PENDING_GATES | `Structures/Structify/MediumHouse1.shstruct` | `MediumHouse1` |
| AlchemistNPCLite/Tinkerer | Forest | Clockwork Shed | Engineer | MAPPED_PENDING_GATES | `Structures/Structify/SmallHouse1.shstruct` | `SmallHouse1` |
| AlchemistNPCLite/YoungBrewer | Forest | Young Brewer Herb Cottage | Healer | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_EsonoraHome.shstruct` | `R7_EsonoraHome` |
| Fargowiltas/LumberJack | Forest | Timber Mill Lodge | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_CastleCoolResidence.shstruct` | `R7_CastleCoolResidence` |
| Fargowiltas/Mutant | Forest | Mutagen Observatory | Warrior | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/steampunk-orb.shstruct` | `Aegis8_SteampunkOrb` |
| Fargowiltas/Squirrel | Forest | Acorn Canopy Lodge | Naturalist | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/forest-treehouse.shstruct` | `Aegis8_ForestTreehouse` |
| Terraria/BestiaryGirl | Forest | Wildlife Sanctuary | Naturalist | MAPPED_PENDING_GATES | `Structures/PrivateHome/HuadiaoUpdated/Huadiao_ElementalSkyHouse.shstruct` | `Huadiao_ElementalSkyHouse` |
| Terraria/Golfer | Forest | Forest Clubhouse | Entertainer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/mountain-lodge.shstruct` | `Aegis8_MountainLodge` |
| Terraria/Guide | Forest | Guide's Library Lodge | Scholar | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/hill-manor.shstruct` | `Greaveil_HillManor` |
| Terraria/Merchant | Forest | Old Forest Trading Post | Trader | MAPPED_PENDING_GATES | `Structures/Structify/House1_Expination.shstruct` | `House1_Expination` |
| ThoriumMod/Cobbler | Forest | Cobbler Rowhouse | Trader | MAPPED_PENDING_GATES | `Structures/Structify/House2_Expination.shstruct` | `House2_Expination` |
| ThoriumMod/ConfusedZombie | Forest | Reclaimed Ivy Ruin | Scholar | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/jungle-watchtower.shstruct` | `Ziiio_JungleWatchtower` |
| ThoriumMod/Cook | Forest | Great Hearth Inn | Chef | MAPPED_PENDING_GATES | `Structures/Structify/LargeHouse1.shstruct` | `LargeHouse1` |
| ThoriumMod/Tracker | Forest | Ranger Watch Lodge | Warrior | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/jungle-east-house.shstruct` | `Greaveil_JungleEastHouse` |
| ThoriumMod/WeaponMaster | Forest | Weapon Master's Dojo | Warrior | MAPPED_PENDING_GATES | `Structures/Structify/Castle1.shstruct` | `Castle1` |
| CalamityMod/Archmage | Snow | Permafrost Observatory | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_SnowMansion.shstruct` | `R7_SnowMansion` |
| Terraria/Cyborg | Snow | Cryogenic Laboratory | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/airship-house.shstruct` | `Ziiio_AirshipHouse` |
| Terraria/Mechanic | Snow | Boreal Gearworks | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/snow-steampunk-manor.shstruct` | `Greaveil_SnowSteampunkManor` |
| Terraria/SantaClaus | Snow | Northwind Toy Workshop | Entertainer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/snow-castle.shstruct` | `Aegis8_SnowCastle` |
| Terraria/TaxCollector | Snow | Frost Ledger Hall | Trader | MAPPED_PENDING_GATES | `Structures/PrivateHome/Hannibal/Hannibal_SnowResidence.shstruct` | `Hannibal_SnowResidence` |
| CalamityMod/Bandit | Desert | Smuggler Casbah | Trader | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/desert-oasis-settlement.shstruct` | `Aegis8_DesertOasisSettlement` |
| Terraria/ArmsDealer | Desert | Dune Gunsmith Fort | Warrior | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/desert-palace.shstruct` | `Greaveil_DesertPalace` |
| Terraria/DyeTrader | Desert | Prismatic Dye Souk | Artist | MAPPED_PENDING_GATES | `Structures/PrivateHome/Magoozy/Magoozy_Palace.shstruct` | `Magoozy_Palace` |
| Terraria/Steampunker | Desert | Brass Sand Foundry | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/WorldOfBuildings/desert-palace.shstruct` | `WoB_DesertPalace` |
| ThoriumMod/DesertAcolyte | Desert | Sun Acolyte Temple | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/desert-sky-palace.shstruct` | `Aegis8_DesertSkyPalace` |
| CalValEX/Jharim | Jungle | Jharim Jungle Shrine | Royal | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/jungle-tree-city.shstruct` | `Aegis8_JungleTreeCity` |
| Terraria/Dryad | Jungle | Living Grove House | Naturalist | MAPPED_PENDING_GATES | `Structures/PrivateHome/HuadiaoUpdated/Huadiao_JungleGothicManor.shstruct` | `Huadiao_JungleGothicManor` |
| Terraria/Painter | Jungle | Canopy Artist Studio | Artist | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/jungle-west-house.shstruct` | `Greaveil_JungleWestHouse` |
| Terraria/WitchDoctor | Jungle | Mahogany Spirit House | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/witch-doctor-hut.shstruct` | `Ziiio_WitchDoctorHut` |
| ThoriumMod/Druid | Jungle | Ancient Druid Conservatory | Naturalist | MAPPED_PENDING_GATES | `Structures/PrivateHome/WorldOfBuildings/jungle-treehouse.shstruct` | `WoB_JungleTreehouse` |
| CalamityMod/SeaKing | Ocean | Tide King's Palace | Royal | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/west-galleon.shstruct` | `Greaveil_WestGalleon` |
| CalValEX/JellyPriestNPC | Ocean | Jellyfish Coral Chapel | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/WorldOfBuildings/harbor-center-house.shstruct` | `WoB_HarborCenterHouse` |
| FishermanNPC/Fisherman | Ocean | Old Fisherman's Smokehouse | Angler | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8Showcase/snow-harbor-settlement.shstruct` | `Aegis8Showcase_SnowHarborSettlement` |
| Terraria/Angler | Ocean | Bright Bait Shack | Angler | MAPPED_PENDING_GATES | `Structures/Structify/FishingPond1.shstruct` | `FishingPond1` |
| Terraria/Pirate | Ocean | Corsair Galleon House | Angler | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8Showcase/cloud-city.shstruct` | `Aegis8Showcase_CloudCity` |
| Terraria/Stylist | Ocean | Seabreeze Salon | Artist | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/barber-shop.shstruct` | `Ziiio_BarberShop` |
| ThoriumMod/Diverman | Ocean | Diving Bell Laboratory | Angler | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/west-lighthouse.shstruct` | `Greaveil_WestLighthouse` |
| UnuBattleRods/Fishylady | Ocean | Mermaid Grotto House | Angler | MAPPED_PENDING_GATES | `Structures/PrivateHome/WorldOfBuildings/harbor-west-house.shstruct` | `WoB_HarborWestHouse` |
| MagicStorage/Golem | Underground | Arcane Storage Vault | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8Showcase/moon-citadel.shstruct` | `Aegis8Showcase_MoonCitadel` |
| miningcracks_take_on_luiafk/Deeps | Underground | Deep Mine Station | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/cavern-lake-manor.shstruct` | `Aegis8_CavernLakeManor` |
| Terraria/Clothier | Underground | Catacomb Tailor Hall | Artist | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/desert-gothic-manor.shstruct` | `Aegis8_DesertGothicManor` |
| Terraria/Demolitionist | Underground | Cavern Blastworks | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/cavern-lake-smithy.shstruct` | `Aegis8_CavernLakeSmithy` |
| Terraria/GoblinTinkerer | Underground | Goblin Mechanist Burrow | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/steampunker-workshop.shstruct` | `Ziiio_SteampunkerWorkshop` |
| ThoriumMod/Blacksmith | Underground | Basalt Grand Forge | Engineer | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_UnrealDeltaCastle.shstruct` | `R7_UnrealDeltaCastle` |
| CalValEX/OracleNPC | Hallow | Crystal Oracle Rotunda | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/HuadiaoUpdated/Huadiao_CrescentVillage.shstruct` | `Huadiao_CrescentVillage` |
| Fargowiltas/Deviantt | Hallow | Prismatic Curio Boutique | Trader | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_PrincessCastle.shstruct` | `R7_PrincessCastle` |
| Terraria/DD2Bartender | Hallow | Aether Defender Tavern | Warrior | MAPPED_PENDING_GATES | `Structures/PrivateHome/ZiiioNpcThemed/tavern.shstruct` | `Ziiio_Tavern` |
| Terraria/Nurse | Hallow | Hallowed Sanctuary Clinic | Healer | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/snow-lodge.shstruct` | `Greaveil_SnowLodge` |
| Terraria/PartyGirl | Hallow | Confetti Pavilion | Entertainer | MAPPED_PENDING_GATES | `Structures/PrivateHome/RoadmapR7/R7_ScarletDevilMansion.shstruct` | `R7_ScarletDevilMansion` |
| Terraria/Princess | Hallow | Rose Crystal Palace | Royal | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/hallow-cliff-manor.shstruct` | `Aegis8_HallowCliffManor` |
| Terraria/Wizard | Hallow | Arcane Star Tower | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/spawn-tree-tower.shstruct` | `Greaveil_SpawnTreeTower` |
| ThoriumMod/Spiritualist | Hallow | Spirit Lantern Chapel | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/floating-pagoda.shstruct` | `Aegis8_FloatingPagoda` |
| WeaponEnchantments/Witch | Hallow | Enchanter Coven House | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/Aegis8/hallow-ridge-village.shstruct` | `Aegis8_HallowRidgeVillage` |
| Terraria/Truffle | Mushroom | Luminous Spore Dome | Naturalist | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/mushroom-spire.shstruct` | `Greaveil_MushroomSpire` |
| CalamityMod/BrimstoneWitch | Underworld | Brimstone Witch Spire | Mystic | MAPPED_PENDING_GATES | `Structures/PrivateHome/HuadiaoUpdated/Huadiao_FireSkyVillage.shstruct` | `Huadiao_FireSkyVillage` |
| Fargowiltas/Abominationn | Underworld | Ashen Abomination Monastery | Warrior | MAPPED_PENDING_GATES | `Structures/PrivateHome/Greaveil/underworld-castle.shstruct` | `Greaveil_UnderworldCastle` |

## Биомный баланс

| Biome | Required | Approved | Gap |
| --- | ---: | ---: | ---: |
| Forest | 20 | 0 | 20 |
| Snow | 5 | 0 | 5 |
| Desert | 5 | 0 | 5 |
| Jungle | 5 | 0 | 5 |
| Ocean | 8 | 0 | 8 |
| Underground POI scenes | 6 | 0 | 6 |
| Hallow | 9 | 0 | 9 |
| Mushroom | 1 | 0 | 1 |
| Underworld POI scenes | 2 | 0 | 2 |
| **Total** | **61** | **0** | **61** |
