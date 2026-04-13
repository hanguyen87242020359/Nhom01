using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[PrimaryKey("MaKhachHang", "MaSanPham")]
[Table("GioHang")]
public partial class GioHang
{
    [Key]
    public int MaKhachHang { get; set; }

    [Key]
    public int MaSanPham { get; set; }

    public int? SoLuong { get; set; }

    [ForeignKey("MaKhachHang")]
    [InverseProperty("GioHangs")]
    public virtual TaiKhoan MaKhachHangNavigation { get; set; } = null!;

    [ForeignKey("MaSanPham")]
    [InverseProperty("GioHangs")]
    public virtual SanPham MaSanPhamNavigation { get; set; } = null!;
}
