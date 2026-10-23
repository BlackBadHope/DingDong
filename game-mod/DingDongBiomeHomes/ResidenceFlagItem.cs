using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace DingDongBiomeHomes;

public sealed class ResidenceFlagItem : ModItem
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.BlueBanner}";

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ResidenceFlagTile>());
		Item.width = 18;
		Item.height = 28;
		Item.value = Item.buyPrice(silver: 50);
		Item.rare = ItemRarityID.Blue;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.BlueBanner)
			.AddIngredient(ItemID.Wood, 10)
			.AddIngredient(ItemID.Silk, 2)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}

public sealed class ResidenceFlagTile : ModTile
{
	public override string Texture => $"Terraria/Images/Tiles_{TileID.Banners}";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.Banners, 0));
		TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<ResidenceFlagTileEntity>().Generic_HookPostPlaceMyPlayer;
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<ResidenceFlagItem>());
		AddMapEntry(new Color(75, 130, 220), Language.GetText("Mods.DingDongBiomeHomes.ResidenceFlag.MapEntry"));
		DustType = DustID.BlueCrystalShard;
	}

	public override bool RightClick(int i, int j)
	{
		Point16 topLeft = ResidenceFlagTileEntity.TopLeft(i, j);
		NpcQuestNetwork.RequestFlagClaim(topLeft.X, topLeft.Y);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<ResidenceFlagItem>();
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		ModContent.GetInstance<ResidenceFlagTileEntity>().Kill(i, j);
		NpcResidenceWorldSystem.HandleFlagRemoved(new Point16(i, j));
	}
}

public sealed class ResidenceFlagTileEntity : ModTileEntity
{
	internal string OwnerId { get; set; } = string.Empty;
	internal string NpcKey { get; set; } = string.Empty;
	internal Point HomePoint { get; set; }
	internal long ClaimRevision { get; set; }

	internal bool Claimed => OwnerId.Length > 0;
	internal bool Assigned => NpcKey.Length > 0;

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Framing.GetTileSafely(x, y);
		return tile.HasTile && tile.TileType == ModContent.TileType<ResidenceFlagTile>();
	}

	public override void SaveData(TagCompound tag)
	{
		tag["OwnerId"] = OwnerId;
		tag["NpcKey"] = NpcKey;
		tag["HomeX"] = HomePoint.X;
		tag["HomeY"] = HomePoint.Y;
		tag["ClaimRevision"] = ClaimRevision;
	}

	public override void LoadData(TagCompound tag)
	{
		OwnerId = tag.GetString("OwnerId");
		NpcKey = tag.GetString("NpcKey");
		HomePoint = new Point(tag.GetInt("HomeX"), tag.GetInt("HomeY"));
		ClaimRevision = tag.GetLong("ClaimRevision");
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(OwnerId);
		writer.Write(NpcKey);
		writer.Write(HomePoint.X);
		writer.Write(HomePoint.Y);
		writer.Write(ClaimRevision);
	}

	public override void NetReceive(BinaryReader reader)
	{
		OwnerId = reader.ReadString();
		NpcKey = reader.ReadString();
		HomePoint = new Point(reader.ReadInt32(), reader.ReadInt32());
		ClaimRevision = reader.ReadInt64();
	}

	internal static Point16 TopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		int row = tile.TileFrameY / 18 % 3;
		return new Point16(i, j - row);
	}

	internal static bool TryGet(Point16 position, out ResidenceFlagTileEntity flag)
	{
		if (TileEntity.ByPosition.TryGetValue(position, out TileEntity entity) && entity is ResidenceFlagTileEntity typed)
		{
			flag = typed;
			return true;
		}
		flag = null;
		return false;
	}
}
