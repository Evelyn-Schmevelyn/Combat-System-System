using UnityEngine;

namespace CombatSystemSystem
{
    [System.Serializable]
    public struct InputData
    {
        public string buttonName;
        public InputMotion motion;

        [System.Serializable]
        public struct InputMotion
        {
            public InputMotion(DirectionUnit[] motionsList)
            {
                this.motionsList = motionsList;
            }

            public DirectionUnit[] motionsList;
        }

        [System.Serializable]
        public struct DirectionUnit
        {
            public DirectionUnit(int direction, int requiredDuration, int window, bool isStrict)
            {
                this.direction = direction;
                this.duration = requiredDuration;
                this.window = window;
                this.strict = isStrict;
            }

            public int direction;
            public int duration;
            public int window;
            public bool strict;
        }
    }
}
