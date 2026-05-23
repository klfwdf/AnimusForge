using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001B RID: 27
	public class PartyPlayerNameplateVM : PartyNameplateVM
	{
		// Token: 0x0600029E RID: 670 RVA: 0x0000B641 File Offset: 0x00009841
		public PartyPlayerNameplateVM()
		{
			this.IsMainParty = true;
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000B65C File Offset: 0x0000985C
		public void InitializePlayerNameplate(Action resetCamera)
		{
			this._isPartyHeroVisualDirty = true;
			this._resetCamera = resetCamera;
			bool isPrisonerBind;
			if (this.IsMainParty && base.Party.LeaderHero == null)
			{
				Hero mainHero = Hero.MainHero;
				isPrisonerBind = mainHero != null && mainHero.IsAlive;
			}
			else
			{
				isPrisonerBind = false;
			}
			this._isPrisonerBind = isPrisonerBind;
			this.MainHeroVisual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(Hero.MainHero.CharacterObject, false));
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0000B6C1 File Offset: 0x000098C1
		public override void Clear()
		{
			base.Clear();
			base.IsInSettlement = true;
			base.IsVisibleOnMap = false;
			this.MainHeroVisual = null;
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0000B6E0 File Offset: 0x000098E0
		public override void RefreshDynamicProperties(bool forceUpdate)
		{
			base.RefreshDynamicProperties(forceUpdate);
			if ((this.IsMainParty && MathF.Abs(Hero.MainHero.Age - this._latestMainHeroAge) >= 1f) || forceUpdate)
			{
				this._latestMainHeroAge = Hero.MainHero.Age;
				this._isPartyHeroVisualDirty = true;
			}
			if (this._isPartyHeroVisualDirty || forceUpdate)
			{
				this._mainHeroVisualBind = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(Hero.MainHero.CharacterObject, false));
				this._isPartyHeroVisualDirty = false;
			}
			bool isPrisonerBind;
			if (this.IsMainParty && base.Party.LeaderHero == null)
			{
				Hero mainHero = Hero.MainHero;
				isPrisonerBind = mainHero != null && mainHero.IsAlive;
			}
			else
			{
				isPrisonerBind = false;
			}
			this._isPrisonerBind = isPrisonerBind;
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0000B795 File Offset: 0x00009995
		public override void RefreshBinding()
		{
			base.RefreshBinding();
			this.IsPrisoner = this._isPrisonerBind;
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000B7AC File Offset: 0x000099AC
		public override void RefreshPosition()
		{
			Vec3 vec = (base.Party.Position + base.Party.EventPositionAdder).AsVec3();
			Vec3 worldSpacePosition = vec + new Vec3(0f, 0f, 0.8f, -1f);
			this._latestX = 0f;
			this._latestY = 0f;
			this._latestW = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, vec, ref this._latestX, ref this._latestY, ref this._latestW);
			this._partyPositionBind = new Vec2(this._latestX, this._latestY);
			this._isHighBind = this._mapCamera.Position.Distance(vec) >= 110f;
			this._isBehindBind = this._latestW < 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, worldSpacePosition, ref this._latestX, ref this._latestY, ref this._latestW);
			this._headPositionBind = new Vec2(this._latestX, this._latestY);
			base.DistanceToCamera = vec.Distance(this._mapCamera.Position);
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x0000B8D9 File Offset: 0x00009AD9
		public void ExecuteSetCameraPosition()
		{
			this._resetCamera();
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x0000B8E6 File Offset: 0x00009AE6
		// (set) Token: 0x060002A6 RID: 678 RVA: 0x0000B8EE File Offset: 0x00009AEE
		[DataSourceProperty]
		public bool IsMainParty
		{
			get
			{
				return this._isMainParty;
			}
			set
			{
				if (value != this._isMainParty)
				{
					this._isMainParty = value;
					base.OnPropertyChangedWithValue(value, "IsMainParty");
				}
			}
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x060002A7 RID: 679 RVA: 0x0000B90C File Offset: 0x00009B0C
		// (set) Token: 0x060002A8 RID: 680 RVA: 0x0000B914 File Offset: 0x00009B14
		[DataSourceProperty]
		public bool IsPrisoner
		{
			get
			{
				return this._isPrisoner;
			}
			set
			{
				if (value != this._isPrisoner)
				{
					this._isPrisoner = value;
					base.OnPropertyChangedWithValue(value, "IsPrisoner");
				}
			}
		}

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x060002A9 RID: 681 RVA: 0x0000B932 File Offset: 0x00009B32
		// (set) Token: 0x060002AA RID: 682 RVA: 0x0000B93A File Offset: 0x00009B3A
		[DataSourceProperty]
		public CharacterImageIdentifierVM MainHeroVisual
		{
			get
			{
				return this._mainHeroVisual;
			}
			set
			{
				if (value != this._mainHeroVisual)
				{
					this._mainHeroVisual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "MainHeroVisual");
				}
			}
		}

		// Token: 0x0400014C RID: 332
		private float _latestMainHeroAge = -1f;

		// Token: 0x0400014D RID: 333
		private bool _isPartyHeroVisualDirty;

		// Token: 0x0400014E RID: 334
		private Action _resetCamera;

		// Token: 0x0400014F RID: 335
		private CharacterImageIdentifierVM _mainHeroVisualBind;

		// Token: 0x04000150 RID: 336
		private bool _isPrisonerBind;

		// Token: 0x04000151 RID: 337
		private bool _isMainParty;

		// Token: 0x04000152 RID: 338
		private bool _isPrisoner;

		// Token: 0x04000153 RID: 339
		private CharacterImageIdentifierVM _mainHeroVisual;
	}
}
