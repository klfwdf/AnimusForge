using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Get and set values for a <see cref="T:System.Reflection.MemberInfo" /> using reflection.
	/// </summary>
	// Token: 0x020000A1 RID: 161
	[NullableContext(1)]
	[Nullable(0)]
	public class ReflectionValueProvider : IValueProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.ReflectionValueProvider" /> class.
		/// </summary>
		/// <param name="memberInfo">The member info.</param>
		// Token: 0x0600083F RID: 2111 RVA: 0x000241CD File Offset: 0x000223CD
		public ReflectionValueProvider(MemberInfo memberInfo)
		{
			ValidationUtils.ArgumentNotNull(memberInfo, "memberInfo");
			this._memberInfo = memberInfo;
		}

		/// <summary>
		/// Sets the value.
		/// </summary>
		/// <param name="target">The target to set the value on.</param>
		/// <param name="value">The value to set on the target.</param>
		// Token: 0x06000840 RID: 2112 RVA: 0x000241E8 File Offset: 0x000223E8
		public void SetValue(object target, [Nullable(2)] object value)
		{
			try
			{
				ReflectionUtils.SetMemberValue(this._memberInfo, target, value);
			}
			catch (Exception innerException)
			{
				throw new JsonSerializationException("Error setting value to '{0}' on '{1}'.".FormatWith(CultureInfo.InvariantCulture, this._memberInfo.Name, target.GetType()), innerException);
			}
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <param name="target">The target to get the value from.</param>
		/// <returns>The value.</returns>
		// Token: 0x06000841 RID: 2113 RVA: 0x0002423C File Offset: 0x0002243C
		[return: Nullable(2)]
		public object GetValue(object target)
		{
			object memberValue;
			try
			{
				PropertyInfo propertyInfo = this._memberInfo as PropertyInfo;
				if (propertyInfo != null && propertyInfo.PropertyType.IsByRef)
				{
					throw new InvalidOperationException("Could not create getter for {0}. ByRef return values are not supported.".FormatWith(CultureInfo.InvariantCulture, propertyInfo));
				}
				memberValue = ReflectionUtils.GetMemberValue(this._memberInfo, target);
			}
			catch (Exception innerException)
			{
				throw new JsonSerializationException("Error getting value from '{0}' on '{1}'.".FormatWith(CultureInfo.InvariantCulture, this._memberInfo.Name, target.GetType()), innerException);
			}
			return memberValue;
		}

		// Token: 0x040002E4 RID: 740
		private readonly MemberInfo _memberInfo;
	}
}
