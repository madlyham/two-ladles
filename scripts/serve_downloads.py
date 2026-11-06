import os
import sys
import http.server
import socketserver

PORT = 8080
PROJECT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APK_PATH = os.path.join(PROJECT_DIR, "build", "android", "two-ladles-debug.apk")
EXE_PATH = os.path.join(PROJECT_DIR, "build", "windows", "two-ladles.exe")

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Two Ladles — Build Download Portal</title>
    <style>
        :root {
            --bg: #12181b;
            --card: #1e262c;
            --text: #e2e8f0;
            --primary: #22c55e;
            --primary-hover: #16a34a;
            --secondary: #38bdf8;
            --secondary-hover: #0284c7;
            --accent: #f59e0b;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            background-color: var(--bg);
            color: var(--text);
            margin: 0;
            padding: 24px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            box-sizing: border-box;
        }
        .container {
            background-color: var(--card);
            border-radius: 16px;
            padding: 32px;
            max-width: 520px;
            width: 100%;
            box-shadow: 0 10px 25px rgba(0,0,0,0.5);
            border: 1px solid #334155;
            text-align: center;
        }
        h1 {
            margin: 0 0 8px 0;
            font-size: 28px;
            color: #ffffff;
            letter-spacing: -0.5px;
        }
        .subtitle {
            color: #94a3b8;
            margin-bottom: 24px;
            font-size: 15px;
        }
        .badge {
            display: inline-block;
            background: #0f766e;
            color: #ccfbf1;
            padding: 4px 12px;
            border-radius: 9999px;
            font-size: 13px;
            font-weight: 600;
            margin-bottom: 20px;
        }
        .btn {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 12px;
            width: 100%;
            padding: 16px 20px;
            border-radius: 12px;
            font-size: 17px;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            transition: all 0.2s ease;
            box-sizing: border-box;
            margin-bottom: 16px;
        }
        .btn-apk {
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            color: #ffffff;
            box-shadow: 0 4px 14px rgba(16, 185, 129, 0.4);
        }
        .btn-apk:hover {
            background: linear-gradient(135deg, #059669 0%, #047857 100%);
            transform: translateY(-2px);
        }
        .btn-exe {
            background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
            color: #ffffff;
            box-shadow: 0 4px 14px rgba(2, 132, 199, 0.4);
        }
        .btn-exe:hover {
            background: linear-gradient(135deg, #0369a1 0%, #075985 100%);
            transform: translateY(-2px);
        }
        .file-info {
            font-size: 13px;
            color: #94a3b8;
            margin-top: 4px;
            font-weight: normal;
        }
        .instructions {
            margin-top: 24px;
            padding-top: 20px;
            border-top: 1px solid #334155;
            text-align: left;
            font-size: 13.5px;
            color: #cbd5e1;
            line-height: 1.6;
        }
        .instructions ol {
            padding-left: 20px;
            margin: 8px 0 0 0;
        }
        .instructions li {
            margin-bottom: 6px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="badge">Korea Game Week 2026 Ready</div>
        <h1>Two Ladles</h1>
        <div class="subtitle">A Vietnam–Korea Kitchen Story (2-Player Co-op)</div>

        <a href="/two-ladles-debug.apk" class="btn btn-apk">
            <span>📱 Download Android APK</span>
            <span class="file-info">(%APK_SIZE% MB)</span>
        </a>

        <a href="/two-ladles.exe" class="btn btn-exe">
            <span>💻 Download Windows Executable</span>
            <span class="file-info">(%EXE_SIZE% MB)</span>
        </a>

        <div class="instructions">
            <strong>Android Phone Installation & Co-op:</strong>
            <ol>
                <li>Tap <strong>Download Android APK</strong> above and tap <em>Install</em>.</li>
                <li>Connect both phones to the same Wi-Fi network.</li>
                <li>Phone 1 taps <strong>HOST SERVER</strong> &rarr; picks Linh &rarr; taps <strong>READY</strong>.</li>
                <li>Phone 2 enters Phone 1's IP &rarr; taps <strong>JOIN LAN</strong> &rarr; picks Jiwon &rarr; taps <strong>READY</strong>.</li>
                <li>Phone 1 taps <strong>START SHIFT</strong> to start cooking!</li>
            </ol>
        </div>
    </div>
</body>
</html>
"""

class DownloadHandler(http.server.BaseHTTPRequestHandler):
    def do_HEAD(self):
        self.handle_request(send_body=False)

    def do_GET(self):
        self.handle_request(send_body=True)

    def handle_request(self, send_body=True):
        if self.path in ("/", "/index.html"):
            apk_size = os.path.getsize(APK_PATH) // 1048576 if os.path.exists(APK_PATH) else 0
            exe_size = os.path.getsize(EXE_PATH) // 1048576 if os.path.exists(EXE_PATH) else 0
            body = (
                HTML_TEMPLATE
                .replace("%APK_SIZE%", str(apk_size))
                .replace("%EXE_SIZE%", str(exe_size))
                .encode("utf-8")
            )
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            if send_body:
                self.wfile.write(body)

        elif self.path == "/two-ladles-debug.apk":
            self.serve_file(APK_PATH, "application/vnd.android.package-archive", "two-ladles-debug.apk", send_body)

        elif self.path == "/two-ladles.exe":
            self.serve_file(EXE_PATH, "application/octet-stream", "two-ladles.exe", send_body)

        else:
            self.send_error(404, "File Not Found")

    def serve_file(self, file_path, content_type, filename, send_body=True):
        if not os.path.exists(file_path):
            self.send_error(404, f"File {filename} not built yet.")
            return

        size = os.path.getsize(file_path)
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(size))
        self.send_header("Content-Disposition", f'attachment; filename="{filename}"')
        self.end_headers()

        if send_body:
            with open(file_path, "rb") as f:
                while chunk := f.read(65536):
                    self.wfile.write(chunk)

    def log_message(self, format, *args):
        print(f"[{self.log_date_time_string()}] {format % args}")

def main():
    with socketserver.TCPServer(("0.0.0.0", PORT), DownloadHandler) as httpd:
        print(f"Two Ladles download server running at http://0.0.0.0:{PORT}")
        httpd.serve_forever()

if __name__ == "__main__":
    main()
