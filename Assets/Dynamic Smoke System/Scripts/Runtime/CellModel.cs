using UnityEngine;

namespace DynamicSmokeSystem {
    internal struct CellModel {
        public int x;
        public int y;
        public int z;

        public CellModel(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static CellModel operator +(CellModel a, CellModel b)
            => new CellModel(a.x + b.x, a.y + b.y, a.z + b.z);        
        

        public static implicit operator Vector3(CellModel c) => new Vector3(c.x, c.y, c.z);

        public void Add(CellModel add) {
            x += add.x;
            y += add.y;
            z += add.z;
        }
    }
}