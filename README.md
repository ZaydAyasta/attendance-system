# Sistema de Asistencia

## Descripción

Sistema de asistencia en evolución hacia una arquitectura modular con backend en ASP.NET Core y frontend en Vue.

La aplicación nueva usa:

- ASP.NET Core + EF Core + PostgreSQL en `src/backend/Attendance.Api`
- Vue 3 + TypeScript + Vite en `src/frontend/attendance-web`

El sistema legacy de migración no es el destino de desarrollo ni de migrations de EF Core.

## Arquitectura

- Modular Monolith
- Backend API con módulos:
  - `Work Calendar`
  - `Absences`
  - `Attendance`
  - `Work Assignments`
- Frontend SPA en Vue
- PostgreSQL para desarrollo local

## Stack

- ASP.NET Core 10
- C#
- EF Core 10
- PostgreSQL / Npgsql
- Vue 3
- TypeScript
- Vite
- Pinia
- Axios
- Zod
- PrimeVue 4.x
- xUnit
- Vitest

## Estructura del repositorio

```text
src/
  backend/
    Attendance.Api/
  frontend/
    attendance-web/
docs/
  attendance-evaluation-rules.md
  bruno/attendance-api/
```

## Desarrollo local

### PostgreSQL

Usa una base local para la aplicación nueva, por ejemplo:

```text
Host=localhost;Database=attendance_dev;Username=postgres;Password=<your-local-password>
```

No uses la base legacy como destino de desarrollo ni de migrations.

### Backend

Opciones comunes para configurar `ConnectionStrings:DefaultConnection`:

- User Secrets
- variable de entorno `ConnectionStrings__DefaultConnection`

Ejemplo:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=attendance_dev;Username=postgres;Password=<your-local-password>"
dotnet run --project src/backend/Attendance.Api --launch-profile http
```

URL local esperada:

- API: `http://localhost:5015`
- OpenAPI JSON: `http://localhost:5015/openapi/v1.json`
- Scalar: `http://localhost:5015/docs`

### Frontend

```bash
cd src/frontend/attendance-web
npm install
npm run dev
```

URL local esperada:

- Frontend: `http://localhost:5173`

En desarrollo, Vite proxya `"/api"` hacia `http://localhost:5015`.

## Identity y autorización

La API usa ASP.NET Core Identity con cookies HttpOnly; no usa JWT ni guarda tokens
de sesión en `localStorage`. Los roles internos son `Admin`, `User` e `IT`.
`User` debe estar asociado explícitamente a un empleado; `Admin` e `IT` pueden no
tener asociación laboral. Las rutas personales resuelven el empleado desde la
sesión, nunca desde un `EmployeeId` enviado por el navegador.

En Development, configura cuentas de prueba con User Secrets. Las contraseñas no
deben ir en archivos trackeados:

```bash
dotnet user-secrets set "Identity:SeedUsers:Admin:Username" "<admin>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:Admin:Password" "<strong-password>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:User:Username" "<user>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:User:Password" "<strong-password>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:User:EmployeeId" "<existing-employee-guid>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:IT:Username" "<it>" --project src/backend/Attendance.Api
dotnet user-secrets set "Identity:SeedUsers:IT:Password" "<strong-password>" --project src/backend/Attendance.Api
```

Al iniciar la API en Development se crean únicamente las cuentas configuradas que
no existan. En producción usa secretos externos, HTTPS obligatorio, cookies Secure,
una base Identity propia con mínimo privilegio y no expongas Scalar fuera de Development.
Las políticas backend son la fuente de autorización: Admin gestiona módulos de negocio,
User sólo consume `/api/me/attendance` y `/api/me/absences`, e IT sólo accede a áreas técnicas.

Las operaciones mutantes requieren antiforgery. La SPA obtiene el token en
`GET /api/auth/csrf` y lo envía mediante el header `X-CSRF-TOKEN`; Bruno incluye
requests equivalentes en las carpetas Auth y My Data.

## Attendance Capture

La captura operativa usa checkpoints físicos y QR dinámicos. IT administra puntos
`EntryExit` y `Cafeteria`; cada QR está firmado, expira en 30 segundos por defecto
(`CheckpointQr:TokenLifetimeSeconds`) y sólo identifica el checkpoint, nunca al
empleado. Un User autenticado escanea el QR y la API resuelve sus acciones válidas
con su asociación Employee/Identity y la zona `America/Lima`.

Las marcas creadas por este flujo registran `Source=DynamicQr` y el `CheckpointId`.
La protección anti-replay es en memoria por token y empleado, apropiada para la
instancia única de intranet prevista. Al reiniciar el servidor se invalidan los QR
activos; una instalación con varias instancias requerirá almacenamiento compartido.

## Base de datos y migrations

Aplicar migrations:

```bash
dotnet ef database update --project src/backend/Attendance.Api
```

Crear una nueva migration:

```bash
dotnet ef migrations add <MigrationName> --project src/backend/Attendance.Api
```

## Tests

### Backend

```bash
dotnet build AttendanceSystem.sln
dotnet test AttendanceSystem.sln
```

### Frontend

```bash
cd src/frontend/attendance-web
npm test
npm run build
```

## Documentación de API

OpenAPI:

- `http://localhost:5015/openapi/v1.json`

Scalar:

- `http://localhost:5015/docs`

Bruno:

- colección versionada en `docs/bruno/attendance-api`

Bruno sigue siendo la colección ejecutable y de ejemplos manuales. Scalar es la referencia visual principal del API en Development.

## Documentación adicional

- Reglas de negocio de evaluación diaria: `docs/attendance-evaluation-rules.md`

## Nota sobre legacy

Nueva aplicación:

- ASP.NET Core + Vue
- PostgreSQL local, por ejemplo `attendance_dev`

Sistema legacy:

- Django + PostgreSQL/Supabase
- relevante sólo como referencia o futura migración de datos
