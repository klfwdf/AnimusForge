using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x0200001A RID: 26
	public class TooltipProperty : ViewModel, ISerializableObject
	{
		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00005050 File Offset: 0x00003250
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00005058 File Offset: 0x00003258
		public bool OnlyShowWhenExtended { get; set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00005061 File Offset: 0x00003261
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00005069 File Offset: 0x00003269
		public bool OnlyShowWhenNotExtended { get; set; }

		// Token: 0x06000168 RID: 360 RVA: 0x00005072 File Offset: 0x00003272
		public TooltipProperty()
		{
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000509C File Offset: 0x0000329C
		public void RefreshValue()
		{
			if (this.valueFunc != null)
			{
				string text = this.valueFunc();
				if (text != null)
				{
					this.ValueLabel = text;
				}
			}
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000050C7 File Offset: 0x000032C7
		public void RefreshDefinition()
		{
			if (this.definitionFunc != null)
			{
				this.DefinitionLabel = this.definitionFunc();
			}
		}

		// Token: 0x0600016B RID: 363 RVA: 0x000050E4 File Offset: 0x000032E4
		public TooltipProperty(string definition, string value, int textHeight, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.TextHeight = textHeight;
			this.DefinitionLabel = definition;
			this.ValueLabel = value;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000513C File Offset: 0x0000333C
		public TooltipProperty(string definition, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.valueFunc = _valueFunc;
			this.TextHeight = textHeight;
			this.DefinitionLabel = definition;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
			this.RefreshValue();
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000519C File Offset: 0x0000339C
		public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.valueFunc = _valueFunc;
			this.TextHeight = textHeight;
			this.definitionFunc = _definitionFunc;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
			this.RefreshDefinition();
			this.RefreshValue();
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00005200 File Offset: 0x00003400
		public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, object[] valueArgs, int textHeight, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.valueFunc = _valueFunc;
			this.TextHeight = textHeight;
			this.definitionFunc = _definitionFunc;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
			this.RefreshDefinition();
			this.RefreshValue();
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00005264 File Offset: 0x00003464
		public TooltipProperty(string definition, string value, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.TextHeight = textHeight;
			this.TextColor = color;
			this.DefinitionLabel = definition;
			this.ValueLabel = value;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x000052C4 File Offset: 0x000034C4
		public TooltipProperty(string definition, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.valueFunc = _valueFunc;
			this.TextHeight = textHeight;
			this.TextColor = color;
			this.DefinitionLabel = definition;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
			this.RefreshValue();
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000532C File Offset: 0x0000352C
		public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipProperty.TooltipPropertyFlags modifier = TooltipProperty.TooltipPropertyFlags.None)
		{
			this.valueFunc = _valueFunc;
			this.definitionFunc = _definitionFunc;
			this.TextHeight = textHeight;
			this.TextColor = color;
			this.OnlyShowWhenExtended = onlyShowWhenExtended;
			this.PropertyModifier = (int)modifier;
			this.RefreshDefinition();
			this.RefreshValue();
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00005398 File Offset: 0x00003598
		public TooltipProperty(TooltipProperty property)
		{
			this.TextHeight = property.TextHeight;
			this.TextColor = property.TextColor;
			this.DefinitionLabel = property.DefinitionLabel;
			this.ValueLabel = property.ValueLabel;
			this.OnlyShowWhenExtended = property.OnlyShowWhenExtended;
			this.PropertyModifier = property.PropertyModifier;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00005412 File Offset: 0x00003612
		public void DeserializeFrom(IReader reader)
		{
			this.TextHeight = reader.ReadInt();
			this.TextColor = reader.ReadColor();
			this.DefinitionLabel = reader.ReadString();
			this.ValueLabel = reader.ReadString();
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00005444 File Offset: 0x00003644
		public void SerializeTo(IWriter writer)
		{
			writer.WriteInt(this.TextHeight);
			writer.WriteColor(this.TextColor);
			writer.WriteString(this.DefinitionLabel);
			writer.WriteString(this.ValueLabel);
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000175 RID: 373 RVA: 0x00005476 File Offset: 0x00003676
		// (set) Token: 0x06000176 RID: 374 RVA: 0x0000547E File Offset: 0x0000367E
		public int TextHeight
		{
			get
			{
				return this._textHeight;
			}
			set
			{
				if (value != this._textHeight)
				{
					this._textHeight = value;
					base.OnPropertyChangedWithValue(value, "TextHeight");
				}
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000177 RID: 375 RVA: 0x0000549C File Offset: 0x0000369C
		// (set) Token: 0x06000178 RID: 376 RVA: 0x000054A4 File Offset: 0x000036A4
		public Color TextColor
		{
			get
			{
				return this._textColor;
			}
			set
			{
				if (value != this._textColor)
				{
					this._textColor = value;
					base.OnPropertyChangedWithValue(value, "TextColor");
				}
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000179 RID: 377 RVA: 0x000054C7 File Offset: 0x000036C7
		// (set) Token: 0x0600017A RID: 378 RVA: 0x000054CF File Offset: 0x000036CF
		public string DefinitionLabel
		{
			get
			{
				return this._definitionLabel;
			}
			set
			{
				if (value != this._definitionLabel)
				{
					this._definitionLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "DefinitionLabel");
				}
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600017B RID: 379 RVA: 0x000054F2 File Offset: 0x000036F2
		// (set) Token: 0x0600017C RID: 380 RVA: 0x000054FA File Offset: 0x000036FA
		public string ValueLabel
		{
			get
			{
				return this._valueLabel;
			}
			set
			{
				if (value != this._valueLabel)
				{
					this._valueLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ValueLabel");
				}
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600017D RID: 381 RVA: 0x0000551D File Offset: 0x0000371D
		// (set) Token: 0x0600017E RID: 382 RVA: 0x00005525 File Offset: 0x00003725
		public int PropertyModifier
		{
			get
			{
				return this._propertyModifier;
			}
			set
			{
				if (value != this._propertyModifier)
				{
					this._propertyModifier = value;
					base.OnPropertyChangedWithValue(value, "PropertyModifier");
				}
			}
		}

		// Token: 0x04000091 RID: 145
		private Func<string> valueFunc;

		// Token: 0x04000092 RID: 146
		private Func<string> definitionFunc;

		// Token: 0x04000093 RID: 147
		private string _definitionLabel;

		// Token: 0x04000094 RID: 148
		private string _valueLabel;

		// Token: 0x04000095 RID: 149
		private Color _textColor = new Color(0f, 0f, 0f, 0f);

		// Token: 0x04000096 RID: 150
		private int _textHeight;

		// Token: 0x04000097 RID: 151
		private int _propertyModifier;

		// Token: 0x02000038 RID: 56
		[Flags]
		public enum TooltipPropertyFlags
		{
			// Token: 0x040000E7 RID: 231
			None = 0,
			// Token: 0x040000E8 RID: 232
			MultiLine = 1,
			// Token: 0x040000E9 RID: 233
			BattleMode = 2,
			// Token: 0x040000EA RID: 234
			BattleModeOver = 4,
			// Token: 0x040000EB RID: 235
			WarFirstEnemy = 8,
			// Token: 0x040000EC RID: 236
			WarFirstAlly = 16,
			// Token: 0x040000ED RID: 237
			WarFirstNeutral = 32,
			// Token: 0x040000EE RID: 238
			WarSecondEnemy = 64,
			// Token: 0x040000EF RID: 239
			WarSecondAlly = 128,
			// Token: 0x040000F0 RID: 240
			WarSecondNeutral = 256,
			// Token: 0x040000F1 RID: 241
			RundownSeperator = 512,
			// Token: 0x040000F2 RID: 242
			DefaultSeperator = 1024,
			// Token: 0x040000F3 RID: 243
			Cost = 2048,
			// Token: 0x040000F4 RID: 244
			Title = 4096,
			// Token: 0x040000F5 RID: 245
			RundownResult = 8192
		}
	}
}
