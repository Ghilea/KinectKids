using UnityEngine;

namespace GreveGast2D
{
    /// <summary>
    /// Song-synchronised miniature intro film. The camera moves from the room
    /// down to a drawing on the floor, Greve Gast circles above it and dives
    /// into the paper. It pulls back before the chase transition at 21.00.
    /// </summary>
    public sealed class GreveGastDrawnIntro : MonoBehaviour
    {
        private GreveGastDrawnController greveGast;
        private KinectKids3D.GreveGastSongController song;
        private Texture2D characterTexture;
        private Texture2D ghostOrbTexture;
        private bool finished;

        public void Configure(GreveGastDrawnController character, KinectKids3D.GreveGastSongController songController)
        {
            greveGast = character;
            song = songController;
            SpriteRenderer renderer = character != null ? character.GetComponentInChildren<SpriteRenderer>(true) : null;
            characterTexture = renderer != null && renderer.sprite != null ? renderer.sprite.texture : null;
            ghostOrbTexture = CreateGhostOrbTexture(128);
            if (greveGast != null) greveGast.SetVisible(false);
        }

        private void Update()
        {
            if (song == null || finished) return;
            if (song.SongTime >= song.GameplayStartTime)
            {
                finished = true;
                if (greveGast != null)
                {
                    greveGast.SetVisible(true);
                    greveGast.SetRunning(true);
                }
                enabled = false;
            }
        }

        private void OnGUI()
        {
            if (song == null || finished) return;
            float time = song.SongTime;
            float fade = 1f - Mathf.InverseLerp(20.35f, 20.86f, time);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);

            // The real room floor remains visible around the paper. Resizing
            // the whole setup reads as a camera dolly without a baked video.
            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.13f, 0.075f, 0.045f));
            DrawFloorBoards();
            float zoomIn = Smooth(Mathf.InverseLerp(0.8f, 5.2f, time));
            float zoomOut = Smooth(Mathf.InverseLerp(16.0f, 20.20f, time));
            float paperFraction = Mathf.Lerp(0.34f, 0.74f, zoomIn);
            paperFraction = Mathf.Lerp(paperFraction, 0.43f, zoomOut);
            float paperWidth = Mathf.Min(Screen.width * paperFraction, Screen.height * paperFraction * 0.82f);
            float paperHeight = paperWidth * 1.18f;
            Rect paper = new Rect(Screen.width * 0.5f - paperWidth * 0.5f,
                Screen.height * 0.52f - paperHeight * 0.5f, paperWidth, paperHeight);

            Fill(new Rect(paper.x + 14f, paper.y + 18f, paper.width, paper.height),
                new Color(0.02f, 0.015f, 0.02f, 0.58f));
            Fill(paper, new Color(0.94f, 0.90f, 0.78f));
            DrawPaperLines(paper);

            // The drawing is always centred and its complete body, including
            // the feet, is constrained inside the paper.
            float characterHeight = paper.height * 0.68f;
            float characterWidth = characterHeight * 0.80f;
            Rect character = CenteredRect(paper.center, characterWidth, characterHeight);

            if (characterTexture != null)
            {
                // The child's original black outline.
                GUI.color = new Color(0.035f, 0.03f, 0.04f, fade * 0.88f);
                GUI.DrawTexture(character, characterTexture, ScaleMode.ScaleToFit, true);

                // Greve Gast circles above the sheet and then flies diagonally
                // into its centre. His coloured energy remains in the drawing.
                if (time >= 3.0f && time < 12.2f)
                {
                    float orbit = Mathf.InverseLerp(3.0f, 9.4f, time);
                    float angle = orbit * Mathf.PI * 6.2f - 0.7f;
                    Vector2 orbitPoint = new Vector2(
                        paper.center.x + Mathf.Cos(angle) * paper.width * 0.47f,
                        paper.y + paper.height * 0.18f + Mathf.Sin(angle) * paper.height * 0.15f);
                    float dive = Smooth(Mathf.InverseLerp(9.4f, 12.2f, time));
                    Vector2 spiritPoint = Vector2.Lerp(orbitPoint, paper.center, dive);
                    float spiritSize = paper.height * Mathf.Lerp(0.21f, 0.09f, dive)
                        * (1f + Mathf.Sin(time * 8f) * 0.05f);
                    Rect spirit = CenteredRect(spiritPoint, spiritSize, spiritSize);
                    DrawGhostOrb(spirit, fade * (1f - dive * 0.82f), time);
                }

                // Colour climbs through the body after Greve Gast enters it.
                float colour = Smooth(Mathf.InverseLerp(11.5f, 15.8f, time));
                if (colour > 0f)
                {
                    float revealedHeight = character.height * colour;
                    Rect clipped = new Rect(character.x, character.yMax - revealedHeight,
                        character.width, revealedHeight);
                    GUI.BeginGroup(clipped);
                    GUI.color = new Color(1f, 1f, 1f, fade);
                    Rect shifted = new Rect(0f, character.y - clipped.y, character.width, character.height);
                    GUI.DrawTexture(shifted, characterTexture, ScaleMode.ScaleToFit, true);
                    GUI.EndGroup();
                }
            }

            if (time >= 13.4f)
                DrawColourPulse(paper, Smooth(Mathf.InverseLerp(13.4f, 16.0f, time)), fade);

            // A crayon beside the sheet establishes the real-world scale.
            Matrix4x4 oldMatrix = GUI.matrix;
            Vector2 crayonPivot = new Vector2(paper.xMax + paper.width * 0.10f, paper.center.y);
            GUIUtility.RotateAroundPivot(-17f, crayonPivot);
            Fill(new Rect(paper.xMax + paper.width * 0.08f, paper.y + paper.height * 0.18f,
                    Mathf.Max(7f, paper.width * 0.025f), paper.height * 0.55f),
                new Color(0.92f, 0.50f, 0.08f));
            GUI.matrix = oldMatrix;
            GUI.color = previous;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height)
        {
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private static float Smooth(float value) => value * value * (3f - 2f * value);

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color *= color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawPaperLines(Rect paper)
        {
            for (int i = 1; i < 14; i++)
                Fill(new Rect(paper.x + 12f, paper.y + i * paper.height / 14f,
                        paper.width - 24f, 1.2f),
                    new Color(0.30f, 0.43f, 0.66f, 0.13f));
        }

        private static void DrawFloorBoards()
        {
            for (int i = 0; i < 11; i++)
                Fill(new Rect(0f, i * Screen.height / 11f, Screen.width, 4f),
                    new Color(0.42f, 0.18f, 0.06f, 0.34f));
            for (int i = 0; i < 7; i++)
                Fill(new Rect(i * Screen.width / 7f, 0f, 3f, Screen.height),
                    new Color(0.05f, 0.025f, 0.015f, 0.35f));
        }

        private void DrawGhostOrb(Rect rect, float alpha, float time)
        {
            if (ghostOrbTexture == null) return;
            Color old = GUI.color;
            Rect glow = new Rect(rect.x - rect.width * 0.18f, rect.y - rect.height * 0.18f,
                rect.width * 1.36f, rect.height * 1.36f);
            GUI.color = new Color(0.72f, 0.34f, 1f, alpha * 0.34f);
            GUI.DrawTexture(glow, ghostOrbTexture, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, ghostOrbTexture, ScaleMode.StretchToFill, true);

            float blink = Mathf.Sin(time * 2.7f) > 0.94f ? 0.025f : 0.08f;
            GUI.color = new Color(1f, 0.94f, 0.72f, alpha * 0.92f);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.27f, rect.y + rect.height * 0.34f,
                rect.width * 0.14f, rect.height * blink), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width * 0.59f, rect.y + rect.height * 0.34f,
                rect.width * 0.14f, rect.height * blink), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static Texture2D CreateGhostOrbTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Greve Gasts spokboll",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), centre) / radius;
                float core = Mathf.Clamp01(1f - distance);
                float glow = Mathf.Clamp01(1.18f - distance);
                float ragged = 0.92f + Mathf.Sin(x * 0.37f + y * 0.21f) * 0.08f;
                pixels[y * size + x] = new Color(
                    0.55f + core * 0.35f,
                    0.16f + core * 0.30f,
                    1f,
                    Mathf.Pow(glow, 1.65f) * ragged);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void DrawColourPulse(Rect paper, float amount, float alpha)
        {
            Color[] colours =
            {
                new Color(1f, 0.18f, 0.12f, 0.65f), new Color(1f, 0.72f, 0.05f, 0.65f),
                new Color(0.04f, 0.65f, 1f, 0.65f), new Color(0.60f, 0.16f, 1f, 0.65f)
            };
            for (int i = 0; i < 8; i++)
            {
                float width = paper.width * amount * (0.05f + i % 3 * 0.018f);
                Color colour = colours[i % colours.Length];
                colour.a *= alpha;
                Fill(new Rect(paper.x + 10f, paper.y + 12f + i * paper.height / 9f,
                    width, 5f + i % 3 * 3f), colour);
                Fill(new Rect(paper.xMax - width - 10f, paper.yMax - 18f - i * paper.height / 9f,
                    width, 5f + i % 3 * 3f), colour);
            }
        }

        private void OnDisable()
        {
            if (finished && greveGast != null) greveGast.SetVisible(true);
        }

        private void OnDestroy()
        {
            if (ghostOrbTexture != null) Destroy(ghostOrbTexture);
        }
    }
}
