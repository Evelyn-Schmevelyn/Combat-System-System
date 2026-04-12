using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Hit Handler")]
    public class HitHandler : MonoBehaviour
    {
        [SerializeField] Collider[] _startingColliders;
        [SerializeField] DamageHandler _damageHandler;

        public MoveHandler moveHandler { get; private set; }

        List<DamageSourceSet> _queuedDamageData = new List<DamageSourceSet>();
        List<GrabSourceSet> _queuedGrabData = new List<GrabSourceSet>();

        int _invincibilityFrames = 0;


        // ----------------------------------------------------------- Public Methods

        // Call to queue a hit
        //
        // Queued hits will be handled when the next CombatManager - HitUpdate occurs (after grabs), or if HandleHitQueue is called remotely
        public void Hit(MoveHandler source, DamageData data)
        {
            if (_invincibilityFrames < 0) return;
            _queuedDamageData.Add(new DamageSourceSet(data, source));
        }

        // Call to queue a grab
        //
        // Queued grabs will be handled when the next CombatManager - HitUpdate occurs (before hits), or if HandleGrabQueue is called remotely
        public void Grab(MoveHandler source, MoveData.HitboxData.SuccessfulGrabData data)
        {
            if (_invincibilityFrames < 0) return;
            _queuedGrabData.Add(new GrabSourceSet(data, source));
        }

        // Call to ignore any take hits and grabs for a number of FixedUpdate frames
        public void SetInvincible(int fixedFramesDuration)
        {
            _invincibilityFrames = fixedFramesDuration;
        }

        // Handles grabs, calling OnGrab on its connected damage handler
        //
        // Returns true if at least one grab was handled
        //
        // Only call if very nessesary.
        public bool HandleGrabQueue()
        {
            if (_queuedGrabData.Count > 0)
            {
                // Best method I can think of right now for handling multiple grabs on the same frame. Change later if needed
                GrabSourceSet successfulGrab = _queuedGrabData[0];

                Transform teleportPosition = successfulGrab.source.currentMovesetExtender.positionDictionary.transforms[successfulGrab.data.teleportPositionDictionaryReference];

                if (_damageHandler.OnGrab(this, successfulGrab.data, teleportPosition))
                {
                    successfulGrab.source.StopMove();
                    successfulGrab.source.SetInactionable(Mathf.RoundToInt(successfulGrab.data.attackerGrabAnimation.length / Time.fixedDeltaTime));
                    successfulGrab.source.animationHandler.PlayAnimationClip(successfulGrab.data.attackerGrabAnimation);
                }

                _queuedGrabData.Clear();

                return true;
            }

            return false;
        }

        // Handles hits, calling OnHit on its connected damage handler
        // Also handles blocks, calling OnBlock instead
        //          -   if the horizontal angle between the direction to the hit source and this transforms Forward direction is less than half the coverage angle of the highest coverage angle block.
        //
        // Returns true if at least one hit was handled
        //
        // Only call if very nessesary.
        public bool HandleHitQueue()
        {
            if (_queuedDamageData.Count > 0)
            {
                if (moveHandler != null)
                {
                    List<DamageData> blockedDamage = new List<DamageData>(), unblockedDamage = new List<DamageData>();

                    MoveData.BlockingData[] currentBlockData = moveHandler.GetActiveBlocks();

                    foreach (DamageSourceSet damageSource in _queuedDamageData)
                    {
                        bool blocked = false;

                        float incomingAngle = Vector2.Angle(new Vector2(transform.forward.x, transform.forward.z),
                            new Vector2(damageSource.source.transform.position.x - transform.position.x, damageSource.source.transform.position.z - transform.position.z));

                        for (int i = 0; i < currentBlockData.Length; i++)
                        {
                            if (currentBlockData[i].coverageAngle / 2 >= incomingAngle)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked) blockedDamage.Add(damageSource.data);
                        else unblockedDamage.Add(damageSource.data);
                    }

                    if (_damageHandler != null)
                    {
                        _damageHandler.OnBlock(this, blockedDamage);
                        _damageHandler.OnHit(this, unblockedDamage);
                    }
                }

                else
                {
                    if (_damageHandler != null) _damageHandler.OnHit(this, DamageSourceSet.toDataList(_queuedDamageData));
                }

                _queuedDamageData.Clear();

                return true;
            }

            return false;
        }

        // ----------------------------------------------------------- Message/Callback Methods
        private void Awake()
        {
            moveHandler = GetComponent<MoveHandler>();

            List<int> initializeIndexesNextFrame = new List<int>();
            for (int i = 0; i < _startingColliders.Length; i++)
            {
                _startingColliders[i].gameObject.layer = LayerMask.NameToLayer("Hurtbox");
                _startingColliders[i].excludeLayers = ~LayerMask.GetMask("Hitbox"); //exclude everything but the hitboxes

                ColliderCallback tryCallback;
                if (!_startingColliders[i].TryGetComponent<ColliderCallback>(out tryCallback))
                {
                    tryCallback = _startingColliders[i].AddComponent<ColliderCallback>();
                    initializeIndexesNextFrame.Add(i);
                }
                else
                {
                    tryCallback.connectedHitHandler = this;
                }

                _startingColliders[i].isTrigger = true;

                Rigidbody tryRB;
                if (!_startingColliders[i].TryGetComponent<Rigidbody>(out tryRB))
                {
                    tryRB = _startingColliders[i].AddComponent<Rigidbody>();
                }
                tryRB.isKinematic = true;
            }

            StartCoroutine(initializeFailedCallbacks(initializeIndexesNextFrame, this));
        }

        private void Start()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            if (!didStart) return;

            if (CombatManager.instance == null)
            {
                Debug.LogError(this.name + " requires a Combat Manager to function. Please add one on any object in the scene \n Add Component > Combat System System > Combat Manager");
                this.enabled = false;
                return;
            }

            CombatManager.instance.hitUpdate += HitUpdate;
        }

        private void OnDisable()
        {
            CombatManager.instance.hitUpdate -= HitUpdate;
        }

        void HitUpdate()
        {
            HandleGrabQueue();
            HandleHitQueue();

            _invincibilityFrames = _invincibilityFrames > 0 ? _invincibilityFrames - 1 : 0;
        }


        // ----------------------------------------------------------- Coroutines

        // For whatever reason, Unity can get the ColliderCallback but refuses to store it in the array.
        // I imagine this is due to some errors caused by the component not being full initialized.
        // Waiting one frame seems to do the trick, and shouldn't cause any problems
        IEnumerator initializeFailedCallbacks(List<int> indexes, HitHandler hitHandler)
        {
            yield return null;
            foreach (int index in indexes)
            {
                _startingColliders[index].GetComponent<ColliderCallback>().connectedHitHandler = this;
            }
        }

        // ----------------------------------------------------------- Internal Structs

        struct DamageSourceSet
        {
            public DamageData data;
            public MoveHandler source;

            public DamageSourceSet (DamageData damageData, MoveHandler moveSource)
            {
                this.data = damageData;
                this.source = moveSource;
            }

            public static List<DamageData> toDataList(List<DamageSourceSet> sourceSets)
            {
                List<DamageData> datas = new List<DamageData>();

                foreach (DamageSourceSet sourceSet in sourceSets)
                {
                    datas.Add(sourceSet.data);
                }

                return datas;   
            }
        }

        struct GrabSourceSet
        {
            public MoveData.HitboxData.SuccessfulGrabData data;
            public MoveHandler source;

            public GrabSourceSet (MoveData.HitboxData.SuccessfulGrabData grabData, MoveHandler moveSource)
            {
                this.data = grabData;
                this.source = moveSource;
            }

            public static List<MoveData.HitboxData.SuccessfulGrabData> toDataList(List<GrabSourceSet> sourceSets)
            {
                List<MoveData.HitboxData.SuccessfulGrabData> datas = new List<MoveData.HitboxData.SuccessfulGrabData>();

                foreach (GrabSourceSet sourceSet in sourceSets)
                {
                    datas.Add(sourceSet.data);
                }

                return datas;
            }
        }
    }
}