using System.Data;
using GarageManagement.DAL;
using GarageManagement.DTO;

namespace GarageManagement.BUS;

public sealed class RepairBUS
{
    private readonly RepairDAL dal=new();
    private static string Plate(string value)
    {
        var plate=value.Trim().ToUpperInvariant();
        if(plate.Length is 0 or >20) throw new ArgumentException("Nhập biển số tối đa 20 ký tự.");
        return plate;
    }
    public DataTable FindVehicle(string plate)=>dal.FindVehicle(Plate(plate));
    public DataTable Parts()=>dal.Parts();
    public DataTable Tickets(bool paid=false)=>dal.Tickets(paid);
    public decimal Revenue()=>dal.Revenue();

    public int Complete(RepairDraft draft)
    {
        if(draft.Lines.Count==0 || draft.Lines.Select(x=>x.MaPhuTung).Distinct().Count()!=draft.Lines.Count)
            throw new ArgumentException("Phiếu phải có phụ tùng, mỗi phụ tùng một dòng.");
        if(draft.Lines.Any(x=>x.MaPhuTung<=0 || x.SoLuong<=0 || x.TienCong<0 || x.TienCong>9999999999999999.99m || Math.Round(x.TienCong,2)!=x.TienCong))
            throw new ArgumentException("Số lượng và tiền công không hợp lệ.");
        return dal.Complete(draft with {Plate=Plate(draft.Plate)});
    }
    public void Pay(int id)=>dal.Pay(id);
    public RepairReceipt Receipt(int id)=>dal.Receipt(id);
}
