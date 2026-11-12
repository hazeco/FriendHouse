using System;
using System.Collections.Generic;

namespace Khetsophon_CSI402_8456.ViewModels;

// READ: ViewModel หลักสำหรับส่งข้อมูลไปยังหน้าแรก (Views/Home/Index.cshtml)
// WHY: รวบรวมข้อมูล Hero Section 3D, รายการสินค้าเด่น และรายการโปรโมชั่นเข้าด้วยกันในออบเจกต์เดียว
public class HomeIndexViewModel
{
    // ข้อมูลสินค้าไฮไลท์ใน Hero Section (โมเดล 3D และรายละเอียดเด่น)
    public HeroProductViewModel? HeroProduct { get; set; }

    // รายการสินค้าแนะนำ/สินค้าคอลเลกชันเด่นที่ดึงจากฐานข้อมูลจริง
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();

    // รายการโปรโมชั่นเด่นหน้าแรก
    public List<HomePromotionCardViewModel> Promotions { get; set; } = new();

    // อีเมลที่ส่งมาจากระบบเข้าสู่ระบบสำหรับการจำลองข้อมูล
    public string? LoginEmail { get; set; }

    // รหัสผ่านที่ส่งมาจากระบบเข้าสู่ระบบสำหรับการจำลองข้อมูล
    public string? LoginPassword { get; set; }
}

// READ: ViewModel สำหรับข้อมูลสินค้าไฮไลท์ 3 มิติใน Hero Section
public class HeroProductViewModel
{
    // รหัสสินค้าของ Hero Figure
    public int ProductId { get; set; }

    // ชื่อสินค้าไฮไลท์
    public string Name { get; set; } = string.Empty;

    // คำอธิบายรายละเอียดสินค้า
    public string Description { get; set; } = string.Empty;

    // ราคาจัดรูปแบบสกุลเงินบาทไทย
    public string PriceFormatted { get; set; } = string.Empty;

    // พาธไฟล์โมเดล 3 มิติ (.glb) ภายใต้ wwwroot/media/models/
    public string Model3DPath { get; set; } = string.Empty;

    // พาธไฟล์ภาพสำรองกรณีเบราว์เซอร์ไม่รองรับ WebGL
    public string FallbackImageUrl { get; set; } = string.Empty;

    // ป้ายข้อความกำกับสถานะ เช่น ฟิกเกอร์คอลเลกชัน · มีสินค้า
    public string BadgeText { get; set; } = string.Empty;

    // ข้อความแจ้งเตือนเงื่อนไขการรับสินค้า
    public string NoticeText { get; set; } = string.Empty;

    // ข้อมูลจำเพาะทางเทคนิคของฟิกเกอร์ เช่น น้ำหนัก วัสดุ ความสูง สเกล
    public List<KeyValuePair<string, string>> Specs { get; set; } = new();
}

// READ: ViewModel สำหรับการ์ดโปรโมชั่นหน้าแรก
public class HomePromotionCardViewModel
{
    // ป้ายแท็กหมวดหมู่โปรโมชั่น
    public string Tag { get; set; } = string.Empty;

    // หัวข้อโปรโมชั่น
    public string Title { get; set; } = string.Empty;

    // รายละเอียดโปรโมชั่น
    public string Description { get; set; } = string.Empty;

    // รหัสคูปองส่วนลดถ้ามี
    public string? Code { get; set; }

    // วันหมดอายุของโปรโมชั่น
    public string? ExpiryText { get; set; }
}
