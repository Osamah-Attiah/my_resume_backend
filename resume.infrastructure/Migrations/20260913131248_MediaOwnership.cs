using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediaOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "i_x_projects_cover_asset_id",
                table: "projects",
                column: "cover_asset_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_media_assets__persons_person_id",
                table: "media_assets",
                column: "person_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_projects_media_assets_cover_asset_id",
                table: "projects",
                column: "cover_asset_id",
                principalTable: "media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_media_assets__persons_person_id",
                table: "media_assets");

            migrationBuilder.DropForeignKey(
                name: "f_k_projects_media_assets_cover_asset_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "i_x_projects_cover_asset_id",
                table: "projects");
        }
    }
}
