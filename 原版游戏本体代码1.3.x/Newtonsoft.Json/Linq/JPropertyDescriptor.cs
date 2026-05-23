using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a view of a <see cref="T:Newtonsoft.Json.Linq.JProperty" />.
	/// </summary>
	// Token: 0x020000C0 RID: 192
	[NullableContext(1)]
	[Nullable(0)]
	public class JPropertyDescriptor : PropertyDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JPropertyDescriptor" /> class.
		/// </summary>
		/// <param name="name">The name.</param>
		// Token: 0x06000A9A RID: 2714 RVA: 0x0002AD9B File Offset: 0x00028F9B
		public JPropertyDescriptor(string name)
			: base(name, null)
		{
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x0002ADA5 File Offset: 0x00028FA5
		private static JObject CastInstance(object instance)
		{
			return (JObject)instance;
		}

		/// <summary>
		/// When overridden in a derived class, returns whether resetting an object changes its value.
		/// </summary>
		/// <returns>
		/// <c>true</c> if resetting the component changes its value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="component">The component to test for reset capability.</param>
		// Token: 0x06000A9C RID: 2716 RVA: 0x0002ADAD File Offset: 0x00028FAD
		public override bool CanResetValue(object component)
		{
			return false;
		}

		/// <summary>
		/// When overridden in a derived class, gets the current value of the property on a component.
		/// </summary>
		/// <returns>
		/// The value of a property for a given component.
		/// </returns>
		/// <param name="component">The component with the property for which to retrieve the value.</param>
		// Token: 0x06000A9D RID: 2717 RVA: 0x0002ADB0 File Offset: 0x00028FB0
		[NullableContext(2)]
		public override object GetValue(object component)
		{
			JObject jobject = component as JObject;
			if (jobject == null)
			{
				return null;
			}
			return jobject[this.Name];
		}

		/// <summary>
		/// When overridden in a derived class, resets the value for this property of the component to the default value.
		/// </summary>
		/// <param name="component">The component with the property value that is to be reset to the default value.</param>
		// Token: 0x06000A9E RID: 2718 RVA: 0x0002ADC9 File Offset: 0x00028FC9
		public override void ResetValue(object component)
		{
		}

		/// <summary>
		/// When overridden in a derived class, sets the value of the component to a different value.
		/// </summary>
		/// <param name="component">The component with the property value that is to be set.</param>
		/// <param name="value">The new value.</param>
		// Token: 0x06000A9F RID: 2719 RVA: 0x0002ADCC File Offset: 0x00028FCC
		[NullableContext(2)]
		public override void SetValue(object component, object value)
		{
			JObject jobject = component as JObject;
			if (jobject != null)
			{
				JToken value2 = (value as JToken) ?? new JValue(value);
				jobject[this.Name] = value2;
			}
		}

		/// <summary>
		/// When overridden in a derived class, determines a value indicating whether the value of this property needs to be persisted.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the property should be persisted; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="component">The component with the property to be examined for persistence.</param>
		// Token: 0x06000AA0 RID: 2720 RVA: 0x0002AE01 File Offset: 0x00029001
		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}

		/// <summary>
		/// When overridden in a derived class, gets the type of the component this property is bound to.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Type" /> that represents the type of component this property is bound to.
		/// When the <see cref="M:System.ComponentModel.PropertyDescriptor.GetValue(System.Object)" /> or
		/// <see cref="M:System.ComponentModel.PropertyDescriptor.SetValue(System.Object,System.Object)" />
		/// methods are invoked, the object specified might be an instance of this type.
		/// </returns>
		// Token: 0x170001E5 RID: 485
		// (get) Token: 0x06000AA1 RID: 2721 RVA: 0x0002AE04 File Offset: 0x00029004
		public override Type ComponentType
		{
			get
			{
				return typeof(JObject);
			}
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether this property is read-only.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the property is read-only; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x170001E6 RID: 486
		// (get) Token: 0x06000AA2 RID: 2722 RVA: 0x0002AE10 File Offset: 0x00029010
		public override bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// When overridden in a derived class, gets the type of the property.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Type" /> that represents the type of the property.
		/// </returns>
		// Token: 0x170001E7 RID: 487
		// (get) Token: 0x06000AA3 RID: 2723 RVA: 0x0002AE13 File Offset: 0x00029013
		public override Type PropertyType
		{
			get
			{
				return typeof(object);
			}
		}

		/// <summary>
		/// Gets the hash code for the name of the member.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The hash code for the name of the member.
		/// </returns>
		// Token: 0x170001E8 RID: 488
		// (get) Token: 0x06000AA4 RID: 2724 RVA: 0x0002AE1F File Offset: 0x0002901F
		protected override int NameHashCode
		{
			get
			{
				return base.NameHashCode;
			}
		}
	}
}
