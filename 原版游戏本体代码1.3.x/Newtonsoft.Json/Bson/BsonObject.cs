using System;
using System.Collections;
using System.Collections.Generic;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x0200010A RID: 266
	internal class BsonObject : BsonToken, IEnumerable<BsonProperty>, IEnumerable
	{
		// Token: 0x06000DA3 RID: 3491 RVA: 0x00037010 File Offset: 0x00035210
		public void Add(string name, BsonToken token)
		{
			this._children.Add(new BsonProperty
			{
				Name = new BsonString(name, false),
				Value = token
			});
			token.Parent = this;
		}

		// Token: 0x1700027A RID: 634
		// (get) Token: 0x06000DA4 RID: 3492 RVA: 0x0003703D File Offset: 0x0003523D
		public override BsonType Type
		{
			get
			{
				return BsonType.Object;
			}
		}

		// Token: 0x06000DA5 RID: 3493 RVA: 0x00037040 File Offset: 0x00035240
		public IEnumerator<BsonProperty> GetEnumerator()
		{
			return this._children.GetEnumerator();
		}

		// Token: 0x06000DA6 RID: 3494 RVA: 0x00037052 File Offset: 0x00035252
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0400043C RID: 1084
		private readonly List<BsonProperty> _children = new List<BsonProperty>();
	}
}
