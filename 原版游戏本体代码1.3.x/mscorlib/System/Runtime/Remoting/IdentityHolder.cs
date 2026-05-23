using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B4 RID: 1972
	internal sealed class IdentityHolder
	{
		// Token: 0x17000E12 RID: 3602
		// (get) Token: 0x06005571 RID: 21873 RVA: 0x0012F776 File Offset: 0x0012D976
		internal static Hashtable URITable
		{
			get
			{
				return IdentityHolder._URITable;
			}
		}

		// Token: 0x17000E13 RID: 3603
		// (get) Token: 0x06005572 RID: 21874 RVA: 0x0012F77D File Offset: 0x0012D97D
		internal static Context DefaultContext
		{
			[SecurityCritical]
			get
			{
				if (IdentityHolder._cachedDefaultContext == null)
				{
					IdentityHolder._cachedDefaultContext = Thread.GetDomain().GetDefaultContext();
				}
				return IdentityHolder._cachedDefaultContext;
			}
		}

		// Token: 0x06005573 RID: 21875 RVA: 0x0012F7A0 File Offset: 0x0012D9A0
		private static string MakeURIKey(string uri)
		{
			return Identity.RemoveAppNameOrAppGuidIfNecessary(uri.ToLower(CultureInfo.InvariantCulture));
		}

		// Token: 0x06005574 RID: 21876 RVA: 0x0012F7B2 File Offset: 0x0012D9B2
		private static string MakeURIKeyNoLower(string uri)
		{
			return Identity.RemoveAppNameOrAppGuidIfNecessary(uri);
		}

		// Token: 0x17000E14 RID: 3604
		// (get) Token: 0x06005575 RID: 21877 RVA: 0x0012F7BA File Offset: 0x0012D9BA
		internal static ReaderWriterLock TableLock
		{
			get
			{
				return Thread.GetDomain().RemotingData.IDTableLock;
			}
		}

		// Token: 0x06005576 RID: 21878 RVA: 0x0012F7CC File Offset: 0x0012D9CC
		private static void CleanupIdentities(object state)
		{
			IDictionaryEnumerator enumerator = IdentityHolder.URITable.GetEnumerator();
			ArrayList arrayList = new ArrayList();
			while (enumerator.MoveNext())
			{
				object value = enumerator.Value;
				WeakReference weakReference = value as WeakReference;
				if (weakReference != null && weakReference.Target == null)
				{
					arrayList.Add(enumerator.Key);
				}
			}
			foreach (object obj in arrayList)
			{
				string key = (string)obj;
				IdentityHolder.URITable.Remove(key);
			}
		}

		// Token: 0x06005577 RID: 21879 RVA: 0x0012F870 File Offset: 0x0012DA70
		[SecurityCritical]
		internal static void FlushIdentityTable()
		{
			ReaderWriterLock tableLock = IdentityHolder.TableLock;
			bool flag = !tableLock.IsWriterLockHeld;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (flag)
				{
					tableLock.AcquireWriterLock(int.MaxValue);
				}
				IdentityHolder.CleanupIdentities(null);
			}
			finally
			{
				if (flag && tableLock.IsWriterLockHeld)
				{
					tableLock.ReleaseWriterLock();
				}
			}
		}

		// Token: 0x06005578 RID: 21880 RVA: 0x0012F8CC File Offset: 0x0012DACC
		private IdentityHolder()
		{
		}

		// Token: 0x06005579 RID: 21881 RVA: 0x0012F8D4 File Offset: 0x0012DAD4
		[SecurityCritical]
		internal static Identity ResolveIdentity(string URI)
		{
			if (URI == null)
			{
				throw new ArgumentNullException("URI");
			}
			ReaderWriterLock tableLock = IdentityHolder.TableLock;
			bool flag = !tableLock.IsReaderLockHeld;
			RuntimeHelpers.PrepareConstrainedRegions();
			Identity result;
			try
			{
				if (flag)
				{
					tableLock.AcquireReaderLock(int.MaxValue);
				}
				result = IdentityHolder.ResolveReference(IdentityHolder.URITable[IdentityHolder.MakeURIKey(URI)]);
			}
			finally
			{
				if (flag && tableLock.IsReaderLockHeld)
				{
					tableLock.ReleaseReaderLock();
				}
			}
			return result;
		}

		// Token: 0x0600557A RID: 21882 RVA: 0x0012F950 File Offset: 0x0012DB50
		[SecurityCritical]
		internal static Identity CasualResolveIdentity(string uri)
		{
			if (uri == null)
			{
				return null;
			}
			Identity identity = IdentityHolder.CasualResolveReference(IdentityHolder.URITable[IdentityHolder.MakeURIKeyNoLower(uri)]);
			if (identity == null)
			{
				identity = IdentityHolder.CasualResolveReference(IdentityHolder.URITable[IdentityHolder.MakeURIKey(uri)]);
				if (identity == null || identity.IsInitializing)
				{
					identity = RemotingConfigHandler.CreateWellKnownObject(uri);
				}
			}
			return identity;
		}

		// Token: 0x0600557B RID: 21883 RVA: 0x0012F9A4 File Offset: 0x0012DBA4
		private static Identity ResolveReference(object o)
		{
			WeakReference weakReference = o as WeakReference;
			if (weakReference != null)
			{
				return (Identity)weakReference.Target;
			}
			return (Identity)o;
		}

		// Token: 0x0600557C RID: 21884 RVA: 0x0012F9D0 File Offset: 0x0012DBD0
		private static Identity CasualResolveReference(object o)
		{
			WeakReference weakReference = o as WeakReference;
			if (weakReference != null)
			{
				return (Identity)weakReference.Target;
			}
			return (Identity)o;
		}

		// Token: 0x0600557D RID: 21885 RVA: 0x0012F9FC File Offset: 0x0012DBFC
		[SecurityCritical]
		internal static ServerIdentity FindOrCreateServerIdentity(MarshalByRefObject obj, string objURI, int flags)
		{
			ServerIdentity serverIdentity = null;
			bool flag;
			serverIdentity = (ServerIdentity)MarshalByRefObject.GetIdentity(obj, out flag);
			if (serverIdentity == null)
			{
				Context serverCtx;
				if (obj is ContextBoundObject)
				{
					serverCtx = Thread.CurrentContext;
				}
				else
				{
					serverCtx = IdentityHolder.DefaultContext;
				}
				ServerIdentity serverIdentity2 = new ServerIdentity(obj, serverCtx);
				if (flag)
				{
					serverIdentity = obj.__RaceSetServerIdentity(serverIdentity2);
				}
				else
				{
					RealProxy realProxy = RemotingServices.GetRealProxy(obj);
					realProxy.IdentityObject = serverIdentity2;
					serverIdentity = (ServerIdentity)realProxy.IdentityObject;
				}
				if (IdOps.bIsInitializing(flags))
				{
					serverIdentity.IsInitializing = true;
				}
			}
			if (IdOps.bStrongIdentity(flags))
			{
				ReaderWriterLock tableLock = IdentityHolder.TableLock;
				bool flag2 = !tableLock.IsWriterLockHeld;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					if (flag2)
					{
						tableLock.AcquireWriterLock(int.MaxValue);
					}
					if (serverIdentity.ObjURI == null || !serverIdentity.IsInIDTable())
					{
						IdentityHolder.SetIdentity(serverIdentity, objURI, DuplicateIdentityOption.Unique);
					}
					if (serverIdentity.IsDisconnected())
					{
						serverIdentity.SetFullyConnected();
					}
				}
				finally
				{
					if (flag2 && tableLock.IsWriterLockHeld)
					{
						tableLock.ReleaseWriterLock();
					}
				}
			}
			return serverIdentity;
		}

		// Token: 0x0600557E RID: 21886 RVA: 0x0012FAF8 File Offset: 0x0012DCF8
		[SecurityCritical]
		internal static Identity FindOrCreateIdentity(string objURI, string URL, ObjRef objectRef)
		{
			Identity identity = null;
			bool flag = URL != null;
			identity = IdentityHolder.ResolveIdentity(flag ? URL : objURI);
			if (flag && identity != null && identity is ServerIdentity)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_WellKnown_CantDirectlyConnect"), URL));
			}
			if (identity == null)
			{
				identity = new Identity(objURI, URL);
				ReaderWriterLock tableLock = IdentityHolder.TableLock;
				bool flag2 = !tableLock.IsWriterLockHeld;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					if (flag2)
					{
						tableLock.AcquireWriterLock(int.MaxValue);
					}
					identity = IdentityHolder.SetIdentity(identity, null, DuplicateIdentityOption.UseExisting);
					identity.RaceSetObjRef(objectRef);
				}
				finally
				{
					if (flag2 && tableLock.IsWriterLockHeld)
					{
						tableLock.ReleaseWriterLock();
					}
				}
			}
			return identity;
		}

		// Token: 0x0600557F RID: 21887 RVA: 0x0012FBA8 File Offset: 0x0012DDA8
		[SecurityCritical]
		private static Identity SetIdentity(Identity idObj, string URI, DuplicateIdentityOption duplicateOption)
		{
			bool flag = idObj is ServerIdentity;
			if (idObj.URI == null)
			{
				idObj.SetOrCreateURI(URI);
				if (idObj.ObjectRef != null)
				{
					idObj.ObjectRef.URI = idObj.URI;
				}
			}
			string key = IdentityHolder.MakeURIKey(idObj.URI);
			object obj = IdentityHolder.URITable[key];
			if (obj != null)
			{
				WeakReference weakReference = obj as WeakReference;
				Identity identity;
				bool flag2;
				if (weakReference != null)
				{
					identity = (Identity)weakReference.Target;
					flag2 = identity is ServerIdentity;
				}
				else
				{
					identity = (Identity)obj;
					flag2 = identity is ServerIdentity;
				}
				if (identity != null && identity != idObj)
				{
					if (duplicateOption == DuplicateIdentityOption.Unique)
					{
						string uri = idObj.URI;
						throw new RemotingException(Environment.GetResourceString("Remoting_URIClash", new object[] { uri }));
					}
					if (duplicateOption == DuplicateIdentityOption.UseExisting)
					{
						idObj = identity;
					}
				}
				else if (weakReference != null)
				{
					if (flag2)
					{
						IdentityHolder.URITable[key] = idObj;
					}
					else
					{
						weakReference.Target = idObj;
					}
				}
			}
			else
			{
				object value;
				if (flag)
				{
					value = idObj;
					((ServerIdentity)idObj).SetHandle();
				}
				else
				{
					value = new WeakReference(idObj);
				}
				IdentityHolder.URITable.Add(key, value);
				idObj.SetInIDTable();
				IdentityHolder.SetIDCount++;
				if (IdentityHolder.SetIDCount % 64 == 0)
				{
					IdentityHolder.CleanupIdentities(null);
				}
			}
			return idObj;
		}

		// Token: 0x06005580 RID: 21888 RVA: 0x0012FCF3 File Offset: 0x0012DEF3
		[SecurityCritical]
		internal static void RemoveIdentity(string uri)
		{
			IdentityHolder.RemoveIdentity(uri, true);
		}

		// Token: 0x06005581 RID: 21889 RVA: 0x0012FCFC File Offset: 0x0012DEFC
		[SecurityCritical]
		internal static void RemoveIdentity(string uri, bool bResetURI)
		{
			string key = IdentityHolder.MakeURIKey(uri);
			ReaderWriterLock tableLock = IdentityHolder.TableLock;
			bool flag = !tableLock.IsWriterLockHeld;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (flag)
				{
					tableLock.AcquireWriterLock(int.MaxValue);
				}
				object obj = IdentityHolder.URITable[key];
				WeakReference weakReference = obj as WeakReference;
				Identity identity;
				if (weakReference != null)
				{
					identity = (Identity)weakReference.Target;
					weakReference.Target = null;
				}
				else
				{
					identity = (Identity)obj;
					if (identity != null)
					{
						((ServerIdentity)identity).ResetHandle();
					}
				}
				if (identity != null)
				{
					IdentityHolder.URITable.Remove(key);
					identity.ResetInIDTable(bResetURI);
				}
			}
			finally
			{
				if (flag && tableLock.IsWriterLockHeld)
				{
					tableLock.ReleaseWriterLock();
				}
			}
		}

		// Token: 0x06005582 RID: 21890 RVA: 0x0012FDB4 File Offset: 0x0012DFB4
		[SecurityCritical]
		internal static bool AddDynamicProperty(MarshalByRefObject obj, IDynamicProperty prop)
		{
			if (RemotingServices.IsObjectOutOfContext(obj))
			{
				RealProxy realProxy = RemotingServices.GetRealProxy(obj);
				return realProxy.IdentityObject.AddProxySideDynamicProperty(prop);
			}
			MarshalByRefObject obj2 = (MarshalByRefObject)RemotingServices.AlwaysUnwrap((ContextBoundObject)obj);
			ServerIdentity serverIdentity = (ServerIdentity)MarshalByRefObject.GetIdentity(obj2);
			if (serverIdentity != null)
			{
				return serverIdentity.AddServerSideDynamicProperty(prop);
			}
			throw new RemotingException(Environment.GetResourceString("Remoting_NoIdentityEntry"));
		}

		// Token: 0x06005583 RID: 21891 RVA: 0x0012FE14 File Offset: 0x0012E014
		[SecurityCritical]
		internal static bool RemoveDynamicProperty(MarshalByRefObject obj, string name)
		{
			if (RemotingServices.IsObjectOutOfContext(obj))
			{
				RealProxy realProxy = RemotingServices.GetRealProxy(obj);
				return realProxy.IdentityObject.RemoveProxySideDynamicProperty(name);
			}
			MarshalByRefObject obj2 = (MarshalByRefObject)RemotingServices.AlwaysUnwrap((ContextBoundObject)obj);
			ServerIdentity serverIdentity = (ServerIdentity)MarshalByRefObject.GetIdentity(obj2);
			if (serverIdentity != null)
			{
				return serverIdentity.RemoveServerSideDynamicProperty(name);
			}
			throw new RemotingException(Environment.GetResourceString("Remoting_NoIdentityEntry"));
		}

		// Token: 0x04002761 RID: 10081
		private static volatile int SetIDCount = 0;

		// Token: 0x04002762 RID: 10082
		private const int CleanUpCountInterval = 64;

		// Token: 0x04002763 RID: 10083
		private const int INFINITE = 2147483647;

		// Token: 0x04002764 RID: 10084
		private static Hashtable _URITable = new Hashtable();

		// Token: 0x04002765 RID: 10085
		private static volatile Context _cachedDefaultContext = null;
	}
}
