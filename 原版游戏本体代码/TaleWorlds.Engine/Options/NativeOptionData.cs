using System;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000AA RID: 170
	public abstract class NativeOptionData : IOptionData
	{
		// Token: 0x06000F40 RID: 3904 RVA: 0x00011E07 File Offset: 0x00010007
		protected NativeOptionData(NativeOptions.NativeOptionsType type)
		{
			this.Type = type;
			this._value = NativeOptions.GetConfig(type);
		}

		// Token: 0x06000F41 RID: 3905 RVA: 0x00011E22 File Offset: 0x00010022
		public virtual float GetDefaultValue()
		{
			return NativeOptions.GetDefaultConfig(this.Type);
		}

		// Token: 0x06000F42 RID: 3906 RVA: 0x00011E2F File Offset: 0x0001002F
		public void Commit()
		{
			NativeOptions.SetConfig(this.Type, this._value);
		}

		// Token: 0x06000F43 RID: 3907 RVA: 0x00011E42 File Offset: 0x00010042
		public float GetValue(bool forceRefresh)
		{
			if (forceRefresh)
			{
				this._value = NativeOptions.GetConfig(this.Type);
			}
			return this._value;
		}

		// Token: 0x06000F44 RID: 3908 RVA: 0x00011E5E File Offset: 0x0001005E
		public void SetValue(float value)
		{
			this._value = value;
		}

		// Token: 0x06000F45 RID: 3909 RVA: 0x00011E67 File Offset: 0x00010067
		public object GetOptionType()
		{
			return this.Type;
		}

		// Token: 0x06000F46 RID: 3910 RVA: 0x00011E74 File Offset: 0x00010074
		public bool IsNative()
		{
			return true;
		}

		// Token: 0x06000F47 RID: 3911 RVA: 0x00011E77 File Offset: 0x00010077
		public bool IsAction()
		{
			return false;
		}

		// Token: 0x06000F48 RID: 3912 RVA: 0x00011E7C File Offset: 0x0001007C
		public ValueTuple<string, bool> GetIsDisabledAndReasonID()
		{
			NativeOptions.NativeOptionsType type = this.Type;
			if (type <= NativeOptions.NativeOptionsType.ResolutionScale)
			{
				if (type != NativeOptions.NativeOptionsType.GyroAimSensitivity)
				{
					if (type == NativeOptions.NativeOptionsType.ResolutionScale)
					{
						if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
						{
							return new ValueTuple<string, bool>("str_dlss_enabled", true);
						}
						if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DynamicResolution) != 0f)
						{
							return new ValueTuple<string, bool>("str_dynamic_resolution_enabled", true);
						}
					}
				}
				else if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableGyroAssistedAim) != 1f)
				{
					return new ValueTuple<string, bool>("str_gyro_disabled", true);
				}
			}
			else if (type != NativeOptions.NativeOptionsType.DLSS)
			{
				if (type != NativeOptions.NativeOptionsType.DynamicResolution)
				{
					if (type == NativeOptions.NativeOptionsType.DynamicResolutionTarget)
					{
						if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DynamicResolution) == 0f)
						{
							return new ValueTuple<string, bool>("str_dynamic_resolution_disabled", true);
						}
						if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
						{
							return new ValueTuple<string, bool>("str_dlss_enabled", true);
						}
					}
				}
				else if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
				{
					return new ValueTuple<string, bool>("str_dlss_enabled", true);
				}
			}
			else if (!NativeOptions.GetIsDLSSAvailable())
			{
				return new ValueTuple<string, bool>("str_dlss_not_available", true);
			}
			return new ValueTuple<string, bool>(string.Empty, false);
		}

		// Token: 0x0400021C RID: 540
		public readonly NativeOptions.NativeOptionsType Type;

		// Token: 0x0400021D RID: 541
		private float _value;
	}
}
