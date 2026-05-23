using System;

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies that the method will not return if the associated Boolean parameter is passed the specified value.
	/// </summary>
	// Token: 0x0200000A RID: 10
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal class DoesNotReturnIfAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute" /> class.
		/// </summary>
		/// <param name="parameterValue">
		/// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
		/// the associated parameter matches this value.
		/// </param>
		// Token: 0x0600000B RID: 11 RVA: 0x000020C5 File Offset: 0x000002C5
		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}

		/// <summary>Gets the condition parameter value.</summary>
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000020D4 File Offset: 0x000002D4
		public bool ParameterValue { get; }
	}
}
