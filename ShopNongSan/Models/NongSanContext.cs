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

    public virtual DbSet<DoiTra> DoiTras { get; set; }

    public virtual DbSet<DoiTraChiTiet> DoiTraChiTiets { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<DonHangChiTiet> DonHangChiTiets { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<GioHangChiTiet> GioHangChiTiets { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThongTinNguoiDung> ThongTinNguoiDungs { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TUANKIETLEE\\SQLSERVERNEW;Database=NongSan;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhMuc__3214EC071F8DDA0E");

            entity.ToTable("DanhMuc");

            entity.HasIndex(e => e.Ten, "UQ__DanhMuc__C451FA83ABD54D82").IsUnique();

            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<DoiTra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DoiTra__3214EC0763F492C6");

            entity.ToTable("DoiTra");

            entity.Property(e => e.LyDo).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.DonHang).WithMany(p => p.DoiTras)
                .HasForeignKey(d => d.DonHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoiTra_DonHang");
        });

        modelBuilder.Entity<DoiTraChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DoiTraCh__3214EC07C55DFF0E");

            entity.ToTable("DoiTraChiTiet");

            entity.HasOne(d => d.DoiTra).WithMany(p => p.DoiTraChiTiets)
                .HasForeignKey(d => d.DoiTraId)
                .HasConstraintName("FK_DRTCT_DoiTra");

            entity.HasOne(d => d.DonHangChiTiet).WithMany(p => p.DoiTraChiTiets)
                .HasForeignKey(d => d.DonHangChiTietId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DRTCT_DHCT");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DonHang__3214EC07D658CA63");

            entity.ToTable("DonHang");

            entity.HasIndex(e => e.MaDonHang, "UQ__DonHang__129584ACA72906BD").IsUnique();

            entity.Property(e => e.MaDonHang)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.TaiKhoanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DonHang_TaiKhoan");
        });

        modelBuilder.Entity<DonHangChiTiet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DonHangC__3214EC07FBC5BB09");

            entity.ToTable("DonHangChiTiet");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

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

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPham__3214EC0729DF377F");

            entity.ToTable("SanPham");

            entity.HasIndex(e => new { e.DanhMucId, e.ThuongHieuId, e.Gia }, "IX_SanPham_Loc");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HinhAnh).HasMaxLength(300);
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
            entity.HasKey(e => e.Id).HasName("PK__TaiKhoan__3214EC07D60341CD");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC0B46025BB").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__ThongTin__3214EC07DC8EDCDB");

            entity.ToTable("ThongTinNguoiDung");

            entity.HasIndex(e => e.TaiKhoanId, "UQ__ThongTin__9A124B4426867C18").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DiaChi).HasMaxLength(200);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);

            entity.HasOne(d => d.TaiKhoan).WithOne(p => p.ThongTinNguoiDung)
                .HasForeignKey<ThongTinNguoiDung>(d => d.TaiKhoanId)
                .HasConstraintName("FK_TTND_TaiKhoan");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThuongHi__3214EC07A075CB8D");

            entity.ToTable("ThuongHieu");

            entity.HasIndex(e => e.Ten, "UQ__ThuongHi__C451FA831A884426").IsUnique();

            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
