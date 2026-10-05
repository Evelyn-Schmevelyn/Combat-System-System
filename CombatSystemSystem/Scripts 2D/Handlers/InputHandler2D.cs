using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/2D Input Handler"), RequireComponent(typeof(MoveHandler2D))]
    public class InputHandler2D : MonoBehaviour
    {
        [SerializeField] int _directionBufferLength = 10;
        [SerializeField, Range(0, .9f)] float _deadzone = .2f;
        [SerializeField] InputActionReference _motionReference;
        [SerializeField] InputActionReference[] _inputsReference;
        // Flips the x and/or y directions of the motion vector
        public bool flipX, flipY;


        InputTime[] _directionBuffer;
        MoveHandler2D _connectedMoveHandler;


        // ----------------------------------------------------------- Message/Callback Methods

        void Start()
        {
            _connectedMoveHandler = GetComponent<MoveHandler2D>();

            _directionBuffer = new InputTime[_directionBufferLength];
            for (int i = 0; i < _directionBuffer.Length; i ++) // Fill direction buffer with neutral inputs
            {
                _directionBuffer[i] = new InputTime(5, 1);
            }

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

            CombatManager.instance.inputUpdate += InputUpdate;

            _motionReference.action.performed += OnGetMotion;
            _motionReference.action.canceled += OnGetMotion;

            foreach (var button in _inputsReference)
            {
                button.action.performed += OnGetInput;
                button.action.canceled += OnReleaseInput;
            }
        }

        private void OnDisable()
        {
            CombatManager.instance.inputUpdate -= InputUpdate;

            _motionReference.action.performed -= OnGetMotion;
            _motionReference.action.canceled -= OnGetMotion;

            foreach (var button in _inputsReference)
            {
                button.action.performed -= OnGetInput;
                button.action.canceled -= OnReleaseInput;
            }
        }

        void InputUpdate()
        {
            _directionBuffer[0].duration++;
        }

        void OnGetMotion(InputAction.CallbackContext context)
        {
            Vector2 inputValue = context.ReadValue<Vector2>();

            int dir = Vector2ToNumpad(inputValue, _deadzone);

            if (dir == _directionBuffer[0].direction) return;

            for (int i = _directionBuffer.Length - 1; i > 0; i--)
            {
                _directionBuffer[i] = _directionBuffer[i - 1];
            }

            _directionBuffer[0] = new InputTime(dir, 0);
        }

        void OnGetInput(InputAction.CallbackContext context)
        {
            MoveData best = null;
            int bestIndex = -1;
            MovesetExtender2D bestExtender = _connectedMoveHandler.currentMovesetExtender;

            if (_connectedMoveHandler.currentMove != null)
            {
                MoveData.AnimationData currentData;
                switch (_connectedMoveHandler.currentMoveActiveState)
                {
                    case MoveHandler2D.MoveActiveState.HoldEnd:
                        currentData = _connectedMoveHandler.currentMove.endAnimation;
                        break;
                    case MoveHandler2D.MoveActiveState.Hold:
                        currentData = _connectedMoveHandler.currentMove.holdAnimation;
                        break;
                    default:
                        currentData = _connectedMoveHandler.currentMove.baseAnimation;
                        break;
                }

                best = GetBestMoveFromList(currentData.GetStillPossibleCancelableMoves(_connectedMoveHandler.currentMoveAnimationFrame), context.action.name);

                if (best != null) _connectedMoveHandler.GetParentMovesetAndIndex(best, out bestExtender, out bestIndex);
            }

            if (best == null || bestExtender == null) best = GetBestMoveFromNeutral(_connectedMoveHandler.movesetExtenders, context.action.name, out bestExtender, out bestIndex);

            if (best != null && bestExtender != null)
            {
                _connectedMoveHandler.QueueMove(bestExtender, bestIndex);
            }
        }

        void OnReleaseInput(InputAction.CallbackContext context)
        {
            if (_connectedMoveHandler.currentMove == null) return;
            if (_connectedMoveHandler.currentMove.input.buttonName == context.action.name) _connectedMoveHandler.EndHold();
        }


        // ----------------------------------------------------------- Internal Methods

        MoveData GetBestMoveFromNeutral(Moveset moveset, string buttonName)
        {
            if (moveset == null) return null;

            MoveData bestMove = null;
            int bestMoveInputLength = -1;
            int bestMoveBufferDepth = 0;

            for (int i = 0; i < moveset.moves.Length; i++)
            {
                if (moveset.moves[i].input.buttonName == buttonName && moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength && moveset.moves[i].accessibleFromNeutral)
                {
                    int currentMoveBufferDepth;

                    if (MotionMatchesBuffer(moveset.moves[i].input.motion, out currentMoveBufferDepth))
                    {
                        if (moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength
                            || (moveset.moves[i].input.motion.motionsList.Length == bestMoveInputLength && currentMoveBufferDepth < bestMoveBufferDepth))
                        {
                            bestMove = moveset.moves[i];
                            bestMoveInputLength = moveset.moves[i].input.motion.motionsList.Length;
                            bestMoveBufferDepth = currentMoveBufferDepth;
                        }
                    }
                }
            }

            return bestMove;
        }
        MoveData GetBestMoveFromNeutral(MovesetExtender2D[] movesets, string buttonName)
        {
            if (movesets == null || movesets.Length == 0)
            {
                return null;
            }

            MoveData bestMove = null;
            int bestMoveInputLength = -1;
            int bestMoveBufferDepth = 0;

            foreach (var movesetExtender in movesets)
            {
                for (int i = 0; i < movesetExtender.moveset.moves.Length; i++)
                {
                    if (movesetExtender.moveset.moves[i].input.buttonName == buttonName 
                        && movesetExtender.moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength 
                        && movesetExtender.moveset.moves[i].accessibleFromNeutral)
                    {
                        int currentMoveBufferDepth;

                        if (MotionMatchesBuffer(movesetExtender.moveset.moves[i].input.motion, out currentMoveBufferDepth))
                        {
                            if (movesetExtender.moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength
                                || (movesetExtender.moveset.moves[i].input.motion.motionsList.Length == bestMoveInputLength && currentMoveBufferDepth < bestMoveBufferDepth))
                            {
                                bestMove = movesetExtender.moveset.moves[i];
                                bestMoveInputLength = movesetExtender.moveset.moves[i].input.motion.motionsList.Length;
                                bestMoveBufferDepth = currentMoveBufferDepth;
                            }
                        }
                    }
                }

                if (bestMove != null)
                {
                    break;
                }
            }

            return bestMove;
        }
        MoveData GetBestMoveFromNeutral(MovesetExtender2D[] movesets, string buttonName, out MovesetExtender2D extender, out int index)
        {
            extender = null;
            index = -1;

            if (movesets == null || movesets.Length == 0) return null;

            MoveData bestMove = null;

            int bestMoveInputLength = -1;
            int bestMoveBufferDepth = 0;

            foreach (var movesetExtender in movesets)
            {
                for (int i = 0; i < movesetExtender.moveset.moves.Length; i++)
                {
                    if (movesetExtender.moveset.moves[i].input.buttonName == buttonName
                        && movesetExtender.moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength
                        && movesetExtender.moveset.moves[i].accessibleFromNeutral)
                    {
                        int currentMoveBufferDepth;

                        if (MotionMatchesBuffer(movesetExtender.moveset.moves[i].input.motion, out currentMoveBufferDepth))
                        {
                            if (movesetExtender.moveset.moves[i].input.motion.motionsList.Length > bestMoveInputLength
                                || (movesetExtender.moveset.moves[i].input.motion.motionsList.Length == bestMoveInputLength && currentMoveBufferDepth < bestMoveBufferDepth))
                            {
                                bestMove = movesetExtender.moveset.moves[i];
                                bestMoveInputLength = movesetExtender.moveset.moves[i].input.motion.motionsList.Length;
                                bestMoveBufferDepth = currentMoveBufferDepth;

                                extender = movesetExtender;
                                index = i;
                            }
                        }
                    }
                }

                if (bestMove != null)
                {
                    break;
                }
            }

            return bestMove;
        }
        MoveData GetBestMoveFromList(MoveData[] list, string buttonName, bool fromNeutralRequired = false)
        {
            if (list.Length == 0) return null;

            MoveData bestMove = null;
            int bestMoveInputLength = -1;
            int bestMoveBufferDepth = 0;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].input.buttonName == buttonName 
                    && list[i].input.motion.motionsList.Length > bestMoveInputLength 
                    && (list[i].accessibleFromNeutral || !fromNeutralRequired))
                {
                    int currentMoveBufferDepth;

                    if (MotionMatchesBuffer(list[i].input.motion, out currentMoveBufferDepth))
                    {
                        if (list[i].input.motion.motionsList.Length > bestMoveInputLength
                            || (list[i].input.motion.motionsList.Length == bestMoveInputLength && currentMoveBufferDepth < bestMoveBufferDepth))
                        {
                            bestMove = list[i];
                            bestMoveInputLength = list[i].input.motion.motionsList.Length;
                            bestMoveBufferDepth = currentMoveBufferDepth;
                        }
                    }
                }
            }

            return bestMove;
        }

        bool MotionMatchesBuffer(InputData.InputMotion input, out int depthInBuffer)
        {
            int bufferTime = 0, lastBufferTime = 0;
            int currentMotionDirectionIndex = 0; // The motion index we're checking for
            depthInBuffer = 0; // How far deep in the buffer we searched before finding the first input

            if (input.motionsList.Length - 1 > _directionBuffer.Length)
            {
                Debug.LogWarning("Motion input too long to fit in buffer. Consider increasing the buffer's size");
                return false;
            }

            for (int i = 0; i < _directionBuffer.Length - (input.motionsList.Length - 1); i ++)
            {
                if (currentMotionDirectionIndex == 0) depthInBuffer = i;
                if (currentMotionDirectionIndex >= input.motionsList.Length) return true;

                int currentMotionDirectionIndexActual = input.motionsList.Length - 1 - currentMotionDirectionIndex;

                if (bufferTime <= lastBufferTime + input.motionsList[currentMotionDirectionIndexActual].window) 
                {
                    if (DirectionAllowable(input.motionsList[currentMotionDirectionIndexActual], _directionBuffer[i].direction)   // Directions match
                        && _directionBuffer[i].duration >= input.motionsList[currentMotionDirectionIndexActual].duration)         // Duration is met
                    {
                        currentMotionDirectionIndex++;
                    }
                }
                else
                {
                    return false;
                }

                lastBufferTime = bufferTime;
                bufferTime += _directionBuffer[i].duration;
            }

            return false;
        }
        bool DirectionAllowable(InputData.DirectionUnit motion, int actualDirection)
        {
            if (motion.direction == actualDirection) return true;

            if (!motion.strict)
            {
                switch (motion.direction)
                {
                    case 1: return (actualDirection == 4 || actualDirection == 2);
                    case 2: return (actualDirection == 1 || actualDirection == 2);
                    case 3: return (actualDirection == 2 || actualDirection == 6);
                    case 4: return (actualDirection == 7 || actualDirection == 1);
                    case 6: return (actualDirection == 3 || actualDirection == 9);
                    case 7: return (actualDirection == 4 || actualDirection == 8);
                    case 8: return (actualDirection == 7 || actualDirection == 9);
                    default: return (actualDirection == 6 || actualDirection == 8);
                }
            }

            return false;
        }
        // Converts a Vector2 to a 1-9 number which we can use for directionality
        // 7 8 9
        // 4 5 6
        // 1 2 3
        int Vector2ToNumpad(Vector2 input, float deadzone)
        {
            
            if (input.magnitude < deadzone) return 5;
            
            input.x = flipX ? -input.x : input.x;
            input.y = flipY ? -input.y : input.y;

            Vector2 absInput = new Vector2(Mathf.Abs(input.x), Mathf.Abs(input.y));

            // If closer to the horizontal axis
            if (absInput.x > absInput.y)
            {
                float half = absInput.x * .4142f;

                if (input.x < 0)
                {
                    if (input.y > half) return 1;
                    if (input.y < -half) return 7;
                    return 4;
                }
                else
                {
                    if (input.y > half) return 3;
                    if (input.y < -half) return 9;
                    return 6;
                }
            }
            // If closer to the vertical axis
            else
            {
                float half = absInput.y * .4142f;

                if (input.y < 0)
                {
                    if (input.x > half) return 3;
                    if (input.x < -half) return 1;
                    return 2;
                }
                else
                {
                    if (input.x > half) return 9;
                    if (input.x < -half) return 7;
                    return 8;
                }
            }
        }


        // ----------------------------------------------------------- Internal Structs

        struct InputTime
        {
            public InputTime(int numpadDirection, int framesHeld)
            {
                direction = numpadDirection;
                duration = framesHeld;
            }

            public int direction;
            public int duration;
        }
    }
}
