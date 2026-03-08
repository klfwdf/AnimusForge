using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Election
{
	// Token: 0x020002CB RID: 715
	public class KingdomElection
	{
		// Token: 0x1700096F RID: 2415
		// (get) Token: 0x06002686 RID: 9862 RVA: 0x000A0272 File Offset: 0x0009E472
		public MBReadOnlyList<DecisionOutcome> PossibleOutcomes
		{
			get
			{
				return this._possibleOutcomes;
			}
		}

		// Token: 0x17000970 RID: 2416
		// (get) Token: 0x06002687 RID: 9863 RVA: 0x000A027A File Offset: 0x0009E47A
		// (set) Token: 0x06002688 RID: 9864 RVA: 0x000A0282 File Offset: 0x0009E482
		[SaveableProperty(7)]
		public bool IsCancelled { get; private set; }

		// Token: 0x17000971 RID: 2417
		// (get) Token: 0x06002689 RID: 9865 RVA: 0x000A028B File Offset: 0x0009E48B
		public bool IsPlayerSupporter
		{
			get
			{
				return this.PlayerAsSupporter != null;
			}
		}

		// Token: 0x17000972 RID: 2418
		// (get) Token: 0x0600268A RID: 9866 RVA: 0x000A0296 File Offset: 0x0009E496
		private Supporter PlayerAsSupporter
		{
			get
			{
				return this._supporters.FirstOrDefault((Supporter x) => x.IsPlayer);
			}
		}

		// Token: 0x17000973 RID: 2419
		// (get) Token: 0x0600268B RID: 9867 RVA: 0x000A02C2 File Offset: 0x0009E4C2
		public bool IsPlayerChooser
		{
			get
			{
				return this._chooser.Leader.IsHumanPlayerCharacter;
			}
		}

		// Token: 0x0600268C RID: 9868 RVA: 0x000A02D4 File Offset: 0x0009E4D4
		public KingdomElection(KingdomDecision decision)
		{
			this._decision = decision;
			this.Setup();
		}

		// Token: 0x0600268D RID: 9869 RVA: 0x000A02EC File Offset: 0x0009E4EC
		private void Setup()
		{
			MBList<DecisionOutcome> initialCandidates = this._decision.DetermineInitialCandidates().ToMBList<DecisionOutcome>();
			this._possibleOutcomes = this._decision.NarrowDownCandidates(initialCandidates, 3);
			this._supporters = this._decision.DetermineSupporters().ToList<Supporter>();
			this._chooser = this._decision.DetermineChooser();
			this._decision.DetermineSponsors(this._possibleOutcomes);
			this._hasPlayerVoted = false;
			this.IsCancelled = false;
			foreach (DecisionOutcome decisionOutcome in this._possibleOutcomes)
			{
				decisionOutcome.InitialSupport = this.DetermineInitialSupport(decisionOutcome);
			}
			float num = this._possibleOutcomes.Sum((DecisionOutcome x) => x.InitialSupport);
			foreach (DecisionOutcome decisionOutcome2 in this._possibleOutcomes)
			{
				decisionOutcome2.Likelihood = ((num == 0f) ? 0f : (decisionOutcome2.InitialSupport / num));
			}
		}

		// Token: 0x0600268E RID: 9870 RVA: 0x000A0438 File Offset: 0x0009E638
		public void StartElection()
		{
			this.Setup();
			this.DetermineSupport(this._possibleOutcomes, false);
			this._decision.DetermineSponsors(this._possibleOutcomes);
			this.UpdateSupport(this._possibleOutcomes);
			if (this._decision.ShouldBeCancelled())
			{
				Debug.Print("SELIM_DEBUG - " + this._decision.GetSupportTitle() + " has been cancelled", 0, Debug.DebugColor.White, 17592186044416UL);
				this.IsCancelled = true;
				return;
			}
			if (!this.IsPlayerSupporter || this._ignorePlayerSupport)
			{
				this.ReadyToAiChoose();
				return;
			}
			if (this._decision.IsSingleClanDecision())
			{
				this._chosenOutcome = this._possibleOutcomes.FirstOrDefault((DecisionOutcome t) => t.SponsorClan != null && t.SponsorClan == Clan.PlayerClan);
				Supporter supporter = new Supporter(Clan.PlayerClan);
				supporter.SupportWeight = Supporter.SupportWeights.FullyPush;
				this._chosenOutcome.AddSupport(supporter);
			}
		}

		// Token: 0x0600268F RID: 9871 RVA: 0x000A0528 File Offset: 0x0009E728
		private float DetermineInitialSupport(DecisionOutcome possibleOutcome)
		{
			float num = 0f;
			foreach (Supporter supporter in this._supporters)
			{
				if (!supporter.IsPlayer)
				{
					num += MathF.Clamp(this._decision.DetermineSupport(supporter.Clan, possibleOutcome), 0f, 100f);
				}
			}
			return num;
		}

		// Token: 0x06002690 RID: 9872 RVA: 0x000A05A8 File Offset: 0x0009E7A8
		public void StartElectionWithoutPlayer()
		{
			this._ignorePlayerSupport = true;
			this.StartElection();
		}

		// Token: 0x06002691 RID: 9873 RVA: 0x000A05B8 File Offset: 0x0009E7B8
		public float GetLikelihoodForSponsor(Clan sponsor)
		{
			foreach (DecisionOutcome decisionOutcome in this._possibleOutcomes)
			{
				if (decisionOutcome.SponsorClan == sponsor)
				{
					return decisionOutcome.Likelihood;
				}
			}
			Debug.FailedAssert("This clan is not a sponsor of any of the outcomes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Election\\KingdomDecisionMaker.cs", "GetLikelihoodForSponsor", 139);
			return -1f;
		}

		// Token: 0x06002692 RID: 9874 RVA: 0x000A0638 File Offset: 0x0009E838
		private void DetermineSupport(MBReadOnlyList<DecisionOutcome> possibleOutcomes, bool calculateRelationshipEffect)
		{
			foreach (Supporter supporter in this._supporters)
			{
				if (!supporter.IsPlayer)
				{
					Supporter.SupportWeights supportWeight = Supporter.SupportWeights.StayNeutral;
					DecisionOutcome decisionOutcome = this._decision.DetermineSupportOption(supporter, possibleOutcomes, out supportWeight, calculateRelationshipEffect);
					if (decisionOutcome != null)
					{
						supporter.SupportWeight = supportWeight;
						decisionOutcome.AddSupport(supporter);
					}
				}
			}
		}

		// Token: 0x06002693 RID: 9875 RVA: 0x000A06B0 File Offset: 0x0009E8B0
		private void UpdateSupport(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
		{
			foreach (DecisionOutcome decisionOutcome in this._possibleOutcomes)
			{
				foreach (Supporter supporter in new List<Supporter>(decisionOutcome.SupporterList))
				{
					decisionOutcome.ResetSupport(supporter);
				}
			}
			this.DetermineSupport(possibleOutcomes, true);
		}

		// Token: 0x06002694 RID: 9876 RVA: 0x000A074C File Offset: 0x0009E94C
		private void ReadyToAiChoose()
		{
			this._chosenOutcome = this.GetAiChoice(this._possibleOutcomes);
			if (this._decision.OnShowDecision())
			{
				this.ApplyChosenOutcome();
			}
		}

		// Token: 0x06002695 RID: 9877 RVA: 0x000A0774 File Offset: 0x0009E974
		private void ApplyChosenOutcome()
		{
			this._decision.ApplyChosenOutcome(this._chosenOutcome);
			this._decision.SupportStatusOfFinalDecision = this.GetSupportStatusOfDecisionOutcome(this._chosenOutcome);
			this.HandleInfluenceCosts();
			this.ApplySecondaryEffects(this._possibleOutcomes, this._chosenOutcome);
			for (int i = 0; i < this._possibleOutcomes.Count; i++)
			{
				if (this._possibleOutcomes[i].SponsorClan != null)
				{
					foreach (Supporter supporter in this._possibleOutcomes[i].SupporterList)
					{
						if (supporter.Clan.Leader != this._possibleOutcomes[i].SponsorClan.Leader && supporter.Clan == Clan.PlayerClan)
						{
							int num = this.GetRelationChangeWithSponsor(supporter.Clan.Leader, supporter.SupportWeight, false);
							if (num != 0)
							{
								num *= ((this._possibleOutcomes.Count > 2) ? 2 : 1);
								ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporter.Clan.Leader, this._possibleOutcomes[i].SponsorClan.Leader, num, true);
							}
						}
					}
					for (int j = 0; j < this._possibleOutcomes.Count; j++)
					{
						if (i != j)
						{
							foreach (Supporter supporter2 in this._possibleOutcomes[j].SupporterList)
							{
								if (supporter2.Clan.Leader != this._possibleOutcomes[i].SponsorClan.Leader && supporter2.Clan == Clan.PlayerClan)
								{
									int relationChangeWithSponsor = this.GetRelationChangeWithSponsor(supporter2.Clan.Leader, supporter2.SupportWeight, true);
									if (relationChangeWithSponsor != 0)
									{
										ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporter2.Clan.Leader, this._possibleOutcomes[i].SponsorClan.Leader, relationChangeWithSponsor, true);
									}
								}
							}
						}
					}
				}
			}
			this._decision.Kingdom.RemoveDecision(this._decision);
			this._decision.Kingdom.OnKingdomDecisionConcluded();
			CampaignEventDispatcher.Instance.OnKingdomDecisionConcluded(this._decision, this._chosenOutcome, this.IsPlayerChooser || this._hasPlayerVoted);
		}

		// Token: 0x06002696 RID: 9878 RVA: 0x000A0A0C File Offset: 0x0009EC0C
		public int GetRelationChangeWithSponsor(Hero opposerOrSupporter, Supporter.SupportWeights supportWeight, bool isOpposingSides)
		{
			int num = 0;
			Clan clan = opposerOrSupporter.Clan;
			if (supportWeight == Supporter.SupportWeights.FullyPush)
			{
				num = (int)((float)this._decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.FullyPush) / 20f);
			}
			else if (supportWeight == Supporter.SupportWeights.StronglyFavor)
			{
				num = (int)((float)this._decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.StronglyFavor) / 20f);
			}
			else if (supportWeight == Supporter.SupportWeights.SlightlyFavor)
			{
				num = (int)((float)this._decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.SlightlyFavor) / 20f);
			}
			int num2 = (isOpposingSides ? (num * -1) : (num * 2));
			if (isOpposingSides && opposerOrSupporter.Culture.HasFeat(DefaultCulturalFeats.SturgianDecisionPenaltyFeat))
			{
				num2 += (int)((float)num2 * DefaultCulturalFeats.SturgianDecisionPenaltyFeat.EffectBonus);
			}
			return num2;
		}

		// Token: 0x06002697 RID: 9879 RVA: 0x000A0AA8 File Offset: 0x0009ECA8
		private void HandleInfluenceCosts()
		{
			DecisionOutcome decisionOutcome = this._possibleOutcomes[0];
			foreach (DecisionOutcome decisionOutcome2 in this._possibleOutcomes)
			{
				if (decisionOutcome2.TotalSupportPoints > decisionOutcome.TotalSupportPoints)
				{
					decisionOutcome = decisionOutcome2;
				}
				for (int i = 0; i < decisionOutcome2.SupporterList.Count; i++)
				{
					Clan clan = decisionOutcome2.SupporterList[i].Clan;
					int num = this._decision.GetInfluenceCost(decisionOutcome2, clan, decisionOutcome2.SupporterList[i].SupportWeight);
					if (this._supporters.Count == 1)
					{
						num = 0;
					}
					if (this._chosenOutcome != decisionOutcome2)
					{
						num /= 2;
					}
					if (decisionOutcome2 == this._chosenOutcome || !clan.Leader.GetPerkValue(DefaultPerks.Charm.GoodNatured))
					{
						ChangeClanInfluenceAction.Apply(clan, (float)(-(float)num));
					}
				}
			}
			if (this._chosenOutcome != decisionOutcome)
			{
				int influenceRequiredToOverrideKingdomDecision = Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(decisionOutcome, this._chosenOutcome, this._decision);
				ChangeClanInfluenceAction.Apply(this._chooser, (float)(-(float)influenceRequiredToOverrideKingdomDecision));
			}
		}

		// Token: 0x06002698 RID: 9880 RVA: 0x000A0BEC File Offset: 0x0009EDEC
		private void ApplySecondaryEffects(MBReadOnlyList<DecisionOutcome> possibleOutcomes, DecisionOutcome chosenOutcome)
		{
			this._decision.ApplySecondaryEffects(possibleOutcomes, chosenOutcome);
		}

		// Token: 0x06002699 RID: 9881 RVA: 0x000A0BFB File Offset: 0x0009EDFB
		private int GetInfluenceRequiredToOverrideDecision(DecisionOutcome popularOutcome, DecisionOutcome overridingOutcome)
		{
			return Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(popularOutcome, overridingOutcome, this._decision);
		}

		// Token: 0x0600269A RID: 9882 RVA: 0x000A0C1C File Offset: 0x0009EE1C
		private DecisionOutcome GetAiChoice(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
		{
			this.DetermineOfficialSupport();
			DecisionOutcome decisionOutcome = possibleOutcomes.MaxBy((DecisionOutcome t) => t.TotalSupportPoints);
			DecisionOutcome result = decisionOutcome;
			if (this._decision.IsKingsVoteAllowed)
			{
				DecisionOutcome decisionOutcome2 = possibleOutcomes.MaxBy((DecisionOutcome t) => this._decision.DetermineSupport(this._chooser, t));
				float num = this._decision.DetermineSupport(this._chooser, decisionOutcome2);
				float num2 = this._decision.DetermineSupport(this._chooser, decisionOutcome);
				float num3 = num - num2;
				num3 = MathF.Min(num3, this._chooser.Influence);
				if (num3 > 10f)
				{
					float num4 = 300f + (float)this.GetInfluenceRequiredToOverrideDecision(decisionOutcome, decisionOutcome2);
					if (num3 > num4)
					{
						float num5 = num4 / num3;
						if (MBRandom.RandomFloat > num5)
						{
							result = decisionOutcome2;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600269B RID: 9883 RVA: 0x000A0CEC File Offset: 0x0009EEEC
		public TextObject GetChosenOutcomeText()
		{
			return this._decision.GetChosenOutcomeText(this._chosenOutcome, this._decision.SupportStatusOfFinalDecision, false);
		}

		// Token: 0x0600269C RID: 9884 RVA: 0x000A0D0C File Offset: 0x0009EF0C
		private KingdomDecision.SupportStatus GetSupportStatusOfDecisionOutcome(DecisionOutcome chosenOutcome)
		{
			KingdomDecision.SupportStatus result = KingdomDecision.SupportStatus.Equal;
			float num = chosenOutcome.WinChance * 100f;
			int num2 = 50;
			if (num > (float)(num2 + 5))
			{
				result = KingdomDecision.SupportStatus.Majority;
			}
			else if (num < (float)(num2 - 5))
			{
				result = KingdomDecision.SupportStatus.Minority;
			}
			return result;
		}

		// Token: 0x0600269D RID: 9885 RVA: 0x000A0D40 File Offset: 0x0009EF40
		public void DetermineOfficialSupport()
		{
			new List<Tuple<DecisionOutcome, float>>();
			float num = 0.001f;
			foreach (DecisionOutcome decisionOutcome in this._possibleOutcomes)
			{
				float num2 = 0f;
				foreach (Supporter supporter in decisionOutcome.SupporterList)
				{
					num2 += (float)MathF.Max(0, supporter.SupportWeight - Supporter.SupportWeights.StayNeutral);
				}
				decisionOutcome.TotalSupportPoints = num2;
				num += decisionOutcome.TotalSupportPoints;
			}
			foreach (DecisionOutcome decisionOutcome2 in this._possibleOutcomes)
			{
				decisionOutcome2.TotalSupportPoints /= num;
			}
		}

		// Token: 0x0600269E RID: 9886 RVA: 0x000A0E48 File Offset: 0x0009F048
		public int GetInfluenceCostOfOutcome(DecisionOutcome outcome, Clan supporter, Supporter.SupportWeights weight)
		{
			return this._decision.GetInfluenceCostOfSupport(supporter, weight);
		}

		// Token: 0x0600269F RID: 9887 RVA: 0x000A0E57 File Offset: 0x0009F057
		public TextObject GetSecondaryEffects()
		{
			return this._decision.GetSecondaryEffects();
		}

		// Token: 0x060026A0 RID: 9888 RVA: 0x000A0E64 File Offset: 0x0009F064
		public void OnPlayerSupport(DecisionOutcome decisionOutcome, Supporter.SupportWeights supportWeight)
		{
			if (!this.IsPlayerChooser)
			{
				foreach (DecisionOutcome decisionOutcome2 in this._possibleOutcomes)
				{
					decisionOutcome2.ResetSupport(this.PlayerAsSupporter);
				}
				this._hasPlayerVoted = true;
				if (decisionOutcome != null)
				{
					this.PlayerAsSupporter.SupportWeight = supportWeight;
					decisionOutcome.AddSupport(this.PlayerAsSupporter);
					return;
				}
			}
			else
			{
				this._chosenOutcome = decisionOutcome;
			}
		}

		// Token: 0x060026A1 RID: 9889 RVA: 0x000A0EEC File Offset: 0x0009F0EC
		public void ApplySelection()
		{
			if (!this.IsCancelled)
			{
				if (this._chooser != Clan.PlayerClan)
				{
					this.ReadyToAiChoose();
					return;
				}
				this.ApplyChosenOutcome();
			}
		}

		// Token: 0x060026A2 RID: 9890 RVA: 0x000A0F10 File Offset: 0x0009F110
		public MBList<DecisionOutcome> GetSortedDecisionOutcomes()
		{
			return this._decision.SortDecisionOutcomes(this._possibleOutcomes);
		}

		// Token: 0x060026A3 RID: 9891 RVA: 0x000A0F23 File Offset: 0x0009F123
		public TextObject GetGeneralTitle()
		{
			return this._decision.GetGeneralTitle();
		}

		// Token: 0x060026A4 RID: 9892 RVA: 0x000A0F30 File Offset: 0x0009F130
		public TextObject GetTitle()
		{
			if (this.IsPlayerChooser)
			{
				return this._decision.GetChooseTitle();
			}
			return this._decision.GetSupportTitle();
		}

		// Token: 0x060026A5 RID: 9893 RVA: 0x000A0F51 File Offset: 0x0009F151
		public TextObject GetDescription()
		{
			if (this.IsPlayerChooser)
			{
				return this._decision.GetChooseDescription();
			}
			return this._decision.GetSupportDescription();
		}

		// Token: 0x04000B46 RID: 2886
		[SaveableField(0)]
		private readonly KingdomDecision _decision;

		// Token: 0x04000B47 RID: 2887
		private MBList<DecisionOutcome> _possibleOutcomes;

		// Token: 0x04000B48 RID: 2888
		[SaveableField(2)]
		private List<Supporter> _supporters;

		// Token: 0x04000B49 RID: 2889
		[SaveableField(3)]
		private Clan _chooser;

		// Token: 0x04000B4A RID: 2890
		[SaveableField(4)]
		private DecisionOutcome _chosenOutcome;

		// Token: 0x04000B4B RID: 2891
		[SaveableField(5)]
		private bool _ignorePlayerSupport;

		// Token: 0x04000B4C RID: 2892
		[SaveableField(6)]
		private bool _hasPlayerVoted;
	}
}
