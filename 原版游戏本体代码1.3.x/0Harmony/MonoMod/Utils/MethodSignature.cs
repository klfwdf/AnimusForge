using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x020008CF RID: 2255
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MethodSignature : IEquatable<MethodSignature>, IDebugFormattable
	{
		// Token: 0x1700084E RID: 2126
		// (get) Token: 0x06002ED3 RID: 11987 RVA: 0x000A1171 File Offset: 0x0009F371
		public Type ReturnType { get; }

		// Token: 0x1700084F RID: 2127
		// (get) Token: 0x06002ED4 RID: 11988 RVA: 0x000A1179 File Offset: 0x0009F379
		public int ParameterCount
		{
			get
			{
				return this.parameters.Length;
			}
		}

		// Token: 0x17000850 RID: 2128
		// (get) Token: 0x06002ED5 RID: 11989 RVA: 0x000A1183 File Offset: 0x0009F383
		public IEnumerable<Type> Parameters
		{
			get
			{
				return this.parameters;
			}
		}

		// Token: 0x17000851 RID: 2129
		// (get) Token: 0x06002ED6 RID: 11990 RVA: 0x000A118B File Offset: 0x0009F38B
		[Nullable(2)]
		public Type FirstParameter
		{
			[NullableContext(2)]
			get
			{
				if (this.parameters.Length < 1)
				{
					return null;
				}
				return this.parameters[0];
			}
		}

		// Token: 0x06002ED7 RID: 11991 RVA: 0x000A11A2 File Offset: 0x0009F3A2
		public MethodSignature(Type returnType, Type[] parameters)
		{
			this.ReturnType = returnType;
			this.parameters = parameters;
		}

		// Token: 0x06002ED8 RID: 11992 RVA: 0x000A11B8 File Offset: 0x0009F3B8
		public MethodSignature(Type returnType, IEnumerable<Type> parameters)
		{
			this.ReturnType = returnType;
			this.parameters = parameters.ToArray<Type>();
		}

		// Token: 0x06002ED9 RID: 11993 RVA: 0x000A11D3 File Offset: 0x0009F3D3
		public MethodSignature(MethodBase method)
			: this(method, false)
		{
		}

		// Token: 0x06002EDA RID: 11994 RVA: 0x000A11E0 File Offset: 0x0009F3E0
		public MethodSignature(MethodBase method, bool ignoreThis)
		{
			MethodInfo methodInfo = method as MethodInfo;
			this.ReturnType = ((methodInfo != null) ? methodInfo.ReturnType : null) ?? typeof(void);
			int thisCount = ((ignoreThis || method.IsStatic) ? 0 : 1);
			ParameterInfo[] methParams = method.GetParameters();
			this.parameters = new Type[methParams.Length + thisCount];
			for (int i = thisCount; i < this.parameters.Length; i++)
			{
				this.parameters[i] = methParams[i - thisCount].ParameterType;
			}
			if (!ignoreThis && !method.IsStatic)
			{
				this.parameters[0] = method.GetThisParamType();
			}
		}

		// Token: 0x06002EDB RID: 11995 RVA: 0x000A127E File Offset: 0x0009F47E
		public static MethodSignature ForMethod(MethodBase method)
		{
			return MethodSignature.ForMethod(method, false);
		}

		// Token: 0x06002EDC RID: 11996 RVA: 0x000A1288 File Offset: 0x0009F488
		public static MethodSignature ForMethod(MethodBase method, bool ignoreThis)
		{
			return (ignoreThis ? MethodSignature.noThisSigMap : MethodSignature.thisSigMap).GetValue(method, (MethodBase m) => new MethodSignature(m, ignoreThis));
		}

		// Token: 0x06002EDD RID: 11997 RVA: 0x000A12C8 File Offset: 0x0009F4C8
		public bool IsCompatibleWith(MethodSignature other)
		{
			Helpers.ThrowIfArgumentNull<MethodSignature>(other, "other");
			return this == other || (this.ReturnType.IsCompatible(other.ReturnType) && this.parameters.SequenceEqual(other.Parameters, MethodSignature.CompatableComparer.Instance));
		}

		// Token: 0x06002EDE RID: 11998 RVA: 0x000A1306 File Offset: 0x0009F506
		public DynamicMethodDefinition CreateDmd(string name)
		{
			return new DynamicMethodDefinition(name, this.ReturnType, this.parameters);
		}

		// Token: 0x06002EDF RID: 11999 RVA: 0x000A131C File Offset: 0x0009F51C
		public override string ToString()
		{
			int literals = 2 + this.parameters.Length - 1;
			int holes = 1 + this.parameters.Length;
			DefaultInterpolatedStringHandler sh = new DefaultInterpolatedStringHandler(literals, holes);
			sh.AppendFormatted<Type>(this.ReturnType);
			sh.AppendLiteral(" (");
			for (int i = 0; i < this.parameters.Length; i++)
			{
				if (i != 0)
				{
					sh.AppendLiteral(", ");
				}
				sh.AppendFormatted<Type>(this.parameters[i]);
			}
			sh.AppendLiteral(")");
			return sh.ToStringAndClear();
		}

		// Token: 0x06002EE0 RID: 12000 RVA: 0x000A13A8 File Offset: 0x0009F5A8
		[NullableContext(0)]
		unsafe bool IDebugFormattable.TryFormatInto(Span<char> span, out int wrote)
		{
			wrote = 0;
			Span<char> span2 = span;
			Span<char> into = span2;
			bool flag;
			FormatIntoInterpolatedStringHandler formatIntoInterpolatedStringHandler = new FormatIntoInterpolatedStringHandler(2, 1, span2, ref flag);
			if (flag && formatIntoInterpolatedStringHandler.AppendFormatted<Type>(this.ReturnType))
			{
				formatIntoInterpolatedStringHandler.AppendLiteral(" (");
			}
			int w;
			if (!DebugFormatter.Into(into, out w, ref formatIntoInterpolatedStringHandler))
			{
				return false;
			}
			wrote += w;
			for (int i = 0; i < this.parameters.Length; i++)
			{
				if (i != 0)
				{
					if (!", ".AsSpan().TryCopyTo(span.Slice(wrote)))
					{
						return false;
					}
					wrote += 2;
				}
				span2 = span.Slice(wrote);
				Span<char> into2 = span2;
				bool flag2;
				FormatIntoInterpolatedStringHandler formatIntoInterpolatedStringHandler2 = new FormatIntoInterpolatedStringHandler(0, 1, span2, ref flag2);
				if (flag2)
				{
					formatIntoInterpolatedStringHandler2.AppendFormatted<Type>(this.parameters[i]);
				}
				if (!DebugFormatter.Into(into2, out w, ref formatIntoInterpolatedStringHandler2))
				{
					return false;
				}
				wrote += w;
			}
			if (span.Slice(wrote).Length < 1)
			{
				return false;
			}
			int num = wrote;
			wrote = num + 1;
			*span[num] = ')';
			return true;
		}

		// Token: 0x06002EE1 RID: 12001 RVA: 0x000A14AB File Offset: 0x0009F6AB
		[NullableContext(2)]
		public bool Equals(MethodSignature other)
		{
			return other != null && (this == other || (this.ReturnType.Equals(other.ReturnType) && this.Parameters.SequenceEqual(other.Parameters)));
		}

		// Token: 0x06002EE2 RID: 12002 RVA: 0x000A14E0 File Offset: 0x0009F6E0
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			MethodSignature sig = obj as MethodSignature;
			return sig != null && this.Equals(sig);
		}

		// Token: 0x06002EE3 RID: 12003 RVA: 0x000A1500 File Offset: 0x0009F700
		public override int GetHashCode()
		{
			HashCode hc = default(HashCode);
			hc.Add<Type>(this.ReturnType);
			hc.Add<int>(this.parameters.Length);
			foreach (Type type in this.parameters)
			{
				hc.Add<Type>(type);
			}
			return hc.ToHashCode();
		}

		// Token: 0x04003B46 RID: 15174
		private readonly Type[] parameters;

		// Token: 0x04003B47 RID: 15175
		private static readonly ConditionalWeakTable<MethodBase, MethodSignature> thisSigMap = new ConditionalWeakTable<MethodBase, MethodSignature>();

		// Token: 0x04003B48 RID: 15176
		private static readonly ConditionalWeakTable<MethodBase, MethodSignature> noThisSigMap = new ConditionalWeakTable<MethodBase, MethodSignature>();

		// Token: 0x020008D0 RID: 2256
		[Nullable(0)]
		private sealed class CompatableComparer : IEqualityComparer<Type>
		{
			// Token: 0x06002EE5 RID: 12005 RVA: 0x000A156F File Offset: 0x0009F76F
			[NullableContext(2)]
			public bool Equals(Type x, Type y)
			{
				return x == y || (x != null && y != null && x.IsCompatible(y));
			}

			// Token: 0x06002EE6 RID: 12006 RVA: 0x00003BBE File Offset: 0x00001DBE
			public int GetHashCode([DisallowNull] Type obj)
			{
				throw new NotSupportedException();
			}

			// Token: 0x04003B49 RID: 15177
			public static readonly MethodSignature.CompatableComparer Instance = new MethodSignature.CompatableComparer();
		}
	}
}
