using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopNongSan.Migrations
{
    /// <inheritdoc />
    public partial class AddGhiChuToThongTinNguoiDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "ThongTinNguoiDung",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "ThongTinNguoiDung");
        }
    }
}
