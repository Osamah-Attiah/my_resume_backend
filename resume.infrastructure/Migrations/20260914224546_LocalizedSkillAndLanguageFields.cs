using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace resume.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocalizedSkillAndLanguageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "proficiency_ar",
                table: "spoken_languages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "proficiency_en",
                table: "spoken_languages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category_ar",
                table: "skills",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category_en",
                table: "skills",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE skills
                SET category_en = category,
                    category_ar = CASE lower(trim(category))
                        WHEN 'mobile' THEN 'تطبيقات الهاتف المحمول'
                        WHEN 'backend' THEN 'الخدمات الخلفية'
                        WHEN 'data' THEN 'البيانات'
                        WHEN 'frontend' THEN 'الواجهات الأمامية'
                        WHEN 'devops' THEN 'عمليات التطوير'
                        WHEN 'testing' THEN 'الاختبارات'
                        ELSE category
                    END
                WHERE category_en = '' OR category_ar = '';
                """);

            migrationBuilder.Sql("""
                UPDATE spoken_languages
                SET proficiency_en = proficiency,
                    proficiency_ar = CASE lower(trim(proficiency))
                        WHEN 'native' THEN 'اللغة الأم'
                        WHEN 'fluent' THEN 'بطلاقة'
                        WHEN 'professional' THEN 'مستوى مهني'
                        WHEN 'intermediate' THEN 'متوسط'
                        WHEN 'basic' THEN 'أساسي'
                        WHEN 'beginner' THEN 'مبتدئ'
                        ELSE proficiency
                    END
                WHERE proficiency_en = '' OR proficiency_ar = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "proficiency_ar",
                table: "spoken_languages");

            migrationBuilder.DropColumn(
                name: "proficiency_en",
                table: "spoken_languages");

            migrationBuilder.DropColumn(
                name: "category_ar",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "category_en",
                table: "skills");
        }
    }
}
