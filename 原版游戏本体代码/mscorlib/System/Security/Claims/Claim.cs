using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace System.Security.Claims
{
	// Token: 0x0200031C RID: 796
	[Serializable]
	public class Claim
	{
		// Token: 0x06002816 RID: 10262 RVA: 0x00092843 File Offset: 0x00090A43
		public Claim(BinaryReader reader)
			: this(reader, null)
		{
		}

		// Token: 0x06002817 RID: 10263 RVA: 0x0009284D File Offset: 0x00090A4D
		public Claim(BinaryReader reader, ClaimsIdentity subject)
		{
			this.m_propertyLock = new object();
			base..ctor();
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.Initialize(reader, subject);
		}

		// Token: 0x06002818 RID: 10264 RVA: 0x00092876 File Offset: 0x00090A76
		public Claim(string type, string value)
			: this(type, value, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", null)
		{
		}

		// Token: 0x06002819 RID: 10265 RVA: 0x00092890 File Offset: 0x00090A90
		public Claim(string type, string value, string valueType)
			: this(type, value, valueType, "LOCAL AUTHORITY", "LOCAL AUTHORITY", null)
		{
		}

		// Token: 0x0600281A RID: 10266 RVA: 0x000928A6 File Offset: 0x00090AA6
		public Claim(string type, string value, string valueType, string issuer)
			: this(type, value, valueType, issuer, issuer, null)
		{
		}

		// Token: 0x0600281B RID: 10267 RVA: 0x000928B6 File Offset: 0x00090AB6
		public Claim(string type, string value, string valueType, string issuer, string originalIssuer)
			: this(type, value, valueType, issuer, originalIssuer, null)
		{
		}

		// Token: 0x0600281C RID: 10268 RVA: 0x000928C8 File Offset: 0x00090AC8
		public Claim(string type, string value, string valueType, string issuer, string originalIssuer, ClaimsIdentity subject)
			: this(type, value, valueType, issuer, originalIssuer, subject, null, null)
		{
		}

		// Token: 0x0600281D RID: 10269 RVA: 0x000928E8 File Offset: 0x00090AE8
		internal Claim(string type, string value, string valueType, string issuer, string originalIssuer, ClaimsIdentity subject, string propertyKey, string propertyValue)
		{
			this.m_propertyLock = new object();
			base..ctor();
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.m_type = type;
			this.m_value = value;
			if (string.IsNullOrEmpty(valueType))
			{
				this.m_valueType = "http://www.w3.org/2001/XMLSchema#string";
			}
			else
			{
				this.m_valueType = valueType;
			}
			if (string.IsNullOrEmpty(issuer))
			{
				this.m_issuer = "LOCAL AUTHORITY";
			}
			else
			{
				this.m_issuer = issuer;
			}
			if (string.IsNullOrEmpty(originalIssuer))
			{
				this.m_originalIssuer = this.m_issuer;
			}
			else
			{
				this.m_originalIssuer = originalIssuer;
			}
			this.m_subject = subject;
			if (propertyKey != null)
			{
				this.Properties.Add(propertyKey, propertyValue);
			}
		}

		// Token: 0x0600281E RID: 10270 RVA: 0x000929A4 File Offset: 0x00090BA4
		protected Claim(Claim other)
			: this(other, (other == null) ? null : other.m_subject)
		{
		}

		// Token: 0x0600281F RID: 10271 RVA: 0x000929BC File Offset: 0x00090BBC
		protected Claim(Claim other, ClaimsIdentity subject)
		{
			this.m_propertyLock = new object();
			base..ctor();
			if (other == null)
			{
				throw new ArgumentNullException("other");
			}
			this.m_issuer = other.m_issuer;
			this.m_originalIssuer = other.m_originalIssuer;
			this.m_subject = subject;
			this.m_type = other.m_type;
			this.m_value = other.m_value;
			this.m_valueType = other.m_valueType;
			if (other.m_properties != null)
			{
				this.m_properties = new Dictionary<string, string>();
				foreach (string key in other.m_properties.Keys)
				{
					this.m_properties.Add(key, other.m_properties[key]);
				}
			}
			if (other.m_userSerializationData != null)
			{
				this.m_userSerializationData = other.m_userSerializationData.Clone() as byte[];
			}
		}

		// Token: 0x1700051F RID: 1311
		// (get) Token: 0x06002820 RID: 10272 RVA: 0x00092AB8 File Offset: 0x00090CB8
		protected virtual byte[] CustomSerializationData
		{
			get
			{
				return this.m_userSerializationData;
			}
		}

		// Token: 0x17000520 RID: 1312
		// (get) Token: 0x06002821 RID: 10273 RVA: 0x00092AC0 File Offset: 0x00090CC0
		public string Issuer
		{
			get
			{
				return this.m_issuer;
			}
		}

		// Token: 0x06002822 RID: 10274 RVA: 0x00092AC8 File Offset: 0x00090CC8
		[OnDeserialized]
		private void OnDeserializedMethod(StreamingContext context)
		{
			this.m_propertyLock = new object();
		}

		// Token: 0x17000521 RID: 1313
		// (get) Token: 0x06002823 RID: 10275 RVA: 0x00092AD5 File Offset: 0x00090CD5
		public string OriginalIssuer
		{
			get
			{
				return this.m_originalIssuer;
			}
		}

		// Token: 0x17000522 RID: 1314
		// (get) Token: 0x06002824 RID: 10276 RVA: 0x00092AE0 File Offset: 0x00090CE0
		public IDictionary<string, string> Properties
		{
			get
			{
				if (this.m_properties == null)
				{
					object propertyLock = this.m_propertyLock;
					lock (propertyLock)
					{
						if (this.m_properties == null)
						{
							this.m_properties = new Dictionary<string, string>();
						}
					}
				}
				return this.m_properties;
			}
		}

		// Token: 0x17000523 RID: 1315
		// (get) Token: 0x06002825 RID: 10277 RVA: 0x00092B3C File Offset: 0x00090D3C
		// (set) Token: 0x06002826 RID: 10278 RVA: 0x00092B44 File Offset: 0x00090D44
		public ClaimsIdentity Subject
		{
			get
			{
				return this.m_subject;
			}
			internal set
			{
				this.m_subject = value;
			}
		}

		// Token: 0x17000524 RID: 1316
		// (get) Token: 0x06002827 RID: 10279 RVA: 0x00092B4D File Offset: 0x00090D4D
		public string Type
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x17000525 RID: 1317
		// (get) Token: 0x06002828 RID: 10280 RVA: 0x00092B55 File Offset: 0x00090D55
		public string Value
		{
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x17000526 RID: 1318
		// (get) Token: 0x06002829 RID: 10281 RVA: 0x00092B5D File Offset: 0x00090D5D
		public string ValueType
		{
			get
			{
				return this.m_valueType;
			}
		}

		// Token: 0x0600282A RID: 10282 RVA: 0x00092B65 File Offset: 0x00090D65
		public virtual Claim Clone()
		{
			return this.Clone(null);
		}

		// Token: 0x0600282B RID: 10283 RVA: 0x00092B6E File Offset: 0x00090D6E
		public virtual Claim Clone(ClaimsIdentity identity)
		{
			return new Claim(this, identity);
		}

		// Token: 0x0600282C RID: 10284 RVA: 0x00092B78 File Offset: 0x00090D78
		private void Initialize(BinaryReader reader, ClaimsIdentity subject)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.m_subject = subject;
			Claim.SerializationMask serializationMask = (Claim.SerializationMask)reader.ReadInt32();
			int num = 1;
			int num2 = reader.ReadInt32();
			this.m_value = reader.ReadString();
			if ((serializationMask & Claim.SerializationMask.NameClaimType) == Claim.SerializationMask.NameClaimType)
			{
				this.m_type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			}
			else if ((serializationMask & Claim.SerializationMask.RoleClaimType) == Claim.SerializationMask.RoleClaimType)
			{
				this.m_type = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			}
			else
			{
				this.m_type = reader.ReadString();
				num++;
			}
			if ((serializationMask & Claim.SerializationMask.StringType) == Claim.SerializationMask.StringType)
			{
				this.m_valueType = reader.ReadString();
				num++;
			}
			else
			{
				this.m_valueType = "http://www.w3.org/2001/XMLSchema#string";
			}
			if ((serializationMask & Claim.SerializationMask.Issuer) == Claim.SerializationMask.Issuer)
			{
				this.m_issuer = reader.ReadString();
				num++;
			}
			else
			{
				this.m_issuer = "LOCAL AUTHORITY";
			}
			if ((serializationMask & Claim.SerializationMask.OriginalIssuerEqualsIssuer) == Claim.SerializationMask.OriginalIssuerEqualsIssuer)
			{
				this.m_originalIssuer = this.m_issuer;
			}
			else if ((serializationMask & Claim.SerializationMask.OriginalIssuer) == Claim.SerializationMask.OriginalIssuer)
			{
				this.m_originalIssuer = reader.ReadString();
				num++;
			}
			else
			{
				this.m_originalIssuer = "LOCAL AUTHORITY";
			}
			if ((serializationMask & Claim.SerializationMask.HasProperties) == Claim.SerializationMask.HasProperties)
			{
				int num3 = reader.ReadInt32();
				for (int i = 0; i < num3; i++)
				{
					this.Properties.Add(reader.ReadString(), reader.ReadString());
				}
			}
			if ((serializationMask & Claim.SerializationMask.UserData) == Claim.SerializationMask.UserData)
			{
				int count = reader.ReadInt32();
				this.m_userSerializationData = reader.ReadBytes(count);
				num++;
			}
			for (int j = num; j < num2; j++)
			{
				reader.ReadString();
			}
		}

		// Token: 0x0600282D RID: 10285 RVA: 0x00092CE2 File Offset: 0x00090EE2
		public virtual void WriteTo(BinaryWriter writer)
		{
			this.WriteTo(writer, null);
		}

		// Token: 0x0600282E RID: 10286 RVA: 0x00092CEC File Offset: 0x00090EEC
		protected virtual void WriteTo(BinaryWriter writer, byte[] userData)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			int num = 1;
			Claim.SerializationMask serializationMask = Claim.SerializationMask.None;
			if (string.Equals(this.m_type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"))
			{
				serializationMask |= Claim.SerializationMask.NameClaimType;
			}
			else if (string.Equals(this.m_type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
			{
				serializationMask |= Claim.SerializationMask.RoleClaimType;
			}
			else
			{
				num++;
			}
			if (!string.Equals(this.m_valueType, "http://www.w3.org/2001/XMLSchema#string", StringComparison.Ordinal))
			{
				num++;
				serializationMask |= Claim.SerializationMask.StringType;
			}
			if (!string.Equals(this.m_issuer, "LOCAL AUTHORITY", StringComparison.Ordinal))
			{
				num++;
				serializationMask |= Claim.SerializationMask.Issuer;
			}
			if (string.Equals(this.m_originalIssuer, this.m_issuer, StringComparison.Ordinal))
			{
				serializationMask |= Claim.SerializationMask.OriginalIssuerEqualsIssuer;
			}
			else if (!string.Equals(this.m_originalIssuer, "LOCAL AUTHORITY", StringComparison.Ordinal))
			{
				num++;
				serializationMask |= Claim.SerializationMask.OriginalIssuer;
			}
			if (this.Properties.Count > 0)
			{
				num++;
				serializationMask |= Claim.SerializationMask.HasProperties;
			}
			if (userData != null && userData.Length != 0)
			{
				num++;
				serializationMask |= Claim.SerializationMask.UserData;
			}
			writer.Write((int)serializationMask);
			writer.Write(num);
			writer.Write(this.m_value);
			if ((serializationMask & Claim.SerializationMask.NameClaimType) != Claim.SerializationMask.NameClaimType && (serializationMask & Claim.SerializationMask.RoleClaimType) != Claim.SerializationMask.RoleClaimType)
			{
				writer.Write(this.m_type);
			}
			if ((serializationMask & Claim.SerializationMask.StringType) == Claim.SerializationMask.StringType)
			{
				writer.Write(this.m_valueType);
			}
			if ((serializationMask & Claim.SerializationMask.Issuer) == Claim.SerializationMask.Issuer)
			{
				writer.Write(this.m_issuer);
			}
			if ((serializationMask & Claim.SerializationMask.OriginalIssuer) == Claim.SerializationMask.OriginalIssuer)
			{
				writer.Write(this.m_originalIssuer);
			}
			if ((serializationMask & Claim.SerializationMask.HasProperties) == Claim.SerializationMask.HasProperties)
			{
				writer.Write(this.Properties.Count);
				foreach (string text in this.Properties.Keys)
				{
					writer.Write(text);
					writer.Write(this.Properties[text]);
				}
			}
			if ((serializationMask & Claim.SerializationMask.UserData) == Claim.SerializationMask.UserData)
			{
				writer.Write(userData.Length);
				writer.Write(userData);
			}
			writer.Flush();
		}

		// Token: 0x0600282F RID: 10287 RVA: 0x00092ED4 File Offset: 0x000910D4
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", this.m_type, this.m_value);
		}

		// Token: 0x04000F84 RID: 3972
		private string m_issuer;

		// Token: 0x04000F85 RID: 3973
		private string m_originalIssuer;

		// Token: 0x04000F86 RID: 3974
		private string m_type;

		// Token: 0x04000F87 RID: 3975
		private string m_value;

		// Token: 0x04000F88 RID: 3976
		private string m_valueType;

		// Token: 0x04000F89 RID: 3977
		[NonSerialized]
		private byte[] m_userSerializationData;

		// Token: 0x04000F8A RID: 3978
		private Dictionary<string, string> m_properties;

		// Token: 0x04000F8B RID: 3979
		[NonSerialized]
		private object m_propertyLock;

		// Token: 0x04000F8C RID: 3980
		[NonSerialized]
		private ClaimsIdentity m_subject;

		// Token: 0x02000B51 RID: 2897
		private enum SerializationMask
		{
			// Token: 0x040033E2 RID: 13282
			None,
			// Token: 0x040033E3 RID: 13283
			NameClaimType,
			// Token: 0x040033E4 RID: 13284
			RoleClaimType,
			// Token: 0x040033E5 RID: 13285
			StringType = 4,
			// Token: 0x040033E6 RID: 13286
			Issuer = 8,
			// Token: 0x040033E7 RID: 13287
			OriginalIssuerEqualsIssuer = 16,
			// Token: 0x040033E8 RID: 13288
			OriginalIssuer = 32,
			// Token: 0x040033E9 RID: 13289
			HasProperties = 64,
			// Token: 0x040033EA RID: 13290
			UserData = 128
		}
	}
}
