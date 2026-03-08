using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x02000034 RID: 52
	public class MissionNameMarkerVM : ViewModel
	{
		// Token: 0x17000137 RID: 311
		// (get) Token: 0x060003EB RID: 1003 RVA: 0x0001028D File Offset: 0x0000E48D
		// (set) Token: 0x060003EC RID: 1004 RVA: 0x00010295 File Offset: 0x0000E495
		public bool IsTargetsAdded { get; private set; }

		// Token: 0x060003ED RID: 1005 RVA: 0x0001029E File Offset: 0x0000E49E
		public MissionNameMarkerVM(List<MissionNameMarkerProvider> providers, Camera missionCamera)
		{
			this.Targets = new MBBindingList<MissionNameMarkerTargetBaseVM>();
			this._providers = providers;
			this._distanceComparer = new MissionNameMarkerVM.MarkerDistanceComparer();
			this._missionCamera = missionCamera;
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x000102CA File Offset: 0x0000E4CA
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Targets.ApplyActionOnAllItems(delegate(MissionNameMarkerTargetBaseVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x000102FC File Offset: 0x0000E4FC
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Targets.ApplyActionOnAllItems(delegate(MissionNameMarkerTargetBaseVM x)
			{
				x.OnFinalize();
			});
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x00010330 File Offset: 0x0000E530
		public void Tick(float dt)
		{
			if (!this.IsTargetsAdded)
			{
				List<MissionNameMarkerTargetBaseVM> list = new List<MissionNameMarkerTargetBaseVM>();
				for (int i = 0; i < this._providers.Count; i++)
				{
					this._providers[i].CreateMarkers(list);
				}
				MBReadOnlyList<MissionNameMarkerTargetBaseVM> mbreadOnlyList;
				MBReadOnlyList<MissionNameMarkerTargetBaseVM> mbreadOnlyList2;
				MissionNameMarkerVM.GetTargetDifferences(this.Targets, list, out mbreadOnlyList, out mbreadOnlyList2);
				for (int j = 0; j < mbreadOnlyList.Count; j++)
				{
					this.Targets.Remove(mbreadOnlyList[j]);
				}
				for (int k = 0; k < mbreadOnlyList2.Count; k++)
				{
					this.Targets.Add(mbreadOnlyList2[k]);
				}
				this.IsTargetsAdded = true;
			}
			if (this.IsEnabled)
			{
				this.UpdateTargetScreenPositions(false);
				this._fadeOutTimerStarted = false;
				this._fadeOutTimer = 0f;
				this._prevEnabledState = this.IsEnabled;
			}
			else
			{
				if (this._prevEnabledState)
				{
					this._fadeOutTimerStarted = true;
				}
				if (this._fadeOutTimerStarted)
				{
					this._fadeOutTimer += dt;
				}
				if (this._fadeOutTimer >= 2f)
				{
					this._fadeOutTimerStarted = false;
				}
				this.UpdateTargetScreenPositions(this._fadeOutTimer < 2f);
			}
			this._prevEnabledState = this.IsEnabled;
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x00010468 File Offset: 0x0000E668
		private static void GetTargetDifferences(IList<MissionNameMarkerTargetBaseVM> currentTargets, IList<MissionNameMarkerTargetBaseVM> newTargets, out MBReadOnlyList<MissionNameMarkerTargetBaseVM> removedTargets, out MBReadOnlyList<MissionNameMarkerTargetBaseVM> addedTargets)
		{
			MBList<MissionNameMarkerTargetBaseVM> mblist = new MBList<MissionNameMarkerTargetBaseVM>();
			MBList<MissionNameMarkerTargetBaseVM> mblist2 = new MBList<MissionNameMarkerTargetBaseVM>();
			for (int i = 0; i < currentTargets.Count; i++)
			{
				MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM = currentTargets[i];
				bool flag = true;
				for (int j = 0; j < newTargets.Count; j++)
				{
					MissionNameMarkerTargetBaseVM other = newTargets[j];
					if (missionNameMarkerTargetBaseVM.Equals(other))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					mblist.Add(missionNameMarkerTargetBaseVM);
				}
			}
			for (int k = 0; k < newTargets.Count; k++)
			{
				MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM2 = newTargets[k];
				bool flag2 = true;
				for (int l = 0; l < currentTargets.Count; l++)
				{
					if (currentTargets[l].Equals(missionNameMarkerTargetBaseVM2))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					mblist2.Add(missionNameMarkerTargetBaseVM2);
				}
			}
			removedTargets = mblist;
			addedTargets = mblist2;
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x00010535 File Offset: 0x0000E735
		public void SetTargetsDirty()
		{
			this.IsTargetsAdded = false;
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x00010540 File Offset: 0x0000E740
		private void UpdateTargetScreenPositions(bool forceUpdate)
		{
			for (int i = 0; i < this.Targets.Count; i++)
			{
				MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM = this.Targets[i];
				if (missionNameMarkerTargetBaseVM.IsEnabled || forceUpdate)
				{
					missionNameMarkerTargetBaseVM.UpdatePosition(this._missionCamera);
				}
			}
			this.Targets.Sort(this._distanceComparer);
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x00010598 File Offset: 0x0000E798
		private void UpdateTargetStates(bool state)
		{
			foreach (MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM in this.Targets)
			{
				missionNameMarkerTargetBaseVM.SetEnabledState(state);
			}
		}

		// Token: 0x17000138 RID: 312
		// (get) Token: 0x060003F5 RID: 1013 RVA: 0x000105E4 File Offset: 0x0000E7E4
		// (set) Token: 0x060003F6 RID: 1014 RVA: 0x000105EC File Offset: 0x0000E7EC
		[DataSourceProperty]
		public MBBindingList<MissionNameMarkerTargetBaseVM> Targets
		{
			get
			{
				return this._targets;
			}
			set
			{
				if (value != this._targets)
				{
					this._targets = value;
					base.OnPropertyChangedWithValue<MBBindingList<MissionNameMarkerTargetBaseVM>>(value, "Targets");
				}
			}
		}

		// Token: 0x17000139 RID: 313
		// (get) Token: 0x060003F7 RID: 1015 RVA: 0x0001060A File Offset: 0x0000E80A
		// (set) Token: 0x060003F8 RID: 1016 RVA: 0x00010612 File Offset: 0x0000E812
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
					this.UpdateTargetStates(value);
					Game.Current.EventManager.TriggerEvent<MissionNameMarkerToggleEvent>(new MissionNameMarkerToggleEvent(value));
				}
			}
		}

		// Token: 0x04000209 RID: 521
		private readonly Camera _missionCamera;

		// Token: 0x0400020A RID: 522
		private bool _prevEnabledState;

		// Token: 0x0400020B RID: 523
		private bool _fadeOutTimerStarted;

		// Token: 0x0400020C RID: 524
		private float _fadeOutTimer;

		// Token: 0x0400020D RID: 525
		private readonly MissionNameMarkerVM.MarkerDistanceComparer _distanceComparer;

		// Token: 0x0400020E RID: 526
		private readonly List<MissionNameMarkerProvider> _providers;

		// Token: 0x0400020F RID: 527
		private MBBindingList<MissionNameMarkerTargetBaseVM> _targets;

		// Token: 0x04000210 RID: 528
		private bool _isEnabled;

		// Token: 0x0200009D RID: 157
		private class MarkerDistanceComparer : IComparer<MissionNameMarkerTargetBaseVM>
		{
			// Token: 0x0600069A RID: 1690 RVA: 0x00016BF4 File Offset: 0x00014DF4
			public int Compare(MissionNameMarkerTargetBaseVM x, MissionNameMarkerTargetBaseVM y)
			{
				return y.Distance.CompareTo(x.Distance);
			}
		}
	}
}
