# Casos de Uso — NiuroTest

Fuente de verdad del comportamiento: `docs/PROJECT.md`. Este documento es el **índice maestro** de casos de uso; cada UC tiene su propio documento en `docs/use-cases/`.

## Actores

- **Solicitante**: usuario sin autenticación que llena el formulario de solicitud de préstamo.
- **Sistema NiuroTest**: el conjunto de piezas propias (API, Worker) que procesan la solicitud.
- **Servicio Externo (Mock)**: servicio fake que recibe los datos por HTTP y responde `200`.

## Estructura de proyectos (backend)

**El worker es un proceso aparte** (decisión de diseño: rendimiento / aislamiento). Requiere **tres proyectos** de backend más una librería compartida:

| Proyecto | Nombre estándar | Responsabilidad |
|---|---|---|
| `core/` | `Niuro.Core` | Librería compartida: dominio, aplicación e infraestructura (DbContext) usada por `backend` y `worker`. |
| `backend/` | `Niuro.Api` | **API** Web: recibe el formulario, corre el rule engine y persiste transaccionalmente. |
| `worker/` | `Niuro.Worker` | **Worker Service**: proceso aparte que lee la tabla `OutboxEvents` y envía al servicio externo en background. |
| `mock/` | `Niuro.Mock` | **Servicio Externo fake** (Minimal API): recibe el payload y responde `200`. |
| `frontend/` | `niuro-frontend` | App Next.js (cliente del formulario y páginas de resultado). |
| `tests/` | `Niuro.Tests` | Tests xUnit (rule engine, endpoint, transaccional). |

> **Estándar de nombres**: cada proyecto se llama `Niuro.{Proyecto}` (`Niuro.Core`, `Niuro.Api`, `Niuro.Worker`, `Niuro.Mock`, `Niuro.Tests`, y el paquete npm `niuro-frontend`).

> **Logging y paquetes**: la solución usa **Central Package Management** (`Directory.Packages.props`, creado en UC-01). `Niuro.Api` y `Niuro.Worker` loguean con **Serilog** al mismo archivo rolling `logs/niuro-backend-YYYYMMDD.log`, diferenciado por la propiedad `Service` del `LogContext`. El mock (servicio de terceros) no loguea a nuestro archivo.

> **Contrato técnico entre proyectos** (detalle completo en cada UC de Fase 1+):
> - `frontend` → `backend` (`Niuro.Api`, `http://localhost:5100`) : `POST /api/loan-applications` → `200` con `{ status: "approved"|"denied", reason?, applicationId? }` (Result Pattern; denegado NO es error HTTP). Payload inválido → `400`/`422` Problem Details.
> - `worker` → `mock` (`Niuro.Mock`, `http://localhost:5200`) : `POST /api/customers` (create) / `PUT /api/customers/{ssn}` (update) → `200`.
> - Mock keyed por **SSN** (clave natural); `GET /api/customers` para demo (UC-15).
> - Razones de denegación: `STATE_NY`, `SSN_BLACKLISTED`. SSN siempre normalizado a guiones `###-##-####` antes de comparar/persistir.

---

## Fase 0 — Preparación del entorno (casos de uso de desarrollo)

> No participan del flujo de negocio: dejan el repositorio listo para implementarlo. Se ejecutan en orden, del más simple al más complejo.

| # | ID | Caso de uso | Archivo |
|---|---|---|---|
| 1º | UC-01 | Crear la solución y el proyecto Servicio Externo `mock/` | [`docs/use-cases/UC-01.md`](use-cases/UC-01.md) |
| 2º | UC-02 | Crear los proyectos `core/` + `backend/` | [`docs/use-cases/UC-02.md`](use-cases/UC-02.md) |
| 3º | UC-03 | Crear el proyecto Worker Service `worker/` (modifica `core/` si aplica) | [`docs/use-cases/UC-03.md`](use-cases/UC-03.md) |
| 4º | UC-04 | Crear el proyecto de tests | [`docs/use-cases/UC-04.md`](use-cases/UC-04.md) |
| 5º | UC-05 | Crear la app Next.js `frontend/` | [`docs/use-cases/UC-05.md`](use-cases/UC-05.md) |
| 6º | UC-06 | Inicializar la base de datos (code-first) | [`docs/use-cases/UC-06.md`](use-cases/UC-06.md) |

---

## Fase 1 — Solicitud y decisión

| ID | Caso de uso | Archivo |
|---|---|---|
| UC-07 | Enviar solicitud de préstamo | [`docs/use-cases/UC-07.md`](use-cases/UC-07.md) |
| UC-08 | Evaluar la solicitud con el rule engine | [`docs/use-cases/UC-08.md`](use-cases/UC-08.md) |

---

## Fase 2 — Denegación

| ID | Caso de uso | Archivo |
|---|---|---|
| UC-09 | Denegar por estado NY | [`docs/use-cases/UC-09.md`](use-cases/UC-09.md) |
| UC-10 | Denegar por SSN en blacklist | [`docs/use-cases/UC-10.md`](use-cases/UC-10.md) |

---

## Fase 3 — Aprobación y persistencia transaccional

| ID | Caso de uso | Archivo |
|---|---|---|
| UC-11 | Aprobar y registrar customer nuevo + application | [`docs/use-cases/UC-11.md`](use-cases/UC-11.md) |
| UC-12 | Actualizar customer que regresa + application | [`docs/use-cases/UC-12.md`](use-cases/UC-12.md) |

---

## Fase 4 — Evento en background al servicio externo

| ID | Caso de uso | Archivo |
|---|---|---|
| UC-13 | Publicar y procesar el evento en background | [`docs/use-cases/UC-13.md`](use-cases/UC-13.md) |
| UC-14 | Recibir y confirmar los datos el Servicio Externo | [`docs/use-cases/UC-14.md`](use-cases/UC-14.md) |
| UC-15 | Consultar registros recibidos por el mock | [`docs/use-cases/UC-15.md`](use-cases/UC-15.md) |