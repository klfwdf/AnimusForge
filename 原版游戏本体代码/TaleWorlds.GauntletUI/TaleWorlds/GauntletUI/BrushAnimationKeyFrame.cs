using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200000D RID: 13
	public class BrushAnimationKeyFrame
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000CE RID: 206 RVA: 0x00003A87 File Offset: 0x00001C87
		// (set) Token: 0x060000CF RID: 207 RVA: 0x00003A8F File Offset: 0x00001C8F
		public float Time { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000D0 RID: 208 RVA: 0x00003A98 File Offset: 0x00001C98
		// (set) Token: 0x060000D1 RID: 209 RVA: 0x00003AA0 File Offset: 0x00001CA0
		public int Index { get; private set; }

		// Token: 0x060000D3 RID: 211 RVA: 0x00003AB1 File Offset: 0x00001CB1
		public void InitializeAsFloat(float time, float value)
		{
			this.Time = time;
			this._valueType = BrushAnimationKeyFrame.ValueType.Float;
			this._valueAsFloat = value;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00003AC8 File Offset: 0x00001CC8
		public void InitializeAsColor(float time, Color value)
		{
			this.Time = time;
			this._valueType = BrushAnimationKeyFrame.ValueType.Color;
			this._valueAsColor = value;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00003ADF File Offset: 0x00001CDF
		public void InitializeAsSprite(float time, Sprite value)
		{
			this.Time = time;
			this._valueType = BrushAnimationKeyFrame.ValueType.Sprite;
			this._valueAsSprite = value;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00003AF6 File Offset: 0x00001CF6
		public void InitializeIndex(int index)
		{
			this.Index = index;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00003AFF File Offset: 0x00001CFF
		public float GetValueAsFloat()
		{
			return this._valueAsFloat;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00003B07 File Offset: 0x00001D07
		public Color GetValueAsColor()
		{
			return this._valueAsColor;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00003B0F File Offset: 0x00001D0F
		public Sprite GetValueAsSprite()
		{
			return this._valueAsSprite;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00003B18 File Offset: 0x00001D18
		public object GetValueAsObject()
		{
			switch (this._valueType)
			{
			case BrushAnimationKeyFrame.ValueType.Float:
				return this._valueAsFloat;
			case BrushAnimationKeyFrame.ValueType.Color:
				return this._valueAsColor;
			case BrushAnimationKeyFrame.ValueType.Sprite:
				return this._valueAsSprite;
			default:
				return null;
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00003B60 File Offset: 0x00001D60
		public BrushAnimationKeyFrame Clone()
		{
			return new BrushAnimationKeyFrame
			{
				_valueType = this._valueType,
				_valueAsFloat = this._valueAsFloat,
				_valueAsColor = this._valueAsColor,
				_valueAsSprite = this._valueAsSprite,
				Time = this.Time,
				Index = this.Index
			};
		}

		// Token: 0x04000037 RID: 55
		private BrushAnimationKeyFrame.ValueType _valueType;

		// Token: 0x04000038 RID: 56
		private float _valueAsFloat;

		// Token: 0x04000039 RID: 57
		private Color _valueAsColor;

		// Token: 0x0400003A RID: 58
		private Sprite _valueAsSprite;

		// Token: 0x02000076 RID: 118
		public enum ValueType
		{
			// Token: 0x040003F5 RID: 1013
			Float,
			// Token: 0x040003F6 RID: 1014
			Color,
			// Token: 0x040003F7 RID: 1015
			Sprite
		}
	}
}
