using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices.TCEAdapterGen
{
	// Token: 0x020009C5 RID: 2501
	internal class TCEAdapterGenerator
	{
		// Token: 0x060063B8 RID: 25528 RVA: 0x00154694 File Offset: 0x00152894
		public void Process(ModuleBuilder ModBldr, ArrayList EventItfList)
		{
			this.m_Module = ModBldr;
			int count = EventItfList.Count;
			for (int i = 0; i < count; i++)
			{
				EventItfInfo eventItfInfo = (EventItfInfo)EventItfList[i];
				Type eventItfType = eventItfInfo.GetEventItfType();
				Type srcItfType = eventItfInfo.GetSrcItfType();
				string eventProviderName = eventItfInfo.GetEventProviderName();
				Type sinkHelperType = new EventSinkHelperWriter(this.m_Module, srcItfType, eventItfType).Perform();
				new EventProviderWriter(this.m_Module, eventProviderName, eventItfType, srcItfType, sinkHelperType).Perform();
			}
		}

		// Token: 0x060063B9 RID: 25529 RVA: 0x0015470C File Offset: 0x0015290C
		internal static void SetClassInterfaceTypeToNone(TypeBuilder tb)
		{
			if (TCEAdapterGenerator.s_NoClassItfCABuilder == null)
			{
				Type[] types = new Type[] { typeof(ClassInterfaceType) };
				ConstructorInfo constructor = typeof(ClassInterfaceAttribute).GetConstructor(types);
				TCEAdapterGenerator.s_NoClassItfCABuilder = new CustomAttributeBuilder(constructor, new object[] { ClassInterfaceType.None });
			}
			tb.SetCustomAttribute(TCEAdapterGenerator.s_NoClassItfCABuilder);
		}

		// Token: 0x060063BA RID: 25530 RVA: 0x00154774 File Offset: 0x00152974
		internal static TypeBuilder DefineUniqueType(string strInitFullName, TypeAttributes attrs, Type BaseType, Type[] aInterfaceTypes, ModuleBuilder mb)
		{
			string text = strInitFullName;
			int num = 2;
			while (mb.GetType(text) != null)
			{
				text = strInitFullName + "_" + num.ToString();
				num++;
			}
			return mb.DefineType(text, attrs, BaseType, aInterfaceTypes);
		}

		// Token: 0x060063BB RID: 25531 RVA: 0x001547BC File Offset: 0x001529BC
		internal static void SetHiddenAttribute(TypeBuilder tb)
		{
			if (TCEAdapterGenerator.s_HiddenCABuilder == null)
			{
				Type[] types = new Type[] { typeof(TypeLibTypeFlags) };
				ConstructorInfo constructor = typeof(TypeLibTypeAttribute).GetConstructor(types);
				TCEAdapterGenerator.s_HiddenCABuilder = new CustomAttributeBuilder(constructor, new object[] { TypeLibTypeFlags.FHidden });
			}
			tb.SetCustomAttribute(TCEAdapterGenerator.s_HiddenCABuilder);
		}

		// Token: 0x060063BC RID: 25532 RVA: 0x00154824 File Offset: 0x00152A24
		internal static MethodInfo[] GetNonPropertyMethods(Type type)
		{
			MethodInfo[] methods = type.GetMethods();
			ArrayList arrayList = new ArrayList(methods);
			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				MethodInfo[] accessors = propertyInfo.GetAccessors();
				foreach (MethodInfo right in accessors)
				{
					for (int k = 0; k < arrayList.Count; k++)
					{
						if ((MethodInfo)arrayList[k] == right)
						{
							arrayList.RemoveAt(k);
						}
					}
				}
			}
			MethodInfo[] array3 = new MethodInfo[arrayList.Count];
			arrayList.CopyTo(array3);
			return array3;
		}

		// Token: 0x060063BD RID: 25533 RVA: 0x001548D4 File Offset: 0x00152AD4
		internal static MethodInfo[] GetPropertyMethods(Type type)
		{
			MethodInfo[] methods = type.GetMethods();
			ArrayList arrayList = new ArrayList();
			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				MethodInfo[] accessors = propertyInfo.GetAccessors();
				foreach (MethodInfo value in accessors)
				{
					arrayList.Add(value);
				}
			}
			MethodInfo[] array3 = new MethodInfo[arrayList.Count];
			arrayList.CopyTo(array3);
			return array3;
		}

		// Token: 0x04002CD7 RID: 11479
		private ModuleBuilder m_Module;

		// Token: 0x04002CD8 RID: 11480
		private Hashtable m_SrcItfToSrcItfInfoMap = new Hashtable();

		// Token: 0x04002CD9 RID: 11481
		private static volatile CustomAttributeBuilder s_NoClassItfCABuilder;

		// Token: 0x04002CDA RID: 11482
		private static volatile CustomAttributeBuilder s_HiddenCABuilder;
	}
}
