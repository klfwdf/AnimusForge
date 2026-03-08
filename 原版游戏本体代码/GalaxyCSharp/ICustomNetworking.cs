using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000C3 RID: 195
	public class ICustomNetworking : IDisposable
	{
		// Token: 0x06000886 RID: 2182 RVA: 0x0001691D File Offset: 0x00014B1D
		internal ICustomNetworking(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x00016939 File Offset: 0x00014B39
		internal static HandleRef getCPtr(ICustomNetworking obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x00016958 File Offset: 0x00014B58
		~ICustomNetworking()
		{
			this.Dispose();
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x00016988 File Offset: 0x00014B88
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ICustomNetworking(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x00016A08 File Offset: 0x00014C08
		public virtual void OpenConnection(string connectionString, IConnectionOpenListener listener)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_OpenConnection__SWIG_0(this.swigCPtr, connectionString, IConnectionOpenListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x00016A2C File Offset: 0x00014C2C
		public virtual void OpenConnection(string connectionString)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_OpenConnection__SWIG_1(this.swigCPtr, connectionString);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x00016A4A File Offset: 0x00014C4A
		public virtual void CloseConnection(ulong connectionID, IConnectionCloseListener listener)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_CloseConnection__SWIG_0(this.swigCPtr, connectionID, IConnectionCloseListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600088D RID: 2189 RVA: 0x00016A6E File Offset: 0x00014C6E
		public virtual void CloseConnection(ulong connectionID)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_CloseConnection__SWIG_1(this.swigCPtr, connectionID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x00016A8C File Offset: 0x00014C8C
		public virtual void SendData(ulong connectionID, byte[] data, uint dataSize)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_SendData(this.swigCPtr, connectionID, data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x00016AAC File Offset: 0x00014CAC
		public virtual uint GetAvailableDataSize(ulong connectionID)
		{
			uint result = GalaxyInstancePINVOKE.ICustomNetworking_GetAvailableDataSize(this.swigCPtr, connectionID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x00016AD7 File Offset: 0x00014CD7
		public virtual void PeekData(ulong connectionID, byte[] dest, uint dataSize)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_PeekData(this.swigCPtr, connectionID, dest, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000891 RID: 2193 RVA: 0x00016AF7 File Offset: 0x00014CF7
		public virtual void ReadData(ulong connectionID, byte[] dest, uint dataSize)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_ReadData(this.swigCPtr, connectionID, dest, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000892 RID: 2194 RVA: 0x00016B17 File Offset: 0x00014D17
		public virtual void PopData(ulong connectionID, uint dataSize)
		{
			GalaxyInstancePINVOKE.ICustomNetworking_PopData(this.swigCPtr, connectionID, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x04000116 RID: 278
		private HandleRef swigCPtr;

		// Token: 0x04000117 RID: 279
		protected bool swigCMemOwn;
	}
}
