using System;
using System.Security;

namespace System.Diagnostics
{
	// Token: 0x020003F6 RID: 1014
	[Serializable]
	internal class LogSwitch
	{
		// Token: 0x06003353 RID: 13139 RVA: 0x000C52DB File Offset: 0x000C34DB
		private LogSwitch()
		{
		}

		// Token: 0x06003354 RID: 13140 RVA: 0x000C52E4 File Offset: 0x000C34E4
		[SecuritySafeCritical]
		public LogSwitch(string name, string description, LogSwitch parent)
		{
			if (name != null && name.Length == 0)
			{
				throw new ArgumentOutOfRangeException("Name", Environment.GetResourceString("Argument_StringZeroLength"));
			}
			if (name != null && parent != null)
			{
				this.strName = name;
				this.strDescription = description;
				this.iLevel = LoggingLevels.ErrorLevel;
				this.iOldLevel = this.iLevel;
				this.ParentSwitch = parent;
				Log.m_Hashtable.Add(this.strName, this);
				Log.AddLogSwitch(this);
				return;
			}
			throw new ArgumentNullException((name == null) ? "name" : "parent");
		}

		// Token: 0x06003355 RID: 13141 RVA: 0x000C5378 File Offset: 0x000C3578
		[SecuritySafeCritical]
		internal LogSwitch(string name, string description)
		{
			this.strName = name;
			this.strDescription = description;
			this.iLevel = LoggingLevels.ErrorLevel;
			this.iOldLevel = this.iLevel;
			this.ParentSwitch = null;
			Log.m_Hashtable.Add(this.strName, this);
			Log.AddLogSwitch(this);
		}

		// Token: 0x17000784 RID: 1924
		// (get) Token: 0x06003356 RID: 13142 RVA: 0x000C53D1 File Offset: 0x000C35D1
		public virtual string Name
		{
			get
			{
				return this.strName;
			}
		}

		// Token: 0x17000785 RID: 1925
		// (get) Token: 0x06003357 RID: 13143 RVA: 0x000C53D9 File Offset: 0x000C35D9
		public virtual string Description
		{
			get
			{
				return this.strDescription;
			}
		}

		// Token: 0x17000786 RID: 1926
		// (get) Token: 0x06003358 RID: 13144 RVA: 0x000C53E1 File Offset: 0x000C35E1
		public virtual LogSwitch Parent
		{
			get
			{
				return this.ParentSwitch;
			}
		}

		// Token: 0x17000787 RID: 1927
		// (get) Token: 0x06003359 RID: 13145 RVA: 0x000C53E9 File Offset: 0x000C35E9
		// (set) Token: 0x0600335A RID: 13146 RVA: 0x000C53F4 File Offset: 0x000C35F4
		public virtual LoggingLevels MinimumLevel
		{
			get
			{
				return this.iLevel;
			}
			[SecuritySafeCritical]
			set
			{
				this.iLevel = value;
				this.iOldLevel = value;
				string strParentName = ((this.ParentSwitch != null) ? this.ParentSwitch.Name : "");
				if (Debugger.IsAttached)
				{
					Log.ModifyLogSwitch((int)this.iLevel, this.strName, strParentName);
				}
				Log.InvokeLogSwitchLevelHandlers(this, this.iLevel);
			}
		}

		// Token: 0x0600335B RID: 13147 RVA: 0x000C5457 File Offset: 0x000C3657
		public virtual bool CheckLevel(LoggingLevels level)
		{
			return this.iLevel <= level || (this.ParentSwitch != null && this.ParentSwitch.CheckLevel(level));
		}

		// Token: 0x0600335C RID: 13148 RVA: 0x000C547C File Offset: 0x000C367C
		public static LogSwitch GetSwitch(string name)
		{
			return (LogSwitch)Log.m_Hashtable[name];
		}

		// Token: 0x040016CA RID: 5834
		internal string strName;

		// Token: 0x040016CB RID: 5835
		internal string strDescription;

		// Token: 0x040016CC RID: 5836
		private LogSwitch ParentSwitch;

		// Token: 0x040016CD RID: 5837
		internal volatile LoggingLevels iLevel;

		// Token: 0x040016CE RID: 5838
		internal volatile LoggingLevels iOldLevel;
	}
}
