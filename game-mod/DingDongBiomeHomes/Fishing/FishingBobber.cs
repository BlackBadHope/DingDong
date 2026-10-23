using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Fishing.Content;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Fishing;

public sealed class FishingBobber : GlobalProjectile
{
	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.bobber;
	private int surfaceY = -1, bottomY, scanX = -1, scanY, refresh;
	private byte rigIndex;
	private bool initialized;

	public override void PostAI(Projectile projectile)
	{
		if (!projectile.bobber || projectile.owner != Main.myPlayer || projectile.ai[0] != 0) return;
		byte wanted = Main.player[projectile.owner].GetModPlayer<FishingPlayer>().ActiveRigIndex;
		if (!initialized || rigIndex != wanted)
		{
			initialized = true;
			rigIndex = wanted;
			surfaceY = -1;
			projectile.netUpdate = true;
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer) => writer.Write(rigIndex);
	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
	{
		byte incoming = reader.ReadByte();
		if (incoming != rigIndex) surfaceY = -1;
		rigIndex = incoming < FishingContentCatalog.Rigs.Count ? incoming : (byte)0;
	}

	internal FishingRigSpec Rig(Projectile p)
	{
		if (p.owner < 0 || p.owner >= Main.maxPlayers) return default;
		// Each peer checks possession/progression; extra AI never grants equipment.
		var player = Main.player[p.owner].GetModPlayer<FishingPlayer>();
		return player.ValidRig(rigIndex);
	}

	internal float Waterline(Projectile p, float native)
	{
		var rig = Rig(p);
		if (rig.TargetDepthTiles <= 0 || p.ai[0] != 0 || p.honeyWet || p.lavaWet || p.shimmerWet) return native;
		int x = (int)(p.Center.X / 16), y = (int)(p.Center.Y / 16);
		if (surfaceY < 0 || scanX != x || Math.Abs(y - scanY) > 16 || ++refresh >= 60)
		{
			var column = FishingEnvironment.Measure(x, y);
			if (column.Height < 3) return native;
			surfaceY = column.SurfaceY;
			bottomY = column.BottomY;
			scanX = x; scanY = y; refresh = 0;
		}
		int depth = FishingPolicy.TargetDepth(rig.TargetDepthTiles, bottomY - surfaceY + 1);
		float target = (surfaceY + depth) * 16 + 8;
		// The original AI performs movement, bite rolls, bait consumption and reeling.
		// Its waterline is approached slowly so a rig cannot teleport through a floor.
		return FishingPolicy.ApproachWaterline(p.Center.Y, target);
	}
}

public sealed class FishingBobberHooks : ModSystem
{
	private ILHook aiHook;
	public override void Load()
	{
		var method = typeof(Projectile).GetMethod("AI_061_FishingBobber", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		if (method == null) throw new MissingMethodException("Native fishing bobber AI unavailable");
		aiHook = new ILHook(method, Patch);
	}
	public override void Unload() { aiHook?.Dispose(); aiHook = null; }
	private static void Patch(ILContext il)
	{
		var waterlines = il.Body.Instructions.Where(i => i.Operand is Mono.Cecil.MethodReference m && m.Name == "AI_061_FishingBobber_GetWaterLine").ToArray();
		var limits = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldc_R4 && (float)i.Operand is 900f or 3000f).ToArray();
		if (waterlines.Length != 1 || limits.Length != 2)
			throw new InvalidOperationException("Native fishing AI layout changed; depth rig patch refused.");
		foreach (var limit in limits)
		{
			var c = new ILCursor(il); c.Goto(limit, MoveType.After);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, Projectile, float>>((native, p) =>
			{
				var rig = p.GetGlobalProjectile<FishingBobber>().Rig(p);
				return rig.TargetDepthTiles <= 0 ? native : Math.Max(native, (rig.TargetDepthTiles + 80) * 16);
			});
		}
		var cursor = new ILCursor(il); cursor.Goto(waterlines[0], MoveType.After);
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<float, Projectile, float>>((native, p) => p.GetGlobalProjectile<FishingBobber>().Waterline(p, native));
	}
}
