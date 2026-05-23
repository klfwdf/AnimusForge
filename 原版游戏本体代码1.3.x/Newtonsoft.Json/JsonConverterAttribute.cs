using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Instructs the <see cref="T:Newtonsoft.Json.JsonSerializer" /> to use the specified <see cref="T:Newtonsoft.Json.JsonConverter" /> when serializing the member or class.
	/// </summary>
	// Token: 0x0200001C RID: 28
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class JsonConverterAttribute : Attribute
	{
		/// <summary>
		/// Gets the <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <value>The <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.JsonConverter" />.</value>
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600008D RID: 141 RVA: 0x00002F59 File Offset: 0x00001159
		public Type ConverterType
		{
			get
			{
				return this._converterType;
			}
		}

		/// <summary>
		/// The parameter list to use when constructing the <see cref="T:Newtonsoft.Json.JsonConverter" /> described by <see cref="P:Newtonsoft.Json.JsonConverterAttribute.ConverterType" />.
		/// If <c>null</c>, the default constructor is used.
		/// </summary>
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00002F61 File Offset: 0x00001161
		[Nullable(new byte[] { 2, 1 })]
		public object[] ConverterParameters
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonConverterAttribute" /> class.
		/// </summary>
		/// <param name="converterType">Type of the <see cref="T:Newtonsoft.Json.JsonConverter" />.</param>
		// Token: 0x0600008F RID: 143 RVA: 0x00002F69 File Offset: 0x00001169
		public JsonConverterAttribute(Type converterType)
		{
			if (converterType == null)
			{
				throw new ArgumentNullException("converterType");
			}
			this._converterType = converterType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonConverterAttribute" /> class.
		/// </summary>
		/// <param name="converterType">Type of the <see cref="T:Newtonsoft.Json.JsonConverter" />.</param>
		/// <param name="converterParameters">Parameter list to use when constructing the <see cref="T:Newtonsoft.Json.JsonConverter" />. Can be <c>null</c>.</param>
		// Token: 0x06000090 RID: 144 RVA: 0x00002F8C File Offset: 0x0000118C
		public JsonConverterAttribute(Type converterType, params object[] converterParameters)
			: this(converterType)
		{
			this.ConverterParameters = converterParameters;
		}

		// Token: 0x0400003C RID: 60
		private readonly Type _converterType;
	}
}
