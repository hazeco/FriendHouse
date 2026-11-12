using System;
using System.Collections.Generic;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงรายละเอียดสินค้าฟิกเกอร์เชิงลึก (Views/Home/ProductDetail.cshtml)
// WHY: รวมข้อมูลสเปก รายละเอียดโมเดล 3D และสถานะสต็อกพร้อมจอง ป้องกันการผูก Domain Model ตรงๆ
public class ProductDetailViewModel
{
    // รหัสสินค้าในระบบฐานข้อมูล
    public int ProductId { get; set; }

    // รหัส SKU สินค้า
    public string Sku { get; set; } = string.Empty;

    // ชื่อสินค้าฟิกเกอร์เต็ม
    public string Name { get; set; } = string.Empty;

    // ชื่อหมวดหมู่สินค้า
    public string? CategoryName { get; set; }

    // ชื่อซีรีส์หรือเรื่อง
    public string? SeriesName { get; set; }

    // ชื่อแบรนด์หรือค่ายผู้ผลิต
    public string? BrandName { get; set; }

    // อัตราส่วนสเกล เช่น 1/7 หรือ 1/8
    public string? Scale { get; set; }

    // ความสูงของโมเดล (เซนติเมตร)
    public decimal? HeightCm { get; set; }

    // วัสดุที่ใช้ผลิต เช่น PVC, ABS, เรซิ่น
    public string? Material { get; set; }

    // ราคาขายปลีกหน้าร้าน (บาท)
    public decimal RetailPrice { get; set; }

    // ข้อความแสดงราคาขายปลีกจัดรูปแบบเงินบาทไทย เช่น ฿4,290
    public string PriceFormatted => $"฿{RetailPrice:N0}";

    // ราคาพิเศษสำหรับการสั่งจองพรีออเดอร์ล่วงหน้า (ถ้ามี)
    public decimal? PreorderPrice { get; set; }

    // ข้อความแสดงราคาพรีออเดอร์จัดรูปแบบเงินบาทไทย
    public string? PreorderPriceFormatted => PreorderPrice.HasValue ? $"฿{PreorderPrice.Value:N0}" : null;

    // ที่อยู่พาธไฟล์รูปภาพสินค้า
    public string ImageUrl { get; set; } = string.Empty;

    // ที่อยู่พาธไฟล์โมเดล 3 มิติ (.glb) สำหรับดูแบบ Interactive
    public string? Model3DPath { get; set; }

    // จำนวนสินค้าคงเหลือที่พร้อมให้สั่งจอง
    public int AvailableStock { get; set; }

    // สถานะว่ามีสินค้าคงเหลือให้สั่งจองหรือไม่
    public bool InStock => AvailableStock > 0;

    // กำหนดการประมาณการสินค้าเข้าสาขา เช่น เมษายน 2568
    public string? ExpectedArrival { get; set; }

    // คำอธิบายรายละเอียดสินค้าและงานปั้น
    public string? Description { get; set; }

    // ตารางข้อมูลจำเพาะเชิงเทคนิคของสินค้า (คู่คีย์-ค่า)
    public List<KeyValuePair<string, string>> Specs { get; set; } = new();

    // ข้อความแจ้งเตือนเงื่อนไขการรับสินค้าและชำระเงินที่หน้าร้าน
    public string NoticeText { get; set; } = "รับสินค้าและชำระเงินที่หน้าร้าน FriendHouse HQ สาขาสยามสแควร์ โดยไม่ต้องชำระเงินออนไลน์ล่วงหน้า";
}
