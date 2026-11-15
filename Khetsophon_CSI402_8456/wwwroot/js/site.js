// อ้างอิงปุ่มเปิดเมนูมือถือและกล่องเมนูมือถือจาก DOM
const navToggle = document.getElementById("nav-toggle");
// อ้างอิงกล่องเมนูนำทางสำหรับหน้าจอมือถือ
const mobileMenu = document.getElementById("mobile-menu");

// ผูกเหตุการณ์คลิกปุ่มแฮมเบอร์เกอร์เพื่อสลับการแสดงเมนูบนมือถือ
navToggle?.addEventListener("click", () => {
    // ตรวจสอบสถานะการเปิดเมนูปัจจุบันจากแอตทริบิวต์ aria-expanded
    const isOpen = navToggle.getAttribute("aria-expanded") === "true";

    // สลับค่าสถานะ aria-expanded
    navToggle.setAttribute("aria-expanded", String(!isOpen));
    // สลับคลาส hidden เพื่อซ่อนหรือแสดงเมนูมือถือ
    mobileMenu?.classList.toggle("hidden");
// สิ้นสุดเหตุการณ์คลิกปุ่มแฮมเบอร์เกอร์
});

// ระบบซิงค์เซสชันข้ามแท็บตามมาตรฐาน 1 เบราว์เซอร์ 1 บทบาท (เมื่ออีกแท็บล็อกอินเปลี่ยนบทบาทหรือออกจากระบบ ให้บังคับรีเฟรชแท็บเดิมไปยังบทบาทใหม่ทันที)
(function initSingleRoleBrowserSync() {
    // อ่านข้อมูลบทบาทและรหัสผู้ใช้ที่เซิร์ฟเวอร์เรนเดอร์ไว้บนแท็ก body
    const bodyEl = document.body;
    // หากไม่พบแท็ก body ให้จบการทำงาน
    if (!bodyEl) return;

    // อ่านค่าบทบาทปัจจุบันของแท็บนี้จากแอตทริบิวต์ data-session-role
    const tabRole = (bodyEl.getAttribute("data-session-role") || "guest").toLowerCase();
    // อ่านค่ารหัสผู้ใช้ปัจจุบันของแท็บนี้จากแอตทริบิวต์ data-session-uid
    const tabUid = bodyEl.getAttribute("data-session-uid") || "0";
    // กำหนดชื่อคีย์ใน localStorage สำหรับซิงค์สถานะระหว่างแท็บ
    const STORAGE_KEY = "fh_active_browser_session";

    // ฟังก์ชันคำนวณ URL หน้าหลักตามบทบาทที่เปลี่ยนไป
    function resolveRoleHomeUrl(role) {
        // แปลงชื่อบทบาทเป็นตัวพิมพ์เล็ก
        const r = (role || "guest").toLowerCase();
        // กรณีพนักงานหน้าร้าน
        if (r === "staff") return "/Staff/Index";
        // กรณีเจ้าของร้าน
        if (r === "owner") return "/Owner/Promotions";
        // กรณีฝ่ายบัญชีและการเงิน
        if (r === "accountant") return "/Accountant/Index";
        // กรณีผู้ดูแลระบบไอที
        if (r === "itsupport") return "/ITSupport/Dashboard";
        // กรณีลูกค้าหรือผู้เยี่ยมชมทั่วไป
        return "/";
    // สิ้นสุดฟังก์ชัน resolveRoleHomeUrl
    }

    // บันทึกสถานะเซสชันล่าสุดจากเซิร์ฟเวอร์ลงใน localStorage เพื่อให้แท็บอื่นรับทราบทันที
    try {
        // สร้างเพย์โหลด JSON เก็บบทบาทและรหัสผู้ใช้งานปัจจุบัน
        const payload = JSON.stringify({ role: tabRole, uid: tabUid });
        // บันทึกลง localStorage (จะกระตุ้นเหตุการณ์ storage ในแท็บอื่นที่เปิดอยู่ทันที)
        localStorage.setItem(STORAGE_KEY, payload);
    // ดักจับข้อผิดพลาดในกรณีที่เบราว์เซอร์ไม่อนุญาตให้เข้าถึง localStorage
    } catch (e) {
        // ข้ามข้อผิดพลาดเมื่อเบราว์เซอร์ปิดกั้นพื้นที่จัดเก็บข้อมูลโลคัล
    }

    // ฟังก์ชันตรวจสอบว่าเซสชันใน localStorage เปลี่ยนไปจากแท็บปัจจุบันหรือไม่
    function checkAndSyncTabRole(rawJson) {
        // หากไม่มีข้อมูล JSON ให้จบการทำงาน
        if (!rawJson) return;
        // เริ่มต้นบล็อกตรวจสอบและแปลงข้อมูลเซสชัน
        try {
            // แปลงข้อมูล JSON เป็นออบเจกต์
            const latest = JSON.parse(rawJson);
            // หากไม่มีออบเจกต์ให้จบการทำงาน
            if (!latest) return;
            // อ่านบทบาทล่าสุดและรหัสผู้ใช้ล่าสุด
            const latestRole = (latest.role || "guest").toLowerCase();
            // อ่านรหัสผู้ใช้ล่าสุดเป็นสตริง
            const latestUid = String(latest.uid || "0");

            // หากบทบาทหรือรหัสผู้ใช้ในเบราว์เซอร์เปลี่ยนไปจากตอนที่แท็บนี้โหลด ให้บังคับเปลี่ยนหน้าไปยังบทบาทใหม่ทันที
            if (latestRole !== tabRole || latestUid !== tabUid) {
                // คำนวณ URL ปลายทางของบทบาทใหม่
                const targetUrl = resolveRoleHomeUrl(latestRole);
                // สั่งเปลี่ยนหน้าไปยังหน้าหลักของบทบาทใหม่ทันที
                window.location.replace(targetUrl);
            // สิ้นสุดเงื่อนไขตรวจสอบการเปลี่ยนบทบาท
            }
        // ดักจับข้อผิดพลาดกรณีโครงสร้าง JSON ไม่ถูกต้อง
        } catch (err) {
            // ข้ามข้อผิดพลาดเมื่อการแปลงข้อมูล JSON ล้มเหลว
        }
    // สิ้นสุดฟังก์ชัน checkAndSyncTabRole
    }

    // ดักจับเหตุการณ์เมื่อแท็บอื่นในเบราว์เซอร์เดียวกันล็อกอินเปลี่ยนบทบาทหรือออกจากระบบ
    window.addEventListener("storage", function (event) {
        // ตรวจสอบว่าเป็นคีย์เซสชันของระบบหรือไม่
        if (event.key === STORAGE_KEY && event.newValue) {
            // บังคับซิงค์แท็บนี้ให้ตรงกับบทบาทใหม่ทันที
            checkAndSyncTabRole(event.newValue);
        // สิ้นสุดเงื่อนไขตรวจสอบคีย์เซสชัน
        }
    // สิ้นสุดการรับฟังเหตุการณ์ storage
    });

    // ดักจับเหตุการณ์เมื่อผู้ใช้สลับกลับมาที่แท็บนี้หรือกดย้อนกลับ (Back/Forward Cache)
    window.addEventListener("focus", function () {
        // เริ่มต้นบล็อกตรวจสอบเซสชันเมื่อโฟกัสกลับมาที่แท็บ
        try {
            // ตรวจสอบค่าล่าสุดใน localStorage
            checkAndSyncTabRole(localStorage.getItem(STORAGE_KEY));
        // ดักจับข้อผิดพลาดกรณีไม่สามารถอ่านค่าใน localStorage ได้
        } catch (e) {
            // ข้ามข้อผิดพลาดในบล็อก focus
        }
    // สิ้นสุดการรับฟังเหตุการณ์ focus
    });

    // ดักจับเหตุการณ์เมื่อแท็บเปลี่ยนสถานะการมองเห็นเป็นแสดงผล
    document.addEventListener("visibilitychange", function () {
        // ตรวจสอบสถานะการมองเห็นของเอกสารว่ากำลังแสดงผลอยู่หรือไม่
        if (document.visibilityState === "visible") {
            // เริ่มต้นบล็อกตรวจสอบเซสชันเมื่อแท็บกลับมามองเห็นได้
            try {
                // ตรวจสอบค่าล่าสุดใน localStorage
                checkAndSyncTabRole(localStorage.getItem(STORAGE_KEY));
            // ดักจับข้อผิดพลาดกรณีไม่สามารถอ่านค่าใน localStorage ได้
            } catch (e) {
                // ข้ามข้อผิดพลาดในบล็อก visibilitychange
            }
        // สิ้นสุดเงื่อนไขตรวจสอบสถานะการมองเห็น
        }
    // สิ้นสุดการรับฟังเหตุการณ์ visibilitychange
    });
// สิ้นสุดฟังก์ชันควบคุมเซสชันบทบาทเดียวบนเบราว์เซอร์
})();
