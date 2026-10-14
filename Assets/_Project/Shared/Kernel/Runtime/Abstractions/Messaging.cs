namespace CleanTime.Shared.Kernel
{
    /// <summary>Marker for a request that mutates state.</summary>
    public interface ICommand
    {
    }

    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        Result Handle(TCommand command);
    }

    /// <summary>Marker for a request that only reads state.</summary>
    public interface IQuery<TResult>
    {
    }

    public interface IQueryHandler<in TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        TResult Handle(TQuery query);
    }
}
