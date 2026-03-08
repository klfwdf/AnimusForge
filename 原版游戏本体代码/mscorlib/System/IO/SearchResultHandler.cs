using System;
using System.Security;
using Microsoft.Win32;

namespace System.IO
{
	// Token: 0x02000190 RID: 400
	internal abstract class SearchResultHandler<TSource>
	{
		// Token: 0x060018A5 RID: 6309
		[SecurityCritical]
		internal abstract bool IsResultIncluded(ref Win32Native.WIN32_FIND_DATA findData);

		// Token: 0x060018A6 RID: 6310
		[SecurityCritical]
		internal abstract TSource CreateObject(Directory.SearchData searchData, ref Win32Native.WIN32_FIND_DATA findData);
	}
}
