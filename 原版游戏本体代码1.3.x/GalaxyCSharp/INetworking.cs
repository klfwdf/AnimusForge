using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000124 RID: 292
	public class INetworking : IDisposable
	{
		// Token: 0x06000B0D RID: 2829 RVA: 0x00018649 File Offset: 0x00016849
		internal INetworking(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B0E RID: 2830 RVA: 0x00018665 File Offset: 0x00016865
		internal static HandleRef getCPtr(INetworking obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B0F RID: 2831 RVA: 0x00018684 File Offset: 0x00016884
		~INetworking()
		{
			this.Dispose();
		}

		// Token: 0x06000B10 RID: 2832 RVA: 0x000186B4 File Offset: 0x000168B4
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_INetworking(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000B11 RID: 2833 RVA: 0x00018734 File Offset: 0x00016934
		public virtual bool SendP2PPacket(GalaxyID galaxyID, byte[] data, uint dataSize, P2PSendType sendType, byte channel)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_SendP2PPacket__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(galaxyID), data, dataSize, (int)sendType, channel);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B12 RID: 2834 RVA: 0x0001876C File Offset: 0x0001696C
		public virtual bool SendP2PPacket(GalaxyID galaxyID, byte[] data, uint dataSize, P2PSendType sendType)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_SendP2PPacket__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(galaxyID), data, dataSize, (int)sendType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B13 RID: 2835 RVA: 0x000187A0 File Offset: 0x000169A0
		public virtual bool PeekP2PPacket(byte[] dest, uint destSize, ref uint outMsgSize, ref GalaxyID outGalaxyID, byte channel)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_PeekP2PPacket__SWIG_0(this.swigCPtr, dest, destSize, ref outMsgSize, GalaxyID.getCPtr(outGalaxyID), channel);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B14 RID: 2836 RVA: 0x000187D8 File Offset: 0x000169D8
		public virtual bool PeekP2PPacket(byte[] dest, uint destSize, ref uint outMsgSize, ref GalaxyID outGalaxyID)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_PeekP2PPacket__SWIG_1(this.swigCPtr, dest, destSize, ref outMsgSize, GalaxyID.getCPtr(outGalaxyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B15 RID: 2837 RVA: 0x00018810 File Offset: 0x00016A10
		public virtual bool IsP2PPacketAvailable(ref uint outMsgSize, byte channel)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_IsP2PPacketAvailable__SWIG_0(this.swigCPtr, ref outMsgSize, channel);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B16 RID: 2838 RVA: 0x0001883C File Offset: 0x00016A3C
		public virtual bool IsP2PPacketAvailable(ref uint outMsgSize)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_IsP2PPacketAvailable__SWIG_1(this.swigCPtr, ref outMsgSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x00018868 File Offset: 0x00016A68
		public virtual bool ReadP2PPacket(byte[] dest, uint destSize, ref uint outMsgSize, ref GalaxyID outGalaxyID, byte channel)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_ReadP2PPacket__SWIG_0(this.swigCPtr, dest, destSize, ref outMsgSize, GalaxyID.getCPtr(outGalaxyID), channel);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B18 RID: 2840 RVA: 0x000188A0 File Offset: 0x00016AA0
		public virtual bool ReadP2PPacket(byte[] dest, uint destSize, ref uint outMsgSize, ref GalaxyID outGalaxyID)
		{
			bool result = GalaxyInstancePINVOKE.INetworking_ReadP2PPacket__SWIG_1(this.swigCPtr, dest, destSize, ref outMsgSize, GalaxyID.getCPtr(outGalaxyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x000188D5 File Offset: 0x00016AD5
		public virtual void PopP2PPacket(byte channel)
		{
			GalaxyInstancePINVOKE.INetworking_PopP2PPacket__SWIG_0(this.swigCPtr, channel);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x000188F3 File Offset: 0x00016AF3
		public virtual void PopP2PPacket()
		{
			GalaxyInstancePINVOKE.INetworking_PopP2PPacket__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B1B RID: 2843 RVA: 0x00018910 File Offset: 0x00016B10
		public virtual int GetPingWith(GalaxyID galaxyID)
		{
			int result = GalaxyInstancePINVOKE.INetworking_GetPingWith(this.swigCPtr, GalaxyID.getCPtr(galaxyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B1C RID: 2844 RVA: 0x00018940 File Offset: 0x00016B40
		public virtual void RequestNatTypeDetection()
		{
			GalaxyInstancePINVOKE.INetworking_RequestNatTypeDetection(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x00018960 File Offset: 0x00016B60
		public virtual NatType GetNatType()
		{
			NatType result = (NatType)GalaxyInstancePINVOKE.INetworking_GetNatType(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x0001898C File Offset: 0x00016B8C
		public virtual ConnectionType GetConnectionType(GalaxyID userID)
		{
			ConnectionType result = (ConnectionType)GalaxyInstancePINVOKE.INetworking_GetConnectionType(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x040001F7 RID: 503
		private HandleRef swigCPtr;

		// Token: 0x040001F8 RID: 504
		protected bool swigCMemOwn;
	}
}
