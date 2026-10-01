using System;using System.Collections.Generic;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.Core;using TaleWorlds.MountAndBlade;
namespace AnimusForge;
public partial class MyBehavior {
 private PersonaEquipmentPromptCaptureAdapter CreatePersonaEquipmentPromptCaptureAdapter() => new(new MyPersonaIntroLivePort {
            BuildAgeBracketLabel = BuildAgeBracketLabel,
            BuildHeroEquipmentSummaryForPrompt = BuildHeroEquipmentSummaryForPrompt,
            BuildHeroIdentityTitleForPrompt = BuildHeroIdentityTitleForPrompt,
            BuildNobleEtiquettePromptForHero = BuildNobleEtiquettePromptForHero,
            BuildNpcPlayerKinshipPromptLine = BuildNpcPlayerKinshipPromptLine,
            GetClanTierReputationLabel = GetClanTierReputationLabel,
            GetHeroCultureNameForPrompt = GetHeroCultureNameForPrompt,
            GetHeroFactionAndLiegeForPrompt = GetHeroFactionAndLiegeForPrompt,
            GetNpcPersonaStrings = GetNpcPersonaStrings
 }, null);
 private static HeroEquipmentPromptLivePort CreateHeroEquipmentPromptLivePort() => new() {ResolveContext=TryResolveEquipmentContextForPrompt,GetItem=TryGetHeroEquipmentItemForPrompt};
 private static HeroIdentityPromptLivePort CreateHeroIdentityPromptLivePort() => new() {ResolveRuledKingdom=TryResolveActiveKingdomRuledByHeroForPrompt};
}
