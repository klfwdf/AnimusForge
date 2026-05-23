using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000023 RID: 35
	[ApplicationInterfaceBase]
	internal interface IBodyPart
	{
		// Token: 0x06000201 RID: 513
		[EngineMethod("do_segments_intersect", false, null, false)]
		bool DoSegmentsIntersect(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint);
	}
}
