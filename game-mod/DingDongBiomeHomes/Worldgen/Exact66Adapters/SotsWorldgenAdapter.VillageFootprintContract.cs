using System;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void VerifyVillageFootprints(Mod provider)
	{
		Check("GenerateNewMineEntrance", "82FC27BBDCC53D528312277068F842A808DE73B7136EEACF90874D3E9358F48A",
			typeof(int), typeof(int));
		Check("GenerateTunnel", "3DED922E07534EE0454DB3CAC065B5B41B59AB2F72FC1323E4DB9FBF130A1DEE",
			typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(float), typeof(int), typeof(int), typeof(bool));
		Check("GenerateCaveCircle", "602BC3E66A4BABDE4518F5F6467AAFC862A4F1623EDF7D9B964642D9843F4A77",
			typeof(int), typeof(int), typeof(float), typeof(float), typeof(int), typeof(float), typeof(float));
		Check("GenHalfCircle", "5C2CA9ED6BF7B8D36C7D89769341071B5232C976D02374F15BEE1392C70313CE",
			typeof(int), typeof(int), typeof(int), typeof(int), typeof(int));

		void Check(string name, string digest, params Type[] arguments) =>
			Exact66ProviderGate.AssertIl(Exact66ProviderGate.Method(provider, AbandonedVillageType,
				name, true, typeof(void), arguments), digest);
	}
}
