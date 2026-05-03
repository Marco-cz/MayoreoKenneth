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
   - `MK_TENANT_SLUG` (opcional) — slug de la tienda (`mayoreokenneth`, `ninadesigns`, …); si falta usa `mayoreokenneth`.

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
