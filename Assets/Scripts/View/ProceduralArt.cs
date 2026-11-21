using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Все спрайты игры генерируются в коде — внешние ассеты не нужны.</summary>
    public static class ProceduralArt
    {
        static bool ready;

        static Sprite whitePixel;
        static Sprite hexTile;
        static Sprite circle;
        static Sprite ring;
        static Sprite roundedRect;
        static Sprite rock;
        static Sprite background;
        static Sprite swordIcon;
        static Sprite bowIcon;
        static Sprite wingIcon;
        static Sprite arrow;
        static Sprite triangle;

        public static Sprite WhitePixel { get { Ensure(); return whitePixel; } }
        public static Sprite HexTile { get { Ensure(); return hexTile; } }
        public static Sprite Circle { get { Ensure(); return circle; } }
        public static Sprite Ring { get { Ensure(); return ring; } }
        public static Sprite RoundedRect { get { Ensure(); return roundedRect; } }
        public static Sprite Rock { get { Ensure(); return rock; } }
        public static Sprite Background { get { Ensure(); return background; } }
        public static Sprite SwordIcon { get { Ensure(); return swordIcon; } }
        public static Sprite BowIcon { get { Ensure(); return bowIcon; } }
        public static Sprite WingIcon { get { Ensure(); return wingIcon; } }
        public static Sprite Arrow { get { Ensure(); return arrow; } }
        public static Sprite Triangle { get { Ensure(); return triangle; } }

        public static Sprite IconFor(UnitDef def)
        {
            if (def == null) return Circle;
            switch (def.Class)
            {
                case UnitClass.Ranged: return BowIcon;
                case UnitClass.Flyer: return WingIcon;
                default: return SwordIcon;
            }
        }

        static void Ensure()
        {
            if (ready) return;
            ready = true;

            whitePixel = MakeSprite(MakeSolidTexture(4, 4, Color.white, "Pixel"), 4f, Vector4.zero);
            hexTile = BuildHex();
            circle = BuildCircle(256, 120f, "Circle");
            ring = BuildRing(256, 108f, 7f);
            roundedRect = BuildRounded(64, 15f, 16);
            rock = BuildRock(220);
            background = BuildBackground(256);
            swordIcon = MakeSdfIcon(96, "IconSword", SwordSdf);
            bowIcon = MakeSdfIcon(96, "IconBow", BowSdf);
            wingIcon = MakeSdfIcon(96, "IconWing", WingSdf);
            arrow = BuildArrow(64, 20);
            triangle = MakeSdfIcon(64, "Marker", TriangleSdf);
        }

        // ---------------------------------------------------------------- texture utils

        static Texture2D MakeSolidTexture(int w, int h, Color color, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeTexture(int w, int h, string name, System.Func<float, float, Color> sample)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color acc = Color.clear;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            acc += sample(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f);
                        }
                    }

                    acc *= 0.25f;
                    pixels[y * w + x] = acc;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static Sprite MakeSprite(Texture2D tex, float ppu, Vector4 border)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
            sprite.name = tex.name;
            return sprite;
        }

        // ---------------------------------------------------------------- shapes

        static Vector2[] HexPolygon(float cx, float cy, float radius)
        {
            var pts = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 90f);
                pts[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }

            return pts;
        }

        static bool PointInPolygon(float px, float py, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((poly[i].y > py) != (poly[j].y > py) &&
                    px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        static Sprite BuildHex()
        {
            const int r = 116;
            int w = Mathf.RoundToInt(Mathf.Sqrt(3f) * r) + 18;
            int h = 2 * r + 18;
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            var outer = HexPolygon(cx, cy, r);
            var inner = HexPolygon(cx, cy, r - 6f);

            var tex = MakeTexture(w, h, "HexTile", (x, y) =>
            {
                if (!PointInPolygon(x, y, outer)) return Color.clear;
                bool border = !PointInPolygon(x, y, inner);
                float shade = border ? 0.72f : 1f;
                float v = Mathf.InverseLerp(cy - r, cy + r, y);
                shade *= Mathf.Lerp(0.90f, 1.04f, v);
                return new Color(shade, shade, shade, 1f);
            });

            float ppu = w / (Mathf.Sqrt(3f) * HexMath.Radius);
            return MakeSprite(tex, ppu, Vector4.zero);
        }

        static Sprite BuildCircle(int size, float radius, string name)
        {
            float c = size * 0.5f;
            var tex = MakeTexture(size, size, name, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float alpha = Mathf.Clamp01(radius + 0.5f - d);
                float shade = d > radius - 8f ? 0.80f : 1f;
                return new Color(shade, shade, shade, alpha);
            });

            return MakeSprite(tex, size, Vector4.zero);
        }

        static Sprite BuildRing(int size, float radius, float thickness)
        {
            float c = size * 0.5f;
            var tex = MakeTexture(size, size, "Ring", (x, y) =>
            {
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) - radius);
                float alpha = Mathf.Clamp01(thickness * 0.5f + 0.5f - d);
                return new Color(1f, 1f, 1f, alpha);
            });

            return MakeSprite(tex, size, Vector4.zero);
        }

        static Sprite BuildRounded(int size, float radius, int border)
        {
            float half = size * 0.5f;
            var tex = MakeTexture(size, size, "RoundedRect", (x, y) =>
            {
                float dx = Mathf.Abs(x - half) - (half - radius);
                float dy = Mathf.Abs(y - half) - (half - radius);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                float d = Mathf.Min(Mathf.Max(dx, dy), 0f) + outside - radius;
                float alpha = Mathf.Clamp01(0.5f - d);
                return new Color(1f, 1f, 1f, alpha);
            });

            return MakeSprite(tex, size, new Vector4(border, border, border, border));
        }

        static Sprite BuildRock(int size)
        {
            float c = size * 0.5f;
            var rng = new System.Random(4242);
            var pts = new Vector2[9];
            for (int i = 0; i < pts.Length; i++)
            {
                float angle = Mathf.PI * 2f * i / pts.Length;
                float radius = size * 0.34f * (0.78f + (float)rng.NextDouble() * 0.28f);
                pts[i] = new Vector2(c + Mathf.Cos(angle) * radius, c + Mathf.Sin(angle) * radius * 0.92f);
            }

            var tex = MakeTexture(size, size, "Rock", (x, y) =>
            {
                if (!PointInPolygon(x, y, pts)) return Color.clear;
                float v = Mathf.InverseLerp(0f, size, y);
                float shade = Mathf.Lerp(0.42f, 0.72f, v);
                float crack = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                shade *= Mathf.Lerp(0.88f, 1.06f, crack);
                return new Color(shade, shade * 0.97f, shade * 0.92f, 1f);
            });

            return MakeSprite(tex, 100f, Vector4.zero);
        }

        static Sprite BuildBackground(int size)
        {
            var tex = MakeTexture(size, size, "Background", (x, y) =>
            {
                float u = x / size;
                float v = y / size;
                Color bottom = new Color(0.055f, 0.052f, 0.045f);
                Color top = new Color(0.115f, 0.100f, 0.078f);
                Color color = Color.Lerp(bottom, top, v);

                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.56f));
                color += new Color(0.10f, 0.075f, 0.04f) * Mathf.Clamp01(0.55f - d);

                float vignette = 0.45f + 0.55f * Mathf.Clamp01(1f - d * 1.5f);
                color *= vignette;
                color.a = 1f;
                return color;
            });

            return MakeSprite(tex, 100f, Vector4.zero);
        }

        // ---------------------------------------------------------------- icons

        static float SdCircle(float px, float py, float cx, float cy, float r)
        {
            float dx = px - cx;
            float dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        static float SdBox(float px, float py, float cx, float cy, float hw, float hh)
        {
            float dx = Mathf.Abs(px - cx) - hw;
            float dy = Mathf.Abs(py - cy) - hh;
            return Mathf.Min(Mathf.Max(dx, dy), 0f) +
                   Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
        }

        static float SdSegment(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float vx = bx - ax;
            float vy = by - ay;
            float wx = px - ax;
            float wy = py - ay;
            float len2 = vx * vx + vy * vy;
            float t = len2 > 0f ? Mathf.Clamp01((wx * vx + wy * vy) / len2) : 0f;
            float dx = wx - vx * t;
            float dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        static float SdPolygon(float px, float py, Vector2[] poly)
        {
            float dist = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                dist = Mathf.Min(dist, SdSegment(px, py, poly[j].x, poly[j].y, poly[i].x, poly[i].y, 0f));
                if ((poly[i].y > py) != (poly[j].y > py) &&
                    px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside ? -dist : dist;
        }

        static Sprite MakeSdfIcon(int size, string name, System.Func<float, float, float> sdf)
        {
            var tex = MakeTexture(size, size, name, (x, y) =>
            {
                float d = sdf(x, y);
                float alpha = Mathf.Clamp01(0.5f - d);
                return new Color(1f, 1f, 1f, alpha);
            });

            return MakeSprite(tex, size, Vector4.zero);
        }

        static float SwordSdf(float x, float y)
        {
            var blade = new[]
            {
                new Vector2(48f, 92f), new Vector2(42.5f, 82f), new Vector2(42.5f, 30f),
                new Vector2(53.5f, 30f), new Vector2(53.5f, 82f)
            };
            float d = SdPolygon(x, y, blade);
            d = Mathf.Min(d, SdBox(x, y, 48f, 27f, 17f, 3.2f));
            d = Mathf.Min(d, SdBox(x, y, 48f, 16f, 3.6f, 9f));
            d = Mathf.Min(d, SdCircle(x, y, 48f, 5.5f, 5f));
            return d;
        }

        static float BowSdf(float x, float y)
        {
            float cx = 44f;
            float cy = 48f;
            float dx = x - cx;
            float dy = y - cy;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            float arc = float.MaxValue;
            if (angle > -72f && angle < 72f)
            {
                arc = Mathf.Abs(radius - 30f) - 3.2f;
            }

            float a = Mathf.Deg2Rad * 72f;
            float tipX = cx + Mathf.Cos(a) * 30f;
            float tipY = cy + Mathf.Sin(a) * 30f;
            float botX = cx + Mathf.Cos(-a) * 30f;
            float botY = cy + Mathf.Sin(-a) * 30f;

            float d = Mathf.Min(arc, SdSegment(x, y, botX, botY, tipX, tipY, 1.1f));
            // стрела
            d = Mathf.Min(d, SdSegment(x, y, 20f, 48f, 70f, 48f, 1.6f));
            var head = new[]
            {
                new Vector2(70f, 54f), new Vector2(86f, 48f), new Vector2(70f, 42f)
            };
            d = Mathf.Min(d, SdPolygon(x, y, head));
            return d;
        }

        static float WingSdf(float x, float y)
        {
            float d = SdCircle(x, y, 24f, 48f, 6.5f);
            d = Mathf.Min(d, SdSegment(x, y, 26f, 56f, 72f, 76f, 5f));
            d = Mathf.Min(d, SdSegment(x, y, 24f, 48f, 66f, 56f, 4.4f));
            d = Mathf.Min(d, SdSegment(x, y, 23f, 40f, 56f, 38f, 3.8f));
            d = Mathf.Min(d, SdSegment(x, y, 22f, 33f, 44f, 26f, 3.2f));
            return d;
        }

        static float TriangleSdf(float x, float y)
        {
            var poly = new[]
            {
                new Vector2(4f, 54f), new Vector2(60f, 54f), new Vector2(32f, 8f)
            };
            return SdPolygon(x, y, poly);
        }

        static Sprite BuildArrow(int w, int h)
        {
            var tex = MakeTexture(w, h, "Arrow", (x, y) =>
            {
                float d = SdBox(x, y, 24f, h * 0.5f, 16f, 2f);
                var head = new[]
                {
                    new Vector2(37f, h * 0.5f + 8f), new Vector2(37f, h * 0.5f - 8f), new Vector2(62f, h * 0.5f)
                };
                d = Mathf.Min(d, SdPolygon(x, y, head));
                d = Mathf.Min(d, SdSegment(x, y, 8f, h * 0.5f + 7f, 20f, h * 0.5f, 1.8f));
                d = Mathf.Min(d, SdSegment(x, y, 8f, h * 0.5f - 7f, 20f, h * 0.5f, 1.8f));
                float alpha = Mathf.Clamp01(0.5f - d);
                return new Color(1f, 1f, 1f, alpha);
            });

            return MakeSprite(tex, 100f, Vector4.zero);
        }
    }
}
