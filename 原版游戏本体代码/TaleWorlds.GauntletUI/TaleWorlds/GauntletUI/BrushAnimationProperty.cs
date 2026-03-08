using System;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200000E RID: 14
	public class BrushAnimationProperty
	{
		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000DC RID: 220 RVA: 0x00003BBA File Offset: 0x00001DBA
		// (set) Token: 0x060000DD RID: 221 RVA: 0x00003BC2 File Offset: 0x00001DC2
		public string LayerName { get; set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000DE RID: 222 RVA: 0x00003BCB File Offset: 0x00001DCB
		public IEnumerable<BrushAnimationKeyFrame> KeyFrames
		{
			get
			{
				return this._keyFrames.AsReadOnly();
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000DF RID: 223 RVA: 0x00003BD8 File Offset: 0x00001DD8
		public int Count
		{
			get
			{
				return this._keyFrames.Count;
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00003BE5 File Offset: 0x00001DE5
		public BrushAnimationProperty()
		{
			this._keyFrames = new List<BrushAnimationKeyFrame>();
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00003BF8 File Offset: 0x00001DF8
		public BrushAnimationKeyFrame GetFrameAfter(float time)
		{
			for (int i = 0; i < this._keyFrames.Count; i++)
			{
				BrushAnimationKeyFrame brushAnimationKeyFrame = this._keyFrames[i];
				if (time < brushAnimationKeyFrame.Time)
				{
					return brushAnimationKeyFrame;
				}
			}
			return null;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00003C34 File Offset: 0x00001E34
		public BrushAnimationKeyFrame GetFrameAt(int i)
		{
			if (i >= 0 && i < this._keyFrames.Count)
			{
				return this._keyFrames[i];
			}
			return null;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00003C56 File Offset: 0x00001E56
		public BrushAnimationProperty Clone()
		{
			BrushAnimationProperty brushAnimationProperty = new BrushAnimationProperty();
			brushAnimationProperty.FillFrom(this);
			return brushAnimationProperty;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00003C64 File Offset: 0x00001E64
		private void FillFrom(BrushAnimationProperty collection)
		{
			this.PropertyType = collection.PropertyType;
			this.LayerName = collection.LayerName;
			this._keyFrames = new List<BrushAnimationKeyFrame>(collection._keyFrames.Count);
			for (int i = 0; i < collection._keyFrames.Count; i++)
			{
				BrushAnimationKeyFrame item = collection._keyFrames[i].Clone();
				this._keyFrames.Add(item);
			}
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00003CD4 File Offset: 0x00001ED4
		public void AddKeyFrame(BrushAnimationKeyFrame keyFrame)
		{
			this._keyFrames.Add(keyFrame);
			this._keyFrames = (from k in this._keyFrames
				orderby k.Time
				select k).ToList<BrushAnimationKeyFrame>();
			for (int i = 0; i < this._keyFrames.Count; i++)
			{
				this._keyFrames[i].InitializeIndex(i);
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00003D4A File Offset: 0x00001F4A
		public void RemoveKeyFrame(BrushAnimationKeyFrame keyFrame)
		{
			this._keyFrames.Remove(keyFrame);
		}

		// Token: 0x0400003E RID: 62
		public BrushAnimationProperty.BrushAnimationPropertyType PropertyType;

		// Token: 0x0400003F RID: 63
		private List<BrushAnimationKeyFrame> _keyFrames;

		// Token: 0x02000077 RID: 119
		public enum BrushAnimationPropertyType
		{
			// Token: 0x040003F9 RID: 1017
			Name,
			// Token: 0x040003FA RID: 1018
			ColorFactor,
			// Token: 0x040003FB RID: 1019
			Color,
			// Token: 0x040003FC RID: 1020
			AlphaFactor,
			// Token: 0x040003FD RID: 1021
			HueFactor,
			// Token: 0x040003FE RID: 1022
			SaturationFactor,
			// Token: 0x040003FF RID: 1023
			ValueFactor,
			// Token: 0x04000400 RID: 1024
			FontColor,
			// Token: 0x04000401 RID: 1025
			OverlayXOffset,
			// Token: 0x04000402 RID: 1026
			OverlayYOffset,
			// Token: 0x04000403 RID: 1027
			TextGlowColor,
			// Token: 0x04000404 RID: 1028
			TextOutlineColor,
			// Token: 0x04000405 RID: 1029
			TextOutlineAmount,
			// Token: 0x04000406 RID: 1030
			TextGlowRadius,
			// Token: 0x04000407 RID: 1031
			TextBlur,
			// Token: 0x04000408 RID: 1032
			TextShadowOffset,
			// Token: 0x04000409 RID: 1033
			TextShadowAngle,
			// Token: 0x0400040A RID: 1034
			TextColorFactor,
			// Token: 0x0400040B RID: 1035
			TextAlphaFactor,
			// Token: 0x0400040C RID: 1036
			TextHueFactor,
			// Token: 0x0400040D RID: 1037
			TextSaturationFactor,
			// Token: 0x0400040E RID: 1038
			TextValueFactor,
			// Token: 0x0400040F RID: 1039
			Sprite,
			// Token: 0x04000410 RID: 1040
			IsHidden,
			// Token: 0x04000411 RID: 1041
			XOffset,
			// Token: 0x04000412 RID: 1042
			YOffset,
			// Token: 0x04000413 RID: 1043
			Rotation,
			// Token: 0x04000414 RID: 1044
			OverridenWidth,
			// Token: 0x04000415 RID: 1045
			OverridenHeight,
			// Token: 0x04000416 RID: 1046
			WidthPolicy,
			// Token: 0x04000417 RID: 1047
			HeightPolicy,
			// Token: 0x04000418 RID: 1048
			HorizontalFlip,
			// Token: 0x04000419 RID: 1049
			VerticalFlip,
			// Token: 0x0400041A RID: 1050
			OverlayMethod,
			// Token: 0x0400041B RID: 1051
			OverlaySprite,
			// Token: 0x0400041C RID: 1052
			ExtendLeft,
			// Token: 0x0400041D RID: 1053
			ExtendRight,
			// Token: 0x0400041E RID: 1054
			ExtendTop,
			// Token: 0x0400041F RID: 1055
			ExtendBottom,
			// Token: 0x04000420 RID: 1056
			UseRandomBaseOverlayXOffset,
			// Token: 0x04000421 RID: 1057
			UseRandomBaseOverlayYOffset,
			// Token: 0x04000422 RID: 1058
			Font,
			// Token: 0x04000423 RID: 1059
			FontStyle,
			// Token: 0x04000424 RID: 1060
			FontSize
		}
	}
}
