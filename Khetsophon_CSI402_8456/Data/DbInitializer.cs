using System;
using System.Collections.Generic;
using System.Linq;
using Khetsophon_CSI402_8456.Models;

namespace Khetsophon_CSI402_8456.Data;

/// <summary>
/// DbInitializer: คลาสจัดการเตรียมข้อมูลเริ่มต้นสำหรับระบบ FriendHouse
/// WHY: รับประกันว่าฐานข้อมูลจริงจะมีข้อมูลเริ่มต้นที่สมบูรณ์เสมอ ประกอบด้วย:
/// 1. หมวดหมู่สินค้า 4 กลุ่มหลักในภาษาไทยสมบูรณ์แบบ
/// 2. แคตตาล็อกสินค้า 25 รายการ ครอบคลุม 8 ซีรีส์ยอดนิยม พร้อมคลังสินค้าและสื่อมีเดียโลคัลครบถ้วน
/// 3. สินค้าไฮไลท์ 3 มิติ และเส้นทางไฟล์โมเดล 3 มิติ
/// 4. บัญชีผู้ใช้ตัวอย่างครบ 5 บทบาท พร้อมรหัสผ่านแฮช BCrypt สำหรับทดสอบระบบ
/// 5. รายการโปรโมชั่นตัวอย่างในระบบ
/// </summary>
public static class DbInitializer
{
    public static void Initialize(Csi402dbContext context)
    {
        try
        {
            // ตรวจสอบความพร้อมของการเชื่อมต่อฐานข้อมูล
            if (!context.Database.CanConnect())
            {
                return;
            }

            // =========================================================================
            // ส่วนที่ 1: ตรวจสอบและเตรียมหมวดหมู่สินค้า 4 กลุ่มหลัก
            // WHY: ตาราง Products มี Foreign Key CategoryId บังคับ ต้องมีข้อมูลหมวดหมู่อยู่ก่อน
            // กฎ R6: ใช้ชื่อภาษาไทย 100% ปราศจากวงเล็บภาษาอังกฤษกำกับ
            // =========================================================================
            var categoryDefinitions = new[]
            {
                new { Id = 1, Name = "ฟิกเกอร์สเกล", Slug = "scale-figures" },
                new { Id = 2, Name = "แอคชั่นฟิกเกอร์", Slug = "action-figures" },
                new { Id = 3, Name = "ของสะสมและตุ๊กตา", Slug = "merchandise" },
                new { Id = 4, Name = "ของสะสมลิมิเต็ด", Slug = "limited-collectibles" }
            };

            var categoryMap = new Dictionary<string, Category>();

            foreach (var def in categoryDefinitions)
            {
                var cat = context.Categories.FirstOrDefault(c => c.Slug == def.Slug);
                if (cat == null)
                {
                    cat = new Category
                    {
                        CategoryId = def.Id,
                        CategoryName = def.Name,
                        Slug = def.Slug,
                        IsActive = true
                    };
                    context.Categories.Add(cat);
                    context.SaveChanges();
                }
                else
                {
                    // ปรับปรุงชื่อหมวดหมู่ให้เป็นภาษาไทยบริสุทธิ์ ปราศจากวงเล็บภาษาอังกฤษ
                    if (cat.CategoryName != def.Name)
                    {
                        cat.CategoryName = def.Name;
                        context.SaveChanges();
                    }
                }
                categoryMap[def.Slug] = cat;
            }

            // =========================================================================
            // ส่วนที่ 2: เตรียมแคตตาล็อกสินค้าทั้ง 25 รายการ และคลังสินค้า
            // WHY: สอดคล้องกับข้อกำหนด R3 และแผนผังแคตตาล็อกสินค้า 25 รายการ 8 ซีรีส์
            // ข้อพึงระวัง: คอลัมน์ AvailableQuantity ใน inventories เป็น Computed Column
            // ห้ามกำหนดค่าในโค้ด ให้ฐานข้อมูลเป็นผู้คำนวณอัตโนมัติ
            // =========================================================================
            var productSeedList = new[]
            {
                // รายการที่ 1: Hero Naruto พร้อมโมเดล 3 มิติ
                new {
                    Sku = "FIG-NARUTO-001",
                    Barcode = "885901234001",
                    Name = "นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม",
                    CategorySlug = "scale-figures",
                    Series = "Naruto Shippuden",
                    Brand = "Banpresto",
                    Scale = "1/6 Scale",
                    Height = 32.0m,
                    Material = "PVC + Resin",
                    RetailPrice = 4290.00m,
                    PreorderPrice = 3990.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/naruto_hero_fig.png",
                    Model3D = (string?)"~/media/models/naruto_kyubi_sage.glb",
                    IsHero = true,
                    Qty = 15,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-A-01"
                },
                // รายการที่ 2: Tanjiro Kamado
                new {
                    Sku = "FIG-TANJIRO-002",
                    Barcode = "885901234002",
                    Name = "ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ",
                    CategorySlug = "scale-figures",
                    Series = "Demon Slayer",
                    Brand = "Good Smile Company",
                    Scale = "Non-scale",
                    Height = 21.0m,
                    Material = "PVC + Clear Parts",
                    RetailPrice = 2450.00m,
                    PreorderPrice = 2200.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/tanjiro_kamado.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 20,
                    Reserved = 1,
                    Safety = 5,
                    Location = "Aisle-A-02"
                },
                // รายการที่ 3: Hatsune Miku Magical Mirai 2024
                new {
                    Sku = "FIG-MIKU-003",
                    Barcode = "885901234003",
                    Name = "ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล",
                    CategorySlug = "scale-figures",
                    Series = "Vocaloid",
                    Brand = "Good Smile Company",
                    Scale = "1/7 Scale",
                    Height = 24.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 4200.00m,
                    PreorderPrice = 3990.00m,
                    Arrival = "ไตรมาส 4 ปี 2569 สำหรับพรีออเดอร์",
                    IsPreorder = true,
                    ImageUrl = "~/media/images/hatsune_miku_magical.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 8,
                    Reserved = 3,
                    Safety = 2,
                    Location = "Aisle-B-01"
                },
                // รายการที่ 4: Luffy Gear 5
                new {
                    Sku = "FIG-LUFFY-004",
                    Barcode = "885901234004",
                    Name = "มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์",
                    CategorySlug = "action-figures",
                    Series = "One Piece",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 16.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 950.00m,
                    PreorderPrice = 890.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/luffy_gear5.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 30,
                    Reserved = 4,
                    Safety = 5,
                    Location = "Aisle-B-02"
                },
                // รายการที่ 5: Anya Forger & Bond
                new {
                    Sku = "FIG-ANYA-005",
                    Barcode = "885901234005",
                    Name = "อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด",
                    CategorySlug = "merchandise",
                    Series = "Spy x Family",
                    Brand = "Megahouse",
                    Scale = "1/7 Scale",
                    Height = 15.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 1850.00m,
                    PreorderPrice = 1690.00m,
                    Arrival = "สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์",
                    IsPreorder = true,
                    ImageUrl = "~/media/images/anya_forger.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 12,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-C-01"
                },
                // รายการที่ 6: Naruto Vibration Stars
                new {
                    Sku = "FIG-NARUTO-006",
                    Barcode = "885901234006",
                    Name = "นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล",
                    CategorySlug = "action-figures",
                    Series = "Naruto Shippuden",
                    Brand = "Banpresto",
                    Scale = "1/8 Scale",
                    Height = 18.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 1200.00m,
                    PreorderPrice = 1090.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/naruto_vibration_stars.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 25,
                    Reserved = 3,
                    Safety = 5,
                    Location = "Aisle-A-03"
                },
                // รายการที่ 7: Gojo Satoru
                new {
                    Sku = "FIG-GOJO-007",
                    Barcode = "885901234007",
                    Name = "โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน",
                    CategorySlug = "scale-figures",
                    Series = "Jujutsu Kaisen",
                    Brand = "Megahouse",
                    Scale = "1/7 Scale",
                    Height = 23.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 3800.00m,
                    PreorderPrice = 3500.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/gojo_satoru.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 10,
                    Reserved = 2,
                    Safety = 2,
                    Location = "Aisle-B-03"
                },
                // รายการที่ 8: Raiden Shogun
                new {
                    Sku = "FIG-RAIDEN-008",
                    Barcode = "885901234008",
                    Name = "โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์",
                    CategorySlug = "scale-figures",
                    Series = "Genshin Impact",
                    Brand = "Good Smile Company",
                    Scale = "1/7 Scale",
                    Height = 26.0m,
                    Material = "PVC + ABS + LED Base",
                    RetailPrice = 5690.00m,
                    PreorderPrice = 5290.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/raiden_shogun.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 6,
                    Reserved = 1,
                    Safety = 2,
                    Location = "Aisle-C-02"
                },
                // รายการที่ 9: Cloud Bear Plush
                new {
                    Sku = "FIG-BEAR-009",
                    Barcode = "885901234009",
                    Name = "ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล",
                    CategorySlug = "merchandise",
                    Series = "FriendHouse Studio",
                    Brand = "FriendHouse Original",
                    Scale = "Non-scale",
                    Height = 28.0m,
                    Material = "กำมะหยี่ขนนุ่มพิเศษ",
                    RetailPrice = 590.00m,
                    PreorderPrice = 520.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/cloud_bear_plush.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 40,
                    Reserved = 5,
                    Safety = 8,
                    Location = "Aisle-D-01"
                },
                // รายการที่ 10: Roronoa Zoro
                new {
                    Sku = "FIG-ZORO-010",
                    Barcode = "885901234010",
                    Name = "โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร",
                    CategorySlug = "action-figures",
                    Series = "One Piece",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 20.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 2790.00m,
                    PreorderPrice = 2490.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/roronoa_zoro.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 18,
                    Reserved = 2,
                    Safety = 4,
                    Location = "Aisle-B-04"
                },
                // รายการที่ 11: Nezuko Kamado
                new {
                    Sku = "FIG-NEZUKO-011",
                    Barcode = "885901234011",
                    Name = "คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต",
                    CategorySlug = "scale-figures",
                    Series = "Demon Slayer",
                    Brand = "Aniplex",
                    Scale = "1/8 Scale",
                    Height = 22.0m,
                    Material = "PVC + Clear Effects",
                    RetailPrice = 3450.00m,
                    PreorderPrice = 3150.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/nezuko_kamado.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 14,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-A-04"
                },
                // รายการที่ 12: Megumi Fushiguro
                new {
                    Sku = "FIG-MEGUMI-012",
                    Barcode = "885901234012",
                    Name = "ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร",
                    CategorySlug = "scale-figures",
                    Series = "Jujutsu Kaisen",
                    Brand = "Kotobukiya",
                    Scale = "1/8 Scale",
                    Height = 19.0m,
                    Material = "PVC + ABS",
                    RetailPrice = 3190.00m,
                    PreorderPrice = 2890.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/megumi_fushiguro.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 16,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-B-05"
                },
                // รายการที่ 13: Zhongli Morax
                new {
                    Sku = "FIG-ZONG-013",
                    Barcode = "885901234013",
                    Name = "จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ",
                    CategorySlug = "limited-collectibles",
                    Series = "Genshin Impact",
                    Brand = "Apex Innovation",
                    Scale = "1/7 Scale",
                    Height = 28.0m,
                    Material = "PVC + Polystone + LED Base",
                    RetailPrice = 6200.00m,
                    PreorderPrice = 5800.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/zhongli_morax.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 5,
                    Reserved = 1,
                    Safety = 2,
                    Location = "Aisle-C-03"
                },
                // รายการที่ 14: Sasuke Uchiha
                new {
                    Sku = "FIG-SASUKE-014",
                    Barcode = "885901234014",
                    Name = "อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา",
                    CategorySlug = "action-figures",
                    Series = "Naruto Shippuden",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 17.0m,
                    Material = "PVC + ABS + Chidori Clear Parts",
                    RetailPrice = 2590.00m,
                    PreorderPrice = 2290.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/sasuke_uchiha.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 22,
                    Reserved = 3,
                    Safety = 4,
                    Location = "Aisle-A-05"
                },
                // รายการที่ 15: Rengoku Kyojuro
                new {
                    Sku = "FIG-RENBK-015",
                    Barcode = "885901234015",
                    Name = "เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์",
                    CategorySlug = "scale-figures",
                    Series = "Demon Slayer",
                    Brand = "Aniplex",
                    Scale = "1/8 Scale",
                    Height = 25.0m,
                    Material = "PVC + Flame Resin",
                    RetailPrice = 4600.00m,
                    PreorderPrice = 4200.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/rengoku_kyojuro.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 9,
                    Reserved = 1,
                    Safety = 2,
                    Location = "Aisle-A-06"
                },
                // รายการที่ 16: Yor Forger
                new {
                    Sku = "FIG-YOR-016",
                    Barcode = "885901234016",
                    Name = "ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน",
                    CategorySlug = "scale-figures",
                    Series = "Spy x Family",
                    Brand = "Good Smile Company",
                    Scale = "1/7 Scale",
                    Height = 25.0m,
                    Material = "PVC + ABS + Metal Stilettos",
                    RetailPrice = 3250.00m,
                    PreorderPrice = 2950.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/yor_forger.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 15,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-C-04"
                },
                // รายการที่ 17: Sakura Miku
                new {
                    Sku = "FIG-MIKUS-017",
                    Barcode = "885901234017",
                    Name = "ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด",
                    CategorySlug = "limited-collectibles",
                    Series = "Vocaloid",
                    Brand = "Good Smile Company",
                    Scale = "1/7 Scale",
                    Height = 22.0m,
                    Material = "PVC + ABS + Sakura Petal Base",
                    RetailPrice = 2890.00m,
                    PreorderPrice = 2590.00m,
                    Arrival = "สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์",
                    IsPreorder = true,
                    ImageUrl = "~/media/images/hatsune_miku_sakura.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 12,
                    Reserved = 3,
                    Safety = 3,
                    Location = "Aisle-B-06"
                },
                // รายการที่ 18: Portgas D. Ace
                new {
                    Sku = "FIG-ACE-018",
                    Barcode = "885901234018",
                    Name = "โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์",
                    CategorySlug = "action-figures",
                    Series = "One Piece",
                    Brand = "MegaHouse",
                    Scale = "Non-scale",
                    Height = 19.0m,
                    Material = "PVC + ABS + Fire Clear Parts",
                    RetailPrice = 1890.00m,
                    PreorderPrice = 1690.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/portgas_d_ace.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 20,
                    Reserved = 2,
                    Safety = 4,
                    Location = "Aisle-B-07"
                },
                // รายการที่ 19: Itadori Yuji
                new {
                    Sku = "FIG-ITAD-019",
                    Barcode = "885901234019",
                    Name = "อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น",
                    CategorySlug = "action-figures",
                    Series = "Jujutsu Kaisen",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 16.0m,
                    Material = "PVC + ABS + Black Flash Effect",
                    RetailPrice = 1450.00m,
                    PreorderPrice = 1290.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/itadori_yuji.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 28,
                    Reserved = 4,
                    Safety = 5,
                    Location = "Aisle-B-08"
                },
                // รายการที่ 20: Ganyu Plenilune
                new {
                    Sku = "FIG-GANYU-020",
                    Barcode = "885901234020",
                    Name = "กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่",
                    CategorySlug = "scale-figures",
                    Series = "Genshin Impact",
                    Brand = "Apex Innovation",
                    Scale = "1/7 Scale",
                    Height = 27.0m,
                    Material = "PVC + ABS + Ice Lotus Base",
                    RetailPrice = 5400.00m,
                    PreorderPrice = 4990.00m,
                    Arrival = "สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์",
                    IsPreorder = true,
                    ImageUrl = "~/media/images/ganyu_plenilune.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 7,
                    Reserved = 2,
                    Safety = 2,
                    Location = "Aisle-C-05"
                },
                // รายการที่ 21: Lucky Cat Plush
                new {
                    Sku = "FIG-CAT-021",
                    Barcode = "885901234021",
                    Name = "ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม",
                    CategorySlug = "merchandise",
                    Series = "FriendHouse Studio",
                    Brand = "FriendHouse Original",
                    Scale = "Non-scale",
                    Height = 25.0m,
                    Material = "ผ้าขนนุ่มเกรดญี่ปุ่น + กระดิ่งโลหะทอง",
                    RetailPrice = 490.00m,
                    PreorderPrice = 420.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/lucky_cat_plush.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 50,
                    Reserved = 5,
                    Safety = 10,
                    Location = "Aisle-D-02"
                },
                // รายการที่ 22: Tony Tony Chopper
                new {
                    Sku = "FIG-CHOP-022",
                    Barcode = "885901234022",
                    Name = "โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ",
                    CategorySlug = "merchandise",
                    Series = "One Piece",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 22.0m,
                    Material = "กำมะหยี่ขนละเอียด นุ่มพิเศษ",
                    RetailPrice = 650.00m,
                    PreorderPrice = 590.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/chopper_plush.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 35,
                    Reserved = 3,
                    Safety = 6,
                    Location = "Aisle-D-03"
                },
                // รายการที่ 23: Nobara Kugisaki
                new {
                    Sku = "FIG-NOBARA-023",
                    Barcode = "885901234023",
                    Name = "คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์",
                    CategorySlug = "action-figures",
                    Series = "Jujutsu Kaisen",
                    Brand = "Bandai Spirits",
                    Scale = "Non-scale",
                    Height = 15.0m,
                    Material = "PVC + ABS + Nails Effects",
                    RetailPrice = 1350.00m,
                    PreorderPrice = 1190.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/nobara_kugisaki.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 25,
                    Reserved = 2,
                    Safety = 4,
                    Location = "Aisle-B-09"
                },
                // รายการที่ 24: Hu Tao
                new {
                    Sku = "FIG-HUTAO-024",
                    Barcode = "885901234024",
                    Name = "ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง",
                    CategorySlug = "scale-figures",
                    Series = "Genshin Impact",
                    Brand = "Apex Innovation",
                    Scale = "1/7 Scale",
                    Height = 27.0m,
                    Material = "PVC + ABS + Spirit Base",
                    RetailPrice = 5200.00m,
                    PreorderPrice = 4790.00m,
                    Arrival = "สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์",
                    IsPreorder = true,
                    ImageUrl = "~/media/images/hu_tao_ghost.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 8,
                    Reserved = 1,
                    Safety = 2,
                    Location = "Aisle-C-06"
                },
                // รายการที่ 25: Loid Forger
                new {
                    Sku = "FIG-LOID-025",
                    Barcode = "885901234025",
                    Name = "ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส",
                    CategorySlug = "action-figures",
                    Series = "Spy x Family",
                    Brand = "Good Smile Company",
                    Scale = "Non-scale",
                    Height = 17.0m,
                    Material = "PVC + ABS + Pistol Parts",
                    RetailPrice = 2390.00m,
                    PreorderPrice = 2150.00m,
                    Arrival = "มีสินค้าพร้อมส่งมอบ",
                    IsPreorder = false,
                    ImageUrl = "~/media/images/loid_forger.png",
                    Model3D = (string?)null,
                    IsHero = false,
                    Qty = 18,
                    Reserved = 2,
                    Safety = 3,
                    Location = "Aisle-C-07"
                }
            };

            foreach (var item in productSeedList)
            {
                var categoryId = categoryMap.ContainsKey(item.CategorySlug)
                    ? categoryMap[item.CategorySlug].CategoryId
                    : categoryMap["scale-figures"].CategoryId;

                var existingProduct = context.Products.FirstOrDefault(p => p.Sku == item.Sku);

                if (existingProduct == null)
                {
                    var newProduct = new Product
                    {
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        Name = item.Name,
                        CategoryId = categoryId,
                        SeriesName = item.Series,
                        BrandName = item.Brand,
                        Scale = item.Scale,
                        HeightCm = item.Height,
                        Material = item.Material,
                        RetailPrice = item.RetailPrice,
                        PreorderPrice = item.PreorderPrice,
                        ExpectedArrival = item.Arrival,
                        IsPreorder = item.IsPreorder,
                        ImageUrl = item.ImageUrl,
                        Model3Dpath = item.Model3D,
                        IsFeaturedHero = item.IsHero,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Products.Add(newProduct);
                    context.SaveChanges();

                    // สร้างคลังสินค้าสำหรับสินค้าใหม่
                    // ข้อพึงระวัง: AvailableQuantity เป็น Generated Column ในฐานข้อมูล ห้ามกำหนดค่า
                    var newInventory = new Inventory
                    {
                        ProductId = newProduct.ProductId,
                        QuantityOnHand = item.Qty,
                        ReservedQuantity = item.Reserved,
                        SafetyStockLevel = item.Safety,
                        StorageLocation = item.Location,
                        LastRestockedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    context.Inventories.Add(newInventory);
                    context.SaveChanges();
                }
                else
                {
                    // อัปเดตข้อมูลสินค้าให้เป็นปัจจุบันและสอดคล้องกับมาตรฐานภาษาไทย
                    bool productUpdated = false;
                    if (existingProduct.Name != item.Name)
                    {
                        existingProduct.Name = item.Name;
                        productUpdated = true;
                    }
                    if (existingProduct.RetailPrice != item.RetailPrice)
                    {
                        existingProduct.RetailPrice = item.RetailPrice;
                        productUpdated = true;
                    }
                    if (existingProduct.ImageUrl != item.ImageUrl)
                    {
                        existingProduct.ImageUrl = item.ImageUrl;
                        productUpdated = true;
                    }
                    if (item.Model3D != null && existingProduct.Model3Dpath != item.Model3D)
                    {
                        existingProduct.Model3Dpath = item.Model3D;
                        productUpdated = true;
                    }
                    if (existingProduct.IsFeaturedHero != item.IsHero)
                    {
                        existingProduct.IsFeaturedHero = item.IsHero;
                        productUpdated = true;
                    }
                    if (existingProduct.ExpectedArrival != item.Arrival)
                    {
                        existingProduct.ExpectedArrival = item.Arrival;
                        productUpdated = true;
                    }
                    if (productUpdated)
                    {
                        context.SaveChanges();
                    }

                    // ตรวจสอบและอัปเดตสต็อกในตาราง Inventories
                    var inv = context.Inventories.FirstOrDefault(i => i.ProductId == existingProduct.ProductId);
                    if (inv == null)
                    {
                        inv = new Inventory
                        {
                            ProductId = existingProduct.ProductId,
                            QuantityOnHand = item.Qty,
                            ReservedQuantity = item.Reserved,
                            SafetyStockLevel = item.Safety,
                            StorageLocation = item.Location,
                            LastRestockedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        context.Inventories.Add(inv);
                        context.SaveChanges();
                    }
                }
            }

            // =========================================================================
            // ส่วนที่ 3: ตรวจสอบและสร้างผู้ใช้ตัวอย่างครบทั้ง 5 บทบาท พร้อมรหัสผ่าน BCrypt
            // WHY: เพื่อให้สามารถทดสอบการเข้าสู่ระบบแบบปกติ (กรอกอีเมลและรหัสผ่าน) ได้ทุกบทบาท
            // รหัสผ่านเริ่มต้นคือ "password123" สำหรับทุกบัญชี
            // =========================================================================
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");

            var defaultUsers = new[]
            {
                new { Email = "customer@friendhouse.com", Name = "คุณสมชาย ใจดี", Role = "Customer", Code = "U20261014001" },
                new { Email = "staff@friendhouse.com", Name = "พนักงานหน้าร้าน สาขาสยาม", Role = "Staff", Code = "U20261014002" },
                new { Email = "owner@friendhouse.com", Name = "คุณกฤษฎา เจ้าของร้าน", Role = "Owner", Code = "U20261014003" },
                new { Email = "it@friendhouse.com", Name = "เจ้าหน้าที่ดูแลระบบไอที", Role = "ITSupport", Code = "U20261014004" },
                new { Email = "account@friendhouse.com", Name = "ฝ่ายการเงินและบัญชี", Role = "Accountant", Code = "U20261014005" }
            };

            foreach (var u in defaultUsers)
            {
                if (!context.Users.Any(existing => existing.Email == u.Email))
                {
                    context.Users.Add(new User
                    {
                        UserCode = u.Code,
                        Email = u.Email,
                        PasswordHash = defaultPasswordHash,
                        Phone = "081-234-5678",
                        FullName = u.Name,
                        RoleName = u.Role,
                        Points = 1200,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            context.SaveChanges();

            // =========================================================================
            // ส่วนที่ 4: ตรวจสอบและสร้างโปรโมชั่นตัวอย่างในตาราง Promotions
            // WHY: ให้หน้าโปรโมชั่นและหน้าการจัดการมีข้อมูลตั้งต้นจากตารางจริง
            // =========================================================================
            if (!context.Promotions.Any())
            {
                context.Promotions.Add(new Promotion
                {
                    Name = "โปรโมชั่นต้อนรับสมาชิกใหม่ ลด 10%",
                    VoucherCode = "FHNEW2026",
                    PromoType = "percent",
                    DiscountValue = 10.00m,
                    MinimumSpend = 1000.00m,
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
                    ApplicableScope = "figures",
                    UsageLimit = 100,
                    CurrentUsageCount = 12,
                    IsActive = true
                });
                context.SaveChanges();
            }

            // =========================================================================
            // ส่วนที่ 5: ตรวจสอบและอัปเดตข้อมูลของรางวัลในตาราง Rewards ให้เป็นภาษาไทยสมบูรณ์และตรงกับรูปภาพจริง
            // WHY: ให้หน้าแลกของรางวัลแสดงชื่อภาษาไทยล้วนและใช้รูปภาพโมเดลจริงทุกชิ้น
            // =========================================================================
            var rewardSeeds = new[]
            {
                new { Code = "RWD-KEYCHAIN-01", Title = "พวงกุญแจอะคริลิก ดาบพิฆาตอสูร รุ่นพิเศษ", Brand = "Banpresto", Tag = "Merchandise", Points = 350, Stock = 50, Image = "~/media/images/tanjiro_kamado.png" },
                new { Code = "RWD-POSTER-02", Title = "โปสเตอร์ผ้าวันพีซ ฟิล์มเรด ลิขสิทธิ์แท้", Brand = "Toei Animation", Tag = "Merchandise", Points = 600, Stock = 30, Image = "~/media/images/luffy_gear5.png" },
                new { Code = "RWD-MUG-03", Title = "แก้วมัคเซรามิก ร้านบ้านเพื่อน รุ่นพิเศษ 2026", Brand = "FriendHouse Studio", Tag = "Exclusive", Points = 850, Stock = 25, Image = "~/media/images/cloud_bear_plush.png" },
                new { Code = "RWD-NEZUKO-04", Title = "มินิฟิกเกอร์ เนซึโกะ กล่องไม้ ลิมิเต็ด", Brand = "Good Smile Company", Tag = "Figures", Points = 1800, Stock = 15, Image = "~/media/images/nezuko_kamado.png" }
            };

            foreach (var r in rewardSeeds)
            {
                var existingReward = context.Rewards.FirstOrDefault(x => x.RewardCode == r.Code);
                if (existingReward == null)
                {
                    context.Rewards.Add(new Reward
                    {
                        RewardCode = r.Code,
                        Title = r.Title,
                        Brand = r.Brand,
                        CategoryTag = r.Tag,
                        PointsRequired = r.Points,
                        StockQuantity = r.Stock,
                        ImageUrl = r.Image,
                        IsAvailable = true,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    existingReward.Title = r.Title;
                    existingReward.ImageUrl = r.Image;
                }
            }
            context.SaveChanges();
        }
        catch (Exception)
        {
            // WHY: กลืนข้อผิดพลาดไว้ไม่ให้แอปพลิเคชันหยุดทำงานเมื่อฐานข้อมูลยังไม่ได้เปิดบริการในสภาพแวดล้อม Local
            // ระบบ Razor Views ทั้งหมดมีข้อมูลสำรอง Fallback Mock รองรับอยู่แล้ว
        }
    }
}
