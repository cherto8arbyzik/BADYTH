"""Generate deterministic 4K master and 2K Unity PBR texture sets for Leshy v1."""

from __future__ import annotations

import gc
import json
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUT_4K = ROOT / "textures" / "4k"
OUT_2K = ROOT / "textures" / "2k"
SIZE = 4096
RNG = np.random.default_rng(17419)


def noise(size: int, cells: int, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    small = rng.random((cells, cells), dtype=np.float32)
    return cv2.resize(small, (size, size), interpolation=cv2.INTER_CUBIC)


def vertical_noise(size: int, width_cells: int, height_cells: int, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    small = rng.random((height_cells, width_cells), dtype=np.float32)
    return cv2.resize(small, (size, size), interpolation=cv2.INTER_CUBIC)


def normalized(value: np.ndarray) -> np.ndarray:
    lo, hi = float(value.min()), float(value.max())
    return np.clip((value - lo) / max(hi - lo, 1e-6), 0.0, 1.0)


def normal_from_height(height: np.ndarray, strength: float) -> np.ndarray:
    gx = cv2.Sobel(height, cv2.CV_32F, 1, 0, ksize=3) * strength
    gy = cv2.Sobel(height, cv2.CV_32F, 0, 1, ksize=3) * strength
    nz = np.ones_like(height)
    length = np.sqrt(gx * gx + gy * gy + nz * nz)
    normal = np.dstack((-gx / length, -gy / length, nz / length))
    return np.clip((normal * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)


def rgb_mix(low: tuple[int, int, int], high: tuple[int, int, int], factor: np.ndarray) -> np.ndarray:
    low_arr = np.array(low, np.float32).reshape(1, 1, 3)
    high_arr = np.array(high, np.float32).reshape(1, 1, 3)
    return np.clip(low_arr + (high_arr - low_arr) * factor[..., None], 0, 255).astype(np.uint8)


def line_mask(size: int, count: int, horizontal: bool, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    canvas = np.zeros((size, size), np.uint8)
    for _ in range(count):
        if horizontal:
            y = int(rng.integers(0, size))
            length = int(rng.integers(size // 80, size // 15))
            x = int(rng.integers(0, max(1, size - length)))
            cv2.line(canvas, (x, y), (x + length, y + int(rng.integers(-3, 4))), 255,
                     int(rng.integers(1, 5)), cv2.LINE_AA)
        else:
            x = int(rng.integers(0, size))
            length = int(rng.integers(size // 20, size // 5))
            y = int(rng.integers(0, max(1, size - length)))
            cv2.line(canvas, (x, y), (x + int(rng.integers(-8, 9)), y + length), 255,
                     int(rng.integers(1, 7)), cv2.LINE_AA)
    return cv2.GaussianBlur(canvas, (0, 0), 0.7).astype(np.float32) / 255.0


def save_set(prefix: str, maps: dict[str, np.ndarray], manifest: dict) -> None:
    OUT_4K.mkdir(parents=True, exist_ok=True)
    OUT_2K.mkdir(parents=True, exist_ok=True)
    manifest[prefix] = {}
    for map_name, image in maps.items():
        path_4k = OUT_4K / f"{prefix}_{map_name}.png"
        path_2k = OUT_2K / f"{prefix}_{map_name}.png"
        cv2.imwrite(str(path_4k), image, [cv2.IMWRITE_PNG_COMPRESSION, 4])
        down = cv2.resize(image, (2048, 2048), interpolation=cv2.INTER_AREA)
        cv2.imwrite(str(path_2k), down, [cv2.IMWRITE_PNG_COMPRESSION, 5])
        manifest[prefix][map_name] = {
            "master": str(path_4k.relative_to(ROOT)).replace("\\", "/"),
            "unity": str(path_2k.relative_to(ROOT)).replace("\\", "/"),
            "master_resolution": [4096, 4096],
            "unity_resolution": [2048, 2048],
        }
        del down


def birch() -> dict[str, np.ndarray]:
    broad = vertical_noise(SIZE, 18, 64, 10)
    detail = vertical_noise(SIZE, 90, 180, 11)
    lenticels = line_mask(SIZE, 1650, True, 12)
    cracks = line_mask(SIZE, 230, False, 13)
    height = normalized(0.56 * broad + 0.25 * detail - 0.65 * lenticels - 0.48 * cracks)
    base = rgb_mix((54, 52, 46), (184, 179, 163), normalized(0.72 * broad + 0.28 * detail))
    dark = np.clip(lenticels * 0.92 + cracks * 0.80, 0, 1)[..., None]
    base = np.clip(base.astype(np.float32) * (1.0 - 0.76 * dark), 0, 255).astype(np.uint8)
    rough = np.clip((0.72 + 0.20 * detail + 0.07 * cracks) * 255, 0, 255).astype(np.uint8)
    ao = np.clip((1.0 - 0.42 * lenticels - 0.30 * cracks) * 255, 0, 255).astype(np.uint8)
    return {"BaseColor": base, "Normal": normal_from_height(height, 4.2), "Roughness": rough,
            "Metallic": np.zeros((SIZE, SIZE), np.uint8), "AO": ao}


def dark_wood() -> dict[str, np.ndarray]:
    grain = vertical_noise(SIZE, 24, 220, 20)
    fine = vertical_noise(SIZE, 120, 520, 21)
    cracks = line_mask(SIZE, 520, False, 22)
    height = normalized(0.62 * grain + 0.30 * fine - 0.78 * cracks)
    base = rgb_mix((9, 8, 7), (64, 53, 40), normalized(0.75 * grain + 0.25 * fine))
    base = np.clip(base.astype(np.float32) * (1.0 - cracks[..., None] * 0.78), 0, 255).astype(np.uint8)
    rough = np.clip((0.76 + 0.20 * fine + 0.04 * cracks) * 255, 0, 255).astype(np.uint8)
    ao = np.clip((1.0 - 0.58 * cracks) * 255, 0, 255).astype(np.uint8)
    return {"BaseColor": base, "Normal": normal_from_height(height, 5.2), "Roughness": rough,
            "Metallic": np.zeros((SIZE, SIZE), np.uint8), "AO": ao}


def moss() -> dict[str, np.ndarray]:
    macro = noise(SIZE, 34, 30)
    medium = noise(SIZE, 120, 31)
    micro = noise(SIZE, 420, 32)
    height = normalized(0.56 * macro + 0.31 * medium + 0.13 * micro)
    base = rgb_mix((7, 13, 3), (75, 87, 25), normalized(0.70 * macro + 0.30 * medium))
    brown = np.clip((0.42 - macro) * 2.5, 0, 1)[..., None]
    base = np.clip(base.astype(np.float32) * (1.0 - 0.45 * brown) + brown * np.array((22, 17, 8)), 0, 255).astype(np.uint8)
    rough = np.clip((0.84 + 0.14 * micro) * 255, 0, 255).astype(np.uint8)
    ao = np.clip((0.78 + 0.22 * height) * 255, 0, 255).astype(np.uint8)
    return {"BaseColor": base, "Normal": normal_from_height(height, 6.4), "Roughness": rough,
            "Metallic": np.zeros((SIZE, SIZE), np.uint8), "AO": ao}


def copper() -> dict[str, np.ndarray]:
    broad = noise(SIZE, 42, 40)
    fine = noise(SIZE, 260, 41)
    patina = np.clip((broad - 0.52) * 4.0 + (fine - 0.55) * 1.3, 0, 1)
    metal = rgb_mix((63, 26, 13), (156, 75, 35), normalized(0.65 * broad + 0.35 * fine))
    green = rgb_mix((20, 53, 42), (55, 111, 91), normalized(fine))
    base = np.clip(metal.astype(np.float32) * (1.0 - patina[..., None]) + green * patina[..., None], 0, 255).astype(np.uint8)
    height = normalized(0.62 * broad + 0.18 * fine - 0.28 * patina)
    rough = np.clip((0.36 + 0.34 * patina + 0.16 * fine) * 255, 0, 255).astype(np.uint8)
    metallic = np.clip((0.88 - 0.55 * patina) * 255, 0, 255).astype(np.uint8)
    ao = np.clip((0.84 + 0.16 * broad) * 255, 0, 255).astype(np.uint8)
    return {"BaseColor": base, "Normal": normal_from_height(height, 3.2), "Roughness": rough,
            "Metallic": metallic, "AO": ao}


def cloth(prefix: str, low: tuple[int, int, int], high: tuple[int, int, int], seed: int) -> dict[str, np.ndarray]:
    y, x = np.indices((SIZE, SIZE), dtype=np.float32)
    weave = (np.sin(x * np.pi / 5.0) * 0.5 + np.sin(y * np.pi / 6.0) * 0.5) * 0.5 + 0.5
    wear = noise(SIZE, 68, seed)
    stains = noise(SIZE, 18, seed + 1)
    height = normalized(0.54 * weave + 0.30 * wear + 0.16 * stains)
    base = rgb_mix(low, high, normalized(0.56 * wear + 0.44 * stains))
    grime = np.clip((0.40 - stains) * 2.0, 0, 1)[..., None]
    base = np.clip(base.astype(np.float32) * (1.0 - 0.42 * grime), 0, 255).astype(np.uint8)
    rough = np.clip((0.82 + 0.16 * wear) * 255, 0, 255).astype(np.uint8)
    ao = np.clip((0.88 + 0.12 * stains) * 255, 0, 255).astype(np.uint8)
    return {"BaseColor": base, "Normal": normal_from_height(height, 2.8), "Roughness": rough,
            "Metallic": np.zeros((SIZE, SIZE), np.uint8), "AO": ao}


def main() -> None:
    manifest: dict[str, dict] = {}
    generators = (
        ("MAT_Leshy_Birch", birch),
        ("MAT_Leshy_DarkWood", dark_wood),
        ("MAT_Leshy_Moss", moss),
        ("MAT_Leshy_Copper", copper),
        ("MAT_Leshy_ClothRed", lambda: cloth("red", (48, 5, 6), (126, 24, 22), 50)),
        ("MAT_Leshy_ClothWhite", lambda: cloth("white", (96, 86, 68), (196, 184, 151), 60)),
    )
    for prefix, generator in generators:
        print(f"Generating {prefix}...", flush=True)
        maps = generator()
        save_set(prefix, maps, manifest)
        del maps
        gc.collect()
    (ROOT / "textures" / "TEXTURE_MANIFEST.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    print(json.dumps({"materials": list(manifest), "master_resolution": "4096x4096", "unity_resolution": "2048x2048"}))


if __name__ == "__main__":
    main()
