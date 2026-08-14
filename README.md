# Niuro — Loan Application Flow

Take-home test: un formulario de solicitud de préstamo, un rule engine que aprueba/deniega,
persistencia transaccional (PostgreSQL) y un evento en background hacia un servicio externo (mock).

> **Video demo:** *(enlace Loom/Jam/Figma pendiente de grabar)*

---

## Arquitectura en una frase

`Next.js` → `API (.NET 10)` → reglas de negocio → transacción (Customer + Application + outbox)
→ `worker` en background lee el outbox → `mock` externo (HTTP).

Ver [ARCHITECTURE.md](ARCHITECTURE.md) para el detalle.

---

## Repositorio

| Carpeta | Rol | Puerto |
|---|---|---|
| `frontend/` | Next.js 16 (App Router) | `http://localhost:3000` |
| `backend/` | API ASP.NET Core 10 (`Niuro.Api`) | `https://localhost:7290` · `http://localhost:5100` |
| `worker/` | Worker en background (`Niuro.Worker`) | — |
| `mock/` | Servicio externo de prueba (Minimal API) | `https://localhost:7124` · `http://localhost:5200` |
| `core/` | Librería compartida (dominio, app, infra) | — |
| `tests/` | Tests (unit + integración con Testcontainers) | — |

---

## Cómo correrlo todo localmente

### Requisitos
- .NET SDK 10
- Node.js 20+
- PostgreSQL (local o vía Docker)
- Docker (solo para correr los **tests de integración**, que usan Testcontainers)

### 1. Base de datos (PostgreSQL)

Opciones:

**Con Docker (recomendado):**
```bash
docker run --name niuro-postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=niuro -p 5432:5432 -d postgres:16-alpine
```

**Local:** crea una base `niuro` y un usuario con password `postgres`.

El API crea el esquema automáticamente con `Database.Migrate()` al arrancar (incluye el seed de
SSNs en blacklist).

### 2. Configurar la cadena de conexión (user secrets, nunca en git)

El API y el worker leen `ConnectionStrings:Postgres` desde user secrets.

```bash
# backend
dotnet user-secrets init --project backend/Niuro.Api.csproj
dotnet user-secrets set --project backend/Niuro.Api.csproj "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=niuro;Username=postgres;Password=postgres"

# worker
dotnet user-secrets init --project worker/Niuro.Worker.csproj
dotnet user-secrets set --project worker/Niuro.Worker.csproj "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=niuro;Username=postgres;Password=postgres"
```

> Ajusta usuario/password si tu Postgres local difiere.

### 3. Backend (API)
```bash
cd backend
dotnet run --launch-profile https
# → https://localhost:7290
```

### 4. Mock (servicio externo)
```bash
cd mock
dotnet run --launch-profile https
# → https://localhost:7124  (worker lo llama por defecto)
```

### 5. Worker (background)
```bash
cd worker
dotnet run
# lee el outbox cada 5s y envía los eventos al mock en https://localhost:7124
```
Para apuntar el worker a otra URL del mock:
```bash
dotnet run -- "ExternalService:BaseUrl=https://localhost:7124"
```
(o setéalo en user secrets: `dotnet user-secrets set --project worker "ExternalService:BaseUrl" "..."`).

### 6. Frontend
```bash
cd frontend
npm install
npm run dev
# → http://localhost:3000
```
El frontend lee `NEXT_PUBLIC_API_URL` (ver `frontend/.env.example`). Por defecto usa
`http://localhost:5100`; si corres el API con HTTPS, copia el ejemplo y ajusta:
```bash
cp .env.example .env.local   # y edita NEXT_PUBLIC_API_URL=https://localhost:7290
```

> Cambiar `NEXT_PUBLIC_*` requiere reiniciar `npm run dev`.

---

## Cómo correr los tests

```bash
dotnet test tests/Niuro.Tests.csproj
```

- Los tests **unitarios** no requieren nada extra.
- Los tests de **integración** levantan un PostgreSQL con **Testcontainers** (requiere **Docker**).
  Si no tienes Docker corriendo, esos tests fallarán; el resto pasa igual.

---

## Datos de prueba

Para probar los flujos desde el formulario:

| Escenario | Qué tipear |
|---|---|
| **Aprobado** | Cualquier SSN válido (`123-45-6789`), State distinto de `NY`, SSN no en blacklist. |
| **Denegado por NY** | State = `NY`. |
| **Denegado por blacklist** | SSN = `111-11-1111`, `222-22-2222` o `333-33-3333` (los 3 en blacklist). |
| **Returning customer** | Envía **dos veces** el mismo SSN (ej. `123-45-6789`). El segundo submit actualiza el customer/application y emite un evento de tipo `Update` al mock. |

> El SSN se normaliza a formato `###-##-####` automáticamente, por lo que puedes tipear
> `123456789` o `123-45-6789`.

---

## Notas

- **Autenticación**: no requerida (no implementada).
- Ver [ARCHITECTURE.md](ARCHITECTURE.md) para diseño, reglas, transacciones y trade-offs.
