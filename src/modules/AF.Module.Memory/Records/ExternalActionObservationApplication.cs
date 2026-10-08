using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;
namespace AnimusForge;
internal delegate void RecordExternalNpcActionCapability(Hero actorHero, string text, string stableKey, string actionKind, bool isMajor, bool isRecent, Hero targetHero, Settlement settlement, string locationText, bool allowNonLordHero, bool? won);
internal delegate void RecordExternalPlayerActionCapability(string text, string stableKey, string actionKind, bool isMajor, Hero targetHero, Settlement settlement, string locationText, bool? won);
internal delegate void RecordNpcMajorActionCapability(Hero hero, string text, string stableKey, NpcActionFacts facts = null, bool allowNonLordHero = false);
internal delegate void RecordNpcRecentActionCapability(Hero hero, string text, string stableKey, bool dedupeAcrossWindow = false, NpcActionFacts facts = null, bool allowNonLordHero = false);
internal delegate void RecordEventSourceMaterialCapability(string materialKind,string label,string snapshotText,string stableKey,string kingdomId,string settlementId,bool includeInWorld,bool includeInKingdom,string actorHeroId="",string actorKingdomId="",int dayOverride=-1,string gameDateOverride="");

// Binding to existing A records; the application has no independent ledger or sequence.
internal sealed class ExternalActionObservationApplication
{
internal readonly RecordExternalNpcActionCapability RecordExternalNpcAction;
internal readonly RecordExternalPlayerActionCapability RecordExternalPlayerAction;
internal readonly RecordNpcMajorActionCapability RecordNpcMajorAction;
internal readonly RecordNpcRecentActionCapability RecordNpcRecentAction;
internal readonly RecordEventSourceMaterialCapability RecordEventSourceMaterial;
internal ExternalActionObservationApplication(RecordExternalNpcActionCapability recordExternalNpcAction,RecordExternalPlayerActionCapability recordExternalPlayerAction,RecordNpcMajorActionCapability recordNpcMajorAction,RecordNpcRecentActionCapability recordNpcRecentAction,RecordEventSourceMaterialCapability recordEventSourceMaterial)
{
RecordExternalNpcAction=recordExternalNpcAction ?? throw new ArgumentNullException(nameof(recordExternalNpcAction));
RecordExternalPlayerAction=recordExternalPlayerAction ?? throw new ArgumentNullException(nameof(recordExternalPlayerAction));
RecordNpcMajorAction=recordNpcMajorAction ?? throw new ArgumentNullException(nameof(recordNpcMajorAction));
RecordNpcRecentAction=recordNpcRecentAction ?? throw new ArgumentNullException(nameof(recordNpcRecentAction));
RecordEventSourceMaterial=recordEventSourceMaterial ?? throw new ArgumentNullException(nameof(recordEventSourceMaterial));
}
internal void RecordNpcOutcome(Hero hero,string text,string stableKey,NpcActionFacts facts,bool allowNonLordHero=false)
{
    RecordNpcMajorAction(hero,text,stableKey,facts,allowNonLordHero);
    RecordNpcRecentAction(hero,text,stableKey,true,facts,allowNonLordHero);
}
}
