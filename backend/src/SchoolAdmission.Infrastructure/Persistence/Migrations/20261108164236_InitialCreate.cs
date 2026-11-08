using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classes", x => x.id);
                    table.CheckConstraint("ck_classes_total_seats_range", "total_seats BETWEEN 1 AND 10000");
                });

            migrationBuilder.CreateTable(
                name: "admission_holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<string>(type: "character(17)", fixedLength: true, maxLength: 17, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_holds", x => x.id);
                    table.CheckConstraint("ck_admission_holds_cancelled_at", "(status = 'CANCELLED') = (cancelled_at IS NOT NULL)");
                    table.CheckConstraint("ck_admission_holds_confirmed_at", "(status = 'CONFIRMED') = (confirmed_at IS NOT NULL)");
                    table.CheckConstraint("ck_admission_holds_expiry_after_creation", "expires_at > created_at");
                    table.CheckConstraint("ck_admission_holds_status", "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                    table.CheckConstraint("ck_admission_holds_student_id_format", "student_id ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "fk_admission_holds_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_holds_class_status_expires",
                table: "admission_holds",
                columns: new[] { "class_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_holds_held_expires",
                table: "admission_holds",
                column: "expires_at",
                filter: "status = 'HELD'");

            migrationBuilder.CreateIndex(
                name: "ix_admission_holds_student_created",
                table: "admission_holds",
                columns: new[] { "student_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_admission_holds_active_student",
                table: "admission_holds",
                columns: new[] { "class_id", "student_id" },
                unique: true,
                filter: "status IN ('HELD', 'CONFIRMED')");

            migrationBuilder.CreateIndex(
                name: "ux_classes_normalized_name",
                table: "classes",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_holds");

            migrationBuilder.DropTable(
                name: "classes");
        }
    }
}
