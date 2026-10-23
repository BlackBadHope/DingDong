using System;
using System.Linq;
using System.Linq.Expressions;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;
// Bound once to the actual provider assembly, with runtime-checked casts. No
// calls to cached SignatureGlobalItem.Level/Damage getters on the crash path.
internal sealed class SignatureProjectileDamageReader
{
    private readonly Func<object> getConfig;
    private readonly Func<object,int> rawLevel, maxLevel, multiplier, maxDamage;
    private readonly Func<object,float> perLevel;
    internal SignatureProjectileDamageReader(Type item, Type config)
    {
        var instance = typeof(ModContent).GetMethods().Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(config);
        getConfig = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Call(instance), typeof(object))).Compile();
        rawLevel = Read<int>(item, "signatureLevel");
        maxLevel = Read<int>(config, "MaxLevel");
        perLevel = Read<float>(config, "Damage", "PerLevel");
        multiplier = Read<int>(config, "Damage", "PerLevelMult");
        maxDamage = Read<int>(config, "Damage", "Max");
    }
    internal object Config() => getConfig();
    internal int Damage(object state, bool capped, int combatCap = 0)
    {
        object config = getConfig();
        if (state == null || config == null) return 0;
        int maximumLevel=maxLevel(config);
        if(combatCap>0)maximumLevel=maximumLevel>0?Math.Min(maximumLevel,combatCap):combatCap;
        return SignatureProjectileDamageRules.Damage(rawLevel(state), maximumLevel, perLevel(config), multiplier(config), maxDamage(config), capped);
    }
    internal static Func<object,T> Read<T>(Type type, params string[] fields)
    {
        var value=Expression.Parameter(typeof(object),"value");
        Expression body=Expression.Convert(value,type);
        foreach(string name in fields) body=Expression.Field(body,name);
        return Expression.Lambda<Func<object,T>>(Expression.Convert(body,typeof(T)),value).Compile();
    }
    internal static Action<object,T> Write<T>(Type type,string field)
    {
        var target=Expression.Parameter(typeof(object),"target");var value=Expression.Parameter(typeof(T),"value");
        var member=Expression.Field(Expression.Convert(target,type),field);
        return Expression.Lambda<Action<object,T>>(Expression.Block(Expression.Assign(member,Expression.Convert(value,member.Type)),Expression.Empty()),target,value).Compile();
    }
}
