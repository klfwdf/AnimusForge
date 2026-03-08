using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000004 RID: 4
	public class DelayedStateChanger : BrushWidget
	{
		// Token: 0x06000011 RID: 17 RVA: 0x0000221F File Offset: 0x0000041F
		public DelayedStateChanger(UIContext context)
			: base(context)
		{
			this._isStarted = false;
			this._isFinished = false;
			this._timePassed = 0f;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002241 File Offset: 0x00000441
		protected override void OnConnectedToRoot()
		{
			this._defaultState = base.CurrentState;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002250 File Offset: 0x00000450
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (this._isFinished || string.IsNullOrEmpty(this.State))
			{
				return;
			}
			if (!this._isStarted)
			{
				if (this.AutoStart)
				{
					this.Start();
					return;
				}
			}
			else
			{
				this._timePassed += dt;
				if (this._timePassed >= this.Delay)
				{
					this._isFinished = true;
					this.SetState(this._widget, this.State, this.IncludeChildren);
				}
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000022CC File Offset: 0x000004CC
		public void Start()
		{
			this._isStarted = true;
			this._isFinished = false;
			this._timePassed = 0f;
			this._widget = this.TargetWidget ?? this;
			this.AddState(this._widget, this.State, this.IncludeChildren);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000231B File Offset: 0x0000051B
		private void Reset()
		{
			this._isStarted = false;
			this._isFinished = true;
			this._widget = this.TargetWidget ?? this;
			this.SetState(this._widget, this._defaultState, this.IncludeChildren);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002354 File Offset: 0x00000554
		private void AddState(Widget widget, string state, bool includeChildren)
		{
			widget.AddState(state);
			if (includeChildren)
			{
				for (int i = 0; i < widget.ChildCount; i++)
				{
					this.AddState(widget.GetChild(i), state, true);
				}
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x0000238C File Offset: 0x0000058C
		private void SetState(Widget widget, string state, bool includeChildren)
		{
			widget.SetState(state);
			if (includeChildren)
			{
				for (int i = 0; i < widget.ChildCount; i++)
				{
					this.SetState(widget.GetChild(i), state, true);
				}
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000023C3 File Offset: 0x000005C3
		private void TriggerUpdated()
		{
			if (this.Trigger)
			{
				this.Start();
				return;
			}
			if (this.StateResetable)
			{
				this.Reset();
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000023E2 File Offset: 0x000005E2
		private void TargetWidgetUpdated()
		{
			this._defaultState = ((this.TargetWidget == null) ? base.CurrentState : this.TargetWidget.CurrentState);
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002405 File Offset: 0x00000605
		// (set) Token: 0x0600001B RID: 27 RVA: 0x0000240D File Offset: 0x0000060D
		[Editor(false)]
		public bool AutoStart
		{
			get
			{
				return this._autoStart;
			}
			set
			{
				if (this._autoStart != value)
				{
					this._autoStart = value;
					base.OnPropertyChanged(value, "AutoStart");
				}
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600001C RID: 28 RVA: 0x0000242B File Offset: 0x0000062B
		// (set) Token: 0x0600001D RID: 29 RVA: 0x00002433 File Offset: 0x00000633
		[Editor(false)]
		public bool Trigger
		{
			get
			{
				return this._trigger;
			}
			set
			{
				if (this._trigger != value)
				{
					this._trigger = value;
					base.OnPropertyChanged(value, "Trigger");
					this.TriggerUpdated();
				}
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600001E RID: 30 RVA: 0x00002457 File Offset: 0x00000657
		// (set) Token: 0x0600001F RID: 31 RVA: 0x0000245F File Offset: 0x0000065F
		[Editor(false)]
		public bool StateResetable
		{
			get
			{
				return this._stateResetable;
			}
			set
			{
				if (this._stateResetable != value)
				{
					this._stateResetable = value;
					base.OnPropertyChanged(value, "StateResetable");
				}
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000020 RID: 32 RVA: 0x0000247D File Offset: 0x0000067D
		// (set) Token: 0x06000021 RID: 33 RVA: 0x00002485 File Offset: 0x00000685
		[Editor(false)]
		public bool IncludeChildren
		{
			get
			{
				return this._includeChildren;
			}
			set
			{
				if (this._includeChildren != value)
				{
					this._includeChildren = value;
					base.OnPropertyChanged(value, "IncludeChildren");
				}
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000024A3 File Offset: 0x000006A3
		// (set) Token: 0x06000023 RID: 35 RVA: 0x000024AB File Offset: 0x000006AB
		[Editor(false)]
		public float Delay
		{
			get
			{
				return this._delay;
			}
			set
			{
				if (this._delay != value)
				{
					this._delay = value;
					base.OnPropertyChanged(value, "Delay");
				}
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000024C9 File Offset: 0x000006C9
		// (set) Token: 0x06000025 RID: 37 RVA: 0x000024D1 File Offset: 0x000006D1
		[Editor(false)]
		public string State
		{
			get
			{
				return this._state;
			}
			set
			{
				if (this._state != value)
				{
					this._state = value;
					base.OnPropertyChanged<string>(value, "State");
				}
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000024F4 File Offset: 0x000006F4
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000024FC File Offset: 0x000006FC
		[Editor(false)]
		public Widget TargetWidget
		{
			get
			{
				return this._targetWidget;
			}
			set
			{
				if (this._targetWidget != value)
				{
					this._targetWidget = value;
					base.OnPropertyChanged<Widget>(value, "TargetWidget");
					this.TargetWidgetUpdated();
				}
			}
		}

		// Token: 0x04000009 RID: 9
		private bool _isStarted;

		// Token: 0x0400000A RID: 10
		private bool _isFinished;

		// Token: 0x0400000B RID: 11
		private float _timePassed;

		// Token: 0x0400000C RID: 12
		private Widget _widget;

		// Token: 0x0400000D RID: 13
		private string _defaultState;

		// Token: 0x0400000E RID: 14
		private bool _autoStart;

		// Token: 0x0400000F RID: 15
		private bool _trigger;

		// Token: 0x04000010 RID: 16
		private bool _stateResetable;

		// Token: 0x04000011 RID: 17
		private bool _includeChildren;

		// Token: 0x04000012 RID: 18
		private float _delay;

		// Token: 0x04000013 RID: 19
		private string _state;

		// Token: 0x04000014 RID: 20
		private Widget _targetWidget;
	}
}
