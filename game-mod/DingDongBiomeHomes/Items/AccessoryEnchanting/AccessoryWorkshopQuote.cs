namespace DingDongBiomeHomes.Items.AccessoryEnchanting;

// Preview and commit share the same validation; quoting never charges or mutates.
internal readonly record struct AccessoryWorkshopQuote(long Cost, string Failure)
{
    internal bool Allowed => Failure == "none";
}
