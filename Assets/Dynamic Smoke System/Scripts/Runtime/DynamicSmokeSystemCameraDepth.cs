using System;
using UnityEngine;

namespace DynamicSmokeSystem {
    public class DynamicSmokeSystemCameraDepth : MonoBehaviour {
        private void Awake() {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}