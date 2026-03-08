using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000091 RID: 145
	public class PartyThinkParams
	{
		// Token: 0x170004E1 RID: 1249
		// (get) Token: 0x0600125D RID: 4701 RVA: 0x00053E7A File Offset: 0x0005207A
		public MBReadOnlyList<ValueTuple<AIBehaviorData, float>> AIBehaviorScores
		{
			get
			{
				return this._aiBehaviorScores;
			}
		}

		// Token: 0x170004E2 RID: 1250
		// (get) Token: 0x0600125E RID: 4702 RVA: 0x00053E82 File Offset: 0x00052082
		public MBReadOnlyList<MobileParty> PossibleArmyMembersUponArmyCreation
		{
			get
			{
				return this._possibleArmyMembersUponArmyCreation;
			}
		}

		// Token: 0x0600125F RID: 4703 RVA: 0x00053E8A File Offset: 0x0005208A
		public PartyThinkParams(MobileParty mobileParty)
		{
			this._aiBehaviorScores = new MBList<ValueTuple<AIBehaviorData, float>>(32);
			this._possibleArmyMembersUponArmyCreation = null;
			this.MobilePartyOf = mobileParty;
			this.WillGatherAnArmy = false;
			this.DoNotChangeBehavior = false;
			this.CurrentObjectiveValue = 0f;
		}

		// Token: 0x06001260 RID: 4704 RVA: 0x00053EC8 File Offset: 0x000520C8
		public void Reset(MobileParty mobileParty)
		{
			this._aiBehaviorScores.Clear();
			MBList<MobileParty> possibleArmyMembersUponArmyCreation = this._possibleArmyMembersUponArmyCreation;
			if (possibleArmyMembersUponArmyCreation != null)
			{
				possibleArmyMembersUponArmyCreation.Clear();
			}
			this.MobilePartyOf = mobileParty;
			this.WillGatherAnArmy = false;
			this.DoNotChangeBehavior = false;
			this.CurrentObjectiveValue = 0f;
			this.StrengthOfLordsWithoutArmy = 0f;
			this.StrengthOfLordsWithArmy = 0f;
			this.StrengthOfLordsAtSameClanWithoutArmy = 0f;
		}

		// Token: 0x06001261 RID: 4705 RVA: 0x00053F34 File Offset: 0x00052134
		public void Initialization()
		{
			this.StrengthOfLordsWithoutArmy = 0f;
			this.StrengthOfLordsWithArmy = 0f;
			this.StrengthOfLordsAtSameClanWithoutArmy = 0f;
			foreach (Hero hero in this.MobilePartyOf.MapFaction.Heroes)
			{
				if (hero.PartyBelongedTo != null)
				{
					MobileParty partyBelongedTo = hero.PartyBelongedTo;
					if (partyBelongedTo.Army != null)
					{
						this.StrengthOfLordsWithArmy += partyBelongedTo.Party.EstimatedStrength;
					}
					else
					{
						this.StrengthOfLordsWithoutArmy += partyBelongedTo.Party.EstimatedStrength;
						Clan clan = hero.Clan;
						Hero leaderHero = this.MobilePartyOf.LeaderHero;
						if (clan == ((leaderHero != null) ? leaderHero.Clan : null))
						{
							this.StrengthOfLordsAtSameClanWithoutArmy += partyBelongedTo.Party.EstimatedStrength;
						}
					}
				}
			}
		}

		// Token: 0x06001262 RID: 4706 RVA: 0x00054034 File Offset: 0x00052234
		public void AddPotentialArmyMember(MobileParty armyMember)
		{
			if (this._possibleArmyMembersUponArmyCreation == null)
			{
				this._possibleArmyMembersUponArmyCreation = new MBList<MobileParty>(16);
			}
			this._possibleArmyMembersUponArmyCreation.Add(armyMember);
		}

		// Token: 0x06001263 RID: 4707 RVA: 0x00054058 File Offset: 0x00052258
		public bool TryGetBehaviorScore(in AIBehaviorData aiBehaviorData, out float score)
		{
			foreach (ValueTuple<AIBehaviorData, float> valueTuple in this._aiBehaviorScores)
			{
				AIBehaviorData item = valueTuple.Item1;
				if (item.Equals(aiBehaviorData))
				{
					score = valueTuple.Item2;
					return true;
				}
			}
			score = 0f;
			return false;
		}

		// Token: 0x06001264 RID: 4708 RVA: 0x000540D0 File Offset: 0x000522D0
		public void SetBehaviorScore(in AIBehaviorData aiBehaviorData, float score)
		{
			for (int i = 0; i < this._aiBehaviorScores.Count; i++)
			{
				if (this._aiBehaviorScores[i].Item1.Equals(aiBehaviorData))
				{
					this._aiBehaviorScores[i] = new ValueTuple<AIBehaviorData, float>(this._aiBehaviorScores[i].Item1, score);
					return;
				}
			}
			Debug.FailedAssert("AIBehaviorScore not found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\ICampaignBehaviorManager.cs", "SetBehaviorScore", 200);
		}

		// Token: 0x06001265 RID: 4709 RVA: 0x00054151 File Offset: 0x00052351
		public void AddBehaviorScore(in ValueTuple<AIBehaviorData, float> value)
		{
			this._aiBehaviorScores.Add(value);
		}

		// Token: 0x04000611 RID: 1553
		public MobileParty MobilePartyOf;

		// Token: 0x04000612 RID: 1554
		private readonly MBList<ValueTuple<AIBehaviorData, float>> _aiBehaviorScores;

		// Token: 0x04000613 RID: 1555
		private MBList<MobileParty> _possibleArmyMembersUponArmyCreation;

		// Token: 0x04000614 RID: 1556
		public float CurrentObjectiveValue;

		// Token: 0x04000615 RID: 1557
		public bool WillGatherAnArmy;

		// Token: 0x04000616 RID: 1558
		public bool DoNotChangeBehavior;

		// Token: 0x04000617 RID: 1559
		public float StrengthOfLordsWithoutArmy;

		// Token: 0x04000618 RID: 1560
		public float StrengthOfLordsWithArmy;

		// Token: 0x04000619 RID: 1561
		public float StrengthOfLordsAtSameClanWithoutArmy;
	}
}
