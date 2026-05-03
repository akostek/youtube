using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YtApi.Migrations
{
    /// <inheritdoc />
    public partial class FrequencyUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostsPerDay",
                table: "pipelines",
                newName: "FrequencyInMinutes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FrequencyInMinutes",
                table: "pipelines",
                newName: "PostsPerDay");
        }
    }
}
