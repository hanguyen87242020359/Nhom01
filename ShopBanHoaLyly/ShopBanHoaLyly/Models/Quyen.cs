using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("Quyen")]
public partial class Quyen
{
    [Key]
    public int MaQuyen { get; set; }

    [StringLength(50)]
    public string? TenQuyen { get; set; }

    [InverseProperty("MaQuyenNavigation")]
    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
