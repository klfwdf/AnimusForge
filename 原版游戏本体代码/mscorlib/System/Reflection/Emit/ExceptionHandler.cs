using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000648 RID: 1608
	[ComVisible(false)]
	public struct ExceptionHandler : IEquatable<ExceptionHandler>
	{
		// Token: 0x17000BC4 RID: 3012
		// (get) Token: 0x06004B73 RID: 19315 RVA: 0x0011176F File Offset: 0x0010F96F
		public int ExceptionTypeToken
		{
			get
			{
				return this.m_exceptionClass;
			}
		}

		// Token: 0x17000BC5 RID: 3013
		// (get) Token: 0x06004B74 RID: 19316 RVA: 0x00111777 File Offset: 0x0010F977
		public int TryOffset
		{
			get
			{
				return this.m_tryStartOffset;
			}
		}

		// Token: 0x17000BC6 RID: 3014
		// (get) Token: 0x06004B75 RID: 19317 RVA: 0x0011177F File Offset: 0x0010F97F
		public int TryLength
		{
			get
			{
				return this.m_tryEndOffset - this.m_tryStartOffset;
			}
		}

		// Token: 0x17000BC7 RID: 3015
		// (get) Token: 0x06004B76 RID: 19318 RVA: 0x0011178E File Offset: 0x0010F98E
		public int FilterOffset
		{
			get
			{
				return this.m_filterOffset;
			}
		}

		// Token: 0x17000BC8 RID: 3016
		// (get) Token: 0x06004B77 RID: 19319 RVA: 0x00111796 File Offset: 0x0010F996
		public int HandlerOffset
		{
			get
			{
				return this.m_handlerStartOffset;
			}
		}

		// Token: 0x17000BC9 RID: 3017
		// (get) Token: 0x06004B78 RID: 19320 RVA: 0x0011179E File Offset: 0x0010F99E
		public int HandlerLength
		{
			get
			{
				return this.m_handlerEndOffset - this.m_handlerStartOffset;
			}
		}

		// Token: 0x17000BCA RID: 3018
		// (get) Token: 0x06004B79 RID: 19321 RVA: 0x001117AD File Offset: 0x0010F9AD
		public ExceptionHandlingClauseOptions Kind
		{
			get
			{
				return this.m_kind;
			}
		}

		// Token: 0x06004B7A RID: 19322 RVA: 0x001117B8 File Offset: 0x0010F9B8
		public ExceptionHandler(int tryOffset, int tryLength, int filterOffset, int handlerOffset, int handlerLength, ExceptionHandlingClauseOptions kind, int exceptionTypeToken)
		{
			if (tryOffset < 0)
			{
				throw new ArgumentOutOfRangeException("tryOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (tryLength < 0)
			{
				throw new ArgumentOutOfRangeException("tryLength", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (filterOffset < 0)
			{
				throw new ArgumentOutOfRangeException("filterOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (handlerOffset < 0)
			{
				throw new ArgumentOutOfRangeException("handlerOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (handlerLength < 0)
			{
				throw new ArgumentOutOfRangeException("handlerLength", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if ((long)tryOffset + (long)tryLength > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("tryLength", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					0,
					int.MaxValue - tryOffset
				}));
			}
			if ((long)handlerOffset + (long)handlerLength > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("handlerLength", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					0,
					int.MaxValue - handlerOffset
				}));
			}
			if (kind == ExceptionHandlingClauseOptions.Clause && (exceptionTypeToken & 16777215) == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeToken", new object[] { exceptionTypeToken }), "exceptionTypeToken");
			}
			if (!ExceptionHandler.IsValidKind(kind))
			{
				throw new ArgumentOutOfRangeException("kind", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
			}
			this.m_tryStartOffset = tryOffset;
			this.m_tryEndOffset = tryOffset + tryLength;
			this.m_filterOffset = filterOffset;
			this.m_handlerStartOffset = handlerOffset;
			this.m_handlerEndOffset = handlerOffset + handlerLength;
			this.m_kind = kind;
			this.m_exceptionClass = exceptionTypeToken;
		}

		// Token: 0x06004B7B RID: 19323 RVA: 0x00111952 File Offset: 0x0010FB52
		internal ExceptionHandler(int tryStartOffset, int tryEndOffset, int filterOffset, int handlerStartOffset, int handlerEndOffset, int kind, int exceptionTypeToken)
		{
			this.m_tryStartOffset = tryStartOffset;
			this.m_tryEndOffset = tryEndOffset;
			this.m_filterOffset = filterOffset;
			this.m_handlerStartOffset = handlerStartOffset;
			this.m_handlerEndOffset = handlerEndOffset;
			this.m_kind = (ExceptionHandlingClauseOptions)kind;
			this.m_exceptionClass = exceptionTypeToken;
		}

		// Token: 0x06004B7C RID: 19324 RVA: 0x00111989 File Offset: 0x0010FB89
		private static bool IsValidKind(ExceptionHandlingClauseOptions kind)
		{
			return kind <= ExceptionHandlingClauseOptions.Finally || kind == ExceptionHandlingClauseOptions.Fault;
		}

		// Token: 0x06004B7D RID: 19325 RVA: 0x00111996 File Offset: 0x0010FB96
		public override int GetHashCode()
		{
			return this.m_exceptionClass ^ this.m_tryStartOffset ^ this.m_tryEndOffset ^ this.m_filterOffset ^ this.m_handlerStartOffset ^ this.m_handlerEndOffset ^ (int)this.m_kind;
		}

		// Token: 0x06004B7E RID: 19326 RVA: 0x001119C8 File Offset: 0x0010FBC8
		public override bool Equals(object obj)
		{
			return obj is ExceptionHandler && this.Equals((ExceptionHandler)obj);
		}

		// Token: 0x06004B7F RID: 19327 RVA: 0x001119E0 File Offset: 0x0010FBE0
		public bool Equals(ExceptionHandler other)
		{
			return other.m_exceptionClass == this.m_exceptionClass && other.m_tryStartOffset == this.m_tryStartOffset && other.m_tryEndOffset == this.m_tryEndOffset && other.m_filterOffset == this.m_filterOffset && other.m_handlerStartOffset == this.m_handlerStartOffset && other.m_handlerEndOffset == this.m_handlerEndOffset && other.m_kind == this.m_kind;
		}

		// Token: 0x06004B80 RID: 19328 RVA: 0x00111A51 File Offset: 0x0010FC51
		public static bool operator ==(ExceptionHandler left, ExceptionHandler right)
		{
			return left.Equals(right);
		}

		// Token: 0x06004B81 RID: 19329 RVA: 0x00111A5B File Offset: 0x0010FC5B
		public static bool operator !=(ExceptionHandler left, ExceptionHandler right)
		{
			return !left.Equals(right);
		}

		// Token: 0x04001F33 RID: 7987
		internal readonly int m_exceptionClass;

		// Token: 0x04001F34 RID: 7988
		internal readonly int m_tryStartOffset;

		// Token: 0x04001F35 RID: 7989
		internal readonly int m_tryEndOffset;

		// Token: 0x04001F36 RID: 7990
		internal readonly int m_filterOffset;

		// Token: 0x04001F37 RID: 7991
		internal readonly int m_handlerStartOffset;

		// Token: 0x04001F38 RID: 7992
		internal readonly int m_handlerEndOffset;

		// Token: 0x04001F39 RID: 7993
		internal readonly ExceptionHandlingClauseOptions m_kind;
	}
}
