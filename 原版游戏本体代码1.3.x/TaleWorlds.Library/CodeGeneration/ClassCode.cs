using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration
{
	// Token: 0x020000BB RID: 187
	public class ClassCode
	{
		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x060006E9 RID: 1769 RVA: 0x00017645 File Offset: 0x00015845
		// (set) Token: 0x060006EA RID: 1770 RVA: 0x0001764D File Offset: 0x0001584D
		public string Name { get; set; }

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x060006EB RID: 1771 RVA: 0x00017656 File Offset: 0x00015856
		// (set) Token: 0x060006EC RID: 1772 RVA: 0x0001765E File Offset: 0x0001585E
		public bool IsGeneric { get; set; }

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x060006ED RID: 1773 RVA: 0x00017667 File Offset: 0x00015867
		// (set) Token: 0x060006EE RID: 1774 RVA: 0x0001766F File Offset: 0x0001586F
		public int GenericTypeCount { get; set; }

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x060006EF RID: 1775 RVA: 0x00017678 File Offset: 0x00015878
		// (set) Token: 0x060006F0 RID: 1776 RVA: 0x00017680 File Offset: 0x00015880
		public bool IsPartial { get; set; }

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x060006F1 RID: 1777 RVA: 0x00017689 File Offset: 0x00015889
		// (set) Token: 0x060006F2 RID: 1778 RVA: 0x00017691 File Offset: 0x00015891
		public ClassCodeAccessModifier AccessModifier { get; set; }

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x060006F3 RID: 1779 RVA: 0x0001769A File Offset: 0x0001589A
		// (set) Token: 0x060006F4 RID: 1780 RVA: 0x000176A2 File Offset: 0x000158A2
		public bool IsClass { get; set; }

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x060006F5 RID: 1781 RVA: 0x000176AB File Offset: 0x000158AB
		// (set) Token: 0x060006F6 RID: 1782 RVA: 0x000176B3 File Offset: 0x000158B3
		public List<string> InheritedInterfaces { get; private set; }

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x060006F7 RID: 1783 RVA: 0x000176BC File Offset: 0x000158BC
		// (set) Token: 0x060006F8 RID: 1784 RVA: 0x000176C4 File Offset: 0x000158C4
		public List<ClassCode> NestedClasses { get; private set; }

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x060006F9 RID: 1785 RVA: 0x000176CD File Offset: 0x000158CD
		// (set) Token: 0x060006FA RID: 1786 RVA: 0x000176D5 File Offset: 0x000158D5
		public List<MethodCode> Methods { get; private set; }

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x060006FB RID: 1787 RVA: 0x000176DE File Offset: 0x000158DE
		// (set) Token: 0x060006FC RID: 1788 RVA: 0x000176E6 File Offset: 0x000158E6
		public List<ConstructorCode> Constructors { get; private set; }

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x060006FD RID: 1789 RVA: 0x000176EF File Offset: 0x000158EF
		// (set) Token: 0x060006FE RID: 1790 RVA: 0x000176F7 File Offset: 0x000158F7
		public List<VariableCode> Variables { get; private set; }

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x060006FF RID: 1791 RVA: 0x00017700 File Offset: 0x00015900
		// (set) Token: 0x06000700 RID: 1792 RVA: 0x00017708 File Offset: 0x00015908
		public CommentSection CommentSection { get; set; }

		// Token: 0x06000701 RID: 1793 RVA: 0x00017714 File Offset: 0x00015914
		public ClassCode()
		{
			this.IsClass = true;
			this.IsGeneric = false;
			this.GenericTypeCount = 0;
			this.InheritedInterfaces = new List<string>();
			this.NestedClasses = new List<ClassCode>();
			this.Methods = new List<MethodCode>();
			this.Constructors = new List<ConstructorCode>();
			this.Variables = new List<VariableCode>();
			this.AccessModifier = ClassCodeAccessModifier.DoNotMention;
			this.Name = "UnnamedClass";
			this.CommentSection = null;
		}

		// Token: 0x06000702 RID: 1794 RVA: 0x0001778C File Offset: 0x0001598C
		public void GenerateInto(CodeGenerationFile codeGenerationFile)
		{
			if (this.CommentSection != null)
			{
				this.CommentSection.GenerateInto(codeGenerationFile);
			}
			string text = "";
			if (this.AccessModifier == ClassCodeAccessModifier.Public)
			{
				text += "public ";
			}
			else if (this.AccessModifier == ClassCodeAccessModifier.Internal)
			{
				text += "internal ";
			}
			if (this.IsPartial)
			{
				text += "partial ";
			}
			string str = "class";
			if (!this.IsClass)
			{
				str = "struct";
			}
			text = text + str + " " + this.Name;
			if (this.InheritedInterfaces.Count > 0)
			{
				text += " : ";
				for (int i = 0; i < this.InheritedInterfaces.Count; i++)
				{
					string str2 = this.InheritedInterfaces[i];
					text = text + " " + str2;
					if (i + 1 != this.InheritedInterfaces.Count)
					{
						text += ", ";
					}
				}
			}
			if (this.IsGeneric)
			{
				text += "<";
				for (int j = 0; j < this.GenericTypeCount; j++)
				{
					if (this.GenericTypeCount == 1)
					{
						text += "T";
					}
					else
					{
						text = text + "T" + j;
					}
					if (j + 1 != this.GenericTypeCount)
					{
						text += ", ";
					}
				}
				text += ">";
			}
			codeGenerationFile.AddLine(text);
			codeGenerationFile.AddLine("{");
			foreach (ClassCode classCode in this.NestedClasses)
			{
				classCode.GenerateInto(codeGenerationFile);
			}
			foreach (VariableCode variableCode in this.Variables)
			{
				string line = variableCode.GenerateLine();
				codeGenerationFile.AddLine(line);
			}
			if (this.Variables.Count > 0)
			{
				codeGenerationFile.AddLine("");
			}
			foreach (ConstructorCode constructorCode in this.Constructors)
			{
				constructorCode.GenerateInto(codeGenerationFile);
				codeGenerationFile.AddLine("");
			}
			foreach (MethodCode methodCode in this.Methods)
			{
				methodCode.GenerateInto(codeGenerationFile);
				codeGenerationFile.AddLine("");
			}
			codeGenerationFile.AddLine("}");
		}

		// Token: 0x06000703 RID: 1795 RVA: 0x00017A54 File Offset: 0x00015C54
		public void AddVariable(VariableCode variableCode)
		{
			this.Variables.Add(variableCode);
		}

		// Token: 0x06000704 RID: 1796 RVA: 0x00017A62 File Offset: 0x00015C62
		public void AddNestedClass(ClassCode clasCode)
		{
			this.NestedClasses.Add(clasCode);
		}

		// Token: 0x06000705 RID: 1797 RVA: 0x00017A70 File Offset: 0x00015C70
		public void AddMethod(MethodCode methodCode)
		{
			this.Methods.Add(methodCode);
		}

		// Token: 0x06000706 RID: 1798 RVA: 0x00017A7E File Offset: 0x00015C7E
		public void AddConsturctor(ConstructorCode constructorCode)
		{
			constructorCode.Name = this.Name;
			this.Constructors.Add(constructorCode);
		}

		// Token: 0x06000707 RID: 1799 RVA: 0x00017A98 File Offset: 0x00015C98
		public void AddInterface(string interfaceName)
		{
			this.InheritedInterfaces.Add(interfaceName);
		}
	}
}
