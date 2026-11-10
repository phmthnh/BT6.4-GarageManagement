using System.Data;
using GarageManagement.DTO;

namespace GarageManagement.DAL;

internal sealed class RepairDAL
{
    internal DataTable FindVehicle(string plate)=>DatabaseHelper.Query("""
    SELECT x.BienSo,x.HieuXe,x.NamSanXuat,k.MaKH,k.TenKH,k.DienThoai,k.DiaChi
    FROM Xe x JOIN KhachHang k ON x.MaKH=k.MaKH WHERE x.BienSo=@plate
    """,Db.Code("@plate",plate,20));
    internal DataTable Parts()=>DatabaseHelper.Query("SELECT MaPhuTung,TenPhuTung,DonGia,SoLuongTon FROM PhuTung ORDER BY MaPhuTung");
    internal DataTable Tickets(bool paid)=>DatabaseHelper.Query("""
    SELECT MaPhieu,BienSo,NgayNhan,NoiDungSua,TongTien,TrangThai FROM PhieuSuaChua
    WHERE @paid=0 OR TrangThai=N'Đã thanh toán' ORDER BY MaPhieu DESC
    """,Db.Int("@paid",paid?1:0));
    internal decimal Revenue()=>DatabaseHelper.Scalar("SELECT COALESCE(SUM(TongTien),0) FROM PhieuSuaChua WHERE TrangThai=N'Đã thanh toán'");

    internal int Complete(RepairDraft draft)=>Db.Transaction((c,t)=>
    {
        var id=(int)Db.Scalar(c,t,"""
        INSERT PhieuSuaChua(BienSo,NoiDungSua,TongTien,TrangThai)
        OUTPUT INSERTED.MaPhieu VALUES(@plate,@note,0,N'Đã hoàn thành')
        """,Db.Code("@plate",draft.Plate,20),Db.Text("@note",draft.Description))!;
        decimal total=0;
        foreach(var line in draft.Lines.OrderBy(line=>line.MaPhuTung))
        {
            var price=Db.Scalar(c,t,"""
            UPDATE PhuTung SET SoLuongTon=SoLuongTon-@qty OUTPUT INSERTED.DonGia
            WHERE MaPhuTung=@part AND SoLuongTon>=@qty
            """,Db.Int("@part",line.MaPhuTung),Db.Int("@qty",line.SoLuong));
            if(price is not decimal unitPrice) throw new InvalidOperationException("Phụ tùng không đủ số lượng trong kho. Phiếu đã được hoàn tác.");
            total+=unitPrice*line.SoLuong+line.TienCong;
            Db.Execute(c,t,"INSERT ChiTietSuaChua VALUES(@ticket,@part,@qty,@labor)",
                Db.Int("@ticket",id),Db.Int("@part",line.MaPhuTung),Db.Int("@qty",line.SoLuong),Db.Money("@labor",line.TienCong));
        }
        if(total>9999999999999999.99m) throw new ArgumentException("Tổng tiền vượt giới hạn.");
        Db.Execute(c,t,"UPDATE PhieuSuaChua SET TongTien=@total WHERE MaPhieu=@id",Db.Money("@total",total),Db.Int("@id",id));
        return id;
    });

    internal void Pay(int id)
    {
        if(DatabaseHelper.Execute("UPDATE PhieuSuaChua SET TrangThai=N'Đã thanh toán' WHERE MaPhieu=@id AND TrangThai=N'Đã hoàn thành'",
            Db.Int("@id",id))!=1) throw new InvalidOperationException("Phiếu chưa hoàn thành hoặc đã thanh toán.");
    }

    internal RepairReceipt Receipt(int id)
    {
        var rows=DatabaseHelper.Query("SELECT MaPhieu,BienSo,NgayNhan,NoiDungSua,TongTien FROM PhieuSuaChua WHERE MaPhieu=@id AND TrangThai=N'Đã thanh toán'",Db.Int("@id",id));
        if(rows.Rows.Count==0) throw new InvalidOperationException("Chọn phiếu đã thanh toán để xuất hóa đơn.");
        var row=rows.Rows[0];
        return new((int)row["MaPhieu"],(string)row["BienSo"],(DateTime)row["NgayNhan"],row["NoiDungSua"].ToString()!, (decimal)row["TongTien"]);
    }
}
