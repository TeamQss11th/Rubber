using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rubber.Gameplay.Ducks
{
    [DisallowMultipleComponent]
    public sealed class RubberDuckReturnRegistry : MonoBehaviour
    {
        private readonly HashSet<int> returnedIds = new();

        public int ReturnedCount => returnedIds.Count;

        public event Action<RubberDuckData> DuckReturned;

        public bool TryRegister(RubberDuckInteractable duck)
        {
            if (!duck || !duck.Data || duck.IsHeld || duck.IsReturned)
                return false;

            RubberDuckData data = duck.Data;
            if (!returnedIds.Add(data.Id))
                return false;

            if (!duck.MarkReturned())
            {
                returnedIds.Remove(data.Id);
                return false;
            }

            DuckReturned?.Invoke(data);
            return true;
        }

        public bool IsReturned(int duckId) => returnedIds.Contains(duckId);
    }
}
