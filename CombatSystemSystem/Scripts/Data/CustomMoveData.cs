using System.Collections.Generic;
using UnityEngine;

namespace CombatSystemSystem
{
    [System.Serializable]
    public struct CustomMoveData
    {

        [Header("IMPORTANT: This feature is unimplemented. \n Do not use this for now")]
        [SerializeField] List<BoolDataSet> _customBoolData;
        [SerializeField] List<IntDataSet> _customIntData;
        [SerializeField] List<FloatDataSet> _customFloatData;

        public Dictionary<string, bool> GetBools(int index)
        {
            Dictionary<string, bool> keyValuePairs = new Dictionary<string, bool>();

            foreach (var data in _customBoolData)
            {
                if (data.values.Length > index) keyValuePairs.Add(data.name, data.values[index]);
            }

            return keyValuePairs;
        }
        public Dictionary<string, int> GetInts(int index)
        {
            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();

            foreach (var data in _customIntData)
            {
                if (data.values.Length > index) keyValuePairs.Add(data.name, data.values[index]);
            }

            return keyValuePairs;
        }
        public Dictionary<string, float> GetFloats(int index)
        {
            Dictionary<string, float> keyValuePairs = new Dictionary<string, float>();

            foreach (var data in _customFloatData)
            {
                if (data.values.Length > index) keyValuePairs.Add(data.name, data.values[index]);
            }

            return keyValuePairs;
        }


        [System.Serializable]
        public struct BoolDataSet
        {
            public string name;
            public bool[] values;

            public BoolDataSet(string name, int length) 
            {
                this.name = name;
                this.values = new bool[length];
            }
        }
        [System.Serializable]
        public struct IntDataSet
        {
            public string name;
            public int[] values;

            public IntDataSet(string name, int length)
            {
                this.name = name;
                this.values = new int[length];
            }
        }
        [System.Serializable]
        public struct FloatDataSet
        {
            public string name;
            public float[] values;

            public FloatDataSet(string name, int length)
            {
                this.name = name;
                this.values = new float[length];
            }
        }
    }
}
