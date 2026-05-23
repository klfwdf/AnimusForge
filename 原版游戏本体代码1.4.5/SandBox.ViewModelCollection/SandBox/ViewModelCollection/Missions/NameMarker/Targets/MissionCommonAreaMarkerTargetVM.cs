using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionCommonAreaMarkerTargetVM : MissionNameMarkerTargetVM<CommonAreaMarker>
{
	public readonly Alley TargetAlley;

	public MissionCommonAreaMarkerTargetVM(CommonAreaMarker target)
		: base(target)
	{
		base.NameType = "Passage";
		base.IconType = "common_area";
		TargetAlley = Hero.MainHero.CurrentSettlement.Alleys[target.AreaIndex - 1];
		UpdateAlleyStatus();
		CampaignEvents.AlleyOwnerChanged.AddNonSerializedListener(this, OnAlleyOwnerChanged);
		RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
	{
		if (TargetAlley == alley && (newOwner == Hero.MainHero || oldOwner == Hero.MainHero))
		{
			UpdateAlleyStatus();
		}
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, base.Target.GetPosition() + MissionNameMarkerHelper.DefaultHeightOffset);
	}

	protected override TextObject GetName()
	{
		return base.Target.GetName();
	}

	private void UpdateAlleyStatus()
	{
		if (TargetAlley == null)
		{
			return;
		}
		Hero owner = TargetAlley.Owner;
		if (owner != null)
		{
			if (owner == Hero.MainHero)
			{
				base.NameType = "Friendly";
				base.IsFriendly = true;
				base.IsEnemy = false;
			}
			else
			{
				base.NameType = "Passage";
				base.IsFriendly = false;
				base.IsEnemy = true;
			}
		}
		else
		{
			base.NameType = "Normal";
			base.IsFriendly = false;
			base.IsEnemy = false;
		}
	}
}
