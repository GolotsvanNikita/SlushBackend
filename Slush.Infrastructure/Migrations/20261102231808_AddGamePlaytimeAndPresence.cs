using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slush.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamePlaytimeAndPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentGameId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentGameTitle",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentSessionStartedAt",
                table: "UserGames",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPlayedAt",
                table: "UserGames",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlaytimeMinutes",
                table: "UserGames",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentGameId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentGameTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentSessionStartedAt",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "LastPlayedAt",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "PlaytimeMinutes",
                table: "UserGames");
        }
    }
}
