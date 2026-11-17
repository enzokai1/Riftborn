"""Conservative static Unity asset audit. Does not delete or modify assets."""
from pathlib import Path
from collections import defaultdict
import csv
import re

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets'
TEXT_EXTENSIONS = {'.meta', '.unity', '.prefab', '.asset', '.mat', '.anim',
                   '.controller', '.overrideController', '.cs', '.shader',
                   '.json', '.inputactions', '.ps1', '.py', '.md',
                   '.scenetemplate', '.shadergraph', '.shadersubgraph'}


def audit():
    references = defaultdict(set)
    code = []
    for folder in ('Assets', 'ProjectSettings', 'Packages', 'Tools'):
        for path in (ROOT / folder).rglob('*'):
            if not path.is_file() or path.suffix not in TEXT_EXTENSIONS:
                continue
            content = path.read_text(encoding='utf-8-sig', errors='replace')
            for guid in re.findall(r'guid:\s*([0-9a-f]{32})', content):
                references[guid].add(path)
            if path.suffix in {'.cs', '.ps1', '.py'} and path != Path(__file__).resolve():
                code.append(content)
    code = '\n'.join(code)
    rows = []
    for meta in sorted(ASSETS.rglob('*.meta')):
        asset = meta.with_suffix('')
        if not asset.is_file():
            continue
        match = re.search(r'^guid:\s*([0-9a-f]{32})', meta.read_text(), re.M)
        if not match or references[match[1]] - {meta}:
            continue
        relative = asset.relative_to(ROOT).as_posix()
        if 'Resources' in asset.parts or 'Editor' in asset.parts or asset.suffix in {'.cs', '.shader', '.hlsl', '.cginc', '.dll', '.md', '.txt', '.pdf', '.html', '.shadergraph', '.shadersubgraph'}:
            continue
        if relative in code or asset.name in code:
            continue
        rows.append((relative, asset.stat().st_size,
                     'Sem referencia GUID externa ou caminho literal detectado; revisar antes de remover'))
    output = ROOT / 'Docs' / 'AssetsSemReferencia.csv'
    output.parent.mkdir(exist_ok=True)
    with output.open('w', newline='', encoding='utf-8-sig') as stream:
        writer = csv.writer(stream)
        writer.writerow(('arquivo', 'bytes', 'observacao'))
        writer.writerows(rows)
    print(f'{len(rows)} candidatos; relatorio: {output}')
    counts = defaultdict(int)
    for path, _, _ in rows:
        counts['/'.join(path.split('/')[:4])] += 1
    for folder, count in sorted(counts.items()):
        print(f'{count:4} {folder}')


if __name__ == '__main__':
    audit()
