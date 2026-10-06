namespace TaleWorlds.CampaignSystem;

public delegate void ReferenceAction<T1>(ref T1 t1);
public delegate void ReferenceAction<T1, T2>(T1 t1, ref T2 t2);
public delegate void ReferenceAction<T1, T2, T3>(T1 t1, T2 t2, ref T3 t3);
