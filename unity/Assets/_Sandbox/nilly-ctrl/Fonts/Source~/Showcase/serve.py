"""Builds the font showcase from the current fonts and serves it on this computer.

    python serve.py

Then open http://127.0.0.1:8760/ in a browser. Ctrl+C stops it. The page fetches its data file,
so it has to be served like this; opening index.html directly shows no fonts.
"""
import functools
import http.server
import os
import runpy

HERE = os.path.dirname(os.path.abspath(__file__))
PORT = 8760

runpy.run_path(os.path.join(HERE, "pack.py"), run_name="__main__")
handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=HERE)
with http.server.ThreadingHTTPServer(("127.0.0.1", PORT), handler) as server:
    print(f"Font showcase: http://127.0.0.1:{PORT}/   (Ctrl+C to stop)")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
