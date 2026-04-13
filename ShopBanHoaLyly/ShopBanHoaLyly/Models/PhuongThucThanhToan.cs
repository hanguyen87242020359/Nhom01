using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("PhuongThucThanhToan")]
public partial class PhuongThucThanhToan
{
    [Key]
    public int MaPhuongThucThanhToan { get; set; }

    [StringLength(100)]
    public string? TenPhuongThucThanhToan { get; set; }

    [InverseProperty("MaPhuongThucThanhToanNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
