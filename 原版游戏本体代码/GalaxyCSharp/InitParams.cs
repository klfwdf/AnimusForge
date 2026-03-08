using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000127 RID: 295
	public class InitParams : IDisposable
	{
		// Token: 0x06000B2D RID: 2861 RVA: 0x000189BC File Offset: 0x00016BBC
		internal InitParams(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x000189D8 File Offset: 0x00016BD8
		public InitParams(string _clientID, string _clientSecret, string _configFilePath, string _galaxyModulePath, bool _loadModule, string _storagePath)
			: this(GalaxyInstancePINVOKE.new_InitParams__SWIG_0(_clientID, _clientSecret, _configFilePath, _galaxyModulePath, _loadModule, _storagePath), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x000189FF File Offset: 0x00016BFF
		public InitParams(string _clientID, string _clientSecret, string _configFilePath, string _galaxyModulePath, bool _loadModule)
			: this(GalaxyInstancePINVOKE.new_InitParams__SWIG_1(_clientID, _clientSecret, _configFilePath, _galaxyModulePath, _loadModule), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B30 RID: 2864 RVA: 0x00018A24 File Offset: 0x00016C24
		public InitParams(string _clientID, string _clientSecret, string _configFilePath, string _galaxyModulePath)
			: this(GalaxyInstancePINVOKE.new_InitParams__SWIG_2(_clientID, _clientSecret, _configFilePath, _galaxyModulePath), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x00018A47 File Offset: 0x00016C47
		public InitParams(string _clientID, string _clientSecret, string _configFilePath)
			: this(GalaxyInstancePINVOKE.new_InitParams__SWIG_3(_clientID, _clientSecret, _configFilePath), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B32 RID: 2866 RVA: 0x00018A68 File Offset: 0x00016C68
		public InitParams(string _clientID, string _clientSecret)
			: this(GalaxyInstancePINVOKE.new_InitParams__SWIG_4(_clientID, _clientSecret), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B33 RID: 2867 RVA: 0x00018A88 File Offset: 0x00016C88
		internal static HandleRef getCPtr(InitParams obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B34 RID: 2868 RVA: 0x00018AA8 File Offset: 0x00016CA8
		~InitParams()
		{
			this.Dispose();
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x00018AD8 File Offset: 0x00016CD8
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_InitParams(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000B37 RID: 2871 RVA: 0x00018B78 File Offset: 0x00016D78
		// (set) Token: 0x06000B36 RID: 2870 RVA: 0x00018B58 File Offset: 0x00016D58
		public string clientID
		{
			get
			{
				string result = GalaxyInstancePINVOKE.InitParams_clientID_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_clientID_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000B39 RID: 2873 RVA: 0x00018BC0 File Offset: 0x00016DC0
		// (set) Token: 0x06000B38 RID: 2872 RVA: 0x00018BA2 File Offset: 0x00016DA2
		public string clientSecret
		{
			get
			{
				string result = GalaxyInstancePINVOKE.InitParams_clientSecret_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_clientSecret_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000B3B RID: 2875 RVA: 0x00018C08 File Offset: 0x00016E08
		// (set) Token: 0x06000B3A RID: 2874 RVA: 0x00018BEA File Offset: 0x00016DEA
		public string configFilePath
		{
			get
			{
				string result = GalaxyInstancePINVOKE.InitParams_configFilePath_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_configFilePath_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000B3D RID: 2877 RVA: 0x00018C50 File Offset: 0x00016E50
		// (set) Token: 0x06000B3C RID: 2876 RVA: 0x00018C32 File Offset: 0x00016E32
		public string storagePath
		{
			get
			{
				string result = GalaxyInstancePINVOKE.InitParams_storagePath_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_storagePath_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000B3F RID: 2879 RVA: 0x00018C98 File Offset: 0x00016E98
		// (set) Token: 0x06000B3E RID: 2878 RVA: 0x00018C7A File Offset: 0x00016E7A
		public string galaxyModulePath
		{
			get
			{
				string result = GalaxyInstancePINVOKE.InitParams_galaxyModulePath_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_galaxyModulePath_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000B41 RID: 2881 RVA: 0x00018CE0 File Offset: 0x00016EE0
		// (set) Token: 0x06000B40 RID: 2880 RVA: 0x00018CC2 File Offset: 0x00016EC2
		public bool loadModule
		{
			get
			{
				bool result = GalaxyInstancePINVOKE.InitParams_loadModule_get(this.swigCPtr);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				return result;
			}
			set
			{
				GalaxyInstancePINVOKE.InitParams_loadModule_set(this.swigCPtr, value);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
		}

		// Token: 0x040001FD RID: 509
		private HandleRef swigCPtr;

		// Token: 0x040001FE RID: 510
		protected bool swigCMemOwn;
	}
}
