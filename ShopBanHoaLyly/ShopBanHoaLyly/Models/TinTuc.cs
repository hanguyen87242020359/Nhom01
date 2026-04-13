using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("TinTuc")]
public partial class TinTuc
{
    [Key]
    public int MaTinTuc { get; set; }

    [StringLength(255)]
    public string TieuDe { get; set; } = null!;

    [StringLength(500)]
    public string? TomTat { get; set; }

    public string NoiDung { get; set; } = null!;

    public int MaTaiKhoan { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayCapNhat { get; set; }

    public bool? TrangThaiHienThi { get; set; }

    [StringLength(255)]
    public string? HinhAnhDaiDien { get; set; }

    [ForeignKey("MaTaiKhoan")]
    [InverseProperty("TinTucs")]
    public virtual TaiKhoan MaTaiKhoanNavigation { get; set; } = null!;
}
