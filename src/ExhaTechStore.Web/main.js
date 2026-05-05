// Yurguen: Producción inyecta URL y slug de tienda vía config.runtime.js (GitHub Actions).
const apiBaseUrl = (() => {
  const raw = typeof window !== "undefined" ? window.__MK_API_BASE_URL__ : "";
  const s = raw != null ? String(raw).trim() : "";
  if (s.length > 0) {
    return s.replace(/\/+$/, "");
  }
  return "http://localhost:5016";
})();
// Yurguen: Segmento de tienda en la URL después de /tienda/ (respeta MayoreoKenneth / NinaDesigns).
function yurguenSegmentoTiendaDesdePath() {
  if (typeof window === "undefined" || !window.location.pathname) return "";
  const path = window.location.pathname.replace(/\\/g, "/");
  const needle = "/tienda/";
  const idx = path.toLowerCase().indexOf(needle);
  if (idx < 0) return "";
  const rest = path.slice(idx + needle.length).split("/").filter(Boolean);
  if (rest.length === 0 || rest[0].toLowerCase() === "index.html") return "";
  const seg = rest[0].replace(/\.html?$/i, "");
  const reservados = ["styles.css", "main.js"];
  return reservados.includes(seg.toLowerCase()) ? "" : seg;
}

// Yurguen: ?slug= API en minúsculas (fallback).
function yurguenSlugDesdeQueryApi() {
  try {
    const raw = new URLSearchParams(window.location.search).get("slug");
    const s = raw != null ? String(raw).trim().toLowerCase().replace(/^\/+|\/+$/g, "") : "";
    return s.length > 0 ? s : "";
  } catch {
    return "";
  }
}

function yurguenInferirSlugApi(segmentoVisual) {
  if (!segmentoVisual) return "";
  return segmentoVisual.trim().toLowerCase();
}

// Yurguen: Slug para API (/t/{slug}/…): pref. config CI; también __MK_TENANT_API_SLUG__ local.
const tenantSlug = (() => {
  const cfgApiRaw = typeof window !== "undefined" ? window.__MK_TENANT_API_SLUG__ : "";
  const cfgApi =
    cfgApiRaw != null ? String(cfgApiRaw).trim().toLowerCase().replace(/^\/+|\/+$/g, "") : "";
  if (cfgApi.length > 0) {
    return cfgApi;
  }
  const legacy = typeof window !== "undefined" ? window.__MK_TENANT_SLUG__ : "";
  const leg = legacy != null ? String(legacy).trim().toLowerCase().replace(/^\/+|\/+$/g, "") : "";
  if (leg.length > 0) {
    return leg;
  }
  const pathSeg = yurguenSegmentoTiendaDesdePath();
  const fromPath = yurguenInferirSlugApi(pathSeg);
  if (fromPath.length > 0) {
    return fromPath;
  }
  const querySlug = yurguenSlugDesdeQueryApi();
  if (querySlug.length > 0) return querySlug;
  return "mayoreokenneth";
})();
// Yurguen: Endpoints públicos por tenant (catálogo). Checkout/órdenes MVP siguen en /api/... global.
function apiTienda(relPath) {
  const base = `${apiBaseUrl}/t/${encodeURIComponent(tenantSlug)}/api`;
  const tail = relPath.startsWith("/") ? relPath : `/${relPath}`;
  return `${base}${tail}`;
}
const cart = [];
// Yurguen: Moneda local CR para toda la tienda.
const yurguenCrc = new Intl.NumberFormat("es-CR", {
  style: "currency",
  currency: "CRC",
  maximumFractionDigits: 0
});
function formatCrc(value) {
  return yurguenCrc.format(Number(value) || 0);
}

const productGrid = document.getElementById("productGrid");
const cartPanel = document.getElementById("cartPanel");
const cartItems = document.getElementById("cartItems");
const cartItemsSlide = document.getElementById("cartItemsSlide");
const cartTotal = document.getElementById("cartTotal");
const cartCount = document.getElementById("cartCount");
const cartCountNav = document.getElementById("cartCountNav");
const cartCountDrawer = document.getElementById("cartCountDrawer");
const checkoutMessage = document.getElementById("checkoutMessage");
const cartToggle = document.getElementById("cartToggle");
const mobileMenuBtn = document.getElementById("mobileMenuBtn");
const mobileNavOverlay = document.getElementById("mobileNavOverlay");
const mobileNavDrawer = document.getElementById("mobileNavDrawer");
const mobileNavClose = document.getElementById("mobileNavClose");
const drawerQuickCart = document.getElementById("drawerQuickCart");
const checkoutBtn = document.getElementById("checkoutBtn");
const ordersHistory = document.getElementById("ordersHistory");
const cartEmptyHint = document.getElementById("cartEmptyHint");
const gotoCheckoutBtn = document.getElementById("gotoCheckoutBtn");
const soporteCorreo = document.getElementById("soporteCorreo");
const soporteNombre = document.getElementById("soporteNombre");
const soporteOrden = document.getElementById("soporteOrden");
const soporteAsunto = document.getElementById("soporteAsunto");
const soporteDescripcion = document.getElementById("soporteDescripcion");
const soporteEnviarBtn = document.getElementById("soporteEnviarBtn");
const soporteMensaje = document.getElementById("soporteMensaje");
const asistenteConsultaBloc = document.getElementById("asistenteConsultaBloc");

function yurguenToggleCarritoFlyout() {
  if (!cartPanel) return;
  cartPanel.classList.toggle("hidden");
}

cartToggle?.addEventListener("click", () => {
  yurguenToggleCarritoFlyout();
});
// Yurguen: Menú ☰ = drawer móvil (estilo Amazon); tap fuera cierra.
function yurguenCerrarDrawerMovil() {
  mobileNavOverlay?.classList.add("hidden");
  mobileNavDrawer?.classList.add("hidden");
  mobileNavOverlay?.setAttribute("aria-hidden", "true");
}

function yurguenAbrirDrawerMovil() {
  mobileNavOverlay?.classList.remove("hidden");
  mobileNavDrawer?.classList.remove("hidden");
  mobileNavOverlay?.setAttribute("aria-hidden", "false");
}

mobileMenuBtn?.addEventListener("click", (e) => {
  e.preventDefault();
  e.stopPropagation();
  yurguenAbrirDrawerMovil();
});
mobileNavClose?.addEventListener("click", (e) => {
  e.preventDefault();
  yurguenCerrarDrawerMovil();
});
mobileNavOverlay?.addEventListener("click", () => {
  yurguenCerrarDrawerMovil();
});
drawerQuickCart?.addEventListener("click", (e) => {
  e.preventDefault();
  e.stopPropagation();
  yurguenCerrarDrawerMovil();
  yurguenToggleCarritoFlyout();
});

checkoutBtn?.addEventListener("click", () => {
  simulateCheckout();
});

soporteEnviarBtn?.addEventListener("click", () => enviarReporteSoporte());

// --- Navegación multipágina dentro de la tienda (Estilo tipo marketplace) ---
function yurguenMostrarPanel(vistaNombre) {
  document.querySelectorAll(".view-panel").forEach((el) => {
    const key = el.getAttribute("data-yurguen-panel");
    el.classList.toggle("hidden", key !== vistaNombre);
  });
  document.querySelectorAll(".nav-tab").forEach((btn) => {
    const vn = btn.getAttribute("data-yurguen-view");
    if (!vn) return;
    btn.classList.toggle("nav-tab-active", vn === vistaNombre);
  });
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function yurguenIrCheckoutDesdeCarritoSiHayItems() {
  if (cart.length === 0) {
    checkoutMessage.textContent = "Agregá productos antes de pasar al pago.";
    yurguenMostrarPanel("catalog");
    return;
  }
  checkoutMessage.textContent = "";
  yurguenMostrarPanel("checkout");
}

document.body.addEventListener("click", (ev) => {
  // Yurguen: No pisar clicks del panel asistente (minimizar / cerrar / chips).
  if (ev.target.closest("#asistentePanel, #asistenteToggle")) {
    return;
  }
  const btn = ev.target.closest("[data-yurguen-view]");
  if (!(btn instanceof HTMLElement)) {
    return;
  }
  const v = btn.getAttribute("data-yurguen-view");
  if (!v) {
    return;
  }
  if (btn.closest("#mobileNavDrawer")) {
    yurguenCerrarDrawerMovil();
  }
  if (v === "checkout") {
    yurguenIrCheckoutDesdeCarritoSiHayItems();
    return;
  }
  yurguenMostrarPanel(v);
});

gotoCheckoutBtn?.addEventListener("click", () => {
  yurguenIrCheckoutDesdeCarritoSiHayItems();
});

// --- Catálogo paginado (Yurguen: bloques grandes para miles de SKU sin chorrear la vista) ---
const CATALOG_PAGE_SIZE = 48;
let catalogDebouncer = null;
let catalogProximaPagina = 1;
let catalogHayMas = false;
// Yurguen: Fallback visual para que catálogo nunca quede en blanco tipo marketplace.
const CATALOGO_RECOMENDADOS = [
  { id: "sug-1", name: "Combo teclado + mouse gamer", sku: "SUG-KEYMOUSE", price: 29.9, ivaIncluidoEnPrecio: true, imageUrl: "" },
  { id: "sug-2", name: "Audífonos bluetooth HD", sku: "SUG-AUD-BT", price: 24.5, ivaIncluidoEnPrecio: true, imageUrl: "" },
  { id: "sug-3", name: "Power bank 20,000 mAh", sku: "SUG-PWB-20K", price: 31.0, ivaIncluidoEnPrecio: true, imageUrl: "" },
  { id: "sug-4", name: "Webcam Full HD", sku: "SUG-WEBCAM", price: 18.75, ivaIncluidoEnPrecio: true, imageUrl: "" },
  { id: "sug-5", name: "SSD NVMe 1TB", sku: "SUG-SSD-1TB", price: 69.99, ivaIncluidoEnPrecio: true, imageUrl: "" },
  { id: "sug-6", name: "Monitor 24\" IPS", sku: "SUG-MON-24IPS", price: 129.0, ivaIncluidoEnPrecio: true, imageUrl: "" }
];

const catalogSearch = document.getElementById("catalogSearch");
const catalogMeta = document.getElementById("catalogMeta");
const catalogLoadMore = document.getElementById("catalogLoadMore");

function yurguenNormalizarRespuestaCatalogo(raw) {
  if (Array.isArray(raw)) {
    const n = raw.length;
    return { items: raw, totalCount: n, page: 1, pageSize: n, hasMore: false };
  }
  return {
    items: raw.items ?? [],
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? CATALOG_PAGE_SIZE,
    hasMore: Boolean(raw.hasMore)
  };
}

function catalogMetaText(totalCount, ultimaPagina, pageSize) {
  if (!catalogMeta) return;
  if (totalCount === 0) {
    catalogMeta.textContent = "Sin resultados.";
    return;
  }
  const fin = Math.min(ultimaPagina * pageSize, totalCount);
  catalogMeta.textContent = `Mostrando 1–${fin} de ${totalCount} productos.`;
}

function actualizarBotonMas() {
  if (!catalogLoadMore) return;
  catalogLoadMore.classList.toggle("hidden", !catalogHayMas);
}

// Yurguen: Alternar layout home por secciones vs grid normal.
function yurguenUsarGridCatalogo() {
  if (!productGrid) return;
  productGrid.classList.remove("catalog-home-sections");
  productGrid.classList.add("grid", "grid-modern");
}

function yurguenUsarSeccionesCatalogo() {
  if (!productGrid) return;
  productGrid.classList.add("catalog-home-sections");
  productGrid.classList.remove("grid", "grid-modern");
}

// Yurguen: Etiquetas demo para separar bloques tipo marketplace.
function yurguenCategoriaHerramienta(product) {
  const sku = String(product?.sku ?? "");
  if (sku.includes("-TAL-") || sku.includes("-SIE-") || sku.includes("-ESM-") || sku.includes("-SOL-")) {
    return "Eléctricas";
  }
  if (sku.includes("-MAR-") || sku.includes("-ALI-") || sku.includes("-LLA-") || sku.includes("-SOC-")) {
    return "Manuales";
  }
  if (sku.includes("-CAS-") || sku.includes("-GAF-") || sku.includes("-GUA-") || sku.includes("-BOT-")) {
    return "Seguridad";
  }
  return "Ferretería";
}

function yurguenCrearSeccionCatalogo(titulo, products) {
  const section = document.createElement("section");
  section.className = "catalog-home-section";

  const head = document.createElement("div");
  head.className = "catalog-home-head";
  const h3 = document.createElement("h3");
  h3.className = "catalog-home-title";
  h3.textContent = titulo;
  head.appendChild(h3);
  const nav = document.createElement("div");
  nav.className = "catalog-home-nav";
  const prev = document.createElement("button");
  prev.type = "button";
  prev.className = "catalog-home-arrow";
  prev.setAttribute("aria-label", `Desplazar ${titulo} a la izquierda`);
  prev.textContent = "‹";
  const next = document.createElement("button");
  next.type = "button";
  next.className = "catalog-home-arrow";
  next.setAttribute("aria-label", `Desplazar ${titulo} a la derecha`);
  next.textContent = "›";
  nav.append(prev, next);
  head.appendChild(nav);
  section.appendChild(head);

  const track = document.createElement("div");
  track.className = "catalog-home-track";
  for (const p of products) {
    track.appendChild(crearNodoProducto(p));
  }
  function yurguenSyncArrows() {
    const maxLeft = track.scrollWidth - track.clientWidth - 2;
    const left = track.scrollLeft;
    prev.classList.toggle("hidden", left <= 2);
    next.classList.toggle("hidden", left >= maxLeft);
  }
  prev.addEventListener("click", () => {
    track.scrollBy({ left: -460, behavior: "smooth" });
  });
  next.addEventListener("click", () => {
    track.scrollBy({ left: 460, behavior: "smooth" });
  });
  track.addEventListener("scroll", yurguenSyncArrows, { passive: true });
  window.requestAnimationFrame(yurguenSyncArrows);
  section.appendChild(track);
  return section;
}

function yurguenRenderCatalogoPorSecciones(items) {
  if (!productGrid) return;
  yurguenUsarSeccionesCatalogo();
  productGrid.replaceChildren();
  if (!Array.isArray(items) || items.length === 0) return;

  const masVendidos = [...items].slice(0, 12);
  const promociones = [...items].filter((x) => Number(x.price) <= 12000).slice(0, 12);
  const agrupado = new Map();
  for (const p of items) {
    const cat = yurguenCategoriaHerramienta(p);
    if (!agrupado.has(cat)) agrupado.set(cat, []);
    if (agrupado.get(cat).length < 12) {
      agrupado.get(cat).push(p);
    }
  }

  productGrid.appendChild(yurguenCrearSeccionCatalogo("Más vendidos", masVendidos));
  if (promociones.length > 0) {
    productGrid.appendChild(yurguenCrearSeccionCatalogo("Promociones", promociones));
  }
  for (const [cat, list] of agrupado.entries()) {
    if (list.length > 0) {
      productGrid.appendChild(yurguenCrearSeccionCatalogo(cat, list));
    }
  }
}

async function yurguenTopSellingDesdeApi(limit = 12) {
  try {
    const res = await fetch(`${apiTienda("products/top-selling")}?limit=${limit}&days=30`);
    if (!res.ok) return [];
    const data = await res.json().catch(() => []);
    return Array.isArray(data) ? data : [];
  } catch {
    return [];
  }
}

// Yurguen: Render de sugeridos para estado vacío/error sin dejar pantalla muerta.
function mostrarCatalogoRecomendado(mensaje) {
  if (!productGrid) return;
  yurguenUsarGridCatalogo();
  productGrid.replaceChildren();
  adjuntarProductosAlDom(CATALOGO_RECOMENDADOS);
  if (catalogMeta) {
    catalogMeta.textContent = mensaje;
  }
  catalogHayMas = false;
  actualizarBotonMas();
}

/** Yurguen: Una tarjeta de producto enlazando addToCart con el objeto API */
// Yurguen: Paso 5 — miniatura desde imageUrl del API (camelCase); sin URL = placeholder.
function crearNodoProducto(product) {
  const article = document.createElement("article");
  article.className = "card";
  // Yurguen: Abrir ficha al tocar la tarjeta (no el botón agregar).
  article.dataset.productId = product.id != null ? String(product.id) : "";

  const thumb = document.createElement("div");
  thumb.className = "card-thumb";
  const imgUrl = typeof product.imageUrl === "string" ? product.imageUrl.trim() : "";
  if (imgUrl.length > 0) {
    const img = document.createElement("img");
    img.src = imgUrl;
    img.alt = product.name || "";
    img.loading = "lazy";
    img.decoding = "async";
    img.referrerPolicy = "no-referrer";
    thumb.appendChild(img);
  } else {
    thumb.classList.add("card-thumb--empty");
    const ph = document.createElement("span");
    ph.textContent = "Sin foto";
    thumb.appendChild(ph);
  }
  article.appendChild(thumb);

  const titulo = document.createElement("h4");
  titulo.textContent = product.name ?? "";
  article.appendChild(titulo);

  const codigo = document.createElement("small");
  codigo.textContent = product.sku ?? "";
  article.appendChild(codigo);

  const precio = document.createElement("p");
  precio.className = "price";
  const ivaTxt = product.ivaIncluidoEnPrecio ? "IVA incluido" : "";
  precio.innerHTML = `${formatCrc(product.price)}${ivaTxt ? ` · <span class="iva-tag">${ivaTxt}</span>` : ""}`;

  article.appendChild(precio);

  const btn = document.createElement("button");
  btn.type = "button";
  btn.className = "add-btn";
  btn.textContent = "Agregar al carrito";
  btn.addEventListener("click", () => addToCart(product));
  article.appendChild(btn);

  return article;
}

function adjuntarProductosAlDom(products) {
  const frag = document.createDocumentFragment();
  for (const product of products) {
    frag.appendChild(crearNodoProducto(product));
  }
  productGrid.appendChild(frag);
}

async function obtenerCatalogo(reset) {
  const termino = catalogSearch && catalogSearch.value ? catalogSearch.value.trim() : "";
  const numeroPagina = reset ? 1 : catalogProximaPagina;

  try {
    const qs = new URLSearchParams({
      page: String(numeroPagina),
      pageSize: String(CATALOG_PAGE_SIZE)
    });
    if (termino.length > 0) {
      qs.set("q", termino);
    }
    const response = await fetch(`${apiTienda("products")}?${qs}`);
    const raw = await response.json().catch(() => ({}));
    if (!response.ok) {
      if (reset) {
        mostrarCatalogoRecomendado("Mostrando recomendados mientras vuelve el catálogo.");
      }
      return;
    }

    const data = yurguenNormalizarRespuestaCatalogo(raw);
    catalogHayMas = data.hasMore;
    catalogProximaPagina = data.page + 1;

    if (reset) {
      productGrid.replaceChildren();
    }

    const vacios = data.items.length === 0;
    if (reset && vacios) {
      mostrarCatalogoRecomendado(
        termino.length > 0
          ? "Sin coincidencias. Le dejamos recomendados mientras probás otro término."
          : "Catálogo temporalmente sin datos. Le dejamos recomendados."
      );
    } else if (reset && !vacios && termino.length === 0) {
      const topSelling = await yurguenTopSellingDesdeApi(12);
      if (topSelling.length > 0) {
        const baseItems = [...data.items];
        const idsTop = new Set(topSelling.map((x) => String(x.id)));
        const merged = [...topSelling, ...baseItems.filter((x) => !idsTop.has(String(x.id)))];
        yurguenRenderCatalogoPorSecciones(merged);
      } else {
        yurguenRenderCatalogoPorSecciones(data.items);
      }
      if (catalogMeta) {
        catalogMeta.textContent = "";
      }
      catalogHayMas = false;
      actualizarBotonMas();
      return;
    } else if (!vacios) {
      yurguenUsarGridCatalogo();
      adjuntarProductosAlDom(data.items);
    }

    catalogMetaText(data.totalCount, data.page, data.pageSize);
    actualizarBotonMas();
  } catch {
    if (reset) {
      mostrarCatalogoRecomendado("Sin conexión al API. Mostrando recomendados.");
    }
  }
}

catalogLoadMore?.addEventListener("click", async () => {
  if (!catalogHayMas) return;
  catalogLoadMore.disabled = true;
  try {
    await obtenerCatalogo(false);
  } finally {
    catalogLoadMore.disabled = false;
  }
});

catalogSearch?.addEventListener("input", () => {
  clearTimeout(catalogDebouncer);
  catalogDebouncer = setTimeout(() => {
    obtenerCatalogo(true);
  }, 300);
});

function iniciarCatalogo() {
  catalogProximaPagina = 1;
  obtenerCatalogo(true);
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

function addToCart(product) {
  const existingItem = cart.find((item) => item.id === product.id);
  if (existingItem) {
    existingItem.quantity += 1;
  } else {
    cart.push({ ...product, quantity: 1 });
  }

  renderCart();
}

function yurguenConstruirLineaCarritoDom(item, compacto) {
  const wrap = document.createElement("div");
  wrap.className = compacto ? "cart-row cart-row-mini" : "cart-row cart-row-full";
  const info = document.createElement("div");
  info.innerHTML = `<strong>${escapeHtml(item.name ?? "")}</strong><div class="sub">${escapeHtml(item.sku ?? "")}</div>`;

  const lineaPrecio = document.createElement("div");
  lineaPrecio.className = "cart-line-actions";
  const menos = document.createElement("button");
  menos.type = "button";
  menos.className = "qty-chip";
  menos.textContent = "−";
  menos.addEventListener("click", () => {
    item.quantity -= 1;
    if (item.quantity <= 0) {
      const i = cart.indexOf(item);
      if (i >= 0) cart.splice(i, 1);
    }
    renderCart();
  });
  const mas = document.createElement("button");
  mas.type = "button";
  mas.className = "qty-chip";
  mas.textContent = "+";
  mas.addEventListener("click", () => {
    item.quantity += 1;
    renderCart();
  });

  const qtySpan = document.createElement("span");
  qtySpan.className = "qty-val";
  qtySpan.textContent = String(item.quantity);

  const precioSpan = document.createElement("strong");
  precioSpan.className = "cart-line-total";
  precioSpan.textContent = formatCrc(Number(item.price) * item.quantity);

  lineaPrecio.append(menos, qtySpan, mas, precioSpan);
  wrap.append(info, lineaPrecio);
  return wrap;
}

function renderCart() {
  const destinos = [cartItems, cartItemsSlide].filter(Boolean);
  for (const d of destinos) {
    d.replaceChildren();
  }

  let total = 0;
  let qty = 0;
  for (const item of cart) {
    total += Number(item.price) * item.quantity;
    qty += item.quantity;
    cartItems?.appendChild(yurguenConstruirLineaCarritoDom(item, false));
    cartItemsSlide?.appendChild(yurguenConstruirLineaCarritoDom(item, true));
  }

  if (cartTotal) cartTotal.textContent = formatCrc(total);
  if (cartCount) cartCount.textContent = String(qty);
  if (cartCountNav) {
    cartCountNav.textContent = String(qty);
  }
  if (cartCountDrawer) {
    cartCountDrawer.textContent = String(qty);
  }
  if (cartEmptyHint) {
    cartEmptyHint.hidden = qty > 0;
  }
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
      <span>${formatCrc(order.totalAmount)}</span>
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
  // Yurguen: Paso Pagar = MVP igual que antes API; después va pasarela real.
  if (cart.length === 0) {
    checkoutMessage.textContent = "Agregue productos antes de pagar.";
    return;
  }

  checkoutBtn.disabled = true;
  checkoutMessage.textContent = "Procesando pedido (MVP, sin pasarela todavía)...";

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
      let errorMessage = "No se pudo completar el pago MVP. Revise el API.";
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
    checkoutMessage.textContent = `Orden ${result.orderNumber} creada. Próximo paso: coordinar entrega cuando la pasarela esté activa.`;
    cart.length = 0;
    renderCart();
    yurguenMostrarPanel("orders");
    await loadOrderHistory();
  } catch {
    checkoutMessage.textContent = "Error de conexión durante el cobro MVP.";
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
  if (!soporteEnviarBtn || !soporteCorreo) {
    return;
  }
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

// --- Asistente (Yurguen: nuevo flujo sólo sugerencias; escribís recién si escalás a consulta/reclamo) ---
const asistenteToggle = document.getElementById("asistenteToggle");
const asistentePanel = document.getElementById("asistentePanel");
const asistenteMinimizar = document.getElementById("asistenteMinimizar");
const asistenteMensajes = document.getElementById("asistenteMensajes");
const asistenteChips = document.getElementById("asistenteChips");
const asistenteReiniciar = document.getElementById("asistenteReiniciar");

function yurguenDeduplicarNodoPorId(id) {
  const nodos = document.querySelectorAll(`#${id}`);
  if (nodos.length <= 1) return;
  for (let i = 1; i < nodos.length; i += 1) {
    nodos[i].remove();
  }
}
yurguenDeduplicarNodoPorId("asistenteToggle");
yurguenDeduplicarNodoPorId("asistentePanel");

// Yurguen: Siempre el mismo ícono en el FAB (?); acceso legible sólo por aria-label / title «Ayuda».
function asistenteSyncFabAyudaEstadoPanel() {
  if (!asistenteToggle || !asistentePanel) return;
  const oculto = asistentePanel.classList.contains("hidden");
  asistenteToggle.textContent = "?";
  asistenteToggle.setAttribute("aria-label", "Ayuda");
  asistenteToggle.setAttribute("title", "Ayuda");
  asistenteToggle.classList.toggle("asistente-fab--idle", oculto);
  asistenteToggle.classList.add("asistente-fab--tiny");
}

function asistenteResetVisualBase() {
  if (asistentePanel) {
    asistentePanel.classList.add("hidden");
    asistentePanel.classList.remove("asistente-panel--compact");
  }
  asistenteSyncFabAyudaEstadoPanel();
  if (asistenteChips) {
    asistenteChips.classList.add("hidden");
    asistenteChips.innerHTML = "";
  }
  if (asistenteConsultaBloc) {
    asistenteConsultaBloc.classList.add("hidden");
  }
}

function asistenteScrollAbajo() {
  if (!asistenteMensajes) return;
  asistenteMensajes.scrollTop = asistenteMensajes.scrollHeight;
}

function asistenteBurbujaBot(texto) {
  if (!asistenteMensajes) return;
  const div = document.createElement("div");
  div.className = "asistente-msg asistente-msg--bot";
  div.textContent = texto;
  asistenteMensajes.appendChild(div);
  asistenteScrollAbajo();
}

function asistenteBurbujaUsuario(texto) {
  if (!asistenteMensajes) return;
  const div = document.createElement("div");
  div.className = "asistente-msg asistente-msg--user";
  div.textContent = texto;
  asistenteMensajes.appendChild(div);
  asistenteScrollAbajo();
}

// Yurguen: cada sugerencia es un botón; evita input libre hasta escalación
function asistenteAgregarChip(etiqueta, fn) {
  if (!asistenteChips) return;
  const btn = document.createElement("button");
  btn.type = "button";
  btn.textContent = etiqueta;
  btn.addEventListener("click", (ev) => {
    ev.preventDefault();
    ev.stopPropagation();
    fn();
  });
  asistenteChips.appendChild(btn);
}

// Yurguen: menú raíz — navegación MVP de la tienda
function asistenteMostrarMenuPrincipal() {
  if (!asistenteChips) return;
  asistenteConsultaBloc?.classList.add("hidden");
  asistenteChips.innerHTML = "";
  asistenteChips.classList.remove("hidden");

  asistenteAgregarChip("Ver catálogo", () => {
    yurguenMostrarPanel("catalog");
    catalogSearch?.focus();
    minimizarPanelAsistente();
    asistenteBurbujaUsuario("Ver catálogo");
  });

  asistenteAgregarChip("Ver carrito", () => {
    yurguenMostrarPanel("cart");
    minimizarPanelAsistente();
    asistenteBurbujaUsuario("Ver carrito");
  });

  asistenteAgregarChip("Ir a pagar", () => {
    if (cart.length === 0) {
      asistenteBurbujaBot("Todavía no tenés nada en el carrito. Primero elegí algo del catálogo.");
      return;
    }
    checkoutMessage.textContent = "";
    yurguenMostrarPanel("checkout");
    setTimeout(() => {
      document.getElementById("checkoutBtn")?.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }, 120);
    minimizarPanelAsistente();
    asistenteBurbujaUsuario("Ir a pagar");
  });

  asistenteAgregarChip("Datos de factura", () => {
    checkoutMessage.textContent = "";
    yurguenMostrarPanel("checkout");
    setTimeout(() => {
      document.getElementById("factTipoId")?.scrollIntoView({ behavior: "smooth", block: "center" });
    }, 150);
    minimizarPanelAsistente();
    asistenteBurbujaUsuario("Ir a datos de factura");
  });

  asistenteAgregarChip("Datos de entrega", () => {
    checkoutMessage.textContent = "";
    yurguenMostrarPanel("checkout");
    setTimeout(() => {
      document.getElementById("envNombre")?.scrollIntoView({ behavior: "smooth", block: "center" });
    }, 150);
    minimizarPanelAsistente();
    asistenteBurbujaUsuario("Ir a datos de entrega");
  });

  asistenteAgregarChip("Preguntas frecuentes", () => {
    asistenteMostrarFaqs();
  });

  asistenteAgregarChip("Consulta o reclamo (escribir)", () => {
    asistenteEscalarConsulta();
  });
}

// Yurguen: FAQs estáticas sólo con chips
function asistenteMostrarFaqs() {
  if (!asistenteChips) return;
  asistenteChips.innerHTML = "";
  asistenteChips.classList.remove("hidden");
  asistenteAgregarChip("← Volver al menú", () => asistenteMostrarMenuPrincipal());

  const faqs = [
    {
      q: "¿Cómo pago?",
      r: "Completá la pantalla «Pagar». La pasarela de cobro se conecta después; ahí mismo van los datos de factura."
    },
    {
      q: "¿Dónde pongo datos de factura?",
      r: "En «Pagar», bloque «Datos para factura». Eso es lo que va al comprobante electrónico."
    },
    {
      q: "¿Puedo rectificar algo?",
      r: "Si ya mandaste algo mal, escribínos con «Consulta o reclamo» y la tienda te guía."
    }
  ];

  for (const f of faqs) {
    asistenteAgregarChip(f.q, () => {
      asistenteBurbujaUsuario(f.q);
      asistenteBurbujaBot(f.r);
    });
  }
}

// Yurguen: acá se desbloquea escribir (formulario de soporte)
function asistenteEscalarConsulta() {
  expandirPanelAsistente();
  asistenteConsultaBloc?.classList.remove("hidden");
  asistenteBurbujaUsuario("Quiero escribir consulta o reclamo");
  asistenteBurbujaBot(
    "Acá ya podés teclear: correo, asunto y mensaje, y después «Enviar consulta». Eso registra el caso para la tienda."
  );

  if (!asistenteChips) return;
  asistenteChips.innerHTML = "";
  asistenteChips.classList.remove("hidden");
  asistenteAgregarChip("← Volver al menú", () => {
    asistenteConsultaBloc?.classList.add("hidden");
    if (soporteMensaje) {
      soporteMensaje.textContent = "";
    }
    asistenteMostrarMenuPrincipal();
  });

  setTimeout(() => {
    soporteCorreo?.focus();
    asistenteConsultaBloc?.scrollIntoView({ behavior: "smooth", block: "nearest" });
  }, 80);
}

function expandirPanelAsistente() {
  if (!asistentePanel) return;
  asistentePanel.classList.remove("asistente-panel--compact");
  asistenteSyncFabAyudaEstadoPanel();
}

function minimizarPanelAsistente() {
  if (!asistentePanel) return;
  asistentePanel.classList.remove("hidden");
  asistentePanel.classList.add("asistente-panel--compact");
  asistenteSyncFabAyudaEstadoPanel();
}

asistentePanel?.addEventListener("click", (ev) => {
  if (!asistentePanel.classList.contains("asistente-panel--compact")) {
    return;
  }
  if (ev.target.closest(".asistente-head-actions")) {
    return;
  }
  if (!ev.target.closest(".asistente-head")) {
    return;
  }
  ev.preventDefault();
  expandirPanelAsistente();
});

function asistenteIniciar() {
  if (!asistentePanel || !asistenteToggle || !asistenteMensajes || !asistenteChips) return;
  asistentePanel.classList.remove("hidden");
  expandirPanelAsistente();
  asistenteMensajes.innerHTML = "";
  asistenteConsultaBloc?.classList.add("hidden");

  if (soporteMensaje) {
    soporteMensaje.textContent = "";
  }
  asistenteBurbujaBot(
    "¡Pura vida! Todo es con botones hasta que vos elijas «Consulta o reclamo»: ahí sí podés escribir el caso."
  );
  asistenteMostrarMenuPrincipal();
}

function asistenteReiniciarFlujo() {
  if (!asistentePanel || !asistenteMensajes || !asistenteChips) return;
  expandirPanelAsistente();
  asistenteMensajes.innerHTML = "";
  asistenteConsultaBloc?.classList.add("hidden");
  if (soporteMensaje) {
    soporteMensaje.textContent = "";
  }
  asistenteBurbujaBot("Menú desde cero:");
  asistenteMostrarMenuPrincipal();
}

asistenteMinimizar?.addEventListener("click", (e) => {
  e.preventDefault();
  e.stopPropagation();
  minimizarPanelAsistente();
});

asistenteToggle?.addEventListener("click", (e) => {
  e.preventDefault();
  e.stopPropagation();
  if (!asistentePanel) return;
  if (asistentePanel.classList.contains("hidden")) {
    asistenteIniciar();
    return;
  }
  if (asistentePanel.classList.contains("asistente-panel--compact")) {
    expandirPanelAsistente();
    return;
  }
  minimizarPanelAsistente();
});

asistenteReiniciar?.addEventListener("click", (e) => {
  e.preventDefault();
  e.stopPropagation();
  asistenteReiniciarFlujo();
});

asistenteResetVisualBase();
window.addEventListener("pageshow", () => {
  asistenteResetVisualBase();
});

// Yurguen: Pie de tienda configurable; toma múltiples campos opcionales del perfil.
function yurguenTextoPlano(valor) {
  if (typeof valor !== "string") return "";
  return valor.trim();
}

function yurguenRenderPieTienda(perfil, nombreTienda) {
  const pie = document.getElementById("tiendaPie");
  if (!pie) return;

  const partes = [];
  const nombre = yurguenTextoPlano(nombreTienda);
  if (nombre) partes.push(nombre);

  const tel =
    yurguenTextoPlano(perfil?.footerPhone) ||
    yurguenTextoPlano(perfil?.phone) ||
    yurguenTextoPlano(perfil?.contactPhone);
  if (tel) partes.push(`Tel: ${tel}`);

  const correo =
    yurguenTextoPlano(perfil?.footerEmail) ||
    yurguenTextoPlano(perfil?.email) ||
    yurguenTextoPlano(perfil?.contactEmail);
  if (correo) partes.push(`Correo: ${correo}`);

  const whatsapp =
    yurguenTextoPlano(perfil?.whatsApp) ||
    yurguenTextoPlano(perfil?.whatsapp) ||
    yurguenTextoPlano(perfil?.contactWhatsapp);
  if (whatsapp) partes.push(`WhatsApp: ${whatsapp}`);

  const direccion =
    yurguenTextoPlano(perfil?.footerAddress) ||
    yurguenTextoPlano(perfil?.address) ||
    yurguenTextoPlano(perfil?.contactAddress);
  if (direccion) partes.push(direccion);

  const horario =
    yurguenTextoPlano(perfil?.footerHours) ||
    yurguenTextoPlano(perfil?.hours) ||
    yurguenTextoPlano(perfil?.businessHours);
  if (horario) partes.push(horario);

  const extra = yurguenTextoPlano(perfil?.footerText) || yurguenTextoPlano(perfil?.tagline);
  if (extra) partes.push(extra);

  if (partes.length === 0) {
    pie.textContent = "";
    pie.hidden = true;
    return;
  }

  pie.textContent = partes.join(" · ");
  pie.hidden = false;
}

// Yurguen: Marca desde API (displayName, logoUrl, footerPhone); fallback al h1 del HTML.
async function aplicarPerfilStorefront() {
  try {
    const response = await fetch(apiTienda("storefront/perfil"));
    if (!response.ok) {
      return;
    }
    const p = await response.json();
    const name = typeof p.displayName === "string" && p.displayName.trim() ? p.displayName.trim() : tenantSlug;
    const h1 = document.getElementById("tituloTiendaCabecera") ?? document.querySelector(".topbar h1");
    if (h1) {
      h1.textContent = name;
    }
    document.title = `${name} · ExhaTech Store`;
    const homeBtn = document.querySelector(".amz-brand-home");
    if (homeBtn) {
      homeBtn.setAttribute("aria-label", `Inicio — ${name}`);
    }
    const lg = document.getElementById("brandLogo");
    const mark = document.getElementById("amzBrandMark");
    if (lg && typeof p.logoUrl === "string" && p.logoUrl.trim().length > 0) {
      lg.src = p.logoUrl.trim();
      lg.alt = name;
      lg.classList.remove("hidden");
      mark?.classList.add("hidden");
    } else {
      lg?.classList.add("hidden");
      if (mark) {
        const c = name.trim().charAt(0);
        mark.textContent = c ? c.toUpperCase() : "?";
        mark.classList.remove("hidden");
      }
    }
    yurguenRenderPieTienda(p, name);
  } catch {
    // Yurguen: Si falla API, mantenemos pie mínimo con nombre técnico de la tienda.
    yurguenRenderPieTienda({}, tenantSlug);
  }
}

const productDetailDialog = document.getElementById("productDetailDialog");
const productDetailBody = document.getElementById("productDetailBody");

function yurguenRenderDetalleEnDialog(d) {
  if (!productDetailBody) {
    return;
  }
  productDetailBody.innerHTML = "";
  const imgUrl = typeof d.imageUrl === "string" ? d.imageUrl.trim() : "";
  if (imgUrl.length > 0) {
    const img = document.createElement("img");
    img.className = "detail-thumb";
    img.src = imgUrl;
    img.alt = d.name || "";
    img.loading = "lazy";
    productDetailBody.appendChild(img);
  } else {
    const ph = document.createElement("div");
    ph.className = "detail-thumb detail-thumb--empty";
    ph.textContent = "Sin foto";
    productDetailBody.appendChild(ph);
  }
  const h3 = document.createElement("h3");
  h3.textContent = d.name ?? "";
  productDetailBody.appendChild(h3);
  const sku = document.createElement("p");
  sku.className = "sub";
  sku.textContent = d.sku ? `SKU: ${d.sku}` : "";
  productDetailBody.appendChild(sku);
  const desc = document.createElement("p");
  desc.textContent = d.description ?? "";
  productDetailBody.appendChild(desc);
  const precio = document.createElement("p");
  precio.className = "price";
  const ivaTxt = d.ivaIncluidoEnPrecio ? "IVA incluido" : "";
  precio.innerHTML = `${formatCrc(d.price)}${ivaTxt ? ` · <span class="iva-tag">${ivaTxt}</span>` : ""}`;
  productDetailBody.appendChild(precio);
  const add = document.createElement("button");
  add.type = "button";
  add.className = "add-btn";
  add.textContent = "Agregar al carrito";
  add.addEventListener("click", () => {
    addToCart({
      id: d.id,
      name: d.name,
      sku: d.sku,
      price: d.price,
      ivaIncluidoEnPrecio: Boolean(d.ivaIncluidoEnPrecio),
      imageUrl: d.imageUrl
    });
    productDetailDialog?.close();
    cartPanel?.classList.remove("hidden");
  });
  productDetailBody.appendChild(add);
}

async function abrirDetalleProducto(productIdStr) {
  if (!productDetailDialog || !productIdStr) {
    return;
  }
  try {
    const response = await fetch(apiTienda(`products/${encodeURIComponent(productIdStr)}`));
    const raw = await response.json().catch(() => ({}));
    if (!response.ok) {
      return;
    }
    yurguenRenderDetalleEnDialog(raw);
    productDetailDialog.showModal();
  } catch {
    /* ignorar */
  }
}

productGrid?.addEventListener("click", (e) => {
  const btn = e.target.closest(".add-btn");
  if (btn) {
    return;
  }
  const card = e.target.closest("article.card");
  if (!card || !productGrid.contains(card)) {
    return;
  }
  const id = card.dataset.productId;
  if (!id) {
    return;
  }
  void abrirDetalleProducto(id);
});

// Yurguen: FAB unificado antes de primera interacción.
(function yurguenFabArranqueMini() {
  if (!asistenteToggle || !asistentePanel) {
    return;
  }
  asistenteSyncFabAyudaEstadoPanel();
})();

(async function iniciarTienda() {
  await aplicarPerfilStorefront();
  iniciarCatalogo();
  await loadOrderHistory();
})();
