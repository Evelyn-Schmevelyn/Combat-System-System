using System;
using UnityEngine;

namespace CombatSystemSystem
{
    public class ColliderCallback2D : MonoBehaviour
    {
        public event Action<Collider2D> onCollision;
        [NonSerialized] public HitHandler2D connectedHitHandler;

        private void OnTriggerEnter2D(Collider2D other)
        {
            onCollision?.Invoke(other);
        }

        // Clears any callbacks subscribed to the onCollision event
        public void Clear()
        {
            if (onCollision == null) return;
            foreach (Action<Collider2D> action in onCollision.GetInvocationList())
            {
                onCollision -= action;
            }
        }
    }
}
