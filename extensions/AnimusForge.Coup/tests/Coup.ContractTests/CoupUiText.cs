namespace AnimusForge.CoupSystem;

// Contract-test fallback for the isolated CoupOutcomeReport compilation.
// The production implementation resolves these strings through Bannerlord TextObject.
internal static class CoupUiText
{
    internal static string PoliticalSettlementComplete => "Coup political settlement is complete.";
    internal static string MinimumTroopsConflict(int minimumTroops, int streetLimit)
        => "篡位配置冲突：最低突击队人数" + minimumTroops + "超过街道上限" + streetLimit + "，请在 MCM 中调整。";
    internal static string InsufficientTier(int current, int required)
        => "家族等级不足：" + current + "／" + required + "级。";
    internal static string InsufficientInfluence(string current, int required)
        => "影响力不足：" + current + "／" + required + "（仅检查，不扣除）。";
    internal static string InsufficientRegulars(int required, int current)
        => "至少需要" + required + "名健康普通士兵，其中1人留守接应；当前" + current + "人。";
    internal static string FormerRulerHeld => "is captured and still held by your party.";
    internal static string FormerRulerNoLongerHeld => "was captured but is no longer held by your party.";
    internal static string FormerRulerReleased => "was forced to abdicate; you chose not to capture him.";

    internal static string VictoryReport(string kingdom, string ruler, string town, string ownerClan, string king,
        string custody, int alliedKilled, int alliedWounded, int defenderKilled, int defenderWounded)
        => $"The coup is complete.\n\nKingdom: {kingdom}\nCurrent ruler: {ruler}\nTown taken: {town}\nTown owner: {ownerClan}\nFormer ruler: {king} {custody}\n\nRecorded casualties:\nAssault party: {alliedKilled} killed, {alliedWounded} wounded\nDefenders: {defenderKilled} killed, {defenderWounded} wounded\n\nAftermath: rebellion eligibility was assessed and the request was registered; whether a rebellion actually begins depends on later campaign results.";

    internal static string ReportNoEntry => "The assault party was assembled, but no actual scene entry was recorded.";
    internal static string ReportStreet => "entered the town street";
    internal static string ReportGate => "breached the hall entrance";
    internal static string ReportHall(int allies) => $"led {allies} assault troops into the lord's hall";
    internal static string ReportSubdued => "subdued the ruler";
    internal static string ReportProcessLabel => "Process: ";
    internal static string ReportResultLabel => "Result: ";
    internal static string ReportIntro(string player, string kingdom, string town, string king, int allies)
        => $"{player} launched an armed coup against {king} in {town} of {kingdom} with {allies} assault troops.";
    internal static string ReportSuccess(string player, string kingdom, string town, bool captured)
        => captured
            ? $"The coup succeeded: {player} took the throne of {kingdom} and seized {town}; the former ruler was captured."
            : $"The coup succeeded: {player} took the throne of {kingdom} and seized {town}; you chose not to capture the former ruler.";
    internal static string ReportFailure(bool atWar)
        => atWar
            ? "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land and went to war with it."
            : "The coup failed; the throne was not taken. The clan left the former kingdom with its existing land.";
    internal static string ReportCasualties(int alliedKilled, int alliedWounded, int defenderKilled, int defenderWounded)
        => $"Recorded casualties: assault party {alliedKilled} killed, {alliedWounded} wounded; defenders {defenderKilled} killed, {defenderWounded} wounded.";
}
