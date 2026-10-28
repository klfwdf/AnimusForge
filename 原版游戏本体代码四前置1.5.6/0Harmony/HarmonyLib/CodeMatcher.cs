using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib;

public class CodeMatcher
{
	public delegate bool ErrorHandler(CodeMatcher matcher, string error);

	private enum MatchPosition
	{
		Start,
		End
	}

	private delegate CodeMatcher MatchDelegate();

	private readonly ILGenerator generator;

	private readonly List<CodeInstruction> codes = new List<CodeInstruction>();

	private Dictionary<string, CodeInstruction> lastMatches = new Dictionary<string, CodeInstruction>();

	private string lastError;

	private MatchDelegate lastMatchCall;

	private ErrorHandler errorHandler;

	public int Pos { get; private set; } = -1;

	public int Length => codes.Count;

	public bool IsValid
	{
		get
		{
			if (Pos >= 0)
			{
				return Pos < Length;
			}
			return false;
		}
	}

	public bool IsInvalid
	{
		get
		{
			if (Pos >= 0)
			{
				return Pos >= Length;
			}
			return true;
		}
	}

	public int Remaining => Length - Math.Max(0, Pos);

	public ref OpCode Opcode => ref codes[Pos].opcode;

	public ref object Operand => ref codes[Pos].operand;

	public ref List<Label> Labels => ref codes[Pos].labels;

	public ref List<ExceptionBlock> Blocks => ref codes[Pos].blocks;

	public CodeInstruction Instruction => codes[Pos];

	private void FixStart()
	{
		Pos = Math.Max(0, Pos);
	}

	private T HandleException<T>(string error, T defaultValue)
	{
		if (errorHandler != null && errorHandler(this, error))
		{
			return defaultValue;
		}
		lastError = error;
		throw new InvalidOperationException(error);
	}

	private void HandleException(string error)
	{
		lastError = error;
		if (errorHandler != null)
		{
			errorHandler(this, error);
			return;
		}
		throw new InvalidOperationException(error);
	}

	private void SetOutOfBounds(int direction)
	{
		Pos = ((direction > 0) ? Length : (-1));
	}

	public CodeMatcher()
	{
	}

	public CodeMatcher(IEnumerable<CodeInstruction> instructions, ILGenerator generator = null)
	{
		this.generator = generator;
		codes = instructions.Select((CodeInstruction c) => new CodeInstruction(c)).ToList();
	}

	public CodeMatcher Clone()
	{
		return new CodeMatcher(codes, generator)
		{
			Pos = Pos,
			lastMatches = new Dictionary<string, CodeInstruction>(lastMatches),
			lastError = lastError,
			lastMatchCall = lastMatchCall,
			errorHandler = errorHandler
		};
	}

	public CodeMatcher Reset(bool atFirstInstruction = true)
	{
		Pos = ((!atFirstInstruction) ? (-1) : 0);
		lastMatches.Clear();
		lastError = null;
		lastMatchCall = null;
		return this;
	}

	public CodeInstruction InstructionAt(int offset)
	{
		return codes[Pos + offset];
	}

	public List<CodeInstruction> Instructions()
	{
		return codes;
	}

	public IEnumerable<CodeInstruction> InstructionEnumeration()
	{
		return codes.AsEnumerable();
	}

	public List<CodeInstruction> Instructions(int count)
	{
		if (Pos < 0 || Pos + count > Length)
		{
			return HandleException("Cannot retrieve instructions: range is out-of-bounds.", new List<CodeInstruction>());
		}
		return (from c in codes.GetRange(Pos, count)
			select new CodeInstruction(c)).ToList();
	}

	public List<CodeInstruction> InstructionsInRange(int start, int end)
	{
		List<CodeInstruction> list = codes;
		if (start > end)
		{
			int num = start;
			start = end;
			end = num;
		}
		if (start < 0 || end >= Length)
		{
			return HandleException("Cannot retrieve instructions: range is out-of-bounds.", new List<CodeInstruction>());
		}
		list = list.GetRange(start, end - start + 1);
		return list.Select((CodeInstruction c) => new CodeInstruction(c)).ToList();
	}

	public List<CodeInstruction> InstructionsWithOffsets(int startOffset, int endOffset)
	{
		return InstructionsInRange(Pos + startOffset, Pos + endOffset);
	}

	public List<Label> DistinctLabels(IEnumerable<CodeInstruction> instructions)
	{
		return instructions.SelectMany((CodeInstruction instruction) => instruction.labels).Distinct().ToList();
	}

	public bool ReportFailure(MethodBase method, Action<string> logger)
	{
		if (IsValid)
		{
			return false;
		}
		string value = lastError ?? "Unexpected code";
		logger($"{value} in {method}");
		return true;
	}

	public CodeMatcher ThrowIfInvalid(string explanation)
	{
		if (explanation == null)
		{
			throw new ArgumentNullException("explanation");
		}
		if (IsInvalid)
		{
			return HandleException(explanation + " - Current state is invalid", this);
		}
		return this;
	}

	public CodeMatcher ThrowIfNotMatch(string explanation, params CodeMatch[] matches)
	{
		ThrowIfInvalid(explanation);
		if (!MatchSequence(Pos, matches))
		{
			return HandleException(explanation + " - Match failed", this);
		}
		return this;
	}

	private void ThrowIfNotMatch(string explanation, int direction, CodeMatch[] matches)
	{
		ThrowIfInvalid(explanation);
		int pos = Pos;
		try
		{
			if (Match(matches, direction, MatchPosition.Start, prepareOnly: false).IsInvalid)
			{
				HandleException(explanation + " - Match failed");
			}
		}
		finally
		{
			Pos = pos;
		}
	}

	public CodeMatcher ThrowIfNotMatchForward(string explanation, params CodeMatch[] matches)
	{
		ThrowIfNotMatch(explanation, 1, matches);
		return this;
	}

	public CodeMatcher ThrowIfNotMatchBack(string explanation, params CodeMatch[] matches)
	{
		ThrowIfNotMatch(explanation, -1, matches);
		return this;
	}

	public CodeMatcher ThrowIfFalse(string explanation, Func<CodeMatcher, bool> stateCheckFunc)
	{
		if (stateCheckFunc == null)
		{
			throw new ArgumentNullException("stateCheckFunc");
		}
		ThrowIfInvalid(explanation);
		if (!stateCheckFunc(this))
		{
			return HandleException(explanation + " - Check function returned false", this);
		}
		return this;
	}

	public CodeMatcher Do(Action<CodeMatcher> action)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		action(this);
		return this;
	}

	public CodeMatcher OnError(ErrorHandler errorHandler)
	{
		this.errorHandler = errorHandler;
		return this;
	}

	public CodeMatcher SetInstruction(CodeInstruction instruction)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot set instruction/opcode at invalid position.", this);
		}
		codes[Pos] = instruction;
		return this;
	}

	public CodeMatcher SetInstructionAndAdvance(CodeInstruction instruction)
	{
		SetInstruction(instruction);
		Pos++;
		return this;
	}

	public CodeMatcher Set(OpCode opcode, object operand)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot set values at invalid position.", this);
		}
		Opcode = opcode;
		Operand = operand;
		return this;
	}

	public CodeMatcher SetAndAdvance(OpCode opcode, object operand)
	{
		Set(opcode, operand);
		Pos++;
		return this;
	}

	public CodeMatcher SetOpcodeAndAdvance(OpCode opcode)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot set opcode at invalid position.", this);
		}
		Opcode = opcode;
		Pos++;
		return this;
	}

	public CodeMatcher SetOperandAndAdvance(object operand)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot set operand at invalid position.", this);
		}
		Operand = operand;
		Pos++;
		return this;
	}

	public CodeMatcher DeclareLocal(Type variableType, out LocalBuilder localVariable)
	{
		if (generator == null)
		{
			localVariable = null;
			return HandleException("Generator must be provided to use this method", this);
		}
		localVariable = generator.DeclareLocal(variableType);
		return this;
	}

	public CodeMatcher DefineLabel(out Label label)
	{
		if (generator == null)
		{
			label = default(Label);
			return HandleException("Generator must be provided to use this method", this);
		}
		label = generator.DefineLabel();
		return this;
	}

	public CodeMatcher CreateLabel(out Label label)
	{
		if (generator == null)
		{
			label = default(Label);
			return HandleException("Generator must be provided to use this method", this);
		}
		label = generator.DefineLabel();
		Labels.Add(label);
		return this;
	}

	public CodeMatcher CreateLabelAt(int position, out Label label)
	{
		if (generator == null)
		{
			label = default(Label);
			return HandleException("Generator must be provided to use this method", this);
		}
		label = generator.DefineLabel();
		AddLabelsAt(position, new _003C_003Ez__ReadOnlySingleElementList<Label>(label));
		return this;
	}

	public CodeMatcher CreateLabelWithOffsets(int offset, out Label label)
	{
		if (generator == null)
		{
			label = default(Label);
			return HandleException("Generator must be provided to use this method", this);
		}
		label = generator.DefineLabel();
		return AddLabelsAt(Pos + offset, new _003C_003Ez__ReadOnlySingleElementList<Label>(label));
	}

	public CodeMatcher AddLabels(IEnumerable<Label> labels)
	{
		Labels.AddRange(labels);
		return this;
	}

	public CodeMatcher AddLabelsAt(int position, IEnumerable<Label> labels)
	{
		if (position < 0 || position >= Length)
		{
			return HandleException("Cannot add labels at invalid position.", this);
		}
		codes[position].labels.AddRange(labels);
		return this;
	}

	public CodeMatcher SetJumpTo(OpCode opcode, int destination, out Label label)
	{
		CreateLabelAt(destination, out label);
		return Set(opcode, label);
	}

	public CodeMatcher Insert(params CodeInstruction[] instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			throw new ArgumentNullException("instructions");
		}
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		codes.InsertRange(Pos, instructions);
		return this;
	}

	public CodeMatcher Insert(IEnumerable<CodeInstruction> instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			throw new ArgumentNullException("instructions");
		}
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		codes.InsertRange(Pos, instructions);
		return this;
	}

	public CodeMatcher InsertBranch(OpCode opcode, int destination)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		CreateLabelAt(destination, out var label);
		codes.Insert(Pos, new CodeInstruction(opcode, label));
		return this;
	}

	public CodeMatcher InsertAndAdvance(params CodeInstruction[] instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			throw new ArgumentNullException("instructions");
		}
		foreach (CodeInstruction codeInstruction in instructions)
		{
			Insert(codeInstruction);
			Pos++;
		}
		return this;
	}

	public CodeMatcher InsertAndAdvance(IEnumerable<CodeInstruction> instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			throw new ArgumentNullException("instructions");
		}
		foreach (CodeInstruction instruction in instructions)
		{
			InsertAndAdvance(instruction);
		}
		return this;
	}

	public CodeMatcher InsertBranchAndAdvance(OpCode opcode, int destination)
	{
		InsertBranch(opcode, destination);
		Pos++;
		return this;
	}

	public CodeMatcher InsertAfter(params CodeInstruction[] instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			throw new ArgumentNullException("instructions");
		}
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		codes.InsertRange(Pos + 1, instructions);
		return this;
	}

	public CodeMatcher InsertAfter(IEnumerable<CodeInstruction> instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			return HandleException("Cannot insert null instructions.", this);
		}
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		codes.InsertRange(Pos + 1, instructions);
		return this;
	}

	public CodeMatcher InsertBranchAfter(OpCode opcode, int destination)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot insert instructions at invalid position.", this);
		}
		CreateLabelAt(destination, out var label);
		codes.Insert(Pos + 1, new CodeInstruction(opcode, label));
		return this;
	}

	public CodeMatcher InsertAfterAndAdvance(params CodeInstruction[] instructions)
	{
		InsertAfter(instructions);
		Pos += instructions.Length;
		return this;
	}

	public CodeMatcher InsertAfterAndAdvance(IEnumerable<CodeInstruction> instructions)
	{
		if (instructions == null || instructions.Any((CodeInstruction i) => i == null))
		{
			return HandleException("Cannot insert null instructions.", this);
		}
		List<CodeInstruction> list = instructions.ToList();
		InsertAfter(list);
		Pos += list.Count;
		return this;
	}

	public CodeMatcher InsertBranchAfterAndAdvance(OpCode opcode, int destination)
	{
		InsertBranchAfter(opcode, destination);
		Pos++;
		return this;
	}

	public CodeMatcher RemoveInstruction()
	{
		if (IsInvalid)
		{
			return HandleException("Cannot remove instructions from an invalid position.", this);
		}
		codes.RemoveAt(Pos);
		return this;
	}

	public CodeMatcher RemoveInstructions(int count)
	{
		if (IsInvalid || Pos + count > Length)
		{
			return HandleException("Cannot remove instructions from an invalid or out-of-range position.", this);
		}
		codes.RemoveRange(Pos, count);
		return this;
	}

	public CodeMatcher RemoveInstructionsInRange(int start, int end)
	{
		if (start > end)
		{
			int num = start;
			start = end;
			end = num;
		}
		if (start < 0 || end >= Length)
		{
			return HandleException("Cannot remove instructions: range is out-of-bounds.", this);
		}
		codes.RemoveRange(start, end - start + 1);
		return this;
	}

	public CodeMatcher RemoveInstructionsWithOffsets(int startOffset, int endOffset)
	{
		return RemoveInstructionsInRange(Pos + startOffset, Pos + endOffset);
	}

	public CodeMatcher Advance(int offset = 1)
	{
		Pos += offset;
		if (!IsValid)
		{
			SetOutOfBounds(offset);
		}
		return this;
	}

	public CodeMatcher Start()
	{
		Pos = 0;
		return this;
	}

	public CodeMatcher End()
	{
		Pos = Length - 1;
		return this;
	}

	public CodeMatcher SearchForward(Func<CodeInstruction, bool> predicate)
	{
		return Search(predicate, 1);
	}

	public CodeMatcher SearchBackwards(Func<CodeInstruction, bool> predicate)
	{
		return Search(predicate, -1);
	}

	private CodeMatcher Search(Func<CodeInstruction, bool> predicate, int direction)
	{
		FixStart();
		while (IsValid && !predicate(Instruction))
		{
			Pos += direction;
		}
		lastError = (IsInvalid ? $"Cannot find {predicate}" : null);
		return this;
	}

	public CodeMatcher MatchStartForward(params CodeMatch[] matches)
	{
		return Match(matches, 1, MatchPosition.Start, prepareOnly: false);
	}

	public CodeMatcher PrepareMatchStartForward(params CodeMatch[] matches)
	{
		return Match(matches, 1, MatchPosition.Start, prepareOnly: true);
	}

	public CodeMatcher MatchEndForward(params CodeMatch[] matches)
	{
		return Match(matches, 1, MatchPosition.End, prepareOnly: false);
	}

	public CodeMatcher PrepareMatchEndForward(params CodeMatch[] matches)
	{
		return Match(matches, 1, MatchPosition.End, prepareOnly: true);
	}

	public CodeMatcher MatchStartBackwards(params CodeMatch[] matches)
	{
		return Match(matches, -1, MatchPosition.Start, prepareOnly: false);
	}

	public CodeMatcher PrepareMatchStartBackwards(params CodeMatch[] matches)
	{
		return Match(matches, -1, MatchPosition.Start, prepareOnly: true);
	}

	public CodeMatcher MatchEndBackwards(params CodeMatch[] matches)
	{
		return Match(matches, -1, MatchPosition.End, prepareOnly: false);
	}

	public CodeMatcher PrepareMatchEndBackwards(params CodeMatch[] matches)
	{
		return Match(matches, -1, MatchPosition.End, prepareOnly: true);
	}

	public CodeMatcher RemoveSearchForward(Func<CodeInstruction, bool> predicate)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot remove instructions from an invalid position.", this);
		}
		int pos = Pos;
		CodeMatcher codeMatcher = Clone().SearchForward(predicate);
		if (codeMatcher.IsInvalid)
		{
			lastError = codeMatcher.lastError;
			SetOutOfBounds(1);
			return this;
		}
		int num = codeMatcher.Pos - 1;
		if (num >= pos)
		{
			RemoveInstructionsInRange(pos, num);
		}
		return this;
	}

	public CodeMatcher RemoveSearchBackward(Func<CodeInstruction, bool> predicate)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot remove instructions from an invalid position.", this);
		}
		int pos = Pos;
		CodeMatcher codeMatcher = Clone().SearchBackwards(predicate);
		if (codeMatcher.IsInvalid)
		{
			lastError = codeMatcher.lastError;
			SetOutOfBounds(-1);
			return this;
		}
		int pos2 = codeMatcher.Pos;
		int num = pos2 + 1;
		if (pos >= num)
		{
			RemoveInstructionsInRange(num, pos);
		}
		Pos = pos2;
		return this;
	}

	public CodeMatcher RemoveUntilForward(params CodeMatch[] matches)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot remove instructions from an invalid position.", this);
		}
		int pos = Pos;
		CodeMatcher codeMatcher = Clone().MatchStartForward(matches);
		if (codeMatcher.IsInvalid)
		{
			lastError = codeMatcher.lastError;
			SetOutOfBounds(1);
			return this;
		}
		int num = codeMatcher.Pos - 1;
		if (num >= pos)
		{
			RemoveInstructionsInRange(pos, num);
		}
		return this;
	}

	public CodeMatcher RemoveUntilBackward(params CodeMatch[] matches)
	{
		if (IsInvalid)
		{
			return HandleException("Cannot remove instructions from an invalid position.", this);
		}
		int pos = Pos;
		CodeMatcher codeMatcher = Clone().MatchEndBackwards(matches);
		if (codeMatcher.IsInvalid)
		{
			lastError = codeMatcher.lastError;
			SetOutOfBounds(-1);
			return this;
		}
		int pos2 = codeMatcher.Pos;
		if (pos > pos2)
		{
			RemoveInstructionsInRange(pos2 + 1, pos);
		}
		Pos = pos2;
		return this;
	}

	private CodeMatcher Match(CodeMatch[] matches, int direction, MatchPosition mode, bool prepareOnly)
	{
		lastMatchCall = delegate
		{
			while (IsValid)
			{
				if (MatchSequence(Pos, matches))
				{
					if (mode == MatchPosition.End)
					{
						Pos += matches.Length - 1;
					}
					break;
				}
				Pos += direction;
			}
			lastError = (IsInvalid ? ("Cannot find " + matches.Join()) : null);
			return this;
		};
		if (prepareOnly)
		{
			return this;
		}
		FixStart();
		return lastMatchCall();
	}

	public CodeMatcher Repeat(Action<CodeMatcher> matchAction, Action<string> notFoundAction = null)
	{
		int num = 0;
		if (lastMatchCall == null)
		{
			return HandleException("No previous Match operation - cannot repeat", this);
		}
		while (IsValid)
		{
			matchAction(this);
			lastMatchCall();
			num++;
		}
		lastMatchCall = null;
		if (num == 0)
		{
			notFoundAction?.Invoke(lastError);
		}
		return this;
	}

	public CodeInstruction NamedMatch(string name)
	{
		return lastMatches[name];
	}

	private bool MatchSequence(int start, CodeMatch[] matches)
	{
		if (start < 0)
		{
			return false;
		}
		lastMatches = new Dictionary<string, CodeInstruction>();
		foreach (CodeMatch codeMatch in matches)
		{
			if (start >= Length || !codeMatch.Matches(codes, codes[start]))
			{
				return false;
			}
			if (codeMatch.name != null)
			{
				lastMatches.Add(codeMatch.name, codes[start]);
			}
			start++;
		}
		return true;
	}
}
