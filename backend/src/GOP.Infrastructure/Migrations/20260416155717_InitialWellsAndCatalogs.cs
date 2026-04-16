using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GOP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWellsAndCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contratos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cuenca = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CodigoDane = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Municipios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    CodigoDane = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipios_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CampoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clusters_Campos_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operadora = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    TipoContrato = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cuenca = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CampoId = table.Column<int>(type: "int", nullable: false),
                    TipoTrayectoria = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Clasificacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoUbicacion = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TipoAngulo = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TipoObjetivo = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TipoTerminacion = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Denominacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Consecutivo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    NombrePozo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    MunicipioId = table.Column<int>(type: "int", nullable: false),
                    ClusterId = table.Column<int>(type: "int", nullable: true),
                    CodigoDaneDpto = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CodigoDaneMpio = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wells_Campos_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wells_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Contratos",
                columns: new[] { "Id", "Cuenca", "Nombre", "Tipo" },
                values: new object[,]
                {
                    { 1, "Llanos Orientales", "Contrato E&P Llanos", "E&P" },
                    { 2, "Piedemonte Llanero", "Contrato E&P Piedemonte", "E&P" },
                    { 3, "Valle Medio del Magdalena", "Contrato E&P Magdalena", "E&P" }
                });

            migrationBuilder.InsertData(
                table: "Departamentos",
                columns: new[] { "Id", "CodigoDane", "Nombre" },
                values: new object[,]
                {
                    { 1, "50", "Meta" },
                    { 2, "85", "Casanare" },
                    { 3, "68", "Santander" }
                });

            migrationBuilder.InsertData(
                table: "Campos",
                columns: new[] { "Id", "ContratoId", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "Campo Rubiales" },
                    { 2, 1, "Campo Quifa" },
                    { 3, 2, "Campo Cusiana" },
                    { 4, 2, "Campo Cupiagua" },
                    { 5, 3, "Campo Lisama" },
                    { 6, 3, "Campo La Cira" }
                });

            migrationBuilder.InsertData(
                table: "Municipios",
                columns: new[] { "Id", "CodigoDane", "DepartamentoId", "Nombre" },
                values: new object[,]
                {
                    { 1, "50568", 1, "Puerto Gaitán" },
                    { 2, "50001", 1, "Villavicencio" },
                    { 3, "85001", 2, "Yopal" },
                    { 4, "85010", 2, "Aguazul" },
                    { 5, "68081", 3, "Barrancabermeja" },
                    { 6, "68001", 3, "Bucaramanga" }
                });

            migrationBuilder.InsertData(
                table: "Clusters",
                columns: new[] { "Id", "CampoId", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "Cluster Norte" },
                    { 2, 1, "Cluster Sur" },
                    { 3, 3, "Cluster Este" },
                    { 4, 3, "Cluster Oeste" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campos_ContratoId",
                table: "Campos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_CampoId",
                table: "Clusters",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_DepartamentoId",
                table: "Municipios",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_CampoId",
                table: "Wells",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_ContratoId",
                table: "Wells",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_TenantId",
                table: "Wells",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropTable(
                name: "Municipios");

            migrationBuilder.DropTable(
                name: "Wells");

            migrationBuilder.DropTable(
                name: "Departamentos");

            migrationBuilder.DropTable(
                name: "Campos");

            migrationBuilder.DropTable(
                name: "Contratos");
        }
    }
}
