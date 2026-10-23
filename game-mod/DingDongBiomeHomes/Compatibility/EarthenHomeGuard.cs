using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// SOTS adds this burrowing, segmented enemy independently of vanilla town
// suppression. Keep its wilderness encounters, but do not let it camp housing.
public sealed class EarthenHomeGuardSystem : ModSystem
{
    internal static int HeadType = -1;
    internal static int TailType = -1;

    public override void PostSetupContent()
    {
        HeadType = ModContent.TryFind<ModNPC>("SOTS/EarthenConstruct", out var head) ? head.Type : -1;
        TailType = ModContent.TryFind<ModNPC>("SOTS/EarthenConstructTail", out var tail) ? tail.Type : -1;
        if (HeadType >= 0)
            Mod.Logger.Info("DINGDONG_EARTHEN_HOME_GUARD|status=ACTIVE|naturalSpawnSuppression=True|serverAuthoritative=True|wildernessUnchanged=True");
    }

    public override void Unload() { HeadType = TailType = -1; }

    internal static bool IsHousing(Point point)
    {
        if (!WorldGen.InWorld(point.X, point.Y, 4)) return false;
        ushort wall = Main.tile[point.X, point.Y].WallType;
        if (wall < Main.wallHouse.Length && Main.wallHouse[wall]) return true;
        return false;
    }
}

public sealed class EarthenHomeGuardNPC : GlobalNPC
{
    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        int headType = EarthenHomeGuardSystem.HeadType;
        Player player = spawnInfo.Player;
        if (headType < 0 || player is null || !player.active) return;
        if (spawnInfo.PlayerSafe || EarthenHomeGuardSystem.IsHousing(player.Center.ToTileCoordinates()) ||
            EarthenHomeGuardSystem.IsHousing(new Point(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY)))
            pool.Remove(headType);
    }

    public override bool PreAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.type != EarthenHomeGuardSystem.HeadType || !npc.active)
            return true;
        bool protectedArea = EarthenHomeGuardSystem.IsHousing(npc.Center.ToTileCoordinates());
        if (!protectedArea && npc.target >= 0 && npc.target < Main.maxPlayers)
        {
            Player target = Main.player[npc.target];
            protectedArea = target is not null && target.active && !target.dead &&
                Vector2.DistanceSquared(target.Center, npc.Center) <= 1920f * 1920f &&
                EarthenHomeGuardSystem.IsHousing(target.Center.ToTileCoordinates());
        }
        if (!protectedArea) return true;

        int headIndex = npc.whoAmI;
        // Remove only this enemy and its own linked tail. Do not call Kill or
        // StrikeNPC: despawning must not grant drops or trigger a death phase.
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC part = Main.npc[i];
            if (part is null || !part.active ||
                (part.whoAmI != headIndex && (part.type != EarthenHomeGuardSystem.TailType || part.realLife != headIndex)))
                continue;
            part.active = false;
            part.netUpdate = true;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: part.whoAmI);
        }
        return false;
    }
}
