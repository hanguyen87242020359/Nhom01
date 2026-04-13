using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("PhuongXa")]
public partial class PhuongXa
{
    [Key]
    public int MaPhuongXa { get; set; }

    [StringLength(200)]
    public string? TenPhuongXa { get; set; }

    public int? MaQuanHuyen { get; set; }

    [ForeignKey("MaQuanHuyen")]
    [InverseProperty("PhuongXas")]
    public virtual QuanHuyen? MaQuanHuyenNavigation { get; set; }

    [InverseProperty("MaPhuongXaNavigation")]
    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
