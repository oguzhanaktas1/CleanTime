using System.Collections.Generic;
using CleanTime.Economy.Application;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Application
{
    public sealed class QuestsModule : FeatureModule
    {
        public override string Name => "Quests";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IQuestObjectiveEvaluator>(_ => new DefaultQuestObjectiveEvaluator());

            services.RegisterSingleton<ICommandHandler<ActivateQuestCommand>>(r => new ActivateQuestHandler(
                r.Resolve<IQuestDefinitionProvider>(), r.Resolve<IQuestRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<ClaimQuestRewardCommand>>(r => new ClaimQuestRewardHandler(
                r.Resolve<IQuestRepository>(), r.Resolve<IWalletService>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestInstance>>>(
                r => new GetActiveQuestsHandler(r.Resolve<IQuestRepository>()));
            services.RegisterSingleton<IQueryHandler<GetCompletedQuestsQuery, IReadOnlyList<QuestInstance>>>(
                r => new GetCompletedQuestsHandler(r.Resolve<IQuestRepository>()));

            services.RegisterSingleton(r => new QuestProgressProjection(
                r.Resolve<IEventBus>(), r.Resolve<IQuestRepository>(), r.Resolve<IQuestObjectiveEvaluator>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<QuestProgressProjection>();
        }
    }
}
