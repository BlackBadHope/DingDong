using System;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Diagnostics;
internal static class LoggedFailureRuntimeProbe
{
 internal static int Run(Player player) {
  int net = Main.netMode; bool active = player.active; Item held = player.inventory[player.selectedItem]; int checks = 0;
  void Require(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks++; }
  try {
   int type = ModLoader.GetMod("miningcracks_take_on_luiafk").Find<ModProjectile>("BuildingProjectile").Type;
   player.active = true;
   Projectile remote = new(); remote.SetDefaults(type); remote.owner = player.whoAmI;
   remote.ModProjectile.OnSpawn(new EntitySource_Misc("DingDong disposable network probe"));
   Main.netMode = NetmodeID.Server; int lifetime = remote.timeLeft; remote.ModProjectile.AI();
   Require(remote.hide && remote.timeLeft > lifetime, "Network-spawned Luiafk cursor has no ItemUse source and does not crash the server");
   Main.netMode = NetmodeID.SinglePlayer;
   Projectile orphan = new(); orphan.SetDefaults(type); orphan.owner = Main.maxPlayers; orphan.ModProjectile.AI();
   Require(orphan.hide, "Luiafk cursor tolerates a disconnected owner");
   Projectile local = new(); local.SetDefaults(type); local.owner = player.whoAmI;
   player.inventory[player.selectedItem] = new Item(ItemID.Wood);
   local.ModProjectile.OnSpawn(new EntitySource_ItemUse(player, player.HeldItem));
   lifetime = local.timeLeft; local.ModProjectile.AI();
   Require(local.timeLeft > lifetime, "Owner's initialized Luiafk building preview still runs native AI");
  } finally { Main.netMode = net; player.active = active; player.inventory[player.selectedItem] = held; }
  return checks + AndroStorageNetwork.RuntimeChecks(player);
 }
}
