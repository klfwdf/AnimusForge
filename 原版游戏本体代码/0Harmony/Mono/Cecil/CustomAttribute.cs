using System;
using System.Diagnostics;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200022E RID: 558
	[DebuggerDisplay("{AttributeType}")]
	internal sealed class CustomAttribute : ICustomAttribute
	{
		// Token: 0x1700026B RID: 619
		// (get) Token: 0x06000BF0 RID: 3056 RVA: 0x0002A335 File Offset: 0x00028535
		// (set) Token: 0x06000BF1 RID: 3057 RVA: 0x0002A33D File Offset: 0x0002853D
		public MethodReference Constructor
		{
			get
			{
				return this.constructor;
			}
			set
			{
				this.constructor = value;
			}
		}

		// Token: 0x1700026C RID: 620
		// (get) Token: 0x06000BF2 RID: 3058 RVA: 0x0002A346 File Offset: 0x00028546
		public TypeReference AttributeType
		{
			get
			{
				return this.constructor.DeclaringType;
			}
		}

		// Token: 0x1700026D RID: 621
		// (get) Token: 0x06000BF3 RID: 3059 RVA: 0x0002A353 File Offset: 0x00028553
		public bool IsResolved
		{
			get
			{
				return this.resolved;
			}
		}

		// Token: 0x1700026E RID: 622
		// (get) Token: 0x06000BF4 RID: 3060 RVA: 0x0002A35B File Offset: 0x0002855B
		public bool HasConstructorArguments
		{
			get
			{
				this.Resolve();
				return !this.arguments.IsNullOrEmpty<CustomAttributeArgument>();
			}
		}

		// Token: 0x1700026F RID: 623
		// (get) Token: 0x06000BF5 RID: 3061 RVA: 0x0002A371 File Offset: 0x00028571
		public Collection<CustomAttributeArgument> ConstructorArguments
		{
			get
			{
				this.Resolve();
				if (this.arguments == null)
				{
					Interlocked.CompareExchange<Collection<CustomAttributeArgument>>(ref this.arguments, new Collection<CustomAttributeArgument>(), null);
				}
				return this.arguments;
			}
		}

		// Token: 0x17000270 RID: 624
		// (get) Token: 0x06000BF6 RID: 3062 RVA: 0x0002A399 File Offset: 0x00028599
		public bool HasFields
		{
			get
			{
				this.Resolve();
				return !this.fields.IsNullOrEmpty<CustomAttributeNamedArgument>();
			}
		}

		// Token: 0x17000271 RID: 625
		// (get) Token: 0x06000BF7 RID: 3063 RVA: 0x0002A3AF File Offset: 0x000285AF
		public Collection<CustomAttributeNamedArgument> Fields
		{
			get
			{
				this.Resolve();
				if (this.fields == null)
				{
					Interlocked.CompareExchange<Collection<CustomAttributeNamedArgument>>(ref this.fields, new Collection<CustomAttributeNamedArgument>(), null);
				}
				return this.fields;
			}
		}

		// Token: 0x17000272 RID: 626
		// (get) Token: 0x06000BF8 RID: 3064 RVA: 0x0002A3D7 File Offset: 0x000285D7
		public bool HasProperties
		{
			get
			{
				this.Resolve();
				return !this.properties.IsNullOrEmpty<CustomAttributeNamedArgument>();
			}
		}

		// Token: 0x17000273 RID: 627
		// (get) Token: 0x06000BF9 RID: 3065 RVA: 0x0002A3ED File Offset: 0x000285ED
		public Collection<CustomAttributeNamedArgument> Properties
		{
			get
			{
				this.Resolve();
				if (this.properties == null)
				{
					Interlocked.CompareExchange<Collection<CustomAttributeNamedArgument>>(ref this.properties, new Collection<CustomAttributeNamedArgument>(), null);
				}
				return this.properties;
			}
		}

		// Token: 0x17000274 RID: 628
		// (get) Token: 0x06000BFA RID: 3066 RVA: 0x0002A415 File Offset: 0x00028615
		internal bool HasImage
		{
			get
			{
				return this.constructor != null && this.constructor.HasImage;
			}
		}

		// Token: 0x17000275 RID: 629
		// (get) Token: 0x06000BFB RID: 3067 RVA: 0x0002A42C File Offset: 0x0002862C
		internal ModuleDefinition Module
		{
			get
			{
				return this.constructor.Module;
			}
		}

		// Token: 0x06000BFC RID: 3068 RVA: 0x0002A439 File Offset: 0x00028639
		internal CustomAttribute(uint signature, MethodReference constructor)
		{
			this.signature = signature;
			this.constructor = constructor;
			this.resolved = false;
		}

		// Token: 0x06000BFD RID: 3069 RVA: 0x0002A456 File Offset: 0x00028656
		public CustomAttribute(MethodReference constructor)
		{
			this.constructor = constructor;
			this.resolved = true;
		}

		// Token: 0x06000BFE RID: 3070 RVA: 0x0002A46C File Offset: 0x0002866C
		public CustomAttribute(MethodReference constructor, byte[] blob)
		{
			this.constructor = constructor;
			this.resolved = false;
			this.blob = blob;
		}

		// Token: 0x06000BFF RID: 3071 RVA: 0x0002A48C File Offset: 0x0002868C
		public byte[] GetBlob()
		{
			if (this.blob != null)
			{
				return this.blob;
			}
			if (!this.HasImage)
			{
				throw new NotSupportedException();
			}
			return this.Module.Read<CustomAttribute, byte[]>(ref this.blob, this, (CustomAttribute attribute, MetadataReader reader) => reader.ReadCustomAttributeBlob(attribute.signature));
		}

		// Token: 0x06000C00 RID: 3072 RVA: 0x0002A4E8 File Offset: 0x000286E8
		private void Resolve()
		{
			if (this.resolved || !this.HasImage)
			{
				return;
			}
			object syncRoot = this.Module.SyncRoot;
			lock (syncRoot)
			{
				if (!this.resolved)
				{
					this.Module.Read<CustomAttribute>(this, delegate(CustomAttribute attribute, MetadataReader reader)
					{
						try
						{
							reader.ReadCustomAttributeSignature(attribute);
							this.resolved = true;
						}
						catch (ResolutionException)
						{
							if (this.arguments != null)
							{
								this.arguments.Clear();
							}
							if (this.fields != null)
							{
								this.fields.Clear();
							}
							if (this.properties != null)
							{
								this.properties.Clear();
							}
							this.resolved = false;
						}
					});
				}
			}
		}

		// Token: 0x04000394 RID: 916
		internal CustomAttributeValueProjection projection;

		// Token: 0x04000395 RID: 917
		internal readonly uint signature;

		// Token: 0x04000396 RID: 918
		internal bool resolved;

		// Token: 0x04000397 RID: 919
		private MethodReference constructor;

		// Token: 0x04000398 RID: 920
		private byte[] blob;

		// Token: 0x04000399 RID: 921
		internal Collection<CustomAttributeArgument> arguments;

		// Token: 0x0400039A RID: 922
		internal Collection<CustomAttributeNamedArgument> fields;

		// Token: 0x0400039B RID: 923
		internal Collection<CustomAttributeNamedArgument> properties;
	}
}
