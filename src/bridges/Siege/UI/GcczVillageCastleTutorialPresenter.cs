using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Library;
using TaleWorlds.Localization;
namespace AnimusForge;
internal static class GcczVillageCastleTutorialPresenter
{
    internal static void Show(bool castle, int page = 0)
    {
        bool english = !GcczLocalizedResourceLoader.UseChineseInterface;
        var pages = castle ? GcczVillageCastleTutorial.Castle(english) : GcczVillageCastleTutorial.Village(english);
        string title = new TextObject(castle ? "{=AF_GCCZ_CastleTutorial}GCCZ castle tutorial" : "{=AF_GCCZ_VILLAGE_Tutorial}GCCZ village tutorial").ToString();
        InformationManager.ShowInquiry(new InquiryData(title + " " + (page + 1) + "/" + pages.Length, pages[page], true, page > 0,
            english ? (page + 1 < pages.Length ? "Next" : "Done") : (page + 1 < pages.Length ? "下一页" : "读完了"), english ? "Previous" : "上一页",
            page + 1 < pages.Length ? () => Show(castle, page + 1) : (System.Action)null, () => Show(castle, page - 1)), true);
    }
}
