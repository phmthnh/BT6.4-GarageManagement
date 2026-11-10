using System.Data;
using Microsoft.Data.SqlClient;
using GarageManagement.BUS;
using GarageManagement.DTO;

namespace GarageManagement.GUI;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        Shown+=(_,_)=>Run(LoadScreen);
    }
    private void Run(Action action)
    {
        try { action(); }
        catch(Exception ex) when(ex is SqlException or ArgumentException or InvalidOperationException or OverflowException or IOException or UnauthorizedAccessException)
        {
            var message=ex is SqlException sql ? sql.Number==547 ? "Dữ liệu đang được sử dụng hoặc mã tham chiếu không tồn tại." : "Không thể thao tác với SQL Server. Kiểm tra kết nối và dữ liệu." : ex.Message;
            MessageBox.Show(this,message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
    private void Catalog(object? sender,EventArgs args)=>Run(()=>
    {
        using var dialog=new CatalogForm();
        dialog.ShowDialog(this);
        LoadScreen();
    });


    private readonly RepairBUS service=new();
    private readonly System.ComponentModel.BindingList<RepairLine> lines=new();
    private string? foundPlate;

    private void LoadScreen()
    {
        parts.DisplayMember="TenPhuTung";
        parts.ValueMember="MaPhuTung";
        parts.DataSource=service.Parts();
        draft.DataSource=lines;
        RefreshReport();
    }
    private void RefreshReport()
    {
        tickets.DataSource=service.Tickets();
        paid.DataSource=service.Tickets(true);
        revenue.Text=$"Tổng tiền đã thanh toán: {service.Revenue():N2} đ";
    }
    private void Lookup(object? sender,EventArgs args)=>Run(()=>
    {
        foundPlate=null;
        var vehicles=service.FindVehicle(plate.Text);
        if(vehicles.Rows.Count==0)
        {
            vehicleInfo.Text="Xe mới. Thêm chủ xe và xe trong Quản lý danh mục.";
            return;
        }
        var row=vehicles.Rows[0];
        foundPlate=(string)row["BienSo"];
        vehicleInfo.Text=$"{foundPlate} | {row["TenKH"]} | {row["HieuXe"]}";
    });
    private void AddLine(object? sender,EventArgs args)=>Run(()=>
    {
        if(parts.SelectedValue is not int id) throw new ArgumentException("Chọn phụ tùng.");
        if(lines.Any(line=>line.MaPhuTung==id)) throw new ArgumentException("Phụ tùng đã có trong phiếu. Xóa dòng để nhập lại.");
        lines.Add(new(id,parts.Text,(int)quantity.Value,labor.Value));
    });
    private void RemoveLine(object? sender,EventArgs args)
    {
        if(draft.CurrentRow?.DataBoundItem is RepairLine line) lines.Remove(line);
    }
    private void Complete(object? sender,EventArgs args)=>Run(()=>
    {
        if(foundPlate is null || !string.Equals(foundPlate,plate.Text.Trim(),StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Tra cứu biển số xe trước khi hoàn tất phiếu.");
        var id=service.Complete(new(foundPlate,description.Text,lines.ToArray()));
        lines.Clear();
        description.Clear();
        LoadScreen();
        MessageBox.Show(this,$"Đã hoàn tất phiếu số {id}.","Phiếu sửa chữa");
    });
    private int SelectedTicket()=>tickets.CurrentRow?.DataBoundItem is DataRowView row?(int)row["MaPhieu"]:0;
    private void Pay(object? sender,EventArgs args)=>Run(()=>
    {
        var id=SelectedTicket();
        if(id==0) throw new ArgumentException("Chọn phiếu sửa chữa.");
        if(MessageBox.Show(this,$"Xác nhận thanh toán phiếu {id}?","Thanh toán",MessageBoxButtons.YesNo)!=DialogResult.Yes) return;
        service.Pay(id);
        RefreshReport();
    });
    private void Export(object? sender,EventArgs args)=>Run(()=>
    {
        var receipt=service.Receipt(SelectedTicket());
        using var dialog=new SaveFileDialog {Filter="Hóa đơn TXT (*.txt)|*.txt",FileName=$"HoaDon_{receipt.MaPhieu}.txt"};
        if(dialog.ShowDialog(this)!=DialogResult.OK) return;
        File.WriteAllText(dialog.FileName,$"HÓA ĐƠN SỬA CHỮA\nPhiếu: {receipt.MaPhieu}\nBiển số: {receipt.BienSo}\nNgày nhận: {receipt.NgayNhan:dd/MM/yyyy HH:mm}\nNội dung: {receipt.NoiDungSua}\nTổng tiền: {receipt.TongTien:N2} đ\nĐã thanh toán");
    });

}
