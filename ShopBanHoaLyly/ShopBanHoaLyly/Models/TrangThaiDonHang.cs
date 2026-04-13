using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("TrangThaiDonHang")]
public partial class TrangThaiDonHang
{
    [Key]
    public int MaTrangThaiDonHang { get; set; }

    [StringLength(50)]
    public string? TenTrangThaiDonHang { get; set; }

    [InverseProperty("MaTrangThaiDonHangNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
