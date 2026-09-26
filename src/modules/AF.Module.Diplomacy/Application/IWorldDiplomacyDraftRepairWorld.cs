using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal interface IWorldDiplomacyDraftRepairWorld : IWorldDiplomacyPromptWorld
{
    List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source, WorldDiplomacyRound round, string author);
    string BuildBilateralState(string author, string target);
    string BuildGovernmentHardFact(string author);
    string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round, string author, List<string> ids, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument source);
    string BuildCanonicalHistoryBlock(long throughSequence);
    string NewId(string prefix);
    string BuildGenerationLegalActionSignature(WorldDiplomacyJob job);
    void EnqueueJob(WorldDiplomacyJob job);
    void Log(string text);
    void AbandonRejectedGeneration(WorldDiplomacyJob job, string author, string target, string reason);
}
