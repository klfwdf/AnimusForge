using System;

namespace Steamworks
{
	// Token: 0x02000184 RID: 388
	[Serializable]
	public struct InputActionSetHandle_t : IEquatable<InputActionSetHandle_t>, IComparable<InputActionSetHandle_t>
	{
		// Token: 0x0600092A RID: 2346 RVA: 0x0000CDC3 File Offset: 0x0000AFC3
		public InputActionSetHandle_t(ulong value)
		{
			this.m_InputActionSetHandle = value;
		}

		// Token: 0x0600092B RID: 2347 RVA: 0x0000CDCC File Offset: 0x0000AFCC
		public override string ToString()
		{
			return this.m_InputActionSetHandle.ToString();
		}

		// Token: 0x0600092C RID: 2348 RVA: 0x0000CDD9 File Offset: 0x0000AFD9
		public override bool Equals(object other)
		{
			return other is InputActionSetHandle_t && this == (InputActionSetHandle_t)other;
		}

		// Token: 0x0600092D RID: 2349 RVA: 0x0000CDF6 File Offset: 0x0000AFF6
		public override int GetHashCode()
		{
			return this.m_InputActionSetHandle.GetHashCode();
		}

		// Token: 0x0600092E RID: 2350 RVA: 0x0000CE03 File Offset: 0x0000B003
		public static bool operator ==(InputActionSetHandle_t x, InputActionSetHandle_t y)
		{
			return x.m_InputActionSetHandle == y.m_InputActionSetHandle;
		}

		// Token: 0x0600092F RID: 2351 RVA: 0x0000CE13 File Offset: 0x0000B013
		public static bool operator !=(InputActionSetHandle_t x, InputActionSetHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x0000CE1F File Offset: 0x0000B01F
		public static explicit operator InputActionSetHandle_t(ulong value)
		{
			return new InputActionSetHandle_t(value);
		}

		// Token: 0x06000931 RID: 2353 RVA: 0x0000CE27 File Offset: 0x0000B027
		public static explicit operator ulong(InputActionSetHandle_t that)
		{
			return that.m_InputActionSetHandle;
		}

		// Token: 0x06000932 RID: 2354 RVA: 0x0000CE03 File Offset: 0x0000B003
		public bool Equals(InputActionSetHandle_t other)
		{
			return this.m_InputActionSetHandle == other.m_InputActionSetHandle;
		}

		// Token: 0x06000933 RID: 2355 RVA: 0x0000CE2F File Offset: 0x0000B02F
		public int CompareTo(InputActionSetHandle_t other)
		{
			return this.m_InputActionSetHandle.CompareTo(other.m_InputActionSetHandle);
		}

		// Token: 0x04000A17 RID: 2583
		public ulong m_InputActionSetHandle;
	}
}
