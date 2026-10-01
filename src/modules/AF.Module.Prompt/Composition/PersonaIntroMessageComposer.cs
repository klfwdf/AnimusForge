using System;using System.Text;
namespace AnimusForge;
internal sealed class SystemNpcIntroSnapshot {
 internal long Generation;
 internal string clanName="";
 internal string factionDisplay="";
 internal string heroName="";
 internal string identityTitle="";
 internal string reputationText="";
 internal string equipmentText="";
 internal string ageText="";
 internal string cultureText="";
 internal string personalityText="";
 internal string backgroundText="";
 internal string nobleEtiquettePrompt="";
 internal string clanRole="";
 internal string relationshipLine="";
 internal int clanTier;
}

internal sealed class SceneNpcIntroSnapshot {
 internal long Generation;
 internal string name="";
 internal string givenName="";
 internal string identity="";
 internal string personality="";
 internal string background="";
 internal string faction="";
 internal string clan="";
 internal string clanRole="";
 internal string reputation="";
 internal string culture="";
 internal string age="";
 internal string equipment="";
 internal string inventorySummary="";
 internal string currentDateText="";
 internal string pregnancySelfKnowledge="";
 internal string heroJoinPartyRuntimeFact="";
 internal string partyRepresentativePrompt="";
 internal string ceremonyRole="";
 internal string npcCurrentMountLine="";
 internal string npcTroopsLine="";
 internal string npcPrisonersLine="";
 internal string nobleEtiquettePrompt="";
 internal string sceneLocationLine="";
 internal string settlementHeroNpcLine="";
 internal string settlementFlavorLine="";
 internal string settlementRulerPresenceLine="";
 internal string playerIntroLine="";
 internal string prisonerContextLine="";
 internal string playerCommandRelationshipLine="";
 internal string nearbyPresentNpcLine="";
 internal string inventoryHeader="";
 internal bool hasHero;
 internal bool isFemale;
 internal bool hideReputation;
 internal bool heroInPlayerParty;
 internal bool hasNonHeroParty;
 internal bool includeInventorySummary;
 internal bool partyTransferTopicSelected;
 internal int clanTier;
}

internal sealed class ScenePlayerIntroSnapshot {
 internal long Generation;
 internal string culture="";
 internal string age="";
 internal string genderText="";
 internal string equipment="";
 internal string equipmentValueInline="";
 internal string identitySentence="";
 internal string clanName="";
 internal string playerPublicName="";
 internal string reputation="";
 internal string playerCurrentMountLine="";
 internal string companionRole="";
 internal string vassalageRelationLine="";
 internal string inlineState="";
 internal string factionWarLine="";
 internal string crimeRatingLine="";
 internal string playerTroopsLine="";
 internal string playerPrisonersLine="";
 internal string townPartyStayHint="";
 internal bool knowsPlayerIdentity;
 internal bool isClanLeader;
 internal bool isCompanion;
 internal bool includePlayerPartyRoster;
 internal int clanTier;
}
internal static class PersonaIntroMessageComposer {
 internal static string ComposeSystemNpc(SystemNpcIntroSnapshot s) {
 if(s==null)return "";
 string clanName=s.clanName;
 string factionDisplay=s.factionDisplay;
 string heroName=s.heroName;
 string identityTitle=s.identityTitle;
 string reputationText=s.reputationText;
 string equipmentText=s.equipmentText;
 string ageText=s.ageText;
 string cultureText=s.cultureText;
 string personalityText=s.personalityText;
 string backgroundText=s.backgroundText;
 string nobleEtiquettePrompt=s.nobleEtiquettePrompt;
 string clanRole=s.clanRole;

		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("你是")
			.Append(factionDisplay)
			.Append("的")
			.Append(clanName)
			.Append("的")
			.Append(heroName)
			.Append("，你是家族中的")
			.Append(clanRole)
			.Append("，你的身份是")
			.Append(identityTitle)
			.Append("，你")
			.Append(reputationText)
			.Append("，你身上穿着")
			.Append(equipmentText)
			.Append("，你的个性为：")
			.Append(personalityText)
			.Append("，你的背景是：")
			.Append(backgroundText);
		string npcPlayerRelationshipLine = s.relationshipLine;
		if (!string.IsNullOrWhiteSpace(npcPlayerRelationshipLine))
		{
			stringBuilder.Append("。").Append(npcPlayerRelationshipLine);
		}
		if (!string.IsNullOrWhiteSpace(nobleEtiquettePrompt))
		{
			stringBuilder.Append("。").Append(nobleEtiquettePrompt).Append("你的年纪是");
		}
		else
		{
			stringBuilder.Append("，你的年纪是");
		}
		stringBuilder
			.Append(ageText)
			.Append("，你是")
			.Append(cultureText)
			.Append("。");
		return stringBuilder.ToString().Trim();
}

 internal static string ComposeSceneNpc(SceneNpcIntroSnapshot s) {
 if(s==null)return "";
 string name=s.name;
 string givenName=s.givenName;
 string identity=s.identity;
 string personality=s.personality;
 string background=s.background;
 string faction=s.faction;
 string clan=s.clan;
 string clanRole=s.clanRole;
 string reputation=s.reputation;
 string culture=s.culture;
 string age=s.age;
 string equipment=s.equipment;
 string inventorySummary=s.inventorySummary;
 string currentDateText=s.currentDateText;

bool hideReputation=s.hideReputation,includeInventorySummary=s.includeInventorySummary,partyTransferTopicSelected=s.partyTransferTopicSelected;
		StringBuilder stringBuilder = new StringBuilder();
		if (s.hasHero)
		{
			string pregnancySelfKnowledge = s.pregnancySelfKnowledge;
			string clanRoleText = (clanRole == "族长") ? "族长" : (s.isFemale ? "女性成员" : "男性成员");
			stringBuilder.Append("你名叫")
				.Append(name)
				.Append("，是")
				.Append(faction)
				.Append("的")
				.Append(clan)
				.Append("的")
				.Append(clanRoleText)
				.Append("，你的身份是")
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append("，你").Append(reputation);
			}
			string heroJoinPartyRuntimeFact = s.heroJoinPartyRuntimeFact;
			if (!string.IsNullOrWhiteSpace(heroJoinPartyRuntimeFact))
			{
				stringBuilder.Append("。").Append(heroJoinPartyRuntimeFact.Trim().TrimEnd('。'));
			}
			if (!string.IsNullOrWhiteSpace(pregnancySelfKnowledge))
			{
				stringBuilder.Append("。").Append(pregnancySelfKnowledge);
			}
			stringBuilder.Append("。");
		}
		else
		{
			stringBuilder.Append("你是一个")
				.Append(name);
			if (!string.IsNullOrWhiteSpace(givenName) && !string.Equals(givenName, name, StringComparison.Ordinal))
			{
				stringBuilder.Append("，名叫").Append(givenName);
			}
			stringBuilder.Append("，你的身份是")
				.Append(identity);
			if (!hideReputation && !string.IsNullOrWhiteSpace(reputation))
			{
				stringBuilder.Append("，你").Append(reputation);
			}
			stringBuilder.Append("。");
			string partyRepresentativePrompt = s.partyRepresentativePrompt;
			if (!string.IsNullOrWhiteSpace(partyRepresentativePrompt))
			{
stringBuilder.Append(partyRepresentativePrompt);
				}
			}
			string ceremonyRole = s.ceremonyRole;
			if (!string.IsNullOrWhiteSpace(ceremonyRole))
			{
				stringBuilder.Append(ceremonyRole);
			}
			stringBuilder.AppendLine()
				.Append("你身上穿着")
			.Append(equipment)
			.Append("。");
		string npcCurrentMountLine = s.npcCurrentMountLine;
		if (!string.IsNullOrWhiteSpace(npcCurrentMountLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(npcCurrentMountLine);
		}
		if (s.hasHero)
		{
			try
			{
				if (!s.heroInPlayerParty)
				{
					bool hasPartyRestrictionContext = false;
					string npcTroopsLine = s.npcTroopsLine;
					string npcPrisonersLine = s.npcPrisonersLine;
					if (!string.IsNullOrWhiteSpace(npcTroopsLine))
					{
						stringBuilder.Append(npcTroopsLine).Append("。");
						hasPartyRestrictionContext = true;
					}
					if (!string.IsNullOrWhiteSpace(npcPrisonersLine))
					{
						stringBuilder.Append(npcPrisonersLine).Append("。");
						hasPartyRestrictionContext = true;
					}
					if (hasPartyRestrictionContext && !partyTransferTopicSelected)
					{
						stringBuilder.Append("备注：你暂时不可以转移你的俘虏和士兵。");
					}
				}
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				bool nonHeroParty = s.hasNonHeroParty;
				if (nonHeroParty)
				{
					string npcTroopsLine = s.npcTroopsLine;
					if (!string.IsNullOrWhiteSpace(npcTroopsLine))
					{
						stringBuilder.Append(npcTroopsLine).Append("。");
					}
					string npcPrisonersLine = s.npcPrisonersLine;
					if (!string.IsNullOrWhiteSpace(npcPrisonersLine) && !string.Equals(npcPrisonersLine, "你所属队伍无俘虏", StringComparison.Ordinal))
					{
						stringBuilder.Append(npcPrisonersLine).Append("。");
					}
				}
			}
			catch
			{
			}
		}
		if (s.hasHero)
		{
			string nobleEtiquettePrompt = s.nobleEtiquettePrompt;
			stringBuilder.AppendLine()
				.Append("你的个性为：")
				.Append(personality)
				.Append("。")
				.AppendLine()
				.Append("你的背景是：")
				.Append(background);
			if (!string.IsNullOrWhiteSpace(nobleEtiquettePrompt))
			{
				stringBuilder.Append("。").Append(nobleEtiquettePrompt);
			}
			else
			{
				stringBuilder.Append("。");
			}
		}
		stringBuilder.AppendLine()
			.Append("你的年纪是")
			.Append(age)
			.Append("，你是")
			.Append(culture)
			.Append("。");
		if (!string.IsNullOrWhiteSpace(currentDateText))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("现在的时间是").Append(currentDateText).Append("。");
		}
		string sceneLocationLine = s.sceneLocationLine;
		if (!string.IsNullOrWhiteSpace(sceneLocationLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(sceneLocationLine);
		}
		string settlementHeroNpcLine = s.settlementHeroNpcLine;
		if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
		{
			const string prefix = "当前定居点HeroNPC：";
			if (settlementHeroNpcLine.StartsWith(prefix, StringComparison.Ordinal))
			{
				settlementHeroNpcLine = settlementHeroNpcLine.Substring(prefix.Length).Trim();
			}
			if (!string.IsNullOrWhiteSpace(settlementHeroNpcLine))
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("这个定居点有这些人物：").Append(settlementHeroNpcLine);
			}
		}
		string settlementFlavorLine = s.settlementFlavorLine;
		if (!string.IsNullOrWhiteSpace(settlementFlavorLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementFlavorLine);
		}
		string settlementRulerPresenceLine = s.settlementRulerPresenceLine;
		if (!string.IsNullOrWhiteSpace(settlementRulerPresenceLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(settlementRulerPresenceLine);
		}


		string playerIntroLine = s.playerIntroLine;
		if (!string.IsNullOrWhiteSpace(playerIntroLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerIntroLine);
		}
		string prisonerContextLine = s.prisonerContextLine;
		if (!string.IsNullOrWhiteSpace(prisonerContextLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(prisonerContextLine);
		}
		string playerCommandRelationshipLine = s.playerCommandRelationshipLine;
		if (!string.IsNullOrWhiteSpace(playerCommandRelationshipLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerCommandRelationshipLine);
		}
		string nearbyPresentNpcLine = s.nearbyPresentNpcLine;
		if (!string.IsNullOrWhiteSpace(nearbyPresentNpcLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(nearbyPresentNpcLine);
		}
		if (includeInventorySummary && !string.IsNullOrWhiteSpace(inventorySummary))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(s.inventoryHeader);
			stringBuilder.AppendLine();
			stringBuilder.Append(inventorySummary);
		}
		return stringBuilder.ToString().Trim();
}

 internal static string ComposeScenePlayer(ScenePlayerIntroSnapshot s) {
 if(s==null)return "";
 string culture=s.culture;
 string age=s.age;
 string genderText=s.genderText;
 string equipment=s.equipment;
 string equipmentValueInline=s.equipmentValueInline;
 string identitySentence=s.identitySentence;
 string clanName=s.clanName;

int clanTier=s.clanTier;bool isClanLeader=s.isClanLeader,includePlayerPartyRoster=s.includePlayerPartyRoster;
		StringBuilder stringBuilder = new StringBuilder();

		bool knowsPlayerIdentity = s.knowsPlayerIdentity;
		if (knowsPlayerIdentity)
		{
			string playerPublicName = s.playerPublicName;
			string reputation = s.reputation;
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，你知道他叫")
				.Append(string.IsNullOrWhiteSpace(playerPublicName) ? "这人" : playerPublicName)
				.Append("，他")
				.Append(reputation)
				.Append("，是")
				.Append(clanName)
				.Append("的")
				.Append(isClanLeader ? "族长" : "成员")
				.Append("。从面貌上来看，是一个")
				.Append(genderText)
				.Append(age)
				.Append("，穿着")
				.Append(equipment)
				.Append("。");
		}
		else if (clanTier >= 2)
		{
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，你不知道他的名字。从面貌上来看，是一个")
				.Append(genderText)
				.Append(age)
				.Append("，穿着")
				.Append(equipment)
				.Append("。");
		}
		else
		{
			stringBuilder.Append("你面前站着一个")
				.Append(culture)
				.Append("，他看起来是个普通人，总之不是贵族，从他的面貌上来看，是一个")
				.Append(genderText)
				.Append(age)
				.Append("，穿着")
				.Append(equipment)
				.Append("。");
		}
		string playerCurrentMountLine = s.playerCurrentMountLine;
		if (!string.IsNullOrWhiteSpace(playerCurrentMountLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(playerCurrentMountLine);
		}
		if (s.isCompanion)
		{
			stringBuilder.Append("你知道他是你的上司，而你是他的")
				.Append(s.companionRole)
				.Append("，你要服从他的命令,并且你无法和他进行任何物品交换，因为你们的库存是共享的。");
		}
		if (!string.IsNullOrWhiteSpace(equipmentValueInline))
		{
			stringBuilder.Append(equipmentValueInline).Append("。");
		}
		if (knowsPlayerIdentity && !string.IsNullOrWhiteSpace(identitySentence))
		{
			stringBuilder.Append(identitySentence);
		}
		string vassalageRelationLine = s.vassalageRelationLine;
		if (!string.IsNullOrWhiteSpace(vassalageRelationLine))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(vassalageRelationLine);
		}
		string inlineState = s.inlineState;
		if (!string.IsNullOrWhiteSpace(inlineState))
		{
			stringBuilder.Append(inlineState);
		}
		string factionWarLine = s.factionWarLine;
		if (!string.IsNullOrWhiteSpace(factionWarLine))
		{
			stringBuilder.Append(factionWarLine);
		}
		string crimeRatingLine = s.crimeRatingLine;
		if (!string.IsNullOrWhiteSpace(crimeRatingLine))
		{
			stringBuilder.Append(crimeRatingLine);
		}
		if (includePlayerPartyRoster)
		{
			try
			{
				string playerTroopsLine = s.playerTroopsLine;
				string playerPrisonersLine = s.playerPrisonersLine;
				if (!string.IsNullOrWhiteSpace(playerTroopsLine))
				{
					stringBuilder.Append(playerTroopsLine).Append("。");
				}
				if (!string.IsNullOrWhiteSpace(playerPrisonersLine))
				{
					stringBuilder.Append(playerPrisonersLine).Append("。");
				}
			}
			catch
			{
			}
		}
		else
		{
			string townPartyStayHint = s.townPartyStayHint;
			if (!string.IsNullOrWhiteSpace(townPartyStayHint))
			{
				stringBuilder.Append(townPartyStayHint).Append("。");
			}
		}
		return stringBuilder.ToString().Trim();
}
}
