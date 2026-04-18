using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicSmokeSystem {
    internal class DynamicSmokeSystemOmissionController : MonoBehaviour {
        public const int BufferSize = 500;
        
        private DynamicSmokeSystemRaymarchController dynamicSmokeSystemRaymarchController;
        private List<AbstractProjectileModel> activeProjectiles = new List<AbstractProjectileModel>();

        private void Awake() {
            dynamicSmokeSystemRaymarchController = GetComponent<DynamicSmokeSystemRaymarchController>();
        }

        private void Update() {
            var omissionModels = new List<OmissionModel>();
            var removed = 0;

            for (var i = activeProjectiles.Count - 1; i >= 0; i--) {
                var projectile = activeProjectiles[i];
                projectile.Update();
                
                if (projectile.IsCompleted) {
                    activeProjectiles.RemoveAt(i);
                    removed++;
                }
                else {
                    omissionModels.Add(projectile.GetShaderModel());
                }
            }

            if (omissionModels.Count > 0 || removed > 0) {
                dynamicSmokeSystemRaymarchController.UpdateOmissions(omissionModels.ToArray());
            }
        }

        internal void AddProjectile(AbstractProjectileModel projectile) {
            if (activeProjectiles.Count >= BufferSize) {
                activeProjectiles.RemoveAt(0);
            }
            
            activeProjectiles.Add(projectile);
        }

        public void Clear() {
            activeProjectiles.Clear();
            dynamicSmokeSystemRaymarchController.UpdateOmissions(Array.Empty<OmissionModel>());
        }
    }
}