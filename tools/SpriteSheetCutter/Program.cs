using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

// SpriteSheetCutter
// -----------------
// Cuts a transparent-background sprite sheet into individual trimmed PNGs using
// connected-component analysis on the alpha channel. Rows/columns are detected
// automatically, so irregular pose layouts still split cleanly.
//
// Usage:
//   SpriteSheetCutter <input.png> <outputDir> <namePrefix> [alphaThreshold] [minArea] [rowTolerance]
//
// Output components are ordered top-to-bottom, then left-to-right, and named
// "<namePrefix>_<index>.png" (index starts at 0). A manifest.txt lists each
// component's index, bounding box and pixel area so poses can be mapped to names.

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: SpriteSheetCutter <input.png> <outputDir> <namePrefix> [alpha=20] [minArea=1200] [rowTol=40]");
            return 1;
        }

        string input = args[0];
        string outDir = args[1];
        string prefix = args[2];
        int alphaThreshold = args.Length > 3 ? int.Parse(args[3]) : 20;
        int minArea = args.Length > 4 ? int.Parse(args[4]) : 1200;
        int rowTolerance = args.Length > 5 ? int.Parse(args[5]) : 40;
        int mergeGap = args.Length > 6 ? int.Parse(args[6]) : 6;
        int minHeight = args.Length > 7 ? int.Parse(args[7]) : 0;

        if (!File.Exists(input)) { Console.Error.WriteLine("Input not found: " + input); return 2; }
        Directory.CreateDirectory(outDir);

        using var bmp = new Bitmap(input);
        int w = bmp.Width, h = bmp.Height;

        // Read alpha into a mask.
        var rect = new Rectangle(0, 0, w, h);
        BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] pixels = new byte[stride * h];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bmp.UnlockBits(data);

        bool Solid(int x, int y) => pixels[y * stride + x * 4 + 3] > alphaThreshold;

        // Flood-fill connected components (4/8-connected via 8 neighbours).
        int[] labels = new int[w * h];
        var components = new List<(int minX, int minY, int maxX, int maxY, int area)>();
        var stack = new Stack<(int, int)>();
        int nextLabel = 0;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!Solid(x, y) || labels[y * w + x] != 0) continue;
            nextLabel++;
            int minX = x, minY = y, maxX = x, maxY = y, area = 0;
            stack.Push((x, y));
            labels[y * w + x] = nextLabel;
            while (stack.Count > 0)
            {
                var (cx, cy) = stack.Pop();
                area++;
                if (cx < minX) minX = cx; if (cx > maxX) maxX = cx;
                if (cy < minY) minY = cy; if (cy > maxY) maxY = cy;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (labels[ny * w + nx] != 0 || !Solid(nx, ny)) continue;
                    labels[ny * w + nx] = nextLabel;
                    stack.Push((nx, ny));
                }
            }
            components.Add((minX, minY, maxX, maxY, area));
        }

        // Merge components whose bounding boxes overlap or nearly touch (re-joins
        // a pose split by thin transparent gaps, e.g. a detached hand or FX wisp
        // close to the body).
        components = mergeGap >= 0 ? MergeNearby(components, gap: mergeGap) : components;

        // Keep only meaningful components.
        var kept = components.Where(c => c.area >= minArea
            && (c.maxY - c.minY + 1) >= minHeight).ToList();

        // Order top-to-bottom in rows, then left-to-right inside a row.
        kept.Sort((a, b) =>
        {
            int ay = (a.minY + a.maxY) / 2, by = (b.minY + b.maxY) / 2;
            if (Math.Abs(ay - by) > rowTolerance) return ay.CompareTo(by);
            return a.minX.CompareTo(b.minX);
        });

        using var manifest = new StreamWriter(Path.Combine(outDir, prefix + "_manifest.txt"));
        manifest.WriteLine($"# {Path.GetFileName(input)}  {w}x{h}  components={kept.Count}");
        manifest.WriteLine("# index\tx\ty\tw\th\tarea");

        int index = 0;
        foreach (var c in kept)
        {
            int pad = 4;
            int rx = Math.Max(0, c.minX - pad);
            int ry = Math.Max(0, c.minY - pad);
            int rw = Math.Min(w - rx, c.maxX - c.minX + 1 + pad * 2);
            int rh = Math.Min(h - ry, c.maxY - c.minY + 1 + pad * 2);

            using var slice = new Bitmap(rw, rh, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(slice))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(bmp, new Rectangle(0, 0, rw, rh), new Rectangle(rx, ry, rw, rh), GraphicsUnit.Pixel);
            }
            string outPath = Path.Combine(outDir, $"{prefix}_{index}.png");
            slice.Save(outPath, ImageFormat.Png);
            manifest.WriteLine($"{index}\t{rx}\t{ry}\t{rw}\t{rh}\t{c.area}");
            Console.WriteLine($"{prefix}_{index}.png  ({rw}x{rh})  area={c.area}");
            index++;
        }

        Console.WriteLine($"Done: {index} sprites -> {outDir}");
        return 0;
    }

    private static List<(int minX, int minY, int maxX, int maxY, int area)> MergeNearby(
        List<(int minX, int minY, int maxX, int maxY, int area)> comps, int gap)
    {
        bool merged = true;
        while (merged)
        {
            merged = false;
            for (int i = 0; i < comps.Count; i++)
            {
                for (int j = i + 1; j < comps.Count; j++)
                {
                    if (!Overlaps(comps[i], comps[j], gap)) continue;
                    var a = comps[i]; var b = comps[j];
                    comps[i] = (Math.Min(a.minX, b.minX), Math.Min(a.minY, b.minY),
                        Math.Max(a.maxX, b.maxX), Math.Max(a.maxY, b.maxY), a.area + b.area);
                    comps.RemoveAt(j);
                    merged = true;
                    break;
                }
                if (merged) break;
            }
        }
        return comps;
    }

    private static bool Overlaps((int minX, int minY, int maxX, int maxY, int area) a,
        (int minX, int minY, int maxX, int maxY, int area) b, int gap)
    {
        return a.minX - gap <= b.maxX && b.minX - gap <= a.maxX &&
               a.minY - gap <= b.maxY && b.minY - gap <= a.maxY;
    }
}
