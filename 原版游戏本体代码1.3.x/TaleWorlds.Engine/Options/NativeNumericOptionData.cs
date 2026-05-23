using System;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000A9 RID: 169
	public class NativeNumericOptionData : NativeOptionData, INumericOptionData, IOptionData
	{
		// Token: 0x06000F39 RID: 3897 RVA: 0x00011C0F File Offset: 0x0000FE0F
		public NativeNumericOptionData(NativeOptions.NativeOptionsType type)
			: base(type)
		{
			this._minValue = NativeNumericOptionData.GetLimitValue(this.Type, true);
			this._maxValue = NativeNumericOptionData.GetLimitValue(this.Type, false);
		}

		// Token: 0x06000F3A RID: 3898 RVA: 0x00011C3C File Offset: 0x0000FE3C
		public float GetMinValue()
		{
			return this._minValue;
		}

		// Token: 0x06000F3B RID: 3899 RVA: 0x00011C44 File Offset: 0x0000FE44
		public float GetMaxValue()
		{
			return this._maxValue;
		}

		// Token: 0x06000F3C RID: 3900 RVA: 0x00011C4C File Offset: 0x0000FE4C
		private static float GetLimitValue(NativeOptions.NativeOptionsType type, bool isMin)
		{
			if (type <= NativeOptions.NativeOptionsType.Brightness)
			{
				switch (type)
				{
				case NativeOptions.NativeOptionsType.MouseSensitivity:
					if (!isMin)
					{
						return 1f;
					}
					return 0.3f;
				case NativeOptions.NativeOptionsType.InvertMouseYAxis:
				case NativeOptions.NativeOptionsType.EnableVibration:
				case NativeOptions.NativeOptionsType.EnableGyroAssistedAim:
					break;
				case NativeOptions.NativeOptionsType.MouseYMovementScale:
					if (!isMin)
					{
						return 4f;
					}
					return 0.25f;
				case NativeOptions.NativeOptionsType.TrailAmount:
					if (!isMin)
					{
						return 1f;
					}
					return 0f;
				case NativeOptions.NativeOptionsType.GyroAimSensitivity:
					if (!isMin)
					{
						return 1f;
					}
					return 0f;
				default:
					switch (type)
					{
					case NativeOptions.NativeOptionsType.ResolutionScale:
						if (!isMin)
						{
							return 100f;
						}
						return 50f;
					case NativeOptions.NativeOptionsType.FrameLimiter:
						if (!isMin)
						{
							return 360f;
						}
						return 30f;
					case NativeOptions.NativeOptionsType.Brightness:
						if (!isMin)
						{
							return 100f;
						}
						return 0f;
					}
					break;
				}
			}
			else if (type != NativeOptions.NativeOptionsType.SharpenAmount)
			{
				switch (type)
				{
				case NativeOptions.NativeOptionsType.BrightnessMin:
					if (!isMin)
					{
						return 0.3f;
					}
					return 0f;
				case NativeOptions.NativeOptionsType.BrightnessMax:
					if (!isMin)
					{
						return 1f;
					}
					return 0.7f;
				case NativeOptions.NativeOptionsType.ExposureCompensation:
					if (!isMin)
					{
						return 2f;
					}
					return -2f;
				case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
					if (!isMin)
					{
						return 240f;
					}
					return 30f;
				}
			}
			else
			{
				if (!isMin)
				{
					return 100f;
				}
				return 0f;
			}
			if (!isMin)
			{
				return 1f;
			}
			return 0f;
		}

		// Token: 0x06000F3D RID: 3901 RVA: 0x00011D94 File Offset: 0x0000FF94
		public bool GetIsDiscrete()
		{
			NativeOptions.NativeOptionsType type = this.Type;
			if (type <= NativeOptions.NativeOptionsType.Brightness)
			{
				if (type - NativeOptions.NativeOptionsType.ResolutionScale > 1 && type != NativeOptions.NativeOptionsType.Brightness)
				{
					return false;
				}
			}
			else if (type != NativeOptions.NativeOptionsType.SharpenAmount)
			{
				switch (type)
				{
				case NativeOptions.NativeOptionsType.BrightnessMin:
				case NativeOptions.NativeOptionsType.BrightnessMax:
				case NativeOptions.NativeOptionsType.ExposureCompensation:
				case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
					break;
				case NativeOptions.NativeOptionsType.BrightnessCalibrated:
				case NativeOptions.NativeOptionsType.DynamicResolution:
					return false;
				default:
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000F3E RID: 3902 RVA: 0x00011DE8 File Offset: 0x0000FFE8
		public int GetDiscreteIncrementInterval()
		{
			NativeOptions.NativeOptionsType type = this.Type;
			if (type == NativeOptions.NativeOptionsType.SharpenAmount)
			{
				return 5;
			}
			return 1;
		}

		// Token: 0x06000F3F RID: 3903 RVA: 0x00011E04 File Offset: 0x00010004
		public bool GetShouldUpdateContinuously()
		{
			return true;
		}

		// Token: 0x0400021A RID: 538
		private readonly float _minValue;

		// Token: 0x0400021B RID: 539
		private readonly float _maxValue;
	}
}
