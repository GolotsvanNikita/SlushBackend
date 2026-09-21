using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slush.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSeenAtToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Users");
        }
    }
}
