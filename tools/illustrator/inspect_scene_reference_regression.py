"""Read recorded outgoing images; write comparison evidence only, never edit source images."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageStat

parser = argparse.ArgumentParser()
parser.add_argument("record", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
trace = json.loads((args.record / "trace.json").read_text(encoding="utf-8-sig"))
request = next(e for e in trace["events"] if e["stage"] == "director_request")
files = [c["image_url"]["url"] for c in request["payload"]["messages"][1]["content"]
         if c.get("type") == "image_url"]
files = [f for f in files if isinstance(f, dict) and f.get("width") == 1024 and f.get("height") == 576]
images = [Image.open(args.record / f["file"]).convert("RGB") for f in files]
args.output.mkdir(parents=True, exist_ok=True)
sheet = Image.new("RGB", (1024, 318 * 3), "#eeeeee")
draw = ImageDraw.Draw(sheet)
metrics = []
for index, (info, image) in enumerate(zip(files, images)):
    x, y = (index % 2) * 512, (index // 2) * 318
    sheet.paste(image.resize((512, 288)), (x, y + 25))
    draw.text((x + 5, y + 5), f"sent scene reference {index + 1}", fill="black")
    background = image.crop((0, 0, 1024, 170))
    baseline = images[0].crop((0, 0, 1024, 170))
    metrics.append({"reference": index + 1, "file": info["file"],
                    "background_mean_absolute_rgb_difference_from_first": sum(ImageStat.Stat(ImageChops.difference(background, baseline)).mean) / 3})
sheet.save(args.output / "recorded-scene-views.jpg", quality=90)
red, green, blue = images[0].split()
swapped = Image.merge("RGB", (blue, green, red))
comparison = Image.new("RGB", (1024, 318), "#eeeeee")
comparison.paste(images[0].resize((512, 288)), (0, 25))
comparison.paste(swapped.resize((512, 288)), (512, 25))
draw = ImageDraw.Draw(comparison)
draw.text((5, 5), "Actual transmitted scene PNG", fill="black")
draw.text((517, 5), "Diagnostic R/B swap candidate (not production)", fill="black")
comparison.save(args.output / "scene-export-color-comparison.jpg", quality=92)
(args.output / "scene-reference-evidence.json").write_text(json.dumps({"recordId": trace["id"],
    "moduleVersionId": trace["moduleVersionId"], "views": metrics,
    "limit": "Fixed-background difference supports repeated framing, not a native cause diagnosis. Color swap is a diagnostic candidate only."}, indent=2), encoding="utf-8")
print(json.dumps(metrics, indent=2))
