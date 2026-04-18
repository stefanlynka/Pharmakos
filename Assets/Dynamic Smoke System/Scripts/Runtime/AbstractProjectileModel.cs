using UnityEngine;

namespace DynamicSmokeSystem {
    public abstract class AbstractProjectileModel {
        internal bool IsCompleted { get; set; }
        
        public float GrowDuration { get; set; } = 0.1f;
        public float StayDuration { get; set; } = 2;
        public float ShrinkDuration { get; set; } = 3f;
        
        public float DurationMultiplier { get; set; } = 1f;

        private int state;
        private float time;
        protected float Radius;
        internal OmissionModel OmissionModel;
        
        public void Update() {
            switch (state) {
                case 0:
                    UpdateRadiusAndStateIfNecessary(GrowDuration * DurationMultiplier, 1);
                    break;
                
                case 1:
                    UpdateStateIfNecessary(StayDuration * DurationMultiplier);

                    break;
                
                case 2:
                    if (UpdateRadiusAndStateIfNecessary(ShrinkDuration * DurationMultiplier, -1)) {
                        IsCompleted = true;
                    }
                    
                    break;
            }
            
            time += UnityEngine.Time.deltaTime;
        }

        private bool UpdateRadiusAndStateIfNecessary(float duration, float direction) {
            var t = Mathf.Clamp01(time / duration);
            
            if (direction < 0) {
                t = 1 - t;
            }
            
            OmissionModel.r = Radius * t;
            OmissionModel.a = t;

            return UpdateStateIfNecessary(duration);
        }

        private bool UpdateStateIfNecessary(float duration) {
            if (time < duration) return false;
            
            state++;
            time = 0;
            
            return true;
        }
        
        internal OmissionModel GetShaderModel() {
            return OmissionModel;
        }
    }
}