using System;

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>Specifies that when a method returns <see cref="P:System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.ReturnValue" />, the parameter will not be null even if the corresponding type allows it.</summary>
	// Token: 0x02000007 RID: 7
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		// Token: 0x06000007 RID: 7 RVA: 0x0000209E File Offset: 0x0000029E
		public NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}

		/// <summary>Gets the return value condition.</summary>
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020AD File Offset: 0x000002AD
		public bool ReturnValue { get; }
	}
}
