using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CombatSystemSystem
{
    [CreateAssetMenu(fileName = "NewMoveData", menuName = "Combat System System/Move Data")]
    public class MoveData : ScriptableObject
    {
        public InputData input;
        public bool accessibleFromNeutral = true;
        public bool canHoldMove;

        public AnimationData baseAnimation;
        public AnimationData holdAnimation;
        public AnimationData endAnimation;


        [HideInInspector] public bool isGrab = false;

        private void OnValidate()
        {
            UpdateIsGrab();
        }

        bool UpdateIsGrab()
        {
            bool hasGrabData = false;

            for (int i = 0; i < baseAnimation.hitboxes.Length; i++)
            {
                if (baseAnimation.hitboxes[i].successfulGrabData.attackerGrabAnimation != null
                    || baseAnimation.hitboxes[i].successfulGrabData.victimGrabAnimation != null) hasGrabData = true;
            }

            if (canHoldMove)
            {
                for (int i = 0; i < holdAnimation.hitboxes.Length; i++)
                {
                    if (holdAnimation.hitboxes[i].successfulGrabData.attackerGrabAnimation != null
                        || holdAnimation.hitboxes[i].successfulGrabData.victimGrabAnimation != null) hasGrabData = true;
                }
                for (int i = 0; i < endAnimation.hitboxes.Length; i++)
                {
                    if (endAnimation.hitboxes[i].successfulGrabData.attackerGrabAnimation != null
                        || endAnimation.hitboxes[i].successfulGrabData.victimGrabAnimation != null) hasGrabData = true;
                }
            }

            isGrab = hasGrabData;

            return isGrab;
        }

        // ------------------------------------------------------- Nested Classes and Structs

        [System.Serializable]
        public struct AnimationData
        {
            public AnimationClip animation;

            public HitboxData[] hitboxes;
            public ProjectileData[] projectiles;
            public BlockingData[] blocks;

            //public CustomMoveData customMoveData;

            public MoveCancelData[] cancelsIntoMoves;

            public MoveData[] GetStillPossibleCancelableMoves(int currentFrame)
            {
                List<MoveData> allActive = new List<MoveData>();
                for (int i = 0; i < cancelsIntoMoves.Length; i++)
                {
                    if (cancelsIntoMoves[i].frameEnd >= currentFrame)
                    {
                        allActive.Add(cancelsIntoMoves[i].moveData);
                    }
                }

                return allActive.ToArray();
            }
        }

        [System.Serializable]
        public struct HitboxData
        {
            public int[] colliderDictionaryReferences;

            public int frameStart, frameEnd;

            //public bool useDamageDataAsMult;
            public DamageData damageData;

            // public string onSuccessEventDictionaryReference, onFailEventDictionaryReference;

            // Not Required
            public SuccessfulGrabData successfulGrabData;

            [System.Serializable]
            public struct SuccessfulGrabData
            {
                public AnimationClip attackerGrabAnimation;
                public AnimationClip victimGrabAnimation;

                public int teleportPositionDictionaryReference;
                public DamageInstance[] damageInstances;
            }

            [System.Serializable]
            public struct DamageInstance
            {
                public int frame;
                public DamageData damageData;
            }
        }

        [System.Serializable]
        public struct ProjectileData
        {
            public GameObject prefab;
            public int frameSpawned;

            public bool useDamageDataAsMult;
            public DamageData damageData;

            public int spawnPositionDictionaryReference;
        }

        [System.Serializable]
        public struct BlockingData
        {
            public int frameStart, frameEnd;

            public float coverageAngle;
            public bool isParry;

            // public string onSuccessEventDictionaryReference, onFailEventDictionaryReference;
        }

        [System.Serializable]
        public struct MoveCancelData
        {
            public int frameStart, frameEnd;
            public MoveData moveData;
        }
    }
}
