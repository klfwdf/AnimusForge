using System;
using System.Globalization;

namespace System
{
	// Token: 0x020000E5 RID: 229
	[__DynamicallyInvokable]
	public abstract class FormattableString : IFormattable
	{
		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x06000E88 RID: 3720
		[__DynamicallyInvokable]
		public abstract string Format
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06000E89 RID: 3721
		[__DynamicallyInvokable]
		public abstract object[] GetArguments();

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x06000E8A RID: 3722
		[__DynamicallyInvokable]
		public abstract int ArgumentCount
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06000E8B RID: 3723
		[__DynamicallyInvokable]
		public abstract object GetArgument(int index);

		// Token: 0x06000E8C RID: 3724
		[__DynamicallyInvokable]
		public abstract string ToString(IFormatProvider formatProvider);

		// Token: 0x06000E8D RID: 3725 RVA: 0x0002CD0F File Offset: 0x0002AF0F
		[__DynamicallyInvokable]
		string IFormattable.ToString(string ignored, IFormatProvider formatProvider)
		{
			return this.ToString(formatProvider);
		}

		// Token: 0x06000E8E RID: 3726 RVA: 0x0002CD18 File Offset: 0x0002AF18
		[__DynamicallyInvokable]
		public static string Invariant(FormattableString formattable)
		{
			if (formattable == null)
			{
				throw new ArgumentNullException("formattable");
			}
			return formattable.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x06000E8F RID: 3727 RVA: 0x0002CD33 File Offset: 0x0002AF33
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E90 RID: 3728 RVA: 0x0002CD40 File Offset: 0x0002AF40
		[__DynamicallyInvokable]
		protected FormattableString()
		{
		}
	}
}
