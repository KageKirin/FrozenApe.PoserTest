using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrozenAPE;
using Unity.Serialization.Json;
using UnityEngine;

namespace Game
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        public enum State
        {
            Idle = 0,
            Pose,
            Freeze,
            Export,
            Exit,
        }

        public State state = State.Idle;
    }
}
