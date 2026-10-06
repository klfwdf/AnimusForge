using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class RetrainOutlawPartyMembersBehavior : CampaignBehaviorBase, IRetrainOutlawPartyMembersCampaignBehavior, ICampaignBehavior
{
	private Dictionary<CharacterObject, int> _retrainTable = new Dictionary<CharacterObject, int>();

	private int GetRetrainedNumberInternal(CharacterObject character)
	{
		if (!_retrainTable.TryGetValue(character, out var value))
		{
			return 0;
		}
		return value;
	}

	private int SetRetrainedNumberInternal(CharacterObject character, int numberRetrained)
	{
		return _retrainTable[character] = numberRetrained;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
	}

	private void DailyTick()
	{
		if (!(MBRandom.RandomFloat > 0.5f))
		{
			return;
		}
		int num = MBRandom.RandomInt(MobileParty.MainParty.MemberRoster.Count);
		bool flag = false;
		for (int i = 0; i < MobileParty.MainParty.MemberRoster.Count; i++)
		{
			if (flag)
			{
				break;
			}
			int index = (i + num) % MobileParty.MainParty.MemberRoster.Count;
			CharacterObject characterAtIndex = MobileParty.MainParty.MemberRoster.GetCharacterAtIndex(index);
			if (characterAtIndex.Occupation == Occupation.Bandit)
			{
				int elementNumber = MobileParty.MainParty.MemberRoster.GetElementNumber(index);
				int retrainedNumberInternal = GetRetrainedNumberInternal(characterAtIndex);
				if (retrainedNumberInternal < elementNumber && !flag)
				{
					retrainedNumberInternal++;
					SetRetrainedNumberInternal(characterAtIndex, retrainedNumberInternal);
				}
				else if (retrainedNumberInternal > elementNumber)
				{
					SetRetrainedNumberInternal(characterAtIndex, elementNumber);
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_retrainTable", ref _retrainTable);
	}

	public int GetRetrainedNumber(CharacterObject character)
	{
		if (character.Occupation == Occupation.Bandit)
		{
			int retrainedNumberInternal = GetRetrainedNumberInternal(character);
			int troopCount = MobileParty.MainParty.MemberRoster.GetTroopCount(character);
			return MathF.Min(retrainedNumberInternal, troopCount);
		}
		return 0;
	}

	public void SetRetrainedNumber(CharacterObject character, int number)
	{
		SetRetrainedNumberInternal(character, number);
	}
}
