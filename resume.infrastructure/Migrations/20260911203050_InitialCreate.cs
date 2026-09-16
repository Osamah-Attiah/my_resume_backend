using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    delivery_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    sha256 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_media_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    photo_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_locale = table.Column<string>(type: "text", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_persons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profile_certifications",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_certifications", x => new { x.profile_id, x.certification_id });
                });

            migrationBuilder.CreateTable(
                name: "profile_educations",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    education_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_educations", x => new { x.profile_id, x.education_id });
                });

            migrationBuilder.CreateTable(
                name: "profile_experience_highlights",
                columns: table => new
                {
                    profile_experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    highlight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_experience_highlights", x => new { x.profile_experience_id, x.highlight_id });
                });

            migrationBuilder.CreateTable(
                name: "profile_experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_experiences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profile_languages",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spoken_language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_languages", x => new { x.profile_id, x.spoken_language_id });
                });

            migrationBuilder.CreateTable(
                name: "profile_links",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_links", x => new { x.profile_id, x.person_link_id });
                });

            migrationBuilder.CreateTable(
                name: "publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: true),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_publications", x => x.id);
                    table.CheckConstraint("ck_publication_target", "(site_id IS NULL) <> (profile_id IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "seo_page_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    page_kind = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title_override = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description_override = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    canonical_override_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    og_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    index_override = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_seo_page_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "site_deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_deployment_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    deployment_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    deployed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_site_deployments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "site_redirects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    target_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_site_redirects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_asset_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    caption = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_media_asset_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_media_asset_translations_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    session_version = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_admin_users", x => x.id);
                    table.ForeignKey(
                        name: "f_k_admin_users__persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    credential_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    credential_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_certifications", x => x.id);
                    table.ForeignKey(
                        name: "f_k_certifications__persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "educations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_educations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_educations__persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employment_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    organization_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_experiences", x => x.id);
                    table.ForeignKey(
                        name: "f_k_experiences__persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_person_links", x => x.id);
                    table.ForeignKey(
                        name: "f_k_person_links_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    country = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_headline = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_person_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_person_translations_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_ongoing = table.Column<bool>(type: "boolean", nullable: false),
                    repository_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    demo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cover_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_projects", x => x.id);
                    table.ForeignKey(
                        name: "f_k_projects_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resume_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    internal_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    slug = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    default_locale = table.Column<string>(type: "text", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_resume_profiles", x => x.id);
                    table.ForeignKey(
                        name: "f_k_resume_profiles_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    slug = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    base_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_locale = table.Column<string>(type: "text", nullable: false),
                    theme_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    deployment_target_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    current_publication_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_primary_identity_site = table.Column<bool>(type: "boolean", nullable: false),
                    search_verification_token = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sites", x => x.id);
                    table.ForeignKey(
                        name: "f_k_sites_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_skills", x => x.id);
                    table.ForeignKey(
                        name: "f_k_skills_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spoken_languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    proficiency = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_spoken_languages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_spoken_languages_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "publication_artifacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locale = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    template_version = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    path_or_artifact_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    qa_report = table.Column<string>(type: "jsonb", nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_publication_artifacts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_publication_artifacts_publications_publication_id",
                        column: x => x.publication_id,
                        principalTable: "publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "publication_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    workflow_run_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    state = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_publication_attempts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_publication_attempts_publications_publication_id",
                        column: x => x.publication_id,
                        principalTable: "publications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certification_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    certification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    issuer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_certification_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_certification_translations_certifications_certification_id",
                        column: x => x.certification_id,
                        principalTable: "certifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "education_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    education_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    institution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    degree = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    field_of_study = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_education_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_education_translations_educations_education_id",
                        column: x => x.education_id,
                        principalTable: "educations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_highlights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_experience_highlights", x => x.id);
                    table.ForeignKey(
                        name: "f_k_experience_highlights_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    organization = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    job_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_experience_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_experience_translations_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_link_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_person_link_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_person_link_translations_person_links_person_link_id",
                        column: x => x.person_link_id,
                        principalTable: "person_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_highlights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_project_highlights", x => x.id);
                    table.ForeignKey(
                        name: "f_k_project_highlights_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_project_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_project_translations_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_contact_settings",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_email = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_email = table.Column<bool>(type: "boolean", nullable: false),
                    web_phone = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_phone = table.Column<bool>(type: "boolean", nullable: false),
                    show_location = table.Column<bool>(type: "boolean", nullable: false),
                    show_photo_web = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_contact_settings", x => x.profile_id);
                    table.ForeignKey(
                        name: "f_k_profile_contact_settings__resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_pdf_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    template_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    paper_size = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    font_size = table.Column<decimal>(type: "numeric", nullable: false),
                    margin_mm = table.Column<decimal>(type: "numeric", nullable: false),
                    target_pages = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_pdf_settings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_profile_pdf_settings__resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_projects", x => x.id);
                    table.ForeignKey(
                        name: "f_k_profile_projects__projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_profile_projects__resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_sections", x => x.id);
                    table.ForeignKey(
                        name: "f_k_profile_sections__resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_skills",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    web_order = table.Column<int>(type: "integer", nullable: false),
                    pdf_order = table.Column<int>(type: "integer", nullable: false),
                    resume_profile_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_skills", x => new { x.profile_id, x.skill_id });
                    table.ForeignKey(
                        name: "f_k_profile_skills__resume_profiles_resume_profile_id",
                        column: x => x.resume_profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "resume_profile_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    seo_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_resume_profile_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_resume_profile_translations_resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_locales",
                columns: table => new
                {
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_site_locales", x => new { x.site_id, x.locale });
                    table.ForeignKey(
                        name: "f_k_site_locales_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path_slug = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    indexable = table.Column<bool>(type: "boolean", nullable: false),
                    is_listed = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_site_profiles", x => x.id);
                    table.ForeignKey(
                        name: "f_k_site_profiles_resume_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "resume_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_site_profiles_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_site_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_site_translations_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_skills",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_project_skills", x => new { x.project_id, x.skill_id });
                    table.ForeignKey(
                        name: "f_k_project_skills__skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_project_skills_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_skill_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_skill_translations_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_highlight_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    highlight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_experience_highlight_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_experience_highlight_translations_experience_highlights_hig~",
                        column: x => x.highlight_id,
                        principalTable: "experience_highlights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_highlight_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    highlight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_project_highlight_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_project_highlight_translations_project_highlights_highlight~",
                        column: x => x.highlight_id,
                        principalTable: "project_highlights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_project_highlights",
                columns: table => new
                {
                    profile_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    highlight_id = table.Column<Guid>(type: "uuid", nullable: false),
                    web_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_project_highlights", x => new { x.profile_project_id, x.highlight_id });
                    table.ForeignKey(
                        name: "f_k_profile_project_highlights__project_highlights_highlight_id",
                        column: x => x.highlight_id,
                        principalTable: "project_highlights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_profile_project_highlights_profile_projects_profile_project~",
                        column: x => x.profile_project_id,
                        principalTable: "profile_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_project_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "text", nullable: false),
                    summary_override = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profile_project_translations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_profile_project_translations_profile_projects_profile_proje~",
                        column: x => x.profile_project_id,
                        principalTable: "profile_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_admin_users_normalized_email",
                table: "admin_users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_admin_users_person_id",
                table: "admin_users",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_certification_translations_certification_id_locale",
                table: "certification_translations",
                columns: new[] { "certification_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_certifications_person_id",
                table: "certifications",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_education_translations_education_id_locale",
                table: "education_translations",
                columns: new[] { "education_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_educations_person_id",
                table: "educations",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_experience_highlight_translations_highlight_id_locale",
                table: "experience_highlight_translations",
                columns: new[] { "highlight_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_experience_highlights_experience_id",
                table: "experience_highlights",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "i_x_experience_translations_experience_id_locale",
                table: "experience_translations",
                columns: new[] { "experience_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_experiences_person_id",
                table: "experiences",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_media_asset_translations_asset_id_locale",
                table: "media_asset_translations",
                columns: new[] { "asset_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_media_assets_person_id_sha256",
                table: "media_assets",
                columns: new[] { "person_id", "sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_person_link_translations_person_link_id_locale",
                table: "person_link_translations",
                columns: new[] { "person_link_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_person_links_person_id",
                table: "person_links",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_person_translations_person_id_locale",
                table: "person_translations",
                columns: new[] { "person_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_persons_archived_at",
                table: "persons",
                column: "archived_at",
                unique: true,
                filter: "archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_experiences_profile_id_experience_id",
                table: "profile_experiences",
                columns: new[] { "profile_id", "experience_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_pdf_settings_profile_id_locale",
                table: "profile_pdf_settings",
                columns: new[] { "profile_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_project_highlights_highlight_id",
                table: "profile_project_highlights",
                column: "highlight_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_project_translations_profile_project_id_locale",
                table: "profile_project_translations",
                columns: new[] { "profile_project_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_projects_profile_id_project_id",
                table: "profile_projects",
                columns: new[] { "profile_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_projects_project_id",
                table: "profile_projects",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profile_sections_profile_id_section_key",
                table: "profile_sections",
                columns: new[] { "profile_id", "section_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_profile_skills_resume_profile_id",
                table: "profile_skills",
                column: "resume_profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_project_highlight_translations_highlight_id_locale",
                table: "project_highlight_translations",
                columns: new[] { "highlight_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_project_highlights_project_id",
                table: "project_highlights",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "i_x_project_skills_skill_id",
                table: "project_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "i_x_project_translations_project_id_locale",
                table: "project_translations",
                columns: new[] { "project_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_projects_person_id_slug",
                table: "projects",
                columns: new[] { "person_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_publication_artifacts_publication_id",
                table: "publication_artifacts",
                column: "publication_id");

            migrationBuilder.CreateIndex(
                name: "i_x_publication_attempts_publication_id_attempt_number",
                table: "publication_attempts",
                columns: new[] { "publication_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_publications_person_id_idempotency_key",
                table: "publications",
                columns: new[] { "person_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_publications_profile_id_revision",
                table: "publications",
                columns: new[] { "profile_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_publications_site_id_revision",
                table: "publications",
                columns: new[] { "site_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_resume_profile_translations_profile_id_locale",
                table: "resume_profile_translations",
                columns: new[] { "profile_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_resume_profiles_person_id_slug",
                table: "resume_profiles",
                columns: new[] { "person_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_site_profiles_profile_id",
                table: "site_profiles",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_site_profiles_site_id",
                table: "site_profiles",
                column: "site_id",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "i_x_site_profiles_site_id_path_slug",
                table: "site_profiles",
                columns: new[] { "site_id", "path_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_site_profiles_site_id_profile_id",
                table: "site_profiles",
                columns: new[] { "site_id", "profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_site_redirects_site_id_source_path",
                table: "site_redirects",
                columns: new[] { "site_id", "source_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_site_translations_site_id_locale",
                table: "site_translations",
                columns: new[] { "site_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_sites_person_id",
                table: "sites",
                column: "person_id",
                unique: true,
                filter: "is_primary_identity_site = true");

            migrationBuilder.CreateIndex(
                name: "i_x_sites_person_id_slug",
                table: "sites",
                columns: new[] { "person_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_skill_translations_skill_id_locale",
                table: "skill_translations",
                columns: new[] { "skill_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_skills_person_id_canonical_name",
                table: "skills",
                columns: new[] { "person_id", "canonical_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_spoken_languages_person_id_language_code",
                table: "spoken_languages",
                columns: new[] { "person_id", "language_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "certification_translations");

            migrationBuilder.DropTable(
                name: "education_translations");

            migrationBuilder.DropTable(
                name: "experience_highlight_translations");

            migrationBuilder.DropTable(
                name: "experience_translations");

            migrationBuilder.DropTable(
                name: "media_asset_translations");

            migrationBuilder.DropTable(
                name: "person_link_translations");

            migrationBuilder.DropTable(
                name: "person_translations");

            migrationBuilder.DropTable(
                name: "profile_certifications");

            migrationBuilder.DropTable(
                name: "profile_contact_settings");

            migrationBuilder.DropTable(
                name: "profile_educations");

            migrationBuilder.DropTable(
                name: "profile_experience_highlights");

            migrationBuilder.DropTable(
                name: "profile_experiences");

            migrationBuilder.DropTable(
                name: "profile_languages");

            migrationBuilder.DropTable(
                name: "profile_links");

            migrationBuilder.DropTable(
                name: "profile_pdf_settings");

            migrationBuilder.DropTable(
                name: "profile_project_highlights");

            migrationBuilder.DropTable(
                name: "profile_project_translations");

            migrationBuilder.DropTable(
                name: "profile_sections");

            migrationBuilder.DropTable(
                name: "profile_skills");

            migrationBuilder.DropTable(
                name: "project_highlight_translations");

            migrationBuilder.DropTable(
                name: "project_skills");

            migrationBuilder.DropTable(
                name: "project_translations");

            migrationBuilder.DropTable(
                name: "publication_artifacts");

            migrationBuilder.DropTable(
                name: "publication_attempts");

            migrationBuilder.DropTable(
                name: "resume_profile_translations");

            migrationBuilder.DropTable(
                name: "seo_page_settings");

            migrationBuilder.DropTable(
                name: "site_deployments");

            migrationBuilder.DropTable(
                name: "site_locales");

            migrationBuilder.DropTable(
                name: "site_profiles");

            migrationBuilder.DropTable(
                name: "site_redirects");

            migrationBuilder.DropTable(
                name: "site_translations");

            migrationBuilder.DropTable(
                name: "skill_translations");

            migrationBuilder.DropTable(
                name: "spoken_languages");

            migrationBuilder.DropTable(
                name: "certifications");

            migrationBuilder.DropTable(
                name: "educations");

            migrationBuilder.DropTable(
                name: "experience_highlights");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "person_links");

            migrationBuilder.DropTable(
                name: "profile_projects");

            migrationBuilder.DropTable(
                name: "project_highlights");

            migrationBuilder.DropTable(
                name: "publications");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "experiences");

            migrationBuilder.DropTable(
                name: "resume_profiles");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "persons");
        }
    }
}
