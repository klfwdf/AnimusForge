using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.GamepadNavigation
{
	// Token: 0x0200004B RID: 75
	public class GamepadNavigationForcedScopeCollection
	{
		// Token: 0x17000142 RID: 322
		// (get) Token: 0x06000462 RID: 1122 RVA: 0x00011C85 File Offset: 0x0000FE85
		// (set) Token: 0x06000463 RID: 1123 RVA: 0x00011C8D File Offset: 0x0000FE8D
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
					Action<GamepadNavigationForcedScopeCollection> onAvailabilityChanged = this.OnAvailabilityChanged;
					if (onAvailabilityChanged == null)
					{
						return;
					}
					onAvailabilityChanged(this);
				}
			}
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x06000464 RID: 1124 RVA: 0x00011CB0 File Offset: 0x0000FEB0
		// (set) Token: 0x06000465 RID: 1125 RVA: 0x00011CBB File Offset: 0x0000FEBB
		public bool IsDisabled
		{
			get
			{
				return !this.IsEnabled;
			}
			set
			{
				if (value == this.IsEnabled)
				{
					this.IsEnabled = !value;
				}
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x06000466 RID: 1126 RVA: 0x00011CD0 File Offset: 0x0000FED0
		// (set) Token: 0x06000467 RID: 1127 RVA: 0x00011CD8 File Offset: 0x0000FED8
		public string CollectionID { get; set; }

		// Token: 0x17000145 RID: 325
		// (get) Token: 0x06000468 RID: 1128 RVA: 0x00011CE1 File Offset: 0x0000FEE1
		// (set) Token: 0x06000469 RID: 1129 RVA: 0x00011CE9 File Offset: 0x0000FEE9
		public int CollectionOrder { get; set; }

		// Token: 0x17000146 RID: 326
		// (get) Token: 0x0600046A RID: 1130 RVA: 0x00011CF2 File Offset: 0x0000FEF2
		// (set) Token: 0x0600046B RID: 1131 RVA: 0x00011CFC File Offset: 0x0000FEFC
		public Widget ParentWidget
		{
			get
			{
				return this._parentWidget;
			}
			set
			{
				if (value != this._parentWidget)
				{
					if (this._parentWidget != null)
					{
						this._invisibleParents.Clear();
						for (Widget parentWidget = this._parentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
						{
							parentWidget.OnVisibilityChanged -= this.OnParentVisibilityChanged;
						}
					}
					this._parentWidget = value;
					for (Widget parentWidget2 = this._parentWidget; parentWidget2 != null; parentWidget2 = parentWidget2.ParentWidget)
					{
						if (!parentWidget2.IsVisible)
						{
							this._invisibleParents.Add(parentWidget2);
						}
						parentWidget2.OnVisibilityChanged += this.OnParentVisibilityChanged;
					}
				}
			}
		}

		// Token: 0x17000147 RID: 327
		// (get) Token: 0x0600046C RID: 1132 RVA: 0x00011D8A File Offset: 0x0000FF8A
		// (set) Token: 0x0600046D RID: 1133 RVA: 0x00011D92 File Offset: 0x0000FF92
		public List<GamepadNavigationScope> Scopes { get; private set; }

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x0600046E RID: 1134 RVA: 0x00011D9B File Offset: 0x0000FF9B
		// (set) Token: 0x0600046F RID: 1135 RVA: 0x00011DA3 File Offset: 0x0000FFA3
		public GamepadNavigationScope ActiveScope { get; set; }

		// Token: 0x17000149 RID: 329
		// (get) Token: 0x06000470 RID: 1136 RVA: 0x00011DAC File Offset: 0x0000FFAC
		// (set) Token: 0x06000471 RID: 1137 RVA: 0x00011DB4 File Offset: 0x0000FFB4
		public GamepadNavigationScope PreviousScope { get; set; }

		// Token: 0x06000472 RID: 1138 RVA: 0x00011DBD File Offset: 0x0000FFBD
		public GamepadNavigationForcedScopeCollection()
		{
			this.Scopes = new List<GamepadNavigationScope>();
			this._invisibleParents = new List<Widget>();
			this.IsEnabled = true;
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x00011DE4 File Offset: 0x0000FFE4
		private void OnParentVisibilityChanged(Widget parent)
		{
			bool flag = this._invisibleParents.Count == 0;
			if (!parent.IsVisible)
			{
				this._invisibleParents.Add(parent);
			}
			else
			{
				this._invisibleParents.Remove(parent);
			}
			bool flag2 = this._invisibleParents.Count == 0;
			if (flag != flag2)
			{
				Action<GamepadNavigationForcedScopeCollection> onAvailabilityChanged = this.OnAvailabilityChanged;
				if (onAvailabilityChanged == null)
				{
					return;
				}
				onAvailabilityChanged(this);
			}
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x00011E48 File Offset: 0x00010048
		public bool IsAvailable()
		{
			if (this.IsEnabled && this._invisibleParents.Count == 0)
			{
				if (this.Scopes.Any((GamepadNavigationScope x) => x.IsAvailable()))
				{
					return this.ParentWidget.Context.GamepadNavigation.IsAvailableForNavigation();
				}
			}
			return false;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x00011EAD File Offset: 0x000100AD
		public void AddScope(GamepadNavigationScope scope)
		{
			if (!this.Scopes.Contains(scope))
			{
				this.Scopes.Add(scope);
			}
			Action<GamepadNavigationForcedScopeCollection> onAvailabilityChanged = this.OnAvailabilityChanged;
			if (onAvailabilityChanged == null)
			{
				return;
			}
			onAvailabilityChanged(this);
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x00011EDA File Offset: 0x000100DA
		public void RemoveScope(GamepadNavigationScope scope)
		{
			if (this.Scopes.Contains(scope))
			{
				this.Scopes.Remove(scope);
			}
			Action<GamepadNavigationForcedScopeCollection> onAvailabilityChanged = this.OnAvailabilityChanged;
			if (onAvailabilityChanged == null)
			{
				return;
			}
			onAvailabilityChanged(this);
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x00011F08 File Offset: 0x00010108
		public void ClearScopes()
		{
			this.Scopes.Clear();
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x00011F15 File Offset: 0x00010115
		public override string ToString()
		{
			return string.Format("ID:{0} C.C.:{1}", this.CollectionID, this.Scopes.Count);
		}

		// Token: 0x04000228 RID: 552
		public Action<GamepadNavigationForcedScopeCollection> OnAvailabilityChanged;

		// Token: 0x04000229 RID: 553
		private List<Widget> _invisibleParents;

		// Token: 0x0400022A RID: 554
		private bool _isEnabled;

		// Token: 0x0400022D RID: 557
		private Widget _parentWidget;
	}
}
