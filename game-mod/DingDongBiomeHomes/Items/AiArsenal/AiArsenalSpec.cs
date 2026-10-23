using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal readonly record struct AiArsenalSpec(AiArsenalCategory Category, AiArsenalTier Tier, int Damage, int UseTime, float Knockback, int Crit, int Rarity, int Value, int ProjectileType = ProjectileID.None, float ShootSpeed = 0f, int AmmoType = 0, int Mana = 0);
