"""Retired private migration wrappers; referenced by deletion and caller-parity tests."""
HOST='src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs'
RETIRED = [
 'private bool EnsureCurrentCanonicalPromptContractBeforeSend(',
 'private bool EnqueueGeneratedDeclarationRepair(',
 'private void ReconcilePlayerDeclarationWithOpenOffer(',
 'private string BuildCurrentGeographicRelations(',
 'private void SynchronizeCourtKnowledge(',
 'private void ProcessPlayerMandatoryResponseTimeout(',
 'private void ProcessPlayerResponseTimeouts(',
 'private void CompleteActiveExchange(',
 'private string BuildAutonomousOpeningPrompt(',
 'private string BuildFallbackAnnualSummary(',
 'private static void AppendOpenOfferResponseIntents(',
 'private void MigratePolicyCountdownHistory(',
 'private List<WorldDiplomacyCanonicalProtectedFact> BuildCanonicalProtectedFactsThrough(',
 'private void MigrateDiplomaticThreatsToNextDeclarationRules(',
 'private void MigrateDiplomaticThreatComplianceConsequencesV3(',
 'private int ApplyInternationalReputationDelta(',
 'private static Settlement ResolveMentionedSettlement(',
 'private void ExecuteMakePeace(',
 'private void ExecuteAlliance(',
 'private void ExecuteTradeAgreement(',
 'private static bool HasProposalTakenEffect('
]
def remove_retired(source, declaration):
 for signature in RETIRED:
  body=declaration(source,signature)
  # Remove the declaration's existing indentation and at most its separator.
  start=source.index(body);line=source.rfind('\n',0,start)+1
  assert not source[line:start].strip()
  end=start+len(body)
  if source[end:end+2]=='\n\n':end+=2
  elif source[end:end+1]=='\n':end+=1
  source=source[:line]+source[end:]
 return source
