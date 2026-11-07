using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Slush.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Description", "ImageUrl", "Title", "XpReward" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "10 років на сайті", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918106/Property_1_golden-zubarik.svg", "Золотий зубарик", 500 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Опубліковано 20 постів кожного типу у Спільноті", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918355/Property_1_socialbird.svg", "Соціальна пташка", 100 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Куплено 50 ігор", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918330/Property_1_gamer.svg", "Справжній геймер", 220 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "5 років на сайті", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918291/Property_1_silver-zubarik.svg", "Срібний зубарик", 300 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Опубліковано 25 рецензій", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918368/Property_1_critic.svg", "Критик", 120 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "Додано до бібліотеки 10 ігор у жанрі \"карткова гра\"", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918258/Property_1_cardplayer.svg", "Картограй", 80 },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "1 рік на сайті", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918307/Property_1_bronze-zubarik.svg", "Бронзовий зубарик", 150 },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "Написано 15 гайдів", "https://res.cloudinary.com/asabjy7s/image/upload/v1793918320/Property_1_warrior.svg", "Воїн світла", 150 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));
        }
    }
}
