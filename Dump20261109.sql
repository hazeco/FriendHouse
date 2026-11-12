CREATE DATABASE  IF NOT EXISTS `csi402db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `csi402db`;
-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: csi402db
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `cartitems`
--

DROP TABLE IF EXISTS `cartitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cartitems` (
  `CartItemId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `ProductId` int NOT NULL,
  `Quantity` int NOT NULL DEFAULT '1',
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CartItemId`),
  UNIQUE KEY `uq_cart_user_product` (`UserId`,`ProductId`),
  KEY `fk_cart_product` (`ProductId`),
  CONSTRAINT `fk_cart_product` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_cart_user` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `chk_cart_qty` CHECK ((`Quantity` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cartitems`
--

LOCK TABLES `cartitems` WRITE;
/*!40000 ALTER TABLE `cartitems` DISABLE KEYS */;
INSERT INTO `cartitems` VALUES (9,3,1,1,'2026-11-08 20:03:58'),(10,1,1,1,'2026-11-09 13:17:29');
/*!40000 ALTER TABLE `cartitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `CategoryId` int NOT NULL AUTO_INCREMENT,
  `CategoryName` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Slug` varchar(100) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`CategoryId`),
  UNIQUE KEY `uq_categories_slug` (`Slug`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'ฟิกเกอร์สเกล','scale-figures',1),(2,'แอคชั่นฟิกเกอร์','action-figures',1),(3,'ของสะสมและตุ๊กตา','merchandise',1),(4,'ของสะสมลิมิเต็ด','limited-collectibles',1);
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `financialledger`
--

DROP TABLE IF EXISTS `financialledger`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `financialledger` (
  `EntryId` int NOT NULL AUTO_INCREMENT,
  `EntryType` varchar(20) NOT NULL DEFAULT 'expense',
  `CategoryName` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Description` varchar(255) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Amount` decimal(12,2) NOT NULL,
  `EntryDate` date NOT NULL,
  `RecordedByUserId` int NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`EntryId`),
  KEY `idx_ledger_date_type` (`EntryDate`,`EntryType`),
  KEY `fk_ledger_user` (`RecordedByUserId`),
  CONSTRAINT `fk_ledger_user` FOREIGN KEY (`RecordedByUserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_ledger_amount` CHECK ((`Amount` > 0)),
  CONSTRAINT `chk_ledger_type` CHECK ((`EntryType` in (_utf8mb4'expense',_utf8mb4'income')))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `financialledger`
--

LOCK TABLES `financialledger` WRITE;
/*!40000 ALTER TABLE `financialledger` DISABLE KEYS */;
INSERT INTO `financialledger` VALUES (1,'expense','ค่าเช่าสถานที่','ค่าเช่าร้าน FriendHouse HQ สาขาสยามสแควร์ ประจำเดือน',45000.00,'2026-11-08',7,'2026-11-08 00:52:02'),(2,'expense','ค่าสาธารณูปโภค','ค่าน้ำ-ค่าไฟฟ้า และอินเทอร์เน็ตความเร็วสูงประจำร้าน',6800.00,'2026-11-08',7,'2026-11-08 00:52:02'),(3,'expense','ค่าขนส่งและโลจิสติกส์','ค่าขนส่งชิปปิ้งนำเข้าฟิกเกอร์จากประเทศญี่ปุ่น ล็อตพิเศษ',12500.00,'2026-11-08',7,'2026-11-08 00:52:02'),(4,'income','รายได้จากการขายหน้าร้าน','สรุปยอดเงินสดและเงินโอนจากการจำหน่ายสินค้าหน้าร้าน',89400.00,'2026-11-08',7,'2026-11-08 00:52:02');
/*!40000 ALTER TABLE `financialledger` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `inventories`
--

DROP TABLE IF EXISTS `inventories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `inventories` (
  `InventoryId` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `QuantityOnHand` int NOT NULL DEFAULT '0',
  `ReservedQuantity` int NOT NULL DEFAULT '0',
  `AvailableQuantity` int GENERATED ALWAYS AS ((`QuantityOnHand` - `ReservedQuantity`)) STORED,
  `SafetyStockLevel` int NOT NULL DEFAULT '3',
  `StorageLocation` varchar(100) DEFAULT NULL,
  `LastRestockedAt` datetime DEFAULT NULL,
  `UpdatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`InventoryId`),
  UNIQUE KEY `uq_inventories_product` (`ProductId`),
  CONSTRAINT `fk_inv_product` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_inv_qoh` CHECK ((`QuantityOnHand` >= 0)),
  CONSTRAINT `chk_inv_rsv` CHECK ((`ReservedQuantity` >= 0)),
  CONSTRAINT `chk_inv_safety` CHECK ((`SafetyStockLevel` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inventories`
--

LOCK TABLES `inventories` WRITE;
/*!40000 ALTER TABLE `inventories` DISABLE KEYS */;
INSERT INTO `inventories` (`InventoryId`, `ProductId`, `QuantityOnHand`, `ReservedQuantity`, `SafetyStockLevel`, `StorageLocation`, `LastRestockedAt`, `UpdatedAt`) VALUES (1,1,23,4,3,'Aisle-A-01','2026-10-14 17:30:13','2026-11-09 20:17:03'),(2,2,20,0,5,'Aisle-A-02',NULL,'2026-11-09 20:19:26'),(3,3,8,2,2,'Aisle-B-01',NULL,'2026-11-09 20:19:26'),(4,4,30,4,5,'Aisle-B-02',NULL,'2026-11-08 00:51:22'),(5,5,11,1,3,'Aisle-C-01',NULL,'2026-11-09 20:17:03'),(6,6,25,3,5,'Aisle-A-03',NULL,'2026-11-09 20:17:03'),(7,7,10,2,2,'Aisle-B-03',NULL,'2026-11-09 20:17:03'),(8,8,6,1,2,'Aisle-C-02',NULL,'2026-11-09 20:17:03'),(9,9,40,5,8,'Aisle-D-01',NULL,'2026-11-09 20:17:03'),(10,10,18,2,4,'Aisle-B-04',NULL,'2026-11-09 20:17:03'),(11,11,14,2,3,'Aisle-A-04',NULL,'2026-11-09 20:17:03'),(12,12,16,2,3,'Aisle-B-05',NULL,'2026-11-09 20:17:03'),(13,13,5,1,2,'Aisle-C-03',NULL,'2026-11-09 20:17:03'),(14,14,22,3,4,'Aisle-A-05',NULL,'2026-11-09 20:17:03'),(15,15,9,1,2,'Aisle-A-06',NULL,'2026-11-09 20:17:03'),(16,16,15,2,3,'Aisle-C-04',NULL,'2026-11-09 20:17:03'),(17,17,12,3,3,'Aisle-B-06',NULL,'2026-11-09 20:17:03'),(18,18,20,2,4,'Aisle-B-07',NULL,'2026-11-09 20:17:03'),(19,19,28,4,5,'Aisle-B-08',NULL,'2026-11-09 20:17:03'),(20,20,7,2,2,'Aisle-C-05',NULL,'2026-11-09 20:17:03'),(21,21,50,5,10,'Aisle-D-02',NULL,'2026-11-09 20:17:03'),(22,22,35,3,6,'Aisle-D-03',NULL,'2026-11-09 20:17:03'),(23,23,25,2,4,'Aisle-B-09',NULL,'2026-11-09 20:17:03'),(24,24,8,1,2,'Aisle-C-06',NULL,'2026-11-09 20:17:03'),(25,25,18,2,3,'Aisle-C-07',NULL,'2026-11-09 20:17:03');
/*!40000 ALTER TABLE `inventories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `labstudent`
--

DROP TABLE IF EXISTS `labstudent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `labstudent` (
  `StdId` varchar(10) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `StdPassword` varchar(30) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `StdName` varchar(50) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `StdLastname` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  PRIMARY KEY (`StdId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `labstudent`
--

LOCK TABLES `labstudent` WRITE;
/*!40000 ALTER TABLE `labstudent` DISABLE KEYS */;
INSERT INTO `labstudent` VALUES ('001',NULL,'acd','bd'),('6401100','gghh','acdghj','bdfsd'),('6401105','asdf','acdghjasdgf','adrfgdfghs'),('6401106','asdfg','sfghsffgh','bd');
/*!40000 ALTER TABLE `labstudent` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `preorderitems`
--

DROP TABLE IF EXISTS `preorderitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `preorderitems` (
  `ItemId` int NOT NULL AUTO_INCREMENT,
  `PreorderId` int NOT NULL,
  `ProductId` int NOT NULL,
  `UnitPrice` decimal(10,2) NOT NULL,
  `Quantity` int NOT NULL DEFAULT '1',
  `LineTotal` decimal(10,2) GENERATED ALWAYS AS ((`UnitPrice` * `Quantity`)) STORED,
  PRIMARY KEY (`ItemId`),
  KEY `fk_poi_preorder` (`PreorderId`),
  KEY `fk_poi_product` (`ProductId`),
  CONSTRAINT `fk_poi_preorder` FOREIGN KEY (`PreorderId`) REFERENCES `preorders` (`PreorderId`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_poi_product` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_poi_price` CHECK ((`UnitPrice` >= 0)),
  CONSTRAINT `chk_poi_qty` CHECK ((`Quantity` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `preorderitems`
--

LOCK TABLES `preorderitems` WRITE;
/*!40000 ALTER TABLE `preorderitems` DISABLE KEYS */;
INSERT INTO `preorderitems` (`ItemId`, `PreorderId`, `ProductId`, `UnitPrice`, `Quantity`) VALUES (4,4,5,1850.00,1),(5,5,1,4290.00,1),(6,6,2,2450.00,1),(7,6,3,4200.00,1),(8,7,1,4290.00,1),(9,7,5,1850.00,1),(10,8,2,2450.00,1),(11,8,3,4200.00,1),(12,1,3,4200.00,1),(13,2,1,1200.00,1),(14,3,2,2450.00,1),(15,9,1,4290.00,1);
/*!40000 ALTER TABLE `preorderitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `preorders`
--

DROP TABLE IF EXISTS `preorders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `preorders` (
  `PreorderId` int NOT NULL AUTO_INCREMENT,
  `ReservationNumber` varchar(30) NOT NULL,
  `UserId` int NOT NULL,
  `ContactPhone` varchar(20) NOT NULL,
  `EstimatedPickupDate` date NOT NULL,
  `CurrentStatus` varchar(50) NOT NULL DEFAULT 'PendingConfirm',
  `TotalFinalPrice` decimal(10,2) NOT NULL DEFAULT '0.00',
  `PickupBranch` varchar(100) NOT NULL DEFAULT 'FriendHouse HQ',
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CompletedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`PreorderId`),
  UNIQUE KEY `uq_preorders_resno` (`ReservationNumber`),
  KEY `idx_preorders_phone` (`ContactPhone`),
  KEY `idx_preorders_status` (`CurrentStatus`,`EstimatedPickupDate`),
  KEY `fk_preorders_user` (`UserId`),
  CONSTRAINT `fk_preorders_user` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_po_final` CHECK ((`TotalFinalPrice` >= 0)),
  CONSTRAINT `chk_po_status` CHECK ((`CurrentStatus` in (_utf8mb4'PendingConfirm',_utf8mb4'WaitingStock',_utf8mb4'ReadyForPickup',_utf8mb4'Completed',_utf8mb4'Cancelled')))
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `preorders`
--

LOCK TABLES `preorders` WRITE;
/*!40000 ALTER TABLE `preorders` DISABLE KEYS */;
INSERT INTO `preorders` VALUES (1,'PO-2026-1001',3,'081-234-5678','2026-11-15','ReadyForPickup',4200.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 00:52:02',NULL),(2,'DB123456',3,'081-234-5678','2026-11-13','ReadyForPickup',1200.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 00:52:02',NULL),(3,'PO-000482',1,'089-111-2233','2026-11-18','ReadyForPickup',2450.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 00:52:02',NULL),(4,'PO-2026-6299',1,'081-234-5678','2026-11-15','ReadyForPickup',1850.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 20:38:14',NULL),(5,'PO-2026-9559',1,'081-234-5678','2026-11-15','ReadyForPickup',4290.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 20:38:26',NULL),(6,'PO-2026-8559',1,'0123456789','2026-11-15','Cancelled',6650.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 20:38:56',NULL),(7,'PO-2026-6495',1,'0123456789','2026-11-15','Completed',6140.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 21:10:03','2026-11-09 20:17:04'),(8,'PO-2026-1179',1,'0123456789','2026-11-15','Completed',6650.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-08 21:18:11','2026-11-08 21:31:55'),(9,'PO-2026-9586',3,'0812345678','2026-11-16','Completed',4290.00,'FriendHouse HQ สาขาสยามสแควร์','2026-11-09 01:44:51','2026-11-09 01:45:33');
/*!40000 ALTER TABLE `preorders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `ProductId` int NOT NULL AUTO_INCREMENT,
  `Sku` varchar(50) NOT NULL,
  `Barcode` varchar(50) DEFAULT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `CategoryId` int NOT NULL,
  `SeriesName` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `BrandName` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `Scale` varchar(30) DEFAULT NULL,
  `HeightCm` decimal(6,2) DEFAULT NULL,
  `Material` varchar(100) DEFAULT NULL,
  `RetailPrice` decimal(10,2) NOT NULL,
  `PreorderPrice` decimal(10,2) DEFAULT NULL,
  `ExpectedArrival` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `IsPreorder` tinyint(1) NOT NULL DEFAULT '0',
  `PreorderDeadline` datetime DEFAULT NULL,
  `ImageUrl` varchar(255) NOT NULL,
  `Model3DPath` varchar(255) DEFAULT NULL,
  `IsFeaturedHero` tinyint(1) NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`ProductId`),
  UNIQUE KEY `uq_products_sku` (`Sku`),
  UNIQUE KEY `uq_products_barcode` (`Barcode`),
  KEY `idx_products_category` (`CategoryId`,`IsActive`),
  KEY `idx_products_preorder` (`IsPreorder`,`IsActive`),
  KEY `idx_products_featured` (`IsFeaturedHero`),
  CONSTRAINT `fk_products_category` FOREIGN KEY (`CategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_products_height` CHECK (((`HeightCm` > 0) or (`HeightCm` is null))),
  CONSTRAINT `chk_products_retail` CHECK ((`RetailPrice` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (1,'FIG-NARUTO-001','885901234001','นารูโตะ อุซึมากิ โหมดคิวบิเซจ อัลติเมตสตอร์ม',1,'Naruto Shippuden','Banpresto','1/6 Scale',32.00,'PVC + Resin',4290.00,3990.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/naruto_hero_fig.png','~/media/models/naruto_kyubi_sage.glb',1,1,'2026-10-14 17:30:13'),(2,'FIG-TANJIRO-002','885901234002','ทันจิโร่ คามาโดะ ระบำเทพสุริยัน ฮิโนะคามิ',1,'Demon Slayer','Good Smile Company','Non-scale',21.00,'PVC + Clear Parts',2450.00,2200.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/tanjiro_kamado.png',NULL,0,1,'2026-11-08 00:51:12'),(3,'FIG-MIKU-003','885901234003','ฮัตสึเนะ มิกุ เมจิคัลมิไร 2024 สเปเชียล',1,'Vocaloid','Good Smile Company','1/7 Scale',24.00,'PVC + ABS',4200.00,3990.00,'ไตรมาส 4 ปี 2569 สำหรับพรีออเดอร์',1,NULL,'~/media/images/hatsune_miku_magical.png',NULL,0,1,'2026-11-08 00:51:12'),(4,'FIG-LUFFY-004','885901234004','มังกี้ ดี ลูฟี่ เกียร์ 5 คิงออฟอาร์ติสต์',2,'One Piece','Bandai Spirits','Non-scale',16.00,'PVC + ABS',950.00,890.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/luffy_gear5.png',NULL,0,1,'2026-11-08 00:51:12'),(5,'FIG-ANYA-005','885901234005','อาเนีย ฟอร์เจอร์ และ บอนด์ ชุดไปเที่ยววันหยุด',3,'Spy x Family','Megahouse','1/7 Scale',15.00,'PVC + ABS',1850.00,1690.00,'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์',1,NULL,'~/media/images/anya_forger.png',NULL,0,1,'2026-11-08 00:51:12'),(6,'FIG-NARUTO-006','885901234006','นารูโตะ อุซึมากิ ไวเบรชันสตาร์ส สเปเชียล',2,'Naruto Shippuden','Banpresto','1/8 Scale',18.00,'PVC + ABS',1200.00,1090.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/naruto_vibration_stars.png',NULL,0,1,'2026-11-08 00:51:12'),(7,'FIG-GOJO-007','885901234007','โกโจ ซาโตรุ อุบัติการณ์ชิบูย่า เนตรริคุกัน',1,'Jujutsu Kaisen','Megahouse','1/7 Scale',23.00,'PVC + ABS',3800.00,3500.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/gojo_satoru.png',NULL,0,1,'2026-11-08 00:51:12'),(8,'FIG-RAIDEN-008','885901234008','โชกุน ไรเดน อาณาจักรแห่งความสงบสุขนิรันดร์',1,'Genshin Impact','Good Smile Company','1/7 Scale',26.00,'PVC + ABS + LED Base',5690.00,5290.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/raiden_shogun.png',NULL,0,1,'2026-11-08 00:51:12'),(9,'FIG-BEAR-009','885901234009','ตุ๊กตาหมีเมฆ คลาวด์แบร์ สีฟ้าพาสเทล',3,'FriendHouse Studio','FriendHouse Original','Non-scale',28.00,'กำมะหยี่ขนนุ่มพิเศษ',590.00,520.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/cloud_bear_plush.png',NULL,0,1,'2026-11-08 00:51:12'),(10,'FIG-ZORO-010','885901234010','โรโรโนอา โซโร ท่าดาบเพลิงยมโลก เก้าดาบสังหาร',2,'One Piece','Bandai Spirits','Non-scale',20.00,'PVC + ABS',2790.00,2490.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/roronoa_zoro.png',NULL,0,1,'2026-11-08 00:51:12'),(11,'FIG-NEZUKO-011','885901234011','คามาโดะ เนซึโกะ ร่างอสูรขั้นสมบูรณ์ระเบิดโลหิต',1,'Demon Slayer','Aniplex','1/8 Scale',22.00,'PVC + Clear Effects',3450.00,3150.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/nezuko_kamado.png',NULL,0,1,'2026-11-08 00:51:12'),(12,'FIG-MEGUMI-012','885901234012','ฟุชิกุโระ เมกุมิ คาถาเงามือแปดหางมังกร',1,'Jujutsu Kaisen','Kotobukiya','1/8 Scale',19.00,'PVC + ABS',3190.00,2890.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/megumi_fushiguro.png',NULL,0,1,'2026-11-08 00:51:12'),(13,'FIG-ZONG-013','885901234013','จงหลี่ ราชาแห่งหินผาและพันธสัญญาโบราณ',4,'Genshin Impact','Apex Innovation','1/7 Scale',28.00,'PVC + Polystone + LED Base',6200.00,5800.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/zhongli_morax.png',NULL,0,1,'2026-11-08 00:51:12'),(14,'FIG-SASUKE-014','885901234014','อุจิวะ ซาสึเกะ เนตรวงแหวนกระจกเงาหมื่นบุปผาและพันปักษา',2,'Naruto Shippuden','Bandai Spirits','Non-scale',17.00,'PVC + ABS + Chidori Clear Parts',2590.00,2290.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/sasuke_uchiha.png',NULL,0,1,'2026-11-08 00:51:12'),(15,'FIG-RENBK-015','885901234015','เร็นโกคุ เคียวจูโร่ เพลิงผลาญอสูรภาคบันทึกรถไฟนิรันดร์',1,'Demon Slayer','Aniplex','1/8 Scale',25.00,'PVC + Flame Resin',4600.00,4200.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/rengoku_kyojuro.png',NULL,0,1,'2026-11-08 00:51:12'),(16,'FIG-YOR-016','885901234016','ยอร์ ฟอร์เจอร์ เจ้าหญิงหนามแห่งองค์กรการ์เดน',1,'Spy x Family','Good Smile Company','1/7 Scale',25.00,'PVC + ABS + Metal Stilettos',3250.00,2950.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/yor_forger.png',NULL,0,1,'2026-11-08 00:51:12'),(17,'FIG-MIKUS-017','885901234017','ฮัตสึเนะ มิกุ ซากุระสปริงบลอสซัม ลิมิเต็ด',4,'Vocaloid','Good Smile Company','1/7 Scale',22.00,'PVC + ABS + Sakura Petal Base',2890.00,2590.00,'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์',1,NULL,'~/media/images/hatsune_miku_sakura.png',NULL,0,1,'2026-11-08 00:51:12'),(18,'FIG-ACE-018','885901234018','โปร์ตกัส ดี เอส หมัดอัคคีเพลิงบรรลัยกัลป์',2,'One Piece','MegaHouse','Non-scale',19.00,'PVC + ABS + Fire Clear Parts',1890.00,1690.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/portgas_d_ace.png',NULL,0,1,'2026-11-08 00:51:12'),(19,'FIG-ITAD-019','885901234019','อิตาโดริ ยูจิ หมัดประกายทมิฬโคเซ็น',2,'Jujutsu Kaisen','Bandai Spirits','Non-scale',16.00,'PVC + ABS + Black Flash Effect',1450.00,1290.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/itadori_yuji.png',NULL,0,1,'2026-11-08 00:51:12'),(20,'FIG-GANYU-020','885901234020','กานอวี่ ปักษาหิมะกลีบเมฆาแห่งศาลาเยว่ไห่',1,'Genshin Impact','Apex Innovation','1/7 Scale',27.00,'PVC + ABS + Ice Lotus Base',5400.00,4990.00,'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์',1,NULL,'~/media/images/ganyu_plenilune.png',NULL,0,1,'2026-11-08 00:51:12'),(21,'FIG-CAT-021','885901234021','ตุ๊กตาแมวนำโชค คิตตี้การ์เดียน ขนนุ่มพรีเมียม',3,'FriendHouse Studio','FriendHouse Original','Non-scale',25.00,'ผ้าขนนุ่มเกรดญี่ปุ่น + กระดิ่งโลหะทอง',490.00,420.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/lucky_cat_plush.png',NULL,0,1,'2026-11-08 00:51:12'),(22,'FIG-CHOP-022','885901234022','โทนี่ โทนี่ ช็อปเปอร์ หมวกซากุระรุ่นพิเศษ',3,'One Piece','Bandai Spirits','Non-scale',22.00,'กำมะหยี่ขนละเอียด นุ่มพิเศษ',650.00,590.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/chopper_plush.png',NULL,0,1,'2026-11-08 00:51:12'),(23,'FIG-NOBARA-023','885901234023','คุกิซากิ โนบาระ คาถาหุ่นฟางตะปูอาถรรพ์',2,'Jujutsu Kaisen','Bandai Spirits','Non-scale',15.00,'PVC + ABS + Nails Effects',1350.00,1190.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/nobara_kugisaki.png',NULL,0,1,'2026-11-08 00:51:12'),(24,'FIG-HUTAO-024','885901234024','ฮูเต๋า ผีเสื้อนำทางวิญญาณแห่งโถงหวังเซิง',1,'Genshin Impact','Apex Innovation','1/7 Scale',27.00,'PVC + ABS + Spirit Base',5200.00,4790.00,'สินค้าเข้าเร็วๆ นี้ สำหรับพรีออเดอร์',1,NULL,'~/media/images/hu_tao_ghost.png',NULL,0,1,'2026-11-08 00:51:12'),(25,'FIG-LOID-025','885901234025','ลอยด์ ฟอร์เจอร์ สนธยา เจ้าหน้าที่สายลับเวสทาลิส',2,'Spy x Family','Good Smile Company','Non-scale',17.00,'PVC + ABS + Pistol Parts',2390.00,2150.00,'มีสินค้าพร้อมส่งมอบ',0,NULL,'~/media/images/loid_forger.png',NULL,0,1,'2026-11-08 00:51:12');
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `promotions`
--

DROP TABLE IF EXISTS `promotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `promotions` (
  `PromotionId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `VoucherCode` varchar(50) NOT NULL,
  `PromoType` varchar(50) NOT NULL,
  `DiscountValue` decimal(10,2) DEFAULT NULL,
  `MinimumSpend` decimal(10,2) DEFAULT '0.00',
  `StartDate` date NOT NULL,
  `EndDate` date NOT NULL,
  `ApplicableScope` varchar(50) NOT NULL DEFAULT 'all',
  `UsageLimit` int DEFAULT NULL,
  `CurrentUsageCount` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`PromotionId`),
  UNIQUE KEY `uq_promotions_voucher` (`VoucherCode`),
  KEY `idx_promotions_dates` (`StartDate`,`EndDate`),
  CONSTRAINT `chk_promo_dates` CHECK ((`EndDate` >= `StartDate`)),
  CONSTRAINT `chk_promo_discount` CHECK (((`DiscountValue` >= 0) or (`DiscountValue` is null))),
  CONSTRAINT `chk_promo_minspend` CHECK (((`MinimumSpend` >= 0) or (`MinimumSpend` is null))),
  CONSTRAINT `chk_promo_scope` CHECK ((`ApplicableScope` in (_utf8mb4'all',_utf8mb4'clearance',_utf8mb4'figures'))),
  CONSTRAINT `chk_promo_type` CHECK ((`PromoType` in (_utf8mb4'percent',_utf8mb4'fixed',_utf8mb4'bogo',_utf8mb4'bundle',_utf8mb4'double_points'))),
  CONSTRAINT `chk_promo_usage_cnt` CHECK ((`CurrentUsageCount` >= 0)),
  CONSTRAINT `chk_promo_usage_lmt` CHECK (((`UsageLimit` > 0) or (`UsageLimit` is null)))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `promotions`
--

LOCK TABLES `promotions` WRITE;
/*!40000 ALTER TABLE `promotions` DISABLE KEYS */;
INSERT INTO `promotions` VALUES (1,'โปรโมชั่นเคลียร์สต็อกต้นปี 2026','CLEAR2026','percent',20.00,1000.00,'2026-11-08','2026-12-08','clearance',100,12,1),(2,'ซื้อ 1 แถม 1 อาร์ตทอยและของสะสมพิเศษ','B1G1FREE','bogo',500.00,2000.00,'2026-11-08','2026-11-23','figures',50,5,1),(3,'ส่วนลดต้อนรับสมาชิกใหม่ลด 10%','WELCOME10','percent',10.00,500.00,'2026-11-08','2027-02-06','all',500,84,1),(4,'ส่วนลดพิเศษพรีออเดอร์ยอดนิยม 15%','PRE15','percent',15.00,3000.00,'2026-11-08','2027-01-07','all',200,38,1),(5,'ดีลพิเศษสเกลฟิกเกอร์ 1/7 ลดสูงสุด 20%','SCALE20','fixed',500.00,4000.00,'2026-11-08','2026-11-28','figures',80,19,0);
/*!40000 ALTER TABLE `promotions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `receiptitems`
--

DROP TABLE IF EXISTS `receiptitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `receiptitems` (
  `ReceiptItemId` int NOT NULL AUTO_INCREMENT,
  `ReceiptId` int NOT NULL,
  `ProductId` int DEFAULT NULL,
  `ItemDescription` varchar(200) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Quantity` int NOT NULL DEFAULT '1',
  `UnitPrice` decimal(10,2) NOT NULL,
  `LineTotal` decimal(10,2) GENERATED ALWAYS AS ((`UnitPrice` * `Quantity`)) STORED,
  PRIMARY KEY (`ReceiptItemId`),
  KEY `fk_ri_receipt` (`ReceiptId`),
  KEY `fk_ri_product` (`ProductId`),
  CONSTRAINT `fk_ri_product` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_ri_receipt` FOREIGN KEY (`ReceiptId`) REFERENCES `receipts` (`ReceiptId`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `chk_ri_price` CHECK ((`UnitPrice` >= 0)),
  CONSTRAINT `chk_ri_qty` CHECK ((`Quantity` > 0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `receiptitems`
--

LOCK TABLES `receiptitems` WRITE;
/*!40000 ALTER TABLE `receiptitems` DISABLE KEYS */;
/*!40000 ALTER TABLE `receiptitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `receipts`
--

DROP TABLE IF EXISTS `receipts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `receipts` (
  `ReceiptId` int NOT NULL AUTO_INCREMENT,
  `ReceiptNo` varchar(40) NOT NULL,
  `PreorderId` int DEFAULT NULL,
  `CustomerId` int DEFAULT NULL,
  `CustomerPhone` varchar(20) DEFAULT NULL,
  `CashierUserId` int NOT NULL,
  `SubtotalAmount` decimal(10,2) NOT NULL,
  `DiscountAmount` decimal(10,2) NOT NULL DEFAULT '0.00',
  `VatAmount` decimal(10,2) NOT NULL,
  `NetTotalAmount` decimal(10,2) NOT NULL,
  `BarcodeData` varchar(100) NOT NULL,
  `IssuedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`ReceiptId`),
  UNIQUE KEY `uq_receipts_no` (`ReceiptNo`),
  KEY `idx_receipts_issued` (`IssuedAt`),
  KEY `idx_receipts_cust` (`CustomerId`),
  KEY `fk_rcpt_preorder` (`PreorderId`),
  KEY `fk_rcpt_cashier` (`CashierUserId`),
  CONSTRAINT `fk_rcpt_cashier` FOREIGN KEY (`CashierUserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_rcpt_customer` FOREIGN KEY (`CustomerId`) REFERENCES `users` (`UserId`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_rcpt_preorder` FOREIGN KEY (`PreorderId`) REFERENCES `preorders` (`PreorderId`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `chk_rcpt_discount` CHECK ((`DiscountAmount` >= 0)),
  CONSTRAINT `chk_rcpt_net` CHECK ((`NetTotalAmount` >= 0)),
  CONSTRAINT `chk_rcpt_subtotal` CHECK ((`SubtotalAmount` >= 0)),
  CONSTRAINT `chk_rcpt_vat` CHECK ((`VatAmount` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `receipts`
--

LOCK TABLES `receipts` WRITE;
/*!40000 ALTER TABLE `receipts` DISABLE KEYS */;
INSERT INTO `receipts` VALUES (1,'RCP20261108213142',8,1,'0123456789',4,6650.00,0.00,465.50,6650.00,'PO-2026-1179','2026-11-08 21:31:43'),(2,'RCP20261108213154',8,1,'0123456789',4,6650.00,0.00,465.50,6650.00,'PO-2026-1179','2026-11-08 21:31:55'),(3,'RCP20261109014533',9,3,'0812345678',4,4290.00,0.00,300.30,4290.00,'PO-2026-9586','2026-11-09 01:45:33'),(4,'RCP20261109201703',7,1,'081-234-5678',4,6140.00,0.00,429.80,6140.00,'PO-2026-6495','2026-11-09 20:17:04');
/*!40000 ALTER TABLE `receipts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rewardredemptions`
--

DROP TABLE IF EXISTS `rewardredemptions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rewardredemptions` (
  `RedemptionId` bigint NOT NULL AUTO_INCREMENT,
  `RedemptionCode` varchar(40) NOT NULL,
  `UserId` int NOT NULL,
  `RewardId` int NOT NULL,
  `PointsSpent` int NOT NULL,
  `Status` varchar(30) NOT NULL DEFAULT 'PendingPickup',
  `RedeemedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CompletedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`RedemptionId`),
  UNIQUE KEY `uq_redemptions_code` (`RedemptionCode`),
  KEY `idx_redemptions_user` (`UserId`),
  KEY `fk_rdm_reward` (`RewardId`),
  CONSTRAINT `fk_rdm_reward` FOREIGN KEY (`RewardId`) REFERENCES `rewards` (`RewardId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_rdm_user` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `chk_rdm_points` CHECK ((`PointsSpent` > 0)),
  CONSTRAINT `chk_rdm_status` CHECK ((`Status` in (_utf8mb4'PendingPickup',_utf8mb4'Completed',_utf8mb4'Cancelled')))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rewardredemptions`
--

LOCK TABLES `rewardredemptions` WRITE;
/*!40000 ALTER TABLE `rewardredemptions` DISABLE KEYS */;
INSERT INTO `rewardredemptions` VALUES (1,'RWD-166742',1,1,350,'Completed','2026-11-09 20:19:03','2026-11-09 20:19:13');
/*!40000 ALTER TABLE `rewardredemptions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rewards`
--

DROP TABLE IF EXISTS `rewards`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rewards` (
  `RewardId` int NOT NULL AUTO_INCREMENT,
  `RewardCode` varchar(30) NOT NULL,
  `Title` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Brand` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `CategoryTag` varchar(50) NOT NULL DEFAULT 'Merchandise',
  `PointsRequired` int NOT NULL,
  `StockQuantity` int NOT NULL DEFAULT '0',
  `ImageUrl` varchar(255) NOT NULL,
  `IsAvailable` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`RewardId`),
  UNIQUE KEY `uq_rewards_code` (`RewardCode`),
  CONSTRAINT `chk_rwd_points` CHECK ((`PointsRequired` > 0)),
  CONSTRAINT `chk_rwd_stock` CHECK ((`StockQuantity` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rewards`
--

LOCK TABLES `rewards` WRITE;
/*!40000 ALTER TABLE `rewards` DISABLE KEYS */;
INSERT INTO `rewards` VALUES (1,'RWD-KEYCHAIN-01','พวงกุญแจอะคริลิก ดาบพิฆาตอสูร รุ่นพิเศษ','Banpresto','Merchandise',350,50,'~/media/images/tanjiro_kamado.png',1,'2026-11-09 02:31:33'),(2,'RWD-POSTER-02','โปสเตอร์ผ้าวันพีซ ฟิล์มเรด ลิขสิทธิ์แท้','Toei Animation','Merchandise',600,30,'~/media/images/luffy_gear5.png',1,'2026-11-09 02:31:33'),(3,'RWD-MUG-03','แก้วมัคเซรามิก ร้านบ้านเพื่อน รุ่นพิเศษ 2026','FriendHouse Studio','Exclusive',850,25,'~/media/images/cloud_bear_plush.png',1,'2026-11-09 02:31:33'),(4,'RWD-NEZUKO-04','มินิฟิกเกอร์ เนซึโกะ กล่องไม้ ลิมิเต็ด','Good Smile Company','Figures',1800,15,'~/media/images/nezuko_kamado.png',1,'2026-11-09 02:31:33');
/*!40000 ALTER TABLE `rewards` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `systemlogs`
--

DROP TABLE IF EXISTS `systemlogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `systemlogs` (
  `LogId` bigint NOT NULL AUTO_INCREMENT,
  `LogCode` varchar(30) NOT NULL,
  `LogLevel` varchar(20) NOT NULL,
  `Module` varchar(100) NOT NULL,
  `Message` varchar(500) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `DetailPayload` longtext,
  `IpAddress` varchar(50) DEFAULT NULL,
  `UserId` int DEFAULT NULL,
  `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`LogId`),
  UNIQUE KEY `uq_logs_code` (`LogCode`),
  KEY `idx_logs_level` (`LogLevel`,`Timestamp` DESC),
  KEY `idx_logs_module` (`Module`),
  KEY `fk_logs_user` (`UserId`),
  CONSTRAINT `fk_logs_user` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `chk_logs_level` CHECK ((`LogLevel` in (_utf8mb4'Info',_utf8mb4'Warning',_utf8mb4'Error',_utf8mb4'Critical')))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `systemlogs`
--

LOCK TABLES `systemlogs` WRITE;
/*!40000 ALTER TABLE `systemlogs` DISABLE KEYS */;
INSERT INTO `systemlogs` VALUES (1,'RESTOCK-20261109014032','Info','Inventory','รับสินค้า นารูโตะ อุซึมากิ โหมดคิวบิเซจ — Ultimate Ninja Storm (SKU: FIG-NARUTO-001) จำนวน +10 ชิ้น เข้าสต็อก คงเหลือรวม 25 ชิ้น',NULL,NULL,1,'2026-11-09 01:40:33'),(2,'RWD-DELIVER-20261109201913','Info','CounterPreorder','ส่งมอบของรางวัลรหัส #RWD-166742 (พวงกุญแจอะคริลิก ดาบพิฆาตอสูร Special Ver.) ให้ลูกค้า สมเกียรติ สถิตมั่น เรียบร้อยแล้ว',NULL,NULL,4,'2026-11-09 20:19:13'),(3,'NOSHOW-20261109201925','Warning','CounterPreorder','ลูกค้าไม่มารับสินค้า ใบจอง #PO-2026-8559 สาเหตุ: ไม่มารับสินค้าตามกำหนด | หมายเหตุ: โทรติดต่อไม่ได้เกิน 3 วัน',NULL,NULL,4,'2026-11-09 20:19:26');
/*!40000 ALTER TABLE `systemlogs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `UserId` int NOT NULL AUTO_INCREMENT,
  `UserCode` varchar(30) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `Email` varchar(150) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `Phone` varchar(20) NOT NULL,
  `FullName` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `AvatarUrl` varchar(255) DEFAULT NULL,
  `RoleName` varchar(30) NOT NULL DEFAULT 'Customer',
  `Points` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `uq_users_usercode` (`UserCode`),
  UNIQUE KEY `uq_users_email` (`Email`),
  KEY `idx_users_phone` (`Phone`),
  KEY `idx_users_role_active` (`RoleName`,`IsActive`),
  CONSTRAINT `chk_users_points` CHECK ((`Points` >= 0)),
  CONSTRAINT `chk_users_role` CHECK ((`RoleName` in (_utf8mb4'Customer',_utf8mb4'Staff',_utf8mb4'Owner',_utf8mb4'ITSupport',_utf8mb4'Accountant')))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'U20261014132600','gg@gmail.com','$2a$11$CSmSneAmr/mqK6UtUXJ0reeIO2katN3Va/omKwWhUTdXnJQt98Xx.','0123456789','สมเกียรติ สถิตมั่น','','customer',980,1,'2026-10-14 06:26:01'),(2,'U20261014140358','ggg@gmail.com','$2a$11$Rje5NBcpC1lqKiwEhKKuL.oje1HLBEoQkEQQsoM3aLkS7x.BZ8FuO','0123456788','สมหญิง มั่งมี','','customer',0,1,'2026-10-14 07:03:59'),(3,'U20261014001','customer@friendhouse.com','$2a$11$uxVOjb9ddTlW7b4lRQijyOPOi1POGZxTPJmBMAfFXGZVXkJzbHmUK','081-234-5678','ลูกค้าทั่วไป สมาชิกหลัก',NULL,'Customer',1814,1,'2026-10-14 17:30:14'),(4,'U20261014002','staff@friendhouse.com','$2a$11$uxVOjb9ddTlW7b4lRQijyOPOi1POGZxTPJmBMAfFXGZVXkJzbHmUK','081-234-5678','พนักงานประจำเคาน์เตอร์',NULL,'Staff',1200,1,'2026-10-14 17:30:14'),(5,'U20261014003','owner@friendhouse.com','$2a$11$uxVOjb9ddTlW7b4lRQijyOPOi1POGZxTPJmBMAfFXGZVXkJzbHmUK','081-234-5678','ผู้จัดการร้าน FriendHouse',NULL,'Owner',1200,1,'2026-10-14 17:30:14'),(6,'U20261014004','it@friendhouse.com','$2a$11$uxVOjb9ddTlW7b4lRQijyOPOi1POGZxTPJmBMAfFXGZVXkJzbHmUK','081-234-5678','เจ้าหน้าที่ฝ่ายไอที',NULL,'ITSupport',1200,1,'2026-10-14 17:30:14'),(7,'U20261014005','account@friendhouse.com','$2a$11$uxVOjb9ddTlW7b4lRQijyOPOi1POGZxTPJmBMAfFXGZVXkJzbHmUK','081-234-5678','เจ้าหน้าที่การเงินบัญชี',NULL,'Accountant',1200,1,'2026-10-14 17:30:14');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-09 20:40:44
