using System;
using System.Collections.Generic;
namespace AnimusForge;

internal readonly struct FieldCampSiteSeed
{
    internal readonly float X, Y, ForwardX, ForwardY;
    internal FieldCampSiteSeed(float x, float y, float fx, float fy) { X=x;Y=y;ForwardX=fx;ForwardY=fy; }
}
internal delegate bool FieldCampSiteSample(float x, float y, bool detailed, out float height);

// One bounded initialization search. Rank the ENTIRE footprint, never a single safe player point.
internal static class FieldCampSitePlanner
{
    internal const float MaxHeightSpread = 1.5f;
    internal const float MaxGrade = 0.08f;
    internal const int MaxSeeds = 1200;
    internal const int DetailedFinalists = 32;
    private struct Scored { internal FieldCampSiteSeed Seed; internal float Score; }
    internal static bool TryChoose(IReadOnlyList<FieldCampSiteSeed> seeds, float halfX, float back, float front,
        FieldCampSiteSample sample, Func<FieldCampSiteSeed,bool> critical, out FieldCampSiteSeed chosen, out float spread)
    {
        chosen=default;spread=0f;
        if (seeds==null || sample==null || critical==null || !(halfX>0f) || !(front>back)) return false;
        var shortlist=new List<Scored>(DetailedFinalists);
        var heights=new float[81];
        for(int i=0;i<Math.Min(MaxSeeds,seeds.Count);i++)
            for(int turn=0;turn<2;turn++)
            {
                var input=seeds[i];
                float length=(float)Math.Sqrt(input.ForwardX*input.ForwardX+input.ForwardY*input.ForwardY);
                if (!(length>0.001f) || float.IsInfinity(length)) continue;
                var seed=turn==0 ? new FieldCampSiteSeed(input.X,input.Y,input.ForwardX/length,input.ForwardY/length)
                    : new FieldCampSiteSeed(input.X,input.Y,-input.ForwardY/length,input.ForwardX/length);
                if(!Measure(seed,halfX,back,front,5,false,sample,heights,out float heightSpread,out float grade))continue;
                float score=heightSpread*1000f+grade*100f;
                int at=0;while(at<shortlist.Count && shortlist[at].Score<=score)at++;
                if(at>=DetailedFinalists)continue;
                shortlist.Insert(at,new Scored {Seed=seed,Score=score});
                if(shortlist.Count>DetailedFinalists)shortlist.RemoveAt(DetailedFinalists);
            }
        foreach(var candidate in shortlist)
            if(Measure(candidate.Seed,halfX,back,front,9,true,sample,heights,out float fineSpread,out _) && critical(candidate.Seed))
            {chosen=candidate.Seed;spread=fineSpread;return true;}
        return false;
    }
    private static bool Measure(FieldCampSiteSeed seed,float halfX,float back,float front,int grid,bool detailed,
        FieldCampSiteSample sample,float[] heights,out float spread,out float grade)
    {
        spread=grade=0f;float low=float.MaxValue,high=float.MinValue;
        float stepX=2f*halfX/(grid-1),stepY=(front-back)/(grid-1);
        for(int x=0;x<grid;x++)for(int y=0;y<grid;y++)
        {
            float lx=-halfX+x*stepX,ly=back+y*stepY;
            if(!sample(seed.X+seed.ForwardY*lx+seed.ForwardX*ly,
                seed.Y-seed.ForwardX*lx+seed.ForwardY*ly,detailed,out float z) || float.IsNaN(z)||float.IsInfinity(z))return false;
            int index=x*grid+y;heights[index]=z;low=Math.Min(low,z);high=Math.Max(high,z);
            if(x>0)grade=Math.Max(grade,Math.Abs(z-heights[index-grid])/stepX);
            if(y>0)grade=Math.Max(grade,Math.Abs(z-heights[index-1])/stepY);
            spread=high-low;
            if(spread>MaxHeightSpread || grade>MaxGrade)return false;
        }
        return true;
    }
}
