/**
 * Lector mínimo de .xlsx sin dependencias (un .xlsx es un ZIP con XML). Lo usan los importadores.
 *   const zip = leerZip(readFileSync(ruta)); const filas = leerHoja(zip, "Clientes");
 * Cada fila es un objeto { encabezado: texto }. Lee valores (no fórmulas) y textos compartidos.
 */
import { inflateRawSync } from "node:zlib";

// --- ZIP mínimo (directorio central + deflate) ---
export function leerZip(buffer) {
  let fin = buffer.length - 22;
  while (fin >= 0 && buffer.readUInt32LE(fin) !== 0x06054b50) fin--;
  if (fin < 0) throw new Error("El archivo no es un .xlsx válido.");
  const total = buffer.readUInt16LE(fin + 10);
  let p = buffer.readUInt32LE(fin + 16);
  const archivos = new Map();
  for (let i = 0; i < total; i++) {
    const metodo = buffer.readUInt16LE(p + 10);
    const comprimido = buffer.readUInt32LE(p + 20);
    const largoNombre = buffer.readUInt16LE(p + 28);
    const largoExtra = buffer.readUInt16LE(p + 30);
    const largoComentario = buffer.readUInt16LE(p + 32);
    const local = buffer.readUInt32LE(p + 42);
    const nombre = buffer.toString("utf8", p + 46, p + 46 + largoNombre);
    archivos.set(nombre, { metodo, comprimido, local });
    p += 46 + largoNombre + largoExtra + largoComentario;
  }
  return (nombre) => {
    const a = archivos.get(nombre);
    if (!a) return null;
    const inicio = a.local + 30 + buffer.readUInt16LE(a.local + 26) + buffer.readUInt16LE(a.local + 28);
    const datos = buffer.subarray(inicio, inicio + a.comprimido);
    return (a.metodo === 8 ? inflateRawSync(datos) : datos).toString("utf8");
  };
}

const xmlTexto = (s) =>
  s.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, "&");

export function leerHoja(zip, nombreHoja) {
  const libro = zip("xl/workbook.xml");
  const hoja = [...libro.matchAll(/<sheet [^>]*name="([^"]+)"[^>]*r:id="([^"]+)"/g)].find((m) => m[1] === nombreHoja);
  if (!hoja) throw new Error(`No hay una hoja llamada «${nombreHoja}».`);
  const rels = zip("xl/_rels/workbook.xml.rels");
  const destino = [...rels.matchAll(/<Relationship [^>]*>/g)]
    .map((m) => m[0])
    .find((r) => r.includes(`Id="${hoja[2]}"`))
    ?.match(/Target="([^"]+)"/)?.[1];
  const xml = zip(`xl/${destino.replace(/^\/?xl\//, "")}`);

  const compartidas = [...(zip("xl/sharedStrings.xml") ?? "").matchAll(/<si>([\s\S]*?)<\/si>/g)].map((m) =>
    xmlTexto([...m[1].matchAll(/<t[^>]*>([\s\S]*?)<\/t>/g)].map((t) => t[1]).join("")),
  );

  const filas = [];
  for (const fila of xml.matchAll(/<row [^>]*>([\s\S]*?)<\/row>/g)) {
    const celdas = {};
    for (const c of fila[1].matchAll(/<c r="([A-Z]+)\d+"([^>]*?)(?:\/>|>([\s\S]*?)<\/c>)/g)) {
      const [, columna, atributos, contenido = ""] = c;
      const tipo = atributos.match(/t="([^"]+)"/)?.[1];
      const v = contenido.match(/<v>([\s\S]*?)<\/v>/)?.[1];
      let valor = "";
      if (tipo === "s") valor = compartidas[Number(v)] ?? "";
      else if (tipo === "inlineStr") valor = xmlTexto([...contenido.matchAll(/<t[^>]*>([\s\S]*?)<\/t>/g)].map((t) => t[1]).join(""));
      else if (v !== undefined) valor = xmlTexto(v);
      celdas[columna] = valor.trim();
    }
    filas.push(celdas);
  }
  const encabezados = filas.shift() ?? {};
  return filas.map((celdas) => Object.fromEntries(Object.entries(encabezados).map(([col, enc]) => [enc, celdas[col] ?? ""])));
}
