using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.Security.Claims
{
	// Token: 0x0200031D RID: 797
	[ComVisible(true)]
	[Serializable]
	public class ClaimsIdentity : IIdentity
	{
		// Token: 0x06002830 RID: 10288 RVA: 0x00092EF1 File Offset: 0x000910F1
		public ClaimsIdentity()
			: this(null)
		{
		}

		// Token: 0x06002831 RID: 10289 RVA: 0x00092EFA File Offset: 0x000910FA
		public ClaimsIdentity(IIdentity identity)
			: this(identity, null)
		{
		}

		// Token: 0x06002832 RID: 10290 RVA: 0x00092F04 File Offset: 0x00091104
		public ClaimsIdentity(IEnumerable<Claim> claims)
			: this(null, claims, null, null, null)
		{
		}

		// Token: 0x06002833 RID: 10291 RVA: 0x00092F11 File Offset: 0x00091111
		public ClaimsIdentity(string authenticationType)
			: this(null, null, authenticationType, null, null)
		{
		}

		// Token: 0x06002834 RID: 10292 RVA: 0x00092F1E File Offset: 0x0009111E
		public ClaimsIdentity(IEnumerable<Claim> claims, string authenticationType)
			: this(null, claims, authenticationType, null, null)
		{
		}

		// Token: 0x06002835 RID: 10293 RVA: 0x00092F2B File Offset: 0x0009112B
		public ClaimsIdentity(IIdentity identity, IEnumerable<Claim> claims)
			: this(identity, claims, null, null, null)
		{
		}

		// Token: 0x06002836 RID: 10294 RVA: 0x00092F38 File Offset: 0x00091138
		public ClaimsIdentity(string authenticationType, string nameType, string roleType)
			: this(null, null, authenticationType, nameType, roleType)
		{
		}

		// Token: 0x06002837 RID: 10295 RVA: 0x00092F45 File Offset: 0x00091145
		public ClaimsIdentity(IEnumerable<Claim> claims, string authenticationType, string nameType, string roleType)
			: this(null, claims, authenticationType, nameType, roleType)
		{
		}

		// Token: 0x06002838 RID: 10296 RVA: 0x00092F53 File Offset: 0x00091153
		public ClaimsIdentity(IIdentity identity, IEnumerable<Claim> claims, string authenticationType, string nameType, string roleType)
			: this(identity, claims, authenticationType, nameType, roleType, true)
		{
		}

		// Token: 0x06002839 RID: 10297 RVA: 0x00092F64 File Offset: 0x00091164
		internal ClaimsIdentity(IIdentity identity, IEnumerable<Claim> claims, string authenticationType, string nameType, string roleType, bool checkAuthType)
		{
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
			this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			this.m_version = "1.0";
			base..ctor();
			bool flag = false;
			bool flag2 = false;
			if (checkAuthType && identity != null && string.IsNullOrEmpty(authenticationType))
			{
				if (identity is WindowsIdentity)
				{
					try
					{
						this.m_authenticationType = identity.AuthenticationType;
						goto IL_85;
					}
					catch (UnauthorizedAccessException)
					{
						this.m_authenticationType = null;
						goto IL_85;
					}
				}
				this.m_authenticationType = identity.AuthenticationType;
			}
			else
			{
				this.m_authenticationType = authenticationType;
			}
			IL_85:
			if (!string.IsNullOrEmpty(nameType))
			{
				this.m_nameType = nameType;
				flag = true;
			}
			if (!string.IsNullOrEmpty(roleType))
			{
				this.m_roleType = roleType;
				flag2 = true;
			}
			ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
			if (claimsIdentity != null)
			{
				this.m_label = claimsIdentity.m_label;
				if (!flag)
				{
					this.m_nameType = claimsIdentity.m_nameType;
				}
				if (!flag2)
				{
					this.m_roleType = claimsIdentity.m_roleType;
				}
				this.m_bootstrapContext = claimsIdentity.m_bootstrapContext;
				if (claimsIdentity.Actor != null)
				{
					if (this.IsCircular(claimsIdentity.Actor))
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperationException_ActorGraphCircular"));
					}
					if (!AppContextSwitches.SetActorAsReferenceWhenCopyingClaimsIdentity)
					{
						this.m_actor = claimsIdentity.Actor.Clone();
					}
					else
					{
						this.m_actor = claimsIdentity.Actor;
					}
				}
				if (claimsIdentity is WindowsIdentity && !(this is WindowsIdentity))
				{
					this.SafeAddClaims(claimsIdentity.Claims);
				}
				else
				{
					this.SafeAddClaims(claimsIdentity.m_instanceClaims);
				}
				if (claimsIdentity.m_userSerializationData != null)
				{
					this.m_userSerializationData = claimsIdentity.m_userSerializationData.Clone() as byte[];
				}
			}
			else if (identity != null && !string.IsNullOrEmpty(identity.Name))
			{
				this.SafeAddClaim(new Claim(this.m_nameType, identity.Name, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", this));
			}
			if (claims != null)
			{
				this.SafeAddClaims(claims);
			}
		}

		// Token: 0x0600283A RID: 10298 RVA: 0x00093148 File Offset: 0x00091348
		public ClaimsIdentity(BinaryReader reader)
		{
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
			this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			this.m_version = "1.0";
			base..ctor();
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.Initialize(reader);
		}

		// Token: 0x0600283B RID: 10299 RVA: 0x000931A8 File Offset: 0x000913A8
		protected ClaimsIdentity(ClaimsIdentity other)
		{
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
			this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			this.m_version = "1.0";
			base..ctor();
			if (other == null)
			{
				throw new ArgumentNullException("other");
			}
			if (other.m_actor != null)
			{
				this.m_actor = other.m_actor.Clone();
			}
			this.m_authenticationType = other.m_authenticationType;
			this.m_bootstrapContext = other.m_bootstrapContext;
			this.m_label = other.m_label;
			this.m_nameType = other.m_nameType;
			this.m_roleType = other.m_roleType;
			if (other.m_userSerializationData != null)
			{
				this.m_userSerializationData = other.m_userSerializationData.Clone() as byte[];
			}
			this.SafeAddClaims(other.m_instanceClaims);
		}

		// Token: 0x0600283C RID: 10300 RVA: 0x00093280 File Offset: 0x00091480
		[SecurityCritical]
		protected ClaimsIdentity(SerializationInfo info, StreamingContext context)
		{
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
			this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			this.m_version = "1.0";
			base..ctor();
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.Deserialize(info, context, true);
		}

		// Token: 0x0600283D RID: 10301 RVA: 0x000932E4 File Offset: 0x000914E4
		[SecurityCritical]
		protected ClaimsIdentity(SerializationInfo info)
		{
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
			this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			this.m_version = "1.0";
			base..ctor();
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.Deserialize(info, default(StreamingContext), false);
		}

		// Token: 0x17000527 RID: 1319
		// (get) Token: 0x0600283E RID: 10302 RVA: 0x0009334D File Offset: 0x0009154D
		public virtual string AuthenticationType
		{
			get
			{
				return this.m_authenticationType;
			}
		}

		// Token: 0x17000528 RID: 1320
		// (get) Token: 0x0600283F RID: 10303 RVA: 0x00093355 File Offset: 0x00091555
		public virtual bool IsAuthenticated
		{
			get
			{
				return !string.IsNullOrEmpty(this.m_authenticationType);
			}
		}

		// Token: 0x17000529 RID: 1321
		// (get) Token: 0x06002840 RID: 10304 RVA: 0x00093365 File Offset: 0x00091565
		// (set) Token: 0x06002841 RID: 10305 RVA: 0x0009336D File Offset: 0x0009156D
		public ClaimsIdentity Actor
		{
			get
			{
				return this.m_actor;
			}
			set
			{
				if (value != null && this.IsCircular(value))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperationException_ActorGraphCircular"));
				}
				this.m_actor = value;
			}
		}

		// Token: 0x1700052A RID: 1322
		// (get) Token: 0x06002842 RID: 10306 RVA: 0x00093392 File Offset: 0x00091592
		// (set) Token: 0x06002843 RID: 10307 RVA: 0x0009339A File Offset: 0x0009159A
		public object BootstrapContext
		{
			get
			{
				return this.m_bootstrapContext;
			}
			[SecurityCritical]
			set
			{
				this.m_bootstrapContext = value;
			}
		}

		// Token: 0x1700052B RID: 1323
		// (get) Token: 0x06002844 RID: 10308 RVA: 0x000933A4 File Offset: 0x000915A4
		public virtual IEnumerable<Claim> Claims
		{
			get
			{
				int num;
				for (int i = 0; i < this.m_instanceClaims.Count; i = num + 1)
				{
					yield return this.m_instanceClaims[i];
					num = i;
				}
				if (this.m_externalClaims != null)
				{
					for (int i = 0; i < this.m_externalClaims.Count; i = num + 1)
					{
						if (this.m_externalClaims[i] != null)
						{
							foreach (Claim claim in this.m_externalClaims[i])
							{
								yield return claim;
							}
							IEnumerator<Claim> enumerator = null;
						}
						num = i;
					}
				}
				yield break;
				yield break;
			}
		}

		// Token: 0x1700052C RID: 1324
		// (get) Token: 0x06002845 RID: 10309 RVA: 0x000933C1 File Offset: 0x000915C1
		protected virtual byte[] CustomSerializationData
		{
			get
			{
				return this.m_userSerializationData;
			}
		}

		// Token: 0x1700052D RID: 1325
		// (get) Token: 0x06002846 RID: 10310 RVA: 0x000933C9 File Offset: 0x000915C9
		internal Collection<IEnumerable<Claim>> ExternalClaims
		{
			[FriendAccessAllowed]
			get
			{
				return this.m_externalClaims;
			}
		}

		// Token: 0x1700052E RID: 1326
		// (get) Token: 0x06002847 RID: 10311 RVA: 0x000933D1 File Offset: 0x000915D1
		// (set) Token: 0x06002848 RID: 10312 RVA: 0x000933D9 File Offset: 0x000915D9
		public string Label
		{
			get
			{
				return this.m_label;
			}
			set
			{
				this.m_label = value;
			}
		}

		// Token: 0x1700052F RID: 1327
		// (get) Token: 0x06002849 RID: 10313 RVA: 0x000933E4 File Offset: 0x000915E4
		public virtual string Name
		{
			get
			{
				Claim claim = this.FindFirst(this.m_nameType);
				if (claim != null)
				{
					return claim.Value;
				}
				return null;
			}
		}

		// Token: 0x17000530 RID: 1328
		// (get) Token: 0x0600284A RID: 10314 RVA: 0x00093409 File Offset: 0x00091609
		public string NameClaimType
		{
			get
			{
				return this.m_nameType;
			}
		}

		// Token: 0x17000531 RID: 1329
		// (get) Token: 0x0600284B RID: 10315 RVA: 0x00093411 File Offset: 0x00091611
		public string RoleClaimType
		{
			get
			{
				return this.m_roleType;
			}
		}

		// Token: 0x0600284C RID: 10316 RVA: 0x0009341C File Offset: 0x0009161C
		public virtual ClaimsIdentity Clone()
		{
			ClaimsIdentity claimsIdentity = new ClaimsIdentity(this.m_instanceClaims);
			claimsIdentity.m_authenticationType = this.m_authenticationType;
			claimsIdentity.m_bootstrapContext = this.m_bootstrapContext;
			claimsIdentity.m_label = this.m_label;
			claimsIdentity.m_nameType = this.m_nameType;
			claimsIdentity.m_roleType = this.m_roleType;
			if (this.Actor != null)
			{
				if (this.IsCircular(this.Actor))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperationException_ActorGraphCircular"));
				}
				if (!AppContextSwitches.SetActorAsReferenceWhenCopyingClaimsIdentity)
				{
					claimsIdentity.Actor = this.Actor.Clone();
				}
				else
				{
					claimsIdentity.Actor = this.Actor;
				}
			}
			return claimsIdentity;
		}

		// Token: 0x0600284D RID: 10317 RVA: 0x000934C0 File Offset: 0x000916C0
		[SecurityCritical]
		public virtual void AddClaim(Claim claim)
		{
			if (claim == null)
			{
				throw new ArgumentNullException("claim");
			}
			if (claim.Subject == this)
			{
				this.m_instanceClaims.Add(claim);
				return;
			}
			this.m_instanceClaims.Add(claim.Clone(this));
		}

		// Token: 0x0600284E RID: 10318 RVA: 0x000934F8 File Offset: 0x000916F8
		[SecurityCritical]
		public virtual void AddClaims(IEnumerable<Claim> claims)
		{
			if (claims == null)
			{
				throw new ArgumentNullException("claims");
			}
			foreach (Claim claim in claims)
			{
				if (claim != null)
				{
					this.AddClaim(claim);
				}
			}
		}

		// Token: 0x0600284F RID: 10319 RVA: 0x00093554 File Offset: 0x00091754
		[SecurityCritical]
		public virtual bool TryRemoveClaim(Claim claim)
		{
			bool result = false;
			for (int i = 0; i < this.m_instanceClaims.Count; i++)
			{
				if (this.m_instanceClaims[i] == claim)
				{
					this.m_instanceClaims.RemoveAt(i);
					result = true;
					break;
				}
			}
			return result;
		}

		// Token: 0x06002850 RID: 10320 RVA: 0x00093599 File Offset: 0x00091799
		[SecurityCritical]
		public virtual void RemoveClaim(Claim claim)
		{
			if (!this.TryRemoveClaim(claim))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ClaimCannotBeRemoved", new object[] { claim }));
			}
		}

		// Token: 0x06002851 RID: 10321 RVA: 0x000935C0 File Offset: 0x000917C0
		[SecuritySafeCritical]
		private void SafeAddClaims(IEnumerable<Claim> claims)
		{
			foreach (Claim claim in claims)
			{
				if (claim.Subject == this)
				{
					this.m_instanceClaims.Add(claim);
				}
				else
				{
					this.m_instanceClaims.Add(claim.Clone(this));
				}
			}
		}

		// Token: 0x06002852 RID: 10322 RVA: 0x0009362C File Offset: 0x0009182C
		[SecuritySafeCritical]
		private void SafeAddClaim(Claim claim)
		{
			if (claim.Subject == this)
			{
				this.m_instanceClaims.Add(claim);
				return;
			}
			this.m_instanceClaims.Add(claim.Clone(this));
		}

		// Token: 0x06002853 RID: 10323 RVA: 0x00093658 File Offset: 0x00091858
		public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
		{
			if (match == null)
			{
				throw new ArgumentNullException("match");
			}
			List<Claim> list = new List<Claim>();
			foreach (Claim claim in this.Claims)
			{
				if (match(claim))
				{
					list.Add(claim);
				}
			}
			return list.AsReadOnly();
		}

		// Token: 0x06002854 RID: 10324 RVA: 0x000936C8 File Offset: 0x000918C8
		public virtual IEnumerable<Claim> FindAll(string type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			List<Claim> list = new List<Claim>();
			foreach (Claim claim in this.Claims)
			{
				if (claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(claim);
				}
			}
			return list.AsReadOnly();
		}

		// Token: 0x06002855 RID: 10325 RVA: 0x00093744 File Offset: 0x00091944
		public virtual bool HasClaim(Predicate<Claim> match)
		{
			if (match == null)
			{
				throw new ArgumentNullException("match");
			}
			foreach (Claim obj in this.Claims)
			{
				if (match(obj))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002856 RID: 10326 RVA: 0x000937A8 File Offset: 0x000919A8
		public virtual bool HasClaim(string type, string value)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			foreach (Claim claim in this.Claims)
			{
				if (claim != null && claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase) && string.Equals(claim.Value, value, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002857 RID: 10327 RVA: 0x00093838 File Offset: 0x00091A38
		public virtual Claim FindFirst(Predicate<Claim> match)
		{
			if (match == null)
			{
				throw new ArgumentNullException("match");
			}
			foreach (Claim claim in this.Claims)
			{
				if (match(claim))
				{
					return claim;
				}
			}
			return null;
		}

		// Token: 0x06002858 RID: 10328 RVA: 0x0009389C File Offset: 0x00091A9C
		public virtual Claim FindFirst(string type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			foreach (Claim claim in this.Claims)
			{
				if (claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
				{
					return claim;
				}
			}
			return null;
		}

		// Token: 0x06002859 RID: 10329 RVA: 0x0009390C File Offset: 0x00091B0C
		[OnSerializing]
		[SecurityCritical]
		private void OnSerializingMethod(StreamingContext context)
		{
			if (this is ISerializable)
			{
				return;
			}
			this.m_serializedClaims = this.SerializeClaims();
			this.m_serializedNameType = this.m_nameType;
			this.m_serializedRoleType = this.m_roleType;
		}

		// Token: 0x0600285A RID: 10330 RVA: 0x0009393C File Offset: 0x00091B3C
		[OnDeserialized]
		[SecurityCritical]
		private void OnDeserializedMethod(StreamingContext context)
		{
			if (this is ISerializable)
			{
				return;
			}
			if (!string.IsNullOrEmpty(this.m_serializedClaims))
			{
				this.DeserializeClaims(this.m_serializedClaims);
				this.m_serializedClaims = null;
			}
			this.m_nameType = (string.IsNullOrEmpty(this.m_serializedNameType) ? "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" : this.m_serializedNameType);
			this.m_roleType = (string.IsNullOrEmpty(this.m_serializedRoleType) ? "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" : this.m_serializedRoleType);
		}

		// Token: 0x0600285B RID: 10331 RVA: 0x000939B2 File Offset: 0x00091BB2
		[OnDeserializing]
		private void OnDeserializingMethod(StreamingContext context)
		{
			if (this is ISerializable)
			{
				return;
			}
			this.m_instanceClaims = new List<Claim>();
			this.m_externalClaims = new Collection<IEnumerable<Claim>>();
		}

		// Token: 0x0600285C RID: 10332 RVA: 0x000939D4 File Offset: 0x00091BD4
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, SerializationFormatter = true)]
		protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			info.AddValue("System.Security.ClaimsIdentity.version", this.m_version);
			if (!string.IsNullOrEmpty(this.m_authenticationType))
			{
				info.AddValue("System.Security.ClaimsIdentity.authenticationType", this.m_authenticationType);
			}
			info.AddValue("System.Security.ClaimsIdentity.nameClaimType", this.m_nameType);
			info.AddValue("System.Security.ClaimsIdentity.roleClaimType", this.m_roleType);
			if (!string.IsNullOrEmpty(this.m_label))
			{
				info.AddValue("System.Security.ClaimsIdentity.label", this.m_label);
			}
			if (this.m_actor != null)
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					binaryFormatter.Serialize(memoryStream, this.m_actor, null, false);
					info.AddValue("System.Security.ClaimsIdentity.actor", Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
				}
			}
			info.AddValue("System.Security.ClaimsIdentity.claims", this.SerializeClaims());
			if (this.m_bootstrapContext != null)
			{
				using (MemoryStream memoryStream2 = new MemoryStream())
				{
					binaryFormatter.Serialize(memoryStream2, this.m_bootstrapContext, null, false);
					info.AddValue("System.Security.ClaimsIdentity.bootstrapContext", Convert.ToBase64String(memoryStream2.GetBuffer(), 0, (int)memoryStream2.Length));
				}
			}
		}

		// Token: 0x0600285D RID: 10333 RVA: 0x00093B20 File Offset: 0x00091D20
		[SecurityCritical]
		private void DeserializeClaims(string serializedClaims)
		{
			if (!string.IsNullOrEmpty(serializedClaims))
			{
				using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(serializedClaims)))
				{
					this.m_instanceClaims = (List<Claim>)new BinaryFormatter().Deserialize(memoryStream, null, false);
					for (int i = 0; i < this.m_instanceClaims.Count; i++)
					{
						this.m_instanceClaims[i].Subject = this;
					}
				}
			}
			if (this.m_instanceClaims == null)
			{
				this.m_instanceClaims = new List<Claim>();
			}
		}

		// Token: 0x0600285E RID: 10334 RVA: 0x00093BB0 File Offset: 0x00091DB0
		[SecurityCritical]
		private string SerializeClaims()
		{
			string result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				new BinaryFormatter().Serialize(memoryStream, this.m_instanceClaims, null, false);
				result = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
			}
			return result;
		}

		// Token: 0x0600285F RID: 10335 RVA: 0x00093C08 File Offset: 0x00091E08
		private bool IsCircular(ClaimsIdentity subject)
		{
			if (this == subject)
			{
				return true;
			}
			ClaimsIdentity claimsIdentity = subject;
			while (claimsIdentity.Actor != null)
			{
				if (this == claimsIdentity.Actor)
				{
					return true;
				}
				claimsIdentity = claimsIdentity.Actor;
			}
			return false;
		}

		// Token: 0x06002860 RID: 10336 RVA: 0x00093C3C File Offset: 0x00091E3C
		private void Initialize(BinaryReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			ClaimsIdentity.SerializationMask serializationMask = (ClaimsIdentity.SerializationMask)reader.ReadInt32();
			if ((serializationMask & ClaimsIdentity.SerializationMask.AuthenticationType) == ClaimsIdentity.SerializationMask.AuthenticationType)
			{
				this.m_authenticationType = reader.ReadString();
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.BootstrapConext) == ClaimsIdentity.SerializationMask.BootstrapConext)
			{
				this.m_bootstrapContext = reader.ReadString();
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.NameClaimType) == ClaimsIdentity.SerializationMask.NameClaimType)
			{
				this.m_nameType = reader.ReadString();
			}
			else
			{
				this.m_nameType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.RoleClaimType) == ClaimsIdentity.SerializationMask.RoleClaimType)
			{
				this.m_roleType = reader.ReadString();
			}
			else
			{
				this.m_roleType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.HasClaims) == ClaimsIdentity.SerializationMask.HasClaims)
			{
				int num = reader.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					Claim item = new Claim(reader, this);
					this.m_instanceClaims.Add(item);
				}
			}
		}

		// Token: 0x06002861 RID: 10337 RVA: 0x00093CEF File Offset: 0x00091EEF
		protected virtual Claim CreateClaim(BinaryReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			return new Claim(reader, this);
		}

		// Token: 0x06002862 RID: 10338 RVA: 0x00093D06 File Offset: 0x00091F06
		public virtual void WriteTo(BinaryWriter writer)
		{
			this.WriteTo(writer, null);
		}

		// Token: 0x06002863 RID: 10339 RVA: 0x00093D10 File Offset: 0x00091F10
		protected virtual void WriteTo(BinaryWriter writer, byte[] userData)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			int num = 0;
			ClaimsIdentity.SerializationMask serializationMask = ClaimsIdentity.SerializationMask.None;
			if (this.m_authenticationType != null)
			{
				serializationMask |= ClaimsIdentity.SerializationMask.AuthenticationType;
				num++;
			}
			if (this.m_bootstrapContext != null)
			{
				string text = this.m_bootstrapContext as string;
				if (text != null)
				{
					serializationMask |= ClaimsIdentity.SerializationMask.BootstrapConext;
					num++;
				}
			}
			if (!string.Equals(this.m_nameType, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", StringComparison.Ordinal))
			{
				serializationMask |= ClaimsIdentity.SerializationMask.NameClaimType;
				num++;
			}
			if (!string.Equals(this.m_roleType, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.Ordinal))
			{
				serializationMask |= ClaimsIdentity.SerializationMask.RoleClaimType;
				num++;
			}
			if (!string.IsNullOrWhiteSpace(this.m_label))
			{
				serializationMask |= ClaimsIdentity.SerializationMask.HasLabel;
				num++;
			}
			if (this.m_instanceClaims.Count > 0)
			{
				serializationMask |= ClaimsIdentity.SerializationMask.HasClaims;
				num++;
			}
			if (this.m_actor != null)
			{
				serializationMask |= ClaimsIdentity.SerializationMask.Actor;
				num++;
			}
			if (userData != null && userData.Length != 0)
			{
				num++;
				serializationMask |= ClaimsIdentity.SerializationMask.UserData;
			}
			writer.Write((int)serializationMask);
			writer.Write(num);
			if ((serializationMask & ClaimsIdentity.SerializationMask.AuthenticationType) == ClaimsIdentity.SerializationMask.AuthenticationType)
			{
				writer.Write(this.m_authenticationType);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.BootstrapConext) == ClaimsIdentity.SerializationMask.BootstrapConext)
			{
				writer.Write(this.m_bootstrapContext as string);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.NameClaimType) == ClaimsIdentity.SerializationMask.NameClaimType)
			{
				writer.Write(this.m_nameType);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.RoleClaimType) == ClaimsIdentity.SerializationMask.RoleClaimType)
			{
				writer.Write(this.m_roleType);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.HasLabel) == ClaimsIdentity.SerializationMask.HasLabel)
			{
				writer.Write(this.m_label);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.HasClaims) == ClaimsIdentity.SerializationMask.HasClaims)
			{
				writer.Write(this.m_instanceClaims.Count);
				foreach (Claim claim in this.m_instanceClaims)
				{
					claim.WriteTo(writer);
				}
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.Actor) == ClaimsIdentity.SerializationMask.Actor)
			{
				this.m_actor.WriteTo(writer);
			}
			if ((serializationMask & ClaimsIdentity.SerializationMask.UserData) == ClaimsIdentity.SerializationMask.UserData)
			{
				writer.Write(userData.Length);
				writer.Write(userData);
			}
			writer.Flush();
		}

		// Token: 0x06002864 RID: 10340 RVA: 0x00093EF0 File Offset: 0x000920F0
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, SerializationFormatter = true)]
		private void Deserialize(SerializationInfo info, StreamingContext context, bool useContext)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			BinaryFormatter binaryFormatter;
			if (useContext)
			{
				binaryFormatter = new BinaryFormatter(null, context);
			}
			else
			{
				binaryFormatter = new BinaryFormatter();
			}
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
				if (num <= 959168042U)
				{
					if (num <= 623923795U)
					{
						if (num != 373632733U)
						{
							if (num == 623923795U)
							{
								if (name == "System.Security.ClaimsIdentity.roleClaimType")
								{
									this.m_roleType = info.GetString("System.Security.ClaimsIdentity.roleClaimType");
								}
							}
						}
						else if (name == "System.Security.ClaimsIdentity.label")
						{
							this.m_label = info.GetString("System.Security.ClaimsIdentity.label");
						}
					}
					else if (num != 656336169U)
					{
						if (num == 959168042U)
						{
							if (name == "System.Security.ClaimsIdentity.nameClaimType")
							{
								this.m_nameType = info.GetString("System.Security.ClaimsIdentity.nameClaimType");
							}
						}
					}
					else if (name == "System.Security.ClaimsIdentity.authenticationType")
					{
						this.m_authenticationType = info.GetString("System.Security.ClaimsIdentity.authenticationType");
					}
				}
				else if (num <= 1476368026U)
				{
					if (num != 1453716852U)
					{
						if (num != 1476368026U)
						{
							continue;
						}
						if (!(name == "System.Security.ClaimsIdentity.actor"))
						{
							continue;
						}
						using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(info.GetString("System.Security.ClaimsIdentity.actor"))))
						{
							this.m_actor = (ClaimsIdentity)binaryFormatter.Deserialize(memoryStream, null, false);
							continue;
						}
					}
					else if (!(name == "System.Security.ClaimsIdentity.claims"))
					{
						continue;
					}
					this.DeserializeClaims(info.GetString("System.Security.ClaimsIdentity.claims"));
				}
				else if (num != 2480284791U)
				{
					if (num == 3659022112U)
					{
						if (name == "System.Security.ClaimsIdentity.bootstrapContext")
						{
							using (MemoryStream memoryStream2 = new MemoryStream(Convert.FromBase64String(info.GetString("System.Security.ClaimsIdentity.bootstrapContext"))))
							{
								this.m_bootstrapContext = binaryFormatter.Deserialize(memoryStream2, null, false);
							}
						}
					}
				}
				else if (name == "System.Security.ClaimsIdentity.version")
				{
					string @string = info.GetString("System.Security.ClaimsIdentity.version");
				}
			}
		}

		// Token: 0x04000F8D RID: 3981
		[NonSerialized]
		private byte[] m_userSerializationData;

		// Token: 0x04000F8E RID: 3982
		[NonSerialized]
		private const string PreFix = "System.Security.ClaimsIdentity.";

		// Token: 0x04000F8F RID: 3983
		[NonSerialized]
		private const string ActorKey = "System.Security.ClaimsIdentity.actor";

		// Token: 0x04000F90 RID: 3984
		[NonSerialized]
		private const string AuthenticationTypeKey = "System.Security.ClaimsIdentity.authenticationType";

		// Token: 0x04000F91 RID: 3985
		[NonSerialized]
		private const string BootstrapContextKey = "System.Security.ClaimsIdentity.bootstrapContext";

		// Token: 0x04000F92 RID: 3986
		[NonSerialized]
		private const string ClaimsKey = "System.Security.ClaimsIdentity.claims";

		// Token: 0x04000F93 RID: 3987
		[NonSerialized]
		private const string LabelKey = "System.Security.ClaimsIdentity.label";

		// Token: 0x04000F94 RID: 3988
		[NonSerialized]
		private const string NameClaimTypeKey = "System.Security.ClaimsIdentity.nameClaimType";

		// Token: 0x04000F95 RID: 3989
		[NonSerialized]
		private const string RoleClaimTypeKey = "System.Security.ClaimsIdentity.roleClaimType";

		// Token: 0x04000F96 RID: 3990
		[NonSerialized]
		private const string VersionKey = "System.Security.ClaimsIdentity.version";

		// Token: 0x04000F97 RID: 3991
		[NonSerialized]
		public const string DefaultIssuer = "LOCAL AUTHORITY";

		// Token: 0x04000F98 RID: 3992
		[NonSerialized]
		public const string DefaultNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

		// Token: 0x04000F99 RID: 3993
		[NonSerialized]
		public const string DefaultRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

		// Token: 0x04000F9A RID: 3994
		[NonSerialized]
		private List<Claim> m_instanceClaims;

		// Token: 0x04000F9B RID: 3995
		[NonSerialized]
		private Collection<IEnumerable<Claim>> m_externalClaims;

		// Token: 0x04000F9C RID: 3996
		[NonSerialized]
		private string m_nameType;

		// Token: 0x04000F9D RID: 3997
		[NonSerialized]
		private string m_roleType;

		// Token: 0x04000F9E RID: 3998
		[OptionalField(VersionAdded = 2)]
		private string m_version;

		// Token: 0x04000F9F RID: 3999
		[OptionalField(VersionAdded = 2)]
		private ClaimsIdentity m_actor;

		// Token: 0x04000FA0 RID: 4000
		[OptionalField(VersionAdded = 2)]
		private string m_authenticationType;

		// Token: 0x04000FA1 RID: 4001
		[OptionalField(VersionAdded = 2)]
		private object m_bootstrapContext;

		// Token: 0x04000FA2 RID: 4002
		[OptionalField(VersionAdded = 2)]
		private string m_label;

		// Token: 0x04000FA3 RID: 4003
		[OptionalField(VersionAdded = 2)]
		private string m_serializedNameType;

		// Token: 0x04000FA4 RID: 4004
		[OptionalField(VersionAdded = 2)]
		private string m_serializedRoleType;

		// Token: 0x04000FA5 RID: 4005
		[OptionalField(VersionAdded = 2)]
		private string m_serializedClaims;

		// Token: 0x02000B52 RID: 2898
		private enum SerializationMask
		{
			// Token: 0x040033EC RID: 13292
			None,
			// Token: 0x040033ED RID: 13293
			AuthenticationType,
			// Token: 0x040033EE RID: 13294
			BootstrapConext,
			// Token: 0x040033EF RID: 13295
			NameClaimType = 4,
			// Token: 0x040033F0 RID: 13296
			RoleClaimType = 8,
			// Token: 0x040033F1 RID: 13297
			HasClaims = 16,
			// Token: 0x040033F2 RID: 13298
			HasLabel = 32,
			// Token: 0x040033F3 RID: 13299
			Actor = 64,
			// Token: 0x040033F4 RID: 13300
			UserData = 128
		}
	}
}
