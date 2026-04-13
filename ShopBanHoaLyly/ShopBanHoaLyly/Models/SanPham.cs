using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("SanPham")]
public partial class SanPham
{
    [Key]
    public int MaSanPham { get; set; }

    [StringLength(255)]
    public string? TenSanPham { get; set; }

    [Column(TypeName = "decimal(19, 0)")]
    public decimal? GiaBan { get; set; }

    public int? SoLuongCon { get; set; }

    public string? MoTa { get; set; }

    public int? MaDanhMuc { get; set; }

    // Đánh dấu xoá mềm. 0 = còn sử dụng, 1 = đã xoá
    public bool DaXoa { get; set; } = false;

    [InverseProperty("MaSanPhamNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [InverseProperty("MaSanPhamNavigation")]
    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    [InverseProperty("MaSanPhamNavigation")]
    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    [InverseProperty("MaSanPhamNavigation")]
    public virtual ICollection<HinhAnh> HinhAnhs { get; set; } = new List<HinhAnh>();

    [ForeignKey("MaDanhMuc")]
    [InverseProperty("SanPhams")]
    public virtual DanhMuc? MaDanhMucNavigation { get; set; }

    [InverseProperty("MaSanPhamNavigation")]
    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
