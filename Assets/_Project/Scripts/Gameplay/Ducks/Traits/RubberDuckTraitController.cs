using System.Collections.Generic;
using UnityEngine;

namespace Rubber.Gameplay.Ducks.Traits
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RubberDuckInteractable), typeof(Rigidbody))]
    public sealed class RubberDuckTraitController : MonoBehaviour
    {
        private sealed class RuntimeTrait
        {
            public IRubberDuckTrait Trait { get; }
            public RubberDuckTraitExecution Execution { get; }
            public float ElapsedTime { get; set; }

            public RuntimeTrait(
                IRubberDuckTrait trait,
                RubberDuckTraitExecution execution)
            {
                Trait = trait;
                Execution = execution;
            }
        }

        private readonly List<RuntimeTrait> runtimeTraits = new();

        private RubberDuckInteractable duck;
        private bool initialized;

        public bool IsInitialized => initialized;

        private void Awake()
        {
            duck = GetComponent<RubberDuckInteractable>();
        }

        private void Start()
        {
            Initialize();
        }

        public bool Initialize()
        {
            if (initialized || !duck || !duck.Data)
                return false;

            var context = new RubberDuckTraitContext(
                duck,
                transform,
                GetComponent<Rigidbody>(),
                GetComponentsInChildren<Collider>(true),
                GetComponentsInChildren<Renderer>(true));

            IReadOnlyList<RubberDuckTraitType> traitTypes = duck.Data.TraitTypes;
            for (int i = 0; i < traitTypes.Count; i++)
            {
                IRubberDuckTrait trait = RubberDuckTraitFactory.Create(traitTypes[i]);
                if (trait == null)
                    continue;

                RubberDuckTraitExecution execution = trait.Init(context);
                runtimeTraits.Add(new RuntimeTrait(trait, execution));
            }

            initialized = true;
            return true;
        }

        public bool TryInteract(GameObject interactor)
        {
            if (!initialized || !interactor)
                return false;

            bool executed = false;
            for (int i = 0; i < runtimeTraits.Count; i++)
            {
                RuntimeTrait runtimeTrait = runtimeTraits[i];
                if (!runtimeTrait.Execution.UsesInteraction)
                    continue;

                executed = true;
                if (runtimeTrait.Trait.OnInteract(interactor) ==
                    TraitInteractionResult.Consumed)
                {
                    break;
                }
            }

            return executed;
        }

        private void Update()
        {
            if (!initialized)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < runtimeTraits.Count; i++)
            {
                RuntimeTrait runtimeTrait = runtimeTraits[i];
                float interval = runtimeTrait.Execution.Interval;
                if (interval <= 0f)
                    continue;

                runtimeTrait.ElapsedTime += deltaTime;
                while (runtimeTrait.ElapsedTime >= interval)
                {
                    runtimeTrait.ElapsedTime -= interval;
                    runtimeTrait.Trait.OnInterval();
                }
            }
        }
    }
}
