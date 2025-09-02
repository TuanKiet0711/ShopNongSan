using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopNongSan.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DanhMuc__3214EC071F8DDA0E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TaiKhoan__3214EC07D60341CD", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThuongHieu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ThuongHi__3214EC07A075CB8D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonHang",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonHang = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TaiKhoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DonHang__3214EC07D658CA63", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHang_TaiKhoan",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GioHang",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GioHang_TaiKhoan",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinNguoiDung",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    TaiKhoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ThongTin__3214EC07DC8EDCDB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TTND_TaiKhoan",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DanhMucId = table.Column<int>(type: "int", nullable: false),
                    ThuongHieuId = table.Column<int>(type: "int", nullable: true),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SanPham__3214EC0729DF377F", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanPham_DanhMuc",
                        column: x => x.DanhMucId,
                        principalTable: "DanhMuc",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SanPham_ThuongHieu",
                        column: x => x.ThuongHieuId,
                        principalTable: "ThuongHieu",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DoiTra",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<long>(type: "bigint", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DoiTra__3214EC0763F492C6", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoiTra_DonHang",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DonHangChiTiet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangId = table.Column<long>(type: "bigint", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DonHangC__3214EC07FBC5BB09", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DHCT_DonHang",
                        column: x => x.DonHangId,
                        principalTable: "DonHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DHCT_SanPham",
                        column: x => x.SanPhamId,
                        principalTable: "SanPham",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GioHangChiTiet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GioHangId = table.Column<long>(type: "bigint", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHangChiTiet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GHCT_GioHang",
                        column: x => x.GioHangId,
                        principalTable: "GioHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GHCT_SanPham",
                        column: x => x.SanPhamId,
                        principalTable: "SanPham",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DoiTraChiTiet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoiTraId = table.Column<long>(type: "bigint", nullable: false),
                    DonHangChiTietId = table.Column<long>(type: "bigint", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DoiTraCh__3214EC07C55DFF0E", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DRTCT_DHCT",
                        column: x => x.DonHangChiTietId,
                        principalTable: "DonHangChiTiet",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DRTCT_DoiTra",
                        column: x => x.DoiTraId,
                        principalTable: "DoiTra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ__DanhMuc__C451FA83ABD54D82",
                table: "DanhMuc",
                column: "Ten",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoiTra_DonHangId",
                table: "DoiTra",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DoiTraChiTiet_DoiTraId",
                table: "DoiTraChiTiet",
                column: "DoiTraId");

            migrationBuilder.CreateIndex(
                name: "IX_DoiTraChiTiet_DonHangChiTietId",
                table: "DoiTraChiTiet",
                column: "DonHangChiTietId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_TaiKhoanId",
                table: "DonHang",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "UQ__DonHang__129584ACA72906BD",
                table: "DonHang",
                column: "MaDonHang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiet_DonHangId",
                table: "DonHangChiTiet",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiet_SanPhamId",
                table: "DonHangChiTiet",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_TaiKhoanId",
                table: "GioHang",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangChiTiet_GioHangId",
                table: "GioHangChiTiet",
                column: "GioHangId");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangChiTiet_SanPhamId",
                table: "GioHangChiTiet",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_Loc",
                table: "SanPham",
                columns: new[] { "DanhMucId", "ThuongHieuId", "Gia" });

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_ThuongHieuId",
                table: "SanPham",
                column: "ThuongHieuId");

            migrationBuilder.CreateIndex(
                name: "UQ__TaiKhoan__55F68FC0B46025BB",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ThongTin__9A124B4426867C18",
                table: "ThongTinNguoiDung",
                column: "TaiKhoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ThuongHi__C451FA831A884426",
                table: "ThuongHieu",
                column: "Ten",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoiTraChiTiet");

            migrationBuilder.DropTable(
                name: "GioHangChiTiet");

            migrationBuilder.DropTable(
                name: "ThongTinNguoiDung");

            migrationBuilder.DropTable(
                name: "DonHangChiTiet");

            migrationBuilder.DropTable(
                name: "DoiTra");

            migrationBuilder.DropTable(
                name: "GioHang");

            migrationBuilder.DropTable(
                name: "SanPham");

            migrationBuilder.DropTable(
                name: "DonHang");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "ThuongHieu");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
