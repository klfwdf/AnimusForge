using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HarmonyLib
{
	/// <summary>Under Mono, HarmonyException wraps IL compile errors with detailed information about the failure</summary>
	// Token: 0x02000082 RID: 130
	public class HarmonyException : Exception
	{
		// Token: 0x0600026D RID: 621 RVA: 0x0000EB0A File Offset: 0x0000CD0A
		internal HarmonyException()
		{
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000EB12 File Offset: 0x0000CD12
		internal HarmonyException(string message)
			: base(message)
		{
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0000EB1B File Offset: 0x0000CD1B
		internal HarmonyException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000EB25 File Offset: 0x0000CD25
		internal HarmonyException(Exception innerException, Dictionary<int, CodeInstruction> instructions, int errorOffset)
			: base("IL Compile Error", innerException)
		{
			this.instructions = instructions;
			this.errorOffset = errorOffset;
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000EB44 File Offset: 0x0000CD44
		internal static Exception Create(Exception ex, Dictionary<int, CodeInstruction> finalInstructions)
		{
			Match match = Regex.Match(ex.Message.TrimEnd(Array.Empty<char>()), "Reason: Invalid IL code in.+: IL_(\\d{4}): (.+)$");
			if (!match.Success)
			{
				return ex;
			}
			int offset = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
			Regex.Replace(match.Groups[2].Value, " {2,}", " ");
			HarmonyException hEx = ex as HarmonyException;
			if (hEx != null)
			{
				hEx.instructions = finalInstructions;
				hEx.errorOffset = offset;
				return hEx;
			}
			return new HarmonyException(ex, finalInstructions, offset);
		}

		/// <summary>Get a list of IL instructions in pairs of offset+code</summary>
		/// <returns>A list of key/value pairs which represent an offset and the code at that offset</returns>
		// Token: 0x06000272 RID: 626 RVA: 0x0000EBD5 File Offset: 0x0000CDD5
		public List<KeyValuePair<int, CodeInstruction>> GetInstructionsWithOffsets()
		{
			return (from ins in this.instructions
				orderby ins.Key
				select ins).ToList<KeyValuePair<int, CodeInstruction>>();
		}

		/// <summary>Get a list of IL instructions without offsets</summary>
		/// <returns>A list of <see cref="T:HarmonyLib.CodeInstruction" /></returns>
		// Token: 0x06000273 RID: 627 RVA: 0x0000EC08 File Offset: 0x0000CE08
		public List<CodeInstruction> GetInstructions()
		{
			return (from ins in this.instructions
				orderby ins.Key
				select ins.Value).ToList<CodeInstruction>();
		}

		/// <summary>Get the error offset of the errornous IL instruction</summary>
		/// <returns>The offset</returns>
		// Token: 0x06000274 RID: 628 RVA: 0x0000EC68 File Offset: 0x0000CE68
		public int GetErrorOffset()
		{
			return this.errorOffset;
		}

		/// <summary>Get the index of the errornous IL instruction</summary>
		/// <returns>The index into the list of instructions or -1 if not found</returns>
		// Token: 0x06000275 RID: 629 RVA: 0x0000EC70 File Offset: 0x0000CE70
		public int GetErrorIndex()
		{
			CodeInstruction instruction;
			if (this.instructions.TryGetValue(this.errorOffset, out instruction))
			{
				return this.GetInstructions().IndexOf(instruction);
			}
			return -1;
		}

		// Token: 0x040001A0 RID: 416
		private Dictionary<int, CodeInstruction> instructions;

		// Token: 0x040001A1 RID: 417
		private int errorOffset;
	}
}
