using System;
namespace DingDongBiomeHomes.Compatibility;
internal static class SignatureProjectileDamageRules
{
    internal static int Damage(int rawLevel,int maxLevel,float perLevel,int multiplier,int maximum,bool capped)
    {
        int level=Math.Clamp(rawLevel,0,maxLevel>0?maxLevel:int.MaxValue);
        float raw=level*perLevel*multiplier;
        if(!float.IsFinite(raw)) return 0;
        int amount=raw>=int.MaxValue?int.MaxValue:raw<=int.MinValue?int.MinValue:(int)raw;
        if(!capped) return amount;
        int lower=maximum==int.MinValue?int.MaxValue:-maximum;
        int upper=maximum>0?maximum:int.MaxValue;
        return Math.Clamp(amount,lower,upper);
    }
}
