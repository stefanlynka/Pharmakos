using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicSmokeSystem {
    public class DynamicSmokeSystemController : MonoBehaviour {
        private static readonly HashSet<DynamicSmokeSystemController> Instances = new HashSet<DynamicSmokeSystemController>();

        [SerializeField] private int amount = 100;
        [SerializeField] private int amountMaxPerFrame = 100;
        [SerializeField] private DynamicSmokeSystemExpansionController.ExpansionShape cloudShape = DynamicSmokeSystemExpansionController.ExpansionShape.Sphere;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float smooth = 15;
        [SerializeField] private float expansionDuration = 0.5f;
        [SerializeField] private AnimationCurve expansionCurve;
        [SerializeField] private float disperseDuration = 2;
        [SerializeField] private AnimationCurve disperseCurve;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private int sdfTextureResolution = 64;

        private Material material;
        private Bounds bounds;
        private DynamicSmokeSystemExpansionController expansionController = new DynamicSmokeSystemExpansionController();
        private DynamicSmokeSystemRaymarchController raymarchController;
        private DynamicSmokeSystemOmissionController omissionController;

        private bool isInitialized;
        private bool hasEmitted;
        private Vector3 lastEmitPosition;

        private void Initialize() {
            if (isInitialized) return;
            isInitialized = true;
            
            raymarchController = GetComponent<DynamicSmokeSystemRaymarchController>();
            omissionController = GetComponent<DynamicSmokeSystemOmissionController>();
            material = GetComponent<Renderer>().material;
        }
        
        private void Awake() {
            Instances.Add(this);
            Initialize();
        }

        private void OnEnable() {
            Instances.Add(this);
        }

        private void OnDisable() {
            Instances.Remove(this);
        }

        private void Update() {
            material.SetVector("_boundsMin", bounds.min);
            material.SetVector("_boundsMax", bounds.max);
            material.SetFloat("_Radius", cellSize);
            material.SetFloat("_Smooth", 1f / cellSize * 2.5f);
        }
        
        public void Emit(Vector3 position) {
            StopAllCoroutines();
            EmitInternal(position);
        }

        public void Emit(Vector3 position, float disperseDelay) {
            StopAllCoroutines();
            EmitInternal(position);
            DisperseInternal(disperseDelay);
        }

        private void EmitInternal(Vector3 position) {
            Initialize();
            lastEmitPosition = position;
            hasEmitted = true;
            
            transform.position = position;

            omissionController.Clear();
            StartCoroutine(EmitCoroutine());
            StartCoroutine(AnimateMaskCoroutine(1, expansionDuration, expansionCurve));
        }

        public void AddProjectile(AbstractProjectileModel projectile) {
            Initialize();
            
            omissionController.AddProjectile(projectile);
        }

        public void Disperse() {
            StopAllCoroutines();
            DisperseInternal();
        }

        public void Rebuild() {
            if (!hasEmitted) {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(RebuildCoroutine());
        }

        public static void RebuildAll() {
            foreach (var instance in Instances) {
                if (instance == null) {
                    continue;
                }

                instance.Rebuild();
            }
        }

        private void DisperseInternal(float delay = 0) {
            Initialize();
            
            StartCoroutine(DisperseAfterDelayCoroutine(delay));
        }

        private IEnumerator DisperseAfterDelayCoroutine(float disperseDelay) {
            yield return new WaitForSeconds(disperseDelay);
            
            var maskStartValue = material.GetFloat("_Mask");
            
            yield return AnimateMaskCoroutine(maskStartValue, disperseDuration, disperseCurve);
        }

        private IEnumerator RebuildCoroutine() {
            yield return DisperseAfterDelayCoroutine(0f);
            EmitInternal(lastEmitPosition);
        }

        private IEnumerator EmitCoroutine() {
            raymarchController.Resolution = sdfTextureResolution;
            raymarchController.Radius = cellSize;
            raymarchController.Smooth = smooth;
            var gpuTexture = raymarchController.Initialize(amount);
            var texture = Instantiate(gpuTexture);
            
            material.SetTexture("_SdfTexture", texture);
            material.SetVector("_MaskPos", transform.position);
            
            expansionController.CellSize = cellSize;
            expansionController.Position = transform.position;
            expansionController.LayerMask = layerMask;
            expansionController.Shape = cloudShape;
            expansionController.InitializeExpand(amount, amountMaxPerFrame);

            while (expansionController.ContinueExpandIfNeeded()) {
                transform.position = bounds.center;
                transform.localScale = bounds.size;
                Graphics.CopyTexture(gpuTexture, texture);
                
                bounds = expansionController.Bounds;
                bounds.size += Vector3.one * cellSize * 4;

                raymarchController.Bounds = bounds;
                raymarchController.UpdateTexture(expansionController.ActiveCells.ToArray());
                
                yield return null;
            }
            
            material.SetTexture("_SdfTexture", gpuTexture);
            texture.Release();
        }

        private IEnumerator AnimateMaskCoroutine(float multiplier, float duration, AnimationCurve curve) {
            var t = 0f;

            while (t < duration) {
                t += Time.deltaTime;
                var f = Mathf.Clamp01(t / duration);
                material.SetFloat("_Mask", curve.Evaluate(f) * multiplier);
                
                yield return null;
            }
        }

        private void OnDestroy() {
            raymarchController.Release();

            if (material != null) {
                Destroy(material);
            }
        }

        private void OnDrawGizmos() {
            expansionController.OnDrawGizmos();
        }
    }
}