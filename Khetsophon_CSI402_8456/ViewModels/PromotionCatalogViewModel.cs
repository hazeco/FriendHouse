using System;
using System.Collections.Generic;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel สำหรับหน้าแสดงโปรโมชั่นและสิทธิพิเศษของลูกค้า (Views/Home/Promotion.cshtml)
// WHY: แยกข้อมูลการแสดงผลโปรโมชั่นออกจาก Domain Model Promotion และรองรับการค้นหาโปรโมชั่นด้วยคำค้นหา
public class PromotionCatalogViewModel : List<Promotion>
{
    // คำค้นหาชื่อโปรโมชั่นหรือรหัสคูปอง
    public string? SearchTerm { get; set; }

    // ประเภทโปรโมชั่นที่เลือกกรอง
    public string? SelectedType { get; set; }

    // รายการรหัสคูปองที่ลูกค้ากดเก็บไว้ใน Session
    public List<string> CollectedCodes { get; set; } = new();

    // รายการการ์ดโปรโมชั่นที่แปลงเป็นภาษาไทยพร้อมแสดงผลบนหน้าเว็บ
    public List<PromotionCardItemViewModel> PromotionCards { get; set; } = new();
}

// READ: ViewModel สำหรับข้อมูลการ์ดโปรโมชั่นแต่ละรายการ
public class PromotionCardItemViewModel
{
    // รหัสโปรโมชั่นในฐานข้อมูล
    public int PromotionId { get; set; }

    // ป้ายกำกับประเภทโปรโมชั่นภาษาไทย เช่น ส่วนลดเปอร์เซ็นต์, ซื้อ 1 แถม 1
    public string Tag { get; set; } = "โปรโมชั่น";

    // ชื่อแคมเปญโปรโมชั่น
    public string Title { get; set; } = string.Empty;

    // คำอธิบายขอบเขตและจำนวนสิทธิ์ของโปรโมชั่นภาษาไทย
    public string Description { get; set; } = string.Empty;

    // ช่วงระยะเวลาที่จัดโปรโมชั่น
    public string Period { get; set; } = string.Empty;

    // เงื่อนไขการใช้รหัสส่วนลดและยอดซื้อขั้นต่ำ
    public string Condition { get; set; } = string.Empty;

    // รหัสคูปองโปรโมชั่น
    public string VoucherCode { get; set; } = string.Empty;

    // ขอบเขตสินค้าที่ใช้ได้จริงในระบบ (all, figures, clearance)
    public string ApplicableScope { get; set; } = "all";

    // สถานะว่าลูกค้าเก็บคูปองนี้ไว้ใช้งานแล้วหรือยัง
    public bool IsCollected { get; set; }
}

