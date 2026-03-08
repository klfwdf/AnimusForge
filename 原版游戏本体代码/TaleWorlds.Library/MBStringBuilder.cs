using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace TaleWorlds.Library
{
	// Token: 0x0200001F RID: 31
	public struct MBStringBuilder
	{
		// Token: 0x0600009F RID: 159 RVA: 0x00003DEF File Offset: 0x00001FEF
		public void Initialize(int capacity = 16, [CallerMemberName] string callerMemberName = "")
		{
			this._cachedStringBuilder = MBStringBuilder.CachedStringBuilder.Acquire(capacity);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00003DFD File Offset: 0x00001FFD
		public string ToStringAndRelease()
		{
			string result = this._cachedStringBuilder.ToString();
			this.Release();
			return result;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00003E10 File Offset: 0x00002010
		public void Release()
		{
			MBStringBuilder.CachedStringBuilder.Release(this._cachedStringBuilder);
			this._cachedStringBuilder = null;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00003E24 File Offset: 0x00002024
		public MBStringBuilder Append(char value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00003E39 File Offset: 0x00002039
		public MBStringBuilder Append(int value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00003E4E File Offset: 0x0000204E
		public MBStringBuilder Append(uint value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00003E63 File Offset: 0x00002063
		public MBStringBuilder Append(float value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00003E78 File Offset: 0x00002078
		public MBStringBuilder Append(double value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00003E8D File Offset: 0x0000208D
		public MBStringBuilder Append<T>(T value)
		{
			this._cachedStringBuilder.Append(value);
			return this;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00003EA7 File Offset: 0x000020A7
		public MBStringBuilder AppendLine()
		{
			this._cachedStringBuilder.AppendLine();
			return this;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00003EBB File Offset: 0x000020BB
		public MBStringBuilder AppendLine<T>(T value)
		{
			this.Append<T>(value);
			this.AppendLine();
			return this;
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x060000AA RID: 170 RVA: 0x00003ED2 File Offset: 0x000020D2
		public int Length
		{
			get
			{
				return this._cachedStringBuilder.Length;
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00003EDF File Offset: 0x000020DF
		public override string ToString()
		{
			Debug.FailedAssert("Don't use this. Use ToStringAndRelease instead!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\CachedStringBuilder.cs", "ToString", 190);
			return null;
		}

		// Token: 0x04000064 RID: 100
		private StringBuilder _cachedStringBuilder;

		// Token: 0x020000C8 RID: 200
		private static class CachedStringBuilder
		{
			// Token: 0x06000748 RID: 1864 RVA: 0x00018417 File Offset: 0x00016617
			public static StringBuilder Acquire(int capacity = 16)
			{
				if (capacity <= 4096 && MBStringBuilder.CachedStringBuilder._cachedStringBuilder != null)
				{
					StringBuilder cachedStringBuilder = MBStringBuilder.CachedStringBuilder._cachedStringBuilder;
					MBStringBuilder.CachedStringBuilder._cachedStringBuilder = null;
					cachedStringBuilder.EnsureCapacity(capacity);
					return cachedStringBuilder;
				}
				return new StringBuilder(capacity);
			}

			// Token: 0x06000749 RID: 1865 RVA: 0x00018442 File Offset: 0x00016642
			public static void Release(StringBuilder sb)
			{
				if (sb.Capacity <= 4096)
				{
					MBStringBuilder.CachedStringBuilder._cachedStringBuilder = sb;
					MBStringBuilder.CachedStringBuilder._cachedStringBuilder.Clear();
				}
			}

			// Token: 0x0600074A RID: 1866 RVA: 0x00018462 File Offset: 0x00016662
			public static string GetStringAndReleaseBuilder(StringBuilder sb)
			{
				string result = sb.ToString();
				MBStringBuilder.CachedStringBuilder.Release(sb);
				return result;
			}

			// Token: 0x04000252 RID: 594
			private const int MaxBuilderSize = 4096;

			// Token: 0x04000253 RID: 595
			[ThreadStatic]
			private static StringBuilder _cachedStringBuilder;
		}
	}
}
