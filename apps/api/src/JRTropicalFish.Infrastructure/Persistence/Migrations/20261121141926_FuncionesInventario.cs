using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones de inventario, compras y pedidos de cliente.
    ///
    /// <para>
    /// <b>Motor de stock</b>: todo cambio de existencias pasa por <c>sp_inventario_mover_uno</c> (origen →
    /// destino con <c>SELECT … FOR UPDATE</c> y sin dejar negativos: <c>SQLSTATE JR002</c>) y su versión por
    /// lotes <c>sp_inventario_mover</c> (jsonb, todo o nada). Recepción y despacho lo llaman dentro de su
    /// propia transacción. Despachar más de lo pendiente de la línea: <c>SQLSTATE JR003</c>. Estado que
    /// cambió mientras tanto: <c>JR001</c>.
    /// </para>
    ///
    /// <para>El orden de columnas de cada RETURNS TABLE = el record en <c>Application/Inventario/InventarioModels.cs</c>.</para>
    /// </summary>
    public partial class FuncionesInventario : Migration
    {
        public const string SqlStateSinStock = "JR002";
        public const string SqlStateSuperaPendiente = "JR003";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);

            migrationBuilder.Sql(
                """
                INSERT INTO consecutivos (prefijo, ultimo) VALUES ('DISP', 0), ('PP', 0), ('REC', 0), ('LOT', 0), ('PC', 0)
                ON CONFLICT (prefijo) DO NOTHING;
                """);

            // Los documentos de inventario usan 4 dígitos como la app anterior (LOT-0532); los del CRM siguen en 3.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_siguiente_consecutivo(p_prefijo text)
                RETURNS text
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_numero integer;
                BEGIN
                    INSERT INTO consecutivos (prefijo, ultimo) VALUES (p_prefijo, 1)
                    ON CONFLICT (prefijo) DO UPDATE SET ultimo = consecutivos.ultimo + 1
                    RETURNING ultimo INTO v_numero;
                    RETURN p_prefijo || '-' || lpad(v_numero::text,
                        CASE WHEN p_prefijo IN ('LOT', 'DISP', 'PP', 'REC', 'PC') THEN 4 ELSE 3 END, '0');
                END;
                $$;
                """);

            CrearMotor(migrationBuilder);
            CrearConsultas(migrationBuilder);
            CrearAcuarios(migrationBuilder);
            CrearCompras(migrationBuilder);
            CrearPedidosCliente(migrationBuilder);
            CrearResumenes(migrationBuilder);
        }

        private static void CrearMotor(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_inventario_mover_uno(
                    p_lote_id uuid, p_origen_acuario uuid, p_origen_condicion text, p_destino_acuario uuid,
                    p_destino_condicion text, p_cantidad integer, p_tipo text, p_pedido_linea_id uuid,
                    p_recepcion_linea_id uuid, p_nota text, p_usuario_id uuid, p_usuario_nombre text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_disponible integer;
                BEGIN
                    IF p_cantidad IS NULL OR p_cantidad <= 0 THEN
                        RAISE EXCEPTION 'La cantidad debe ser mayor que cero.';
                    END IF;

                    IF p_origen_condicion IS NOT NULL THEN
                        SELECT cantidad INTO v_disponible
                        FROM existencias
                        WHERE lote_id = p_lote_id AND acuario_id IS NOT DISTINCT FROM p_origen_acuario
                          AND condicion = p_origen_condicion
                        FOR UPDATE;

                        IF coalesce(v_disponible, 0) < p_cantidad THEN
                            RAISE EXCEPTION 'Solo hay % disponibles en ese origen.', coalesce(v_disponible, 0)
                                USING ERRCODE = '{SqlStateSinStock}';
                        END IF;

                        UPDATE existencias SET cantidad = cantidad - p_cantidad, actualizado_en = p_fecha
                        WHERE lote_id = p_lote_id AND acuario_id IS NOT DISTINCT FROM p_origen_acuario
                          AND condicion = p_origen_condicion;

                        DELETE FROM existencias
                        WHERE lote_id = p_lote_id AND acuario_id IS NOT DISTINCT FROM p_origen_acuario
                          AND condicion = p_origen_condicion AND cantidad = 0;
                    END IF;

                    IF p_destino_condicion IS NOT NULL THEN
                        INSERT INTO existencias (id, lote_id, acuario_id, condicion, cantidad, actualizado_en)
                        VALUES (gen_random_uuid(), p_lote_id, p_destino_acuario, p_destino_condicion, p_cantidad, p_fecha)
                        ON CONFLICT (lote_id, acuario_id, condicion)
                        DO UPDATE SET cantidad = existencias.cantidad + EXCLUDED.cantidad, actualizado_en = EXCLUDED.actualizado_en;
                    END IF;

                    INSERT INTO movimientos_inventario (id, tipo, lote_id, acuario_origen_id, condicion_origen, acuario_destino_id,
                                                       condicion_destino, cantidad, pedido_cliente_linea_id, recepcion_linea_id,
                                                       nota, usuario_id, usuario_nombre, fecha)
                    VALUES (gen_random_uuid(), p_tipo, p_lote_id,
                            CASE WHEN p_origen_condicion IS NOT NULL THEN p_origen_acuario END, p_origen_condicion,
                            CASE WHEN p_destino_condicion IS NOT NULL THEN p_destino_acuario END, p_destino_condicion,
                            p_cantidad, p_pedido_linea_id, p_recepcion_linea_id, p_nota, p_usuario_id, p_usuario_nombre, p_fecha);
                END;
                $$;
                """);

            // Varios movimientos en una transacción (ubicar un lote en varios acuarios, etc.): todo o nada.
            // p_movimientos: [{lote, origenAcuario, origenCondicion, destinoAcuario, destinoCondicion, cantidad, tipo, nota}]
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_inventario_mover(p_movimientos jsonb, p_usuario_id uuid, p_usuario_nombre text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    m jsonb;
                BEGIN
                    FOR m IN SELECT * FROM jsonb_array_elements(p_movimientos) LOOP
                        CALL sp_inventario_mover_uno(
                            (m->>'lote')::uuid, (m->>'origenAcuario')::uuid, m->>'origenCondicion',
                            (m->>'destinoAcuario')::uuid, m->>'destinoCondicion', (m->>'cantidad')::integer, m->>'tipo',
                            NULL, NULL, m->>'nota', p_usuario_id, p_usuario_nombre, p_fecha);
                    END LOOP;
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_lote_crear(
                    p_id uuid, p_codigo text, p_producto_id uuid, p_proveedor_id uuid, p_centro_acopio_id uuid,
                    p_recepcion_linea_id uuid, p_fecha_ingreso date, p_costo_unitario numeric, p_recibidos integer,
                    p_muertos_llegada integer, p_origen text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO lotes (id, codigo, producto_id, proveedor_id, centro_acopio_id, recepcion_linea_id, fecha_ingreso,
                                       costo_unitario, recibidos, muertos_llegada, origen, creado_en)
                    VALUES (p_id, p_codigo, p_producto_id, p_proveedor_id, p_centro_acopio_id, p_recepcion_linea_id, p_fecha_ingreso,
                            p_costo_unitario, p_recibidos, p_muertos_llegada, p_origen, p_fecha);
                $$;

                CREATE FUNCTION fn_lote_codigo_existe(p_codigo text)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM lotes WHERE upper(codigo) = upper(btrim(p_codigo)));
                $$;
                """);
        }

        private static void CrearConsultas(MigrationBuilder migrationBuilder)
        {
            // Existencias con stock (> 0). dias_en_bodega contra el "hoy" de negocio.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_existencias_listar(p_hoy date, p_producto_id uuid, p_lote_id uuid, p_acuario_id uuid)
                RETURNS TABLE (id uuid, lote_id uuid, lote_codigo text, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, acuario_id uuid, acuario_codigo text, condicion text, cantidad integer,
                               fecha_ingreso date, dias_en_bodega integer, proveedor_nombre text, centro_nombre text)
                LANGUAGE sql STABLE AS $$
                    SELECT e.id, e.lote_id, l.codigo::text, l.producto_id, p.codigo::text, p.nombre_cientifico::text,
                           p.nombre_espanol::text, e.acuario_id, a.codigo::text, e.condicion::text, e.cantidad,
                           l.fecha_ingreso, (p_hoy - l.fecha_ingreso)::integer, pv.nombre::text, c.nombre::text
                    FROM existencias e
                    JOIN lotes l ON l.id = e.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    LEFT JOIN acuarios a ON a.id = e.acuario_id
                    LEFT JOIN proveedores pv ON pv.id = l.proveedor_id
                    LEFT JOIN centros_acopio c ON c.id = l.centro_acopio_id
                    WHERE e.cantidad > 0
                      AND (p_producto_id IS NULL OR l.producto_id = p_producto_id)
                      AND (p_lote_id IS NULL OR e.lote_id = p_lote_id)
                      AND (p_acuario_id IS NULL OR e.acuario_id = p_acuario_id)
                    ORDER BY p.codigo, l.fecha_ingreso, a.codigo NULLS FIRST, e.condicion;
                $$;
                """);

            // Lotes con su trazabilidad: lo que queda (sano/enfermo/por ubicar), lo despachado y lo muerto.
            const string columnasLote =
                """
                RETURNS TABLE (id uuid, codigo text, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, proveedor_id uuid, proveedor_nombre text, centro_acopio_id uuid,
                               centro_nombre text, recepcion_codigo text, fecha_ingreso date, dias_en_bodega integer,
                               costo_unitario numeric, recibidos integer, muertos_llegada integer, origen text,
                               sanos integer, enfermos integer, por_ubicar integer, despachados integer, muertos integer,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.codigo::text, l.producto_id, p.codigo::text, p.nombre_cientifico::text,
                           p.nombre_espanol::text, l.proveedor_id, pv.nombre::text, l.centro_acopio_id,
                           c.nombre::text, r.codigo::text, l.fecha_ingreso, (p_hoy - l.fecha_ingreso)::integer,
                           l.costo_unitario, l.recibidos, l.muertos_llegada, l.origen::text,
                           coalesce((SELECT sum(cantidad) FROM existencias WHERE lote_id = l.id AND condicion = 'sano' AND acuario_id IS NOT NULL), 0)::integer,
                           coalesce((SELECT sum(cantidad) FROM existencias WHERE lote_id = l.id AND condicion = 'enfermo'), 0)::integer,
                           coalesce((SELECT sum(cantidad) FROM existencias WHERE lote_id = l.id AND condicion = 'sano' AND acuario_id IS NULL), 0)::integer,
                           coalesce((SELECT sum(cantidad) FROM movimientos_inventario WHERE lote_id = l.id AND tipo = 'despacho'), 0)::integer,
                           (l.muertos_llegada + coalesce((SELECT sum(cantidad) FROM movimientos_inventario WHERE lote_id = l.id AND tipo = 'mortalidad'), 0))::integer,
                           l.creado_en
                    FROM lotes l
                    JOIN productos p ON p.id = l.producto_id
                    LEFT JOIN proveedores pv ON pv.id = l.proveedor_id
                    LEFT JOIN centros_acopio c ON c.id = l.centro_acopio_id
                    LEFT JOIN recepciones_lineas rl ON rl.id = l.recepcion_linea_id
                    LEFT JOIN recepciones r ON r.id = rl.recepcion_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_lotes_listar(p_hoy date, p_producto_id uuid, p_solo_con_stock boolean)
                {columnasLote}
                    WHERE (p_producto_id IS NULL OR l.producto_id = p_producto_id)
                      AND (NOT p_solo_con_stock OR EXISTS (SELECT 1 FROM existencias e WHERE e.lote_id = l.id AND e.cantidad > 0))
                    ORDER BY l.fecha_ingreso DESC, l.codigo DESC;
                $$;

                CREATE FUNCTION fn_lote_por_id(p_id uuid, p_hoy date)
                {columnasLote}
                    WHERE l.id = p_id;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_movimientos_listar(p_lote_id uuid, p_limite integer)
                RETURNS TABLE (id uuid, tipo text, lote_id uuid, lote_codigo text, producto_codigo text, nombre_cientifico text,
                               acuario_origen_codigo text, condicion_origen text, acuario_destino_codigo text,
                               condicion_destino text, cantidad integer, pedido_codigo text, recepcion_codigo text,
                               nota text, usuario_nombre text, fecha timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT m.id, m.tipo::text, m.lote_id, l.codigo::text, p.codigo::text, p.nombre_cientifico::text,
                           ao.codigo::text, m.condicion_origen::text, ad.codigo::text, m.condicion_destino::text, m.cantidad,
                           pc.codigo::text, r.codigo::text, m.nota::text, m.usuario_nombre::text, m.fecha
                    FROM movimientos_inventario m
                    JOIN lotes l ON l.id = m.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    LEFT JOIN acuarios ao ON ao.id = m.acuario_origen_id
                    LEFT JOIN acuarios ad ON ad.id = m.acuario_destino_id
                    LEFT JOIN pedidos_cliente_lineas pcl ON pcl.id = m.pedido_cliente_linea_id
                    LEFT JOIN pedidos_cliente pc ON pc.id = pcl.pedido_id
                    LEFT JOIN recepciones_lineas rl ON rl.id = m.recepcion_linea_id
                    LEFT JOIN recepciones r ON r.id = rl.recepcion_id
                    WHERE p_lote_id IS NULL OR m.lote_id = p_lote_id
                    ORDER BY m.fecha DESC, m.id DESC
                    LIMIT p_limite;
                $$;
                """);
        }

        private static void CrearAcuarios(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, sector text, nivel integer, posicion integer, capacidad_litros numeric,
                               activo boolean, sanos integer, enfermos integer, lotes integer)
                LANGUAGE sql STABLE AS $$
                    SELECT a.id, a.codigo::text, a.sector::text, a.nivel, a.posicion, a.capacidad_litros, a.activo,
                           coalesce(sum(e.cantidad) FILTER (WHERE e.condicion = 'sano'), 0)::integer,
                           coalesce(sum(e.cantidad) FILTER (WHERE e.condicion = 'enfermo'), 0)::integer,
                           count(DISTINCT e.lote_id)::integer
                    FROM acuarios a
                    LEFT JOIN existencias e ON e.acuario_id = a.id AND e.cantidad > 0
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_acuarios_listar()
                {columnas}
                    GROUP BY a.id
                    ORDER BY a.sector, a.nivel, a.posicion, a.codigo;
                $$;

                CREATE FUNCTION fn_acuario_por_id(p_id uuid)
                {columnas}
                    WHERE a.id = p_id
                    GROUP BY a.id;
                $$;

                CREATE FUNCTION fn_acuario_codigo_existe(p_codigo text, p_excluir_id uuid)
                RETURNS boolean
                LANGUAGE sql STABLE AS $$
                    SELECT EXISTS (SELECT 1 FROM acuarios WHERE upper(codigo) = upper(btrim(p_codigo))
                                   AND (p_excluir_id IS NULL OR id <> p_excluir_id));
                $$;

                CREATE PROCEDURE sp_acuario_guardar(p_id uuid, p_es_nuevo boolean, p_codigo text, p_sector text, p_nivel integer,
                                                    p_posicion integer, p_capacidad_litros numeric, p_activo boolean,
                                                    p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO acuarios (id, codigo, sector, nivel, posicion, capacidad_litros, activo, creado_en)
                        VALUES (p_id, upper(p_codigo), upper(p_sector), p_nivel, p_posicion, p_capacidad_litros, p_activo, p_fecha);
                    ELSE
                        UPDATE acuarios SET sector = upper(p_sector), nivel = p_nivel, posicion = p_posicion,
                                            capacidad_litros = p_capacidad_litros, activo = p_activo, actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;
                END;
                $$;
                """);
        }

        private static void CrearCompras(MigrationBuilder migrationBuilder)
        {
            // --- Ofertas (pre-disponibilidad) ---
            const string columnasOferta =
                """
                RETURNS TABLE (id uuid, codigo text, proveedor_id uuid, proveedor_nombre text, fecha date,
                               fecha_estimada_llegada date, estado text, observaciones text, lineas integer, unidades integer,
                               creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT o.id, o.codigo::text, o.proveedor_id, pv.nombre::text, o.fecha, o.fecha_estimada_llegada,
                           o.estado::text, o.observaciones::text,
                           (SELECT count(*)::integer FROM ofertas_proveedor_lineas WHERE oferta_id = o.id),
                           coalesce((SELECT sum(cantidad) FROM ofertas_proveedor_lineas WHERE oferta_id = o.id), 0)::integer,
                           o.creado_en
                    FROM ofertas_proveedor o
                    JOIN proveedores pv ON pv.id = o.proveedor_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_ofertas_listar()
                {columnasOferta}
                    ORDER BY o.fecha DESC, o.codigo DESC;
                $$;

                CREATE FUNCTION fn_oferta_por_id(p_id uuid)
                {columnasOferta}
                    WHERE o.id = p_id;
                $$;

                CREATE FUNCTION fn_oferta_lineas(p_oferta_id uuid)
                RETURNS TABLE (id uuid, oferta_id uuid, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, cantidad integer)
                LANGUAGE sql STABLE AS $$
                    SELECT ol.id, ol.oferta_id, ol.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           ol.cantidad
                    FROM ofertas_proveedor_lineas ol
                    JOIN productos p ON p.id = ol.producto_id
                    WHERE ol.oferta_id = p_oferta_id
                    ORDER BY p.codigo;
                $$;

                CREATE PROCEDURE sp_oferta_guardar(p_id uuid, p_es_nueva boolean, p_codigo text, p_proveedor_id uuid, p_fecha date,
                                                   p_fecha_estimada_llegada date, p_estado text, p_observaciones text,
                                                   p_lineas jsonb, p_fecha_registro timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nueva THEN
                        INSERT INTO ofertas_proveedor (id, codigo, proveedor_id, fecha, fecha_estimada_llegada, estado, observaciones, creado_en)
                        VALUES (p_id, p_codigo, p_proveedor_id, p_fecha, p_fecha_estimada_llegada, p_estado, p_observaciones, p_fecha_registro);
                    ELSE
                        UPDATE ofertas_proveedor SET proveedor_id = p_proveedor_id, fecha = p_fecha,
                                                     fecha_estimada_llegada = p_fecha_estimada_llegada, estado = p_estado,
                                                     observaciones = p_observaciones, actualizado_en = p_fecha_registro
                        WHERE id = p_id;
                    END IF;

                    DELETE FROM ofertas_proveedor_lineas WHERE oferta_id = p_id;
                    INSERT INTO ofertas_proveedor_lineas (id, oferta_id, producto_id, cantidad)
                    SELECT gen_random_uuid(), p_id, (e->>'producto')::uuid, (e->>'cantidad')::integer
                    FROM jsonb_array_elements(p_lineas) e;
                END;
                $$;
                """);

            // --- Pedidos a proveedor ---
            const string columnasPedido =
                """
                RETURNS TABLE (id uuid, codigo text, proveedor_id uuid, proveedor_nombre text, oferta_id uuid, oferta_codigo text,
                               fecha date, fecha_estimada_llegada date, estado text, observaciones text, unidades integer,
                               recibidas integer, costo_total numeric, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT pp.id, pp.codigo::text, pp.proveedor_id, pv.nombre::text, pp.oferta_id, o.codigo::text,
                           pp.fecha, pp.fecha_estimada_llegada, pp.estado::text, pp.observaciones::text,
                           coalesce((SELECT sum(cantidad) FROM pedidos_proveedor_lineas WHERE pedido_id = pp.id), 0)::integer,
                           coalesce((SELECT sum(rl.recibida) FROM recepciones_lineas rl JOIN recepciones r ON r.id = rl.recepcion_id
                                     WHERE r.pedido_proveedor_id = pp.id), 0)::integer,
                           (SELECT sum(cantidad * costo_unitario) FROM pedidos_proveedor_lineas WHERE pedido_id = pp.id),
                           pp.creado_en
                    FROM pedidos_proveedor pp
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    LEFT JOIN ofertas_proveedor o ON o.id = pp.oferta_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_pedidos_proveedor_listar()
                {columnasPedido}
                    ORDER BY pp.fecha DESC, pp.codigo DESC;
                $$;

                CREATE FUNCTION fn_pedido_proveedor_por_id(p_id uuid)
                {columnasPedido}
                    WHERE pp.id = p_id;
                $$;

                CREATE FUNCTION fn_pedido_proveedor_lineas(p_pedido_id uuid)
                RETURNS TABLE (id uuid, pedido_id uuid, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, cantidad integer, costo_unitario numeric, observacion text, recibida integer)
                LANGUAGE sql STABLE AS $$
                    SELECT pl.id, pl.pedido_id, pl.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           pl.cantidad, pl.costo_unitario, pl.observacion::text,
                           coalesce((SELECT sum(recibida) FROM recepciones_lineas WHERE pedido_linea_id = pl.id), 0)::integer
                    FROM pedidos_proveedor_lineas pl
                    JOIN productos p ON p.id = pl.producto_id
                    WHERE pl.pedido_id = p_pedido_id
                    ORDER BY p.codigo;
                $$;

                -- Reemplaza las líneas (solo antes de la primera recepción: lo valida el servicio). Sin permiso de
                -- costos (p_actualizar_costos = false) cada línea conserva el costo que tenía su producto.
                CREATE PROCEDURE sp_pedido_proveedor_guardar(p_id uuid, p_es_nuevo boolean, p_codigo text, p_proveedor_id uuid,
                                                             p_oferta_id uuid, p_fecha date, p_fecha_estimada_llegada date,
                                                             p_estado text, p_observaciones text, p_lineas jsonb,
                                                             p_actualizar_costos boolean, p_fecha_registro timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_costos jsonb;
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO pedidos_proveedor (id, codigo, proveedor_id, oferta_id, fecha, fecha_estimada_llegada, estado,
                                                       observaciones, creado_en)
                        VALUES (p_id, p_codigo, p_proveedor_id, p_oferta_id, p_fecha, p_fecha_estimada_llegada, p_estado,
                                p_observaciones, p_fecha_registro);
                    ELSE
                        UPDATE pedidos_proveedor SET proveedor_id = p_proveedor_id, oferta_id = p_oferta_id, fecha = p_fecha,
                                                     fecha_estimada_llegada = p_fecha_estimada_llegada,
                                                     observaciones = p_observaciones, actualizado_en = p_fecha_registro
                        WHERE id = p_id;
                    END IF;

                    -- Costos actuales por producto, para conservarlos si quien guarda no puede verlos.
                    SELECT coalesce(jsonb_object_agg(producto_id::text, costo_unitario), jsonb_build_object()) INTO v_costos
                    FROM pedidos_proveedor_lineas WHERE pedido_id = p_id;

                    DELETE FROM pedidos_proveedor_lineas WHERE pedido_id = p_id;
                    INSERT INTO pedidos_proveedor_lineas (id, pedido_id, producto_id, cantidad, costo_unitario, observacion)
                    SELECT gen_random_uuid(), p_id, (e->>'producto')::uuid, (e->>'cantidad')::integer,
                           CASE WHEN p_actualizar_costos THEN (e->>'costo')::numeric
                                ELSE (v_costos->>(e->>'producto'))::numeric END,
                           e->>'observacion'
                    FROM jsonb_array_elements(p_lineas) e;
                END;
                $$;

                CREATE PROCEDURE sp_pedido_proveedor_cambiar_estado(p_id uuid, p_esperado text, p_estado text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE pedidos_proveedor SET estado = p_estado, actualizado_en = p_fecha WHERE id = p_id AND estado = p_esperado;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El pedido cambió de estado mientras tanto.' USING ERRCODE = 'JR001';
                    END IF;
                END;
                $$;
                """);

            // --- Recepciones ---
            const string columnasRecepcion =
                """
                RETURNS TABLE (id uuid, codigo text, pedido_proveedor_id uuid, pedido_codigo text, proveedor_nombre text, fecha date,
                               observaciones text, usuario_nombre text, unidades integer, enfermas integer, muertas integer,
                               costo_total numeric, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT r.id, r.codigo::text, r.pedido_proveedor_id, pp.codigo::text, pv.nombre::text, r.fecha,
                           r.observaciones::text, u.nombre::text,
                           coalesce((SELECT sum(recibida) FROM recepciones_lineas WHERE recepcion_id = r.id), 0)::integer,
                           coalesce((SELECT sum(enferma) FROM recepciones_lineas WHERE recepcion_id = r.id), 0)::integer,
                           coalesce((SELECT sum(muerta) FROM recepciones_lineas WHERE recepcion_id = r.id), 0)::integer,
                           coalesce((SELECT sum(costo_total) FROM recepciones_lineas WHERE recepcion_id = r.id), 0),
                           r.creado_en
                    FROM recepciones r
                    JOIN pedidos_proveedor pp ON pp.id = r.pedido_proveedor_id
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    LEFT JOIN usuarios u ON u.id = r.usuario_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_recepciones_listar(p_pedido_id uuid)
                {columnasRecepcion}
                    WHERE p_pedido_id IS NULL OR r.pedido_proveedor_id = p_pedido_id
                    ORDER BY r.fecha DESC, r.codigo DESC;
                $$;

                CREATE FUNCTION fn_recepcion_lineas(p_recepcion_id uuid)
                RETURNS TABLE (id uuid, recepcion_id uuid, pedido_linea_id uuid, producto_id uuid, producto_codigo text,
                               nombre_cientifico text, recibida integer, enferma integer, muerta integer, costo_unitario numeric,
                               costo_total numeric, lote_id uuid, lote_codigo text)
                LANGUAGE sql STABLE AS $$
                    SELECT rl.id, rl.recepcion_id, rl.pedido_linea_id, rl.producto_id, p.codigo::text, p.nombre_cientifico::text,
                           rl.recibida, rl.enferma, rl.muerta, rl.costo_unitario, rl.costo_total, rl.lote_id, l.codigo::text
                    FROM recepciones_lineas rl
                    JOIN productos p ON p.id = rl.producto_id
                    JOIN lotes l ON l.id = rl.lote_id
                    WHERE rl.recepcion_id = p_recepcion_id
                    ORDER BY p.codigo;
                $$;
                """);

            // Registrar la llegada: por línea crea el lote y mete el stock vivo "por ubicar" (sano y enfermo).
            // Los muertos al llegar quedan en el lote (muertos_llegada) para la trazabilidad, no entran al stock.
            // p_lineas: [{id, pedidoLinea, producto, recibida, enferma, muerta, costo, lote, loteCodigo}]
            migrationBuilder.Sql(
                """
                CREATE PROCEDURE sp_recepcion_registrar(
                    p_id uuid, p_codigo text, p_pedido_id uuid, p_estado_esperado text, p_fecha date, p_observaciones text,
                    p_usuario_id uuid, p_usuario_nombre text, p_lineas jsonb, p_fecha_registro timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_pedido pedidos_proveedor%ROWTYPE;
                    e jsonb;
                    v_sanos integer;
                    v_pendiente integer;
                    v_centro uuid;
                BEGIN
                    SELECT * INTO v_pedido FROM pedidos_proveedor WHERE id = p_pedido_id FOR UPDATE;
                    IF v_pedido.estado IS DISTINCT FROM p_estado_esperado THEN
                        RAISE EXCEPTION 'El pedido cambió de estado mientras tanto.' USING ERRCODE = 'JR001';
                    END IF;

                    INSERT INTO recepciones (id, codigo, pedido_proveedor_id, fecha, observaciones, usuario_id, creado_en)
                    VALUES (p_id, p_codigo, p_pedido_id, p_fecha, p_observaciones, p_usuario_id, p_fecha_registro);

                    FOR e IN SELECT * FROM jsonb_array_elements(p_lineas) LOOP
                        -- CALL no admite subconsultas como argumento: el centro (el primero del producto) va en variable.
                        SELECT centro_acopio_id INTO v_centro FROM producto_centros_acopio
                        WHERE producto_id = (e->>'producto')::uuid ORDER BY orden LIMIT 1;

                        CALL sp_lote_crear(
                            (e->>'lote')::uuid, e->>'loteCodigo', (e->>'producto')::uuid, v_pedido.proveedor_id, v_centro,
                            NULL, p_fecha, (e->>'costo')::numeric, (e->>'recibida')::integer, (e->>'muerta')::integer,
                            'recepcion', p_fecha_registro);

                        INSERT INTO recepciones_lineas (id, recepcion_id, pedido_linea_id, producto_id, recibida, enferma, muerta,
                                                        costo_unitario, lote_id)
                        VALUES ((e->>'id')::uuid, p_id, (e->>'pedidoLinea')::uuid, (e->>'producto')::uuid,
                                (e->>'recibida')::integer, (e->>'enferma')::integer, (e->>'muerta')::integer,
                                (e->>'costo')::numeric, (e->>'lote')::uuid);

                        UPDATE lotes SET recepcion_linea_id = (e->>'id')::uuid WHERE id = (e->>'lote')::uuid;

                        v_sanos := (e->>'recibida')::integer - (e->>'enferma')::integer - (e->>'muerta')::integer;
                        IF v_sanos > 0 THEN
                            CALL sp_inventario_mover_uno((e->>'lote')::uuid, NULL, NULL, NULL, 'sano', v_sanos, 'entrada',
                                                         NULL, (e->>'id')::uuid, NULL, p_usuario_id, p_usuario_nombre, p_fecha_registro);
                        END IF;
                        IF (e->>'enferma')::integer > 0 THEN
                            CALL sp_inventario_mover_uno((e->>'lote')::uuid, NULL, NULL, NULL, 'enfermo', (e->>'enferma')::integer,
                                                         'entrada', NULL, (e->>'id')::uuid, 'Llegó enfermo', p_usuario_id,
                                                         p_usuario_nombre, p_fecha_registro);
                        END IF;
                    END LOOP;

                    -- Pendiente de todo el pedido: si ya llegó todo, recibido; si no, recibido parcial.
                    SELECT coalesce(sum(greatest(pl.cantidad - coalesce(
                               (SELECT sum(rl.recibida) FROM recepciones_lineas rl WHERE rl.pedido_linea_id = pl.id), 0), 0)), 0)
                    INTO v_pendiente
                    FROM pedidos_proveedor_lineas pl WHERE pl.pedido_id = p_pedido_id;

                    UPDATE pedidos_proveedor
                    SET estado = CASE WHEN v_pendiente = 0 THEN 'recibido' ELSE 'recibido_parcial' END,
                        actualizado_en = p_fecha_registro
                    WHERE id = p_pedido_id;
                END;
                $$;
                """);
        }

        private static void CrearPedidosCliente(MigrationBuilder migrationBuilder)
        {
            const string columnas =
                """
                RETURNS TABLE (id uuid, codigo text, cliente_id uuid, cliente_nombre text, comercial_id uuid, comercial_nombre text,
                               venta_id uuid, venta_codigo text, moneda text, fecha date, ciudad text, estado text,
                               observaciones text, unidades integer, despachadas integer, total numeric, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT pc.id, pc.codigo::text, pc.cliente_id, c.nombre_empresa::text, pc.comercial_id, u.nombre::text,
                           pc.venta_id, v.codigo::text, v.moneda::text, pc.fecha, pc.ciudad::text, pc.estado::text,
                           pc.observaciones::text,
                           coalesce((SELECT sum(cantidad) FROM pedidos_cliente_lineas WHERE pedido_id = pc.id), 0)::integer,
                           coalesce((SELECT sum(m.cantidad) FROM movimientos_inventario m
                                     JOIN pedidos_cliente_lineas l ON l.id = m.pedido_cliente_linea_id
                                     WHERE l.pedido_id = pc.id AND m.tipo = 'despacho'), 0)::integer,
                           (SELECT sum(cantidad * precio_unitario) FROM pedidos_cliente_lineas WHERE pedido_id = pc.id),
                           pc.creado_en
                    FROM pedidos_cliente pc
                    JOIN clientes c ON c.id = pc.cliente_id
                    JOIN usuarios u ON u.id = pc.comercial_id
                    LEFT JOIN ventas v ON v.id = pc.venta_id
                """;

            migrationBuilder.Sql(
                $"""
                CREATE FUNCTION fn_pedidos_cliente_listar(p_comercial_id uuid)
                {columnas}
                    WHERE p_comercial_id IS NULL OR pc.comercial_id = p_comercial_id
                    ORDER BY pc.fecha DESC, pc.codigo DESC;
                $$;

                CREATE FUNCTION fn_pedido_cliente_por_id(p_id uuid)
                {columnas}
                    WHERE pc.id = p_id;
                $$;

                CREATE FUNCTION fn_pedido_cliente_por_venta(p_venta_id uuid)
                {columnas}
                    WHERE pc.venta_id = p_venta_id;
                $$;

                CREATE FUNCTION fn_pedido_cliente_lineas(p_pedido_id uuid)
                RETURNS TABLE (id uuid, pedido_id uuid, producto_id uuid, producto_codigo text, nombre_cientifico text,
                               nombre_espanol text, cantidad integer, precio_unitario numeric, observacion text, despachada integer)
                LANGUAGE sql STABLE AS $$
                    SELECT l.id, l.pedido_id, l.producto_id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           l.cantidad, l.precio_unitario, l.observacion::text,
                           coalesce((SELECT sum(cantidad) FROM movimientos_inventario WHERE pedido_cliente_linea_id = l.id
                                     AND tipo = 'despacho'), 0)::integer
                    FROM pedidos_cliente_lineas l
                    JOIN productos p ON p.id = l.producto_id
                    WHERE l.pedido_id = p_pedido_id
                    ORDER BY p.codigo;
                $$;

                -- Upsert de líneas por id; las que no vienen se borran (el servicio no deja borrar una línea con despachos).
                -- p_lineas: arreglo de objetos con id, producto, cantidad, precio, observacion.
                CREATE PROCEDURE sp_pedido_cliente_guardar(p_id uuid, p_es_nuevo boolean, p_codigo text, p_cliente_id uuid,
                                                           p_comercial_id uuid, p_venta_id uuid, p_fecha date, p_ciudad text,
                                                           p_estado text, p_observaciones text, p_lineas jsonb,
                                                           p_fecha_registro timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO pedidos_cliente (id, codigo, cliente_id, comercial_id, venta_id, fecha, ciudad, estado,
                                                     observaciones, creado_en)
                        VALUES (p_id, p_codigo, p_cliente_id, p_comercial_id, p_venta_id, p_fecha, p_ciudad, p_estado,
                                p_observaciones, p_fecha_registro);
                    ELSE
                        UPDATE pedidos_cliente SET cliente_id = p_cliente_id, comercial_id = p_comercial_id, fecha = p_fecha,
                                                   ciudad = p_ciudad, observaciones = p_observaciones,
                                                   actualizado_en = p_fecha_registro
                        WHERE id = p_id;
                    END IF;

                    DELETE FROM pedidos_cliente_lineas
                    WHERE pedido_id = p_id
                      AND id NOT IN (SELECT (e->>'id')::uuid FROM jsonb_array_elements(p_lineas) e);

                    INSERT INTO pedidos_cliente_lineas (id, pedido_id, producto_id, cantidad, precio_unitario, observacion)
                    SELECT (e->>'id')::uuid, p_id, (e->>'producto')::uuid, (e->>'cantidad')::integer,
                           (e->>'precio')::numeric, e->>'observacion'
                    FROM jsonb_array_elements(p_lineas) e
                    ON CONFLICT (id) DO UPDATE SET producto_id = EXCLUDED.producto_id, cantidad = EXCLUDED.cantidad,
                                                   precio_unitario = EXCLUDED.precio_unitario, observacion = EXCLUDED.observacion;
                END;
                $$;

                -- Cambia el estado y sincroniza la preparación de la venta del CRM enlazada.
                CREATE PROCEDURE sp_pedido_cliente_cambiar_estado(p_id uuid, p_esperado text, p_estado text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_venta uuid;
                BEGIN
                    UPDATE pedidos_cliente SET estado = p_estado, actualizado_en = p_fecha
                    WHERE id = p_id AND estado = p_esperado
                    RETURNING venta_id INTO v_venta;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El pedido cambió de estado mientras tanto.' USING ERRCODE = 'JR001';
                    END IF;

                    IF v_venta IS NOT NULL THEN
                        UPDATE ventas SET estado_preparacion = CASE p_estado WHEN 'en_preparacion' THEN 'en_empaque'
                                                                             WHEN 'despachado' THEN 'listo_despacho' END,
                                          actualizado_en = p_fecha
                        WHERE id = v_venta AND p_estado IN ('en_preparacion', 'despachado') AND estado_preparacion <> 'entregado';
                    END IF;
                END;
                $$;
                """);

            // Despacho (picking): cada salida es sano → nada contra una línea del pedido; no supera lo pendiente.
            // Al final: todo despachado → «despachado»; si no, «en preparación». La venta enlazada se sincroniza.
            // p_salidas: [{linea, lote, acuario, cantidad}]
            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE sp_pedido_cliente_despachar(p_pedido_id uuid, p_salidas jsonb, p_usuario_id uuid,
                                                             p_usuario_nombre text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_estado text;
                    v_codigo text;
                    s jsonb;
                    v_linea pedidos_cliente_lineas%ROWTYPE;
                    v_despachada integer;
                    v_pendiente integer;
                    v_nuevo text;
                BEGIN
                    SELECT estado, codigo INTO v_estado, v_codigo FROM pedidos_cliente WHERE id = p_pedido_id FOR UPDATE;
                    IF v_estado NOT IN ('confirmado', 'en_preparacion') THEN
                        RAISE EXCEPTION 'Solo se despachan pedidos confirmados o en preparación.' USING ERRCODE = 'JR001';
                    END IF;

                    FOR s IN SELECT * FROM jsonb_array_elements(p_salidas) LOOP
                        SELECT * INTO v_linea FROM pedidos_cliente_lineas
                        WHERE id = (s->>'linea')::uuid AND pedido_id = p_pedido_id FOR UPDATE;
                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'La línea no pertenece al pedido.';
                        END IF;

                        IF NOT EXISTS (SELECT 1 FROM lotes WHERE id = (s->>'lote')::uuid AND producto_id = v_linea.producto_id) THEN
                            RAISE EXCEPTION 'El lote no es del producto de la línea.';
                        END IF;

                        SELECT coalesce(sum(cantidad), 0) INTO v_despachada FROM movimientos_inventario
                        WHERE pedido_cliente_linea_id = v_linea.id AND tipo = 'despacho';
                        IF v_despachada + (s->>'cantidad')::integer > v_linea.cantidad THEN
                            RAISE EXCEPTION 'Solo quedan % por despachar en esa línea.', v_linea.cantidad - v_despachada
                                USING ERRCODE = '{SqlStateSuperaPendiente}';
                        END IF;

                        CALL sp_inventario_mover_uno((s->>'lote')::uuid, (s->>'acuario')::uuid, 'sano', NULL, NULL,
                                                     (s->>'cantidad')::integer, 'despacho', v_linea.id, NULL,
                                                     'Pedido ' || v_codigo, p_usuario_id, p_usuario_nombre, p_fecha);
                    END LOOP;

                    SELECT coalesce(sum(greatest(l.cantidad - coalesce(
                               (SELECT sum(m.cantidad) FROM movimientos_inventario m
                                WHERE m.pedido_cliente_linea_id = l.id AND m.tipo = 'despacho'), 0), 0)), 0)
                    INTO v_pendiente
                    FROM pedidos_cliente_lineas l WHERE l.pedido_id = p_pedido_id;

                    v_nuevo := CASE WHEN v_pendiente = 0 THEN 'despachado' ELSE 'en_preparacion' END;
                    IF v_nuevo <> v_estado THEN
                        CALL sp_pedido_cliente_cambiar_estado(p_pedido_id, v_estado, v_nuevo, p_fecha);
                    END IF;
                END;
                $$;
                """);
        }

        private static void CrearResumenes(MigrationBuilder migrationBuilder)
        {
            // Por producto: stock (sano ubicado, por ubicar, enfermo), demanda pendiente de clientes, en tránsito
            // (pedidos a proveedor sin recibir) y la última oferta abierta. Base del dashboard y de las necesidades.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_inventario_por_producto()
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_espanol text,
                               centro_acopio_id uuid, centro_nombre text, sanos integer, por_ubicar integer, enfermos integer,
                               pendiente_clientes integer, en_transito integer, ultima_oferta_proveedor text,
                               ultima_oferta_cantidad integer, ultima_oferta_fecha date)
                LANGUAGE sql STABLE AS $$
                    WITH stock AS (
                        SELECT l.producto_id,
                               sum(e.cantidad) FILTER (WHERE e.condicion = 'sano' AND e.acuario_id IS NOT NULL) AS sanos,
                               sum(e.cantidad) FILTER (WHERE e.condicion = 'sano' AND e.acuario_id IS NULL) AS por_ubicar,
                               sum(e.cantidad) FILTER (WHERE e.condicion = 'enfermo') AS enfermos
                        FROM existencias e JOIN lotes l ON l.id = e.lote_id
                        GROUP BY l.producto_id),
                    demanda AS (
                        SELECT l.producto_id, sum(greatest(l.cantidad - coalesce(
                                   (SELECT sum(m.cantidad) FROM movimientos_inventario m
                                    WHERE m.pedido_cliente_linea_id = l.id AND m.tipo = 'despacho'), 0), 0)) AS pendiente
                        FROM pedidos_cliente_lineas l JOIN pedidos_cliente pc ON pc.id = l.pedido_id
                        WHERE pc.estado IN ('confirmado', 'en_preparacion')
                        GROUP BY l.producto_id),
                    transito AS (
                        SELECT pl.producto_id, sum(greatest(pl.cantidad - coalesce(
                                   (SELECT sum(rl.recibida) FROM recepciones_lineas rl WHERE rl.pedido_linea_id = pl.id), 0), 0)) AS cantidad
                        FROM pedidos_proveedor_lineas pl JOIN pedidos_proveedor pp ON pp.id = pl.pedido_id
                        WHERE pp.estado IN ('enviado', 'en_transito', 'recibido_parcial')
                        GROUP BY pl.producto_id),
                    ofertas AS (
                        SELECT DISTINCT ON (ol.producto_id) ol.producto_id, pv.nombre AS proveedor, ol.cantidad, o.fecha
                        FROM ofertas_proveedor_lineas ol
                        JOIN ofertas_proveedor o ON o.id = ol.oferta_id AND o.estado = 'abierta'
                        JOIN proveedores pv ON pv.id = o.proveedor_id
                        ORDER BY ol.producto_id, o.fecha DESC, o.codigo DESC),
                    centros AS (
                        SELECT DISTINCT ON (pc.producto_id) pc.producto_id, pc.centro_acopio_id, c.nombre
                        FROM producto_centros_acopio pc JOIN centros_acopio c ON c.id = pc.centro_acopio_id
                        ORDER BY pc.producto_id, pc.orden)
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, p.nombre_espanol::text,
                           ce.centro_acopio_id, ce.nombre::text,
                           coalesce(s.sanos, 0)::integer, coalesce(s.por_ubicar, 0)::integer, coalesce(s.enfermos, 0)::integer,
                           coalesce(d.pendiente, 0)::integer, coalesce(t.cantidad, 0)::integer,
                           o.proveedor::text, o.cantidad, o.fecha
                    FROM productos p
                    LEFT JOIN stock s ON s.producto_id = p.id
                    LEFT JOIN demanda d ON d.producto_id = p.id
                    LEFT JOIN transito t ON t.producto_id = p.id
                    LEFT JOIN ofertas o ON o.producto_id = p.id
                    LEFT JOIN centros ce ON ce.producto_id = p.id
                    WHERE p.eliminado_en IS NULL
                      AND (s.producto_id IS NOT NULL OR d.producto_id IS NOT NULL OR t.producto_id IS NOT NULL OR o.producto_id IS NOT NULL)
                    ORDER BY p.codigo;
                $$;
                """);

            // Especies más vendidas (despachos) y proveedores a los que más se compra (recepciones) en un periodo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_inventario_despachos_por_producto(p_desde timestamptz, p_hasta timestamptz)
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, cantidad integer)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, sum(m.cantidad)::integer
                    FROM movimientos_inventario m
                    JOIN lotes l ON l.id = m.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    WHERE m.tipo = 'despacho' AND m.fecha >= p_desde AND m.fecha < p_hasta
                    GROUP BY p.id
                    ORDER BY 4 DESC
                    LIMIT 10;
                $$;

                CREATE FUNCTION fn_inventario_compras_por_proveedor(p_desde date, p_hasta date)
                RETURNS TABLE (proveedor_id uuid, proveedor_nombre text, recepciones integer, unidades integer, costo_total numeric,
                               pedidas integer)
                LANGUAGE sql STABLE AS $$
                    SELECT pv.id, pv.nombre::text, count(DISTINCT r.id)::integer, sum(rl.recibida)::integer, sum(rl.costo_total),
                           sum(pl.cantidad)::integer
                    FROM recepciones r
                    JOIN recepciones_lineas rl ON rl.recepcion_id = r.id
                    JOIN pedidos_proveedor_lineas pl ON pl.id = rl.pedido_linea_id
                    JOIN pedidos_proveedor pp ON pp.id = r.pedido_proveedor_id
                    JOIN proveedores pv ON pv.id = pp.proveedor_id
                    WHERE r.fecha BETWEEN p_desde AND p_hasta
                    GROUP BY pv.id
                    ORDER BY 4 DESC;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var objeto in new[]
                     {
                         "FUNCTION fn_inventario_compras_por_proveedor", "FUNCTION fn_inventario_despachos_por_producto",
                         "FUNCTION fn_inventario_por_producto",
                         "PROCEDURE sp_pedido_cliente_despachar", "PROCEDURE sp_pedido_cliente_cambiar_estado",
                         "PROCEDURE sp_pedido_cliente_guardar", "FUNCTION fn_pedido_cliente_lineas", "FUNCTION fn_pedido_cliente_por_venta",
                         "FUNCTION fn_pedido_cliente_por_id", "FUNCTION fn_pedidos_cliente_listar",
                         "PROCEDURE sp_recepcion_registrar", "FUNCTION fn_recepcion_lineas", "FUNCTION fn_recepciones_listar",
                         "PROCEDURE sp_pedido_proveedor_cambiar_estado", "PROCEDURE sp_pedido_proveedor_guardar",
                         "FUNCTION fn_pedido_proveedor_lineas", "FUNCTION fn_pedido_proveedor_por_id", "FUNCTION fn_pedidos_proveedor_listar",
                         "PROCEDURE sp_oferta_guardar", "FUNCTION fn_oferta_lineas", "FUNCTION fn_oferta_por_id", "FUNCTION fn_ofertas_listar",
                         "PROCEDURE sp_acuario_guardar", "FUNCTION fn_acuario_codigo_existe", "FUNCTION fn_acuario_por_id",
                         "FUNCTION fn_acuarios_listar",
                         "FUNCTION fn_movimientos_listar", "FUNCTION fn_lote_por_id", "FUNCTION fn_lotes_listar",
                         "FUNCTION fn_existencias_listar",
                         "FUNCTION fn_lote_codigo_existe", "PROCEDURE sp_lote_crear", "PROCEDURE sp_inventario_mover",
                         "PROCEDURE sp_inventario_mover_uno",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }

            migrationBuilder.Sql("DELETE FROM consecutivos WHERE prefijo IN ('DISP', 'PP', 'REC', 'LOT', 'PC');");
        }
    }
}
