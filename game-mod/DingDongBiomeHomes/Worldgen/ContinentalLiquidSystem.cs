using System;
using System.Diagnostics;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

public sealed class ContinentalLiquidSystem : ModSystem
{
	private const int NativeCapacity = 49998;
	private const int BackgroundLane = 255;
	private delegate void AddOriginal(int x, int y);
	private delegate void AddHook(AddOriginal original, int x, int y);
	private delegate void DeleteOriginal(int index);
	private delegate void DeleteHook(DeleteOriginal original, int index);
	private delegate void ResetOriginal();
	private delegate void ResetHook(ResetOriginal original);
	// The native queues and the sidecar form one transaction. Hardmode's
	// ThreadPool tile-framing task calls AddWater while the main thread drains.
	// Locking only the Dictionary would still race the native count/tile flags.
	private static readonly object queueGate = new();
	private static readonly ContinentalLiquidQueue pending = new(256, NativeCapacity);
	private static ContinentalLiquidOverflow overflow;
	private static ContinentalLiquidHookSet hooks;
	private static ContinentalLiquidSystem owner;
	private static bool installed, degraded;
	private static int bootstrapIndex, width, height;
	private static int playerCount;
	private static readonly int[] playerIds = new int[255], playerX = new int[255], playerY = new int[255];
	private static long pumpStarted;
	private static readonly Func<bool> HasSpace = () => Liquid.numLiquid < Liquid.curMaxLiquid - 1;
	private static readonly Func<bool> WithinBudget = () =>
		Stopwatch.GetElapsedTime(pumpStarted).TotalMilliseconds < ContinentalLiquidPump.MaximumMilliseconds;

	public override void Load()
	{
		owner = this;
		hooks = new();
		try
		{
			hooks.Add(() => new ILHook(Method(typeof(Liquid), nameof(Liquid.UpdateLiquid)), PatchRefill));
			hooks.Add(() => new Hook(Method(typeof(LiquidBuffer), nameof(LiquidBuffer.AddBuffer), typeof(int), typeof(int)), (AddHook)AddBuffer));
			hooks.Add(() => new Hook(Method(typeof(LiquidBuffer), nameof(LiquidBuffer.DelBuffer), typeof(int)), (DeleteHook)DeleteBuffer));
			hooks.Add(() => new Hook(Method(typeof(WorldGen), nameof(WorldGen.WaterCheck)), (ResetHook)ResetThenRun));
			hooks.Add(() => new Hook(Method(typeof(Liquid), nameof(Liquid.StartPanic)), (ResetHook)StartPanic));
			hooks.Add(() => new Hook(Method(typeof(Liquid), nameof(Liquid.AddWater), typeof(int), typeof(int)), (AddHook)AddWaterLocked));
			hooks.Add(() => new Hook(Method(typeof(Liquid), nameof(Liquid.DelWater), typeof(int)), (DeleteHook)DeleteWaterLocked));
			hooks.Add(() => new Hook(Method(typeof(Liquid), nameof(Liquid.UpdateLiquid)), (ResetHook)UpdateLiquidLocked));
			installed = true;
		}
		catch (Exception error)
		{
			hooks.Dispose(); installed = false;
			Mod.Logger.Warn("DINGDONG_LIQUID_QUEUE|disabled=unsupported-native-shape|" + error.Message);
		}
	}

	public override void Unload()
	{
		installed = false;
		try { hooks?.Dispose(); }
		finally { hooks = null; Reset(); owner = null; }
	}
	public override void OnWorldLoad() => Reset();
	public override void OnWorldUnload() => Reset();
	private void CapturePlayers()
	{
		playerCount = 0;
		if (!WorldEligible()) return;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player player = Main.player[i];
			if (player == null || !player.active) continue;
			playerIds[playerCount] = i;
			playerX[playerCount] = (int)(player.Center.X / 16);
			playerY[playerCount++] = (int)(player.Center.Y / 16);
		}
	}
	private void PumpOverflow()
	{
		// If a foreign mutation invalidates the optional native sidecar, its
		// overflow obligations still receive bounded service with vanilla FIFO.
		if (!degraded || !WorldEligible() || Liquid.panicMode || overflow == null) return;
		pumpStarted = Stopwatch.GetTimestamp();
		ContinentalLiquidPump.Run(HasSpace, WithinBudget, () => false, RecheckOverflow);
	}

	public override void PostUpdatePlayers() { lock (queueGate) CapturePlayers(); }
	public override void PostUpdateWorld() { lock (queueGate) PumpOverflow(); }
	private static void Reset() { lock (queueGate) ResetCore(); }
	private static void ResetThenRun(ResetOriginal original) { lock (queueGate) ResetThenRunCore(original); }
	private static void StartPanic(ResetOriginal original) { lock (queueGate) StartPanicCore(original); }
	private static void AddBuffer(AddOriginal original, int x, int y) { lock (queueGate) AddBufferCore(original, x, y); }
	private static void DeleteBuffer(DeleteOriginal original, int index) { lock (queueGate) DeleteBufferCore(original, index); }
	private static void Drain() { lock (queueGate) DrainCore(); }
	private static void AddWaterLocked(AddOriginal original, int x, int y)
	{
		if (!ContinentalWorldDimensions.IsContinental) { original(x, y); return; }
		lock (queueGate) original(x, y);
	}
	private static void DeleteWaterLocked(DeleteOriginal original, int index)
	{
		if (!ContinentalWorldDimensions.IsContinental) { original(index); return; }
		lock (queueGate) original(index);
	}
	private static void UpdateLiquidLocked(ResetOriginal original)
	{
		if (!ContinentalWorldDimensions.IsContinental) { original(); return; }
		lock (queueGate) original();
	}

	private static MethodInfo Method(Type type, string name, params Type[] args) =>
		type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, args, null) ??
			throw new MissingMethodException(type.FullName, name);
	private static bool WorldEligible() => installed && !WorldGen.gen &&
		Main.netMode != NetmodeID.MultiplayerClient && ContinentalWorldDimensions.IsContinental;
	private static bool Enabled() => WorldEligible() && !degraded;
	private static int Key(int x, int y) => (x << 16) | y;

	private static void ResetCore()
	{
		pending.Clear(); overflow = null; bootstrapIndex = width = height = playerCount = 0; degraded = false;
	}
	private static void ResetThenRunCore(ResetOriginal original)
	{
		// Native WaterCheck fully revisits the world; StartPanic does the same via
		// QuickWater followed by WaterCheck. Never mix obligations between epochs.
		Reset();
		original();
	}
	private static void StartPanicCore(ResetOriginal original)
	{
		if (!Liquid.panicMode) Reset(); // Already panicking is a native no-op.
		original();
	}
	private static void Fail(Exception error)
	{
		degraded = true;
		pending.Clear(); bootstrapIndex = 0;
		owner?.Mod.Logger.Warn("DINGDONG_LIQUID_QUEUE|disabled=inconsistent-native-queue|" + error.Message);
	}
	private static void EnsureOverflow()
	{
		if (overflow != null && width == Main.maxTilesX && height == Main.maxTilesY) return;
		width = Main.maxTilesX; height = Main.maxTilesY;
		overflow = new(width, height, 256);
	}

	private static int Lane(int x, int y)
	{
		int lane = BackgroundLane;
		long best = 160L * 160;
		// No players need to stay near the cell to keep it scheduled. This only
		// chooses a service lane; background remains an equal participant.
		for (int i = 0; i < playerCount; i++)
		{
			long dx = x - playerX[i], dy = y - playerY[i];
			long distance = dx * dx + dy * dy;
			if (distance < best) { best = distance; lane = playerIds[i]; }
		}
		return lane;
	}

	private static void AddBufferCore(AddOriginal original, int x, int y)
	{
		int before = LiquidBuffer.numLiquidBuffer;
		bool eligible = WorldEligible(), track = eligible && !degraded;
		bool refusedFull = eligible && before >= NativeCapacity && !Main.tile[x, y].CheckingLiquid;
		original(x, y);
		if (!eligible) return;
		try
		{
			if (track && before == 0 && pending.Count > 0) { pending.Clear(); bootstrapIndex = 0; }
			if (track && LiquidBuffer.numLiquidBuffer == before + 1)
				pending.Add(Key(x, y), before, Lane(x, y));
			else if (refusedFull)
			{
				EnsureOverflow();
				overflow.Mark(x, y, Lane(x, y));
			}
		}
		catch (Exception error) { Fail(error); }
	}

	private static void DeleteBufferCore(DeleteOriginal original, int index)
	{
		if (!Enabled()) { original(index); return; }
		int last = LiquidBuffer.numLiquidBuffer - 1;
		if (index < 0 || index > last) { original(index); return; }
		LiquidBuffer removed = Main.liquidBuffer[index], moved = Main.liquidBuffer[last];
		int removedKey = Key(removed.x, removed.y), movedKey = Key(moved.x, moved.y);
		original(index);
		try
		{
			pending.Remove(removedKey);
			if (index < last)
			{
				if (pending.Contains(movedKey)) pending.Move(movedKey, index);
				else if (index < bootstrapIndex) pending.Add(movedKey, index, Lane(moved.x, moved.y));
			}
		}
		catch (Exception error) { Fail(error); }
	}

	private static void DrainCore()
	{
		pumpStarted = Stopwatch.GetTimestamp();
		try
		{
			// A load can finish with a native buffer populated while gen=true.
			// Bootstrap in bounded slices, while hooked swap-deletes keep indices
			// valid. Buffered admission waits for this bounded initialization;
			// native active-cell physics continues normally on every update.
			if (bootstrapIndex >= LiquidBuffer.numLiquidBuffer && pending.Count != LiquidBuffer.numLiquidBuffer)
			{
				// An external direct counter reset is a new indexing epoch. Keeping
				// stale keys would otherwise make every bounded rebuild incomplete.
				pending.Clear(); bootstrapIndex = 0;
			}
			int scanned = 0;
			while (bootstrapIndex < LiquidBuffer.numLiquidBuffer && scanned++ < ContinentalLiquidPump.MaximumAttempts && WithinBudget())
			{
				LiquidBuffer cell = Main.liquidBuffer[bootstrapIndex];
				int key = Key(cell.x, cell.y);
				if (!pending.Contains(key)) pending.Add(key, bootstrapIndex, Lane(cell.x, cell.y));
				bootstrapIndex++;
			}
			if (pending.Count != LiquidBuffer.numLiquidBuffer) return;
			ContinentalLiquidPump.Run(HasSpace, WithinBudget, TakeBuffered, RecheckOverflow);
		}
		catch (Exception error) { Fail(error); }
	}

	private static bool TakeBuffered()
	{
		if (!HasSpace() || !pending.TryNext(out int key, out int index)) return false;
		if (index < 0 || index >= LiquidBuffer.numLiquidBuffer) throw new InvalidOperationException("Liquid sidecar index escaped buffer.");
		LiquidBuffer cell = Main.liquidBuffer[index];
		int x = cell.x, y = cell.y;
		if (Key(x, y) != key) throw new InvalidOperationException("Liquid sidecar cell/index mismatch.");
		// Delete BEFORE admitting: no remove/re-enqueue cycle at a full pool.
		LiquidBuffer.DelBuffer(index);
		Tile tile = Main.tile[x, y];
		tile.CheckingLiquid = false;
		Liquid.AddWater(x, y);
		return true;
	}

	private static bool RecheckOverflow()
	{
		if (!HasSpace() || overflow == null || !overflow.TryNext(out int x, out int y)) return false;
		if (x >= 5 && y >= 5 && x < Main.maxTilesX - 5 && y < Main.maxTilesY - 5)
			Liquid.AddWater(x, y); // Native handles type, solidity, flags and networking.
		return true;
	}

	private static void PatchRefill(ILContext il)
	{
		(int start, int end) = ContinentalLiquidIlShape.FindRefill(il.Body.Instructions);
		ILCursor cursor = new(il) { Index = start };
		ILLabel native = cursor.DefineLabel(), resume = cursor.DefineLabel();
		native.Target = il.Body.Instructions[start];
		resume.Target = il.Body.Instructions[end];
		cursor.EmitDelegate<Func<bool>>(Enabled);
		cursor.Emit(OpCodes.Brfalse, native);
		cursor.EmitDelegate<Action>(Drain);
		cursor.Emit(OpCodes.Br, resume);
	}
}
