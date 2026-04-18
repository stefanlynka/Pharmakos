//#define DEBUG_ON

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicSmokeSystem {
    internal class DynamicSmokeSystemExpansionController {
        public enum ExpansionShape {
            Sphere,
            Disk
        }

        public List<Vector3> ActiveCells => activeCells;
        public Bounds Bounds;
        public float CellSize { get; set; }
        public Vector3 Position { get; set; }
        public LayerMask LayerMask { get; set; }
        public ExpansionShape Shape { get; set; }
        private List<Vector3> activeCells = new List<Vector3>();

        private int amountLeft;
        private int amountMaxPerFrame;
        
        private Queue<CellModel> queue = new Queue<CellModel>();
        private Collider[] collidersNonAlloc = new Collider[1];

        private bool[][][] walkedCells;
        private CellModel minCell;
        private CellModel maxCell;

        private static readonly CellModel[] SpherePermutations = new CellModel[26] {
            new CellModel(1, 0, 0),
            new CellModel(-1, 0, 0),
            new CellModel(0, 1, 0),
            new CellModel(0, -1, 0),
            new CellModel(0, 0, 1),
            new CellModel(0, 0, -1),
            new CellModel(1, 1, 0),
            new CellModel(1, -1, 0),
            new CellModel(-1, 1, 0),
            new CellModel(-1, -1, 0),
            new CellModel(1, 0, 1),
            new CellModel(1, 0, -1),
            new CellModel(-1, 0, 1),
            new CellModel(-1, 0, -1),
            new CellModel(0, 1, 1),
            new CellModel(0, 1, -1),
            new CellModel(0, -1, 1),
            new CellModel(0, -1, -1),
            new CellModel(1, 1, 1),
            new CellModel(1, 1, -1),
            new CellModel(1, -1, 1),
            new CellModel(1, -1, -1),
            new CellModel(-1, 1, 1),
            new CellModel(-1, 1, -1),
            new CellModel(-1, -1, 1),
            new CellModel(-1, -1, -1)
        };

        private static readonly CellModel[] DiskPermutations = new CellModel[8] {
            new CellModel(1, 0, 0),
            new CellModel(-1, 0, 0),
            new CellModel(0, 0, 1),
            new CellModel(0, 0, -1),
            new CellModel(1, 0, 1),
            new CellModel(1, 0, -1),
            new CellModel(-1, 0, 1),
            new CellModel(-1, 0, -1)
        };

        private CellModel startCellPosition;
        
#if DEBUG_ON
        private Queue<(Vector3 Pos, bool Ok)> debug = new Queue<(Vector3 Pos, bool Ok)>();
#endif

        public void InitializeExpand(int totalAmount, int maxAmountPerFrame) {
            amountLeft = totalAmount;
            amountMaxPerFrame = maxAmountPerFrame;

            minCell = maxCell = new CellModel();
            activeCells.Clear();
            queue.Clear();

            totalAmount /= 30;
            
            if (totalAmount % 2 != 0) {
                totalAmount++;
            }

            if (walkedCells == null) {
                InitializeCellsLookupTable(totalAmount);
            }
            else {
                ResetCellsLookupTable();
            }

            startCellPosition = GetCellAt(Position);
            queue.Enqueue(new CellModel());
            Bounds = new Bounds(Position, Vector3.zero);
        }

        private void ResetCellsLookupTable() {
            for (var i = 0; i < walkedCells.Length; i++) {
                for (var j = 0; j < walkedCells[i].Length; j++) {
                    for (var k = 0; k < walkedCells[i][j].Length; k++) {
                        walkedCells[i][j][k] = false;
                    }
                }
            }
        }

        private void InitializeCellsLookupTable(int totalAmount) {
            walkedCells = new bool[totalAmount][][];

            for (var i = 0; i < totalAmount; i++) {
                walkedCells[i] = new bool[totalAmount][];

                for (var j = 0; j < totalAmount; j++) {
                    walkedCells[i][j] = new bool[totalAmount];
                }
            }
        }

        public bool ContinueExpandIfNeeded() {
#if DEBUG_ON
            Debug.Break();
#endif
            
            var handledThisFrame = 0;
            var halfTotalSize = walkedCells.GetLength(0) / 2;

            var worldPosition = new Vector3();
            var indexer = new CellModel();
            var halfCellSize = Vector3.one * (CellSize * 0.5f);
            var permutations = Shape == ExpansionShape.Disk ? DiskPermutations : SpherePermutations;
            
            while (queue.Count > 0 && amountLeft > 0 && handledThisFrame < amountMaxPerFrame) {
                var cell = queue.Dequeue();
                indexer.x = halfTotalSize + cell.x; 
                indexer.y = halfTotalSize + cell.y;
                indexer.z = halfTotalSize + cell.z;

                if (walkedCells[indexer.x][indexer.y][indexer.z]) {
                    continue;
                }
                
                handledThisFrame++;
                
                worldPosition.x = (startCellPosition.x + cell.x) * CellSize; 
                worldPosition.y = (startCellPosition.y + cell.y) * CellSize; 
                worldPosition.z = (startCellPosition.z + cell.z) * CellSize;

                //var colliders = Physics.OverlapSphereNonAlloc(worldPosition, halfCellSize, collidersNonAlloc, LayerMask);
                var colliders = Physics.OverlapBoxNonAlloc(worldPosition, halfCellSize, collidersNonAlloc, Quaternion.identity, LayerMask);

                walkedCells
                    [indexer.x]
                    [indexer.y]
                    [indexer.z] = true;

#if DEBUG_ON
                (Vector3 Pos, bool Ok) debugEntry;
                
                if (debug.Count >= amountMaxPerFrame) {
                    debugEntry = debug.Dequeue();
                    debugEntry.Pos = worldPosition;
                    debugEntry.Ok = colliders <= 0;
                }
                else {
                    debugEntry = (Pos: worldPosition, Ok: colliders <= 0);
                }
                
                debug.Enqueue(debugEntry);
#endif
                
                var isInitialCellFound = activeCells.Count > 0;
                
                // if the current cell is blocked, skip expanding on it.
                // Do not skip if we don't have any cells yet, because in that case we still need to find our first available cell.
                if (colliders > 0 && isInitialCellFound) {
                    continue;
                }

                // if first cell was not found yet, raycast to check if cell is reachable
                // from the emission point
                if (!isInitialCellFound) {
                    var vector = worldPosition - Position;
                    
                    if (Physics.Raycast(Position, vector.normalized, vector.magnitude, LayerMask)) {
                        continue;
                    }
                }
                
                activeCells.Add(worldPosition);
                amountLeft--;

                if (cell.x > maxCell.x || cell.y > maxCell.y || cell.z > maxCell.z) {
                    maxCell.x = Mathf.Max(maxCell.x, cell.x);
                    maxCell.y = Mathf.Max(maxCell.y, cell.y);
                    maxCell.z = Mathf.Max(maxCell.z, cell.z);
                }

                if (cell.x < minCell.x || cell.y < minCell.y || cell.z < minCell.z) {
                    minCell.x = Mathf.Min(minCell.x, cell.x);
                    minCell.y = Mathf.Min(minCell.y, cell.y);
                    minCell.z = Mathf.Min(minCell.z, cell.z);
                }
                
                for (var i = 0; i < permutations.Length; i++) {
                    if (walkedCells
                            [indexer.x + permutations[i].x]
                        [indexer.y + permutations[i].y]
                        [indexer.z + permutations[i].z]) continue;
                    
                    queue.Enqueue(cell + permutations[i]);
                }
            }

            Bounds.SetMinMax(
                (Vector3) (startCellPosition + minCell) * CellSize, 
                (Vector3) (startCellPosition + maxCell) * CellSize);
            
            return handledThisFrame > 0;
        }

        private CellModel GetCellAt(Vector3 position) {
            return new CellModel(
                Mathf.RoundToInt(position.x / CellSize),
                Mathf.RoundToInt(position.y / CellSize),
                Mathf.RoundToInt(position.z / CellSize));
        }
        

        public void OnDrawGizmos() {
#if DEBUG_ON
            foreach (var (pos, ok) in debug) {
                Gizmos.color = ok ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos, Vector3.one * CellSize);
            }
#endif
        }
    }
}