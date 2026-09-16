using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PdfTemplateMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET font_size = 10, margin_mm = 16.5
                WHERE template_key = 'senior-mobile-engineer-v1';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "margin_mm",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 16.5m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 15m);

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 10m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 10.5m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET font_size = 10.5, margin_mm = 15
                WHERE template_key = 'senior-mobile-engineer-v1';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "margin_mm",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 15m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 16.5m);

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 10.5m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 10m);
        }
    }
}
