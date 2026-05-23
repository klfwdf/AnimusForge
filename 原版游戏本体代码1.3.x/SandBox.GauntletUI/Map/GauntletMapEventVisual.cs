using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000038 RID: 56
	public class GauntletMapEventVisual : IMapEventVisual
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000299 RID: 665 RVA: 0x0000F989 File Offset: 0x0000DB89
		// (set) Token: 0x0600029A RID: 666 RVA: 0x0000F991 File Offset: 0x0000DB91
		public MapEvent MapEvent { get; private set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600029B RID: 667 RVA: 0x0000F99A File Offset: 0x0000DB9A
		// (set) Token: 0x0600029C RID: 668 RVA: 0x0000F9A2 File Offset: 0x0000DBA2
		public Vec2 WorldPosition { get; private set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600029D RID: 669 RVA: 0x0000F9AB File Offset: 0x0000DBAB
		// (set) Token: 0x0600029E RID: 670 RVA: 0x0000F9B3 File Offset: 0x0000DBB3
		public bool IsVisible { get; private set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600029F RID: 671 RVA: 0x0000F9BC File Offset: 0x0000DBBC
		private Scene MapScene
		{
			get
			{
				if (this._mapScene == null)
				{
					Campaign campaign = Campaign.Current;
					if (((campaign != null) ? campaign.MapSceneWrapper : null) != null)
					{
						this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
					}
				}
				return this._mapScene;
			}
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0000FA0A File Offset: 0x0000DC0A
		public GauntletMapEventVisual(MapEvent mapEvent, Action<GauntletMapEventVisual> onInitialized, Action<GauntletMapEventVisual> onVisibilityChanged, Action<GauntletMapEventVisual> onDeactivate)
		{
			this._onDeactivate = onDeactivate;
			this._onInitialized = onInitialized;
			this._onVisibilityChanged = onVisibilityChanged;
			this.MapEvent = mapEvent;
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0000FA30 File Offset: 0x0000DC30
		public void Initialize(CampaignVec2 position, int battleSizeValue, bool isVisible)
		{
			this.WorldPosition = position.ToVec2();
			this.IsVisible = isVisible;
			Action<GauntletMapEventVisual> onInitialized = this._onInitialized;
			if (onInitialized != null)
			{
				onInitialized(this);
			}
			int num = -1;
			int num2 = 4;
			if (this.MapEvent.IsNavalMapEvent || this.MapEvent.IsBlockade || this.MapEvent.IsBlockadeSallyOut)
			{
				num = GauntletMapEventVisual._navalBattleSoundEventIndex;
			}
			else if (this.MapEvent.IsFieldBattle || this.MapEvent.IsSallyOut)
			{
				num = GauntletMapEventVisual._battleSoundEventIndex;
				num2 = battleSizeValue;
			}
			else if (this.MapEvent.IsSiegeAssault || this.MapEvent.IsSiegeOutside || this.MapEvent.IsSiegeAmbush)
			{
				num = GauntletMapEventVisual._siegeSoundEventIndex;
			}
			else if (this.MapEvent.IsRaid)
			{
				num = GauntletMapEventVisual._raidSoundEventIndex;
			}
			else if (this.MapEvent.IsHideoutBattle)
			{
				num = GauntletMapEventVisual._hideoutBattleSoundEventIndex;
			}
			if (num != -1)
			{
				float num3 = 0f;
				Settlement mapEventSettlement = this.MapEvent.MapEventSettlement;
				CampaignVec2 campaignVec = ((mapEventSettlement != null) ? mapEventSettlement.Position : this.MapEvent.Position);
				Campaign.Current.MapSceneWrapper.GetHeightAtPoint(campaignVec, ref num3);
				this._mapEventSoundEvent = SoundEvent.CreateEvent(num, this.MapScene);
				this._mapEventSoundEvent.SetParameter("battle_size", (float)num2);
				this._mapEventSoundEvent.PlayInPosition(new Vec3(position.X, position.Y, num3 + 2f, -1f));
				if (!isVisible)
				{
					this._mapEventSoundEvent.Pause();
				}
			}
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0000FBB1 File Offset: 0x0000DDB1
		public void OnMapEventEnd()
		{
			Action<GauntletMapEventVisual> onDeactivate = this._onDeactivate;
			if (onDeactivate != null)
			{
				onDeactivate(this);
			}
			if (this._mapEventSoundEvent != null)
			{
				this._mapEventSoundEvent.Stop();
				this._mapEventSoundEvent = null;
			}
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000FBE0 File Offset: 0x0000DDE0
		public void SetVisibility(bool isVisible)
		{
			this.IsVisible = isVisible;
			Action<GauntletMapEventVisual> onVisibilityChanged = this._onVisibilityChanged;
			if (onVisibilityChanged != null)
			{
				onVisibilityChanged(this);
			}
			SoundEvent mapEventSoundEvent = this._mapEventSoundEvent;
			if (mapEventSoundEvent != null && mapEventSoundEvent.IsValid)
			{
				if (isVisible && this._mapEventSoundEvent.IsPaused())
				{
					this._mapEventSoundEvent.Resume();
					return;
				}
				if (!isVisible && !this._mapEventSoundEvent.IsPaused())
				{
					this._mapEventSoundEvent.Pause();
				}
			}
		}

		// Token: 0x040000EF RID: 239
		private static int _battleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle");

		// Token: 0x040000F0 RID: 240
		private static int _navalBattleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/naval_battle_loop");

		// Token: 0x040000F1 RID: 241
		private static int _raidSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_raid");

		// Token: 0x040000F2 RID: 242
		private static int _siegeSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_siege");

		// Token: 0x040000F3 RID: 243
		private static int _hideoutBattleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_hideout");

		// Token: 0x040000F4 RID: 244
		private SoundEvent _mapEventSoundEvent;

		// Token: 0x040000F8 RID: 248
		private readonly Action<GauntletMapEventVisual> _onDeactivate;

		// Token: 0x040000F9 RID: 249
		private readonly Action<GauntletMapEventVisual> _onInitialized;

		// Token: 0x040000FA RID: 250
		private readonly Action<GauntletMapEventVisual> _onVisibilityChanged;

		// Token: 0x040000FB RID: 251
		private Scene _mapScene;
	}
}
