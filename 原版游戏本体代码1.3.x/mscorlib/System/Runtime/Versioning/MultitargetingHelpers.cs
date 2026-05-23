using System;
using System.Security;
using System.Threading;

namespace System.Runtime.Versioning
{
	// Token: 0x02000726 RID: 1830
	internal static class MultitargetingHelpers
	{
		// Token: 0x06005166 RID: 20838 RVA: 0x0011F0D8 File Offset: 0x0011D2D8
		internal static string GetAssemblyQualifiedName(Type type, Func<Type, string> converter)
		{
			string text = null;
			if (type != null)
			{
				if (converter != null)
				{
					try
					{
						text = converter(type);
					}
					catch (Exception ex)
					{
						if (MultitargetingHelpers.IsSecurityOrCriticalException(ex))
						{
							throw;
						}
					}
				}
				if (text == null)
				{
					text = MultitargetingHelpers.defaultConverter(type);
				}
			}
			return text;
		}

		// Token: 0x06005167 RID: 20839 RVA: 0x0011F12C File Offset: 0x0011D32C
		private static bool IsCriticalException(Exception ex)
		{
			return ex is NullReferenceException || ex is StackOverflowException || ex is OutOfMemoryException || ex is ThreadAbortException || ex is IndexOutOfRangeException || ex is AccessViolationException;
		}

		// Token: 0x06005168 RID: 20840 RVA: 0x0011F161 File Offset: 0x0011D361
		private static bool IsSecurityOrCriticalException(Exception ex)
		{
			return ex is SecurityException || MultitargetingHelpers.IsCriticalException(ex);
		}

		// Token: 0x0400242F RID: 9263
		private static Func<Type, string> defaultConverter = (Type t) => t.AssemblyQualifiedName;
	}
}
