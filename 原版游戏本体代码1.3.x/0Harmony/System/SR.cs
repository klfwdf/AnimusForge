using System;
using System.Resources;
using System.Runtime.CompilerServices;
using FxResources.System.ValueTuple;

namespace System
{
	// Token: 0x02000912 RID: 2322
	internal static class SR
	{
		// Token: 0x17000871 RID: 2161
		// (get) Token: 0x0600309B RID: 12443 RVA: 0x000A7604 File Offset: 0x000A5804
		private static ResourceManager ResourceManager
		{
			get
			{
				ResourceManager result;
				if ((result = System.SR.s_resourceManager) == null)
				{
					result = (System.SR.s_resourceManager = new ResourceManager(System.SR.ResourceType));
				}
				return result;
			}
		}

		// Token: 0x0600309C RID: 12444 RVA: 0x0001B69F File Offset: 0x0001989F
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static bool UsingResourceKeys()
		{
			return false;
		}

		// Token: 0x0600309D RID: 12445 RVA: 0x000A7620 File Offset: 0x000A5820
		internal static string GetResourceString(string resourceKey, string defaultString)
		{
			string text = null;
			try
			{
				text = System.SR.ResourceManager.GetString(resourceKey);
			}
			catch (MissingManifestResourceException)
			{
			}
			if (defaultString != null && resourceKey.Equals(text, StringComparison.Ordinal))
			{
				return defaultString;
			}
			return text;
		}

		// Token: 0x0600309E RID: 12446 RVA: 0x000A7660 File Offset: 0x000A5860
		internal static string Format(string resourceFormat, params object[] args)
		{
			if (args == null)
			{
				return resourceFormat;
			}
			if (System.SR.UsingResourceKeys())
			{
				return resourceFormat + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}

		// Token: 0x0600309F RID: 12447 RVA: 0x000A7687 File Offset: 0x000A5887
		internal static string Format(string resourceFormat, object p1)
		{
			if (System.SR.UsingResourceKeys())
			{
				return string.Join(", ", new object[] { resourceFormat, p1 });
			}
			return string.Format(resourceFormat, p1);
		}

		// Token: 0x060030A0 RID: 12448 RVA: 0x000A76B0 File Offset: 0x000A58B0
		internal static string Format(string resourceFormat, object p1, object p2)
		{
			if (System.SR.UsingResourceKeys())
			{
				return string.Join(", ", new object[] { resourceFormat, p1, p2 });
			}
			return string.Format(resourceFormat, p1, p2);
		}

		// Token: 0x060030A1 RID: 12449 RVA: 0x000A76DE File Offset: 0x000A58DE
		internal static string Format(string resourceFormat, object p1, object p2, object p3)
		{
			if (System.SR.UsingResourceKeys())
			{
				return string.Join(", ", new object[] { resourceFormat, p1, p2, p3 });
			}
			return string.Format(resourceFormat, p1, p2, p3);
		}

		// Token: 0x17000872 RID: 2162
		// (get) Token: 0x060030A2 RID: 12450 RVA: 0x000A7711 File Offset: 0x000A5911
		internal static Type ResourceType { get; } = typeof(FxResources.System.ValueTuple.SR);

		// Token: 0x17000873 RID: 2163
		// (get) Token: 0x060030A3 RID: 12451 RVA: 0x000A7718 File Offset: 0x000A5918
		internal static string ArgumentException_ValueTupleIncorrectType
		{
			get
			{
				return System.SR.GetResourceString("ArgumentException_ValueTupleIncorrectType", null);
			}
		}

		// Token: 0x17000874 RID: 2164
		// (get) Token: 0x060030A4 RID: 12452 RVA: 0x000A7725 File Offset: 0x000A5925
		internal static string ArgumentException_ValueTupleLastArgumentNotAValueTuple
		{
			get
			{
				return System.SR.GetResourceString("ArgumentException_ValueTupleLastArgumentNotAValueTuple", null);
			}
		}

		// Token: 0x04003C19 RID: 15385
		private static ResourceManager s_resourceManager;
	}
}
