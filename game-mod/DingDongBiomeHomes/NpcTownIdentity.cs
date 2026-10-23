using System;

namespace DingDongBiomeHomes;

public enum TownRoofStyle
{
	Gable,
	Gambrel,
	Mansard,
	Flat,
	Dome,
	Pagoda,
	Spire,
	Sawtooth,
	Canopy,
	Hull,
	Vault,
	Igloo
}

[Flags]
public enum TownFeature
{
	None = 0,
	Porch = 1 << 0,
	Balcony = 1 << 1,
	Tower = 1 << 2,
	Chimney = 1 << 3,
	Awning = 1 << 4,
	Garden = 1 << 5,
	Workshop = 1 << 6,
	Observatory = 1 << 7,
	Shrine = 1 << 8,
	Dock = 1 << 9,
	Greenhouse = 1 << 10,
	Forge = 1 << 11,
	Tree = 1 << 12,
	Fountain = 1 << 13,
	Crypt = 1 << 14
}

public sealed class NpcTownIdentity
{
	public string NpcKey { get; }
	public string Theme { get; }
	public HomeBiome Biome { get; }
	public HomeArchetype Archetype { get; }
	public int Width { get; }
	public int Height { get; }
	public int Floors { get; }
	public TownRoofStyle Roof { get; }
	public TownFeature Features { get; }
	public byte Paint { get; }
	public string ShopItemName { get; }
	public int ShopPriceSilver { get; }
	public int Variant { get; }

	public NpcTownIdentity(
		string npcKey,
		string theme,
		HomeBiome biome,
		HomeArchetype archetype,
		int width,
		int floors,
		TownRoofStyle roof,
		TownFeature features,
		byte paint,
		string shopItemName,
		int shopPriceSilver,
		int variant)
	{
		NpcKey = npcKey;
		Theme = theme;
		Biome = biome;
		Archetype = archetype;
		Width = width;
		Floors = floors;
		Height = 20 + floors * 8;
		Roof = roof;
		Features = features;
		Paint = paint;
		ShopItemName = shopItemName;
		ShopPriceSilver = shopPriceSilver;
		Variant = variant;
	}
}
