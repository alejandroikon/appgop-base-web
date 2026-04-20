using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GOP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FullDaneSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Municipios",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
