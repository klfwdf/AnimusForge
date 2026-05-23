using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;
using System.Security;
using System.Text;

namespace System.Runtime.Remoting
{
	// Token: 0x020007CD RID: 1997
	[SecurityCritical]
	[ComVisible(true)]
	public class SoapServices
	{
		// Token: 0x060056A5 RID: 22181 RVA: 0x0013373B File Offset: 0x0013193B
		private SoapServices()
		{
		}

		// Token: 0x060056A6 RID: 22182 RVA: 0x00133743 File Offset: 0x00131943
		private static string CreateKey(string elementName, string elementNamespace)
		{
			if (elementNamespace == null)
			{
				return elementName;
			}
			return elementName + " " + elementNamespace;
		}

		// Token: 0x060056A7 RID: 22183 RVA: 0x00133756 File Offset: 0x00131956
		[SecurityCritical]
		public static void RegisterInteropXmlElement(string xmlElement, string xmlNamespace, Type type)
		{
			SoapServices._interopXmlElementToType[SoapServices.CreateKey(xmlElement, xmlNamespace)] = type;
			SoapServices._interopTypeToXmlElement[type] = new SoapServices.XmlEntry(xmlElement, xmlNamespace);
		}

		// Token: 0x060056A8 RID: 22184 RVA: 0x0013377C File Offset: 0x0013197C
		[SecurityCritical]
		public static void RegisterInteropXmlType(string xmlType, string xmlTypeNamespace, Type type)
		{
			SoapServices._interopXmlTypeToType[SoapServices.CreateKey(xmlType, xmlTypeNamespace)] = type;
			SoapServices._interopTypeToXmlType[type] = new SoapServices.XmlEntry(xmlType, xmlTypeNamespace);
		}

		// Token: 0x060056A9 RID: 22185 RVA: 0x001337A4 File Offset: 0x001319A4
		[SecurityCritical]
		public static void PreLoad(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!(type is RuntimeType))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			MethodInfo[] methods = type.GetMethods();
			foreach (MethodInfo mb in methods)
			{
				SoapServices.RegisterSoapActionForMethodBase(mb);
			}
			SoapTypeAttribute soapTypeAttribute = (SoapTypeAttribute)InternalRemotingServices.GetCachedSoapAttribute(type);
			if (soapTypeAttribute.IsInteropXmlElement())
			{
				SoapServices.RegisterInteropXmlElement(soapTypeAttribute.XmlElementName, soapTypeAttribute.XmlNamespace, type);
			}
			if (soapTypeAttribute.IsInteropXmlType())
			{
				SoapServices.RegisterInteropXmlType(soapTypeAttribute.XmlTypeName, soapTypeAttribute.XmlTypeNamespace, type);
			}
			int num = 0;
			SoapServices.XmlToFieldTypeMap xmlToFieldTypeMap = new SoapServices.XmlToFieldTypeMap();
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				SoapFieldAttribute soapFieldAttribute = (SoapFieldAttribute)InternalRemotingServices.GetCachedSoapAttribute(fieldInfo);
				if (soapFieldAttribute.IsInteropXmlElement())
				{
					string xmlElementName = soapFieldAttribute.XmlElementName;
					string xmlNamespace = soapFieldAttribute.XmlNamespace;
					if (soapFieldAttribute.UseAttribute)
					{
						xmlToFieldTypeMap.AddXmlAttribute(fieldInfo.FieldType, fieldInfo.Name, xmlElementName, xmlNamespace);
					}
					else
					{
						xmlToFieldTypeMap.AddXmlElement(fieldInfo.FieldType, fieldInfo.Name, xmlElementName, xmlNamespace);
					}
					num++;
				}
			}
			if (num > 0)
			{
				SoapServices._xmlToFieldTypeMap[type] = xmlToFieldTypeMap;
			}
		}

		// Token: 0x060056AA RID: 22186 RVA: 0x001338EC File Offset: 0x00131AEC
		[SecurityCritical]
		public static void PreLoad(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (!(assembly is RuntimeAssembly))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");
			}
			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				SoapServices.PreLoad(type);
			}
		}

		// Token: 0x060056AB RID: 22187 RVA: 0x0013394B File Offset: 0x00131B4B
		[SecurityCritical]
		public static Type GetInteropTypeFromXmlElement(string xmlElement, string xmlNamespace)
		{
			return (Type)SoapServices._interopXmlElementToType[SoapServices.CreateKey(xmlElement, xmlNamespace)];
		}

		// Token: 0x060056AC RID: 22188 RVA: 0x00133963 File Offset: 0x00131B63
		[SecurityCritical]
		public static Type GetInteropTypeFromXmlType(string xmlType, string xmlTypeNamespace)
		{
			return (Type)SoapServices._interopXmlTypeToType[SoapServices.CreateKey(xmlType, xmlTypeNamespace)];
		}

		// Token: 0x060056AD RID: 22189 RVA: 0x0013397C File Offset: 0x00131B7C
		public static void GetInteropFieldTypeAndNameFromXmlElement(Type containingType, string xmlElement, string xmlNamespace, out Type type, out string name)
		{
			if (containingType == null)
			{
				type = null;
				name = null;
				return;
			}
			SoapServices.XmlToFieldTypeMap xmlToFieldTypeMap = (SoapServices.XmlToFieldTypeMap)SoapServices._xmlToFieldTypeMap[containingType];
			if (xmlToFieldTypeMap != null)
			{
				xmlToFieldTypeMap.GetFieldTypeAndNameFromXmlElement(xmlElement, xmlNamespace, out type, out name);
				return;
			}
			type = null;
			name = null;
		}

		// Token: 0x060056AE RID: 22190 RVA: 0x001339C4 File Offset: 0x00131BC4
		public static void GetInteropFieldTypeAndNameFromXmlAttribute(Type containingType, string xmlAttribute, string xmlNamespace, out Type type, out string name)
		{
			if (containingType == null)
			{
				type = null;
				name = null;
				return;
			}
			SoapServices.XmlToFieldTypeMap xmlToFieldTypeMap = (SoapServices.XmlToFieldTypeMap)SoapServices._xmlToFieldTypeMap[containingType];
			if (xmlToFieldTypeMap != null)
			{
				xmlToFieldTypeMap.GetFieldTypeAndNameFromXmlAttribute(xmlAttribute, xmlNamespace, out type, out name);
				return;
			}
			type = null;
			name = null;
		}

		// Token: 0x060056AF RID: 22191 RVA: 0x00133A0C File Offset: 0x00131C0C
		[SecurityCritical]
		public static bool GetXmlElementForInteropType(Type type, out string xmlElement, out string xmlNamespace)
		{
			SoapServices.XmlEntry xmlEntry = (SoapServices.XmlEntry)SoapServices._interopTypeToXmlElement[type];
			if (xmlEntry != null)
			{
				xmlElement = xmlEntry.Name;
				xmlNamespace = xmlEntry.Namespace;
				return true;
			}
			SoapTypeAttribute soapTypeAttribute = (SoapTypeAttribute)InternalRemotingServices.GetCachedSoapAttribute(type);
			if (soapTypeAttribute.IsInteropXmlElement())
			{
				xmlElement = soapTypeAttribute.XmlElementName;
				xmlNamespace = soapTypeAttribute.XmlNamespace;
				return true;
			}
			xmlElement = null;
			xmlNamespace = null;
			return false;
		}

		// Token: 0x060056B0 RID: 22192 RVA: 0x00133A6C File Offset: 0x00131C6C
		[SecurityCritical]
		public static bool GetXmlTypeForInteropType(Type type, out string xmlType, out string xmlTypeNamespace)
		{
			SoapServices.XmlEntry xmlEntry = (SoapServices.XmlEntry)SoapServices._interopTypeToXmlType[type];
			if (xmlEntry != null)
			{
				xmlType = xmlEntry.Name;
				xmlTypeNamespace = xmlEntry.Namespace;
				return true;
			}
			SoapTypeAttribute soapTypeAttribute = (SoapTypeAttribute)InternalRemotingServices.GetCachedSoapAttribute(type);
			if (soapTypeAttribute.IsInteropXmlType())
			{
				xmlType = soapTypeAttribute.XmlTypeName;
				xmlTypeNamespace = soapTypeAttribute.XmlTypeNamespace;
				return true;
			}
			xmlType = null;
			xmlTypeNamespace = null;
			return false;
		}

		// Token: 0x060056B1 RID: 22193 RVA: 0x00133ACC File Offset: 0x00131CCC
		[SecurityCritical]
		public static string GetXmlNamespaceForMethodCall(MethodBase mb)
		{
			SoapMethodAttribute soapMethodAttribute = (SoapMethodAttribute)InternalRemotingServices.GetCachedSoapAttribute(mb);
			return soapMethodAttribute.XmlNamespace;
		}

		// Token: 0x060056B2 RID: 22194 RVA: 0x00133AEC File Offset: 0x00131CEC
		[SecurityCritical]
		public static string GetXmlNamespaceForMethodResponse(MethodBase mb)
		{
			SoapMethodAttribute soapMethodAttribute = (SoapMethodAttribute)InternalRemotingServices.GetCachedSoapAttribute(mb);
			return soapMethodAttribute.ResponseXmlNamespace;
		}

		// Token: 0x060056B3 RID: 22195 RVA: 0x00133B0C File Offset: 0x00131D0C
		[SecurityCritical]
		public static void RegisterSoapActionForMethodBase(MethodBase mb)
		{
			SoapMethodAttribute soapMethodAttribute = (SoapMethodAttribute)InternalRemotingServices.GetCachedSoapAttribute(mb);
			if (soapMethodAttribute.SoapActionExplicitySet)
			{
				SoapServices.RegisterSoapActionForMethodBase(mb, soapMethodAttribute.SoapAction);
			}
		}

		// Token: 0x060056B4 RID: 22196 RVA: 0x00133B3C File Offset: 0x00131D3C
		public static void RegisterSoapActionForMethodBase(MethodBase mb, string soapAction)
		{
			if (soapAction != null)
			{
				SoapServices._methodBaseToSoapAction[mb] = soapAction;
				ArrayList arrayList = (ArrayList)SoapServices._soapActionToMethodBase[soapAction];
				if (arrayList == null)
				{
					Hashtable soapActionToMethodBase = SoapServices._soapActionToMethodBase;
					lock (soapActionToMethodBase)
					{
						arrayList = ArrayList.Synchronized(new ArrayList());
						SoapServices._soapActionToMethodBase[soapAction] = arrayList;
					}
				}
				arrayList.Add(mb);
			}
		}

		// Token: 0x060056B5 RID: 22197 RVA: 0x00133BB8 File Offset: 0x00131DB8
		[SecurityCritical]
		public static string GetSoapActionFromMethodBase(MethodBase mb)
		{
			string text = (string)SoapServices._methodBaseToSoapAction[mb];
			if (text == null)
			{
				SoapMethodAttribute soapMethodAttribute = (SoapMethodAttribute)InternalRemotingServices.GetCachedSoapAttribute(mb);
				text = soapMethodAttribute.SoapAction;
			}
			return text;
		}

		// Token: 0x060056B6 RID: 22198 RVA: 0x00133BF0 File Offset: 0x00131DF0
		[SecurityCritical]
		public static bool IsSoapActionValidForMethodBase(string soapAction, MethodBase mb)
		{
			if (mb == null)
			{
				throw new ArgumentNullException("mb");
			}
			if (soapAction[0] == '"' && soapAction[soapAction.Length - 1] == '"')
			{
				soapAction = soapAction.Substring(1, soapAction.Length - 2);
			}
			SoapMethodAttribute soapMethodAttribute = (SoapMethodAttribute)InternalRemotingServices.GetCachedSoapAttribute(mb);
			if (string.CompareOrdinal(soapMethodAttribute.SoapAction, soapAction) == 0)
			{
				return true;
			}
			string text = (string)SoapServices._methodBaseToSoapAction[mb];
			if (text != null && string.CompareOrdinal(text, soapAction) == 0)
			{
				return true;
			}
			string[] array = soapAction.Split(new char[] { '#' });
			if (array.Length != 2)
			{
				return false;
			}
			bool flag;
			string typeNameForSoapActionNamespace = XmlNamespaceEncoder.GetTypeNameForSoapActionNamespace(array[0], out flag);
			if (typeNameForSoapActionNamespace == null)
			{
				return false;
			}
			string value = array[1];
			RuntimeMethodInfo runtimeMethodInfo = mb as RuntimeMethodInfo;
			RuntimeConstructorInfo runtimeConstructorInfo = mb as RuntimeConstructorInfo;
			RuntimeModule runtimeModule;
			if (runtimeMethodInfo != null)
			{
				runtimeModule = runtimeMethodInfo.GetRuntimeModule();
			}
			else
			{
				if (!(runtimeConstructorInfo != null))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
				}
				runtimeModule = runtimeConstructorInfo.GetRuntimeModule();
			}
			string text2 = mb.DeclaringType.FullName;
			if (flag)
			{
				text2 = text2 + ", " + runtimeModule.GetRuntimeAssembly().GetSimpleName();
			}
			return text2.Equals(typeNameForSoapActionNamespace) && mb.Name.Equals(value);
		}

		// Token: 0x060056B7 RID: 22199 RVA: 0x00133D3C File Offset: 0x00131F3C
		public static bool GetTypeAndMethodNameFromSoapAction(string soapAction, out string typeName, out string methodName)
		{
			if (soapAction[0] == '"' && soapAction[soapAction.Length - 1] == '"')
			{
				soapAction = soapAction.Substring(1, soapAction.Length - 2);
			}
			ArrayList arrayList = (ArrayList)SoapServices._soapActionToMethodBase[soapAction];
			if (arrayList != null)
			{
				if (arrayList.Count > 1)
				{
					typeName = null;
					methodName = null;
					return false;
				}
				MethodBase methodBase = (MethodBase)arrayList[0];
				if (methodBase != null)
				{
					RuntimeMethodInfo runtimeMethodInfo = methodBase as RuntimeMethodInfo;
					RuntimeConstructorInfo runtimeConstructorInfo = methodBase as RuntimeConstructorInfo;
					RuntimeModule runtimeModule;
					if (runtimeMethodInfo != null)
					{
						runtimeModule = runtimeMethodInfo.GetRuntimeModule();
					}
					else
					{
						if (!(runtimeConstructorInfo != null))
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
						}
						runtimeModule = runtimeConstructorInfo.GetRuntimeModule();
					}
					typeName = methodBase.DeclaringType.FullName + ", " + runtimeModule.GetRuntimeAssembly().GetSimpleName();
					methodName = methodBase.Name;
					return true;
				}
			}
			string[] array = soapAction.Split(new char[] { '#' });
			if (array.Length != 2)
			{
				typeName = null;
				methodName = null;
				return false;
			}
			bool flag;
			typeName = XmlNamespaceEncoder.GetTypeNameForSoapActionNamespace(array[0], out flag);
			if (typeName == null)
			{
				methodName = null;
				return false;
			}
			methodName = array[1];
			return true;
		}

		// Token: 0x17000E3B RID: 3643
		// (get) Token: 0x060056B8 RID: 22200 RVA: 0x00133E64 File Offset: 0x00132064
		public static string XmlNsForClrType
		{
			get
			{
				return SoapServices.startNS;
			}
		}

		// Token: 0x17000E3C RID: 3644
		// (get) Token: 0x060056B9 RID: 22201 RVA: 0x00133E6B File Offset: 0x0013206B
		public static string XmlNsForClrTypeWithAssembly
		{
			get
			{
				return SoapServices.assemblyNS;
			}
		}

		// Token: 0x17000E3D RID: 3645
		// (get) Token: 0x060056BA RID: 22202 RVA: 0x00133E72 File Offset: 0x00132072
		public static string XmlNsForClrTypeWithNs
		{
			get
			{
				return SoapServices.namespaceNS;
			}
		}

		// Token: 0x17000E3E RID: 3646
		// (get) Token: 0x060056BB RID: 22203 RVA: 0x00133E79 File Offset: 0x00132079
		public static string XmlNsForClrTypeWithNsAndAssembly
		{
			get
			{
				return SoapServices.fullNS;
			}
		}

		// Token: 0x060056BC RID: 22204 RVA: 0x00133E80 File Offset: 0x00132080
		public static bool IsClrTypeNamespace(string namespaceString)
		{
			return namespaceString.StartsWith(SoapServices.startNS, StringComparison.Ordinal);
		}

		// Token: 0x060056BD RID: 22205 RVA: 0x00133E94 File Offset: 0x00132094
		[SecurityCritical]
		public static string CodeXmlNamespaceForClrTypeNamespace(string typeNamespace, string assemblyName)
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			if (SoapServices.IsNameNull(typeNamespace))
			{
				if (SoapServices.IsNameNull(assemblyName))
				{
					throw new ArgumentNullException("typeNamespace,assemblyName");
				}
				stringBuilder.Append(SoapServices.assemblyNS);
				SoapServices.UriEncode(assemblyName, stringBuilder);
			}
			else if (SoapServices.IsNameNull(assemblyName))
			{
				stringBuilder.Append(SoapServices.namespaceNS);
				stringBuilder.Append(typeNamespace);
			}
			else
			{
				stringBuilder.Append(SoapServices.fullNS);
				if (typeNamespace[0] == '.')
				{
					stringBuilder.Append(typeNamespace.Substring(1));
				}
				else
				{
					stringBuilder.Append(typeNamespace);
				}
				stringBuilder.Append('/');
				SoapServices.UriEncode(assemblyName, stringBuilder);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060056BE RID: 22206 RVA: 0x00133F40 File Offset: 0x00132140
		[SecurityCritical]
		public static bool DecodeXmlNamespaceForClrTypeNamespace(string inNamespace, out string typeNamespace, out string assemblyName)
		{
			if (SoapServices.IsNameNull(inNamespace))
			{
				throw new ArgumentNullException("inNamespace");
			}
			assemblyName = null;
			typeNamespace = "";
			if (inNamespace.StartsWith(SoapServices.assemblyNS, StringComparison.Ordinal))
			{
				assemblyName = SoapServices.UriDecode(inNamespace.Substring(SoapServices.assemblyNS.Length));
			}
			else if (inNamespace.StartsWith(SoapServices.namespaceNS, StringComparison.Ordinal))
			{
				typeNamespace = inNamespace.Substring(SoapServices.namespaceNS.Length);
			}
			else
			{
				if (!inNamespace.StartsWith(SoapServices.fullNS, StringComparison.Ordinal))
				{
					return false;
				}
				int num = inNamespace.IndexOf("/", SoapServices.fullNS.Length);
				typeNamespace = inNamespace.Substring(SoapServices.fullNS.Length, num - SoapServices.fullNS.Length);
				assemblyName = SoapServices.UriDecode(inNamespace.Substring(num + 1));
			}
			return true;
		}

		// Token: 0x060056BF RID: 22207 RVA: 0x0013400C File Offset: 0x0013220C
		internal static void UriEncode(string value, StringBuilder sb)
		{
			if (value == null || value.Length == 0)
			{
				return;
			}
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] == ' ')
				{
					sb.Append("%20");
				}
				else if (value[i] == '=')
				{
					sb.Append("%3D");
				}
				else if (value[i] == ',')
				{
					sb.Append("%2C");
				}
				else
				{
					sb.Append(value[i]);
				}
			}
		}

		// Token: 0x060056C0 RID: 22208 RVA: 0x00134090 File Offset: 0x00132290
		internal static string UriDecode(string value)
		{
			if (value == null || value.Length == 0)
			{
				return value;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] == '%' && value.Length - i >= 3)
				{
					if (value[i + 1] == '2' && value[i + 2] == '0')
					{
						stringBuilder.Append(' ');
						i += 2;
					}
					else if (value[i + 1] == '3' && value[i + 2] == 'D')
					{
						stringBuilder.Append('=');
						i += 2;
					}
					else if (value[i + 1] == '2' && value[i + 2] == 'C')
					{
						stringBuilder.Append(',');
						i += 2;
					}
					else
					{
						stringBuilder.Append(value[i]);
					}
				}
				else
				{
					stringBuilder.Append(value[i]);
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060056C1 RID: 22209 RVA: 0x00134182 File Offset: 0x00132382
		private static bool IsNameNull(string name)
		{
			return name == null || name.Length == 0;
		}

		// Token: 0x040027AA RID: 10154
		private static Hashtable _interopXmlElementToType = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027AB RID: 10155
		private static Hashtable _interopTypeToXmlElement = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027AC RID: 10156
		private static Hashtable _interopXmlTypeToType = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027AD RID: 10157
		private static Hashtable _interopTypeToXmlType = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027AE RID: 10158
		private static Hashtable _xmlToFieldTypeMap = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027AF RID: 10159
		private static Hashtable _methodBaseToSoapAction = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027B0 RID: 10160
		private static Hashtable _soapActionToMethodBase = Hashtable.Synchronized(new Hashtable());

		// Token: 0x040027B1 RID: 10161
		internal static string startNS = "http://schemas.microsoft.com/clr/";

		// Token: 0x040027B2 RID: 10162
		internal static string assemblyNS = "http://schemas.microsoft.com/clr/assem/";

		// Token: 0x040027B3 RID: 10163
		internal static string namespaceNS = "http://schemas.microsoft.com/clr/ns/";

		// Token: 0x040027B4 RID: 10164
		internal static string fullNS = "http://schemas.microsoft.com/clr/nsassem/";

		// Token: 0x02000C6C RID: 3180
		private class XmlEntry
		{
			// Token: 0x060070A7 RID: 28839 RVA: 0x0018487E File Offset: 0x00182A7E
			public XmlEntry(string name, string xmlNamespace)
			{
				this.Name = name;
				this.Namespace = xmlNamespace;
			}

			// Token: 0x040037E1 RID: 14305
			public string Name;

			// Token: 0x040037E2 RID: 14306
			public string Namespace;
		}

		// Token: 0x02000C6D RID: 3181
		private class XmlToFieldTypeMap
		{
			// Token: 0x060070A9 RID: 28841 RVA: 0x001848B2 File Offset: 0x00182AB2
			[SecurityCritical]
			public void AddXmlElement(Type fieldType, string fieldName, string xmlElement, string xmlNamespace)
			{
				this._elements[SoapServices.CreateKey(xmlElement, xmlNamespace)] = new SoapServices.XmlToFieldTypeMap.FieldEntry(fieldType, fieldName);
			}

			// Token: 0x060070AA RID: 28842 RVA: 0x001848CE File Offset: 0x00182ACE
			[SecurityCritical]
			public void AddXmlAttribute(Type fieldType, string fieldName, string xmlAttribute, string xmlNamespace)
			{
				this._attributes[SoapServices.CreateKey(xmlAttribute, xmlNamespace)] = new SoapServices.XmlToFieldTypeMap.FieldEntry(fieldType, fieldName);
			}

			// Token: 0x060070AB RID: 28843 RVA: 0x001848EC File Offset: 0x00182AEC
			[SecurityCritical]
			public void GetFieldTypeAndNameFromXmlElement(string xmlElement, string xmlNamespace, out Type type, out string name)
			{
				SoapServices.XmlToFieldTypeMap.FieldEntry fieldEntry = (SoapServices.XmlToFieldTypeMap.FieldEntry)this._elements[SoapServices.CreateKey(xmlElement, xmlNamespace)];
				if (fieldEntry != null)
				{
					type = fieldEntry.Type;
					name = fieldEntry.Name;
					return;
				}
				type = null;
				name = null;
			}

			// Token: 0x060070AC RID: 28844 RVA: 0x00184930 File Offset: 0x00182B30
			[SecurityCritical]
			public void GetFieldTypeAndNameFromXmlAttribute(string xmlAttribute, string xmlNamespace, out Type type, out string name)
			{
				SoapServices.XmlToFieldTypeMap.FieldEntry fieldEntry = (SoapServices.XmlToFieldTypeMap.FieldEntry)this._attributes[SoapServices.CreateKey(xmlAttribute, xmlNamespace)];
				if (fieldEntry != null)
				{
					type = fieldEntry.Type;
					name = fieldEntry.Name;
					return;
				}
				type = null;
				name = null;
			}

			// Token: 0x040037E3 RID: 14307
			private Hashtable _attributes = new Hashtable();

			// Token: 0x040037E4 RID: 14308
			private Hashtable _elements = new Hashtable();

			// Token: 0x02000D13 RID: 3347
			private class FieldEntry
			{
				// Token: 0x0600722C RID: 29228 RVA: 0x001897FC File Offset: 0x001879FC
				public FieldEntry(Type type, string name)
				{
					this.Type = type;
					this.Name = name;
				}

				// Token: 0x0400396C RID: 14700
				public Type Type;

				// Token: 0x0400396D RID: 14701
				public string Name;
			}
		}
	}
}
