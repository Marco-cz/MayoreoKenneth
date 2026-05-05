# ExhaTechStore — implementación y despliegue (Yurguen)

Mono-repo GitHub (**SaaS multi-tienda** por slug de URL):

| Pieza | Dónde vive | Cómo se publica |
|--------|-------------|-----------------|
| **Tienda web** (`src/ExhaTechStore.Web`) | Repo GitHub | **GitHub Actions → GitHub Pages** (workflow `deploy-web-github-pages.yml`) |
| **API .NET** (`src/ExhaTechStore.Api` + Domain + Infrastructure) | Mismo repo | **Docker** (`Dockerfile` en raíz) → Railway, Fly.io, Render u otro PaaS con Postgres gestionado (Neon, Supabase, etc.) |

---

## 1. Front (Pages)

1. En el repo GitHub: **Settings → Pages → Build and deployment**: **GitHub Actions**.
2. **Settings → Secrets and variables → Actions → Variables** (no hace falta máxima seguridad):

   - `MK_API_BASE_URL` — URL base pública del API, **sin** `/` final. Ejemplo: `https://api-exhatech.up.railway.app`
   - `MK_STORE_SLUGS` (opcional) — nombres de carpeta URL separados por comas (**`MayoreoKenneth,NinaDesigns`**). El slug del API sigue siendo **minúsculas** (`mayoreokenneth`). El workflow copia **`tienda/MayoreoKenneth/`** a cada ruta y genera **`stores.manifest.json`**. También crea **`admin/MayoreoKenneth/config.runtime.js`**, etc., junto al HTML que ya está en repo.

La web llama **`{MK_API_BASE_URL}/t/{slug}/api/products`** y el admin **`.../t/{slug}/api/tiendaconfig`** y **`.../t/{slug}/api/admin/Productos`**. Modo desarrollo sin Postgres: mismo patrón con `Catalog:DevTenants` en `appsettings.Development.json`.

**Inventario por producto (`Product.InventoryMode`):** `SupplierManaged` (proveedor + consulta en vivo), `TenantManaged` (solo `ManualStockQuantity`), `Hybrid` (datos/precios del sync desde proveedor, cantidad vendible = `ManualStockQuantity`).

3. Cada push a `main` que toque la carpeta `src/ExhaTechStore.Web/` dispara el deploy. También podés lanzar manual **Actions → Deploy web → Run workflow**.

4. Tu sitio quedará en `https://<usuario>.github.io/<repo>/` (o dominio propio configurado en Pages).

El workflow **sobrescribe** `_site/config.runtime.js` para que la web pegue al API prod. En local podés usar `src/ExhaTechStore.Web/config.runtime.js` (vacío = fallback a localhost en `main.js`).

---

## 2. Back (Docker + Postgres)

```bash
docker build -t exhatechstore-api .
docker run --rm -p 8080:8080 ^
  -e ConnectionStrings__DefaultConnection="Host=...;Username=...;Password=...;Database=..." ^
  exhatechstore-api
```

Variables típicas en el hosting:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- `Cors__AllowedOrigins__0` = URL exacta del front en Pages **con** esquema, ej. `https://tuusuario.github.io`
  - Para más orígenes: `Cors__AllowedOrigins__1`, etc., o ampliamos el código si hace falta un array desde JSON único.

`ASPNETCORE_ENVIRONMENT` en el contenedor queda **Production** (HTTPS lo termina el edge del PaaS; no forzamos redirect interno incompatible).

Migraciones BD (una vez lista la cadena):

```bash
dotnet ef database update --project src/ExhaTechStore.Infrastructure/ExhaTechStore.Infrastructure.csproj --startup-project src/ExhaTechStore.Api/ExhaTechStore.Api.csproj
```

(O correras esto desde pipeline / máquina con acceso al Postgres.)

---

## 3. Checklist rápido

- [ ] Postgres creado (Neon/Railway/otro).
- [ ] API desplegada y salud GET (ej. Swagger si lo habilitás).
- [ ] Variable `MK_API_BASE_URL` apunta a esa API.
- [ ] CORS en API incluye el origen exacto de Pages.
- [ ] JWT y connection string sólo como secretos/env en el host, no en código.

---

## 4. Pasos pendientes (Yurguen — roadmap multi-tenant)

Objetivo: **dar de alta un tenant solo en BD** y que desde ahí nazcan **URL de tienda + admin**, sin tener que crear carpetas a mano en el front cada vez.

1. **[ ] Alta real solo con BD + API dueños**
   - Confirmar **`Catalog:UseInMemory: false`** en prod con Postgres aplicado (`dotnet ef database update`).
   - `POST /api/platform/PlatformTenants` debe ser el único alta “oficial”; documentar slug (`[a-z0-9-]`) y qué datos mínimos pide (`displayName`, etc.).

2. **[ ] Tienda única dinámica por slug (sin carpeta por tenant en el repo)**
   - Una sola plantilla cargable en **`/tienda/{slug}/`** (mismo HTML/JS para todos los slugs) vía **fallback de Pages**, **routing client-side**, o **build que genere** las rutas desde la lista de tenants (menos flexible).
   - El `main.js` ya puede inferir slug desde pathname; falta que el **sitio estático** no dependa de copiar `mayoreokenneth/` / `ninadesigns/` manualmente.

3. **[ ] Panel `admin/{slug}/` por tienda**
   - Login contra **`POST /api/auth/iniciar-sesion`** (rol **Admin** de ese tenant).
   - Pantallas mínimas: **config** (`TiendaConfigController`), y luego CRUD/marketing según necesidad.

4. **[ ] Tras crear tenant en BD — datos iniciales**
   - Opcional recomendado: transacción o servicio que cree **filas relacionadas** (ej. `StoreSettings`, proveedor default si aplica) para que la tienda sea configurable desde el día 1.

5. **[ ] Coordinar deploy cuando exista nuevo slug**
   - Si seguimos en Pages “estático”: o **workflow** que regenere entradas, o **solo SPA**/`404.html` → que la URL siempre cargue la app y el slug sea solo path.
   - Documentar cómo enlazar **portal dueños → “URLs de esta tienda”** sin exponer el portal en las tiendas públicas.

*(Los ítems de producto siguientes ya acordados: imágenes de producto → branding tienda/logo/pie → integrar consultas en asistente → pasarela real → UX tipo lista detalle tipo Amazon.)*

---

## 5. Las 11 mejoras ExhaTech Store (Yurguen — checklist cerrado en repo)

| # | Mejora | Estado |
|---|--------|--------|
| 1 | Portal dueños en raíz y tiendas bajo `/tienda/...` sin cruzar enlaces públicos | Hecho |
| 2 | Asistente guiado + chips (catálogo / entrega / continuar) + texto hacia **Pagar** | Hecho |
| 3 | Slug por URL `/tienda/{slug}/` + soporte `?slug=` sobre plantilla única | Hecho |
| 4 | Deploy Pages: variable **`MK_STORE_SLUGS`** + plantilla única (sin duplicar HTML a mano por tenant) | Hecho |
| 5 | Paginación + búsqueda catálogo (API + UI) | Hecho |
| 6 | Imágenes en listado y detalle (`imageUrl`) | Hecho |
| 7 | **`GET /t/{slug}/api/storefront/perfil`** — nombre, `logoUrl`, `footerPhone` (+ columnas en `Tenant`) | Hecho |
| 8 | UX ficha: click en tarjeta → diálogo detalle + agregar al carrito | Hecho |
| 9 | Panel **admin** estático en **`/admin/`** (login JWT + ver `tiendaconfig` por slug) MVP | Hecho |
| 10 | Roadmap **§4** (Postgres, alta solo BD, datos iniciales, pasarela doc) | Pendiente operativo (checklist §3 + §4) |
| 11 | Pasarela real + checkout multi-tenant persistente | Pendiente (seguir usando `POST /api/checkout/simulate` MVP) |

**Nota:** migración `YurguenTenantStorefrontBranding` agrega `Tenant.LogoUrl` y `Tenant.FooterPhone`. Corré `dotnet ef database update` cuando pruebes con Postgres.

**Ejemplo público marca:** actualizá en BD los campos del tenant y recargás la tienda; en modo **InMemory** el perfil viene de `Catalog:DevTenants` (sin logo/tel hasta BD).
