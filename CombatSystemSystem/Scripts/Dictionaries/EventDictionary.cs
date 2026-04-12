using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CombatSystemSystem
{
    [AddComponentMenu("Combat System System/Event Dictionary")]
    public class EventDictionary : MonoBehaviour
    {
        [Header("IMPORTANT: This feature is unimplemented. \n Do not use this for now")]
        public List<DataSet> events;

        public UnityEvent<GameObject, GameObject> GetEvent(string name)
        {
            return events.Find(e => { return e.name == name; }).eventToCall;
        }

        [System.Serializable]
        public struct DataSet
        {
            public string name;

            // Source, Target
            public UnityEvent<GameObject, GameObject> eventToCall;
        }
    }
}
