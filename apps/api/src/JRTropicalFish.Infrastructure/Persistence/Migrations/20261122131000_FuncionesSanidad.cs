using JRTropicalFish.Infrastructure.Persistence.Sembrado;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Funciones de sanidad (medicación, cuarentena, alimentación) y reservas de despacho.
    ///
    /// <para>
    /// Siembra los medicamentos de los reportes AQUAVET (ene–feb 2026) con su dosis de referencia y la fuente,
    /// y un protocolo <b>en borrador</b> con las recomendaciones generales: un protocolo solo se sugiere aprobado,
    /// y lo aprueba el veterinario o gerencia (cada reporte vale solo para el grupo muestreado).
    /// </para>
    ///
    /// <para>
    /// Rehace <c>sp_pedido_cliente_despachar</c> (misma firma): rechaza acuarios en tratamiento (<c>JR004</c>),
    /// respeta lo reservado para otros pedidos y consume la reserva de la línea.
    /// </para>
    ///
    /// <para>El orden de columnas de cada RETURNS TABLE = el record en <c>Application/Sanidad/SanidadModels.cs</c>.</para>
    /// </summary>
    public partial class FuncionesSanidad : Migration
    {
        /// <summary>Acuario en tratamiento o dentro del tiempo de retiro: no se reserva ni se despacha.</summary>
        public const string SqlStateEnTratamiento = "JR004";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SembradoPermisos.Aplicar(migrationBuilder);
            Sembrar(migrationBuilder);
            CrearMaestros(migrationBuilder);
            CrearProtocolos(migrationBuilder);
            CrearTratamientos(migrationBuilder);
            CrearReservas(migrationBuilder);
            CrearTablero(migrationBuilder);
            migrationBuilder.Sql("DROP PROCEDURE sp_pedido_cliente_despachar;");
            migrationBuilder.Sql(DespacharConReservas);
        }

        private static void Sembrar(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO parametros_sanidad (id, horas_ayuno_vuelo) VALUES (1, 48) ON CONFLICT DO NOTHING;
                INSERT INTO consecutivos (prefijo, ultimo) VALUES ('TRT', 0), ('PRT', 1) ON CONFLICT DO NOTHING;

                INSERT INTO medicamentos (id, codigo, nombre, principio_activo, presentacion, unidad, tipo_dosis, dosis_cantidad,
                                          dosis_maxima, dosis_base, exposicion_minutos, requiere_ayuno, dias_retiro, indicaciones,
                                          contraindicaciones, fuente, activo, creado_en)
                SELECT gen_random_uuid(), v.codigo, v.nombre, v.principio, v.presentacion, v.unidad, v.tipo, v.cantidad, v.maxima,
                       v.base, v.exposicion, v.ayuno, 0, v.indicaciones, NULL, v.fuente, true, now()
                FROM (VALUES
                    ('LEV15', 'Levamisol 15 %', 'Levamisol', 'Solución 15 %', 'ml', 'por_volumen', 0.6, NULL::numeric, 100, NULL::integer, true,
                     'Camallanus cotti (nematodo). Baño de inmersión prolongado 24 a 48 h; repetir a los 15 días.',
                     'AQUAVET · Corydoras y P. altum, ene 2026'),
                    ('FENLAK', 'Fenlak (fenbendazol)', 'Fenbendazol', 'Solución', 'ml', 'por_volumen', 2, NULL, 100, NULL, true,
                     'Camallanus cotti. Baño prolongado 24 h; repetir a los 15 días. Para 80 L el veterinario indicó 1,8 mL.',
                     'AQUAVET · Corydoras y P. altum, ene 2026'),
                    ('MEBEN', 'Mebendazol', 'Mebendazol', NULL, 'mg', 'por_alimento', 200, NULL, 100, NULL, false,
                     'Camallanus cotti. En el alimento por 7 días.',
                     'AQUAVET · Corydoras y P. altum, ene 2026'),
                    ('PRAZI', 'Praziquantel', 'Praziquantel', NULL, 'mg', 'por_volumen', 250, NULL, 100, NULL, true,
                     'Clinostomum (trematodo). Baño prolongado 24 h; repetir a los 15 días. En Cucha/Pia: 500 mg/100 L, repetir a los 7 días.',
                     'AQUAVET · Corydoras, P. altum y Cucha, ene–feb 2026'),
                    ('METRO', 'Metronidazol', 'Metronidazol', 'Tableta 500 mg', 'mg', 'por_volumen', 500, NULL, 80, NULL, true,
                     'Spironucleus. Baño prolongado 72 h con cambio de agua y nueva dosis cada 24 h; repetir a los 15 días.',
                     'AQUAVET · Heros, ene 2026'),
                    ('CUSO4', 'Sulfato de cobre', 'Sulfato de cobre', NULL, 'mg', 'por_volumen', 0.1, NULL, 1, NULL, true,
                     'Ectoparásitos branquiales. Cada 24 h por 6 a 12 días, con cambio de agua diario del 50 %.',
                     'AQUAVET · Albina, Otocinclus y Punto oro, ene–feb 2026'),
                    ('CUPRA', 'Cupramine', 'Cobre quelado', 'Solución', 'ml', 'por_volumen', 1, NULL, 80, NULL, true,
                     'Repetir a las 48 h; 48 h después cambiar el 80 % del agua. En Cucha/Pia: 10 gotas/40 L.',
                     'AQUAVET · Albina, Otocinclus, Punto oro y Cucha, ene–feb 2026'),
                    ('ACRIF', 'Acriflavina', 'Acriflavina', NULL, 'mg', 'por_volumen', 2.5, 5, 1, 60, true,
                     'Baño de 30 a 60 minutos. El veterinario indicó 400 mg para 80 L (5 mg/L).',
                     'AQUAVET · Albina, Otocinclus y Punto oro, ene–feb 2026'),
                    ('FMC', 'FMC', 'Formalina, verde de malaquita y azul de metileno', 'Solución', 'ml', 'por_volumen', 3.8, NULL, 80, NULL, true,
                     'Día por medio, 5 aplicaciones, con aireación fuerte.',
                     'AQUAVET · Heros, ene 2026'),
                    ('KMNO4', 'Permanganato de potasio', 'Permanganato de potasio', NULL, 'g', 'por_volumen', 1, NULL, 100, 15, true,
                     'Baño de 15 minutos (Heros) a 1 hora (Panaque royal); repetir a los 7 días.',
                     'AQUAVET · Heros y Panaque royal, ene 2026'),
                    ('SAL', 'Sal marina', 'Cloruro de sodio', NULL, 'g', 'por_volumen', 1, NULL, 1, NULL, false,
                     '1 g por litro, previamente diluida, durante la medicación. Baños: 4 g/L (Heros) o 7 a 10 g/L por 20 minutos (Panaque royal).',
                     'AQUAVET · recomendación general de los reportes, ene–feb 2026'),
                    ('SULFA', 'Sulfa', 'Sulfonamida', NULL, 'mg', 'por_volumen', 250, NULL, 100, NULL, true,
                     'Cada 24 h por 3 a 5 días.',
                     'AQUAVET · Panaque ojo azul, feb 2026'),
                    ('STRESS', 'Stress Guard', NULL, 'Solución', 'ml', 'por_volumen', 5, NULL, 40, NULL, false,
                     'Acondicionador antiestrés.',
                     'AQUAVET · Panaque ojo azul, feb 2026')
                ) AS v(codigo, nombre, principio, presentacion, unidad, tipo, cantidad, maxima, base, exposicion, ayuno, indicaciones, fuente)
                ON CONFLICT (codigo) DO NOTHING;

                INSERT INTO protocolos (id, codigo, nombre, descripcion, estado, dias_cuarentena, ayuno_inicial_horas, creado_en)
                VALUES ('0193f000-0000-7000-8000-000000000001', 'PRT-001', 'Profilaxis de llegada',
                        'Borrador con las recomendaciones generales de los reportes AQUAVET (ene–feb 2026). El veterinario '
                        || 'define los días de cuarentena y los medicamentos antes de aprobarlo; los tratamientos de los '
                        || 'reportes valen solo para el grupo muestreado.',
                        'borrador', NULL, 24, now())
                ON CONFLICT (codigo) DO NOTHING;

                INSERT INTO protocolo_pasos (id, protocolo_id, dia, orden, accion, medicamento_id, porcentaje_agua, nota)
                SELECT gen_random_uuid(), '0193f000-0000-7000-8000-000000000001', v.dia, v.orden, v.accion,
                       (SELECT id FROM medicamentos WHERE codigo = v.medicamento), v.agua, v.nota
                FROM (VALUES
                    (1, 1, 'ayuno', NULL, NULL::integer, 'Mínimo 24 h sin alimento antes de tratar.'),
                    (1, 2, 'cambio_agua', NULL, 50, 'Cambio de agua del 50 % antes de medicar.'),
                    (1, 3, 'medicar', 'SAL', NULL, 'Sal marina 1 g/L, previamente diluida.'),
                    (2, 1, 'cambio_agua', NULL, 80, 'Al terminar la medicación. Mover al último día cuando se defina la duración.')
                ) AS v(dia, orden, accion, medicamento, agua, nota)
                WHERE EXISTS (SELECT 1 FROM protocolos WHERE id = '0193f000-0000-7000-8000-000000000001')
                  AND NOT EXISTS (SELECT 1 FROM protocolo_pasos WHERE protocolo_id = '0193f000-0000-7000-8000-000000000001');
                """);
        }

        private static void CrearMaestros(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_medicamentos_listar()
                RETURNS TABLE (id uuid, codigo text, nombre text, principio_activo text, presentacion text, unidad text, tipo_dosis text,
                               dosis_cantidad numeric, dosis_maxima numeric, dosis_base numeric, exposicion_minutos integer,
                               requiere_ayuno boolean, dias_retiro integer, indicaciones text, contraindicaciones text, fuente text,
                               activo boolean, incompatibles_texto text)
                LANGUAGE sql STABLE AS $$
                    SELECT m.id, m.codigo::text, m.nombre::text, m.principio_activo::text, m.presentacion::text, m.unidad::text,
                           m.tipo_dosis::text, m.dosis_cantidad, m.dosis_maxima, m.dosis_base, m.exposicion_minutos, m.requiere_ayuno,
                           m.dias_retiro, m.indicaciones::text, m.contraindicaciones::text, m.fuente::text, m.activo,
                           (SELECT string_agg((CASE WHEN i.medicamento_id = m.id THEN i.incompatible_id ELSE i.medicamento_id END)::text, ',')
                                     FROM medicamentos_incompatibles i
                                     WHERE i.medicamento_id = m.id OR i.incompatible_id = m.id)
                    FROM medicamentos m
                    ORDER BY m.activo DESC, m.nombre;
                $$;

                CREATE PROCEDURE sp_medicamento_guardar(
                    p_id uuid, p_es_nuevo boolean, p_codigo text, p_nombre text, p_principio_activo text, p_presentacion text,
                    p_unidad text, p_tipo_dosis text, p_dosis_cantidad numeric, p_dosis_maxima numeric, p_dosis_base numeric,
                    p_exposicion_minutos integer, p_requiere_ayuno boolean, p_dias_retiro integer, p_indicaciones text,
                    p_contraindicaciones text, p_fuente text, p_activo boolean, p_incompatibles uuid[], p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO medicamentos (id, codigo, nombre, principio_activo, presentacion, unidad, tipo_dosis, dosis_cantidad,
                                                  dosis_maxima, dosis_base, exposicion_minutos, requiere_ayuno, dias_retiro, indicaciones,
                                                  contraindicaciones, fuente, activo, creado_en)
                        VALUES (p_id, p_codigo, p_nombre, p_principio_activo, p_presentacion, p_unidad, p_tipo_dosis, p_dosis_cantidad,
                                p_dosis_maxima, p_dosis_base, p_exposicion_minutos, p_requiere_ayuno, p_dias_retiro, p_indicaciones,
                                p_contraindicaciones, p_fuente, p_activo, p_fecha);
                    ELSE
                        UPDATE medicamentos SET codigo = p_codigo, nombre = p_nombre, principio_activo = p_principio_activo,
                               presentacion = p_presentacion, unidad = p_unidad, tipo_dosis = p_tipo_dosis,
                               dosis_cantidad = p_dosis_cantidad, dosis_maxima = p_dosis_maxima, dosis_base = p_dosis_base,
                               exposicion_minutos = p_exposicion_minutos, requiere_ayuno = p_requiere_ayuno, dias_retiro = p_dias_retiro,
                               indicaciones = p_indicaciones, contraindicaciones = p_contraindicaciones, fuente = p_fuente,
                               activo = p_activo, actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;

                    DELETE FROM medicamentos_incompatibles WHERE medicamento_id = p_id OR incompatible_id = p_id;
                    INSERT INTO medicamentos_incompatibles (medicamento_id, incompatible_id)
                    SELECT DISTINCT least(p_id, o), greatest(p_id, o)
                    FROM unnest(coalesce(p_incompatibles, '{}')) AS o
                    WHERE o <> p_id AND EXISTS (SELECT 1 FROM medicamentos WHERE id = o)
                    ON CONFLICT DO NOTHING;
                END;
                $$;

                CREATE FUNCTION fn_alimentos_listar()
                RETURNS TABLE (id uuid, codigo text, nombre text, tipo text, unidad text, activo boolean)
                LANGUAGE sql STABLE AS $$
                    SELECT id, codigo::text, nombre::text, tipo::text, unidad::text, activo
                    FROM alimentos ORDER BY activo DESC, nombre;
                $$;

                CREATE PROCEDURE sp_alimento_guardar(p_id uuid, p_es_nuevo boolean, p_codigo text, p_nombre text, p_tipo text,
                                                     p_unidad text, p_activo boolean, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO alimentos (id, codigo, nombre, tipo, unidad, activo, creado_en)
                        VALUES (p_id, p_codigo, p_nombre, p_tipo, p_unidad, p_activo, p_fecha);
                    ELSE
                        UPDATE alimentos SET codigo = p_codigo, nombre = p_nombre, tipo = p_tipo, unidad = p_unidad, activo = p_activo,
                               actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;
                END;
                $$;

                CREATE FUNCTION fn_dietas_listar()
                RETURNS TABLE (id uuid, grupo_id uuid, grupo_nombre text, producto_id uuid, producto_codigo text, producto_nombre text,
                               alimento_id uuid, alimento_nombre text, unidad text, veces_dia integer, cantidad_por100 numeric, nota text)
                LANGUAGE sql STABLE AS $$
                    SELECT d.id, d.grupo_id, g.nombre::text, d.producto_id, p.codigo::text, p.nombre_cientifico::text,
                           d.alimento_id, a.nombre::text, a.unidad::text, d.veces_dia, d.cantidad_por100, d.nota::text
                    FROM dietas d
                    JOIN alimentos a ON a.id = d.alimento_id
                    LEFT JOIN grupos_producto g ON g.id = d.grupo_id
                    LEFT JOIN productos p ON p.id = d.producto_id
                    ORDER BY coalesce(g.nombre, p.codigo), a.nombre;
                $$;

                CREATE PROCEDURE sp_dieta_guardar(p_id uuid, p_es_nueva boolean, p_grupo_id uuid, p_producto_id uuid, p_alimento_id uuid,
                                                  p_veces_dia integer, p_cantidad_por100 numeric, p_nota text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nueva THEN
                        INSERT INTO dietas (id, grupo_id, producto_id, alimento_id, veces_dia, cantidad_por100, nota, creado_en)
                        VALUES (p_id, p_grupo_id, p_producto_id, p_alimento_id, p_veces_dia, p_cantidad_por100, p_nota, p_fecha);
                    ELSE
                        UPDATE dietas SET grupo_id = p_grupo_id, producto_id = p_producto_id, alimento_id = p_alimento_id,
                               veces_dia = p_veces_dia, cantidad_por100 = p_cantidad_por100, nota = p_nota, actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;
                END;
                $$;

                CREATE PROCEDURE sp_dieta_eliminar(p_id uuid)
                LANGUAGE sql AS $$ DELETE FROM dietas WHERE id = p_id; $$;

                CREATE FUNCTION fn_fichas_listar()
                RETURNS TABLE (producto_id uuid, producto_codigo text, nombre_cientifico text, nombre_comun text, grupo_id uuid,
                               grupo_nombre text, tiene_ficha boolean, ph_min numeric, ph_max numeric, temperatura_min numeric,
                               temperatura_max numeric, ayuno_llegada_horas integer, sensibilidad_quimica text)
                LANGUAGE sql STABLE AS $$
                    SELECT p.id, p.codigo::text, p.nombre_cientifico::text, coalesce(p.nombre_espanol, p.nombre_comun)::text, p.grupo_id,
                           g.nombre::text, f.producto_id IS NOT NULL, f.ph_min, f.ph_max, f.temperatura_min, f.temperatura_max,
                           coalesce(f.ayuno_llegada_horas, 24), f.sensibilidad_quimica::text
                    FROM productos p
                    LEFT JOIN grupos_producto g ON g.id = p.grupo_id
                    LEFT JOIN fichas_tecnicas f ON f.producto_id = p.id
                    WHERE p.eliminado_en IS NULL
                    ORDER BY p.codigo;
                $$;

                CREATE FUNCTION fn_ficha_tecnica(p_producto_id uuid)
                RETURNS TABLE (producto_id uuid, procedencia text, caracteristicas text, dimorfismo text, ph_min numeric, ph_max numeric,
                               gh_min numeric, gh_max numeric, temperatura_min numeric, temperatura_max numeric, etologia text,
                               empaque text, sensibilidad_quimica text, ayuno_llegada_horas integer)
                LANGUAGE sql STABLE AS $$
                    SELECT producto_id, procedencia::text, caracteristicas::text, dimorfismo::text, ph_min, ph_max, gh_min, gh_max,
                           temperatura_min, temperatura_max, etologia::text, empaque::text, sensibilidad_quimica::text, ayuno_llegada_horas
                    FROM fichas_tecnicas WHERE producto_id = p_producto_id;
                $$;

                CREATE PROCEDURE sp_ficha_tecnica_guardar(
                    p_producto_id uuid, p_procedencia text, p_caracteristicas text, p_dimorfismo text, p_ph_min numeric, p_ph_max numeric,
                    p_gh_min numeric, p_gh_max numeric, p_temperatura_min numeric, p_temperatura_max numeric, p_etologia text,
                    p_empaque text, p_sensibilidad_quimica text, p_ayuno_llegada_horas integer, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO fichas_tecnicas (producto_id, procedencia, caracteristicas, dimorfismo, ph_min, ph_max, gh_min, gh_max,
                                                 temperatura_min, temperatura_max, etologia, empaque, sensibilidad_quimica,
                                                 ayuno_llegada_horas, creado_en)
                    VALUES (p_producto_id, p_procedencia, p_caracteristicas, p_dimorfismo, p_ph_min, p_ph_max, p_gh_min, p_gh_max,
                            p_temperatura_min, p_temperatura_max, p_etologia, p_empaque, p_sensibilidad_quimica, p_ayuno_llegada_horas,
                            p_fecha)
                    ON CONFLICT (producto_id) DO UPDATE SET
                        procedencia = excluded.procedencia, caracteristicas = excluded.caracteristicas, dimorfismo = excluded.dimorfismo,
                        ph_min = excluded.ph_min, ph_max = excluded.ph_max, gh_min = excluded.gh_min, gh_max = excluded.gh_max,
                        temperatura_min = excluded.temperatura_min, temperatura_max = excluded.temperatura_max,
                        etologia = excluded.etologia, empaque = excluded.empaque, sensibilidad_quimica = excluded.sensibilidad_quimica,
                        ayuno_llegada_horas = excluded.ayuno_llegada_horas, actualizado_en = p_fecha;
                $$;

                CREATE FUNCTION fn_parametros_sanidad()
                RETURNS TABLE (horas_ayuno_vuelo integer)
                LANGUAGE sql STABLE AS $$ SELECT horas_ayuno_vuelo FROM parametros_sanidad WHERE id = 1; $$;

                CREATE PROCEDURE sp_parametros_sanidad_actualizar(p_horas_ayuno_vuelo integer, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE parametros_sanidad SET horas_ayuno_vuelo = p_horas_ayuno_vuelo, actualizado_en = p_fecha WHERE id = 1;
                $$;

                CREATE PROCEDURE sp_acuario_responsable(p_acuario_id uuid, p_responsable_id uuid, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE acuarios SET responsable_id = p_responsable_id, actualizado_en = p_fecha WHERE id = p_acuario_id;
                $$;
                """);
        }

        private static void CrearProtocolos(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_protocolos_listar()
                RETURNS TABLE (id uuid, codigo text, nombre text, descripcion text, estado text, producto_id uuid, producto_codigo text,
                               grupo_id uuid, grupo_nombre text, centro_acopio_id uuid, centro_nombre text, cuenca_id uuid,
                               cuenca_nombre text, dias_cuarentena integer, ayuno_inicial_horas integer, aprobado_por text,
                               aprobado_en timestamptz, pasos integer)
                LANGUAGE sql STABLE AS $$
                    SELECT pr.id, pr.codigo::text, pr.nombre::text, pr.descripcion::text, pr.estado::text, pr.producto_id, p.codigo::text,
                           pr.grupo_id, g.nombre::text, pr.centro_acopio_id, c.nombre::text, pr.cuenca_id, cu.nombre::text,
                           pr.dias_cuarentena, pr.ayuno_inicial_horas, u.nombre::text, pr.aprobado_en,
                           (SELECT count(*) FROM protocolo_pasos ps WHERE ps.protocolo_id = pr.id)::integer
                    FROM protocolos pr
                    LEFT JOIN productos p ON p.id = pr.producto_id
                    LEFT JOIN grupos_producto g ON g.id = pr.grupo_id
                    LEFT JOIN centros_acopio c ON c.id = pr.centro_acopio_id
                    LEFT JOIN cuencas cu ON cu.id = pr.cuenca_id
                    LEFT JOIN usuarios u ON u.id = pr.aprobado_por_id
                    ORDER BY CASE pr.estado WHEN 'aprobado' THEN 0 WHEN 'borrador' THEN 1 ELSE 2 END, pr.codigo;
                $$;

                CREATE FUNCTION fn_protocolo_pasos(p_protocolo_id uuid)
                RETURNS TABLE (id uuid, dia integer, orden integer, accion text, medicamento_id uuid, medicamento_nombre text,
                               porcentaje_agua integer, nota text)
                LANGUAGE sql STABLE AS $$
                    SELECT ps.id, ps.dia, ps.orden, ps.accion::text, ps.medicamento_id, m.nombre::text, ps.porcentaje_agua, ps.nota::text
                    FROM protocolo_pasos ps LEFT JOIN medicamentos m ON m.id = ps.medicamento_id
                    WHERE ps.protocolo_id = p_protocolo_id
                    ORDER BY ps.dia, ps.orden;
                $$;

                -- Editar un protocolo aprobado lo devuelve a borrador: hay que volver a aprobarlo.
                CREATE PROCEDURE sp_protocolo_guardar(
                    p_id uuid, p_es_nuevo boolean, p_codigo text, p_nombre text, p_descripcion text, p_producto_id uuid, p_grupo_id uuid,
                    p_centro_acopio_id uuid, p_cuenca_id uuid, p_dias_cuarentena integer, p_ayuno_inicial_horas integer,
                    p_pasos jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF p_es_nuevo THEN
                        INSERT INTO protocolos (id, codigo, nombre, descripcion, estado, producto_id, grupo_id, centro_acopio_id, cuenca_id,
                                                dias_cuarentena, ayuno_inicial_horas, creado_en)
                        VALUES (p_id, p_codigo, p_nombre, p_descripcion, 'borrador', p_producto_id, p_grupo_id, p_centro_acopio_id,
                                p_cuenca_id, p_dias_cuarentena, p_ayuno_inicial_horas, p_fecha);
                    ELSE
                        UPDATE protocolos SET nombre = p_nombre, descripcion = p_descripcion,
                               estado = CASE WHEN estado = 'aprobado' THEN 'borrador' ELSE estado END,
                               aprobado_por_id = CASE WHEN estado = 'aprobado' THEN NULL ELSE aprobado_por_id END,
                               aprobado_en = CASE WHEN estado = 'aprobado' THEN NULL ELSE aprobado_en END,
                               producto_id = p_producto_id, grupo_id = p_grupo_id, centro_acopio_id = p_centro_acopio_id,
                               cuenca_id = p_cuenca_id, dias_cuarentena = p_dias_cuarentena,
                               ayuno_inicial_horas = p_ayuno_inicial_horas, actualizado_en = p_fecha
                        WHERE id = p_id;
                    END IF;

                    DELETE FROM protocolo_pasos WHERE protocolo_id = p_id;
                    INSERT INTO protocolo_pasos (id, protocolo_id, dia, orden, accion, medicamento_id, porcentaje_agua, nota)
                    SELECT (x->>'id')::uuid, p_id, (x->>'dia')::integer, (x->>'orden')::integer, x->>'accion',
                           (x->>'medicamento')::uuid, (x->>'agua')::integer, x->>'nota'
                    FROM jsonb_array_elements(p_pasos) AS x;
                END;
                $$;

                CREATE PROCEDURE sp_protocolo_cambiar_estado(p_id uuid, p_esperado text, p_estado text, p_usuario_id uuid,
                                                             p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_actual text;
                BEGIN
                    SELECT estado INTO v_actual FROM protocolos WHERE id = p_id FOR UPDATE;
                    IF v_actual IS DISTINCT FROM p_esperado THEN
                        RAISE EXCEPTION 'El protocolo cambió de estado.' USING ERRCODE = 'JR001';
                    END IF;

                    UPDATE protocolos SET estado = p_estado,
                           aprobado_por_id = CASE WHEN p_estado = 'aprobado' THEN p_usuario_id ELSE aprobado_por_id END,
                           aprobado_en = CASE WHEN p_estado = 'aprobado' THEN p_fecha ELSE aprobado_en END,
                           actualizado_en = p_fecha
                    WHERE id = p_id;
                END;
                $$;
                """);
        }

        private static void CrearTratamientos(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Fecha hasta la que el acuario no se reserva ni se despacha (nula = libre): tratamiento activo, o terminado
                -- y aún dentro del tiempo de retiro.
                CREATE FUNCTION fn_acuario_liberacion(p_acuario_id uuid, p_hoy date)
                RETURNS date
                LANGUAGE sql STABLE AS $$
                    SELECT max(CASE WHEN estado = 'activo' THEN greatest(fecha_liberacion, p_hoy + 1) ELSE fecha_liberacion END)
                    FROM tratamientos
                    WHERE acuario_id = p_acuario_id
                      AND (estado = 'activo' OR (estado = 'terminado' AND fecha_liberacion > p_hoy));
                $$;

                CREATE FUNCTION fn_tratamientos_listar(p_id uuid, p_solo_vigentes boolean, p_hoy date)
                RETURNS TABLE (id uuid, codigo text, acuario_id uuid, acuario_codigo text, protocolo_id uuid, protocolo_nombre text,
                               motivo text, diagnostico text, litros numeric, fecha_inicio date, fecha_fin date, fecha_liberacion date,
                               estado text, reporte_clave text, nota text, aplicaciones integer, registradas integer,
                               creado_por text, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT t.id, t.codigo::text, t.acuario_id, a.codigo::text, t.protocolo_id, pr.nombre::text, t.motivo::text,
                           t.diagnostico::text, t.litros, t.fecha_inicio, t.fecha_fin, t.fecha_liberacion, t.estado::text,
                           t.reporte_clave::text, t.nota::text,
                           (SELECT count(*) FROM tratamiento_aplicaciones x WHERE x.tratamiento_id = t.id)::integer,
                           (SELECT count(*) FROM tratamiento_aplicaciones x WHERE x.tratamiento_id = t.id AND x.estado <> 'pendiente')::integer,
                           u.nombre::text, t.creado_en
                    FROM tratamientos t
                    JOIN acuarios a ON a.id = t.acuario_id
                    LEFT JOIN protocolos pr ON pr.id = t.protocolo_id
                    LEFT JOIN usuarios u ON u.id = t.creado_por_id
                    WHERE (p_id IS NULL OR t.id = p_id)
                      AND (NOT p_solo_vigentes OR t.estado = 'activo' OR (t.estado = 'terminado' AND t.fecha_liberacion > p_hoy))
                    ORDER BY t.estado = 'activo' DESC, t.fecha_inicio DESC, t.codigo DESC;
                $$;

                -- Aplicaciones de un tratamiento, o de los tratamientos activos entre dos fechas (Mi día).
                CREATE FUNCTION fn_aplicaciones_listar(p_tratamiento_id uuid, p_desde date, p_hasta date)
                RETURNS TABLE (id uuid, tratamiento_id uuid, tratamiento_codigo text, acuario_id uuid, acuario_codigo text, fecha date,
                               dia integer, dias_total integer, orden integer, accion text, medicamento_id uuid, medicamento_nombre text,
                               porcentaje_agua integer, cantidad_calculada numeric, cantidad_aplicada numeric, unidad text,
                               estado text, nota text, usuario_nombre text, registrado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT x.id, t.id, t.codigo::text, a.id, a.codigo::text, x.fecha, x.dia, (t.fecha_fin - t.fecha_inicio + 1)::integer,
                           x.orden, x.accion::text, x.medicamento_id, m.nombre::text, x.porcentaje_agua, x.cantidad_calculada,
                           x.cantidad_aplicada, x.unidad::text, x.estado::text, x.nota::text, x.usuario_nombre::text, x.registrado_en
                    FROM tratamiento_aplicaciones x
                    JOIN tratamientos t ON t.id = x.tratamiento_id
                    JOIN acuarios a ON a.id = t.acuario_id
                    LEFT JOIN medicamentos m ON m.id = x.medicamento_id
                    WHERE (p_tratamiento_id IS NOT NULL AND x.tratamiento_id = p_tratamiento_id)
                       OR (p_tratamiento_id IS NULL AND t.estado = 'activo' AND x.fecha BETWEEN p_desde AND p_hasta)
                    ORDER BY x.fecha, a.codigo, x.orden;
                $$;

                CREATE PROCEDURE sp_tratamiento_crear(
                    p_id uuid, p_codigo text, p_acuario_id uuid, p_protocolo_id uuid, p_motivo text, p_diagnostico text, p_litros numeric,
                    p_fecha_inicio date, p_fecha_fin date, p_fecha_liberacion date, p_nota text, p_creado_por_id uuid,
                    p_aplicaciones jsonb, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM 1 FROM acuarios WHERE id = p_acuario_id AND activo FOR UPDATE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'El acuario no existe o está inactivo.';
                    END IF;

                    INSERT INTO tratamientos (id, codigo, acuario_id, protocolo_id, motivo, diagnostico, litros, fecha_inicio, fecha_fin,
                                              fecha_liberacion, estado, nota, creado_por_id, creado_en)
                    VALUES (p_id, p_codigo, p_acuario_id, p_protocolo_id, p_motivo, p_diagnostico, p_litros, p_fecha_inicio, p_fecha_fin,
                            p_fecha_liberacion, 'activo', p_nota, p_creado_por_id, p_fecha);

                    INSERT INTO tratamiento_aplicaciones (id, tratamiento_id, fecha, dia, orden, accion, medicamento_id, porcentaje_agua,
                                                          cantidad_calculada, unidad, estado, nota)
                    SELECT gen_random_uuid(), p_id, (x->>'fecha')::date, (x->>'dia')::integer, (x->>'orden')::integer, x->>'accion',
                           (x->>'medicamento')::uuid, (x->>'agua')::integer, (x->>'cantidad')::numeric, x->>'unidad', 'pendiente',
                           x->>'nota'
                    FROM jsonb_array_elements(p_aplicaciones) AS x;
                END;
                $$;

                CREATE PROCEDURE sp_aplicacion_registrar(p_id uuid, p_estado text, p_cantidad numeric, p_nota text, p_usuario_id uuid,
                                                         p_usuario_nombre text, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_estado text;
                    v_tratamiento text;
                BEGIN
                    SELECT x.estado, t.estado INTO v_estado, v_tratamiento
                    FROM tratamiento_aplicaciones x JOIN tratamientos t ON t.id = x.tratamiento_id
                    WHERE x.id = p_id FOR UPDATE OF x;
                    IF v_estado IS DISTINCT FROM 'pendiente' THEN
                        RAISE EXCEPTION 'Esta aplicación ya se registró.' USING ERRCODE = 'JR001';
                    END IF;
                    IF v_tratamiento <> 'activo' THEN
                        RAISE EXCEPTION 'El tratamiento ya no está activo.';
                    END IF;

                    UPDATE tratamiento_aplicaciones SET estado = p_estado, cantidad_aplicada = p_cantidad,
                           nota = coalesce(p_nota, nota), usuario_id = p_usuario_id, usuario_nombre = p_usuario_nombre,
                           registrado_en = p_fecha
                    WHERE id = p_id;
                END;
                $$;

                CREATE PROCEDURE sp_tratamiento_cambiar_estado(p_id uuid, p_esperado text, p_estado text, p_fecha_liberacion date,
                                                               p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_actual text;
                BEGIN
                    SELECT estado INTO v_actual FROM tratamientos WHERE id = p_id FOR UPDATE;
                    IF v_actual IS DISTINCT FROM p_esperado THEN
                        RAISE EXCEPTION 'El tratamiento cambió de estado.' USING ERRCODE = 'JR001';
                    END IF;

                    UPDATE tratamientos SET estado = p_estado,
                           fecha_liberacion = coalesce(p_fecha_liberacion, fecha_liberacion),
                           actualizado_en = p_fecha
                    WHERE id = p_id;

                    IF p_estado = 'cancelado' THEN
                        UPDATE tratamiento_aplicaciones SET estado = 'omitida', nota = coalesce(nota, 'Tratamiento cancelado'),
                               registrado_en = p_fecha
                        WHERE tratamiento_id = p_id AND estado = 'pendiente';
                    END IF;
                END;
                $$;

                CREATE PROCEDURE sp_tratamiento_reporte(p_id uuid, p_clave text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    UPDATE tratamientos SET reporte_clave = p_clave, actualizado_en = p_fecha WHERE id = p_id;
                $$;

                CREATE PROCEDURE sp_alimentacion_registrar(p_acuario_id uuid, p_alimento_id uuid, p_cantidad numeric, p_usuario_id uuid,
                                                           p_usuario_nombre text, p_fecha timestamptz)
                LANGUAGE sql AS $$
                    INSERT INTO registros_alimentacion (id, acuario_id, alimento_id, cantidad, fecha, usuario_id, usuario_nombre)
                    VALUES (gen_random_uuid(), p_acuario_id, p_alimento_id, p_cantidad, p_fecha, p_usuario_id, p_usuario_nombre);
                $$;

                CREATE FUNCTION fn_alimentacion_desde(p_desde timestamptz)
                RETURNS TABLE (acuario_id uuid, tomas integer, ultima timestamptz, ultimo_usuario text)
                LANGUAGE sql STABLE AS $$
                    SELECT r.acuario_id, count(*)::integer, max(r.fecha),
                           (array_agg(r.usuario_nombre ORDER BY r.fecha DESC))[1]::text
                    FROM registros_alimentacion r
                    WHERE r.fecha >= p_desde
                    GROUP BY r.acuario_id;
                $$;
                """);
        }

        private static void CrearReservas(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Reservado vigente de un lote en un acuario (pedidos confirmados o en preparación), sin contar una línea.
                CREATE FUNCTION fn_reservado(p_lote_id uuid, p_acuario_id uuid, p_excluir_linea uuid)
                RETURNS integer
                LANGUAGE sql STABLE AS $$
                    SELECT coalesce(sum(r.cantidad), 0)::integer
                    FROM reservas_despacho r
                    JOIN pedidos_cliente_lineas l ON l.id = r.pedido_cliente_linea_id
                    JOIN pedidos_cliente p ON p.id = l.pedido_id
                    WHERE r.lote_id = p_lote_id AND r.acuario_id = p_acuario_id
                      AND p.estado IN ('confirmado', 'en_preparacion')
                      AND (p_excluir_linea IS NULL OR r.pedido_cliente_linea_id <> p_excluir_linea);
                $$;

                CREATE FUNCTION fn_reservas_pedido(p_pedido_id uuid)
                RETURNS TABLE (id uuid, linea_id uuid, producto_id uuid, producto_codigo text, lote_id uuid, lote_codigo text,
                               acuario_id uuid, acuario_codigo text, cantidad integer, creado_en timestamptz)
                LANGUAGE sql STABLE AS $$
                    SELECT r.id, l.id, p.id, p.codigo::text, lo.id, lo.codigo::text, a.id, a.codigo::text, r.cantidad, r.creado_en
                    FROM reservas_despacho r
                    JOIN pedidos_cliente_lineas l ON l.id = r.pedido_cliente_linea_id
                    JOIN productos p ON p.id = l.producto_id
                    JOIN lotes lo ON lo.id = r.lote_id
                    JOIN acuarios a ON a.id = r.acuario_id
                    WHERE l.pedido_id = p_pedido_id
                    ORDER BY p.codigo, a.codigo;
                $$;

                -- Fija lo reservado de una línea en un lote y acuario (0 = quitar). Valida pedido vigente, producto, acuario
                -- libre de tratamiento, lo pendiente de la línea y lo disponible (sano − reservado a otros), con la fila bloqueada.
                CREATE PROCEDURE sp_reserva_fijar(p_linea_id uuid, p_lote_id uuid, p_acuario_id uuid, p_cantidad integer,
                                                  p_usuario_id uuid, p_fecha timestamptz)
                LANGUAGE plpgsql AS $$
                DECLARE
                    v_linea pedidos_cliente_lineas%ROWTYPE;
                    v_estado text;
                    v_hoy date := (p_fecha AT TIME ZONE 'America/Bogota')::date;
                    v_liberacion date;
                    v_acuario text;
                    v_stock integer;
                    v_otras integer;
                    v_despachada integer;
                    v_reservada_linea integer;
                BEGIN
                    SELECT * INTO v_linea FROM pedidos_cliente_lineas WHERE id = p_linea_id FOR UPDATE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'La línea del pedido no existe.';
                    END IF;
                    SELECT estado INTO v_estado FROM pedidos_cliente WHERE id = v_linea.pedido_id;
                    IF v_estado NOT IN ('confirmado', 'en_preparacion') THEN
                        RAISE EXCEPTION 'Solo se reservan pedidos confirmados o en preparación.';
                    END IF;

                    IF p_cantidad <= 0 THEN
                        DELETE FROM reservas_despacho
                        WHERE pedido_cliente_linea_id = p_linea_id AND lote_id = p_lote_id AND acuario_id = p_acuario_id;
                        RETURN;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM lotes WHERE id = p_lote_id AND producto_id = v_linea.producto_id) THEN
                        RAISE EXCEPTION 'El lote no es del producto de la línea.';
                    END IF;

                    SELECT codigo INTO v_acuario FROM acuarios WHERE id = p_acuario_id;
                    v_liberacion := fn_acuario_liberacion(p_acuario_id, v_hoy);
                    IF v_liberacion IS NOT NULL THEN
                        RAISE EXCEPTION '% está en tratamiento: se puede despachar desde el %.', v_acuario, to_char(v_liberacion, 'DD/MM/YYYY')
                            USING ERRCODE = 'JR004';
                    END IF;

                    SELECT coalesce(sum(m.cantidad), 0) INTO v_despachada FROM movimientos_inventario m
                    WHERE m.pedido_cliente_linea_id = p_linea_id AND m.tipo = 'despacho';
                    SELECT coalesce(sum(r.cantidad), 0) INTO v_reservada_linea FROM reservas_despacho r
                    WHERE r.pedido_cliente_linea_id = p_linea_id AND NOT (r.lote_id = p_lote_id AND r.acuario_id = p_acuario_id);
                    IF v_despachada + v_reservada_linea + p_cantidad > v_linea.cantidad THEN
                        RAISE EXCEPTION 'Solo quedan % por reservar en esa línea.', greatest(v_linea.cantidad - v_despachada - v_reservada_linea, 0)
                            USING ERRCODE = 'JR003';
                    END IF;

                    SELECT cantidad INTO v_stock FROM existencias
                    WHERE lote_id = p_lote_id AND acuario_id = p_acuario_id AND condicion = 'sano' FOR UPDATE;
                    v_otras := fn_reservado(p_lote_id, p_acuario_id, p_linea_id);
                    IF coalesce(v_stock, 0) - v_otras < p_cantidad THEN
                        RAISE EXCEPTION 'en % hay % sanos y % reservados para otros pedidos.', v_acuario, coalesce(v_stock, 0), v_otras
                            USING ERRCODE = 'JR002';
                    END IF;

                    INSERT INTO reservas_despacho (id, pedido_cliente_linea_id, lote_id, acuario_id, cantidad, creado_por_id, creado_en)
                    VALUES (gen_random_uuid(), p_linea_id, p_lote_id, p_acuario_id, p_cantidad, p_usuario_id, p_fecha)
                    ON CONFLICT (pedido_cliente_linea_id, lote_id, acuario_id)
                    DO UPDATE SET cantidad = excluded.cantidad, creado_por_id = excluded.creado_por_id, creado_en = excluded.creado_en;
                END;
                $$;
                """);
        }

        private static void CrearTablero(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_sanidad_acuarios()
                RETURNS TABLE (id uuid, codigo text, sector text, litros numeric, responsable_id uuid, responsable_nombre text)
                LANGUAGE sql STABLE AS $$
                    SELECT a.id, a.codigo::text, a.sector::text, a.capacidad_litros, a.responsable_id, u.nombre::text
                    FROM acuarios a LEFT JOIN usuarios u ON u.id = a.responsable_id
                    WHERE a.activo
                    ORDER BY a.sector, a.nivel, a.posicion, a.codigo;
                $$;

                -- Existencias con stock, con lo que las reglas de sanidad necesitan: producto, grupo, procedencia, hora de
                -- llegada (registro del lote; en el saldo inicial, su fecha de ingreso) y horas de ayuno de llegada.
                CREATE FUNCTION fn_sanidad_existencias()
                RETURNS TABLE (acuario_id uuid, lote_id uuid, lote_codigo text, producto_id uuid, producto_codigo text, producto_nombre text,
                               grupo_id uuid, centro_acopio_id uuid, cuenca_id uuid, condicion text, cantidad integer, llegada timestamptz,
                               ayuno_llegada_horas integer)
                LANGUAGE sql STABLE AS $$
                    SELECT e.acuario_id, l.id, l.codigo::text, p.id, p.codigo::text, p.nombre_cientifico::text, p.grupo_id,
                           l.centro_acopio_id, c.cuenca_id, e.condicion::text, e.cantidad,
                           CASE WHEN l.origen = 'saldo_inicial' THEN l.fecha_ingreso::timestamp AT TIME ZONE 'America/Bogota'
                                ELSE l.creado_en END,
                           coalesce(f.ayuno_llegada_horas, 24)
                    FROM existencias e
                    JOIN lotes l ON l.id = e.lote_id
                    JOIN productos p ON p.id = l.producto_id
                    LEFT JOIN centros_acopio c ON c.id = l.centro_acopio_id
                    LEFT JOIN fichas_tecnicas f ON f.producto_id = p.id
                    WHERE e.cantidad > 0;
                $$;

                CREATE FUNCTION fn_sanidad_tratamientos_vigentes(p_hoy date)
                RETURNS TABLE (id uuid, codigo text, acuario_id uuid, estado text, fecha_inicio date, fecha_fin date, fecha_liberacion date,
                               nombre text, requiere_ayuno boolean, alimento_medicado boolean, pendientes_hoy integer)
                LANGUAGE sql STABLE AS $$
                    SELECT t.id, t.codigo::text, t.acuario_id, t.estado::text, t.fecha_inicio, t.fecha_fin, t.fecha_liberacion,
                           coalesce(pr.nombre, CASE t.motivo WHEN 'prescripcion' THEN 'Prescripción veterinaria' ELSE 'Tratamiento' END)::text,
                           EXISTS (SELECT 1 FROM tratamiento_aplicaciones x LEFT JOIN medicamentos m ON m.id = x.medicamento_id
                                   WHERE x.tratamiento_id = t.id AND (x.accion = 'ayuno' OR (x.accion = 'medicar' AND m.requiere_ayuno))),
                           EXISTS (SELECT 1 FROM tratamiento_aplicaciones x JOIN medicamentos m ON m.id = x.medicamento_id
                                   WHERE x.tratamiento_id = t.id AND m.tipo_dosis = 'por_alimento'),
                           (SELECT count(*) FROM tratamiento_aplicaciones x
                            WHERE x.tratamiento_id = t.id AND x.estado = 'pendiente' AND x.fecha <= p_hoy)::integer
                    FROM tratamientos t
                    LEFT JOIN protocolos pr ON pr.id = t.protocolo_id
                    WHERE t.estado = 'activo' OR (t.estado = 'terminado' AND t.fecha_liberacion > p_hoy);
                $$;

                -- Reservas de pedidos vigentes con el vuelo de su embarque (si es exportación).
                CREATE FUNCTION fn_sanidad_reservas()
                RETURNS TABLE (acuario_id uuid, lote_id uuid, cantidad integer, pedido_id uuid, pedido_codigo text, cliente_nombre text,
                               embarque_id uuid, embarque_codigo text, salida_vuelo timestamptz, fecha_despacho date, destino text)
                LANGUAGE sql STABLE AS $$
                    SELECT r.acuario_id, r.lote_id, r.cantidad, p.id, p.codigo::text, c.nombre_empresa::text, e.id, e.codigo::text,
                           e.salida_vuelo, e.fecha_despacho, e.destino::text
                    FROM reservas_despacho r
                    JOIN pedidos_cliente_lineas l ON l.id = r.pedido_cliente_linea_id
                    JOIN pedidos_cliente p ON p.id = l.pedido_id
                    JOIN clientes c ON c.id = p.cliente_id
                    LEFT JOIN embarques e ON e.pedido_cliente_id = p.id AND e.estado NOT IN ('cancelado', 'cerrado')
                    WHERE p.estado IN ('confirmado', 'en_preparacion');
                $$;
                """);
        }

        /// <summary>
        /// Despacho con reservas: además de lo de antes, rechaza acuarios en tratamiento (JR004), no toma lo
        /// reservado para otros pedidos (JR002) y descuenta la reserva de la línea. Al quedar despachado borra las reservas.
        /// </summary>
        private const string DespacharConReservas =
            """
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
                v_lote uuid;
                v_acuario uuid;
                v_cantidad integer;
                v_hoy date := (p_fecha AT TIME ZONE 'America/Bogota')::date;
                v_liberacion date;
                v_stock integer;
                v_otras integer;
            BEGIN
                SELECT estado, codigo INTO v_estado, v_codigo FROM pedidos_cliente WHERE id = p_pedido_id FOR UPDATE;
                IF v_estado NOT IN ('confirmado', 'en_preparacion') THEN
                    RAISE EXCEPTION 'Solo se despachan pedidos confirmados o en preparación.' USING ERRCODE = 'JR001';
                END IF;

                FOR s IN SELECT * FROM jsonb_array_elements(p_salidas) LOOP
                    v_lote := (s->>'lote')::uuid;
                    v_acuario := (s->>'acuario')::uuid;
                    v_cantidad := (s->>'cantidad')::integer;

                    SELECT * INTO v_linea FROM pedidos_cliente_lineas
                    WHERE id = (s->>'linea')::uuid AND pedido_id = p_pedido_id FOR UPDATE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'La línea no pertenece al pedido.';
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM lotes WHERE id = v_lote AND producto_id = v_linea.producto_id) THEN
                        RAISE EXCEPTION 'El lote no es del producto de la línea.';
                    END IF;

                    SELECT coalesce(sum(cantidad), 0) INTO v_despachada FROM movimientos_inventario
                    WHERE pedido_cliente_linea_id = v_linea.id AND tipo = 'despacho';
                    IF v_despachada + v_cantidad > v_linea.cantidad THEN
                        RAISE EXCEPTION 'Solo quedan % por despachar en esa línea.', v_linea.cantidad - v_despachada
                            USING ERRCODE = 'JR003';
                    END IF;

                    IF v_acuario IS NOT NULL THEN
                        v_liberacion := fn_acuario_liberacion(v_acuario, v_hoy);
                        IF v_liberacion IS NOT NULL THEN
                            RAISE EXCEPTION '% está en tratamiento: se puede despachar desde el %.',
                                (SELECT codigo FROM acuarios WHERE id = v_acuario), to_char(v_liberacion, 'DD/MM/YYYY')
                                USING ERRCODE = 'JR004';
                        END IF;

                        SELECT cantidad INTO v_stock FROM existencias
                        WHERE lote_id = v_lote AND acuario_id = v_acuario AND condicion = 'sano' FOR UPDATE;
                        v_otras := fn_reservado(v_lote, v_acuario, v_linea.id);
                        IF v_otras > 0 AND coalesce(v_stock, 0) - v_otras < v_cantidad THEN
                            RAISE EXCEPTION 'quedan % libres en %: % están reservados para otros pedidos.',
                                greatest(coalesce(v_stock, 0) - v_otras, 0), (SELECT codigo FROM acuarios WHERE id = v_acuario), v_otras
                                USING ERRCODE = 'JR002';
                        END IF;
                    END IF;

                    CALL sp_inventario_mover_uno(v_lote, v_acuario, 'sano', NULL, NULL, v_cantidad, 'despacho', v_linea.id, NULL,
                                                 'Pedido ' || v_codigo, p_usuario_id, p_usuario_nombre, p_fecha);

                    DELETE FROM reservas_despacho
                    WHERE pedido_cliente_linea_id = v_linea.id AND lote_id = v_lote AND acuario_id = v_acuario AND cantidad <= v_cantidad;
                    UPDATE reservas_despacho SET cantidad = cantidad - v_cantidad
                    WHERE pedido_cliente_linea_id = v_linea.id AND lote_id = v_lote AND acuario_id = v_acuario;
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

                IF v_nuevo = 'despachado' THEN
                    DELETE FROM reservas_despacho r USING pedidos_cliente_lineas l
                    WHERE l.id = r.pedido_cliente_linea_id AND l.pedido_id = p_pedido_id;
                END IF;
            END;
            $$;
            """;

        /// <summary>El procedimiento tal como lo dejó <c>FuncionesInventario</c>, para el Down.</summary>
        private const string DespacharOriginal =
            """
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
                            USING ERRCODE = 'JR003';
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
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_pedido_cliente_despachar;");
            migrationBuilder.Sql(DespacharOriginal);

            foreach (var objeto in new[]
                     {
                         "FUNCTION fn_sanidad_reservas", "FUNCTION fn_sanidad_tratamientos_vigentes", "FUNCTION fn_sanidad_existencias",
                         "FUNCTION fn_sanidad_acuarios", "PROCEDURE sp_reserva_fijar", "FUNCTION fn_reservas_pedido", "FUNCTION fn_reservado",
                         "FUNCTION fn_alimentacion_desde", "PROCEDURE sp_alimentacion_registrar", "PROCEDURE sp_tratamiento_reporte",
                         "PROCEDURE sp_tratamiento_cambiar_estado", "PROCEDURE sp_aplicacion_registrar", "PROCEDURE sp_tratamiento_crear",
                         "FUNCTION fn_aplicaciones_listar", "FUNCTION fn_tratamientos_listar", "FUNCTION fn_acuario_liberacion",
                         "PROCEDURE sp_protocolo_cambiar_estado", "PROCEDURE sp_protocolo_guardar", "FUNCTION fn_protocolo_pasos",
                         "FUNCTION fn_protocolos_listar", "PROCEDURE sp_acuario_responsable", "PROCEDURE sp_parametros_sanidad_actualizar",
                         "FUNCTION fn_parametros_sanidad", "PROCEDURE sp_ficha_tecnica_guardar", "FUNCTION fn_ficha_tecnica",
                         "FUNCTION fn_fichas_listar", "PROCEDURE sp_dieta_eliminar", "PROCEDURE sp_dieta_guardar", "FUNCTION fn_dietas_listar",
                         "PROCEDURE sp_alimento_guardar", "FUNCTION fn_alimentos_listar", "PROCEDURE sp_medicamento_guardar",
                         "FUNCTION fn_medicamentos_listar",
                     })
            {
                var (tipo, nombre) = (objeto.Split(' ')[0], objeto.Split(' ')[1]);
                migrationBuilder.Sql($"DROP {tipo} IF EXISTS {nombre};");
            }

            migrationBuilder.Sql("DELETE FROM consecutivos WHERE prefijo IN ('TRT', 'PRT');");
        }
    }
}
