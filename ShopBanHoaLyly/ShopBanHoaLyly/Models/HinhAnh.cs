using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("HinhAnh")]
public partial class HinhAnh
{
    [Key]
    public int MaAnh { get; set; }

    public int? MaSanPham { get; set; }

    [StringLength(255)]
    public string? DuongDan { get; set; }

    [ForeignKey("MaSanPham")]
    [InverseProperty("HinhAnhs")]
    public virtual SanPham? MaSanPhamNavigation { get; set; }
}
