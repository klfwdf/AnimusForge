using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>A collection of commonly used transpilers</summary>
	// Token: 0x020000A1 RID: 161
	public static class Transpilers
	{
		/// <summary>A transpiler that replaces all occurrences of a given method with another one using the same signature</summary>
		/// <param name="instructions">The enumeration of <see cref="T:HarmonyLib.CodeInstruction" /> to act on</param>
		/// <param name="from">Method or constructor to search for</param>
		/// <param name="to">Method or constructor to replace with</param>
		/// <returns>Modified enumeration of <see cref="T:HarmonyLib.CodeInstruction" /></returns>
		// Token: 0x06000334 RID: 820 RVA: 0x000116E2 File Offset: 0x0000F8E2
		public static IEnumerable<CodeInstruction> MethodReplacer(this IEnumerable<CodeInstruction> instructions, MethodBase from, MethodBase to)
		{
			Transpilers.<MethodReplacer>d__0 <MethodReplacer>d__ = new Transpilers.<MethodReplacer>d__0(-2);
			<MethodReplacer>d__.<>3__instructions = instructions;
			<MethodReplacer>d__.<>3__from = from;
			<MethodReplacer>d__.<>3__to = to;
			return <MethodReplacer>d__;
		}

		/// <summary>A transpiler that alters instructions that match a predicate by calling an action</summary>
		/// <param name="instructions">The enumeration of <see cref="T:HarmonyLib.CodeInstruction" /> to act on</param>
		/// <param name="predicate">A predicate selecting the instructions to change</param>
		/// <param name="action">An action to apply to matching instructions</param>
		/// <returns>Modified enumeration of <see cref="T:HarmonyLib.CodeInstruction" /></returns>
		// Token: 0x06000335 RID: 821 RVA: 0x00011700 File Offset: 0x0000F900
		public static IEnumerable<CodeInstruction> Manipulator(this IEnumerable<CodeInstruction> instructions, Func<CodeInstruction, bool> predicate, Action<CodeInstruction> action)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException("predicate");
			}
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			return instructions.Select(delegate(CodeInstruction instruction)
			{
				if (predicate(instruction))
				{
					action(instruction);
				}
				return instruction;
			}).AsEnumerable<CodeInstruction>();
		}

		/// <summary>A transpiler that logs a text at the beginning of the method</summary>
		/// <param name="instructions">The instructions to act on</param>
		/// <param name="text">The log text</param>
		/// <returns>Modified enumeration of <see cref="T:HarmonyLib.CodeInstruction" /></returns>
		// Token: 0x06000336 RID: 822 RVA: 0x0001175E File Offset: 0x0000F95E
		public static IEnumerable<CodeInstruction> DebugLogger(this IEnumerable<CodeInstruction> instructions, string text)
		{
			Transpilers.<DebugLogger>d__2 <DebugLogger>d__ = new Transpilers.<DebugLogger>d__2(-2);
			<DebugLogger>d__.<>3__instructions = instructions;
			<DebugLogger>d__.<>3__text = text;
			return <DebugLogger>d__;
		}
	}
}
