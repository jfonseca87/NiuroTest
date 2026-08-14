# Architecture

## Estructura del proyecto

```
core/                      # Librería compartida — clean architecture
  Domain/                  # Reglas de negocio y entidades (sin dependencias externas)
    Rules/                 # Rule engine + reglas de denegación
      IDenialRule.cs       #   contrato de una regla
      StateNyRule.cs       #   regla: estado NY → denegar
      BlacklistedSsnRule.cs#   regla: SSN en blacklist → denegar
      RuleEngine.cs        #   motor que itera las reglas
    Entities/              # Customer, LoanApplication, OutboxEvent, Address, BlacklistedSsn
    Queries/               # Contratos de lectura (ICustomerQuery, IBlacklistedSsnQuery)
  Application/             # Casos de uso y DTOs (orquesta dominio + infra)
    UseCases/SubmitLoanApplication.cs  # caso de uso transaccional
    Validators/            # FluentValidation (UC-07)
    Results/               # Result pattern
  Infrastructure/          # Implementaciones reemplazables (EF Core, HTTP, queries)
    NiuroDbContext.cs      # EF Core / PostgreSQL
    Messaging/MockExternalClient.cs   # cliente HTTP del servicio externo
    Queries/               # Implementaciones EF de las queries
    Migrations/            # Migraciones + seed de SSNs en blacklist

backend/                   # API ASP.NET Core (composition root + minimal APIs)
  Program.cs               # DI y pipeline HTTP (registra el módulo de endpoints en una línea)
  Endpoints/LoanApplicationEndpoints.cs  # endpoints minimal (MapGroup) con handler delgado

worker/                    # Host en background
  Infrastructure/OutboxProcessor.cs  # procesa el outbox (lógica testable)
  Infrastructure/OutboxWorker.cs     # BackgroundService (bucle de polling)

mock/                      # Servicio externo de prueba (Minimal API en memoria)
frontend/                  # Next.js
tests/                     # Unit + integración (Testcontainers)
```

**Dirección de dependencias:** `Endpoints → Application → Domain`; `Domain` no depende de nada.
`Infrastructure` implementa contratos definidos en `Domain`/`Application`, así que la BD, el
cliente HTTP o el messaging son reemplazables sin tocar las capas internas.

---

## Rule engine

- Cada regla implementa `IDenialRule` (`ReasonCode` + `AppliesAsync`).
- `RuleEngine` recibe un `IEnumerable<IDenialRule>` y evalúa en orden: la **primera que aplica**
  devuelve `Result.Failure(reasonCode)`; si ninguna aplica → `Result.Success()`.

### Cómo agregar una regla nueva
1. Crea una clase `XxxRule : IDenialRule` en `core/Domain/Rules/` con su `ReasonCode`.
2. Regístrala en `backend/Program.cs`:
   ```csharp
   builder.Services.AddScoped<IDenialRule, XxxRule>();
   ```
   El DI recolecta todas las implementaciones de `IDenialRule` y las inyecta en `RuleEngine`.
3. **No se toca** ninguna regla existente (Open/Closed) ni el `RuleEngine`.

Se exponen `IRuleEngine` y `ISubmitLoanApplication` como abstracciones para que el handler de
minimal API dependa de contratos (DIP) y sea testeable.

---

## Persistencia y transacción

El caso de uso `SubmitLoanApplication`:

1. Normaliza el SSN.
2. Evalúa el rule engine (si deniega, devuelve `Failure` y **no persiste nada**).
3. Busca el customer por SSN:
   - **Nuevo** → crea `Customer` + `Application` + `OutboxEvent(Create)`.
   - **Recurrente** → actualiza el `Customer`, reutiliza/actualiza la `Application`, agrega
     `OutboxEvent(Update)`. Nunca duplica customer ni application (mismo SSN = un customer).
4. Todo se guarda dentro de una **transacción explícita** (`BeginTransaction`): un solo
   `SaveChanges` + `CommitAsync`. Si cualquier paso falla → `RollbackAsync`.

**Qué pasa si falla:**
- **BD / persistencia**: la transacción hace rollback → no queda customer huérfano, ni
  application sin customer, ni evento publicado. El API responde `500`.
- **Publicación del evento al externo**: la publicación no ocurre en el request HTTP. El evento
  se persiste en la tabla `OutboxEvents` en la **misma transacción** que el customer/application.
  El `worker` lee los eventos `Pending` en background y los envía al mock. Si el mock falla, el
  evento se marca `Failed` y **se reintenta en el siguiente ciclo** (el outbox es durable); la
  solicitud ya se respondió correctamente al usuario.

---

## Evento en background y servicio externo

- El outbox garantiza entrega durable: crear/actualizar datos y registrar el evento es atómico.
- `OutboxWorker` (BackgroundService) hace polling cada **5s**, toma hasta **100** eventos
  `Pending` ordenados por `CreatedAt` y delega en `OutboxProcessor`.
- `OutboxProcessor` decide la operación según el evento:
  - `Create` → `POST /api/customers` (crea en el externo).
  - `Update` → `PUT /api/customers/{ssn}` (actualiza en el externo).
- Respuesta 2xx → evento `Sent`. No-2xx o excepción → evento `Failed` con `Error`; se reintenta
  en el siguiente ciclo (los `Pending` y `Failed` pendientes quedan visibles para reproceso).

### Contrato del mock (elegido)
| Método | Ruta | Uso |
|---|---|---|
| `POST` | `/api/customers` | crear customer (evento `Create`) |
| `PUT` | `/api/customers/{ssn}` | actualizar customer (evento `Update`) |
| `GET` | `/api/customers` | debug/demo |

El payload viaja en **snake_case** (`customer`, `application`, `address`), consistente en todo el
flujo (outbox → worker → mock). El mock es un servicio en memoria que responde `200`; se usa
`https://localhost:7124` por defecto (configurable con `ExternalService:BaseUrl`).

**Reintentos:** se eligió un reintento simple por polling (outbox durable) en lugar de una cola
de mensajes o reintentos con backoff complejo, por simplicidad — un outbox con reintento continuo
cubre la mayoría de fallos transitorios sin añadir infraestructura.

---

## Tests

- **Unitarios** (`tests/RuleEngine`, `tests/Validators`, `tests/Endpoints`, `tests/Domain`):
  reglas, motor, validación, mapeo HTTP del handler de minimal API, normalización de SSN y update de customer.
- **Integración** (`tests/Integration`): `SubmitLoanApplication` y el endpoint HTTP completo
  contra **PostgreSQL real** (Testcontainers). Se comparte un único contenedor entre tests.
- **Worker** (`tests/Worker`): contrato del `MockExternalClient` (stub handler) y
  `OutboxProcessor` contra Postgres real (verifica `Sent`/`Failed` y que solo procesa `Pending`).

---

## Trade-offs y decisiones

- **Un solo `core/` compartido** (en vez de proyectos `Domain/Application/Infrastructure`
  separados): suficiente para el tamaño del problema; la separación lógica en carpetas preserva
  la arquitectura sin fragmentar la solución.
- **Outbox + polling** en vez de una cola de mensajes (RabbitMQ/Kafka): cumple el requisito de
  evento en background con entrega durable, mínimo sobre-ingeniería. El patrón outbox además hace
  la transacción y la publicación consistentes.
- **Sin reintentos con backoff/retry avanzado**: el reintento por polling del outbox es simple y
  suficiente; se documenta en lugar de añadir complejidad.
- **Logging**: Serilog con archivo rolling compartido entre API y worker (propiedad `Service`
  distingue el proceso), sin infraestructura extra (ELK etc.).
- **CORS**: solo se habilita una política permisiva en **Development** (app de prueba); en
  producción no se aplica ninguna política por defecto.
- **Minimal APIs en vez de controllers**: los endpoints viven en `Endpoints/` y el startup los
  registra con una sola línea (`MapLoanApplicationEndpoints`). Se mantiene `AddControllers()`
  únicamente porque `WebApplicationFactory` lo requiere en los tests de integración (no se mapea
  ningún controller). Ruta explícita `api/loan-applications` para que el frontend la consuma con guiones.
- **Errores de validación en camelCase**: las claves se normalizan para que el frontend las mapee
  directamente.
- **Auth**: no implementada (no requerida).
- **Videos/CI/Docker**: no incluidos salvo lo que aporta valor (Testcontainers para tests).
