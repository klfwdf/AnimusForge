using System;
using System.Collections.Generic;

namespace System.Reflection
{
	// Token: 0x020005CD RID: 1485
	[__DynamicallyInvokable]
	public static class CustomAttributeExtensions
	{
		// Token: 0x060044B8 RID: 17592 RVA: 0x000FD136 File Offset: 0x000FB336
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this Assembly element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType);
		}

		// Token: 0x060044B9 RID: 17593 RVA: 0x000FD13F File Offset: 0x000FB33F
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this Module element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType);
		}

		// Token: 0x060044BA RID: 17594 RVA: 0x000FD148 File Offset: 0x000FB348
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType);
		}

		// Token: 0x060044BB RID: 17595 RVA: 0x000FD151 File Offset: 0x000FB351
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this ParameterInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttribute(element, attributeType);
		}

		// Token: 0x060044BC RID: 17596 RVA: 0x000FD15A File Offset: 0x000FB35A
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this Assembly element) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T)));
		}

		// Token: 0x060044BD RID: 17597 RVA: 0x000FD171 File Offset: 0x000FB371
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this Module element) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T)));
		}

		// Token: 0x060044BE RID: 17598 RVA: 0x000FD188 File Offset: 0x000FB388
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T)));
		}

		// Token: 0x060044BF RID: 17599 RVA: 0x000FD19F File Offset: 0x000FB39F
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this ParameterInfo element) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T)));
		}

		// Token: 0x060044C0 RID: 17600 RVA: 0x000FD1B6 File Offset: 0x000FB3B6
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType, bool inherit)
		{
			return Attribute.GetCustomAttribute(element, attributeType, inherit);
		}

		// Token: 0x060044C1 RID: 17601 RVA: 0x000FD1C0 File Offset: 0x000FB3C0
		[__DynamicallyInvokable]
		public static Attribute GetCustomAttribute(this ParameterInfo element, Type attributeType, bool inherit)
		{
			return Attribute.GetCustomAttribute(element, attributeType, inherit);
		}

		// Token: 0x060044C2 RID: 17602 RVA: 0x000FD1CA File Offset: 0x000FB3CA
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T), inherit));
		}

		// Token: 0x060044C3 RID: 17603 RVA: 0x000FD1E2 File Offset: 0x000FB3E2
		[__DynamicallyInvokable]
		public static T GetCustomAttribute<T>(this ParameterInfo element, bool inherit) where T : Attribute
		{
			return (T)((object)element.GetCustomAttribute(typeof(T), inherit));
		}

		// Token: 0x060044C4 RID: 17604 RVA: 0x000FD1FA File Offset: 0x000FB3FA
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element)
		{
			return Attribute.GetCustomAttributes(element);
		}

		// Token: 0x060044C5 RID: 17605 RVA: 0x000FD202 File Offset: 0x000FB402
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this Module element)
		{
			return Attribute.GetCustomAttributes(element);
		}

		// Token: 0x060044C6 RID: 17606 RVA: 0x000FD20A File Offset: 0x000FB40A
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element)
		{
			return Attribute.GetCustomAttributes(element);
		}

		// Token: 0x060044C7 RID: 17607 RVA: 0x000FD212 File Offset: 0x000FB412
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element)
		{
			return Attribute.GetCustomAttributes(element);
		}

		// Token: 0x060044C8 RID: 17608 RVA: 0x000FD21A File Offset: 0x000FB41A
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, bool inherit)
		{
			return Attribute.GetCustomAttributes(element, inherit);
		}

		// Token: 0x060044C9 RID: 17609 RVA: 0x000FD223 File Offset: 0x000FB423
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, bool inherit)
		{
			return Attribute.GetCustomAttributes(element, inherit);
		}

		// Token: 0x060044CA RID: 17610 RVA: 0x000FD22C File Offset: 0x000FB42C
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType);
		}

		// Token: 0x060044CB RID: 17611 RVA: 0x000FD235 File Offset: 0x000FB435
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this Module element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType);
		}

		// Token: 0x060044CC RID: 17612 RVA: 0x000FD23E File Offset: 0x000FB43E
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType);
		}

		// Token: 0x060044CD RID: 17613 RVA: 0x000FD247 File Offset: 0x000FB447
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType)
		{
			return Attribute.GetCustomAttributes(element, attributeType);
		}

		// Token: 0x060044CE RID: 17614 RVA: 0x000FD250 File Offset: 0x000FB450
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this Assembly element) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T));
		}

		// Token: 0x060044CF RID: 17615 RVA: 0x000FD267 File Offset: 0x000FB467
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this Module element) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T));
		}

		// Token: 0x060044D0 RID: 17616 RVA: 0x000FD27E File Offset: 0x000FB47E
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T));
		}

		// Token: 0x060044D1 RID: 17617 RVA: 0x000FD295 File Offset: 0x000FB495
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T));
		}

		// Token: 0x060044D2 RID: 17618 RVA: 0x000FD2AC File Offset: 0x000FB4AC
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType, bool inherit)
		{
			return Attribute.GetCustomAttributes(element, attributeType, inherit);
		}

		// Token: 0x060044D3 RID: 17619 RVA: 0x000FD2B6 File Offset: 0x000FB4B6
		[__DynamicallyInvokable]
		public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType, bool inherit)
		{
			return Attribute.GetCustomAttributes(element, attributeType, inherit);
		}

		// Token: 0x060044D4 RID: 17620 RVA: 0x000FD2C0 File Offset: 0x000FB4C0
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T), inherit);
		}

		// Token: 0x060044D5 RID: 17621 RVA: 0x000FD2D8 File Offset: 0x000FB4D8
		[__DynamicallyInvokable]
		public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element, bool inherit) where T : Attribute
		{
			return (IEnumerable<T>)element.GetCustomAttributes(typeof(T), inherit);
		}

		// Token: 0x060044D6 RID: 17622 RVA: 0x000FD2F0 File Offset: 0x000FB4F0
		[__DynamicallyInvokable]
		public static bool IsDefined(this Assembly element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType);
		}

		// Token: 0x060044D7 RID: 17623 RVA: 0x000FD2F9 File Offset: 0x000FB4F9
		[__DynamicallyInvokable]
		public static bool IsDefined(this Module element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType);
		}

		// Token: 0x060044D8 RID: 17624 RVA: 0x000FD302 File Offset: 0x000FB502
		[__DynamicallyInvokable]
		public static bool IsDefined(this MemberInfo element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType);
		}

		// Token: 0x060044D9 RID: 17625 RVA: 0x000FD30B File Offset: 0x000FB50B
		[__DynamicallyInvokable]
		public static bool IsDefined(this ParameterInfo element, Type attributeType)
		{
			return Attribute.IsDefined(element, attributeType);
		}

		// Token: 0x060044DA RID: 17626 RVA: 0x000FD314 File Offset: 0x000FB514
		[__DynamicallyInvokable]
		public static bool IsDefined(this MemberInfo element, Type attributeType, bool inherit)
		{
			return Attribute.IsDefined(element, attributeType, inherit);
		}

		// Token: 0x060044DB RID: 17627 RVA: 0x000FD31E File Offset: 0x000FB51E
		[__DynamicallyInvokable]
		public static bool IsDefined(this ParameterInfo element, Type attributeType, bool inherit)
		{
			return Attribute.IsDefined(element, attributeType, inherit);
		}
	}
}
