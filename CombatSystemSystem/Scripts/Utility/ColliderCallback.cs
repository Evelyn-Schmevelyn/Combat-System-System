using System;
using UnityEngine;

namespace CombatSystemSystem
{
    public class ColliderCallback : MonoBehaviour
    {
        public event Action<Collider> onCollision;
        [NonSerialized] public HitHandler connectedHitHandler;

        private void OnTriggerEnter(Collider other)
        {
            onCollision?.Invoke(other);
        }

        // Clears any callbacks subscribed to the onCollision event
        public void Clear()
        {
            if (onCollision == null) return;
            foreach (Action<Collider> action in onCollision.GetInvocationList())
            {
                onCollision -= action;
            }
        }
    }
}
