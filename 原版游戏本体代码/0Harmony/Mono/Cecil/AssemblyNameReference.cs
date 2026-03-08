using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Mono.Cecil
{
	// Token: 0x020001EC RID: 492
	internal class AssemblyNameReference : IMetadataScope, IMetadataTokenProvider
	{
		// Token: 0x1700023D RID: 573
		// (get) Token: 0x06000982 RID: 2434 RVA: 0x0001EC34 File Offset: 0x0001CE34
		// (set) Token: 0x06000983 RID: 2435 RVA: 0x0001EC3C File Offset: 0x0001CE3C
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
				this.full_name = null;
			}
		}

		// Token: 0x1700023E RID: 574
		// (get) Token: 0x06000984 RID: 2436 RVA: 0x0001EC4C File Offset: 0x0001CE4C
		// (set) Token: 0x06000985 RID: 2437 RVA: 0x0001EC54 File Offset: 0x0001CE54
		public string Culture
		{
			get
			{
				return this.culture;
			}
			set
			{
				this.culture = value;
				this.full_name = null;
			}
		}

		// Token: 0x1700023F RID: 575
		// (get) Token: 0x06000986 RID: 2438 RVA: 0x0001EC64 File Offset: 0x0001CE64
		// (set) Token: 0x06000987 RID: 2439 RVA: 0x0001EC6C File Offset: 0x0001CE6C
		public Version Version
		{
			get
			{
				return this.version;
			}
			set
			{
				this.version = Mixin.CheckVersion(value);
				this.full_name = null;
			}
		}

		// Token: 0x17000240 RID: 576
		// (get) Token: 0x06000988 RID: 2440 RVA: 0x0001EC81 File Offset: 0x0001CE81
		// (set) Token: 0x06000989 RID: 2441 RVA: 0x0001EC89 File Offset: 0x0001CE89
		public AssemblyAttributes Attributes
		{
			get
			{
				return (AssemblyAttributes)this.attributes;
			}
			set
			{
				this.attributes = (uint)value;
			}
		}

		// Token: 0x17000241 RID: 577
		// (get) Token: 0x0600098A RID: 2442 RVA: 0x0001EC92 File Offset: 0x0001CE92
		// (set) Token: 0x0600098B RID: 2443 RVA: 0x0001ECA0 File Offset: 0x0001CEA0
		public bool HasPublicKey
		{
			get
			{
				return this.attributes.GetAttributes(1U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1U, value);
			}
		}

		// Token: 0x17000242 RID: 578
		// (get) Token: 0x0600098C RID: 2444 RVA: 0x0001ECB5 File Offset: 0x0001CEB5
		// (set) Token: 0x0600098D RID: 2445 RVA: 0x0001ECC3 File Offset: 0x0001CEC3
		public bool IsSideBySideCompatible
		{
			get
			{
				return this.attributes.GetAttributes(0U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(0U, value);
			}
		}

		// Token: 0x17000243 RID: 579
		// (get) Token: 0x0600098E RID: 2446 RVA: 0x0001ECD8 File Offset: 0x0001CED8
		// (set) Token: 0x0600098F RID: 2447 RVA: 0x0001ECEA File Offset: 0x0001CEEA
		public bool IsRetargetable
		{
			get
			{
				return this.attributes.GetAttributes(256U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(256U, value);
			}
		}

		// Token: 0x17000244 RID: 580
		// (get) Token: 0x06000990 RID: 2448 RVA: 0x0001ED03 File Offset: 0x0001CF03
		// (set) Token: 0x06000991 RID: 2449 RVA: 0x0001ED15 File Offset: 0x0001CF15
		public bool IsWindowsRuntime
		{
			get
			{
				return this.attributes.GetAttributes(512U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(512U, value);
			}
		}

		// Token: 0x17000245 RID: 581
		// (get) Token: 0x06000992 RID: 2450 RVA: 0x0001ED2E File Offset: 0x0001CF2E
		// (set) Token: 0x06000993 RID: 2451 RVA: 0x0001ED3F File Offset: 0x0001CF3F
		public byte[] PublicKey
		{
			get
			{
				return this.public_key ?? Empty<byte>.Array;
			}
			set
			{
				this.public_key = value;
				this.HasPublicKey = !this.public_key.IsNullOrEmpty<byte>();
				this.public_key_token = null;
				this.full_name = null;
			}
		}

		// Token: 0x17000246 RID: 582
		// (get) Token: 0x06000994 RID: 2452 RVA: 0x0001ED6C File Offset: 0x0001CF6C
		// (set) Token: 0x06000995 RID: 2453 RVA: 0x0001EDCF File Offset: 0x0001CFCF
		public byte[] PublicKeyToken
		{
			get
			{
				if (this.public_key_token == null && !this.public_key.IsNullOrEmpty<byte>())
				{
					byte[] hash = this.HashPublicKey();
					byte[] local_public_key_token = new byte[8];
					Array.Copy(hash, hash.Length - 8, local_public_key_token, 0, 8);
					Array.Reverse(local_public_key_token, 0, 8);
					Interlocked.CompareExchange<byte[]>(ref this.public_key_token, local_public_key_token, null);
				}
				return this.public_key_token ?? Empty<byte>.Array;
			}
			set
			{
				this.public_key_token = value;
				this.full_name = null;
			}
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x0001EDE0 File Offset: 0x0001CFE0
		private byte[] HashPublicKey()
		{
			HashAlgorithm algorithm;
			if (this.hash_algorithm == AssemblyHashAlgorithm.MD5)
			{
				algorithm = MD5.Create();
			}
			else
			{
				algorithm = SHA1.Create();
			}
			byte[] result;
			using (algorithm)
			{
				result = algorithm.ComputeHash(this.public_key);
			}
			return result;
		}

		// Token: 0x17000247 RID: 583
		// (get) Token: 0x06000997 RID: 2455 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual MetadataScopeType MetadataScopeType
		{
			get
			{
				return MetadataScopeType.AssemblyNameReference;
			}
		}

		// Token: 0x17000248 RID: 584
		// (get) Token: 0x06000998 RID: 2456 RVA: 0x0001EE34 File Offset: 0x0001D034
		public string FullName
		{
			get
			{
				if (this.full_name != null)
				{
					return this.full_name;
				}
				StringBuilder builder = new StringBuilder();
				builder.Append(this.name);
				builder.Append(", ");
				builder.Append("Version=");
				builder.Append(this.version.ToString(4));
				builder.Append(", ");
				builder.Append("Culture=");
				builder.Append(string.IsNullOrEmpty(this.culture) ? "neutral" : this.culture);
				builder.Append(", ");
				builder.Append("PublicKeyToken=");
				byte[] pk_token = this.PublicKeyToken;
				if (!pk_token.IsNullOrEmpty<byte>() && pk_token.Length != 0)
				{
					for (int i = 0; i < pk_token.Length; i++)
					{
						builder.Append(pk_token[i].ToString("x2"));
					}
				}
				else
				{
					builder.Append("null");
				}
				if (this.IsRetargetable)
				{
					builder.Append(", ");
					builder.Append("Retargetable=Yes");
				}
				Interlocked.CompareExchange<string>(ref this.full_name, builder.ToString(), null);
				return this.full_name;
			}
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x0001EF60 File Offset: 0x0001D160
		public static AssemblyNameReference Parse(string fullName)
		{
			if (fullName == null)
			{
				throw new ArgumentNullException("fullName");
			}
			if (fullName.Length == 0)
			{
				throw new ArgumentException("Name can not be empty");
			}
			AssemblyNameReference name = new AssemblyNameReference();
			string[] tokens = fullName.Split(new char[] { ',' });
			for (int i = 0; i < tokens.Length; i++)
			{
				string token = tokens[i].Trim();
				if (i == 0)
				{
					name.Name = token;
				}
				else
				{
					string[] parts = token.Split(new char[] { '=' });
					if (parts.Length != 2)
					{
						throw new ArgumentException("Malformed name");
					}
					string a = parts[0].ToLowerInvariant();
					if (!(a == "version"))
					{
						if (!(a == "culture"))
						{
							if (a == "publickeytoken")
							{
								string pk_token = parts[1];
								if (!(pk_token == "null"))
								{
									name.PublicKeyToken = new byte[pk_token.Length / 2];
									for (int j = 0; j < name.PublicKeyToken.Length; j++)
									{
										name.PublicKeyToken[j] = byte.Parse(pk_token.Substring(j * 2, 2), NumberStyles.HexNumber);
									}
								}
							}
						}
						else
						{
							name.Culture = ((parts[1] == "neutral") ? "" : parts[1]);
						}
					}
					else
					{
						name.Version = new Version(parts[1]);
					}
				}
			}
			return name;
		}

		// Token: 0x17000249 RID: 585
		// (get) Token: 0x0600099A RID: 2458 RVA: 0x0001F0C7 File Offset: 0x0001D2C7
		// (set) Token: 0x0600099B RID: 2459 RVA: 0x0001F0CF File Offset: 0x0001D2CF
		public AssemblyHashAlgorithm HashAlgorithm
		{
			get
			{
				return this.hash_algorithm;
			}
			set
			{
				this.hash_algorithm = value;
			}
		}

		// Token: 0x1700024A RID: 586
		// (get) Token: 0x0600099C RID: 2460 RVA: 0x0001F0D8 File Offset: 0x0001D2D8
		// (set) Token: 0x0600099D RID: 2461 RVA: 0x0001F0E0 File Offset: 0x0001D2E0
		public virtual byte[] Hash
		{
			get
			{
				return this.hash;
			}
			set
			{
				this.hash = value;
			}
		}

		// Token: 0x1700024B RID: 587
		// (get) Token: 0x0600099E RID: 2462 RVA: 0x0001F0E9 File Offset: 0x0001D2E9
		// (set) Token: 0x0600099F RID: 2463 RVA: 0x0001F0F1 File Offset: 0x0001D2F1
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x060009A0 RID: 2464 RVA: 0x0001F0FA File Offset: 0x0001D2FA
		internal AssemblyNameReference()
		{
			this.version = Mixin.ZeroVersion;
			this.token = new MetadataToken(TokenType.AssemblyRef);
		}

		// Token: 0x060009A1 RID: 2465 RVA: 0x0001F11D File Offset: 0x0001D31D
		public AssemblyNameReference(string name, Version version)
		{
			Mixin.CheckName(name);
			this.name = name;
			this.version = Mixin.CheckVersion(version);
			this.hash_algorithm = AssemblyHashAlgorithm.None;
			this.token = new MetadataToken(TokenType.AssemblyRef);
		}

		// Token: 0x060009A2 RID: 2466 RVA: 0x0001F155 File Offset: 0x0001D355
		public override string ToString()
		{
			return this.FullName;
		}

		// Token: 0x04000331 RID: 817
		private string name;

		// Token: 0x04000332 RID: 818
		private string culture;

		// Token: 0x04000333 RID: 819
		private Version version;

		// Token: 0x04000334 RID: 820
		private uint attributes;

		// Token: 0x04000335 RID: 821
		private byte[] public_key;

		// Token: 0x04000336 RID: 822
		private byte[] public_key_token;

		// Token: 0x04000337 RID: 823
		private AssemblyHashAlgorithm hash_algorithm;

		// Token: 0x04000338 RID: 824
		private byte[] hash;

		// Token: 0x04000339 RID: 825
		internal MetadataToken token;

		// Token: 0x0400033A RID: 826
		private string full_name;
	}
}
