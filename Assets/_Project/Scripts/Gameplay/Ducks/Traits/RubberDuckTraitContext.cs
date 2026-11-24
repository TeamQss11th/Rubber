using System;
using UnityEngine;

namespace Rubber.Gameplay.Ducks.Traits
{
    public sealed class RubberDuckTraitContext
    {
        public RubberDuckInteractable Duck { get; }
        public Transform Transform { get; }
        public Rigidbody Rigidbody { get; }
        public Collider[] Colliders { get; }
        public Renderer[] Renderers { get; }

        public RubberDuckTraitContext(
            RubberDuckInteractable duck,
            Transform transform,
            Rigidbody rigidbody,
            Collider[] colliders,
            Renderer[] renderers)
        {
            Duck = duck ? duck : throw new ArgumentNullException(nameof(duck));
            Transform = transform ? transform : throw new ArgumentNullException(nameof(transform));
            Rigidbody = rigidbody ? rigidbody : throw new ArgumentNullException(nameof(rigidbody));
            Colliders = colliders ?? throw new ArgumentNullException(nameof(colliders));
            Renderers = renderers ?? throw new ArgumentNullException(nameof(renderers));
        }
    }
}
