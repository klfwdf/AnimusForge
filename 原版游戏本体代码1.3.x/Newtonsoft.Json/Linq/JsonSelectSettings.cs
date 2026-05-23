using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies the settings used when selecting JSON.
	/// </summary>
	// Token: 0x020000C6 RID: 198
	public class JsonSelectSettings
	{
		/// <summary>
		/// Gets or sets a timeout that will be used when executing regular expressions.
		/// </summary>
		/// <value>The timeout that will be used when executing regular expressions.</value>
		// Token: 0x170001F3 RID: 499
		// (get) Token: 0x06000AD1 RID: 2769 RVA: 0x0002B3C0 File Offset: 0x000295C0
		// (set) Token: 0x06000AD2 RID: 2770 RVA: 0x0002B3C8 File Offset: 0x000295C8
		public TimeSpan? RegexMatchTimeout { get; set; }

		/// <summary>
		/// Gets or sets a flag that indicates whether an error should be thrown if
		/// no tokens are found when evaluating part of the expression.
		/// </summary>
		/// <value>
		/// A flag that indicates whether an error should be thrown if
		/// no tokens are found when evaluating part of the expression.
		/// </value>
		// Token: 0x170001F4 RID: 500
		// (get) Token: 0x06000AD3 RID: 2771 RVA: 0x0002B3D1 File Offset: 0x000295D1
		// (set) Token: 0x06000AD4 RID: 2772 RVA: 0x0002B3D9 File Offset: 0x000295D9
		public bool ErrorWhenNoMatch { get; set; }
	}
}
