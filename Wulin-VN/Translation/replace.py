import json
import csv
from pathlib import Path

# ----- 1. Đọc từ điển (TSV) -----
dict_path = Path("tudien.csv")
replace_map = {}

with dict_path.open("r", encoding="utf-8") as f:
    reader = csv.reader(f, delimiter="\t")
    for row in reader:
        if len(row) >= 2:
            chinese = row[0].strip()
            vietnamese = row[1].strip()
            replace_map[chinese] = vietnamese

# ----- 2. Đọc JSON -----
source_path = Path("LocData.json")
output_path = Path("source_translated.json")

with source_path.open("r", encoding="utf-8") as f:
    data = json.load(f)

# ----- 3. Thay thế trường "Translated" nếu khớp -----
changed_count = 0
for entry in data:
    translated = entry.get("Translated", "").strip()
    if translated in replace_map:
        entry["Translated"] = replace_map[translated]
        changed_count += 1

# ----- 4. Ghi lại ra file mới -----
with output_path.open("w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print(f"Hoàn tất: Đã thay {changed_count} dòng.")
print(f"Kết quả được lưu vào: {output_path}")