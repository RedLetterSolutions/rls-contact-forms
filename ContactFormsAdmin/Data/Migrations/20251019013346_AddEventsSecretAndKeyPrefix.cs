using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactFormsAdmin.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsSecretAndKeyPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "events",
                table: "webhooks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secret",
                table: "webhooks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "key_prefix",
                table: "api_keys",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "events",
                table: "webhooks");

            migrationBuilder.DropColumn(
                name: "secret",
                table: "webhooks");

            migrationBuilder.DropColumn(
                name: "key_prefix",
                table: "api_keys");
        }
    }
}
