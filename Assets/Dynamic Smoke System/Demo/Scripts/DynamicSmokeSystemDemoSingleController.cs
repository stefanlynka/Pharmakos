using System;
using DynamicSmokeSystem;
using UnityEngine;

namespace Dynamic_Smoke_System.Demo {
    public class DynamicSmokeSystemDemoSingleController : MonoBehaviour {
        [SerializeField] private DynamicSmokeSystemController dynamicSmokeSystemController;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float cameraRotationSpeed = 10;
        [SerializeField] private float bulletRadius = 0.3f;
        [SerializeField] private float ballShootForce = 500;
        [SerializeField] private DynamicSmokeSystemDemoBallController ballControllerPrefab;

        private new Camera camera;
        
        private void Start() {
            camera = Camera.main;
            
            Emit();
        }
        
        public void Emit() {
            dynamicSmokeSystemController.Emit(transform.position);
        }

        public void Disperse() {
            dynamicSmokeSystemController.Disperse();
        }
        
        private void Update() {
            cameraPivot.Rotate(Vector3.up, cameraRotationSpeed * Time.deltaTime);

            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Input.GetMouseButtonDown(0)) {
                dynamicSmokeSystemController.AddProjectile(
                    new LineProjectileModel(
                        ray.origin, 
                        ray.origin + ray.direction * 100,
                        bulletRadius));
            } 
            else if (Input.GetMouseButtonDown(1)) {
                var instance = Instantiate(ballControllerPrefab.gameObject);
                instance.transform.position = camera.transform.position;
                var rigidbody = instance.GetComponent<Rigidbody>();
                rigidbody.AddForce(ray.direction * ballShootForce);
            }
        }
    }
}