using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("DonHang")]
public partial class DonHang
{
    [Key]
    public int MaDonHang { get; set; }

    public int? MaTaiKhoan { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayDatHang { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayGiaoHang { get; set; }

    public int? MaTrangThaiDonHang { get; set; }

    public int? MaPhuongThucThanhToan { get; set; }

    public bool? TrangThaiThanhToan { get; set; }

    [StringLength(255)]
    public string? DiaChiGiaoHang { get; set; }

    [Column(TypeName = "decimal(19, 0)")]
    public decimal? PhiVanChuyen { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [StringLength(255)]
    public string? NguoiNhan { get; set; }

    [StringLength(11)]
    [Unicode(false)]
    public string SoDienThoaiNhan { get; set; } = null!;

    [InverseProperty("MaDonHangNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [ForeignKey("MaPhuongThucThanhToan")]
    [InverseProperty("DonHangs")]
    public virtual PhuongThucThanhToan? MaPhuongThucThanhToanNavigation { get; set; }

    [ForeignKey("MaTaiKhoan")]
    [InverseProperty("DonHangs")]
    public virtual TaiKhoan? MaTaiKhoanNavigation { get; set; }

    [ForeignKey("MaTrangThaiDonHang")]
    [InverseProperty("DonHangs")]
    public virtual TrangThaiDonHang? MaTrangThaiDonHangNavigation { get; set; }
}
