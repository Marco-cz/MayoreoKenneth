// Yurguen: Login Admin + config por tienda; slug fijo en cada /admin/{Marca}/ desde data-yurguen-api-slug.
const apiBaseUrl = (() => {
  const raw = typeof window !== "undefined" ? window.__MK_API_BASE_URL__ : "";
  const s = raw != null ? String(raw).trim() : "";
  if (s.length > 0) {
    return s.replace(/\/+$/, "");
  }
  return "http://localhost:5016";
})();

const slugTenantApi = (() => {
  const ds = typeof document !== "undefined" ? document.body?.dataset : undefined;
  const s = ds?.yurguenApiSlug != null ? String(ds.yurguenApiSlug).trim().toLowerCase() : "";
  return s.length > 0 ? s : "mayoreokenneth";
})();

function apiTenant(slug, relPath) {
  const base = `${apiBaseUrl}/t/${encodeURIComponent(slug)}/api`;
  const tail = relPath.startsWith("/") ? relPath : `/${relPath}`;
  return `${base}${tail}`;
}

const loginView = document.getElementById("loginView");
const dashView = document.getElementById("dashView");
const userIn = document.getElementById("emailIn");
const passIn = document.getElementById("passIn");
const btnLogin = document.getElementById("btnLogin");
const loginMsg = document.getElementById("loginMsg");
const btnOut = document.getElementById("btnOut");
const slugBadge = document.getElementById("slugBadge");
const cfgOut = document.getElementById("cfgOut");
const dashMsg = document.getElementById("dashMsg");

let tokenActual = "";
let slugActual = slugTenantApi;

btnLogin.addEventListener("click", async () => {
  loginMsg.textContent = "";
  dashMsg.textContent = "";
  slugActual = slugTenantApi;
  const email = (userIn.value || "").trim();
  const password = passIn.value || "";
  if (!email || !password) {
    loginMsg.textContent = "Completá usuario y contraseña.";
    return;
  }

  btnLogin.disabled = true;
  try {
    const res = await fetch(`${apiBaseUrl}/api/auth/iniciar-sesion`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      loginMsg.textContent = data.message || "No se pudo iniciar sesión.";
      return;
    }
    const rol = String(data.role ?? data.Role ?? "").toLowerCase();
    if (rol !== "admin") {
      loginMsg.textContent = "Se requiere rol Admin para ver la configuración.";
      return;
    }
    tokenActual = data.token ?? data.Token ?? "";
    if (!tokenActual) {
      loginMsg.textContent = "Respuesta de login incompleta (sin token).";
      return;
    }
    const key = `mk_admin_jwt_${slugActual}`;
    sessionStorage.setItem(key, tokenActual);
    await cargarConfig();
    loginView.classList.add("hidden");
    dashView.classList.remove("hidden");
    slugBadge.textContent = slugActual;
  } catch {
    // Yurguen: Mostrar base URL para depurar cuando el API no responde.
    loginMsg.textContent = `No se alcanzó el API (${apiBaseUrl}). Verificá que el backend esté arriba.`;
  } finally {
    btnLogin.disabled = false;
  }
});

btnOut.addEventListener("click", () => {
  tokenActual = "";
  sessionStorage.removeItem(`mk_admin_jwt_${slugTenantApi}`);
  dashView.classList.add("hidden");
  loginView.classList.remove("hidden");
  cfgOut.textContent = "";
});

async function cargarConfig() {
  dashMsg.textContent = "Cargando…";
  try {
    const res = await fetch(apiTenant(slugActual, "tiendaconfig"), {
      headers: { Authorization: `Bearer ${tokenActual}` }
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      dashMsg.textContent = data.message || "No autorizado o tienda inexistente.";
      cfgOut.textContent = "";
      return;
    }
    cfgOut.textContent = JSON.stringify(data, null, 2);
    dashMsg.textContent =
      "Listo. Marca: PATCH /t/{slug}/api/storefront/marca. Texto producto: PATCH /t/{slug}/api/admin/Productos/{id}/texto-catalogo.";
  } catch {
    dashMsg.textContent = "Error de red.";
  }
}

(function restaurarSesion() {
  slugActual = slugTenantApi;
  const t = sessionStorage.getItem(`mk_admin_jwt_${slugTenantApi}`);
  if (t) {
    tokenActual = t;
    void cargarConfig().then(() => {
      loginView.classList.add("hidden");
      dashView.classList.remove("hidden");
      slugBadge.textContent = slugActual;
    });
  }
})();
