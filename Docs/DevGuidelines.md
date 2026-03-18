# Project Convention

## Data and text encoding rules

1. All text configuration files (including JSON/CSV/TSV/TXT/YAML) must be read and written as UTF-8.
2. Use UTF-8 without BOM when writing files from scripts.
3. Do not use default shell encoding when reading or writing text files.
4. Do not use editors that force legacy encodings (ANSI/GBK/GB2312) on these files.

## Script conventions

- Prefer running helper scripts under `Tools/Json` before data-heavy changes.
- `Update-CardArt.ps1`:
  - Adds/normalizes `artPath` fields in `Data/cards.json`.
  - Enforces UTF-8 in output.
- `Repair-DescriptionZh.ps1`:
  - Provides a recovery attempt for mojibake in `descriptionZh`.
  - Backs up original file before rewrite.
- `JsonEncodingGuard.ps1`:
  - Checks encoding sanity and JSON parse health for target files.

## Workflow (recommended)

1. Edit or generate data only with scripts in `Tools/Json`.
2. Run:
   - `pwsh Tools/Json/JsonEncodingGuard.ps1 -TargetJson Data/cards.json`
3. If checks pass, run project normally.
4. If parsing fails, run:
   - `pwsh Tools/Json/Repair-DescriptionZh.ps1 -InputPath Data/cards.json` (backup kept).

## Notes

- If a `descriptionZh` entry has been accidentally re-encoded, it may be unrecoverable by algorithm alone.
- When a value looks unfixable, restore that line from git history or known-good backup, then reapply scripts.
