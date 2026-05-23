using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MCM.UI.ButterLib
{
	// Token: 0x02000033 RID: 51
	[NullableContext(1)]
	[Nullable(0)]
	internal class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, object>>
	{
		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00007D75 File Offset: 0x00005F75
		[Nullable(2)]
		internal LogValuesFormatter Formatter
		{
			[NullableContext(2)]
			get
			{
				return this._formatter;
			}
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00007D80 File Offset: 0x00005F80
		[NullableContext(2)]
		public FormattedLogValues(string format, [Nullable(new byte[] { 2, 1 })] params object[] values)
		{
			if ((values == null || values.Length != 0) && format != null)
			{
				if (FormattedLogValues._count >= 1024)
				{
					if (!FormattedLogValues._formatters.TryGetValue(format, out this._formatter))
					{
						this._formatter = new LogValuesFormatter(format);
					}
				}
				else
				{
					this._formatter = FormattedLogValues._formatters.GetOrAdd(format, delegate(string f)
					{
						Interlocked.Increment(ref FormattedLogValues._count);
						return new LogValuesFormatter(f);
					});
				}
			}
			this._originalMessage = format ?? "[null]";
			this._values = values;
		}

		// Token: 0x1700008E RID: 142
		[Nullable(new byte[] { 0, 1, 1 })]
		public KeyValuePair<string, object> this[int index]
		{
			[return: Nullable(new byte[] { 0, 1, 1 })]
			get
			{
				if (index < 0 || index >= this.Count)
				{
					throw new IndexOutOfRangeException("index");
				}
				if (index != this.Count - 1)
				{
					return this._formatter.GetValue(this._values, index);
				}
				return new KeyValuePair<string, object>("{OriginalFormat}", this._originalMessage);
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00007E6B File Offset: 0x0000606B
		public int Count
		{
			get
			{
				if (this._formatter != null)
				{
					return this._formatter.ValueNames.Count + 1;
				}
				return 1;
			}
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00007E89 File Offset: 0x00006089
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			FormattedLogValues.<GetEnumerator>d__14 <GetEnumerator>d__ = new FormattedLogValues.<GetEnumerator>d__14(0);
			<GetEnumerator>d__.<>4__this = this;
			return <GetEnumerator>d__;
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x00007E98 File Offset: 0x00006098
		public override string ToString()
		{
			if (this._formatter != null)
			{
				return this._formatter.Format(this._values);
			}
			return this._originalMessage;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00007EBA File Offset: 0x000060BA
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x04000074 RID: 116
		internal const int MaxCachedFormatters = 1024;

		// Token: 0x04000075 RID: 117
		private const string NullFormat = "[null]";

		// Token: 0x04000076 RID: 118
		private static int _count;

		// Token: 0x04000077 RID: 119
		private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();

		// Token: 0x04000078 RID: 120
		[Nullable(2)]
		private readonly LogValuesFormatter _formatter;

		// Token: 0x04000079 RID: 121
		[Nullable(new byte[] { 2, 1 })]
		private readonly object[] _values;

		// Token: 0x0400007A RID: 122
		private readonly string _originalMessage;
	}
}
