using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal sealed partial class DingDongHubUiState
{
	private void BuildResidences()
	{
		contentTitle.SetText(Text("Residences.Title"));
		contentSubtitle.SetText(Text("Residences.Subtitle"));
		ResidenceDiscoveryPlayer discovery = Main.LocalPlayer.GetModPlayer<ResidenceDiscoveryPlayer>();
		int discovered = SettlementWorldState.Residences.Count(residence => discovery.HasDiscovered(residence.ReviewId));
		DamageGuideTone statusTone = SettlementWorldState.HasCompleteResidenceRegistry ? DamageGuideTone.Positive : DamageGuideTone.Warning;
		string summary = Text("Residences.Summary", discovered, NpcTownManifest.AuthoredCount, SettlementWorldState.StructuresBuilt, SettlementWorldState.HousingValid, SettlementWorldState.RoomsDiscovered);
		contentCards.Add(CreateInfoCard(Text("Residences.SummaryTitle"), summary, statusTone, ItemID.LivingWoodWand));
		List<SettlementResidenceReport> known = SettlementWorldState.Residences.Where(residence => discovery.HasDiscovered(residence.ReviewId)).OrderBy(residence => residence.ReviewId).ToList();
		if (known.Count == 0)
		{
			contentCards.Add(CreateInfoCard(Text("Residences.EmptyTitle"), Text("Residences.EmptyBody"), DamageGuideTone.Information, ItemID.Compass));
			return;
		}

		foreach (SettlementResidenceReport residence in known)
		{
			NpcResidentRegistry.TryGetType(residence.NpcKey, out int npcType);
			string body = Text("Residences.EntryBody", Text($"Biomes.{residence.Biome}"), residence.ReviewId.ToString(CultureInfo.CurrentCulture), residence.Area.Center.X.ToString(CultureInfo.CurrentCulture), residence.Area.Center.Y.ToString(CultureInfo.CurrentCulture));
			contentCards.Add(CreateInfoCard(NpcQuestSystem.ResidentName(residence.NpcKey), body, DamageGuideTone.Normal, ItemID.None, npcType));
		}
	}

	private void BuildHelp()
	{
		contentTitle.SetText(Text("Help.Title"));
		contentSubtitle.SetText(Text("Help.Subtitle"));
		contentCards.Add(CreateInfoCard(Text("Help.ControlsTitle"), Text("Help.ControlsBody"), DamageGuideTone.Positive, ItemID.Wrench));
		contentCards.Add(CreateInfoCard(Text("Help.ModulesTitle"), Text("Help.ModulesBody"), DamageGuideTone.Information, ItemID.MechanicalLens));
		contentCards.Add(CreateInfoCard(Text("Help.ServerTitle"), Text("Help.ServerBody"), DamageGuideTone.Normal, ItemID.DefenderMedal));
	}
}
