using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct FormationDeploymentOrder
{
	public class DeploymentOrderComparer : IComparer<FormationDeploymentOrder>
	{
		public int Compare(FormationDeploymentOrder a, FormationDeploymentOrder b)
		{
			return a.Key - b.Key;
		}
	}

	private static DeploymentOrderComparer _comparer;

	public int Key { get; private set; }

	public int Offset { get; private set; }

	private FormationDeploymentOrder(FormationClass formationClass, int offset = 0)
	{
		int formationClassPriority = GetFormationClassPriority(formationClass);
		Offset = MathF.Max(0, offset);
		Key = formationClassPriority + Offset * 11;
	}

	public static FormationDeploymentOrder GetDeploymentOrder(FormationClass fClass, int offset = 0)
	{
		return new FormationDeploymentOrder(fClass, offset);
	}

	public static DeploymentOrderComparer GetComparer()
	{
		return _comparer ?? (_comparer = new DeploymentOrderComparer());
	}

	private static int GetFormationClassPriority(FormationClass fClass)
	{
		return fClass switch
		{
			FormationClass.NumberOfDefaultFormations => 0, 
			FormationClass.HeavyInfantry => 1, 
			FormationClass.Infantry => 2, 
			FormationClass.HeavyCavalry => 3, 
			FormationClass.Cavalry => 4, 
			FormationClass.Ranged => 5, 
			FormationClass.HorseArcher => 6, 
			FormationClass.LightCavalry => 7, 
			FormationClass.Bodyguard => 8, 
			FormationClass.NumberOfRegularFormations => 9, 
			_ => 10, 
		};
	}
}
