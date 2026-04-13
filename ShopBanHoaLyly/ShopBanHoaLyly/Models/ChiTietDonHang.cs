using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[PrimaryKey("MaDonHang", "MaSanPham")]
[Table("ChiTietDonHang")]
public partial class ChiTietDonHang
{
    [Key]
    public int MaDonHang { get; set; }

    [Key]
    public int MaSanPham { get; set; }

    [StringLength(255)]
    public string? TenSanPham { get; set; }

    [Column(TypeName = "decimal(19, 0)")]
    public decimal? DonGia { get; set; }

    public int? SoLuongDat { get; set; }

    [Column(TypeName = "decimal(30, 0)")]
    public decimal? ThanhTien { get; set; }

    [ForeignKey("MaDonHang")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual DonHang MaDonHangNavigation { get; set; } = null!;

    [ForeignKey("MaSanPham")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual SanPham MaSanPhamNavigation { get; set; } = null!;
    
    [InverseProperty("ChiTietDonHang")]
    public virtual DanhGia? DanhGia { get; set; }
}
