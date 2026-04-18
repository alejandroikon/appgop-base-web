using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWellsForV2_PpdmUwi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wells_Uwi",
                table: "Wells");

            migrationBuilder.AlterColumn<string>(
                name: "TipoTrayectoria",
                table: "Wells",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "TipoAngulo",
                table: "Wells",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "NombrePozo",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<int>(
                name: "MunicipioId",
                table: "Wells",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DepartamentoId",
                table: "Wells",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Consecutivo",
                table: "Wells",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoDaneMpio",
                table: "Wells",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoDaneDpto",
                table: "Wells",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "CampoId",
                table: "Wells",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Campo",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cluster",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contrato",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Forma101Radicada",
                table: "Wells",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Wells",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubClasificacion",
                table: "Wells",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Abreviatura",
                table: "Clusters",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Clusters",
                keyColumn: "Id",
                keyValue: 1,
                column: "Abreviatura",
                value: "CN");

            migrationBuilder.UpdateData(
                table: "Clusters",
                keyColumn: "Id",
                keyValue: 2,
                column: "Abreviatura",
                value: "CS");

            migrationBuilder.UpdateData(
                table: "Clusters",
                keyColumn: "Id",
                keyValue: 3,
                column: "Abreviatura",
                value: "CE");

            migrationBuilder.UpdateData(
                table: "Clusters",
                keyColumn: "Id",
                keyValue: 4,
                column: "Abreviatura",
                value: "CO");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_ClusterId",
                table: "Wells",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_DepartamentoId",
                table: "Wells",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_MunicipioId",
                table: "Wells",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_TenantId_Estado",
                table: "Wells",
                columns: new[] { "TenantId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Wells_TenantId_NombrePozo",
                table: "Wells",
                columns: new[] { "TenantId", "NombrePozo" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Wells_Uwi_Global",
                table: "Wells",
                column: "Uwi",
                unique: true,
                filter: "[Uwi] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Wells_Clusters_ClusterId",
                table: "Wells",
                column: "ClusterId",
                principalTable: "Clusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wells_Departamentos_DepartamentoId",
                table: "Wells",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wells_Municipios_MunicipioId",
                table: "Wells",
                column: "MunicipioId",
                principalTable: "Municipios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wells_Clusters_ClusterId",
                table: "Wells");

            migrationBuilder.DropForeignKey(
                name: "FK_Wells_Departamentos_DepartamentoId",
                table: "Wells");

            migrationBuilder.DropForeignKey(
                name: "FK_Wells_Municipios_MunicipioId",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_ClusterId",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_DepartamentoId",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_MunicipioId",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_TenantId_Estado",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_TenantId_NombrePozo",
                table: "Wells");

            migrationBuilder.DropIndex(
                name: "IX_Wells_Uwi_Global",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Campo",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Cluster",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Contrato",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Forma101Radicada",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "SubClasificacion",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Abreviatura",
                table: "Clusters");

            migrationBuilder.AlterColumn<string>(
                name: "TipoTrayectoria",
                table: "Wells",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "TipoAngulo",
                table: "Wells",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "NombrePozo",
                table: "Wells",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "MunicipioId",
                table: "Wells",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DepartamentoId",
                table: "Wells",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Consecutivo",
                table: "Wells",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoDaneMpio",
                table: "Wells",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoDaneDpto",
                table: "Wells",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CampoId",
                table: "Wells",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wells_Uwi",
                table: "Wells",
                column: "Uwi",
                unique: true,
                filter: "[Uwi] IS NOT NULL");
        }
    }
}
