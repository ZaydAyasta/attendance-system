# Migración desde el legacy

`Attendance.LegacyImporter` es una herramienta aislada; no expone endpoints HTTP y nunca se conecta al legacy con el contexto EF de la aplicación.

## Configuración local

Defina las conexiones únicamente en el entorno de la consola, no en archivos trackeados:

```powershell
$env:LEGACY_DATABASE_URL = '<connection string del reader legacy>'
$env:ConnectionStrings__DefaultConnection = '<connection string local de attendance_dev>'
```

La herramienta exige que el origen se autentique como `legacy_migration_reader`, que el destino sea `localhost`/`127.0.0.1`/`::1` y que su base sea exactamente `attendance_dev`. También rechaza origen y destino iguales.

## Discovery seguro

```powershell
dotnet run --project src/tools/Attendance.LegacyImporter -- inspect --excel <archivo-o-carpeta>
```

`inspect` sólo abre el origen. Valida la identidad y ejecuta cada consulta dentro de una transacción `REPEATABLE READ` marcada como `READ ONLY`, que termina en `ROLLBACK`. La salida redacta host, base y usuario.

El discovery de 02/09/2026 confirmó que el reader actual está sujeto a RLS y no ve filas ni una tabla de marcaciones. Antes de usar `dry-run`, el legacy debe exponer una política de `SELECT` o una vista de sólo lectura específicamente aprobada para migración. La herramienta no intenta evadir RLS.

## Fuentes y ejecución

Supabase aporta exclusivamente identidad laboral: `employee_profiles.employee_key`, `employee_profiles.employee_name` y `employees.hire_date`. Las marcaciones provienen exclusivamente de archivos `.xlsx`; el importador no busca ni crea tablas de asistencia en Supabase.

```powershell
dotnet run --project src/tools/Attendance.LegacyImporter -- dry-run --excel <archivo-o-carpeta>
dotnet run --project src/tools/Attendance.LegacyImporter -- import --apply --excel <archivo-o-carpeta>
dotnet run --project src/tools/Attendance.LegacyImporter -- verify --excel <archivo-o-carpeta>
```

El lector recorre carpetas recursivamente, ignora `~$*`, acepta sólo `.xlsx` y rechaza `.xls`. No altera los Excel. Detecta bloques por los nueve encabezados exactos, convierte `dd/MM/yyyy` + `HH:mm` como `America/Lima`, y asocia empleados por nombre exacto normalizado (mayúsculas, acentos y espacios repetidos), sin fuzzy matching. Cualquier dato obligatorio ausente o ambiguo bloquea apply.

Cuando estén disponibles, el orden obligatorio será:

1. `inspect` y revisar `legacy-schema-analysis.md`.
2. `dry-run`, sin conexiones de escritura al destino.
3. `import --apply` únicamente si el dry-run no tiene errores críticos.
4. `verify` y una segunda ejecución para demostrar idempotencia.

El alcance sigue limitado a `Employees` y `AttendanceMarks`; no se migrarán credenciales, perfiles, DNI, fotos, calendario, ausencias ni asignaciones. Apply usa una única transacción PostgreSQL en `attendance_dev`, sin actualizar registros existentes.

## Metadata de idempotencia

La migración aditiva `20260902143000_AddLegacyImportMappings` crea `legacy_import_mappings` sólo en el destino. Guarda sistema de origen, tipo de registro, id legacy inmutable, id destino e instante de importación, con índice único sobre `(source_system, entity_type, legacy_id)`. No se aplica automáticamente y jamás se crea en la fuente legacy.
