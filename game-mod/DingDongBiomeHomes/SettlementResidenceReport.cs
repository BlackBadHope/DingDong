using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class SettlementResidenceReport
{
	public int ReviewId { get; }
	public string NpcKey { get; }
	public string PresetId { get; }
	public HomeBiome Biome { get; }
	public Rectangle Area { get; }

	public SettlementResidenceReport(int reviewId, string npcKey, string presetId, HomeBiome biome, Rectangle area)
	{
		ReviewId = reviewId;
		NpcKey = npcKey;
		PresetId = presetId;
		Biome = biome;
		Area = area;
	}

	public bool IsCanonical()
	{
		if (ReviewId < 1 || ReviewId > NpcTownManifest.AuthoredCount || !NpcTownManifest.TryGet(NpcKey, out NpcTownIdentity identity) || identity.Variant + 1 != ReviewId || !AuthoredCandidateDeploymentManifest.TryGet(NpcKey, out AuthoredDeploymentProfile profile) || !string.Equals(profile.PresetId, PresetId, StringComparison.Ordinal) || !AuthoredResidencePlacementManifest.TryGet(NpcKey, out AuthoredResidencePlacementContract contract) || !contract.AllowedBiomes.Contains(identity.Biome) || !contract.AllowedBiomes.Contains(profile.TargetBiome) || !contract.AllowedBiomes.Contains(Biome) || !string.Equals(contract.PresetId, PresetId, StringComparison.Ordinal) || !NpcAuthoredPresetManifest.TryGetCandidate(NpcKey, out AuthoredPreset preset) || !string.Equals(preset.Id, PresetId, StringComparison.Ordinal))
			return false;
		return Area.Width == preset.Width && Area.Height == preset.Height && Area.Width > 0 && Area.Height > 0;
	}

	internal TagCompound Save() => new()
	{
		["QuestLocationsV1"] = QuestLocationCoordinates(),
		["ReviewId"] = ReviewId,
		["NpcKey"] = NpcKey,
		["PresetId"] = PresetId,
		["Biome"] = (int)Biome,
		["X"] = Area.X,
		["Y"] = Area.Y,
		["Width"] = Area.Width,
		["Height"] = Area.Height
	};
	internal static bool TryLoad(TagCompound tag, out SettlementResidenceReport report)
	{
		int biomeValue = tag.GetInt("Biome");
		if (!Enum.IsDefined(typeof(HomeBiome), biomeValue))
		{
			report = null;
			return false;
		}

		report = new SettlementResidenceReport(tag.GetInt("ReviewId"), tag.GetString("NpcKey"), tag.GetString("PresetId"), (HomeBiome)biomeValue, new Rectangle(tag.GetInt("X"), tag.GetInt("Y"), tag.GetInt("Width"), tag.GetInt("Height")));
		if (tag.ContainsKey("QuestLocationsV1")) report.ReadQuestCoordinates(tag.GetIntArray("QuestLocationsV1"));
		return true;
	}

	internal void NetSend(BinaryWriter writer)
	{
		writer.Write(ReviewId);
		writer.Write(NpcKey);
		writer.Write(PresetId);
		writer.Write((byte)Biome);
		writer.Write(Area.X);
		writer.Write(Area.Y);
		writer.Write(Area.Width);
		writer.Write(Area.Height);
		foreach (int coordinate in QuestLocationCoordinates()) writer.Write(coordinate);
	}

	internal static SettlementResidenceReport NetReceive(BinaryReader reader)
	{
		var report = new SettlementResidenceReport(reader.ReadInt32(), reader.ReadString(), reader.ReadString(), (HomeBiome)reader.ReadByte(), new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
		int[] coordinates = new int[32];
		for (int i = 0; i < coordinates.Length; i++) coordinates[i] = reader.ReadInt32();
		report.ReadQuestCoordinates(coordinates);
		return report;
	}
}
