using System;
using psai.net;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View
{
	// Token: 0x02000004 RID: 4
	public class CampaignMusicHandler : IMusicHandler
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		bool IMusicHandler.IsPausable
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000205B File Offset: 0x0000025B
		private CampaignMusicHandler()
		{
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002064 File Offset: 0x00000264
		public static void Create()
		{
			CampaignMusicHandler campaignMusicHandler = new CampaignMusicHandler();
			MBMusicManager.Current.OnCampaignMusicHandlerInit(campaignMusicHandler);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002082 File Offset: 0x00000282
		void IMusicHandler.OnUpdated(float dt)
		{
			this.CheckMusicMode();
			this.TickCampaignMusic(dt);
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002091 File Offset: 0x00000291
		private void CheckMusicMode()
		{
			if (MBMusicManager.Current.CurrentMode == MusicMode.Paused)
			{
				MBMusicManager.Current.ActivateCampaignMode();
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020AC File Offset: 0x000002AC
		private void TickCampaignMusic(float dt)
		{
			bool flag = PsaiCore.Instance.GetPsaiInfo().psaiState == PsaiState.playing;
			if (this._restTimer <= 0f)
			{
				this._restTimer += dt;
				if (this._restTimer > 0f)
				{
					MBMusicManager.Current.StartThemeWithConstantIntensity(MBMusicManager.Current.GetCampaignMusicTheme(this.GetNearbyCulture(), this.GetMoodOfMainParty() < MusicParameters.CampaignDarkModeThreshold, this.IsPlayerInAnArmy(), this.GetIsMainPartyAtSea()), false);
					Debug.Print("Campaign music play started.", 0, Debug.DebugColor.Yellow, 64UL);
					return;
				}
			}
			else if (!flag)
			{
				MBMusicManager.Current.ForceStopThemeWithFadeOut();
				this._restTimer = -(30f + MBRandom.RandomFloat * 90f);
				Debug.Print("Campaign music rest started.", 0, Debug.DebugColor.Yellow, 64UL);
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002174 File Offset: 0x00000374
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

		// Token: 0x0600000A RID: 10 RVA: 0x0000221C File Offset: 0x0000041C
		private bool IsPlayerInAnArmy()
		{
			return MobileParty.MainParty.Army != null;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000222B File Offset: 0x0000042B
		private float GetMoodOfMainParty()
		{
			return MathF.Clamp(MobileParty.MainParty.Morale / 100f, 0f, 1f);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000224C File Offset: 0x0000044C
		private bool GetIsMainPartyAtSea()
		{
			return MobileParty.MainParty.IsCurrentlyAtSea;
		}

		// Token: 0x04000001 RID: 1
		private const float MinRestDurationInSeconds = 30f;

		// Token: 0x04000002 RID: 2
		private const float MaxRestDurationInSeconds = 120f;

		// Token: 0x04000003 RID: 3
		private float _restTimer;
	}
}
