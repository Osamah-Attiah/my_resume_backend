"""Normalize multi-codepoint Arabic ActualText clusters emitted in visual order.

QuestPDF correctly shapes RTL text and writes /ActualText for every glyph cluster.
Poppler and several ATS extractors reverse the characters inside multi-character
Arabic clusters a second time. Reversing only those cluster values keeps the
visible PDF byte-for-byte equivalent while restoring logical Unicode extraction.
"""

import re
import sys
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from pypdf.generic import DecodedStreamObject, NameObject


def normalize_cluster(match: re.Match[bytes]) -> bytes:
    raw = bytes.fromhex(match.group(1).decode("ascii"))
    text = raw.decode("utf-16-be")
    if text.startswith("\ufeff"):
        text = text[1:]
    if len(text) > 1 and any("\u0600" <= character <= "\u06ff" for character in text):
        text = text[::-1]
    encoded = ("\ufeff" + text).encode("utf-16-be").hex().upper().encode("ascii")
    return b"/ActualText <" + encoded + b">"


if len(sys.argv) != 3:
    raise SystemExit("Usage: normalize_actual_text.py <input.pdf> <output.pdf>")

input_path, output_path = map(Path, sys.argv[1:3])
reader = PdfReader(input_path)
writer = PdfWriter(clone_from=reader)

for page in writer.pages:
    original = page.get_contents().get_data()
    normalized = re.sub(rb"/ActualText <([0-9A-F]+)>", normalize_cluster, original)
    stream = DecodedStreamObject()
    stream.set_data(normalized)
    page[NameObject("/Contents")] = writer._add_object(stream)

output_path.parent.mkdir(parents=True, exist_ok=True)
with output_path.open("wb") as target:
    writer.write(target)
