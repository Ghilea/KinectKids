"""Isolate the supplied sprites and pack padded atlases (Pillow, NumPy, SciPy)."""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/greveGast"
DEST = ROOT / "unity/KinectKids3D/Assets/KinectKids/Games/GreveGast/Resources/GreveChase/GreveAnimations"
PREVIEW = ROOT / "artifacts/greve-animation-crops"
FOUR_COLUMNS = {"intro_taunt", "intro_to_idle", "idle_loop", "chase_fast_loop", "surge_forward"}
PADDING = 16


def isolate(source, columns):
    rgba = np.array(Image.open(source).convert("RGBA"))
    alpha = rgba[:, :, 3]
    foreground, _ = ndimage.label(alpha > 20)
    sizes = np.bincount(foreground.ravel())
    sizes[0] = 0
    main = sizes[foreground] > 2000
    # Two erosion steps separate the thin fog bridge between surge frames 7 and 8.
    cores, _ = ndimage.label(ndimage.binary_erosion(main, iterations=2))
    core_sizes = np.bincount(cores.ravel())
    core_sizes[0] = 0
    cores[core_sizes[cores] <= 2000] = 0
    ids = np.unique(cores)
    ids = ids[ids != 0]
    if len(ids) != columns * 2:
        raise ValueError(f"{source.name}: expected {columns * 2} figures, found {len(ids)}")
    # Restore original antialiased edges, assigning the shared fog to its nearest core.
    indices = ndimage.distance_transform_edt(cores == 0, return_distances=False, return_indices=True)
    owners = cores[tuple(indices)]
    keep = ndimage.binary_dilation(main, iterations=2) & (alpha != 0)
    frames = [None] * (columns * 2)
    for identity in ids:
        cy, cx = ndimage.center_of_mass(cores == identity)
        row = int(cy >= rgba.shape[0] / 2)
        column = min(columns - 1, int(cx * columns / rgba.shape[1]))
        index = row * columns + column
        if frames[index] is not None:
            raise ValueError(f"{source.name}: multiple figures assigned to frame {index}")
        mask = keep & (owners == identity)
        ys, xs = np.nonzero(mask)
        top, bottom, left, right = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        pixels = rgba[top:bottom, left:right].copy()
        pixels[~mask[top:bottom, left:right]] = 0
        frames[index] = Image.fromarray(pixels)
    # This source mirrors the whole left sheet, including the order of its columns.
    if source.stem == "greve_shout_right":
        frames = [frames[i] for i in (2, 1, 0, 5, 4, 3)]
    return frames


def prepare():
    DEST.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    total = 0
    for source in sorted(SOURCE.glob("*.png")):
        clip = source.stem.removeprefix("greve_")
        if clip == "reach_center":
            continue  # Opaque numbered reference; runtime uses transparent surge_forward.
        columns = 4 if clip in FOUR_COLUMNS else 3
        frames = isolate(source, columns)
        width = (max(frame.width for frame in frames) + PADDING * 2 + 3) // 4 * 4
        height = (max(frame.height for frame in frames) + PADDING * 2 + 3) // 4 * 4
        atlas = Image.new("RGBA", (width * columns, height * 2))
        preview = Image.new("RGBA", (width * columns, (height + 30) * 2), (28, 31, 43, 255))
        draw = ImageDraw.Draw(preview)
        for i, frame in enumerate(frames):
            canvas = Image.new("RGBA", (width, height))
            canvas.paste(frame, ((width - frame.width) // 2, height - PADDING - frame.height))
            bbox = canvas.getbbox()
            if min(bbox[0], bbox[1], width - bbox[2], height - bbox[3]) < PADDING:
                raise ValueError(f"{clip} {i}: missing edge clearance")
            atlas.paste(canvas, (i % columns * width, i // columns * height))
            preview.alpha_composite(canvas, (i % columns * width, i // columns * (height + 30)))
            draw.text((i % columns * width + 8, i // columns * (height + 30) + height + 6),
                      f"{clip} {i + 1}", fill="white")
        atlas.save(DEST / source.name)
        preview.convert("RGB").save(PREVIEW / source.name)
        total += len(frames)
        print(f"{clip}: {len(frames)} isolated frames, {width}x{height} canvas")
    print(f"Prepared {total} frames; original sheets preserved in {SOURCE}")


if __name__ == "__main__":
    prepare()
