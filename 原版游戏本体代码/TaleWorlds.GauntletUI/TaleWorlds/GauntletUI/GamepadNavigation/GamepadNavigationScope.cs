using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.GamepadNavigation
{
	// Token: 0x0200004D RID: 77
	public class GamepadNavigationScope
	{
		// Token: 0x1700014A RID: 330
		// (get) Token: 0x06000485 RID: 1157 RVA: 0x00012728 File Offset: 0x00010928
		// (set) Token: 0x06000486 RID: 1158 RVA: 0x00012730 File Offset: 0x00010930
		public string ScopeID { get; set; } = "DefaultScopeID";

		// Token: 0x1700014B RID: 331
		// (get) Token: 0x06000487 RID: 1159 RVA: 0x00012739 File Offset: 0x00010939
		// (set) Token: 0x06000488 RID: 1160 RVA: 0x00012741 File Offset: 0x00010941
		public bool IsActiveScope { get; private set; }

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x06000489 RID: 1161 RVA: 0x0001274A File Offset: 0x0001094A
		// (set) Token: 0x0600048A RID: 1162 RVA: 0x00012752 File Offset: 0x00010952
		public bool DoNotAutomaticallyFindChildren { get; set; }

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x0600048B RID: 1163 RVA: 0x0001275B File Offset: 0x0001095B
		// (set) Token: 0x0600048C RID: 1164 RVA: 0x00012763 File Offset: 0x00010963
		public GamepadNavigationTypes ScopeMovements { get; set; }

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x0600048D RID: 1165 RVA: 0x0001276C File Offset: 0x0001096C
		// (set) Token: 0x0600048E RID: 1166 RVA: 0x00012774 File Offset: 0x00010974
		public GamepadNavigationTypes AlternateScopeMovements { get; set; }

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x0600048F RID: 1167 RVA: 0x0001277D File Offset: 0x0001097D
		// (set) Token: 0x06000490 RID: 1168 RVA: 0x00012785 File Offset: 0x00010985
		public int AlternateMovementStepSize { get; set; }

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x06000491 RID: 1169 RVA: 0x0001278E File Offset: 0x0001098E
		// (set) Token: 0x06000492 RID: 1170 RVA: 0x00012796 File Offset: 0x00010996
		public bool HasCircularMovement { get; set; }

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x06000493 RID: 1171 RVA: 0x0001279F File Offset: 0x0001099F
		public ReadOnlyCollection<Widget> NavigatableWidgets { get; }

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x06000494 RID: 1172 RVA: 0x000127A7 File Offset: 0x000109A7
		// (set) Token: 0x06000495 RID: 1173 RVA: 0x000127B0 File Offset: 0x000109B0
		public Widget ParentWidget
		{
			get
			{
				return this._parentWidget;
			}
			set
			{
				if (value != this._parentWidget)
				{
					if (this._parentWidget != null)
					{
						this._invisibleParents.Clear();
						for (Widget parentWidget = this._parentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
						{
							parentWidget.OnVisibilityChanged -= this.OnParentVisibilityChanged;
						}
					}
					this._parentWidget = value;
					for (Widget parentWidget2 = this._parentWidget; parentWidget2 != null; parentWidget2 = parentWidget2.ParentWidget)
					{
						if (!parentWidget2.IsVisible)
						{
							this._invisibleParents.Add(parentWidget2);
						}
						parentWidget2.OnVisibilityChanged += this.OnParentVisibilityChanged;
					}
				}
			}
		}

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x06000496 RID: 1174 RVA: 0x0001283E File Offset: 0x00010A3E
		// (set) Token: 0x06000497 RID: 1175 RVA: 0x00012846 File Offset: 0x00010A46
		public int LatestNavigationElementIndex { get; set; }

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x06000498 RID: 1176 RVA: 0x0001284F File Offset: 0x00010A4F
		// (set) Token: 0x06000499 RID: 1177 RVA: 0x00012857 File Offset: 0x00010A57
		public bool DoNotAutoGainNavigationOnInit { get; set; }

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x0600049A RID: 1178 RVA: 0x00012860 File Offset: 0x00010A60
		// (set) Token: 0x0600049B RID: 1179 RVA: 0x00012868 File Offset: 0x00010A68
		public bool ForceGainNavigationBasedOnDirection { get; set; }

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x0600049C RID: 1180 RVA: 0x00012871 File Offset: 0x00010A71
		// (set) Token: 0x0600049D RID: 1181 RVA: 0x00012879 File Offset: 0x00010A79
		public bool ForceGainNavigationOnClosestChild { get; set; }

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x00012882 File Offset: 0x00010A82
		// (set) Token: 0x0600049F RID: 1183 RVA: 0x0001288A File Offset: 0x00010A8A
		public bool ForceGainNavigationOnFirstChild { get; set; }

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x00012893 File Offset: 0x00010A93
		// (set) Token: 0x060004A1 RID: 1185 RVA: 0x0001289B File Offset: 0x00010A9B
		public bool NavigateFromScopeEdges { get; set; }

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x060004A2 RID: 1186 RVA: 0x000128A4 File Offset: 0x00010AA4
		// (set) Token: 0x060004A3 RID: 1187 RVA: 0x000128AC File Offset: 0x00010AAC
		public bool UseDiscoveryAreaAsScopeEdges { get; set; }

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x060004A4 RID: 1188 RVA: 0x000128B5 File Offset: 0x00010AB5
		// (set) Token: 0x060004A5 RID: 1189 RVA: 0x000128BD File Offset: 0x00010ABD
		public bool DoNotAutoNavigateAfterSort { get; set; }

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x060004A6 RID: 1190 RVA: 0x000128C6 File Offset: 0x00010AC6
		// (set) Token: 0x060004A7 RID: 1191 RVA: 0x000128CE File Offset: 0x00010ACE
		public bool FollowMobileTargets { get; set; }

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x060004A8 RID: 1192 RVA: 0x000128D7 File Offset: 0x00010AD7
		// (set) Token: 0x060004A9 RID: 1193 RVA: 0x000128DF File Offset: 0x00010ADF
		public bool DoNotAutoCollectChildScopes { get; set; }

		// Token: 0x1700015D RID: 349
		// (get) Token: 0x060004AA RID: 1194 RVA: 0x000128E8 File Offset: 0x00010AE8
		// (set) Token: 0x060004AB RID: 1195 RVA: 0x000128F0 File Offset: 0x00010AF0
		public bool IsDefaultNavigationScope { get; set; }

		// Token: 0x1700015E RID: 350
		// (get) Token: 0x060004AC RID: 1196 RVA: 0x000128F9 File Offset: 0x00010AF9
		// (set) Token: 0x060004AD RID: 1197 RVA: 0x00012901 File Offset: 0x00010B01
		public float ExtendDiscoveryAreaRight { get; set; }

		// Token: 0x1700015F RID: 351
		// (get) Token: 0x060004AE RID: 1198 RVA: 0x0001290A File Offset: 0x00010B0A
		// (set) Token: 0x060004AF RID: 1199 RVA: 0x00012912 File Offset: 0x00010B12
		public float ExtendDiscoveryAreaTop { get; set; }

		// Token: 0x17000160 RID: 352
		// (get) Token: 0x060004B0 RID: 1200 RVA: 0x0001291B File Offset: 0x00010B1B
		// (set) Token: 0x060004B1 RID: 1201 RVA: 0x00012923 File Offset: 0x00010B23
		public float ExtendDiscoveryAreaBottom { get; set; }

		// Token: 0x17000161 RID: 353
		// (get) Token: 0x060004B2 RID: 1202 RVA: 0x0001292C File Offset: 0x00010B2C
		// (set) Token: 0x060004B3 RID: 1203 RVA: 0x00012934 File Offset: 0x00010B34
		public float ExtendDiscoveryAreaLeft { get; set; }

		// Token: 0x17000162 RID: 354
		// (get) Token: 0x060004B4 RID: 1204 RVA: 0x0001293D File Offset: 0x00010B3D
		// (set) Token: 0x060004B5 RID: 1205 RVA: 0x00012948 File Offset: 0x00010B48
		public float ExtendChildrenCursorAreaLeft
		{
			get
			{
				return this._extendChildrenCursorAreaLeft;
			}
			set
			{
				if (value != this._extendChildrenCursorAreaLeft)
				{
					this._extendChildrenCursorAreaLeft = value;
					for (int i = 0; i < this._navigatableWidgets.Count; i++)
					{
						this._navigatableWidgets[i].ExtendCursorAreaLeft = value;
					}
				}
			}
		}

		// Token: 0x17000163 RID: 355
		// (get) Token: 0x060004B6 RID: 1206 RVA: 0x0001298D File Offset: 0x00010B8D
		// (set) Token: 0x060004B7 RID: 1207 RVA: 0x00012998 File Offset: 0x00010B98
		public float ExtendChildrenCursorAreaRight
		{
			get
			{
				return this._extendChildrenCursorAreaRight;
			}
			set
			{
				if (value != this._extendChildrenCursorAreaRight)
				{
					this._extendChildrenCursorAreaRight = value;
					for (int i = 0; i < this._navigatableWidgets.Count; i++)
					{
						this._navigatableWidgets[i].ExtendCursorAreaRight = value;
					}
				}
			}
		}

		// Token: 0x17000164 RID: 356
		// (get) Token: 0x060004B8 RID: 1208 RVA: 0x000129DD File Offset: 0x00010BDD
		// (set) Token: 0x060004B9 RID: 1209 RVA: 0x000129E8 File Offset: 0x00010BE8
		public float ExtendChildrenCursorAreaTop
		{
			get
			{
				return this._extendChildrenCursorAreaTop;
			}
			set
			{
				if (value != this._extendChildrenCursorAreaTop)
				{
					this._extendChildrenCursorAreaTop = value;
					for (int i = 0; i < this._navigatableWidgets.Count; i++)
					{
						this._navigatableWidgets[i].ExtendCursorAreaTop = value;
					}
				}
			}
		}

		// Token: 0x17000165 RID: 357
		// (get) Token: 0x060004BA RID: 1210 RVA: 0x00012A2D File Offset: 0x00010C2D
		// (set) Token: 0x060004BB RID: 1211 RVA: 0x00012A38 File Offset: 0x00010C38
		public float ExtendChildrenCursorAreaBottom
		{
			get
			{
				return this._extendChildrenCursorAreaBottom;
			}
			set
			{
				if (value != this._extendChildrenCursorAreaBottom)
				{
					this._extendChildrenCursorAreaBottom = value;
					for (int i = 0; i < this._navigatableWidgets.Count; i++)
					{
						this._navigatableWidgets[i].ExtendCursorAreaBottom = value;
					}
				}
			}
		}

		// Token: 0x17000166 RID: 358
		// (get) Token: 0x060004BC RID: 1212 RVA: 0x00012A7D File Offset: 0x00010C7D
		// (set) Token: 0x060004BD RID: 1213 RVA: 0x00012A85 File Offset: 0x00010C85
		public float DiscoveryAreaOffsetX { get; set; }

		// Token: 0x17000167 RID: 359
		// (get) Token: 0x060004BE RID: 1214 RVA: 0x00012A8E File Offset: 0x00010C8E
		// (set) Token: 0x060004BF RID: 1215 RVA: 0x00012A96 File Offset: 0x00010C96
		public float DiscoveryAreaOffsetY { get; set; }

		// Token: 0x17000168 RID: 360
		// (get) Token: 0x060004C0 RID: 1216 RVA: 0x00012A9F File Offset: 0x00010C9F
		// (set) Token: 0x060004C1 RID: 1217 RVA: 0x00012AA7 File Offset: 0x00010CA7
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					this.IsDisabled = !value;
					Action<GamepadNavigationScope> onNavigatableWidgetsChanged = this.OnNavigatableWidgetsChanged;
					if (onNavigatableWidgetsChanged == null)
					{
						return;
					}
					onNavigatableWidgetsChanged(this);
				}
			}
		}

		// Token: 0x17000169 RID: 361
		// (get) Token: 0x060004C2 RID: 1218 RVA: 0x00012AD4 File Offset: 0x00010CD4
		// (set) Token: 0x060004C3 RID: 1219 RVA: 0x00012ADC File Offset: 0x00010CDC
		public bool IsDisabled
		{
			get
			{
				return this._isDisabled;
			}
			set
			{
				if (value != this._isDisabled)
				{
					this._isDisabled = value;
					this.IsEnabled = !value;
				}
			}
		}

		// Token: 0x1700016A RID: 362
		// (get) Token: 0x060004C4 RID: 1220 RVA: 0x00012AF8 File Offset: 0x00010CF8
		// (set) Token: 0x060004C5 RID: 1221 RVA: 0x00012B06 File Offset: 0x00010D06
		public string UpNavigationScopeID
		{
			get
			{
				return this.ManualScopeIDs[GamepadNavigationTypes.Up];
			}
			set
			{
				this.ManualScopeIDs[GamepadNavigationTypes.Up] = value;
			}
		}

		// Token: 0x1700016B RID: 363
		// (get) Token: 0x060004C6 RID: 1222 RVA: 0x00012B15 File Offset: 0x00010D15
		// (set) Token: 0x060004C7 RID: 1223 RVA: 0x00012B23 File Offset: 0x00010D23
		public string RightNavigationScopeID
		{
			get
			{
				return this.ManualScopeIDs[GamepadNavigationTypes.Right];
			}
			set
			{
				this.ManualScopeIDs[GamepadNavigationTypes.Right] = value;
			}
		}

		// Token: 0x1700016C RID: 364
		// (get) Token: 0x060004C8 RID: 1224 RVA: 0x00012B32 File Offset: 0x00010D32
		// (set) Token: 0x060004C9 RID: 1225 RVA: 0x00012B40 File Offset: 0x00010D40
		public string DownNavigationScopeID
		{
			get
			{
				return this.ManualScopeIDs[GamepadNavigationTypes.Down];
			}
			set
			{
				this.ManualScopeIDs[GamepadNavigationTypes.Down] = value;
			}
		}

		// Token: 0x1700016D RID: 365
		// (get) Token: 0x060004CA RID: 1226 RVA: 0x00012B4F File Offset: 0x00010D4F
		// (set) Token: 0x060004CB RID: 1227 RVA: 0x00012B5D File Offset: 0x00010D5D
		public string LeftNavigationScopeID
		{
			get
			{
				return this.ManualScopeIDs[GamepadNavigationTypes.Left];
			}
			set
			{
				this.ManualScopeIDs[GamepadNavigationTypes.Left] = value;
			}
		}

		// Token: 0x1700016E RID: 366
		// (get) Token: 0x060004CC RID: 1228 RVA: 0x00012B6C File Offset: 0x00010D6C
		// (set) Token: 0x060004CD RID: 1229 RVA: 0x00012B7A File Offset: 0x00010D7A
		public GamepadNavigationScope UpNavigationScope
		{
			get
			{
				return this.ManualScopes[GamepadNavigationTypes.Up];
			}
			set
			{
				this.ManualScopes[GamepadNavigationTypes.Up] = value;
			}
		}

		// Token: 0x1700016F RID: 367
		// (get) Token: 0x060004CE RID: 1230 RVA: 0x00012B89 File Offset: 0x00010D89
		// (set) Token: 0x060004CF RID: 1231 RVA: 0x00012B97 File Offset: 0x00010D97
		public GamepadNavigationScope RightNavigationScope
		{
			get
			{
				return this.ManualScopes[GamepadNavigationTypes.Right];
			}
			set
			{
				this.ManualScopes[GamepadNavigationTypes.Right] = value;
			}
		}

		// Token: 0x17000170 RID: 368
		// (get) Token: 0x060004D0 RID: 1232 RVA: 0x00012BA6 File Offset: 0x00010DA6
		// (set) Token: 0x060004D1 RID: 1233 RVA: 0x00012BB4 File Offset: 0x00010DB4
		public GamepadNavigationScope DownNavigationScope
		{
			get
			{
				return this.ManualScopes[GamepadNavigationTypes.Down];
			}
			set
			{
				this.ManualScopes[GamepadNavigationTypes.Down] = value;
			}
		}

		// Token: 0x17000171 RID: 369
		// (get) Token: 0x060004D2 RID: 1234 RVA: 0x00012BC3 File Offset: 0x00010DC3
		// (set) Token: 0x060004D3 RID: 1235 RVA: 0x00012BD1 File Offset: 0x00010DD1
		public GamepadNavigationScope LeftNavigationScope
		{
			get
			{
				return this.ManualScopes[GamepadNavigationTypes.Left];
			}
			set
			{
				this.ManualScopes[GamepadNavigationTypes.Left] = value;
			}
		}

		// Token: 0x17000172 RID: 370
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x00012BE0 File Offset: 0x00010DE0
		internal Widget LastNavigatedWidget
		{
			get
			{
				if (this.LatestNavigationElementIndex >= 0 && this.LatestNavigationElementIndex < this._navigatableWidgets.Count)
				{
					return this._navigatableWidgets[this.LatestNavigationElementIndex];
				}
				return null;
			}
		}

		// Token: 0x17000173 RID: 371
		// (get) Token: 0x060004D5 RID: 1237 RVA: 0x00012C11 File Offset: 0x00010E11
		// (set) Token: 0x060004D6 RID: 1238 RVA: 0x00012C19 File Offset: 0x00010E19
		internal bool IsInitialized { get; private set; }

		// Token: 0x17000174 RID: 372
		// (get) Token: 0x060004D7 RID: 1239 RVA: 0x00012C22 File Offset: 0x00010E22
		// (set) Token: 0x060004D8 RID: 1240 RVA: 0x00012C2A File Offset: 0x00010E2A
		internal GamepadNavigationScope PreviousScope { get; set; }

		// Token: 0x17000175 RID: 373
		// (get) Token: 0x060004D9 RID: 1241 RVA: 0x00012C33 File Offset: 0x00010E33
		// (set) Token: 0x060004DA RID: 1242 RVA: 0x00012C3B File Offset: 0x00010E3B
		internal Dictionary<GamepadNavigationTypes, string> ManualScopeIDs { get; private set; }

		// Token: 0x17000176 RID: 374
		// (get) Token: 0x060004DB RID: 1243 RVA: 0x00012C44 File Offset: 0x00010E44
		// (set) Token: 0x060004DC RID: 1244 RVA: 0x00012C4C File Offset: 0x00010E4C
		internal Dictionary<GamepadNavigationTypes, GamepadNavigationScope> ManualScopes { get; private set; }

		// Token: 0x17000177 RID: 375
		// (get) Token: 0x060004DD RID: 1245 RVA: 0x00012C55 File Offset: 0x00010E55
		// (set) Token: 0x060004DE RID: 1246 RVA: 0x00012C5D File Offset: 0x00010E5D
		internal bool IsAdditionalMovementsDirty { get; set; }

		// Token: 0x17000178 RID: 376
		// (get) Token: 0x060004DF RID: 1247 RVA: 0x00012C66 File Offset: 0x00010E66
		// (set) Token: 0x060004E0 RID: 1248 RVA: 0x00012C6E File Offset: 0x00010E6E
		internal Dictionary<GamepadNavigationTypes, GamepadNavigationScope> InterScopeMovements { get; private set; }

		// Token: 0x17000179 RID: 377
		// (get) Token: 0x060004E1 RID: 1249 RVA: 0x00012C77 File Offset: 0x00010E77
		// (set) Token: 0x060004E2 RID: 1250 RVA: 0x00012C7F File Offset: 0x00010E7F
		internal GamepadNavigationScope ParentScope { get; private set; }

		// Token: 0x1700017A RID: 378
		// (get) Token: 0x060004E3 RID: 1251 RVA: 0x00012C88 File Offset: 0x00010E88
		// (set) Token: 0x060004E4 RID: 1252 RVA: 0x00012C90 File Offset: 0x00010E90
		internal ReadOnlyCollection<GamepadNavigationScope> ChildScopes { get; private set; }

		// Token: 0x060004E5 RID: 1253 RVA: 0x00012C9C File Offset: 0x00010E9C
		public GamepadNavigationScope()
		{
			this._widgetIndices = new Dictionary<Widget, int>();
			this._navigatableWidgets = new List<Widget>();
			this.NavigatableWidgets = new ReadOnlyCollection<Widget>(this._navigatableWidgets);
			this._invisibleParents = new List<Widget>();
			this.InterScopeMovements = new Dictionary<GamepadNavigationTypes, GamepadNavigationScope>
			{
				{
					GamepadNavigationTypes.Up,
					null
				},
				{
					GamepadNavigationTypes.Right,
					null
				},
				{
					GamepadNavigationTypes.Down,
					null
				},
				{
					GamepadNavigationTypes.Left,
					null
				}
			};
			this.ManualScopeIDs = new Dictionary<GamepadNavigationTypes, string>
			{
				{
					GamepadNavigationTypes.Up,
					null
				},
				{
					GamepadNavigationTypes.Right,
					null
				},
				{
					GamepadNavigationTypes.Down,
					null
				},
				{
					GamepadNavigationTypes.Left,
					null
				}
			};
			this.ManualScopes = new Dictionary<GamepadNavigationTypes, GamepadNavigationScope>
			{
				{
					GamepadNavigationTypes.Up,
					null
				},
				{
					GamepadNavigationTypes.Right,
					null
				},
				{
					GamepadNavigationTypes.Down,
					null
				},
				{
					GamepadNavigationTypes.Left,
					null
				}
			};
			this._navigatableWidgetComparer = new GamepadNavigationScope.WidgetNavigationIndexComparer();
			this.LatestNavigationElementIndex = -1;
			this._childScopes = new List<GamepadNavigationScope>();
			this.ChildScopes = new ReadOnlyCollection<GamepadNavigationScope>(this._childScopes);
			this.IsInitialized = false;
			this.IsEnabled = true;
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x00012DAC File Offset: 0x00010FAC
		public void AddWidgetAtIndex(Widget widget, int index)
		{
			if (index < this._navigatableWidgets.Count)
			{
				this._navigatableWidgets.Insert(index, widget);
				this._widgetIndices.Add(widget, index);
			}
			else
			{
				this._navigatableWidgets.Add(widget);
				this._widgetIndices.Add(widget, this._navigatableWidgets.Count - 1);
			}
			Action<GamepadNavigationScope> onNavigatableWidgetsChanged = this.OnNavigatableWidgetsChanged;
			if (onNavigatableWidgetsChanged != null)
			{
				onNavigatableWidgetsChanged(this);
			}
			this.SetCursorAreaExtensionsForChild(widget);
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x00012E21 File Offset: 0x00011021
		public void AddWidget(Widget widget)
		{
			this._navigatableWidgets.Add(widget);
			Action<GamepadNavigationScope> onNavigatableWidgetsChanged = this.OnNavigatableWidgetsChanged;
			if (onNavigatableWidgetsChanged != null)
			{
				onNavigatableWidgetsChanged(this);
			}
			this.SetCursorAreaExtensionsForChild(widget);
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x00012E48 File Offset: 0x00011048
		public void RemoveWidget(Widget widget)
		{
			this._navigatableWidgets.Remove(widget);
			Action<GamepadNavigationScope> onNavigatableWidgetsChanged = this.OnNavigatableWidgetsChanged;
			if (onNavigatableWidgetsChanged == null)
			{
				return;
			}
			onNavigatableWidgetsChanged(this);
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x00012E68 File Offset: 0x00011068
		public void SetParentScope(GamepadNavigationScope scope)
		{
			if (this.ParentScope != null)
			{
				this.ParentScope._childScopes.Remove(this);
			}
			GamepadNavigationScope parentScope = this.ParentScope;
			this.ParentScope = scope;
			Action<GamepadNavigationScope, GamepadNavigationScope> onParentScopeChanged = this.OnParentScopeChanged;
			if (onParentScopeChanged != null)
			{
				onParentScopeChanged(parentScope, this.ParentScope);
			}
			if (this.ParentScope != null)
			{
				this.ParentScope._childScopes.Add(this);
				this.ClearMyWidgetsFromParentScope();
			}
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x00012ED4 File Offset: 0x000110D4
		internal void SetIsActiveScope(bool isActive)
		{
			this.IsActiveScope = isActive;
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x00012EDD File Offset: 0x000110DD
		internal bool IsVisible()
		{
			return this._invisibleParents.Count == 0;
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x00012EED File Offset: 0x000110ED
		internal bool IsAvailable()
		{
			return this.IsEnabled && this._navigatableWidgets.Count > 0 && this.IsVisible();
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x00012F0D File Offset: 0x0001110D
		internal void Initialize()
		{
			if (!this.DoNotAutomaticallyFindChildren)
			{
				this.FindNavigatableChildren();
			}
			this.IsInitialized = true;
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x00012F24 File Offset: 0x00011124
		internal void RefreshNavigatableChildren()
		{
			if (this.IsInitialized)
			{
				this.FindNavigatableChildren();
			}
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x00012F34 File Offset: 0x00011134
		internal bool HasMovement(GamepadNavigationTypes movement)
		{
			return (this.ScopeMovements & movement) != GamepadNavigationTypes.None || (this.AlternateScopeMovements & movement) > GamepadNavigationTypes.None;
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x00012F4D File Offset: 0x0001114D
		private void FindNavigatableChildren()
		{
			this._navigatableWidgets.Clear();
			if (this.IsParentWidgetAvailableForNavigation())
			{
				this.CollectNavigatableChildrenOfWidget(this.ParentWidget);
			}
			Action<GamepadNavigationScope> onNavigatableWidgetsChanged = this.OnNavigatableWidgetsChanged;
			if (onNavigatableWidgetsChanged == null)
			{
				return;
			}
			onNavigatableWidgetsChanged(this);
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x00012F80 File Offset: 0x00011180
		private bool IsParentWidgetAvailableForNavigation()
		{
			for (Widget parentWidget = this.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
			{
				if (parentWidget.DoNotAcceptNavigation)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x00012FAC File Offset: 0x000111AC
		private void CollectNavigatableChildrenOfWidget(Widget widget)
		{
			if (widget.DoNotAcceptNavigation)
			{
				return;
			}
			for (int i = 0; i < this._childScopes.Count; i++)
			{
				if (this._childScopes[i].ParentWidget == widget)
				{
					return;
				}
			}
			if (widget.GamepadNavigationIndex != -1)
			{
				this._navigatableWidgets.Add(widget);
			}
			List<GamepadNavigationScope> list;
			if (!this.DoNotAutoCollectChildScopes && this.ParentWidget != widget && GauntletGamepadNavigationManager.Instance.NavigationScopeParents.TryGetValue(widget, out list))
			{
				for (int j = 0; j < list.Count; j++)
				{
					list[j].SetParentScope(this);
				}
			}
			for (int k = 0; k < widget.Children.Count; k++)
			{
				this.CollectNavigatableChildrenOfWidget(widget.Children[k]);
			}
			this.ClearMyWidgetsFromParentScope();
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x00013074 File Offset: 0x00011274
		internal GamepadNavigationTypes GetMovementsToReachMyPosition(Vector2 fromPosition)
		{
			SimpleRectangle rectangle = this.GetRectangle();
			GamepadNavigationTypes gamepadNavigationTypes = GamepadNavigationTypes.None;
			if (fromPosition.X > rectangle.X + rectangle.Width)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Left;
			}
			else if (fromPosition.X < rectangle.X)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Right;
			}
			if (fromPosition.Y > rectangle.Y + rectangle.Height)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Up;
			}
			else if (fromPosition.Y < rectangle.Y)
			{
				gamepadNavigationTypes |= GamepadNavigationTypes.Down;
			}
			return gamepadNavigationTypes;
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x000130E7 File Offset: 0x000112E7
		internal bool GetShouldFindScopeByPosition(GamepadNavigationTypes movement)
		{
			return this.ManualScopeIDs[movement] == null && this.ManualScopes[movement] == null;
		}

		// Token: 0x060004F5 RID: 1269 RVA: 0x00013108 File Offset: 0x00011308
		internal GamepadNavigationTypes GetMovementsInsideScope()
		{
			GamepadNavigationTypes gamepadNavigationTypes = this.ScopeMovements;
			GamepadNavigationTypes gamepadNavigationTypes2 = this.AlternateScopeMovements;
			if (!this.HasCircularMovement || this._navigatableWidgets.Count == 1)
			{
				bool flag = false;
				bool flag2 = false;
				if (this.LatestNavigationElementIndex >= 0 && this.LatestNavigationElementIndex < this._navigatableWidgets.Count)
				{
					for (int i = this.LatestNavigationElementIndex + 1; i < this._navigatableWidgets.Count; i++)
					{
						if (this.IsWidgetVisible(this._navigatableWidgets[i]))
						{
							flag2 = true;
							break;
						}
					}
					int num = this.LatestNavigationElementIndex - 1;
					if (this.HasCircularMovement && num < 0)
					{
						num = this._navigatableWidgets.Count - 1;
					}
					for (int j = num; j >= 0; j--)
					{
						if (this.IsWidgetVisible(this._navigatableWidgets[j]))
						{
							flag = true;
							break;
						}
					}
				}
				if (this.LatestNavigationElementIndex == 0 || !flag)
				{
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Left;
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Up;
				}
				if (this.LatestNavigationElementIndex == this.NavigatableWidgets.Count - 1 || !flag2)
				{
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Right;
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Down;
				}
				if (gamepadNavigationTypes2 != GamepadNavigationTypes.None && this.AlternateMovementStepSize > 0)
				{
					if (this.LatestNavigationElementIndex % this.AlternateMovementStepSize == 0)
					{
						gamepadNavigationTypes &= ~GamepadNavigationTypes.Left;
						gamepadNavigationTypes &= ~GamepadNavigationTypes.Up;
					}
					if (this.LatestNavigationElementIndex % this.AlternateMovementStepSize == this.AlternateMovementStepSize - 1)
					{
						gamepadNavigationTypes &= ~GamepadNavigationTypes.Right;
						gamepadNavigationTypes &= ~GamepadNavigationTypes.Down;
					}
					if (this.LatestNavigationElementIndex - this.AlternateMovementStepSize < 0)
					{
						gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Up;
						gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Left;
					}
					int num2 = this._navigatableWidgets.Count % this.AlternateMovementStepSize;
					if (this._navigatableWidgets.Count > 0 && num2 == 0)
					{
						num2 = this.AlternateMovementStepSize;
					}
					if (this.LatestNavigationElementIndex + num2 > this._navigatableWidgets.Count - 1)
					{
						gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Right;
						gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Down;
					}
				}
			}
			return gamepadNavigationTypes | gamepadNavigationTypes2;
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x000132DC File Offset: 0x000114DC
		internal int FindIndexOfWidget(Widget widget)
		{
			int result;
			if (widget != null && this._navigatableWidgets.Count != 0 && this._widgetIndices.TryGetValue(widget, out result))
			{
				return result;
			}
			return -1;
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001330C File Offset: 0x0001150C
		internal void SortWidgets()
		{
			this._navigatableWidgets.Sort(this._navigatableWidgetComparer);
			this._widgetIndices.Clear();
			for (int i = 0; i < this._navigatableWidgets.Count; i++)
			{
				this._widgetIndices[this._navigatableWidgets[i]] = i;
			}
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x00013363 File Offset: 0x00011563
		public void ClearNavigatableWidgets()
		{
			this._navigatableWidgets.Clear();
			this._widgetIndices.Clear();
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x0001337C File Offset: 0x0001157C
		internal SimpleRectangle GetDiscoveryRectangle()
		{
			float customScale = this.ParentWidget.EventManager.Context.CustomScale;
			return new SimpleRectangle(this.DiscoveryAreaOffsetX + this.ParentWidget.GlobalPosition.X - this.ExtendDiscoveryAreaLeft * customScale, this.DiscoveryAreaOffsetY + this.ParentWidget.GlobalPosition.Y - this.ExtendDiscoveryAreaTop * customScale, this.ParentWidget.Size.X + (this.ExtendDiscoveryAreaLeft + this.ExtendDiscoveryAreaRight) * customScale, this.ParentWidget.Size.Y + (this.ExtendDiscoveryAreaTop + this.ExtendDiscoveryAreaBottom) * customScale);
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x00013424 File Offset: 0x00011624
		internal SimpleRectangle GetRectangle()
		{
			if (this.ParentWidget == null)
			{
				return new SimpleRectangle(0f, 0f, 1f, 1f);
			}
			return new SimpleRectangle(this.ParentWidget.GlobalPosition.X, this.ParentWidget.GlobalPosition.Y, this.ParentWidget.Size.X, this.ParentWidget.Size.Y);
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x00013498 File Offset: 0x00011698
		internal bool IsWidgetVisible(Widget widget)
		{
			for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
			{
				if (!widget2.IsVisible)
				{
					return false;
				}
				if (widget2 == this.ParentWidget)
				{
					return this.IsVisible();
				}
			}
			return true;
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x000134D0 File Offset: 0x000116D0
		internal Widget GetFirstAvailableWidget()
		{
			int num = -1;
			for (int i = 0; i < this._navigatableWidgets.Count; i++)
			{
				if (this.IsWidgetVisible(this._navigatableWidgets[i]))
				{
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				return this._navigatableWidgets[num];
			}
			return null;
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x00013520 File Offset: 0x00011720
		internal Widget GetLastAvailableWidget()
		{
			int num = -1;
			for (int i = this._navigatableWidgets.Count - 1; i >= 0; i--)
			{
				if (this.IsWidgetVisible(this._navigatableWidgets[i]))
				{
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				return this._navigatableWidgets[num];
			}
			return null;
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x00013571 File Offset: 0x00011771
		private int GetApproximatelyClosestWidgetIndexToPosition(Vector2 position, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			if (this._navigatableWidgets.Count <= 0)
			{
				distance = -1f;
				return -1;
			}
			if (this.AlternateMovementStepSize > 0)
			{
				return this.GetClosesWidgetIndexForWithAlternateMovement(position, out distance, movement, angleCheck);
			}
			return this.GetClosesWidgetIndexForRegular(position, out distance, movement, angleCheck);
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x000135AC File Offset: 0x000117AC
		internal Widget GetApproximatelyClosestWidgetToPosition(Vector2 position, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			float num;
			return this.GetApproximatelyClosestWidgetToPosition(position, out num, movement, angleCheck);
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x000135C4 File Offset: 0x000117C4
		internal Widget GetApproximatelyClosestWidgetToPosition(Vector2 position, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			int approximatelyClosestWidgetIndexToPosition = this.GetApproximatelyClosestWidgetIndexToPosition(position, out distance, movement, angleCheck);
			if (approximatelyClosestWidgetIndexToPosition != -1)
			{
				return this._navigatableWidgets[approximatelyClosestWidgetIndexToPosition];
			}
			return null;
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x000135F0 File Offset: 0x000117F0
		private void OnParentVisibilityChanged(Widget parent)
		{
			bool flag = this.IsVisible();
			if (!parent.IsVisible)
			{
				this._invisibleParents.Add(parent);
			}
			else
			{
				this._invisibleParents.Remove(parent);
			}
			bool flag2 = this.IsVisible();
			if (flag != flag2)
			{
				Action<GamepadNavigationScope, bool> onVisibilityChanged = this.OnVisibilityChanged;
				if (onVisibilityChanged == null)
				{
					return;
				}
				onVisibilityChanged(this, flag2);
			}
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00013644 File Offset: 0x00011844
		private void ClearMyWidgetsFromParentScope()
		{
			if (this.ParentScope != null)
			{
				for (int i = 0; i < this._navigatableWidgets.Count; i++)
				{
					this.ParentScope.RemoveWidget(this._navigatableWidgets[i]);
				}
			}
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x00013688 File Offset: 0x00011888
		private Vector2 GetRelativePositionRatio(Vector2 position)
		{
			float toValue = 0f;
			float fromValue = 0f;
			float toValue2 = 0f;
			float fromValue2 = 0f;
			for (int i = 0; i < this._navigatableWidgets.Count; i++)
			{
				if (this.IsWidgetVisible(this._navigatableWidgets[i]))
				{
					fromValue = this._navigatableWidgets[i].GlobalPosition.Y;
					fromValue2 = this._navigatableWidgets[i].GlobalPosition.X;
					break;
				}
			}
			for (int j = this._navigatableWidgets.Count - 1; j >= 0; j--)
			{
				if (this.IsWidgetVisible(this._navigatableWidgets[j]))
				{
					toValue = this._navigatableWidgets[j].GlobalPosition.Y + this._navigatableWidgets[j].Size.Y;
					toValue2 = this._navigatableWidgets[j].GlobalPosition.X + this._navigatableWidgets[j].Size.X;
					break;
				}
			}
			float num = Mathf.Clamp(GamepadNavigationScope.InverseLerp(fromValue2, toValue2, position.X), 0f, 1f);
			float num2 = Mathf.Clamp(GamepadNavigationScope.InverseLerp(fromValue, toValue, position.Y), 0f, 1f);
			return new Vector2(num, num2);
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x000137E8 File Offset: 0x000119E8
		private bool IsPositionAvailableForMovement(Vector2 fromPos, Vector2 toPos, GamepadNavigationTypes movement)
		{
			if (movement == GamepadNavigationTypes.Right)
			{
				return fromPos.X <= toPos.X;
			}
			if (movement == GamepadNavigationTypes.Left)
			{
				return fromPos.X >= toPos.X;
			}
			if (movement == GamepadNavigationTypes.Up)
			{
				return fromPos.Y >= toPos.Y;
			}
			return movement != GamepadNavigationTypes.Down || fromPos.Y <= toPos.Y;
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00013850 File Offset: 0x00011A50
		private int GetClosesWidgetIndexForWithAlternateMovement(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			distance = -1f;
			List<int> list = new List<int>();
			Vector2 relativePositionRatio = this.GetRelativePositionRatio(fromPos);
			float num = float.MaxValue;
			int result = -1;
			SimpleRectangle rectangle = this.GetRectangle();
			if (!rectangle.IsPointInside(fromPos))
			{
				List<int> list2 = new List<int>();
				if (fromPos.X < rectangle.X)
				{
					for (int i = 0; i < this._navigatableWidgets.Count; i += this.AlternateMovementStepSize)
					{
						list2.Add(i);
					}
				}
				else if (fromPos.X > rectangle.X2)
				{
					for (int j = MathF.Min(this.AlternateMovementStepSize - 1, this._navigatableWidgets.Count - 1); j < this._navigatableWidgets.Count; j += this.AlternateMovementStepSize)
					{
						list2.Add(j);
					}
				}
				if (list2.Count > 0)
				{
					int[] targetIndicesFromListByRatio = GamepadNavigationScope.GetTargetIndicesFromListByRatio(relativePositionRatio.Y, list2);
					for (int k = 0; k < targetIndicesFromListByRatio.Length; k++)
					{
						list.Add(targetIndicesFromListByRatio[k]);
					}
				}
				if (fromPos.Y < rectangle.Y)
				{
					int endIndex = Mathf.Clamp(this.AlternateMovementStepSize - 1, 0, this._navigatableWidgets.Count - 1);
					int[] targetIndicesByRatio = GamepadNavigationScope.GetTargetIndicesByRatio(relativePositionRatio.X, 0, endIndex, 5);
					for (int l = 0; l < targetIndicesByRatio.Length; l++)
					{
						list.Add(targetIndicesByRatio[l]);
					}
				}
				else if (fromPos.Y > rectangle.Y2)
				{
					int num2 = this._navigatableWidgets.Count % this.AlternateMovementStepSize;
					if (this._navigatableWidgets.Count > 0 && num2 == 0)
					{
						num2 = this.AlternateMovementStepSize;
					}
					int startIndex = Mathf.Clamp(this._navigatableWidgets.Count - num2, 0, this._navigatableWidgets.Count - 1);
					int[] targetIndicesByRatio2 = GamepadNavigationScope.GetTargetIndicesByRatio(relativePositionRatio.X, startIndex, this._navigatableWidgets.Count - 1, 5);
					for (int m = 0; m < targetIndicesByRatio2.Length; m++)
					{
						list.Add(targetIndicesByRatio2[m]);
					}
				}
				for (int n = 0; n < list.Count; n++)
				{
					int num3 = list[n];
					Vector2 toPos;
					float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(this._navigatableWidgets[num3], fromPos, movement, out toPos);
					if (distanceToClosestWidgetEdge < num && (!angleCheck || this.IsPositionAvailableForMovement(fromPos, toPos, movement)))
					{
						num = distanceToClosestWidgetEdge;
						distance = num;
						result = num3;
					}
				}
			}
			else
			{
				result = this.GetClosesWidgetIndexForRegular(fromPos, out distance, GamepadNavigationTypes.None, false);
			}
			return result;
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x00013AB8 File Offset: 0x00011CB8
		private int GetClosestWidgetIndexForRegularInefficient(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			distance = -1f;
			int result = -1;
			float num = float.MaxValue;
			for (int i = 0; i < this._navigatableWidgets.Count; i++)
			{
				Vector2 toPos;
				float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(this._navigatableWidgets[i], fromPos, movement, out toPos);
				if (distanceToClosestWidgetEdge < num && this.IsWidgetVisible(this._navigatableWidgets[i]) && (!angleCheck || this.IsPositionAvailableForMovement(fromPos, toPos, movement)))
				{
					num = distanceToClosestWidgetEdge;
					distance = num;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x00013B30 File Offset: 0x00011D30
		private int GetClosesWidgetIndexForRegular(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
		{
			distance = -1f;
			List<int> list = new List<int>();
			Vector2 relativePositionRatio = this.GetRelativePositionRatio(fromPos);
			int[] targetIndicesByRatio = GamepadNavigationScope.GetTargetIndicesByRatio(relativePositionRatio.X, 0, this._navigatableWidgets.Count - 1, 5);
			int[] targetIndicesByRatio2 = GamepadNavigationScope.GetTargetIndicesByRatio(relativePositionRatio.Y, 0, this._navigatableWidgets.Count - 1, 5);
			for (int i = 0; i < targetIndicesByRatio.Length; i++)
			{
				if (!list.Contains(targetIndicesByRatio[i]))
				{
					list.Add(targetIndicesByRatio[i]);
				}
			}
			for (int j = 0; j < targetIndicesByRatio2.Length; j++)
			{
				if (!list.Contains(targetIndicesByRatio2[j]))
				{
					list.Add(targetIndicesByRatio2[j]);
				}
			}
			float num = float.MaxValue;
			int result = -1;
			int num2 = 0;
			for (int k = 0; k < list.Count; k++)
			{
				int num3 = list[k];
				if (num3 != -1 && this.IsWidgetVisible(this._navigatableWidgets[num3]))
				{
					num2++;
					Vector2 toPos;
					float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(this._navigatableWidgets[num3], fromPos, movement, out toPos);
					if (distanceToClosestWidgetEdge < num && (!angleCheck || this.IsPositionAvailableForMovement(fromPos, toPos, movement)))
					{
						num = distanceToClosestWidgetEdge;
						distance = num;
						result = num3;
					}
				}
			}
			if (num2 == 0)
			{
				return this.GetClosestWidgetIndexForRegularInefficient(fromPos, out distance, GamepadNavigationTypes.None, false);
			}
			return result;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x00013C69 File Offset: 0x00011E69
		private static float InverseLerp(float fromValue, float toValue, float value)
		{
			if (fromValue == toValue)
			{
				return 0f;
			}
			return (value - fromValue) / (toValue - fromValue);
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x00013C7C File Offset: 0x00011E7C
		private static int[] GetTargetIndicesFromListByRatio(float ratio, List<int> lookupIndices)
		{
			int num = MathF.Round((float)lookupIndices.Count * ratio);
			return new int[]
			{
				lookupIndices[Mathf.Clamp(num - 2, 0, lookupIndices.Count - 1)],
				lookupIndices[Mathf.Clamp(num - 1, 0, lookupIndices.Count - 1)],
				lookupIndices[Mathf.Clamp(num, 0, lookupIndices.Count - 1)],
				lookupIndices[Mathf.Clamp(num + 1, 0, lookupIndices.Count - 1)],
				lookupIndices[Mathf.Clamp(num + 2, 0, lookupIndices.Count - 1)]
			};
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00013D20 File Offset: 0x00011F20
		private static int[] GetTargetIndicesByRatio(float ratio, int startIndex, int endIndex, int arraySize = 5)
		{
			int num = MathF.Round((float)startIndex + (float)(endIndex - startIndex) * ratio);
			int[] array = new int[arraySize];
			int num2 = MathF.Floor((float)arraySize / 2f);
			for (int i = 0; i < arraySize; i++)
			{
				int num3 = -num2 + i;
				array[i] = Mathf.Clamp(num - num3, 0, endIndex);
			}
			return array;
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x00013D74 File Offset: 0x00011F74
		private void SetCursorAreaExtensionsForChild(Widget child)
		{
			if (this.ExtendChildrenCursorAreaLeft != 0f)
			{
				child.ExtendCursorAreaLeft = this.ExtendChildrenCursorAreaLeft;
			}
			if (this.ExtendChildrenCursorAreaRight != 0f)
			{
				child.ExtendCursorAreaRight = this.ExtendChildrenCursorAreaRight;
			}
			if (this.ExtendChildrenCursorAreaTop != 0f)
			{
				child.ExtendCursorAreaTop = this.ExtendChildrenCursorAreaTop;
			}
			if (this.ExtendChildrenCursorAreaBottom != 0f)
			{
				child.ExtendCursorAreaBottom = this.ExtendChildrenCursorAreaBottom;
			}
		}

		// Token: 0x04000238 RID: 568
		private List<Widget> _navigatableWidgets;

		// Token: 0x0400023A RID: 570
		private Dictionary<Widget, int> _widgetIndices;

		// Token: 0x0400023B RID: 571
		private Widget _parentWidget;

		// Token: 0x0400024B RID: 587
		private float _extendChildrenCursorAreaLeft;

		// Token: 0x0400024C RID: 588
		private float _extendChildrenCursorAreaRight;

		// Token: 0x0400024D RID: 589
		private float _extendChildrenCursorAreaTop;

		// Token: 0x0400024E RID: 590
		private float _extendChildrenCursorAreaBottom;

		// Token: 0x04000251 RID: 593
		private bool _isEnabled;

		// Token: 0x04000252 RID: 594
		private bool _isDisabled;

		// Token: 0x04000259 RID: 601
		private GamepadNavigationScope.WidgetNavigationIndexComparer _navigatableWidgetComparer;

		// Token: 0x0400025A RID: 602
		private List<Widget> _invisibleParents;

		// Token: 0x0400025C RID: 604
		private List<GamepadNavigationScope> _childScopes;

		// Token: 0x0400025E RID: 606
		internal Action<GamepadNavigationScope> OnNavigatableWidgetsChanged;

		// Token: 0x0400025F RID: 607
		internal Action<GamepadNavigationScope, bool> OnVisibilityChanged;

		// Token: 0x04000260 RID: 608
		internal Action<GamepadNavigationScope, GamepadNavigationScope> OnParentScopeChanged;

		// Token: 0x02000088 RID: 136
		private class WidgetNavigationIndexComparer : IComparer<Widget>
		{
			// Token: 0x060008FE RID: 2302 RVA: 0x000239A0 File Offset: 0x00021BA0
			public int Compare(Widget x, Widget y)
			{
				return x.GamepadNavigationIndex.CompareTo(y.GamepadNavigationIndex);
			}
		}
	}
}
