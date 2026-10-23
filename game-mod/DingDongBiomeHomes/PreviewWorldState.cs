using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

/// <summary>
/// Single-use marker for an isolated player visual-preview world. This state
/// never approves a candidate and is not consumed by normal world generation.
/// </summary>
public sealed class PreviewWorldState : ModSystem
{
	public static bool PreviewPlaced { get; private set; }
	public static string NpcKey { get; private set; } = string.Empty;
	public static string PresetId { get; private set; } = string.Empty;
	public static Rectangle Area { get; private set; }
	public static int ValidRooms { get; private set; }
	public static bool LoadedFromSave { get; private set; }

	public override void OnWorldLoad() => Reset();
	public override void OnWorldUnload() => Reset();

	public override void SaveWorldData(TagCompound tag)
	{
		if (!PreviewPlaced)
			return;
		tag["PreviewPlaced"] = true;
		tag["PreviewNpcKey"] = NpcKey;
		tag["PreviewPresetId"] = PresetId;
		tag["PreviewAreaX"] = Area.X;
		tag["PreviewAreaY"] = Area.Y;
		tag["PreviewAreaWidth"] = Area.Width;
		tag["PreviewAreaHeight"] = Area.Height;
		tag["PreviewValidRooms"] = ValidRooms;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		if (!tag.GetBool("PreviewPlaced"))
			return;
		PreviewPlaced = true;
		NpcKey = tag.GetString("PreviewNpcKey");
		PresetId = tag.GetString("PreviewPresetId");
		Area = new Rectangle(
			tag.GetInt("PreviewAreaX"),
			tag.GetInt("PreviewAreaY"),
			tag.GetInt("PreviewAreaWidth"),
			tag.GetInt("PreviewAreaHeight"));
		ValidRooms = tag.GetInt("PreviewValidRooms");
		LoadedFromSave = true;
	}

	public static void Commit(string npcKey, string presetId, Rectangle area, int validRooms)
	{
		PreviewPlaced = true;
		NpcKey = npcKey;
		PresetId = presetId;
		Area = area;
		ValidRooms = validRooms;
		LoadedFromSave = false;
	}

	private static void Reset()
	{
		PreviewPlaced = false;
		NpcKey = string.Empty;
		PresetId = string.Empty;
		Area = Rectangle.Empty;
		ValidRooms = 0;
		LoadedFromSave = false;
	}
}
