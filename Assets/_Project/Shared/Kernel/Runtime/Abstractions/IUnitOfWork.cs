using System;

namespace CleanTime.Shared.Kernel
{
    public interface IUnitOfWork
    {
        /// <summary>Runs the action atomically. A failed Result or an exception rolls everything back.</summary>
        Result ExecuteInTransaction(Func<Result> action);
    }
}
