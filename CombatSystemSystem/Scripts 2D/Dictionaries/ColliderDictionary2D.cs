using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/2D Collider Dictionary")]
    public class ColliderDictionary2D : MonoBehaviour
    {
        public ColliderData[] colliders;

        // Activates or Deactivates the colliders at the given indexes in the collider array
        // On deactivate, it clears all data from the colliders
        public void SetActiveAtIndexes(int[] indexes, bool setValue)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                colliders[indexes[i]].collider.enabled = setValue;
                if (!setValue)
                {
                    colliders[indexes[i]].callback.Clear();
                    colliders[indexes[i]].collidersHit.Clear();
                }
            }
        }
        // Provides a callback that is called when the colliders of the chosen type start a collision
        public void ProvideCallbackAtIndexes(int[] indexes, Action<Collider2D> onCollisionCallback, ColliderData.ColliderType filter)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                if (onCollisionCallback != null && colliders[indexes[i]].type == filter) colliders[indexes[i]].callback.onCollision += onCollisionCallback;
            }
        }

        [System.Serializable]
        public struct ColliderData
        {
            public Collider2D collider;
            public ColliderType type;
            [NonSerialized] public ColliderCallback2D callback;
            [NonSerialized] public List<Collider2D> collidersHit;

            public enum ColliderType
            {
                Hitbox,
                Hurtbox
            }
        }
    }
}