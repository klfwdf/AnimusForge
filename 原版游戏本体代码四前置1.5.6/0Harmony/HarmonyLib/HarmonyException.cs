using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace HarmonyLib;

public class HarmonyException : Exception
{
	private Dictionary<int, CodeInstruction> instructions;

	private int errorOffset;

	internal HarmonyException()
	{
	}

	internal HarmonyException(string message)
		: base(message)
	{
	}

	internal HarmonyException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	internal HarmonyException(Exception innerException, Dictionary<int, CodeInstruction> instructions, int errorOffset)
		: base("IL Compile Error", innerException)
	{
		this.instructions = instructions;
		this.errorOffset = errorOffset;
	}

	internal static Exception Create(Exception ex, Dictionary<int, CodeInstruction> finalInstructions)
	{
		Match match = Regex.Match(ex.Message.TrimEnd(), "Reason: Invalid IL code in.+: IL_(\\d{4}): (.+)$");
		if (!match.Success)
		{
			return ex;
		}
		int num = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
		Regex.Replace(match.Groups[2].Value, " {2,}", " ");
		if (ex is HarmonyException ex2)
		{
			ex2.instructions = finalInstructions;
			ex2.errorOffset = num;
			return ex2;
		}
		return new HarmonyException(ex, finalInstructions, num);
	}

	public List<KeyValuePair<int, CodeInstruction>> GetInstructionsWithOffsets()
	{
		return instructions.OrderBy((KeyValuePair<int, CodeInstruction> ins) => ins.Key).ToList();
	}

	public List<CodeInstruction> GetInstructions()
	{
		return (from ins in instructions
			orderby ins.Key
			select ins.Value).ToList();
	}

	public int GetErrorOffset()
	{
		return errorOffset;
	}

	public int GetErrorIndex()
	{
		if (instructions.TryGetValue(errorOffset, out var value))
		{
			return GetInstructions().IndexOf(value);
		}
		return -1;
	}
}
