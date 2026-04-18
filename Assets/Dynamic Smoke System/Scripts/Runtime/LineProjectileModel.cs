using UnityEngine;

namespace DynamicSmokeSystem {
    public class LineProjectileModel : AbstractProjectileModel {
        public LineProjectileModel(Vector3 start, Vector3 end, float radius) {
            OmissionModel.posA = start;
            OmissionModel.posB = end;
            OmissionModel.type = 0;
            Radius = radius;
        }
    }
}