using System;

namespace HarmonyLib
{
	/// <summary>A reflection helper to read and write private elements</summary>
	/// <typeparam name="T">The result type defined by GetValue()</typeparam>
	// Token: 0x020001C9 RID: 457
	public class Traverse<T>
	{
		// Token: 0x06000809 RID: 2057 RVA: 0x00002B15 File Offset: 0x00000D15
		private Traverse()
		{
		}

		/// <summary>Creates a traverse instance from an existing instance</summary>
		/// <param name="traverse">The existing <see cref="T:HarmonyLib.Traverse" /> instance</param>
		// Token: 0x0600080A RID: 2058 RVA: 0x0001ACCC File Offset: 0x00018ECC
		public Traverse(Traverse traverse)
		{
			this.traverse = traverse;
		}

		/// <summary>Gets/Sets the current value</summary>
		/// <value>The value to read or write</value>
		// Token: 0x170001F9 RID: 505
		// (get) Token: 0x0600080B RID: 2059 RVA: 0x0001ACDB File Offset: 0x00018EDB
		// (set) Token: 0x0600080C RID: 2060 RVA: 0x0001ACE8 File Offset: 0x00018EE8
		public T Value
		{
			get
			{
				return this.traverse.GetValue<T>();
			}
			set
			{
				this.traverse.SetValue(value);
			}
		}

		// Token: 0x040002BF RID: 703
		private readonly Traverse traverse;
	}
}
