using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AA RID: 2474
	public struct OSPlatform : IEquatable<OSPlatform>
	{
		// Token: 0x17001118 RID: 4376
		// (get) Token: 0x060062FB RID: 25339 RVA: 0x0015183C File Offset: 0x0014FA3C
		public static OSPlatform Linux { get; } = new OSPlatform("LINUX");

		// Token: 0x17001119 RID: 4377
		// (get) Token: 0x060062FC RID: 25340 RVA: 0x00151843 File Offset: 0x0014FA43
		public static OSPlatform OSX { get; } = new OSPlatform("OSX");

		// Token: 0x1700111A RID: 4378
		// (get) Token: 0x060062FD RID: 25341 RVA: 0x0015184A File Offset: 0x0014FA4A
		public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

		// Token: 0x060062FE RID: 25342 RVA: 0x00151851 File Offset: 0x0014FA51
		private OSPlatform(string osPlatform)
		{
			if (osPlatform == null)
			{
				throw new ArgumentNullException("osPlatform");
			}
			if (osPlatform.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyValue"), "osPlatform");
			}
			this._osPlatform = osPlatform;
		}

		// Token: 0x060062FF RID: 25343 RVA: 0x00151885 File Offset: 0x0014FA85
		public static OSPlatform Create(string osPlatform)
		{
			return new OSPlatform(osPlatform);
		}

		// Token: 0x06006300 RID: 25344 RVA: 0x0015188D File Offset: 0x0014FA8D
		public bool Equals(OSPlatform other)
		{
			return this.Equals(other._osPlatform);
		}

		// Token: 0x06006301 RID: 25345 RVA: 0x0015189B File Offset: 0x0014FA9B
		internal bool Equals(string other)
		{
			return string.Equals(this._osPlatform, other, StringComparison.Ordinal);
		}

		// Token: 0x06006302 RID: 25346 RVA: 0x001518AA File Offset: 0x0014FAAA
		public override bool Equals(object obj)
		{
			return obj is OSPlatform && this.Equals((OSPlatform)obj);
		}

		// Token: 0x06006303 RID: 25347 RVA: 0x001518C2 File Offset: 0x0014FAC2
		public override int GetHashCode()
		{
			if (this._osPlatform != null)
			{
				return this._osPlatform.GetHashCode();
			}
			return 0;
		}

		// Token: 0x06006304 RID: 25348 RVA: 0x001518D9 File Offset: 0x0014FAD9
		public override string ToString()
		{
			return this._osPlatform ?? string.Empty;
		}

		// Token: 0x06006305 RID: 25349 RVA: 0x001518EA File Offset: 0x0014FAEA
		public static bool operator ==(OSPlatform left, OSPlatform right)
		{
			return left.Equals(right);
		}

		// Token: 0x06006306 RID: 25350 RVA: 0x001518F4 File Offset: 0x0014FAF4
		public static bool operator !=(OSPlatform left, OSPlatform right)
		{
			return !(left == right);
		}

		// Token: 0x04002CAB RID: 11435
		private readonly string _osPlatform;
	}
}
