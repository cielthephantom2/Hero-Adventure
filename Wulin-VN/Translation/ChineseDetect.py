#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phát hiện các dòng chứa ký tự Trung Quốc trong tệp văn bản,
ghi chúng ra output.txt và thống kê số dòng.
Bỏ qua những dòng có chứa 'TagNum' hoặc 'NpcJobTalk'.
"""

import re
import sys
from pathlib import Path

# Regex tiếng Trung (CJK)
CJK_PATTERN = re.compile(r'[\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]+')

# Các chuỗi phải bỏ qua (viết hoa–thường không phân biệt)
SKIP_KEYWORDS = ("TAGNUM", "NPCJOBTALK")

def detect_chinese_lines(txt_path: str, out_path: str = "output.txt"):
    txt_path = Path(txt_path)
    if not txt_path.is_file():
        raise FileNotFoundError(f"Không tìm thấy tệp: {txt_path}")

    out_file = Path(out_path)
    count = 0

    # Mở output.txt trước để ghi (xóa nội dung cũ nếu có)
    with out_file.open("w", encoding="utf-8") as fout:
        with txt_path.open("r", encoding="utf-8") as fin:
            for idx, line in enumerate(fin, 1):
                # Bỏ qua các dòng chứa keyword cần skip
                upper_line = line.upper()
                if any(kw in upper_line for kw in SKIP_KEYWORDS):
                    continue

                # Kiểm tra ký tự Trung Quốc
                if CJK_PATTERN.search(line):
                    print(line.rstrip())  # in ra console không có "Line xxx:"
                    fout.write(line)      # ghi thẳng dòng gốc
                    count += 1

    print(f"\n→ Đã ghi {count} dòng chứa ký tự Trung Quốc vào '{out_file.name}'")
    return count

if __name__ == "__main__":
    # Cách dùng: python detect_cjk.py <file.txt> [output.txt]
    if len(sys.argv) not in (2, 3):
        print("Cách dùng: python detect_cjk.py <file.txt> [output.txt]")
        sys.exit(1)

    input_txt = sys.argv[1]
    output_txt = sys.argv[2] if len(sys.argv) == 3 else "output.txt"
    detect_chinese_lines(input_txt, output_txt)
