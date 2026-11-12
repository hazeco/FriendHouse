namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับค้นหาใบสั่งจองพรีออเดอร์และรหัสรับของรางวัลที่หน้าเคาน์เตอร์พนักงาน
// WHY: แยกพารามิเตอร์การค้นหาของพนักงานหน้าร้านออกจากโมเดลหลักของฐานข้อมูล
public class CounterPreorderViewModel
{
    // เบอร์โทรศัพท์ของลูกค้าที่ใช้ค้นหาใบจอง
    public string Phone { get; set; } = string.Empty;

    // หมายเลขใบสั่งจองพรีออเดอร์ เช่น PO-2026-1001
    public string ReservationNumber { get; set; } = string.Empty;

    // รหัสอ้างอิงการแลกของรางวัลด้วยแต้มสะสม เช่น RWD-166742
    public string RedemptionNumber { get; set; } = string.Empty;
}
