using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000819 RID: 2073
	[SecurityCritical]
	[AttributeUsage(AttributeTargets.Class)]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class SynchronizationAttribute : ContextAttribute, IContributeServerContextSink, IContributeClientContextSink
	{
		// Token: 0x17000EBD RID: 3773
		// (get) Token: 0x060058E3 RID: 22755 RVA: 0x00139388 File Offset: 0x00137588
		// (set) Token: 0x060058E4 RID: 22756 RVA: 0x00139390 File Offset: 0x00137590
		public virtual bool Locked
		{
			get
			{
				return this._locked;
			}
			set
			{
				this._locked = value;
			}
		}

		// Token: 0x17000EBE RID: 3774
		// (get) Token: 0x060058E5 RID: 22757 RVA: 0x00139399 File Offset: 0x00137599
		public virtual bool IsReEntrant
		{
			get
			{
				return this._bReEntrant;
			}
		}

		// Token: 0x17000EBF RID: 3775
		// (get) Token: 0x060058E6 RID: 22758 RVA: 0x001393A1 File Offset: 0x001375A1
		// (set) Token: 0x060058E7 RID: 22759 RVA: 0x001393A9 File Offset: 0x001375A9
		internal string SyncCallOutLCID
		{
			get
			{
				return this._syncLcid;
			}
			set
			{
				this._syncLcid = value;
			}
		}

		// Token: 0x17000EC0 RID: 3776
		// (get) Token: 0x060058E8 RID: 22760 RVA: 0x001393B2 File Offset: 0x001375B2
		internal ArrayList AsyncCallOutLCIDList
		{
			get
			{
				return this._asyncLcidList;
			}
		}

		// Token: 0x060058E9 RID: 22761 RVA: 0x001393BC File Offset: 0x001375BC
		internal bool IsKnownLCID(IMessage reqMsg)
		{
			string logicalCallID = ((LogicalCallContext)reqMsg.Properties[Message.CallContextKey]).RemotingData.LogicalCallID;
			return logicalCallID.Equals(this._syncLcid) || this._asyncLcidList.Contains(logicalCallID);
		}

		// Token: 0x060058EA RID: 22762 RVA: 0x00139405 File Offset: 0x00137605
		public SynchronizationAttribute()
			: this(4, false)
		{
		}

		// Token: 0x060058EB RID: 22763 RVA: 0x0013940F File Offset: 0x0013760F
		public SynchronizationAttribute(bool reEntrant)
			: this(4, reEntrant)
		{
		}

		// Token: 0x060058EC RID: 22764 RVA: 0x00139419 File Offset: 0x00137619
		public SynchronizationAttribute(int flag)
			: this(flag, false)
		{
		}

		// Token: 0x060058ED RID: 22765 RVA: 0x00139423 File Offset: 0x00137623
		public SynchronizationAttribute(int flag, bool reEntrant)
			: base("Synchronization")
		{
			this._bReEntrant = reEntrant;
			if (flag - 1 <= 1 || flag == 4 || flag == 8)
			{
				this._flavor = flag;
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "flag");
		}

		// Token: 0x060058EE RID: 22766 RVA: 0x00139461 File Offset: 0x00137661
		internal void Dispose()
		{
			if (this._waitHandle != null)
			{
				this._waitHandle.Unregister(null);
			}
		}

		// Token: 0x060058EF RID: 22767 RVA: 0x00139478 File Offset: 0x00137678
		[SecurityCritical]
		[ComVisible(true)]
		public override bool IsContextOK(Context ctx, IConstructionCallMessage msg)
		{
			if (ctx == null)
			{
				throw new ArgumentNullException("ctx");
			}
			if (msg == null)
			{
				throw new ArgumentNullException("msg");
			}
			bool result = true;
			if (this._flavor == 8)
			{
				result = false;
			}
			else
			{
				SynchronizationAttribute synchronizationAttribute = (SynchronizationAttribute)ctx.GetProperty("Synchronization");
				if ((this._flavor == 1 && synchronizationAttribute != null) || (this._flavor == 4 && synchronizationAttribute == null))
				{
					result = false;
				}
				if (this._flavor == 4)
				{
					this._cliCtxAttr = synchronizationAttribute;
				}
			}
			return result;
		}

		// Token: 0x060058F0 RID: 22768 RVA: 0x001394EC File Offset: 0x001376EC
		[SecurityCritical]
		[ComVisible(true)]
		public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
		{
			if (this._flavor == 1 || this._flavor == 2 || ctorMsg == null)
			{
				return;
			}
			if (this._cliCtxAttr != null)
			{
				ctorMsg.ContextProperties.Add(this._cliCtxAttr);
				this._cliCtxAttr = null;
				return;
			}
			ctorMsg.ContextProperties.Add(this);
		}

		// Token: 0x060058F1 RID: 22769 RVA: 0x00139540 File Offset: 0x00137740
		internal virtual void InitIfNecessary()
		{
			lock (this)
			{
				if (this._asyncWorkEvent == null)
				{
					this._asyncWorkEvent = new AutoResetEvent(false);
					this._workItemQueue = new Queue();
					this._asyncLcidList = new ArrayList();
					WaitOrTimerCallback callBack = new WaitOrTimerCallback(this.DispatcherCallBack);
					this._waitHandle = ThreadPool.RegisterWaitForSingleObject(this._asyncWorkEvent, callBack, null, SynchronizationAttribute._timeOut, false);
				}
			}
		}

		// Token: 0x060058F2 RID: 22770 RVA: 0x001395C8 File Offset: 0x001377C8
		private void DispatcherCallBack(object stateIgnored, bool ignored)
		{
			Queue workItemQueue = this._workItemQueue;
			WorkItem work;
			lock (workItemQueue)
			{
				work = (WorkItem)this._workItemQueue.Dequeue();
			}
			this.ExecuteWorkItem(work);
			this.HandleWorkCompletion();
		}

		// Token: 0x060058F3 RID: 22771 RVA: 0x00139620 File Offset: 0x00137820
		internal virtual void HandleThreadExit()
		{
			this.HandleWorkCompletion();
		}

		// Token: 0x060058F4 RID: 22772 RVA: 0x00139628 File Offset: 0x00137828
		internal virtual void HandleThreadReEntry()
		{
			WorkItem workItem = new WorkItem(null, null, null);
			workItem.SetDummy();
			this.HandleWorkRequest(workItem);
		}

		// Token: 0x060058F5 RID: 22773 RVA: 0x0013964C File Offset: 0x0013784C
		internal virtual void HandleWorkCompletion()
		{
			WorkItem workItem = null;
			bool flag = false;
			Queue workItemQueue = this._workItemQueue;
			lock (workItemQueue)
			{
				if (this._workItemQueue.Count >= 1)
				{
					workItem = (WorkItem)this._workItemQueue.Peek();
					flag = true;
					workItem.SetSignaled();
				}
				else
				{
					this._locked = false;
				}
			}
			if (flag)
			{
				if (workItem.IsAsync())
				{
					this._asyncWorkEvent.Set();
					return;
				}
				WorkItem obj = workItem;
				lock (obj)
				{
					Monitor.Pulse(workItem);
				}
			}
		}

		// Token: 0x060058F6 RID: 22774 RVA: 0x00139704 File Offset: 0x00137904
		internal virtual void HandleWorkRequest(WorkItem work)
		{
			if (!this.IsNestedCall(work._reqMsg))
			{
				if (work.IsAsync())
				{
					bool flag = true;
					Queue workItemQueue = this._workItemQueue;
					lock (workItemQueue)
					{
						work.SetWaiting();
						this._workItemQueue.Enqueue(work);
						if (!this._locked && this._workItemQueue.Count == 1)
						{
							work.SetSignaled();
							this._locked = true;
							this._asyncWorkEvent.Set();
						}
						return;
					}
				}
				lock (work)
				{
					Queue workItemQueue2 = this._workItemQueue;
					bool flag;
					lock (workItemQueue2)
					{
						if (!this._locked && this._workItemQueue.Count == 0)
						{
							this._locked = true;
							flag = false;
						}
						else
						{
							flag = true;
							work.SetWaiting();
							this._workItemQueue.Enqueue(work);
						}
					}
					if (flag)
					{
						Monitor.Wait(work);
						if (!work.IsDummy())
						{
							this.DispatcherCallBack(null, true);
							return;
						}
						Queue workItemQueue3 = this._workItemQueue;
						lock (workItemQueue3)
						{
							this._workItemQueue.Dequeue();
							return;
						}
					}
					if (!work.IsDummy())
					{
						work.SetSignaled();
						this.ExecuteWorkItem(work);
						this.HandleWorkCompletion();
					}
					return;
				}
			}
			work.SetSignaled();
			work.Execute();
		}

		// Token: 0x060058F7 RID: 22775 RVA: 0x001398A4 File Offset: 0x00137AA4
		internal void ExecuteWorkItem(WorkItem work)
		{
			work.Execute();
		}

		// Token: 0x060058F8 RID: 22776 RVA: 0x001398AC File Offset: 0x00137AAC
		internal bool IsNestedCall(IMessage reqMsg)
		{
			bool flag = false;
			if (!this.IsReEntrant)
			{
				string syncCallOutLCID = this.SyncCallOutLCID;
				if (syncCallOutLCID != null)
				{
					LogicalCallContext logicalCallContext = (LogicalCallContext)reqMsg.Properties[Message.CallContextKey];
					if (logicalCallContext != null && syncCallOutLCID.Equals(logicalCallContext.RemotingData.LogicalCallID))
					{
						flag = true;
					}
				}
				if (!flag && this.AsyncCallOutLCIDList.Count > 0)
				{
					LogicalCallContext logicalCallContext2 = (LogicalCallContext)reqMsg.Properties[Message.CallContextKey];
					if (this.AsyncCallOutLCIDList.Contains(logicalCallContext2.RemotingData.LogicalCallID))
					{
						flag = true;
					}
				}
			}
			return flag;
		}

		// Token: 0x060058F9 RID: 22777 RVA: 0x00139940 File Offset: 0x00137B40
		[SecurityCritical]
		public virtual IMessageSink GetServerContextSink(IMessageSink nextSink)
		{
			this.InitIfNecessary();
			return new SynchronizedServerContextSink(this, nextSink);
		}

		// Token: 0x060058FA RID: 22778 RVA: 0x0013995C File Offset: 0x00137B5C
		[SecurityCritical]
		public virtual IMessageSink GetClientContextSink(IMessageSink nextSink)
		{
			this.InitIfNecessary();
			return new SynchronizedClientContextSink(this, nextSink);
		}

		// Token: 0x0400287C RID: 10364
		public const int NOT_SUPPORTED = 1;

		// Token: 0x0400287D RID: 10365
		public const int SUPPORTED = 2;

		// Token: 0x0400287E RID: 10366
		public const int REQUIRED = 4;

		// Token: 0x0400287F RID: 10367
		public const int REQUIRES_NEW = 8;

		// Token: 0x04002880 RID: 10368
		private const string PROPERTY_NAME = "Synchronization";

		// Token: 0x04002881 RID: 10369
		private static readonly int _timeOut = -1;

		// Token: 0x04002882 RID: 10370
		[NonSerialized]
		internal AutoResetEvent _asyncWorkEvent;

		// Token: 0x04002883 RID: 10371
		[NonSerialized]
		private RegisteredWaitHandle _waitHandle;

		// Token: 0x04002884 RID: 10372
		[NonSerialized]
		internal Queue _workItemQueue;

		// Token: 0x04002885 RID: 10373
		[NonSerialized]
		internal bool _locked;

		// Token: 0x04002886 RID: 10374
		internal bool _bReEntrant;

		// Token: 0x04002887 RID: 10375
		internal int _flavor;

		// Token: 0x04002888 RID: 10376
		[NonSerialized]
		private SynchronizationAttribute _cliCtxAttr;

		// Token: 0x04002889 RID: 10377
		[NonSerialized]
		private string _syncLcid;

		// Token: 0x0400288A RID: 10378
		[NonSerialized]
		private ArrayList _asyncLcidList;
	}
}
