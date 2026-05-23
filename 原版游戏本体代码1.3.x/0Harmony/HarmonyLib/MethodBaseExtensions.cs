using System;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>General extensions for collections</summary>
	// Token: 0x020001C4 RID: 452
	public static class MethodBaseExtensions
	{
		/// <summary>Tests a class member if it has an IL method body (external methods for example don't have a body)</summary>
		/// <param name="member">The member to test</param>
		/// <returns>Returns true if the member has an IL body or false if not</returns>
		// Token: 0x060007E3 RID: 2019 RVA: 0x00019EEC File Offset: 0x000180EC
		public static bool HasMethodBody(this MethodBase member)
		{
			MethodBody methodBody = member.GetMethodBody();
			int? num;
			if (methodBody == null)
			{
				num = null;
			}
			else
			{
				byte[] ilasByteArray = methodBody.GetILAsByteArray();
				num = ((ilasByteArray != null) ? new int?(ilasByteArray.Length) : null);
			}
			int? num2 = num;
			return num2.GetValueOrDefault() > 0;
		}
	}
}
