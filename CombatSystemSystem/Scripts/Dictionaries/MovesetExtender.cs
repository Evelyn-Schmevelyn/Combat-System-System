using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Moveset Extender")]
    public class MovesetExtender : MonoBehaviour
    {
        public Moveset moveset;
        public ColliderDictionary colliderDictionary;
        public PositionDictionary positionDictionary;
        public EventDictionary eventDictionary;

        // Call each time the MovesetExtender is moved onto a MoveHandler
        public void Setup(HitHandler hitHandler)
        {
            List<int> initializeIndexesNextFrame = new List<int>();
            for (int i = 0; i <  colliderDictionary.colliders.Length; i++)
            {
                colliderDictionary.colliders[i].collider.enabled = false;
                colliderDictionary.colliders[i].collider.isTrigger = true;
                colliderDictionary.colliders[i].collidersHit = new List<Collider>();

                switch (colliderDictionary.colliders[i].type)
                {
                    case ColliderDictionary.ColliderData.ColliderType.Hitbox:
                        colliderDictionary.colliders[i].collider.gameObject.layer = LayerMask.NameToLayer("Hitbox");
                        colliderDictionary.colliders[i].collider.excludeLayers = ~LayerMask.GetMask("Hurtbox"); //exclude everything but the hurtboxes
                        break;
                    case ColliderDictionary.ColliderData.ColliderType.Hurtbox:
                        colliderDictionary.colliders[i].collider.gameObject.layer = LayerMask.NameToLayer("Hurtbox");
                        colliderDictionary.colliders[i].collider.excludeLayers = ~LayerMask.GetMask("Hitbox"); //exclude everything but the hitboxes
                        break;
                }

                

                ColliderCallback tryCallback;
                if (!colliderDictionary.colliders[i].collider.TryGetComponent<ColliderCallback>(out tryCallback))
                {
                    tryCallback = colliderDictionary.colliders[i].collider.gameObject.AddComponent<ColliderCallback>();
                    initializeIndexesNextFrame.Add(i);
                }
                else if (colliderDictionary.colliders[i].type == ColliderDictionary.ColliderData.ColliderType.Hurtbox)
                {
                    tryCallback.connectedHitHandler = hitHandler;
                }

                colliderDictionary.colliders[i].callback = tryCallback;
            }

            StartCoroutine(initializeFailedCallbacks(initializeIndexesNextFrame, hitHandler));
        }

        // For whatever reason, Unity can get the ColliderCallback but refuses to store it in the array.
        // I imagine this is due to some errors caused by the component not being full initialized.
        // Waiting one frame seems to do the trick, and shouldn't cause any problems
        IEnumerator initializeFailedCallbacks(List<int> indexes, HitHandler hitHandler)
        {
            yield return null;
            foreach (int index in indexes)
            {
                colliderDictionary.colliders[index].callback = colliderDictionary.colliders[index].collider.GetComponent<ColliderCallback>();
                colliderDictionary.colliders[index].callback.connectedHitHandler = hitHandler;
            }
        }
    }
}
