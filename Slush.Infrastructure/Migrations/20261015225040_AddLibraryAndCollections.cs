using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slush.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryAndCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserGames_UserId",
                table: "UserGames");

            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "UserGames",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Developer",
                table: "UserGames",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiskSize",
                table: "UserGames",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderImage",
                table: "UserGames",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "UserGames",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstalled",
                table: "UserGames",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReleaseDate",
                table: "UserGames",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LibraryCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryCollections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGames_CollectionId",
                table: "UserGames",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGames_UserId_GameId",
                table: "UserGames",
                columns: new[] { "UserId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryCollections_UserId",
                table: "LibraryCollections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGames_LibraryCollections_CollectionId",
                table: "UserGames",
                column: "CollectionId",
                principalTable: "LibraryCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGames_LibraryCollections_CollectionId",
                table: "UserGames");

            migrationBuilder.DropTable(
                name: "LibraryCollections");

            migrationBuilder.DropIndex(
                name: "IX_UserGames_CollectionId",
                table: "UserGames");

            migrationBuilder.DropIndex(
                name: "IX_UserGames_UserId_GameId",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "Developer",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "DiskSize",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "HeaderImage",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "IsInstalled",
                table: "UserGames");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "UserGames");

            migrationBuilder.CreateIndex(
                name: "IX_UserGames_UserId",
                table: "UserGames",
                column: "UserId");
        }
    }
}
