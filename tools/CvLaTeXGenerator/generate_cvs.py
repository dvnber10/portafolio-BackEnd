#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
generate_cvs.py
Genera los CV en LaTeX (moderncv) a partir de cv_data.json (fuente única).

Uso:
    python3 generate_cvs.py [ruta_a_cv_data.json] [carpeta_salida]

- El CV "general" incluye todo.
- Cada perfil (backend-net, ia, analista-datos, cientifico-datos) incluye
  automáticamente las secciones compartidas (datos personales, educación,
  experiencia profesional, idiomas) + su perfil, logros, habilidades, proyectos
  e intereses específicos.
- Si pdflatex está disponible, compila los .tex a PDF; si no, genera solo .tex
  (puedes compilarlos en Overleaf).
"""
import json
import os
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent.parent
SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "src/PortfolioApi.Api/Data/cv_data.json"
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else Path(__file__).resolve().parent / "out"

PROFILES_TO_KEEP = [".tex", ".pdf"]


def tex_escape(text: str) -> str:
    if not text:
        return ""
    escapes = {
        "\\": r"\textbackslash{}",
        "{": r"\{",
        "}": r"\}",
        "$": r"\$",
        "&": r"\&",
        "#": r"\#",
        "%": r"\%",
        "_": r"\_",
        "~": r"\textasciitilde{}",
        "^": r"\textasciicircum{}",
        "·": r"$\cdot$",
    }
    out = []
    for ch in text:
        out.append(escapes.get(ch, ch))
    return "".join(out)


def cventry(period, title, org, place, degree, body):
    head = "\\cventry{" + period + "}{" + title + "}{" + org + "}{" + place + "}{" + tex_escape(degree or "") + "}{\\small\n"
    middle = "\\begin{itemize}[label=$\\cdot$]\n"
    middle += "\n".join("    \\item " + tex_escape(line) for line in body)
    tail = "\n\\end{itemize}}"
    return head + middle + tail + "\n\n"


def section(title):
    return f"\\section{{{tex_escape(title)}}}\n"


def build_document(data, profile) -> str:
    p = data.get("personal", {})
    edu_list = data.get("education", [])
    exp_list = data.get("experience", [])
    langs = data.get("languages", [])
    all_projects = {item["title"]: item for item in data.get("projects", [])}

    is_general = (profile.get("slug") == "general")
    projects = [item for item in data.get("projects", [])
                if is_general or profile.get("slug") in item.get("profiles", [])]

    doc = []
    doc.append("\\documentclass[11pt,a4paper,sans]{moderncv}")
    doc.append("\\moderncvstyle{classic}")
    doc.append("\\moderncvcolor{blue}")
    doc.append("\\usepackage[utf8]{inputenc}")
    doc.append("\\usepackage[scale=0.85]{geometry}")
    doc.append("\\usepackage{enumitem}")
    doc.append("")
    doc.append("% Información personal")
    full_name = tex_escape(p.get("fullName", "")).strip()
    name_parts = full_name.split()
    if len(name_parts) > 2:
        first_name, last_name = " ".join(name_parts[:2]), " ".join(name_parts[2:])
    elif len(name_parts) == 2:
        first_name, last_name = name_parts[0], name_parts[1]
    else:
        first_name, last_name = full_name, ""
    doc.append(f"\\name{{{first_name}}}{{{last_name}}}")
    doc.append(f"\\title{{{tex_escape(profile.get('title', p.get('title','')))}}}")
    loc = tex_escape(p.get("location", ""))
    doc.append(f"\\address{{{loc}}}{{}}")
    doc.append(f"\\phone[mobile]{{{tex_escape(p.get('phone',''))}}}")
    doc.append(f"\\email{{{tex_escape(p.get('email',''))}}}")
    doc.append(f"\\homepage{{{tex_escape(p.get('github',''))}}}")
    doc.append(f"\\social[linkedin]{{{tex_escape(p.get('linkedin',''))}}}")
    portfolio = tex_escape(p.get("portfolio", ""))
    if portfolio:
        doc.append(f"\\extrainfo{{Portafolio: \\url{{{portfolio}}}}}")
    doc.append("")
    doc.append("\\begin{document}")
    doc.append("\\makecvtitle")
    doc.append("")

    # Perfil
    doc.append(section("Perfil Profesional"))
    doc.append(f"\\cvitem{{}}{{\\small {tex_escape(profile.get('summary',''))}}}")
    doc.append("")

    # Logros destacados
    highlights = profile.get("highlights", [])
    if highlights:
        doc.append(section("Logros Destacados"))
        for h in highlights:
            doc.append(f"\\cvitem{{}}{{\\small {tex_escape(h)}}}")
        doc.append("")

    # Educación (compartida)
    doc.append(section("Educación"))
    for edu in edu_list:
        doc.append(cventry(
            tex_escape(edu.get("period", "")),
            tex_escape(edu.get("degree", "")),
            tex_escape(edu.get("institution", "")),
            tex_escape(edu.get("place", "")),
            "",
            [edu.get("detail", "")]))
    doc.append("")

    # Experiencia profesional y proyectos (separados por tipo y por perfil)
    def in_profile(item):
        profs = item.get("profiles") or []
        return not profs or profile.get("slug") in profs

    professional = [x for x in exp_list if x.get("type") != "project" and in_profile(x)]
    personal_projects = [x for x in exp_list if x.get("type") == "project" and in_profile(x)]

    if professional:
        doc.append(section("Experiencia Profesional"))
        for item in professional:
            loc = item.get("location", "")
            place = f"{tex_escape(loc)}"
            doc.append(cventry(
                tex_escape(item.get("period", "")),
                tex_escape(item.get("role", "")),
                tex_escape(item.get("organization", "")),
                place,
                tex_escape(item.get("project", "") or ""),
                [r for r in item.get("responsibilities", []) if r]))
        doc.append("")

    if personal_projects:
        doc.append(section("Experiencia y Proyectos"))
        for item in personal_projects:
            proj = item.get("project") or ""
            link = item.get("link") or ""
            subtitle = ", ".join(x for x in [tex_escape(proj), tex_escape(link)] if x)
            doc.append(cventry(
                tex_escape(item.get("period", "")),
                tex_escape(item.get("role", "")),
                tex_escape(item.get("organization", "")),
                tex_escape(item.get("location", "")),
                subtitle,
                [r for r in item.get("responsibilities", []) if r]))
        doc.append("")

    # Proyectos relevantes (desde cv_data)
    if projects:
        doc.append(section("Proyectos Relevantes"))
        for item in projects:
            body = [item.get("description", "")]
            link = item.get("repoLink") or item.get("link") or ""
            if link:
                body.append(tex_escape(link))
            doc.append(cventry(
                "",
                tex_escape(item.get("title", "")),
                tex_escape(item.get("category", "")),
                "",
                "",
                body))
        doc.append("")

    # Habilidades
    doc.append(section("Habilidades Técnicas"))
    skills = profile.get("skills", [])
    doc.append(f"\\cvitem{{Habilidades}}{{\\small {' • '.join(tex_escape(s) for s in skills)}}}")
    doc.append("")

    # Idiomas (compartido)
    doc.append(section("Idiomas"))
    for lang in langs:
        doc.append(f"\\cvitem{{{tex_escape(lang.get('name',''))}}}"
                   f"{{\\small {tex_escape(lang.get('level',''))}}}")
    doc.append("")

    # Intereses
    doc.append(section("Intereses"))
    doc.append(f"\\cvitem{{}}{{\\small {tex_escape(profile.get('interests', ''))}}}")
    doc.append("")
    doc.append("\\end{document}")
    return "\n".join(doc)


def main():
    data = json.loads(SOURCE.read_text(encoding="utf-8"))
    OUT.mkdir(parents=True, exist_ok=True)

    profiles = data.get("profiles", [])
    if not profiles:
        print("No hay perfiles en cv_data.json")
        sys.exit(1)

    for profile in profiles:
        slug = profile.get("slug", "general")
        tex = build_document(data, profile)
        tex_file = OUT / f"cv_{slug}.tex"
        tex_file.write_text(tex, encoding="utf-8")
        print(f"Generado: {tex_file.name}")

    print(f"\nCarpeta de salida: {OUT}")
    if shutil.which("pdflatex"):
        print("Compilando con pdflatex...")
        env = dict(os.environ, max_print_line="200")
        for tex_file in OUT.glob("cv_*.tex"):
            result = os.system(f"cd {shutil.quote(str(OUT))} && pdflatex -interaction=nonstopmode {shutil.quote(tex_file.name)} >/dev/null 2>&1 && pdflatex -interaction=nonstopmode {shutil.quote(tex_file.name)} >/dev/null 2>&1")
            print(f"  {'OK' if result == 0 else 'ERROR'}: {tex_file.stem}")
    else:
        print("pdflatex no está instalado: se generaron solo los .tex (compílalos en Overleaf).")


if __name__ == "__main__":
    main()