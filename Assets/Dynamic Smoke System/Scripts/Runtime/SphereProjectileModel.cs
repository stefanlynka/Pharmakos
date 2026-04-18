using UnityEngine;

namespace DynamicSmokeSystem {
    public class SphereProjectileModel : AbstractProjectileModel {
        public Vector3 Position {
            get => OmissionModel.posA;
            set => OmissionModel.posA = value;
        }
        
        public SphereProjectileModel(Vector3 position, float radius) {
            OmissionModel.posA = position;
            OmissionModel.type = 1;
            Radius = radius;
        }

        public SphereProjectileModel Clone() {
            return new SphereProjectileModel(OmissionModel.posA, Radius);
        }
    }
}