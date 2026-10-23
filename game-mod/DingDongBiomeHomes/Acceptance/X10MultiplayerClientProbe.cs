using System;
using System.IO;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

public sealed partial class X10MultiplayerClientProbe : ModPlayer
{
	private string stage;
	private Point anchor;
	private Vector2 start, previous;
	private int ticks, unloadedTicks;
	private float maxStep;
	private bool ready, finished, crossed;
	private string ControlPath => Environment.GetEnvironmentVariable("DINGDONG_X10_MP_CONTROL");
	private bool Active => X10MultiplayerProbe.Enabled && Main.netMode == NetmodeID.MultiplayerClient &&
		Player.whoAmI == Main.myPlayer && ContinentalWorldDimensions.IsContinental &&
		ControlPath != null && ControlPath.Contains("mp-runtime-", StringComparison.Ordinal);
	public override void SetControls()
	{
		if (!Active) return;
		if (Main.GameUpdateCount % 30 == 0) ReadControl();
		if (stage == null) return;
		Player.controlLeft = false;
		Player.controlRight = ready && !finished && Player.position.X < start.X + 512;
		Player.controlJump = Player.controlRight && ticks % 40 < 20;
		Player.controlDown = false;
		Player.controlUp = false;
		Player.controlUseItem = false;
	}
	private void ReadControl()
	{
		if (!File.Exists(ControlPath)) return;
		string[] fields;
		try { fields = File.ReadAllText(ControlPath).Trim().Split('|'); }
		catch (IOException) { return; }
		if (fields.Length != 3 || fields[0] == stage ||
			!int.TryParse(fields[1], out int x) || !int.TryParse(fields[2], out int y)) return;
		if (x < 10 || y < 20 || x >= Main.maxTilesX - 20 || y >= Main.maxTilesY - 20) return;
		stage = fields[0];
		anchor = new Point(x, y);
		ready = finished = crossed = false;
		ticks = unloadedTicks = 0;
		maxStep = 0;
	}
	public override void PostUpdate()
	{
		if (!Active || stage == null || finished) return;
		if (!ready)
		{
			if (Math.Abs(Player.position.X / 16 - anchor.X) > 4 ||
				Math.Abs(Player.Bottom.Y / 16 - anchor.Y) > 8 || !Loaded()) return;
			ready = true;
			previous = start = Player.position;
			Emit("READY", "none");
			return;
		}
		ticks++;
		float step = Vector2.Distance(previous, Player.position);
		maxStep = Math.Max(maxStep, step);
		if (!Loaded()) unloadedTicks++;
		if (Player.dead || step > 128 || Player.Bottom.Y / 16 > anchor.Y + 16 ||
			(!Loaded() && Player.velocity.Y > 0.1f))
		{
			finished = true;
			Emit("FAIL", "death-discontinuity-or-unloaded-fall");
			return;
		}
		crossed |= Netplay.GetSectionX((int)(Player.position.X / 16)) != Netplay.GetSectionX(anchor.X);
		previous = Player.position;
		bool moved = Player.position.X - start.X >= 384 && crossed;
		if (ticks >= 300 && moved && ClimateReady())
		{
			finished = true;
			Emit("PASS", "none");
		}
		else if (ticks >= 2100)
		{
			finished = true;
			Emit("FAIL", moved ? "climate-buff-deadline" : "insufficient-natural-movement");
		}
	}
}
