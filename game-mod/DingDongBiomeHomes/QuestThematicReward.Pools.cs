using Terraria.ID;

namespace DingDongBiomeHomes;

internal static partial class QuestThematicReward
{
	private static ThematicRewardOption[] ArchetypePool(HomeArchetype archetype) => archetype switch
	{
		HomeArchetype.Scholar => [O(ItemID.Book, 1, 3), O(ItemID.SpellTome), O(ItemID.FallenStar, 2, 5), O(ItemID.Feather, 2, 6), O(ItemID.BlackInk), O(ItemID.Glass, 5, 12), O(ItemID.Silk, 2, 6), O(ItemID.Lens, 2, 5), O(ItemID.ManaPotion, 2, 4), O(ItemID.WormholePotion)],
		HomeArchetype.Trader => [O(ItemID.GoldDust, 2, 6), O(ItemID.Safe), O(ItemID.PiggyBank), O(ItemID.Extractinator), O(ItemID.CratePotion), O(ItemID.Ruby, 1, 2), O(ItemID.Emerald, 1, 2), O(ItemID.Sapphire, 1, 2), O(ItemID.Topaz, 1, 2), O(ItemID.Diamond, 1, 2)],
		HomeArchetype.Healer => [O(ItemID.HealingPotion, 2, 5), O(ItemID.RegenerationPotion), O(ItemID.IronskinPotion), O(ItemID.EndurancePotion), O(ItemID.LifeforcePotion), O(ItemID.HeartreachPotion), O(ItemID.BottledHoney, 2, 5), O(ItemID.BottledWater, 3, 8), O(ItemID.RestorationPotion, 1, 3), O(ItemID.LesserRestorationPotion, 2, 5)],
		HomeArchetype.Engineer => [O(ItemID.Wire, 20, 50), O(ItemID.Actuator, 5, 12), O(ItemID.Chain, 5, 12), O(ItemID.Cog, 5, 12), O(ItemID.MinecartTrack, 20, 50), O(ItemID.IronBar, 2, 5), O(ItemID.LeadBar, 2, 5), O(ItemID.Bomb, 2, 5), O(ItemID.StickyBomb, 2, 5), O(ItemID.Dynamite)],
		HomeArchetype.Mystic => [O(ItemID.FallenStar, 2, 5), O(ItemID.ManaPotion, 2, 4), O(ItemID.MagicPowerPotion), O(ItemID.ManaRegenerationPotion), O(ItemID.CrystalShard, 3, 8), O(ItemID.PixieDust, 4, 10), O(ItemID.UnicornHorn, 1, 2), O(ItemID.Amethyst, 1, 3), O(ItemID.Diamond, 1, 2), O(ItemID.SpellTome)],
		HomeArchetype.Angler => [O(ItemID.Worm, 3, 8), O(ItemID.EnchantedNightcrawler, 2, 5), O(ItemID.ApprenticeBait, 2, 5), O(ItemID.JourneymanBait, 2, 5), O(ItemID.MasterBait, 1, 3), O(ItemID.FishingPotion), O(ItemID.SonarPotion), O(ItemID.CratePotion), O(ItemID.Coral, 2, 6), O(ItemID.Starfish, 1, 3)],
		HomeArchetype.Warrior => [O(ItemID.IronskinPotion), O(ItemID.EndurancePotion), O(ItemID.RagePotion), O(ItemID.WrathPotion), O(ItemID.BattlePotion), O(ItemID.ThornsPotion), O(ItemID.TitanPotion), O(ItemID.ArcheryPotion), O(ItemID.Grenade, 3, 8), O(ItemID.HellfireArrow, 12, 30)],
		HomeArchetype.Artist => [O(ItemID.RedPaint, 8, 18), O(ItemID.BluePaint, 8, 18), O(ItemID.YellowPaint, 8, 18), O(ItemID.GreenPaint, 8, 18), O(ItemID.OrangePaint, 8, 18), O(ItemID.VioletPaint, 8, 18), O(ItemID.PinkPaint, 8, 18), O(ItemID.BlackPaint, 8, 18), O(ItemID.WhitePaint, 8, 18), O(ItemID.DeepPurplePaint, 5, 12)],
		HomeArchetype.Chef => [O(ItemID.Mushroom, 3, 8), O(ItemID.GlowingMushroom, 5, 12), O(ItemID.CookedFish, 1, 3), O(ItemID.CookedShrimp, 1, 3), O(ItemID.BowlofSoup, 1, 3), O(ItemID.PumpkinPie, 1, 3), O(ItemID.Ale, 1, 3), O(ItemID.Sake, 1, 3), O(ItemID.Apple, 1, 3), O(ItemID.Grapes, 1, 3)],
		HomeArchetype.Entertainer => [O(ItemID.Confetti, 15, 35), O(ItemID.ConfettiGun), O(ItemID.SmokeBomb, 2, 6), O(ItemID.BeachBall), O(ItemID.Bubble, 8, 18), O(ItemID.FireworkFountain), O(ItemID.RedRocket, 2, 5), O(ItemID.BlueRocket, 2, 5), O(ItemID.GreenRocket, 2, 5), O(ItemID.YellowRocket, 2, 5)],
		HomeArchetype.Naturalist => [O(ItemID.Acorn, 4, 10), O(ItemID.Daybloom, 2, 5), O(ItemID.Moonglow, 2, 5), O(ItemID.Blinkroot, 2, 5), O(ItemID.Deathweed, 2, 5), O(ItemID.Waterleaf, 2, 5), O(ItemID.Fireblossom, 2, 5), O(ItemID.Shiverthorn, 2, 5), O(ItemID.HerbBag), O(ItemID.JungleGrassSeeds, 1, 3)],
		HomeArchetype.Royal => [O(ItemID.GoldBar, 1, 3), O(ItemID.PlatinumBar, 1, 3), O(ItemID.Silk, 3, 8), O(ItemID.Ruby, 1, 2), O(ItemID.Emerald, 1, 2), O(ItemID.Sapphire, 1, 2), O(ItemID.Topaz, 1, 2), O(ItemID.Amethyst, 1, 2), O(ItemID.Diamond, 1, 2), O(ItemID.Amber, 1, 2)],
		_ => [O(ItemID.HealingPotion)]
	};

	private static ThematicRewardOption[] BiomePool(HomeBiome biome) => biome switch
	{
		HomeBiome.Forest => [O(ItemID.Wood, 15, 30), O(ItemID.Acorn, 4, 10), O(ItemID.Daybloom, 2, 5), O(ItemID.Mushroom, 3, 8), O(ItemID.Gel, 12, 30), O(ItemID.Lens, 2, 5), O(ItemID.Apple, 1, 3), O(ItemID.Firefly, 3, 8), O(ItemID.Grasshopper, 3, 8), O(ItemID.HerbBag)],
		HomeBiome.Snow => [O(ItemID.BorealWood, 15, 30), O(ItemID.IceTorch, 8, 18), O(ItemID.Shiverthorn, 2, 5), O(ItemID.SlushBlock, 12, 30), O(ItemID.SnowBlock, 15, 35), O(ItemID.IceBlock, 12, 30), O(ItemID.FrostburnArrow, 20, 50), O(ItemID.WarmthPotion), O(ItemID.Flare, 10, 25), O(ItemID.Sapphire, 1, 3)],
		HomeBiome.Desert => [O(ItemID.SandBlock, 15, 30), O(ItemID.Cactus, 5, 12), O(ItemID.Waterleaf, 2, 5), O(ItemID.AntlionMandible, 2, 5), O(ItemID.ScarabBomb, 2, 5), O(ItemID.FossilOre, 2, 6), O(ItemID.Sandstone, 12, 30), O(ItemID.HardenedSand, 12, 30), O(ItemID.Amber, 1, 3), O(ItemID.ThornsPotion)],
		HomeBiome.Jungle => [O(ItemID.RichMahogany, 15, 30), O(ItemID.JungleSpores, 2, 5), O(ItemID.Stinger, 3, 8), O(ItemID.Vine, 1, 3), O(ItemID.JungleGrassSeeds, 1, 3), O(ItemID.BeeWax, 3, 8), O(ItemID.BottledHoney, 2, 5), O(ItemID.Moonglow, 2, 5), O(ItemID.BambooBlock, 12, 30), O(ItemID.Mango, 1, 3)],
		HomeBiome.Ocean => [O(ItemID.Coral, 2, 6), O(ItemID.Seashell, 1, 3), O(ItemID.Starfish, 1, 3), O(ItemID.SharkFin, 1, 3), O(ItemID.PalmWood, 12, 30), O(ItemID.WaterWalkingPotion), O(ItemID.GillsPotion), O(ItemID.SonarPotion), O(ItemID.CookedShrimp, 1, 3), O(ItemID.Coconut, 1, 3)],
		HomeBiome.Underground => [O(ItemID.StoneBlock, 20, 40), O(ItemID.Bomb, 2, 5), O(ItemID.SpelunkerPotion), O(ItemID.MiningPotion), O(ItemID.TrapsightPotion), O(ItemID.SiltBlock, 12, 30), O(ItemID.ClayBlock, 12, 30), O(ItemID.Amethyst, 1, 3), O(ItemID.Topaz, 1, 3), O(ItemID.Rope, 20, 50)],
		HomeBiome.Hallow => [O(ItemID.CrystalShard, 4, 10), O(ItemID.PixieDust, 4, 10), O(ItemID.UnicornHorn, 1, 2), O(ItemID.PearlsandBlock, 12, 30), O(ItemID.Pearlwood, 12, 30), O(ItemID.CrystalBullet, 20, 50), O(ItemID.HolyArrow, 15, 35), O(ItemID.PinkGel, 2, 5), O(ItemID.FairyGlowstick, 5, 12), O(ItemID.GravitationPotion)],
		HomeBiome.Mushroom => [O(ItemID.GlowingMushroom, 8, 18), O(ItemID.MushroomGrassSeeds, 1, 3), O(ItemID.ShinePotion), O(ItemID.Glowstick, 8, 18), O(ItemID.BlueSolution, 5, 12), O(ItemID.Mushroom, 3, 8), O(ItemID.GillsPotion), O(ItemID.BuilderPotion), O(ItemID.ManaPotion, 2, 5), O(ItemID.Gel, 12, 30)],
		HomeBiome.Underworld => [O(ItemID.AshBlock, 15, 30), O(ItemID.Obsidian, 3, 8), O(ItemID.Hellstone, 2, 5), O(ItemID.HellstoneBar, 1, 3), O(ItemID.Fireblossom, 2, 5), O(ItemID.ObsidianSkinPotion), O(ItemID.HellfireArrow, 15, 35), O(ItemID.Lavafly, 2, 5), O(ItemID.MagmaSnail, 1, 3), O(ItemID.LivingFireBlock, 5, 12)],
		_ => [O(ItemID.HealingPotion)]
	};
}
