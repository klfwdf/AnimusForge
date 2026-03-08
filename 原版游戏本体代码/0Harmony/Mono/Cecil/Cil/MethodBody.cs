using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002F1 RID: 753
	internal sealed class MethodBody
	{
		// Token: 0x170004F3 RID: 1267
		// (get) Token: 0x060013A7 RID: 5031 RVA: 0x0003DF97 File Offset: 0x0003C197
		public MethodDefinition Method
		{
			get
			{
				return this.method;
			}
		}

		// Token: 0x170004F4 RID: 1268
		// (get) Token: 0x060013A8 RID: 5032 RVA: 0x0003DF9F File Offset: 0x0003C19F
		// (set) Token: 0x060013A9 RID: 5033 RVA: 0x0003DFA7 File Offset: 0x0003C1A7
		public int MaxStackSize
		{
			get
			{
				return this.max_stack_size;
			}
			set
			{
				this.max_stack_size = value;
			}
		}

		// Token: 0x170004F5 RID: 1269
		// (get) Token: 0x060013AA RID: 5034 RVA: 0x0003DFB0 File Offset: 0x0003C1B0
		public int CodeSize
		{
			get
			{
				return this.code_size;
			}
		}

		// Token: 0x170004F6 RID: 1270
		// (get) Token: 0x060013AB RID: 5035 RVA: 0x0003DFB8 File Offset: 0x0003C1B8
		// (set) Token: 0x060013AC RID: 5036 RVA: 0x0003DFC0 File Offset: 0x0003C1C0
		public bool InitLocals
		{
			get
			{
				return this.init_locals;
			}
			set
			{
				this.init_locals = value;
			}
		}

		// Token: 0x170004F7 RID: 1271
		// (get) Token: 0x060013AD RID: 5037 RVA: 0x0003DFC9 File Offset: 0x0003C1C9
		// (set) Token: 0x060013AE RID: 5038 RVA: 0x0003DFD1 File Offset: 0x0003C1D1
		public MetadataToken LocalVarToken
		{
			get
			{
				return this.local_var_token;
			}
			set
			{
				this.local_var_token = value;
			}
		}

		// Token: 0x170004F8 RID: 1272
		// (get) Token: 0x060013AF RID: 5039 RVA: 0x0003DFDA File Offset: 0x0003C1DA
		public Collection<Instruction> Instructions
		{
			get
			{
				if (this.instructions == null)
				{
					Interlocked.CompareExchange<Collection<Instruction>>(ref this.instructions, new InstructionCollection(this.method), null);
				}
				return this.instructions;
			}
		}

		// Token: 0x170004F9 RID: 1273
		// (get) Token: 0x060013B0 RID: 5040 RVA: 0x0003E002 File Offset: 0x0003C202
		public bool HasExceptionHandlers
		{
			get
			{
				return !this.exceptions.IsNullOrEmpty<ExceptionHandler>();
			}
		}

		// Token: 0x170004FA RID: 1274
		// (get) Token: 0x060013B1 RID: 5041 RVA: 0x0003E012 File Offset: 0x0003C212
		public Collection<ExceptionHandler> ExceptionHandlers
		{
			get
			{
				if (this.exceptions == null)
				{
					Interlocked.CompareExchange<Collection<ExceptionHandler>>(ref this.exceptions, new Collection<ExceptionHandler>(), null);
				}
				return this.exceptions;
			}
		}

		// Token: 0x170004FB RID: 1275
		// (get) Token: 0x060013B2 RID: 5042 RVA: 0x0003E034 File Offset: 0x0003C234
		public bool HasVariables
		{
			get
			{
				return !this.variables.IsNullOrEmpty<VariableDefinition>();
			}
		}

		// Token: 0x170004FC RID: 1276
		// (get) Token: 0x060013B3 RID: 5043 RVA: 0x0003E044 File Offset: 0x0003C244
		public Collection<VariableDefinition> Variables
		{
			get
			{
				if (this.variables == null)
				{
					Interlocked.CompareExchange<Collection<VariableDefinition>>(ref this.variables, new VariableDefinitionCollection(this.method), null);
				}
				return this.variables;
			}
		}

		// Token: 0x170004FD RID: 1277
		// (get) Token: 0x060013B4 RID: 5044 RVA: 0x0003E06C File Offset: 0x0003C26C
		public ParameterDefinition ThisParameter
		{
			get
			{
				if (this.method == null || this.method.DeclaringType == null)
				{
					throw new NotSupportedException();
				}
				if (!this.method.HasThis)
				{
					return null;
				}
				if (this.this_parameter == null)
				{
					Interlocked.CompareExchange<ParameterDefinition>(ref this.this_parameter, MethodBody.CreateThisParameter(this.method), null);
				}
				return this.this_parameter;
			}
		}

		// Token: 0x060013B5 RID: 5045 RVA: 0x0003E0CC File Offset: 0x0003C2CC
		private static ParameterDefinition CreateThisParameter(MethodDefinition method)
		{
			TypeReference parameter_type = method.DeclaringType;
			if (parameter_type.HasGenericParameters)
			{
				GenericInstanceType instance = new GenericInstanceType(parameter_type, parameter_type.GenericParameters.Count);
				for (int i = 0; i < parameter_type.GenericParameters.Count; i++)
				{
					instance.GenericArguments.Add(parameter_type.GenericParameters[i]);
				}
				parameter_type = instance;
			}
			if (parameter_type.IsValueType || parameter_type.IsPrimitive)
			{
				parameter_type = new ByReferenceType(parameter_type);
			}
			return new ParameterDefinition(parameter_type, method);
		}

		// Token: 0x060013B6 RID: 5046 RVA: 0x0003E147 File Offset: 0x0003C347
		public MethodBody(MethodDefinition method)
		{
			this.method = method;
		}

		// Token: 0x060013B7 RID: 5047 RVA: 0x0003E156 File Offset: 0x0003C356
		public ILProcessor GetILProcessor()
		{
			return new ILProcessor(this);
		}

		// Token: 0x040008B0 RID: 2224
		internal readonly MethodDefinition method;

		// Token: 0x040008B1 RID: 2225
		internal ParameterDefinition this_parameter;

		// Token: 0x040008B2 RID: 2226
		internal int max_stack_size;

		// Token: 0x040008B3 RID: 2227
		internal int code_size;

		// Token: 0x040008B4 RID: 2228
		internal bool init_locals;

		// Token: 0x040008B5 RID: 2229
		internal MetadataToken local_var_token;

		// Token: 0x040008B6 RID: 2230
		internal Collection<Instruction> instructions;

		// Token: 0x040008B7 RID: 2231
		internal Collection<ExceptionHandler> exceptions;

		// Token: 0x040008B8 RID: 2232
		internal Collection<VariableDefinition> variables;
	}
}
