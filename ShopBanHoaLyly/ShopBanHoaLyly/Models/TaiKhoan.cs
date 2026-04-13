using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHoaLyly.Models;

[Table("TaiKhoan")]
public partial class TaiKhoan
{
    [Key]
    public int MaTaiKhoan { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TenTaiKhoan { get; set; }

    [StringLength(255)]
    public string? HoVaTen { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(11)]
    [Unicode(false)]
    public string? SoDienThoai { get; set; }

    [StringLength(64)]
    [Unicode(false)]
    public string? MatKhau { get; set; }

    public int? MaPhuongXa { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    public int? MaQuyen { get; set; }

    public bool? TrangThaiTaiKhoan { get; set; }

    [InverseProperty("MaTaiKhoanNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    [InverseProperty("MaKhachHangNavigation")]
    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    [ForeignKey("MaPhuongXa")]
    [InverseProperty("TaiKhoans")]
    public virtual PhuongXa? MaPhuongXaNavigation { get; set; }

    [ForeignKey("MaQuyen")]
    [InverseProperty("TaiKhoans")]
    public virtual Quyen? MaQuyenNavigation { get; set; }

    [InverseProperty("MaTaiKhoanNavigation")]
    public virtual ICollection<TinTuc> TinTucs { get; set; } = new List<TinTuc>();

    [InverseProperty("MaKhachHangNavigation")]
    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
