using System.Collections.Generic;
using System;
using UnityEngine;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/2D Move Handler"), RequireComponent(typeof(HitHandler2D))]
    public class MoveHandler2D : MonoBehaviour
    {
        // Inspector Variables
        [SerializeField] List<MovesetExtender2D> _movesetExtenders;
        [SerializeField] int _moveQueueDuration = 40;
        [SerializeField] AnimationHandler _animationHandler;

        // Public Variables
        public AnimationHandler animationHandler { get { return _animationHandler; } }
        public HitHandler2D hitHandler { get; private set; }
        public MovesetExtender2D[] movesetExtenders { get { return _movesetExtenders.ToArray(); } }
        public MovesetExtender2D currentMovesetExtender { get; private set; }
        public MoveData currentMove { get { return currentMovesetExtender?.moveset.moves[_currentMoveIndex]; } }
        public MoveActiveState currentMoveActiveState { get; private set; }
        public int currentMoveAnimationFrame { get; private set; }

        // Internal Variables
        int _currentMoveIndex = -1, _currentMoveFixedFrame;

        MovesetExtender2D _queuedMovesetExtender;
        int _queuedMoveIndex = -1, _queuedMoveLife, _inactionableTime = 0;

        // Internal Enums
        public enum MoveActiveState
        {
            Base, Hold, HoldEnd
        }


        // ----------------------------------------------------------- Public Methods

        // Queues a move to be played once there isn't a currentMove playing
        // If the move is in the queue for a number of FixedUpdate frames longer than the _moveQueueDuration, it gets removed from the queue
        //
        // moveset and moveIndex are used in place of a MoveData in order to be able to find the moveset's connected ColliderDictionary etc.
        public void QueueMove(MovesetExtender2D moveset, int moveIndex)
        {
            _queuedMoveIndex = moveIndex;
            _queuedMovesetExtender = moveset;
            _queuedMoveLife = _moveQueueDuration;
        }

        // Clears the move queue
        public void ClearQueue()
        {
            _queuedMoveIndex = -1;
            _queuedMovesetExtender = null;
            _queuedMoveLife = 0;
        }

        // Stops the currently playing move, if any
        public void StopMove(bool skipAnimationStop = false)
        {
            if (currentMove == null) return;

            // Purge leftover collider data
            foreach (var hitbox in currentMove.baseAnimation.hitboxes)
            {
                currentMovesetExtender.colliderDictionary.SetActiveAtIndexes(hitbox.colliderDictionaryReferences, false);
            }
            foreach (var hitbox in currentMove.holdAnimation.hitboxes)
            {
                currentMovesetExtender.colliderDictionary.SetActiveAtIndexes(hitbox.colliderDictionaryReferences, false);
            }
            foreach (var hitbox in currentMove.endAnimation.hitboxes)
            {
                currentMovesetExtender.colliderDictionary.SetActiveAtIndexes(hitbox.colliderDictionaryReferences, false);
            }

            currentMovesetExtender = null;
            _currentMoveIndex = -1;

            if (!skipAnimationStop) _animationHandler?.StopAnimation();
        }

        // Ends the holding of a canHoldMove move
        public void EndHold()
        {
            if (currentMove.canHoldMove
                && currentMoveActiveState != MoveActiveState.HoldEnd)
            {
                int saveIndex = _currentMoveIndex;
                MovesetExtender2D saveExtender = currentMovesetExtender;

                StopMove();

                StartMove(saveExtender, saveIndex, MoveActiveState.HoldEnd);

                return;
            }
        }

        // Set a number of FixedUpdate frames that no move in the move queue will be started
        public void SetInactionable(int fixedFrameDuration)
        {
            _inactionableTime = fixedFrameDuration;
        }

        // Use to search for the connected movesetExtenders for the first apperance of the MoveData, outing the reference and index
        public void GetParentMovesetAndIndex(MoveData move, out MovesetExtender2D movesetExtender, out int index)
        {
            for (int i = 0; i < _movesetExtenders.Count; i++)
            {
                for (int j = 0; j < _movesetExtenders[i].moveset.moves.Length; j++)
                {
                    if (_movesetExtenders[i].moveset.moves[j] == move)
                    {
                        movesetExtender = _movesetExtenders[i];
                        index = j;
                        return;
                    }
                }
            }

            movesetExtender = null;
            index = -1;
        }

        // Returns all active BlockingData in the current move
        public MoveData.BlockingData[] GetActiveBlocks()
        {
            List<MoveData.BlockingData> list = new List<MoveData.BlockingData>();
            if (currentMove == null) { return list.ToArray(); }

            MoveData.AnimationData currentAnimationData;
            switch (currentMoveActiveState)
            {
                case MoveActiveState.HoldEnd:
                    currentAnimationData = currentMove.endAnimation;
                    break;
                case MoveActiveState.Hold:
                    currentAnimationData = currentMove.holdAnimation;
                    break;
                default:
                    currentAnimationData = currentMove.baseAnimation;
                    break;
            }

            if (currentAnimationData.blocks.Length != 0)
            {
                foreach (var block in currentAnimationData.blocks)
                {
                    if (block.frameStart <= currentMoveAnimationFrame && currentMoveAnimationFrame < block.frameEnd)
                    {
                        list.Add(block);
                    }
                }
            }

            return list.ToArray();
        }


        // ----------------------------------------------------------- Message/Callback Methods

        private void Awake()
        {
            hitHandler = GetComponent<HitHandler2D>();

            foreach (var extender in _movesetExtenders)
            {
                extender.Setup(hitHandler);
            }
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

            if (_moveQueueDuration <= 0) Debug.LogWarning(this.name + "'s Move Queue duration is set to 0 or lower. This will cause it to no longer be able to play moves");

            CombatManager.instance.moveUpdate += MoveUpdate;
        }
        private void OnDisable()
        {
            CombatManager.instance.moveUpdate -= MoveUpdate;
        }
        void MoveUpdate()
        {
            ProgressQueue();
            ProgressMove();
        }


        // ----------------------------------------------------------- Internal Methods

        void StartMove(MovesetExtender2D moveset, int moveIndex, MoveActiveState state = MoveActiveState.Base)
        {
            _currentMoveIndex = moveIndex;
            currentMovesetExtender = moveset;
            _currentMoveFixedFrame = 0;
            currentMoveAnimationFrame = 0;
            currentMoveActiveState = state;

            MoveData.AnimationData currentAnimationData;
            switch (state)
            {
                case MoveActiveState.HoldEnd:
                    currentAnimationData = currentMove.endAnimation;
                    break;
                case MoveActiveState.Hold:
                    currentAnimationData = currentMove.holdAnimation;
                    break;
                default:
                    currentAnimationData = currentMove.baseAnimation;
                    break;
            }

            _animationHandler?.PlayAnimationClip(currentAnimationData.animation, .1f, false, 
                state == MoveActiveState.Hold || (currentMove.canHoldMove && state == MoveActiveState.Base));
        }
        void ProgressQueue()
        {
            if (_inactionableTime > 0)
            {
                _inactionableTime--;
                _queuedMoveLife--;
                return;
            }

            if (_queuedMovesetExtender == null || _queuedMoveIndex == -1) return;

            // End queue if lifespan extended
            if (_queuedMoveLife <= 0)
            {
                _queuedMovesetExtender = null;
                _queuedMoveIndex = -1;
                return;
            }

            // End queue and start move if _currentMove is empty
            if (currentMovesetExtender == null && _currentMoveIndex == -1)
            {
                StartMove(_queuedMovesetExtender, _queuedMoveIndex);

                _queuedMovesetExtender = null;
                _queuedMoveIndex = -1;
                _queuedMoveLife = 0;
                return;
            }

            _queuedMoveLife--;
        }

        void ProgressMove()
        {
            if (currentMovesetExtender == null) return;
            if (currentMove == null) return;

            // Get the correct animation data for the current attack
            MoveData.AnimationData currentAnimationData;
            switch (currentMoveActiveState)
            {
                case MoveActiveState.HoldEnd:
                    currentAnimationData = currentMove.endAnimation;
                    break;
                case MoveActiveState.Hold:
                    currentAnimationData = currentMove.holdAnimation;
                    break;
                default:
                    currentAnimationData = currentMove.baseAnimation;
                    break;
            }
            // We convert the fixed update frames to their expected animation frame (The two have seperate framerates)
            // We do this to make it easier to layout what frames things should turn on and off, as you can now write them in animation time.
            currentMoveAnimationFrame = Mathf.RoundToInt((((float)_currentMoveFixedFrame * Time.fixedDeltaTime) % currentAnimationData.animation.length) * currentAnimationData.animation.frameRate);

            // End Move
            if (currentMoveAnimationFrame >= Mathf.RoundToInt(currentAnimationData.animation.length * currentAnimationData.animation.frameRate)
                && currentMoveActiveState != MoveActiveState.Hold)
            {
                int saveIndex = _currentMoveIndex;
                MovesetExtender2D saveExtender = currentMovesetExtender;
                bool moveNext = currentMoveActiveState != MoveActiveState.HoldEnd && currentMove.canHoldMove;

                StopMove(moveNext);

                if (moveNext)
                {
                    StartMove(saveExtender, saveIndex, MoveActiveState.Hold);
                }

                return;
            }

            // Cancel Move
            if (_queuedMovesetExtender != null)
            {
                foreach (var cancel in currentAnimationData.cancelsIntoMoves)
                {
                    if (cancel.frameStart <= currentMoveAnimationFrame && currentMoveAnimationFrame < cancel.frameEnd
                        && cancel.moveData == _queuedMovesetExtender.moveset.moves[_queuedMoveIndex])
                    {
                        StopMove(true);
                        StartMove(_queuedMovesetExtender, _queuedMoveIndex);
                    }
                }
            }

            // Hitboxes
            if (currentAnimationData.hitboxes.Length != 0)
            {
                foreach (var hitbox in currentAnimationData.hitboxes)
                {
                    if (hitbox.frameStart == currentMoveAnimationFrame)
                    {
                        // Must declare callback here, as it needs to change based on the hitbox we're activating, which is only accesible here without way too many workarounds.
                        Action<Collider2D> onHitCallback;

                        if (currentMove.isGrab)
                        {
                            onHitCallback = (collider) =>
                            {
                                foreach (int i in hitbox.colliderDictionaryReferences)
                                {
                                    if (currentMovesetExtender.colliderDictionary.colliders[i].collidersHit.Contains(collider)) return;
                                    currentMovesetExtender.colliderDictionary.colliders[i].collidersHit.Add(collider);
                                }
                                //Debug.Log($"Collider \"{collider.name}\" was grabbed by \"{currentMove.name}\" with hitbox starting frame {hitbox.frameStart}");

                                HitHandler2D hitHandler = collider.GetComponent<ColliderCallback2D>()?.connectedHitHandler;
                                hitHandler?.Grab(this, hitbox.successfulGrabData);
                            };
                        }
                        else
                        {
                            onHitCallback = (collider) =>
                            {
                                foreach (int i in hitbox.colliderDictionaryReferences)
                                {
                                    if (currentMovesetExtender.colliderDictionary.colliders[i].collidersHit.Contains(collider)) return;
                                    currentMovesetExtender.colliderDictionary.colliders[i].collidersHit.Add(collider);
                                }
                                //Debug.Log($"Collider \"{collider.name}\" was hit by \"{currentMove.name}\" with hitbox starting frame {hitbox.frameStart}");

                                HitHandler2D hitHandler = collider.GetComponent<ColliderCallback2D>()?.connectedHitHandler;
                                hitHandler?.Hit(this, hitbox.damageData);
                            };
                        }

                        currentMovesetExtender.colliderDictionary.SetActiveAtIndexes(hitbox.colliderDictionaryReferences, true);
                        currentMovesetExtender.colliderDictionary.ProvideCallbackAtIndexes(hitbox.colliderDictionaryReferences, onHitCallback, ColliderDictionary2D.ColliderData.ColliderType.Hitbox);
                    }
                    if (hitbox.frameEnd == currentMoveAnimationFrame)
                    {
                        currentMovesetExtender.colliderDictionary.SetActiveAtIndexes(hitbox.colliderDictionaryReferences, false);
                    }
                }
            }
            // Projectiles (Not yet implemented)
            if (currentAnimationData.projectiles.Length != 0)
            {
                foreach (var projectile in currentAnimationData.projectiles)
                {
                    if (projectile.frameSpawned == currentMoveAnimationFrame)
                    {
                        Debug.Log("Should spawn projectile");
                    }
                }
            }
            

            _currentMoveFixedFrame ++;
        }
    }
}