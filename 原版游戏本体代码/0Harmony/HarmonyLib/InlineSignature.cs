using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Cecil;
using MonoMod.Utils;

namespace HarmonyLib
{
	/// <summary>
	///  A mutable representation of an inline signature, similar to Mono.Cecil's CallSite.
	///  Used by the calli instruction, can be used by transpilers
	///  </summary>
	// Token: 0x02000028 RID: 40
	internal class InlineSignature : ICallSiteGenerator
	{
		/// <summary>See <see cref="F:System.Reflection.CallingConventions.HasThis" /></summary>
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000C1 RID: 193 RVA: 0x0000564B File Offset: 0x0000384B
		// (set) Token: 0x060000C2 RID: 194 RVA: 0x00005653 File Offset: 0x00003853
		public bool HasThis { get; set; }

		/// <summary>See <see cref="F:System.Reflection.CallingConventions.ExplicitThis" /></summary>
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000565C File Offset: 0x0000385C
		// (set) Token: 0x060000C4 RID: 196 RVA: 0x00005664 File Offset: 0x00003864
		public bool ExplicitThis { get; set; }

		/// <summary>See <see cref="T:System.Runtime.InteropServices.CallingConvention" /></summary>
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x0000566D File Offset: 0x0000386D
		// (set) Token: 0x060000C6 RID: 198 RVA: 0x00005675 File Offset: 0x00003875
		public CallingConvention CallingConvention { get; set; } = CallingConvention.Winapi;

		/// <summary>The list of all parameter types or function pointer signatures received by the call site</summary>
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x0000567E File Offset: 0x0000387E
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x00005686 File Offset: 0x00003886
		public List<object> Parameters { get; set; } = new List<object>();

		/// <summary>The return type or function pointer signature returned by the call site</summary>
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060000C9 RID: 201 RVA: 0x0000568F File Offset: 0x0000388F
		// (set) Token: 0x060000CA RID: 202 RVA: 0x00005697 File Offset: 0x00003897
		public object ReturnType { get; set; } = typeof(void);

		/// <summary>Returns a string representation of the inline signature</summary>
		/// <returns>A string representation of the inline signature</returns>
		// Token: 0x060000CB RID: 203 RVA: 0x000056A0 File Offset: 0x000038A0
		public override string ToString()
		{
			Type rt = this.ReturnType as Type;
			string str;
			if (rt == null)
			{
				object returnType = this.ReturnType;
				str = ((returnType != null) ? returnType.ToString() : null);
			}
			else
			{
				str = rt.FullDescription();
			}
			return str + " (" + this.Parameters.Join(delegate(object p)
			{
				Type pt = p as Type;
				if (pt != null)
				{
					return pt.FullDescription();
				}
				if (p == null)
				{
					return null;
				}
				return p.ToString();
			}, ", ") + ")";
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00005714 File Offset: 0x00003914
		internal static TypeReference GetTypeReference(ModuleDefinition module, object param)
		{
			Type paramType = param as Type;
			TypeReference result;
			if (paramType == null)
			{
				InlineSignature paramSig = param as InlineSignature;
				if (paramSig == null)
				{
					InlineSignature.ModifierType paramMod = param as InlineSignature.ModifierType;
					if (paramMod == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Unsupported inline signature parameter type: ");
						defaultInterpolatedStringHandler.AppendFormatted<object>(param);
						defaultInterpolatedStringHandler.AppendLiteral(" (");
						defaultInterpolatedStringHandler.AppendFormatted((param != null) ? param.GetType().FullDescription() : null);
						defaultInterpolatedStringHandler.AppendLiteral(")");
						throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					result = paramMod.ToTypeReference(module);
				}
				else
				{
					result = paramSig.ToFunctionPointer(module);
				}
			}
			else
			{
				result = module.ImportReference(paramType);
			}
			return result;
		}

		// Token: 0x060000CD RID: 205 RVA: 0x000057BC File Offset: 0x000039BC
		Mono.Cecil.CallSite ICallSiteGenerator.ToCallSite(ModuleDefinition module)
		{
			Mono.Cecil.CallSite callsite = new Mono.Cecil.CallSite(InlineSignature.GetTypeReference(module, this.ReturnType))
			{
				HasThis = this.HasThis,
				ExplicitThis = this.ExplicitThis,
				CallingConvention = (MethodCallingConvention)((byte)this.CallingConvention - 1)
			};
			foreach (object param in this.Parameters)
			{
				callsite.Parameters.Add(new ParameterDefinition(InlineSignature.GetTypeReference(module, param)));
			}
			return callsite;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000585C File Offset: 0x00003A5C
		private FunctionPointerType ToFunctionPointer(ModuleDefinition module)
		{
			FunctionPointerType fptr = new FunctionPointerType
			{
				ReturnType = InlineSignature.GetTypeReference(module, this.ReturnType),
				HasThis = this.HasThis,
				ExplicitThis = this.ExplicitThis,
				CallingConvention = (MethodCallingConvention)((byte)this.CallingConvention - 1)
			};
			foreach (object param in this.Parameters)
			{
				fptr.Parameters.Add(new ParameterDefinition(InlineSignature.GetTypeReference(module, param)));
			}
			return fptr;
		}

		/// <summary>
		/// A mutable representation of a parameter type with an attached type modifier,
		/// similar to Mono.Cecil's OptionalModifierType / RequiredModifierType and C#'s modopt / modreq
		/// </summary>
		// Token: 0x02000029 RID: 41
		public class ModifierType
		{
			/// <summary>Returns a string representation of the modifier type</summary>
			/// <returns>A string representation of the modifier type</returns>
			// Token: 0x060000D0 RID: 208 RVA: 0x0000592C File Offset: 0x00003B2C
			public override string ToString()
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 3);
				Type rt = this.Type as Type;
				string value;
				if (rt == null)
				{
					object type = this.Type;
					value = ((type != null) ? type.ToString() : null);
				}
				else
				{
					value = rt.FullDescription();
				}
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(" mod");
				defaultInterpolatedStringHandler.AppendFormatted(this.IsOptional ? "opt" : "req");
				defaultInterpolatedStringHandler.AppendLiteral("(");
				Type modifier = this.Modifier;
				defaultInterpolatedStringHandler.AppendFormatted((modifier != null) ? modifier.FullDescription() : null);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}

			// Token: 0x060000D1 RID: 209 RVA: 0x000059D4 File Offset: 0x00003BD4
			internal TypeReference ToTypeReference(ModuleDefinition module)
			{
				if (this.IsOptional)
				{
					return new OptionalModifierType(module.ImportReference(this.Modifier), InlineSignature.GetTypeReference(module, this.Type));
				}
				return new RequiredModifierType(module.ImportReference(this.Modifier), InlineSignature.GetTypeReference(module, this.Type));
			}

			/// <summary>Whether this is a modopt (optional modifier type) or a modreq (required modifier type)</summary>
			// Token: 0x04000080 RID: 128
			public bool IsOptional;

			/// <summary>The modifier type attached to the parameter type</summary>
			// Token: 0x04000081 RID: 129
			public Type Modifier;

			/// <summary>The modified parameter type</summary>
			// Token: 0x04000082 RID: 130
			public object Type;
		}
	}
}
