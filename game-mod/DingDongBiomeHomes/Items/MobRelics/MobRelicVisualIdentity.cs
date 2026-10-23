using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Items.MobRelics;

internal readonly record struct MobRelicVisualIdentity(string Signature, Color Accent, byte Glyph)
{
	internal static MobRelicVisualIdentity Create(MobRelicSourceDefinition source, string texture)
		=> new($"asset:{texture}", Color.White, 0);
}
