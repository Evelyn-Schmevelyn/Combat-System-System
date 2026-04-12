using UnityEngine;

namespace CombatSystemSystem
{
    [CreateAssetMenu(fileName = "Moveset", menuName = "Combat System System/Moveset")]
    public class Moveset : ScriptableObject
    {
        public MoveData[] moves;
        public DamageData damage;
        public AnimationClip deflectedAnimation;
        public AnimationClip parriedAnimation;
    }
}
