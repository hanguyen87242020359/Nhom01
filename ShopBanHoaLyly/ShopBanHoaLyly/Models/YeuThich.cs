using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[PrimaryKey("MaKhachHang", "MaSanPham")]
[Table("YeuThich")]
public partial class YeuThich
{
    [Key]
    public int MaKhachHang { get; set; }

    [Key]
    public int MaSanPham { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayThem { get; set; }

    [ForeignKey("MaKhachHang")]
    [InverseProperty("YeuThiches")]
    public virtual TaiKhoan MaKhachHangNavigation { get; set; } = null!;

    [ForeignKey("MaSanPham")]
    [InverseProperty("YeuThiches")]
    public virtual SanPham MaSanPhamNavigation { get; set; } = null!;
}
