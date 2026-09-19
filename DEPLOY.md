# Despliegue

Sistema de portafolio en dos partes: **backend .NET 8** (API + PDFs + base de datos SQL Server) y **frontend React** (portafolio web + panel admin).

## Flujo de datos (fuente única)

```
cv_data.json  ──>  API (seed + PUT /api/admin/cv desde /admin o sync_api.py)
     │
     ├──> PDFs por perfil (QuestPDF, en la nube, sin LaTeX)
     └──> Frontend (consume /api/cv; fallback a src/data/cv.json)
```

Rutas de perfil (el link actúa como ID): `/{slug}` con slugs `general`, `backend-net`, `ia`, `analista-datos`, `cientifico-datos`.

---

## 1) Backend (.NET 8 + SQL Server)

### Variables de entorno
| Variable | Descripción |
|---|---|
| `Database__Connection` | Cadena de conexión a SQL Server, p.ej. `Server=...;Database=PortfolioApi;User Id=...;Password=...;TrustServerCertificate=True` |
| `Admin__Key` (o `ADMIN_KEY`) | Clave secreta del panel `/admin`. Usa algo largo y aleatorio. |
| `Cors__Origins` | Orígenes permitidos separados por comas, p.ej. `https://mi-portafolio.vercel.app` (default: `http://localhost:3000,http://localhost:5173`). |
| `ASPNETCORE_URLS` | `http://+:8080` (ya definida en el Dockerfile). |

### Opciones de despliegue
- **Railway / Render / Azure Apps** desde el `Dockerfile` (puerto 8080):
  - Railway: apunta al `Dockerfile`, añade una base de datos SQL Server y las 3 variables.
  - En el arranque, el código ejecuta `db.Database.MigrateAsync()` en `try/catch`, así que la primera migración se aplica sola. Si el arranque falla por la BD, revisa la cadena de conexión y las reglas de firewall.
- Local (dev): `dotnet run --project src/PortfolioApi.Api` con `appsettings.Development.json` (clave de dev `dev-secret-cambiar-en-produccion`, SQL Server local `Trusted_Connection`).

### Probar la API después de desplegar
```bash
curl <URL>/api/portfolio            # resumen
curl <URL>/api/portfolio/backend-net # datos de un perfil
curl <URL>/api/cvs/ia?download=1 -o CV_IA.pdf
curl <URL>/api/stats/visits          # contador de visitas (GET)
curl -X POST <URL>/api/stats/visits  # registra una visita
curl -X POST <URL>/api/admin/auth -H "X-Admin-Key: TU_CLAVE"
```

---

## 2) Frontend (React, `~/Documents/Desarrollo/React/my-potfolio`)

### Build y variables
- Variable de build: `REACT_APP_API_URL=https://tu-api.up.railway.app` (ver `.env.example`).
  - En **desarrollo** (`npm start`) el frontend se conecta solo a `http://localhost:5227`, así que editar desde `/admin` cambia al instante lo que ves.
  - Sin API, el frontend funciona con los datos locales (`src/data/cv.json`), pero sin PDF ni `/admin`.
- Deploy en **Vercel** (o donde quieras): exporta `REACT_APP_API_URL` en el panel de Vercel y sube el repo. Rebuild para que tome la variable.
- Rutas por `/{slug}` requieren `_redirects`/rewrite del SPA (Vercel lo maneja automáticamente para CRA; asegúrate de que haya un archivo de redirect en `public/_redirects`: `/* /index.html 200`).

### Sincronizar datos
- Edita y guarda desde `/admin` (editor JSON + preview de los PDFs), o
- Desde tu máquina: `python3 tools/CvLaTeXGenerator/sync_api.py push --base <URL_API> --key <CLAVE>`.
- Actualiza el fallback local si quieres que el frontend sirva datos nuevos sin API:
  `cp src/PortfolioApi.Api/Data/cv_data.json ~/Documents/Desarrollo/React/my-potfolio/src/data/cv.json`

---

## 3) CVs en LaTeX (uso personal / Overleaf)

`tools/CvLaTeXGenerator/generate_cvs.py` lee el mismo `cv_data.json` y genera los 5 `.tex` (general + 4 cargos), con la información general incluida automáticamente en los específicos. Compila con `pdflatex` si está instalado; si no, abre los `.tex` en Overleaf. Los cambios al CV general se propagan a los demás en todas las salidas (LaTeX, PDFs de la API y portafolio web).