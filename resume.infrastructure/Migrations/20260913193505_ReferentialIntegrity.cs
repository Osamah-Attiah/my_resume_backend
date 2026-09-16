using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReferentialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_profile_skills__resume_profiles_resume_profile_id",
                table: "profile_skills");

            migrationBuilder.DropIndex(
                name: "i_x_profile_skills_resume_profile_id",
                table: "profile_skills");

            migrationBuilder.DropColumn(
                name: "resume_profile_id",
                table: "profile_skills");

            migrationBuilder.CreateIndex(
                name: "i_x_sites_current_publication_id",
                table: "sites",
                column: "current_publication_id");

            migrationBuilder.CreateIndex(
                name: "i_x_seo_page_settings_profile_id",
                table: "seo_page_settings",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_seo_page_settings_project_id",
                table: "seo_page_settings",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "i_x_seo_page_settings_site_id",
                table: "seo_page_settings",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "i_x_publication_artifacts_profile_id",
                table: "publication_artifacts",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_skills_skill_id",
                table: "profile_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_links_person_link_id",
                table: "profile_links",
                column: "person_link_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_languages_spoken_language_id",
                table: "profile_languages",
                column: "spoken_language_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_experiences_experience_id",
                table: "profile_experiences",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_experience_highlights_highlight_id",
                table: "profile_experience_highlights",
                column: "highlight_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_educations_education_id",
                table: "profile_educations",
                column: "education_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_certifications_certification_id",
                table: "profile_certifications",
                column: "certification_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_certifications__resume_profiles_profile_id",
                table: "profile_certifications",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_certifications_certifications_certification_id",
                table: "profile_certifications",
                column: "certification_id",
                principalTable: "certifications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_educations__resume_profiles_profile_id",
                table: "profile_educations",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_educations_educations_education_id",
                table: "profile_educations",
                column: "education_id",
                principalTable: "educations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_experience_highlights_experience_highlights_highlig~",
                table: "profile_experience_highlights",
                column: "highlight_id",
                principalTable: "experience_highlights",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_experience_highlights_profile_experiences_profile_e~",
                table: "profile_experience_highlights",
                column: "profile_experience_id",
                principalTable: "profile_experiences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_experiences__resume_profiles_profile_id",
                table: "profile_experiences",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_experiences_experiences_experience_id",
                table: "profile_experiences",
                column: "experience_id",
                principalTable: "experiences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_languages__resume_profiles_profile_id",
                table: "profile_languages",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_languages__spoken_languages_spoken_language_id",
                table: "profile_languages",
                column: "spoken_language_id",
                principalTable: "spoken_languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_links__resume_profiles_profile_id",
                table: "profile_links",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_links_person_links_person_link_id",
                table: "profile_links",
                column: "person_link_id",
                principalTable: "person_links",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_skills__resume_profiles_profile_id",
                table: "profile_skills",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_skills__skills_skill_id",
                table: "profile_skills",
                column: "skill_id",
                principalTable: "skills",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_publication_artifacts__resume_profiles_profile_id",
                table: "publication_artifacts",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_publications__resume_profiles_profile_id",
                table: "publications",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_publications__sites_site_id",
                table: "publications",
                column: "site_id",
                principalTable: "sites",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_publications_persons_person_id",
                table: "publications",
                column: "person_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_seo_page_settings__sites_site_id",
                table: "seo_page_settings",
                column: "site_id",
                principalTable: "sites",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_seo_page_settings_projects_project_id",
                table: "seo_page_settings",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_seo_page_settings_resume_profiles_profile_id",
                table: "seo_page_settings",
                column: "profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_site_redirects_sites_site_id",
                table: "site_redirects",
                column: "site_id",
                principalTable: "sites",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_sites_publications_current_publication_id",
                table: "sites",
                column: "current_publication_id",
                principalTable: "publications",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_profile_certifications__resume_profiles_profile_id",
                table: "profile_certifications");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_certifications_certifications_certification_id",
                table: "profile_certifications");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_educations__resume_profiles_profile_id",
                table: "profile_educations");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_educations_educations_education_id",
                table: "profile_educations");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_experience_highlights_experience_highlights_highlig~",
                table: "profile_experience_highlights");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_experience_highlights_profile_experiences_profile_e~",
                table: "profile_experience_highlights");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_experiences__resume_profiles_profile_id",
                table: "profile_experiences");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_experiences_experiences_experience_id",
                table: "profile_experiences");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_languages__resume_profiles_profile_id",
                table: "profile_languages");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_languages__spoken_languages_spoken_language_id",
                table: "profile_languages");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_links__resume_profiles_profile_id",
                table: "profile_links");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_links_person_links_person_link_id",
                table: "profile_links");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_skills__resume_profiles_profile_id",
                table: "profile_skills");

            migrationBuilder.DropForeignKey(
                name: "f_k_profile_skills__skills_skill_id",
                table: "profile_skills");

            migrationBuilder.DropForeignKey(
                name: "f_k_publication_artifacts__resume_profiles_profile_id",
                table: "publication_artifacts");

            migrationBuilder.DropForeignKey(
                name: "f_k_publications__resume_profiles_profile_id",
                table: "publications");

            migrationBuilder.DropForeignKey(
                name: "f_k_publications__sites_site_id",
                table: "publications");

            migrationBuilder.DropForeignKey(
                name: "f_k_publications_persons_person_id",
                table: "publications");

            migrationBuilder.DropForeignKey(
                name: "f_k_seo_page_settings__sites_site_id",
                table: "seo_page_settings");

            migrationBuilder.DropForeignKey(
                name: "f_k_seo_page_settings_projects_project_id",
                table: "seo_page_settings");

            migrationBuilder.DropForeignKey(
                name: "f_k_seo_page_settings_resume_profiles_profile_id",
                table: "seo_page_settings");

            migrationBuilder.DropForeignKey(
                name: "f_k_site_redirects_sites_site_id",
                table: "site_redirects");

            migrationBuilder.DropForeignKey(
                name: "f_k_sites_publications_current_publication_id",
                table: "sites");

            migrationBuilder.DropIndex(
                name: "i_x_sites_current_publication_id",
                table: "sites");

            migrationBuilder.DropIndex(
                name: "i_x_seo_page_settings_profile_id",
                table: "seo_page_settings");

            migrationBuilder.DropIndex(
                name: "i_x_seo_page_settings_project_id",
                table: "seo_page_settings");

            migrationBuilder.DropIndex(
                name: "i_x_seo_page_settings_site_id",
                table: "seo_page_settings");

            migrationBuilder.DropIndex(
                name: "i_x_publication_artifacts_profile_id",
                table: "publication_artifacts");

            migrationBuilder.DropIndex(
                name: "i_x_profile_skills_skill_id",
                table: "profile_skills");

            migrationBuilder.DropIndex(
                name: "i_x_profile_links_person_link_id",
                table: "profile_links");

            migrationBuilder.DropIndex(
                name: "i_x_profile_languages_spoken_language_id",
                table: "profile_languages");

            migrationBuilder.DropIndex(
                name: "i_x_profile_experiences_experience_id",
                table: "profile_experiences");

            migrationBuilder.DropIndex(
                name: "i_x_profile_experience_highlights_highlight_id",
                table: "profile_experience_highlights");

            migrationBuilder.DropIndex(
                name: "i_x_profile_educations_education_id",
                table: "profile_educations");

            migrationBuilder.DropIndex(
                name: "i_x_profile_certifications_certification_id",
                table: "profile_certifications");

            migrationBuilder.AddColumn<Guid>(
                name: "resume_profile_id",
                table: "profile_skills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_skills_resume_profile_id",
                table: "profile_skills",
                column: "resume_profile_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_profile_skills__resume_profiles_resume_profile_id",
                table: "profile_skills",
                column: "resume_profile_id",
                principalTable: "resume_profiles",
                principalColumn: "id");
        }
    }
}
