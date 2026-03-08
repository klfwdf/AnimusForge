using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x02000602 RID: 1538
	[Serializable]
	internal class MemberInfoSerializationHolder : ISerializable, IObjectReference
	{
		// Token: 0x060046ED RID: 18157 RVA: 0x001031A5 File Offset: 0x001013A5
		public static void GetSerializationInfo(SerializationInfo info, string name, RuntimeType reflectedClass, string signature, MemberTypes type)
		{
			MemberInfoSerializationHolder.GetSerializationInfo(info, name, reflectedClass, signature, null, type, null);
		}

		// Token: 0x060046EE RID: 18158 RVA: 0x001031B4 File Offset: 0x001013B4
		public static void GetSerializationInfo(SerializationInfo info, string name, RuntimeType reflectedClass, string signature, string signature2, MemberTypes type, Type[] genericArguments)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			string fullName = reflectedClass.Module.Assembly.FullName;
			string fullName2 = reflectedClass.FullName;
			info.SetType(typeof(MemberInfoSerializationHolder));
			info.AddValue("Name", name, typeof(string));
			info.AddValue("AssemblyName", fullName, typeof(string));
			info.AddValue("ClassName", fullName2, typeof(string));
			info.AddValue("Signature", signature, typeof(string));
			info.AddValue("Signature2", signature2, typeof(string));
			info.AddValue("MemberType", (int)type);
			info.AddValue("GenericArguments", genericArguments, typeof(Type[]));
		}

		// Token: 0x060046EF RID: 18159 RVA: 0x0010328C File Offset: 0x0010148C
		internal MemberInfoSerializationHolder(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			string @string = info.GetString("AssemblyName");
			string string2 = info.GetString("ClassName");
			if (@string == null || string2 == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
			}
			Assembly assembly = FormatterServices.LoadAssemblyFromString(@string);
			this.m_reflectedType = assembly.GetType(string2, true, false) as RuntimeType;
			this.m_memberName = info.GetString("Name");
			this.m_signature = info.GetString("Signature");
			this.m_signature2 = (string)info.GetValueNoThrow("Signature2", typeof(string));
			this.m_memberType = (MemberTypes)info.GetInt32("MemberType");
			this.m_info = info;
		}

		// Token: 0x060046F0 RID: 18160 RVA: 0x00103350 File Offset: 0x00101550
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x060046F1 RID: 18161 RVA: 0x00103364 File Offset: 0x00101564
		[SecurityCritical]
		public virtual object GetRealObject(StreamingContext context)
		{
			if (this.m_memberName == null || this.m_reflectedType == null || this.m_memberType == (MemberTypes)0)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
			}
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.OptionalParamBinding;
			MemberTypes memberType = this.m_memberType;
			switch (memberType)
			{
			case MemberTypes.Constructor:
			{
				if (this.m_signature == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_NullSignature"));
				}
				ConstructorInfo[] array = this.m_reflectedType.GetMember(this.m_memberName, MemberTypes.Constructor, bindingAttr) as ConstructorInfo[];
				if (array.Length == 1)
				{
					return array[0];
				}
				if (array.Length > 1)
				{
					for (int i = 0; i < array.Length; i++)
					{
						if (this.m_signature2 != null)
						{
							if (((RuntimeConstructorInfo)array[i]).SerializationToString().Equals(this.m_signature2))
							{
								return array[i];
							}
						}
						else if (array[i].ToString().Equals(this.m_signature))
						{
							return array[i];
						}
					}
				}
				throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
			}
			case MemberTypes.Event:
			{
				EventInfo[] array2 = this.m_reflectedType.GetMember(this.m_memberName, MemberTypes.Event, bindingAttr) as EventInfo[];
				if (array2.Length == 0)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
				}
				return array2[0];
			}
			case MemberTypes.Constructor | MemberTypes.Event:
				break;
			case MemberTypes.Field:
			{
				FieldInfo[] array3 = this.m_reflectedType.GetMember(this.m_memberName, MemberTypes.Field, bindingAttr) as FieldInfo[];
				if (array3.Length == 0)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
				}
				return array3[0];
			}
			default:
				if (memberType != MemberTypes.Method)
				{
					if (memberType == MemberTypes.Property)
					{
						PropertyInfo[] array4 = this.m_reflectedType.GetMember(this.m_memberName, MemberTypes.Property, bindingAttr) as PropertyInfo[];
						if (array4.Length == 0)
						{
							throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
						}
						if (array4.Length == 1)
						{
							return array4[0];
						}
						if (array4.Length > 1)
						{
							for (int j = 0; j < array4.Length; j++)
							{
								if (this.m_signature2 != null)
								{
									if (((RuntimePropertyInfo)array4[j]).SerializationToString().Equals(this.m_signature2))
									{
										return array4[j];
									}
								}
								else if (array4[j].ToString().Equals(this.m_signature))
								{
									return array4[j];
								}
							}
						}
						throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
					}
				}
				else
				{
					MethodInfo methodInfo = null;
					if (this.m_signature == null)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_NullSignature"));
					}
					Type[] array5 = this.m_info.GetValueNoThrow("GenericArguments", typeof(Type[])) as Type[];
					MethodInfo[] array6 = this.m_reflectedType.GetMember(this.m_memberName, MemberTypes.Method, bindingAttr) as MethodInfo[];
					if (array6.Length == 1)
					{
						methodInfo = array6[0];
					}
					else if (array6.Length > 1)
					{
						for (int k = 0; k < array6.Length; k++)
						{
							if (this.m_signature2 != null)
							{
								if (((RuntimeMethodInfo)array6[k]).SerializationToString().Equals(this.m_signature2))
								{
									methodInfo = array6[k];
									break;
								}
							}
							else if (array6[k].ToString().Equals(this.m_signature))
							{
								methodInfo = array6[k];
								break;
							}
							if (array5 != null && array6[k].IsGenericMethod && array6[k].GetGenericArguments().Length == array5.Length)
							{
								MethodInfo methodInfo2 = array6[k].MakeGenericMethod(array5);
								if (this.m_signature2 != null)
								{
									if (((RuntimeMethodInfo)methodInfo2).SerializationToString().Equals(this.m_signature2))
									{
										methodInfo = methodInfo2;
										break;
									}
								}
								else if (methodInfo2.ToString().Equals(this.m_signature))
								{
									methodInfo = methodInfo2;
									break;
								}
							}
						}
					}
					if (methodInfo == null)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", new object[] { this.m_memberName }));
					}
					if (!methodInfo.IsGenericMethodDefinition)
					{
						return methodInfo;
					}
					if (array5 == null)
					{
						return methodInfo;
					}
					if (array5[0] == null)
					{
						return null;
					}
					return methodInfo.MakeGenericMethod(array5);
				}
				break;
			}
			throw new ArgumentException(Environment.GetResourceString("Serialization_MemberTypeNotRecognized"));
		}

		// Token: 0x04001D58 RID: 7512
		private string m_memberName;

		// Token: 0x04001D59 RID: 7513
		private RuntimeType m_reflectedType;

		// Token: 0x04001D5A RID: 7514
		private string m_signature;

		// Token: 0x04001D5B RID: 7515
		private string m_signature2;

		// Token: 0x04001D5C RID: 7516
		private MemberTypes m_memberType;

		// Token: 0x04001D5D RID: 7517
		private SerializationInfo m_info;
	}
}
