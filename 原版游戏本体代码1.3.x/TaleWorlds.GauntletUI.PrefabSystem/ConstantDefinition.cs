using System;
using System.Collections.Generic;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000002 RID: 2
	public class ConstantDefinition
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
		public string Name { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002059 File Offset: 0x00000259
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002061 File Offset: 0x00000261
		public string Value { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000005 RID: 5 RVA: 0x0000206A File Offset: 0x0000026A
		// (set) Token: 0x06000006 RID: 6 RVA: 0x00002072 File Offset: 0x00000272
		public string SpriteName { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000207B File Offset: 0x0000027B
		// (set) Token: 0x06000008 RID: 8 RVA: 0x00002083 File Offset: 0x00000283
		public string BrushName { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000009 RID: 9 RVA: 0x0000208C File Offset: 0x0000028C
		// (set) Token: 0x0600000A RID: 10 RVA: 0x00002094 File Offset: 0x00000294
		public string LayerName { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000B RID: 11 RVA: 0x0000209D File Offset: 0x0000029D
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000020A5 File Offset: 0x000002A5
		public string Additive { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600000D RID: 13 RVA: 0x000020AE File Offset: 0x000002AE
		// (set) Token: 0x0600000E RID: 14 RVA: 0x000020B6 File Offset: 0x000002B6
		public string Prefix { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600000F RID: 15 RVA: 0x000020BF File Offset: 0x000002BF
		// (set) Token: 0x06000010 RID: 16 RVA: 0x000020C7 File Offset: 0x000002C7
		public string Suffix { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000020D0 File Offset: 0x000002D0
		// (set) Token: 0x06000012 RID: 18 RVA: 0x000020D8 File Offset: 0x000002D8
		public float MultiplyResult { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000013 RID: 19 RVA: 0x000020E1 File Offset: 0x000002E1
		// (set) Token: 0x06000014 RID: 20 RVA: 0x000020E9 File Offset: 0x000002E9
		public string OnTrueValue { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000015 RID: 21 RVA: 0x000020F2 File Offset: 0x000002F2
		// (set) Token: 0x06000016 RID: 22 RVA: 0x000020FA File Offset: 0x000002FA
		public string OnFalseValue { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000017 RID: 23 RVA: 0x00002103 File Offset: 0x00000303
		// (set) Token: 0x06000018 RID: 24 RVA: 0x0000210B File Offset: 0x0000030B
		public ConstantDefinitionType Type { get; set; }

		// Token: 0x06000019 RID: 25 RVA: 0x00002114 File Offset: 0x00000314
		public ConstantDefinition(string name)
		{
			this.Name = name;
			this.MultiplyResult = 1f;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002130 File Offset: 0x00000330
		public string GetValue(BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
		{
			string text = "";
			if (this.Type == ConstantDefinitionType.Constant)
			{
				string text2 = ConstantDefinition.GetActualValueOf(this.Value, brushFactory, spriteData, constants, parameters, defaultParameters);
				if (!string.IsNullOrEmpty(this.Additive))
				{
					int num = Convert.ToInt32(ConstantDefinition.GetActualValueOf(this.Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
					text2 = (Convert.ToInt32(decimal.Parse(text2)) + num).ToString();
				}
				text = text2;
			}
			else if (this.Type == ConstantDefinitionType.BooleanCheck)
			{
				if (ConstantDefinition.GetActualValueOf(this.Value, brushFactory, spriteData, constants, parameters, defaultParameters) == "true")
				{
					text = ConstantDefinition.GetActualValueOf(this.OnTrueValue, brushFactory, spriteData, constants, parameters, defaultParameters);
				}
				else
				{
					text = ConstantDefinition.GetActualValueOf(this.OnFalseValue, brushFactory, spriteData, constants, parameters, defaultParameters);
				}
			}
			else if (this.Type == ConstantDefinitionType.BrushLayerWidth || this.Type == ConstantDefinitionType.BrushLayerHeight)
			{
				string actualValueOf = ConstantDefinition.GetActualValueOf(this.BrushName, brushFactory, spriteData, constants, parameters, defaultParameters);
				Brush brush = brushFactory.GetBrush(actualValueOf);
				if (brush != null)
				{
					BrushLayer layer = brush.GetLayer(this.LayerName);
					if (layer != null)
					{
						Sprite sprite = layer.Sprite;
						if (sprite != null)
						{
							int num2 = 0;
							if (!string.IsNullOrEmpty(this.Additive))
							{
								num2 = Convert.ToInt32(ConstantDefinition.GetActualValueOf(this.Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
							}
							if (this.Type == ConstantDefinitionType.BrushLayerWidth)
							{
								float extendLeft = layer.ExtendLeft;
								float extendRight = layer.ExtendRight;
								text = ((int)((float)sprite.Width - extendLeft - extendRight + (float)num2)).ToString();
							}
							else if (this.Type == ConstantDefinitionType.BrushLayerHeight)
							{
								float extendTop = layer.ExtendTop;
								float extendBottom = layer.ExtendBottom;
								text = ((int)((float)sprite.Height - extendTop - extendBottom + (float)num2)).ToString();
							}
						}
					}
				}
			}
			else if (this.Type == ConstantDefinitionType.SpriteWidth || this.Type == ConstantDefinitionType.SpriteHeight)
			{
				Sprite sprite2 = spriteData.GetSprite(this.SpriteName);
				if (sprite2 != null)
				{
					int num3 = 0;
					if (!string.IsNullOrEmpty(this.Additive))
					{
						num3 = Convert.ToInt32(ConstantDefinition.GetActualValueOf(this.Additive, brushFactory, spriteData, constants, parameters, defaultParameters));
					}
					if (this.Type == ConstantDefinitionType.SpriteWidth)
					{
						text = (sprite2.Width + num3).ToString();
					}
					else if (this.Type == ConstantDefinitionType.SpriteHeight)
					{
						text = (sprite2.Height + num3).ToString();
					}
				}
			}
			if (this.MultiplyResult != 1f)
			{
				text = ((float)Convert.ToInt32(text) * this.MultiplyResult).ToString();
			}
			if (!string.IsNullOrEmpty(this.Prefix))
			{
				text = this.Prefix + text;
			}
			if (!string.IsNullOrEmpty(this.Suffix))
			{
				text += this.Suffix;
			}
			return text;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000023DC File Offset: 0x000005DC
		public static string GetActualValueOf(string value, BrushFactory brushFactory, SpriteData spriteData, Dictionary<string, ConstantDefinition> constants, Dictionary<string, WidgetAttributeTemplate> parameters, Dictionary<string, string> defaultParameters)
		{
			string result = "";
			if (value.StartsWith("!"))
			{
				string key = value.Substring("!".Length);
				if (constants.ContainsKey(key))
				{
					result = constants[key].GetValue(brushFactory, spriteData, constants, parameters, defaultParameters);
				}
			}
			else if (value.StartsWith("*"))
			{
				string key2 = value.Substring("*".Length);
				if (parameters.ContainsKey(key2))
				{
					result = parameters[key2].Value;
				}
				else if (defaultParameters != null && defaultParameters.ContainsKey(key2))
				{
					result = defaultParameters[key2];
				}
			}
			else
			{
				result = value;
			}
			return result;
		}
	}
}
