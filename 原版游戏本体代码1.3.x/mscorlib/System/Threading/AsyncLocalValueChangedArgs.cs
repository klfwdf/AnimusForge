using System;

namespace System.Threading
{
	// Token: 0x020004E7 RID: 1255
	[__DynamicallyInvokable]
	public struct AsyncLocalValueChangedArgs<T>
	{
		// Token: 0x17000902 RID: 2306
		// (get) Token: 0x06003B7E RID: 15230 RVA: 0x000E2566 File Offset: 0x000E0766
		// (set) Token: 0x06003B7F RID: 15231 RVA: 0x000E256E File Offset: 0x000E076E
		[__DynamicallyInvokable]
		public T PreviousValue
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x17000903 RID: 2307
		// (get) Token: 0x06003B80 RID: 15232 RVA: 0x000E2577 File Offset: 0x000E0777
		// (set) Token: 0x06003B81 RID: 15233 RVA: 0x000E257F File Offset: 0x000E077F
		[__DynamicallyInvokable]
		public T CurrentValue
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x17000904 RID: 2308
		// (get) Token: 0x06003B82 RID: 15234 RVA: 0x000E2588 File Offset: 0x000E0788
		// (set) Token: 0x06003B83 RID: 15235 RVA: 0x000E2590 File Offset: 0x000E0790
		[__DynamicallyInvokable]
		public bool ThreadContextChanged
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x06003B84 RID: 15236 RVA: 0x000E2599 File Offset: 0x000E0799
		internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged)
		{
			this = default(AsyncLocalValueChangedArgs<T>);
			this.PreviousValue = previousValue;
			this.CurrentValue = currentValue;
			this.ThreadContextChanged = contextChanged;
		}
	}
}
