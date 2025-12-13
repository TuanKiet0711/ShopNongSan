# SHOP NÔNG SẢN SẠCH

## 1. Giới thiệu đề tài
Website **Shop Nông Sản Sạch** là hệ thống thương mại điện tử cho phép khách hàng
mua các sản phẩm nông sản trực tuyến và hỗ trợ quản trị viên quản lý hoạt động bán hàng,
đơn hàng và thống kê doanh thu.

## 3. Chức năng chính
### 3.1. Chức năng khách hàng
- Đăng ký, đăng nhập tài khoản
- Xem danh sách sản phẩm
- Tìm kiếm, xem chi tiết sản phẩm
- Thêm sản phẩm vào giỏ hàng
- Đặt hàng
- Chọn ngày giao hàng mong muốn
- Thanh toán:
  - Thanh toán khi nhận hàng (COD)
  - Thanh toán bằng thẻ (Stripe)
### 4.2. Chức năng quản trị (Admin)
- Dashboard thống kê tổng quan
- Thống kê đơn hàng theo khoảng ngày, xuất báo cáo
- Biểu đồ:
  - Doanh thu
  - Số đơn hàng
- Thống kê:
  - Đơn hàng đã đặt
  - Đơn hàng bị hủy
- Xuất báo cáo Excel theo khoảng thời gian
- Quản lý (thêm, xóa, sửa, tìm kiếm):
  - Sản phẩm
  - Danh mục
  - Thương hiệu
  - Đơn hàng

---

## 5. Hướng dẫn cài đặt & chạy project

### Bước 1: Mở project
- Mở file `ShopNongSan.sln` bằng Visual Studio 2022+
### Bước 2: Tạo database
- Project đã chuẩn bị sẵn file SchemaAndData.sql
- Tạo database tên NongSan rồi chạy lệnh từ : CREATE TABLE DanhMuc
### Bước 3: Cấu hình cơ sở dữ liệu
- Mở file `appsettings.json`
- Cập nhật chuỗi kết nối SQL Server cho phù hợp với máy:

Chuỗi kết nối database
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=NongSan;Trusted_Connection=True;TrustServerCertificate=True"
}
```
### Bước 4: Chạy chương trình

### Bước 5: Dùng tài khoản admin để thao tác tất cả chức năng
- Tên đăng nhập: kietletuan002
- Mật khẩu: kietlatoi1
### Bước 6: Dùng thông tin thẻ để thanh toán (nếu chọn phương thức thanh toán Stripe)
- Card:
    + Số thẻ: 4242 4242 4242 4242
    + Ngày hết hạn: 12/34
    + CVC: 123




