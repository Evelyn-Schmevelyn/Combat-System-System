using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace CombatSystemSystem
{
    [CreateAssetMenu(fileName = "NewAnimationDictionary", menuName = "Combat System System/Animation Dictionary")]
    public class AnimationDictionary : ScriptableObject, ISerializationCallbackReceiver
    {
        public Dictionary<string, AnimationClip> animations { get; private set; }
        [SerializeField] DataSet[] _animationDatas;

        // Do not call these remotely. They are for serialization only
        public void OnBeforeSerialize()
        {
            animations = new Dictionary<string, AnimationClip>();
            foreach (var animationData in _animationDatas)
            {
                if (!animations.ContainsKey(animationData.name)) animations.Add(animationData.name == "" ? animationData.clip.name : animationData.name, animationData.clip);
            }
        }
        public void OnAfterDeserialize()
        {
            animations = new Dictionary<string, AnimationClip>();
            foreach (var animationData in _animationDatas)
            {
                if(!animations.ContainsKey(animationData.name)) animations.Add(animationData.name, animationData.clip);
            }
        }

        [System.Serializable]
        struct DataSet
        {
            public string name;
            public AnimationClip clip;
        }
    }
}
