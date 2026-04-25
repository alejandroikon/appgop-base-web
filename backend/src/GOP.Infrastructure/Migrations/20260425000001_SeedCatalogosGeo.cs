using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogosGeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Departamentos — idempotente, IDs secuenciales alineados con staging (ED-09)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 1)
                    INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                    VALUES (1, N'Meta', N'50');
                IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 2)
                    INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                    VALUES (2, N'Casanare', N'85');
                IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 3)
                    INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                    VALUES (3, N'Santander', N'68');
            ");

            // Municipios — idempotente, IDs secuenciales alineados con staging (ED-09)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 1)
                    INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                    VALUES (1, N'Puerto Gaitán', 1, N'50568');
                IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 2)
                    INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                    VALUES (2, N'Puerto López', 1, N'50573');
                IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 3)
                    INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                    VALUES (3, N'Tauramena', 2, N'85410');
                IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 4)
                    INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                    VALUES (4, N'Aguazul', 2, N'85010');
                IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 5)
                    INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                    VALUES (5, N'Barrancabermeja', 3, N'68081');
            ");

            // ED-11: Corregir DANE de Aguazul en staging (pudo haberse insertado como 85015)
            // Chámeza es 85015, Aguazul es 85010. Este UPDATE es no-op si el dato ya es correcto.
            migrationBuilder.Sql(@"
                UPDATE [Municipios] SET [CodigoDane] = N'85010'
                WHERE [Id] = 4 AND [CodigoDane] = N'85015';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [Municipios] WHERE [Id] IN (1, 2, 3, 4, 5);
                DELETE FROM [Departamentos] WHERE [Id] IN (1, 2, 3);
            ");
        }
    }
}
