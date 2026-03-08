using System;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007AE RID: 1966
	internal class DomainSpecificRemotingData
	{
		// Token: 0x0600550F RID: 21775 RVA: 0x0012E3DC File Offset: 0x0012C5DC
		internal DomainSpecificRemotingData()
		{
			this._flags = 0;
			this._ConfigLock = new object();
			this._ChannelServicesData = new ChannelServicesData();
			this._IDTableLock = new ReaderWriterLock();
			this._appDomainProperties = new IContextProperty[1];
			this._appDomainProperties[0] = new LeaseLifeTimeServiceProperty();
		}

		// Token: 0x17000DF6 RID: 3574
		// (get) Token: 0x06005510 RID: 21776 RVA: 0x0012E430 File Offset: 0x0012C630
		// (set) Token: 0x06005511 RID: 21777 RVA: 0x0012E438 File Offset: 0x0012C638
		internal LeaseManager LeaseManager
		{
			get
			{
				return this._LeaseManager;
			}
			set
			{
				this._LeaseManager = value;
			}
		}

		// Token: 0x17000DF7 RID: 3575
		// (get) Token: 0x06005512 RID: 21778 RVA: 0x0012E441 File Offset: 0x0012C641
		internal object ConfigLock
		{
			get
			{
				return this._ConfigLock;
			}
		}

		// Token: 0x17000DF8 RID: 3576
		// (get) Token: 0x06005513 RID: 21779 RVA: 0x0012E449 File Offset: 0x0012C649
		internal ReaderWriterLock IDTableLock
		{
			get
			{
				return this._IDTableLock;
			}
		}

		// Token: 0x17000DF9 RID: 3577
		// (get) Token: 0x06005514 RID: 21780 RVA: 0x0012E451 File Offset: 0x0012C651
		// (set) Token: 0x06005515 RID: 21781 RVA: 0x0012E459 File Offset: 0x0012C659
		internal LocalActivator LocalActivator
		{
			[SecurityCritical]
			get
			{
				return this._LocalActivator;
			}
			[SecurityCritical]
			set
			{
				this._LocalActivator = value;
			}
		}

		// Token: 0x17000DFA RID: 3578
		// (get) Token: 0x06005516 RID: 21782 RVA: 0x0012E462 File Offset: 0x0012C662
		// (set) Token: 0x06005517 RID: 21783 RVA: 0x0012E46A File Offset: 0x0012C66A
		internal ActivationListener ActivationListener
		{
			get
			{
				return this._ActivationListener;
			}
			set
			{
				this._ActivationListener = value;
			}
		}

		// Token: 0x17000DFB RID: 3579
		// (get) Token: 0x06005518 RID: 21784 RVA: 0x0012E473 File Offset: 0x0012C673
		// (set) Token: 0x06005519 RID: 21785 RVA: 0x0012E480 File Offset: 0x0012C680
		internal bool InitializingActivation
		{
			get
			{
				return (this._flags & 1) == 1;
			}
			set
			{
				if (value)
				{
					this._flags |= 1;
					return;
				}
				this._flags &= -2;
			}
		}

		// Token: 0x17000DFC RID: 3580
		// (get) Token: 0x0600551A RID: 21786 RVA: 0x0012E4A3 File Offset: 0x0012C6A3
		// (set) Token: 0x0600551B RID: 21787 RVA: 0x0012E4B0 File Offset: 0x0012C6B0
		internal bool ActivationInitialized
		{
			get
			{
				return (this._flags & 2) == 2;
			}
			set
			{
				if (value)
				{
					this._flags |= 2;
					return;
				}
				this._flags &= -3;
			}
		}

		// Token: 0x17000DFD RID: 3581
		// (get) Token: 0x0600551C RID: 21788 RVA: 0x0012E4D3 File Offset: 0x0012C6D3
		// (set) Token: 0x0600551D RID: 21789 RVA: 0x0012E4E0 File Offset: 0x0012C6E0
		internal bool ActivatorListening
		{
			get
			{
				return (this._flags & 4) == 4;
			}
			set
			{
				if (value)
				{
					this._flags |= 4;
					return;
				}
				this._flags &= -5;
			}
		}

		// Token: 0x17000DFE RID: 3582
		// (get) Token: 0x0600551E RID: 21790 RVA: 0x0012E503 File Offset: 0x0012C703
		internal IContextProperty[] AppDomainContextProperties
		{
			get
			{
				return this._appDomainProperties;
			}
		}

		// Token: 0x17000DFF RID: 3583
		// (get) Token: 0x0600551F RID: 21791 RVA: 0x0012E50B File Offset: 0x0012C70B
		internal ChannelServicesData ChannelServicesData
		{
			get
			{
				return this._ChannelServicesData;
			}
		}

		// Token: 0x0400272C RID: 10028
		private const int ACTIVATION_INITIALIZING = 1;

		// Token: 0x0400272D RID: 10029
		private const int ACTIVATION_INITIALIZED = 2;

		// Token: 0x0400272E RID: 10030
		private const int ACTIVATOR_LISTENING = 4;

		// Token: 0x0400272F RID: 10031
		[SecurityCritical]
		private LocalActivator _LocalActivator;

		// Token: 0x04002730 RID: 10032
		private ActivationListener _ActivationListener;

		// Token: 0x04002731 RID: 10033
		private IContextProperty[] _appDomainProperties;

		// Token: 0x04002732 RID: 10034
		private int _flags;

		// Token: 0x04002733 RID: 10035
		private object _ConfigLock;

		// Token: 0x04002734 RID: 10036
		private ChannelServicesData _ChannelServicesData;

		// Token: 0x04002735 RID: 10037
		private LeaseManager _LeaseManager;

		// Token: 0x04002736 RID: 10038
		private ReaderWriterLock _IDTableLock;
	}
}
