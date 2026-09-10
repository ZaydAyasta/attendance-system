# Publicación para producción

Esta aplicación se publica como una sola unidad: la API sirve el resultado
compilado de la SPA desde `wwwroot`. Así las peticiones `/api` y las cookies de
sesión permanecen en el mismo origen, sin habilitar CORS ni exponer una cookie a
otro subdominio.

## Preparación de configuración

No copies `appsettings.Production.example.json` con contraseñas. Usa ese archivo
únicamente como inventario de claves y guarda los valores reales en el gestor de
secretos o en variables de entorno del servicio:

- `ASPNETCORE_ENVIRONMENT=Production`
- `AllowedHosts=<nombre-DNS-publico>`
- `ConnectionStrings__DefaultConnection=<conexion-del-usuario-de-aplicacion>`
- `DataProtection__KeysDirectory=<carpeta-persistente-protegida>`

El usuario de PostgreSQL debe tener únicamente permisos sobre la base y el
esquema de esta aplicación. La identidad que ejecuta la API debe ser la única
con acceso a `DataProtection__KeysDirectory`. En Windows, la aplicación cifra
estas claves con DPAPI.

Antes del primer arranque, provisiona temporalmente los secretos de bootstrap
del administrador descritos en el README. Verifica el acceso y elimínalos antes
del siguiente reinicio.

## Crear el artefacto

Desde la raíz del repositorio, con .NET SDK 10.0.302 y Node 24:

```powershell
Push-Location src/frontend/attendance-web
npm ci
npm run build
Pop-Location

dotnet restore AttendanceSystem.sln
dotnet publish src/backend/Attendance.Api/Attendance.Api.csproj -c Release --no-restore -o artifacts/attendance-release
```

El comando `dotnet publish` falla si no existe `dist/index.html`; evita publicar
una API sin interfaz. Confirma que el resultado contiene
`artifacts/attendance-release/wwwroot/index.html` y los archivos bajo
`wwwroot/assets`.

## Orden de despliegue

1. Haz un respaldo verificable de PostgreSQL y revisa los duplicados descritos
   en el README antes de aplicar la migration de constraints de Identity.
2. Publica el artefacto en el host Windows configurado con HTTPS, con el mismo
   nombre DNS configurado en `AllowedHosts`.
3. Ejecuta las migrations durante la ventana de mantenimiento:

   ```powershell
   dotnet ef database update --project src/backend/Attendance.Api --configuration Release
   ```

4. Inicia la aplicación y verifica `https://<host>/health/live` y
   `https://<host>/health/ready`.
5. Comprueba inicio y cierre de sesión, un flujo de administrador, un flujo de
   empleado y la navegación directa a una ruta SPA, por ejemplo
   `https://<host>/login`.

Después de configurar el DNS y TLS, ejecuta el smoke test anónimo desde una
máquina que alcance la URL pública:

```powershell
.\scripts\Test-ProductionDeployment.ps1 -BaseUrl https://<host>
```

El script valida HTTPS, la página principal, una ruta SPA directa, ambos health
checks y las respuestas `404` de rutas API y mutantes inexistentes. No inicia
sesión ni requiere contraseñas.

No despliegues detrás de un proxy que termine TLS sin configurar y validar el
reenvío de protocolo y encabezados en ese entorno. De lo contrario,
`UseHttpsRedirection` puede redirigir indebidamente solicitudes ya seguras.
