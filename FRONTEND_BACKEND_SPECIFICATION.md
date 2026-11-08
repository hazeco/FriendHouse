# เอกสารข้อกำหนดการเชื่อมโยงระบบ Frontend และ Backend (FriendHouse)
## FRONTEND TO BACKEND SPECIFICATION & VIEWMODEL CONTRACTS

เอกสารฉบับนี้จัดทำขึ้นเพื่อเป็นคู่มือมาตรฐานสำหรับนักพัฒนาฝั่ง **Backend / Full-Stack** ในการเชื่อมโยง Business Logic, Database Access, API Services และ Data Binding เข้ากับส่วนติดต่อผู้ใช้งาน (Frontend Razor Views `.cshtml`) ของระบบร้านฟิกเกอร์และโมเดล **FriendHouse**

---

## สารบัญระบบตามบทบาทผู้ใช้งาน (User Roles)
1. [กลุ่มที่ 1: บัญชีและสมาชิกลูกค้า (Account & Customer)](#1-กลุ่มบัญชีและสมาชิกลูกค้า-account--customer)
2. [กลุ่มที่ 2: หน้าร้านและการสั่งจองพรีออเดอร์ (Storefront & Pre-order)](#2-กลุ่มหน้าร้านและการสั่งจองพรีออเดอร์-storefront--pre-order)
3. [กลุ่มที่ 3: พนักงานหน้าร้าน (Staff Console)](#3-กลุ่มพนักงานหน้าร้าน-staff-console)
4. [กลุ่มที่ 4: เจ้าของร้าน (Owner Portal)](#4-กลุ่มเจ้าของร้าน-owner-portal)
5. [กลุ่มที่ 5: ผู้ดูแลระบบ (IT Support Portal)](#5-กลุ่มผู้ดูแลระบบ-it-support-portal)
6. [กลุ่มที่ 6: คนทำบัญชีและการเงิน (Accountant Portal)](#6-กลุ่มคนทำบัญชีและการเงิน-accountant-portal)

---

## 1. กลุ่มบัญชีและสมาชิกลูกค้า (Account & Customer)

### 1.1 หน้าเข้าสู่ระบบ (Login)
- **View File**: `Views/Account/Login.cshtml`
- **Controller**: `AccountController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public IActionResult Login() => View();
  ```
- **Action (POST)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
  ```
- **ViewModel Definition**:
  ```csharp
  namespace Khetsophon_CSI402_8456.ViewModels;

  public class LoginViewModel
  {
      [Required(ErrorMessage = "กรุณากรอกอีเมล")]
      [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
      public string Email { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
      [DataType(DataType.Password)]
      public string Password { get; set; } = string.Empty;

      public bool RememberMe { get; set; } = false;
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Email: string, Password: string, RememberMe: bool]`
- **สิ่งที่ Controller ต้องจัดการ**:
  - ตรวจสอบความถูกต้องของบัญชีผู้ใช้ในระบบ Identity / Database
  - สร้าง ClaimsPrincipal และลงทะเบียน Authentication Cookie
  - นำทาง (Redirect) ไปยังหน้าร้าน `Home/Index` หรือ Dashboard ตามสิทธิ์ (Role)

---

### 1.2 หน้าสมัครสมาชิกใหม่ (Register)
- **View File**: `Views/Account/Register.cshtml`
- **Controller**: `AccountController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public IActionResult Register() => View();
  ```
- **Action (POST)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> Register(RegisterViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  namespace Khetsophon_CSI402_8456.ViewModels;

  public class RegisterViewModel
  {
      [Required(ErrorMessage = "กรุณากรอกอีเมล")]
      [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
      public string Email { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
      [Phone(ErrorMessage = "เบอร์โทรศัพท์ไม่ถูกต้อง")]
      public string Phone { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
      [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 8 ตัวอักษร")]
      [DataType(DataType.Password)]
      public string Password { get; set; } = string.Empty;

      [DataType(DataType.Password)]
      [Compare("Password", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกัน")]
      public string ConfirmPassword { get; set; } = string.Empty;
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Email: string, Phone: string, Password: string, ConfirmPassword: string]`
- **สิ่งที่ Controller ต้องจัดการ**:
  - ตรวจสอบความซ้ำซ้อนของอีเมลและเบอร์โทรศัพท์
  - แฮชรหัสผ่าน (Password Hashing: BCrypt หรือ PBKDF2)
  - สร้างบัญชี Role `Customer` และตั้งต้นแต้มสะสมเริ่มต้น 0 แต้ม

---

### 1.3 หน้าเปลี่ยน / รีเซ็ตรหัสผ่าน (Reset Password)
- **View File**: `Views/Account/ResetPassword.cshtml`
- **Controller**: `AccountController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public IActionResult ResetPassword() => View();
  ```
- **Action (POST)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  public class ResetPasswordViewModel
  {
      [Required]
      [DataType(DataType.Password)]
      public string CurrentPassword { get; set; } = string.Empty;

      [Required]
      [StringLength(100, MinimumLength = 8)]
      [DataType(DataType.Password)]
      public string NewPassword { get; set; } = string.Empty;

      [Compare("NewPassword")]
      public string ConfirmNewPassword { get; set; } = string.Empty;
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[CurrentPassword: string, NewPassword: string, ConfirmNewPassword: string]`

---

### 1.4 หน้าโปรไฟล์ผู้ใช้ (Profile)
- **View File**: `Views/Account/Profile.cshtml`
- **Controller**: `AccountController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Profile()
  ```
- **Action (POST - Edit Profile)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> EditProfile(UserProfileViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  public class UserProfileViewModel
  {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string Phone { get; set; } = string.Empty;
      public string Role { get; set; } = "Customer";
      public int RewardPoints { get; set; }
      public int PreorderActiveCount { get; set; }
      public int CompletedOrdersCount { get; set; }
      public string? AvatarUrl { get; set; }
      public IFormFile? AvatarFile { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[FullName: string, Email: string, Phone: string, AvatarFile: IFormFile (optional)]`

---

## 2. กลุ่มหน้าร้านและการสั่งจองพรีออเดอร์ (Storefront & Pre-order)

### 2.1 หน้าแรกของร้าน (Home Storefront)
- **View File**: `Views/Home/Index.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Index()
  ```
- **ViewModel Definition**:
  ```csharp
  public class StorefrontViewModel
  {
      public ProductItemDto FeaturedHeroProduct { get; set; } = new();
      public List<PromotionDto> ActivePromotions { get; set; } = new();
      public List<ProductItemDto> RecommendedFigures { get; set; } = new();
      public List<ProductItemDto> PreorderHighlights { get; set; } = new();
  }
  ```
- **สิ่งที่ Controller ต้องจัดการ**:
  - ดึงข้อมูลสินค้าพรีออเดอร์เด่น (ที่มีไฟล์ 3D `.glb` หรือภาพ 360)
  - ดึงโปรโมชั่นที่เปิดใช้งานอยู่ 5 รูปแบบ

---

### 2.2 หน้าคอลเลกชันสินค้าทั้งหมด (Catalog Collection)
- **View File**: `Views/Home/Collection.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Collection(string? series, string? category, string? status, string? search, int page = 1)
  ```
- **พารามิเตอร์ Query Strings**:
  - `[series: string?, category: string?, status: string?, search: string?, page: int]`

---

### 2.3 หน้ารายละเอียดสินค้าและโมดอลยืนยันการจอง (Product Detail & Preorder Modal)
- **View File**: `Views/Home/ProductDetail.cshtml`
- **Controller**: `HomeController`
- **Action (GET - รายละเอียดสินค้า)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> ProductDetail(int id)
  ```
- **Action (POST - ยืนยันการสั่งจองพรีออเดอร์จากโมดอล)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ConfirmPreorder(PreorderBookingDto booking)
  ```
- **ViewModel Definition**:
  ```csharp
  public class PreorderBookingDto
  {
      [Required]
      public int ProductId { get; set; }

      [Required]
      [Range(1, 10)]
      public int Quantity { get; set; } = 1;

      [Required(ErrorMessage = "กรุณาระบุวันที่สะดวกเข้ารับ")]
      public DateTime PickupDate { get; set; }

      [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์ติดต่อ")]
      public string ContactPhone { get; set; } = string.Empty;

      public string? Note { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์มโมดอล**:
  - `[ProductId: int, Quantity: int, PickupDate: DateTime, ContactPhone: string]`
- **สิ่งที่ Controller ต้องจัดการ**:
  - บันทึกคำสั่งจองเข้าตาราง `Preorders`
  - สร้างรหัสอ้างอิงใบจอง เช่น `#PO-000482`
  - Redirect ไปยัง `Home/PreorderStatus` พร้อมส่งเลขที่ใบจอง

---

### 2.4 หน้าตะกร้าสินค้า (Cart)
- **View File**: `Views/Home/Cart.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Cart()
  ```
- **Action (POST - ยืนยันตะกร้าเข้าสู่คิวพรีออเดอร์)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> CheckoutCart(List<int> selectedCartItemIds)
  ```

---

### 2.5 หน้าตรวจสอบสถานะพรีออเดอร์ของฉัน (Preorder Status)
- **View File**: `Views/Home/PreorderStatus.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> PreorderStatus(string? orderNo)
  ```
- **พารามิเตอร์ Query String**:
  - `[orderNo: string (เช่น PO-000482)]`

---

### 2.6 หน้าแคตตาล็อกแลกแต้มรางวัล (Rewards Catalog)
- **View File**: `Views/Home/Rewards.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Rewards()
  ```
- **Action (POST - ยืนยันการแลกรางวัล)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> RedeemReward(int rewardId)
  ```

---

### 2.7 หน้าโปรโมชั่นทั้งหมด (Promotions Storefront)
- **View File**: `Views/Home/Promotion.cshtml`
- **Controller**: `HomeController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Promotion()
  ```

---

## 3. กลุ่มพนักงานหน้าร้าน (Staff Console)

### 3.1 หน้ารายการสต็อกสินค้าคงเหลือ (Stock Inventory Index)
- **View File**: `Views/Staff/Index.cshtml`
- **Controller**: `StaffController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Index(string? searchQuery, string? category, string? stockStatus)
  ```
- **พารามิเตอร์ Query Strings / Filter Form**:
  - `[searchQuery: string?, category: string?, stockStatus: string?]`
- **ViewModel Definition**:
  ```csharp
  public class StockInventoryViewModel
  {
      public List<StockItemDto> Items { get; set; } = new();
      public int TotalCount { get; set; }
      public int LowStockCount { get; set; }
      public int OutOfStockCount { get; set; }
  }

  public class StockItemDto
  {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Sku { get; set; } = string.Empty;
      public string Category { get; set; } = string.Empty;
      public int CurrentQuantity { get; set; }
      public int SafetyThreshold { get; set; } = 5;
      public string Status { get; set; } = "ปกติ"; // ปกติ, ใกล้หมด, หมด
      public string StorageLocation { get; set; } = string.Empty;
      public decimal RetailPrice { get; set; }
      public DateTime LastReceivedDate { get; set; }
  }
  ```

---

### 3.2 หน้าสแกนรับสินค้าเข้าสต็อก (Scan Receive)
- **View File**: `Views/Staff/ScanReceive.cshtml`
- **Controller**: `StaffController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public IActionResult ScanReceive() => View();
  ```
- **Action (POST - รับเข้าผ่าน Manual หรือ Scanner)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ScanReceive(ScanReceiveInputDto input)
  ```
- **ViewModel Definition**:
  ```csharp
  public class ScanReceiveInputDto
  {
      [Required(ErrorMessage = "กรุณาระบุรหัสบาร์โค้ดหรือ SKU")]
      public string Sku { get; set; } = string.Empty;

      [Required]
      [Range(1, 1000, ErrorMessage = "จำนวนรับเข้าต้องมากกว่า 0")]
      public int Quantity { get; set; } = 1;

      public string? BatchNumber { get; set; }
      public string? Note { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Sku: string, Quantity: int]`

---

### 3.3 หน้าเคาน์เตอร์พรีออเดอร์หน้าร้าน (Pre-order Pickup Counter)
- **View File**: `Views/Staff/CounterPreorder.cshtml`
- **Controller**: `StaffController`
- **Action (GET / POST)**:
  ```csharp
  [HttpGet]
  public IActionResult CounterPreorder() => View();

  [HttpPost]
  public async Task<IActionResult> CounterPreorder(CounterPreorderViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  namespace Khetsophon_CSI402_8456.ViewModels;

  public class CounterPreorderViewModel
  {
      [Required(ErrorMessage = "กรุณาระบุเบอร์โทรหรือเลขที่ใบจอง")]
      public string ReservationNumber { get; set; } = string.Empty;

      public string? Phone { get; set; }
      public string? RedemptionNumber { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[ReservationNumber: string (เบอร์โทรศัพท์ของลูกค้า หรือรหัสใบจอง เช่น 081-234-5000 หรือ PO-000482)]`
- **สิ่งที่ Controller ต้องจัดการ**:
  - ค้นหาคำสั่งพรีออเดอร์ที่สถานะ `พร้อมรับสินค้าที่ร้าน`
  - คำนวณยอดเงินมัดจำที่ชำระแล้ว และยอดส่วนที่เหลือที่ต้องชำระหน้าร้าน
  - บันทึกการส่งมอบสินค้าและพิมพ์ใบเสร็จรับเงิน

---

## 4. กลุ่มเจ้าของร้าน (Owner Portal)

### 4.1 หน้าจัดการโปรโมชั่น (Promotions Index)
- **View File**: `Views/Owner/Promotions.cshtml`
- **Controller**: `OwnerController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Promotions()
  ```
- **Action (POST - Toggle Status)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> TogglePromotionStatus(int id, bool isActive)
  ```
- **พารามิเตอร์**:
  - `[id: int, isActive: bool]`

---

### 4.2 หน้าสร้างโปรโมชั่นใหม่ (Add Promotion)
- **View File**: `Views/Owner/AddPromotion.cshtml`
- **Controller**: `OwnerController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public IActionResult AddPromotion() => View();
  ```
- **Action (POST)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> AddPromotion(CreatePromotionViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  public class CreatePromotionViewModel
  {
      [Required(ErrorMessage = "กรุณาระบุชื่อโปรโมชั่น")]
      public string Name { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณาระบุรหัสโค้ดโปรโมชั่น")]
      public string Code { get; set; } = string.Empty;

      [Required]
      public string PromoType { get; set; } = "percent"; // percent, fixed, bogo, bundle, double_points

      public decimal? DiscountValue { get; set; }
      public decimal? MinSpend { get; set; }

      [Required]
      public DateTime StartDate { get; set; }

      [Required]
      public DateTime EndDate { get; set; }

      public string ApplicableScope { get; set; } = "all"; // all, clearance, scale_figures, preorder_only
      public int? UsageLimit { get; set; }
      public bool IsActive { get; set; } = true;
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Name: string, Code: string, PromoType: string, DiscountValue: decimal?, MinSpend: decimal?, StartDate: DateTime, EndDate: DateTime, ApplicableScope: string, UsageLimit: int?, IsActive: bool]`

---

### 4.3 หน้าแก้ไขโปรโมชั่น (Edit Promotion)
- **View File**: `Views/Owner/EditPromotion.cshtml`
- **Controller**: `OwnerController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> EditPromotion(int id)
  ```
- **Action (POST)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> EditPromotion(EditPromotionViewModel model)
  ```
- **ViewModel Definition**:
  ```csharp
  public class EditPromotionViewModel : CreatePromotionViewModel
  {
      [Required]
      public int Id { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Id: int, Name: string, Code: string, PromoType: string, DiscountValue: decimal?, MinSpend: decimal?, StartDate: DateTime, EndDate: DateTime, ApplicableScope: string, UsageLimit: int?, IsActive: bool]`

---

### 4.4 หน้ารายงานสรุปยอดประจำเดือน (Monthly Report)
- **View File**: `Views/Owner/MonthlyReport.cshtml`
- **Controller**: `OwnerController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> MonthlyReport(string? month, string? quarter)
  ```
- **Action (POST / FileResult - Export Report)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ExportReport(string exportScope, string exportFormat, string period)
  ```
- **พารามิเตอร์**:
  - `[exportScope: string (month/quarter), exportFormat: string (excel/pdf), period: string]`

---

## 5. กลุ่มผู้ดูแลระบบ (IT Support Portal)

### 5.1 หน้าภาพรวมสถานะระบบ (Dashboard)
- **View File**: `Views/ITSupport/Dashboard.cshtml`
- **Controller**: `ITSupportController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Dashboard()
  ```
- **ViewModel Definition**:
  ```csharp
  public class SystemOverviewViewModel
  {
      public string ServerStatus { get; set; } = "Online";
      public int LatencyMs { get; set; } = 118;
      public double UptimePercentage { get; set; } = 99.98;
      public int ActiveDbConnections { get; set; } = 24;
      public double StorageUsedGb { get; set; } = 4.2;
      public double StorageTotalGb { get; set; } = 50.0;
      public List<SystemLogDto> RecentCriticalLogs { get; set; } = new();
  }
  ```

---

### 5.2 หน้าจัดการบัญชีผู้ใช้งาน (Users Management)
- **View File**: `Views/ITSupport/Users.cshtml`
- **Controller**: `ITSupportController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Users(string? searchQuery, string? role, string? status, int page = 1)
  ```
- **Action (POST - Create User)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> CreateUser(CreateUserDto dto)
  ```
- **Action (POST - Edit User)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> EditUser(EditUserDto dto)
  ```
- **Action (POST - Reset Password)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> HelpResetPassword(int userId)
  ```
- **Action (POST - Delete User)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> DeleteUser(int userId)
  ```
- **Action (POST - Export CSV)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ExportUsersCsv(string timeRange)
  ```
- **ViewModel Definition**:
  ```csharp
  public class CreateUserDto
  {
      [Required]
      public string Role { get; set; } = "customer"; // customer, staff, owner, itsupport, accountant

      public string? FullName { get; set; } // สำหรับ role ที่ไม่ใช่ customer

      [Required]
      [EmailAddress]
      public string Email { get; set; } = string.Empty;

      [Required]
      [Phone]
      public string Phone { get; set; } = string.Empty;

      [Required]
      [MinLength(8)]
      public string Password { get; set; } = string.Empty;

      public IFormFile? AvatarFile { get; set; }
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - Create: `[Role: string, FullName: string?, Email: string, Phone: string, Password: string, AvatarFile: IFormFile?]`
  - Edit: `[Id: int, FullName: string?, Email: string, Phone: string, Role: string]`
  - Delete: `[Id: int]`

---

### 5.3 หน้าบันทึกกิจกรรมและข้อผิดพลาด (System Logs)
- **View File**: `Views/ITSupport/Logs.cshtml`
- **Controller**: `ITSupportController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Logs(string? level, string? timeRange, string? search, int page = 1)
  ```
- **Action (POST - Export CSV)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ExportLogsCsv(string timeRange, List<string> levels)
  ```
- **พารามิเตอร์**:
  - `[level: string (all, Error, Warning, Info), timeRange: string, search: string, page: int]`

---

## 6. กลุ่มคนทำบัญชีและการเงิน (Accountant Portal)

### 6.1 หน้าแดชบอร์ดการเงิน (Financial Dashboard)
- **View File**: `Views/Accountant/Index.cshtml`
- **Controller**: `AccountantController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Index(string? month)
  ```
- **ViewModel Definition**:
  ```csharp
  public class FinancialDashboardViewModel
  {
      public decimal TotalMonthlyRevenue { get; set; }
      public decimal StorefrontRevenue { get; set; }
      public decimal PreorderDepositPending { get; set; }
      public decimal RecordedExpenses { get; set; }
      public decimal EstimatedGrossMargin => TotalMonthlyRevenue - RecordedExpenses;
      public double PromptPaySharePercent { get; set; } = 65.0;
      public double CashSharePercent { get; set; } = 35.0;
      public List<RecentTransactionDto> RecentSales { get; set; } = new();
  }
  ```

---

### 6.2 หน้าบันทึกรายรับ-รายจ่าย (Ledger)
- **View File**: `Views/Accountant/Ledger.cshtml`
- **Controller**: `AccountantController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Ledger(string? month, string? type, string? search)
  ```
- **Action (POST - บันทึกรายการใหม่ / แก้ไข)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> SaveLedgerEntry(LedgerEntryInputDto model)
  ```
- **Action (POST - ลบรายการ)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> DeleteLedgerEntry(int id)
  ```
- **ViewModel Definition**:
  ```csharp
  public class LedgerEntryInputDto
  {
      public int? Id { get; set; }

      [Required]
      public string Type { get; set; } = "expense"; // expense หรือ income

      [Required(ErrorMessage = "กรุณาเลือกหมวดหมู่")]
      public string Category { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณากรอกรายละเอียด")]
      public string Description { get; set; } = string.Empty;

      [Required(ErrorMessage = "กรุณาระบุจำนวนเงิน")]
      [Range(0.01, 10000000, ErrorMessage = "จำนวนเงินต้องมากกว่า 0")]
      public decimal Amount { get; set; }

      [Required]
      public DateTime Date { get; set; } = DateTime.Today;
  }
  ```
- **พารามิเตอร์ที่ส่งจากฟอร์ม**:
  - `[Id: int?, Type: string, Category: string, Description: string, Amount: decimal, Date: DateTime]`

---

### 6.3 หน้าประวัติใบเสร็จและธุรกรรม (Receipts History & Preview)
- **View File**: `Views/Accountant/Receipts.cshtml`
- **Controller**: `AccountantController`
- **Action (GET)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> Receipts(string? search, DateTime? fromDate, DateTime? toDate, int page = 1)
  ```
- **Action (POST - Export Excel)**:
  ```csharp
  [HttpPost]
  public async Task<IActionResult> ExportReceiptsExcel(string timeRange)
  ```
- **Action (GET - ข้อมูลใบเสร็จสำหรับพิมพ์ / พรีวิว)**:
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetReceiptDetail(string receiptNo)
  ```
- **ViewModel Definition**:
  ```csharp
  public class ReceiptDetailDto
  {
      public string ReceiptNo { get; set; } = string.Empty;
      public DateTime DateTime { get; set; }
      public string CustomerName { get; set; } = string.Empty;
      public string CustomerPhone { get; set; } = string.Empty;
      public string StaffName { get; set; } = string.Empty;
      public string PaymentMethod { get; set; } = string.Empty;
      public List<ReceiptItemLineDto> Items { get; set; } = new();
      public decimal Subtotal { get; set; }
      public decimal VatAmount { get; set; }
      public decimal TotalAmount { get; set; }
  }
  ```

---

## สรุปหลักการความปลอดภัยและการจัดการข้อผิดพลาด (Security & Exception Rules)
1. **การป้องกัน CSRF (Cross-Site Request Forgery)**: ทุกการส่งฟอร์มแบบ `POST` แนะนำให้ประดับแอตทริบิวต์ `[ValidateAntiForgeryToken]` ที่ระดับ Action method ฝั่ง Controller
2. **การผูกสิทธิ์ตามบทบาท (Role-based Authorization)**:
   - สมาชิกทั่วไป: `[Authorize(Roles = "Customer")]`
   - พนักงาน: `[Authorize(Roles = "Staff,Owner")]`
   - เจ้าของร้าน: `[Authorize(Roles = "Owner")]`
   - ไอทีซัพพอร์ต: `[Authorize(Roles = "ITSupport")]`
   - คนทำบัญชี: `[Authorize(Roles = "Accountant,Owner")]`
3. **การจัดการ Session / Token หมดอายุ**: หากผู้ใช้ไม่มีสิทธิ์หรือ Session หลุด ระบบจะนำทางกลับไปยัง `Account/Login` พร้อมพารามิเตอร์ `returnUrl`

---

## 7. รายการเส้นทางระบบ (MVC Endpoints / Route Catalog) ทั้งหมดในระบบ (34 Actions)

ในระบบ ASP.NET Core MVC เส้นทางการทำงาน (Endpoints) แต่ละเส้นจะถูกกำหนดเป็น **Controller Action Methods** ซึ่งทำหน้าที่รับ Request จาก Client, ดำเนินการ CRUD บนฐานข้อมูล, และส่งต่อ Model ไปยัง View หรือส่ง JSON Response กลับมา

### สรุปจำนวนเส้นทางตาม Controller:
- **HomeController**: 13 เส้นทาง (Storefront, Hero 3D, Catalog, Cart CRUD, Preorder)
- **AccountController**: 8 เส้นทาง (Auth, Fast Login, Profile Update, Password Change)
- **StaffController**: 3 เส้นทาง (Stock Inventory, Counter Preorder, Scan Receive)
- **OwnerController**: 5 เส้นทาง (Promotions CRUD, Quick Toggle, Monthly Report)
- **ITSupportController**: 6 เส้นทาง (Dashboard Health, Users CRUD, Reset Password, Logs)
- **AccountantController**: 5 เส้นทาง (Financial Dashboard, Ledger CRUD, Receipts History)
- **รวมทั้งสิ้น: 34 เส้นทาง (Action Endpoints)**

---

### รายละเอียดเส้นทางราย Controller และเหตุผลในการออกแบบ (WHY over WHAT)

#### 1. HomeController (13 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 1 | `/Home/Index` | `GET` | **Read (R)** | - | `View(Product heroProduct)` | ดึง Hero Product จาก DB มาแสดงผลพร้อมโมเดล 3D หากไม่มีจะ Fallback ใช้นารูโตะ เพื่อให้หน้าแรกไม่พังและดึงดูดลูกค้า |
| 2 | `/Home/Collection` | `GET` | **Read (R)** | - | `View(List<Product>)` | ดึงสินค้าที่เปิดขายอยู่จริงเพื่อนำมาแสดงในหน้าแคตตาล็อกรวม |
| 3 | `/Home/Promotion` | `GET` | **Read (R)** | - | `View(List<Promotion>)` | ดึงโปรโมชั่นที่เปิดใช้งานจริงมาให้ลูกค้าเลือกดู |
| 4 | `/Home/ProductDetail/{id?}` | `GET` | **Read (R)** | `int? id` | `View(Product)` | นำ ID สินค้ามา Query ข้อมูลสเปกและสต็อกคงเหลือจริงเพื่อแสดงรายละเอียดก่อนสั่งจอง |
| 5 | `/Home/Cart` | `GET` | **Read (R)** | - | `View(List<Cartitem>)` | ดึงรายการสินค้าในตะกร้าของสมาชิกที่ล็อกอินอยู่จากตาราง `Cartitems` |
| 6 | `/Home/AddToCart` | `POST` | **Create (C)** | `int productId, int quantity` | `Json({success, message})` | บันทึกสินค้าลงตะกร้าหรือบวกจำนวนเพิ่ม หากเป็นสินค้าเดิม เพื่อประสบการณ์ใช้งานที่ลื่นไหลแบบ AJAX |
| 7 | `/Home/UpdateCartQuantity` | `POST` | **Update (U)** | `int cartItemId, int quantity` | `Json({success, quantity})` | อัปเดตจำนวนสินค้าในตะกร้าทันที หรือลบทิ้งหากยอดเป็น 0 |
| 8 | `/Home/RemoveFromCart` | `POST` | **Delete (D)** | `int cartItemId` | `Json({success, message})` | ลบสินค้าออกจากตะกร้าแบบ Hard Delete เพราะเป็นเพียงข้อมูลตะกร้าชั่วคราว |
| 9 | `/Home/ClearCart` | `POST` | **Delete (D)** | - | `Json({success})` | ล้างสินค้าทั้งหมดในตะกร้าของ User ในคราวเดียว |
| 10 | `/Home/ConfirmPreorder` | `POST` | **Create (C)** | `string? contactPhone, string? pickupBranch` | `RedirectToAction("PreorderStatus")` | สร้างใบจองพรีออเดอร์ (`Preorders` + `Preorderitems`), ตัดยอดจองในสต็อก (`ReservedQuantity`), และเคลียร์ตะกร้าสินค้า |
| 11 | `/Home/PreorderStatus` | `GET` | **Read (R)** | - | `View(List<Preorder>)` | ดึงสถานะใบจองของ User ปัจจุบันเพื่อให้ลูกค้าติดตามสถานะการจัดส่ง/รับสินค้า |
| 12 | `/Home/Rewards` | `GET` | **Read (R)** | - | `View()` | แสดงรายการของรางวัลสำหรับแต้มสะสม |
| 13 | `/Home/Customer` | `GET` | **Read (R)** | - | `View("Index")` | เส้นทางทางลัดสำหรับสลับบทบาทลูกค้ากลับสู่หน้าแรก |

---

#### 2. AccountController (8 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 14 | `/Account/Login` | `GET / POST` | **Read (R)** | `LoginViewModel model` | `View()` หรือ `Redirect` | ตรวจสอบอีเมลและรหัสผ่านด้วย BCrypt ป้องกัน Credential Leak และจดจำ Session |
| 15 | `/Account/FastLogin` | `POST` | **Create (C)** | `string role` | `Json({success, redirectUrl})` | ให้กรรมการ/อาจารย์ทดสอบระบบได้ทันทีโดยไม่ต้องจำรหัสผ่าน พร้อมจำลองสิทธิ์ใน Session |
| 16 | `/Account/Register` | `GET / POST` | **Create (C)** | `RegisterViewModel model` | `View()` หรือ `Redirect` | สมัครสมาชิกใหม่ แฮชรหัสผ่านด้วย BCrypt และกำหนด Role เริ่มต้นเป็น Customer |
| 17 | `/Account/Logout` | `GET / POST` | **Delete (D)** | - | `RedirectToAction("Login")` | เคลียร์ Session ทั้งหมดออกจากระบบเพื่อความปลอดภัย |
| 18 | `/Account/Profile` | `GET` | **Read (R)** | - | `View(User)` | ดึงข้อมูลส่วนตัว, แต้มสะสม, และประวัติของผู้ใช้ที่ล็อกอินอยู่ |
| 19 | `/Account/UpdateProfile` | `POST` | **Update (U)** | `string fullName, string phone` | `Json({success, message})` | อัปเดตข้อมูลติดต่อของตนเองเพื่อรับ SMS แจ้งเตือนสินค้า |
| 20 | `/Account/ChangePassword` | `POST` | **Update (U)** | `string oldPassword, string newPassword` | `Json({success, message})` | ตรวจสอบรหัสผ่านเดิมก่อนอนุญาตให้เปลี่ยนรหัสผ่านใหม่ ป้องกันการโดนขโมยเซสชัน |
| 21 | `/Account/ResetPassword` | `GET` | **Read (R)** | - | `View()` | หน้าสำหรับให้ผู้ใช้ขอรีเซ็ตรหัสผ่าน |

---

#### 3. StaffController (3 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 22 | `/Staff/Index` | `GET` | **Read (R)** | `string? search, string? category` | `View(List<Product>)` | ตรวจสอบสต็อกคงเหลือหน้าร้านและยอดที่ถูกจองไว้แบบ Real-time |
| 23 | `/Staff/CounterPreorder` | `GET / POST` | **Read (R) / Update (U)** | `CounterPreorderViewModel model, string? actionType, string? cancelReason` | `View(Preorder)` | ค้นหาใบจองจากเบอร์/รหัสใบจอง และอัปเดตสถานะเป็น "Fulfilled" (รับของแล้ว) หรือ "Cancelled" (ยกเลิก) พร้อมบันทึกสาเหตุลง Log |
| 24 | `/Staff/ScanReceive` | `GET / POST` | **Read (R) / Update (U)** | `string? sku, int? quantity` | `View()` | จำลองการสแกนบาร์โค้ดรับสินค้าเข้าคลังจริง เพิ่มจำนวนใน `Inventories.QuantityOnHand` |

---

#### 4. OwnerController (5 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 25 | `/Owner/Promotions` | `GET` | **Read (R)** | - | `View(List<Promotion>)` | เจ้าของร้านดูภาพรวมโปรโมชั่นทั้งหมดที่กำลังรันอยู่ |
| 26 | `/Owner/AddPromotion` | `GET / POST` | **Create (C)** | `Promotion data` | `View()` หรือ `Redirect` | สร้างโปรโมชั่นส่วนลดใหม่ บันทึกลงตาราง `Promotions` |
| 27 | `/Owner/EditPromotion/{id?}` | `GET / POST` | **Read (R) / Update (U)** | `int? id, Promotion data` | `View()` หรือ `Redirect` | แก้ไขรายละเอียด เงื่อนไข และระยะเวลาของโปรโมชั่นที่มีอยู่ |
| 28 | `/Owner/TogglePromotion` | `POST` | **Update (U)** | `int id` | `Json({success, isActive})` | สลับเปิด/ปิดโปรโมชั่นหน้าร้านทันทีด้วย AJAX โดยไม่ต้องรีโหลดหน้า |
| 29 | `/Owner/DeletePromotion` | `POST` | **Delete (D - Soft)** | `int id` | `Json({success, message})` | ทำ Soft Delete (`IsActive = false`) เพื่อไม่ให้กระทบ Foreign Key ของคำสั่งซื้อเก่า |
| 30 | `/Owner/MonthlyReport` | `GET` | **Read (R)** | `int? year, int? month` | `View(MonthlyReportViewModel)` | สรุปผลประกอบการ ยอดขาย และสถานะสินค้าคงคลังรายเดือน |

---

#### 5. ITSupportController (6 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 31 | `/ITSupport/Dashboard` | `GET` | **Read (R)** | - | `View()` | มอนิเตอร์สุขภาพเซิร์ฟเวอร์ การเชื่อมต่อ Database และ Uptime |
| 32 | `/ITSupport/Users` | `GET` | **Read (R)** | `string? role, string? search` | `View(List<User>)` | ตรวจสอบรายชื่อผู้ใช้งานและบทบาททั้งหมดในระบบ |
| 33 | `/ITSupport/AddUser` | `POST` | **Create (C)** | `string fullName, string email, string phone, string role, string password` | `Json({success, message})` | สร้างบัญชีพนักงาน/ผู้ใช้ใหม่พร้อมกำหนดสิทธิ์และแฮชรหัสผ่าน |
| 34 | `/ITSupport/EditUser` | `POST` | **Update (U)** | `string email, string? phone, string? role, string? status, string? fullName` | `Json({success, message})` | แก้ไขข้อมูลสิทธิ์ เบอร์โทร หรือสถานะบัญชีผู้ใช้งาน |
| 35 | `/ITSupport/ResetPassword` | `POST` | **Update (U)** | `int userId` | `Json({success, tempPassword})` | ช่วยเหลือผู้ใช้ที่ลืมรหัสผ่านโดยสุ่มรหัสผ่านชั่วคราวและบันทึกประวัติการกระทำลงใน Log |
| 36 | `/ITSupport/DeleteUser` | `POST` | **Delete (D - Soft)** | `string email` | `Json({success, message})` | ระงับการใช้งานบัญชี (`IsActive = false`) ห้ามลบจริงเพื่อรักษา Audit Trail และความสมบูรณ์ของใบเสร็จ |
| 37 | `/ITSupport/Logs` | `GET` | **Read (R)** | `string? level, string? search` | `View(List<Systemlog>)` | ตรวจสอบประวัติข้อผิดพลาดและการกระทำสำคัญในระบบ (Audit Logs) |

---

#### 6. AccountantController (5 เส้นทาง)
| ลำดับ | เส้นทาง (Route / URL) | HTTP Method | ประเภท CRUD | สิ่งที่ส่งมา (Input Parameters) | สิ่งที่ส่งกลับ (Output) | เหตุผลในการออกแบบ (WHY) |
| :---: | :--- | :---: | :---: | :--- | :--- | :--- |
| 38 | `/Accountant/Index` | `GET` | **Read (R)** | - | `View()` | แดชบอร์ดสรุปรายรับ-รายจ่าย ยอดรวม และเงินสดหมุนเวียน |
| 39 | `/Accountant/Ledger` | `GET / POST` | **Read (R) / Create (C)** | `Financialledger entry` | `View(List<Financialledger>)` | บันทึกรายการค่าใช้จ่ายใหม่ เช่น ค่าเช่า ค่าน้ำไฟ พร้อมแสดงบัญชีย้อนหลัง |
| 40 | `/Accountant/EditLedgerEntry` | `POST` | **Update (U)** | `int ledgerId, string? categoryName, string? entryType, decimal? amount, DateOnly? entryDate, string? note` | `Json({success, message})` | แก้ไขตัวเลขหรือหมวดหมู่บัญชีที่ลงบันทึกผิดพลาด |
| 41 | `/Accountant/DeleteLedgerEntry` | `POST` | **Delete (D - Hard)** | `int ledgerId` | `Json({success, message})` | ลบรายการบัญชีที่ผิดพลาดออกจากตาราง `Financialledgers` |
| 42 | `/Accountant/Receipts` | `GET` | **Read (R)** | `string? search, int? month` | `View(List<Receipt>)` | ดูประวัติใบเสร็จรับเงินที่ออกให้ลูกค้าหน้าร้านทั้งหมด |

---

## 8. การวิเคราะห์จุดต่างระหว่าง UI และฐานข้อมูล (Database & Data Dictionary Gap Analysis)

> [!IMPORTANT]
> รายการต่อไปนี้เป็นสิ่งที่หน้าจอ **UI มีการแสดงผลหรือมี Modal รองรับ** แต่ใน **Database Schema / Data Dictionary ปัจจุบันยังไม่มีคอลัมน์รองรับโดยตรง** (ทางเราได้ออกแบบวิธีชดเชยการทำงานไว้ชั่วคราวแล้วโดยไม่แตะโครงสร้าง DB):

1. **ระบบเงินมัดจำ (Deposit / Balance Due)**:
   - **UI**: มีข้อความและช่องแสดง "เงินมัดจำ", "ยอดค้างชำระหน้าร้าน"
   - **Database**: ตาราง `Preorders` มีเพียงฟิลด์ `TotalFinalPrice` (ยอดเต็ม) ไม่มีฟิลด์ `DepositAmount` หรือ `BalanceDue`
   - **วิธีชดเชยปัจจุบัน**: คำนวณยอดเต็มและแสดงผลเป็นยอดที่ต้องชำระเมื่อมารับของหน้าร้าน

2. **สาเหตุการยกเลิก / ไม่รับสินค้า (Cancellation Reason)**:
   - **UI**: เมื่อพนักงานกด "ไม่รับสินค้า" จะมี Modal ให้กรอกสาเหตุที่ไม่รับ
   - **Database**: ตาราง `Preorders` มีเพียงคอลัมน์ `CurrentStatus` ('Pending', 'Fulfilled', 'Cancelled') ไม่มีคอลัมน์ `CancellationReason` หรือ `CancelNote`
   - **วิธีชดเชยปัจจุบัน**: บันทึกสาเหตุการยกเลิกลงในตาราง `Systemlogs` ในคอลัมน์ `Details` พร้อมอ้างอิงเลขที่ใบจอง เพื่อคงประวัติไว้ตรวจสอบได้

3. **คำบรรยายสินค้าแบบยาว (Product Description)**:
   - **UI**: หน้ารายละเอียดสินค้ามีคำบรรยายสินค้ายาวๆ (Description)
   - **Database**: ตาราง `Products` มีฟิลด์ `SeriesName`, `BrandName`, `Scale`, `Material`, `ExpectedArrival` แต่ไม่มีคอลัมน์ `Description`
   - **วิธีชดเชยปัจจุบัน**: รวมข้อมูลจาก `SeriesName`, `BrandName`, `Scale`, `Material` มาสร้างเป็นข้อความบรรยายอัตโนมัติ

4. **ประวัติการแลกของรางวัลด้วยแต้มสะสม (Points Redemption History)**:
   - **UI**: หน้า Rewards มีการแลกของรางวัลด้วยแต้มสะสม
   - **Database**: ตาราง `Users` มีคอลัมน์ `Points` สำหรับเก็บแต้มคงเหลือ แต่ยังไม่มีตาราง `PointsRedemptions` หรือ `RewardsCatalog` ใน Schema
   - **วิธีชดเชยปัจจุบัน**: ใช้ Mock Catalog ของรางวัลใน View และหักลบแต้มจาก `Users.Points`

5. **เงื่อนไขโปรโมชั่น (Promotion Terms & Conditions)**:
   - **UI**: หน้าโปรโมชั่นมีช่องแสดงเงื่อนไขและข้อตกลงการใช้สิทธิ์
   - **Database**: ตาราง `Promotions` มี `Name`, `DiscountPercentage`, `DiscountAmount`, `StartDate`, `EndDate`, `IsActive` แต่ไม่มีคอลัมน์ `Terms` หรือ `Description`
   - **วิธีชดเชยปัจจุบัน**: แสดงข้อความเงื่อนไขมาตรฐานของร้านที่ส่วนบนของ View

