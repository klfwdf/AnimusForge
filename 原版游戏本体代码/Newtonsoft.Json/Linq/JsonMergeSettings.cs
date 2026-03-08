using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies the settings used when merging JSON.
	/// </summary>
	// Token: 0x020000C5 RID: 197
	public class JsonMergeSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JsonMergeSettings" /> class.
		/// </summary>
		// Token: 0x06000ACA RID: 2762 RVA: 0x0002B345 File Offset: 0x00029545
		public JsonMergeSettings()
		{
			this._propertyNameComparison = StringComparison.Ordinal;
		}

		/// <summary>
		/// Gets or sets the method used when merging JSON arrays.
		/// </summary>
		/// <value>The method used when merging JSON arrays.</value>
		// Token: 0x170001F0 RID: 496
		// (get) Token: 0x06000ACB RID: 2763 RVA: 0x0002B354 File Offset: 0x00029554
		// (set) Token: 0x06000ACC RID: 2764 RVA: 0x0002B35C File Offset: 0x0002955C
		public MergeArrayHandling MergeArrayHandling
		{
			get
			{
				return this._mergeArrayHandling;
			}
			set
			{
				if (value < MergeArrayHandling.Concat || value > MergeArrayHandling.Merge)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._mergeArrayHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how null value properties are merged.
		/// </summary>
		/// <value>How null value properties are merged.</value>
		// Token: 0x170001F1 RID: 497
		// (get) Token: 0x06000ACD RID: 2765 RVA: 0x0002B378 File Offset: 0x00029578
		// (set) Token: 0x06000ACE RID: 2766 RVA: 0x0002B380 File Offset: 0x00029580
		public MergeNullValueHandling MergeNullValueHandling
		{
			get
			{
				return this._mergeNullValueHandling;
			}
			set
			{
				if (value < MergeNullValueHandling.Ignore || value > MergeNullValueHandling.Merge)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._mergeNullValueHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets the comparison used to match property names while merging.
		/// The exact property name will be searched for first and if no matching property is found then
		/// the <see cref="T:System.StringComparison" /> will be used to match a property.
		/// </summary>
		/// <value>The comparison used to match property names while merging.</value>
		// Token: 0x170001F2 RID: 498
		// (get) Token: 0x06000ACF RID: 2767 RVA: 0x0002B39C File Offset: 0x0002959C
		// (set) Token: 0x06000AD0 RID: 2768 RVA: 0x0002B3A4 File Offset: 0x000295A4
		public StringComparison PropertyNameComparison
		{
			get
			{
				return this._propertyNameComparison;
			}
			set
			{
				if (value < StringComparison.CurrentCulture || value > StringComparison.OrdinalIgnoreCase)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._propertyNameComparison = value;
			}
		}

		// Token: 0x0400038F RID: 911
		private MergeArrayHandling _mergeArrayHandling;

		// Token: 0x04000390 RID: 912
		private MergeNullValueHandling _mergeNullValueHandling;

		// Token: 0x04000391 RID: 913
		private StringComparison _propertyNameComparison;
	}
}
