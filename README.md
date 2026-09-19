# Documentación del API PortfolioApi
#
# Backend del portafolio personal (Clean Architecture .NET 8 + SQL Server + QuestPDF).
#
# ## Requisitos
# - .NET 8 SDK (dotnet --version)
# - SQL Server (local o en la nube, p. ej. Azure SQL)
#
# ## Variables de entorno
# | Variable                    | Descripción                                                        |
# |-----------------------------|--------------------------------------------------------------------|
# | Database__Connection        | Cadena de conexión a SQL Server                                    |
# | ConnectionStrings__Default  | Alternativa para la cadena de conexión                             |
# | Admin__Key                  | Clave secreta del panel de administración (X-Admin-Key)            |
# | Cors__Origins               | Orígenes permitidos separados por coma (ej: https://portafolio.com)|
#
# ## Nota
# En producción se recomienda inyectar la clave de administrador usando
# la variable de entorno ADMIN_KEY (la lee Admin:Key automáticamente).
#
# ## Publicar en Render / Railway / Azure
# 1. Crear un SQL Server (Azure SQL o un servicio compatible).
# 2. Publicar la solución (Dockerfile o dotnet publish).
# 3. Configurar en el hosting:
#    - Database__Connection  -> cadena de conexión del SQL Server.
#    - Admin__Key            -> clave secreta (uso exclusivo del dueño).
#    - Cors__Origins         -> dominio del portafolio desplegado.
# 4. En el arranque el API aplica migraciones y siembra los datos por defecto.