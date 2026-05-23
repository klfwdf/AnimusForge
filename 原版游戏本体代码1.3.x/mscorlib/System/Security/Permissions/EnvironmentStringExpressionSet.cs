using System;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x020002DD RID: 733
	[Serializable]
	internal class EnvironmentStringExpressionSet : StringExpressionSet
	{
		// Token: 0x060025B2 RID: 9650 RVA: 0x00089703 File Offset: 0x00087903
		public EnvironmentStringExpressionSet()
			: base(true, null, false)
		{
		}

		// Token: 0x060025B3 RID: 9651 RVA: 0x0008970E File Offset: 0x0008790E
		public EnvironmentStringExpressionSet(string str)
			: base(true, str, false)
		{
		}

		// Token: 0x060025B4 RID: 9652 RVA: 0x00089719 File Offset: 0x00087919
		protected override StringExpressionSet CreateNewEmpty()
		{
			return new EnvironmentStringExpressionSet();
		}

		// Token: 0x060025B5 RID: 9653 RVA: 0x00089720 File Offset: 0x00087920
		protected override bool StringSubsetString(string left, string right, bool ignoreCase)
		{
			if (!ignoreCase)
			{
				return string.Compare(left, right, StringComparison.Ordinal) == 0;
			}
			return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) == 0;
		}

		// Token: 0x060025B6 RID: 9654 RVA: 0x0008973C File Offset: 0x0008793C
		protected override string ProcessWholeString(string str)
		{
			return str;
		}

		// Token: 0x060025B7 RID: 9655 RVA: 0x0008973F File Offset: 0x0008793F
		protected override string ProcessSingleString(string str)
		{
			return str;
		}

		// Token: 0x060025B8 RID: 9656 RVA: 0x00089742 File Offset: 0x00087942
		[SecuritySafeCritical]
		public override string ToString()
		{
			return base.UnsafeToString();
		}
	}
}
