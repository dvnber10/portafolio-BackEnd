#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sync_api.py
Sincroniza el cv_data.json canónico con la API de Portfolio (endpoints de admin).

Uso:
    python3 sync_api.py push --base https://mi-api.example.com --key MI_CLAVE
    python3 sync_api.py fetch --base https://mi-api.example.com --key MI_CLAVE

- push: sube el cv_data.json local a la API (PUT /api/admin/cv).
- fetch: descarga el JSON actual desde la API y lo guarda (GET /api/admin/cv).
La clave admin se toma de --key, de la env ADMIN_KEY o del archivo .admin_key.
"""
import argparse
import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent
SOURCE = ROOT / "src/PortfolioApi.Api/Data/cv_data.json"
KEY_FILE = Path(__file__).resolve().parent / ".admin_key"


def read_key(arg):
    key = arg or os.environ.get("ADMIN_KEY") or ""
    if not key and KEY_FILE.exists():
        key = KEY_FILE.read_text().strip()
    if not key:
        print("ERROR: falta la clave admin. Usa --key, la env ADMIN_KEY o .admin_key.")
        sys.exit(1)
    return key


def request(base, path, key, method="GET", payload=None):
    url = base.rstrip("/") + path
    headers = {"X-Admin-Key": key, "Accept": "application/json", "User-Agent": "Pfolio-sync/1.0"}
    data = None
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode("utf-8") or "{}")


def main():
    parser = argparse.ArgumentParser(description="Sincroniza cv_data.json con la API.")
    parser.add_argument("action", choices=["push", "fetch"])
    parser.add_argument("--base", required=True, help="URL base de la API, p.ej. https://portfolio-api.railway.app")
    parser.add_argument("--key", default="", help="Clave de administrador (o env ADMIN_KEY / .admin_key).")
    parser.add_argument("--data", default=str(SOURCE), help="Ruta al cv_data.json canónico.")
    args = parser.parse_args()

    key = read_key(args.key)

    status, body = request(args.base, "/api/admin/auth", key, "POST")
    if not body.get("valid"):
        print(f"ERROR: clave inválida (HTTP {status}).")
        sys.exit(1)
    print("Auth correcta.")

    if args.action == "push":
        data_path = Path(args.data)
        if not data_path.exists():
            print(f"ERROR: no existe {data_path}")
            sys.exit(1)
        raw = data_path.read_text(encoding="utf-8")
        json.loads(raw)  # validar antes de subir
        status, result = request(args.base, "/api/admin/cv", key, "PUT", {"json": raw})
        if result.get("ok"):
            print(f"Subido ok (HTTP {status}). Errores: {len(result.get('errors', []))}")
        else:
            print(f"ERROR al subir (HTTP {status}): {result}")
            sys.exit(1)
    else:  # fetch
        status, result = request(args.base, "/api/admin/cv", key, "GET")
        js = result.get("json")
        if not js:
            print(f"ERROR al descargar (HTTP {status}): {result}")
            sys.exit(1)
        data_path = Path(args.data)
        data_path.write_text(json.dumps(json.loads(js), ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Guardado en {data_path.resolve()}")


if __name__ == "__main__":
    main()