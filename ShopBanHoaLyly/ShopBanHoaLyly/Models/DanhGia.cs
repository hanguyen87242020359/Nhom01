using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

public partial class DanhGia
{
    [Key]
    public int MaDanhGia { get; set; }

    public int? SoSao { get; set; }

    [StringLength(1000)]
    public string? NoiDung { get; set; }

    public int MaDonHang { get; set; }

    public int MaSanPham { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayDanhGia { get; set; }

    [ForeignKey("MaDonHang, MaSanPham")]
    [InverseProperty("DanhGia")]
    public virtual ChiTietDonHang ChiTietDonHang { get; set; } = null!;

    [ForeignKey("MaSanPham")]
    [InverseProperty("DanhGia")]
    public virtual SanPham? MaSanPhamNavigation { get; set; }
}
