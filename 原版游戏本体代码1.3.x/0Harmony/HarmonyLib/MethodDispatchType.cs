using System;

namespace HarmonyLib
{
	/// <summary>Specifies the type of method call dispatching mechanics</summary>
	// Token: 0x02000062 RID: 98
	public enum MethodDispatchType
	{
		/// <summary>Call the method using dynamic dispatching if method is virtual (including overriden)</summary>
		/// <remarks>
		///     <para>
		/// This is the built-in form of late binding (a.k.a. dynamic binding) and is the default dispatching mechanic in C#.
		/// This directly corresponds with the <see cref="F:System.Reflection.Emit.OpCodes.Callvirt" /> instruction.
		/// </para>
		///     <para>
		/// For virtual (including overriden) methods, the instance type's most-derived/overriden implementation of the method is called.
		/// For non-virtual (including static) methods, same behavior as <see cref="F:HarmonyLib.MethodDispatchType.Call" />: the exact specified method implementation is called.
		/// </para>
		///     <para>
		/// Note: This is not a fully dynamic dispatch, since non-virtual (including static) methods are still called non-virtually.
		/// A fully dynamic dispatch in C# involves using
		/// the <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#the-dynamic-type"><c>dynamic</c> type</see>
		/// (actually a fully dynamic binding, since even the name and overload resolution happens at runtime), which <see cref="T:HarmonyLib.MethodDispatchType" /> does not support.
		/// </para>
		/// </remarks>
		// Token: 0x04000178 RID: 376
		VirtualCall,
		/// <summary>Call the method using static dispatching, regardless of whether method is virtual (including overriden) or non-virtual (including static)</summary>
		/// <remarks>
		///     <para>
		/// a.k.a. non-virtual dispatching, early binding, or static binding.
		/// This directly corresponds with the <see cref="F:System.Reflection.Emit.OpCodes.Call" /> instruction.
		/// </para>
		///     <para>
		/// For both virtual (including overriden) and non-virtual (including static) methods, the exact specified method implementation is called, without virtual/override mechanics.
		/// </para>
		/// </remarks>
		// Token: 0x04000179 RID: 377
		Call
	}
}
