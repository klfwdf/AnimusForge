using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HarmonyLib
{
	/// <summary>General extensions for common cases</summary>
	// Token: 0x020001BD RID: 445
	public static class GeneralExtensions
	{
		/// <summary>Joins an enumeration with a value converter and a delimiter to a string</summary>
		/// <typeparam name="T">The inner type of the enumeration</typeparam>
		/// <param name="enumeration">The enumeration</param>
		/// <param name="converter">An optional value converter (from T to string)</param>
		/// <param name="delimiter">An optional delimiter</param>
		/// <returns>The values joined into a string</returns>
		// Token: 0x060007A7 RID: 1959 RVA: 0x00018AC0 File Offset: 0x00016CC0
		public static string Join<T>(this IEnumerable<T> enumeration, Func<T, string> converter = null, string delimiter = ", ")
		{
			if (converter == null)
			{
				converter = (T t) => t.ToString();
			}
			return enumeration.Aggregate("", (string prev, T curr) => prev + ((prev.Length > 0) ? delimiter : "") + converter(curr));
		}

		/// <summary>Converts an array of types (for example methods arguments) into a human readable form</summary>
		/// <param name="parameters">The array of types</param>
		/// <returns>A human readable description including brackets</returns>
		// Token: 0x060007A8 RID: 1960 RVA: 0x00018B28 File Offset: 0x00016D28
		public static string Description(this Type[] parameters)
		{
			if (parameters == null)
			{
				return "NULL";
			}
			return "(" + parameters.Join((Type p) => p.FullDescription(), ", ") + ")";
		}

		/// <summary>A full description of a type</summary>
		/// <param name="type">The type</param>
		/// <returns>A human readable description</returns>
		// Token: 0x060007A9 RID: 1961 RVA: 0x00018B78 File Offset: 0x00016D78
		public static string FullDescription(this Type type)
		{
			if (type == null)
			{
				return "null";
			}
			string ns = type.Namespace;
			if (!string.IsNullOrEmpty(ns))
			{
				ns += ".";
			}
			string result = ns + type.Name;
			if (type.IsGenericType)
			{
				result += "<";
				Type[] subTypes = type.GetGenericArguments();
				for (int i = 0; i < subTypes.Length; i++)
				{
					if (!result.EndsWith("<", StringComparison.Ordinal))
					{
						result += ", ";
					}
					result += subTypes[i].FullDescription();
				}
				result += ">";
			}
			return result;
		}

		/// <summary>A a full description of a method or a constructor without assembly details but with generics</summary>
		/// <param name="member">The method/constructor</param>
		/// <returns>A human readable description</returns>
		// Token: 0x060007AA RID: 1962 RVA: 0x00018C18 File Offset: 0x00016E18
		public static string FullDescription(this MethodBase member)
		{
			if (member == null)
			{
				return "null";
			}
			Type returnType = AccessTools.GetReturnedType(member);
			StringBuilder result = new StringBuilder();
			if (member.IsStatic)
			{
				result.Append("static ");
			}
			if (member.IsAbstract)
			{
				result.Append("abstract ");
			}
			if (member.IsVirtual)
			{
				result.Append("virtual ");
			}
			result.Append(returnType.FullDescription() + " ");
			if (member.DeclaringType != null)
			{
				result.Append(member.DeclaringType.FullDescription() + "::");
			}
			string parameterString = member.GetParameters().Join((ParameterInfo p) => p.ParameterType.FullDescription() + " " + p.Name, ", ");
			result.Append(member.Name + "(" + parameterString + ")");
			return result.ToString();
		}

		/// <summary>A helper converting parameter infos to types</summary>
		/// <param name="pinfo">The array of parameter infos</param>
		/// <returns>An array of types</returns>
		// Token: 0x060007AB RID: 1963 RVA: 0x00018D05 File Offset: 0x00016F05
		public static Type[] Types(this ParameterInfo[] pinfo)
		{
			return (from pi in pinfo
				select pi.ParameterType).ToArray<Type>();
		}

		/// <summary>Tests if a type has the <see cref="T:HarmonyLib.HarmonyAttribute" /></summary>
		/// <param name="type">The class/type to test</param>
		/// <returns>True if the type has the <see cref="T:HarmonyLib.HarmonyAttribute" /></returns>
		// Token: 0x060007AC RID: 1964 RVA: 0x00018D31 File Offset: 0x00016F31
		public static bool HasHarmonyAttribute(this Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			return HarmonyMethodExtensions.GetFromType(type).Count > 0;
		}

		/// <summary>A helper to access a value via key from a dictionary</summary>
		/// <typeparam name="S">The key type</typeparam>
		/// <typeparam name="T">The value type</typeparam>
		/// <param name="dictionary">The dictionary</param>
		/// <param name="key">The key</param>
		/// <returns>The value for the key or the default value (of T) if that key does not exist</returns>
		// Token: 0x060007AD RID: 1965 RVA: 0x00018D50 File Offset: 0x00016F50
		public static T GetValueSafe<S, T>(this Dictionary<S, T> dictionary, S key)
		{
			T result;
			if (dictionary.TryGetValue(key, out result))
			{
				return result;
			}
			return default(T);
		}

		/// <summary>A helper to access a value via key from a dictionary with extra casting</summary>
		/// <typeparam name="T">The value type</typeparam>
		/// <param name="dictionary">The dictionary</param>
		/// <param name="key">The key</param>
		/// <returns>The value for the key or the default value (of T) if that key does not exist or cannot be cast to T</returns>
		// Token: 0x060007AE RID: 1966 RVA: 0x00018D74 File Offset: 0x00016F74
		public static T GetTypedValue<T>(this Dictionary<string, object> dictionary, string key)
		{
			object result;
			if (dictionary.TryGetValue(key, out result) && result is T)
			{
				return (T)((object)result);
			}
			return default(T);
		}

		/// <summary>Escapes Unicode and ASCII non printable characters</summary>
		/// <param name="input">The string to convert</param>
		/// <param name="quoteChar">The string to convert</param>
		/// <returns>A string literal surrounded by <paramref name="quoteChar" /></returns>
		// Token: 0x060007AF RID: 1967 RVA: 0x00018DA4 File Offset: 0x00016FA4
		public static string ToLiteral(this string input, string quoteChar = "\"")
		{
			StringBuilder literal = new StringBuilder(input.Length + 2);
			literal.Append(quoteChar);
			int i = 0;
			while (i < input.Length)
			{
				char c = input[i];
				if (c <= '"')
				{
					switch (c)
					{
					case '\0':
						literal.Append("\\0");
						break;
					case '\u0001':
					case '\u0002':
					case '\u0003':
					case '\u0004':
					case '\u0005':
					case '\u0006':
						goto IL_12C;
					case '\a':
						literal.Append("\\a");
						break;
					case '\b':
						literal.Append("\\b");
						break;
					case '\t':
						literal.Append("\\t");
						break;
					case '\n':
						literal.Append("\\n");
						break;
					case '\v':
						literal.Append("\\v");
						break;
					case '\f':
						literal.Append("\\f");
						break;
					case '\r':
						literal.Append("\\r");
						break;
					default:
						if (c != '"')
						{
							goto IL_12C;
						}
						literal.Append("\\\"");
						break;
					}
				}
				else if (c != '\'')
				{
					if (c != '\\')
					{
						goto IL_12C;
					}
					literal.Append("\\\\");
				}
				else
				{
					literal.Append("\\'");
				}
				IL_162:
				i++;
				continue;
				IL_12C:
				if (c >= ' ' && c <= '~')
				{
					literal.Append(c);
					goto IL_162;
				}
				literal.Append("\\u");
				StringBuilder stringBuilder = literal;
				int num = (int)c;
				stringBuilder.Append(num.ToString("x4"));
				goto IL_162;
			}
			literal.Append(quoteChar);
			return literal.ToString();
		}
	}
}
