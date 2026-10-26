using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

public sealed class IslandSeaBiome : ModBiome
{
    public override string BestiaryIcon => "Terraria/Images/Item_" + ItemID.Coral;
    public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;
    public override int Music => MusicID.Ocean;
    public override bool IsBiomeActive(Player player) => OceanExpeditionWorldSystem.TryGetZone((int)player.Center.X / 16, (int)player.Center.Y / 16, out var zone) && zone != OceanExpeditionZone.AirGrotto;
}
public sealed class BreathwardGrottoBiome : ModBiome
{
    public override string BestiaryIcon => "Terraria/Images/Item_" + ItemID.Geode;
    public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
    public override int Music => MusicID.Underground;
    public override bool IsBiomeActive(Player player) => OceanExpeditionWorldSystem.TryGetZone((int)player.Center.X / 16, (int)player.Center.Y / 16, out var zone) && zone == OceanExpeditionZone.AirGrotto;
}
