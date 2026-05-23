using System;

namespace System.Runtime.Versioning
{
	// Token: 0x02000724 RID: 1828
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class TargetFrameworkAttribute : Attribute
	{
		// Token: 0x06005162 RID: 20834 RVA: 0x0011F0A1 File Offset: 0x0011D2A1
		[__DynamicallyInvokable]
		public TargetFrameworkAttribute(string frameworkName)
		{
			if (frameworkName == null)
			{
				throw new ArgumentNullException("frameworkName");
			}
			this._frameworkName = frameworkName;
		}

		// Token: 0x17000D6D RID: 3437
		// (get) Token: 0x06005163 RID: 20835 RVA: 0x0011F0BE File Offset: 0x0011D2BE
		[__DynamicallyInvokable]
		public string FrameworkName
		{
			[__DynamicallyInvokable]
			get
			{
				return this._frameworkName;
			}
		}

		// Token: 0x17000D6E RID: 3438
		// (get) Token: 0x06005164 RID: 20836 RVA: 0x0011F0C6 File Offset: 0x0011D2C6
		// (set) Token: 0x06005165 RID: 20837 RVA: 0x0011F0CE File Offset: 0x0011D2CE
		[__DynamicallyInvokable]
		public string FrameworkDisplayName
		{
			[__DynamicallyInvokable]
			get
			{
				return this._frameworkDisplayName;
			}
			[__DynamicallyInvokable]
			set
			{
				this._frameworkDisplayName = value;
			}
		}

		// Token: 0x04002424 RID: 9252
		private string _frameworkName;

		// Token: 0x04002425 RID: 9253
		private string _frameworkDisplayName;
	}
}
