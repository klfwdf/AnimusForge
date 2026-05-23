using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008FB RID: 2299
	[__DynamicallyInvokable]
	public static class FormattableStringFactory
	{
		// Token: 0x06005E50 RID: 24144 RVA: 0x0014B5A6 File Offset: 0x001497A6
		[__DynamicallyInvokable]
		public static FormattableString Create(string format, params object[] arguments)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			if (arguments == null)
			{
				throw new ArgumentNullException("arguments");
			}
			return new FormattableStringFactory.ConcreteFormattableString(format, arguments);
		}

		// Token: 0x02000C97 RID: 3223
		private sealed class ConcreteFormattableString : FormattableString
		{
			// Token: 0x06007111 RID: 28945 RVA: 0x00185400 File Offset: 0x00183600
			internal ConcreteFormattableString(string format, object[] arguments)
			{
				this._format = format;
				this._arguments = arguments;
			}

			// Token: 0x17001363 RID: 4963
			// (get) Token: 0x06007112 RID: 28946 RVA: 0x00185416 File Offset: 0x00183616
			public override string Format
			{
				get
				{
					return this._format;
				}
			}

			// Token: 0x06007113 RID: 28947 RVA: 0x0018541E File Offset: 0x0018361E
			public override object[] GetArguments()
			{
				return this._arguments;
			}

			// Token: 0x17001364 RID: 4964
			// (get) Token: 0x06007114 RID: 28948 RVA: 0x00185426 File Offset: 0x00183626
			public override int ArgumentCount
			{
				get
				{
					return this._arguments.Length;
				}
			}

			// Token: 0x06007115 RID: 28949 RVA: 0x00185430 File Offset: 0x00183630
			public override object GetArgument(int index)
			{
				return this._arguments[index];
			}

			// Token: 0x06007116 RID: 28950 RVA: 0x0018543A File Offset: 0x0018363A
			public override string ToString(IFormatProvider formatProvider)
			{
				return string.Format(formatProvider, this._format, this._arguments);
			}

			// Token: 0x04003855 RID: 14421
			private readonly string _format;

			// Token: 0x04003856 RID: 14422
			private readonly object[] _arguments;
		}
	}
}
