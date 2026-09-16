using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeoOgAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "i_x_seo_page_settings_og_asset_id",
                table: "seo_page_settings",
                column: "og_asset_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_seo_page_settings_media_assets_og_asset_id",
                table: "seo_page_settings",
                column: "og_asset_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_seo_page_settings_media_assets_og_asset_id",
                table: "seo_page_settings");

            migrationBuilder.DropIndex(
                name: "i_x_seo_page_settings_og_asset_id",
                table: "seo_page_settings");
        }
    }
}
