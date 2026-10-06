using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace NavalDLC;

public class FishingPartyComponent : VillagerPartyComponent
{
	[SaveableField(1)]
	private bool _isFishing;

	[SaveableField(2)]
	private bool _isRoaming;

	public bool IsFishing
	{
		get
		{
			return _isFishing;
		}
		set
		{
			if (_isFishing != value)
			{
				_isFishing = value;
				if (_isFishing)
				{
					FishingWaitStartTime = CampaignTime.Now;
				}
				else
				{
					FishingWaitStartTime = CampaignTime.Never;
				}
			}
		}
	}

	public bool IsRoaming
	{
		get
		{
			return _isRoaming;
		}
		set
		{
			if (_isRoaming != value)
			{
				_isRoaming = value;
				if (_isRoaming)
				{
					RoamingStartTime = CampaignTime.Now;
				}
				else
				{
					RoamingStartTime = CampaignTime.Never;
				}
			}
		}
	}

	[SaveableProperty(3)]
	public CampaignTime FishingWaitStartTime { get; private set; }

	[SaveableProperty(4)]
	public CampaignTime RoamingStartTime { get; private set; }

	public override TextObject Name
	{
		get
		{
			if (_cachedName == null)
			{
				_cachedName = new TextObject("{=a9TivyGv}Fishers of {VILLAGE_NAME}");
				_cachedName.SetTextVariable("VILLAGE_NAME", base.Village.Name);
			}
			return _cachedName;
		}
	}

	public static MobileParty CreateFishingParty(string stringId, Village village)
	{
		return MobileParty.CreateParty(stringId, new FishingPartyComponent(village));
	}

	protected FishingPartyComponent(Village village)
		: base(village, null)
	{
	}

	protected override void OnMobilePartySetOnCreation()
	{
		base.MobileParty.Aggressiveness = 0f;
		base.MobileParty.InitializePartyTrade(0);
		PartyTemplateObject fishingPartyTemplate = base.Village.Settlement.Culture.FishingPartyTemplate;
		CampaignVec2 portPosition = base.Village.Settlement.PortPosition;
		base.MobileParty.InitializeMobilePartyAroundPosition(fishingPartyTemplate, portPosition, 1f);
		base.Party.SetVisualAsDirty();
		base.MobileParty.SetLandNavigationAccess(access: false);
	}

	protected override void OnInitialize()
	{
		if (!NavalDLCManager.Instance.FishingParties.TryGetValue(base.Village, out var value))
		{
			value = new List<FishingPartyComponent>();
			NavalDLCManager.Instance.FishingParties.Add(base.Village, value);
		}
		value.Add(this);
	}

	protected override void OnFinalize()
	{
		if (NavalDLCManager.Instance.FishingParties.TryGetValue(base.Village, out var value))
		{
			value.Remove(this);
		}
		else
		{
			Debug.FailedAssert("parties.Contains(fishingParty)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\FishingPartyComponent.cs", "OnFinalize", 136);
		}
	}
}
