using System;

namespace System.Security.Cryptography
{
	// Token: 0x0200026A RID: 618
	public struct HashAlgorithmName : IEquatable<HashAlgorithmName>
	{
		// Token: 0x17000426 RID: 1062
		// (get) Token: 0x060021E9 RID: 8681 RVA: 0x00078298 File Offset: 0x00076498
		public static HashAlgorithmName MD5
		{
			get
			{
				return new HashAlgorithmName("MD5");
			}
		}

		// Token: 0x17000427 RID: 1063
		// (get) Token: 0x060021EA RID: 8682 RVA: 0x000782A4 File Offset: 0x000764A4
		public static HashAlgorithmName SHA1
		{
			get
			{
				return new HashAlgorithmName("SHA1");
			}
		}

		// Token: 0x17000428 RID: 1064
		// (get) Token: 0x060021EB RID: 8683 RVA: 0x000782B0 File Offset: 0x000764B0
		public static HashAlgorithmName SHA256
		{
			get
			{
				return new HashAlgorithmName("SHA256");
			}
		}

		// Token: 0x17000429 RID: 1065
		// (get) Token: 0x060021EC RID: 8684 RVA: 0x000782BC File Offset: 0x000764BC
		public static HashAlgorithmName SHA384
		{
			get
			{
				return new HashAlgorithmName("SHA384");
			}
		}

		// Token: 0x1700042A RID: 1066
		// (get) Token: 0x060021ED RID: 8685 RVA: 0x000782C8 File Offset: 0x000764C8
		public static HashAlgorithmName SHA512
		{
			get
			{
				return new HashAlgorithmName("SHA512");
			}
		}

		// Token: 0x060021EE RID: 8686 RVA: 0x000782D4 File Offset: 0x000764D4
		public HashAlgorithmName(string name)
		{
			this._name = name;
		}

		// Token: 0x1700042B RID: 1067
		// (get) Token: 0x060021EF RID: 8687 RVA: 0x000782DD File Offset: 0x000764DD
		public string Name
		{
			get
			{
				return this._name;
			}
		}

		// Token: 0x060021F0 RID: 8688 RVA: 0x000782E5 File Offset: 0x000764E5
		public override string ToString()
		{
			return this._name ?? string.Empty;
		}

		// Token: 0x060021F1 RID: 8689 RVA: 0x000782F6 File Offset: 0x000764F6
		public override bool Equals(object obj)
		{
			return obj is HashAlgorithmName && this.Equals((HashAlgorithmName)obj);
		}

		// Token: 0x060021F2 RID: 8690 RVA: 0x0007830E File Offset: 0x0007650E
		public bool Equals(HashAlgorithmName other)
		{
			return this._name == other._name;
		}

		// Token: 0x060021F3 RID: 8691 RVA: 0x00078321 File Offset: 0x00076521
		public override int GetHashCode()
		{
			if (this._name != null)
			{
				return this._name.GetHashCode();
			}
			return 0;
		}

		// Token: 0x060021F4 RID: 8692 RVA: 0x00078338 File Offset: 0x00076538
		public static bool operator ==(HashAlgorithmName left, HashAlgorithmName right)
		{
			return left.Equals(right);
		}

		// Token: 0x060021F5 RID: 8693 RVA: 0x00078342 File Offset: 0x00076542
		public static bool operator !=(HashAlgorithmName left, HashAlgorithmName right)
		{
			return !(left == right);
		}

		// Token: 0x04000C59 RID: 3161
		private readonly string _name;
	}
}
