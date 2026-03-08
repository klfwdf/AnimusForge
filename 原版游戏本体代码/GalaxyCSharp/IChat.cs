using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x020000AB RID: 171
	public class IChat : IDisposable
	{
		// Token: 0x060007FC RID: 2044 RVA: 0x000165F0 File Offset: 0x000147F0
		internal IChat(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007FD RID: 2045 RVA: 0x0001660C File Offset: 0x0001480C
		internal static HandleRef getCPtr(IChat obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007FE RID: 2046 RVA: 0x0001662C File Offset: 0x0001482C
		~IChat()
		{
			this.Dispose();
		}

		// Token: 0x060007FF RID: 2047 RVA: 0x0001665C File Offset: 0x0001485C
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IChat(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000800 RID: 2048 RVA: 0x000166DC File Offset: 0x000148DC
		public virtual void RequestChatRoomWithUser(GalaxyID userID, IChatRoomWithUserRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IChat_RequestChatRoomWithUser__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IChatRoomWithUserRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000801 RID: 2049 RVA: 0x00016705 File Offset: 0x00014905
		public virtual void RequestChatRoomWithUser(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IChat_RequestChatRoomWithUser__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x00016728 File Offset: 0x00014928
		public virtual void RequestChatRoomMessages(ulong chatRoomID, uint limit, ulong referenceMessageID, IChatRoomMessagesRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IChat_RequestChatRoomMessages__SWIG_0(this.swigCPtr, chatRoomID, limit, referenceMessageID, IChatRoomMessagesRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x0001674F File Offset: 0x0001494F
		public virtual void RequestChatRoomMessages(ulong chatRoomID, uint limit, ulong referenceMessageID)
		{
			GalaxyInstancePINVOKE.IChat_RequestChatRoomMessages__SWIG_1(this.swigCPtr, chatRoomID, limit, referenceMessageID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x0001676F File Offset: 0x0001496F
		public virtual void RequestChatRoomMessages(ulong chatRoomID, uint limit)
		{
			GalaxyInstancePINVOKE.IChat_RequestChatRoomMessages__SWIG_2(this.swigCPtr, chatRoomID, limit);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x00016790 File Offset: 0x00014990
		public virtual uint SendChatRoomMessage(ulong chatRoomID, string msg, IChatRoomMessageSendListener listener)
		{
			uint result = GalaxyInstancePINVOKE.IChat_SendChatRoomMessage__SWIG_0(this.swigCPtr, chatRoomID, msg, IChatRoomMessageSendListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000806 RID: 2054 RVA: 0x000167C4 File Offset: 0x000149C4
		public virtual uint SendChatRoomMessage(ulong chatRoomID, string msg)
		{
			uint result = GalaxyInstancePINVOKE.IChat_SendChatRoomMessage__SWIG_1(this.swigCPtr, chatRoomID, msg);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000807 RID: 2055 RVA: 0x000167F0 File Offset: 0x000149F0
		public virtual uint GetChatRoomMessageByIndex(uint index, ref ulong messageID, ref ChatMessageType messageType, ref GalaxyID senderID, ref uint sendTime, out string buffer, uint bufferLength)
		{
			int num = 0;
			byte[] array = new byte[bufferLength];
			uint result;
			try
			{
				uint num2 = GalaxyInstancePINVOKE.IChat_GetChatRoomMessageByIndex(this.swigCPtr, index, ref messageID, ref num, GalaxyID.getCPtr(senderID), ref sendTime, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
				result = num2;
			}
			finally
			{
				messageType = (ChatMessageType)num;
				buffer = Encoding.UTF8.GetString(array);
			}
			return result;
		}

		// Token: 0x06000808 RID: 2056 RVA: 0x00016860 File Offset: 0x00014A60
		public virtual uint GetChatRoomMemberCount(ulong chatRoomID)
		{
			uint result = GalaxyInstancePINVOKE.IChat_GetChatRoomMemberCount(this.swigCPtr, chatRoomID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000809 RID: 2057 RVA: 0x0001688C File Offset: 0x00014A8C
		public virtual GalaxyID GetChatRoomMemberUserIDByIndex(ulong chatRoomID, uint index)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IChat_GetChatRoomMemberUserIDByIndex(this.swigCPtr, chatRoomID, index);
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

		// Token: 0x0600080A RID: 2058 RVA: 0x000168D4 File Offset: 0x00014AD4
		public virtual uint GetChatRoomUnreadMessageCount(ulong chatRoomID)
		{
			uint result = GalaxyInstancePINVOKE.IChat_GetChatRoomUnreadMessageCount(this.swigCPtr, chatRoomID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600080B RID: 2059 RVA: 0x000168FF File Offset: 0x00014AFF
		public virtual void MarkChatRoomAsRead(ulong chatRoomID)
		{
			GalaxyInstancePINVOKE.IChat_MarkChatRoomAsRead(this.swigCPtr, chatRoomID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x040000DE RID: 222
		private HandleRef swigCPtr;

		// Token: 0x040000DF RID: 223
		protected bool swigCMemOwn;
	}
}
