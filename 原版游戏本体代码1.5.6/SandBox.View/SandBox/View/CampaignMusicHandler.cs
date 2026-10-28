using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using psai.net;

namespace SandBox.View;

public class CampaignMusicHandler : IMusicHandler
{
	private const float MinRestDurationInSeconds = 30f;

	private const float MaxRestDurationInSeconds = 120f;

	private float _restTimer;

	bool IMusicHandler.IsPausable => false;

	private CampaignMusicHandler()
	{
	}

	public static void Create()
	{
		CampaignMusicHandler campaignMusicHandler = new CampaignMusicHandler();
		MBMusicManager.Current.OnCampaignMusicHandlerInit(campaignMusicHandler);
	}

	void IMusicHandler.OnUpdated(float dt)
	{
		CheckMusicMode();
		TickCampaignMusic(dt);
	}

	private void CheckMusicMode()
	{
		if (MBMusicManager.Current.CurrentMode == MusicMode.Paused)
		{
			MBMusicManager.Current.ActivateCampaignMode();
		}
	}

	private void TickCampaignMusic(float dt)
	{
		bool flag = PsaiCore.Instance.GetPsaiInfo().psaiState == PsaiState.playing;
		if (_restTimer <= 0f)
		{
			_restTimer += dt;
			if (_restTimer > 0f)
			{
				MBMusicManager.Current.StartThemeWithConstantIntensity(MBMusicManager.Current.GetCampaignMusicTheme(GetNearbyCulture(), GetMoodOfMainParty() < MusicParameters.CampaignDarkModeThreshold, IsPlayerInAnArmy(), GetIsMainPartyAtSea()));
				Debug.Print("Campaign music play started.", 0, Debug.DebugColor.Yellow, 64uL);
			}
		}
		else if (!flag)
		{
			MBMusicManager.Current.ForceStopThemeWithFadeOut();
			_restTimer = 0f - (30f + MBRandom.RandomFloat * 90f);
			Debug.Print("Campaign music rest started.", 0, Debug.DebugColor.Yellow, 64uL);
		}
	}

	private CultureObject GetNearbyCulture()
	{
		CultureObject result = null;
		float num = float.MaxValue;
		foreach (Settlement settlement in Campaign.Current.Settlements)
		{
			if (settlement.IsTown || settlement.IsVillage)
			{
				float num2 = settlement.Position.DistanceSquared(MobileParty.MainParty.Position);
				if (settlement.IsVillage)
				{
					num2 *= 1.05f;
				}
				if (num > num2)
				{
					result = settlement.Culture;
					num = num2;
				}
			}
		}
		return result;
	}

	private bool IsPlayerInAnArmy()
	{
		return MobileParty.MainParty.Army != null;
	}

	private float GetMoodOfMainParty()
	{
		return MathF.Clamp(MobileParty.MainParty.Morale / 100f, 0f, 1f);
	}

	private bool GetIsMainPartyAtSea()
	{
		return MobileParty.MainParty.IsCurrentlyAtSea;
	}
}
