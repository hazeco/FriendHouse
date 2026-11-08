This is a single self-contained fix; keep it small and focused. Update the member points modal in the staff counter preorder page so that when staff clicks "มารับแล้ว / ชำระเงินแล้ว", the member phone input automatically fills with the actual contact phone of the current preorder instead of the hardcoded phone number "081-234-5678".

Working directory: d:\code\CSI402\Khetsophon_CSI402_8456
Integrity mode: development

## Requirements

### R1. Dynamic Contact Phone Auto-fill
In `Views/Staff/CounterPreorder.cshtml`, clicking "มารับแล้ว / ชำระเงินแล้ว" must open the member points modal (`#memberPointsModal`) with the phone input (`#memberPhoneInput`) automatically prefilled with the active preorder's contact phone. If the preorder has no direct contact phone (`ContactPhone`), fall back to the registered user's phone (`User.Phone`). If neither exists, leave the input empty.

### R2. Removal of Hardcoded Mock Phone Number
The hardcoded value `"081-234-5678"` in the modal's phone input and related opening logic must be removed so it no longer overrides or masks real preorder data.

### R3. Preservation of Form Submission & Validation Flow
Preserve the existing modal functionality: confirming points submission (`/Staff/CompletePickup`), skipping points accumulation, warning displays for unregistered member phones, and modal transitions to the success screen.

### R4. Project Constraints & Architectural Integrity
Changes must strictly adhere to the project rules: frontend views and scripts only, without modifying backend database queries, business logic, or controller actions. Maintain Tailwind CSS styling, responsive layout, and ensure no emojis are added.

## Acceptance Criteria

### Modal Pre-fill Behavior
- [ ] When viewing a preorder with a contact phone (e.g. `0123456789`), clicking "มารับแล้ว / ชำระเงินแล้ว" opens `#memberPointsModal` with `#memberPhoneInput` holding that exact contact phone.
- [ ] If the preorder contact phone is empty or null, `#memberPhoneInput` falls back to the registered user's phone number (`User.Phone`).
- [ ] If neither contact phone nor user phone exists, `#memberPhoneInput` is empty.
- [ ] The hardcoded string "081-234-5678" is not prefilled when viewing an active preorder.

### Functional Flow
- [ ] Clicking "ยืนยันและสะสมแต้ม" submits the autofilled or user-edited phone number to `/Staff/CompletePickup` and proceeds to success modal or not-found warning as expected.
- [ ] Clicking "ไม่ได้เป็นสมาชิก / ข้าม" completes pickup without points as expected.

### Build & Integrity Check
- [ ] The ASP.NET Core solution builds with 0 errors via `dotnet build`.