USE CSI402DB;
SET NAMES utf8mb4;

-- 1. Ensure Categories
INSERT IGNORE INTO categories (CategoryId, CategoryName, Slug, IsActive) VALUES
(1, 'ฟิกเกอร์สเกล (Scale Figures)', 'scale-figures', 1),
(2, 'แอคชั่นฟิกเกอร์ (Action Figures)', 'action-figures', 1),
(3, 'ของสะสมและตุ๊กตา (Merchandise)', 'merchandise', 1);

-- 2. Update Product 1 (Naruto Hero with 3D Model)
UPDATE products SET
  Sku = 'FIG-NARUTO-001',
  Barcode = '885901234001',
  Name = 'นารูโตะ อุซึมากิ โหมดคิวบิเซจ — Ultimate Ninja Storm',
  CategoryId = 1,
  SeriesName = 'Naruto Shippuden',
  BrandName = 'Banpresto',
  Scale = '1/6 Scale',
  HeightCm = 32.00,
  Material = 'PVC + Resin',
  RetailPrice = 4290.00,
  ExpectedArrival = 'มีสินค้าพร้อมส่งมอบ',
  IsPreorder = 0,
  ImageUrl = '~/media/images/naruto_hero_fig.png',
  Model3DPath = '~/media/models/naruto_kyubi_sage.glb',
  IsFeaturedHero = 1,
  IsActive = 1
WHERE ProductId = 1;

-- Ensure Products 2 to 5
INSERT INTO products (ProductId, Sku, Barcode, Name, CategoryId, SeriesName, BrandName, Scale, HeightCm, Material, RetailPrice, PreorderPrice, ExpectedArrival, IsPreorder, ImageUrl, Model3DPath, IsFeaturedHero, IsActive)
VALUES
(2, 'FIG-TANJIRO-002', '885901234002', 'Tanjiro Kamado - Hinokami Kagura Dance', 1, 'Demon Slayer', 'Good Smile Company', 'Non-scale', 21.00, 'PVC + Clear Parts', 2450.00, 2200.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/tanjiro_kamado.png', NULL, 0, 1),
(3, 'FIG-MIKU-003', '885901234003', 'Hatsune Miku - Magical Mirai 2024 Ver.', 1, 'Vocaloid', 'Good Smile Company', '1/7 Scale', 24.00, 'PVC + ABS', 4200.00, 3990.00, 'ไตรมาส 4 ปี 2569', 1, '~/media/images/hatsune_miku_magical.png', NULL, 0, 1),
(4, 'FIG-LUFFY-004', '885901234004', 'Monkey D. Luffy - Gear 5 King of Artist', 2, 'One Piece', 'Bandai Spirits', 'Non-scale', 16.00, 'PVC + ABS', 950.00, 890.00, 'มีสินค้าพร้อมส่งมอบ', 0, '~/media/images/luffy_gear5.png', NULL, 0, 1),
(5, 'FIG-ANYA-005', '885901234005', 'Anya Forger & Bond - Spy x Family', 3, 'Spy x Family', 'Megahouse', '1/7 Scale', 15.00, 'PVC + ABS', 1850.00, 1690.00, 'สินค้าเข้าเร็วๆ นี้', 1, '~/media/images/anya_forger.png', NULL, 0, 1)
ON DUPLICATE KEY UPDATE
  Name = VALUES(Name),
  RetailPrice = VALUES(RetailPrice),
  ImageUrl = VALUES(ImageUrl),
  IsActive = VALUES(IsActive);

-- 3. Ensure Inventories for Products 1 to 5 (AvailableQuantity is generated, do not insert)
INSERT INTO inventories (ProductId, QuantityOnHand, ReservedQuantity, SafetyStockLevel, StorageLocation)
VALUES
(1, 15, 2, 3, 'Aisle-A-01'),
(2, 20, 1, 5, 'Aisle-A-02'),
(3, 8, 3, 2, 'Aisle-B-01'),
(4, 30, 4, 5, 'Aisle-B-02'),
(5, 12, 2, 3, 'Aisle-C-01')
ON DUPLICATE KEY UPDATE
  QuantityOnHand = VALUES(QuantityOnHand),
  ReservedQuantity = VALUES(ReservedQuantity);

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
