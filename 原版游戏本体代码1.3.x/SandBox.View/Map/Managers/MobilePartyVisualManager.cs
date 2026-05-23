using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000075 RID: 117
	public class MobilePartyVisualManager : EntityVisualManagerBase<PartyBase>
	{
		// Token: 0x170000AB RID: 171
		// (get) Token: 0x06000515 RID: 1301 RVA: 0x00026B4D File Offset: 0x00024D4D
		public override int Priority
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000516 RID: 1302 RVA: 0x00026B51 File Offset: 0x00024D51
		public static MobilePartyVisualManager Current
		{
			get
			{
				return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<MobilePartyVisualManager>();
			}
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x00026B60 File Offset: 0x00024D60
		public override void OnTick(float realDt, float dt)
		{
			this._dirtyPartyVisualCount = -1;
			TWParallel.For(0, this._visualsFlattened.Count, delegate(int startInclusive, int endExclusive)
			{
				for (int k = startInclusive; k < endExclusive; k++)
				{
					this._visualsFlattened[k].Tick(dt, realDt, ref this._dirtyPartyVisualCount, ref this._dirtyPartiesList);
				}
			}, 16);
			for (int i = 0; i < this._dirtyPartyVisualCount + 1; i++)
			{
				this._dirtyPartiesList[i].ValidateIsDirty();
			}
			for (int j = this._fadingPartiesFlatten.Count - 1; j >= 0; j--)
			{
				this._fadingPartiesFlatten[j].TickFadingState(realDt, dt);
			}
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x00026C04 File Offset: 0x00024E04
		public override void ClearVisualMemory()
		{
			foreach (MobilePartyVisual mobilePartyVisual in this._visualsFlattened)
			{
				mobilePartyVisual.ClearVisualMemory();
			}
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x00026C54 File Offset: 0x00024E54
		public override void OnVisualTick(MapScreen screen, float realDt, float dt)
		{
			base.OnVisualTick(screen, realDt, dt);
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x00026C60 File Offset: 0x00024E60
		public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
		{
			for (int i = entityCount - 1; i >= 0; i--)
			{
				UIntPtr uintPtr = intersectedEntityIDs[i];
				MapEntityVisual mapEntityVisual;
				MobilePartyVisual mobilePartyVisual;
				if (uintPtr != UIntPtr.Zero && MapScreen.VisualsOfEntities.TryGetValue(uintPtr, out mapEntityVisual) && (mobilePartyVisual = mapEntityVisual as MobilePartyVisual) != null && mapEntityVisual.IsVisibleOrFadingOut() && (!mobilePartyVisual.MapEntity.IsMobile || mobilePartyVisual.MapEntity.MobileParty.IsMainParty || !mobilePartyVisual.MapEntity.MobileParty.IsInRaftState))
				{
					hoveredVisual = mapEntityVisual.AttachedTo ?? mapEntityVisual;
					if (!mapEntityVisual.IsMainEntity && (mapEntityVisual.AttachedTo == null || !mapEntityVisual.AttachedTo.IsMainEntity))
					{
						selectedVisual = mapEntityVisual.AttachedTo ?? mapEntityVisual;
					}
				}
			}
			return selectedVisual != null;
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x00026D28 File Offset: 0x00024F28
		public override MapEntityVisual<PartyBase> GetVisualOfEntity(PartyBase partyBase)
		{
			MobileParty mobileParty = partyBase.MobileParty;
			if (mobileParty != null && !mobileParty.IsCurrentlyAtSea)
			{
				MobilePartyVisual result;
				this._partiesAndVisuals.TryGetValue(partyBase, out result);
				return result;
			}
			return null;
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x00026D60 File Offset: 0x00024F60
		protected override void OnFinalize()
		{
			foreach (MobilePartyVisual mobilePartyVisual in this._partiesAndVisuals.Values)
			{
				mobilePartyVisual.ReleaseResources();
			}
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x00026DC0 File Offset: 0x00024FC0
		protected override void OnInitialize()
		{
			base.OnInitialize();
			foreach (MobileParty mobileParty in MobileParty.All)
			{
				this.AddNewPartyVisualForParty(mobileParty, true);
			}
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x00026E48 File Offset: 0x00025048
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			this.RemovePartyVisualForParty(mobileParty);
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x00026E51 File Offset: 0x00025051
		private void OnMobilePartyCreated(MobileParty mobileParty)
		{
			this.AddNewPartyVisualForParty(mobileParty, false);
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00026E5B File Offset: 0x0002505B
		public MobilePartyVisual GetPartyVisual(PartyBase partyBase)
		{
			return this._partiesAndVisuals[partyBase];
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00026E69 File Offset: 0x00025069
		internal void RegisterFadingVisual(MobilePartyVisual visual)
		{
			if (!this._fadingPartiesSet.Contains(visual))
			{
				this._fadingPartiesFlatten.Add(visual);
				this._fadingPartiesSet.Add(visual);
			}
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00026E94 File Offset: 0x00025094
		internal void UnRegisterFadingVisual(MobilePartyVisual visual)
		{
			if (this._fadingPartiesSet.Contains(visual))
			{
				int index = this._fadingPartiesFlatten.IndexOf(visual);
				this._fadingPartiesFlatten[index] = this._fadingPartiesFlatten[this._fadingPartiesFlatten.Count - 1];
				this._fadingPartiesFlatten.Remove(this._fadingPartiesFlatten[this._fadingPartiesFlatten.Count - 1]);
				this._fadingPartiesSet.Remove(visual);
			}
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x00026F14 File Offset: 0x00025114
		private void AddNewPartyVisualForParty(MobileParty mobileParty, bool shouldTick = false)
		{
			if (!mobileParty.IsGarrison && !mobileParty.IsMilitia && !this._partiesAndVisuals.ContainsKey(mobileParty.Party))
			{
				MobilePartyVisual mobilePartyVisual = new MobilePartyVisual(mobileParty.Party);
				mobilePartyVisual.OnStartup();
				this._partiesAndVisuals.Add(mobileParty.Party, mobilePartyVisual);
				this._visualsFlattened.Add(mobilePartyVisual);
				if (shouldTick)
				{
					mobilePartyVisual.Tick(0.1f, 0.1f, ref this._dirtyPartyVisualCount, ref this._dirtyPartiesList);
				}
			}
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x00026F94 File Offset: 0x00025194
		private void RemovePartyVisualForParty(MobileParty mobileParty)
		{
			MobilePartyVisual mobilePartyVisual;
			if (this._partiesAndVisuals.TryGetValue(mobileParty.Party, out mobilePartyVisual))
			{
				mobilePartyVisual.OnPartyRemoved();
				this._visualsFlattened.Remove(mobilePartyVisual);
				this._partiesAndVisuals.Remove(mobileParty.Party);
			}
		}

		// Token: 0x0400024E RID: 590
		private readonly Dictionary<PartyBase, MobilePartyVisual> _partiesAndVisuals = new Dictionary<PartyBase, MobilePartyVisual>();

		// Token: 0x0400024F RID: 591
		private readonly List<MobilePartyVisual> _visualsFlattened = new List<MobilePartyVisual>();

		// Token: 0x04000250 RID: 592
		private int _dirtyPartyVisualCount;

		// Token: 0x04000251 RID: 593
		private MobilePartyVisual[] _dirtyPartiesList = new MobilePartyVisual[2500];

		// Token: 0x04000252 RID: 594
		private readonly List<MobilePartyVisual> _fadingPartiesFlatten = new List<MobilePartyVisual>();

		// Token: 0x04000253 RID: 595
		private readonly HashSet<MobilePartyVisual> _fadingPartiesSet = new HashSet<MobilePartyVisual>();
	}
}
