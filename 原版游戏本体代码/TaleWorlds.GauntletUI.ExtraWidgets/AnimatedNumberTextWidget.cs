using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000002 RID: 2
	public class AnimatedNumberTextWidget : TextWidget
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		public AnimatedNumberTextWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002054 File Offset: 0x00000254
		protected override void OnUpdate(float dt)
		{
			if (!this._isAnimationActive)
			{
				return;
			}
			this._timePassed += dt;
			if (this._timePassed >= this.AnimationDelay)
			{
				float num = this._timePassed - this.AnimationDelay;
				this._currentNumber = (int)(num / this.AnimationDuration * (float)this.ReferenceNumber);
				this._currentNumber = MathF.Min(this._currentNumber, this.Number);
				if (this._currentNumber == this.Number)
				{
					this._isAnimationActive = false;
				}
				base.IntText = this._currentNumber;
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020E3 File Offset: 0x000002E3
		public void StartAnimation()
		{
			if (this.AnimationDuration <= 0f || this.ReferenceNumber <= 0)
			{
				return;
			}
			this._isAnimationActive = true;
			this._currentNumber = 0;
			base.IntText = 0;
			this._timePassed = 0f;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000211C File Offset: 0x0000031C
		public void Reset()
		{
			this._isAnimationActive = false;
			this._currentNumber = 0;
			base.IntText = 0;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002133 File Offset: 0x00000333
		private void NumberChanged()
		{
			if (this.AutoStart)
			{
				this.StartAnimation();
			}
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002143 File Offset: 0x00000343
		// (set) Token: 0x06000007 RID: 7 RVA: 0x0000214B File Offset: 0x0000034B
		[Editor(false)]
		public float AnimationDelay
		{
			get
			{
				return this._animationDelay;
			}
			set
			{
				if (this._animationDelay != value)
				{
					this._animationDelay = value;
					base.OnPropertyChanged(value, "AnimationDelay");
				}
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002169 File Offset: 0x00000369
		// (set) Token: 0x06000009 RID: 9 RVA: 0x00002171 File Offset: 0x00000371
		[Editor(false)]
		public float AnimationDuration
		{
			get
			{
				return this._animationDuration;
			}
			set
			{
				if (this._animationDuration != value)
				{
					this._animationDuration = value;
					base.OnPropertyChanged(value, "AnimationDuration");
				}
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000A RID: 10 RVA: 0x0000218F File Offset: 0x0000038F
		// (set) Token: 0x0600000B RID: 11 RVA: 0x00002197 File Offset: 0x00000397
		[Editor(false)]
		public int ReferenceNumber
		{
			get
			{
				return this._referenceNumber;
			}
			set
			{
				if (this._referenceNumber != value)
				{
					this._referenceNumber = value;
					base.OnPropertyChanged(value, "ReferenceNumber");
				}
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000021B5 File Offset: 0x000003B5
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000021BD File Offset: 0x000003BD
		[Editor(false)]
		public int Number
		{
			get
			{
				return this._number;
			}
			set
			{
				if (this._number != value)
				{
					this._number = value;
					base.OnPropertyChanged(value, "Number");
					this.NumberChanged();
				}
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000021E1 File Offset: 0x000003E1
		// (set) Token: 0x0600000F RID: 15 RVA: 0x000021E9 File Offset: 0x000003E9
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

		// Token: 0x04000001 RID: 1
		private int _currentNumber;

		// Token: 0x04000002 RID: 2
		private bool _isAnimationActive;

		// Token: 0x04000003 RID: 3
		private float _timePassed;

		// Token: 0x04000004 RID: 4
		private float _animationDelay;

		// Token: 0x04000005 RID: 5
		private float _animationDuration;

		// Token: 0x04000006 RID: 6
		private int _referenceNumber;

		// Token: 0x04000007 RID: 7
		private int _number;

		// Token: 0x04000008 RID: 8
		private bool _autoStart;
	}
}
