using UnityEngine;

namespace DualityWalker.World
{
    public static class WorldVisualFactory
    {
        private static Sprite pixel;

        public static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
                return pixel;
            }
        }
    }
}
