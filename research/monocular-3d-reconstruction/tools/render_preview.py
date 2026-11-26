"""Render tracked Markdown with available local markdown-it-py."""
import json,re,hashlib
from pathlib import Path
from markdown_it import MarkdownIt
ROOT=Path(__file__).resolve().parents[3];R=ROOT/"research/monocular-3d-reconstruction";LOCAL=ROOT/".local";LOCAL.mkdir(parents=True,exist_ok=True)
d=json.loads((R/"data/results.json").read_text())
lines=["# Experiment ledger","","Aggregate observations from actual local metric records. Only authorized equipment-only image derivatives accompany these measurements; raw captures and geometry are not distributed.","",
"## LingBot execution","","| Recording / frames | Inference s | Total s | Reserved GiB | Depth-derived points |","|---|---:|---:|---:|---:|"]
for key,label in [("lingbot32","V1 /32"),("lingbot128","V1 /128"),("lingbot_v2","V2 /128")]:
    m=d[key];lines.append(f'| {label} | {m["inference_seconds"]:.2f} | {m["total_seconds"]:.2f} | {m["peak_reserved_bytes"]/2**30:.3f} | {m["ply_point_count"]:,} |')
lines+=["","## Matched Gaussian runs","","| Cohort / method | Train / held-out | PSNR dB | SSIM | Training s | Total s | Reserved GiB |","|---|---:|---:|---:|---:|---:|---:|"]
for version in ("v1","v2"):
    for method in ("A","B"):
        m=d[version][method]
        lines.append(f'| {version.upper()} / {method} | {m["training_views"]} / {m["heldout_views"]} | {m["test_psnr"]:.2f} | {m["test_ssim"]:.3f} | {m["training_seconds"]:.2f} | {m["total_seconds"]:.2f} | {m["peak_gpu_reserved_bytes"]/2**30:.3f} |')
lines+=["","Different views/cohorts preclude a controlled cross-recording geometry comparison. Camera estimation may see held-out RGB.","",
"## V2 registration","","| Sampling / matching | Largest model | Fraction | Stage s | Sparse points | Reprojection px |","|---|---:|---:|---:|---:|---:|"]
for key,label in [("baseline128","128 / exhaustive extension"),("dense_sequential","364 / sequential"),("blur_sequential","364 / blur-aware sequential"),("dense_exhaustive","364 / exhaustive extension")]:
    m=d[key];lines.append(f'| {label} | {m["registered"]}/{m["requested"]} | {100*m["registered"]/m["requested"]:.1f}% | {m["total_seconds"]:.2f} | {m["points3D"]:,} | {m["mean_reprojection_error_pixels"]:.3f} |')
lines+=["","Exhaustive timings are additional to sequential extraction/matching. Dense:91.13+441.49=532.62s; preparation56.45s. Previous128:26.54+59.61=86.16s.",
"Same128 control:38→82 registered. No Gaussian training on the235-camera result.",
"","## Evidence boundary","","Arrays, inverses, rotations, camera names and disk counts were checked; this is not physical ground truth.",
"Source records remain in the POC. evidence-sources.json records hashes and logical record names. Results.json contains explicit aggregate-field allowlists, no raw poses or private images."]
(R/"technical/EXPERIMENTS.md").write_text("\n".join(lines),encoding="utf-8")
md=MarkdownIt("commonmark",{"html":True}).enable("table")
def render(text):
    result=md.render(text);seen={}
    def heading(m):
        level,content=m.group(1),m.group(2)
        plain=re.sub("<[^>]+>","",content).lower();slug=re.sub(r"[^\w -]","",plain).replace(" ","-")
        n=seen.get(slug,0);seen[slug]=n+1
        return f'<h{level} id="{slug+("-"+str(n) if n else "")}">{content}</h{level}>'
    result=re.sub(r"<h([1-6])>(.*?)</h\1>",heading,result)
    return re.sub(r'<pre><code class="language-mermaid">(.*?)</code></pre>',r'<pre class="mermaid">\1</pre>',result,flags=re.S)
body=render((ROOT/"README.md").read_text(encoding="utf-8-sig"))
renderer="markdown-it-py"
check=LOCAL/"github-render-check.json"
if check.exists():
    receipt=json.loads(check.read_text(encoding="utf-8-sig"))
    if receipt.get("status")==200 and receipt["readme_sha256"].lower()==hashlib.sha256((ROOT/"README.md").read_bytes()).hexdigest():
        from bs4 import BeautifulSoup
        soup=BeautifulSoup((LOCAL/"github-render.html").read_text(encoding="utf-8-sig"),"html.parser")
        for node in soup.select(".highlight-source-mermaid"):
            pre=soup.new_tag("pre");pre["class"]="mermaid";pre.string=node.get_text();node.replace_with(pre)
        for node in soup.find_all(re.compile("^h[1-6]$")):
            slug=re.sub(r"[^\w -]","",node.get_text().lower()).replace(" ","-")
            node["id"]=slug
        body=str(soup);renderer="GitHub Markdown API HTML; local heading IDs and Mermaid hydration"
(LOCAL/"readme-rendered.html").write_text(body,encoding="utf-8")
for p in R.rglob("*.md"):
    out=LOCAL/"documents"/p.relative_to(ROOT).with_suffix(".html");out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(render(p.read_text(encoding="utf-8-sig")),encoding="utf-8")
(LOCAL/"local-render.json").write_text(json.dumps({"renderer":renderer,"readme_sha256":hashlib.sha256((ROOT/"README.md").read_bytes()).hexdigest(),"note":"Local aid, not a live GitHub branch-page test."},indent=2))
print("Rendered root README and English notes.")
