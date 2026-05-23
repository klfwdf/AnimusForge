using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib
{
	/// <summary>A CodeInstruction match</summary>
	// Token: 0x020001A8 RID: 424
	public class CodeMatch : CodeInstruction
	{
		/// <summary>The matched opcodes</summary>
		// Token: 0x170001EC RID: 492
		// (get) Token: 0x060006FC RID: 1788 RVA: 0x00016F62 File Offset: 0x00015162
		// (set) Token: 0x060006FD RID: 1789 RVA: 0x00016F70 File Offset: 0x00015170
		[Obsolete("Use opcodeSet instead")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public List<OpCode> opcodes
		{
			get
			{
				return this.opcodeSet.ToList<OpCode>();
			}
			set
			{
				HashSet<OpCode> hashSet = new HashSet<OpCode>();
				foreach (OpCode item in value)
				{
					hashSet.Add(item);
				}
				this.opcodeSet = hashSet;
			}
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x00016FCC File Offset: 0x000151CC
		internal CodeMatch Set(object operand, string name)
		{
			if (this.operand == null)
			{
				this.operand = operand;
			}
			if (operand != null)
			{
				this.operands.Add(operand);
			}
			if (this.name == null)
			{
				this.name = name;
			}
			return this;
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x00016FFC File Offset: 0x000151FC
		internal CodeMatch Set(OpCode opcode, object operand, string name)
		{
			this.opcode = opcode;
			this.opcodeSet.Add(opcode);
			if (this.operand == null)
			{
				this.operand = operand;
			}
			if (operand != null)
			{
				this.operands.Add(operand);
			}
			if (this.name == null)
			{
				this.name = name;
			}
			return this;
		}

		/// <summary>Creates a code match</summary>
		/// <param name="opcode">The optional opcode</param>
		/// <param name="operand">The optional operand</param>
		/// <param name="name">The optional name</param>
		// Token: 0x06000700 RID: 1792 RVA: 0x0001704C File Offset: 0x0001524C
		public CodeMatch(OpCode? opcode = null, object operand = null, string name = null)
		{
			if (opcode != null)
			{
				OpCode opcodeValue = opcode.GetValueOrDefault();
				this.opcode = opcodeValue;
				this.opcodeSet.Add(opcodeValue);
			}
			if (operand != null)
			{
				this.operands.Add(operand);
			}
			this.operand = operand;
			this.name = name;
		}

		/// <summary>Creates a code match</summary>
		/// <param name="opcodes">The opcodes</param>
		/// <param name="operand">The optional operand</param>
		/// <param name="name">The optional name</param>
		// Token: 0x06000701 RID: 1793 RVA: 0x000170D0 File Offset: 0x000152D0
		public static CodeMatch WithOpcodes(HashSet<OpCode> opcodes, object operand = null, string name = null)
		{
			return new CodeMatch(null, operand, name)
			{
				opcodeSet = opcodes
			};
		}

		/// <summary>Creates a code match that calls a method</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <param name="name">The optional name</param>
		// Token: 0x06000702 RID: 1794 RVA: 0x000170F4 File Offset: 0x000152F4
		public CodeMatch(Expression<Action> expression, string name = null)
		{
			this.opcodeSet.UnionWith(CodeInstructionExtensions.opcodesCalling);
			this.operand = SymbolExtensions.GetMethodInfo(expression);
			if (this.operand != null)
			{
				this.operands.Add(this.operand);
			}
			this.name = name;
		}

		/// <summary>Creates a code match that calls a method</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <param name="name">The optional name</param>
		// Token: 0x06000703 RID: 1795 RVA: 0x00017170 File Offset: 0x00015370
		public CodeMatch(LambdaExpression expression, string name = null)
		{
			this.opcodeSet.UnionWith(CodeInstructionExtensions.opcodesCalling);
			this.operand = SymbolExtensions.GetMethodInfo(expression);
			if (this.operand != null)
			{
				this.operands.Add(this.operand);
			}
			this.name = name;
		}

		/// <summary>Creates a code match</summary>
		/// <param name="instruction">The CodeInstruction</param>
		/// <param name="name">An optional name</param>
		// Token: 0x06000704 RID: 1796 RVA: 0x000171EB File Offset: 0x000153EB
		public CodeMatch(CodeInstruction instruction, string name = null)
			: this(new OpCode?(instruction.opcode), instruction.operand, name)
		{
		}

		/// <summary>Creates a code match</summary>
		/// <param name="predicate">The predicate</param>
		/// <param name="name">An optional name</param>
		// Token: 0x06000705 RID: 1797 RVA: 0x00017208 File Offset: 0x00015408
		public CodeMatch(Func<CodeInstruction, bool> predicate, string name = null)
		{
			this.predicate = predicate;
			this.name = name;
		}

		// Token: 0x06000706 RID: 1798 RVA: 0x00017258 File Offset: 0x00015458
		internal bool Matches(List<CodeInstruction> codes, CodeInstruction instruction)
		{
			if (this.predicate != null)
			{
				return this.predicate(instruction);
			}
			if (this.opcodeSet.Count > 0 && !this.opcodeSet.Contains(instruction.opcode))
			{
				return false;
			}
			if (this.operands.Count > 0 && !this.operands.Contains(instruction.operand))
			{
				return false;
			}
			if (this.labels.Count > 0 && !this.labels.Intersect(instruction.labels).Any<Label>())
			{
				return false;
			}
			if (this.blocks.Count > 0 && !this.blocks.Intersect(instruction.blocks).Any<ExceptionBlock>())
			{
				return false;
			}
			if (this.jumpsFrom.Count > 0 && !(from index in this.jumpsFrom
				select codes[index].operand).OfType<Label>().Intersect(instruction.labels).Any<Label>())
			{
				return false;
			}
			if (this.jumpsTo.Count > 0)
			{
				object operand = instruction.operand;
				if (operand == null || operand.GetType() != typeof(Label))
				{
					return false;
				}
				Label label = (Label)operand;
				IEnumerable<int> indices = from idx in Enumerable.Range(0, codes.Count)
					where codes[idx].labels.Contains(label)
					select idx;
				if (!this.jumpsTo.Intersect(indices).Any<int>())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Tests for any form of Ldarg*</summary>
		/// <param name="n">The (optional) index</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000707 RID: 1799 RVA: 0x000173E4 File Offset: 0x000155E4
		public static CodeMatch IsLdarg(int? n = null)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.IsLdarg(n), null);
		}

		/// <summary>Tests for Ldarga/Ldarga_S</summary>
		/// <param name="n">The (optional) index</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000708 RID: 1800 RVA: 0x00017410 File Offset: 0x00015610
		public static CodeMatch IsLdarga(int? n = null)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.IsLdarga(n), null);
		}

		/// <summary>Tests for Starg/Starg_S</summary>
		/// <param name="n">The (optional) index</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000709 RID: 1801 RVA: 0x0001743C File Offset: 0x0001563C
		public static CodeMatch IsStarg(int? n = null)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.IsStarg(n), null);
		}

		/// <summary>Tests for any form of Ldloc*</summary>
		/// <param name="variable">The optional local variable</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600070A RID: 1802 RVA: 0x00017468 File Offset: 0x00015668
		public static CodeMatch IsLdloc(LocalBuilder variable = null)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.IsLdloc(variable), null);
		}

		/// <summary>Tests for any form of Stloc*</summary>
		/// <param name="variable">The optional local variable</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600070B RID: 1803 RVA: 0x00017494 File Offset: 0x00015694
		public static CodeMatch IsStloc(LocalBuilder variable = null)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.IsStloc(variable), null);
		}

		/// <summary>Tests if the code instruction calls the method/constructor</summary>
		/// <param name="method">The method</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600070C RID: 1804 RVA: 0x000174C0 File Offset: 0x000156C0
		public static CodeMatch Calls(MethodInfo method)
		{
			return CodeMatch.WithOpcodes(CodeInstructionExtensions.opcodesCalling, method, null);
		}

		/// <summary>Tests if the code instruction loads a constant</summary>
		/// <returns>A new code match</returns>
		// Token: 0x0600070D RID: 1805 RVA: 0x000174CE File Offset: 0x000156CE
		public static CodeMatch LoadsConstant()
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsConstant(), null);
		}

		/// <summary>Tests if the code instruction loads an integer constant</summary>
		/// <param name="number">The integer constant</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600070E RID: 1806 RVA: 0x000174F8 File Offset: 0x000156F8
		public static CodeMatch LoadsConstant(long number)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsConstant(number), null);
		}

		/// <summary>Tests if the code instruction loads a floating point constant</summary>
		/// <param name="number">The floating point constant</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600070F RID: 1807 RVA: 0x00017524 File Offset: 0x00015724
		public static CodeMatch LoadsConstant(double number)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsConstant(number), null);
		}

		/// <summary>Tests if the code instruction loads an enum constant</summary>
		/// <param name="e">The enum</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000710 RID: 1808 RVA: 0x00017550 File Offset: 0x00015750
		public static CodeMatch LoadsConstant(Enum e)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsConstant(e), null);
		}

		/// <summary>Tests if the code instruction loads a string constant</summary>
		/// <param name="str">The string</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000711 RID: 1809 RVA: 0x0001757C File Offset: 0x0001577C
		public static CodeMatch LoadsConstant(string str)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsConstant(str), null);
		}

		/// <summary>Tests if the code instruction loads a field</summary>
		/// <param name="field">The field</param>
		/// <param name="byAddress">Set to true if the address of the field is loaded</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000712 RID: 1810 RVA: 0x000175A8 File Offset: 0x000157A8
		public static CodeMatch LoadsField(FieldInfo field, bool byAddress = false)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.LoadsField(field, byAddress), null);
		}

		/// <summary>Tests if the code instruction stores a field</summary>
		/// <param name="field">The field</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000713 RID: 1811 RVA: 0x000175DC File Offset: 0x000157DC
		public static CodeMatch StoresField(FieldInfo field)
		{
			return new CodeMatch((CodeInstruction instruction) => instruction.StoresField(field), null);
		}

		/// <summary>Creates a code match that calls a method</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000714 RID: 1812 RVA: 0x00017608 File Offset: 0x00015808
		public static CodeMatch Calls(Expression<Action> expression)
		{
			return new CodeMatch(expression, null);
		}

		/// <summary>Creates a code match that calls a method</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000715 RID: 1813 RVA: 0x00017611 File Offset: 0x00015811
		public static CodeMatch Calls(LambdaExpression expression)
		{
			return new CodeMatch(expression, null);
		}

		/// <summary>Creates a code match for local loads</summary>
		/// <param name="useAddress">Whether to match for address loads</param>
		/// <param name="name">An optional name</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000716 RID: 1814 RVA: 0x0001761A File Offset: 0x0001581A
		public static CodeMatch LoadsLocal(bool useAddress = false, string name = null)
		{
			return CodeMatch.WithOpcodes(useAddress ? CodeInstructionExtensions.opcodesLoadingLocalByAddress : CodeInstructionExtensions.opcodesLoadingLocalNormal, null, name);
		}

		/// <summary>Creates a code match for local stores</summary>
		/// <param name="name">An optional name</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000717 RID: 1815 RVA: 0x00017632 File Offset: 0x00015832
		public static CodeMatch StoresLocal(string name = null)
		{
			return CodeMatch.WithOpcodes(CodeInstructionExtensions.opcodesStoringLocal, null, name);
		}

		/// <summary>Creates a code match for argument loads</summary>
		/// <param name="useAddress">Whether to match for address loads</param>
		/// <param name="name">An optional name</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000718 RID: 1816 RVA: 0x00017640 File Offset: 0x00015840
		public static CodeMatch LoadsArgument(bool useAddress = false, string name = null)
		{
			return CodeMatch.WithOpcodes(useAddress ? CodeInstructionExtensions.opcodesLoadingArgumentByAddress : CodeInstructionExtensions.opcodesLoadingArgumentNormal, null, name);
		}

		/// <summary>Creates a code match for argument stores</summary>
		/// <param name="name">An optional name</param>
		/// <returns>A new code match</returns>
		// Token: 0x06000719 RID: 1817 RVA: 0x00017658 File Offset: 0x00015858
		public static CodeMatch StoresArgument(string name = null)
		{
			return CodeMatch.WithOpcodes(CodeInstructionExtensions.opcodesStoringArgument, null, name);
		}

		/// <summary>Creates a code match for branching</summary>
		/// <param name="name">An optional name</param>
		/// <returns>A new code match</returns>
		// Token: 0x0600071A RID: 1818 RVA: 0x00017666 File Offset: 0x00015866
		public static CodeMatch Branches(string name = null)
		{
			return CodeMatch.WithOpcodes(CodeInstructionExtensions.opcodesBranching, null, name);
		}

		/// <summary>Returns a string that represents the match</summary>
		/// <returns>A string representation</returns>
		// Token: 0x0600071B RID: 1819 RVA: 0x00017674 File Offset: 0x00015874
		public override string ToString()
		{
			string result = "[";
			if (this.name != null)
			{
				result = result + this.name + ": ";
			}
			if (this.opcodeSet.Count > 0)
			{
				result = result + "opcodes=" + this.opcodeSet.Join(null, ", ") + " ";
			}
			if (this.operands.Count > 0)
			{
				result = result + "operands=" + this.operands.Join(null, ", ") + " ";
			}
			if (this.labels.Count > 0)
			{
				result = result + "labels=" + this.labels.Join(null, ", ") + " ";
			}
			if (this.blocks.Count > 0)
			{
				result = result + "blocks=" + this.blocks.Join(null, ", ") + " ";
			}
			if (this.jumpsFrom.Count > 0)
			{
				result = result + "jumpsFrom=" + this.jumpsFrom.Join(null, ", ") + " ";
			}
			if (this.jumpsTo.Count > 0)
			{
				result = result + "jumpsTo=" + this.jumpsTo.Join(null, ", ") + " ";
			}
			if (this.predicate != null)
			{
				result += "predicate=yes ";
			}
			return result.TrimEnd(Array.Empty<char>()) + "]";
		}

		/// <summary>The name of the match</summary>
		// Token: 0x04000273 RID: 627
		public string name;

		/// <summary>The matched opcodes</summary>
		// Token: 0x04000274 RID: 628
		public HashSet<OpCode> opcodeSet = new HashSet<OpCode>();

		/// <summary>The matched operands</summary>
		// Token: 0x04000275 RID: 629
		public List<object> operands = new List<object>();

		/// <summary>The jumps from the match</summary>
		// Token: 0x04000276 RID: 630
		public List<int> jumpsFrom = new List<int>();

		/// <summary>The jumps to the match</summary>
		// Token: 0x04000277 RID: 631
		public List<int> jumpsTo = new List<int>();

		/// <summary>The match predicate</summary>
		// Token: 0x04000278 RID: 632
		public Func<CodeInstruction, bool> predicate;
	}
}
