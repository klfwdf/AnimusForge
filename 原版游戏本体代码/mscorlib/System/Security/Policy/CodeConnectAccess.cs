using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x0200035E RID: 862
	[ComVisible(true)]
	[Serializable]
	public class CodeConnectAccess
	{
		// Token: 0x06002A8B RID: 10891 RVA: 0x0009D315 File Offset: 0x0009B515
		public CodeConnectAccess(string allowScheme, int allowPort)
		{
			if (!CodeConnectAccess.IsValidScheme(allowScheme))
			{
				throw new ArgumentOutOfRangeException("allowScheme");
			}
			this.SetCodeConnectAccess(allowScheme.ToLower(CultureInfo.InvariantCulture), allowPort);
		}

		// Token: 0x06002A8C RID: 10892 RVA: 0x0009D344 File Offset: 0x0009B544
		public static CodeConnectAccess CreateOriginSchemeAccess(int allowPort)
		{
			CodeConnectAccess codeConnectAccess = new CodeConnectAccess();
			codeConnectAccess.SetCodeConnectAccess(CodeConnectAccess.OriginScheme, allowPort);
			return codeConnectAccess;
		}

		// Token: 0x06002A8D RID: 10893 RVA: 0x0009D364 File Offset: 0x0009B564
		public static CodeConnectAccess CreateAnySchemeAccess(int allowPort)
		{
			CodeConnectAccess codeConnectAccess = new CodeConnectAccess();
			codeConnectAccess.SetCodeConnectAccess(CodeConnectAccess.AnyScheme, allowPort);
			return codeConnectAccess;
		}

		// Token: 0x06002A8E RID: 10894 RVA: 0x0009D384 File Offset: 0x0009B584
		private CodeConnectAccess()
		{
		}

		// Token: 0x06002A8F RID: 10895 RVA: 0x0009D38C File Offset: 0x0009B58C
		private void SetCodeConnectAccess(string lowerCaseScheme, int allowPort)
		{
			this._LowerCaseScheme = lowerCaseScheme;
			if (allowPort == CodeConnectAccess.DefaultPort)
			{
				this._LowerCasePort = "$default";
			}
			else if (allowPort == CodeConnectAccess.OriginPort)
			{
				this._LowerCasePort = "$origin";
			}
			else
			{
				if (allowPort < 0 || allowPort > 65535)
				{
					throw new ArgumentOutOfRangeException("allowPort");
				}
				this._LowerCasePort = allowPort.ToString(CultureInfo.InvariantCulture);
			}
			this._IntPort = allowPort;
		}

		// Token: 0x170005AA RID: 1450
		// (get) Token: 0x06002A90 RID: 10896 RVA: 0x0009D3FA File Offset: 0x0009B5FA
		public string Scheme
		{
			get
			{
				return this._LowerCaseScheme;
			}
		}

		// Token: 0x170005AB RID: 1451
		// (get) Token: 0x06002A91 RID: 10897 RVA: 0x0009D402 File Offset: 0x0009B602
		public int Port
		{
			get
			{
				return this._IntPort;
			}
		}

		// Token: 0x06002A92 RID: 10898 RVA: 0x0009D40C File Offset: 0x0009B60C
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			CodeConnectAccess codeConnectAccess = o as CodeConnectAccess;
			return codeConnectAccess != null && this.Scheme == codeConnectAccess.Scheme && this.Port == codeConnectAccess.Port;
		}

		// Token: 0x06002A93 RID: 10899 RVA: 0x0009D450 File Offset: 0x0009B650
		public override int GetHashCode()
		{
			return this.Scheme.GetHashCode() + this.Port.GetHashCode();
		}

		// Token: 0x06002A94 RID: 10900 RVA: 0x0009D478 File Offset: 0x0009B678
		internal CodeConnectAccess(string allowScheme, string allowPort)
		{
			if (allowScheme == null || allowScheme.Length == 0)
			{
				throw new ArgumentNullException("allowScheme");
			}
			if (allowPort == null || allowPort.Length == 0)
			{
				throw new ArgumentNullException("allowPort");
			}
			this._LowerCaseScheme = allowScheme.ToLower(CultureInfo.InvariantCulture);
			if (this._LowerCaseScheme == CodeConnectAccess.OriginScheme)
			{
				this._LowerCaseScheme = CodeConnectAccess.OriginScheme;
			}
			else if (this._LowerCaseScheme == CodeConnectAccess.AnyScheme)
			{
				this._LowerCaseScheme = CodeConnectAccess.AnyScheme;
			}
			else if (!CodeConnectAccess.IsValidScheme(this._LowerCaseScheme))
			{
				throw new ArgumentOutOfRangeException("allowScheme");
			}
			this._LowerCasePort = allowPort.ToLower(CultureInfo.InvariantCulture);
			if (this._LowerCasePort == "$default")
			{
				this._IntPort = CodeConnectAccess.DefaultPort;
				return;
			}
			if (this._LowerCasePort == "$origin")
			{
				this._IntPort = CodeConnectAccess.OriginPort;
				return;
			}
			this._IntPort = int.Parse(allowPort, CultureInfo.InvariantCulture);
			if (this._IntPort < 0 || this._IntPort > 65535)
			{
				throw new ArgumentOutOfRangeException("allowPort");
			}
			this._LowerCasePort = this._IntPort.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x170005AC RID: 1452
		// (get) Token: 0x06002A95 RID: 10901 RVA: 0x0009D5B3 File Offset: 0x0009B7B3
		internal bool IsOriginScheme
		{
			get
			{
				return this._LowerCaseScheme == CodeConnectAccess.OriginScheme;
			}
		}

		// Token: 0x170005AD RID: 1453
		// (get) Token: 0x06002A96 RID: 10902 RVA: 0x0009D5C2 File Offset: 0x0009B7C2
		internal bool IsAnyScheme
		{
			get
			{
				return this._LowerCaseScheme == CodeConnectAccess.AnyScheme;
			}
		}

		// Token: 0x170005AE RID: 1454
		// (get) Token: 0x06002A97 RID: 10903 RVA: 0x0009D5D1 File Offset: 0x0009B7D1
		internal bool IsDefaultPort
		{
			get
			{
				return this.Port == CodeConnectAccess.DefaultPort;
			}
		}

		// Token: 0x170005AF RID: 1455
		// (get) Token: 0x06002A98 RID: 10904 RVA: 0x0009D5E0 File Offset: 0x0009B7E0
		internal bool IsOriginPort
		{
			get
			{
				return this.Port == CodeConnectAccess.OriginPort;
			}
		}

		// Token: 0x170005B0 RID: 1456
		// (get) Token: 0x06002A99 RID: 10905 RVA: 0x0009D5EF File Offset: 0x0009B7EF
		internal string StrPort
		{
			get
			{
				return this._LowerCasePort;
			}
		}

		// Token: 0x06002A9A RID: 10906 RVA: 0x0009D5F8 File Offset: 0x0009B7F8
		internal static bool IsValidScheme(string scheme)
		{
			if (scheme == null || scheme.Length == 0 || !CodeConnectAccess.IsAsciiLetter(scheme[0]))
			{
				return false;
			}
			for (int i = scheme.Length - 1; i > 0; i--)
			{
				if (!CodeConnectAccess.IsAsciiLetterOrDigit(scheme[i]) && scheme[i] != '+' && scheme[i] != '-' && scheme[i] != '.')
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002A9B RID: 10907 RVA: 0x0009D665 File Offset: 0x0009B865
		private static bool IsAsciiLetterOrDigit(char character)
		{
			return CodeConnectAccess.IsAsciiLetter(character) || (character >= '0' && character <= '9');
		}

		// Token: 0x06002A9C RID: 10908 RVA: 0x0009D680 File Offset: 0x0009B880
		private static bool IsAsciiLetter(char character)
		{
			return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
		}

		// Token: 0x0400114E RID: 4430
		private string _LowerCaseScheme;

		// Token: 0x0400114F RID: 4431
		private string _LowerCasePort;

		// Token: 0x04001150 RID: 4432
		private int _IntPort;

		// Token: 0x04001151 RID: 4433
		private const string DefaultStr = "$default";

		// Token: 0x04001152 RID: 4434
		private const string OriginStr = "$origin";

		// Token: 0x04001153 RID: 4435
		internal const int NoPort = -1;

		// Token: 0x04001154 RID: 4436
		internal const int AnyPort = -2;

		// Token: 0x04001155 RID: 4437
		public static readonly int DefaultPort = -3;

		// Token: 0x04001156 RID: 4438
		public static readonly int OriginPort = -4;

		// Token: 0x04001157 RID: 4439
		public static readonly string OriginScheme = "$origin";

		// Token: 0x04001158 RID: 4440
		public static readonly string AnyScheme = "*";
	}
}
