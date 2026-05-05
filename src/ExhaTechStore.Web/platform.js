// Yurguen: Panel dueños contra /api/platform/… (JWT rol PlatformOwner).
const apiBaseUrl = (() => {
  const raw = typeof window !== "undefined" ? window.__MK_API_BASE_URL__ : "";
  const s = raw != null ? String(raw).trim() : "";
  if (s.length > 0) {
    return s.replace(/\/+$/, "");
  }
  return "http://localhost:5016";
})();

const TOKEN_KEY = "yurguen_exha_platform_token";
let chartVentasInst = null;
let chartPieInst = null;

const elLogin = document.getElementById("vista-login");
const elDash = document.getElementById("vista-dash");
const loginBtn = document.getElementById("loginBtn");
const loginMsg = document.getElementById("loginMsg");
const btnLogout = document.getElementById("btnLogout");
const btnRefresh = document.getElementById("btnRefresh");
const ownerHello = document.getElementById("ownerHello");
const btnAltaTenant = document.getElementById("btnAltaTenant");
const altaMsg = document.getElementById("altaMsg");
const tenantsTabla = document.getElementById("tenantsTabla");
const tenantsHint = document.getElementById("tenantsHint");
const formAlta = document.getElementById("formAlta");
const tabResumen = document.getElementById("tabResumen");
const tabClientes = document.getElementById("tabClientes");
const panelResumen = document.getElementById("panelResumen");
const panelClientes = document.getElementById("panelClientes");

loginBtn.addEventListener("click", iniciarSesion);
btnLogout.addEventListener("click", cerrarSesion);
btnRefresh.addEventListener("click", () => cargarTodo());
btnAltaTenant.addEventListener("click", altaTenant);
tabResumen?.addEventListener("click", () => mostrarTab("resumen"));
tabClientes?.addEventListener("click", () => mostrarTab("clientes"));

function mostrarTab(nombre) {
  const isResumen = nombre !== "clientes";
  panelResumen?.classList.toggle("hidden", !isResumen);
  panelClientes?.classList.toggle("hidden", isResumen);
  tabResumen?.classList.toggle("dash-tab--active", isResumen);
  tabClientes?.classList.toggle("dash-tab--active", !isResumen);
}

function tokenActual() {
  return sessionStorage.getItem(TOKEN_KEY) || "";
}

function authHeadersJson() {
  const t = tokenActual();
  return {
    "Content-Type": "application/json",
    ...(t ? { Authorization: `Bearer ${t}` } : {})
  };
}

async function iniciarSesion() {
  loginMsg.textContent = "";
  const email = document.getElementById("loginEmail").value.trim();
  const password = document.getElementById("loginPass").value;
  try {
    const res = await fetch(`${apiBaseUrl}/api/platform/auth/iniciar-sesion`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      loginMsg.textContent = data.message || "Credenciales inválidas.";
      return;
    }
    sessionStorage.setItem(TOKEN_KEY, data.token);
    mostrarDashboard(data.fullName || data.email || "Dueño");
    await cargarTodo();
  } catch {
    loginMsg.textContent = "No se alcanzó el API.";
  }
}

function cerrarSesion() {
  sessionStorage.removeItem(TOKEN_KEY);
  elDash.classList.add("hidden");
  elLogin.classList.remove("hidden");
  if (chartVentasInst) {
    chartVentasInst.destroy();
    chartVentasInst = null;
  }
  if (chartPieInst) {
    chartPieInst.destroy();
    chartPieInst = null;
  }
}

function mostrarDashboard(nombre) {
  elLogin.classList.add("hidden");
  elDash.classList.remove("hidden");
  ownerHello.textContent = `Hola ${nombre}`;
}

async function cargarTodo() {
  if (!tokenActual()) return;
  await Promise.all([cargarMetricas(), cargarTenants()]);
}

async function cargarMetricas() {
  const res = await fetch(`${apiBaseUrl}/api/platform/PlatformMetrics/resumen`, {
    headers: authHeadersJson()
  });
  if (res.status === 401) {
    cerrarSesion();
    return;
  }
  const data = await res.json();

  const t = data.totales || {};
  const kpiCards = document.getElementById("kpiCards");
  kpiCards.innerHTML = "";
  [
    ["Órdenes (periodo / MVP)", String(t.ordenesPeriodo ?? "—"), data.modo || ""],
    ["Tenants activos", String(t.tenantsActivos ?? "—"), ""],
    ["Comisión ejemplo %", String(t.comisionEjemploPct ?? ""), ""]
  ].forEach(([tit, val, hint]) => {
    const div = document.createElement("div");
    div.className = "kpi";
    div.innerHTML = `<span>${tit}</span><strong>${val}</strong>${hint ? `<span>${hint}</span>` : ""}`;
    kpiCards.appendChild(div);
  });

  const labels = (data.ventasPorMes || []).map((x) => x.mes);
  const montos = (data.ventasPorMes || []).map((x) => Number(x.montoVentas));
  const comisiones = (data.ventasPorMes || []).map((x) => Number(x.comisionEstimada));

  const ctx = document.getElementById("chartVentas").getContext("2d");
  if (chartVentasInst) chartVentasInst.destroy();
  chartVentasInst = new Chart(ctx, {
    type: "bar",
    data: {
      labels,
      datasets: [
        // Yurguen: Dos barras por mes para ver bruto vs comisión ejemplo.
        { label: "Ventas", data: montos, backgroundColor: "#3155d9" },
        { label: "Comisión ejemplo", data: comisiones, backgroundColor: "#2d8f4e" }
      ]
    },
    options: {
      responsive: true,
      scales: {
        x: { stacked: false },
        y: { stacked: false, beginAtZero: true }
      }
    }
  });

  const pieWrap = document.getElementById("pieWrap");
  const porTenant = data.porTenantSlug;
  if (porTenant && porTenant.length > 0) {
    pieWrap.classList.remove("hidden");
    const pc = document.getElementById("chartTenants").getContext("2d");
    if (chartPieInst) chartPieInst.destroy();
    chartPieInst = new Chart(pc, {
      type: "doughnut",
      data: {
        labels: porTenant.map((p) => p.slug),
        datasets: [{
          data: porTenant.map((p) => Number(p.montoVentas)),
          backgroundColor: ["#3155d9", "#e07b39", "#2d8f4e", "#9b59b6", "#34495e", "#16a085"]
        }]
      },
      options: { responsive: true, plugins: { legend: { position: "bottom" } } }
    });
  } else {
    pieWrap.classList.add("hidden");
  }
}

async function cargarTenants() {
  const res = await fetch(`${apiBaseUrl}/api/platform/PlatformTenants`, {
    headers: authHeadersJson()
  });
  if (res.status === 401) {
    cerrarSesion();
    return;
  }
  const data = await res.json();

  tenantsHint.textContent =
    data.modo === "inMemory"
      ? "Modo desarrollo sin Postgres: alta y activar/desactivar no aplican hasta UseInMemory=false."
      : "Clientes registrados en base de datos.";

  const inMemory = data.modo === "inMemory";
  formAlta?.classList.remove("hidden");
  btnAltaTenant.disabled = false;
  if (inMemory && !altaMsg.textContent) {
    altaMsg.textContent = "Modo demo activo: podés probar el botón, pero la API no guarda clientes hasta UseInMemory=false.";
  }

  const rows = data.tenants || [];
  let html =
    "<table><thead><tr><th>Slug</th><th>Nombre</th><th>Activo</th><th>Mantenimiento</th></tr></thead><tbody>";

  rows.forEach((t) => {
    const estado = t.isActive ? "Sí" : '<span class="badge-off">No</span>';
    const mant =
      data.modo === "inMemory"
        ? "—"
        : `<button type="button" class="tog" data-id="${t.id}" data-act="${t.isActive ? "1" : "0"}">${t.isActive ? "Desactivar" : "Activar"}</button>`;

    html += `<tr><td>${escapeHtml(t.slug)}</td><td>${escapeHtml(t.displayName)}</td><td>${estado}</td><td>${mant}</td></tr>`;
  });

  html += "</tbody></table>";
  tenantsTabla.innerHTML = rows.length === 0 ? "<p>No hay tenants.</p>" : html;

  tenantsTabla.querySelectorAll(".tog").forEach((btn) => {
    btn.addEventListener("click", () => cambiarTenant(btn.dataset.id, btn.dataset.act !== "1"));
  });
}

async function cambiarTenant(id, activoNuevo) {
  altaMsg.textContent = "";
  try {
    const res = await fetch(`${apiBaseUrl}/api/platform/PlatformTenants/${id}/estado`, {
      method: "PATCH",
      headers: authHeadersJson(),
      body: JSON.stringify({ activo: activoNuevo })
    });
    const msg = await res.json().catch(() => ({}));
    if (!res.ok) {
      altaMsg.textContent = msg.message || "No se actualizó.";
      return;
    }
    await cargarTenants();
  } catch {
    altaMsg.textContent = "Error de red.";
  }
}

async function altaTenant() {
  altaMsg.textContent = "";
  const slug = document.getElementById("tenantSlug").value.trim().toLowerCase();
  const displayName = document.getElementById("tenantNombre").value.trim();
  try {
    const res = await fetch(`${apiBaseUrl}/api/platform/PlatformTenants`, {
      method: "POST",
      headers: authHeadersJson(),
      body: JSON.stringify({ slug, displayName })
    });
    const msg = await res.json().catch(() => ({}));
    if (!res.ok) {
      altaMsg.textContent = msg.message || "No se creó el tenant.";
      return;
    }
    document.getElementById("tenantSlug").value = "";
    document.getElementById("tenantNombre").value = "";
    await cargarTenants();
  } catch {
    altaMsg.textContent = "Error de red.";
  }
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

(function init() {
  const t = tokenActual();
  if (t) {
    mostrarDashboard("Dueño");
    cargarTodo();
  }
})();
