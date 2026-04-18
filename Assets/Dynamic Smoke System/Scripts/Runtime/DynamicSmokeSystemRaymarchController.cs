using System;
using UnityEngine;

namespace DynamicSmokeSystem {
    internal class DynamicSmokeSystemRaymarchController : MonoBehaviour {
        [SerializeField] private ComputeShader computeShader;
        
        public int Resolution { get; set; }
        public float Radius { get; set; }
        public float Smooth { get; set; }
        public Bounds Bounds { get; set; }

        private bool isInitialized;
        private ComputeBuffer positionsBuffer;
        private ComputeBuffer omissionBuffer;
        
        private Vector3[] positions = Array.Empty<Vector3>();
        private OmissionModel[] omissions = Array.Empty<OmissionModel>();
        
        [SerializeField] private RenderTexture renderTexture;
        
        public RenderTexture Initialize(int amount) {
            positionsBuffer?.Release();
            positionsBuffer = new ComputeBuffer(amount, 3 * sizeof(float));

            omissionBuffer?.Release();
            omissionBuffer = new ComputeBuffer(DynamicSmokeSystemOmissionController.BufferSize, 8 * sizeof(float) + 1 * sizeof(uint));

            if (renderTexture == null) {
                renderTexture = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                renderTexture.volumeDepth = Resolution;
                renderTexture.enableRandomWrite = true;
                renderTexture.name = "Dynamic Smoke System (RenderTexture)";
                renderTexture.Create();
            }

            isInitialized = true;
            
            return renderTexture;
        }
        
        public void Release() {
            positionsBuffer?.Release();
            omissionBuffer?.Release();

            if (renderTexture != null) {
                Destroy(renderTexture);
            }
        }

        public void UpdateTexture(Vector3[] positions) {
            if (!isInitialized) return;

            this.positions = positions;
            
            SetShaderVariables();

            var groups = Mathf.CeilToInt(Resolution / 8f);
            computeShader.Dispatch(0, groups, groups, groups);
        }

        public void UpdateOmissions(OmissionModel[] omissions) {
            if (!isInitialized) return;

            this.omissions = omissions;
            
            SetShaderVariables();
            
            var groups = Mathf.CeilToInt(Resolution / 8f);
            computeShader.Dispatch(1, groups, groups, groups);
        }

        private void SetShaderVariables() {
            positionsBuffer.SetData(positions);
            omissionBuffer.SetData(omissions);

            computeShader.SetInt("_Resolution", Resolution);
            computeShader.SetVector("_BoundsMin", Bounds.min);
            computeShader.SetVector("_BoundsMax", Bounds.max);
            computeShader.SetFloat("_Radius", Radius);
            computeShader.SetFloat("_Smooth", Smooth);
            computeShader.SetTexture(0, "_Result", renderTexture);
            computeShader.SetTexture(1, "_Result", renderTexture);

            computeShader.SetInt("_PositionCount", positions.Length);
            computeShader.SetBuffer(0, "_Positions", positionsBuffer);
            
            computeShader.SetInt("_OmissionCount", omissions.Length);
            computeShader.SetBuffer(1, "_Omissions", omissionBuffer);
        }
    }
}