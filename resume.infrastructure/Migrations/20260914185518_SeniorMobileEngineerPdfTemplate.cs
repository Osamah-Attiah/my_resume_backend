using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeniorMobileEngineerPdfTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET template_key = 'senior-mobile-engineer-v1', paper_size = 'Letter', font_size = 10.5, margin_mm = 15, target_pages = 2
                WHERE template_key IN ('ats-classic-v1', 'classic');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "template_key",
                table: "profile_pdf_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "senior-mobile-engineer-v1",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<int>(
                name: "target_pages",
                table: "profile_pdf_settings",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "paper_size",
                table: "profile_pdf_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Letter",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<decimal>(
                name: "margin_mm",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 15m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 10.5m,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET template_key = 'ats-classic-v1', paper_size = 'A4', font_size = 11, margin_mm = 15, target_pages = 2
                WHERE template_key = 'senior-mobile-engineer-v1';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "template_key",
                table: "profile_pdf_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldDefaultValue: "senior-mobile-engineer-v1");

            migrationBuilder.AlterColumn<int>(
                name: "target_pages",
                table: "profile_pdf_settings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<string>(
                name: "paper_size",
                table: "profile_pdf_settings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldDefaultValue: "Letter");

            migrationBuilder.AlterColumn<decimal>(
                name: "margin_mm",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 15m);

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 10.5m);
        }
    }
}
