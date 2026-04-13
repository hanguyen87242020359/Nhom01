using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

public partial class ShopHoaLyLyContext : DbContext
{
    public ShopHoaLyLyContext()
    {
    }

    public ShopHoaLyLyContext(DbContextOptions<ShopHoaLyLyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<DanhGia> DanhGia { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HinhAnh> HinhAnhs { get; set; }

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }

    public virtual DbSet<PhuongXa> PhuongXas { get; set; }

    public virtual DbSet<QuanHuyen> QuanHuyens { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TinTuc> TinTucs { get; set; }

    public virtual DbSet<TrangThaiDonHang> TrangThaiDonHangs { get; set; }

    public virtual DbSet<YeuThich> YeuThiches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => new { e.MaDonHang, e.MaSanPham }).HasName("PK__ChiTietD__DD39F0EF46E07898");

            entity.Property(e => e.ThanhTien).HasComputedColumnSql("([DonGia]*[SoLuongDat])", true);

            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__MaDon__59FA5E80");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.ChiTietDonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDo__MaSan__5AEE82B9");
        });

        modelBuilder.Entity<DanhGia>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGia__AA9515BF1710576A");

            entity.Property(e => e.NgayDanhGia).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ChiTietDonHang)
                .WithOne(p => p.DanhGia)
                .HasForeignKey<DanhGia>(d => new { d.MaDonHang, d.MaSanPham })
                .HasConstraintName("FK__DanhGia__6477ECF3");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.DanhGia).HasConstraintName("FK__DanhGia__MaSanPh__6477ECF3");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK__DanhMuc__B3750887810D8ACE");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.MaDonHang).HasName("PK__DonHang__129584AD19039AEB");

            entity.Property(e => e.PhiVanChuyen).HasDefaultValue(0m);
            entity.Property(e => e.TrangThaiThanhToan).HasDefaultValue(false);

            entity.HasOne(d => d.MaPhuongThucThanhToanNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK__DonHang__MaPhuon__5629CD9C");

            entity.HasOne(d => d.MaTaiKhoanNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK__DonHang__MaTaiKh__5535A963");

            entity.HasOne(d => d.MaTrangThaiDonHangNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK__DonHang__MaTrang__571DF1D5");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => new { e.MaKhachHang, e.MaSanPham }).HasName("PK__GioHang__477E84A703FE3367");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.GioHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GioHang__MaKhach__6754599E");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.GioHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GioHang__MaSanPh__68487DD7");
        });

        modelBuilder.Entity<HinhAnh>(entity =>
        {
            entity.HasKey(e => e.MaAnh).HasName("PK__HinhAnh__356240DF8920E470");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.HinhAnhs).HasConstraintName("FK__HinhAnh__MaSanPh__4E88ABD4");
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaPhuongThucThanhToan).HasName("PK__PhuongTh__D02708991F3AA465");

            entity.Property(e => e.MaPhuongThucThanhToan).ValueGeneratedNever();
        });

        modelBuilder.Entity<PhuongXa>(entity =>
        {
            entity.HasKey(e => e.MaPhuongXa).HasName("PK__PhuongXa__55F1EFA9684D4688");

            entity.Property(e => e.MaPhuongXa).ValueGeneratedNever();

            entity.HasOne(d => d.MaQuanHuyenNavigation).WithMany(p => p.PhuongXas).HasConstraintName("FK__PhuongXa__MaQuan__3B75D760");
        });

        modelBuilder.Entity<QuanHuyen>(entity =>
        {
            entity.HasKey(e => e.MaQuanHuyen).HasName("PK__QuanHuye__B86B827ABDF36628");

            entity.Property(e => e.MaQuanHuyen).ValueGeneratedNever();
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.MaQuyen).HasName("PK__Quyen__1D4B7ED4B1535DC0");

            entity.Property(e => e.MaQuyen).ValueGeneratedNever();
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.MaSanPham).HasName("PK__SanPham__FAC7442D2E7DD7EC");

            entity.HasOne(d => d.MaDanhMucNavigation).WithMany(p => p.SanPhams).HasConstraintName("FK__SanPham__MaDanhM__4BAC3F29");

            entity.HasQueryFilter(sp => !sp.DaXoa);
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.MaTaiKhoan).HasName("PK__TaiKhoan__AD7C652977288199");

            entity.Property(e => e.TrangThaiTaiKhoan).HasDefaultValue(true);

            entity.HasOne(d => d.MaPhuongXaNavigation).WithMany(p => p.TaiKhoans).HasConstraintName("FK__TaiKhoan__MaPhuo__3F466844");

            entity.HasOne(d => d.MaQuyenNavigation).WithMany(p => p.TaiKhoans).HasConstraintName("FK__TaiKhoan__MaQuye__403A8C7D");
        });

        modelBuilder.Entity<TinTuc>(entity =>
        {
            entity.HasKey(e => e.MaTinTuc).HasName("PK__TinTuc__B53648C0712C23C7");

            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThaiHienThi).HasDefaultValue(false);

            entity.HasOne(d => d.MaTaiKhoanNavigation).WithMany(p => p.TinTucs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TinTuc__MaTaiKho__44FF419A");
        });

        modelBuilder.Entity<TrangThaiDonHang>(entity =>
        {
            entity.HasKey(e => e.MaTrangThaiDonHang).HasName("PK__TrangTha__B57A45F54964EE22");

            entity.Property(e => e.MaTrangThaiDonHang).ValueGeneratedNever();
        });

        modelBuilder.Entity<YeuThich>(entity =>
        {
            entity.HasKey(e => new { e.MaKhachHang, e.MaSanPham }).HasName("PK__YeuThich__477E84A7F30625F1");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.YeuThiches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__YeuThich__MaKhac__5DCAEF64");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.YeuThiches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__YeuThich__MaSanP__5EBF139D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
