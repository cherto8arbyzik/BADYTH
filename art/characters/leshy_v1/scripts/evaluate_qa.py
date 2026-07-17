"""Measure rendered Leshy silhouettes against the locked reference masks."""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
QA = ROOT / "qa"
RENDERS = ROOT / "renders"
VIEWS = ("front", "back", "left", "front_3q", "back_3q")


def binary(path: Path, rendered: bool = False) -> np.ndarray:
    image = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    if image is None:
        raise FileNotFoundError(path)
    if image.ndim == 3:
        if image.shape[2] == 4:
            image = cv2.cvtColor(image, cv2.COLOR_BGRA2GRAY)
        else:
            image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    threshold = 128 if rendered else 1
    return (image > threshold).astype(np.uint8)


def bbox(mask: np.ndarray) -> list[int]:
    ys, xs = np.where(mask > 0)
    return [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def contour_distance(a: np.ndarray, b: np.ndarray) -> tuple[float, float]:
    ca = cv2.Canny(a * 255, 80, 160) > 0
    cb = cv2.Canny(b * 255, 80, 160) > 0
    dist_to_b = cv2.distanceTransform((~cb).astype(np.uint8), cv2.DIST_L2, 3)
    dist_to_a = cv2.distanceTransform((~ca).astype(np.uint8), cv2.DIST_L2, 3)
    values = np.concatenate((dist_to_b[ca], dist_to_a[cb]))
    return float(values.mean()), float(np.percentile(values, 95))


def metrics(reference: np.ndarray, render: np.ndarray) -> dict:
    intersection = int(np.logical_and(reference, render).sum())
    union = int(np.logical_or(reference, render).sum())
    render_area = int(render.sum())
    ref_area = int(reference.sum())
    mean_contour, p95_contour = contour_distance(reference, render)
    ref_box = bbox(reference)
    height = ref_box[3] - ref_box[1] + 1
    return {
        "iou": round(intersection / union, 6),
        "precision": round(intersection / max(render_area, 1), 6),
        "recall": round(intersection / max(ref_area, 1), 6),
        "reference_area_px": ref_area,
        "render_area_px": render_area,
        "reference_bbox_px": ref_box,
        "render_bbox_px": bbox(render),
        "mean_contour_distance_px": round(mean_contour, 4),
        "p95_contour_distance_px": round(p95_contour, 4),
        "mean_contour_distance_height_pct": round(100.0 * mean_contour / height, 4),
        "p95_contour_distance_height_pct": round(100.0 * p95_contour / height, 4),
    }


def overlay(reference: np.ndarray, render: np.ndarray) -> np.ndarray:
    h, w = reference.shape
    out = np.full((h, w, 3), 24, np.uint8)
    ref_only = (reference == 1) & (render == 0)
    render_only = (reference == 0) & (render == 1)
    common = (reference == 1) & (render == 1)
    out[common] = (174, 174, 174)
    out[ref_only] = (36, 40, 224)       # red in BGR
    out[render_only] = (224, 115, 32)   # cyan-blue in BGR
    ref_contour = cv2.Canny(reference * 255, 80, 160) > 0
    render_contour = cv2.Canny(render * 255, 80, 160) > 0
    out[ref_contour] = (0, 255, 0)
    out[render_contour] = (255, 0, 255)
    return out


def append_log(iteration: int, view_metrics: dict) -> None:
    log = QA / "iteration_log.md"
    front = view_metrics["front"]
    back = view_metrics["back"]
    left = view_metrics["left"]
    block = (
        f"\n## Iteration {iteration:02d}\n\n"
        f"- Evaluated: {datetime.now(timezone.utc).isoformat()}\n"
        f"- Front IoU: {front['iou']:.6f}; mean contour: {front['mean_contour_distance_height_pct']:.3f}% H\n"
        f"- Back IoU: {back['iou']:.6f}; mean contour: {back['mean_contour_distance_height_pct']:.3f}% H\n"
        f"- Left IoU: {left['iou']:.6f}; mean contour: {left['mean_contour_distance_height_pct']:.3f}% H\n"
        "- Overlay legend: green = reference contour, magenta = render contour, red = missing model area, cyan = excess model area.\n"
    )
    if not log.exists():
        log.write_text("# Leshy v1 QA iteration log\n", encoding="utf-8")
    with log.open("a", encoding="utf-8") as handle:
        handle.write(block)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--iteration", type=int, required=True)
    args = parser.parse_args()
    render_dir = RENDERS / f"iter_{args.iteration:02d}"
    overlay_dir = QA / f"iter_{args.iteration:02d}" / "overlays"
    render_mask_dir = QA / f"iter_{args.iteration:02d}" / "render_masks"
    overlay_dir.mkdir(parents=True, exist_ok=True)
    render_mask_dir.mkdir(parents=True, exist_ok=True)

    result = {"iteration": args.iteration, "views": {}}
    for view in VIEWS:
        ref = binary(QA / "reference_masks" / f"{view}.png")
        rendered = binary(render_dir / f"{view}_silhouette.png", rendered=True)
        if rendered.shape != ref.shape:
            rendered = cv2.resize(rendered, (ref.shape[1], ref.shape[0]), interpolation=cv2.INTER_NEAREST)
        result["views"][view] = metrics(ref, rendered)
        cv2.imwrite(str(render_mask_dir / f"{view}.png"), rendered * 255)
        cv2.imwrite(str(overlay_dir / f"{view}_overlay.png"), overlay(ref, rendered))

    gates = {"front": 0.93, "back": 0.90, "left": 0.90}
    result["acceptance"] = {
        view: {"required_iou": gate, "passed": result["views"][view]["iou"] >= gate}
        for view, gate in gates.items()
    }
    result["all_silhouette_gates_passed"] = all(x["passed"] for x in result["acceptance"].values())
    metrics_file = QA / "metrics.json"
    history = {"iterations": []}
    if metrics_file.exists():
        history = json.loads(metrics_file.read_text(encoding="utf-8"))
    history["iterations"] = [x for x in history.get("iterations", []) if x.get("iteration") != args.iteration]
    history["iterations"].append(result)
    history["iterations"].sort(key=lambda x: x["iteration"])
    history["latest_iteration"] = args.iteration
    metrics_file.write_text(json.dumps(history, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    append_log(args.iteration, result["views"])
    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
