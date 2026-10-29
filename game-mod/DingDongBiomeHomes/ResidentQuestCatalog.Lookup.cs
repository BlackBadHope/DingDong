namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
    internal static int SelectQuestId(string persistentId, string npcKey, int completed) =>
        PersonalResidentQuestCatalog.FirstId + System.Math.Max(0, completed);
    internal static ResidentQuestDefinition SelectAvailable(string persistentId, NpcTownIdentity identity, int completed, bool allowResidenceObjectives = true) =>
        PersonalResidentQuestCatalog.Resolve(identity, completed);
}
