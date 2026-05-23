using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x02000154 RID: 340
	public class IStorage : IDisposable
	{
		// Token: 0x06000C6B RID: 3179 RVA: 0x0001981A File Offset: 0x00017A1A
		internal IStorage(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C6C RID: 3180 RVA: 0x00019836 File Offset: 0x00017A36
		internal static HandleRef getCPtr(IStorage obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C6D RID: 3181 RVA: 0x00019854 File Offset: 0x00017A54
		~IStorage()
		{
			this.Dispose();
		}

		// Token: 0x06000C6E RID: 3182 RVA: 0x00019884 File Offset: 0x00017A84
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IStorage(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000C6F RID: 3183 RVA: 0x00019904 File Offset: 0x00017B04
		public virtual void FileWrite(string fileName, byte[] data, uint dataSize)
		{
			GalaxyInstancePINVOKE.IStorage_FileWrite(this.swigCPtr, fileName, data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x00019924 File Offset: 0x00017B24
		public virtual uint FileRead(string fileName, byte[] data, uint dataSize)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_FileRead(this.swigCPtr, fileName, data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x00019951 File Offset: 0x00017B51
		public virtual void FileDelete(string fileName)
		{
			GalaxyInstancePINVOKE.IStorage_FileDelete(this.swigCPtr, fileName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x00019970 File Offset: 0x00017B70
		public virtual bool FileExists(string fileName)
		{
			bool result = GalaxyInstancePINVOKE.IStorage_FileExists(this.swigCPtr, fileName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C73 RID: 3187 RVA: 0x0001999C File Offset: 0x00017B9C
		public virtual uint GetFileSize(string fileName)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_GetFileSize(this.swigCPtr, fileName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C74 RID: 3188 RVA: 0x000199C8 File Offset: 0x00017BC8
		public virtual uint GetFileTimestamp(string fileName)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_GetFileTimestamp(this.swigCPtr, fileName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C75 RID: 3189 RVA: 0x000199F4 File Offset: 0x00017BF4
		public virtual uint GetFileCount()
		{
			uint result = GalaxyInstancePINVOKE.IStorage_GetFileCount(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C76 RID: 3190 RVA: 0x00019A20 File Offset: 0x00017C20
		public virtual string GetFileNameByIndex(uint index)
		{
			string result = GalaxyInstancePINVOKE.IStorage_GetFileNameByIndex(this.swigCPtr, index);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C77 RID: 3191 RVA: 0x00019A4C File Offset: 0x00017C4C
		public virtual void GetFileNameCopyByIndex(uint index, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IStorage_GetFileNameCopyByIndex(this.swigCPtr, index, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000C78 RID: 3192 RVA: 0x00019AA4 File Offset: 0x00017CA4
		public virtual void FileShare(string fileName, IFileShareListener listener)
		{
			GalaxyInstancePINVOKE.IStorage_FileShare__SWIG_0(this.swigCPtr, fileName, IFileShareListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C79 RID: 3193 RVA: 0x00019AC8 File Offset: 0x00017CC8
		public virtual void FileShare(string fileName)
		{
			GalaxyInstancePINVOKE.IStorage_FileShare__SWIG_1(this.swigCPtr, fileName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C7A RID: 3194 RVA: 0x00019AE6 File Offset: 0x00017CE6
		public virtual void DownloadSharedFile(ulong sharedFileID, ISharedFileDownloadListener listener)
		{
			GalaxyInstancePINVOKE.IStorage_DownloadSharedFile__SWIG_0(this.swigCPtr, sharedFileID, ISharedFileDownloadListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C7B RID: 3195 RVA: 0x00019B0A File Offset: 0x00017D0A
		public virtual void DownloadSharedFile(ulong sharedFileID)
		{
			GalaxyInstancePINVOKE.IStorage_DownloadSharedFile__SWIG_1(this.swigCPtr, sharedFileID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C7C RID: 3196 RVA: 0x00019B28 File Offset: 0x00017D28
		public virtual string GetSharedFileName(ulong sharedFileID)
		{
			string result = GalaxyInstancePINVOKE.IStorage_GetSharedFileName(this.swigCPtr, sharedFileID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C7D RID: 3197 RVA: 0x00019B54 File Offset: 0x00017D54
		public virtual void GetSharedFileNameCopy(ulong sharedFileID, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IStorage_GetSharedFileNameCopy(this.swigCPtr, sharedFileID, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000C7E RID: 3198 RVA: 0x00019BAC File Offset: 0x00017DAC
		public virtual uint GetSharedFileSize(ulong sharedFileID)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_GetSharedFileSize(this.swigCPtr, sharedFileID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C7F RID: 3199 RVA: 0x00019BD8 File Offset: 0x00017DD8
		public virtual GalaxyID GetSharedFileOwner(ulong sharedFileID)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IStorage_GetSharedFileOwner(this.swigCPtr, sharedFileID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			GalaxyID result = null;
			if (intPtr != IntPtr.Zero)
			{
				result = new GalaxyID(intPtr, true);
			}
			return result;
		}

		// Token: 0x06000C80 RID: 3200 RVA: 0x00019C20 File Offset: 0x00017E20
		public virtual uint SharedFileRead(ulong sharedFileID, byte[] data, uint dataSize, uint offset)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_SharedFileRead__SWIG_0(this.swigCPtr, sharedFileID, data, dataSize, offset);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C81 RID: 3201 RVA: 0x00019C50 File Offset: 0x00017E50
		public virtual uint SharedFileRead(ulong sharedFileID, byte[] data, uint dataSize)
		{
			uint result = GalaxyInstancePINVOKE.IStorage_SharedFileRead__SWIG_1(this.swigCPtr, sharedFileID, data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C82 RID: 3202 RVA: 0x00019C7D File Offset: 0x00017E7D
		public virtual void SharedFileClose(ulong sharedFileID)
		{
			GalaxyInstancePINVOKE.IStorage_SharedFileClose(this.swigCPtr, sharedFileID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C83 RID: 3203 RVA: 0x00019C9C File Offset: 0x00017E9C
		public virtual uint GetDownloadedSharedFileCount()
		{
			uint result = GalaxyInstancePINVOKE.IStorage_GetDownloadedSharedFileCount(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C84 RID: 3204 RVA: 0x00019CC8 File Offset: 0x00017EC8
		public virtual ulong GetDownloadedSharedFileByIndex(uint index)
		{
			ulong result = GalaxyInstancePINVOKE.IStorage_GetDownloadedSharedFileByIndex(this.swigCPtr, index);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400026A RID: 618
		private HandleRef swigCPtr;

		// Token: 0x0400026B RID: 619
		protected bool swigCMemOwn;
	}
}
