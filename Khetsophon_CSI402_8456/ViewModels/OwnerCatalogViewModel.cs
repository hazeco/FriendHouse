using System;
using System.Collections.Generic;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.ViewModels;

// คลาส ViewModel สำหรับหน้าจัดการสินค้าและของแลกแต้มของเจ้าของร้าน
public class OwnerCatalogViewModel
{
    // ชื่อแท็บที่กำลังเปิดใช้งานอยู่ (products สำหรับสินค้าขาย หรือ rewards สำหรับของแลกแต้ม)
    public string ActiveTab { get; set; } = "products";

    // คำค้นหาสำหรับกรองรายการสินค้าหรือของรางวัล
    public string? SearchQuery { get; set; }

    // รหัสหมวดหมู่ที่เลือกกรองในแท็บสินค้า
    public string CategoryFilter { get; set; } = "all";

    // รูปแบบการขายที่เลือกกรอง (all, sale, preorder, hidden)
    public string SaleModeFilter { get; set; } = "all";

    // รายการสินค้าในหน้าปัจจุบัน
    public List<Product> Products { get; set; } = new();

    // รายการของรางวัลแลกแต้มในหน้าปัจจุบัน
    public List<Reward> Rewards { get; set; } = new();

    // รายชื่อหมวดหมู่ทั้งหมดที่มีในฐานข้อมูลสำหรับตัวเลือกแบบเลื่อนลง
    public List<Category> AllCategories { get; set; } = new();

    // รายการเส้นทางรูปภาพทั้งหมดที่มีอยู่ในโฟลเดอร์ wwwroot/media/images สำหรับเลือกใช้งาน
    public List<string> AvailableImages { get; set; } = new();

    // ชุดข้อมูลรหัส SKU หรือชื่อสินค้าที่มีรายการแลกแต้มคู่กันอยู่แล้วในตาราง Rewards
    public HashSet<string> LinkedRewardTitles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // จำนวนสินค้าทั้งหมดในฐานข้อมูล
    public int TotalProducts { get; set; }

    // จำนวนสินค้าที่เปิดวางขายหน้าร้านอยู่
    public int ActiveSaleProducts { get; set; }

    // จำนวนสินค้าที่เปิดรับพรีออเดอร์อยู่
    public int PreorderProducts { get; set; }

    // จำนวนรายการของแลกแต้มทั้งหมดในฐานข้อมูล
    public int TotalRewards { get; set; }

    // จำนวนรายการของแลกแต้มที่เปิดให้แลกอยู่
    public int ActiveRewards { get; set; }

    // จำนวนรายการทั้งหมดของแท็บที่กำลังแสดงอยู่
    public int TotalCount { get; set; }

    // หมายเลขหน้าปัจจุบันสำหรับการแบ่งหน้า
    public int CurrentPage { get; set; } = 1;

    // จำนวนรายการต่อหน้า (มาตรฐาน 10 รายการต่อหน้า)
    public int PageSize { get; set; } = 10;

    // จำนวนหน้าทั้งหมดสำหรับแท็บที่กำลังแสดงอยู่
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
}

