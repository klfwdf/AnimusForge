using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(MissionSingleplayerKillNotificationUIHandler))]
internal class MissionGauntletNavalKillNotificationSingleplayerUIHandler : MissionGauntletKillNotificationSingleplayerUIHandler
{
	private NavalShipsLogic _navalShipsLogic;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		if (_navalShipsLogic != null)
		{
			_navalShipsLogic.ShipRammingEvent += OnShipRamming;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		if (_navalShipsLogic != null)
		{
			_navalShipsLogic.ShipRammingEvent -= OnShipRamming;
		}
	}

	private void OnShipRamming(MissionShip rammingShip, MissionShip rammedShip, float damagePercent, bool isFirstImpact, CapsuleData capsuleData, int ramQuality)
	{
		if (_isPersonalFeedEnabled && _dataSource != null && isFirstImpact && damagePercent > 0f && rammingShip != null && rammedShip != null && rammingShip.IsPlayerShip && rammingShip.CanDealDamage(rammedShip))
		{
			string message;
			switch (ramQuality)
			{
			case 1:
				message = new TextObject("{=P49bHPbv}Ineffective Ram!").ToString();
				break;
			case 2:
				message = new TextObject("{=SdAhadD3}Weak Ram!").ToString();
				break;
			case 3:
				message = new TextObject("{=CbaYmAuR}Average Ram!").ToString();
				break;
			case 4:
				message = new TextObject("{=GaCMFRjH}Good Ram!").ToString();
				break;
			case 5:
				message = new TextObject("{=DKukCkai}Excellent Ram!").ToString();
				break;
			default:
				Debug.FailedAssert("Ram quality is out of bounds!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.GauntletUI\\MissionViews\\MissionGauntletNavalKillNotificationSingleplayerUIHandler.cs", "OnShipRamming", 70);
				message = new TextObject("{=CbaYmAuR}Average Ram!").ToString();
				break;
			}
			_dataSource.OnPersonalMessage(message);
		}
	}
}
