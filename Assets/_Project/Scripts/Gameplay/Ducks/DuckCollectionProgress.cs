namespace Rubber.Gameplay.Ducks
{
    public readonly struct DuckCollectionProgress
    {
        public DuckCollectionProgress(int returnedCount, int totalCount)
        {
            ReturnedCount = returnedCount;
            TotalCount = totalCount;
        }

        public int ReturnedCount { get; }
        public int TotalCount { get; }
        public int RemainingCount => System.Math.Max(0, TotalCount - ReturnedCount);
        public bool IsComplete => TotalCount > 0 && ReturnedCount >= TotalCount;
    }
}
