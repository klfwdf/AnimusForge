using System;
using System.Deployment.Internal.Isolation.Manifest;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Hosting
{
	// Token: 0x02000A55 RID: 2645
	internal sealed class ManifestRunner
	{
		// Token: 0x060066BE RID: 26302 RVA: 0x00159D94 File Offset: 0x00157F94
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
		internal ManifestRunner(AppDomain domain, ActivationContext activationContext)
		{
			this.m_domain = domain;
			string text;
			string text2;
			CmsUtils.GetEntryPoint(activationContext, out text, out text2);
			if (string.IsNullOrEmpty(text))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NoMain"));
			}
			if (string.IsNullOrEmpty(text2))
			{
				this.m_args = new string[0];
			}
			else
			{
				this.m_args = text2.Split(new char[] { ' ' });
			}
			this.m_apt = ApartmentState.Unknown;
			string applicationDirectory = activationContext.ApplicationDirectory;
			this.m_path = Path.Combine(applicationDirectory, text);
		}

		// Token: 0x1700118C RID: 4492
		// (get) Token: 0x060066BF RID: 26303 RVA: 0x00159E18 File Offset: 0x00158018
		internal RuntimeAssembly EntryAssembly
		{
			[SecurityCritical]
			[FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
			[SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
			get
			{
				if (this.m_assembly == null)
				{
					this.m_assembly = (RuntimeAssembly)Assembly.LoadFrom(this.m_path);
				}
				return this.m_assembly;
			}
		}

		// Token: 0x060066C0 RID: 26304 RVA: 0x00159E44 File Offset: 0x00158044
		[SecurityCritical]
		private void NewThreadRunner()
		{
			this.m_runResult = this.Run(false);
		}

		// Token: 0x060066C1 RID: 26305 RVA: 0x00159E54 File Offset: 0x00158054
		[SecurityCritical]
		private int RunInNewThread()
		{
			Thread thread = new Thread(new ThreadStart(this.NewThreadRunner));
			thread.SetApartmentState(this.m_apt);
			thread.Start();
			thread.Join();
			return this.m_runResult;
		}

		// Token: 0x060066C2 RID: 26306 RVA: 0x00159E94 File Offset: 0x00158094
		[SecurityCritical]
		private int Run(bool checkAptModel)
		{
			if (checkAptModel && this.m_apt != ApartmentState.Unknown)
			{
				if (Thread.CurrentThread.GetApartmentState() != ApartmentState.Unknown && Thread.CurrentThread.GetApartmentState() != this.m_apt)
				{
					return this.RunInNewThread();
				}
				Thread.CurrentThread.SetApartmentState(this.m_apt);
			}
			return this.m_domain.nExecuteAssembly(this.EntryAssembly, this.m_args);
		}

		// Token: 0x060066C3 RID: 26307 RVA: 0x00159EFC File Offset: 0x001580FC
		[SecurityCritical]
		internal int ExecuteAsAssembly()
		{
			object[] customAttributes = this.EntryAssembly.EntryPoint.GetCustomAttributes(typeof(STAThreadAttribute), false);
			if (customAttributes.Length != 0)
			{
				this.m_apt = ApartmentState.STA;
			}
			customAttributes = this.EntryAssembly.EntryPoint.GetCustomAttributes(typeof(MTAThreadAttribute), false);
			if (customAttributes.Length != 0)
			{
				if (this.m_apt == ApartmentState.Unknown)
				{
					this.m_apt = ApartmentState.MTA;
				}
				else
				{
					this.m_apt = ApartmentState.Unknown;
				}
			}
			return this.Run(true);
		}

		// Token: 0x04002E12 RID: 11794
		private AppDomain m_domain;

		// Token: 0x04002E13 RID: 11795
		private string m_path;

		// Token: 0x04002E14 RID: 11796
		private string[] m_args;

		// Token: 0x04002E15 RID: 11797
		private ApartmentState m_apt;

		// Token: 0x04002E16 RID: 11798
		private RuntimeAssembly m_assembly;

		// Token: 0x04002E17 RID: 11799
		private int m_runResult;
	}
}
