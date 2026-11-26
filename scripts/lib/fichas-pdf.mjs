/**
 * Lector del PDF «Ficha Técnica por Especie» (FT.CA.01, exportado de Excel): 4 fichas por fila, varias filas por
 * página y fichas partidas entre dos páginas. Trabaja sobre las palabras con su posición (las vuelca
 * `scripts/extraer-palabras-pdf`): [pagina, x, y desde arriba, ancho, alto, texto].
 *
 * Cada ficha se ancla en su etiqueta «CÓDIGO INTERNO» y va del «ESPECIE» anterior al «WEB» siguiente de la misma
 * columna. Excel centra cada valor en su celda combinada, igual que la etiqueta: cada párrafo se asigna a la
 * etiqueta cuyo centro está más cerca. La rejilla del agua (pH, GH, temperatura) se lee por posición.
 */

const ALTO_PAGINA = 612;
const ANCHO_COLUMNA = 158;
const MARGEN_ETIQUETA = 37;
const ETIQUETAS = [
  ["especie", ["ESPECIE"]],
  ["foto", ["ASPECTO/"]],
  ["codigo", ["CÓDIGO"]],
  ["cientifico", ["CIENTÍFICO", "GÉNERO"]],
  ["comercial", ["COMERCIAL"]],
  ["procedencia", ["PROCEDENCIA"]],
  ["caracteristicas", ["CARACTERISTICAS"]],
  ["dimorfismo", ["DIFORMISMO"]],
  ["condiciones", ["CONDICIONES", "AGUA"]],
  ["etologia", ["ETOLOGÍA", "ETOLOGIA"]],
  ["empaque", ["EMPAQUE"]],
  ["web", ["WEB"]],
];
const ENCABEZADO = /^(jrtropicalfish\.com|JR|TROPICAL|FISH|LTDA\.|Código|FT\.CA\.01|Versión|\d{2}\/\d{2}\/\d{4}|Pagina)$/;

export function leerFichas(palabrasCrudas) {
  const palabras = palabrasCrudas
    .filter((w) => String(w[5]).trim() !== "")
    .map(([p, x, y, , , t]) => ({ p, x, y: (p - 1) * ALTO_PAGINA + y, t }));
  const fichas = [];
  const problemas = [];
  for (const ancla of palabras.filter((w) => w.t === "CÓDIGO")) {
    const f = ficha(palabras, ancla);
    if (typeof f === "string") problemas.push(f);
    else fichas.push(f);
  }
  fichas.sort((a, b) => a.pagina - b.pagina || a.orden - b.orden);
  return { fichas, problemas };
}

function ficha(palabras, a) {
  const col = palabras.filter((w) => w.x >= a.x - 2 && w.x < a.x + ANCHO_COLUMNA);
  const etiqueta = (t) => col.filter((w) => w.t === t && Math.abs(w.x - a.x) < 2);
  const especie = etiqueta("ESPECIE").filter((w) => w.y < a.y && a.y - w.y < 250).sort((x, y) => y.y - x.y)[0];
  const web = etiqueta("WEB").filter((w) => w.y > a.y && w.y - a.y < 450).sort((x, y) => x.y - y.y)[0];
  if (!especie || !web) return `Ficha sin límites en la página ${a.p}`;
  const region = col.filter((w) => w.y >= especie.y - 6 && w.y <= web.y + 3 && !ENCABEZADO.test(w.t));

  const centros = {};
  for (const [clave, textos] of ETIQUETAS) {
    const ws = region.filter((w) => w.x < a.x + MARGEN_ETIQUETA && textos.includes(w.t));
    if (ws.length) centros[clave] = ws.reduce((s, w) => s + w.y, 0) / ws.length;
  }

  // Rejilla del agua.
  const ph = region.find((w) => w.t === "Ph" && w.x >= a.x + MARGEN_ETIQUETA);
  const gh = region.find((w) => w.t === "Gh");
  const temp = region.find((w) => w.t === "Temperatura");
  const xOxigeno = region.find((w) => /^Oxígen/.test(w.t))?.x ?? a.x + 109;
  const entre = (y0, y1, x0) =>
    region.filter((w) => w.y >= y0 && w.y <= y1 && w.x >= x0 && w.x < xOxigeno - 1).sort((x, y) => x.x - y.x).map((w) => w.t).join(" ");
  const phTexto = ph ? entre(ph.y - 0.8, ph.y + 1.8, ph.x + 4) : "";
  const ghTexto = gh ? entre(gh.y - 1.5, gh.y + 1.8, gh.x + 3) : "";
  const tempTexto = temp ? entre(temp.y - 1.5, temp.y + 3, temp.x + 10) : "";
  const aguaDesde = (ph?.y ?? centros.condiciones ?? 0) - 3;
  const aguaHasta = (temp?.y ?? centros.condiciones ?? 0) + 3.2;
  const enAgua = (w) => (ph || temp) && w.y >= aguaDesde && w.y <= aguaHasta && w.x >= a.x + 40;

  // Líneas de texto libre.
  const lineas = [];
  for (const w of region.filter((w) => w.x >= a.x + MARGEN_ETIQUETA - 2 && !enAgua(w)).sort((x, y) => x.y - y.y || x.x - y.x)) {
    const l = lineas.find((l) => Math.abs(l.y - w.y) < 1.5);
    if (l) l.ws.push(w);
    else lineas.push({ y: w.y, ws: [w] });
  }
  lineas.sort((x, y) => x.y - y.y);

  // Párrafos por espaciado, cada uno a la etiqueta más cercana a su centro.
  const parrafos = [];
  for (const l of lineas) {
    const texto = l.ws.sort((x, y) => x.x - y.x).map((w) => w.t).join(" ");
    const ultimo = parrafos.at(-1);
    if (ultimo && l.y - ultimo.hasta <= 4.4) {
      ultimo.hasta = l.y;
      ultimo.lineas.push(texto);
    } else parrafos.push({ desde: l.y, hasta: l.y, lineas: [texto] });
  }
  const claves = Object.keys(centros);
  const campos = {};
  for (const p of parrafos) {
    const texto = limpiar(p.lineas.join(" "));
    if (!texto) continue;
    const centro = (p.desde + p.hasta) / 2;
    let clave = claves.reduce((m, k) => (Math.abs(centros[k] - centro) < Math.abs(centros[m] - centro) ? k : m), claves[0]);
    if (clave === "condiciones") clave = "etologia";
    if (clave === "foto") clave = "especie";
    if (clave === "web") clave = "empaque";
    campos[clave] = campos[clave] ? `${campos[clave]} ${texto}` : texto;
  }

  const [phMin, phMax] = rango(phTexto);
  const [ghMin, ghMax] = rango(ghTexto);
  const temperaturas = numeros(tempTexto).filter((n) => n >= 10 && n <= 40);
  return {
    pagina: a.p,
    orden: a.y * 10 + a.x / 100,
    codigos: [...new Set((campos.codigo ?? "").match(/\b[A-Z]{2,3}\d{3}[A-Z]?\b/g) ?? [])],
    especie: campos.especie ?? null,
    nombreCientifico: campos.cientifico ?? null,
    nombreComercial: campos.comercial ?? null,
    procedencia: campos.procedencia ?? null,
    caracteristicas: campos.caracteristicas ?? null,
    dimorfismo: campos.dimorfismo ?? null,
    phMin, phMax, ghMin, ghMax,
    temperaturaMin: temperaturas.length ? Math.min(temperaturas[0], temperaturas[1] ?? temperaturas[0]) : null,
    temperaturaMax: temperaturas.length ? Math.max(temperaturas[0], temperaturas[1] ?? temperaturas[0]) : null,
    etologia: campos.etologia ?? null,
    empaque: campos.empaque ?? null,
    textoAgua: { ph: phTexto || null, gh: ghTexto || null, temperatura: tempTexto || null },
  };
}

const limpiar = (s) => s.replace(/\s+/g, " ").replace(/\s+([,.;:)])/g, "$1").replace(/\(\s+/g, "(").trim();

const numeros = (t) => [...(t ?? "").replace(/[´'°º]/g, " ").matchAll(/\d+(?:[.,]\d+)?/g)].map((m) => Number(m[0].replace(",", ".")));

/** "5.5 - 6.5", "´2 -8", "6,0 y 7,2", "4º – 8º" → [min, max]; "<5º" → [null, 5]; sin números → [null, null]. */
function rango(texto) {
  const n = numeros(texto);
  if (!n.length) return [null, null];
  if (/^\s*</.test(texto ?? "")) return [null, n[0]];
  const [a, b = n[0]] = n;
  return a <= b ? [a, b] : [b, a];
}

/** Palabras de 4 letras o más del nombre, sin tildes, para comparar una ficha con el producto. */
const palabrasNombre = (s) =>
  new Set(
    (s ?? "")
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .toLowerCase()
      .split(/[^a-z0-9]+/)
      .filter((t) => t.length >= 4 && !/idae$/.test(t) && t !== "especie"),
  );

/**
 * Asigna fichas a productos por código interno. Como el PDF trae códigos repetidos o equivocados, la ficha solo
 * se acepta si su nombre (científico, especie o comercial) comparte alguna palabra con el nombre científico del
 * producto; si dos fichas traen el mismo código, gana la que más coincide.
 */
export function asignarFichas(fichas, productos) {
  const porCodigo = new Map();
  for (const f of fichas) for (const c of f.codigos) (porCodigo.get(c) ?? porCodigo.set(c, []).get(c)).push(f);
  const asignadas = [];
  const sinProducto = [];
  const noCoinciden = [];
  const productosPorCodigo = new Map(productos.map((p) => [p.codigo, p]));
  for (const [codigo, candidatas] of porCodigo) {
    const producto = productosPorCodigo.get(codigo);
    if (!producto) {
      sinProducto.push({ codigo, especie: candidatas[0].especie });
      continue;
    }
    const delProducto = palabrasNombre(producto.nombreCientifico);
    const puntajes = candidatas.map((f) => {
      const deFicha = palabrasNombre([f.nombreCientifico, f.especie, f.nombreComercial].join(" "));
      return [...delProducto].filter((t) => deFicha.has(t)).length;
    });
    const mejor = Math.max(...puntajes);
    if (mejor === 0) {
      noCoinciden.push({ codigo, producto: producto.nombreCientifico, fichas: candidatas.map((f) => `${f.especie} (${f.nombreCientifico}), pág. ${f.pagina}`) });
      continue;
    }
    asignadas.push({ producto, ficha: candidatas[puntajes.indexOf(mejor)] });
  }
  return { asignadas, sinProducto, noCoinciden };
}
