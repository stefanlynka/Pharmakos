using System;
using UnityEngine;

namespace DynamicSmokeSystem {
    [Serializable]
    internal struct OmissionModel {
        public Vector3 posA;
        public Vector3 posB;
        public float r;
        public float a;
        public uint type;
    }
}