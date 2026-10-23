using System;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Fishing.Encounters;

public enum FishingEnemyBehavior : byte
{
	CrabLunge, TidePulse, ReefCharge, SquidRetreat, LanternAmbush, EelSlalom,
	ShadowStalk, ShellGuard, CrystalVolley, SoundHunt, RiverJet, SiltHop
}

public readonly record struct FishingMotionInput(int Tick, Vector2 Velocity, Vector2 ToTarget,
	Vector2 TargetVelocity, bool CollideX, bool CollideY);
public readonly record struct FishingMotionOutput(Vector2 Velocity, float ContactMultiplier,
	int DefenseBonus, int ShotCount, float ShotSpeed, int DebuffMode, bool Telegraph);

// Calculated only by the authoritative NPC. Pure math stays independent of tiles, players,
// random state and networking so the actual behavior can be checked without running a game.
public static class FishingEnemyMotion
{
	public static FishingMotionOutput Step(FishingEnemyBehavior behavior, FishingMotionInput input)
	{
		Vector2 direction = Unit(input.ToTarget), side = new(-direction.Y, direction.X);
		float distance = input.ToTarget.Length();
		Vector2 v = input.Velocity;
		float contact = 1f;
		int defense = 0, shots = 0, mode = 0;
		float speed = 4f;
		bool telegraph = false;
		int t = Math.Max(0, input.Tick);
		switch (behavior)
		{
			case FishingEnemyBehavior.CrabLunge:
				int crab = t % 150;
				telegraph = crab >= 90 && crab < 105;
				if (crab < 90) v = new Vector2(MathHelper.Lerp(v.X, Math.Sign(direction.X) * 1.3f, .04f), Math.Min(3f, v.Y + .18f));
				else if (telegraph) { v *= .82f; contact = .4f; defense = 5; }
				else if (crab == 105) v = new Vector2(Math.Sign(direction.X) * 5.5f, -4f);
				else { v.X *= .98f; v.Y = Math.Min(4f, v.Y + .15f); }
				break;
			case FishingEnemyBehavior.TidePulse:
				v = Follow(v, direction * (distance > 170 ? 1.4f : -.4f) + side * 1.4f, .035f);
				telegraph = t % 180 >= 120 && t % 180 < 140;
				if (t % 180 == 140) { shots = 3; speed = 3f; mode = 1; }
				break;
			case FishingEnemyBehavior.ReefCharge:
				int shark = t % 150;
				telegraph = shark >= 95 && shark < 120;
				if (shark < 95) v = Follow(v, direction * 1.3f + side * 1.9f, .025f);
				else if (telegraph) { v *= .83f; contact = .35f; }
				else if (shark == 120) v = direction * 7f;
				else v *= .98f;
				break;
			case FishingEnemyBehavior.SquidRetreat:
				int squid = t % 160;
				v = Follow(v, direction * (distance < 260 ? -2.6f : 1.3f) + side * .8f, .04f);
				telegraph = squid >= 70 && squid < 90;
				if (squid == 90) { shots = 3; speed = 4f; mode = 2; v = -direction * 4.5f; }
				break;
			case FishingEnemyBehavior.LanternAmbush:
				int lantern = t % 210;
				telegraph = lantern >= 120 && lantern < 150;
				if (lantern < 120) v = Follow(v, direction * (distance > 220 ? 1.1f : -.2f), .03f);
				else if (telegraph) { v *= .8f; contact = .4f; }
				else if (lantern == 150) v = direction * 8f;
				else v *= .97f;
				break;
			case FishingEnemyBehavior.EelSlalom:
				v = Follow(v, direction * 2.6f + side * MathF.Sin(t * .095f) * 2.1f, .09f);
				telegraph = t % 120 >= 75 && t % 120 < 90;
				if (t % 120 == 90) { shots = 2; speed = 5f; mode = 1; }
				break;
			case FishingEnemyBehavior.ShadowStalk:
				int shadow = t % 180;
				telegraph = shadow >= 100 && shadow < 130;
				if (shadow < 100) v = Follow(v, direction * (distance < 300 ? -.9f : 1.4f) - side * 1.1f, .035f);
				else if (telegraph) { v *= .86f; contact = .4f; }
				else if (shadow == 130) v = direction * 8.5f;
				else v *= .965f;
				if (shadow == 150) { shots = 2; speed = 3.5f; mode = 2; }
				break;
			case FishingEnemyBehavior.ShellGuard:
				int shell = t % 180;
				telegraph = shell < 75;
				if (telegraph) { defense = 24; contact = .35f; v *= .88f; }
				else v = Follow(v, direction * 2.8f, .035f);
				if (shell == 90) { shots = 5; speed = 4.5f; mode = 3; }
				break;
			case FishingEnemyBehavior.CrystalVolley:
				v = Follow(v, new Vector2(0, .35f), .055f);
				telegraph = t % 150 >= 95 && t % 150 < 120;
				contact = .6f;
				if (t % 150 == 120) { shots = 5; speed = 3.5f; mode = 3; }
				break;
			case FishingEnemyBehavior.SoundHunt:
				int hunter = t % 160;
				float noiseSpeed = input.TargetVelocity.LengthSquared() > 2.25f ? 3.8f : .9f;
				telegraph = hunter >= 100 && hunter < 125;
				if (hunter < 100) v = Follow(v, direction * noiseSpeed, .05f);
				else if (telegraph) { v *= .85f; contact = .4f; }
				else if (hunter == 125) v = direction * 6.5f;
				else v *= .96f;
				break;
			case FishingEnemyBehavior.RiverJet:
				int jet = t % 110;
				telegraph = jet >= 60 && jet < 75;
				if (jet < 60) v = Follow(v, direction * 1.7f + new Vector2(0, MathF.Sin(t * .12f)), .06f);
				else if (telegraph) { v *= .8f; contact = .4f; }
				else if (jet == 75) v = direction * 7.5f;
				else v *= .975f;
				if (jet == 95) { shots = 1; speed = 5.5f; mode = 1; }
				break;
			case FishingEnemyBehavior.SiltHop:
				int silt = t % 200;
				telegraph = silt >= 140 && silt < 170;
				if (silt < 140) v = new Vector2(MathHelper.Lerp(v.X, Math.Sign(direction.X) * .8f, .035f), Math.Min(3f, v.Y + .12f));
				else if (telegraph) { v *= .8f; defense = 12; contact = .35f; }
				else if (silt == 170) { v = new Vector2(direction.X * 4f, -4.5f); shots = 3; speed = 2.5f; mode = 1; }
				else { v.X *= .985f; v.Y = Math.Min(3f, v.Y + .1f); }
				break;
		}
		if (input.CollideX) v.X = -v.X * .6f;
		if (input.CollideY) v.Y = -Math.Abs(v.Y) * .4f;
		v = Finite(v);
		if (v.LengthSquared() > 9f * 9f) v = Unit(v) * 9f;
		return new FishingMotionOutput(v, contact, defense, shots, speed, mode, telegraph);
	}
	private static Vector2 Follow(Vector2 from, Vector2 to, float amount) => Vector2.Lerp(from, to, amount);
	private static Vector2 Unit(Vector2 value)
	{
		value = Finite(value);
		return value.LengthSquared() > .0001f ? Vector2.Normalize(value) : Vector2.UnitX;
	}
	private static Vector2 Finite(Vector2 v) => new(float.IsFinite(v.X) ? v.X : 0, float.IsFinite(v.Y) ? v.Y : 0);
}
