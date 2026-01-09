using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShopNongSan.Models;

public partial class NongSanContext : DbContext
{
    public NongSanContext()
    {
    }

    public NongSanContext(DbContextOptions<NongSanContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DemRateLimit> DemRateLimits { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<DonHangChiTiet> DonHangChiTiets { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<GioHangChiTiet> GioHangChiTiets { get; set; }

    public virtual DbSet<NhatKyDangNhap> NhatKyDangNhaps { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThongTinNguoiDung> ThongTinNguoiDungs { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NongSan;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhMuc__3214EC07B48DDB35");

            entity.ToTable("DanhMuc");

            entity.HasIndex(e => e.Ten, "UQ__DanhMuc__C451FA83AA6A5135").IsUnique();

            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<DemRateLimit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DemRateL__3214EC07E25B05AE");

            entity.ToTable("DemRateLimit");

            entity.HasIndex(e => new { e.GiaTriKhoa, e.CapNhatLuc }, "IX_DemRateLimit_Key_CapNhatLuc").IsDescending(false, true);

            entity.HasIndex(e => new { e.GiaTriKhoa, e.Endpoint, e.BatDauCuaSo, e.KetThucCuaSo }, "UX_DemRateLimit_Key_Window").IsUnique();

            entity.Property(e => e.CapNhatLuc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(100)
                .HasDefaultValue("/tai-khoan/dang-nhap");
            entity.Property(e => e.GiaTriKhoa).HasMaxLength(200);
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DonHang__3214EC074BFEB4D4");

            entity.ToTable("DonHang");

            entity.HasIndex(e => e.MaDonHang, "UQ__DonHang__129584AC20A845C6").IsUnique();

            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.GhiChu).HasMaxLength(300);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaDonHang)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NgayDat).HasColumnType("datetime");
            entity.Property(e => e.NgayGiao).HasColumnType("datetime");
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(20);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.TaiKhoanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DonHang_TaiKhoan");
        });

        modelBuilder.Entity<DonHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DonHangC__3214EC0789E683EB");

            entity.ToTable("DonHangChiTiet");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayDat).HasColumnType("datetime");
            entity.Property(e => e.NgayGiao).HasColumnType("datetime");

            entity.HasOne(d => d.DonHang).WithMany(p => p.DonHangChiTiets)
                .HasForeignKey(d => d.DonHangId)
                .HasConstraintName("FK_DHCT_DonHang");

            entity.HasOne(d => d.SanPham).WithMany(p => p.DonHangChiTiets)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DHCT_SanPham");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.ToTable("GioHang");

            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK_GioHang_TaiKhoan");
        });

        modelBuilder.Entity<GioHangChiTiet>(entity =>
        {
            entity.ToTable("GioHangChiTiet");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.GioHang).WithMany(p => p.GioHangChiTiets)
                .HasForeignKey(d => d.GioHangId)
                .HasConstraintName("FK_GHCT_GioHang");

            entity.HasOne(d => d.SanPham).WithMany(p => p.GioHangChiTiets)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GHCT_SanPham");
        });

        modelBuilder.Entity<NhatKyDangNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhatKyDa__3214EC071BB3DDFA");

            entity.ToTable("NhatKyDangNhap");

            entity.HasIndex(e => new { e.DiaChiIp, e.ThoiGian }, "IX_NhatKyDangNhap_DiaChiIP_ThoiGian").IsDescending(false, true);

            entity.HasIndex(e => new { e.KhoaRateLimit, e.ThoiGian }, "IX_NhatKyDangNhap_KhoaRateLimit_ThoiGian").IsDescending(false, true);

            entity.HasIndex(e => new { e.TaiKhoanId, e.ThoiGian }, "IX_NhatKyDangNhap_TaiKhoanId_ThoiGian").IsDescending(false, true);

            entity.HasIndex(e => new { e.TenDangNhap, e.ThoiGian }, "IX_NhatKyDangNhap_TenDangNhap_ThoiGian").IsDescending(false, true);

            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .HasColumnName("DiaChiIP");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(100)
                .HasDefaultValue("/tai-khoan/dang-nhap");
            entity.Property(e => e.KhoaRateLimit).HasMaxLength(200);
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(10)
                .HasDefaultValue("POST");
            entity.Property(e => e.TenDangNhap).HasMaxLength(50);
            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ThongBao).HasMaxLength(200);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPham__3214EC07D2D11B4F");

            entity.ToTable("SanPham");

            entity.HasIndex(e => new { e.DanhMucId, e.ThuongHieuId, e.Gia }, "IX_SanPham_Loc");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HinhAnh).HasMaxLength(300);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.Ten).HasMaxLength(150);

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.DanhMucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SanPham_DanhMuc");

            entity.HasOne(d => d.ThuongHieu).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.ThuongHieuId)
                .HasConstraintName("FK_SanPham_ThuongHieu");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaiKhoan__3214EC07D8DAD552");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC0BDFBE154").IsUnique();

            entity.HasIndex(e => e.TenDangNhap, "UX_TaiKhoan_TenDangNhap").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhau).HasMaxLength(200);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TenDangNhap).HasMaxLength(50);
            entity.Property(e => e.VaiTro)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ThongTinNguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThongTin__3214EC07A33E0D53");

            entity.ToTable("ThongTinNguoiDung");

            entity.HasIndex(e => e.TaiKhoanId, "UQ__ThongTin__9A124B447EAC6B6E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.GhiChu).HasMaxLength(300);
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(20);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);

            entity.HasOne(d => d.TaiKhoan).WithOne(p => p.ThongTinNguoiDung)
                .HasForeignKey<ThongTinNguoiDung>(d => d.TaiKhoanId)
                .HasConstraintName("FK_TTND_TaiKhoan");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThuongHi__3214EC0749EA4C89");

            entity.ToTable("ThuongHieu");

            entity.HasIndex(e => e.Ten, "UQ__ThuongHi__C451FA83C9F0ADCA").IsUnique();

            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
