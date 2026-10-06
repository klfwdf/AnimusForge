using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

[DefaultView]
public class MissionSoundParametersView : MissionView
{
	public enum SoundParameterMissionCulture : short
	{
		None,
		Aserai,
		Battania,
		Empire,
		Khuzait,
		Sturgia,
		Vlandia,
		Nord,
		ReservedA,
		ReservedB,
		Bandit
	}

	private enum SoundParameterMissionProsperityLevel : short
	{
		None = 0,
		Low = 0,
		Mid = 1,
		High = 2
	}

	private const string CultureParameterId = "MissionCulture";

	private const string ProsperityParameterId = "MissionProsperity";

	private const string CombatParameterId = "MissionCombatMode";

	public override void EarlyStart()
	{
		base.EarlyStart();
		InitializeGlobalParameters();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		SoundManager.SetGlobalParameter("MissionCulture", 0f);
		SoundManager.SetGlobalParameter("MissionProsperity", 0f);
		SoundManager.SetGlobalParameter("MissionCombatMode", 0f);
	}

	public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
		InitializeCombatModeParameter();
	}

	private void InitializeGlobalParameters()
	{
		InitializeCultureParameter();
		InitializeProsperityParameter();
		InitializeCombatModeParameter();
	}

	private void InitializeCultureParameter()
	{
		SoundParameterMissionCulture soundParameterMissionCulture = SoundParameterMissionCulture.None;
		if (Campaign.Current != null)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null)
			{
				if (currentSettlement.IsHideout)
				{
					soundParameterMissionCulture = SoundParameterMissionCulture.Bandit;
				}
				else
				{
					switch (currentSettlement.Culture.StringId)
					{
					case "empire":
						soundParameterMissionCulture = SoundParameterMissionCulture.Empire;
						break;
					case "sturgia":
						soundParameterMissionCulture = SoundParameterMissionCulture.Sturgia;
						break;
					case "aserai":
						soundParameterMissionCulture = SoundParameterMissionCulture.Aserai;
						break;
					case "vlandia":
						soundParameterMissionCulture = SoundParameterMissionCulture.Vlandia;
						break;
					case "battania":
						soundParameterMissionCulture = SoundParameterMissionCulture.Battania;
						break;
					case "khuzait":
						soundParameterMissionCulture = SoundParameterMissionCulture.Khuzait;
						break;
					case "nord":
						soundParameterMissionCulture = SoundParameterMissionCulture.Nord;
						break;
					}
				}
			}
		}
		SoundManager.SetGlobalParameter("MissionCulture", (float)soundParameterMissionCulture);
	}

	private void InitializeProsperityParameter()
	{
		SoundParameterMissionProsperityLevel soundParameterMissionProsperityLevel = SoundParameterMissionProsperityLevel.None;
		if (Campaign.Current != null && Settlement.CurrentSettlement != null)
		{
			switch (Settlement.CurrentSettlement.SettlementComponent.GetProsperityLevel())
			{
			case SettlementComponent.ProsperityLevel.Low:
				soundParameterMissionProsperityLevel = SoundParameterMissionProsperityLevel.None;
				break;
			case SettlementComponent.ProsperityLevel.Mid:
				soundParameterMissionProsperityLevel = SoundParameterMissionProsperityLevel.Mid;
				break;
			case SettlementComponent.ProsperityLevel.High:
				soundParameterMissionProsperityLevel = SoundParameterMissionProsperityLevel.High;
				break;
			}
		}
		SoundManager.SetGlobalParameter("MissionProsperity", (float)soundParameterMissionProsperityLevel);
	}

	private void InitializeCombatModeParameter()
	{
		bool flag = base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel || base.Mission.Mode == MissionMode.Tournament;
		SoundManager.SetGlobalParameter("MissionCombatMode", flag ? 1 : 0);
	}
}
