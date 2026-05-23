using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Get and set values for a <see cref="T:System.Reflection.MemberInfo" /> using dynamic methods.
	/// </summary>
	// Token: 0x0200007D RID: 125
	[NullableContext(1)]
	[Nullable(0)]
	public class ExpressionValueProvider : IValueProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.ExpressionValueProvider" /> class.
		/// </summary>
		/// <param name="memberInfo">The member info.</param>
		// Token: 0x0600067F RID: 1663 RVA: 0x0001C0F1 File Offset: 0x0001A2F1
		public ExpressionValueProvider(MemberInfo memberInfo)
		{
			ValidationUtils.ArgumentNotNull(memberInfo, "memberInfo");
			this._memberInfo = memberInfo;
		}

		/// <summary>
		/// Sets the value.
		/// </summary>
		/// <param name="target">The target to set the value on.</param>
		/// <param name="value">The value to set on the target.</param>
		// Token: 0x06000680 RID: 1664 RVA: 0x0001C10C File Offset: 0x0001A30C
		public void SetValue(object target, [Nullable(2)] object value)
		{
			try
			{
				if (this._setter == null)
				{
					this._setter = ExpressionReflectionDelegateFactory.Instance.CreateSet<object>(this._memberInfo);
				}
				this._setter(target, value);
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
		// Token: 0x06000681 RID: 1665 RVA: 0x0001C180 File Offset: 0x0001A380
		[return: Nullable(2)]
		public object GetValue(object target)
		{
			object result;
			try
			{
				if (this._getter == null)
				{
					this._getter = ExpressionReflectionDelegateFactory.Instance.CreateGet<object>(this._memberInfo);
				}
				result = this._getter(target);
			}
			catch (Exception innerException)
			{
				throw new JsonSerializationException("Error getting value from '{0}' on '{1}'.".FormatWith(CultureInfo.InvariantCulture, this._memberInfo.Name, target.GetType()), innerException);
			}
			return result;
		}

		// Token: 0x04000249 RID: 585
		private readonly MemberInfo _memberInfo;

		// Token: 0x0400024A RID: 586
		[Nullable(new byte[] { 2, 1, 2 })]
		private Func<object, object> _getter;

		// Token: 0x0400024B RID: 587
		[Nullable(new byte[] { 2, 1, 2 })]
		private Action<object, object> _setter;
	}
}
