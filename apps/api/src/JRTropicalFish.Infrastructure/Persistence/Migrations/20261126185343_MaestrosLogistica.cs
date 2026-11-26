using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JRTropicalFish.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Maestros de logística y comercio exterior: aerolíneas, agentes de carga, aeropuertos, causas de mortalidad, conceptos de
    /// costo y tipos de documento. Usan las funciones genéricas de maestros (<c>fn_maestro_tabla</c> los habilita).
    /// </summary>
    public partial class MaestrosLogistica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aerolineas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aerolineas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "aeropuertos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aeropuertos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agentes_carga",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agentes_carga", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "causas_mortalidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_causas_mortalidad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conceptos_costo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conceptos_costo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_documento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eliminado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipos_documento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aerolineas_codigo",
                table: "aerolineas",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_aerolineas_nombre",
                table: "aerolineas",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_aeropuertos_codigo",
                table: "aeropuertos",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_aeropuertos_nombre",
                table: "aeropuertos",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agentes_carga_codigo",
                table: "agentes_carga",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agentes_carga_nombre",
                table: "agentes_carga",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_causas_mortalidad_codigo",
                table: "causas_mortalidad",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_causas_mortalidad_nombre",
                table: "causas_mortalidad",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_conceptos_costo_codigo",
                table: "conceptos_costo",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_conceptos_costo_nombre",
                table: "conceptos_costo",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tipos_documento_codigo",
                table: "tipos_documento",
                column: "codigo",
                unique: true,
                filter: "eliminado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tipos_documento_nombre",
                table: "tipos_documento",
                column: "nombre",
                unique: true,
                filter: "eliminado_en IS NULL");

            // Lista blanca de los maestros genéricos y sembrado: aeropuertos y aerolíneas que ya usan los clientes y las
            // ventas, y los documentos del despacho. Causas de mortalidad, conceptos de costo y agentes de carga los llena el
            // equipo.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_maestro_tabla(p_tabla text)
                RETURNS text
                LANGUAGE plpgsql IMMUTABLE AS $$
                BEGIN
                    IF p_tabla IS NULL OR p_tabla NOT IN ('tipos_identificacion', 'cargos', 'areas', 'tipos_contrato',
                                                          'grupos_producto', 'cuencas',
                                                          'aerolineas', 'agentes_carga', 'aeropuertos', 'causas_mortalidad',
                                                          'conceptos_costo', 'tipos_documento') THEN
                        RAISE EXCEPTION 'Maestro no permitido: %', p_tabla;
                    END IF;
                    RETURN p_tabla;
                END;
                $$;

                INSERT INTO aeropuertos (id, codigo, nombre, activo, creado_en)
                SELECT gen_random_uuid(), v.codigo, v.nombre, true, now()
                FROM (VALUES
                    ('AMS', 'Amsterdam (Países Bajos)'),
                    ('ATL', 'Atlanta (Estados Unidos)'),
                    ('BKK', 'Bangkok (Tailandia)'),
                    ('BOG', 'Bogotá (Colombia)'),
                    ('CAN', 'Guangzhou (China)'),
                    ('CDG', 'París (Francia)'),
                    ('CGK', 'Yakarta (Indonesia)'),
                    ('EWR', 'Nueva Jersey (Estados Unidos)'),
                    ('FRA', 'Frankfurt (Alemania)'),
                    ('GUA', 'Ciudad de Guatemala (Guatemala)'),
                    ('HKG', 'Hong Kong (China)'),
                    ('ICN', 'Seúl (Corea del Sur)'),
                    ('IND', 'Indianápolis (Estados Unidos)'),
                    ('JFK', 'Nueva York (Estados Unidos)'),
                    ('KIX', 'Osaka (Japón)'),
                    ('LHR', 'Londres (Reino Unido)'),
                    ('MAD', 'Madrid (España)'),
                    ('MEX', 'Ciudad de México (México)'),
                    ('MGA', 'Managua (Nicaragua)'),
                    ('MIA', 'Miami (Estados Unidos)'),
                    ('NGO', 'Nagoya (Japón)'),
                    ('NRT', 'Tokio (Japón)'),
                    ('POS', 'Puerto España (Trinidad y Tobago)'),
                    ('SAL', 'San Salvador (El Salvador)'),
                    ('SDQ', 'Santo Domingo (República Dominicana)'),
                    ('SJO', 'San José (Costa Rica)'),
                    ('TPE', 'Taipei (Taiwán)'),
                    ('WAW', 'Varsovia (Polonia)'),
                    ('YUL', 'Montreal (Canadá)'),
                    ('YVR', 'Vancouver (Canadá)'),
                    ('ZRH', 'Zurich (Suiza)')
                ) AS v (codigo, nombre)
                WHERE NOT EXISTS (SELECT 1 FROM aeropuertos t WHERE t.codigo = v.codigo AND t.eliminado_en IS NULL);

                INSERT INTO aerolineas (id, codigo, nombre, activo, creado_en)
                SELECT gen_random_uuid(), v.codigo, v.nombre, true, now()
                FROM (VALUES
                    ('QR', 'Qatar Airways')
                ) AS v (codigo, nombre)
                WHERE NOT EXISTS (SELECT 1 FROM aerolineas t WHERE t.codigo = v.codigo AND t.eliminado_en IS NULL);

                INSERT INTO tipos_documento (id, codigo, nombre, activo, creado_en)
                SELECT gen_random_uuid(), v.codigo, v.nombre, true, now()
                FROM (VALUES
                    ('PREINVOICE', 'Preinvoice (cotización)'),
                    ('PROFORMA', 'Proforma'),
                    ('VUCE', 'VUCE (autorización de exportación)'),
                    ('DEX', 'DEX (declaración de exportación)'),
                    ('FACTURA', 'Factura comercial'),
                    ('FACTURA-DIAN', 'Factura electrónica DIAN'),
                    ('PACKING', 'Packing list'),
                    ('AWB', 'Guía aérea (AWB)'),
                    ('CITES', 'Certificado CITES'),
                    ('ICA', 'Certificado ICA'),
                    ('ZOOSANITARIO', 'Certificado zoosanitario'),
                    ('NOTA-CREDITO', 'Nota crédito')
                ) AS v (codigo, nombre)
                WHERE NOT EXISTS (SELECT 1 FROM tipos_documento t WHERE t.codigo = v.codigo AND t.eliminado_en IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.DropTable(
                name: "aerolineas");

            migrationBuilder.DropTable(
                name: "aeropuertos");

            migrationBuilder.DropTable(
                name: "agentes_carga");

            migrationBuilder.DropTable(
                name: "causas_mortalidad");

            migrationBuilder.DropTable(
                name: "conceptos_costo");

            migrationBuilder.DropTable(
                name: "tipos_documento");
        }
    }
}
