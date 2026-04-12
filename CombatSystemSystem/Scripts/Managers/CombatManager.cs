using System;
using UnityEngine;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Combat Manager")]
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager instance;

        public event Action inputUpdate, moveUpdate, hitUpdate;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple Game Managers found. Destroying " + gameObject.name);
                Destroy(gameObject);
            }
        }

        private void FixedUpdate() //Sequences things to allow for all hit detection to complete before hit behaviours start
        {
            inputUpdate?.Invoke();
            moveUpdate?.Invoke();
            hitUpdate?.Invoke();
        }
    }
}
