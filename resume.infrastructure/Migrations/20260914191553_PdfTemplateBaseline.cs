using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PdfTemplateBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET font_size = 9.5
                WHERE template_key = 'senior-mobile-engineer-v1';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 9.5m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 10m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE profile_pdf_settings
                SET font_size = 10
                WHERE template_key = 'senior-mobile-engineer-v1';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "font_size",
                table: "profile_pdf_settings",
                type: "numeric",
                nullable: false,
                defaultValue: 10m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 9.5m);
        }
    }
}
