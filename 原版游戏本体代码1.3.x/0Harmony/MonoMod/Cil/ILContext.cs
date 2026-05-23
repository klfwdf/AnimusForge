using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using MonoMod.Utils;

namespace MonoMod.Cil
{
	// Token: 0x0200086B RID: 2155
	[NullableContext(1)]
	[Nullable(0)]
	internal class ILContext : IDisposable
	{
		// Token: 0x17000817 RID: 2071
		// (get) Token: 0x0600297A RID: 10618 RVA: 0x0008CD28 File Offset: 0x0008AF28
		// (set) Token: 0x0600297B RID: 10619 RVA: 0x0008CD30 File Offset: 0x0008AF30
		public MethodDefinition Method { get; private set; }

		// Token: 0x17000818 RID: 2072
		// (get) Token: 0x0600297C RID: 10620 RVA: 0x0008CD39 File Offset: 0x0008AF39
		// (set) Token: 0x0600297D RID: 10621 RVA: 0x0008CD41 File Offset: 0x0008AF41
		public ILProcessor IL { get; private set; }

		// Token: 0x17000819 RID: 2073
		// (get) Token: 0x0600297E RID: 10622 RVA: 0x0008CD4A File Offset: 0x0008AF4A
		public Mono.Cecil.Cil.MethodBody Body
		{
			get
			{
				return this.Method.Body;
			}
		}

		// Token: 0x1700081A RID: 2074
		// (get) Token: 0x0600297F RID: 10623 RVA: 0x0008CD57 File Offset: 0x0008AF57
		public ModuleDefinition Module
		{
			get
			{
				return this.Method.Module;
			}
		}

		// Token: 0x1700081B RID: 2075
		// (get) Token: 0x06002980 RID: 10624 RVA: 0x0008CD64 File Offset: 0x0008AF64
		public Mono.Collections.Generic.Collection<Instruction> Instrs
		{
			get
			{
				return this.Body.Instructions;
			}
		}

		// Token: 0x1700081C RID: 2076
		// (get) Token: 0x06002981 RID: 10625 RVA: 0x0008CD71 File Offset: 0x0008AF71
		public System.Collections.ObjectModel.ReadOnlyCollection<ILLabel> Labels
		{
			get
			{
				return this._Labels.AsReadOnly();
			}
		}

		// Token: 0x1700081D RID: 2077
		// (get) Token: 0x06002982 RID: 10626 RVA: 0x0008CD7E File Offset: 0x0008AF7E
		public bool IsReadOnly
		{
			get
			{
				return this.IL == null;
			}
		}

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06002983 RID: 10627 RVA: 0x0008CD8C File Offset: 0x0008AF8C
		// (remove) Token: 0x06002984 RID: 10628 RVA: 0x0008CDC4 File Offset: 0x0008AFC4
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event Action OnDispose;

		// Token: 0x06002985 RID: 10629 RVA: 0x0008CDFC File Offset: 0x0008AFFC
		public ILContext(MethodDefinition method)
		{
			Helpers.ThrowIfArgumentNull<MethodDefinition>(method, "method");
			this.Method = method;
			this.IL = method.Body.GetILProcessor();
		}

		// Token: 0x06002986 RID: 10630 RVA: 0x0008CE48 File Offset: 0x0008B048
		public void Invoke(ILContext.Manipulator manip)
		{
			Helpers.ThrowIfArgumentNull<ILContext.Manipulator>(manip, "manip");
			if (this.IsReadOnly)
			{
				throw new InvalidOperationException();
			}
			foreach (Instruction instr in this.Instrs)
			{
				Instruction target = instr.Operand as Instruction;
				if (target != null)
				{
					instr.Operand = new ILLabel(this, target);
				}
				else
				{
					Instruction[] targets = instr.Operand as Instruction[];
					if (targets != null)
					{
						instr.Operand = (from t in targets
							select new ILLabel(this, t)).ToArray<ILLabel>();
					}
				}
			}
			manip(this);
			if (this.IsReadOnly)
			{
				return;
			}
			foreach (Instruction instr2 in this.Instrs)
			{
				ILLabel label = instr2.Operand as ILLabel;
				if (label != null)
				{
					instr2.Operand = label.Target;
				}
				else
				{
					ILLabel[] targets2 = instr2.Operand as ILLabel[];
					if (targets2 != null)
					{
						instr2.Operand = (from l in targets2
							select l.Target).ToArray<Instruction>();
					}
				}
			}
			this.Method.FixShortLongOps();
		}

		// Token: 0x06002987 RID: 10631 RVA: 0x0008CFB4 File Offset: 0x0008B1B4
		public void MakeReadOnly()
		{
			this.Method = null;
			this.IL = null;
			this._Labels.Clear();
			this._Labels.Capacity = 0;
		}

		// Token: 0x06002988 RID: 10632 RVA: 0x0008CFDB File Offset: 0x0008B1DB
		[Obsolete("Use new ILCursor(il).Goto(index)")]
		public ILCursor At(int index)
		{
			return new ILCursor(this).Goto(index, MoveType.Before, false);
		}

		// Token: 0x06002989 RID: 10633 RVA: 0x0008CFEB File Offset: 0x0008B1EB
		[Obsolete("Use new ILCursor(il).Goto(index)")]
		public ILCursor At(ILLabel label)
		{
			return new ILCursor(this).GotoLabel(label, MoveType.AfterLabel, false);
		}

		// Token: 0x0600298A RID: 10634 RVA: 0x0008CFFB File Offset: 0x0008B1FB
		[Obsolete("Use new ILCursor(il).Goto(index)")]
		public ILCursor At(Instruction instr)
		{
			return new ILCursor(this).Goto(instr, MoveType.Before, false);
		}

		// Token: 0x0600298B RID: 10635 RVA: 0x0008D00B File Offset: 0x0008B20B
		public FieldReference Import(FieldInfo field)
		{
			return this.Module.ImportReference(field);
		}

		// Token: 0x0600298C RID: 10636 RVA: 0x0008D019 File Offset: 0x0008B219
		public MethodReference Import(MethodBase method)
		{
			return this.Module.ImportReference(method);
		}

		// Token: 0x0600298D RID: 10637 RVA: 0x0008D027 File Offset: 0x0008B227
		public TypeReference Import(Type type)
		{
			return this.Module.ImportReference(type);
		}

		// Token: 0x0600298E RID: 10638 RVA: 0x0008D035 File Offset: 0x0008B235
		public ILLabel DefineLabel()
		{
			return new ILLabel(this);
		}

		// Token: 0x0600298F RID: 10639 RVA: 0x0008D03D File Offset: 0x0008B23D
		public ILLabel DefineLabel(Instruction target)
		{
			return new ILLabel(this, target);
		}

		// Token: 0x06002990 RID: 10640 RVA: 0x0008D048 File Offset: 0x0008B248
		[NullableContext(2)]
		public int IndexOf(Instruction instr)
		{
			if (instr == null)
			{
				return this.Instrs.Count;
			}
			int index = this.Instrs.IndexOf(instr);
			if (index != -1)
			{
				return index;
			}
			return this.Instrs.Count;
		}

		// Token: 0x06002991 RID: 10641 RVA: 0x0008D084 File Offset: 0x0008B284
		public IEnumerable<ILLabel> GetIncomingLabels([Nullable(2)] Instruction instr)
		{
			return from l in this._Labels
				where l.Target == instr
				select l;
		}

		// Token: 0x06002992 RID: 10642 RVA: 0x0008D0B8 File Offset: 0x0008B2B8
		[NullableContext(2)]
		public int AddReference<T>(in T value)
		{
			int count = this.managedObjectRefs.Count;
			DynamicReferenceCell dynamicReferenceCell;
			DataScope<DynamicReferenceCell> scope = DynamicReferenceManager.AllocReference<T>(value, out dynamicReferenceCell);
			this.managedObjectRefs.Add(scope);
			return count;
		}

		// Token: 0x06002993 RID: 10643 RVA: 0x0008D0E8 File Offset: 0x0008B2E8
		[NullableContext(2)]
		public T GetReference<T>(int id)
		{
			if (id < 0 || id >= this.managedObjectRefs.Count)
			{
				throw new ArgumentOutOfRangeException("id");
			}
			return DynamicReferenceManager.GetValue<T>(this.managedObjectRefs[id].Data);
		}

		// Token: 0x06002994 RID: 10644 RVA: 0x0008D12C File Offset: 0x0008B32C
		[NullableContext(2)]
		public void SetReference<T>(int id, in T value)
		{
			if (id < 0 || id >= this.managedObjectRefs.Count)
			{
				throw new ArgumentOutOfRangeException("id");
			}
			DynamicReferenceManager.SetValue<T>(this.managedObjectRefs[id].Data, value);
		}

		// Token: 0x06002995 RID: 10645 RVA: 0x0008D170 File Offset: 0x0008B370
		public DynamicReferenceCell GetReferenceCell(int id)
		{
			if (id < 0 || id >= this.managedObjectRefs.Count)
			{
				throw new ArgumentOutOfRangeException("id");
			}
			return this.managedObjectRefs[id].Data;
		}

		// Token: 0x06002996 RID: 10646 RVA: 0x0008D1AE File Offset: 0x0008B3AE
		public VariableDefinition CreateLocal<[Nullable(2)] T>()
		{
			return this.CreateLocal(typeof(T));
		}

		// Token: 0x06002997 RID: 10647 RVA: 0x0008D1C0 File Offset: 0x0008B3C0
		public VariableDefinition CreateLocal(Type type)
		{
			return this.CreateLocal(this.Import(type));
		}

		// Token: 0x06002998 RID: 10648 RVA: 0x0008D1D0 File Offset: 0x0008B3D0
		public VariableDefinition CreateLocal(TypeReference typeRef)
		{
			VariableDefinition newLocal = new VariableDefinition(typeRef);
			this.Method.Body.Variables.Add(newLocal);
			return newLocal;
		}

		// Token: 0x06002999 RID: 10649 RVA: 0x0008D1FC File Offset: 0x0008B3FC
		public override string ToString()
		{
			if (this.Method == null)
			{
				return "// ILContext: READONLY";
			}
			StringBuilder builder = new StringBuilder();
			StringBuilder stringBuilder = builder;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
			defaultInterpolatedStringHandler.AppendLiteral("// ILContext: ");
			defaultInterpolatedStringHandler.AppendFormatted<MethodDefinition>(this.Method);
			stringBuilder.AppendLine(defaultInterpolatedStringHandler.ToStringAndClear());
			foreach (Instruction instr in this.Instrs)
			{
				ILContext.ToString(builder, instr);
			}
			return builder.ToString();
		}

		// Token: 0x0600299A RID: 10650 RVA: 0x0008D29C File Offset: 0x0008B49C
		internal static StringBuilder ToString(StringBuilder builder, [Nullable(2)] Instruction instr)
		{
			if (instr == null)
			{
				return builder;
			}
			object operand = instr.Operand;
			ILLabel label = operand as ILLabel;
			if (label != null)
			{
				instr.Operand = label.Target;
			}
			else
			{
				ILLabel[] labels = operand as ILLabel[];
				if (labels != null)
				{
					instr.Operand = (from l in labels
						select l.Target).ToArray<Instruction>();
				}
			}
			builder.AppendLine(instr.ToString());
			instr.Operand = operand;
			return builder;
		}

		// Token: 0x0600299B RID: 10651 RVA: 0x0008D31C File Offset: 0x0008B51C
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				Action onDispose = this.OnDispose;
				if (onDispose != null)
				{
					onDispose();
				}
				this.OnDispose = null;
				foreach (DataScope<DynamicReferenceCell> scope in this.managedObjectRefs)
				{
					scope.Dispose();
				}
				this.managedObjectRefs.Clear();
				this.managedObjectRefs.Capacity = 0;
				this.MakeReadOnly();
				this.disposedValue = true;
			}
		}

		// Token: 0x0600299C RID: 10652 RVA: 0x0008D3B4 File Offset: 0x0008B5B4
		~ILContext()
		{
			this.Dispose(false);
		}

		// Token: 0x0600299D RID: 10653 RVA: 0x0008D3E4 File Offset: 0x0008B5E4
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x04003A34 RID: 14900
		internal List<ILLabel> _Labels = new List<ILLabel>();

		// Token: 0x04003A35 RID: 14901
		private bool disposedValue;

		// Token: 0x04003A36 RID: 14902
		[Nullable(new byte[] { 1, 0 })]
		private readonly List<DataScope<DynamicReferenceCell>> managedObjectRefs = new List<DataScope<DynamicReferenceCell>>();

		// Token: 0x0200086C RID: 2156
		// (Invoke) Token: 0x060029A0 RID: 10656
		[NullableContext(0)]
		public delegate void Manipulator(ILContext il);
	}
}
