using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Security.Permissions
{
	// Token: 0x02000310 RID: 784
	[ComVisible(true)]
	[Serializable]
	public sealed class ZoneIdentityPermission : CodeAccessPermission, IBuiltInPermission
	{
		// Token: 0x0600279A RID: 10138 RVA: 0x000902A4 File Offset: 0x0008E4A4
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != (StreamingContextStates)0)
			{
				if (this.m_serializedPermission != null)
				{
					this.FromXml(SecurityElement.FromString(this.m_serializedPermission));
					this.m_serializedPermission = null;
					return;
				}
				this.SecurityZone = this.m_zone;
				this.m_zone = SecurityZone.NoZone;
			}
		}

		// Token: 0x0600279B RID: 10139 RVA: 0x000902F4 File Offset: 0x0008E4F4
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != (StreamingContextStates)0)
			{
				this.m_serializedPermission = this.ToXml().ToString();
				this.m_zone = this.SecurityZone;
			}
		}

		// Token: 0x0600279C RID: 10140 RVA: 0x00090322 File Offset: 0x0008E522
		[OnSerialized]
		private void OnSerialized(StreamingContext ctx)
		{
			if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != (StreamingContextStates)0)
			{
				this.m_serializedPermission = null;
				this.m_zone = SecurityZone.NoZone;
			}
		}

		// Token: 0x0600279D RID: 10141 RVA: 0x00090341 File Offset: 0x0008E541
		public ZoneIdentityPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.m_zones = 31U;
				return;
			}
			if (state == PermissionState.None)
			{
				this.m_zones = 0U;
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x0600279E RID: 10142 RVA: 0x00090377 File Offset: 0x0008E577
		public ZoneIdentityPermission(SecurityZone zone)
		{
			this.SecurityZone = zone;
		}

		// Token: 0x0600279F RID: 10143 RVA: 0x0009038D File Offset: 0x0008E58D
		internal ZoneIdentityPermission(uint zones)
		{
			this.m_zones = zones & 31U;
		}

		// Token: 0x060027A0 RID: 10144 RVA: 0x000903A8 File Offset: 0x0008E5A8
		internal void AppendZones(ArrayList zoneList)
		{
			int num = 0;
			for (uint num2 = 1U; num2 < 31U; num2 <<= 1)
			{
				if ((this.m_zones & num2) != 0U)
				{
					zoneList.Add((SecurityZone)num);
				}
				num++;
			}
		}

		// Token: 0x1700050F RID: 1295
		// (get) Token: 0x060027A2 RID: 10146 RVA: 0x00090400 File Offset: 0x0008E600
		// (set) Token: 0x060027A1 RID: 10145 RVA: 0x000903DF File Offset: 0x0008E5DF
		public SecurityZone SecurityZone
		{
			get
			{
				SecurityZone securityZone = SecurityZone.NoZone;
				int num = 0;
				for (uint num2 = 1U; num2 < 31U; num2 <<= 1)
				{
					if ((this.m_zones & num2) != 0U)
					{
						if (securityZone != SecurityZone.NoZone)
						{
							return SecurityZone.NoZone;
						}
						securityZone = (SecurityZone)num;
					}
					num++;
				}
				return securityZone;
			}
			set
			{
				ZoneIdentityPermission.VerifyZone(value);
				if (value == SecurityZone.NoZone)
				{
					this.m_zones = 0U;
					return;
				}
				this.m_zones = 1U << (int)value;
			}
		}

		// Token: 0x060027A3 RID: 10147 RVA: 0x00090437 File Offset: 0x0008E637
		private static void VerifyZone(SecurityZone zone)
		{
			if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IllegalZone"));
			}
		}

		// Token: 0x060027A4 RID: 10148 RVA: 0x00090451 File Offset: 0x0008E651
		public override IPermission Copy()
		{
			return new ZoneIdentityPermission(this.m_zones);
		}

		// Token: 0x060027A5 RID: 10149 RVA: 0x00090460 File Offset: 0x0008E660
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.m_zones == 0U;
			}
			ZoneIdentityPermission zoneIdentityPermission = target as ZoneIdentityPermission;
			if (zoneIdentityPermission == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return (this.m_zones & zoneIdentityPermission.m_zones) == this.m_zones;
		}

		// Token: 0x060027A6 RID: 10150 RVA: 0x000904C0 File Offset: 0x0008E6C0
		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			ZoneIdentityPermission zoneIdentityPermission = target as ZoneIdentityPermission;
			if (zoneIdentityPermission == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			uint num = this.m_zones & zoneIdentityPermission.m_zones;
			if (num == 0U)
			{
				return null;
			}
			return new ZoneIdentityPermission(num);
		}

		// Token: 0x060027A7 RID: 10151 RVA: 0x0009051C File Offset: 0x0008E71C
		public override IPermission Union(IPermission target)
		{
			if (target == null)
			{
				if (this.m_zones == 0U)
				{
					return null;
				}
				return this.Copy();
			}
			else
			{
				ZoneIdentityPermission zoneIdentityPermission = target as ZoneIdentityPermission;
				if (zoneIdentityPermission == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
				}
				return new ZoneIdentityPermission(this.m_zones | zoneIdentityPermission.m_zones);
			}
		}

		// Token: 0x060027A8 RID: 10152 RVA: 0x00090580 File Offset: 0x0008E780
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.ZoneIdentityPermission");
			if (this.SecurityZone != SecurityZone.NoZone)
			{
				securityElement.AddAttribute("Zone", Enum.GetName(typeof(SecurityZone), this.SecurityZone));
			}
			else
			{
				int num = 0;
				for (uint num2 = 1U; num2 < 31U; num2 <<= 1)
				{
					if ((this.m_zones & num2) != 0U)
					{
						SecurityElement securityElement2 = new SecurityElement("Zone");
						securityElement2.AddAttribute("Zone", Enum.GetName(typeof(SecurityZone), (SecurityZone)num));
						securityElement.AddChild(securityElement2);
					}
					num++;
				}
			}
			return securityElement;
		}

		// Token: 0x060027A9 RID: 10153 RVA: 0x0009061C File Offset: 0x0008E81C
		public override void FromXml(SecurityElement esd)
		{
			this.m_zones = 0U;
			CodeAccessPermission.ValidateElement(esd, this);
			string text = esd.Attribute("Zone");
			if (text != null)
			{
				this.SecurityZone = (SecurityZone)Enum.Parse(typeof(SecurityZone), text);
			}
			if (esd.Children != null)
			{
				foreach (object obj in esd.Children)
				{
					SecurityElement securityElement = (SecurityElement)obj;
					text = securityElement.Attribute("Zone");
					int num = (int)Enum.Parse(typeof(SecurityZone), text);
					if (num != -1)
					{
						this.m_zones |= 1U << num;
					}
				}
			}
		}

		// Token: 0x060027AA RID: 10154 RVA: 0x000906EC File Offset: 0x0008E8EC
		int IBuiltInPermission.GetTokenIndex()
		{
			return ZoneIdentityPermission.GetTokenIndex();
		}

		// Token: 0x060027AB RID: 10155 RVA: 0x000906F3 File Offset: 0x0008E8F3
		internal static int GetTokenIndex()
		{
			return 14;
		}

		// Token: 0x04000F5A RID: 3930
		private const uint AllZones = 31U;

		// Token: 0x04000F5B RID: 3931
		[OptionalField(VersionAdded = 2)]
		private uint m_zones;

		// Token: 0x04000F5C RID: 3932
		[OptionalField(VersionAdded = 2)]
		private string m_serializedPermission;

		// Token: 0x04000F5D RID: 3933
		private SecurityZone m_zone = SecurityZone.NoZone;
	}
}
