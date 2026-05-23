using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace TaleWorlds.Library
{
	// Token: 0x02000073 RID: 115
	public class NavigationPath : ISerializable
	{
		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000417 RID: 1047 RVA: 0x0000E756 File Offset: 0x0000C956
		// (set) Token: 0x06000418 RID: 1048 RVA: 0x0000E75E File Offset: 0x0000C95E
		public Vec2[] PathPoints { get; private set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000419 RID: 1049 RVA: 0x0000E767 File Offset: 0x0000C967
		// (set) Token: 0x0600041A RID: 1050 RVA: 0x0000E76F File Offset: 0x0000C96F
		[CachedData]
		public int Size { get; set; }

		// Token: 0x0600041B RID: 1051 RVA: 0x0000E778 File Offset: 0x0000C978
		public NavigationPath()
		{
			this.PathPoints = new Vec2[128];
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x0000E790 File Offset: 0x0000C990
		protected NavigationPath(SerializationInfo info, StreamingContext context)
		{
			this.PathPoints = new Vec2[128];
			this.Size = info.GetInt32("s");
			for (int i = 0; i < this.Size; i++)
			{
				float single = info.GetSingle("x" + i);
				float single2 = info.GetSingle("y" + i);
				this.PathPoints[i] = new Vec2(single, single2);
			}
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x0000E818 File Offset: 0x0000CA18
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("s", this.Size);
			for (int i = 0; i < this.Size; i++)
			{
				info.AddValue("x" + i, this.PathPoints[i].x);
				info.AddValue("y" + i, this.PathPoints[i].y);
			}
		}

		// Token: 0x17000067 RID: 103
		public Vec2 this[int i]
		{
			get
			{
				return this.PathPoints[i];
			}
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x0000E8A3 File Offset: 0x0000CAA3
		public void OverridePathPointAtIndex(int index, in Vec2 newValue)
		{
			this.PathPoints[index] = newValue;
		}

		// Token: 0x04000146 RID: 326
		private const int PathSize = 128;
	}
}
