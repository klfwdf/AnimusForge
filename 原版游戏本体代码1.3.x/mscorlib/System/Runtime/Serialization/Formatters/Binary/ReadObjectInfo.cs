using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A2 RID: 1954
	internal sealed class ReadObjectInfo
	{
		// Token: 0x060054D3 RID: 21715 RVA: 0x0012D8A1 File Offset: 0x0012BAA1
		internal ReadObjectInfo()
		{
		}

		// Token: 0x060054D4 RID: 21716 RVA: 0x0012D8A9 File Offset: 0x0012BAA9
		internal void ObjectEnd()
		{
		}

		// Token: 0x060054D5 RID: 21717 RVA: 0x0012D8AB File Offset: 0x0012BAAB
		internal void PrepareForReuse()
		{
			this.lastPosition = 0;
		}

		// Token: 0x060054D6 RID: 21718 RVA: 0x0012D8B4 File Offset: 0x0012BAB4
		[SecurityCritical]
		internal static ReadObjectInfo Create(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
		{
			ReadObjectInfo objectInfo = ReadObjectInfo.GetObjectInfo(serObjectInfoInit);
			objectInfo.Init(objectType, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
			return objectInfo;
		}

		// Token: 0x060054D7 RID: 21719 RVA: 0x0012D8DA File Offset: 0x0012BADA
		[SecurityCritical]
		internal void Init(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
		{
			this.objectType = objectType;
			this.objectManager = objectManager;
			this.context = context;
			this.serObjectInfoInit = serObjectInfoInit;
			this.formatterConverter = converter;
			this.bSimpleAssembly = bSimpleAssembly;
			this.InitReadConstructor(objectType, surrogateSelector, context);
		}

		// Token: 0x060054D8 RID: 21720 RVA: 0x0012D914 File Offset: 0x0012BB14
		[SecurityCritical]
		internal static ReadObjectInfo Create(Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
		{
			ReadObjectInfo objectInfo = ReadObjectInfo.GetObjectInfo(serObjectInfoInit);
			objectInfo.Init(objectType, memberNames, memberTypes, surrogateSelector, context, objectManager, serObjectInfoInit, converter, bSimpleAssembly);
			return objectInfo;
		}

		// Token: 0x060054D9 RID: 21721 RVA: 0x0012D940 File Offset: 0x0012BB40
		[SecurityCritical]
		internal void Init(Type objectType, string[] memberNames, Type[] memberTypes, ISurrogateSelector surrogateSelector, StreamingContext context, ObjectManager objectManager, SerObjectInfoInit serObjectInfoInit, IFormatterConverter converter, bool bSimpleAssembly)
		{
			this.objectType = objectType;
			this.objectManager = objectManager;
			this.wireMemberNames = memberNames;
			this.wireMemberTypes = memberTypes;
			this.context = context;
			this.serObjectInfoInit = serObjectInfoInit;
			this.formatterConverter = converter;
			this.bSimpleAssembly = bSimpleAssembly;
			if (memberNames != null)
			{
				this.isNamed = true;
			}
			if (memberTypes != null)
			{
				this.isTyped = true;
			}
			if (objectType != null)
			{
				this.InitReadConstructor(objectType, surrogateSelector, context);
			}
		}

		// Token: 0x060054DA RID: 21722 RVA: 0x0012D9AC File Offset: 0x0012BBAC
		[SecurityCritical]
		private void InitReadConstructor(Type objectType, ISurrogateSelector surrogateSelector, StreamingContext context)
		{
			if (objectType.IsArray)
			{
				this.InitNoMembers();
				return;
			}
			ISurrogateSelector surrogateSelector2 = null;
			if (surrogateSelector != null)
			{
				this.serializationSurrogate = surrogateSelector.GetSurrogate(objectType, context, out surrogateSelector2);
			}
			if (this.serializationSurrogate != null)
			{
				this.isSi = true;
			}
			else if (objectType != Converter.typeofObject && Converter.typeofISerializable.IsAssignableFrom(objectType))
			{
				this.isSi = true;
			}
			if (this.isSi)
			{
				this.InitSiRead();
				return;
			}
			this.InitMemberInfo();
		}

		// Token: 0x060054DB RID: 21723 RVA: 0x0012DA1F File Offset: 0x0012BC1F
		private void InitSiRead()
		{
			if (this.memberTypesList != null)
			{
				this.memberTypesList = new List<Type>(20);
			}
		}

		// Token: 0x060054DC RID: 21724 RVA: 0x0012DA36 File Offset: 0x0012BC36
		private void InitNoMembers()
		{
			this.cache = new SerObjectInfoCache(this.objectType);
		}

		// Token: 0x060054DD RID: 21725 RVA: 0x0012DA4C File Offset: 0x0012BC4C
		[SecurityCritical]
		private void InitMemberInfo()
		{
			this.cache = new SerObjectInfoCache(this.objectType);
			this.cache.memberInfos = FormatterServices.GetSerializableMembers(this.objectType, this.context);
			this.count = this.cache.memberInfos.Length;
			this.cache.memberNames = new string[this.count];
			this.cache.memberTypes = new Type[this.count];
			for (int i = 0; i < this.count; i++)
			{
				this.cache.memberNames[i] = this.cache.memberInfos[i].Name;
				this.cache.memberTypes[i] = this.GetMemberType(this.cache.memberInfos[i]);
			}
			this.isTyped = true;
			this.isNamed = true;
		}

		// Token: 0x060054DE RID: 21726 RVA: 0x0012DB24 File Offset: 0x0012BD24
		internal MemberInfo GetMemberInfo(string name)
		{
			if (this.cache == null)
			{
				return null;
			}
			if (this.isSi)
			{
				string key = "Serialization_MemberInfo";
				object[] array = new object[1];
				int num = 0;
				Type type = this.objectType;
				array[num] = ((type != null) ? type.ToString() : null) + " " + name;
				throw new SerializationException(Environment.GetResourceString(key, array));
			}
			if (this.cache.memberInfos == null)
			{
				string key2 = "Serialization_NoMemberInfo";
				object[] array2 = new object[1];
				int num2 = 0;
				Type type2 = this.objectType;
				array2[num2] = ((type2 != null) ? type2.ToString() : null) + " " + name;
				throw new SerializationException(Environment.GetResourceString(key2, array2));
			}
			int num3 = this.Position(name);
			if (num3 != -1)
			{
				return this.cache.memberInfos[this.Position(name)];
			}
			return null;
		}

		// Token: 0x060054DF RID: 21727 RVA: 0x0012DBE0 File Offset: 0x0012BDE0
		internal Type GetType(string name)
		{
			int num = this.Position(name);
			if (num == -1)
			{
				return null;
			}
			Type type;
			if (this.isTyped)
			{
				type = this.cache.memberTypes[num];
			}
			else
			{
				type = this.memberTypesList[num];
			}
			if (type == null)
			{
				string key = "Serialization_ISerializableTypes";
				object[] array = new object[1];
				int num2 = 0;
				Type type2 = this.objectType;
				array[num2] = ((type2 != null) ? type2.ToString() : null) + " " + name;
				throw new SerializationException(Environment.GetResourceString(key, array));
			}
			return type;
		}

		// Token: 0x060054E0 RID: 21728 RVA: 0x0012DC5C File Offset: 0x0012BE5C
		internal void AddValue(string name, object value, ref SerializationInfo si, ref object[] memberData)
		{
			if (this.isSi)
			{
				si.AddValue(name, value);
				return;
			}
			int num = this.Position(name);
			if (num != -1)
			{
				memberData[num] = value;
			}
		}

		// Token: 0x060054E1 RID: 21729 RVA: 0x0012DC90 File Offset: 0x0012BE90
		internal void InitDataStore(ref SerializationInfo si, ref object[] memberData)
		{
			if (this.isSi)
			{
				if (si == null)
				{
					si = new SerializationInfo(this.objectType, this.formatterConverter);
					return;
				}
			}
			else if (memberData == null && this.cache != null)
			{
				memberData = new object[this.cache.memberNames.Length];
			}
		}

		// Token: 0x060054E2 RID: 21730 RVA: 0x0012DCE0 File Offset: 0x0012BEE0
		internal void RecordFixup(long objectId, string name, long idRef)
		{
			if (this.isSi)
			{
				this.objectManager.RecordDelayedFixup(objectId, name, idRef);
				return;
			}
			int num = this.Position(name);
			if (num != -1)
			{
				this.objectManager.RecordFixup(objectId, this.cache.memberInfos[num], idRef);
			}
		}

		// Token: 0x060054E3 RID: 21731 RVA: 0x0012DD2A File Offset: 0x0012BF2A
		[SecurityCritical]
		internal void PopulateObjectMembers(object obj, object[] memberData)
		{
			if (!this.isSi && memberData != null)
			{
				FormatterServices.PopulateObjectMembers(obj, this.cache.memberInfos, memberData);
			}
		}

		// Token: 0x060054E4 RID: 21732 RVA: 0x0012DD4C File Offset: 0x0012BF4C
		[Conditional("SER_LOGGING")]
		private void DumpPopulate(MemberInfo[] memberInfos, object[] memberData)
		{
			for (int i = 0; i < memberInfos.Length; i++)
			{
			}
		}

		// Token: 0x060054E5 RID: 21733 RVA: 0x0012DD67 File Offset: 0x0012BF67
		[Conditional("SER_LOGGING")]
		private void DumpPopulateSi()
		{
		}

		// Token: 0x060054E6 RID: 21734 RVA: 0x0012DD6C File Offset: 0x0012BF6C
		private int Position(string name)
		{
			if (this.cache == null)
			{
				return -1;
			}
			if (this.cache.memberNames.Length != 0 && this.cache.memberNames[this.lastPosition].Equals(name))
			{
				return this.lastPosition;
			}
			int num = this.lastPosition + 1;
			this.lastPosition = num;
			if (num < this.cache.memberNames.Length && this.cache.memberNames[this.lastPosition].Equals(name))
			{
				return this.lastPosition;
			}
			for (int i = 0; i < this.cache.memberNames.Length; i++)
			{
				if (this.cache.memberNames[i].Equals(name))
				{
					this.lastPosition = i;
					return this.lastPosition;
				}
			}
			this.lastPosition = 0;
			return -1;
		}

		// Token: 0x060054E7 RID: 21735 RVA: 0x0012DE38 File Offset: 0x0012C038
		internal Type[] GetMemberTypes(string[] inMemberNames, Type objectType)
		{
			if (this.isSi)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_ISerializableTypes", new object[] { objectType }));
			}
			if (this.cache == null)
			{
				return null;
			}
			if (this.cache.memberTypes == null)
			{
				this.cache.memberTypes = new Type[this.count];
				for (int i = 0; i < this.count; i++)
				{
					this.cache.memberTypes[i] = this.GetMemberType(this.cache.memberInfos[i]);
				}
			}
			bool flag = false;
			if (inMemberNames.Length < this.cache.memberInfos.Length)
			{
				flag = true;
			}
			Type[] array = new Type[this.cache.memberInfos.Length];
			for (int j = 0; j < this.cache.memberInfos.Length; j++)
			{
				if (!flag && inMemberNames[j].Equals(this.cache.memberInfos[j].Name))
				{
					array[j] = this.cache.memberTypes[j];
				}
				else
				{
					bool flag2 = false;
					for (int k = 0; k < inMemberNames.Length; k++)
					{
						if (this.cache.memberInfos[j].Name.Equals(inMemberNames[k]))
						{
							array[j] = this.cache.memberTypes[j];
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						object[] customAttributes = this.cache.memberInfos[j].GetCustomAttributes(typeof(OptionalFieldAttribute), false);
						if ((customAttributes == null || customAttributes.Length == 0) && !this.bSimpleAssembly)
						{
							throw new SerializationException(Environment.GetResourceString("Serialization_MissingMember", new object[]
							{
								this.cache.memberNames[j],
								objectType,
								typeof(OptionalFieldAttribute).FullName
							}));
						}
					}
				}
			}
			return array;
		}

		// Token: 0x060054E8 RID: 21736 RVA: 0x0012E004 File Offset: 0x0012C204
		internal Type GetMemberType(MemberInfo objMember)
		{
			Type result;
			if (objMember is FieldInfo)
			{
				result = ((FieldInfo)objMember).FieldType;
			}
			else
			{
				if (!(objMember is PropertyInfo))
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_SerMemberInfo", new object[] { objMember.GetType() }));
				}
				result = ((PropertyInfo)objMember).PropertyType;
			}
			return result;
		}

		// Token: 0x060054E9 RID: 21737 RVA: 0x0012E060 File Offset: 0x0012C260
		private static ReadObjectInfo GetObjectInfo(SerObjectInfoInit serObjectInfoInit)
		{
			return new ReadObjectInfo
			{
				objectInfoId = Interlocked.Increment(ref ReadObjectInfo.readObjectInfoCounter)
			};
		}

		// Token: 0x040026F3 RID: 9971
		internal int objectInfoId;

		// Token: 0x040026F4 RID: 9972
		internal static int readObjectInfoCounter;

		// Token: 0x040026F5 RID: 9973
		internal Type objectType;

		// Token: 0x040026F6 RID: 9974
		internal ObjectManager objectManager;

		// Token: 0x040026F7 RID: 9975
		internal int count;

		// Token: 0x040026F8 RID: 9976
		internal bool isSi;

		// Token: 0x040026F9 RID: 9977
		internal bool isNamed;

		// Token: 0x040026FA RID: 9978
		internal bool isTyped;

		// Token: 0x040026FB RID: 9979
		internal bool bSimpleAssembly;

		// Token: 0x040026FC RID: 9980
		internal SerObjectInfoCache cache;

		// Token: 0x040026FD RID: 9981
		internal string[] wireMemberNames;

		// Token: 0x040026FE RID: 9982
		internal Type[] wireMemberTypes;

		// Token: 0x040026FF RID: 9983
		private int lastPosition;

		// Token: 0x04002700 RID: 9984
		internal ISurrogateSelector surrogateSelector;

		// Token: 0x04002701 RID: 9985
		internal ISerializationSurrogate serializationSurrogate;

		// Token: 0x04002702 RID: 9986
		internal StreamingContext context;

		// Token: 0x04002703 RID: 9987
		internal List<Type> memberTypesList;

		// Token: 0x04002704 RID: 9988
		internal SerObjectInfoInit serObjectInfoInit;

		// Token: 0x04002705 RID: 9989
		internal IFormatterConverter formatterConverter;
	}
}
