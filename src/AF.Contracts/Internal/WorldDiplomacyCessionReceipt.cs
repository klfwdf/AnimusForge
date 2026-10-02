namespace AnimusForge;

internal readonly struct WorldDiplomacyCessionReceipt
{
    internal readonly bool Requested, Applied, Known;
    internal readonly string Message;
    internal WorldDiplomacyCessionReceipt(bool requested, bool applied, bool known, string message)
    { Requested = requested; Applied = applied; Known = known; Message = message ?? ""; }
    internal bool Complete => !Requested || (Known && Applied);
}
