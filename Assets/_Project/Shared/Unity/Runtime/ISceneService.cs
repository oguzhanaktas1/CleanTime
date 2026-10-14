using CleanTime.Shared.Kernel;

namespace CleanTime.Shared.Unity
{
    /// <summary>
    /// Implemented by scene MonoBehaviours that need services. The scene lifetime scope calls
    /// <see cref="Construct"/> once the scene container exists; initialise in Construct, not Awake/Start.
    /// </summary>
    public interface ISceneService
    {
        void Construct(IServiceResolver services);
    }
}
