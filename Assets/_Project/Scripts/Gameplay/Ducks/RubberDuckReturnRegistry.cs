using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    public sealed class RubberDuckReturnRegistry : MonoBehaviour
    {
        [SerializeField] private List<RubberDuckData> collectionDucks = new();

        private readonly HashSet<int> collectionIds = new();
        private readonly HashSet<int> returnedIds = new();
        private bool collectionInitialized;
        private bool completionRaised;

        public int ReturnedCount => returnedIds.Count;
        public int TotalDuckCount
        {
            get
            {
                EnsureCollectionInitialized();
                return collectionIds.Count;
            }
        }
        public int RemainingCount => CurrentProgress.RemainingCount;
        public bool IsComplete => CurrentProgress.IsComplete;
        public DuckCollectionProgress CurrentProgress => new(ReturnedCount, TotalDuckCount);

        public event Action<RubberDuckData> DuckReturned;
        public event Action<DuckCollectionProgress> ProgressChanged;
        public event Action AllDucksReturned;

        private void Awake() => RebuildCollectionIds();

        public void Configure(IReadOnlyList<RubberDuckData> ducks)
        {
            collectionDucks.Clear();
            if (ducks != null)
            {
                for (int i = 0; i < ducks.Count; i++)
                    collectionDucks.Add(ducks[i]);
            }

            RebuildCollectionIds();
        }

        public bool TryRegister(RubberDuckInteractable duck)
        {
            EnsureCollectionInitialized();
            if (!duck || !duck.Data || duck.IsHeld || duck.IsReturned)
                return false;

            RubberDuckData data = duck.Data;
            if (!collectionIds.Contains(data.Id))
                return false;
            if (!returnedIds.Add(data.Id))
                return false;

            if (!duck.MarkReturned())
            {
                returnedIds.Remove(data.Id);
                return false;
            }

            DuckReturned?.Invoke(data);
            ProgressChanged?.Invoke(CurrentProgress);
            if (!completionRaised && IsComplete)
            {
                completionRaised = true;
                AllDucksReturned?.Invoke();
            }
            return true;
        }

        public bool IsReturned(int duckId) => returnedIds.Contains(duckId);

        private void RebuildCollectionIds()
        {
            collectionIds.Clear();
            for (int i = 0; i < collectionDucks.Count; i++)
            {
                RubberDuckData data = collectionDucks[i];
                if (data)
                    collectionIds.Add(data.Id);
            }
            collectionInitialized = true;
        }

        private void EnsureCollectionInitialized()
        {
            if (!collectionInitialized)
                RebuildCollectionIds();
        }
    }
}
