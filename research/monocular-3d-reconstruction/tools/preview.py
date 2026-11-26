"""Local research README preview. No image upload or approval endpoint."""
import argparse,json,html,mimetypes,hashlib
from pathlib import Path
from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
from urllib.parse import unquote,urlsplit
ROOT=Path(__file__).resolve().parents[3]
LOCAL=ROOT/".local";RESEARCH=ROOT/"research/monocular-3d-reconstruction"
STYLE=""".markdown-alert{padding:16px 20px;border:1px solid #c9d9eb;background:#f0f5fa;margin-bottom:20px}.markdown-alert-title{font-weight:700;display:flex;align-items:center;gap:8px}body{margin:0;background:#f6f8fa;color:#1f2328;font:16px/1.65 -apple-system,BlinkMacSystemFont,Segoe UI,Arial,sans-serif}nav{background:#142b49;color:#fff;padding:15px 28px;display:flex;gap:24px;align-items:center;flex-wrap:wrap}nav a{color:#d1f1f4}main{max-width:1012px;margin:28px auto;background:white;border:1px solid #d1d9e0;border-radius:8px;padding:44px;box-sizing:border-box}img,svg{max-width:100%;height:auto}h1{font-size:2em}h2{font-size:1.55em;border-bottom:1px solid #d1d9e0;padding-bottom:.3em;margin-top:2.1em;line-height:1.3}h3{font-size:1.2em;margin-top:1.7em;line-height:1.35}p,ul,ol,pre,table,blockquote{margin:0 0 18px}a{color:#0969da;text-underline-offset:3px}a:hover{text-decoration-thickness:2px}blockquote{padding:12px 20px;background:#f0f5fa;border:1px solid #c9d9eb;color:#304a68}pre{padding:18px;overflow:auto;background:#f6f8fa;border-radius:6px;font-size:13px;line-height:1.6}code{font-family:Consolas,monospace}table{border-collapse:collapse;display:block;max-width:100%;overflow:auto}th,td{border:1px solid #d1d9e0;padding:8px 12px;text-align:left}tr:nth-child(2n){background:#f6f8fa}details{border:1px solid #d1d9e0;padding:15px;margin-bottom:20px}summary{cursor:pointer}summary:focus-visible,a:focus-visible{outline:3px solid #007f87;outline-offset:3px}figure{margin:25px 0}figcaption{color:#42536b}hr{border:0;border-top:1px solid #d1d9e0;margin:38px 0}.review{border-top:1px solid #d1d9e0;padding:30px 0}.pending{color:#7d4e09;font-weight:700}.mermaid{text-align:center;background:#fff;overflow:auto}footer{padding:20px;text-align:center;color:#51647e;font-size:13px}@media(max-width:700px){main{margin:0;border:0;border-radius:0;padding:22px 18px}nav{padding:12px 18px;font-size:13px;gap:14px}h1{font-size:1.7em}h2{font-size:1.35em}}"""
def shell(title,body):
    return ('<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
      '<title>'+html.escape(title)+'</title><style>'+STYLE+'</style></head><body><nav><strong>RESEARCH · LOCAL REVIEW</strong><a href="/">README preview</a><a href="/review">Private image candidates</a><span>Equipment-only publication scope</span></nav><main>'+body+
      '</main><footer>Local rendering aid. The live GitHub branch page is checked separately after the authorized research-branch push.</footer>'
      '<script src="/local-runtime/mermaid.min.js"></script><script>if(window.mermaid){mermaid.initialize({startOnLoad:true,theme:"neutral",securityLevel:"strict",fontFamily:"Arial"});}</script></body></html>')
class Handler(BaseHTTPRequestHandler):
    def send(self,body,mime="text/html; charset=utf-8",status=200):
        if isinstance(body,str):body=body.encode()
        self.send_response(status);self.send_header("Content-Type",mime);self.send_header("Cache-Control","no-store")
        self.send_header("X-Content-Type-Options","nosniff");self.send_header("Referrer-Policy","no-referrer")
        self.send_header("Content-Security-Policy","default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; font-src 'self' data:; connect-src 'self'")
        self.end_headers();self.wfile.write(body)
    def do_GET(self):
        path=unquote(urlsplit(self.path).path)
        if path=="/":
            p=LOCAL/"readme-rendered.html"
            return self.send(shell("Monocular reconstruction — local README preview",p.read_text(encoding="utf-8"))) if p.exists() else self.send("Run the renderer first.",status=503)
        if path=="/local-runtime/mermaid.min.js":
            p=LOCAL/"mermaid.min.js"
            return self.send(p.read_bytes(),"application/javascript") if p.exists() else self.send("Mermaid runtime not prepared.",status=404)
        manifest=LOCAL/"private-review.json"
        data=json.loads(manifest.read_text(encoding="utf-8-sig")) if manifest.exists() else []
        if path=="/review":
            body='<h1>Private image review</h1><p class="pending">Full original images are not authorized. Only the exact equipment crops in the README are in scope.</p><p>These are actual experiment outputs. Review the exact files before approving specific images in chat. This page has no upload or approval action.</p>'
            for row in data:
                body+='<section class="review"><h2>'+html.escape(row["id"]+" · "+row["title"])+'</h2><p>'+html.escape(row["caption"])+'</p>'
                for i,f in enumerate(row.get("files",[])):
                    body+='<figure><img loading="lazy" src="/private/'+row["id"]+'/'+str(i)+'" alt="'+html.escape(row["title"])+'"><figcaption>'+html.escape(f["name"])+' · SHA256 <code>'+f["sha256"]+'</code></figcaption></figure>'
                if not row.get("files"):body+='<p>No candidate screenshot is saved yet. The README retains its placeholder.</p>'
                body+='</section>'
            return self.send(shell("Private experiment image review",body))
        if path.startswith("/private/"):
            parts=path.strip("/").split("/")
            try:
                assert len(parts)==3
                row=next(r for r in data if r["id"]==parts[1]);f=row["files"][int(parts[2])]
                p=Path(f["path"]);return self.send(p.read_bytes(),mimetypes.guess_type(p.name)[0] or "application/octet-stream")
            except (AssertionError,StopIteration,ValueError,IndexError,FileNotFoundError):return self.send("Not found",status=404)
        if path.startswith("/research/monocular-3d-reconstruction/"):
            p=(ROOT/path.lstrip("/")).resolve()
            if not p.is_relative_to(RESEARCH.resolve()):return self.send("Not found",status=404)
            if p.is_file() and p.suffix==".md":
                rendered=LOCAL/"documents"/p.relative_to(ROOT).with_suffix(".html")
                return self.send(shell(p.stem,rendered.read_text(encoding="utf-8"))) if rendered.exists() else self.send("Not rendered.",status=404)
            if p.is_file() and p.suffix==".png" and p.parent==(RESEARCH/"assets/approved").resolve():
                approved=json.loads((RESEARCH/"assets/approved/APPROVAL.json").read_text(encoding="utf-8-sig"))
                entry=next((x for x in approved["files"] if x["file"]==p.name),None)
                raw=p.read_bytes()
                if entry and hashlib.sha256(raw).hexdigest()==entry["sha256"]:
                    return self.send(raw,"image/png")
                return self.send("Not authorized",status=404)
            if p.is_file() and p.suffix in (".svg",".json"):return self.send(p.read_bytes(),mimetypes.guess_type(p.name)[0] or "application/json")
        return self.send("Not found",status=404)
    def do_POST(self):self.send("No upload or approval endpoint exists.",status=405)
    def log_message(self,*args):pass
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--port",type=int,default=8093);a=p.parse_args()
    print(f"Research preview: http://127.0.0.1:{a.port}/",flush=True)
    ThreadingHTTPServer(("127.0.0.1",a.port),Handler).serve_forever()
