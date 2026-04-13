-- 1. Đảm bảo không có kết nối nào đang dùng database cần xóa
USE master;  -- Chuyển context ra khỏi database cần xóa

-- 2. Kiểm tra và ngắt tất cả kết nối đến database (nếu cần)
ALTER DATABASE ShopHoaLyLy SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- 3. Xóa database nếu tồn tại
DROP DATABASE IF EXISTS ShopHoaLyLy;

-- 4. Tạo lại database
CREATE DATABASE ShopHoaLyLy;
GO

USE ShopHoaLyLy
GO

-- ============================
-- TẠO CÁC BẢNG CHÍNH
-- ============================

-- Bảng Quyen (Không dùng IDENTITY vì thường là bảng lookup với ID cố định)
CREATE TABLE Quyen (
    MaQuyen INT PRIMARY KEY,
    TenQuyen NVARCHAR(50)
);
GO

-- Bảng Quan/Huyen (Không dùng IDENTITY vì thường là bảng lookup với ID cố định)
CREATE TABLE QuanHuyen (
    MaQuanHuyen INT PRIMARY KEY,
    TenQuanHuyen NVARCHAR(200)
);
GO

-- Bảng Phuong/Xa (Không dùng IDENTITY vì thường là bảng lookup với ID cố định)
CREATE TABLE PhuongXa (
    MaPhuongXa INT PRIMARY KEY,
    TenPhuongXa NVARCHAR(200),
    MaQuanHuyen INT,
    FOREIGN KEY (MaQuanHuyen) REFERENCES QuanHuyen(MaQuanHuyen)
);
GO

-- Bảng TaiKhoan (Sử dụng IDENTITY cho MaTaiKhoan)
CREATE TABLE TaiKhoan (
    MaTaiKhoan INT PRIMARY KEY IDENTITY(1,1),
    TenTaiKhoan VARCHAR(100),
    HoVaTen NVARCHAR(255),
    Email VARCHAR(100),
    SoDienThoai VARCHAR(11),
    MatKhau VARCHAR(64),
    MaPhuongXa INT,
    DiaChi NVARCHAR(255),
    MaQuyen INT,
    TrangThaiTaiKhoan BIT DEFAULT 1,
    FOREIGN KEY (MaPhuongXa) REFERENCES PhuongXa(MaPhuongXa),
    FOREIGN KEY (MaQuyen) REFERENCES Quyen(MaQuyen)
);
GO

-- Bảng TinTuc
CREATE TABLE TinTuc (
    MaTinTuc INT PRIMARY KEY IDENTITY(1,1),
    TieuDe NVARCHAR(255) NOT NULL,
    TomTat NVARCHAR(500),
    NoiDung NVARCHAR(MAX) NOT NULL,
    MaTaiKhoan INT NOT NULL,
    NgayCapNhat DATETIME DEFAULT GETDATE() NOT NULL,
    TrangThaiHienThi BIT DEFAULT 0,
    HinhAnhDaiDien NVARCHAR(255),
    FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoan(MaTaiKhoan)
);
GO

-- Bảng TrangThaiDonHang
CREATE TABLE TrangThaiDonHang (
    MaTrangThaiDonHang INT PRIMARY KEY,
    TenTrangThaiDonHang NVARCHAR(50)
);
GO

-- Bảng DanhMuc
CREATE TABLE DanhMuc (
    MaDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100)
);
GO

-- Bảng SanPham
CREATE TABLE SanPham (
    MaSanPham INT PRIMARY KEY IDENTITY(1,1),
    TenSanPham NVARCHAR(255),
    GiaBan DECIMAL(19, 0),
    SoLuongCon INT,
    MoTa NVARCHAR(MAX),
    MaDanhMuc INT,
    DaXoa BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc)
);
GO

-- Bảng HinhAnh
CREATE TABLE HinhAnh (
    MaAnh INT PRIMARY KEY IDENTITY(1,1),
    MaSanPham INT,
    DuongDan NVARCHAR(255),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);
GO

-- Bảng PhuongThucThanhToan
CREATE TABLE PhuongThucThanhToan (
    MaPhuongThucThanhToan INT PRIMARY KEY,
    TenPhuongThucThanhToan NVARCHAR(100)
);
GO

-- Bảng DonHang
CREATE TABLE DonHang (
    MaDonHang INT PRIMARY KEY IDENTITY(1,1),
    MaTaiKhoan INT,
    NgayDatHang DATETIME NOT NULL,
    NgayGiaoHang DATETIME,
    MaTrangThaiDonHang INT,
    MaPhuongThucThanhToan INT,
    TrangThaiThanhToan BIT DEFAULT 0,
    DiaChiGiaoHang NVARCHAR(255),
    PhiVanChuyen DECIMAL(19, 0) DEFAULT 0,
    GhiChu NVARCHAR(500),
    NguoiNhan NVARCHAR(255),
    SoDienThoaiNhan VARCHAR(11) NOT NULL,
    FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoan(MaTaiKhoan),
    FOREIGN KEY (MaPhuongThucThanhToan) REFERENCES PhuongThucThanhToan(MaPhuongThucThanhToan),
    FOREIGN KEY (MaTrangThaiDonHang) REFERENCES TrangThaiDonHang(MaTrangThaiDonHang)
);
GO

-- Bảng ChiTietDonHang
CREATE TABLE ChiTietDonHang (
    MaDonHang INT,
    MaSanPham INT,
    TenSanPham NVARCHAR(255),
    DonGia DECIMAL(19, 0),
    SoLuongDat INT,
    ThanhTien AS (DonGia * SoLuongDat) PERSISTED,
    PRIMARY KEY (MaDonHang, MaSanPham),
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);
GO

-- Bảng YeuThich
CREATE TABLE YeuThich (
    MaKhachHang INT,
    MaSanPham INT,
    NgayThem DATETIME,
    PRIMARY KEY (MaKhachHang, MaSanPham),
    FOREIGN KEY (MaKhachHang) REFERENCES TaiKhoan(MaTaiKhoan),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);
GO

-- =========================================================
-- Bảng DanhGia (ĐÃ CẬP NHẬT THEO YÊU CẦU)
-- Thay đổi:
-- 1. Bỏ MaKhachHang.
-- 2. Thêm MaDonHang.
-- 3. Khóa ngoại giờ đây tham chiếu đến ChiTietDonHang để đảm bảo
--    đánh giá gắn liền với một sản phẩm cụ thể trong một đơn hàng cụ thể.
-- =========================================================
CREATE TABLE DanhGia (
    MaDanhGia INT PRIMARY KEY IDENTITY(1,1),
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung NVARCHAR(1000),
    MaDonHang INT NOT NULL,      -- Đã thêm
    MaSanPham INT NOT NULL,      -- Giữ lại
    NgayDanhGia DATETIME DEFAULT GETDATE(),
    -- Khóa ngoại này đảm bảo chỉ có thể đánh giá sản phẩm đã mua trong đơn hàng đó
    FOREIGN KEY (MaDonHang, MaSanPham) REFERENCES ChiTietDonHang(MaDonHang, MaSanPham)
);
GO

-- Bảng GioHang
CREATE TABLE GioHang (
    MaKhachHang INT,
    MaSanPham INT,
    SoLuong INT,
    PRIMARY KEY (MaKhachHang, MaSanPham),
    FOREIGN KEY (MaKhachHang) REFERENCES TaiKhoan(MaTaiKhoan),
    FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham)
);
GO

-- ============================
-- DỮ LIỆU MẪU (Không thay đổi các phần trên)
-- ============================

-- Thêm Quyền
INSERT INTO Quyen (MaQuyen, TenQuyen) VALUES
(1, N'Quản trị viên'),
(2, N'Nhân viên'),
(3, N'Khách hàng');
GO
-- Thêm Quận/Huyện
INSERT INTO QuanHuyen (MaQuanHuyen, TenQuanHuyen) VALUES
(1, N'Hải Châu'),
(2, N'Liên Chiểu'),
(3, N'Thanh Khê'),
(4, N'Cẩm Lệ'),
(5, N'Sơn Trà'),
(6, N'Ngũ Hành Sơn'),
(7, N'Hòa Vang');
GO
-- Thêm Phường/Xã
INSERT INTO PhuongXa (MaPhuongXa, TenPhuongXa, MaQuanHuyen) VALUES
(101, N'Bình Thuận', 1),
(102, N'Hải Châu', 1),
(103, N'Hòa Cường Bắc', 1),
(104, N'Hòa Cường Nam', 1),
(105, N'Hòa Thuận Tây', 1),
(106, N'Phước Ninh', 1),
(107, N'Thạch Thang', 1),
(108, N'Thanh Bình', 1),
(109, N'Thuận Phước', 1),
(201, N'Hòa Hiệp Bắc', 2),
(202, N'Hòa Hiệp Nam', 2),
(203, N'Hòa Khánh Bắc', 2),
(204, N'Hòa Khánh Nam', 2),
(205, N'Hòa Minh', 2),
(301, N'An Khê', 3),
(302, N'Chính Gián', 3),
(303, N'Thạc Gián', 3),
(304, N'Thanh Khê Đông', 3),
(305, N'Thanh Khê Tây', 3),
(306, N'Xuân Hà', 3),
(401, N'Hòa An', 4),
(402, N'Hòa Phát', 4),
(403, N'Hòa Thọ Đông', 4),
(404, N'Hòa Thọ Tây', 4),
(405, N'Hòa Xuân', 4),
(406, N'Khuê Trung', 4),
(501, N'An Hải Bắc', 5),
(502, N'An Hải Nam', 5),
(503, N'Mân Thái', 5),
(504, N'Nại Hiên Đông', 5),
(505, N'Phước Mỹ', 5),
(506, N'Thọ Quang', 5),
(601, N'Hòa Hải', 6),
(602, N'Hòa Quý', 6),
(603, N'Khuê Mỹ', 6),
(604, N'Mỹ An', 6),
(701, N'Hòa Bắc', 7),
(702, N'Hòa Châu', 7),
(703, N'Hòa Khương', 7),
(704, N'Hòa Liên', 7),
(705, N'Hòa Nhơn', 7),
(706, N'Hòa Ninh', 7),
(707, N'Hòa Phong', 7),
(708, N'Hòa Phú', 7),
(709, N'Hòa Phước', 7),
(710, N'Hòa Sơn', 7),
(711, N'Hòa Tiến', 7);
GO
-- Thêm tài khoản
INSERT INTO TaiKhoan (TenTaiKhoan, HoVaTen, Email, SoDienThoai, MatKhau, MaPhuongXa, DiaChi, MaQuyen, TrangThaiTaiKhoan) VALUES
('nguyenvana', N'Nguyễn Văn A', 'vana@gmail.com', '0912345678', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 101, N'123 Lê Duẩn', 1, 1),
('tranthib', N'Trần Thị B', 'tranb@gmail.com', '0987654321', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 104, N'56 Nguyễn Trãi', 2, 1),
('lehoangc', N'Lê Hoàng C', 'hoangc@yahoo.com', '0909123456', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 107, N'789 Điện Biên Phủ', 1, 1),
('phamminhd', N'Phạm Minh D', 'minhd@hotmail.com', '0934567890', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 201, N'222 Hải Phòng', 2, 0),
('dangthie', N'Đặng Thị E', 'thie@gmail.com', '0923456789', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 204, N'36 Trưng Nữ Vương', 1, 1),
('ngothif', N'Ngô Thị F', 'ngof@outlook.com', '0978234561', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 301, N'99 Lý Thường Kiệt', 1, 1),
('buiquangg', N'Bùi Quang G', 'quangg@gmail.com', '0932123456', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 304, N'12 Nguyễn Văn Linh', 2, 0),
('hoangminhh', N'Hoàng Minh H', 'minhh@gmail.com', '0908765432', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 401, N'456 Hoàng Diệu', 1, 1),
('dinhiv', N'Đinh I V', 'dinhiv@gmail.com', '0911223344', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 404, N'33 Pasteur', 2, 1),
('maithiuj', N'Mai Thị J', 'maij@fmail.com', '0966112233', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 501, N'14 Trần Hưng Đạo', 1, 1),
('ngocanh01', N'Ngọc Ánh', 'ngocanh01@gmail.com', '0911112233', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 504, N'123 Nguyễn Văn Linh', 3, 1),
('minhhoang92', N'Minh Hoàng', 'minhhoang92@gmail.com', '0922334455', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 601, N'45 Lê Duẩn', 3, 1),
('tranhuong', N'Trần Thị Hương', 'huongtran@hotmail.com', '0933445566', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 101, N'56 Trần Cao Vân', 3, 1),
('hoangnam', N'Hoàng Nam', 'namhoang@outlook.com', '0944556677', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 104, N'78 Nguyễn Tri Phương', 3, 1),
('phuongle', N'Lê Phương', 'phuongle@yahoo.com', '0955667788', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 107, N'90 Trưng Nữ Vương', 3, 1),
('dinhnguyen', N'Đinh Nguyễn', 'dinhnguyen@gmail.com', '0966778899', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 101, N'21 Hải Phòng', 3, 1),
('khanhlinh', N'Khánh Linh', 'khanhlinh@live.com', '0977889900', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 104, N'12 Pasteur', 3, 1),
('vuquang', N'Vũ Quang', 'vuquang123@gmail.com', '0988999001', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 107, N'34 Hùng Vương', 3, 1),
('hathao', N'Hà Thảo', 'hathao@gmail.com', '0999000112', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 101, N'11 Nguyễn Hữu Thọ', 3, 1),
('nguyenloan', N'Nguyễn Thị Loan', 'loannguyen@gmail.com', '0901234567', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 107, N'88 Lê Lợi', 3, 1);
GO
-- Thêm Tin Tức
INSERT INTO TinTuc (TieuDe, TomTat, NoiDung, MaTaiKhoan, TrangThaiHienThi, HinhAnhDaiDien)
VALUES
(N'Khám phá thế giới hoa hồng với 10 loại phổ biến nhất',
 N'Bài viết này sẽ giới thiệu cho bạn 10 loại hoa hồng phổ biến nhất, từ vẻ đẹp cổ điển đến những giống mới lạ, cùng những ý nghĩa đặc biệt của chúng, giúp bạn dễ dàng lựa chọn loại hoa ưng ý nhất cho mọi dịp.',
 N'Hoa hồng, với vẻ đẹp kiêu sa và hương thơm quyến rũ, luôn là nữ hoàng của các loài hoa và là biểu tượng vĩnh cửu của tình yêu, sự đam mê, và vẻ đẹp. Dù bạn muốn gửi gắm một thông điệp lãng mạn, bày tỏ lòng biết ơn, hay chỉ đơn giản là tô điểm cho không gian sống, hoa hồng luôn là lựa chọn hoàn hảo. Dưới đây là 10 loại hoa hồng phổ biến nhất mà bạn không thể bỏ qua, mỗi loại mang một sắc thái và ý nghĩa riêng:

1.  <b>Hoa hồng đỏ:</b> Biểu tượng kinh điển của tình yêu nồng cháy, lòng nhiệt huyết và sự tôn trọng sâu sắc. Một bó hồng đỏ thắm là cách tuyệt vời để bày tỏ tình cảm chân thành nhất.
2.  <b>Hoa hồng trắng:</b> Tượng trưng cho sự trong trắng, thuần khiết, thanh lịch và khởi đầu mới. Thường được sử dụng trong các đám cưới và lễ tưởng niệm để thể hiện sự kính trọng.
3.  <b>Hoa hồng vàng:</b> Mang ý nghĩa về tình bạn chân thành, niềm vui, sự lạc quan và hạnh phúc. Một bó hồng vàng có thể làm bừng sáng cả không gian và mang lại năng lượng tích cực.
4.  <b>Hoa hồng phấn (Hồng nhạt):</b> Thể hiện sự dịu dàng, duyên dáng, sự ngưỡng mộ và lòng biết ơn. Đây là lựa chọn lý tưởng cho những mối quan hệ mới chớm nở hoặc khi muốn thể hiện sự cảm kích.
5.  <b>Hoa hồng cam:</b> Biểu tượng của sự nhiệt huyết, đam mê, sự khao khát và năng lượng tích cực. Màu cam rực rỡ mang đến cảm giác hứng khởi và đầy sức sống.
6.  <b>Hoa hồng tím:</b> Đại diện cho sự thủy chung, lãng mạn, sự say mê và tình yêu sét đánh. Hoa hồng tím thường được dùng để tạo ấn tượng mạnh mẽ và độc đáo.
7.  <b>Hoa hồng xanh:</b> Mang ý nghĩa về sự bí ẩn, độc đáo, sự bất khả thi hoặc ước mơ thành hiện thực. Đây là loại hoa hiếm và thường gây ấn tượng mạnh.
8.  <b>Hoa hồng leo:</b> Với khả năng leo bám và nở hoa rực rỡ, hoa hồng leo rất phù hợp để trang trí tường, hàng rào, cổng vòm hay ban công, tạo nên một khung cảnh lãng mạn và cổ điển.
9.  <b>Hoa hồng bụi:</b> Dễ trồng và chăm sóc, hoa hồng bụi phát triển thành những bụi lớn với nhiều hoa, phù hợp cho việc trồng trong vườn hoặc chậu lớn, mang lại vẻ đẹp tự nhiên, phong phú.
10. <b>Hoa hồng tỉ muội:</b> Nhỏ xinh, đáng yêu, tượng trưng cho tình chị em thân thiết, tình bạn gắn bó và sự gắn kết gia đình. Chúng thường được dùng để trang trí bàn làm việc hoặc làm quà tặng nhỏ xinh.

Mỗi loại hoa hồng đều có vẻ đẹp riêng biệt và thông điệp sâu sắc, phù hợp với từng dịp đặc biệt và cảm xúc mà bạn muốn gửi gắm. Hãy lựa chọn bông hoa ưng ý để thể hiện tấm lòng của mình một cách tinh tế nhất.',
 1, -- MaTaiKhoan của Nguyễn Văn A (Quản trị viên)
 1, -- Đã xuất bản
 N'tin_tuc_hoa_hong_chinh.jpg'),

(N'Mẹo chăm sóc hoa tươi lâu hơn tại nhà: Giữ vẻ đẹp rạng rỡ cho bó hoa yêu thích của bạn',
 N'Chăm sóc hoa tươi đúng cách sẽ giúp chúng giữ được vẻ đẹp và sự tươi tắn rạng rỡ trong thời gian dài, biến ngôi nhà bạn thành một không gian tràn ngập sức sống. Áp dụng những mẹo nhỏ sau để kéo dài tuổi thọ cho bó hoa yêu quý của bạn và tận hưởng vẻ đẹp của chúng lâu hơn.',
 N'Để những bó hoa tươi thắm mà bạn yêu thích giữ được vẻ đẹp rạng rỡ và sức sống bền bỉ trong ngôi nhà của bạn, việc chăm sóc đúng cách là vô cùng quan trọng. Chỉ với một vài mẹo đơn giản, bạn có thể kéo dài tuổi thọ của hoa và tận hưởng hương sắc của chúng mỗi ngày. Dưới đây là những bí quyết chi tiết giúp hoa tươi lâu hơn:

1.  <b>Cắt gốc hoa đúng cách:</b> Ngay khi mua hoa về, hãy dùng một con dao sắc hoặc kéo chuyên dụng để cắt chéo gốc hoa một góc 45 độ dưới vòi nước chảy. Việc này giúp loại bỏ phần gốc đã bị bít lại do không khí hoặc bụi bẩn, đồng thời tăng diện tích bề mặt để hoa hút nước hiệu quả hơn. Thay đổi góc cắt và cắt lại gốc hoa mỗi khi bạn thay nước.
2.  <b>Thay nước và làm sạch bình thường xuyên:</b> Đây là yếu tố then chốt. Thay nước trong bình mỗi ngày hoặc ít nhất hai ngày một lần. Nước sạch, không chứa vi khuẩn, là môi trường lý tưởng để hoa phát triển. Khi thay nước, hãy rửa sạch bình hoa bằng xà phòng và nước ấm để loại bỏ bất kỳ mảng bám hoặc vi khuẩn nào có thể làm thối rữa gốc hoa.
3.  <b>Sử dụng dung dịch dưỡng hoa:</b> Bạn có thể mua các gói dung dịch dưỡng hoa chuyên dụng tại các cửa hàng hoa. Những gói này chứa đường (cung cấp dinh dưỡng), axit (giúp hoa hút nước tốt hơn) và chất diệt khuẩn (ngăn ngừa sự phát triển của vi khuẩn). Nếu không có, bạn có thể tự pha chế bằng cách hòa tan một thìa cà phê đường, một thìa cà phê giấm trắng (hoặc chanh) và vài giọt thuốc tẩy pha loãng vào một lít nước sạch. Đường cung cấp năng lượng, giấm/chanh giúp cân bằng độ pH, và thuốc tẩy ngăn chặn vi khuẩn.
4.  <b>Loại bỏ lá úa và lá dưới mực nước:</b> Kiểm tra và loại bỏ ngay lập tức bất kỳ lá nào bị úa vàng hoặc lá nằm dưới mực nước trong bình. Lá bị úa hoặc ngâm trong nước sẽ nhanh chóng phân hủy, tạo điều kiện thuận lợi cho vi khuẩn phát triển mạnh mẽ, làm ô nhiễm nước và gây hại cho hoa.
5.  <b>Tránh ánh nắng trực tiếp và nguồn nhiệt:</b> Đặt bó hoa ở nơi mát mẻ, tránh ánh nắng trực tiếp từ mặt trời, quạt máy, điều hòa hoặc các thiết bị điện tử tỏa nhiệt. Nhiệt độ cao và luồng gió mạnh sẽ làm hoa nhanh mất nước và héo úa.
6.  <b>Xịt phun sương cho cánh hoa (với một số loại hoa):</b> Đối với một số loại hoa như hoa hồng, hoa cẩm tú cầu, bạn có thể nhẹ nhàng phun một lớp sương mỏng lên cánh hoa mỗi ngày để giữ độ ẩm, đặc biệt là vào những ngày khô hanh.

Áp dụng những mẹo nhỏ này không chỉ giúp bó hoa của bạn luôn rạng rỡ, tràn đầy sức sống mà còn giúp bạn tận hưởng vẻ đẹp thiên nhiên ngay trong không gian sống của mình lâu hơn.',
 3, -- MaTaiKhoan của Lê Hoàng C (Quản trị viên khác)
 1, -- Đã xuất bản
 N'tin_tuc_cham_soc_hoa.jpg'),

(N'Xu hướng hoa cưới năm 2025: Đơn giản mà tinh tế - Vẻ đẹp vượt thời gian cho ngày trọng đại',
 N'Năm 2025 hứa hẹn mang đến những xu hướng hoa cưới mới mẻ, tập trung vào sự tối giản nhưng không kém phần sang trọng và tinh tế. Những lựa chọn này không chỉ phản ánh cá tính của cô dâu chú rể mà còn tạo nên một không gian cưới lãng mạn, gần gũi với thiên nhiên và đầy ý nghĩa.',
N'Xu hướng hoa cưới trong năm 2025 đang chứng kiến một sự chuyển mình mạnh mẽ, rời xa những thiết kế cầu kỳ, đồ sộ để hướng tới vẻ đẹp tối giản, thanh lịch nhưng vẫn đầy lãng mạn và tinh tế. Cô dâu chú rể ngày càng yêu thích những bó hoa cưới và cách trang trí tiệc cưới mang phong cách tự nhiên, gần gũi với thiên nhiên và phản ánh rõ nét cá tính của họ.

Dưới đây là những điểm nhấn chính trong xu hướng hoa cưới 2025:

1.  <b>Sự lên ngôi của những loài hoa nhỏ nhắn, nhẹ nhàng:</b> Thay vì chỉ tập trung vào các loại hoa lớn, sang trọng, năm nay chứng kiến sự bùng nổ của hoa baby, hoa cúc họa mi, hoa sao, và các loại hoa dại mỏng manh. Chúng mang đến vẻ đẹp mộc mạc, thanh khiết, tạo cảm giác nhẹ nhàng, bay bổng nhưng khi kết hợp khéo léo lại tạo hiệu ứng thị giác ấn tượng, đầy cuốn hút.
2.  <b>Tông màu trung tính và pastel dịu mát:</b> Các tông màu nhẹ nhàng, dễ chịu như hồng phấn, xanh mint, kem, trắng sữa, be và các sắc độ đất đang được ưa chuộng. Những màu sắc này không chỉ tạo cảm giác thanh lịch, sang trọng mà còn dễ dàng kết hợp với nhiều phong cách trang trí và màu sắc trang phục khác nhau, mang lại sự hài hòa và tinh tế cho toàn bộ buổi lễ.
3.  <b>Bó hoa cầm tay tối giản:</b> Xu hướng bó hoa cưới không còn quá cầu kỳ, đồ sộ mà thay vào đó là những bó hoa cầm tay nhỏ gọn, tinh giản. Chúng không chỉ giúp cô dâu di chuyển dễ dàng hơn mà còn tôn lên vẻ đẹp tự nhiên của từng bông hoa và sự thanh thoát của trang phục cưới, tạo nên một tổng thể hài hòa và hiện đại.
4.  <b>Trang trí không gian mở và gần gũi với thiên nhiên:</b> Việc sử dụng hoa để tạo điểm nhấn tại các khu vực đón khách, bàn tiệc, cổng chào theo hướng tự nhiên, không quá sắp đặt đang rất được yêu thích. Hoa được cắm tự do, kết hợp với lá xanh, cành cây khô, hoặc các vật liệu tự nhiên khác để tạo cảm giác như một khu vườn cổ tích, hòa mình vào không gian xanh mát.
5.  <b>Chú trọng tính bền vững và thân thiện với môi trường:</b> Ngày càng nhiều cặp đôi ưu tiên lựa chọn các loại hoa theo mùa, hoa địa phương hoặc hoa được trồng hữu cơ để giảm thiểu chi phí vận chuyển, hạn chế tác động tiêu cực đến môi trường. Đây không chỉ là một lựa chọn ý thức mà còn giúp hoa tươi lâu hơn và có vẻ đẹp tự nhiên, chân thật nhất.
6.  <b>Kết hợp hoa và cây xanh độc đáo:</b> Bên cạnh hoa, các loại cây xanh có lá đẹp, cành lá độc đáo như khuynh diệp, dương xỉ, cành oliu, hay thậm chí là các loại cây gia vị nhỏ cũng được sử dụng để tạo thêm chiều sâu, kết cấu và hương thơm tự nhiên cho các thiết kế hoa cưới.

Những xu hướng này không chỉ mang lại vẻ đẹp độc đáo, vượt thời gian cho ngày trọng đại mà còn phản ánh phong cách sống hiện đại, đề cao sự tự nhiên, sự chân thật và bền vững trong mọi khía cạnh của cuộc sống.',
 1, -- MaTaiKhoan của Nguyễn Văn A (Quản trị viên)
 0, -- Bản nháp (chưa xuất bản)
 NULL); -- Không có ảnh đại diện cho bản nháp này
GO

-- Thêm TrangThaiDonHang
INSERT INTO TrangThaiDonHang (MaTrangThaiDonHang,TenTrangThaiDonHang) VALUES
(1,N'Hoàn tất đặt hàng'),
(2,N'Đang chuẩn bị hàng'),
(3,N'Đang giao hàng'),
(4,N'Đã giao hàng'),
(5,N'Đã hủy');
GO
-- Thêm DanhMuc
INSERT INTO DanhMuc (TenDanhMuc) VALUES
(N'Hoa sinh nhật'),
(N'Hoa tình yêu'),
(N'Hoa chúc mừng'),
(N'Hoa khai trương'),
(N'Hoa tang lễ'),
(N'Hoa cưới'),
(N'Giỏ hoa'),
(N'Bó hoa'),
(N'Hoa theo mùa'),
(N'Hoa nghệ thuật'),
(N'Hoa 20/10'),
(N'Hoa 8/3'),
(N'Hoa 14/2 (Valentine)'),
(N'Hoa ngày Nhà giáo Việt Nam'),
(N'Hoa Giáng sinh'),
(N'Hoa Tết'),
(N'Hoa lễ Phật đản'),
(N'Hoa trang trí sự kiện'),
(N'Hoa bàn tiệc'),
(N'Hoa để bàn làm việc');
GO
-- Thêm SanPham
INSERT INTO SanPham (TenSanPham, GiaBan, SoLuongCon, MoTa, MaDanhMuc) VALUES
(N'Bó hoa hồng đỏ lãng mạn', 350000, 20, N'Đây không chỉ là một bó hoa, mà là một tuyệt tác của tình yêu. Những bông hồng Ecuador đỏ nhung, loại hoa trứ danh với kích thước lớn và cánh hoa dày dặn, được tuyển chọn kỹ lưỡng. Mỗi bông hoa hé nở viên mãn, phô bày lớp cánh mượt mà, xếp lớp tinh xảo, ẩn chứa hương thơm nồng nàn, quyến rũ. Bó hoa được bao bọc bởi lớp giấy gói cao cấp với tone màu tối giản, càng làm nổi bật sắc đỏ đam mê. Dải ruy băng lụa mềm mại thắt hờ hững, hoàn thiện một vẻ đẹp vừa sang trọng vừa là một thông điệp không lời về một tình yêu bất diệt.', 2),
(N'Giỏ hoa hướng dương', 420000, 15, N'Tựa như một cánh đồng mặt trời thu nhỏ, giỏ hoa này là sự bùng nổ của năng lượng và sự lạc quan. Những đóa hướng dương vàng rực, căng tràn nhựa sống, đồng loạt vươn mình kiêu hãnh. Xen kẽ giữa chúng là những cành salem tím và cúc tana trắng nhỏ xinh, tạo nên sự tương phản màu sắc thú vị. Tất cả được sắp xếp hài hòa trong một chiếc giỏ mây tre đan mộc mạc, gần gũi, khơi dậy cảm giác ấm áp, niềm tin và một nguồn hy vọng vô tận.', 7),
(N'Hoa lan trắng thanh lịch', 500000, 10, N'Một cành lan hồ điệp trắng muốt, uốn lượn mềm mại và đầy kiêu hãnh. Những bông hoa to tròn, đối xứng hoàn hảo, mang vẻ đẹp trong ngần không tì vết. Vẻ đẹp của nó không phô trương, mà toát lên từ sự tinh tế, quý phái và một sức hút tĩnh lặng đầy mê hoặc. Được đặt trong chậu sứ trắng tối giản, cành lan trở thành một tác phẩm nghệ thuật sống, một biểu tượng của sự thanh cao và sang trọng.', 9),
(N'Bó hoa baby trắng', 250000, 25, N'Một ôm trọn cả một thiên hà lấp lánh, đó là cảm giác mà bó hoa baby trắng này mang lại. Hàng ngàn, hàng vạn bông hoa nhỏ li ti, trắng muốt như những bông tuyết đầu mùa, kết thành một khối cầu mềm mại, bồng bềnh. Vẻ đẹp của nó nằm ở sự tinh khôi, nhẹ nhàng và thuần khiết. Bó hoa như một đám mây êm ái, mang đến cảm giác bình yên và sự trong trẻo tuyệt đối.', 8),
(N'Bình hoa để bàn hồng pastel', 300000, 18, N'Một bản giao hưởng của những sắc hồng ngọt ngào nhất. Hoa hồng kem, cẩm chướng phớt hồng và những cành phi yến mảnh mai được kết hợp đầy nghệ thuật trong một chiếc bình thủy tinh trong suốt. Màu sắc pastel dịu dàng, trang nhã mang đến một làn gió tươi mát và lãng mạn. Đây là một điểm nhấn tinh tế, giúp không gian trở nên thơ mộng và thư thái hơn.', 10),
(N'Hoa hồng vàng chúc mừng', 400000, 12, N'Sắc vàng tươi tắn của những bông hồng nở rộ như những nụ cười rạng rỡ dưới ánh nắng. Bó hoa là biểu tượng của tình bạn ấm áp, của niềm vui và sự khởi đầu may mắn. Hương thơm dịu nhẹ cùng màu sắc sống động của nó có khả năng lan tỏa năng lượng tích cực, thắp sáng cả một góc phòng và sưởi ấm trái tim người nhận.', 3),
(N'Giỏ hoa lily trắng', 550000, 8, N'Những đóa lily trắng ngát hương, với những cánh hoa cong mềm mại và nhụy hoa điểm xuyết tinh tế, đang bung nở kiêu hãnh trong một giỏ hoa trang nhã. Hương thơm thanh khiết, sang trọng của lily lan tỏa khắp không gian, tạo nên một cảm giác vừa thư thái vừa quyền quý. Đây là hiện thân của vẻ đẹp đức hạnh, cao quý và sự tôn trọng sâu sắc.', 1),
(N'Bó hoa cúc họa mi', 270000, 30, N'Mang cả một góc trời mùa thu Hà Nội vào trong vòng tay. Bó hoa cúc họa mi với vẻ đẹp mộc mạc, trong sáng và gần gũi. Hàng trăm bông hoa nhỏ với nhụy vàng và cánh trắng mỏng manh, rung rinh trong gió, gợi lên cảm giác bình yên, hoài niệm và những ký ức tuổi thơ trong sáng.', 9),
(N'Hoa tang lễ tone trắng đen', 600000, 5, N'Một kệ hoa được thiết kế với sự trang nghiêm và thành kính tuyệt đối. Trên nền lá cọ và thiết mộc lan sẫm màu, những đóa lily trắng, cúc trắng và lan trắng vươn lên, tượng trưng cho sự thanh thản và tinh khiết. Bố cục được sắp xếp trang trọng, thể hiện sự tiếc thương vô hạn và là lời tiễn biệt sau cùng, nguyện cầu cho một linh hồn được an nghỉ nơi vĩnh hằng.', 5),
(N'Bó hoa cưới tone pastel', 750000, 6, N'Một giấc mơ ngọt ngào được kết tinh từ những đóa hoa. Hoa hồng Juliet màu kem đào, mẫu đơn phớt hồng và những cành baby mềm mại được bó lại một cách tự nhiên. Bó hoa không chỉ là phụ kiện, mà còn là vật chứng cho tình yêu, tôn lên vẻ đẹp dịu dàng, lãng mạn và trong sáng của cô dâu trong ngày hạnh phúc nhất.', 6),
(N'Hoa tươi ngày Nhà giáo', 380000, 20, N'Giỏ hoa là một lời cảm ơn được thể hiện qua ngôn ngữ của những đóa hoa. Hoa hồng đỏ tượng trưng cho tình yêu, cẩm chướng thể hiện sự ngưỡng mộ và hoa đồng tiền gửi gắm lời chúc sức khỏe. Tất cả được sắp xếp trang trọng, tươi tắn, thay cho tấm lòng tri ân sâu sắc và sự kính trọng gửi đến những người lái đò thầm lặng.', 14),
(N'Giỏ hoa Tết sum vầy', 580000, 10, N'Không khí Tết cổ truyền được tái hiện sống động qua giỏ hoa rực rỡ sắc màu. Sắc đỏ may mắn của hoa đồng tiền, sắc vàng tài lộc của hoa mai và cúc đại đóa, xen kẽ với những cành lá xanh tươi tốt. Giỏ hoa là biểu tượng của sự sum vầy, ấm no, mang theo lời chúc một năm mới an khang, thịnh vượng và vạn sự như ý.', 16),
(N'Bình hoa Giáng sinh đỏ xanh', 450000, 12, N'Bản hòa ca của mùa lễ hội được thể hiện qua sự kết hợp kinh điển giữa sắc đỏ và xanh. Những đóa trạng nguyên đỏ thắm, quả châu lấp lánh và hoa hồng đỏ được làm nổi bật trên nền xanh thẫm của lá thông và cành tùng. Bình hoa mang đậm tinh thần Giáng sinh, khơi dậy không khí an lành, ấm áp và vui tươi.', 15),
(N'Bó hoa 20/10 thanh lịch', 370000, 22, N'Một thiết kế tinh tế và đầy ý nghĩa, là bản tình ca ca ngợi vẻ đẹp người phụ nữ Việt Nam. Hoa hồng pastel dịu dàng, hoa cát tường duyên dáng và những nhánh baby nhỏ xinh được gói trong giấy lụa mềm mại. Bó hoa như một lời thì thầm ngọt ngào, một sự trân trọng và tôn vinh sâu sắc.', 11),
(N'Hoa Phật đản trang nghiêm', 490000, 7, N'Những đóa sen trắng và vàng, biểu tượng của sự giác ngộ và thanh tịnh, được sắp đặt trang nghiêm trên đài hoa. Mỗi bông hoa vươn mình khỏi bùn lầy để tỏa hương khoe sắc, cũng như tâm hồn hướng thiện vượt qua trần tục. Kệ hoa thể hiện lòng thành kính vô biên và tâm nguyện bình an trong ngày lễ trọng đại.', 17),
(N'Hoa nhập khẩu cao cấp', 980000, 4, N'Một giỏ hoa hội tụ những tinh hoa của thế giới. Mẫu đơn Hà Lan với những cánh hoa mỏng manh xếp chồng lên nhau, tulip Pháp kiêu sa và hồng David Austin trứ danh với hương thơm cổ điển. Mỗi bông hoa là một tuyệt tác, thể hiện sự xa hoa, đẳng cấp và một sự trân trọng không thể diễn tả bằng lời.', 12),
(N'Hoa phong lan mini', 430000, 15, N'Dù nhỏ bé nhưng không kém phần quyến rũ, chậu lan mini khoe những chuỗi hoa xinh xắn một cách bền bỉ. Vẻ đẹp của nó nằm ở sự tinh tế, kiên cường và sức sống mãnh liệt. Đặt trên bàn làm việc hay bệ cửa sổ, nó mang lại một nét duyên dáng thầm lặng và sự thư thái cho tâm hồn.', 11),
(N'Bó hoa hồng sáp vĩnh cửu', 310000, 17, N'Những bông hồng được chế tác tỉ mỉ từ sáp thơm, giữ trọn vẹn hình dáng và màu sắc như hoa thật, nhưng lại trường tồn với thời gian. Hương thơm dịu nhẹ lan tỏa từ bó hoa là hiện thân của một tình yêu vĩnh cửu, một vẻ đẹp không bao giờ phai tàn, một kỷ niệm được lưu giữ mãi mãi.', 13),
(N'Hoa dành cho ngày 8/3', 400000, 18, N'Một bó hoa rực rỡ và ngọt ngào, được thiết kế để mang lại nụ cười. Sự hòa quyện của hoa hồng, hoa cẩm tú cầu và các loài hoa mang sắc hồng, tím và kem. Bố cục tròn đầy, phong phú như một lời ca ngợi vẻ đẹp đa chiều, sự mạnh mẽ và dịu dàng của những người phụ nữ tuyệt vời.', 12),
(N'Giỏ hoa khai trương phát tài', 620000, 8, N'Một kệ hoa hai tầng bề thế, được thiết kế với tone màu đỏ và vàng làm chủ đạo. Hoa đồng tiền, lan mokara và hướng dương được sắp xếp hướng lên trên, tượng trưng cho sự phát triển không ngừng. Đây là một lời chúc hùng hồn cho sự nghiệp thăng tiến, kinh doanh phát tài và thành công rực rỡ.', 4),
(N'Hoa bàn tiệc sang trọng', 700000, 9, N'Một lẵng hoa được thiết kế dáng dài, mềm mại, là điểm nhấn trung tâm hoàn hảo cho mọi bàn tiệc. Sự kết hợp giữa hoa lily, hoa hồng và lan tường trên nền lá xanh mướt tạo nên một bố cục sang trọng, tinh xảo. Nó không chỉ làm đẹp không gian mà còn nâng tầm đẳng cấp của sự kiện.', 19),
(N'Hoa baby nhiều màu', 280000, 25, N'Một bó hoa phá cách và đầy sức sống, nơi những bông baby nhỏ xinh được khoác lên mình tấm áo cầu vồng rực rỡ. Vẻ đẹp vui tươi, năng động và có phần tinh nghịch của nó có thể làm bừng sáng một ngày u ám, mang lại nụ cười và sự bất ngờ thú vị.', 8),
(N'Hoa nghệ thuật Ikebana', 900000, 3, N'Không chỉ là cắm hoa, đây là một tác phẩm nghệ thuật thiền định. Theo triết lý Ikebana, mỗi cành hoa, chiếc lá, thậm chí cả khoảng trống đều được sắp đặt có chủ đích để tạo ra sự hài hòa giữa thiên - địa - nhân. Vẻ đẹp của nó nằm ở sự tĩnh lặng, chiều sâu và sự tôn vinh vẻ đẹp bất toàn của tự nhiên.', 18),
(N'Hoa cắm lọ thủy tinh mini', 350000, 20, N'Sự duyên dáng đến từ điều giản đơn nhất. Vài cành hoa tươi tắn theo mùa được cắm một cách tự nhiên trong chiếc lọ thủy tinh trong suốt, để lộ cả phần thân lá xanh non. Nó mang một góc thiên nhiên nhỏ bé, tinh khôi và đầy sức sống vào không gian riêng của bạn.', 14),
(N'Bó hoa cẩm chướng đỏ', 320000, 22, N'Những bông cẩm chướng đỏ thắm với phần viền cánh hoa lượn sóng đặc trưng, tạo nên một vẻ đẹp vừa cổ điển vừa nồng nàn. Màu đỏ của hoa thể hiện sự ngưỡng mộ, lòng biết ơn và một tình cảm chân thành, sâu sắc. Bó hoa giản dị nhưng chứa đựng nhiều ý nghĩa lớn lao.', 6),
(N'Hoa thu hoạch mùa vàng', 650000, 10, N'Một giỏ hoa mang hơi thở của đồng quê và mùa màng bội thu. Những bông lúa mạch vàng óng, hoa cúc sao vàng và các loại hoa dại được kết hợp một cách tự nhiên. Toàn bộ giỏ hoa toát lên một cảm giác ấm no, trù phú, thịnh vượng và rất đỗi bình yên.', 7),
(N'Bó hoa sen hồng', 550000, 16, N'Vẻ đẹp thanh cao và thoát tục của quốc hoa. Những búp sen hồng chúm chím, e ấp vươn mình mạnh mẽ bên những chiếc lá sen xanh to tròn. Bó hoa không chỉ đẹp mà còn là biểu tượng của sự thuần khiết, của ý chí và nghị lực vươn lên trong mọi hoàn cảnh, một vẻ đẹp đầy triết lý.', 4),
(N'Bình hoa cẩm tú cầu trắng', 780000, 9, N'Những khối cầu hoa hoàn hảo được kết lại từ hàng trăm bông hoa cẩm tú cầu trắng nhỏ li ti. Vẻ đẹp của nó nằm ở sự viên mãn, đủ đầy và trong sáng. Đặt trong bình sứ trắng, nó toát lên sự tinh khôi, tượng trưng cho những cảm xúc chân thành và lòng biết ơn sâu sắc.', 5),
(N'Hoa nghệ thuật hoa cúc rực rỡ', 900000, 5, N'Một tác phẩm sắp đặt đầy táo bạo và ngẫu hứng, sử dụng hoa cúc với nhiều hình dáng và màu sắc khác nhau. Từ cúc ping pong tròn trịa đến cúc calimero nhỏ xinh, tất cả hòa quyện tạo nên một bức tranh sống động, thể hiện sự lạc quan và sức sống mãnh liệt, không theo bất kỳ quy tắc nào.', 20),
(N'Giỏ hoa đám cưới sang trọng', 1000000, 4, N'Sự kết hợp đỉnh cao của những loài hoa mang ý nghĩa tốt lành. Vẻ đẹp quý phái của lan hồ điệp, nét cổ điển vượt thời gian của hoa hồng trắng và sự tinh khôi của baby. Giỏ hoa được thiết kế cao ráo, bề thế, là một lời chúc phúc sang trọng, trọn vẹn và vĩnh cửu gửi đến đôi uyên ương.', 11);
GO
-- Thêm HinhAnh
INSERT INTO HinhAnh (MaSanPham, DuongDan) VALUES
(1, N'Hoa hồng đỏ.jpg'), (1, N'Hoa hồng đỏ1.jpg'), (1, N'Hoa hồng đỏ2.jpg'),
(2, N'giỏ hoa hướng dương 1.jpg'), (2, N'giỏ hoa hướng dương 2.jpg'), (2, N'giỏ hoa hướng dương 3.jpg'),
(3, N'Hoa lan trắng 1.jpg'), (3, N'Hoa lan trắng 2.jpg'), (3, N'Hoa lan trắng 3.jpg'),
(4, N'hoa baby 1.jpg'), (4, N'hoa baby 2.jpg'), (4, N'hoa baby 3.jpg'),
(5, N'hoa để bàn hồng 1.jpg'), (5, N'hoa để bàn hồng 2.jpg'), (5, N'hoa để bàn hồng 3.jpg'),
(6, N'hoa hồng vàng 1.jpg'), (6, N'hoa hồng vàng 2.jpg'), (6, N'hoa hồng vàng 3.jpg'),
(7, N'giỏ hoa lily 1.jpg'), (7, N'giỏ hoa lily 2.jpg'), (7, N'giỏ hoa lily 3.jpg'),
(8, N'hoa cúc họa mi 1.jpg'), (8, N'hoa cúc họa mi 2.jpg'), (8, N'hoa cúc họa mi 3.jpg'),
(9, N'hoa tang lễ 1.jpg'), (9, N'hoa tang lễ 2.jpg'), (9, N'hoa tang lễ 3.jpg'),
(10, N'hoa cưới 1.jpg'), (10, N'hoa cưới 2.jpg'), (10, N'hoa cưới 3.jpg'),
(11, N'nhà giáo 1.jpg'), (11, N'nhà giáo 2.jpg'), (11, N'nhà giáo 3.jpg'),
(12, N'hoa tết 1.jpg'), (12, N'hoa tết 2.jpg'), (12, N'hoa tết 3.jpg'),
(13, N'giáng sinh 1.jpg'), (13, N'giáng sinh 2.jpg'), (13, N'giáng sinh 3.jpg'),
(14, N'hoa 20 10 1.jpg'), (14, N'hoa 20 10 2.jpg'), (14, N'hoa 20 10 3.jpg'),
(15, N'phật đản 1.jpg'), (15, N'phật đản 2.jpg'), (15, N'phật đản 3.png'),
(16, N'hoa nhập khẩu 1.jpg'), (16, N'hoa nhập khẩu 2.jpg'), (16, N'hoa nhập khẩu 3.jpg'),
(17, N'hoa phong lan 1.jpg'), (17, N'hoa phong lan 2.jpg'), (17, N'hoa phong lan 3.jpg'),
(18, N'hoa hồng sáp 1.jpg'), (18, N'hoa hồng sáp 2.jpg'), (18, N'hoa hồng sáp 3.jpg'),
(19, N'hoa 83 1.jpg'), (19, N'hoa 83 2.jpg'), (19, N'hoa 83 3.jpg'),
(20, N'khai trương 1.jpg'), (20, N'khai trương 2.jpg'), (20, N'khai trương 3.jpg'),
(21, N'bàn tiệc 1.jpg'), (21, N'bàn tiệc 2.jpg'), (21, N'bàn tiệc 3.jpg'),
(22, N'baby nhiều màu 1.jpg'), (22, N'baby nhiều màu 2.jpg'), (22, N'baby nhiều màu 3.jpg'),
(23, N'ikebana 1.jpg'), (23, N'ikebana 2.jpg'), (23, N'ikebana 3.png'),
(24, N'lọ mini 1.jpg'), (24, N'lọ mini 2.jpg'), (24, N'lọ mini 3.jpg'),
(25, N'cẩm chướng đỏ 1.jpg'), (25, N'cẩm chướng đỏ 2.jpg'), (25, N'cẩm chướng đỏ 3.jpg'),
(26, N'thu hoạch vàng 1.jpg'), (26, N'thu hoạch vàng 2.jpg'), (26, N'thu hoạch vàng 3.jpg'),
(27, N'sen hồng 1.jpg'), (27, N'sen hồng 2.jpg'), (27, N'sen hồng 3.jpg'),
(28, N'cẩm tú trắng 1.jpg'), (28, N'cẩm tú trắng 2.jpg'), (28, N'cẩm tú trắng 3.jpg'),
(29, N'hoa cúc 1.jpg'), (29, N'hoa cúc 2.jpg'), (29, N'hoa cúc 3.jpg'),
(30, N'hoa đám cưới 1.jpg'), (30, N'hoa đám cưới 2.jpg'), (30, N'hoa đám cưới 3.jpg');
GO
-- Thêm PhuongThucThanhToan
INSERT INTO PhuongThucThanhToan (MaPhuongThucThanhToan, TenPhuongThucThanhToan) VALUES
(1, N'Thanh toán khi nhận hàng (COD)'),
(2, N'VNPay');
GO
-- Thêm Đơn hàng và Chi tiết đơn hàng
-- -----------------------------
-- Tháng 1, 2022
-- -----------------------------
-- Đơn hàng 1
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2022-01-15 08:20:10', '2022-01-17 11:00:00', 4, 2, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Giao trong giờ hành chính', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (1, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (1, 4, N'Bó hoa baby trắng', 250000, 1);

-- Đơn hàng 2
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2022-01-22 14:05:00', '2022-01-24 16:30:00', 4, 1, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, NULL, N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (2, 7, N'Giỏ hoa lily trắng', 550000, 1);

-- Đơn hàng 3 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2022-01-28 10:00:00', NULL, 5, 1, 0, N'34 Hùng Vương, Đà Nẵng', 25000, N'Khách báo hủy do đổi ý', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (3, 10, N'Bó hoa cưới tone pastel', 750000, 1);

-- -----------------------------
-- Tháng 2, 2022 (Valentine)
-- -----------------------------
-- Đơn hàng 4
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2022-02-10 18:00:00', '2022-02-12 10:00:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 20000, N'Tặng bạn gái', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (4, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 2);

-- Đơn hàng 5
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2022-02-11 09:30:00', '2022-02-13 14:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Giao bí mật', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (5, 18, N'Bó hoa hồng sáp vĩnh cửu', 310000, 1);

-- Đơn hàng 6
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2022-02-12 20:15:00', '2022-02-14 08:30:00', 4, 1, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Giao đúng ngày 14/2', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (6, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- -----------------------------
-- Tháng 3, 2022 (Ngày Quốc tế Phụ nữ 8/3)
-- -----------------------------
-- Đơn hàng 7
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2022-03-05 11:45:00', '2022-03-07 15:00:00', 4, 1, 1, N'21 Hải Phòng, Hải Châu', 20000, N'Tặng mẹ', N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (7, 19, N'Hoa dành cho ngày 8/3', 400000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (7, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- Đơn hàng 8
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2022-03-06 16:20:00', '2022-03-08 09:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Tặng vợ yêu', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (8, 19, N'Hoa dành cho ngày 8/3', 400000, 1);

-- Đơn hàng 9
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2022-03-07 10:10:00', '2022-03-08 11:30:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Tặng sếp nữ', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (9, 3, N'Hoa lan trắng thanh lịch', 500000, 1);

-- -----------------------------
-- Tháng 4, 2022
-- -----------------------------
-- Đơn hàng 10
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2022-04-10 14:55:00', '2022-04-12 17:00:00', 4, 2, 1, N'12 Pasteur, Đà Nẵng', 20000, NULL, N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (10, 20, N'Giỏ hoa khai trương phát tài', 620000, 1);

-- Đơn hàng 11
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2022-04-25 09:12:00', '2022-04-27 10:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Giao cho lễ tân', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (11, 21, N'Hoa bàn tiệc sang trọng', 700000, 2);

-- -----------------------------
-- Tháng 5, 2022
-- -----------------------------
-- Đơn hàng 12
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2022-05-19 13:30:00', '2022-05-21 15:20:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Chúc mừng sinh nhật', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (12, 6, N'Hoa hồng vàng chúc mừng', 400000, 1);

-- -----------------------------
-- Tháng 6, 2022
-- -----------------------------
-- Đơn hàng 13
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2022-06-01 10:00:00', '2022-06-03 11:00:00', 4, 2, 1, N'34 Hùng Vương, Đà Nẵng', 25000, NULL, N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (13, 22, N'Hoa baby nhiều màu', 280000, 2);

-- Đơn hàng 14 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2022-06-20 17:00:00', NULL, 5, 2, 0, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Khách không nghe máy', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (14, 27, N'Bó hoa sen hồng', 550000, 1);

-- -----------------------------
-- Tháng 7, 2022
-- -----------------------------
-- Đơn hàng 15
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2022-07-18 11:25:00', '2022-07-20 14:00:00', 4, 1, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, NULL, N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (15, 24, N'Hoa cắm lọ thủy tinh mini', 350000, 1);

-- -----------------------------
-- Tháng 8, 2022
-- -----------------------------
-- Đơn hàng 16
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2022-08-05 16:10:00', '2022-08-07 18:00:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Hoa chia buồn', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (16, 9, N'Hoa tang lễ tone trắng đen', 600000, 1);

-- Đơn hàng 17
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2022-08-21 12:00:00', '2022-08-23 15:30:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, NULL, N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (17, 26, N'Hoa thu hoạch mùa vàng', 650000, 1);

-- -----------------------------
-- Tháng 9, 2022
-- -----------------------------
-- Đơn hàng 18
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2022-09-01 07:30:00', '2022-09-02 10:00:00', 4, 2, 1, N'21 Hải Phòng, Hải Châu', 20000, N'Khai trương hồng phát', N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (18, 20, N'Giỏ hoa khai trương phát tài', 620000, 2);

-- -----------------------------
-- Tháng 10, 2022 (Ngày Phụ nữ Việt Nam 20/10)
-- -----------------------------
-- Đơn hàng 19
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2022-10-18 15:00:00', '2022-10-20 09:30:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Tặng cô giáo', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (19, 14, N'Bó hoa 20/10 thanh lịch', 370000, 1);

-- Đơn hàng 20
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2022-10-19 11:00:00', '2022-10-20 14:00:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Tặng chị gái', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (20, 5, N'Bình hoa để bàn hồng pastel', 300000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (20, 8, N'Bó hoa cúc họa mi', 270000, 1);

-- -----------------------------
-- Tháng 11, 2022 (Ngày Nhà giáo Việt Nam 20/11)
-- -----------------------------
-- Đơn hàng 21
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2022-11-17 19:00:00', '2022-11-19 10:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Tri ân thầy cô', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (21, 11, N'Hoa tươi ngày Nhà giáo', 380000, 3);

-- Đơn hàng 22
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2022-11-18 14:20:00', '2022-11-20 08:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Giao sớm giúp mình', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (22, 24, N'Hoa cắm lọ thủy tinh mini', 350000, 2);

-- -----------------------------
-- Tháng 12, 2022 (Giáng sinh & cuối năm)
-- -----------------------------
-- Đơn hàng 23
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2022-12-21 20:00:00', '2022-12-23 19:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Merry Christmas!', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (23, 13, N'Bình hoa Giáng sinh đỏ xanh', 450000, 1);

-- Đơn hàng 24
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2022-12-28 10:40:00', '2022-12-30 11:00:00', 4, 1, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Hoa tặng tất niên', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (24, 12, N'Giỏ hoa Tết sum vầy', 580000, 1);

-- -----------------------------
-- Tháng 1, 2023 (Dịp Tết Nguyên Đán)
-- -----------------------------
-- Đơn hàng 25
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2023-01-15 10:10:00', '2023-01-17 14:00:00', 4, 2, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Trang trí nhà cửa đón Tết', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (25, 12, N'Giỏ hoa Tết sum vầy', 580000, 2);

-- Đơn hàng 26
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2023-01-20 09:00:00', '2023-01-22 11:30:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Chúc Tết đối tác', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (26, 27, N'Bó hoa sen hồng', 550000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (26, 6, N'Hoa hồng vàng chúc mừng', 400000, 1);

-- -----------------------------
-- Tháng 2, 2023 (Valentine)
-- -----------------------------
-- Đơn hàng 27
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2023-02-11 21:00:00', '2023-02-13 17:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Tặng người thương', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (27, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- Đơn hàng 28
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2023-02-12 11:30:00', '2023-02-14 09:00:00', 4, 2, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Vui lòng giao đúng 9h sáng 14/2', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (28, 18, N'Bó hoa hồng sáp vĩnh cửu', 310000, 1);

-- Đơn hàng 29
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2023-02-13 14:00:00', '2023-02-14 15:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, NULL, N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (29, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- -----------------------------
-- Tháng 3, 2023 (Ngày Quốc tế Phụ nữ 8/3)
-- -----------------------------
-- Đơn hàng 30
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2023-03-06 08:15:00', '2023-03-08 10:00:00', 4, 1, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Tặng vợ', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (30, 19, N'Hoa dành cho ngày 8/3', 400000, 1);

-- Đơn hàng 31 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2023-03-07 10:00:00', NULL, 5, 2, 0, N'21 Hải Phòng, Hải Châu', 20000, N'Khách đặt nhầm sản phẩm', N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (31, 7, N'Giỏ hoa lily trắng', 550000, 1);

-- Đơn hàng 32
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2023-03-07 16:45:00', '2023-03-08 16:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Tặng các chị em trong phòng', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (32, 25, N'Bó hoa cẩm chướng đỏ', 320000, 3);

-- -----------------------------
-- Tháng 4, 2023
-- -----------------------------
-- Đơn hàng 33
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2023-04-12 11:20:00', '2023-04-14 11:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Khai trương cửa hàng bạn', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (33, 20, N'Giỏ hoa khai trương phát tài', 620000, 1);

-- Đơn hàng 34
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2023-04-28 17:00:00', '2023-04-30 10:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Tặng sinh nhật', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (34, 3, N'Hoa lan trắng thanh lịch', 500000, 1);

-- -----------------------------
-- Tháng 5, 2023
-- -----------------------------
-- Đơn hàng 35
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2023-05-20 13:00:00', '2023-05-22 15:00:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, NULL, N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (35, 22, N'Hoa baby nhiều màu', 280000, 1);

-- -----------------------------
-- Tháng 6, 2023
-- -----------------------------
-- Đơn hàng 36
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2023-06-10 16:30:00', '2023-06-12 17:00:00', 4, 2, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Tặng tốt nghiệp', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (36, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- Đơn hàng 37
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2023-06-25 09:45:00', '2023-06-27 11:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, NULL, N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (37, 17, N'Hoa phong lan mini', 430000, 1);

-- -----------------------------
-- Tháng 7, 2023
-- -----------------------------
-- Đơn hàng 38
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2023-07-07 14:00:00', '2023-07-09 16:00:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, NULL, N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (38, 23, N'Hoa nghệ thuật Ikebana', 900000, 1);

-- -----------------------------
-- Tháng 8, 2023
-- -----------------------------
-- Đơn hàng 39 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2023-08-15 18:00:00', NULL, 5, 1, 0, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Không liên lạc được khách hàng', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (39, 10, N'Bó hoa cưới tone pastel', 750000, 1);

-- Đơn hàng 40
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2023-08-22 10:20:00', '2023-08-24 11:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Hoa chia buồn', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (40, 9, N'Hoa tang lễ tone trắng đen', 600000, 1);

-- -----------------------------
-- Tháng 9, 2023
-- -----------------------------
-- Đơn hàng 41
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2023-09-04 09:30:00', '2023-09-05 14:30:00', 4, 1, 1, N'21 Hải Phòng, Hải Châu', 20000, NULL, N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (41, 5, N'Bình hoa để bàn hồng pastel', 300000, 2);

-- -----------------------------
-- Tháng 10, 2023 (Ngày Phụ nữ Việt Nam 20/10)
-- -----------------------------
-- Đơn hàng 42
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2023-10-18 19:30:00', '2023-10-20 09:00:00', 4, 2, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Tặng mẹ yêu', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (42, 14, N'Bó hoa 20/10 thanh lịch', 370000, 1);

-- Đơn hàng 43
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2023-10-19 12:00:00', '2023-10-20 15:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, NULL, N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (43, 28, N'Bình hoa cẩm tú cầu trắng', 780000, 1);

-- Đơn hàng 44
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2023-10-19 22:00:00', '2023-10-20 18:00:00', 4, 2, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Giao sau 5h chiều', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (44, 8, N'Bó hoa cúc họa mi', 270000, 2);

-- -----------------------------
-- Tháng 11, 2023 (Ngày Nhà giáo Việt Nam 20/11)
-- -----------------------------
-- Đơn hàng 45
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2023-11-18 08:00:00', '2023-11-19 10:30:00', 4, 1, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Tri ân thầy cô', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (45, 11, N'Hoa tươi ngày Nhà giáo', 380000, 1);

-- Đơn hàng 46
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2023-11-19 14:00:00', '2023-11-20 09:00:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Tặng cô chủ nhiệm', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (46, 24, N'Hoa cắm lọ thủy tinh mini', 350000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (46, 29, N'Hoa nghệ thuật hoa cúc rực rỡ', 900000, 1);

-- -----------------------------
-- Tháng 12, 2023 (Giáng sinh & cuối năm)
-- -----------------------------
-- Đơn hàng 47
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2023-12-22 17:50:00', '2023-12-24 16:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Merry Christmas!', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (47, 13, N'Bình hoa Giáng sinh đỏ xanh', 450000, 2);

-- Đơn hàng 48
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2023-12-29 11:00:00', '2023-12-31 11:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Happy New Year 2024', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (48, 16, N'Hoa nhập khẩu cao cấp', 980000, 1);

-- Đơn hàng 49
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2023-12-30 09:00:00', '2023-12-31 15:00:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Trang trí tiệc cuối năm', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (49, 21, N'Hoa bàn tiệc sang trọng', 700000, 2);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (49, 30, N'Giỏ hoa đám cưới sang trọng', 1000000, 1);

-- -----------------------------
-- Tháng 1, 2024
-- -----------------------------
-- Đơn hàng 50
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2024-01-10 11:35:00', '2024-01-12 14:00:00', 4, 2, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Chúc mừng sinh nhật', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (50, 6, N'Hoa hồng vàng chúc mừng', 400000, 1);

-- Đơn hàng 51
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2024-01-25 09:15:00', '2024-01-27 10:30:00', 4, 1, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Hoa trang trí văn phòng', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (51, 29, N'Hoa nghệ thuật hoa cúc rực rỡ', 900000, 1);

-- -----------------------------
-- Tháng 2, 2024 (Tết Nguyên Đán + Valentine)
-- -----------------------------
-- Đơn hàng 52
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2024-02-05 14:00:00', '2024-02-07 16:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Giao quà Tết', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (52, 12, N'Giỏ hoa Tết sum vầy', 580000, 2);

-- Đơn hàng 53
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2024-02-12 10:00:00', '2024-02-14 08:30:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Giao đúng sáng Valentine', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (53, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (53, 18, N'Bó hoa hồng sáp vĩnh cửu', 310000, 1);

-- Đơn hàng 54
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2024-02-13 18:20:00', '2024-02-14 19:00:00', 4, 1, 1, N'21 Hải Phòng, Hải Châu', 20000, N'Tặng bạn gái', N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (54, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- -----------------------------
-- Tháng 3, 2024 (Ngày Quốc tế Phụ nữ 8/3)
-- -----------------------------
-- Đơn hàng 55
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2024-03-05 11:00:00', '2024-03-07 15:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Tặng mẹ và chị gái', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (55, 19, N'Hoa dành cho ngày 8/3', 400000, 2);

-- Đơn hàng 56
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2024-03-06 20:10:00', '2024-03-08 10:00:00', 4, 1, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Tặng đồng nghiệp nữ', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (56, 3, N'Hoa lan trắng thanh lịch', 500000, 1);

-- Đơn hàng 57 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2024-03-07 12:00:00', NULL, 5, 2, 0, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Khách báo hủy, mua trực tiếp tại cửa hàng', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (57, 25, N'Bó hoa cẩm chướng đỏ', 320000, 1);

-- -----------------------------
-- Tháng 4, 2024
-- -----------------------------
-- Đơn hàng 58
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2024-04-15 08:30:00', '2024-04-17 11:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Khai trương', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (58, 20, N'Giỏ hoa khai trương phát tài', 620000, 1);

-- Đơn hàng 59
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2024-04-29 16:00:00', '2024-04-30 18:00:00', 4, 2, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Giao trước lễ', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (59, 21, N'Hoa bàn tiệc sang trọng', 700000, 1);

-- -----------------------------
-- Tháng 5, 2024
-- -----------------------------
-- Đơn hàng 60
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2024-05-18 10:10:00', '2024-05-20 14:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Hoa thăm hỏi', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (60, 7, N'Giỏ hoa lily trắng', 550000, 1);

-- -----------------------------
-- Tháng 6, 2024
-- -----------------------------
-- Đơn hàng 61
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2024-06-05 13:00:00', '2024-06-07 15:30:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, NULL, N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (61, 4, N'Bó hoa baby trắng', 250000, 2);

-- Đơn hàng 62
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2024-06-21 09:00:00', '2024-06-23 10:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Tặng sinh nhật bạn thân', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (62, 8, N'Bó hoa cúc họa mi', 270000, 1);

-- -----------------------------
-- Tháng 7, 2024
-- -----------------------------
-- Đơn hàng 63
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2024-07-11 15:20:00', '2024-07-13 17:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Hoa chia buồn', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (63, 9, N'Hoa tang lễ tone trắng đen', 600000, 1);

-- -----------------------------
-- Tháng 8, 2024
-- -----------------------------
-- Đơn hàng 64
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2024-08-19 11:45:00', '2024-08-21 14:00:00', 4, 1, 1, N'21 Hải Phòng, Hải Châu', 20000, NULL, N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (64, 17, N'Hoa phong lan mini', 430000, 1);

-- Đơn hàng 65 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2024-08-28 17:00:00', NULL, 5, 2, 0, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Khách bom hàng', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (65, 26, N'Hoa thu hoạch mùa vàng', 650000, 1);

-- -----------------------------
-- Tháng 9, 2024
-- -----------------------------
-- Đơn hàng 66
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2024-09-01 08:00:00', '2024-09-02 11:00:00', 4, 2, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Mừng khai trương', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (66, 4, N'Bó hoa baby trắng', 250000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (66, 20, N'Giỏ hoa khai trương phát tài', 620000, 1);

-- -----------------------------
-- Tháng 10, 2024 (Ngày Phụ nữ Việt Nam 20/10)
-- -----------------------------
-- Đơn hàng 67
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2024-10-18 16:30:00', '2024-10-20 09:30:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Tặng vợ yêu', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (67, 14, N'Bó hoa 20/10 thanh lịch', 370000, 1);

-- Đơn hàng 68
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2024-10-19 10:00:00', '2024-10-20 14:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Tặng các chị trong công ty', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (68, 5, N'Bình hoa để bàn hồng pastel', 300000, 3);

-- Đơn hàng 69
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2024-10-19 21:00:00', '2024-10-20 18:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, NULL, N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (69, 16, N'Hoa nhập khẩu cao cấp', 980000, 1);

-- -----------------------------
-- Tháng 11, 2024 (Ngày Nhà giáo Việt Nam 20/11)
-- -----------------------------
-- Đơn hàng 70
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2024-11-18 09:25:00', '2024-11-19 11:00:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Tri ân thầy cô', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (70, 11, N'Hoa tươi ngày Nhà giáo', 380000, 2);

-- Đơn hàng 71
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2024-11-19 15:00:00', '2024-11-20 09:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Tặng cô giáo con trai', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (71, 24, N'Hoa cắm lọ thủy tinh mini', 350000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (71, 27, N'Bó hoa sen hồng', 550000, 1);

-- -----------------------------
-- Tháng 12, 2024 (Giáng sinh & cuối năm)
-- -----------------------------
-- Đơn hàng 72
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2024-12-22 19:00:00', '2024-12-24 18:00:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Giáng sinh an lành', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (72, 13, N'Bình hoa Giáng sinh đỏ xanh', 450000, 1);

-- Đơn hàng 73
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2024-12-28 10:00:00', '2024-12-30 14:00:00', 4, 1, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Hoa tất niên công ty', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (73, 30, N'Giỏ hoa đám cưới sang trọng', 1000000, 1);
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (73, 21, N'Hoa bàn tiệc sang trọng', 700000, 2);

-- Đơn hàng 74
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2024-12-30 11:30:00', '2024-12-31 16:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Chúc mừng năm mới 2025!', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (74, 12, N'Giỏ hoa Tết sum vầy', 580000, 1);

-- -----------------------------
-- Tháng 1, 2025
-- -----------------------------
-- Đơn hàng 75
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2025-01-15 09:30:00', '2025-01-17 11:00:00', 4, 2, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Hoa chúc mừng năm mới', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (75, 12, N'Giỏ hoa Tết sum vầy', 580000, 1);

-- Đơn hàng 76
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2025-01-28 14:00:00', '2025-01-30 16:30:00', 4, 1, 1, N'21 Hải Phòng, Hải Châu', 20000, NULL, N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (76, 27, N'Bó hoa sen hồng', 550000, 1);

-- -----------------------------
-- Tháng 2, 2025 (Valentine)
-- -----------------------------
-- Đơn hàng 77
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2025-02-11 19:00:00', '2025-02-13 20:00:00', 4, 2, 1, N'34 Hùng Vương, Đà Nẵng', 25000, N'Tặng người yêu', N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (77, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- Đơn hàng 78
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2025-02-12 11:30:00', '2025-02-14 09:00:00', 4, 1, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Giao đúng ngày Valentine nhé shop', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (78, 18, N'Bó hoa hồng sáp vĩnh cửu', 310000, 2);

-- -----------------------------
-- Tháng 3, 2025 (Ngày Quốc tế Phụ nữ 8/3)
-- -----------------------------
-- Đơn hàng 79
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2025-03-06 08:00:00', '2025-03-08 10:00:00', 4, 2, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Tặng mẹ', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (79, 19, N'Hoa dành cho ngày 8/3', 400000, 1);

-- Đơn hàng 80 (Hủy)
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2025-03-07 15:00:00', NULL, 5, 1, 0, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Khách hàng báo hủy do đi công tác đột xuất', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (80, 7, N'Giỏ hoa lily trắng', 550000, 1);

-- -----------------------------
-- Tháng 4, 2025
-- -----------------------------
-- Đơn hàng 81
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2025-04-10 16:00:00', '2025-04-12 17:00:00', 4, 1, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Khai trương chi nhánh mới', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (81, 20, N'Giỏ hoa khai trương phát tài', 620000, 2);

-- Đơn hàng 82
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2025-04-28 10:00:00', '2025-04-30 11:00:00', 4, 2, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, N'Tặng sinh nhật sếp', N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (82, 16, N'Hoa nhập khẩu cao cấp', 980000, 1);

-- ----------------------------------------------------
-- DỮ LIỆU DÀY ĐẶC (22/05/2025 - 22/06/2025)
-- ----------------------------------------------------

-- 22/05/2025 - Đơn hàng 83
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2025-05-22 08:15:00', '2025-05-23 10:00:00', 4, 2, 1, N'12 Pasteur, Đà Nẵng', 20000, NULL, N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (83, 5, N'Bình hoa để bàn hồng pastel', 300000, 1);

-- 23/05/2025 - Đơn hàng 84
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2025-05-23 11:00:00', '2025-05-24 14:00:00', 4, 1, 1, N'88 Lê Lợi, Đà Nẵng', 25000, N'Hoa trang trí sự kiện', N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (84, 21, N'Hoa bàn tiệc sang trọng', 700000, 3);

-- 24/05/2025 - Đơn hàng 85
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2025-05-24 14:30:00', '2025-05-25 16:00:00', 4, 2, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, NULL, N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (85, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- 25/05/2025 - Đơn hàng 86
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2025-05-25 09:00:00', '2025-05-26 11:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Tặng sinh nhật', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (86, 6, N'Hoa hồng vàng chúc mừng', 400000, 1);

-- 26/05/2025 - Đơn hàng 87
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2025-05-26 13:20:00', '2025-05-27 15:00:00', 4, 2, 1, N'21 Hải Phòng, Hải Châu', 20000, NULL, N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (87, 4, N'Bó hoa baby trắng', 250000, 2);

-- 27/05/2025 - Đơn hàng 88
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2025-05-27 10:45:00', '2025-05-28 14:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, NULL, N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (88, 8, N'Bó hoa cúc họa mi', 270000, 1);

-- 28/05/2025 - Đơn hàng 89
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2025-05-28 17:00:00', '2025-05-29 18:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Tặng tốt nghiệp', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (89, 26, N'Hoa thu hoạch mùa vàng', 650000, 1);

-- 29/05/2025 - Đơn hàng 90
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2025-05-29 09:10:00', '2025-05-30 11:30:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, NULL, N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (90, 28, N'Bình hoa cẩm tú cầu trắng', 780000, 1);

-- 30/05/2025 - Đơn hàng 91
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2025-05-30 11:50:00', '2025-05-31 15:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, N'Hoa cảm ơn', N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (91, 25, N'Bó hoa cẩm chướng đỏ', 320000, 1);

-- 31/05/2025 - Đơn hàng 92
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2025-05-31 14:00:00', '2025-06-01 16:00:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, N'Quốc tế thiếu nhi', N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (92, 22, N'Hoa baby nhiều màu', 280000, 1);

-- 01/06/2025 - Đơn hàng 93
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2025-06-01 08:30:00', '2025-06-02 10:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, NULL, N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (93, 3, N'Hoa lan trắng thanh lịch', 500000, 1);

-- 02/06/2025 - Đơn hàng 94
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2025-06-02 10:00:00', '2025-06-03 11:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, NULL, N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (94, 17, N'Hoa phong lan mini', 430000, 1);

-- 03/06/2025 - Đơn hàng 95
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2025-06-03 15:00:00', '2025-06-04 16:30:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, N'Chúc mừng sinh nhật', N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (95, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- 04/06/2025 - Đơn hàng 96
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2025-06-04 11:20:00', '2025-06-05 14:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, NULL, N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (96, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- 05/06/2025 - Đơn hàng 97
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2025-06-05 09:45:00', '2025-06-06 11:00:00', 4, 2, 1, N'21 Hải Phòng, Hải Châu', 20000, N'Tặng sếp', N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (97, 23, N'Hoa nghệ thuật Ikebana', 900000, 1);

-- 06/06/2025 - Đơn hàng 98
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2025-06-06 13:00:00', '2025-06-07 15:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, NULL, N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (98, 10, N'Bó hoa cưới tone pastel', 750000, 1);

-- 07/06/2025 - Đơn hàng 99
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2025-06-07 16:30:00', '2025-06-08 17:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, N'Hoa cưới bạn thân', N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (99, 30, N'Giỏ hoa đám cưới sang trọng', 1000000, 1);

-- 08/06/2025 - Đơn hàng 100
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2025-06-08 08:00:00', '2025-06-09 10:00:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, NULL, N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (100, 24, N'Hoa cắm lọ thủy tinh mini', 350000, 1);

-- 09/06/2025 - Đơn hàng 101
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2025-06-09 10:15:00', '2025-06-10 14:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, NULL, N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (101, 29, N'Hoa nghệ thuật hoa cúc rực rỡ', 900000, 1);

-- 10/06/2025 - Đơn hàng 102
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2025-06-10 12:00:00', '2025-06-11 15:30:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, NULL, N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (102, 5, N'Bình hoa để bàn hồng pastel', 300000, 1);

-- 11/06/2025 - Đơn hàng 103
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2025-06-11 14:45:00', '2025-06-12 16:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, NULL, N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (103, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- 12/06/2025 - Đơn hàng 104
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2025-06-12 09:00:00', '2025-06-13 11:00:00', 4, 1, 1, N'123 Nguyễn Văn Linh, Hải Châu', 20000, NULL, N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (104, 2, N'Giỏ hoa hướng dương', 420000, 1);

-- 13/06/2025 - Đơn hàng 105
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (15, '2025-06-13 11:00:00', '2025-06-14 14:00:00', 4, 2, 1, N'90 Trưng Nữ Vương, Liên Chiểu', 35000, NULL, N'Lê Phương', '0955667788');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (105, 6, N'Hoa hồng vàng chúc mừng', 400000, 1);

-- 14/06/2025 - Đơn hàng 106
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2025-06-14 13:30:00', '2025-06-15 15:00:00', 4, 1, 1, N'45 Lê Duẩn, Đà Nẵng', 25000, NULL, N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (106, 7, N'Giỏ hoa lily trắng', 550000, 1);

-- 15/06/2025 - Đơn hàng 107
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (16, '2025-06-15 10:00:00', '2025-06-16 11:30:00', 4, 2, 1, N'21 Hải Phòng, Hải Châu', 20000, NULL, N'Đinh Nguyễn', '0966778899');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (107, 8, N'Bó hoa cúc họa mi', 270000, 2);

-- 16/06/2025 - Đơn hàng 108
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (18, '2025-06-16 16:00:00', '2025-06-17 17:00:00', 4, 1, 1, N'34 Hùng Vương, Đà Nẵng', 25000, NULL, N'Vũ Quang', '0988999001');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (108, 9, N'Hoa tang lễ tone trắng đen', 600000, 1);

-- 17/06/2025 - Đơn hàng 109
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (14, '2025-06-17 12:45:00', '2025-06-18 15:00:00', 4, 2, 1, N'78 Nguyễn Tri Phương, Hải Châu', 20000, NULL, N'Hoàng Nam', '0944556677');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (109, 4, N'Bó hoa baby trắng', 250000, 1);

-- 18/06/2025 - Đơn hàng 110
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (13, '2025-06-18 09:30:00', '2025-06-19 11:00:00', 4, 1, 1, N'56 Trần Cao Vân, Thanh Khê', 30000, NULL, N'Trần Thị Hương', '0933445566');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (110, 20, N'Giỏ hoa khai trương phát tài', 620000, 1);

-- 19/06/2025 - Đơn hàng 111
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (19, '2025-06-19 14:00:00', '2025-06-20 16:00:00', 4, 2, 1, N'11 Nguyễn Hữu Thọ, Hải Châu', 25000, NULL, N'Hà Thảo', '0999000112');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (111, 21, N'Hoa bàn tiệc sang trọng', 700000, 1);

-- 20/06/2025 - Đơn hàng 112
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (17, '2025-06-20 10:20:00', '2025-06-21 14:00:00', 4, 1, 1, N'12 Pasteur, Đà Nẵng', 20000, NULL, N'Khánh Linh', '0977889900');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (112, 16, N'Hoa nhập khẩu cao cấp', 980000, 1);

-- 21/06/2025 - Đơn hàng 113
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (20, '2025-06-21 11:30:00', '2025-06-22 15:00:00', 4, 2, 1, N'88 Lê Lợi, Đà Nẵng', 25000, NULL, N'Nguyễn Thị Loan', '0901234567');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (113, 17, N'Hoa phong lan mini', 430000, 2);

-- 22/06/2025 - Đơn hàng 114
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (11, '2025-06-22 09:00:00', NULL, 2, 1, 0, N'123 Nguyễn Văn Linh, Hải Châu', 20000, N'Đơn hàng đang chuẩn bị', N'Ngọc Ánh', '0911112233');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (114, 1, N'Bó hoa hồng đỏ lãng mạn', 350000, 1);

-- 22/06/2025 - Đơn hàng 115
INSERT INTO DonHang (MaTaiKhoan, NgayDatHang, NgayGiaoHang, MaTrangThaiDonHang, MaPhuongThucThanhToan, TrangThaiThanhToan, DiaChiGiaoHang, PhiVanChuyen, GhiChu, NguoiNhan, SoDienThoaiNhan) VALUES (12, '2025-06-22 10:30:00', NULL, 1, 2, 0, N'45 Lê Duẩn, Đà Nẵng', 25000, N'Hoàn tất đặt hàng', N'Minh Hoàng', '0922334455');
INSERT INTO ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuongDat) VALUES (115, 2, N'Giỏ hoa hướng dương', 420000, 1);

GO

-- Thêm Yêu thích
INSERT INTO YeuThich (MaKhachHang, MaSanPham, NgayThem) VALUES
(11, 3, GETDATE()),
(12, 5, GETDATE()),
(13, 1, GETDATE()),
(14, 6, GETDATE()),
(15, 4, GETDATE()),
(16, 2, GETDATE()),
(17, 10, GETDATE()),
(18, 7, GETDATE()),
(19, 8, GETDATE()),
(20, 9, GETDATE());
GO

-- Thêm đánh giá
INSERT INTO DanhGia (SoSao, NoiDung, MaDonHang, MaSanPham) VALUES
(5, N'Giỏ hoa hướng dương rực rỡ, tràn đầy năng lượng.', 7, 2),
(4, N'Bình hoa xinh xắn, màu pastel dễ thương đúng như ảnh.', 20, 5),
(5, N'Hoa hồng đỏ thắm, đúng như lời tỏ tình lãng mạn.', 4, 1),
(5, N'Bó hoa baby đi kèm rất xinh, gói giấy đẹp. Shop làm việc có tâm.', 1, 4),
(5, N'Giỏ hoa lily sang trọng, tinh tế. Sẽ ủng hộ shop dài dài.', 2, 7),
(3, N'Hoa cúc họa mi nhìn mỏng manh, một vài cành hơi héo. Giao hàng thì nhanh.', 20, 8),
(5, N'Bó hoa cưới đẹp hơn cả mong đợi. 100 điểm!', 3, 10),
(5, N'Hoa tặng cô giáo rất đẹp và ý nghĩa. Cô khen tấm tắc.', 21, 11),
(4, N'Bình hoa trang trí Giáng sinh rất hợp không khí. Giao hàng hơi chậm.', 23, 13),
(5, N'Mua tặng mẹ hôm 20/10, mẹ mình rất vui và khen hoa đẹp.', 19, 14);
GO
GO

-- Thêm giỏ hàng
INSERT INTO GioHang (MaKhachHang, MaSanPham, SoLuong) VALUES
(11, 5, 2),
(12, 8, 1),
(13, 2, 3),
(14, 15, 1),
(15, 9, 4),
(16, 7, 2),
(17, 10, 1),
(18, 18, 2),
(19, 3, 5),
(20, 6, 1);
GO

INSERT INTO TaiKhoan (TenTaiKhoan, HoVaTen, Email, SoDienThoai, MatKhau, MaPhuongXa, DiaChi, MaQuyen, TrangThaiTaiKhoan) VALUES
('admin', N'Nguyễn Văn A', 'admin@gmail.com', '0912345678', '123456', 101, N'123 Lê Duẩn', 1, 1);