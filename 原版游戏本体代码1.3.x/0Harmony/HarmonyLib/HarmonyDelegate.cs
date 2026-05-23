using System;

namespace HarmonyLib
{
	/// <summary>Annotation to define the original method for delegate injection</summary>
	// Token: 0x02000066 RID: 102
	[AttributeUsage(AttributeTargets.Delegate, AllowMultiple = true)]
	public class HarmonyDelegate : HarmonyPatch
	{
		/// <summary>An annotation that specifies a class to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		// Token: 0x060001ED RID: 493 RVA: 0x0000D625 File Offset: 0x0000B825
		public HarmonyDelegate(Type declaringType)
			: base(declaringType)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="argumentTypes">The argument types of the method or constructor to patch</param>
		// Token: 0x060001EE RID: 494 RVA: 0x0000D62E File Offset: 0x0000B82E
		public HarmonyDelegate(Type declaringType, Type[] argumentTypes)
			: base(declaringType, argumentTypes)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		// Token: 0x060001EF RID: 495 RVA: 0x0000D638 File Offset: 0x0000B838
		public HarmonyDelegate(Type declaringType, string methodName)
			: base(declaringType, methodName)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001F0 RID: 496 RVA: 0x0000D642 File Offset: 0x0000B842
		public HarmonyDelegate(Type declaringType, string methodName, params Type[] argumentTypes)
			: base(declaringType, methodName, argumentTypes)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001F1 RID: 497 RVA: 0x0000D64D File Offset: 0x0000B84D
		public HarmonyDelegate(Type declaringType, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations)
			: base(declaringType, methodName, argumentTypes, argumentVariations)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		// Token: 0x060001F2 RID: 498 RVA: 0x0000D65A File Offset: 0x0000B85A
		public HarmonyDelegate(Type declaringType, MethodDispatchType methodDispatchType)
			: base(declaringType, MethodType.Normal)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001F3 RID: 499 RVA: 0x0000D673 File Offset: 0x0000B873
		public HarmonyDelegate(Type declaringType, MethodDispatchType methodDispatchType, params Type[] argumentTypes)
			: base(declaringType, MethodType.Normal, argumentTypes)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001F4 RID: 500 RVA: 0x0000D68D File Offset: 0x0000B88D
		public HarmonyDelegate(Type declaringType, MethodDispatchType methodDispatchType, Type[] argumentTypes, ArgumentType[] argumentVariations)
			: base(declaringType, MethodType.Normal, argumentTypes, argumentVariations)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		// Token: 0x060001F5 RID: 501 RVA: 0x0000D6A9 File Offset: 0x0000B8A9
		public HarmonyDelegate(Type declaringType, string methodName, MethodDispatchType methodDispatchType)
			: base(declaringType, methodName, MethodType.Normal)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		// Token: 0x060001F6 RID: 502 RVA: 0x0000D6C3 File Offset: 0x0000B8C3
		public HarmonyDelegate(string methodName)
			: base(methodName)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001F7 RID: 503 RVA: 0x0000D6CC File Offset: 0x0000B8CC
		public HarmonyDelegate(string methodName, params Type[] argumentTypes)
			: base(methodName, argumentTypes)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001F8 RID: 504 RVA: 0x0000D6D6 File Offset: 0x0000B8D6
		public HarmonyDelegate(string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations)
			: base(methodName, argumentTypes, argumentVariations)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		// Token: 0x060001F9 RID: 505 RVA: 0x0000D6E1 File Offset: 0x0000B8E1
		public HarmonyDelegate(string methodName, MethodDispatchType methodDispatchType)
			: base(methodName, MethodType.Normal)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies call dispatching mechanics for the delegate</summary>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		// Token: 0x060001FA RID: 506 RVA: 0x0000D6FA File Offset: 0x0000B8FA
		public HarmonyDelegate(MethodDispatchType methodDispatchType)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001FB RID: 507 RVA: 0x0000D711 File Offset: 0x0000B911
		public HarmonyDelegate(MethodDispatchType methodDispatchType, params Type[] argumentTypes)
			: base(MethodType.Normal, argumentTypes)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodDispatchType">The <see cref="T:HarmonyLib.MethodDispatchType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001FC RID: 508 RVA: 0x0000D72A File Offset: 0x0000B92A
		public HarmonyDelegate(MethodDispatchType methodDispatchType, Type[] argumentTypes, ArgumentType[] argumentVariations)
			: base(MethodType.Normal, argumentTypes, argumentVariations)
		{
			this.info.nonVirtualDelegate = methodDispatchType == MethodDispatchType.Call;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001FD RID: 509 RVA: 0x0000D744 File Offset: 0x0000B944
		public HarmonyDelegate(Type[] argumentTypes)
			: base(argumentTypes)
		{
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001FE RID: 510 RVA: 0x0000D74D File Offset: 0x0000B94D
		public HarmonyDelegate(Type[] argumentTypes, ArgumentType[] argumentVariations)
			: base(argumentTypes, argumentVariations)
		{
		}
	}
}
