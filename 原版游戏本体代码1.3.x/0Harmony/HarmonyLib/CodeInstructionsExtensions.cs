using System;
using System.Collections.Generic;

namespace HarmonyLib
{
	/// <summary>Extensions for a sequence of <see cref="T:HarmonyLib.CodeInstruction" /></summary>
	// Token: 0x020001C2 RID: 450
	public static class CodeInstructionsExtensions
	{
		/// <summary>Searches a list of <see cref="T:HarmonyLib.CodeInstruction" /> by running a sequence of <see cref="T:HarmonyLib.CodeMatch" /> against it</summary>
		/// <param name="instructions">The CodeInstructions (like a body of a method) to search in</param>
		/// <param name="matches">An array of <see cref="T:HarmonyLib.CodeMatch" /> representing the sequence of codes you want to search for</param>
		/// <returns />
		// Token: 0x060007DB RID: 2011 RVA: 0x00019D0F File Offset: 0x00017F0F
		public static bool Matches(this IEnumerable<CodeInstruction> instructions, CodeMatch[] matches)
		{
			return new CodeMatcher(instructions, null).MatchStartForward(matches).IsValid;
		}
	}
}
