SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF DB_ID(N'GarageDB') IS NULL EXEC(N'CREATE DATABASE [GarageDB]');
GO
USE [GarageDB];
GO
IF OBJECT_ID(N'dbo.KhachHang',N'U') IS NULL
BEGIN
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE KhachHang(MaKH int IDENTITY PRIMARY KEY,TenKH nvarchar(100) NOT NULL,DienThoai varchar(15) UNIQUE,DiaChi nvarchar(200));
CREATE TABLE Xe(BienSo varchar(20) PRIMARY KEY,MaKH int NOT NULL REFERENCES KhachHang,HieuXe nvarchar(50),NamSanXuat int);
CREATE TABLE PhuTung(MaPhuTung int IDENTITY PRIMARY KEY,TenPhuTung nvarchar(150) NOT NULL,DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0),SoLuongTon int NOT NULL CHECK(SoLuongTon>=0));
CREATE TABLE PhieuSuaChua(MaPhieu int IDENTITY PRIMARY KEY,BienSo varchar(20) NOT NULL REFERENCES Xe,NgayNhan datetime NOT NULL DEFAULT GETDATE(),NoiDungSua nvarchar(max),TongTien decimal(18,2) NOT NULL,TrangThai nvarchar(30) NOT NULL);
CREATE TABLE ChiTietSuaChua(MaPhieu int REFERENCES PhieuSuaChua,MaPhuTung int REFERENCES PhuTung,SoLuong int NOT NULL CHECK(SoLuong>0),TienCong decimal(18,2) NOT NULL CHECK(TienCong>=0),PRIMARY KEY(MaPhieu,MaPhuTung));
INSERT KhachHang(TenKH,DienThoai,DiaChi) VALUES(N'Chủ xe mẫu','0000000001',N'Địa chỉ mẫu');
INSERT Xe VALUES('30A-000.01',1,N'Toyota',2022);
INSERT PhuTung(TenPhuTung,DonGia,SoLuongTon) VALUES(N'Lọc dầu',150000,10),(N'Lọc gió',200000,5),(N'Bugi',100000,8);

COMMIT;
END;
GO
