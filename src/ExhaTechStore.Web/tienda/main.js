// Yurguen: Producción inyecta URL y slug de tienda vía config.runtime.js (GitHub Actions).
const apiBaseUrl = (() => {
  const raw = typeof window !== "undefined" ? window.__MK_API_BASE_URL__ : "";
  const s = raw != null ? String(raw).trim() : "";
  if (s.length > 0) {
    return s.replace(/\/+$/, "");
  }
  return "https://localhost:7253";
})();
const tenantSlug = (() => {
  const raw = typeof window !== "undefined" ? window.__MK_TENANT_SLUG__ : "";
  const s = raw != null ? String(raw).trim().toLowerCase() : "";
  if (s.length > 0) {
    return s.replace(/^\/+|\/+$/g, "");
  }
  return "mayoreokenneth";
})();
// Yurguen: Endpoints públicos por tenant (catálogo). Checkout/órdenes MVP siguen en /api/... global.
function apiTienda(relPath) {
  const base = `${apiBaseUrl}/t/${encodeURIComponent(tenantSlug)}/api`;
  const tail = relPath.startsWith("/") ? relPath : `/${relPath}`;
  return `${base}${tail}`;
}
const cart = [];

const productGrid = document.getElementById("productGrid");
const cartPanel = document.getElementById("cartPanel");
const cartItems = document.getElementById("cartItems");
const cartTotal = document.getElementById("cartTotal");
const cartCount = document.getElementById("cartCount");
const checkoutMessage = document.getElementById("checkoutMessage");
const cartToggle = document.getElementById("cartToggle");
const checkoutBtn = document.getElementById("checkoutBtn");
const ordersHistory = document.getElementById("ordersHistory");
const soporteCorreo = document.getElementById("soporteCorreo");
const soporteNombre = document.getElementById("soporteNombre");
const soporteOrden = document.getElementById("soporteOrden");
const soporteAsunto = document.getElementById("soporteAsunto");
const soporteDescripcion = document.getElementById("soporteDescripcion");
const soporteEnviarBtn = document.getElementById("soporteEnviarBtn");
const soporteMensaje = document.getElementById("soporteMensaje");

cartToggle.addEventListener("click", () => {
  cartPanel.classList.toggle("hidden");
});

checkoutBtn.addEventListener("click", () => {
  simulateCheckout();
});
soporteEnviarBtn.addEventListener("click", () => enviarReporteSoporte());

async function loadProducts() {
  try {
    const response = await fetch(apiTienda("products"));
    const products = await response.json();
    renderProducts(products);
  } catch {
    productGrid.innerHTML = "<p>No se pudo conectar al API. Verifica que este corriendo.</p>";
  }
}

async function loadOrderHistory() {
  try {
    const response = await fetch(`${apiBaseUrl}/api/orders/mvp-history`);
    if (!response.ok) {
      ordersHistory.innerHTML = "<p>No fue posible cargar historial de ordenes.</p>";
      return;
    }

    const orders = await response.json();
    renderOrderHistory(orders);
  } catch {
    ordersHistory.innerHTML = "<p>No se pudo conectar para cargar historial.</p>";
  }
}

function renderProducts(products) {
  // Yurguen: Un solo reflow al grid (DocumentFragment) para tienda más ágil.
  const frag = document.createDocumentFragment();
  for (const product of products) {
    const article = document.createElement("article");
    article.className = "card";
    const ivaTxt = product.ivaIncluidoEnPrecio ? "IVA incluido" : "";
    article.innerHTML = `
      <h4>${product.name}</h4>
      <small>${product.sku}</small>
      <p class="price">$${Number(product.price).toFixed(2)}${ivaTxt ? ` · <span class="iva-tag">${ivaTxt}</span>` : ""}</p>
      <button class="add-btn">Agregar al carrito</button>
    `;

    article.querySelector(".add-btn").addEventListener("click", () => addToCart(product));
    frag.appendChild(article);
  }
  productGrid.replaceChildren(frag);
}

function addToCart(product) {
  const existingItem = cart.find((item) => item.id === product.id);
  if (existingItem) {
    existingItem.quantity += 1;
  } else {
    cart.push({ ...product, quantity: 1 });
  }

  renderCart();
}

function renderCart() {
  cartItems.innerHTML = "";

  let total = 0;
  let qty = 0;
  for (const item of cart) {
    total += Number(item.price) * item.quantity;
    qty += item.quantity;

    const row = document.createElement("div");
    row.className = "cart-item";
    row.innerHTML = `
      <div>
        <strong>${item.name}</strong>
        <div>Cant: ${item.quantity}</div>
      </div>
      <div>$${(Number(item.price) * item.quantity).toFixed(2)}</div>
    `;
    cartItems.appendChild(row);
  }

  cartTotal.textContent = `$${total.toFixed(2)}`;
  cartCount.textContent = String(qty);
}

function renderOrderHistory(orders) {
  if (!orders || orders.length === 0) {
    ordersHistory.innerHTML = "<p>No hay ordenes simuladas todavia.</p>";
    return;
  }

  ordersHistory.innerHTML = "";
  for (const order of orders) {
    const row = document.createElement("div");
    row.className = "order-row";
    const fac = order.datosFacturacion;
    const ent = order.direccionEntrega;
    const extra =
      fac && ent
        ? `<div class="order-extra">Factura: ${escapeHtml(fac.nombreCompleto)} · Entrega: ${escapeHtml(ent.provincia)}</div>`
        : "";
    row.innerHTML = `
      <div>
        <strong>${order.orderNumber}</strong>
        <div class="status">Estado: ${order.orderStatus}</div>
        ${extra}
      </div>
      <span>$${Number(order.totalAmount).toFixed(2)}</span>
      <div class="actions">
        <button class="small-btn confirm-btn">Confirmar</button>
        <button class="small-btn cancel-btn">Cancelar</button>
      </div>
    `;

    // Yurguen: Acciones MVP para simular ciclo de vida de orden.
    const confirmBtn = row.querySelector(".confirm-btn");
    const cancelBtn = row.querySelector(".cancel-btn");
    confirmBtn.addEventListener("click", async () => {
      await changeOrderStatus(order.orderId, "confirm-mvp");
    });
    cancelBtn.addEventListener("click", async () => {
      await changeOrderStatus(order.orderId, "cancel-mvp");
    });

    if (order.orderStatus !== "PENDING_MVP") {
      confirmBtn.disabled = true;
      cancelBtn.disabled = true;
    }

    ordersHistory.appendChild(row);
  }
}

async function changeOrderStatus(orderId, action) {
  try {
    const response = await fetch(`${apiBaseUrl}/api/orders/${orderId}/${action}`, {
      method: "POST"
    });

    if (!response.ok) {
      checkoutMessage.textContent = "No se pudo actualizar estado de orden.";
      return;
    }

    checkoutMessage.textContent = "Estado de orden actualizado en modo MVP.";
    await loadOrderHistory();
  } catch {
    checkoutMessage.textContent = "Error de conexion al actualizar orden.";
  }
}

// Yurguen: Lectura de formulario para enviar al API (MVP).
function leerDatosFacturacion() {
  return {
    tipoIdentificacion: document.getElementById("factTipoId").value,
    numeroIdentificacion: document.getElementById("factNumeroId").value.trim(),
    nombreCompleto: document.getElementById("factNombre").value.trim(),
    correoElectronico: document.getElementById("factCorreo").value.trim(),
    telefono: document.getElementById("factTelefono").value.trim()
  };
}

// Yurguen: Direccion de entrega independiente de facturacion.
function leerDireccionEntrega() {
  return {
    nombreContacto: document.getElementById("envNombre").value.trim(),
    telefono: document.getElementById("envTelefono").value.trim(),
    provincia: document.getElementById("envProvincia").value.trim(),
    canton: document.getElementById("envCanton").value.trim(),
    distrito: document.getElementById("envDistrito").value.trim() || null,
    direccionExacta: document.getElementById("envDireccion").value.trim(),
    referencias: document.getElementById("envReferencias").value.trim() || null
  };
}

async function simulateCheckout() {
  // Yurguen: Este checkout es MVP y luego se reemplaza por flujo productivo.
  if (cart.length === 0) {
    checkoutMessage.textContent = "Agrega productos antes de simular checkout.";
    return;
  }

  checkoutBtn.disabled = true;
  checkoutMessage.textContent = "Procesando checkout simulado...";

  try {
    const payload = {
      items: cart.map((item) => ({
        productId: item.id,
        quantity: item.quantity,
        unitPrice: Number(item.price)
      })),
      datosFacturacion: leerDatosFacturacion(),
      direccionEntrega: leerDireccionEntrega()
    };

    const response = await fetch(`${apiBaseUrl}/api/checkout/simulate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      let errorMessage = "No se pudo simular checkout. Revisa el API.";
      try {
        const errorBody = await response.json();
        if (errorBody?.message) {
          errorMessage = errorBody.message;
        }
      } catch {
        // Yurguen: Si el API no devuelve JSON, mostramos mensaje por defecto.
      }
      checkoutMessage.textContent = errorMessage;
      return;
    }

    const result = await response.json();
    checkoutMessage.textContent = `Orden ${result.orderNumber} creada en modo MVP.`;
    cart.length = 0;
    renderCart();
    await loadOrderHistory();
  } catch {
    checkoutMessage.textContent = "Error de conexion durante checkout.";
  } finally {
    checkoutBtn.disabled = false;
  }
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// Yurguen: Formulario publico sin login; Admin/Soporte atienden desde API u herramienta interna.
async function enviarReporteSoporte() {
  const body = {
    correo: soporteCorreo.value.trim(),
    nombreContacto: soporteNombre.value.trim(),
    numeroOrden: soporteOrden.value.trim() || null,
    asunto: soporteAsunto.value.trim(),
    descripcion: soporteDescripcion.value.trim()
  };

  soporteEnviarBtn.disabled = true;
  soporteMensaje.textContent = "Enviando...";

  try {
    const response = await fetch(`${apiBaseUrl}/api/soporte/reportar`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    const data = await response.json();
    if (!response.ok) {
      soporteMensaje.textContent = data?.message ?? "No se pudo enviar el mensaje.";
      return;
    }
    soporteMensaje.textContent = data.message ?? "Mensaje registrado.";
    soporteAsunto.value = "";
    soporteDescripcion.value = "";
  } catch {
    soporteMensaje.textContent = "Error de conexión al enviar el mensaje.";
  } finally {
    soporteEnviarBtn.disabled = false;
  }
}

// --- Asistente de datos (Yurguen: guía automática en la misma página, sin IA ni resolución real) ---
const asistenteToggle = document.getElementById("asistenteToggle");
const asistentePanel = document.getElementById("asistentePanel");
const asistenteCerrar = document.getElementById("asistenteCerrar");
const asistenteMensajes = document.getElementById("asistenteMensajes");
const asistenteChips = document.getElementById("asistenteChips");
const asistenteForm = document.getElementById("asistenteForm");
const asistenteInput = document.getElementById("asistenteInput");
const asistenteReiniciar = document.getElementById("asistenteReiniciar");

const ASISTENTE_PASOS = [
  {
    texto:
      "¡Pura vida! Soy el asistente automático de ExhaTech Store. Le voy llenando el formulario del carrito paso a paso. No sustituyo a una persona ni resuelvo reclamos."
  },
  { campo: "factNombre", texto: "¿Cómo debe verse su nombre completo o razón social en la factura?" },
  { especial: "tipo", texto: "Elija el tipo de identificación para la factura:" },
  { campo: "factNumeroId", texto: "Digite el número de identificación." },
  { campo: "factCorreo", texto: "¿Cuál es su correo electrónico para enviarle la factura?" },
  { campo: "factTelefono", texto: "¿Teléfono de contacto para la factura? (mínimo 8 dígitos)" },
  { campo: "envNombre", texto: "¿Quién recibe el pedido? (puede repetir su nombre si es la misma persona)" },
  { campo: "envTelefono", texto: "Teléfono de quien recibe el pedido." },
  { campo: "envProvincia", texto: "Provincia de entrega (ej: San José)." },
  { campo: "envCanton", texto: "Cantón de entrega." },
  {
    campo: "envDistrito",
    texto: "Distrito (opcional). Escriba «no» si no desea indicarlo.",
    opcional: true
  },
  { campo: "envDireccion", texto: "Dirección exacta: calle, número, edificio, apartamento, señas..." },
  {
    campo: "envReferencias",
    texto: "Referencias para el reparto (opcional). Escriba «no» si no hay.",
    opcional: true
  }
];

let asistentePaso = 0;
let asistenteEsperandoTipo = false;

function asistenteScrollAbajo() {
  asistenteMensajes.scrollTop = asistenteMensajes.scrollHeight;
}

function asistenteBurbujaBot(texto) {
  const div = document.createElement("div");
  div.className = "asistente-msg asistente-msg--bot";
  div.textContent = texto;
  asistenteMensajes.appendChild(div);
  asistenteScrollAbajo();
}

function asistenteBurbujaUsuario(texto) {
  const div = document.createElement("div");
  div.className = "asistente-msg asistente-msg--user";
  div.textContent = texto;
  asistenteMensajes.appendChild(div);
  asistenteScrollAbajo();
}

function asistenteMostrarChipsTipo() {
  asistenteEsperandoTipo = true;
  asistenteInput.disabled = true;
  asistenteChips.innerHTML = "";
  asistenteChips.classList.remove("hidden");
  const opciones = [
    { valor: "FISICA", etiqueta: "Persona física" },
    { valor: "JURIDICA", etiqueta: "Persona jurídica" },
    { valor: "DIMEX", etiqueta: "DIMEX" },
    { valor: "NITE", etiqueta: "NITE" }
  ];
  for (const op of opciones) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.textContent = op.etiqueta;
    btn.addEventListener("click", () => {
      document.getElementById("factTipoId").value = op.valor;
      asistenteBurbujaUsuario(op.etiqueta);
      asistenteChips.classList.add("hidden");
      asistenteChips.innerHTML = "";
      asistenteEsperandoTipo = false;
      asistenteInput.disabled = false;
      asistentePaso += 1;
      asistenteMostrarPasoActual();
    });
    asistenteChips.appendChild(btn);
  }
}

function asistenteMostrarPasoActual() {
  if (asistentePaso >= ASISTENTE_PASOS.length) {
    asistenteBurbujaBot(
      "Listo. Revise los campos en el carrito; si todo está correcto puede simular el checkout. Gracias por usar la guía automática."
    );
    asistenteInput.disabled = true;
    return;
  }

  const paso = ASISTENTE_PASOS[asistentePaso];
  asistenteBurbujaBot(paso.texto);
  if (paso.especial === "tipo") {
    asistenteMostrarChipsTipo();
  } else {
    asistenteInput.disabled = false;
    asistenteInput.focus();
  }
}

function asistenteIniciar() {
  asistentePanel.classList.remove("hidden");
  cartPanel.classList.remove("hidden");
  asistenteMensajes.innerHTML = "";
  asistenteChips.classList.add("hidden");
  asistenteChips.innerHTML = "";
  asistentePaso = 0;
  asistenteEsperandoTipo = false;
  asistenteInput.disabled = false;
  asistenteInput.value = "";
  asistenteMostrarPasoActual();
}

function asistenteReiniciarFlujo() {
  asistenteMensajes.innerHTML = "";
  asistenteChips.classList.add("hidden");
  asistenteChips.innerHTML = "";
  asistentePaso = 0;
  asistenteEsperandoTipo = false;
  asistenteInput.disabled = false;
  asistenteInput.value = "";
  asistenteMostrarPasoActual();
}

asistenteToggle.addEventListener("click", () => {
  const oculto = asistentePanel.classList.contains("hidden");
  if (oculto) {
    asistenteIniciar();
  } else {
    asistentePanel.classList.add("hidden");
  }
});

asistenteCerrar.addEventListener("click", () => {
  asistentePanel.classList.add("hidden");
  asistenteChips.classList.add("hidden");
  asistenteChips.innerHTML = "";
  asistenteEsperandoTipo = false;
  asistenteInput.disabled = false;
});

asistenteReiniciar.addEventListener("click", () => asistenteReiniciarFlujo());

asistenteForm.addEventListener("submit", (e) => {
  e.preventDefault();
  if (asistenteEsperandoTipo) {
    return;
  }
  if (asistentePaso >= ASISTENTE_PASOS.length) {
    return;
  }

  const paso = ASISTENTE_PASOS[asistentePaso];
  if (paso.especial === "tipo") {
    return;
  }
  if (!paso.campo) {
    asistenteBurbujaUsuario(asistenteInput.value.trim() || "ok");
    asistentePaso += 1;
    asistenteInput.value = "";
    asistenteMostrarPasoActual();
    return;
  }

  const raw = asistenteInput.value.trim();
  if (!paso.opcional && !raw) {
    return;
  }

  let valor = raw;
  if (paso.opcional && (raw.toLowerCase() === "no" || raw === "-" || raw === "")) {
    valor = "";
  }

  const el = document.getElementById(paso.campo);
  if (el) {
    el.value = valor;
  }

  asistenteBurbujaUsuario(raw || "(omitido)");
  asistenteInput.value = "";
  asistentePaso += 1;
  asistenteMostrarPasoActual();
});

// Yurguen: Nombres de marca en UI (URLs siguen con slug en minúsculas).
(function marcarTituloTienda() {
  const titulos = { mayoreokenneth: "MayoreoKenneth", ninadesigns: "NinaDesigns" };
  const titulo = titulos[tenantSlug];
  if (titulo) {
    const h1 = document.querySelector(".topbar h1");
    if (h1) h1.textContent = titulo;
    document.title = `${titulo} · ExhaTech Store`;
  }
})();

loadProducts();
loadOrderHistory();
