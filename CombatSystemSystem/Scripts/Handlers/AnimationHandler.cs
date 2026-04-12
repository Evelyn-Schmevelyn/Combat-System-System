using CombatSystemSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Animation Handler")]
    public class AnimationHandler : MonoBehaviour
    {
        [SerializeField] Animator _animator;


        public Animator getAnimator { get { return _animator; } }

        // Playables Graph Nodes
        PlayableGraph _playableGraph;
        AnimationMixerPlayable _animatorMixer, _lastAnimationMixer;
        AnimatorControllerPlayable _animatorPlayable;
        AnimationClipPlayable _oneShotPlayable, _lastAnimationPlayable;

        // Blending Variables
        Coroutine _oneShotBlendingCoroutine, _insertionBlendingCoroutine;
        bool _pauseBlend;
        float _currentOneShotWeight = 0;


        // ----------------------------------------------------------- Public Methods

        // Plays a given animation clip before returning to the animator state.
        // Does not interfere with the animator
        // 
        // It applies a linear blend between the two animations with a length of blendSeconds or half the clip length, whichever is shorter.
        // This stops any problems with the blend in and blend out overlapping;
        // 
        // It will check if the animation is already playing, and if so nothing will happen unless skipAlreadyPlayingCheck is true
        //
        // If loop animation is true, it will continue to play until Stop Animation is called.
        public void PlayAnimationClip(AnimationClip clip, float blendSeconds = 0.1f, bool skipAlreadyPlayingCheck = false, bool loopAnimation = false)
        {
            if (!skipAlreadyPlayingCheck && _oneShotPlayable.IsValid() && _oneShotPlayable.GetAnimationClip() == clip) return;

            InterruptOneShot();

            _lastAnimationMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
            _lastAnimationMixer.SetInputWeight(1, _currentOneShotWeight);

            _oneShotPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _animatorMixer.ConnectInput(1, _oneShotPlayable, 0);

            if (loopAnimation)
            {
                _oneShotBlendingCoroutine = StartCoroutine(crBlendIn(blendSeconds));
            }
            else
            {
                _oneShotBlendingCoroutine = StartCoroutine(crBlendInOut(blendSeconds));
            }
        }

        // Plays an animation clip before returning to the last used animation clip
        // Use this if you want to return to a looping PlayAnimationClip animation after playing the clip
        //
        // All variables are the same as PlayAnimationClip
        public void InsertAnimationClip(AnimationClip clip, float blendSeconds, bool skipAlreadyPlayingCheck = false)
        {
            if (!skipAlreadyPlayingCheck && _lastAnimationPlayable.IsValid() && _lastAnimationPlayable.GetAnimationClip() == clip) return;

            if (_insertionBlendingCoroutine != null) StopCoroutine(_insertionBlendingCoroutine);

            _insertionBlendingCoroutine = StartCoroutine(crInsertBlendInOut(blendSeconds, clip));
        }

        // Stops any animation clip being played by this Animation Handler
        public void StopAnimation()
        {
            InterruptOneShot();
        }

        // Returns the animation clip being played by this Animation Handler, if any
        public AnimationClip GetCurrentAnimationClip()
        {
            //if (!_oneShotPlayable.IsValid()) return null;
            return _oneShotPlayable.GetAnimationClip();
        }


        // ----------------------------------------------------------- Message/Callback Methods

        private void OnEnable()
        {
            _playableGraph = PlayableGraph.Create(gameObject.name + " Animation Graph");
            //_playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);

            _animatorMixer = AnimationMixerPlayable.Create(_playableGraph, 2);
            playableOutput.SetSourcePlayable(_animatorMixer);

            _lastAnimationMixer = AnimationMixerPlayable.Create(_playableGraph, 2);
            _animatorMixer.ConnectInput(0, _lastAnimationMixer, 0);

            _animatorPlayable = AnimatorControllerPlayable.Create(_playableGraph, _animator.runtimeAnimatorController);
            _lastAnimationMixer.ConnectInput(0, _animatorPlayable, 0);
            _lastAnimationMixer.SetInputWeight(0, 1f);
            _playableGraph.GetRootPlayable(0).SetInputWeight(0, 1f);

            _playableGraph.Play();
        }
        private void OnDisable()
        {
            _playableGraph.Destroy();
        }


        // ----------------------------------------------------------- Internal Methods

        void InterruptOneShot()
        {
            if (_oneShotBlendingCoroutine != null) StopCoroutine(_oneShotBlendingCoroutine);
            if (_insertionBlendingCoroutine != null) StopCoroutine(_insertionBlendingCoroutine);

            _animatorMixer.SetInputWeight(0, 1f);
            _animatorMixer.SetInputWeight(1, 0f);

            DisconnectOneshot();
        }

        void DisconnectOneshot()
        {
            if (_oneShotPlayable.IsValid())
            {
                if (_lastAnimationPlayable.IsValid())
                {
                    _lastAnimationMixer.DisconnectInput(1);
                    _playableGraph.DestroyPlayable(_lastAnimationPlayable);
                }

                _lastAnimationPlayable = AnimationClipPlayable.Create(_playableGraph, _oneShotPlayable.GetAnimationClip());
                _lastAnimationPlayable.SetTime(_oneShotPlayable.GetTime());
                _lastAnimationMixer.ConnectInput(1, _lastAnimationPlayable, 0);

                _pauseBlend = false;

                _animatorMixer.DisconnectInput(1);
                _playableGraph.DestroyPlayable(_oneShotPlayable);
            }
        }


        // ----------------------------------------------------------- Blending Coroutines

        IEnumerator crBlendInOut(float blendSeconds)
        {
            if (!_oneShotPlayable.IsValid()) yield break;
            float clipSeconds = _oneShotPlayable.GetAnimationClip().length;

            if (blendSeconds > clipSeconds / 2) blendSeconds = clipSeconds / 2;

            // Blend In
            float blendTime = 0;
            while (blendTime < 1 && blendSeconds > 0)
            {
                // Handle Pause
                if (!_pauseBlend)
                {
                    blendTime += Time.deltaTime / blendSeconds;
                    _currentOneShotWeight = blendTime;

                    _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
                    _animatorMixer.SetInputWeight(1, _currentOneShotWeight);
                }
                yield return null;
            }

            blendTime = 1;
            _currentOneShotWeight = blendTime;

            _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
            _animatorMixer.SetInputWeight(1, _currentOneShotWeight);



            // Wait
            blendTime = 0;
            while (blendTime < clipSeconds - (2 * blendSeconds))
            {
                yield return null;

                if (_pauseBlend) continue;
                blendTime += Time.deltaTime;
            }

            // Reset to Animator
            _lastAnimationMixer.SetInputWeight(1, 0f);
            _lastAnimationMixer.SetInputWeight(0, 1f);



            //Blend Out
            blendTime = 1;
            while (blendTime > 0 && blendSeconds > 0)
            {
                // Handle Pause
                if (!_pauseBlend)
                {
                    blendTime -= Time.deltaTime / blendSeconds;
                    _currentOneShotWeight = blendTime;

                    _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
                    _animatorMixer.SetInputWeight(1, _currentOneShotWeight);
                }

                yield return null;
            }

            blendTime = 0;
            _currentOneShotWeight = blendTime;

            _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
            _animatorMixer.SetInputWeight(1, _currentOneShotWeight);

            DisconnectOneshot();
        }

        IEnumerator crBlendIn(float blendSeconds)
        {
            if (!_oneShotPlayable.IsValid()) yield break;
            float clipSeconds = _oneShotPlayable.GetAnimationClip().length;

            if (blendSeconds > clipSeconds / 2) blendSeconds = clipSeconds / 2;

            // Blend In
            float blendTime = 0;
            while (blendTime < 1 && blendSeconds > 0)
            {
                // Handle Pause
                if (_pauseBlend)
                {
                    yield return null;
                    continue;
                }

                blendTime += Time.deltaTime / blendSeconds;
                _currentOneShotWeight = blendTime;

                _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
                _animatorMixer.SetInputWeight(1, _currentOneShotWeight);

                yield return null;
            }

            blendTime = 1;
            _currentOneShotWeight = blendTime;

            _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
            _animatorMixer.SetInputWeight(1, _currentOneShotWeight);
        }

        IEnumerator crBlendOut(float blendSeconds)
        {
            if (!_oneShotPlayable.IsValid()) yield break;
            float clipSeconds = _oneShotPlayable.GetAnimationClip().length;

            if (blendSeconds > clipSeconds / 2) blendSeconds = clipSeconds / 2;
            
            // Reset to Animator
            _lastAnimationMixer.SetInputWeight(1, 0f);
            _lastAnimationMixer.SetInputWeight(0, 1f);

            // Blend Out
            float blendTime = 0;
            while (blendTime > 0 && blendSeconds > 0)
            {
                // Handle Pause
                if (_pauseBlend)
                {
                    yield return null;
                    continue;
                }

                blendTime -= Time.deltaTime / blendSeconds;
                _currentOneShotWeight = blendTime;

                _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
                _animatorMixer.SetInputWeight(1, _currentOneShotWeight);

                yield return null;
            }

            blendTime = 0;
            _currentOneShotWeight = blendTime;

            _animatorMixer.SetInputWeight(0, 1 - _currentOneShotWeight);
            _animatorMixer.SetInputWeight(1, _currentOneShotWeight);

            DisconnectOneshot();
        }

        // Plays an animation clip and returns to the already playing animation.
        // Does not change the return value of "GetCurrentAnimation".
        // Use for flourishes and things like blockstun that need to return to their previous animation after.
        IEnumerator crInsertBlendInOut(float blendSeconds, AnimationClip clip)
        {
            if (!_oneShotPlayable.IsValid()) yield break;

            // Use Last Animation to blend
            if (_lastAnimationPlayable.IsValid())
            {
                _lastAnimationMixer.DisconnectInput(1);
                _playableGraph.DestroyPlayable(_lastAnimationPlayable);
            }
            _lastAnimationPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _lastAnimationMixer.ConnectInput(1, _lastAnimationPlayable, 0);

            _lastAnimationMixer.SetInputWeight(0, 0f);
            _lastAnimationMixer.SetInputWeight(1, 1f);

            _oneShotPlayable.Pause();
            _pauseBlend = true;

            // Blend Proper
            float clipSeconds = _oneShotPlayable.GetAnimationClip().length;

            if (blendSeconds > clipSeconds / 2) blendSeconds = clipSeconds / 2;

            // Blend In
            float blendTime = 0;
            while (blendTime < 1 && blendSeconds > 0)
            {
                blendTime += Time.deltaTime / blendSeconds;

                _animatorMixer.SetInputWeight(0, blendTime);
                _animatorMixer.SetInputWeight(1, 1 - blendTime);

                yield return null;
            }

            blendTime = 1;

            _animatorMixer.SetInputWeight(0, blendTime);
            _animatorMixer.SetInputWeight(1, 1 - blendTime);

            yield return new WaitForSeconds(clipSeconds - (2 * blendSeconds));

            // Reset to Animator

            //Blend Out
            while (blendTime > 0 && blendSeconds > 0)
            {
                blendTime -= Time.deltaTime / blendSeconds;

                _animatorMixer.SetInputWeight(0, blendTime);
                _animatorMixer.SetInputWeight(1, 1 - blendTime);

                yield return null;
            }

            blendTime = 0;

            _animatorMixer.SetInputWeight(0, blendTime);
            _animatorMixer.SetInputWeight(1, 1 - blendTime);

            _oneShotPlayable.Play();
            _pauseBlend = false;
        }


        // Made using git-amend's: https://www.youtube.com/watch?v=fQzKJO-0dS8
    }
}
