using System;
using System.Collections.Generic;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับแสดงรายการสินค้าทั้งหมดและการค้นหา/กรอง (Views/Home/Collection.cshtml)
// WHY: รวบรวมผลลัพธ์สินค้าพร้อมตัวเลือกสำหรับ Dropdown ค้นหาและสถานะตัวกรองในแบบแผน MVC
public class ProductCatalogViewModel
{
    // รายการสินค้าฟิกเกอร์ที่ผ่านการกรองตามเงื่อนไข
    public List<ProductCardViewModel> Products { get; set; } = new();

    // คำค้นหาที่ผู้ใช้งานพิมพ์เข้ามา (จาก Navbar หรือหน้า Collection)
    public string? SearchTerm { get; set; }

    // หมวดหมู่สินค้าที่เลือกกรอง เช่น สเกลฟิกเกอร์ หรือ ทั้งหมด
    public string? SelectedCategory { get; set; }

    // ซีรีส์อนิเมะที่เลือกกรอง เช่น นารูโตะ ดาบพิฆาตอสูร หรือ ทั้งหมด
    public string? SelectedSeries { get; set; }

    // สถานะสินค้าที่เลือกกรอง เช่น มีสินค้า หรือ ทั้งหมด
    public string? SelectedStatus { get; set; }

    // ช่วงราคาที่เลือกกรอง เช่น ฿0 - ฿1,000 หรือ ทั้งหมด
    public string? SelectedPriceRange { get; set; }

    // รายการชื่อหมวดหมู่ทั้งหมดที่มีในฐานข้อมูลสำหรับใส่ใน Select Dropdown
    public List<string> AvailableCategories { get; set; } = new();

    // รายการชื่อซีรีส์ทั้งหมดที่มีในฐานข้อมูลสำหรับใส่ใน Select Dropdown
    public List<string> AvailableSeries { get; set; } = new();

    // รายการตัวเลือกช่วงราคาสำหรับใส่ใน Select Dropdown
    public List<string> AvailablePriceRanges { get; set; } = new()
    {
        "ทั้งหมด",
        "฿0 - ฿1,000",
        "฿1,001 - ฿3,000",
        "฿3,001 - ฿5,000",
        "มากกว่า ฿5,000"
    };

    // จำนวนสินค้าทั้งหมดที่พบจากการค้นหาและกรอง
    public int TotalItems => Products.Count;
}
