using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.BarterSystem;

public class BarterManager
{
	public delegate bool BarterContextInitializer(Barterable barterable, BarterData args, object obj = null);

	public delegate void BarterCloseEventDelegate();

	public delegate void BarterBeginEventDelegate(BarterData args);

	public BarterCloseEventDelegate Closed;

	public BarterBeginEventDelegate BarterBegin;

	[SaveableField(2)]
	private readonly Dictionary<Hero, CampaignTime> _barteredHeroes;

	private float _overpayAmount;

	public static BarterManager Instance => Campaign.Current.BarterManager;

	[SaveableProperty(1)]
	public bool LastBarterIsAccepted { get; internal set; }

	public BarterManager()
	{
		_barteredHeroes = new Dictionary<Hero, CampaignTime>();
	}

	public void BeginPlayerBarter(BarterData args)
	{
		if (BarterBegin != null)
		{
			BarterBegin(args);
		}
		CampaignMission.Current?.SetMissionMode(MissionMode.Barter, atStart: false);
	}

	private void AddBaseBarterables(BarterData args, IEnumerable<Barterable> defaultBarterables)
	{
		if (defaultBarterables == null)
		{
			return;
		}
		bool flag = false;
		foreach (Barterable defaultBarterable in defaultBarterables)
		{
			if (!flag)
			{
				args.AddBarterGroup(new DefaultsBarterGroup());
				flag = true;
			}
			defaultBarterable.SetIsOffered(value: true);
			args.AddBarterable<OtherBarterGroup>(defaultBarterable, isContextDependent: true);
			defaultBarterable.SetIsOffered(value: true);
		}
	}

	public void StartBarterOffer(Hero offerer, Hero other, PartyBase offererParty, PartyBase otherParty, Hero beneficiaryOfOtherHero = null, BarterContextInitializer InitContext = null, int persuasionCostReduction = 0, bool isAIBarter = false, IEnumerable<Barterable> defaultBarterables = null)
	{
		LastBarterIsAccepted = false;
		if (offerer == Hero.MainHero && other != null && InitContext == null)
		{
			if (!CanPlayerBarterWithHero(other))
			{
				Debug.FailedAssert("Barter with the hero is on cooldown.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BarterSystem\\BarterManager.cs", "StartBarterOffer", 83);
				return;
			}
			ClearHeroCooldowns();
		}
		BarterData args = new BarterData(offerer, beneficiaryOfOtherHero ?? other, offererParty, otherParty, InitContext, persuasionCostReduction);
		AddBaseBarterables(args, defaultBarterables);
		CampaignEventDispatcher.Instance.OnBarterablesRequested(args);
		Campaign.Current.ConversationManager.CurrentConversationIsFirst = false;
		if (!isAIBarter)
		{
			Campaign.Current.BarterManager.BeginPlayerBarter(args);
		}
	}

	public void ExecuteAiBarter(IFaction faction1, IFaction faction2, Hero faction1Hero, Hero faction2Hero, Barterable barterable)
	{
		ExecuteAiBarter(faction1, faction2, faction1Hero, faction2Hero, new Barterable[1] { barterable });
	}

	public void ExecuteAiBarter(IFaction faction1, IFaction faction2, Hero faction1Hero, Hero faction2Hero, IEnumerable<Barterable> baseBarterables)
	{
		BarterData barterData = new BarterData(faction1.Leader, faction2.Leader, null, null, null, 0, isAiBarter: true);
		barterData.AddBarterGroup(new DefaultsBarterGroup());
		foreach (Barterable baseBarterable in baseBarterables)
		{
			baseBarterable.SetIsOffered(value: true);
			barterData.AddBarterable<DefaultsBarterGroup>(baseBarterable, isContextDependent: true);
		}
		CampaignEventDispatcher.Instance.OnBarterablesRequested(barterData);
		Campaign.Current.BarterManager.ExecuteAIBarter(barterData, faction1, faction2, faction1Hero, faction2Hero);
	}

	public void ExecuteAIBarter(BarterData barterData, IFaction faction1, IFaction faction2, Hero faction1Hero, Hero faction2Hero)
	{
		MakeBalanced(barterData, faction1, faction2, faction2Hero, 1f);
		MakeBalanced(barterData, faction2, faction1, faction1Hero, 1f);
		float offerValueForFaction = GetOfferValueForFaction(barterData, faction1);
		float offerValueForFaction2 = GetOfferValueForFaction(barterData, faction2);
		if (offerValueForFaction >= 0f && offerValueForFaction2 >= 0f)
		{
			ApplyBarterOffer(barterData.OffererHero, barterData.OtherHero, barterData.GetOfferedBarterables());
		}
	}

	private void MakeBalanced(BarterData args, IFaction faction1, IFaction faction2, Hero faction2Hero, float fulfillRatio)
	{
		foreach (var (barterable, num) in BarterHelper.GetAutoBalanceBarterablesAdd(args, faction1, faction2, faction2Hero, fulfillRatio))
		{
			if (!barterable.IsOffered)
			{
				barterable.SetIsOffered(value: true);
				barterable.CurrentAmount = 0;
			}
			barterable.CurrentAmount += num;
		}
	}

	public void Close()
	{
		if (CampaignMission.Current != null)
		{
			CampaignMission.Current.SetMissionMode(MissionMode.Conversation, atStart: false);
		}
		if (Closed != null)
		{
			Closed();
		}
	}

	public bool IsOfferAcceptable(BarterData args, Hero hero, PartyBase party)
	{
		return GetOfferValue(hero, party, args.OffererParty, args.GetOfferedBarterables()) > -0.01f;
	}

	public float GetOfferValueForFaction(BarterData barterData, IFaction faction)
	{
		int num = 0;
		foreach (Barterable offeredBarterable in barterData.GetOfferedBarterables())
		{
			num += offeredBarterable.GetValueForFaction(faction);
		}
		return num;
	}

	public float GetOfferValue(Hero selfHero, PartyBase selfParty, PartyBase offererParty, IEnumerable<Barterable> offeredBarters)
	{
		float num = 0f;
		IFaction faction;
		if (selfHero?.Clan != null)
		{
			IFaction clan = selfHero.Clan;
			faction = clan;
		}
		else
		{
			faction = selfParty.MapFaction;
		}
		IFaction faction2 = faction;
		foreach (Barterable offeredBarter in offeredBarters)
		{
			num += (float)offeredBarter.GetValueForFaction(faction2);
		}
		_overpayAmount = ((num > 0f) ? num : 0f);
		return num;
	}

	public void ApplyAndFinalizePlayerBarter(Hero offererHero, Hero otherHero, BarterData barterData)
	{
		LastBarterIsAccepted = true;
		ApplyBarterOffer(offererHero, otherHero, barterData.GetOfferedBarterables());
		if (otherHero != null)
		{
			HandleHeroCooldown(otherHero);
		}
	}

	public void CancelAndFinalizePlayerBarter(Hero offererHero, Hero otherHero, BarterData barterData)
	{
		CancelBarter(offererHero, otherHero, barterData.GetOfferedBarterables());
	}

	private void ApplyBarterOffer(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		foreach (Barterable barter in barters)
		{
			barter.Apply();
		}
		CampaignEventDispatcher.Instance.OnBarterAccepted(offererHero, otherHero, barters);
		if (offererHero == Hero.MainHero)
		{
			if (_overpayAmount > 0f && otherHero != null)
			{
				ApplyOverpayBonus(otherHero);
			}
			Close();
			if (Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				Campaign.Current.ConversationManager.ContinueConversation();
			}
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_offer_accepted"));
		}
	}

	private void CancelBarter(Hero offererHero, Hero otherHero, List<Barterable> offeredBarters)
	{
		Close();
		MBInformationManager.AddQuickInformation(GameTexts.FindText("str_offer_rejected"));
		CampaignEventDispatcher.Instance.OnBarterCanceled(offererHero, otherHero, offeredBarters);
		Campaign.Current.ConversationManager.ContinueConversation();
	}

	private void ApplyOverpayBonus(Hero otherHero)
	{
		if (!otherHero.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			int num = Campaign.Current.Models.BarterModel.CalculateOverpayRelationIncreaseCosts(otherHero, _overpayAmount);
			if (num > 0)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, otherHero, num);
			}
		}
	}

	public bool CanPlayerBarterWithHero(Hero hero)
	{
		if (_barteredHeroes.TryGetValue(hero, out var value))
		{
			return value.IsPast;
		}
		return true;
	}

	private void HandleHeroCooldown(Hero hero)
	{
		CampaignTime value = CampaignTime.Now + CampaignTime.Days(Campaign.Current.Models.BarterModel.BarterCooldownWithHeroInDays);
		if (!_barteredHeroes.ContainsKey(hero))
		{
			_barteredHeroes.Add(hero, value);
		}
		else
		{
			_barteredHeroes[hero] = value;
		}
	}

	private void ClearHeroCooldowns()
	{
		foreach (KeyValuePair<Hero, CampaignTime> item in new Dictionary<Hero, CampaignTime>(_barteredHeroes))
		{
			if (item.Value.IsPast)
			{
				_barteredHeroes.Remove(item.Key);
			}
		}
	}

	public bool InitializeMarriageBarterContext(Barterable barterable, BarterData args, object obj)
	{
		Hero hero = null;
		Hero hero2 = null;
		if (obj != null && obj is Tuple<Hero, Hero> tuple)
		{
			hero = tuple.Item1;
			hero2 = tuple.Item2;
		}
		if (barterable is MarriageBarterable marriageBarterable && hero != null && hero2 != null && marriageBarterable.ProposingHero == hero2)
		{
			return marriageBarterable.HeroBeingProposedTo == hero;
		}
		return false;
	}

	public bool InitializeJoinFactionBarterContext(Barterable barterable, BarterData args, object obj)
	{
		if (barterable.GetType() == typeof(JoinKingdomAsClanBarterable))
		{
			return barterable.OriginalOwner == Hero.OneToOneConversationHero;
		}
		return false;
	}

	public bool InitializeMakePeaceBarterContext(Barterable barterable, BarterData args, object obj)
	{
		if (barterable.GetType() == typeof(PeaceBarterable))
		{
			return barterable.OriginalOwner == args.OtherHero;
		}
		return false;
	}

	public bool InitializeSafePassageBarterContext(Barterable barterable, BarterData args, object obj)
	{
		if (barterable.GetType() == typeof(SafePassageBarterable))
		{
			return barterable.OriginalParty == MobileParty.ConversationParty?.Party;
		}
		return false;
	}

	internal static void AutoGeneratedStaticCollectObjectsBarterManager(object o, List<object> collectedObjects)
	{
		((BarterManager)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(_barteredHeroes);
	}

	internal static object AutoGeneratedGetMemberValueLastBarterIsAccepted(object o)
	{
		return ((BarterManager)o).LastBarterIsAccepted;
	}

	internal static object AutoGeneratedGetMemberValue_barteredHeroes(object o)
	{
		return ((BarterManager)o)._barteredHeroes;
	}
}
