# MPM Task List — Detailed (Copilot‑Ready)
A concrete, end‑to‑end backlog for MetalProjekts Management (MPM). This version is **Copilot‑ready**: every story includes code scaffolding, file paths, and paste‑in prompts to accelerate implementation. Status defaults to `Todo`.

**Priority**: P0 critical, P1 high, P2 medium, P3 low  
**Estimate**: rough person‑days  
**Timezone**: Europe/Riga (auto clock‑out 17:01)

---
## 0) Global Conventions
- **Repos & Branching**: `main` protected. Feature branches `feat/<epic>/<ticket>`, fixes `fix/<ticket>`. Conventional commits.
- **Projects**: `MPM.Api` (ASP.NET), `MPM.App` (Blazor), `MPM.Domain`, `MPM.Infrastructure`, `MPM.Tests`.
- **Coding**: nullable enabled, analyzers warning‑as‑error, async suffix, EF Core fluent config, UTC in DB.
- **UI**: MudBlazor; responsive layouts; DataGrid for lists; Dialogs for create/edit; `IValidator<T>` for forms.
- **Auth**: Entra ID, roles claim in ID token; policies in API and route guards in Blazor.
- **Observability**: App Insights logging + traces. Correlation ID per request.
- **IDs & Units**: lengths in **mm** (int). Prices in currency with 2 decimals.
- **Definition of Done**: code + tests + migration + telemetry + docs; deployed to staging.

**Paste once per PR:**
```md
### PR Checklist
- [ ] Story acceptance criteria met
- [ ] EF migration applied locally
- [ ] Unit/integ tests pass
- [ ] Telemetry added (log scopes, event names)
- [ ] Docs updated (README/Wiki)
```

---
## EPIC FND — Foundation & DevOps
**Goal**: stable repo, environments, CI/CD, observability

### FND-01 Repo & Solution Skeleton — P0 — 1d — _None_
**Subtasks**
- Create solution and projects, enable analyzers, set Directory.Build.props, .editorconfig, .gitignore, .gitattributes
- Add `src/` and `tests/` folder layout
- Wire MudBlazor to `MPM.App`

**CLI**
```bash
mkdir mpm && cd mpm
mkdir -p src tests
dotnet new sln -n MPM
cd src
dotnet new webapi -n MPM.Api
dotnet new blazorwasm -n MPM.App --hosted false
dotnet new classlib -n MPM.Domain
dotnet new classlib -n MPM.Infrastructure
cd ../tests
dotnet new xunit -n MPM.Tests
cd ..
dotnet sln add src/* tests/*
```

**Files**
- `Directory.Build.props` with LangVersion latest, TreatWarningsAsErrors
- `.editorconfig` for C# style
- `src/MPM.App/Program.cs` add MudBlazor services

**Acceptance**
- Builds locally; App renders MudBlazor layout; API returns `/healthz` 200

**Copilot prompt**
```text
Create a Directory.Build.props enabling nullable, warnings as errors, C# latest; add shared PackageReferences for Swashbuckle.AspNetCore and Microsoft.Extensions.Logging.Abstractions.
```

### FND-02 CI/CD Pipelines — P0 — 2d — Dep: FND-01
**Subtasks**
- Build + test on PR, publish artifacts, deploy to staging on main
- Cache NuGet; dotnet version matrix pinned to .NET 9
- Environment variables via Key Vault references

**Acceptance**
- Merge to `main` triggers staging deploy; PR shows test + code coverage badges

**Copilot prompt**
```text
Write a GitHub Actions workflow for .NET 9: restore, build, test with coverage, publish MPM.Api and MPM.App artifacts. On push to main, deploy to Azure Web App (placeholders for names and publish profiles).
```

### FND-03 IaC for Azure — P1 — 3d — Dep: FND-01
**Subtasks**
- Bicep/Terraform for App Service, Azure SQL (server + db), Storage, Key Vault, App Insights
- Managed identity for API; Key Vault secrets for SQL conn

**Acceptance**
- `deploy:infra` stands up from scratch; outputs connection strings and URLs

**Copilot prompt**
```text
Generate Bicep modules for: app service plan, web app, azure sql server + db, key vault, storage account, application insights. Expose outputs for connection strings and endpoints. Include role assignments for system-assigned identity to Key Vault.
```

### FND-04 Secrets & Config — P0 — 1d — Dep: FND-03
**Subtasks**
- Bind config from Key Vault; strongly typed options; environment specific `appsettings.*.json`

**Acceptance**
- No secrets in repo; API bootstraps using managed identity locally and in staging

### FND-05 Telemetry & Dashboards — P1 — 2d — Dep: FND-02
**Subtasks**
- Structured logging with source‑generated LoggerMessage; dependency + request telemetry; custom events for inventory mutations
- App Insights dashboard: RPS, error rate, p95 latency

**Acceptance**
- Live charts show traffic and errors; at least 3 custom events visible

---
## EPIC AUTH — Auth & RBAC
**Goal**: Entra ID auth, roles, policies, audit

### AUTH-01 App Registration — P0 — 1d — Dep: FND-03
**Subtasks**
- Create app registration, reply URLs, expose API scope `api://mpm/access`
- Configure Blazor to use MSAL; add login button and user badge

**Files**
- `MPM.App/Services/AuthProvider.cs`
- `MPM.Api/Startup.Auth.cs`

**Acceptance**
- Sign‑in works in dev; ID token contains `roles` claim

**Copilot prompt**
```text
In a Blazor WebAssembly app, add MSAL authentication with Entra ID. Create a top-bar login/logout and display the user's name and roles. Protect a route `/secure` requiring authentication.
```

### AUTH-02 Role Definitions — P0 — 1d — Dep: AUTH-01
Roles: Admin, Inventory, ShopFloor, ProjectManager, Finance, Viewer

**Subtasks**
- Policy names: `CanManageInventory`, `CanManageProjects`, etc.
- Role UI: show/hide menu items

**Acceptance**
- Route guards block unauthorized; API returns 403 for missing role

### AUTH-03 Policy Enforcement — P0 — 2d — Dep: AUTH-02
**Files**
- `MPM.Api/Security/Policies.cs`
- `MPM.App/Routes/AuthGuards.razor`

**Acceptance**
- E2E: Inventory page is inaccessible to Viewer

### AUTH-04 Audit Middleware — P1 — 2d — Dep: FND-02
**Subtasks**
- Middleware captures user, action, entity, before/after JSON snapshot
- Write to `AuditEntries` table with correlation ID

**Acceptance**
- Audit entries created for CRUD on Sheets and Profiles

**Copilot prompt**
```text
Create ASP.NET middleware that inspects requests to `/api/*` and records user identity, route, verb, entity id, and before/after payload where available. Write a service interface `IAuditWriter` and an EFCore implementation.
```

---
## EPIC DB — Database & EF Core
**Goal**: normalized schema, migrations, seeds

### DB-01 Logical ERD — P0 — 1d — _None_
**Subtasks**
- Draft ERD matching Master Prompt entities; add indices on IDs, ProjectId, InvoiceId, timestamps

**Deliverable**
- `docs/erd/mpm-erd.vsdx` and `docs/erd/mpm-erd.png`

### DB-02 EF Core Models & Config — P0 — 3d — Dep: DB-01
**Subtasks**
- Entities: Suppliers, Invoices, Sheets, Profiles, ProfileRemnants, SteelGrades, Projects, ManufacturingOrders, ProfileUsages, SheetUsages, Quotations, QuoteItems, TimeLogs, Notifications, ProfileTypes
- Fluent configuration, shadow audit columns, query filters for soft delete
- `dotnet ef` tools and `InitialCreate` migration

**CLI**
```bash
dotnet tool install --global dotnet-ef
cd src/MPM.Api
dotnet ef migrations add InitialCreate
```

**Copilot prompt**
```text
Generate EF Core entity classes and configurations for Sheets, Profiles, ProfileRemnants with required FKs and indices. Use int mm lengths. Add concurrency token rowversion. Configure query filter to exclude IsDeleted=true.
```

### DB-03 Seed Data — P1 — 1d — Dep: DB-02
- Seed suppliers, steel grades, profile types, dummy invoices; deterministic seeds for tests

### DB-04 Soft Delete & Audit Columns — P2 — 1d — Dep: DB-02
- Add `IsDeleted`, `CreatedBy/At`, `UpdatedBy/At`; global filters

### DB-05 Blob Metadata Tables — P1 — 0.5d — Dep: DB-02
- `Blobs(Id, Uri, ContentType, Sha256, Size, CreatedAt)` and `MoDrawing(MoId, BlobId, Filename)`

---
## EPIC SUP — Supplier Registry
### SUP-01 CRUD API & UI — P1 — 1.5d — Dep: DB-02
**Endpoints**
- `GET/POST/PUT/DELETE /api/suppliers`

**UI**
- Suppliers list, search by name/VAT; dialog for create/edit

**Acceptance**
- VAT format validation; duplicate check by name+VAT

**Copilot prompt**
```text
Build a MudBlazor DataGrid listing Suppliers with search and paging. Add a Drawer form to create/edit a supplier with validation attributes and server-side validation.
```

### SUP-02 Contact Sub-Entities — P2 — 1d — Dep: SUP-01
- One-to-many contacts with roles; inline grid editor

---
## EPIC INV — Invoice Registry
### INV-01 Invoice CRUD — P0 — 2d — Dep: DB-02, SUP-01
**Endpoints**
- `/api/invoices` with filter by supplier and date range

**UI**
- Invoice list with inline expand to lines

**Acceptance**
- Required fields enforced; currency code ISO 4217

### INV-02 Receive Profiles & Sheets — P0 — 3d — Dep: INV-01
**Subtasks**
- Line types: SheetLine, ProfileLine
- Create inventory rows on save; transactional

**Acceptance**
- Inventory rows exist with correct IDs and quantities

**Copilot prompt**
```text
Implement an API endpoint `POST /api/invoices/{id}/receive` that reads invoice lines and inserts Sheets and Profiles records accordingly within a single transaction. Return created entity ids.
```

### INV-03 ID Generators — P0 — 1d — Dep: INV-02
**Rules**
- SheetId: `Thickness + Seq (+R + index)`
- Profile LotId: `Letter + 1..300` with legacy parser for letter+NNN

**Tests**
- 20 sample ids, collision prevention, parsing legacy ids

### INV-04 Price Per Meter Calc — P1 — 1d — Dep: INV-02
- Compute `PricePerMeter` and `TotalForPosition` for profiles on receive

---
## EPIC SH — Sheets Inventory
### SH-01 CRUD & Views — P0 — 2d — Dep: INV-02
**UI**
- Tabs: Full sheets, Remnants; filters by thickness/size

**Acceptance**
- Grid shows live counts; cannot delete if used

### SH-02 Sheet Remnants — P1 — 2d — Dep: SH-01
- Track generated remnants (length, width) per nest; link to SheetUsage

### SH-03 SignalR Live Updates — P2 — 1d — Dep: SH-01
- Broadcast `SheetChanged` events; client updates row in place

**Copilot prompt**
```text
Create a SignalR hub `InventoryHub` with methods `SheetChanged` and `ProfileChanged`. In Blazor, subscribe and update MudTable rows without reloading.
```

---
## EPIC PRF — Profiles Inventory
### PRF-01 CRUD & Views — P0 — 2d — Dep: INV-02
- Validate LotId pattern; FK to ProfileType and SteelGrade

### PRF-02 Reserved/Available Logic — P1 — 1d — Dep: PRJ-02
- Reserve per project; release when project completion set

### PRF-03 Price & Totals — P2 — 0.5d — Dep: INV-04
- Show Price/m and totals; exportable

---
## EPIC REM — Profile Remnants
### REM-01 Inheritance Model — P1 — 1d — Dep: PRF-01
- Auto-populate type, dimensions, invoice from parent

### REM-02 ID Formation — P1 — 0.5d — Dep: REM-01
- RemnantId: `LotId-<RemnantLengthMm>`; unique per lot

---
## EPIC USG — Usage Logs
### USG-01 Profiles Usage — P0 — 2d — Dep: PRF-01
**Endpoints**
- `POST /api/profiles/{lotId}/use` with pieces used and optional remnants

**Rules**
- Prevent negative stock; audit entry written

**Copilot prompt**
```text
Design a ProfilesUsage service that decrements PiecesLeft atomically and records remnant lengths. Throw a domain exception on insufficient stock. Include unit tests for concurrency.
```

### USG-02 Sheets Usage — P0 — 2d — Dep: SH-01
- Log nests, worker, generated remnants; update remaining area/length

### USG-03 Validation & Rules — P1 — 1d — Dep: USG-01, USG-02
- Cross-check availability; block inconsistent updates

---
## EPIC QR — QR, Labels, Exports
### QR-01 Selection Page — P0 — 1d — Dep: PRF-01 or SH-01
- Multi-select with filters; preserve selection across pages

### QR-02 Export CSV & Excel — P0 — 2d — Dep: QR-01
**Content**
- QR image path, plain IDs and numeric IDs; both CSV and XLSX

**Copilot prompt**
```text
Generate a Blazor page that exports selected records to CSV and Excel. Implement an `IExportService` that writes UTF-8 BOM CSV and an XLSX using ClosedXML with bold headers and auto-fit columns.
```

### QR-03 Tests — P1 — 1d — Dep: QR-02
- Golden-file tests for CSV/XLSX content

---
## EPIC PRJ — Projects
### PRJ-01 CRUD & Lifecycle — P0 — 2d — Dep: DB-02
- Fields: customer, address, timeline, statuses, HDG, packing, collection
- Server-side transition rules

### PRJ-02 Reservation Linkage — P1 — 1d — Dep: PRJ-01, PRF-01
- Reserve profiles to project; enforce on usage

### PRJ-03 ReadyForInvoicing Flow — P1 — 1d — Dep: NTF-02
- On collection, set flag; push to Email table + Teams

### PRJ-04 HDG & Zinc Deadlines — P2 — 1d — Dep: PRJ-01
- Track HDG sent date; warning for overdue zinc deadlines

---
## EPIC MO — Manufacturing Orders
### MO-01 MO CRUD — P0 — 2d — Dep: PRJ-01
- Create MOs linked to projects; statuses and assigned users

### MO-02 Steps/Checklist UI — P1 — 2d — Dep: MO-01
- Laser/material checked, cutter, cutting/drilling/welding hours, packing, collection, zinc deadlines; per-step timestamps and user stamps

### MO-03 Drawings Storage — P1 — 2d — Dep: FND-03, DB-05
- Upload to blob; store metadata; preview PDF/PNG in UI; checksum stored

**Copilot prompt**
```text
Implement a file upload API writing to Azure Blob Storage, returning BlobId and SAS URL. Store metadata in SQL. In Blazor, render PDF previews with a download button and show a missing-drawing warning on the MO card.
```

---
## EPIC NTF — Notifications & Integrations
### NTF-01 Provider Selection — P1 — 1d — _None_
- ADR choosing providers for Email/SMS/Teams; template engine, retry policy

### NTF-02 Pack/Problem/Collection Hooks — P1 — 2d — Dep: PRJ-01, MO-02
- Domain events on MO step changes; handlers send notifications and log to `EmailQueue`

### NTF-03 Template Library — P2 — 1d — Dep: NTF-01
- Parametric templates with localization hooks; variables render correctly

---
## EPIC KSK — Shop-Floor Kiosk Integration
### KSK-01 SQL Tables & API — P0 — 2d — Dep: DB-02
- Endpoints: `POST /api/timelogs/clock-in`, `POST /api/timelogs/clock-out` with StationId

### KSK-02 Power Apps Flow — P1 — 1d — Dep: KSK-01
- Call API on QR scans; simple auth model

### KSK-03 Auto Clock-Out Flow — P0 — 1d — Dep: KSK-01
- Power Automate cloud flow to auto-clock out at 17:01 Europe/Riga

### KSK-04 Overtime Logic — P1 — 0.5d — Dep: KSK-03
- Manual re-clock-in after 17:01 sets `IsOvertime = true`

**Copilot prompt**
```text
Write SQL and EF models for TimeLogs with constraints: a user cannot have overlapping open logs. Implement API actions to close any open log for a user when they clock in to a new one.
```

---
## EPIC RPT — Reporting & Analytics
### RPT-01 Inventory Reports — P1 — 2d — Dep: SH-02, PRF-01
- Full vs remnants; filters; CSV/XLSX export matching UI

### RPT-02 Usage & Cost Rollups — P1 — 2d — Dep: USG-03, INV-04
- Per project usage, per part costs; reconcile to movements

### RPT-03 Labor Time Dashboards — P1 — 2d — Dep: KSK-04
- Hours per project per user; overtime split; chart + table with drill-down

### RPT-04 Exception Reports — P2 — 2d — Dep: MO-03, PRJ-04
- Missing drawings, negative balances, overdue zinc deadlines; daily digest

---
## EPIC QTE — Pricing & Quotation Engine
### QTE-01 Coefficient Store — P1 — 1d — Dep: DB-02
- CRUD for drilling/welding/cutting/PM/laser/bending; versioning

### QTE-02 Process Scale Mapping — P1 — 0.5d — Dep: QTE-01
- Finalize scale multipliers for 2 and 3; single source of truth

### QTE-03 Formula Engine — P1 — 2d — Dep: QTE-01, QTE-02
- Implement formula with optional processes; coating thresholds at 5 kg

### QTE-04 Fixed-Price Components — P2 — 1d — Dep: QTE-03
- CRUD and usage in quotes; mixed calculated + fixed parts

### QTE-05 Nested Components — P2 — 1.5d — Dep: QTE-04
- Assemblies/subassemblies/parts composition; totals roll up recursively

**Copilot prompt**
```text
Create a pricing service with input DTOs for weight, material cost, optional coating cost, and per-kg process rates with process scale. Return line item breakdown and grand total with 2-decimal currency. Include unit tests for <5kg and >=5kg branches.
```

---
## EPIC SRE — Observability & Reliability
### SRE-01 SLIs/SLOs & Alerts — P1 — 1d — Dep: FND-05
- Targets for uptime, error rate, p95; alerts in App Insights

### SRE-02 Health Endpoints — P2 — 0.5d — Dep: FND-02
- Liveness/readiness with DB, storage checks; orchestrator probes pass

---
## EPIC SEC — Security & Compliance
### SEC-01 Data Retention & PII Review — P2 — 1d — Dep: DB-02
- Policy for drawings and logs; purge jobs; document in repo

### SEC-02 Access Reviews — P3 — 0.5d — Dep: AUTH-02
- Quarterly checklist and calendar reminder

---
## EPIC PERF — Performance & Load
### PERF-01 Index & Query Tuning — P2 — 1d — Dep: RPT-02
- Identify top queries; add indices; measure p95

### PERF-02 Load Test Scenarios — P3 — 1d — Dep: FND-02
- Simulate concurrent scans, exports, MO updates; baseline report

---
## EPIC DOC — Documentation & Admin
### DOC-01 Admin Guide — P2 — 1d — Dep: AUTH-03
- Roles, user management, feature flags

### DOC-02 User Guide — P2 — 1.5d — Dep: SH-01, PRF-01
- Inventory usage, QR export, projects; task-based chapters

### DOC-03 Runbooks — P2 — 1d — Dep: FND-05
- Incident, rollback, key rotations; referenced by on‑call

---
## Sprint Plan (target)
- **Sprint 0 (6–8d)**: FND‑01/02/03/04, AUTH‑01, DB‑01/02  
- **Sprint 1 (7–9d)**: SUP‑01, INV‑01/02/03, PRF‑01, SH‑01  
- **Sprint 2 (7–9d)**: USG‑01/02, QR‑01/02, DB‑03, AUTH‑03  
- **Sprint 3 (7–9d)**: PRJ‑01/02, MO‑01/03, NTF‑01/02  
- **Sprint 4 (7–9d)**: KSK‑01/03, RPT‑01/03, PRJ‑03, SH‑02  
- **Sprint 5 (7–9d)**: QTE‑01/02/03, RPT‑02, PRF‑02, MO‑02  
- **Sprint 6 (7–9d)**: QR‑03, RPT‑04, SRE‑01, FND‑05, DOC‑02, SEC‑01

---
## Open Items to Decide
- Email/SMS/Teams providers and rate limits  
- Final process scale multipliers for levels 2 and 3  
- Currency handling and VAT rules

