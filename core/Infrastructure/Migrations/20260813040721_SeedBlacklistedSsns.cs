using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Niuro.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBlacklistedSsns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed: 3 SSNs en blacklist. Si ya existen (ejecutado antes), son idempotentes.
            migrationBuilder.Sql(@"
                INSERT INTO ""BlacklistedSsns"" (""Ssn"")
                VALUES ('111-11-1111'), ('222-22-2222'), ('333-33-3333')
                ON CONFLICT (""Ssn"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Seeds: reversible — se eliminan los SSNs insertados por Up().
            migrationBuilder.Sql(@"
                DELETE FROM ""BlacklistedSsns""
                WHERE ""Ssn"" IN ('111-11-1111', '222-22-2222', '333-33-3333');
            ");
        }
    }
}
