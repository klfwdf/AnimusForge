using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000612 RID: 1554
	[ComVisible(true)]
	public class ExceptionHandlingClause
	{
		// Token: 0x060047FC RID: 18428 RVA: 0x00105EAF File Offset: 0x001040AF
		protected ExceptionHandlingClause()
		{
		}

		// Token: 0x17000B16 RID: 2838
		// (get) Token: 0x060047FD RID: 18429 RVA: 0x00105EB7 File Offset: 0x001040B7
		public virtual ExceptionHandlingClauseOptions Flags
		{
			get
			{
				return this.m_flags;
			}
		}

		// Token: 0x17000B17 RID: 2839
		// (get) Token: 0x060047FE RID: 18430 RVA: 0x00105EBF File Offset: 0x001040BF
		public virtual int TryOffset
		{
			get
			{
				return this.m_tryOffset;
			}
		}

		// Token: 0x17000B18 RID: 2840
		// (get) Token: 0x060047FF RID: 18431 RVA: 0x00105EC7 File Offset: 0x001040C7
		public virtual int TryLength
		{
			get
			{
				return this.m_tryLength;
			}
		}

		// Token: 0x17000B19 RID: 2841
		// (get) Token: 0x06004800 RID: 18432 RVA: 0x00105ECF File Offset: 0x001040CF
		public virtual int HandlerOffset
		{
			get
			{
				return this.m_handlerOffset;
			}
		}

		// Token: 0x17000B1A RID: 2842
		// (get) Token: 0x06004801 RID: 18433 RVA: 0x00105ED7 File Offset: 0x001040D7
		public virtual int HandlerLength
		{
			get
			{
				return this.m_handlerLength;
			}
		}

		// Token: 0x17000B1B RID: 2843
		// (get) Token: 0x06004802 RID: 18434 RVA: 0x00105EDF File Offset: 0x001040DF
		public virtual int FilterOffset
		{
			get
			{
				if (this.m_flags != ExceptionHandlingClauseOptions.Filter)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Arg_EHClauseNotFilter"));
				}
				return this.m_filterOffset;
			}
		}

		// Token: 0x17000B1C RID: 2844
		// (get) Token: 0x06004803 RID: 18435 RVA: 0x00105F00 File Offset: 0x00104100
		public virtual Type CatchType
		{
			get
			{
				if (this.m_flags != ExceptionHandlingClauseOptions.Clause)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Arg_EHClauseNotClause"));
				}
				Type result = null;
				if (!MetadataToken.IsNullToken(this.m_catchMetadataToken))
				{
					Type declaringType = this.m_methodBody.m_methodBase.DeclaringType;
					Module module = ((declaringType == null) ? this.m_methodBody.m_methodBase.Module : declaringType.Module);
					result = module.ResolveType(this.m_catchMetadataToken, (declaringType == null) ? null : declaringType.GetGenericArguments(), (this.m_methodBody.m_methodBase is MethodInfo) ? this.m_methodBody.m_methodBase.GetGenericArguments() : null);
				}
				return result;
			}
		}

		// Token: 0x06004804 RID: 18436 RVA: 0x00105FAC File Offset: 0x001041AC
		public override string ToString()
		{
			if (this.Flags == ExceptionHandlingClauseOptions.Clause)
			{
				return string.Format(CultureInfo.CurrentUICulture, "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, CatchType={5}", new object[] { this.Flags, this.TryOffset, this.TryLength, this.HandlerOffset, this.HandlerLength, this.CatchType });
			}
			if (this.Flags == ExceptionHandlingClauseOptions.Filter)
			{
				return string.Format(CultureInfo.CurrentUICulture, "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, FilterOffset={5}", new object[] { this.Flags, this.TryOffset, this.TryLength, this.HandlerOffset, this.HandlerLength, this.FilterOffset });
			}
			return string.Format(CultureInfo.CurrentUICulture, "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}", new object[] { this.Flags, this.TryOffset, this.TryLength, this.HandlerOffset, this.HandlerLength });
		}

		// Token: 0x04001DCF RID: 7631
		private MethodBody m_methodBody;

		// Token: 0x04001DD0 RID: 7632
		private ExceptionHandlingClauseOptions m_flags;

		// Token: 0x04001DD1 RID: 7633
		private int m_tryOffset;

		// Token: 0x04001DD2 RID: 7634
		private int m_tryLength;

		// Token: 0x04001DD3 RID: 7635
		private int m_handlerOffset;

		// Token: 0x04001DD4 RID: 7636
		private int m_handlerLength;

		// Token: 0x04001DD5 RID: 7637
		private int m_catchMetadataToken;

		// Token: 0x04001DD6 RID: 7638
		private int m_filterOffset;
	}
}
