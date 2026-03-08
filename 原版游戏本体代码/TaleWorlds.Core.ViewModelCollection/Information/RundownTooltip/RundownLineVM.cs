using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip
{
	// Token: 0x0200001B RID: 27
	public class RundownLineVM : ViewModel
	{
		// Token: 0x0600017F RID: 383 RVA: 0x00005543 File Offset: 0x00003743
		public RundownLineVM(string name, float value)
		{
			this.Name = name;
			this.ValueAsString = string.Format("{0:0.##}", value);
			this.Value = value;
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000180 RID: 384 RVA: 0x0000556F File Offset: 0x0000376F
		// (set) Token: 0x06000181 RID: 385 RVA: 0x00005577 File Offset: 0x00003777
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000182 RID: 386 RVA: 0x0000559A File Offset: 0x0000379A
		// (set) Token: 0x06000183 RID: 387 RVA: 0x000055A2 File Offset: 0x000037A2
		[DataSourceProperty]
		public string ValueAsString
		{
			get
			{
				return this._valueAsString;
			}
			set
			{
				if (value != this._valueAsString)
				{
					this._valueAsString = value;
					base.OnPropertyChangedWithValue<string>(value, "ValueAsString");
				}
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000184 RID: 388 RVA: 0x000055C5 File Offset: 0x000037C5
		// (set) Token: 0x06000185 RID: 389 RVA: 0x000055CD File Offset: 0x000037CD
		[DataSourceProperty]
		public float Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (value != this._value)
				{
					this._value = value;
					base.OnPropertyChangedWithValue(value, "Value");
				}
			}
		}

		// Token: 0x04000098 RID: 152
		private string _name;

		// Token: 0x04000099 RID: 153
		private string _valueAsString;

		// Token: 0x0400009A RID: 154
		private float _value;
	}
}
