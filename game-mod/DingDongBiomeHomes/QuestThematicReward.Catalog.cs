using Terraria.ID;

namespace DingDongBiomeHomes;

internal static partial class QuestThematicReward
{
	private static readonly ThematicRewardOption[] SupplementalPool =
	[
		O(ItemID.HealingPotion, 2, 5), O(ItemID.LesserHealingPotion, 3, 8), O(ItemID.ManaPotion, 2, 5), O(ItemID.LesserManaPotion, 3, 8),
		O(ItemID.RecallPotion, 1, 3), O(ItemID.WormholePotion, 1, 2), O(ItemID.SwiftnessPotion), O(ItemID.IronskinPotion),
		O(ItemID.RegenerationPotion), O(ItemID.ShinePotion), O(ItemID.NightOwlPotion), O(ItemID.GillsPotion), O(ItemID.HunterPotion),
		O(ItemID.MiningPotion), O(ItemID.SpelunkerPotion), O(ItemID.BuilderPotion), O(ItemID.CalmingPotion), O(ItemID.WarmthPotion),
		O(ItemID.ObsidianSkinPotion), O(ItemID.GravitationPotion), O(ItemID.FeatherfallPotion), O(ItemID.WaterWalkingPotion),
		O(ItemID.BattlePotion), O(ItemID.ThornsPotion), O(ItemID.ArcheryPotion), O(ItemID.FishingPotion), O(ItemID.SonarPotion),
		O(ItemID.CratePotion), O(ItemID.TrapsightPotion), O(ItemID.InvisibilityPotion), O(ItemID.MagicPowerPotion),
		O(ItemID.ManaRegenerationPotion), O(ItemID.EndurancePotion), O(ItemID.LifeforcePotion), O(ItemID.InfernoPotion),
		O(ItemID.WrathPotion), O(ItemID.RagePotion), O(ItemID.AmmoReservationPotion), O(ItemID.TitanPotion), O(ItemID.SummoningPotion),
		O(ItemID.Wood, 12, 30), O(ItemID.BorealWood, 12, 30), O(ItemID.RichMahogany, 12, 30), O(ItemID.PalmWood, 12, 30),
		O(ItemID.Ebonwood, 12, 30), O(ItemID.Shadewood, 12, 30), O(ItemID.DynastyWood, 12, 30), O(ItemID.BambooBlock, 12, 30),
		O(ItemID.StoneBlock, 15, 40), O(ItemID.DirtBlock, 15, 40), O(ItemID.ClayBlock, 12, 30), O(ItemID.SandBlock, 15, 40),
		O(ItemID.EbonsandBlock, 12, 30), O(ItemID.CrimsandBlock, 12, 30), O(ItemID.PearlsandBlock, 12, 30),
		O(ItemID.SiltBlock, 12, 30), O(ItemID.SlushBlock, 12, 30), O(ItemID.AshBlock, 12, 30), O(ItemID.Obsidian, 3, 8),
		O(ItemID.Hellstone, 2, 5), O(ItemID.Meteorite, 2, 5), O(ItemID.Gel, 12, 30), O(ItemID.Silk, 3, 8),
		O(ItemID.Cobweb, 15, 35), O(ItemID.Bone, 5, 12), O(ItemID.Coral, 2, 6), O(ItemID.Seashell, 1, 3),
		O(ItemID.Starfish, 1, 3), O(ItemID.Cactus, 5, 12), O(ItemID.Pumpkin, 3, 8), O(ItemID.Hay, 12, 30),
		O(ItemID.Glass, 8, 20), O(ItemID.Rope, 20, 50), O(ItemID.Chain, 5, 12), O(ItemID.Wire, 20, 50),
		O(ItemID.Actuator, 5, 12), O(ItemID.FallenStar, 2, 5), O(ItemID.CrystalShard, 3, 8), O(ItemID.PixieDust, 4, 10),
		O(ItemID.UnicornHorn, 1, 2), O(ItemID.JungleSpores, 2, 5), O(ItemID.Stinger, 3, 8), O(ItemID.Vine, 1, 3),
		O(ItemID.BeeWax, 3, 8), O(ItemID.Lens, 2, 6), O(ItemID.BlackLens), O(ItemID.Feather, 3, 8),
		O(ItemID.AntlionMandible, 2, 5), O(ItemID.SharkFin, 1, 3), O(ItemID.Daybloom, 2, 5), O(ItemID.Moonglow, 2, 5),
		O(ItemID.Blinkroot, 2, 5), O(ItemID.Deathweed, 2, 5), O(ItemID.Waterleaf, 2, 5), O(ItemID.Fireblossom, 2, 5),
		O(ItemID.Shiverthorn, 2, 5), O(ItemID.CopperBar, 2, 5), O(ItemID.TinBar, 2, 5), O(ItemID.IronBar, 2, 5),
		O(ItemID.LeadBar, 2, 5), O(ItemID.SilverBar, 2, 5), O(ItemID.TungstenBar, 2, 5), O(ItemID.GoldBar, 1, 3),
		O(ItemID.PlatinumBar, 1, 3), O(ItemID.DemoniteBar, 1, 3), O(ItemID.CrimtaneBar, 1, 3), O(ItemID.MeteoriteBar, 1, 3),
		O(ItemID.HellstoneBar, 1, 3), O(ItemID.Bomb, 2, 5), O(ItemID.StickyBomb, 2, 5), O(ItemID.BouncyBomb, 2, 5),
		O(ItemID.ScarabBomb, 2, 5), O(ItemID.Dynamite), O(ItemID.BouncyDynamite), O(ItemID.Grenade, 3, 8),
		O(ItemID.Beenade, 2, 5), O(ItemID.ThrowingKnife, 15, 35), O(ItemID.PoisonedKnife, 12, 30), O(ItemID.Shuriken, 15, 35),
		O(ItemID.WoodenArrow, 30, 70), O(ItemID.FlamingArrow, 25, 60), O(ItemID.FrostburnArrow, 25, 60),
		O(ItemID.JestersArrow, 15, 35), O(ItemID.UnholyArrow, 15, 35), O(ItemID.HellfireArrow, 12, 30),
		O(ItemID.MusketBall, 30, 70), O(ItemID.SilverBullet, 25, 60), O(ItemID.MeteorShot, 20, 50),
		O(ItemID.Worm, 3, 8), O(ItemID.EnchantedNightcrawler, 2, 5), O(ItemID.Firefly, 3, 8), O(ItemID.LightningBug, 3, 8),
		O(ItemID.Grasshopper, 3, 8), O(ItemID.Grubby, 3, 8), O(ItemID.Sluggy, 3, 8), O(ItemID.Buggy, 3, 8),
		O(ItemID.ApprenticeBait, 2, 5), O(ItemID.JourneymanBait, 2, 5), O(ItemID.MasterBait, 1, 3),
		O(ItemID.Mushroom, 3, 8), O(ItemID.GlowingMushroom, 8, 18), O(ItemID.CookedFish, 1, 3),
		O(ItemID.CookedShrimp, 1, 3), O(ItemID.BowlofSoup, 1, 3), O(ItemID.PumpkinPie, 1, 3), O(ItemID.Ale, 1, 3), O(ItemID.Sake, 1, 3),
		O(ItemID.Apple, 1, 3), O(ItemID.Peach, 1, 3), O(ItemID.Lemon, 1, 3), O(ItemID.Cherry, 1, 3), O(ItemID.Coconut, 1, 3),
		O(ItemID.Banana, 1, 3), O(ItemID.Grapes, 1, 3), O(ItemID.Pineapple, 1, 3)
	];
}
