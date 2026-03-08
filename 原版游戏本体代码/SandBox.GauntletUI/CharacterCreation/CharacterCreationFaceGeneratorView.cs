using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.CharacterCreation
{
	// Token: 0x0200004B RID: 75
	[CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))]
	public class CharacterCreationFaceGeneratorView : CharacterCreationStageViewBase
	{
		// Token: 0x06000366 RID: 870 RVA: 0x00014440 File Offset: 0x00012640
		public CharacterCreationFaceGeneratorView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction, TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText, ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
			: base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
		{
			this._characterCreationManager = characterCreationManager;
			MBObjectManager objectManager = Game.Current.ObjectManager;
			string str = "player_char_creation_show_";
			CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
			string str2;
			if (playerCharacter == null)
			{
				str2 = null;
			}
			else
			{
				CultureObject culture = playerCharacter.Culture;
				str2 = ((culture != null) ? culture.StringId : null);
			}
			MBEquipmentRoster @object = objectManager.GetObject<MBEquipmentRoster>(str + str2);
			Equipment dressedEquipment = ((@object != null) ? @object.DefaultEquipment : null);
			this._faceGeneratorView = new BodyGeneratorView(new ControlCharacterCreationStage(this.NextStage), affirmativeActionText, new ControlCharacterCreationStage(this.PreviousStage), negativeActionText, CharacterObject.PlayerCharacter, false, null, dressedEquipment, getCurrentStageIndexAction, getTotalStageCountAction, getFurthestIndexAction, goToIndexAction, this._characterCreationManager.FaceGenHistory);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x000144ED File Offset: 0x000126ED
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._faceGeneratorView.OnFinalize();
			this._faceGeneratorView = null;
		}

		// Token: 0x06000368 RID: 872 RVA: 0x00014507 File Offset: 0x00012707
		public override IEnumerable<ScreenLayer> GetLayers()
		{
			return new List<ScreenLayer>
			{
				this._faceGeneratorView.SceneLayer,
				this._faceGeneratorView.GauntletLayer
			};
		}

		// Token: 0x06000369 RID: 873 RVA: 0x00014530 File Offset: 0x00012730
		public override void PreviousStage()
		{
			this._negativeAction();
		}

		// Token: 0x0600036A RID: 874 RVA: 0x0001453D File Offset: 0x0001273D
		public override void NextStage()
		{
			this._affirmativeAction();
		}

		// Token: 0x0600036B RID: 875 RVA: 0x0001454A File Offset: 0x0001274A
		public override void Tick(float dt)
		{
			this._faceGeneratorView.OnTick(dt);
		}

		// Token: 0x0600036C RID: 876 RVA: 0x00014558 File Offset: 0x00012758
		public override int GetVirtualStageCount()
		{
			return 1;
		}

		// Token: 0x0600036D RID: 877 RVA: 0x0001455B File Offset: 0x0001275B
		public override void GoToIndex(int index)
		{
			this._goToIndexAction(index);
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00014569 File Offset: 0x00012769
		public override void LoadEscapeMenuMovie()
		{
			this._escapeMenuDatasource = new EscapeMenuVM(base.GetEscapeMenuItems(this), null);
			this._escapeMenuMovie = this._faceGeneratorView.GauntletLayer.LoadMovie("EscapeMenu", this._escapeMenuDatasource);
		}

		// Token: 0x0600036F RID: 879 RVA: 0x0001459F File Offset: 0x0001279F
		public override void ReleaseEscapeMenuMovie()
		{
			this._faceGeneratorView.GauntletLayer.ReleaseMovie(this._escapeMenuMovie);
			this._escapeMenuDatasource = null;
			this._escapeMenuMovie = null;
		}

		// Token: 0x0400016D RID: 365
		private BodyGeneratorView _faceGeneratorView;

		// Token: 0x0400016E RID: 366
		private readonly CharacterCreationManager _characterCreationManager;

		// Token: 0x0400016F RID: 367
		private EscapeMenuVM _escapeMenuDatasource;

		// Token: 0x04000170 RID: 368
		private GauntletMovieIdentifier _escapeMenuMovie;
	}
}
