using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublicationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "i_x_publication_artifacts_publication_id",
                table: "publication_artifacts");

            migrationBuilder.CreateIndex(
                name: "i_x_site_deployments_attempt_id",
                table: "site_deployments",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_site_deployments_publication_id",
                table: "site_deployments",
                column: "publication_id");

            migrationBuilder.CreateIndex(
                name: "i_x_site_deployments_site_id",
                table: "site_deployments",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "i_x_publication_artifacts_publication_id",
                table: "publication_artifacts",
                column: "publication_id",
                unique: true,
                filter: "kind = 'Manifest'");

            migrationBuilder.CreateIndex(
                name: "i_x_publication_artifacts_publication_id_profile_id_locale",
                table: "publication_artifacts",
                columns: new[] { "publication_id", "profile_id", "locale" },
                unique: true,
                filter: "kind = 'Pdf'");

            migrationBuilder.AddForeignKey(
                name: "f_k_site_deployments_publication_attempts_attempt_id",
                table: "site_deployments",
                column: "attempt_id",
                principalTable: "publication_attempts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_site_deployments_publications_publication_id",
                table: "site_deployments",
                column: "publication_id",
                principalTable: "publications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_site_deployments_sites_site_id",
                table: "site_deployments",
                column: "site_id",
                principalTable: "sites",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_site_deployments_publication_attempts_attempt_id",
                table: "site_deployments");

            migrationBuilder.DropForeignKey(
                name: "f_k_site_deployments_publications_publication_id",
                table: "site_deployments");

            migrationBuilder.DropForeignKey(
                name: "f_k_site_deployments_sites_site_id",
                table: "site_deployments");

            migrationBuilder.DropIndex(
                name: "i_x_site_deployments_attempt_id",
                table: "site_deployments");

            migrationBuilder.DropIndex(
                name: "i_x_site_deployments_publication_id",
                table: "site_deployments");

            migrationBuilder.DropIndex(
                name: "i_x_site_deployments_site_id",
                table: "site_deployments");

            migrationBuilder.DropIndex(
                name: "i_x_publication_artifacts_publication_id",
                table: "publication_artifacts");

            migrationBuilder.DropIndex(
                name: "i_x_publication_artifacts_publication_id_profile_id_locale",
                table: "publication_artifacts");

            migrationBuilder.CreateIndex(
                name: "i_x_publication_artifacts_publication_id",
                table: "publication_artifacts",
                column: "publication_id");
        }
    }
}
