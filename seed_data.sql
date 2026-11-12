USE CSI402DB;
SET NAMES utf8mb4;

-- 1. Ensure Categories (100% Full Thai Language, no English in parentheses)
INSERT INTO categories (CategoryId, CategoryName, Slug, IsActive) VALUES
(1, 'ฟิกเกอร์สเกล', 'scale-figures', 1),
(2, 'แอคชั่นฟิกเกอร์', 'action-figures', 1),
(3, 'ของสะสมและตุ๊กตา', 'merchandise', 1),
(4, 'ของสะสมลิมิเต็ด', 'limited-collectibles', 1)
ON DUPLICATE KEY UPDATE
  CategoryName = VALUES(CategoryName),
  Slug = VALUES(Slug),
  IsActive = VALUES(IsActive);

-- 2. Ensure Product 1 (Naruto Hero with 3D Model)
INSERT INTO products (ProductId, Sku, Barcode, Name, CategoryId, SeriesName, BrandName, Scale, HeightCm, Material, RetailPrice, PreorderPrice, ExpectedArrival, IsPreorder, ImageUrl, Model3DPath, IsFeaturedHero, IsActive)
VALUES
(1, 'FIG-NARUTO-001', '885901234001', 'นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม', 1, 'Naruto Shippuden', 'Banpresto', '1/6 Scale', 32.00, 'PVC + Resin', 4290.00, 3990.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/naruto_hero_fig.png', '~/media/models/naruto_kyubi_sage.glb', 1, 1)
ON DUPLICATE KEY UPDATE
  Sku = VALUES(Sku),
  Barcode = VALUES(Barcode),
  Name = VALUES(Name),
  CategoryId = VALUES(CategoryId),
  SeriesName = VALUES(SeriesName),
  BrandName = VALUES(BrandName),
  Scale = VALUES(Scale),
  HeightCm = VALUES(HeightCm),
  Material = VALUES(Material),
  RetailPrice = VALUES(RetailPrice),
  PreorderPrice = VALUES(PreorderPrice),
  ExpectedArrival = VALUES(ExpectedArrival),
  IsPreorder = VALUES(IsPreorder),
  ImageUrl = VALUES(ImageUrl),
  Model3DPath = VALUES(Model3DPath),
  IsFeaturedHero = VALUES(IsFeaturedHero),
  IsActive = VALUES(IsActive);

-- Ensure Products 2 to 25
INSERT INTO products (ProductId, Sku, Barcode, Name, CategoryId, SeriesName, BrandName, Scale, HeightCm, Material, RetailPrice, PreorderPrice, ExpectedArrival, IsPreorder, ImageUrl, Model3DPath, IsFeaturedHero, IsActive)
VALUES
(2, 'FIG-TANJIRO-002', '885901234002', 'ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ', 1, 'Demon Slayer', 'Good Smile Company', 'Non-scale', 21.00, 'PVC + Clear Parts', 2450.00, 2200.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/tanjiro_kamado.png', NULL, 0, 1),
(3, 'FIG-MIKU-003', '885901234003', 'ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล', 1, 'Vocaloid', 'Good Smile Company', '1/7 Scale', 24.00, 'PVC + ABS', 4200.00, 3990.00, 'ไตรมาส 4 ปี 2569 สำหรับพรีออเดอร์', 1, '~/media/images/hatsune_miku_magical.png', NULL, 0, 1),
(4, 'FIG-LUFFY-004', '885901234004', 'มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์', 2, 'One Piece', 'Bandai Spirits', 'Non-scale', 16.00, 'PVC + ABS', 950.00, 890.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/luffy_gear5.png', NULL, 0, 1),
(5, 'FIG-ANYA-005', '885901234005', 'อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด', 3, 'Spy x Family', 'Megahouse', '1/7 Scale', 15.00, 'PVC + ABS', 1850.00, 1690.00, 'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์', 1, '~/media/images/anya_forger.png', NULL, 0, 1),
(6, 'FIG-NARUTO-006', '885901234006', 'นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล', 2, 'Naruto Shippuden', 'Banpresto', '1/8 Scale', 18.00, 'PVC + ABS', 1200.00, 1090.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/naruto_vibration_stars.png', NULL, 0, 1),
(7, 'FIG-GOJO-007', '885901234007', 'โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน', 1, 'Jujutsu Kaisen', 'Megahouse', '1/7 Scale', 23.00, 'PVC + ABS', 3800.00, 3500.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/gojo_satoru.png', NULL, 0, 1),
(8, 'FIG-RAIDEN-008', '885901234008', 'โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์', 1, 'Genshin Impact', 'Good Smile Company', '1/7 Scale', 26.00, 'PVC + ABS + LED Base', 5690.00, 5290.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/raiden_shogun.png', NULL, 0, 1),
(9, 'FIG-BEAR-009', '885901234009', 'ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล', 3, 'FriendHouse Studio', 'FriendHouse Original', 'Non-scale', 28.00, 'กำมะหยี่ขนนุ่มพิเศษ', 590.00, 520.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/cloud_bear_plush.png', NULL, 0, 1),
(10, 'FIG-ZORO-010', '885901234010', 'โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร', 2, 'One Piece', 'Bandai Spirits', 'Non-scale', 20.00, 'PVC + ABS', 2790.00, 2490.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/roronoa_zoro.png', NULL, 0, 1),
(11, 'FIG-NEZUKO-011', '885901234011', 'คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต', 1, 'Demon Slayer', 'Aniplex', '1/8 Scale', 22.00, 'PVC + Clear Effects', 3450.00, 3150.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/nezuko_kamado.png', NULL, 0, 1),
(12, 'FIG-MEGUMI-012', '885901234012', 'ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร', 1, 'Jujutsu Kaisen', 'Kotobukiya', '1/8 Scale', 19.00, 'PVC + ABS', 3190.00, 2890.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/megumi_fushiguro.png', NULL, 0, 1),
(13, 'FIG-ZONG-013', '885901234013', 'จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ', 4, 'Genshin Impact', 'Apex Innovation', '1/7 Scale', 28.00, 'PVC + Polystone + LED Base', 6200.00, 5800.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/zhongli_morax.png', NULL, 0, 1),
(14, 'FIG-SASUKE-014', '885901234014', 'อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา', 2, 'Naruto Shippuden', 'Bandai Spirits', 'Non-scale', 17.00, 'PVC + ABS + Chidori Clear Parts', 2590.00, 2290.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/sasuke_uchiha.png', NULL, 0, 1),
(15, 'FIG-RENBK-015', '885901234015', 'เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์', 1, 'Demon Slayer', 'Aniplex', '1/8 Scale', 25.00, 'PVC + Flame Resin', 4600.00, 4200.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/rengoku_kyojuro.png', NULL, 0, 1),
(16, 'FIG-YOR-016', '885901234016', 'ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน', 1, 'Spy x Family', 'Good Smile Company', '1/7 Scale', 25.00, 'PVC + ABS + Metal Stilettos', 3250.00, 2950.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/yor_forger.png', NULL, 0, 1),
(17, 'FIG-MIKUS-017', '885901234017', 'ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด', 4, 'Vocaloid', 'Good Smile Company', '1/7 Scale', 22.00, 'PVC + ABS + Sakura Petal Base', 2890.00, 2590.00, 'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์', 1, '~/media/images/hatsune_miku_sakura.png', NULL, 0, 1),
(18, 'FIG-ACE-018', '885901234018', 'โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์', 2, 'One Piece', 'MegaHouse', 'Non-scale', 19.00, 'PVC + ABS + Fire Clear Parts', 1890.00, 1690.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/portgas_d_ace.png', NULL, 0, 1),
(19, 'FIG-ITAD-019', '885901234019', 'อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น', 2, 'Jujutsu Kaisen', 'Bandai Spirits', 'Non-scale', 16.00, 'PVC + ABS + Black Flash Effect', 1450.00, 1290.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/itadori_yuji.png', NULL, 0, 1),
(20, 'FIG-GANYU-020', '885901234020', 'กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่', 1, 'Genshin Impact', 'Apex Innovation', '1/7 Scale', 27.00, 'PVC + ABS + Ice Lotus Base', 5400.00, 4990.00, 'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์', 1, '~/media/images/ganyu_plenilune.png', NULL, 0, 1),
(21, 'FIG-CAT-021', '885901234021', 'ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม', 3, 'FriendHouse Studio', 'FriendHouse Original', 'Non-scale', 25.00, 'ผ้าขนนุ่มเกรดญี่ปุ่น + กระดิ่งโลหะทอง', 490.00, 420.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/lucky_cat_plush.png', NULL, 0, 1),
(22, 'FIG-CHOP-022', '885901234022', 'โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ', 3, 'One Piece', 'Bandai Spirits', 'Non-scale', 22.00, 'กำมะหยี่ขนละเอียด นุ่มพิเศษ', 650.00, 590.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/chopper_plush.png', NULL, 0, 1),
(23, 'FIG-NOBARA-023', '885901234023', 'คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์', 2, 'Jujutsu Kaisen', 'Bandai Spirits', 'Non-scale', 15.00, 'PVC + ABS + Nails Effects', 1350.00, 1190.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/nobara_kugisaki.png', NULL, 0, 1),
(24, 'FIG-HUTAO-024', '885901234024', 'ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง', 1, 'Genshin Impact', 'Apex Innovation', '1/7 Scale', 27.00, 'PVC + ABS + Spirit Base', 5200.00, 4790.00, 'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์', 1, '~/media/images/hu_tao_ghost.png', NULL, 0, 1),
(25, 'FIG-LOID-025', '885901234025', 'ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส', 2, 'Spy x Family', 'Good Smile Company', 'Non-scale', 17.00, 'PVC + ABS + Pistol Parts', 2390.00, 2150.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/loid_forger.png', NULL, 0, 1)
ON DUPLICATE KEY UPDATE
  Sku = VALUES(Sku),
  Barcode = VALUES(Barcode),
  Name = VALUES(Name),
  CategoryId = VALUES(CategoryId),
  SeriesName = VALUES(SeriesName),
  BrandName = VALUES(BrandName),
  Scale = VALUES(Scale),
  HeightCm = VALUES(HeightCm),
  Material = VALUES(Material),
  RetailPrice = VALUES(RetailPrice),
  PreorderPrice = VALUES(PreorderPrice),
  ExpectedArrival = VALUES(ExpectedArrival),
  IsPreorder = VALUES(IsPreorder),
  ImageUrl = VALUES(ImageUrl),
  IsActive = VALUES(IsActive);

-- 3. Ensure Inventories for Products 1 to 25 (AvailableQuantity is generated, do not insert)
INSERT INTO inventories (ProductId, QuantityOnHand, ReservedQuantity, SafetyStockLevel, StorageLocation)
VALUES
(1, 15, 2, 3, 'Aisle-A-01'),
(2, 20, 1, 5, 'Aisle-A-02'),
(3, 8, 3, 2, 'Aisle-B-01'),
(4, 30, 4, 5, 'Aisle-B-02'),
(5, 12, 2, 3, 'Aisle-C-01'),
(6, 25, 3, 5, 'Aisle-A-03'),
(7, 10, 2, 2, 'Aisle-B-03'),
(8, 6, 1, 2, 'Aisle-C-02'),
(9, 40, 5, 8, 'Aisle-D-01'),
(10, 18, 2, 4, 'Aisle-B-04'),
(11, 14, 2, 3, 'Aisle-A-04'),
(12, 16, 2, 3, 'Aisle-B-05'),
(13, 5, 1, 2, 'Aisle-C-03'),
(14, 22, 3, 4, 'Aisle-A-05'),
(15, 9, 1, 2, 'Aisle-A-06'),
(16, 15, 2, 3, 'Aisle-C-04'),
(17, 12, 3, 3, 'Aisle-B-06'),
(18, 20, 2, 4, 'Aisle-B-07'),
(19, 28, 4, 5, 'Aisle-B-08'),
(20, 7, 2, 2, 'Aisle-C-05'),
(21, 50, 5, 10, 'Aisle-D-02'),
(22, 35, 3, 6, 'Aisle-D-03'),
(23, 25, 2, 4, 'Aisle-B-09'),
(24, 8, 1, 2, 'Aisle-C-06'),
(25, 18, 2, 3, 'Aisle-C-07')
ON DUPLICATE KEY UPDATE
  QuantityOnHand = VALUES(QuantityOnHand),
  ReservedQuantity = VALUES(ReservedQuantity),
  SafetyStockLevel = VALUES(SafetyStockLevel),
  StorageLocation = VALUES(StorageLocation);

-- 4. Ensure Sample Preorders in preorders table
-- Preorder 1: PO-2026-1001 (Phone 081-234-5678, User 3: สมชาย ใจดี, Miku 4,200)
INSERT INTO preorders (PreorderId, ReservationNumber, UserId, ContactPhone, EstimatedPickupDate, CurrentStatus, TotalFinalPrice, PickupBranch, CreatedAt)
VALUES
(1, 'PO-2026-1001', 3, '081-234-5678', DATE_ADD(CURRENT_DATE, INTERVAL 7 DAY), 'ReadyForPickup', 4200.00, 'FriendHouse HQ สาขาสยามสแควร์', NOW()),
(2, 'DB123456', 3, '081-234-5678', DATE_ADD(CURRENT_DATE, INTERVAL 5 DAY), 'ReadyForPickup', 1200.00, 'FriendHouse HQ สาขาสยามสแควร์', NOW()),
(3, 'PO-000482', 1, '089-111-2233', DATE_ADD(CURRENT_DATE, INTERVAL 10 DAY), 'ReadyForPickup', 2450.00, 'FriendHouse HQ สาขาสยามสแควร์', NOW())
ON DUPLICATE KEY UPDATE
  ReservationNumber = VALUES(ReservationNumber),
  CurrentStatus = VALUES(CurrentStatus),
  ContactPhone = VALUES(ContactPhone);

-- 5. Ensure Preorder Items (LineTotal is generated column, do not insert into LineTotal)
DELETE FROM preorderitems WHERE PreorderId IN (1, 2, 3);
INSERT INTO preorderitems (PreorderId, ProductId, UnitPrice, Quantity) VALUES
(1, 3, 4200.00, 1),
(2, 1, 1200.00, 1),
(3, 2, 2450.00, 1);

-- 6. Ensure 5 Promotions for Owner Management
INSERT INTO promotions (PromotionId, Name, VoucherCode, PromoType, DiscountValue, MinimumSpend, StartDate, EndDate, ApplicableScope, UsageLimit, CurrentUsageCount, IsActive)
VALUES
(1, 'ล้างสต๊อกส่งท้ายปี 2026', 'CLEAR2026', 'percent', 20.00, 1000.00, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 30 DAY), 'clearance', 100, 12, 1),
(2, 'ซื้อ 1 แถม 1 ฟิกเกอร์หมวดหมู่พิเศษ', 'B1G1FREE', 'bogo', 500.00, 2000.00, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 15 DAY), 'figures', 50, 5, 1),
(3, 'สมาชิกใหม่รับส่วนลดทันที', 'WELCOME10', 'percent', 10.00, 500.00, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 90 DAY), 'all', 500, 84, 1),
(4, 'พรีออเดอร์ล่วงหน้าลดพิเศษ 15%', 'PRE15', 'percent', 15.00, 3000.00, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 60 DAY), 'all', 200, 38, 1),
(5, 'ส่วนลดพิเศษสเกลฟิกเกอร์ 1/7', 'SCALE20', 'fixed', 500.00, 4000.00, CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 20 DAY), 'figures', 80, 19, 0)
ON DUPLICATE KEY UPDATE
  Name = VALUES(Name),
  DiscountValue = VALUES(DiscountValue),
  IsActive = VALUES(IsActive);

-- 7. Ensure Financial Ledger Entries for Accountant
INSERT INTO financialledger (EntryId, EntryType, CategoryName, Description, Amount, EntryDate, RecordedByUserId, CreatedAt)
VALUES
(1, 'expense', 'ค่าเช่าสถานที่', 'ค่าเช่าร้าน FriendHouse HQ สาขาสยามสแควร์ ประจำเดือน', 45000.00, CURRENT_DATE, 7, NOW()),
(2, 'expense', 'สาธารณูปโภค', 'ค่าน้ำ-ค่าไฟฟ้า และอินเทอร์เน็ตความเร็วสูงประจำร้าน', 6800.00, CURRENT_DATE, 7, NOW()),
(3, 'expense', 'ค่าขนส่งและโลจิสติกส์', 'ค่าขนส่งชิปปิ้งนำเข้าฟิกเกอร์จากประเทศญี่ปุ่น ล็อตพิเศษ', 12500.00, CURRENT_DATE, 7, NOW()),
(4, 'income', 'รายรับขายหน้าร้าน', 'สรุปยอดเงินสดและเงินโอนจากการจำหน่ายสินค้าหน้าร้าน', 89400.00, CURRENT_DATE, 7, NOW())
ON DUPLICATE KEY UPDATE
  CategoryName = VALUES(CategoryName),
  Amount = VALUES(Amount);
