using Microsoft.EntityFrameworkCore.Migrations;

namespace ImageUploader.Data.Migrations
{
    public partial class Initailize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(maxLength: 250, nullable: false),
                    Url = table.Column<string>(maxLength: 2048, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Images", x => x.Id));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
