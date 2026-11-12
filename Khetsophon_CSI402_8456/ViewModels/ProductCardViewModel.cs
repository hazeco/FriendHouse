using System;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงผลการ์ดสินค้าฟิกเกอร์ในหน้าแรกและหน้าคอลเลกชัน
// WHY: รวมข้อมูลที่จำเป็นสำหรับการเรนเดอร์การ์ดสินค้า ป้องกันการส่ง Domain Entity Product ไปยัง View โดยตรง
public class ProductCardViewModel
{
    // รหัสสินค้าในระบบฐานข้อมูล
    public int ProductId { get; set; }

    // รหัส SKU ประจำสินค้า
    public string Sku { get; set; } = string.Empty;

    // ชื่อสินค้าฟิกเกอร์
    public string Name { get; set; } = string.Empty;

    // ชื่อซีรีส์หรืออนิเมะต้นสังกัด
    public string? SeriesName { get; set; }

    // ชื่อหมวดหมู่สินค้า
    public string? CategoryName { get; set; }

    // แบรนด์ผู้ผลิตสินค้า
    public string? BrandName { get; set; }

    // อัตราส่วนหรือสเกลของโมเดล เช่น 1/7, 1/8 หรือนอนสเกล
    public string? Scale { get; set; }

    // ราคาขายปลีกหน้าร้าน (บาท)
    public decimal RetailPrice { get; set; }

    // สตริงราคาที่จัดรูปแบบเงินบาทไทยแล้ว เช่น ฿4,290
    public string PriceFormatted => $"฿{RetailPrice:N0}";

    // ที่อยู่พาธไฟล์รูปภาพสินค้าใน wwwroot/media/images/
    public string ImageUrl { get; set; } = string.Empty;

    // จำนวนสต็อกสินค้าคงเหลือที่พร้อมให้สั่งจอง คำนวณจากจำนวนในคลังหักลบยอดจอง
    public int AvailableStock { get; set; }

    // สถานะว่ามีสินค้าคงเหลือพร้อมสั่งจองหรือไม่
    public bool InStock => AvailableStock > 0;

    // ข้อความแสดงสถานะสต็อก เช่น มีสินค้าพร้อมส่ง หรือ สินค้าหมดชั่วคราว
    public string StatusText => InStock ? $"มีสินค้า {AvailableStock} ชิ้น" : "สินค้าหมดชั่วคราว";

    // คลาสสีสำหรับ Badge แสดงสถานะสินค้า
    public string BadgeClass => InStock ? "bg-emerald-100 text-emerald-800" : "bg-rose-100 text-rose-800";
}
