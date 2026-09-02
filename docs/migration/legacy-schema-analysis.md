# Análisis del esquema legacy

## Estado

Discovery realizado el 02/09/2026 con `LEGACY_DATABASE_URL`, mediante el usuario efectivo `legacy_migration_reader` y `BEGIN TRANSACTION READ ONLY`. La fuente es distinta del destino local `attendance_dev`; el reader tiene `SELECT` sobre `public.employee_profiles` y no tiene privilegios `INSERT`, `UPDATE` ni `DELETE`.

El reader aún no ve datos migrables: los conteos visibles de `employees` y `employee_profiles` son cero. No se intentó evadir RLS ni usar un rol con privilegios superiores. La política `SELECT` limitada al reader debe habilitarse externamente antes del dry-run real.

## Schema visible

| Tabla | Filas visibles | Observación |
| --- | ---: | --- |
| `areas` | 0 | RLS activo. |
| `employee_profiles` | 0 | RLS activo; FK opcional `employee_id -> employees.id`; PK y únicos sobre `id`, `employee_id` y `employee_key`. |
| `employees` | 0 | RLS activo; PK `id`; `employee_key` tiene índice único. |
| `equipment` | 0 | RLS activo; no está en el alcance. |
| `equipment_accessories` | 0 | RLS activo; no está en el alcance. |
| `incidents` | 0 | RLS activo; no está en el alcance. |

No existen otras tablas no sistémicas visibles para el reader, incluidos schemas fuera de `public`. En particular, no hay tabla visible de asistencia, marcaciones, dispositivos de marcación, vacaciones ni permisos.

### `public.employee_profiles`

Columnas verificadas: `id uuid`, `employee_id uuid nullable`, `employee_key text`, `employee_name text`, `area text`, `dni text nullable`, `hire_date text nullable`, `photo_url text nullable`, `employee_index integer nullable`, `job_title text nullable`, `profile_data jsonb` y `updated_at timestamptz`.

No fue posible evaluar formatos reales de `hire_date`, duplicados, nulabilidad efectiva, unicidad de contenido ni componentes en `profile_data`, porque RLS no devuelve filas al reader.

## Mapping aprobado

Supabase aporta sólo identidad. `employee_profiles.employee_key` es `EmployeeCode`; el primer token de `employee_name` es `FirstName` y el resto `LastName` (un único token bloquea); `employees.hire_date` es obligatorio. Las marcaciones vienen exclusivamente de Excel.

El Excel de referencia tiene bloques detectables por estos encabezados exactos: `Fecha`, `Entrada`, `Inicio Almuerzo`, `Fin Almuerzo`, `Salida`, `Entrada por comisión`, `Salida por comisión`, `Entrada por otros`, `Salida por otros`. Fecha y hora son estrictamente `dd/MM/yyyy` y `HH:mm`. No se documentan nombres ni valores personales. Entrada por comisión/otros se mapea literalmente a `CommissionReturn`/`OtherReturn`; las salidas a `CommissionExit`/`OtherExit`.

El LegacyId de una marca es SHA-256 de versión, employee key, fecha local, hora local y tipo. Duplicados entre archivos se colapsan; un mapping existente se salta y una marca `LegacyFingerprint` igual sin mapping se reconcilia creando sólo el mapping.

## Procedimiento de discovery

El comando `Attendance.LegacyImporter inspect` verificará primero el usuario efectivo `legacy_migration_reader` y consultará `information_schema` en una transacción de sólo lectura. El análisis final incluirá, sin datos personales:

- tablas, columnas, tipos, nulabilidad, claves y relaciones de `public`;
- conteos y rangos temporales agregados;
- constraints e índices relevantes;
- la PK, unicidad y semántica de `employee_profiles`;
- la visibilidad de `employees` y `employee_profiles` bajo la política RLS aprobada.

## Mappings pendientes de evidencia

| Destino | Decisión necesaria antes de aplicar |
| --- | --- |
| `Employee.EmployeeCode` | Confirmar cuál identificador legacy es único y estable. |
| `Employee.FirstName` / `LastName` | Determinar si el nombre legacy tiene componentes seguros; no se dividirá un nombre ambiguo por heurística. |
| `Employee.HireDate` | Validar formatos reales de texto y rechazar valores ambiguos. |
| `AttendanceMark.Type` | Construir una tabla exacta con los valores distintos; cualquier tipo sin mapping bloqueará apply. |
| `AttendanceMark.OccurredAt` | Determinar si el valor es UTC, `timestamptz` o hora Lima antes de convertirlo. |

Todas las marcas migradas deberán tener `Source = LegacyFingerprint` y `CheckpointId = null`; esa regla se aplicará sólo después de identificar las filas legacy reales.

## Bloqueadores reales

1. La política RLS del source aún no expone registros al usuario `legacy_migration_reader`.

La corrección debe ser una política `SELECT` de migración limitada al reader sobre ambas tablas. No se debe desactivar RLS globalmente, elevar el importer ni usar el MCP para evadir esta restricción.

## Riesgos abiertos

- Identidad de empleado ambigua o duplicada.
- Fechas de contratación no parseables sin pérdida semántica.
- Tipos de marcación desconocidos.
- Conversión doble de zona horaria.

Estos riesgos bloquean `import --apply`; no se compensarán con valores por defecto.
