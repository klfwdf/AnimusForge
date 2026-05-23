using System;
using System.Collections.Generic;

namespace HarmonyLib
{
	/// <summary>Annotation to define your Harmony patch methods</summary>
	// Token: 0x02000065 RID: 101
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true)]
	public class HarmonyPatch : HarmonyAttribute
	{
		/// <summary>An empty annotation can be used together with TargetMethod(s)</summary>
		// Token: 0x060001D8 RID: 472 RVA: 0x0000D2F9 File Offset: 0x0000B4F9
		public HarmonyPatch()
		{
		}

		/// <summary>An annotation that specifies a class to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		// Token: 0x060001D9 RID: 473 RVA: 0x0000D301 File Offset: 0x0000B501
		public HarmonyPatch(Type declaringType)
		{
			this.info.declaringType = declaringType;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="argumentTypes">The argument types of the method or constructor to patch</param>
		// Token: 0x060001DA RID: 474 RVA: 0x0000D315 File Offset: 0x0000B515
		public HarmonyPatch(Type declaringType, Type[] argumentTypes)
		{
			this.info.declaringType = declaringType;
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		// Token: 0x060001DB RID: 475 RVA: 0x0000D335 File Offset: 0x0000B535
		public HarmonyPatch(Type declaringType, string methodName)
		{
			this.info.declaringType = declaringType;
			this.info.methodName = methodName;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001DC RID: 476 RVA: 0x0000D355 File Offset: 0x0000B555
		public HarmonyPatch(Type declaringType, string methodName, params Type[] argumentTypes)
		{
			this.info.declaringType = declaringType;
			this.info.methodName = methodName;
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001DD RID: 477 RVA: 0x0000D381 File Offset: 0x0000B581
		public HarmonyPatch(Type declaringType, string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			this.info.declaringType = declaringType;
			this.info.methodName = methodName;
			this.ParseSpecialArguments(argumentTypes, argumentVariations);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		// Token: 0x060001DE RID: 478 RVA: 0x0000D3AA File Offset: 0x0000B5AA
		public HarmonyPatch(Type declaringType, MethodType methodType)
		{
			this.info.declaringType = declaringType;
			this.info.methodType = new MethodType?(methodType);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001DF RID: 479 RVA: 0x0000D3CF File Offset: 0x0000B5CF
		public HarmonyPatch(Type declaringType, MethodType methodType, params Type[] argumentTypes)
		{
			this.info.declaringType = declaringType;
			this.info.methodType = new MethodType?(methodType);
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">Array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001E0 RID: 480 RVA: 0x0000D400 File Offset: 0x0000B600
		public HarmonyPatch(Type declaringType, MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			this.info.declaringType = declaringType;
			this.info.methodType = new MethodType?(methodType);
			this.ParseSpecialArguments(argumentTypes, argumentVariations);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="declaringType">The declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		// Token: 0x060001E1 RID: 481 RVA: 0x0000D42E File Offset: 0x0000B62E
		public HarmonyPatch(Type declaringType, string methodName, MethodType methodType)
		{
			this.info.declaringType = declaringType;
			this.info.methodName = methodName;
			this.info.methodType = new MethodType?(methodType);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		// Token: 0x060001E2 RID: 482 RVA: 0x0000D45F File Offset: 0x0000B65F
		public HarmonyPatch(string methodName)
		{
			this.info.methodName = methodName;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001E3 RID: 483 RVA: 0x0000D473 File Offset: 0x0000B673
		public HarmonyPatch(string methodName, params Type[] argumentTypes)
		{
			this.info.methodName = methodName;
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001E4 RID: 484 RVA: 0x0000D493 File Offset: 0x0000B693
		public HarmonyPatch(string methodName, Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			this.info.methodName = methodName;
			this.ParseSpecialArguments(argumentTypes, argumentVariations);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		// Token: 0x060001E5 RID: 485 RVA: 0x0000D4AF File Offset: 0x0000B6AF
		public HarmonyPatch(string methodName, MethodType methodType)
		{
			this.info.methodName = methodName;
			this.info.methodType = new MethodType?(methodType);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		// Token: 0x060001E6 RID: 486 RVA: 0x0000D4D4 File Offset: 0x0000B6D4
		public HarmonyPatch(MethodType methodType)
		{
			this.info.methodType = new MethodType?(methodType);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001E7 RID: 487 RVA: 0x0000D4ED File Offset: 0x0000B6ED
		public HarmonyPatch(MethodType methodType, params Type[] argumentTypes)
		{
			this.info.methodType = new MethodType?(methodType);
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001E8 RID: 488 RVA: 0x0000D512 File Offset: 0x0000B712
		public HarmonyPatch(MethodType methodType, Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			this.info.methodType = new MethodType?(methodType);
			this.ParseSpecialArguments(argumentTypes, argumentVariations);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		// Token: 0x060001E9 RID: 489 RVA: 0x0000D533 File Offset: 0x0000B733
		public HarmonyPatch(Type[] argumentTypes)
		{
			this.info.argumentTypes = argumentTypes;
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="argumentTypes">An array of argument types to target overloads</param>
		/// <param name="argumentVariations">An array of <see cref="T:HarmonyLib.ArgumentType" /></param>
		// Token: 0x060001EA RID: 490 RVA: 0x0000D547 File Offset: 0x0000B747
		public HarmonyPatch(Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			this.ParseSpecialArguments(argumentTypes, argumentVariations);
		}

		/// <summary>An annotation that specifies a method, property or constructor to patch</summary>
		/// <param name="typeName">The full name of the declaring class/type</param>
		/// <param name="methodName">The name of the method, property or constructor to patch</param>
		/// <param name="methodType">The <see cref="T:HarmonyLib.MethodType" /></param>
		// Token: 0x060001EB RID: 491 RVA: 0x0000D557 File Offset: 0x0000B757
		public HarmonyPatch(string typeName, string methodName, MethodType methodType = MethodType.Normal)
		{
			this.info.declaringType = AccessTools.TypeByName(typeName);
			this.info.methodName = methodName;
			this.info.methodType = new MethodType?(methodType);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000D590 File Offset: 0x0000B790
		private void ParseSpecialArguments(Type[] argumentTypes, ArgumentType[] argumentVariations)
		{
			if (argumentVariations == null || argumentVariations.Length == 0)
			{
				this.info.argumentTypes = argumentTypes;
				return;
			}
			if (argumentTypes.Length < argumentVariations.Length)
			{
				throw new ArgumentException("argumentVariations contains more elements than argumentTypes", "argumentVariations");
			}
			List<Type> types = new List<Type>();
			for (int i = 0; i < argumentTypes.Length; i++)
			{
				Type type = argumentTypes[i];
				switch (argumentVariations[i])
				{
				case ArgumentType.Ref:
				case ArgumentType.Out:
					type = type.MakeByRefType();
					break;
				case ArgumentType.Pointer:
					type = type.MakePointerType();
					break;
				}
				types.Add(type);
			}
			this.info.argumentTypes = types.ToArray();
		}
	}
}
