"""Create repeatable QA silhouette masks from the five Leshy references.

The source PNGs are never modified.  Masks are produced with conservative
GrabCut seeds plus colour/edge recovery so that thin branch tips remain part of
the measured silhouette.
"""

from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
REF_DIR = ROOT / "source_refs"
QA_DIR = ROOT / "qa"
MASK_DIR = QA_DIR / "reference_masks"
PREVIEW_DIR = QA_DIR / "reference_mask_previews"

VIEWS = {
    "front": ("leshy_front.png", (74, 26, 950, 1518)),
    "back": ("leshy_back.png", (78, 29, 948, 1518)),
    "left": ("leshy_left.png", (220, 28, 835, 1517)),
    "front_3q": ("leshy_front_3q.png", (105, 27, 930, 1518)),
    "back_3q": ("leshy_back_3q.png", (94, 29, 936, 1518)),
}


def _largest_and_detail_components(mask: np.ndarray, detail: np.ndarray) -> np.ndarray:
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
    if count <= 1:
        return mask
    order = np.argsort(stats[1:, cv2.CC_STAT_AREA])[::-1] + 1
    keep = np.zeros_like(mask)
    largest = order[: min(24, len(order))]
    for label in largest:
        area = int(stats[label, cv2.CC_STAT_AREA])
        component = labels == label
        # Keep substantial components and tiny high-contrast branch/accessory islands.
        if area >= 24 or (area >= 5 and float(detail[component].mean()) > 24.0):
            keep[component] = 255
    return keep


def make_mask(image: np.ndarray, bbox: tuple[int, int, int, int]) -> tuple[np.ndarray, dict]:
    h, w = image.shape[:2]
    lab = cv2.cvtColor(image, cv2.COLOR_BGR2LAB).astype(np.float32)

    border = np.zeros((h, w), np.uint8)
    border[:18, :] = 1
    border[-12:, :] = 1
    border[:, :18] = 1
    border[:, -18:] = 1
    samples = lab[border == 1]
    bg = np.median(samples, axis=0)
    distance = np.linalg.norm(lab - bg, axis=2)

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    gx = cv2.Sobel(gray, cv2.CV_32F, 1, 0, ksize=3)
    gy = cv2.Sobel(gray, cv2.CV_32F, 0, 1, ksize=3)
    edge = cv2.magnitude(gx, gy)
    chroma = np.sqrt((lab[:, :, 1] - 128.0) ** 2 + (lab[:, :, 2] - 128.0) ** 2)
    detail = np.maximum(distance, np.minimum(edge, 80.0))

    x0, y0, x1, y1 = bbox
    inside = np.zeros((h, w), np.uint8)
    inside[y0:y1, x0:x1] = 1

    # GrabCut itself runs at half resolution for iteration speed.  Edge recovery
    # and component filtering below still operate on the original pixels.
    sw, sh = w // 2, h // 2
    small = cv2.resize(image, (sw, sh), interpolation=cv2.INTER_AREA)
    small_inside = cv2.resize(inside, (sw, sh), interpolation=cv2.INTER_NEAREST)
    small_border = cv2.resize(border, (sw, sh), interpolation=cv2.INTER_NEAREST)
    small_distance = cv2.resize(distance, (sw, sh), interpolation=cv2.INTER_AREA)
    small_edge = cv2.resize(edge, (sw, sh), interpolation=cv2.INTER_AREA)
    small_lab = cv2.cvtColor(small, cv2.COLOR_BGR2LAB).astype(np.float32)
    small_chroma = np.sqrt((small_lab[:, :, 1] - 128.0) ** 2 + (small_lab[:, :, 2] - 128.0) ** 2)
    small_luma = small_lab[:, :, 0]

    seeds = np.full((sh, sw), cv2.GC_PR_BGD, np.uint8)
    seeds[small_border == 1] = cv2.GC_BGD
    seeds[small_inside == 0] = cv2.GC_BGD
    probable_fg = (small_inside == 1) & (
        (small_chroma > 2.5) | (small_luma < 107.0) | (small_luma > 128.0) | (small_edge > 14.0)
    )
    seeds[probable_fg] = cv2.GC_PR_FGD
    definite_bg = (small_inside == 1) & (small_chroma < 1.5) & (
        (small_luma >= 107.0) & (small_luma <= 128.0) & (small_edge < 11.0)
    )
    seeds[definite_bg] = cv2.GC_BGD
    definite_fg = (small_inside == 1) & (
        (small_chroma > 7.0)
        | (small_luma < 98.0)
        | (small_luma > 138.0)
        | ((small_chroma > 3.0) & (small_edge > 18.0))
    )
    seeds[definite_fg] = cv2.GC_FGD

    bg_model = np.zeros((1, 65), np.float64)
    fg_model = np.zeros((1, 65), np.float64)
    cv2.grabCut(small, seeds, None, bg_model, fg_model, 5, cv2.GC_INIT_WITH_MASK)
    small_mask = np.where((seeds == cv2.GC_FGD) | (seeds == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
    mask = cv2.resize(small_mask, (w, h), interpolation=cv2.INTER_NEAREST)

    # Recover one-pixel and anti-aliased tips which GrabCut can classify as background.
    luma = lab[:, :, 0]
    appearance = (chroma > 3.0) | (luma < 106.0) | (luma > 130.0)
    recovery = ((inside == 1) & appearance & ((edge > 12.0) | (chroma > 6.0))).astype(np.uint8) * 255
    recovery = cv2.dilate(recovery, np.ones((3, 3), np.uint8), iterations=1)
    permissive_appearance = (chroma > 1.5) | (luma < 105.0) | (luma > 131.0) | (edge > 16.0)
    recovery = cv2.bitwise_and(recovery, permissive_appearance.astype(np.uint8) * 255)
    mask = cv2.bitwise_or(mask, recovery)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8), iterations=1)
    mask = _largest_and_detail_components(mask, detail)

    ys, xs = np.where(mask > 0)
    stats = {
        "background_lab_median": [round(float(v), 3) for v in bg],
        "pixel_area": int(len(xs)),
        "bbox_px": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
        "height_px": int(ys.max() - ys.min() + 1),
        "width_px": int(xs.max() - xs.min() + 1),
        "coverage": round(float(len(xs) / (w * h)), 6),
    }
    return mask, stats


def main() -> None:
    MASK_DIR.mkdir(parents=True, exist_ok=True)
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    manifest: dict[str, dict] = {}

    for view, (filename, bbox) in VIEWS.items():
        source = REF_DIR / filename
        image = cv2.imread(str(source), cv2.IMREAD_COLOR)
        if image is None:
            raise FileNotFoundError(source)
        mask, stats = make_mask(image, bbox)
        cv2.imwrite(str(MASK_DIR / f"{view}.png"), mask)

        contour = cv2.Canny(mask, 80, 160)
        preview = image.copy()
        preview[contour > 0] = (0, 255, 0)
        tint = np.zeros_like(image)
        tint[:, :, 2] = 120
        preview = np.where((mask == 0)[..., None], cv2.addWeighted(preview, 0.42, tint, 0.58, 0), preview)
        cv2.imwrite(str(PREVIEW_DIR / f"{view}_mask_preview.png"), preview)
        manifest[view] = {"source": filename, "mask": f"reference_masks/{view}.png", **stats}

    (QA_DIR / "reference_mask_stats.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    print(json.dumps(manifest, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
