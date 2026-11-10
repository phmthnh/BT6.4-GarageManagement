namespace GarageManagement.DTO;

public sealed record RepairLine(int MaPhuTung,string TenPhuTung,int SoLuong,decimal TienCong);
public sealed record RepairDraft(string Plate,string Description,IReadOnlyList<RepairLine> Lines);
public sealed record RepairReceipt(int MaPhieu,string BienSo,DateTime NgayNhan,string NoiDungSua,decimal TongTien);
