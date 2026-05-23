using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class Crafting
{
	public class RefiningFormula
	{
		public CraftingMaterials Output { get; }

		public int OutputCount { get; }

		public CraftingMaterials Output2 { get; }

		public int Output2Count { get; }

		public CraftingMaterials Input1 { get; }

		public int Input1Count { get; }

		public CraftingMaterials Input2 { get; }

		public int Input2Count { get; }

		public RefiningFormula(CraftingMaterials input1, int input1Count, CraftingMaterials input2, int input2Count, CraftingMaterials output, int outputCount = 1, CraftingMaterials output2 = CraftingMaterials.IronOre, int output2Count = 0)
		{
			Output = output;
			OutputCount = outputCount;
			Output2 = output2;
			Output2Count = output2Count;
			Input1 = input1;
			Input1Count = input1Count;
			Input2 = input2;
			Input2Count = input2Count;
		}
	}

	private static class CraftedItemGenerationHelper
	{
		private struct CraftingStats
		{
			private WeaponDesign _craftedData;

			private WeaponDescription _weaponDescription;

			private float _stoppingTorque;

			private float _armInertia;

			private float _swingDamageFactor;

			private float _thrustDamageFactor;

			private float _currentWeaponWeight;

			private float _currentWeaponReach;

			private float _currentWeaponSweetSpot;

			private float _currentWeaponCenterOfMass;

			private float _currentWeaponInertia;

			private float _currentWeaponInertiaAroundShoulder;

			private float _currentWeaponInertiaAroundGrip;

			private float _currentWeaponSwingSpeed;

			private float _currentWeaponThrustSpeed;

			private float _currentWeaponHandling;

			private float _currentWeaponSwingDamage;

			private float _currentWeaponThrustDamage;

			private WeaponComponentData.WeaponTiers _currentWeaponTier;

			public static void FillWeapon(ItemObject item, WeaponDescription weaponDescription, WeaponFlags weaponFlags, bool isAlternative, out WeaponComponentData filledWeapon)
			{
				filledWeapon = new WeaponComponentData(item, weaponDescription.WeaponClass, weaponFlags);
				CraftingStats craftingStats = new CraftingStats
				{
					_craftedData = item.WeaponDesign,
					_weaponDescription = weaponDescription
				};
				craftingStats.CalculateStats();
				craftingStats.SetWeaponData(filledWeapon, isAlternative);
			}

			private void CalculateStats()
			{
				WeaponDesign craftedData = _craftedData;
				_stoppingTorque = 10f;
				_armInertia = 2.9f;
				if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
				{
					_stoppingTorque *= 1.5f;
					_armInertia *= 1.4f;
				}
				if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip))
				{
					_stoppingTorque *= 1.5f;
					_armInertia *= 1.4f;
				}
				_currentWeaponWeight = 0f;
				_currentWeaponReach = 0f;
				_currentWeaponCenterOfMass = 0f;
				_currentWeaponInertia = 0f;
				_currentWeaponInertiaAroundShoulder = 0f;
				_currentWeaponInertiaAroundGrip = 0f;
				_currentWeaponSwingSpeed = 1f;
				_currentWeaponThrustSpeed = 1f;
				_currentWeaponSwingDamage = 0f;
				_currentWeaponThrustDamage = 0f;
				_currentWeaponHandling = 1f;
				_currentWeaponTier = WeaponComponentData.WeaponTiers.Tier1;
				_currentWeaponWeight = TaleWorlds.Library.MathF.Round(craftedData.UsedPieces.Sum((WeaponDesignElement selectedUsablePiece) => selectedUsablePiece.ScaledWeight), 2);
				_currentWeaponReach = TaleWorlds.Library.MathF.Round(_craftedData.CraftedWeaponLength, 2);
				_currentWeaponCenterOfMass = CalculateCenterOfMass();
				_currentWeaponInertia = CalculateWeaponInertia();
				_currentWeaponInertiaAroundShoulder = ParallelAxis(_currentWeaponInertia, _currentWeaponWeight, 0.5f + _currentWeaponCenterOfMass);
				_currentWeaponInertiaAroundGrip = ParallelAxis(_currentWeaponInertia, _currentWeaponWeight, _currentWeaponCenterOfMass);
				_currentWeaponSwingSpeed = CalculateSwingSpeed();
				_currentWeaponThrustSpeed = CalculateThrustSpeed();
				_currentWeaponHandling = CalculateAgility();
				_currentWeaponTier = CalculateWeaponTier();
				_swingDamageFactor = _craftedData.UsedPieces[0].CraftingPiece.BladeData.SwingDamageFactor;
				_thrustDamageFactor = _craftedData.UsedPieces[0].CraftingPiece.BladeData.ThrustDamageFactor;
				if (_weaponDescription.WeaponClass == WeaponClass.ThrowingAxe || _weaponDescription.WeaponClass == WeaponClass.ThrowingKnife || _weaponDescription.WeaponClass == WeaponClass.Javelin)
				{
					_currentWeaponSwingDamage = 0f;
					CalculateMissileDamage(out _currentWeaponThrustDamage);
				}
				else
				{
					CalculateSwingBaseDamage(out _currentWeaponSwingDamage);
					CalculateThrustBaseDamage(out _currentWeaponThrustDamage);
				}
				_currentWeaponSweetSpot = CalculateSweetSpot();
			}

			private void SetWeaponData(WeaponComponentData weapon, bool isAlternative)
			{
				BladeData bladeData = _craftedData.UsedPieces[0].CraftingPiece.BladeData;
				short maxDataValue = 0;
				string passBySoundCode = "";
				int accuracy = 0;
				int missileSpeed = 0;
				MatrixFrame stickingFrame = MatrixFrame.Identity;
				short reloadPhaseCount = 1;
				if (_weaponDescription.WeaponClass == WeaponClass.Javelin || _weaponDescription.WeaponClass == WeaponClass.ThrowingAxe || _weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
				{
					short num = (short)(isAlternative ? 1 : bladeData.StackAmount);
					switch (_weaponDescription.WeaponClass)
					{
					case WeaponClass.Javelin:
						maxDataValue = num;
						accuracy = 92;
						passBySoundCode = "event:/mission/combat/missile/passby";
						break;
					case WeaponClass.ThrowingAxe:
						maxDataValue = num;
						accuracy = 93;
						passBySoundCode = "event:/mission/combat/throwing/passby";
						break;
					case WeaponClass.ThrowingKnife:
						maxDataValue = num;
						accuracy = 95;
						passBySoundCode = "event:/mission/combat/throwing/passby";
						break;
					}
					missileSpeed = TaleWorlds.Library.MathF.Floor(CalculateMissileSpeed());
					Mat3 rot = Mat3.Identity;
					switch (_weaponDescription.WeaponClass)
					{
					case WeaponClass.Javelin:
						rot.RotateAboutSide(System.MathF.PI / 2f);
						stickingFrame = new MatrixFrame(in rot, -Vec3.Up * _currentWeaponReach);
						break;
					case WeaponClass.ThrowingAxe:
					{
						float bladeWidth = _craftedData.UsedPieces[0].CraftingPiece.BladeData.BladeWidth;
						float num2 = _craftedData.PiecePivotDistances[0];
						float scaledDistanceToNextPiece = _craftedData.UsedPieces[0].ScaledDistanceToNextPiece;
						rot.RotateAboutUp(System.MathF.PI / 2f);
						rot.RotateAboutSide((0f - (15f + scaledDistanceToNextPiece * 3f / num2 * 25f)) * (System.MathF.PI / 180f));
						stickingFrame = new MatrixFrame(in rot, -rot.u * (num2 + scaledDistanceToNextPiece * 0.6f) - rot.f * bladeWidth * 0.8f);
						break;
					}
					case WeaponClass.ThrowingKnife:
						rot.RotateAboutForward(-System.MathF.PI / 2f);
						stickingFrame = new MatrixFrame(in rot, Vec3.Side * _currentWeaponReach);
						break;
					}
				}
				if (_weaponDescription.WeaponClass == WeaponClass.Arrow || _weaponDescription.WeaponClass == WeaponClass.Bolt)
				{
					stickingFrame.rotation.RotateAboutSide(System.MathF.PI / 2f);
				}
				Vec3 rotationSpeed = Vec3.Zero;
				if (_weaponDescription.WeaponClass == WeaponClass.ThrowingAxe)
				{
					rotationSpeed = new Vec3(0f, 18f);
				}
				else if (_weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
				{
					rotationSpeed = new Vec3(0f, 24f);
				}
				weapon.Init(_weaponDescription.StringId, bladeData.PhysicsMaterial, GetItemUsage(), bladeData.ThrustDamageType, bladeData.SwingDamageType, GetWeaponHandArmorBonus(), (int)(_currentWeaponReach * 100f), TaleWorlds.Library.MathF.Round(GetWeaponBalance(), 2), _currentWeaponInertia, _currentWeaponCenterOfMass, TaleWorlds.Library.MathF.Floor(_currentWeaponHandling), TaleWorlds.Library.MathF.Round(_swingDamageFactor, 2), TaleWorlds.Library.MathF.Round(_thrustDamageFactor, 2), maxDataValue, passBySoundCode, accuracy, missileSpeed, stickingFrame, GetAmmoClass(), _currentWeaponSweetSpot, TaleWorlds.Library.MathF.Floor(_currentWeaponSwingSpeed * 4.5454545f), TaleWorlds.Library.MathF.Round(_currentWeaponSwingDamage), TaleWorlds.Library.MathF.Floor(_currentWeaponThrustSpeed * 11.764706f), TaleWorlds.Library.MathF.Round(_currentWeaponThrustDamage), rotationSpeed, _currentWeaponTier, reloadPhaseCount);
				Mat3 rot2 = Mat3.Identity;
				Vec3 v = Vec3.Zero;
				if (_weaponDescription.RotatedInHand)
				{
					rot2.RotateAboutSide(System.MathF.PI);
				}
				if (_weaponDescription.UseCenterOfMassAsHandBase)
				{
					v = -Vec3.Up * _currentWeaponCenterOfMass;
				}
				weapon.SetFrame(new MatrixFrame(in rot2, rot2.TransformToParent(in v)));
			}

			private float CalculateSweetSpot()
			{
				float num = -1f;
				float result = -1f;
				for (int i = 0; i < 100; i++)
				{
					float num2 = 0.01f * (float)i;
					float num3 = CombatStatCalculator.CalculateStrikeMagnitudeForSwing(_currentWeaponSwingSpeed, num2, _currentWeaponWeight, _currentWeaponReach, _currentWeaponInertia, _currentWeaponCenterOfMass, 0f);
					if (num < num3)
					{
						num = num3;
						result = num2;
					}
				}
				return result;
			}

			private float CalculateCenterOfMass()
			{
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				PieceData[] buildOrders = _craftedData.Template.BuildOrders;
				for (int i = 0; i < buildOrders.Length; i++)
				{
					PieceData pieceData = buildOrders[i];
					CraftingPiece.PieceTypes pieceType = pieceData.PieceType;
					WeaponDesignElement weaponDesignElement = _craftedData.UsedPieces[(int)pieceType];
					if (weaponDesignElement.IsValid)
					{
						float scaledWeight = weaponDesignElement.ScaledWeight;
						float num4 = 0f;
						if (pieceData.Order < 0)
						{
							num4 -= (num3 + (weaponDesignElement.ScaledLength - weaponDesignElement.ScaledCenterOfMass)) * scaledWeight;
							num3 += weaponDesignElement.ScaledLength;
						}
						else
						{
							num4 += (num2 + weaponDesignElement.ScaledCenterOfMass) * scaledWeight;
							num2 += weaponDesignElement.ScaledLength;
						}
						num += num4;
					}
				}
				return num / _currentWeaponWeight - (_craftedData.UsedPieces[2].ScaledDistanceToPreviousPiece - _craftedData.UsedPieces[2].ScaledPieceOffset);
			}

			private float CalculateWeaponInertia()
			{
				float num = 0f - _currentWeaponCenterOfMass;
				float num2 = 0f;
				PieceData[] buildOrders = _craftedData.Template.BuildOrders;
				foreach (PieceData pieceData in buildOrders)
				{
					WeaponDesignElement weaponDesignElement = _craftedData.UsedPieces[(int)pieceData.PieceType];
					if (weaponDesignElement.IsValid)
					{
						float weightMultiplier = 1f;
						num2 += ParallelAxis(weaponDesignElement, num, weightMultiplier);
						num += weaponDesignElement.ScaledLength;
					}
				}
				return num2;
			}

			private float CalculateSwingSpeed()
			{
				double num = 1.0 * (double)_currentWeaponInertiaAroundShoulder + 0.9;
				double num2 = 170.0;
				double num3 = 90.0;
				double num4 = 27.0;
				double num5 = 15.0;
				double num6 = 7.0;
				if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
				{
					if (_weaponDescription.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip))
					{
						num += 1.5;
						num6 *= 4.0;
						num5 *= 1.7;
						num3 *= 1.3;
						num2 *= 1.15;
					}
					else
					{
						num += 1.0;
						num6 *= 2.4;
						num5 *= 1.3;
						num3 *= 1.35;
						num2 *= 1.15;
					}
				}
				num4 = TaleWorlds.Library.MathF.Max(1.0, num4 - num);
				num5 = TaleWorlds.Library.MathF.Max(1.0, num5 - num);
				num6 = TaleWorlds.Library.MathF.Max(1.0, num6 - num);
				SimulateSwingLayer(1.5, 200.0, num4, 2.0 + num, out var _, out var finalTime);
				SimulateSwingLayer(1.5, num2, num5, 1.0 + num, out var _, out var finalTime2);
				SimulateSwingLayer(1.5, num3, num6, 0.5 + num, out var _, out var finalTime3);
				double num7 = 0.33 * (finalTime + finalTime2 + finalTime3);
				return (float)(20.8 / num7);
			}

			private float CalculateThrustSpeed()
			{
				double num = 1.8 + (double)_currentWeaponWeight + (double)_currentWeaponInertiaAroundGrip * 0.2;
				double num2 = 170.0;
				double num3 = 90.0;
				double num4 = 24.0;
				double num5 = 15.0;
				if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand) && !_weaponDescription.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip))
				{
					num += 0.6;
					num5 *= 1.9;
					num4 *= 1.1;
					num3 *= 1.2;
					num2 *= 1.05;
				}
				else if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand | WeaponFlags.WideGrip))
				{
					num += 0.9;
					num5 *= 2.1;
					num4 *= 1.2;
					num3 *= 1.2;
					num2 *= 1.05;
				}
				SimulateThrustLayer(0.6, 250.0, 48.0, 4.0 + num, out var _, out var finalTime);
				SimulateThrustLayer(0.6, num2, num4, 2.0 + num, out var _, out var finalTime2);
				SimulateThrustLayer(0.6, num3, num5, 0.5 + num, out var _, out var finalTime3);
				double num6 = 0.33 * (finalTime + finalTime2 + finalTime3);
				return (float)(3.8500000000000005 / num6);
			}

			private void CalculateSwingBaseDamage(out float damage)
			{
				float num = 0f;
				for (float num2 = 0.93f; num2 > 0.5f; num2 -= 0.05f)
				{
					float num3 = CombatStatCalculator.CalculateBaseBlowMagnitudeForSwing(_currentWeaponSwingSpeed, _currentWeaponReach, _currentWeaponWeight, _currentWeaponInertia, _currentWeaponCenterOfMass, num2, 0f);
					if (num3 > num)
					{
						num = num3;
					}
				}
				damage = num * _swingDamageFactor;
			}

			private void CalculateThrustBaseDamage(out float damage, bool isThrown = false)
			{
				float num = CombatStatCalculator.CalculateStrikeMagnitudeForThrust(_currentWeaponThrustSpeed, _currentWeaponWeight, 0f, isThrown);
				damage = num * _thrustDamageFactor;
			}

			private void CalculateMissileDamage(out float damage)
			{
				switch (_weaponDescription.WeaponClass)
				{
				case WeaponClass.ThrowingAxe:
					CalculateSwingBaseDamage(out damage);
					damage *= 2f;
					break;
				case WeaponClass.ThrowingKnife:
					CalculateThrustBaseDamage(out damage, isThrown: true);
					damage *= 3.3f;
					break;
				case WeaponClass.Javelin:
					CalculateThrustBaseDamage(out damage, isThrown: true);
					damage *= 9f;
					break;
				default:
					damage = 0f;
					Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateMissileDamage", 508);
					break;
				}
			}

			private WeaponComponentData.WeaponTiers CalculateWeaponTier()
			{
				int num = 0;
				int num2 = 0;
				foreach (WeaponDesignElement item in _craftedData.UsedPieces.Where((WeaponDesignElement ucp) => ucp.IsValid))
				{
					num += item.CraftingPiece.PieceTier;
					num2++;
				}
				if (Enum.TryParse<WeaponComponentData.WeaponTiers>(((int)((float)num / (float)num2)).ToString(), out var result))
				{
					return result;
				}
				Debug.FailedAssert("Couldn't calculate weapon tier", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateWeaponTier", 529);
				return WeaponComponentData.WeaponTiers.Tier1;
			}

			private string GetItemUsage()
			{
				List<string> list = _weaponDescription.ItemUsageFeatures.Split(new char[1] { ':' }).ToList();
				foreach (WeaponDesignElement item in _craftedData.UsedPieces.Where((WeaponDesignElement ucp) => ucp.IsValid))
				{
					string[] array = item.CraftingPiece.ItemUsageFeaturesToExclude.Split(new char[1] { ':' });
					foreach (string text in array)
					{
						if (!string.IsNullOrEmpty(text))
						{
							list.Remove(text);
						}
					}
				}
				string text2 = "";
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					text2 += list[num2];
					if (num2 < list.Count - 1)
					{
						text2 += "_";
					}
				}
				return text2;
			}

			private float CalculateMissileSpeed()
			{
				if (_weaponDescription.WeaponClass == WeaponClass.ThrowingAxe)
				{
					return _currentWeaponThrustSpeed * 3.2f;
				}
				if (_weaponDescription.WeaponClass == WeaponClass.ThrowingKnife)
				{
					return _currentWeaponThrustSpeed * 3.9f;
				}
				if (_weaponDescription.WeaponClass == WeaponClass.Javelin)
				{
					return _currentWeaponThrustSpeed * 3.6f;
				}
				Debug.FailedAssert("Weapon is not a missile.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "CalculateMissileSpeed", 580);
				return 10f;
			}

			private int CalculateAgility()
			{
				float currentWeaponInertiaAroundGrip = _currentWeaponInertiaAroundGrip;
				if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.NotUsableWithOneHand))
				{
					currentWeaponInertiaAroundGrip *= 0.5f;
					currentWeaponInertiaAroundGrip += 0.9f;
				}
				else if (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip))
				{
					currentWeaponInertiaAroundGrip *= 0.4f;
					currentWeaponInertiaAroundGrip += 1f;
				}
				else
				{
					currentWeaponInertiaAroundGrip += 0.7f;
				}
				float num = TaleWorlds.Library.MathF.Pow(1f / currentWeaponInertiaAroundGrip, 0.55f);
				num *= 1f;
				return TaleWorlds.Library.MathF.Round(100f * num);
			}

			private float GetWeaponBalance()
			{
				return MBMath.ClampFloat((_currentWeaponSwingSpeed * 4.5454545f - 70f) / 30f, 0f, 1f);
			}

			private int GetWeaponHandArmorBonus()
			{
				return _craftedData.UsedPieces[1]?.CraftingPiece.ArmorBonus ?? 0;
			}

			private WeaponClass GetAmmoClass()
			{
				if (_weaponDescription.WeaponClass != WeaponClass.ThrowingKnife && _weaponDescription.WeaponClass != WeaponClass.ThrowingAxe && _weaponDescription.WeaponClass != WeaponClass.Javelin)
				{
					return WeaponClass.Undefined;
				}
				return _weaponDescription.WeaponClass;
			}

			private static float ParallelAxis(WeaponDesignElement selectedPiece, float offset, float weightMultiplier)
			{
				float inertia = selectedPiece.CraftingPiece.Inertia;
				float offsetFromCm = offset + selectedPiece.CraftingPiece.CenterOfMass;
				float mass = selectedPiece.ScaledWeight * weightMultiplier;
				return ParallelAxis(inertia, mass, offsetFromCm);
			}

			private static float ParallelAxis(float inertiaAroundCm, float mass, float offsetFromCm)
			{
				return inertiaAroundCm + mass * offsetFromCm * offsetFromCm;
			}

			private void SimulateSwingLayer(double angleSpan, double usablePower, double maxUsableTorque, double inertia, out double finalSpeed, out double finalTime)
			{
				double num = 0.0;
				double num2 = 0.01;
				double num3 = 0.0;
				double num4 = 3.9 * (double)_currentWeaponReach * (_weaponDescription.WeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon | WeaponFlags.WideGrip) ? 1.0 : 0.3);
				while (num < angleSpan)
				{
					double num5 = usablePower / num2;
					if (num5 > maxUsableTorque)
					{
						num5 = maxUsableTorque;
					}
					num5 -= num2 * num4;
					double num6 = 0.009999999776482582 * num5 / inertia;
					num2 += num6;
					num += num2 * 0.009999999776482582;
					num3 += 0.009999999776482582;
				}
				finalSpeed = num2;
				finalTime = num3;
			}

			private void SimulateThrustLayer(double distance, double usablePower, double maxUsableForce, double mass, out double finalSpeed, out double finalTime)
			{
				double num = 0.0;
				double num2 = 0.01;
				double num3 = 0.0;
				while (num < distance)
				{
					double num4 = usablePower / num2;
					if (num4 > maxUsableForce)
					{
						num4 = maxUsableForce;
					}
					double num5 = 0.01 * num4 / mass;
					num2 += num5;
					num += num2 * 0.01;
					num3 += 0.01;
				}
				finalSpeed = num2;
				finalTime = num3;
			}
		}

		public static ItemObject GenerateCraftedItem(ItemObject item, WeaponDesign weaponDesign, ItemModifierGroup itemModifierGroup)
		{
			WeaponDesignElement[] usedPieces = weaponDesign.UsedPieces;
			foreach (WeaponDesignElement weaponDesignElement in usedPieces)
			{
				if ((weaponDesignElement.IsValid && !weaponDesign.Template.Pieces.Contains(weaponDesignElement.CraftingPiece)) || (weaponDesignElement.CraftingPiece.IsInitialized && !weaponDesignElement.IsValid))
				{
					Debug.Print(weaponDesignElement.CraftingPiece.StringId + " is not a valid valid anymore.");
					return null;
				}
			}
			bool isAlternative = false;
			WeaponDescription[] weaponDescriptions = weaponDesign.Template.WeaponDescriptions;
			foreach (WeaponDescription weaponDescription in weaponDescriptions)
			{
				int num = 4;
				for (int j = 0; j < weaponDesign.UsedPieces.Length; j++)
				{
					if (!weaponDesign.UsedPieces[j].IsValid)
					{
						num--;
					}
				}
				foreach (CraftingPiece availablePiece in weaponDescription.AvailablePieces)
				{
					int pieceType = (int)availablePiece.PieceType;
					if (weaponDesign.UsedPieces[pieceType].CraftingPiece == availablePiece)
					{
						num--;
					}
					if (num == 0)
					{
						break;
					}
				}
				if (num <= 0)
				{
					WeaponFlags weaponFlags = weaponDescription.WeaponFlags | weaponDesign.WeaponFlags;
					CraftingStats.FillWeapon(item, weaponDescription, weaponFlags, isAlternative, out var filledWeapon);
					item.AddWeapon(filledWeapon, itemModifierGroup);
					isAlternative = true;
				}
			}
			return item;
		}
	}

	public const int WeightOfCrudeIron = 1;

	public const int WeightOfIron = 2;

	public const int WeightOfCompositeIron = 3;

	public const int WeightOfSteel = 4;

	public const int WeightOfRefinedSteel = 5;

	public const int WeightOfCalradianSteel = 6;

	private List<WeaponDesign> _history;

	private int _currentHistoryIndex;

	private ItemObject _craftedItemObject;

	public BasicCultureObject CurrentCulture { get; }

	public CraftingTemplate CurrentCraftingTemplate { get; }

	public WeaponDesign CurrentWeaponDesign { get; private set; }

	public ItemModifierGroup CurrentItemModifierGroup { get; private set; }

	public TextObject CraftedWeaponName { get; private set; }

	public List<WeaponDesignElement>[] UsablePiecesList { get; private set; }

	public WeaponDesignElement[] SelectedPieces => CurrentWeaponDesign.UsedPieces;

	public Crafting(CraftingTemplate craftingTemplate, BasicCultureObject culture, TextObject name)
	{
		CraftedWeaponName = name;
		CurrentCraftingTemplate = craftingTemplate;
		CurrentCulture = culture;
	}

	public void SetCraftedWeaponName(TextObject weaponName)
	{
		if (!weaponName.Equals(CraftedWeaponName))
		{
			CraftedWeaponName = weaponName;
			_craftedItemObject.SetCraftedWeaponName(CraftedWeaponName);
		}
	}

	public void Init()
	{
		_history = new List<WeaponDesign>();
		UsablePiecesList = new List<WeaponDesignElement>[4];
		foreach (CraftingPiece craftingPiece in (IEnumerable<CraftingPiece>)CurrentCraftingTemplate.Pieces)
		{
			if (!CurrentCraftingTemplate.BuildOrders.All((PieceData x) => x.PieceType != craftingPiece.PieceType))
			{
				int pieceType = (int)craftingPiece.PieceType;
				if (UsablePiecesList[pieceType] == null)
				{
					UsablePiecesList[pieceType] = new List<WeaponDesignElement>();
				}
				UsablePiecesList[pieceType].Add(WeaponDesignElement.CreateUsablePiece(craftingPiece));
			}
		}
		WeaponDesignElement[] array = new WeaponDesignElement[4];
		for (int num = 0; num < array.Length; num++)
		{
			if (UsablePiecesList[num] != null)
			{
				array[num] = UsablePiecesList[num].First((WeaponDesignElement p) => !p.CraftingPiece.IsHiddenOnDesigner);
			}
			else
			{
				array[num] = WeaponDesignElement.GetInvalidPieceForType((CraftingPiece.PieceTypes)num);
			}
		}
		CurrentWeaponDesign = new WeaponDesign(CurrentCraftingTemplate, null, array, CurrentCraftingTemplate.StringId);
		_history.Add(CurrentWeaponDesign);
	}

	public WeaponDesignElement GetRandomPieceOfType(CraftingPiece.PieceTypes pieceType, bool randomScale)
	{
		if (!CurrentCraftingTemplate.IsPieceTypeUsable(pieceType))
		{
			return WeaponDesignElement.GetInvalidPieceForType(pieceType);
		}
		WeaponDesignElement copy = UsablePiecesList[(int)pieceType][MBRandom.RandomInt(UsablePiecesList[(int)pieceType].Count)].GetCopy();
		if (randomScale)
		{
			copy.SetScale((int)(90f + MBRandom.RandomFloat * 20f));
		}
		return copy;
	}

	public void SwitchToCraftedItem(ItemObject item)
	{
		WeaponDesignElement[] usedPieces = item.WeaponDesign.UsedPieces;
		WeaponDesignElement[] array = new WeaponDesignElement[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = usedPieces[i].GetCopy();
		}
		CurrentWeaponDesign = new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, array);
		ReIndex();
	}

	public void Randomize()
	{
		WeaponDesignElement[] array = new WeaponDesignElement[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = GetRandomPieceOfType((CraftingPiece.PieceTypes)i, randomScale: true);
		}
		CurrentWeaponDesign = new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, array);
		ReIndex();
	}

	public void SwitchToPiece(WeaponDesignElement piece)
	{
		CraftingPiece.PieceTypes pieceType = piece.CraftingPiece.PieceType;
		WeaponDesignElement[] array = new WeaponDesignElement[4];
		for (int i = 0; i < array.Length; i++)
		{
			if (pieceType == (CraftingPiece.PieceTypes)i)
			{
				array[i] = piece.GetCopy();
				array[i].SetScale(100);
				continue;
			}
			array[i] = CurrentWeaponDesign.UsedPieces[i].GetCopy();
			if (array[i].IsValid)
			{
				array[i].SetScale(CurrentWeaponDesign.UsedPieces[i].ScalePercentage);
			}
		}
		CurrentWeaponDesign = new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, array);
		ReIndex();
	}

	public void ScaleThePiece(CraftingPiece.PieceTypes scalingPieceType, int percentage)
	{
		WeaponDesignElement[] array = new WeaponDesignElement[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = SelectedPieces[i].GetCopy();
			if (SelectedPieces[i].IsPieceScaled)
			{
				array[i].SetScale(SelectedPieces[i].ScalePercentage);
			}
		}
		array[(int)scalingPieceType].SetScale(percentage);
		CurrentWeaponDesign = new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, array);
		ReIndex();
	}

	public void ReIndex(bool enforceReCreation = false)
	{
		if (!TextObject.IsNullOrEmpty(CurrentWeaponDesign.WeaponName) && !CurrentWeaponDesign.WeaponName.ToString().Equals(CraftedWeaponName.ToString()))
		{
			CraftedWeaponName = CurrentWeaponDesign.WeaponName.CopyTextObject();
		}
		if (enforceReCreation)
		{
			CurrentWeaponDesign = new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, CurrentWeaponDesign.UsedPieces.ToArray());
		}
		SetItemObject();
	}

	public bool Undo()
	{
		if (_currentHistoryIndex <= 0)
		{
			return false;
		}
		_currentHistoryIndex--;
		CurrentWeaponDesign = _history[_currentHistoryIndex];
		ReIndex();
		return true;
	}

	public bool Redo()
	{
		if (_currentHistoryIndex + 1 >= _history.Count)
		{
			return false;
		}
		_currentHistoryIndex++;
		CurrentWeaponDesign = _history[_currentHistoryIndex];
		ReIndex();
		return true;
	}

	public void UpdateHistory()
	{
		if (_currentHistoryIndex < _history.Count - 1)
		{
			_history.RemoveRange(_currentHistoryIndex + 1, _history.Count - 1 - _currentHistoryIndex);
		}
		WeaponDesignElement[] array = new WeaponDesignElement[CurrentWeaponDesign.UsedPieces.Length];
		for (int i = 0; i < CurrentWeaponDesign.UsedPieces.Length; i++)
		{
			array[i] = CurrentWeaponDesign.UsedPieces[i].GetCopy();
			if (array[i].IsValid)
			{
				array[i].SetScale(CurrentWeaponDesign.UsedPieces[i].ScalePercentage);
			}
		}
		_history.Add(new WeaponDesign(CurrentWeaponDesign.Template, CurrentWeaponDesign.WeaponName, array));
		_currentHistoryIndex = _history.Count - 1;
	}

	public TextObject GetRandomCraftName()
	{
		return new TextObject("{=!}RANDOM_NAME");
	}

	public static void GenerateItem(WeaponDesign weaponDesignTemplate, TextObject name, BasicCultureObject culture, ItemModifierGroup itemModifierGroup, ref ItemObject itemObject, string customId = null)
	{
		if (itemObject == null)
		{
			itemObject = new ItemObject();
		}
		WeaponDesignElement[] array = new WeaponDesignElement[weaponDesignTemplate.UsedPieces.Length];
		for (int i = 0; i < weaponDesignTemplate.UsedPieces.Length; i++)
		{
			WeaponDesignElement weaponDesignElement = weaponDesignTemplate.UsedPieces[i];
			array[i] = WeaponDesignElement.CreateUsablePiece(weaponDesignElement.CraftingPiece, weaponDesignElement.ScalePercentage);
		}
		WeaponDesign weaponDesign = new WeaponDesign(weaponDesignTemplate.Template, name, array, customId);
		float weight = TaleWorlds.Library.MathF.Round(weaponDesign.UsedPieces.Sum((WeaponDesignElement selectedUsablePiece) => selectedUsablePiece.ScaledWeight), 2);
		float appearance = (weaponDesign.UsedPieces[3].IsValid ? weaponDesign.UsedPieces[3].CraftingPiece.Appearance : weaponDesign.UsedPieces[0].CraftingPiece.Appearance);
		if (!string.IsNullOrEmpty(customId))
		{
			itemObject.StringId = customId;
		}
		else
		{
			itemObject.StringId = ((!string.IsNullOrEmpty(itemObject.StringId)) ? itemObject.StringId : weaponDesignTemplate.Template.StringId);
		}
		ItemObject.InitCraftedItemObject(ref itemObject, name, culture, GetItemFlags(weaponDesign), weight, appearance, weaponDesign, weaponDesign.Template.ItemType);
		itemObject = CraftedItemGenerationHelper.GenerateCraftedItem(itemObject, weaponDesign, itemModifierGroup);
		if (itemObject != null)
		{
			if (itemObject.IsCraftedByPlayer)
			{
				itemObject.IsReady = true;
			}
			itemObject.DetermineValue();
			itemObject.DetermineItemCategoryForItem();
		}
	}

	private static ItemFlags GetItemFlags(WeaponDesign weaponDesign)
	{
		return weaponDesign.UsedPieces[0].CraftingPiece.AdditionalItemFlags;
	}

	private void SetItemObject(ItemObject itemObject = null, string customId = null)
	{
		if (itemObject == null)
		{
			itemObject = new ItemObject();
		}
		GenerateItem(CurrentWeaponDesign, CraftedWeaponName, CurrentCulture, CurrentItemModifierGroup, ref itemObject, customId);
		_craftedItemObject = itemObject;
	}

	public ItemObject GetCurrentCraftedItemObject(bool forceReCreate = false, string customId = null)
	{
		if (forceReCreate)
		{
			SetItemObject(null, customId);
		}
		return _craftedItemObject;
	}

	public static IEnumerable<CraftingStatData> GetStatDatasFromTemplate(int usageIndex, ItemObject craftedItemObject, CraftingTemplate template)
	{
		WeaponComponentData weapon = craftedItemObject.GetWeaponWithUsageIndex(usageIndex);
		DamageTypes statDamageType = DamageTypes.Invalid;
		foreach (KeyValuePair<CraftingTemplate.CraftingStatTypes, float> statData in template.GetStatDatas(weapon.WeaponDescriptionId, weapon.ThrustDamageType, weapon.SwingDamageType))
		{
			TextObject textObject = GameTexts.FindText("str_crafting_stat", statData.Key.ToString());
			switch (statData.Key)
			{
			case CraftingTemplate.CraftingStatTypes.MissileDamage:
				if (weapon.ThrustDamageType != DamageTypes.Invalid)
				{
					textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
					statDamageType = weapon.ThrustDamageType;
				}
				else if (weapon.SwingDamageType != DamageTypes.Invalid)
				{
					textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
					statDamageType = weapon.SwingDamageType;
				}
				else
				{
					Debug.FailedAssert("Missile damage type is missing.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "GetStatDatasFromTemplate", 1192);
				}
				break;
			case CraftingTemplate.CraftingStatTypes.ThrustDamage:
				textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
				statDamageType = weapon.ThrustDamageType;
				break;
			case CraftingTemplate.CraftingStatTypes.SwingDamage:
				textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
				statDamageType = weapon.SwingDamageType;
				break;
			}
			float value = statData.Value;
			float valueForCraftingStatForWeaponOfUsageIndex = GetValueForCraftingStatForWeaponOfUsageIndex(statData.Key, craftedItemObject, weapon);
			valueForCraftingStatForWeaponOfUsageIndex = MBMath.ClampFloat(valueForCraftingStatForWeaponOfUsageIndex, 0f, value);
			yield return new CraftingStatData(textObject, valueForCraftingStatForWeaponOfUsageIndex, value, statData.Key, statDamageType);
		}
	}

	private static float GetValueForCraftingStatForWeaponOfUsageIndex(CraftingTemplate.CraftingStatTypes craftingStatType, ItemObject item, WeaponComponentData weapon)
	{
		return craftingStatType switch
		{
			CraftingTemplate.CraftingStatTypes.Weight => item.Weight, 
			CraftingTemplate.CraftingStatTypes.WeaponReach => weapon.WeaponLength, 
			CraftingTemplate.CraftingStatTypes.ThrustSpeed => weapon.ThrustSpeed, 
			CraftingTemplate.CraftingStatTypes.SwingSpeed => weapon.SwingSpeed, 
			CraftingTemplate.CraftingStatTypes.ThrustDamage => weapon.ThrustDamage, 
			CraftingTemplate.CraftingStatTypes.SwingDamage => weapon.SwingDamage, 
			CraftingTemplate.CraftingStatTypes.Handling => weapon.Handling, 
			CraftingTemplate.CraftingStatTypes.MissileDamage => weapon.MissileDamage, 
			CraftingTemplate.CraftingStatTypes.MissileSpeed => weapon.MissileSpeed, 
			CraftingTemplate.CraftingStatTypes.Accuracy => weapon.Accuracy, 
			CraftingTemplate.CraftingStatTypes.StackAmount => weapon.GetModifiedStackCount(null), 
			_ => throw new ArgumentOutOfRangeException("craftingStatType", craftingStatType, null), 
		};
	}

	public IEnumerable<CraftingStatData> GetStatDatas(int usageIndex)
	{
		WeaponComponentData weapon = _craftedItemObject.GetWeaponWithUsageIndex(usageIndex);
		foreach (KeyValuePair<CraftingTemplate.CraftingStatTypes, float> statData in CurrentCraftingTemplate.GetStatDatas(weapon.WeaponDescriptionId, weapon.ThrustDamageType, weapon.SwingDamageType))
		{
			DamageTypes damageType = DamageTypes.Invalid;
			TextObject textObject = GameTexts.FindText("str_crafting_stat", statData.Key.ToString());
			switch (statData.Key)
			{
			case CraftingTemplate.CraftingStatTypes.MissileDamage:
				if (weapon.ThrustDamageType != DamageTypes.Invalid)
				{
					textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
					damageType = weapon.ThrustDamageType;
				}
				else if (weapon.SwingDamageType != DamageTypes.Invalid)
				{
					textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
					damageType = weapon.SwingDamageType;
				}
				else
				{
					Debug.FailedAssert("Missile damage type is missing.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Crafting.cs", "GetStatDatas", 1277);
				}
				break;
			case CraftingTemplate.CraftingStatTypes.ThrustDamage:
				textObject.SetTextVariable("THRUST_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.ThrustDamageType).ToString()));
				damageType = weapon.ThrustDamageType;
				break;
			case CraftingTemplate.CraftingStatTypes.SwingDamage:
				textObject.SetTextVariable("SWING_DAMAGE_TYPE", GameTexts.FindText("str_inventory_dmg_type", ((int)weapon.SwingDamageType).ToString()));
				damageType = weapon.SwingDamageType;
				break;
			}
			float valueForCraftingStatForWeaponOfUsageIndex = GetValueForCraftingStatForWeaponOfUsageIndex(statData.Key, _craftedItemObject, weapon);
			float value = statData.Value;
			yield return new CraftingStatData(textObject, valueForCraftingStatForWeaponOfUsageIndex, value, statData.Key, damageType);
		}
	}

	public string GetXmlCodeForCurrentItem(ItemObject item)
	{
		string text = "";
		text = string.Concat(text, "<CraftedItem id=\"", CurrentWeaponDesign.HashedCode, "\"\n\t\t\t\t\t\t\t name=\"", CraftedWeaponName, "\"\n\t\t\t\t\t\t\t crafting_template=\"", CurrentCraftingTemplate.StringId, "\">");
		text += "\n";
		text += "<Pieces>";
		text += "\n";
		WeaponDesignElement[] selectedPieces = SelectedPieces;
		foreach (WeaponDesignElement weaponDesignElement in selectedPieces)
		{
			if (weaponDesignElement.IsValid)
			{
				string text2 = "";
				if (weaponDesignElement.ScalePercentage != 100)
				{
					int scalePercentage = weaponDesignElement.ScalePercentage;
					text2 = "\n\t\t\t scale_factor=\"" + scalePercentage + "\"";
				}
				text = string.Concat(text, "<Piece id=\"", weaponDesignElement.CraftingPiece.StringId, "\"\n\t\t\t Type=\"", weaponDesignElement.CraftingPiece.PieceType, "\"", text2, "/>");
				text += "\n";
			}
		}
		text += "</Pieces>";
		text += "\n";
		text += "<!-- ";
		text = text + "Length: " + item.PrimaryWeapon.WeaponLength;
		text = text + " Weight: " + TaleWorlds.Library.MathF.Round(item.Weight, 2);
		text += " -->";
		text += "\n";
		return text + "</CraftedItem>";
	}

	public bool TryGetWeaponPropertiesFromXmlCode(string xmlCode, out CraftingTemplate craftingTemplate, out (CraftingPiece, int)[] pieces)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xmlCode);
			pieces = new(CraftingPiece, int)[4];
			XmlNode? xmlNode = xmlDocument.SelectSingleNode("CraftedItem");
			string value = xmlNode.Attributes["crafting_template"].Value;
			craftingTemplate = CraftingTemplate.GetTemplateFromId(value);
			foreach (XmlNode childNode in xmlNode.SelectSingleNode("Pieces").ChildNodes)
			{
				CraftingPiece.PieceTypes pieceTypes = CraftingPiece.PieceTypes.Invalid;
				string pieceId = null;
				int item = 100;
				foreach (XmlAttribute attribute in childNode.Attributes)
				{
					if (attribute.Name == "Type")
					{
						pieceTypes = (CraftingPiece.PieceTypes)Enum.Parse(typeof(CraftingPiece.PieceTypes), attribute.Value);
					}
					else if (attribute.Name == "id")
					{
						pieceId = attribute.Value;
					}
					else if (attribute.Name == "scale_factor")
					{
						item = int.Parse(attribute.Value);
					}
				}
				if (pieceTypes != CraftingPiece.PieceTypes.Invalid && !string.IsNullOrEmpty(pieceId) && craftingTemplate.IsPieceTypeUsable(pieceTypes))
				{
					pieces[(int)pieceTypes] = (CraftingPiece.All.FirstOrDefault((CraftingPiece p) => p.StringId == pieceId), item);
				}
			}
			return true;
		}
		catch (Exception)
		{
			craftingTemplate = null;
			pieces = null;
			return false;
		}
	}

	public static ItemObject CreatePreCraftedWeaponOnDeserialize(ItemObject itemObject, WeaponDesignElement[] usedPieces, string templateId, TextObject craftedWeaponName, ItemModifierGroup itemModifierGroup)
	{
		for (int i = 0; i < usedPieces.Length; i++)
		{
			if (usedPieces[i] == null)
			{
				usedPieces[i] = WeaponDesignElement.GetInvalidPieceForType((CraftingPiece.PieceTypes)i);
			}
		}
		if (TextObject.IsNullOrEmpty(craftedWeaponName))
		{
			Debug.Print("ItemObject with id = (" + itemObject.StringId + ") name is null from xml, make sure this is intended");
			craftedWeaponName = new TextObject("{=Uz1HHeKg}Crafted Random Weapon");
		}
		WeaponDesign weaponDesign = new WeaponDesign(CraftingTemplate.GetTemplateFromId(templateId), craftedWeaponName, usedPieces, itemObject.StringId);
		Crafting crafting = new Crafting(CraftingTemplate.GetTemplateFromId(templateId), null, craftedWeaponName);
		crafting.CurrentWeaponDesign = weaponDesign;
		crafting.CurrentItemModifierGroup = itemModifierGroup;
		crafting._history = new List<WeaponDesign> { weaponDesign };
		crafting.SetItemObject(itemObject, itemObject.StringId);
		return crafting._craftedItemObject;
	}

	public static ItemObject InitializePreCraftedWeaponOnLoad(ItemObject itemObject, WeaponDesign craftedData, TextObject itemName, BasicCultureObject culture)
	{
		Crafting crafting = new Crafting(craftedData.Template, culture, itemName);
		crafting.CurrentWeaponDesign = craftedData;
		crafting._history = new List<WeaponDesign> { craftedData };
		crafting.SetItemObject(itemObject, itemObject.StringId);
		return crafting._craftedItemObject;
	}
}
