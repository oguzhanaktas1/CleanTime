using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Domain
{
    public sealed class AffordabilityPurchasePolicy : IPurchasePolicy
    {
        public Result CanPurchase(Wallet wallet, Money price)
        {
            if (price.Cents <= 0)
                return Result.Failure(EconomyErrors.InvalidDebit, "Price must be positive.");

            return wallet.CanAfford(price)
                ? Result.Success()
                : Result.Failure(EconomyErrors.InsufficientFunds, "Not enough money.");
        }
    }
}
