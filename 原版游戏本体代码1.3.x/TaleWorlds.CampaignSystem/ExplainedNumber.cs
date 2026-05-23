using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000085 RID: 133
	public struct ExplainedNumber
	{
		// Token: 0x1700044D RID: 1101
		// (get) Token: 0x06001108 RID: 4360 RVA: 0x00051653 File Offset: 0x0004F853
		public float ResultNumber
		{
			get
			{
				return MathF.Clamp(this._unclampedResultNumber, this.LimitMinValue, this.LimitMaxValue);
			}
		}

		// Token: 0x1700044E RID: 1102
		// (get) Token: 0x06001109 RID: 4361 RVA: 0x0005166C File Offset: 0x0004F86C
		public int RoundedResultNumber
		{
			get
			{
				return MathF.Round(this.ResultNumber);
			}
		}

		// Token: 0x1700044F RID: 1103
		// (get) Token: 0x0600110A RID: 4362 RVA: 0x00051679 File Offset: 0x0004F879
		// (set) Token: 0x0600110B RID: 4363 RVA: 0x00051681 File Offset: 0x0004F881
		public float BaseNumber { get; private set; }

		// Token: 0x17000450 RID: 1104
		// (get) Token: 0x0600110C RID: 4364 RVA: 0x0005168A File Offset: 0x0004F88A
		public bool IncludeDescriptions
		{
			get
			{
				return this._explainer != null;
			}
		}

		// Token: 0x17000451 RID: 1105
		// (get) Token: 0x0600110D RID: 4365 RVA: 0x00051695 File Offset: 0x0004F895
		public float LimitMinValue
		{
			get
			{
				if (this._limitMinValue == null)
				{
					return float.MinValue;
				}
				return this._limitMinValue.Value;
			}
		}

		// Token: 0x17000452 RID: 1106
		// (get) Token: 0x0600110E RID: 4366 RVA: 0x000516B6 File Offset: 0x0004F8B6
		public float LimitMaxValue
		{
			get
			{
				if (this._limitMaxValue == null)
				{
					return float.MaxValue;
				}
				return this._limitMaxValue.Value;
			}
		}

		// Token: 0x17000453 RID: 1107
		// (get) Token: 0x0600110F RID: 4367 RVA: 0x000516D7 File Offset: 0x0004F8D7
		// (set) Token: 0x06001110 RID: 4368 RVA: 0x000516DF File Offset: 0x0004F8DF
		public float SumOfFactors { get; private set; }

		// Token: 0x17000454 RID: 1108
		// (get) Token: 0x06001111 RID: 4369 RVA: 0x000516E8 File Offset: 0x0004F8E8
		private float _unclampedResultNumber
		{
			get
			{
				return this.BaseNumber + this.BaseNumber * this.SumOfFactors;
			}
		}

		// Token: 0x06001112 RID: 4370 RVA: 0x00051700 File Offset: 0x0004F900
		public ExplainedNumber(float baseNumber = 0f, bool includeDescriptions = false, TextObject baseText = null)
		{
			this.BaseNumber = baseNumber;
			this._explainer = (includeDescriptions ? new ExplainedNumber.StatExplainer() : null);
			this.SumOfFactors = 0f;
			this._limitMinValue = new float?(float.MinValue);
			this._limitMaxValue = new float?(float.MaxValue);
			if (this._explainer != null && !this.BaseNumber.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				this._explainer.AddLine((baseText ?? ExplainedNumber.BaseText).ToString(), this.BaseNumber, ExplainedNumber.StatExplainer.OperationType.Base);
			}
		}

		// Token: 0x06001113 RID: 4371 RVA: 0x00051790 File Offset: 0x0004F990
		public string GetExplanations()
		{
			if (this._explainer == null)
			{
				return "";
			}
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "GetExplanations");
			foreach (ValueTuple<string, float> valueTuple in this._explainer.GetLines(this.BaseNumber, this._unclampedResultNumber, null, null, null))
			{
				string value = string.Format("{0} : {1}{2:0.##}\n", valueTuple.Item1, (valueTuple.Item2 > 0.001f) ? "+" : "", valueTuple.Item2);
				mbstringBuilder.Append<string>(value);
			}
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x06001114 RID: 4372 RVA: 0x0005185C File Offset: 0x0004FA5C
		[return: TupleElementNames(new string[] { "name", "number" })]
		public List<ValueTuple<string, float>> GetLines()
		{
			if (this._explainer == null)
			{
				return new List<ValueTuple<string, float>>();
			}
			return this._explainer.GetLines(this.BaseNumber, this._unclampedResultNumber, null, null, null);
		}

		// Token: 0x06001115 RID: 4373 RVA: 0x00051888 File Offset: 0x0004FA88
		public void AddFromExplainedNumber(ExplainedNumber explainedNumber, TextObject baseText)
		{
			if (explainedNumber._explainer != null && this._explainer != null)
			{
				TextObject textObject = new TextObject("{=HKoLNyIm}{BASE} Maximum", null);
				TextObject textObject2 = new TextObject("{=0Fliz2vk}{BASE} Minimum", null);
				textObject.SetTextVariable("BASE", baseText);
				textObject2.SetTextVariable("BASE", baseText);
				foreach (ValueTuple<string, float> valueTuple in explainedNumber._explainer.GetLines(explainedNumber.BaseNumber, explainedNumber._unclampedResultNumber, baseText, textObject, textObject2))
				{
					this._explainer.AddLine(valueTuple.Item1, valueTuple.Item2, ExplainedNumber.StatExplainer.OperationType.Add);
				}
			}
			this.BaseNumber += explainedNumber.ResultNumber;
		}

		// Token: 0x06001116 RID: 4374 RVA: 0x00051960 File Offset: 0x0004FB60
		public void SubtractFromExplainedNumber(ExplainedNumber explainedNumber, TextObject baseText)
		{
			if (explainedNumber._explainer != null && this._explainer != null)
			{
				TextObject textObject = new TextObject("{=HKoLNyIm}{BASE} Maximum", null);
				TextObject textObject2 = new TextObject("{=0Fliz2vk}{BASE} Minimum", null);
				textObject.SetTextVariable("BASE", baseText);
				textObject2.SetTextVariable("BASE", baseText);
				foreach (ValueTuple<string, float> valueTuple in explainedNumber._explainer.GetLines(explainedNumber.BaseNumber, explainedNumber._unclampedResultNumber, baseText, textObject, textObject2))
				{
					this._explainer.AddLine(valueTuple.Item1, -valueTuple.Item2, ExplainedNumber.StatExplainer.OperationType.Add);
				}
			}
			this.BaseNumber -= explainedNumber.ResultNumber;
		}

		// Token: 0x06001117 RID: 4375 RVA: 0x00051A38 File Offset: 0x0004FC38
		public void Add(float value, TextObject description = null, TextObject variable = null)
		{
			if (value.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return;
			}
			this.BaseNumber += value;
			if (this._explainer != null && description != null && !value.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				if (variable != null)
				{
					description.SetTextVariable("A0", variable);
				}
				this._explainer.AddLine(description.ToString(), value, ExplainedNumber.StatExplainer.OperationType.Add);
			}
		}

		// Token: 0x06001118 RID: 4376 RVA: 0x00051AB4 File Offset: 0x0004FCB4
		public void AddFactor(float value, TextObject description = null)
		{
			if (value.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return;
			}
			this.SumOfFactors += value;
			if (description != null && this._explainer != null && !value.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				this._explainer.AddLine(description.ToString(), MathF.Round(value, 3) * 100f, ExplainedNumber.StatExplainer.OperationType.Multiply);
			}
		}

		// Token: 0x06001119 RID: 4377 RVA: 0x00051B24 File Offset: 0x0004FD24
		public void LimitMin(float minValue)
		{
			this._limitMinValue = new float?(minValue);
			if (this._explainer != null)
			{
				this._explainer.AddLine(ExplainedNumber.LimitMinText.ToString(), minValue, ExplainedNumber.StatExplainer.OperationType.LimitMin);
			}
		}

		// Token: 0x0600111A RID: 4378 RVA: 0x00051B51 File Offset: 0x0004FD51
		public void LimitMax(float maxValue, TextObject description = null)
		{
			this._limitMaxValue = new float?(maxValue);
			if (this._explainer != null)
			{
				this._explainer.AddLine((description ?? ExplainedNumber.LimitMaxText).ToString(), maxValue, ExplainedNumber.StatExplainer.OperationType.LimitMax);
			}
		}

		// Token: 0x0600111B RID: 4379 RVA: 0x00051B83 File Offset: 0x0004FD83
		public void Clamp(float minValue, float maxValue)
		{
			this.LimitMin(minValue);
			this.LimitMax(maxValue, null);
		}

		// Token: 0x0400054C RID: 1356
		private static readonly TextObject LimitMinText = new TextObject("{=GNalaRaN}Minimum", null);

		// Token: 0x0400054D RID: 1357
		private static readonly TextObject LimitMaxText = new TextObject("{=cfjTtxWv}Maximum", null);

		// Token: 0x0400054E RID: 1358
		private static readonly TextObject BaseText = new TextObject("{=basevalue}Base", null);

		// Token: 0x04000550 RID: 1360
		private float? _limitMinValue;

		// Token: 0x04000551 RID: 1361
		private float? _limitMaxValue;

		// Token: 0x04000552 RID: 1362
		private ExplainedNumber.StatExplainer _explainer;

		// Token: 0x0200053E RID: 1342
		private class StatExplainer
		{
			// Token: 0x17000ED6 RID: 3798
			// (get) Token: 0x06004C28 RID: 19496 RVA: 0x001794D6 File Offset: 0x001776D6
			// (set) Token: 0x06004C29 RID: 19497 RVA: 0x001794DE File Offset: 0x001776DE
			public List<ExplainedNumber.StatExplainer.ExplanationLine> Lines { get; private set; } = new List<ExplainedNumber.StatExplainer.ExplanationLine>();

			// Token: 0x17000ED7 RID: 3799
			// (get) Token: 0x06004C2A RID: 19498 RVA: 0x001794E7 File Offset: 0x001776E7
			// (set) Token: 0x06004C2B RID: 19499 RVA: 0x001794EF File Offset: 0x001776EF
			public ExplainedNumber.StatExplainer.ExplanationLine? BaseLine { get; private set; }

			// Token: 0x17000ED8 RID: 3800
			// (get) Token: 0x06004C2C RID: 19500 RVA: 0x001794F8 File Offset: 0x001776F8
			// (set) Token: 0x06004C2D RID: 19501 RVA: 0x00179500 File Offset: 0x00177700
			public ExplainedNumber.StatExplainer.ExplanationLine? LimitMinLine { get; private set; }

			// Token: 0x17000ED9 RID: 3801
			// (get) Token: 0x06004C2E RID: 19502 RVA: 0x00179509 File Offset: 0x00177709
			// (set) Token: 0x06004C2F RID: 19503 RVA: 0x00179511 File Offset: 0x00177711
			public ExplainedNumber.StatExplainer.ExplanationLine? LimitMaxLine { get; private set; }

			// Token: 0x06004C30 RID: 19504 RVA: 0x0017951C File Offset: 0x0017771C
			[return: TupleElementNames(new string[] { "name", "number" })]
			public List<ValueTuple<string, float>> GetLines(float baseNumber, float unclampedResultNumber, TextObject overrideBaseLineText = null, TextObject overrideMaximumLineText = null, TextObject overrideMinimumLineText = null)
			{
				List<ValueTuple<string, float>> list = new List<ValueTuple<string, float>>();
				if (this.BaseLine != null)
				{
					list.Add(new ValueTuple<string, float>((overrideBaseLineText != null) ? overrideBaseLineText.ToString() : this.BaseLine.Value.Name, this.BaseLine.Value.Number));
				}
				foreach (ExplainedNumber.StatExplainer.ExplanationLine explanationLine in this.Lines)
				{
					float num = explanationLine.Number;
					if (explanationLine.OperationType == ExplainedNumber.StatExplainer.OperationType.Multiply)
					{
						num = baseNumber * num * 0.01f;
					}
					list.Add(new ValueTuple<string, float>(explanationLine.Name, num));
				}
				if (this.LimitMinLine != null && this.LimitMinLine.Value.Number > unclampedResultNumber)
				{
					list.Add(new ValueTuple<string, float>((overrideMinimumLineText != null) ? overrideMinimumLineText.ToString() : this.LimitMinLine.Value.Name, this.LimitMinLine.Value.Number - unclampedResultNumber));
				}
				if (this.LimitMaxLine != null && this.LimitMaxLine.Value.Number < unclampedResultNumber)
				{
					list.Add(new ValueTuple<string, float>((overrideMaximumLineText != null) ? overrideMaximumLineText.ToString() : this.LimitMaxLine.Value.Name, this.LimitMaxLine.Value.Number - unclampedResultNumber));
				}
				return list;
			}

			// Token: 0x06004C31 RID: 19505 RVA: 0x001796CC File Offset: 0x001778CC
			public void AddLine(string name, float number, ExplainedNumber.StatExplainer.OperationType opType)
			{
				ExplainedNumber.StatExplainer.ExplanationLine explanationLine = new ExplainedNumber.StatExplainer.ExplanationLine(name, number, opType);
				if (opType == ExplainedNumber.StatExplainer.OperationType.Add || opType == ExplainedNumber.StatExplainer.OperationType.Multiply)
				{
					int num = -1;
					for (int i = 0; i < this.Lines.Count; i++)
					{
						if (this.Lines[i].Name.Equals(name) && this.Lines[i].OperationType == opType)
						{
							num = i;
							break;
						}
					}
					if (num < 0)
					{
						this.Lines.Add(explanationLine);
						return;
					}
					explanationLine = new ExplainedNumber.StatExplainer.ExplanationLine(name, number + this.Lines[num].Number, opType);
					this.Lines[num] = explanationLine;
					return;
				}
				else
				{
					if (opType == ExplainedNumber.StatExplainer.OperationType.Base)
					{
						this.BaseLine = new ExplainedNumber.StatExplainer.ExplanationLine?(explanationLine);
						return;
					}
					if (opType == ExplainedNumber.StatExplainer.OperationType.LimitMin)
					{
						this.LimitMinLine = new ExplainedNumber.StatExplainer.ExplanationLine?(explanationLine);
						return;
					}
					if (opType == ExplainedNumber.StatExplainer.OperationType.LimitMax)
					{
						this.LimitMaxLine = new ExplainedNumber.StatExplainer.ExplanationLine?(explanationLine);
					}
					return;
				}
			}

			// Token: 0x020008A3 RID: 2211
			public enum OperationType
			{
				// Token: 0x04002478 RID: 9336
				Base,
				// Token: 0x04002479 RID: 9337
				Add,
				// Token: 0x0400247A RID: 9338
				Multiply,
				// Token: 0x0400247B RID: 9339
				LimitMin,
				// Token: 0x0400247C RID: 9340
				LimitMax
			}

			// Token: 0x020008A4 RID: 2212
			public readonly struct ExplanationLine
			{
				// Token: 0x060067D7 RID: 26583 RVA: 0x001C411B File Offset: 0x001C231B
				public ExplanationLine(string name, float number, ExplainedNumber.StatExplainer.OperationType operationType)
				{
					this.Name = name;
					this.Number = number;
					this.OperationType = operationType;
				}

				// Token: 0x0400247D RID: 9341
				public readonly float Number;

				// Token: 0x0400247E RID: 9342
				public readonly string Name;

				// Token: 0x0400247F RID: 9343
				public readonly ExplainedNumber.StatExplainer.OperationType OperationType;
			}
		}
	}
}
