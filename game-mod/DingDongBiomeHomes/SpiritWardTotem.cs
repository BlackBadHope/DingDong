using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace DingDongBiomeHomes;

// A floor totem that keeps wall-phasing enemies (wraiths, ghosts, reapers,
// cursed skulls, dungeon spirits and their modded kin) from spawning around a
// player who stands within a screen of it. Enemies that respect walls are
// untouched, so the totem is a home comfort, not a safe room.
public sealed class SpiritWardTotemItem : ModItem
{
	public override string Texture => "DingDongBiomeHomes/Assets/Items/SpiritWardTotem";

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpiritWardTotemTile>());
		Item.width = 24;
		Item.height = 32;
		Item.value = Item.buyPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Wood, 20)
			.AddIngredient(ItemID.Bone, 8)
			.AddIngredient(ItemID.Lens, 2)
			.AddIngredient(ItemID.FallenStar, 1)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}

public sealed class SpiritWardTotemTile : ModTile
{
	public override string Texture => "DingDongBiomeHomes/Assets/Tiles/SpiritWardTotem";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
		TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<SpiritWardTotemTileEntity>().Generic_HookPostPlaceMyPlayer;
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<SpiritWardTotemItem>());
		AddMapEntry(new Color(120, 200, 230), Language.GetText("Mods.DingDongBiomeHomes.SpiritWard.MapEntry"));
		DustType = DustID.Bone;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<SpiritWardTotemItem>();
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		ModContent.GetInstance<SpiritWardTotemTileEntity>().Kill(i - frameX / 18, j - frameY / 18);
	}
}

public sealed class SpiritWardTotemTileEntity : ModTileEntity
{
	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Framing.GetTileSafely(x, y);
		return tile.HasTile && tile.TileType == ModContent.TileType<SpiritWardTotemTile>();
	}

	public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 3);
			NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
			return -1;
		}
		return Place(i, j);
	}
}

public sealed class SpiritWardSpawnGuard : GlobalNPC
{
	// One conventional screen (1920x1080) in tiles, measured from the totem.
	internal const int HalfWidthTiles = 60;
	internal const int HalfHeightTiles = 34;

	internal static bool Warded(Point tile)
	{
		foreach (TileEntity entity in TileEntity.ByID.Values)
		{
			if (entity is not SpiritWardTotemTileEntity totem) continue;
			int dx = tile.X - totem.Position.X, dy = tile.Y - totem.Position.Y;
			if (dx >= -HalfWidthTiles && dx <= HalfWidthTiles && dy >= -HalfHeightTiles && dy <= HalfHeightTiles)
				return true;
		}
		return false;
	}

	internal static bool PhasesThroughWalls(int type)
	{
		if (!ContentSamples.NpcsByNetId.TryGetValue(type, out NPC sample) || sample == null) return false;
		return sample.noTileCollide && !sample.boss && !sample.townNPC && !sample.friendly &&
			sample.aiStyle != NPCAIStyleID.Worm && sample.lifeMax > 0;
	}

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		Player player = spawnInfo.Player;
		if (player is null || !player.active || pool.Count == 0) return;
		if (!Warded(player.Center.ToTileCoordinates()) && !Warded(new Point(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY)))
			return;
		List<int> blocked = null;
		foreach (int type in pool.Keys)
			if (PhasesThroughWalls(type)) (blocked ??= new()).Add(type);
		if (blocked == null) return;
		foreach (int type in blocked) pool.Remove(type);
	}
}
