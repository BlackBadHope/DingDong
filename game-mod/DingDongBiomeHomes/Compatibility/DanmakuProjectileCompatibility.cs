using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DingDongBiomeHomes.Compatibility;

internal static class DanmakuProjectileCompatibility
{
	private static Func<object> config;
	private static Func<object, bool> enabled;
	private static Func<object, int> additional, spacing;
	private static Func<object, List<ProjectileDefinition>> blacklist;

	internal static void Install(List<ILHook> hooks)
	{
		Type type = R19CompatibilitySystem.Provider("danmaku", "1.2.1", "danmaku.Proj100GlobalProjectile");
		if (type == null) return;
		if (type.Module.ModuleVersionId != new Guid("dcef802e-f143-42ae-9851-e0aeef109a59"))
			throw new InvalidOperationException("Danmaku projectile provider identity changed.");
		Type settings = type.Assembly.GetType("danmaku.Proj100Config", true);
		MethodInfo getter = typeof(ModContent).GetMethods(BindingFlags.Static | BindingFlags.Public)
			.Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(settings);
		config = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Call(getter), typeof(object))).Compile();
		enabled = Field<bool>(settings, "EnableMod"); additional = Field<int>(settings, "AdditionalProjectiles");
		spacing = Field<int>(settings, "ProjectileSpacing"); blacklist = Field<List<ProjectileDefinition>>(settings, "BlacklistedProjectiles");
		hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "OnSpawn"), il => {
			var cursor = new ILCursor(il);
			cursor.Emit(OpCodes.Ldarg_1); cursor.Emit(OpCodes.Ldarg_2);
			cursor.EmitDelegate<Action<Projectile, IEntitySource>>(OnSpawn);
			cursor.Emit(OpCodes.Ret);
		}));
	}
	private static Func<object, T> Field<T>(Type type, string name)
	{
		ParameterExpression argument = Expression.Parameter(typeof(object));
		return Expression.Lambda<Func<object, T>>(Expression.Field(Expression.Convert(argument, type), name), argument).Compile();
	}

	private static void OnSpawn(Projectile projectile, IEntitySource source)
	{
		object settings = config();
		int count = additional(settings);
		if (count <= 0 || !enabled(settings) || !IsCombatProjectile(projectile)) return;
		if (blacklist(settings).Any(entry => entry.Type == projectile.type)) return;
		bool allowed = source is EntitySource_TileInteraction || source is EntitySource_Parent parent &&
			(parent.Entity is Player || parent.Entity is Projectile origin && (origin.minion || origin.sentry));
		if (!allowed) return;
		projectile.usesIDStaticNPCImmunity = false;
		projectile.usesLocalNPCImmunity = true;
		projectile.localNPCHitCooldown = 10;
		count = Math.Min(count, Main.maxProjectiles);
		float gap = spacing(settings) * 0.1f;
		bool horizontal = Math.Abs(projectile.velocity.X) > Math.Abs(projectile.velocity.Y);
		for (int index = 0; index < count; index++)
		{
			// Multiplication must not evict existing shots, minions or utility entities.
			bool free = false;
			for (int slot = 0; slot < Main.maxProjectiles; slot++)
				if (!Main.projectile[slot].active) { free = true; break; }
			if (!free) break;
			float offset = (index - (count - 1) * 0.5f) * gap;
			Vector2 position = projectile.Center + (horizontal ? new Vector2(0, offset) : new Vector2(offset, 0));
			Projectile.NewProjectile(projectile.GetSource_FromThis(), position, projectile.velocity, projectile.type,
				projectile.damage, projectile.knockBack, projectile.owner, projectile.ai[0], projectile.ai[1], projectile.ai[2]);
		}
	}
	internal static bool IsCombatProjectile(Projectile projectile) => projectile != null && projectile.active &&
		projectile.damage > 0 && (projectile.friendly || projectile.hostile) && !projectile.bobber &&
		!projectile.minion && !projectile.sentry && !Main.projPet[projectile.type] && !Main.projHook[projectile.type];
	internal static void Reset() { config = null; enabled = null; additional = spacing = null; blacklist = null; }
}
