using System;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// 仅负责游戏模型包装的装配，不复制模型计算规则。
/// 保持最后一个非 AF 模型作为 inner、默认模型兜底和逐项失败继续。
/// 只在 Campaign 注册时扫描 Models；不得移入 Tick 或加入全局模型缓存。
/// </summary>
internal static class CampaignModelComposition
{
    internal static void Register(CampaignGameStarter campaignGameStarter)
    {
        RegisterCourierFoodConsumptionModel(campaignGameStarter);
        RegisterCourierMobilePartyAiModel(campaignGameStarter);
        RegisterAnimusForgeSettlementAccessModel(campaignGameStarter);
        RegisterAnimusForgeSettlementLoyaltyModel(campaignGameStarter);
    }

    private static void RegisterCourierFoodConsumptionModel(CampaignGameStarter campaignGameStarter)
    {
        if (campaignGameStarter == null)
        {
            return;
        }
        try
        {
            MobilePartyFoodConsumptionModel inner = null;
            foreach (GameModel model in campaignGameStarter.Models)
            {
                if (model is MobilePartyFoodConsumptionModel foodModel && !(foodModel is CourierFoodConsumptionModel))
                {
                    inner = foodModel;
                }
            }
            inner ??= new DefaultMobilePartyFoodConsumptionModel();
            campaignGameStarter.AddModel<MobilePartyFoodConsumptionModel>(new CourierFoodConsumptionModel(inner));
            Logger.LogTrace("SubModule", ">>> Courier food consumption model registered.");
        }
        catch (Exception ex)
        {
            Logger.LogTrace("SubModule", ">>> Courier food consumption model registration failed: " + ex);
        }
    }

    private static void RegisterCourierMobilePartyAiModel(CampaignGameStarter campaignGameStarter)
    {
        if (campaignGameStarter == null)
        {
            return;
        }
        try
        {
            MobilePartyAIModel inner = null;
            foreach (GameModel model in campaignGameStarter.Models)
            {
                if (model is MobilePartyAIModel aiModel && !(aiModel is CourierMobilePartyAIModel))
                {
                    inner = aiModel;
                }
            }
            inner ??= new DefaultMobilePartyAIModel();
            campaignGameStarter.AddModel<MobilePartyAIModel>(new CourierMobilePartyAIModel(inner));
            Logger.LogTrace("SubModule", ">>> Courier mobile party AI model registered.");
        }
        catch (Exception ex)
        {
            Logger.LogTrace("SubModule", ">>> Courier mobile party AI model registration failed: " + ex);
        }
    }

    private static void RegisterAnimusForgeSettlementAccessModel(CampaignGameStarter campaignGameStarter)
    {
        if (campaignGameStarter == null)
        {
            return;
        }
        try
        {
            SettlementAccessModel inner = null;
            foreach (GameModel model in campaignGameStarter.Models)
            {
                if (model is SettlementAccessModel accessModel && !(accessModel is AnimusForgeSettlementAccessModel))
                {
                    inner = accessModel;
                }
            }
            inner ??= new DefaultSettlementAccessModel();
            campaignGameStarter.AddModel<SettlementAccessModel>(new AnimusForgeSettlementAccessModel(inner));
            Logger.LogTrace("SubModule", ">>> AnimusForge settlement access model registered.");
        }
        catch (Exception ex)
        {
            Logger.LogTrace("SubModule", ">>> AnimusForge settlement access model registration failed: " + ex);
        }
    }

    private static void RegisterAnimusForgeSettlementLoyaltyModel(CampaignGameStarter campaignGameStarter)
    {
        if (campaignGameStarter == null)
        {
            return;
        }
        try
        {
            SettlementLoyaltyModel inner = null;
            foreach (GameModel model in campaignGameStarter.Models)
            {
                if (model is SettlementLoyaltyModel loyaltyModel && !(loyaltyModel is AnimusForgeSettlementLoyaltyModel))
                {
                    inner = loyaltyModel;
                }
            }
            inner ??= new DefaultSettlementLoyaltyModel();
            campaignGameStarter.AddModel<SettlementLoyaltyModel>(new AnimusForgeSettlementLoyaltyModel(inner));
            Logger.LogTrace("SubModule", ">>> AnimusForge settlement loyalty model registered.");
        }
        catch (Exception ex)
        {
            Logger.LogTrace("SubModule", ">>> AnimusForge settlement loyalty model registration failed: " + ex);
        }
    }
}
