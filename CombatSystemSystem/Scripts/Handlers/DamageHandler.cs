using UnityEngine;
using System.Collections.Generic;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Damage Handler")]
    public abstract class DamageHandler : MonoBehaviour
    {
        // ----------------------------------------------------------- Public Methods

        // Called when a hit is taken
        //
        // Override for your own custom hit handling
        // Returns true if a hit is actually taken
        public virtual bool OnHit(HitHandler hitHandler, List<DamageData> data)
        {
            return false;
        }

        // Called when a hit is blocked
        //
        // Override for your own custom block handling
        // Returns true if a hit is actually blocked
        public virtual bool OnBlock(HitHandler hitHandler, List<DamageData> data)
        {
            return false;
        }

        // Called when grabbed
        //
        // Override for your own grab handling
        // Returns true if a grab is actualy started
        public virtual bool OnGrab(HitHandler hitHandler, MoveData.HitboxData.SuccessfulGrabData data, Transform teleportPosition)
        {
            return false;
        }
    }
}
