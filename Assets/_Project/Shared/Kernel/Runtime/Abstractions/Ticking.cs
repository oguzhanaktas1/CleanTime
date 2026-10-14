namespace CleanTime.Shared.Kernel
{
    public interface IFrameTickable
    {
        void Tick(float deltaTime);
    }

    public interface IFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }
}
