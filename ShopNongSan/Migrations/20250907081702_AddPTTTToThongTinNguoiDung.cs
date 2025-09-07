using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopNongSan.Migrations
{
    /// <inheritdoc />
    public partial class AddPTTTToThongTinNguoiDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhuongThucThanhToan",
                table: "ThongTinNguoiDung",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhuongThucThanhToan",
                table: "ThongTinNguoiDung");
        }
    }
}
