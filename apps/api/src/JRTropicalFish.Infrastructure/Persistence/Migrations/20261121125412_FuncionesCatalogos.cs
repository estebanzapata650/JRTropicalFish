using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones del catálogo y sembrado: permisos, maestros nuevos (grupos de producto, cuencas) en la
    /// lista blanca de <c>fn_maestro_tabla</c>, cuencas, tipo de identificación NIT, las 8 listas de
    /// precios del Excel «JR MAIN STOCK LIST» con sus reglas, y los parámetros de precios.
    ///
    /// <para>
    /// El orden de columnas de cada <c>RETURNS TABLE</c> coincide con su record en
    /// <c>Application/Catalogos/CatalogosModels.cs</c>.
    /// </para>
    /// </summary>
    public partial class FuncionesCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);
            SembrarMaestros(migrationBuilder);
            SembrarPrecios(migrationBuilder);
            CrearFuncionesCentros(migrationBuilder);
            CrearFuncionesProductos(migrationBuilder);
            CrearFuncionesPrecios(migrationBuilder);
            CrearFuncionesProveedores(migrationBuilder);
        }

        private static void SembrarMaestros(MigrationBuilder migrationBuilder)
        {
            // Misma firma: CREATE OR REPLACE basta para ampliar la lista blanca.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_maestro_tabla(p_tabla text)
                RETURNS text
                LANGUAGE plpgsql IMMUTABLE AS $$
                BEGIN
                    IF p_tabla IS NULL OR p_tabla NOT IN ('tipos_identificacion', 'cargos', 'areas', 'tipos_contrato',
                                                          'grupos_producto', 'cuencas') THEN
                        RAISE EXCEPTION 'Maestro no permitido: %', p_tabla;
                    END IF;
                    RETURN p_tabla;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO cuencas (id, codigo, nombre, activo, creado_en) VALUES
                    (gen_random_uuid(), 'ORINOCO', 'Orinoco', true, now()),
                    (gen_random_uuid(), 'AMAZONAS', 'Amazonas', true, now()),
                    (gen_random_uuid(), 'MAGDALENA', 'Magdalena', true, now()),
                    (gen_random_uuid(), 'PACIFICO', 'Pacífico', true, now()),
                    (gen_random_uuid(), 'CARIBE', 'Caribe', true, now()),
                    (gen_random_uuid(), 'BOGOTA', 'Bogotá', true, now()),
                    (gen_random_uuid(), 'CRIADERO', 'Criadero', true, now())
                ON CONFLICT DO NOTHING;

                INSERT INTO tipos_identificacion (id, codigo, nombre, activo, creado_en) VALUES
                    (gen_random_uuid(), 'NIT', 'Número de identificación tributaria', true, now())
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <summary>
        /// Listas del Excel con las reglas deducidas de sus datos (cuadran con redondeo a $50):
        /// Minoristas = Standard × 1,2; Glaser = Standard × 1,1; Mayorista = Costo × (2 | 1,3 | 2) × IVA × Wompi;
        /// David Ramírez = Mayorista × 1,2; Detal = Mayorista × (2 | 1,5 | 2). Wholesale Asian es manual.
        /// </summary>
        private static void SembrarPrecios(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO parametros_precios (id, iva, pasarela, trm_referencia) VALUES (1, 19, 3.15, 3150)
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO listas_precios (id, codigo, nombre, moneda, es_costo, orden, activo, aplica_impuestos, redondeo, creado_en)
                VALUES
                    (gen_random_uuid(), 'STANDARD', 'Standard', 'USD', false, 10, true, false, 0.01, now()),
                    (gen_random_uuid(), 'MAYORISTA_ASIA', 'Wholesale Asian', 'USD', false, 20, true, false, 0.01, now()),
                    (gen_random_uuid(), 'GLASER', 'Glaser', 'USD', false, 30, true, false, 0.01, now()),
                    (gen_random_uuid(), 'MINORISTA_EXTERIOR', 'Minoristas del exterior', 'USD', false, 40, true, false, 0.01, now()),
                    (gen_random_uuid(), 'COSTO', 'Costo de compra', 'COP', true, 50, true, false, 50, now()),
                    (gen_random_uuid(), 'MAYORISTA_NACIONAL', 'Mayorista nacional', 'COP', false, 60, true, true, 50, now()),
                    (gen_random_uuid(), 'DAVID_RAMIREZ', 'David Ramírez', 'COP', false, 70, true, false, 50, now()),
                    (gen_random_uuid(), 'DETAL_NACIONAL', 'Al detal nacional', 'COP', false, 80, true, false, 50, now())
                ON CONFLICT DO NOTHING;

                UPDATE listas_precios l SET lista_base_id = b.id, factor_salvaje = 1.1, factor_criadero = 1.1, factor_especial = 1.1
                FROM listas_precios b WHERE l.codigo = 'GLASER' AND b.codigo = 'STANDARD' AND l.lista_base_id IS NULL;
                UPDATE listas_precios l SET lista_base_id = b.id, factor_salvaje = 1.2, factor_criadero = 1.2, factor_especial = 1.2
                FROM listas_precios b WHERE l.codigo = 'MINORISTA_EXTERIOR' AND b.codigo = 'STANDARD' AND l.lista_base_id IS NULL;
                UPDATE listas_precios l SET lista_base_id = b.id, factor_salvaje = 2, factor_criadero = 1.3, factor_especial = 2
                FROM listas_precios b WHERE l.codigo = 'MAYORISTA_NACIONAL' AND b.codigo = 'COSTO' AND l.lista_base_id IS NULL;
                UPDATE listas_precios l SET lista_base_id = b.id, factor_salvaje = 1.2, factor_criadero = 1.2, factor_especial = 1.2
                FROM listas_precios b WHERE l.codigo = 'DAVID_RAMIREZ' AND b.codigo = 'MAYORISTA_NACIONAL' AND l.lista_base_id IS NULL;
                UPDATE listas_precios l SET lista_base_id = b.id, factor_salvaje = 2, factor_criadero = 1.5, factor_especial = 2
                FROM listas_precios b WHERE l.codigo = 'DETAL_NACIONAL' AND b.codigo = 'MAYORISTA_NACIONAL' AND l.lista_base_id IS NULL;

                -- La lista de David Ramírez es de un cliente: se enlaza si ya existe (importado del Excel comercial).
                UPDATE listas_precios SET cliente_id = (
                    SELECT id FROM clientes WHERE eliminado_en IS NULL AND nombre_empresa ILIKE 'David Ram%rez%' LIMIT 1)
                WHERE codigo = 'DAVID_RAMIREZ' AND cliente_id IS NULL;
                """);
        }

        private static void CrearFuncionesCentros(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, nombre text, cuenca_id uuid, cuenca_nombre text, activo boolean,
                               productos integer, proveedores integer, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT c.id, c.nombre::text, c.cuenca_id, cu.nombre::text, c.activo,
                           (SELECT count(*)::integer FROM producto_centros_acopio pc JOIN productos p ON p.id = pc.producto_id
                            WHERE pc.centro_acopio_id = c.id AND p.eliminado_en IS NULL),
                           (SELECT count(*)::integer FROM proveedor_centros_acopio pv JOIN proveedores v ON v.id = pv.proveedor_id
                            WHERE pv.centro_acopio_id = c.id AND v.eliminado_en IS NULL),
                           c.creado_en
                    FROM centros_acopio c
                    JOIN cuencas cu ON cu.id = c.cuenca_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_centros_acopio_listar()
                {columnas}
                    WHERE c.eliminado_en IS NULL
                    ORDER BY c.activo DESC, c.nombre;
                $$;

                CREATE FUNCTION fn_centro_acopio_por_id(p_id uuid)
                {columnas}
                    WHERE c.id = p_id AND c.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_centro_acopio_nombre_existe(p_nombre text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM centros_acopio WHERE eliminado_en IS NULL AND lower(nombre) = lower(btrim(p_nombre))
                                   AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;

                CREATE PROCEDURE sp_centro_acopio_guardar(p_id uuid, p_es_nuevo boolean, p_nombre text, p_cuenca_id uuid,
                                                          p_activo boolean, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO centros_acopio (id, nombre, cuenca_id, activo, creado_en)
                        VALUES (p_id, p_nombre, p_cuenca_id, p_activo, p_fecha);
                    ELSE
                        UPDATE centros_acopio SET nombre = p_nombre, cuenca_id = p_cuenca_id, activo = p_activo, actualizado_en = p_fecha
                        WHERE id = p_id AND eliminado_en IS NULL;
                    END IF;
                END;
                $$;

                CREATE PROCEDURE sp_centro_acopio_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE centros_acopio SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        private static void CrearFuncionesProductos(MigrationBuilder migrationBuilder)
        {
            // El grupo se une sin filtrar eliminado_en: el producto conserva el nombre de su grupo.
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, grupo_id uuid, grupo_codigo text, grupo_nombre text, tipo text,
                               nombre_cientifico text, nombre_comun text, nombre_espanol text, talla_min_cm numeric,
                               talla_max_cm numeric, talla_etiqueta text, bolsa_min integer, bolsa_max integer,
                               disponibilidad text, activo boolean, notas text, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.grupo_id, g.codigo::text, g.nombre::text, p.tipo::text,
                           p.nombre_cientifico::text, p.nombre_comun::text, p.nombre_espanol::text, p.talla_min_cm,
                           p.talla_max_cm, p.talla_etiqueta::text, p.bolsa_min, p.bolsa_max,
                           p.disponibilidad::text, p.activo, p.notas::text, p.creado_en
                    FROM productos p
                    JOIN grupos_producto g ON g.id = p.grupo_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_productos_listar()
                {columnas}
                    WHERE p.eliminado_en IS NULL
                    ORDER BY p.codigo;
                $$;

                CREATE FUNCTION fn_producto_por_id(p_id uuid)
                {columnas}
                    WHERE p.id = p_id AND p.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_producto_centros(p_producto_id uuid)
                RETURNS TABLE (producto_id uuid, centro_acopio_id uuid, nombre text, orden integer)
                LANGUAGE sql STABLE AS $$
                    SELECT pc.producto_id, pc.centro_acopio_id, c.nombre::text, pc.orden
                    FROM producto_centros_acopio pc
                    JOIN centros_acopio c ON c.id = pc.centro_acopio_id
                    JOIN productos p ON p.id = pc.producto_id AND p.eliminado_en IS NULL
                    WHERE p_producto_id IS NULL OR pc.producto_id = p_producto_id
                    ORDER BY pc.producto_id, pc.orden;
                $$;

                CREATE FUNCTION fn_producto_codigo_existe(p_codigo text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM productos WHERE eliminado_en IS NULL AND upper(codigo) = upper(btrim(p_codigo))
                                   AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;

                -- Siguiente código del grupo, de 5 en 5 como en el Excel (AP020 → AP025), con el ancho que ya usa el grupo.
                CREATE FUNCTION fn_producto_siguiente_codigo(p_prefijo text)
                RETURNS text
                LANGUAGE sql STABLE AS $$
                    WITH numeros AS (
                        SELECT substring(codigo FROM length(p_prefijo) + 1) AS sufijo
                        FROM productos
                        WHERE codigo ~ ('^' || p_prefijo || '[0-9]+$'))
                    SELECT p_prefijo || lpad(((coalesce(max(sufijo::integer), 0) / 5 + 1) * 5)::text,
                                             coalesce(max(length(sufijo)), 3), '0')
                    FROM numeros;
                $$;
                """);

            // Guardar = producto + centros (reemplaza) + precios indicados (los demás no se tocan) en una
            // transacción. p_precios: [{"lista": uuid, "precio": número | null}]; null o 0 = sin precio.
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_producto_guardar(
                    p_id uuid, p_es_nuevo boolean, p_codigo text, p_grupo_id uuid, p_tipo text, p_nombre_cientifico text,
                    p_nombre_comun text, p_nombre_espanol text, p_talla_min_cm numeric, p_talla_max_cm numeric,
                    p_talla_etiqueta text, p_bolsa_min integer, p_bolsa_max integer, p_disponibilidad text, p_activo boolean,
                    p_notas text, p_centros uuid[], p_precios jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO productos (id, codigo, grupo_id, tipo, nombre_cientifico, nombre_comun, nombre_espanol,
                                               talla_min_cm, talla_max_cm, talla_etiqueta, bolsa_min, bolsa_max, disponibilidad,
                                               activo, notas, creado_en)
                        VALUES (p_id, upper(p_codigo), p_grupo_id, p_tipo, p_nombre_cientifico, p_nombre_comun, p_nombre_espanol,
                                p_talla_min_cm, p_talla_max_cm, p_talla_etiqueta, p_bolsa_min, p_bolsa_max, p_disponibilidad,
                                p_activo, p_notas, p_fecha);
                    ELSE
                        UPDATE productos
                        SET grupo_id = p_grupo_id, tipo = p_tipo, nombre_cientifico = p_nombre_cientifico,
                            nombre_comun = p_nombre_comun, nombre_espanol = p_nombre_espanol, talla_min_cm = p_talla_min_cm,
                            talla_max_cm = p_talla_max_cm, talla_etiqueta = p_talla_etiqueta, bolsa_min = p_bolsa_min,
                            bolsa_max = p_bolsa_max, disponibilidad = p_disponibilidad, activo = p_activo, notas = p_notas,
                            actualizado_en = p_fecha
                        WHERE id = p_id AND eliminado_en IS NULL;
                    END IF;

                    DELETE FROM producto_centros_acopio WHERE producto_id = p_id;
                    INSERT INTO producto_centros_acopio (producto_id, centro_acopio_id, orden)
                    SELECT p_id, c.id, c.orden::integer FROM unnest(p_centros) WITH ORDINALITY AS c(id, orden);

                    DELETE FROM precios_producto pp
                    USING jsonb_array_elements(p_precios) e
                    WHERE pp.producto_id = p_id AND pp.lista_precios_id = (e->>'lista')::uuid
                      AND coalesce((e->>'precio')::numeric, 0) <= 0;

                    INSERT INTO precios_producto (producto_id, lista_precios_id, precio, actualizado_en)
                    SELECT p_id, (e->>'lista')::uuid, (e->>'precio')::numeric, p_fecha
                    FROM jsonb_array_elements(p_precios) e
                    WHERE coalesce((e->>'precio')::numeric, 0) > 0
                    ON CONFLICT (producto_id, lista_precios_id)
                    DO UPDATE SET precio = EXCLUDED.precio, actualizado_en = EXCLUDED.actualizado_en
                    WHERE precios_producto.precio IS DISTINCT FROM EXCLUDED.precio;
                END;
                $$;

                CREATE PROCEDURE sp_producto_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE productos SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        private static void CrearFuncionesPrecios(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, nombre text, moneda text, es_costo boolean, cliente_id uuid,
                               cliente_nombre text, orden integer, activo boolean, lista_base_id uuid, lista_base_nombre text,
                               factor_salvaje numeric, factor_criadero numeric, factor_especial numeric,
                               aplica_impuestos boolean, redondeo numeric, productos integer)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.codigo::text, l.nombre::text, l.moneda::text, l.es_costo, l.cliente_id,
                           c.nombre_empresa::text, l.orden, l.activo, l.lista_base_id, b.nombre::text,
                           l.factor_salvaje, l.factor_criadero, l.factor_especial, l.aplica_impuestos, l.redondeo,
                           (SELECT count(*)::integer FROM precios_producto pp JOIN productos p ON p.id = pp.producto_id
                            WHERE pp.lista_precios_id = l.id AND p.eliminado_en IS NULL)
                    FROM listas_precios l
                    LEFT JOIN clientes c ON c.id = l.cliente_id
                    LEFT JOIN listas_precios b ON b.id = l.lista_base_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_listas_precios_listar()
                {columnas}
                    ORDER BY l.orden, l.nombre;
                $$;

                CREATE FUNCTION fn_lista_precios_por_id(p_id uuid)
                {columnas}
                    WHERE l.id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                -- Precios de productos no eliminados (todas las listas: el servicio quita las de costo si no hay permiso).
                CREATE FUNCTION fn_precios_listar(p_producto_id uuid, p_lista_id uuid)
                RETURNS TABLE (producto_id uuid, lista_precios_id uuid, precio numeric)
                LANGUAGE sql STABLE AS $$
                    SELECT pp.producto_id, pp.lista_precios_id, pp.precio
                    FROM precios_producto pp
                    JOIN productos p ON p.id = pp.producto_id AND p.eliminado_en IS NULL
                    WHERE (p_producto_id IS NULL OR pp.producto_id = p_producto_id)
                      AND (p_lista_id IS NULL OR pp.lista_precios_id = p_lista_id);
                $$;

                CREATE FUNCTION fn_lista_precios_valor_existe(p_campo text, p_valor text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM listas_precios
                                   WHERE (p_excluir_id IS NULL OR id <> p_excluir_id)
                                     AND CASE p_campo WHEN 'codigo' THEN upper(codigo) = upper(btrim(p_valor))
                                                      WHEN 'nombre' THEN lower(nombre) = lower(btrim(p_valor)) END);
                $$;

                CREATE PROCEDURE sp_lista_precios_guardar(
                    p_id uuid, p_es_nueva boolean, p_codigo text, p_nombre text, p_moneda text, p_es_costo boolean,
                    p_cliente_id uuid, p_orden integer, p_activo boolean, p_lista_base_id uuid, p_factor_salvaje numeric,
                    p_factor_criadero numeric, p_factor_especial numeric, p_aplica_impuestos boolean, p_redondeo numeric,
                    p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nueva THEN
                        INSERT INTO listas_precios (id, codigo, nombre, moneda, es_costo, cliente_id, orden, activo, lista_base_id,
                                                    factor_salvaje, factor_criadero, factor_especial, aplica_impuestos,
                                                    redondeo, creado_en)
                        VALUES (p_id, upper(p_codigo), p_nombre, p_moneda, p_es_costo, p_cliente_id, p_orden, p_activo,
                                p_lista_base_id, p_factor_salvaje, p_factor_criadero, p_factor_especial, p_aplica_impuestos,
                                p_redondeo, p_fecha);
                    ELSE
                        -- Código, moneda y "es costo" no cambian: los precios guardados dependen de ellos.
                        UPDATE listas_precios
                        SET nombre = p_nombre, cliente_id = p_cliente_id, orden = p_orden, activo = p_activo,
                            lista_base_id = p_lista_base_id, factor_salvaje = p_factor_salvaje,
                            factor_criadero = p_factor_criadero, factor_especial = p_factor_especial,
                            aplica_impuestos = p_aplica_impuestos, redondeo = p_redondeo, actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;
                END;
                $$;

                -- Recalcular en bloque: el cálculo lo hace C# (ReglasPrecio, probado); aquí solo se escribe el lote.
                CREATE PROCEDURE sp_precios_guardar_lote(p_lista_id uuid, p_precios jsonb, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO precios_producto (producto_id, lista_precios_id, precio, actualizado_en)
                    SELECT (e->>'producto')::uuid, p_lista_id, (e->>'precio')::numeric, p_fecha
                    FROM jsonb_array_elements(p_precios) e
                    WHERE (e->>'precio')::numeric > 0
                    ON CONFLICT (producto_id, lista_precios_id)
                    DO UPDATE SET precio = EXCLUDED.precio, actualizado_en = EXCLUDED.actualizado_en
                    WHERE precios_producto.precio IS DISTINCT FROM EXCLUDED.precio;
                $$;

                CREATE FUNCTION fn_parametros_precios()
                RETURNS TABLE (iva numeric, pasarela numeric, trm_referencia numeric, actualizado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT iva, pasarela, trm_referencia, actualizado_en FROM parametros_precios WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_parametros_precios_actualizar(p_iva numeric, p_pasarela numeric, p_trm_referencia numeric,
                                                                  p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE parametros_precios SET iva = p_iva, pasarela = p_pasarela, trm_referencia = p_trm_referencia,
                                                  actualizado_en = p_fecha
                    WHERE id = 1;
                $$;
                """);
        }

        private static void CrearFuncionesProveedores(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, nombre text, tipo_identificacion_id uuid, tipo_identificacion_codigo text,
                               numero_identificacion text, telefono text, contacto text, email text, notas text,
                               activo boolean, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT v.id, v.nombre::text, v.tipo_identificacion_id, ti.codigo::text, v.numero_identificacion::text,
                           v.telefono::text, v.contacto::text, v.email::text, v.notas::text, v.activo, v.creado_en
                    FROM proveedores v
                    JOIN tipos_identificacion ti ON ti.id = v.tipo_identificacion_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_proveedores_listar()
                {columnas}
                    WHERE v.eliminado_en IS NULL
                    ORDER BY v.activo DESC, v.nombre;
                $$;

                CREATE FUNCTION fn_proveedor_por_id(p_id uuid)
                {columnas}
                    WHERE v.id = p_id AND v.eliminado_en IS NULL;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_proveedor_centros(p_proveedor_id uuid)
                RETURNS TABLE (proveedor_id uuid, centro_acopio_id uuid, nombre text)
                LANGUAGE sql STABLE AS $$
                    SELECT pv.proveedor_id, pv.centro_acopio_id, c.nombre::text
                    FROM proveedor_centros_acopio pv
                    JOIN centros_acopio c ON c.id = pv.centro_acopio_id
                    WHERE p_proveedor_id IS NULL OR pv.proveedor_id = p_proveedor_id
                    ORDER BY pv.proveedor_id, c.nombre;
                $$;

                CREATE FUNCTION fn_proveedor_identificacion_existe(p_tipo_id uuid, p_numero text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM proveedores WHERE eliminado_en IS NULL AND tipo_identificacion_id = p_tipo_id
                                   AND numero_identificacion = p_numero AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;

                CREATE PROCEDURE sp_proveedor_guardar(
                    p_id uuid, p_es_nuevo boolean, p_nombre text, p_tipo_identificacion_id uuid, p_numero_identificacion text,
                    p_telefono text, p_contacto text, p_email text, p_notas text, p_activo boolean, p_centros uuid[],
                    p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO proveedores (id, nombre, tipo_identificacion_id, numero_identificacion, telefono, contacto,
                                                 email, notas, activo, creado_en)
                        VALUES (p_id, p_nombre, p_tipo_identificacion_id, p_numero_identificacion, p_telefono, p_contacto,
                                lower(p_email), p_notas, p_activo, p_fecha);
                    ELSE
                        UPDATE proveedores
                        SET nombre = p_nombre, tipo_identificacion_id = p_tipo_identificacion_id,
                            numero_identificacion = p_numero_identificacion, telefono = p_telefono, contacto = p_contacto,
                            email = lower(p_email), notas = p_notas, activo = p_activo, actualizado_en = p_fecha
                        WHERE id = p_id AND eliminado_en IS NULL;
                    END IF;

                    DELETE FROM proveedor_centros_acopio WHERE proveedor_id = p_id;
                    INSERT INTO proveedor_centros_acopio (proveedor_id, centro_acopio_id)
                    SELECT DISTINCT p_id, c FROM unnest(p_centros) AS c;
                END;
                $$;

                CREATE PROCEDURE sp_proveedor_eliminar(p_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE proveedores SET eliminado_en = p_fecha WHERE id = p_id AND eliminado_en IS NULL;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "PROCEDURE sp_proveedor_eliminar", "PROCEDURE sp_proveedor_guardar", "FUNCTION fn_proveedor_identificacion_existe",
                         "FUNCTION fn_proveedor_centros", "FUNCTION fn_proveedor_por_id", "FUNCTION fn_proveedores_listar",
                         "PROCEDURE sp_parametros_precios_actualizar", "FUNCTION fn_parametros_precios", "PROCEDURE sp_precios_guardar_lote",
                         "PROCEDURE sp_lista_precios_guardar", "FUNCTION fn_lista_precios_valor_existe", "FUNCTION fn_precios_listar",
                         "FUNCTION fn_lista_precios_por_id", "FUNCTION fn_listas_precios_listar",
                         "PROCEDURE sp_producto_eliminar", "PROCEDURE sp_producto_guardar", "FUNCTION fn_producto_siguiente_codigo",
                         "FUNCTION fn_producto_codigo_existe", "FUNCTION fn_producto_centros", "FUNCTION fn_producto_por_id",
                         "FUNCTION fn_productos_listar",
                         "PROCEDURE sp_centro_acopio_eliminar", "PROCEDURE sp_centro_acopio_guardar",
                         "FUNCTION fn_centro_acopio_nombre_existe", "FUNCTION fn_centro_acopio_por_id", "FUNCTION fn_centros_acopio_listar",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }

            // Lista blanca de maestros como estaba (sin grupos_producto ni cuencas, cuyas tablas borra el Down de Catalogos).
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_maestro_tabla(p_tabla text)
                RETURNS text
                LANGUAGE plpgsql IMMUTABLE AS $$
                BEGIN
                    IF p_tabla IS NULL OR p_tabla NOT IN ('tipos_identificacion', 'cargos', 'areas', 'tipos_contrato') THEN
                        RAISE EXCEPTION 'Maestro no permitido: %', p_tabla;
                    END IF;
                    RETURN p_tabla;
                END;
                $$;
                """);
        }
    }
}
