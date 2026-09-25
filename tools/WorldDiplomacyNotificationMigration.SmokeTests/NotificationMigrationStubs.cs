using System.Collections.Generic;

namespace AnimusForge;

public sealed class WorldDiplomacyStorage
{
    public int DiplomacyNotificationStateSchemaVersion { get; set; }
    public List<WorldDiplomacyDocument> Documents { get; set; } = new List<WorldDiplomacyDocument>();
}

public sealed class WorldDiplomacyDocument
{
    public bool IsPlayerAuthored { get; set; }
    public bool IsReadyForPublication { get; set; }
    public bool IsNotified { get; set; }
    public bool IsRead { get; set; }
    public bool IsCompressed { get; set; }
    public bool RumorNotified { get; set; }
    public bool FormalNoticeShown { get; set; }
}
