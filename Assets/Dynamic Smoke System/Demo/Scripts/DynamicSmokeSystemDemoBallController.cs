using System;
using DynamicSmokeSystem;
using UnityEngine;

namespace Dynamic_Smoke_System.Demo {
    public class DynamicSmokeSystemDemoBallController : MonoBehaviour {
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private float minDistance = 0.5f;
        [SerializeField] private float lifetime = 5;
        
        private DynamicSmokeSystemController[] smokeSystemControllers;
        private Vector3 lastPosition;
        private float instanceLifetime;

        private void Start() {
            lastPosition = transform.position;
            smokeSystemControllers = FindObjectsOfType<DynamicSmokeSystemController>();
        }

        private void Update() {
            instanceLifetime += Time.deltaTime;

            if (instanceLifetime >= lifetime) {
                Destroy(gameObject);
                return;
            }
            
            var currentPosition = transform.position;
            var delta = currentPosition - lastPosition;

            if (!(delta.magnitude >= minDistance)) return;

            var projectile = new SphereProjectileModel(transform.position, radius);
            
            foreach (var dynamicSmokeSystemController in smokeSystemControllers) {
                dynamicSmokeSystemController.AddProjectile(projectile);
            } 
            
            lastPosition = currentPosition;
        }
    }
}