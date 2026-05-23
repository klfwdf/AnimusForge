using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D2 RID: 210
	internal class ArraySliceFilter : PathFilter
	{
		// Token: 0x1700020D RID: 525
		// (get) Token: 0x06000BF8 RID: 3064 RVA: 0x0002F811 File Offset: 0x0002DA11
		// (set) Token: 0x06000BF9 RID: 3065 RVA: 0x0002F819 File Offset: 0x0002DA19
		public int? Start { get; set; }

		// Token: 0x1700020E RID: 526
		// (get) Token: 0x06000BFA RID: 3066 RVA: 0x0002F822 File Offset: 0x0002DA22
		// (set) Token: 0x06000BFB RID: 3067 RVA: 0x0002F82A File Offset: 0x0002DA2A
		public int? End { get; set; }

		// Token: 0x1700020F RID: 527
		// (get) Token: 0x06000BFC RID: 3068 RVA: 0x0002F833 File Offset: 0x0002DA33
		// (set) Token: 0x06000BFD RID: 3069 RVA: 0x0002F83B File Offset: 0x0002DA3B
		public int? Step { get; set; }

		// Token: 0x06000BFE RID: 3070 RVA: 0x0002F844 File Offset: 0x0002DA44
		[NullableContext(1)]
		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, [Nullable(2)] JsonSelectSettings settings)
		{
			int? num = this.Step;
			int num2 = 0;
			if ((num.GetValueOrDefault() == num2) & (num != null))
			{
				throw new JsonException("Step cannot be zero.");
			}
			foreach (JToken jtoken in current)
			{
				JArray a = jtoken as JArray;
				if (a != null)
				{
					int stepCount = this.Step ?? 1;
					int num3 = this.Start ?? ((stepCount > 0) ? 0 : (a.Count - 1));
					int stopIndex = this.End ?? ((stepCount > 0) ? a.Count : (-1));
					num = this.Start;
					num2 = 0;
					if ((num.GetValueOrDefault() < num2) & (num != null))
					{
						num3 = a.Count + num3;
					}
					num = this.End;
					num2 = 0;
					if ((num.GetValueOrDefault() < num2) & (num != null))
					{
						stopIndex = a.Count + stopIndex;
					}
					num3 = Math.Max(num3, (stepCount > 0) ? 0 : int.MinValue);
					num3 = Math.Min(num3, (stepCount > 0) ? a.Count : (a.Count - 1));
					stopIndex = Math.Max(stopIndex, -1);
					stopIndex = Math.Min(stopIndex, a.Count);
					bool positiveStep = stepCount > 0;
					if (this.IsValid(num3, stopIndex, positiveStep))
					{
						int i = num3;
						while (this.IsValid(i, stopIndex, positiveStep))
						{
							yield return a[i];
							i += stepCount;
						}
					}
					else if (settings != null && settings.ErrorWhenNoMatch)
					{
						throw new JsonException("Array slice of {0} to {1} returned no results.".FormatWith(CultureInfo.InvariantCulture, (this.Start != null) ? this.Start.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) : "*", (this.End != null) ? this.End.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) : "*"));
					}
				}
				else if (settings != null && settings.ErrorWhenNoMatch)
				{
					throw new JsonException("Array slice is not valid on {0}.".FormatWith(CultureInfo.InvariantCulture, jtoken.GetType().Name));
				}
				a = null;
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000BFF RID: 3071 RVA: 0x0002F862 File Offset: 0x0002DA62
		private bool IsValid(int index, int stopIndex, bool positiveStep)
		{
			if (positiveStep)
			{
				return index < stopIndex;
			}
			return index > stopIndex;
		}
	}
}
