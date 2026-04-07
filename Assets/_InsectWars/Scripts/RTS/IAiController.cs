namespace InsectWars.RTS
{
    /// <summary>
    /// Hook for future strategic AI. Demo 0 uses <see cref="SimpleEnemyAi"/>.
    /// </summary>
    public interface IAiController
    {
        void Tick(float deltaTime);
    }
}
