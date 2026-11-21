using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Отображение поля боя: гексы, жетоны отрядов, анимации.</summary>
    public class BattleView : MonoBehaviour
    {
        class Token
        {
            public UnitStack Stack;
            public GameObject Root;
            public SpriteRenderer Shadow;
            public SpriteRenderer FactionRing;
            public SpriteRenderer Body;
            public SpriteRenderer Icon;
            public TextMesh CountText;
            public SpriteRenderer HpBg;
            public SpriteRenderer HpFill;
            public SpriteRenderer Marker;
            public Color BaseBodyColor;
            public Color BaseRingColor;
        }

        BattleState state;
        Camera cam;
        Transform root;

        readonly Dictionary<Hex, SpriteRenderer> tiles = new Dictionary<Hex, SpriteRenderer>();
        readonly Dictionary<Hex, Color> tileBaseColors = new Dictionary<Hex, Color>();
        readonly Dictionary<UnitStack, Token> tokens = new Dictionary<UnitStack, Token>();
        readonly HashSet<UnitStack> deadAnimated = new HashSet<UnitStack>();

        SpriteRenderer selectionRing;
        Token currentToken;

        public float OrthoSize { get; private set; }

        public void Build(BattleState battleState)
        {
            state = battleState;
            var rootGo = new GameObject("BattleField");
            rootGo.transform.SetParent(transform, false);
            root = rootGo.transform;

            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
            }

            FitCamera();
            BuildBackground();
            BuildTiles();
            BuildObstacles();
            BuildTokens();
            BuildSelectionRing();
            SyncTokens();
        }

        void FitCamera()
        {
            float halfW = HexMath.Width * 0.5f;
            float halfH = HexMath.Height * 0.5f;
            float minX = -halfW;
            float maxX = HexMath.Width * (BattleState.Width - 1 + 0.5f) + halfW;
            float maxY = halfH;
            float minY = -HexMath.RowStep * (BattleState.Height - 1) - halfH;

            float fieldW = maxX - minX;
            float fieldH = maxY - minY;
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            float sizeByHeight = fieldH * 1.06f * 0.5f / 0.76f;
            float sizeByWidth = fieldW * 1.04f * 0.5f / aspect;
            OrthoSize = Mathf.Max(sizeByHeight, sizeByWidth, 5f);

            cam.orthographic = true;
            cam.orthographicSize = OrthoSize;
            cam.transform.position = new Vector3(centerX, centerY - OrthoSize * 0.045f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.035f, 0.03f);
        }

        void BuildBackground()
        {
            var go = new GameObject("Background");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 6f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Background;
            sr.sortingOrder = -100;

            float viewHeight = OrthoSize * 2f;
            float viewWidth = viewHeight * cam.aspect;
            float spriteSize = 512f / 100f;
            float scale = Mathf.Max(viewWidth, viewHeight) * 1.25f / spriteSize;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void BuildTiles()
        {
            for (int row = 0; row < BattleState.Height; row++)
            {
                for (int col = 0; col < BattleState.Width; col++)
                {
                    var hex = new Hex(col, row);
                    var go = new GameObject("Tile_" + col + "_" + row);
                    go.transform.SetParent(root, false);
                    go.transform.position = HexMath.ToWorld(hex);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = ProceduralArt.HexTile;
                    sr.sortingOrder = -10;

                    Color color = ((col + row) & 1) == 0
                        ? new Color(0.30f, 0.35f, 0.22f)
                        : new Color(0.33f, 0.38f, 0.24f);

                    if (col <= 2) color = Color.Lerp(color, new Color(0.20f, 0.28f, 0.40f), 0.45f);
                    if (col >= BattleState.Width - 3) color = Color.Lerp(color, new Color(0.40f, 0.21f, 0.19f), 0.45f);

                    sr.color = color;
                    tiles[hex] = sr;
                    tileBaseColors[hex] = color;
                }
            }
        }

        void BuildObstacles()
        {
            var rng = new System.Random(state.Seed + 991);
            int index = 0;
            foreach (var hex in state.Obstacles)
            {
                var go = new GameObject("Rock_" + index++);
                go.transform.SetParent(root, false);
                go.transform.position = HexMath.ToWorld(hex) + new Vector3(0f, 0.18f, 0f);
                go.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 360f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralArt.Rock;
                sr.color = new Color(0.62f, 0.58f, 0.52f);
                sr.sortingOrder = 6;
            }
        }

        void BuildSelectionRing()
        {
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(root, false);
            selectionRing = go.AddComponent<SpriteRenderer>();
            selectionRing.sprite = ProceduralArt.Ring;
            selectionRing.color = new Color(1f, 0.86f, 0.35f, 0.95f);
            selectionRing.sortingOrder = 8;
            go.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
            go.SetActive(false);
        }

        void BuildTokens()
        {
            for (int i = 0; i < state.Stacks.Count; i++)
            {
                var stack = state.Stacks[i];
                tokens[stack] = CreateToken(stack);
            }
        }

        Token CreateToken(UnitStack stack)
        {
            var go = new GameObject("Unit_" + stack.Id);
            go.transform.SetParent(root, false);
            go.transform.position = HexMath.ToWorld(stack.Pos);

            var token = new Token
            {
                Stack = stack,
                Root = go,
                BaseRingColor = UiKit.FactionColor(stack.Side),
                BaseBodyColor = UiKit.Darken(stack.Def.Color, 0.92f)
            };

            token.Shadow = AddSprite(go.transform, "Shadow", ProceduralArt.Circle, new Color(0f, 0f, 0f, 0.32f), new Vector3(0f, -0.13f, 0f), 1.0f, 0);
            token.FactionRing = AddSprite(go.transform, "Faction", ProceduralArt.Circle, token.BaseRingColor, Vector3.zero, 1.03f, 1);
            token.Body = AddSprite(go.transform, "Body", ProceduralArt.Circle, token.BaseBodyColor, Vector3.zero, 0.94f, 2);
            token.Icon = AddSprite(go.transform, "Icon", ProceduralArt.IconFor(stack.Def), new Color(1f, 1f, 1f, 0.92f), new Vector3(0f, 0.15f, 0f), 0.42f, 3);
            token.CountText = AddText(go.transform, "Count", stack.Count.ToString(), 64, 0.085f, Color.white, new Vector3(0f, -0.2f, 0f), 4);

            token.HpBg = AddSprite(go.transform, "HpBg", ProceduralArt.WhitePixel, new Color(0.06f, 0.05f, 0.04f, 0.9f), new Vector3(0f, 0.63f, 0f), 1f, 5);
            token.HpBg.transform.localScale = new Vector3(1.0f, 0.13f, 1f);

            token.HpFill = AddSprite(go.transform, "HpFill", ProceduralArt.WhitePixel, new Color(0.35f, 0.78f, 0.35f), new Vector3(0f, 0.63f, 0f), 1f, 6);
            token.HpFill.transform.localScale = new Vector3(0.96f, 0.09f, 1f);

            token.Marker = AddSprite(go.transform, "Marker", ProceduralArt.Triangle, new Color(1f, 0.85f, 0.30f), new Vector3(0f, 1.02f, 0f), 0.34f, 7);
            token.Marker.gameObject.SetActive(false);

            ApplySorting(token, stack.Pos.Row);
            return token;
        }

        SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, Vector3 localPos, float scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        TextMesh AddText(Transform parent, string name, string value, int fontSize, float characterSize, Color color, Vector3 localPos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.font = UiKit.Font;
            text.fontSize = fontSize;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            text.richText = false;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = UiKit.Font.material;
                renderer.sortingOrder = order;
            }

            return text;
        }

        void ApplySorting(Token token, int row)
        {
            int order = 10 + Mathf.Clamp(row, 0, BattleState.Height) * 6;
            token.Shadow.sortingOrder = order - 2;
            token.FactionRing.sortingOrder = order - 1;
            token.Body.sortingOrder = order;
            token.Icon.sortingOrder = order + 1;
            var countRenderer = token.CountText.GetComponent<MeshRenderer>();
            if (countRenderer != null) countRenderer.sortingOrder = order + 2;
            token.HpBg.sortingOrder = order + 3;
            token.HpFill.sortingOrder = order + 4;
            token.Marker.sortingOrder = order + 5;
        }

        public UnitStack StackAt(Hex hex)
        {
            return state != null ? state.StackAt(hex) : null;
        }

        public Hex HexAt(Vector3 world)
        {
            return HexMath.FromWorld(world, BattleState.Width, BattleState.Height);
        }

        public void SyncTokens()
        {
            foreach (var pair in tokens)
            {
                var stack = pair.Key;
                var token = pair.Value;
                if (!stack.Alive) continue;

                token.Root.SetActive(true);
                token.Root.transform.position = HexMath.ToWorld(stack.Pos);
                token.Root.transform.localScale = Vector3.one;
                ApplySorting(token, stack.Pos.Row);

                token.CountText.text = stack.Count.ToString();
                token.Body.color = token.BaseBodyColor;
                token.FactionRing.color = token.BaseRingColor;

                float ratio = stack.HpRatio;
                token.HpFill.transform.localScale = new Vector3(Mathf.Max(0.02f, 0.96f * ratio), 0.09f, 1f);
                token.HpFill.transform.localPosition = new Vector3(-0.48f + 0.48f * ratio, 0.63f, 0f);
                token.HpFill.color = ratio > 0.6f
                    ? new Color(0.35f, 0.78f, 0.35f)
                    : ratio > 0.3f
                        ? new Color(0.90f, 0.72f, 0.25f)
                        : new Color(0.85f, 0.30f, 0.25f);
            }

            if (selectionRing != null && selectionRing.gameObject.activeSelf && currentToken == null)
            {
                selectionRing.gameObject.SetActive(false);
            }
        }

        public void SetCurrent(UnitStack stack)
        {
            if (currentToken != null)
            {
                currentToken.Marker.gameObject.SetActive(false);
                currentToken = null;
            }

            Token token;
            if (stack != null && tokens.TryGetValue(stack, out token))
            {
                currentToken = token;
                token.Marker.gameObject.SetActive(true);
            }
        }

        public void ShowSelectionRing(UnitStack stack)
        {
            Token token;
            if (stack != null && tokens.TryGetValue(stack, out token) && stack.Alive)
            {
                selectionRing.transform.position = token.Root.transform.position + new Vector3(0f, 0.02f, 0f);
                selectionRing.gameObject.SetActive(true);
            }
            else
            {
                selectionRing.gameObject.SetActive(false);
            }
        }

        public void HighlightMove(IEnumerable<Hex> hexes)
        {
            if (hexes == null) return;
            foreach (var hex in hexes)
            {
                SpriteRenderer tile;
                if (tiles.TryGetValue(hex, out tile))
                {
                    Color baseColor = tileBaseColors[hex];
                    tile.color = Color.Lerp(baseColor, new Color(0.42f, 0.66f, 0.98f), 0.38f);
                }
            }
        }

        public void SetAttackTargets(IEnumerable<UnitStack> stacks)
        {
            if (stacks == null) return;
            foreach (var stack in stacks)
            {
                Token token;
                if (tokens.TryGetValue(stack, out token))
                {
                    token.FactionRing.color = new Color(1f, 0.32f, 0.25f);
                    token.Body.color = UiKit.Darken(stack.Def.Color, 1.05f);
                }
            }
        }

        public void SetSpellTargets(IEnumerable<UnitStack> stacks, Color color)
        {
            if (stacks == null) return;
            foreach (var stack in stacks)
            {
                Token token;
                if (tokens.TryGetValue(stack, out token))
                {
                    token.FactionRing.color = color;
                }
            }
        }

        public void ClearHighlights()
        {
            foreach (var pair in tiles)
            {
                pair.Value.color = tileBaseColors[pair.Key];
            }

            foreach (var pair in tokens)
            {
                if (!pair.Key.Alive) continue;
                pair.Value.FactionRing.color = pair.Value.BaseRingColor;
                pair.Value.Body.color = pair.Value.BaseBodyColor;
            }
        }

        public IEnumerator MoveUnit(UnitStack stack, List<Hex> path)
        {
            if (path == null || path.Count == 0) yield break;

            Token token;
            if (!tokens.TryGetValue(stack, out token)) yield break;

            var points = new List<Vector3>();
            points.Add(token.Root.transform.position);
            for (int i = 0; i < path.Count; i++) points.Add(HexMath.ToWorld(path[i]));

            float totalLength = 0f;
            for (int i = 1; i < points.Count; i++) totalLength += Vector3.Distance(points[i - 1], points[i]);

            float duration = Mathf.Clamp(totalLength / 9f, 0.18f, 1.4f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                token.Root.transform.position = SamplePath(points, k * totalLength);
                int row = Mathf.Clamp(Mathf.RoundToInt(-token.Root.transform.position.y / HexMath.RowStep), 0, BattleState.Height);
                ApplySorting(token, row);
                yield return null;
            }

            token.Root.transform.position = points[points.Count - 1];
            ApplySorting(token, stack.Pos.Row);
        }

        static Vector3 SamplePath(List<Vector3> points, float distance)
        {
            float walked = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                float segment = Vector3.Distance(points[i - 1], points[i]);
                if (walked + segment >= distance || i == points.Count - 1)
                {
                    float t = segment > 0.0001f ? Mathf.Clamp01((distance - walked) / segment) : 1f;
                    return Vector3.Lerp(points[i - 1], points[i], t);
                }

                walked += segment;
            }

            return points[points.Count - 1];
        }

        public IEnumerator AttackLunge(UnitStack attacker, UnitStack target)
        {
            Token a, b;
            if (!tokens.TryGetValue(attacker, out a)) yield break;
            tokens.TryGetValue(target, out b);

            Sfx.Play(SfxId.Melee, 0.7f);

            Vector3 start = a.Root.transform.position;
            Vector3 dir = b != null ? (b.Root.transform.position - start).normalized : Vector3.up;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.up;

            float duration = 0.22f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                a.Root.transform.position = start + dir * (Mathf.Sin(k * Mathf.PI) * 0.45f);
                yield return null;
            }

            a.Root.transform.position = start;

            if (b != null)
            {
                Vector3 targetStart = b.Root.transform.position;
                float recoil = 0.12f;
                for (float t = 0f; t < recoil; t += Time.deltaTime)
                {
                    float k = Mathf.Clamp01(t / recoil);
                    b.Root.transform.position = targetStart + dir * (Mathf.Sin(k * Mathf.PI) * 0.16f);
                    yield return null;
                }

                b.Root.transform.position = targetStart;
            }
        }

        public IEnumerator ShootArrow(UnitStack attacker, UnitStack target)
        {
            Token a, b;
            if (!tokens.TryGetValue(attacker, out a) || !tokens.TryGetValue(target, out b)) yield break;

            Sfx.Play(SfxId.Shoot, 0.7f);

            var go = new GameObject("Arrow");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Arrow;
            sr.color = new Color(1f, 0.94f, 0.70f);
            sr.sortingOrder = 80;

            Vector3 start = a.Root.transform.position + new Vector3(0f, 0.12f, 0f);
            Vector3 end = b.Root.transform.position + new Vector3(0f, 0.12f, 0f);
            Vector3 delta = end - start;
            go.transform.position = start;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            float duration = 0.22f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                go.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t / duration));
                yield return null;
            }

            go.transform.position = end;
            Destroy(go);
        }

        public IEnumerator CastSpellFx(UnitStack target, SpellId spell)
        {
            Token token;
            if (!tokens.TryGetValue(target, out token)) yield break;

            Sfx.Play(spell == SpellId.MagicArrow ? SfxId.MagicArrow : SfxId.Buff, 0.8f);

            var color = Spellbook.Get(spell).Color;
            var go = new GameObject("SpellFx");
            go.transform.SetParent(root, false);
            go.transform.position = token.Root.transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Circle;
            sr.color = new Color(color.r, color.g, color.b, 0.85f);
            sr.sortingOrder = 90;

            float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                float scale = 0.25f + k * 1.6f;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                sr.color = new Color(color.r, color.g, color.b, 0.85f * (1f - k));
                yield return null;
            }

            Destroy(go);
        }

        public void FloatingDamage(UnitStack target, DamageInfo info)
        {
            if (info == null) return;

            Token token;
            if (!tokens.TryGetValue(target, out token)) return;

            Color color = info.Spell
                ? new Color(0.62f, 0.82f, 1f)
                : info.Retaliation
                    ? new Color(1f, 0.66f, 0.35f)
                    : new Color(1f, 0.38f, 0.30f);

            StartCoroutine(FloatText(token.Root.transform.position + new Vector3(0f, 0.75f, 0f), "-" + info.Damage, color));
            StartCoroutine(FlashTest(token));
        }

        public void FloatingMessage(UnitStack target, string message, Color color)
        {
            Token token;
            if (!tokens.TryGetValue(target, out token)) return;
            StartCoroutine(FloatText(token.Root.transform.position + new Vector3(0f, 0.75f, 0f), message, color));
        }

        IEnumerator FloatText(Vector3 worldPos, string text, Color color)
        {
            var go = new GameObject("FloatText");
            go.transform.SetParent(root, false);
            go.transform.position = worldPos;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UiKit.Font;
            mesh.fontSize = 72;
            mesh.characterSize = 0.075f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.richText = false;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = UiKit.Font.material;
                renderer.sortingOrder = 120;
            }

            float duration = 0.9f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                go.transform.position = worldPos + new Vector3(0f, 1.0f * k, 0f);
                Color c = color;
                c.a = 1f - k;
                mesh.color = c;
                yield return null;
            }

            Destroy(go);
        }

        IEnumerator FlashTest(Token token)
        {
            Color start = token.Body.color;
            token.Body.color = Color.white;
            float duration = 0.16f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                token.Body.color = Color.Lerp(Color.white, start, k);
                yield return null;
            }

            token.Body.color = start;
        }

        public IEnumerator PlayDeaths()
        {
            var fresh = new List<Token>();
            foreach (var pair in tokens)
            {
                if (pair.Key.Alive || deadAnimated.Contains(pair.Key)) continue;
                deadAnimated.Add(pair.Key);
                fresh.Add(pair.Value);
            }

            if (fresh.Count > 0) Sfx.Play(SfxId.Death, 0.6f);

            for (int i = 0; i < fresh.Count; i++)
            {
                StartCoroutine(DeathAnim(fresh[i]));
            }

            if (fresh.Count > 0) yield return new WaitForSeconds(0.45f);
        }

        IEnumerator DeathAnim(Token token)
        {
            var spriteColors = new List<KeyValuePair<SpriteRenderer, Color>>();
            foreach (var sr in token.Root.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteColors.Add(new KeyValuePair<SpriteRenderer, Color>(sr, sr.color));
            }

            var text = token.CountText;
            Color textColor = text.color;
            Vector3 startScale = token.Root.transform.localScale;

            float duration = 0.4f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                token.Root.transform.localScale = startScale * (1f - 0.75f * k);
                for (int i = 0; i < spriteColors.Count; i++)
                {
                    var sr = spriteColors[i].Key;
                    var baseColor = spriteColors[i].Value;
                    sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - k));
                }

                text.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * (1f - k));
                yield return null;
            }

            token.Root.SetActive(false);
        }

        void Update()
        {
            if (currentToken != null && currentToken.Marker != null)
            {
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.07f;
                currentToken.Marker.transform.localPosition = new Vector3(0f, 1.02f + bob, 0f);
            }
        }
    }
}
