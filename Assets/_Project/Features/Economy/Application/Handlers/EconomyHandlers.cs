using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    internal sealed class CreditMoneyHandler : ICommandHandler<CreditMoneyCommand>
    {
        private readonly IWalletService _wallet;

        public CreditMoneyHandler(IWalletService wallet)
        {
            _wallet = wallet;
        }

        public Result Handle(CreditMoneyCommand command) =>
            _wallet.Credit(command.Amount, command.Reason, command.ReferenceId);
    }

    internal sealed class DebitMoneyHandler : ICommandHandler<DebitMoneyCommand>
    {
        private readonly IWalletService _wallet;

        public DebitMoneyHandler(IWalletService wallet)
        {
            _wallet = wallet;
        }

        public Result Handle(DebitMoneyCommand command) =>
            _wallet.Debit(command.Amount, command.Reason, command.ReferenceId);
    }

    internal sealed class GetPlayerWalletHandler : IQueryHandler<GetPlayerWalletQuery, Money>
    {
        private readonly IWalletService _wallet;

        public GetPlayerWalletHandler(IWalletService wallet)
        {
            _wallet = wallet;
        }

        public Money Handle(GetPlayerWalletQuery query) => _wallet.Balance;
    }
}
