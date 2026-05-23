using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x0200015B RID: 347
	public class IUser : IDisposable
	{
		// Token: 0x06000CB1 RID: 3249 RVA: 0x0001A144 File Offset: 0x00018344
		internal IUser(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000CB2 RID: 3250 RVA: 0x0001A160 File Offset: 0x00018360
		internal static HandleRef getCPtr(IUser obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000CB3 RID: 3251 RVA: 0x0001A180 File Offset: 0x00018380
		~IUser()
		{
			this.Dispose();
		}

		// Token: 0x06000CB4 RID: 3252 RVA: 0x0001A1B0 File Offset: 0x000183B0
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUser(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000CB5 RID: 3253 RVA: 0x0001A230 File Offset: 0x00018430
		public virtual bool SignedIn()
		{
			bool result = GalaxyInstancePINVOKE.IUser_SignedIn(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CB6 RID: 3254 RVA: 0x0001A25C File Offset: 0x0001845C
		public virtual GalaxyID GetGalaxyID()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IUser_GetGalaxyID(this.swigCPtr);
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

		// Token: 0x06000CB7 RID: 3255 RVA: 0x0001A2A0 File Offset: 0x000184A0
		public virtual void SignInCredentials(string login, string password, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInCredentials__SWIG_0(this.swigCPtr, login, password, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CB8 RID: 3256 RVA: 0x0001A2C5 File Offset: 0x000184C5
		public virtual void SignInCredentials(string login, string password)
		{
			GalaxyInstancePINVOKE.IUser_SignInCredentials__SWIG_1(this.swigCPtr, login, password);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x0001A2E4 File Offset: 0x000184E4
		public virtual void SignInToken(string refreshToken, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInToken__SWIG_0(this.swigCPtr, refreshToken, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x0001A308 File Offset: 0x00018508
		public virtual void SignInToken(string refreshToken)
		{
			GalaxyInstancePINVOKE.IUser_SignInToken__SWIG_1(this.swigCPtr, refreshToken);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBB RID: 3259 RVA: 0x0001A326 File Offset: 0x00018526
		public virtual void SignInSteam(byte[] steamAppTicket, uint steamAppTicketSize, string personaName, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInSteam__SWIG_0(this.swigCPtr, steamAppTicket, steamAppTicketSize, personaName, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBC RID: 3260 RVA: 0x0001A34D File Offset: 0x0001854D
		public virtual void SignInSteam(byte[] steamAppTicket, uint steamAppTicketSize, string personaName)
		{
			GalaxyInstancePINVOKE.IUser_SignInSteam__SWIG_1(this.swigCPtr, steamAppTicket, steamAppTicketSize, personaName);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBD RID: 3261 RVA: 0x0001A36D File Offset: 0x0001856D
		public virtual void SignInEpic(string epicAccessToken, string epicUsername, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInEpic__SWIG_0(this.swigCPtr, epicAccessToken, epicUsername, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBE RID: 3262 RVA: 0x0001A392 File Offset: 0x00018592
		public virtual void SignInEpic(string epicAccessToken, string epicUsername)
		{
			GalaxyInstancePINVOKE.IUser_SignInEpic__SWIG_1(this.swigCPtr, epicAccessToken, epicUsername);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CBF RID: 3263 RVA: 0x0001A3B1 File Offset: 0x000185B1
		public virtual void SignInGalaxy(bool requireOnline, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInGalaxy__SWIG_0(this.swigCPtr, requireOnline, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC0 RID: 3264 RVA: 0x0001A3D5 File Offset: 0x000185D5
		public virtual void SignInGalaxy(bool requireOnline)
		{
			GalaxyInstancePINVOKE.IUser_SignInGalaxy__SWIG_1(this.swigCPtr, requireOnline);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC1 RID: 3265 RVA: 0x0001A3F3 File Offset: 0x000185F3
		public virtual void SignInGalaxy()
		{
			GalaxyInstancePINVOKE.IUser_SignInGalaxy__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC2 RID: 3266 RVA: 0x0001A410 File Offset: 0x00018610
		public virtual void SignInUWP(IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInUWP__SWIG_0(this.swigCPtr, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC3 RID: 3267 RVA: 0x0001A433 File Offset: 0x00018633
		public virtual void SignInUWP()
		{
			GalaxyInstancePINVOKE.IUser_SignInUWP__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC4 RID: 3268 RVA: 0x0001A450 File Offset: 0x00018650
		public virtual void SignInPS4(string ps4ClientID, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInPS4__SWIG_0(this.swigCPtr, ps4ClientID, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC5 RID: 3269 RVA: 0x0001A474 File Offset: 0x00018674
		public virtual void SignInPS4(string ps4ClientID)
		{
			GalaxyInstancePINVOKE.IUser_SignInPS4__SWIG_1(this.swigCPtr, ps4ClientID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC6 RID: 3270 RVA: 0x0001A492 File Offset: 0x00018692
		public virtual void SignInXB1(string xboxOneUserID, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInXB1__SWIG_0(this.swigCPtr, xboxOneUserID, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC7 RID: 3271 RVA: 0x0001A4B6 File Offset: 0x000186B6
		public virtual void SignInXB1(string xboxOneUserID)
		{
			GalaxyInstancePINVOKE.IUser_SignInXB1__SWIG_1(this.swigCPtr, xboxOneUserID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC8 RID: 3272 RVA: 0x0001A4D4 File Offset: 0x000186D4
		public virtual void SignInXBLive(string token, string signature, string marketplaceID, string locale, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInXBLive__SWIG_0(this.swigCPtr, token, signature, marketplaceID, locale, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CC9 RID: 3273 RVA: 0x0001A4FD File Offset: 0x000186FD
		public virtual void SignInXBLive(string token, string signature, string marketplaceID, string locale)
		{
			GalaxyInstancePINVOKE.IUser_SignInXBLive__SWIG_1(this.swigCPtr, token, signature, marketplaceID, locale);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCA RID: 3274 RVA: 0x0001A51F File Offset: 0x0001871F
		public virtual void SignInAnonymous(IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInAnonymous__SWIG_0(this.swigCPtr, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCB RID: 3275 RVA: 0x0001A542 File Offset: 0x00018742
		public virtual void SignInAnonymous()
		{
			GalaxyInstancePINVOKE.IUser_SignInAnonymous__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCC RID: 3276 RVA: 0x0001A55F File Offset: 0x0001875F
		public virtual void SignInAnonymousTelemetry(IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInAnonymousTelemetry__SWIG_0(this.swigCPtr, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCD RID: 3277 RVA: 0x0001A582 File Offset: 0x00018782
		public virtual void SignInAnonymousTelemetry()
		{
			GalaxyInstancePINVOKE.IUser_SignInAnonymousTelemetry__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCE RID: 3278 RVA: 0x0001A59F File Offset: 0x0001879F
		public virtual void SignInServerKey(string serverKey, IAuthListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SignInServerKey__SWIG_0(this.swigCPtr, serverKey, IAuthListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CCF RID: 3279 RVA: 0x0001A5C3 File Offset: 0x000187C3
		public virtual void SignInServerKey(string serverKey)
		{
			GalaxyInstancePINVOKE.IUser_SignInServerKey__SWIG_1(this.swigCPtr, serverKey);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CD0 RID: 3280 RVA: 0x0001A5E1 File Offset: 0x000187E1
		public virtual void SignOut()
		{
			GalaxyInstancePINVOKE.IUser_SignOut(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CD1 RID: 3281 RVA: 0x0001A5FE File Offset: 0x000187FE
		public virtual void RequestUserData(GalaxyID userID, ISpecificUserDataListener listener)
		{
			GalaxyInstancePINVOKE.IUser_RequestUserData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), ISpecificUserDataListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CD2 RID: 3282 RVA: 0x0001A627 File Offset: 0x00018827
		public virtual void RequestUserData(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IUser_RequestUserData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CD3 RID: 3283 RVA: 0x0001A64A File Offset: 0x0001884A
		public virtual void RequestUserData()
		{
			GalaxyInstancePINVOKE.IUser_RequestUserData__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CD4 RID: 3284 RVA: 0x0001A668 File Offset: 0x00018868
		public virtual bool IsUserDataAvailable(GalaxyID userID)
		{
			bool result = GalaxyInstancePINVOKE.IUser_IsUserDataAvailable__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CD5 RID: 3285 RVA: 0x0001A698 File Offset: 0x00018898
		public virtual bool IsUserDataAvailable()
		{
			bool result = GalaxyInstancePINVOKE.IUser_IsUserDataAvailable__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CD6 RID: 3286 RVA: 0x0001A6C4 File Offset: 0x000188C4
		public virtual string GetUserData(string key, GalaxyID userID)
		{
			string result = GalaxyInstancePINVOKE.IUser_GetUserData__SWIG_0(this.swigCPtr, key, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CD7 RID: 3287 RVA: 0x0001A6F8 File Offset: 0x000188F8
		public virtual string GetUserData(string key)
		{
			string result = GalaxyInstancePINVOKE.IUser_GetUserData__SWIG_1(this.swigCPtr, key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CD8 RID: 3288 RVA: 0x0001A724 File Offset: 0x00018924
		public virtual void GetUserDataCopy(string key, out string buffer, uint bufferLength, GalaxyID userID)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IUser_GetUserDataCopy__SWIG_0(this.swigCPtr, key, array, bufferLength, GalaxyID.getCPtr(userID));
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

		// Token: 0x06000CD9 RID: 3289 RVA: 0x0001A784 File Offset: 0x00018984
		public virtual void GetUserDataCopy(string key, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IUser_GetUserDataCopy__SWIG_1(this.swigCPtr, key, array, bufferLength);
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

		// Token: 0x06000CDA RID: 3290 RVA: 0x0001A7DC File Offset: 0x000189DC
		public virtual void SetUserData(string key, string value, ISpecificUserDataListener listener)
		{
			GalaxyInstancePINVOKE.IUser_SetUserData__SWIG_0(this.swigCPtr, key, value, ISpecificUserDataListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CDB RID: 3291 RVA: 0x0001A801 File Offset: 0x00018A01
		public virtual void SetUserData(string key, string value)
		{
			GalaxyInstancePINVOKE.IUser_SetUserData__SWIG_1(this.swigCPtr, key, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CDC RID: 3292 RVA: 0x0001A820 File Offset: 0x00018A20
		public virtual uint GetUserDataCount(GalaxyID userID)
		{
			uint result = GalaxyInstancePINVOKE.IUser_GetUserDataCount__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CDD RID: 3293 RVA: 0x0001A850 File Offset: 0x00018A50
		public virtual uint GetUserDataCount()
		{
			uint result = GalaxyInstancePINVOKE.IUser_GetUserDataCount__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CDE RID: 3294 RVA: 0x0001A87C File Offset: 0x00018A7C
		public virtual bool GetUserDataByIndex(uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength, GalaxyID userID)
		{
			bool result = GalaxyInstancePINVOKE.IUser_GetUserDataByIndex__SWIG_0(this.swigCPtr, index, key, keyLength, value, valueLength, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CDF RID: 3295 RVA: 0x0001A8B8 File Offset: 0x00018AB8
		public virtual bool GetUserDataByIndex(uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength)
		{
			bool result = GalaxyInstancePINVOKE.IUser_GetUserDataByIndex__SWIG_1(this.swigCPtr, index, key, keyLength, value, valueLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CE0 RID: 3296 RVA: 0x0001A8EB File Offset: 0x00018AEB
		public virtual void DeleteUserData(string key, ISpecificUserDataListener listener)
		{
			GalaxyInstancePINVOKE.IUser_DeleteUserData__SWIG_0(this.swigCPtr, key, ISpecificUserDataListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CE1 RID: 3297 RVA: 0x0001A90F File Offset: 0x00018B0F
		public virtual void DeleteUserData(string key)
		{
			GalaxyInstancePINVOKE.IUser_DeleteUserData__SWIG_1(this.swigCPtr, key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CE2 RID: 3298 RVA: 0x0001A930 File Offset: 0x00018B30
		public virtual bool IsLoggedOn()
		{
			bool result = GalaxyInstancePINVOKE.IUser_IsLoggedOn(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CE3 RID: 3299 RVA: 0x0001A95A File Offset: 0x00018B5A
		public virtual void RequestEncryptedAppTicket(byte[] data, uint dataSize, IEncryptedAppTicketListener listener)
		{
			GalaxyInstancePINVOKE.IUser_RequestEncryptedAppTicket__SWIG_0(this.swigCPtr, data, dataSize, IEncryptedAppTicketListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CE4 RID: 3300 RVA: 0x0001A97F File Offset: 0x00018B7F
		public virtual void RequestEncryptedAppTicket(byte[] data, uint dataSize)
		{
			GalaxyInstancePINVOKE.IUser_RequestEncryptedAppTicket__SWIG_1(this.swigCPtr, data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CE5 RID: 3301 RVA: 0x0001A99E File Offset: 0x00018B9E
		public virtual void GetEncryptedAppTicket(byte[] encryptedAppTicket, uint maxEncryptedAppTicketSize, ref uint currentEncryptedAppTicketSize)
		{
			GalaxyInstancePINVOKE.IUser_GetEncryptedAppTicket(this.swigCPtr, encryptedAppTicket, maxEncryptedAppTicketSize, ref currentEncryptedAppTicketSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000CE6 RID: 3302 RVA: 0x0001A9C0 File Offset: 0x00018BC0
		public virtual ulong GetSessionID()
		{
			ulong result = GalaxyInstancePINVOKE.IUser_GetSessionID(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CE7 RID: 3303 RVA: 0x0001A9EC File Offset: 0x00018BEC
		public virtual string GetAccessToken()
		{
			string result = GalaxyInstancePINVOKE.IUser_GetAccessToken(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CE8 RID: 3304 RVA: 0x0001AA18 File Offset: 0x00018C18
		public virtual void GetAccessTokenCopy(out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IUser_GetAccessTokenCopy(this.swigCPtr, array, bufferLength);
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

		// Token: 0x06000CE9 RID: 3305 RVA: 0x0001AA70 File Offset: 0x00018C70
		public virtual bool ReportInvalidAccessToken(string accessToken, string info)
		{
			bool result = GalaxyInstancePINVOKE.IUser_ReportInvalidAccessToken__SWIG_0(this.swigCPtr, accessToken, info);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000CEA RID: 3306 RVA: 0x0001AA9C File Offset: 0x00018C9C
		public virtual bool ReportInvalidAccessToken(string accessToken)
		{
			bool result = GalaxyInstancePINVOKE.IUser_ReportInvalidAccessToken__SWIG_1(this.swigCPtr, accessToken);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400027D RID: 637
		private HandleRef swigCPtr;

		// Token: 0x0400027E RID: 638
		protected bool swigCMemOwn;
	}
}
