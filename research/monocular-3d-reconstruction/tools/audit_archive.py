"""Audit the publishable research scope; only hash/check approved derivatives, never read full private source images."""
import ast,json,re,hashlib,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];R=ROOT/"research/monocular-3d-reconstruction"
files=[p for p in R.rglob("*") if p.is_file() and "__pycache__" not in p.parts]
approval=R/"assets/approved/APPROVAL.json"
approved={}
if approval.exists():
    for item in json.loads(approval.read_text(encoding="utf-8-sig"))["files"]:
        approved["assets/approved/"+item["file"]]=item
allowed={".md",".svg",".json",".py"}
failures=[]
for p in files:
    rel=str(p.relative_to(R)).replace("\\","/")
    if p.suffix==".png":
        expected=approved.get(rel)
        if not expected or hashlib.sha256(p.read_bytes()).hexdigest()!=expected["sha256"]:
            failures.append("Unapproved or changed raster: "+rel)
        else:
            import struct
            raw=p.read_bytes()
            if raw[:8]!=b"\x89PNG\r\n\x1a\n" or list(struct.unpack(">II",raw[16:24]))!=expected["dimensions"]:
                failures.append("Invalid PNG dimensions: "+rel)
            offset=8
            while offset<len(raw):
                length=int.from_bytes(raw[offset:offset+4],"big");kind=raw[offset+4:offset+8]
                if kind in (b"eXIf",b"iTXt",b"tEXt",b"zTXt"):failures.append("Unexpected PNG metadata: "+rel)
                offset+=length+12
    elif p.name!=".gitignore" and p.suffix not in allowed:
        failures.append("Unexpected file type: "+str(p.relative_to(ROOT)))
    if p.stat().st_size>1_000_000:failures.append("Oversized archive file: "+p.name)
    if p.suffix==".py":ast.parse(p.read_text(encoding="utf-8-sig"),filename=str(p))
    if p.suffix==".svg":
        tree=ET.fromstring(p.read_text(encoding="utf-8-sig"))
        for el in tree.iter():
            if el.tag.rsplit("}",1)[-1] in ("image","script","foreignObject"):failures.append("Raster/script/foreignObject in SVG: "+p.name)
            for k,v in el.attrib.items():
                if k.endswith("href") and not v.startswith("#"):failures.append("External SVG reference: "+p.name)
    if p.suffix in (".md",".json",".py"):
        text=p.read_text(encoding="utf-8-sig")
        if re.search(r"[A-Z]:[\\/](Users|Windows)[\\/]",text,re.I):failures.append("Machine-specific path: "+p.name)
        if re.search(r"gh[pousr]_[A-Za-z0-9]{20,}|-----BEGIN (RSA |OPENSSH )?PRIVATE KEY-----",text):failures.append("Secret-like content: "+p.name)
readme=(ROOT/"README.md").read_text(encoding="utf-8-sig")
headings=[re.sub(r"[^\w -]","",s.lower()).replace(" ","-") for s in re.findall(r"^#{1,6} (.+)$",readme,re.M)]
links=re.findall(r'!?\[[^\]]*\]\(([^)\s]+)\)|href="([^"]+)"|src="([^"]+)"',readme)
local_count=0
for pair in links:
    url=next(x for x in pair if x)
    if url.startswith(("http://","https://")):continue
    if url.startswith("#"):
        if url[1:] not in headings:failures.append("Missing anchor: "+url)
    else:
        local_count+=1
        if not (ROOT/url.split("#")[0]).is_file():failures.append("Missing local target: "+url)
data=json.loads((R/"data/results.json").read_text())
assert data["dense_exhaustive"]["registered"]==235 and data["dense_exhaustive"]["requested"]==364
assert data["baseline128"]["registered"]==38 and data["dense_analysis"]["same128_registered"]==82
assert data["v1"]["A"]["training_views"]==33 and data["v2"]["A"]["heldout_views"]==6
provenance=[]
for p in sorted((R/"assets").glob("*.svg")):
    kind="aggregate measurement chart" if p.stem in ("registration","gaussian-quality","runtime-memory") else ("aggregate experiment-order timeline" if p.stem=="timeline" else "explicit layout placeholder; not reconstruction imagery")
    provenance.append({"file":p.name,"kind":kind,"sha256":hashlib.sha256(p.read_bytes()).hexdigest(),"private_room_pixels":False})
(R/"assets/PROVENANCE.json").write_text(json.dumps(provenance,indent=2))
receipt={"status":"passed" if not failures else "failed","readme_sha256":hashlib.sha256((ROOT/"README.md").read_bytes()).hexdigest(),
    "research_files_checked":len(files),"local_readme_targets":local_count,"mermaid_blocks":readme.count("~~~mermaid"),
    "private_raster_files":0,"failures":failures,
    "limits":"Static scope/privacy/link checks; not a complete secret scanner or live GitHub branch-page test."}
out=ROOT/".local/archive-audit.json";out.write_text(json.dumps(receipt,indent=2))
print(json.dumps(receipt,indent=2))
if approved:
    receipt["private_raster_files"]=len(approved)
    receipt["authorized_equipment_crops"]=sum(x.get("id","").startswith("C") for x in approved.values())
    receipt["authorized_viewer_screenshots"]=sum(x.get("id","").startswith("S") for x in approved.values())
    receipt["unapproved_rasters"]=len([x for x in failures if "raster" in x])
    for item in approved.values():
        provenance.append({"file":"approved/"+item["file"],"kind":("user-authorized viewer screenshot with documented privacy handling" if item.get("id","").startswith("S") else "actual equipment-only privacy crop"),
            "sha256":item["sha256"],"private_room_pixels":True,"authorization_scope":item["authorization_scope"]})
    (R/"assets/PROVENANCE.json").write_text(json.dumps(provenance,indent=2))
    out.write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt,indent=2))
if failures:raise SystemExit(1)
