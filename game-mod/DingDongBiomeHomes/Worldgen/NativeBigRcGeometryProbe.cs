using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Export the exact freshly generated geometry before load clears GenVars.
internal sealed class NativeBigRcGeometryProbe : ModSystem
{
    public override void PostWorldGen()
    {
        if(!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_BIG_GENERATION_PROBE")!="1" ||
            !(Main.ActiveWorldFileData?.Name??"").StartsWith("DingDongTerrainProbe-",StringComparison.Ordinal))return;
        MapHelper.Initialize();
        string folder=Path.Combine(Main.SavePath,"DingDongDiagnostics","BigRcGeometry");Directory.CreateDirectory(folder);
        NativeTerrainRaster.ExportRegion(Path.Combine(folder,"surface-full-width.bmp"),new(0,Math.Max(0,(int)Main.worldSurface-700),Main.maxTilesX,1400),4);
        Rectangle desert=GenVars.UndergroundDesertLocation;
        if(desert.Width>0 && desert.Height>0)
            NativeTerrainRaster.ExportRegion(Path.Combine(folder,"desert.bmp"),new(desert.X-120,Math.Max(0,desert.Y-500),desert.Width+240,desert.Height+600),2);
        Rectangle temple=new(GenVars.tLeft,GenVars.tTop,Math.Max(0,GenVars.tRight-GenVars.tLeft),Math.Max(0,GenVars.tBottom-GenVars.tTop));
        if(temple.Width>0 && temple.Height>0)
        {
            Rectangle context=temple;context.Inflate(80,80);
            NativeTerrainRaster.ExportRegion(Path.Combine(folder,"temple.bmp"),context,2);
        }
        File.WriteAllText(Path.Combine(folder,"state.json"),JsonSerializer.Serialize(new {
            stage="after-native-generation-before-load",state=NativeWorldgenStateSnapshot.Capture(),
            temple=new{x=temple.X,y=temple.Y,width=temple.Width,height=temple.Height},
            desert=new{x=desert.X,y=desert.Y,width=desert.Width,height=desert.Height},
            diagnosticOnly=true,userSavesUsed=false,visualApproval=false
        },new JsonSerializerOptions{WriteIndented=true}));
        Mod.Logger.Info("DINGDONG_BIG_RC_GEOMETRY|state=EXPORTED|directory="+folder);
    }
}
