using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Гекс в offset-координатах (odd-r: нечётные ряды сдвинуты вправо).</summary>
    public struct Hex : System.IEquatable<Hex>
    {
        public int Col;
        public int Row;

        public Hex(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public bool Equals(Hex other)
        {
            return Col == other.Col && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is Hex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Col * 73856093) ^ (Row * 19349663);
        }

        public override string ToString()
        {
            return "(" + Col + "," + Row + ")";
        }

        public static bool operator ==(Hex a, Hex b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Hex a, Hex b)
        {
            return !a.Equals(b);
        }
    }

    public static class HexMath
    {
        public const float Radius = 1f;
        public static readonly float Sqrt3 = Mathf.Sqrt(3f);
        public static float Width { get { return Sqrt3 * Radius; } }
        public static float Height { get { return 2f * Radius; } }
        public static float RowStep { get { return 1.5f * Radius; } }

        static readonly Hex[] EvenRowDirs =
        {
            new Hex(1, 0), new Hex(-1, 0), new Hex(0, -1),
            new Hex(-1, -1), new Hex(0, 1), new Hex(-1, 1)
        };

        static readonly Hex[] OddRowDirs =
        {
            new Hex(1, 0), new Hex(-1, 0), new Hex(1, -1),
            new Hex(0, -1), new Hex(1, 1), new Hex(0, 1)
        };

        public static IEnumerable<Hex> Neighbors(Hex h)
        {
            var dirs = (h.Row & 1) == 0 ? EvenRowDirs : OddRowDirs;
            for (int i = 0; i < dirs.Length; i++)
            {
                yield return new Hex(h.Col + dirs[i].Col, h.Row + dirs[i].Row);
            }
        }

        public static Vector2Int ToCube(Hex h)
        {
            int q = h.Col - ((h.Row - (h.Row & 1)) / 2);
            int r = h.Row;
            return new Vector2Int(q, r);
        }

        public static int Distance(Hex a, Hex b)
        {
            var ca = ToCube(a);
            var cb = ToCube(b);
            int dq = Mathf.Abs(ca.x - cb.x);
            int dr = Mathf.Abs(ca.y - cb.y);
            int ds = Mathf.Abs((ca.x + ca.y) - (cb.x + cb.y));
            return Mathf.Max(dq, Mathf.Max(dr, ds));
        }

        public static Vector3 ToWorld(Hex h)
        {
            float x = Width * (h.Col + 0.5f * (h.Row & 1));
            float y = -h.Row * RowStep;
            return new Vector3(x, y, 0f);
        }

        /// <summary>Ближайший гекс к точке мира; (Col = -1) если точка далеко от поля.</summary>
        public static Hex FromWorld(Vector3 world, int width, int height)
        {
            Hex best = new Hex(-1, -1);
            float bestDist = float.MaxValue;
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    var h = new Hex(col, row);
                    float d = (ToWorld(h) - world).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = h;
                    }
                }
            }

            if (bestDist > (Width * 0.55f) * (Width * 0.55f))
            {
                return new Hex(-1, -1);
            }

            return best;
        }
    }
}
