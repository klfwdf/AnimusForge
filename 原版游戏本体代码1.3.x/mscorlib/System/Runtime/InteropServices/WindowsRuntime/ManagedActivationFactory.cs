using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F2 RID: 2546
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	internal sealed class ManagedActivationFactory : IActivationFactory, IManagedActivationFactory
	{
		// Token: 0x060064BF RID: 25791 RVA: 0x00157330 File Offset: 0x00155530
		[SecurityCritical]
		internal ManagedActivationFactory(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!(type is RuntimeType) || !type.IsExportedToWindowsRuntime)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotActivatableViaWindowsRuntime", new object[] { type }), "type");
			}
			this.m_type = type;
		}

		// Token: 0x060064C0 RID: 25792 RVA: 0x00157390 File Offset: 0x00155590
		public object ActivateInstance()
		{
			object result;
			try
			{
				result = Activator.CreateInstance(this.m_type);
			}
			catch (MissingMethodException)
			{
				throw new NotImplementedException();
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
			return result;
		}

		// Token: 0x060064C1 RID: 25793 RVA: 0x001573D8 File Offset: 0x001555D8
		void IManagedActivationFactory.RunClassConstructor()
		{
			RuntimeHelpers.RunClassConstructor(this.m_type.TypeHandle);
		}

		// Token: 0x04002CFD RID: 11517
		private Type m_type;
	}
}
