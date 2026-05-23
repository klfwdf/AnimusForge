using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000D RID: 13
	public class InputKeyVisualWidget : Widget
	{
		// Token: 0x060000C2 RID: 194 RVA: 0x00003FB9 File Offset: 0x000021B9
		public InputKeyVisualWidget(UIContext context)
			: base(context)
		{
			base.DoNotAcceptEvents = true;
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00003FE0 File Offset: 0x000021E0
		private string GetKeyVisualName(string keyID)
		{
			Input.ControllerTypes controllerType = Input.ControllerType;
			string result = "None";
			uint num = <PrivateImplementationDetails>.ComputeStringHash(keyID);
			if (num <= 2144691513U)
			{
				if (num <= 1107541039U)
				{
					if (num <= 265173022U)
					{
						if (num <= 139650141U)
						{
							if (num <= 106449248U)
							{
								if (num <= 97396832U)
								{
									if (num != 75071339U)
									{
										if (num != 97396832U)
										{
											return result;
										}
										if (!(keyID == "D3"))
										{
											return result;
										}
										goto IL_15CB;
									}
									else
									{
										if (!(keyID == "LeftAlt"))
										{
											return result;
										}
										goto IL_1637;
									}
								}
								else if (num != 100894848U)
								{
									if (num != 106449248U)
									{
										return result;
									}
									if (!(keyID == "RightMouseButton"))
									{
										return result;
									}
									goto IL_1578;
								}
								else
								{
									if (!(keyID == "Extended"))
									{
										return result;
									}
									return result;
								}
							}
							else if (num <= 115203180U)
							{
								if (num != 114174451U)
								{
									if (num != 115203180U)
									{
										return result;
									}
									if (!(keyID == "BackSpace"))
									{
										return result;
									}
									goto IL_1578;
								}
								else
								{
									if (!(keyID == "D2"))
									{
										return result;
									}
									goto IL_15C0;
								}
							}
							else if (num != 117964108U)
							{
								if (num != 130952070U)
								{
									if (num != 139650141U)
									{
										return result;
									}
									if (!(keyID == "OpenBraces"))
									{
										return result;
									}
									return result;
								}
								else
								{
									if (!(keyID == "D1"))
									{
										return result;
									}
									goto IL_15B5;
								}
							}
							else
							{
								if (!(keyID == "NumpadMinus"))
								{
									return result;
								}
								return "-";
							}
						}
						else if (num <= 198062546U)
						{
							if (num <= 164507308U)
							{
								if (num != 147729689U)
								{
									if (num != 164507308U)
									{
										return result;
									}
									if (!(keyID == "D7"))
									{
										return result;
									}
									goto IL_15F7;
								}
								else if (!(keyID == "D0"))
								{
									return result;
								}
							}
							else if (num != 181284927U)
							{
								if (num != 198062546U)
								{
									return result;
								}
								if (!(keyID == "D5"))
								{
									return result;
								}
								goto IL_15E1;
							}
							else
							{
								if (!(keyID == "D6"))
								{
									return result;
								}
								goto IL_15EC;
							}
						}
						else if (num <= 214840165U)
						{
							if (num != 198356736U)
							{
								if (num != 214840165U)
								{
									return result;
								}
								if (!(keyID == "D4"))
								{
									return result;
								}
								goto IL_15D6;
							}
							else
							{
								if (!(keyID == "F9"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 215134355U)
						{
							if (num != 254900552U)
							{
								if (num != 265173022U)
								{
									return result;
								}
								if (!(keyID == "D9"))
								{
									return result;
								}
								goto IL_1607;
							}
							else
							{
								if (!(keyID == "Insert"))
								{
									return result;
								}
								return result;
							}
						}
						else
						{
							if (!(keyID == "F8"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 416465783U)
					{
						if (num <= 354454743U)
						{
							if (num <= 302657454U)
							{
								if (num != 281950641U)
								{
									if (num != 302657454U)
									{
										return result;
									}
									if (!(keyID == "ControllerRStickUp"))
									{
										return result;
									}
									goto IL_1657;
								}
								else
								{
									if (!(keyID == "D8"))
									{
										return result;
									}
									goto IL_15FF;
								}
							}
							else if (num != 332577688U)
							{
								if (num != 354454743U)
								{
									return result;
								}
								if (!(keyID == "ControllerRThumb"))
								{
									return result;
								}
								return "controllerrthumb";
							}
							else
							{
								if (!(keyID == "F1"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num <= 382910545U)
						{
							if (num != 366132926U)
							{
								if (num != 382910545U)
								{
									return result;
								}
								if (!(keyID == "F2"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "F3"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 389828744U)
						{
							if (num != 399688164U)
							{
								if (num != 416465783U)
								{
									return result;
								}
								if (!(keyID == "F4"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "F5"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else
						{
							if (!(keyID == "MouseScrollUp"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 575450500U)
					{
						if (num <= 450021021U)
						{
							if (num != 433243402U)
							{
								if (num != 450021021U)
								{
									return result;
								}
								if (!(keyID == "F6"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "F7"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 513712005U)
						{
							if (num != 575450500U)
							{
								return result;
							}
							if (!(keyID == "Apostrophe"))
							{
								return result;
							}
							return result;
						}
						else
						{
							if (!(keyID == "Right"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 1044186795U)
					{
						if (num != 1039550435U)
						{
							if (num != 1044186795U)
							{
								return result;
							}
							if (!(keyID == "PageUp"))
							{
								return result;
							}
							return result;
						}
						else
						{
							if (!(keyID == "ControllerLDown"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num != 1050238388U)
					{
						if (num != 1081442551U)
						{
							if (num != 1107541039U)
							{
								return result;
							}
							if (!(keyID == "ControllerRTrigger"))
							{
								return result;
							}
							goto IL_1578;
						}
						else
						{
							if (!(keyID == "CloseBraces"))
							{
								return result;
							}
							return result;
						}
					}
					else
					{
						if (!(keyID == "Equals"))
						{
							return result;
						}
						return result;
					}
				}
				else if (num <= 1706424088U)
				{
					if (num <= 1355078617U)
					{
						if (num <= 1231278590U)
						{
							if (num <= 1138704245U)
							{
								if (num != 1123244352U)
								{
									if (num != 1138704245U)
									{
										return result;
									}
									if (!(keyID == "X1MouseButton"))
									{
										return result;
									}
									return result;
								}
								else
								{
									if (!(keyID == "Up"))
									{
										return result;
									}
									goto IL_1578;
								}
							}
							else if (num != 1174120482U)
							{
								if (num != 1231278590U)
								{
									return result;
								}
								if (!(keyID == "RightControl"))
								{
									return result;
								}
								goto IL_162F;
							}
							else
							{
								if (!(keyID == "ControllerLUp"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num <= 1304745760U)
						{
							if (num != 1296647161U)
							{
								if (num != 1304745760U)
								{
									return result;
								}
								if (!(keyID == "F21"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "ControllerLTrigger"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 1321523379U)
						{
							if (num != 1338300998U)
							{
								if (num != 1355078617U)
								{
									return result;
								}
								if (!(keyID == "F22"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "F23"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else
						{
							if (!(keyID == "F20"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 1469573738U)
					{
						if (num <= 1391791790U)
						{
							if (num != 1388633855U)
							{
								if (num != 1391791790U)
								{
									return result;
								}
								if (!(keyID == "Home"))
								{
									return result;
								}
								return result;
							}
							else
							{
								if (!(keyID == "F24"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 1428210068U)
						{
							if (num != 1469573738U)
							{
								return result;
							}
							if (!(keyID == "Delete"))
							{
								return result;
							}
							return result;
						}
						else
						{
							if (!(keyID == "LeftShift"))
							{
								return result;
							}
							goto IL_1627;
						}
					}
					else if (num <= 1537849368U)
					{
						if (num != 1529719870U)
						{
							if (num != 1537849368U)
							{
								return result;
							}
							if (!(keyID == "ControllerROption"))
							{
								return result;
							}
							goto IL_1584;
						}
						else
						{
							if (!(keyID == "ControllerLLeft"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num != 1650792303U)
					{
						if (num != 1702612722U)
						{
							if (num != 1706424088U)
							{
								return result;
							}
							if (!(keyID == "Comma"))
							{
								return result;
							}
							return result;
						}
						else
						{
							if (!(keyID == "ControllerRBumper"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else
					{
						if (!(keyID == "ControllerRStickRight"))
						{
							return result;
						}
						goto IL_1657;
					}
				}
				else if (num <= 1910265404U)
				{
					if (num <= 1859932547U)
					{
						if (num <= 1843154928U)
						{
							if (num != 1806183147U)
							{
								if (num != 1843154928U)
								{
									return result;
								}
								if (!(keyID == "Numpad4"))
								{
									return result;
								}
								goto IL_15D6;
							}
							else
							{
								if (!(keyID == "MiddleMouseButton"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 1852896292U)
						{
							if (num != 1859932547U)
							{
								return result;
							}
							if (!(keyID == "Numpad5"))
							{
								return result;
							}
							goto IL_15E1;
						}
						else
						{
							if (!(keyID == "ControllerRLeft"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 1876710166U)
					{
						if (num != 1868010299U)
						{
							if (num != 1876710166U)
							{
								return result;
							}
							if (!(keyID == "Numpad6"))
							{
								return result;
							}
							goto IL_15EC;
						}
						else
						{
							if (!(keyID == "ControllerRStick"))
							{
								return result;
							}
							goto IL_1657;
						}
					}
					else if (num != 1893487785U)
					{
						if (num != 1898928778U)
						{
							if (num != 1910265404U)
							{
								return result;
							}
							if (!(keyID == "Numpad0"))
							{
								return result;
							}
						}
						else
						{
							if (!(keyID == "Slash"))
							{
								return result;
							}
							return result;
						}
					}
					else
					{
						if (!(keyID == "Numpad7"))
						{
							return result;
						}
						goto IL_15F7;
					}
				}
				else if (num <= 2008406340U)
				{
					if (num <= 1943820642U)
					{
						if (num != 1927043023U)
						{
							if (num != 1943820642U)
							{
								return result;
							}
							if (!(keyID == "Numpad2"))
							{
								return result;
							}
							goto IL_15C0;
						}
						else
						{
							if (!(keyID == "Numpad1"))
							{
								return result;
							}
							goto IL_15B5;
						}
					}
					else if (num != 1960598261U)
					{
						if (num != 2008406340U)
						{
							return result;
						}
						if (!(keyID == "ControllerLStickUp"))
						{
							return result;
						}
						goto IL_1647;
					}
					else
					{
						if (!(keyID == "Numpad3"))
						{
							return result;
						}
						goto IL_15CB;
					}
				}
				else if (num <= 2061263975U)
				{
					if (num != 2044486356U)
					{
						if (num != 2061263975U)
						{
							return result;
						}
						if (!(keyID == "Numpad9"))
						{
							return result;
						}
						goto IL_1607;
					}
					else
					{
						if (!(keyID == "Numpad8"))
						{
							return result;
						}
						goto IL_15FF;
					}
				}
				else if (num != 2083773698U)
				{
					if (num != 2112836247U)
					{
						if (num != 2144691513U)
						{
							return result;
						}
						if (!(keyID == "ControllerRDown"))
						{
							return result;
						}
						goto IL_1578;
					}
					else
					{
						if (!(keyID == "ControllerRStickDown"))
						{
							return result;
						}
						goto IL_1657;
					}
				}
				else
				{
					if (!(keyID == "ControllerRStickLeft"))
					{
						return result;
					}
					goto IL_1657;
				}
				return "0";
				IL_15B5:
				return "1";
				IL_15C0:
				return "2";
				IL_15CB:
				return "3";
				IL_15D6:
				return "4";
				IL_15E1:
				return "5";
				IL_15EC:
				return "6";
				IL_15F7:
				return "7";
				IL_15FF:
				return "8";
				IL_1607:
				return "9";
				IL_1657:
				return "controllerrstick";
			}
			if (num <= 3423339364U)
			{
				if (num <= 3082514982U)
				{
					if (num <= 2746130317U)
					{
						if (num <= 2365054562U)
						{
							if (num <= 2267317284U)
							{
								if (num != 2157724748U)
								{
									if (num != 2267317284U)
									{
										return result;
									}
									if (!(keyID == "Period"))
									{
										return result;
									}
									return result;
								}
								else
								{
									if (!(keyID == "ControllerLStickLeft"))
									{
										return result;
									}
									goto IL_1647;
								}
							}
							else if (num != 2340347977U)
							{
								if (num != 2365054562U)
								{
									return result;
								}
								if (!(keyID == "NumpadEnter"))
								{
									return result;
								}
							}
							else
							{
								if (!(keyID == "Tilde"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num <= 2457286800U)
						{
							if (num != 2434225852U)
							{
								if (num != 2457286800U)
								{
									return result;
								}
								if (!(keyID == "Left"))
								{
									return result;
								}
								goto IL_1578;
							}
							else
							{
								if (!(keyID == "RightAlt"))
								{
									return result;
								}
								goto IL_1637;
							}
						}
						else if (num != 2595691489U)
						{
							if (num != 2728445041U)
							{
								if (num != 2746130317U)
								{
									return result;
								}
								if (!(keyID == "ControllerLThumb"))
								{
									return result;
								}
								return "controllerlthumb";
							}
							else
							{
								if (!(keyID == "ControllerLStickDown"))
								{
									return result;
								}
								goto IL_1647;
							}
						}
						else
						{
							if (!(keyID == "ControllerLStick"))
							{
								return result;
							}
							goto IL_1647;
						}
					}
					else if (num <= 2906557000U)
					{
						if (num <= 2762355378U)
						{
							if (num != 2761510965U)
							{
								if (num != 2762355378U)
								{
									return result;
								}
								if (!(keyID == "X2MouseButton"))
								{
									return result;
								}
								return result;
							}
							else
							{
								if (!(keyID == "Down"))
								{
									return result;
								}
								goto IL_1578;
							}
						}
						else if (num != 2769091631U)
						{
							if (num != 2906557000U)
							{
								return result;
							}
							if (!(keyID == "ControllerLBumper"))
							{
								return result;
							}
							goto IL_1578;
						}
						else
						{
							if (!(keyID == "CapsLock"))
							{
								return result;
							}
							goto IL_1578;
						}
					}
					else if (num <= 2952291245U)
					{
						if (num != 2913305049U)
						{
							if (num != 2952291245U)
							{
								return result;
							}
							if (!(keyID == "Enter"))
							{
								return result;
							}
						}
						else
						{
							if (!(keyID == "ControllerLStickRight"))
							{
								return result;
							}
							goto IL_1647;
						}
					}
					else if (num != 3001337907U)
					{
						if (num != 3036628469U)
						{
							if (num != 3082514982U)
							{
								return result;
							}
							if (!(keyID == "Escape"))
							{
								return result;
							}
							goto IL_1578;
						}
						else
						{
							if (!(keyID == "LeftControl"))
							{
								return result;
							}
							goto IL_162F;
						}
					}
					else
					{
						if (!(keyID == "LeftMouseButton"))
						{
							return result;
						}
						goto IL_1578;
					}
					return "enter";
				}
				if (num <= 3294917732U)
				{
					if (num <= 3241480638U)
					{
						if (num <= 3222007936U)
						{
							if (num != 3093862813U)
							{
								if (num != 3222007936U)
								{
									return result;
								}
								if (!(keyID == "E"))
								{
									return result;
								}
							}
							else
							{
								if (!(keyID == "NumpadPeriod"))
								{
									return result;
								}
								return result;
							}
						}
						else if (num != 3238785555U)
						{
							if (num != 3241480638U)
							{
								return result;
							}
							if (!(keyID == "PageDown"))
							{
								return result;
							}
							return result;
						}
						else if (!(keyID == "D"))
						{
							return result;
						}
					}
					else if (num <= 3255563174U)
					{
						if (num != 3250860581U)
						{
							if (num != 3255563174U)
							{
								return result;
							}
							if (!(keyID == "G"))
							{
								return result;
							}
						}
						else if (!(keyID == "Space"))
						{
							return result;
						}
					}
					else if (num != 3272340793U)
					{
						if (num != 3289118412U)
						{
							if (num != 3294917732U)
							{
								return result;
							}
							if (!(keyID == "NumpadPlus"))
							{
								return result;
							}
							return "+";
						}
						else if (!(keyID == "A"))
						{
							return result;
						}
					}
					else if (!(keyID == "F"))
					{
						return result;
					}
				}
				else if (num <= 3373006507U)
				{
					if (num <= 3339451269U)
					{
						if (num != 3322673650U)
						{
							if (num != 3339451269U)
							{
								return result;
							}
							if (!(keyID == "B"))
							{
								return result;
							}
						}
						else if (!(keyID == "C"))
						{
							return result;
						}
					}
					else if (num != 3356228888U)
					{
						if (num != 3373006507U)
						{
							return result;
						}
						if (!(keyID == "L"))
						{
							return result;
						}
					}
					else if (!(keyID == "M"))
					{
						return result;
					}
				}
				else if (num <= 3388411298U)
				{
					if (num != 3388260431U)
					{
						if (num != 3388411298U)
						{
							return result;
						}
						if (!(keyID == "ControllerLOption"))
						{
							return result;
						}
						goto IL_1584;
					}
					else
					{
						if (!(keyID == "Minus"))
						{
							return result;
						}
						return result;
					}
				}
				else if (num != 3389784126U)
				{
					if (num != 3406561745U)
					{
						if (num != 3423339364U)
						{
							return result;
						}
						if (!(keyID == "I"))
						{
							return result;
						}
					}
					else if (!(keyID == "N"))
					{
						return result;
					}
				}
				else if (!(keyID == "O"))
				{
					return result;
				}
			}
			else if (num <= 3703400824U)
			{
				if (num <= 3540782697U)
				{
					if (num <= 3482547786U)
					{
						if (num <= 3456894602U)
						{
							if (num != 3440116983U)
							{
								if (num != 3456894602U)
								{
									return result;
								}
								if (!(keyID == "K"))
								{
									return result;
								}
							}
							else if (!(keyID == "H"))
							{
								return result;
							}
						}
						else if (num != 3473672221U)
						{
							if (num != 3482547786U)
							{
								return result;
							}
							if (!(keyID == "End"))
							{
								return result;
							}
							return result;
						}
						else if (!(keyID == "J"))
						{
							return result;
						}
					}
					else if (num <= 3490449840U)
					{
						if (num != 3485937324U)
						{
							if (num != 3490449840U)
							{
								return result;
							}
							if (!(keyID == "U"))
							{
								return result;
							}
						}
						else if (!(keyID == "ControllerRUp"))
						{
							return result;
						}
					}
					else if (num != 3507227459U)
					{
						if (num != 3524005078U)
						{
							if (num != 3540782697U)
							{
								return result;
							}
							if (!(keyID == "V"))
							{
								return result;
							}
						}
						else if (!(keyID == "W"))
						{
							return result;
						}
					}
					else if (!(keyID == "T"))
					{
						return result;
					}
				}
				else if (num <= 3585957491U)
				{
					if (num <= 3569179872U)
					{
						if (num != 3557560316U)
						{
							if (num != 3569179872U)
							{
								return result;
							}
							if (!(keyID == "F18"))
							{
								return result;
							}
						}
						else if (!(keyID == "Q"))
						{
							return result;
						}
					}
					else if (num != 3574337935U)
					{
						if (num != 3585957491U)
						{
							return result;
						}
						if (!(keyID == "F19"))
						{
							return result;
						}
					}
					else if (!(keyID == "P"))
					{
						return result;
					}
				}
				else if (num <= 3592460967U)
				{
					if (num != 3591115554U)
					{
						if (num != 3592460967U)
						{
							return result;
						}
						if (!(keyID == "RightShift"))
						{
							return result;
						}
						goto IL_1627;
					}
					else if (!(keyID == "S"))
					{
						return result;
					}
				}
				else if (num != 3607893173U)
				{
					if (num != 3691781268U)
					{
						if (num != 3703400824U)
						{
							return result;
						}
						if (!(keyID == "F10"))
						{
							return result;
						}
					}
					else if (!(keyID == "Y"))
					{
						return result;
					}
				}
				else if (!(keyID == "R"))
				{
					return result;
				}
			}
			else if (num <= 3787288919U)
			{
				if (num <= 3737177789U)
				{
					if (num <= 3720178443U)
					{
						if (num != 3708558887U)
						{
							if (num != 3720178443U)
							{
								return result;
							}
							if (!(keyID == "F11"))
							{
								return result;
							}
						}
						else if (!(keyID == "X"))
						{
							return result;
						}
					}
					else if (num != 3736956062U)
					{
						if (num != 3737177789U)
						{
							return result;
						}
						if (!(keyID == "MouseScrollDown"))
						{
							return result;
						}
					}
					else if (!(keyID == "F12"))
					{
						return result;
					}
				}
				else if (num <= 3742114125U)
				{
					if (num != 3737220883U)
					{
						if (num != 3742114125U)
						{
							return result;
						}
						if (!(keyID == "Z"))
						{
							return result;
						}
					}
					else if (!(keyID == "ControllerLRight"))
					{
						return result;
					}
				}
				else if (num != 3753733681U)
				{
					if (num != 3770511300U)
					{
						if (num != 3787288919U)
						{
							return result;
						}
						if (!(keyID == "F15"))
						{
							return result;
						}
					}
					else if (!(keyID == "F14"))
					{
						return result;
					}
				}
				else if (!(keyID == "F13"))
				{
					return result;
				}
			}
			else if (num <= 3862950033U)
			{
				if (num <= 3820844157U)
				{
					if (num != 3804066538U)
					{
						if (num != 3820844157U)
						{
							return result;
						}
						if (!(keyID == "F17"))
						{
							return result;
						}
					}
					else if (!(keyID == "F16"))
					{
						return result;
					}
				}
				else if (num != 3821858654U)
				{
					if (num != 3862950033U)
					{
						return result;
					}
					if (!(keyID == "ControllerRRight"))
					{
						return result;
					}
				}
				else
				{
					if (!(keyID == "SemiColon"))
					{
						return result;
					}
					return result;
				}
			}
			else if (num <= 3958280132U)
			{
				if (num != 3890594748U)
				{
					if (num != 3958280132U)
					{
						return result;
					}
					if (!(keyID == "ControllerShare"))
					{
						return result;
					}
					if (controllerType == Input.ControllerTypes.PlayStationDualShock)
					{
						return keyID.ToLower() + "_4";
					}
					return keyID.ToLower();
				}
				else
				{
					if (!(keyID == "NumpadMultiply"))
					{
						return result;
					}
					return result;
				}
			}
			else if (num != 4080261303U)
			{
				if (num != 4149056477U)
				{
					if (num != 4219689196U)
					{
						return result;
					}
					if (!(keyID == "Tab"))
					{
						return result;
					}
				}
				else
				{
					if (!(keyID == "NumpadSlash"))
					{
						return result;
					}
					return result;
				}
			}
			else
			{
				if (!(keyID == "BackSlash"))
				{
					return result;
				}
				return result;
			}
			IL_1578:
			return keyID.ToLower();
			IL_1584:
			if (controllerType == Input.ControllerTypes.PlayStationDualShock)
			{
				return keyID.ToLower() + "_4";
			}
			return keyID.ToLower();
			IL_1627:
			return "shift";
			IL_162F:
			return "control";
			IL_1637:
			return "alt";
			IL_1647:
			result = "controllerlstick";
			return result;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x0000566C File Offset: 0x0000386C
		private void SetKeyVisual(string visualName)
		{
			string text = this.IconsPath + "\\" + visualName;
			if (Input.GetPrimaryControllerType().IsPlaystation())
			{
				base.Sprite = base.Context.SpriteData.GetSprite(text + "_ps");
				return;
			}
			base.Sprite = base.Context.SpriteData.GetSprite(text);
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x000056D0 File Offset: 0x000038D0
		// (set) Token: 0x060000C6 RID: 198 RVA: 0x000056D8 File Offset: 0x000038D8
		public string KeyID
		{
			get
			{
				return this._keyID;
			}
			set
			{
				if (value != this._keyID)
				{
					this._keyID = value;
					this._visualName = this.GetKeyVisualName(value);
					this.SetKeyVisual(this._visualName);
				}
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00005708 File Offset: 0x00003908
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x00005710 File Offset: 0x00003910
		public string IconsPath
		{
			get
			{
				return this._iconsPath;
			}
			set
			{
				if (value != this._iconsPath)
				{
					this._iconsPath = value;
					this.SetKeyVisual(this._visualName);
				}
			}
		}

		// Token: 0x04000055 RID: 85
		private string _visualName = "None";

		// Token: 0x04000056 RID: 86
		private string _keyID;

		// Token: 0x04000057 RID: 87
		private string _iconsPath = "General\\InputKeys";
	}
}
