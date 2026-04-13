using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("QuanHuyen")]
public partial class QuanHuyen
{
    [Key]
    public int MaQuanHuyen { get; set; }

    [StringLength(200)]
    public string? TenQuanHuyen { get; set; }

    [InverseProperty("MaQuanHuyenNavigation")]
    public virtual ICollection<PhuongXa> PhuongXas { get; set; } = new List<PhuongXa>();
}
