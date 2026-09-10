# Runbook de producción: Railway + PostgreSQL

## Arquitectura y límites

Railway publica una sola aplicación `Attendance.Api`: el contenedor ASP.NET
Core sirve la SPA Vue compilada desde `wwwroot`. La interfaz, `/api` y la cookie
de Identity comparten exactamente el mismo origen; no se debe crear un servicio
frontend separado ni habilitar CORS amplio. Railway termina TLS y reenvía el
protocolo al contenedor. La aplicación sólo confía en un salto de proxy y procesa
`X-Forwarded-For` y `X-Forwarded-Proto` antes de HSTS/redirección HTTPS.

`/health/live` comprueba únicamente que el proceso responde y es el healthcheck
recomendado para reiniciar el contenedor. `/health/ready` comprueba PostgreSQL y
debe usarse para disponibilidad, no para provocar restarts por una caída breve
de la base.

## Preparar Railway

1. Crea un proyecto Railway con un servicio PostgreSQL y un servicio conectado
   al repositorio GitHub, rama `master`.
2. En el servicio de aplicación selecciona el `Dockerfile` del repositorio.
   Railway asigna `PORT`; el entrypoint escucha `0.0.0.0:$PORT` (8080 sólo es el
   fallback local de contenedor).
3. Enlaza `ConnectionStrings__DefaultConnection` al connection string privado
   del PostgreSQL de Railway. No uses `DATABASE_URL` salvo que se mapee
   explícitamente a esa variable; la aplicación sólo lee `DefaultConnection`.
4. Configura `ASPNETCORE_ENVIRONMENT=Production` y `AllowedHosts` con el host
   exacto del dominio Railway generado. Si se agrega un dominio propio, añade
   ambos hosts separados por `;`. Nunca uses `*` en Production.
5. Define el healthcheck Railway como `/health/live`, genera el dominio Railway
   y valida HTTPS. Railway gestiona el certificado; no configures certificados
   dentro de Kestrel. Para un dominio propio, configura el DNS indicado por
   Railway y después añádelo a `AllowedHosts`.

Valores que se guardan como secretos/variables, nunca en Git:

| Variable | Uso |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | conexión de usuario PostgreSQL con mínimo privilegio |
| `AllowedHosts` | host Railway y, si aplica, host personalizado |
| `Identity__BootstrapAdmin__Enabled` | sólo `true` durante el bootstrap inicial |
| `Identity__BootstrapAdmin__Username` | administrador inicial temporal |
| `Identity__BootstrapAdmin__Password` | contraseña fuerte temporal |
| `CheckpointQr__TokenLifetimeSeconds` | sólo si se desea cambiar el valor configurado |

No se necesitan claves Data Protection en filesystem: la aplicación persiste las
claves de Data Protection en la tabla PostgreSQL `DataProtectionKeys`, con
`ApplicationName=AttendanceSystem`. Esa tabla es parte
de la migración `PersistDataProtectionKeys`, queda fuera del dominio/auditoría y
permite que las cookies existentes sobrevivan redeploys y reemplazos del
contenedor. El proveedor EF no agrega cifrado de claves en reposo por sí mismo:
el acceso a la base debe limitarse al usuario de la aplicación y a operadores
autorizados, y una copia de la base incluye dichas claves y debe protegerse como
secreto. Si la política corporativa exige cifrado adicional de claves en reposo,
se requiere una solución aprobada de gestión de claves/certificados antes de
producción; no se implementó criptografía propia.

## Migraciones y primer administrador

Nunca se aplican migraciones automáticamente al arrancar la aplicación. En una
ventana de mantenimiento, desde una estación segura con la cadena de producción
configurada sólo para ese proceso:

1. Confirma host y nombre de base: debe ser la base PostgreSQL de producción de
   Railway, nunca `attendance_dev` ni la base legacy.
2. Crea un backup y conserva su ubicación y checksum.
3. Revisa las migraciones pendientes con `dotnet ef migrations list --project
   src/backend/Attendance.Api --configuration Release`.
4. Ejecuta `dotnet ef database update --project src/backend/Attendance.Api
   --configuration Release`.
5. Arranca/despliega y verifica `/health/ready`, incluidos los roles y la tabla
   `DataProtectionKeys`.

Para la primera cuenta, provisiona temporalmente las tres variables
`Identity__BootstrapAdmin__*`, despliega, inicia sesión y elimínalas antes del
siguiente reinicio. No hay contraseña predeterminada ni seed permanente. Si se
necesita importar datos legacy, hazlo después de migrations y bootstrap con el
importador controlado; no subas Excel, dumps ni credenciales legacy al servicio.

## Backups, restore y rollback

Activa y verifica en Railway una política de backups diarios, semanales y
mensuales; no supongas que está habilitada por defecto. Además, semanalmente un
operador debe ejecutar `scripts/Backup-ProductionDatabase.ps1` con una cadena
recibida por variable de entorno y guardar el dump fuera de Railway (Drive,
OneDrive, NAS u otro almacenamiento corporativo aprobado). El script no sube
datos ni imprime contraseñas.

Prueba cada backup en una base temporal/local `attendance_restore_test`, nunca
sobre producción: crea la base, restaura el dump, comprueba schema, empleados,
marcas, usuarios y auditoría, y registra resultado/fecha. Un backup sin prueba
de restore no está validado.

Antes de una migración, el backup es obligatorio. Si una migración falla, no
continúes el deploy. Si ya migró y la app falla, intenta primero redeployar una
versión compatible desde el deployment anterior de Railway. No ejecutes `Down`
ni restores sobre producción a ciegas: usa backup/restore o una migración
correctiva planificada.

## Validación y go-live

Después del deploy ejecuta:

```powershell
.\scripts\Test-ProductionDeployment.ps1 -BaseUrl https://<dominio-railway>
```

El smoke anónimo exige HTTPS, SPA, ruta SPA directa, live/ready y `404` para API,
asset y mutación inexistentes. Luego valida manualmente Admin (usuarios,
empleados, calendario, ausencias, reportes y auditoría), User (login,
remember-me, `/me`, asistencia, ausencias y QR) e IT (checkpoints y QR). En un
teléfono real, comprueba cámara y Entry/LunchStart/LunchEnd/Exit. No se declara
QR listo sin esa prueba.

Prueba también persistencia de sesión: inicia sesión con remember-me, redeploya
o reinicia Railway y vuelve a abrir la aplicación. La sesión debe persistir. No
reinicies PostgreSQL de producción sólo para probar; verifica que readiness falla
y luego se recupera durante una indisponibilidad planificada.

Checklist de salida:

- [ ] CI verde, Docker build correcto y auditorías revisadas.
- [ ] PostgreSQL, variables, migrations y `DataProtectionKeys` verificados.
- [ ] primer Admin creado y secretos bootstrap eliminados.
- [ ] backups Railway activos, dump externo y restore probado.
- [ ] health y smoke correctos; pruebas Admin, User, IT, QR y reportes completas.
- [ ] piloto de jefe Admin, soporte IT y 2–3 usuarios durante 1–2 días aprobado.
- [ ] resto de aproximadamente 30 usuarios creado tras la aprobación.

Los logs deben contener arranque, conectividad, fallos de autenticación sin
contraseñas y excepciones del servidor, pero jamás cadenas de conexión, cookies,
tokens CSRF, QR completos ni stack traces al cliente. Scalar/OpenAPI quedan sólo
en Development. En producción se mantienen HSTS, cookies HttpOnly/Secure/Lax,
validación de SecurityStamp y antiforgery; no se desactiva CSRF para Railway.
