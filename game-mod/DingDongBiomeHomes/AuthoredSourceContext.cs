using System;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

// Restores missing source context at 1:1 tile scale, not a compact house variant.
// All 120x100 original house cells and all five planes remain byte-identical.
internal static class AuthoredSourceContext
{
	internal const string JungleResource = "Structures/PrivateHome/Greaveil/jungle-east-house-restored.shstruct";
	internal const string JungleSha256 = "AEC823E3C58603CF070A9528D19DC4D3B9C4F9A01247ED62DB58DF4A32FB69BD";
	internal const int JungleWidth = 156, JungleHeight = 122;
	internal const int ReferenceX = 36, ReferenceY = 22, ReferenceWidth = 120, ReferenceHeight = 100;
	// Original reviewed anchor 73 translated with the original core, not resampled
	// across the enlarged canopy/capsule canvas.
	internal const int JungleTerrainAnchorY = 73 + ReferenceY;
	internal const int DresserX = 30, DresserY = 70;

	internal static bool HasRestoredReference(AuthoredPreset preset) =>
		preset.Id == "Greaveil_JungleEastHouse" &&
		string.Equals(preset.ResourcePath, JungleResource, StringComparison.Ordinal);

	// Sampling reference only. Collision, liquid admission, framing and visual
	// protection must continue to use the complete restored scene mutation mask.
	internal static Rectangle ReferenceBounds(AuthoredPreset preset) =>
		HasRestoredReference(preset)
			? new Rectangle(ReferenceX, ReferenceY, ReferenceWidth, ReferenceHeight)
			: new Rectangle(0, 0, preset.Width, preset.Height);

	internal static int ReferenceCenterOffsetX(AuthoredPreset preset) =>
		ReferenceBounds(preset).Center.X;

	internal static Rectangle ReferenceContactBounds(AuthoredPreset preset, Rectangle fullVisualBounds) =>
		// The original reviewed mutation focus was 0,0,117,100; its three null
		// right columns were not terrain contacts. Translate, do not rescale it.
		HasRestoredReference(preset)
			? new Rectangle(ReferenceX, ReferenceY, 117, ReferenceHeight)
			: fullVisualBounds;
}
