using System;

namespace KinectKids.Input
{
    /// <summary>En tillfällig kamerabild som bara används för visning och aldrig sparas.</summary>
    public sealed class ColorFrameEventArgs : EventArgs
    {
        public ColorFrameEventArgs(byte[] pixels, int width, int height)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
        }

        public byte[] Pixels { get; }
        public int Width { get; }
        public int Height { get; }
        public int Stride => Width * 4;
    }
}
