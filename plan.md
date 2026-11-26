# Kế Hoạch Mô Phỏng Drone Trong Môi Trường Thành Phố (Urban UAV & Wind Simulation)

**Mục tiêu:** Mô phỏng Drone và hệ thống gió khí động học trong môi trường đô thị (`SimulateMain.cs`, `Drone.cs`).

**Phiên bản:** v3 — 2026-11-26 (chuẩn hóa bố cục, sửa các tham chiếu chéo, bổ sung giải thích thuật ngữ cho dễ đọc)

---

## 0. Quy ước làm việc (bắt buộc)

| Vai trò | Ai thực hiện | Phạm vi được phép |
|---|---|---|
| Viết / sửa code `.cs` | **Người dùng** | Tự viết, tự sửa để học và thực hành |
| Review code, chỉ lỗi công thức / vật lý / API | **Agent** | Chỉ đọc và nhận xét, **không tự sửa code hộ** |
| Sửa file tài liệu (`.md`) | **Agent** | Chỉ sửa khi đã báo trước và được người dùng đồng ý |

- Đánh dấu tiến độ bằng `- [ ]` / `- [x]` ngay trong file này.
- Chỉ tick `[x]` khi task đó **đã chạy thử và đạt đúng tiêu chí kiểm chứng**.
- Làm tuần tự từng Phase: Phase hiện tại phải chạy được, không có lỗi Console mới chuyển sang Phase tiếp theo.

---

## 1. Số liệu nền của Scene (Baseline — dùng để hiệu chuẩn)

Số liệu đo trực tiếp từ `Assets/1.Scenes/Prototype 1.unity` và `ProjectSettings/`.

### 1.1. Môi trường vật lý

| Thông số | Giá trị | Nguồn |
|---|---|---|
| Phiên bản Unity | 6000.6.8f1 (URP) | `ProjectVersion.txt` |
| Trọng lực (Gravity) | 9.81 m/s² | `DynamicsManager.asset` |
| Fixed Timestep | 0.02 s (50 Hz) | `TimeManager.asset` |
| Layer 6 | `Drone` | `TagManager.asset` |
| Layer 7 | `Wall` (khớp với `_obstacleLayer` = bitmask **128**) | `TagManager.asset`, scene |
| Collision matrix | Toàn bộ đang bật (mọi cặp layer đều va chạm) | `DynamicsManager.asset` |

### 1.2. Địa hình & Tòa nhà

- **Mặt đất (`Cube`):** tâm `(966.6, 0, -164.5)`, kích thước `5134.7 × 1 × 3372.7`, **mặt trên ở `y = 0.5`**. Đang ở layer `Default` (0) → *không* nằm trong `_obstacleLayer`.
- **12 tòa nhà** (`tower`, `tower (1)` … `tower (11)`): là object con của `Cube`, đang ở layer **7 (`Wall`)**.
- **Kích thước mỗi tòa:** rộng trục X ≈ `415.8`, cao `H` từ **`81.4` → `295.8`**, dài trục Z từ **`270.3` → `1475.8`**.
- **Độ cao nóc nhà:** cao nhất **`y ≈ 295.8`**, thấp nhất **`y ≈ 81.4`**.
- **Độ rộng khe phố (trục X):** khoảng trống giữa 2 dãy nhà là **`454 – 504` unit**.

### 1.3. Thông số Drone

| Thông số | Giá trị trên Inspector (Scene) | Giá trị thực tế khi Play |
|---|---|---|
| Vị trí ban đầu | `(1304, 23.8, -1295)` | — |
| Scale | `51.1` đều cả 3 trục | — |
| SphereCollider radius | `0.5` (local) | **25.5 unit** (thế giới) → đường kính ~51 |
| Rigidbody mass | `1` | **1.5 kg** (bị database ghi đè trong `Awake`) |
| Constraints | `None (0)` | **FreezeRotation** (bị `Drone.Awake` gán lại) |
| `_objectId` | `drone` | Tra database → id `Drone` |
| `Cd` / Diện tích `A` | `5` / `2` | **1.0 / 0.06 m²** (bị database ghi đè) |
| `_objectForce` | `1` | **14.715 N** (bị database ghi đè) |
| `_moveForce` | `34.4` | **25.0** (bị database ghi đè) |

> **Nguồn ghi đè:** `Assets/Resources/SimulateObjectDatabase.json` → mục `Drone`: `Cd = 1.0`, `force = 14.715`, `moveForce = 25.0`, `area = 0.06`, `mass = 1.5`.

### 1.4. Thông số Gió (`Simulate Controller`)

| Thông số | Giá trị |
|---|---|
| Vị trí | `(0, 350, 0)`, xoay X = 1° → `transform.forward ≈ (0, -0.0175, 0.9998)` |
| `_windSpeed` | 8.14 m/s |
| `_windCheckDistance` | 61.3 unit |
| `_obstacleLayer` | 128 (`Wall`) |

> ⚠️ **Lưu ý:** `_windDirection` luôn bị code ghi đè bằng `transform.forward` trong `FixedUpdate` và `OnDrawGizmos`. Vì vậy, chỉnh `_windDirection` trên Inspector sẽ **không có tác dụng** — muốn đổi hướng gió phải xoay Transform của object `Simulate Controller`.

### 1.5. Các mốc lực để đối chiếu khi test

| Trường hợp | Công thức tính | Kết quả |
|---|---|---|
| Trọng lượng Drone | `m · g = 1.5 × 9.81` | **14.72 N** |
| Lực cản gió thường (8.14 m/s) | `0.5 · ρ · v² · Cd · A = 0.5 × 1.225 × 66.3 × 1.0 × 0.06` | **≈ 2.44 N** (~16.6% trọng lượng) |
| Lực cản gió Venturi ×2.5 (20.35 m/s) | `0.5 · ρ · v² · Cd · A = 0.5 × 1.225 × 414 × 1.0 × 0.06` | **≈ 15.2 N** (~103% trọng lượng) |

→ **Ý nghĩa thiết kế:** Gió thường không đủ làm lật drone, nhưng **gió lùa qua khe nhà (Venturi) thì đủ**. Đây là ngưỡng hợp lý để tạo cảm giác "phố yên ổn, ngã tư nguy hiểm".

---

## 2. Giải thích nhanh các thuật ngữ trong kế hoạch

| Thuật ngữ | Giải thích ngắn gọn |
|---|---|
| **Wind Shear** | Gió thổi càng mạnh khi lên càng cao (do ít bị mặt đất và nhà cửa cản). |
| **Hiệu ứng Venturi** | Khi gió lùa qua khe hẹp giữa hai tòa nhà, tiết diện thu hẹp làm tốc độ gió tăng vọt. |
| **Wake Turbulence** | Vùng gió xoáy, nhiễu loạn phía sau vật cản (sau lưng tòa nhà) thay vì đứng yên hoàn toàn. |
| **A_eff (Diện tích hiệu dụng)** | Diện tích bề mặt thực tế của drone đang hướng thẳng vào luồng gió (thay đổi khi drone nghiêng). |
| **PID (P - I - D)** | Bộ điều khiển tự động cân bằng gồm 3 khâu: **P** (phản ứng theo độ lệch hiện tại), **I** (bù sai số tích lũy lâu dài), **D** (hãm bớt để khỏi vọt lố / rung lắc). |
| **Integral Windup** | Lỗi khâu **I** tích lũy quá lớn khi drone bị kẹt lâu, làm drone giật mạnh lúc thoát ra; khắc phục bằng cách giới hạn trần (`clamp`). |
| **Derivative Kick** | Lỗi khâu **D** giật đột ngột khi đổi góc mục tiêu; khắc phục bằng cách tính đạo hàm trên *giá trị đo được* thay vì *sai số*. |
| **Yaw-relative tilt** | Nghiêng tiến/lùi/trái/phải tính theo hướng mũi của drone đang quay, thay vì theo trục cố định của thế giới. |
| **Downwash** | Luồng khí từ cánh quạt thổi mạnh xuống phía dưới, đẩy các vật nhẹ xung quanh. |
| **Ground Effect** | Khi drone bay sát mặt đất, luồng khí bị nén lại giúp tăng lực nâng lên một chút. |

---

## 3. Các vấn đề thiết kế cần xử lý trước khi code

Các mục dưới đây là lỗi logic hoặc xung đột được phát hiện khi đối chiếu bản thiết kế cũ với scene thực tế:

### 3.1. `_windCheckDistance = 61.3` quá nhỏ so với scene
Mặt tòa nhà gần drone nhất cách khoảng **1417 unit** (`tower (5)` ở `z = 122`, drone ở `z = -1295`). Với `61.3`, hàm `IsBlockFromWind()` gần như **không bao giờ trả về `true`**, khiến phần gió xoáy sau tòa nhà mất tác dụng.
- **Cách sửa:** Tăng `_windCheckDistance` lên khoảng **[250, 500]** (xấp xỉ nửa bề rộng khe phố).

### 3.2. Công thức `A_eff` cũ bị sai khi bay ngang
Nếu tính `A_eff = A · dot(transform.up, relativeVelocity.normalized)`, khi drone bay bằng và gió thổi ngang thì `dot(up, relDir) = 0` → **`A_eff = 0` → mất hoàn toàn lực cản gió**, dù gió đang thổi thẳng vào hông drone.
- **Mô hình đúng (hình chiếu hộp 3 trục):**
  ```
  n     = hướng gió tương đối (đã chuẩn hóa)
  A_eff = |dot(n, up)|      · A_top
        + |dot(n, forward)| · A_front
        + |dot(n, right)|   · A_right
  ```
- Nếu coi drone đối xứng (`A_top = A_front = A_right = A_0`) hoặc chỉ muốn 1 tham số: `A_eff = A_0 · (α + (1 − α) · |dot(n, up)|)` với `α ≈ 0.6` để `A_eff` không bao giờ bằng 0 khi có gió.

### 3.3. Công thức Wind Shear cũ bị chạm trần quá sớm
Công thức cũ `Mathf.Clamp(1f + y * 0.05f, 1f, 3f)` khiến drone ở `y = 23.8` đã có hệ số `2.19`, và lên `y = 40` đã chạm trần `3.0` — toàn bộ khoảng bay phía trên không còn sự chênh lệch theo độ cao.
- **Cách sửa:** Chuẩn hóa theo chiều cao tòa nhà cao nhất (`H_ref ≈ 296`):
  ```
  k_shear = Mathf.Clamp(1f + gain * (y / H_ref), 1f, k_max)   // gợi ý: gain ≈ 1.0, k_max ≈ 2.2
  ```
  *(Hoặc dùng công thức lũy thừa chuẩn khí tượng cho đô thị: `k = (y / H_ref)^α` với `α ≈ 0.14 – 0.25`.)*

### 3.4. Xung đột `FreezeRotation` với `AddTorque`
`Drone.Awake` đang gán `RigidbodyConstraints.FreezeRotation`, nên mọi lệnh `_rb.AddTorque(...)` sẽ bị vô hiệu hóa hoàn toàn. Cần chọn **1 trong 2 hướng**:

| Hướng | Cách xoay Drone | PID xuất ra giá trị gì | Đặc điểm |
|---|---|---|---|
| **A. Kinematic-style** *(đang dùng)* | `MoveRotation` | Góc nghiêng mục tiêu | Đơn giản, dễ ổn định, không có quán tính xoay |
| **B. Dynamic** *(vật lý thật)* | `AddTorque` | Mô-men xoắn (N·m) | Chân thực hơn, tự lắc lư khi gặp gió nhưng khó cân chỉnh |

> **Đề xuất:** Làm theo **Hướng A** trước cho khớp với code hiện tại; chỉ chuyển sang **B** nếu muốn mô phỏng cảm giác drone tự lảo đảo vật lý thật.

### 3.5. Singleton `SimulateObject.Instance` sẽ gây lỗi khi thêm vật thể thứ 2
Hiện tại `SimulateObject.Instance` là biến `static` dùng chung, `Drone` lại che đi bằng `new static Instance`, còn `SimulateMain` chỉ lấy `_target = SimulateObject.Instance` và cache `_rb` đúng **1 lần trong `Start`**. Khi thêm vật thể thứ 2 (như khối hộp để test Downwash ở Phase 4), gió sẽ đẩy nhầm đối tượng.
- **Cách sửa:** Chuyển `SimulateMain` sang quản lý danh sách `List<Rigidbody>` hoặc `List<SimulateObject>` trước khi làm Phase 4.

### 3.6. Giá trị Inspector bị Database ghi đè ngầm
`ApplyFromDatabase()` chạy trong `Awake` và ghi đè `_objectCoefficient`, `_objectForce`, `_moveForce`, `_crossSelectionArea`, `mass`. Vì vậy, chỉnh các số này trên Inspector sẽ không có tác dụng (trừ khi tắt `_autoLoadFromDatabase`). Muốn chỉnh thông số chuẩn, hãy sửa trong `Assets/Resources/SimulateObjectDatabase.json`.

### 3.7. Chênh lệch giữa kích thước Collider và diện tích đón gió `A`
Collider của drone có đường kính **51 unit**, nhưng `crossSelectionArea = 0.06 m²` (tương ứng drone thật cỡ 25×23 cm). Cần chốt 1 trong 2 quy ước:
- **(a)** Coi như drone nhỏ nhưng **phóng to model để dễ nhìn** → giữ nguyên `A = 0.06` và giữ nguyên ngưỡng lực ở Mục 1.5 *(khuyến nghị)*.
- **(b)** Coi đúng **1 unit = 1 m** → `A ≈ π · 25.5² ≈ 2043 m²` (lực gió sẽ rất lớn, phải giảm mạnh `Cd` hoặc mật độ không khí).

### 3.8. `_objectForce` và `_moveForce` đang không được dùng trong `Drone`
- Khi `_autoHover = true`, `Drone.GetThrustForce()` tự tính lực nâng bằng `mass · g / upAlignment` và **bỏ qua** `_objectForce`.
- `Drone.GetMoveForce()` đang trả về `Vector3.zero`, nên `_moveForce = 25` hoàn toàn chưa dùng tới.
- Không cần mất thời gian chỉnh 2 số này trong JSON ở giai đoạn hiện tại.

### 3.9. Lỗi góc nghiêng khi drone xoay hướng (Yaw khác 0)
`Drone.FixedUpdate` đang ghép góc bằng `Quaternion.Euler(targetPitch, transform.eulerAngles.y, targetRoll)` theo trục thế giới. Nếu drone xoay ngang 90°, bấm nghiêng tiến/lùi sẽ biến thành nghiêng trái/phải. Ngoài ra, việc đọc `transform.eulerAngles` (dễ bị nhảy 0–360°) trong khi ghi rotation qua Rigidbody cũng dễ gây sai lệch.
- **Cách sửa (Phase 3):** Tính `Quaternion.Euler(pitch, 0, roll)` theo không gian local, rồi nhân với góc yaw hiện tại được lưu ở một biến riêng.

---

## 4. Lộ Trình Thực Hiện (Checklist)

### Phase 0 — Chuẩn bị & Đo đạc ban đầu

- [ ] **P0.1 — Đo thông số gốc (Baseline) trước khi sửa code**
  - *Cách làm:* Bấm Play trong 10 giây, kiểm tra xem drone có đứng yên hay rơi, đồng thời `Debug.Log` lực gió mỗi `0.5 s`.
  - *Kiểm chứng:* Drone bay giữ độ cao ổn định ở `y ≈ 23.8`, lực gió in ra `≈ 2.4 N`.
- [ ] **P0.2 — Chốt 2 quyết định thiết kế (Mục 3.4 và Mục 3.7)**
  - *Cách làm:* Chọn Hướng A/B cho điều khiển góc nghiêng và quy ước (a)/(b) cho diện tích `A`, ghi lại vào Mục 6 (Nhật ký).
- [ ] **P0.3 — Bỏ Singleton trong `SimulateMain` (theo Mục 3.5)**
  - *Cách làm:* Cho `SimulateMain` nhận một danh sách đối tượng (`List`) thay vì `SimulateObject.Instance`, cập nhật `_rb` tương ứng.
  - *Kiểm chứng:* Thêm 1 Cube có `Rigidbody` vào danh sách → cả Drone và Cube đều chịu lực gió, không báo lỗi `NullReferenceException`.

### Phase 1 — Hoàn thiện lõi hệ thống Gió

- [ ] **P1.1 — Hiệu chuẩn khoảng cách dò vật cản `_windCheckDistance` ∈ `[250, 500]` (theo Mục 3.1)**
  - *Kiểm chứng:* Bật Gizmos, đặt drone sát mặt sau một tòa nhà → tia ray màu đỏ chạm tường và `IsBlockFromWind()` trả về `true`.
- [ ] **P1.2 — Đảm bảo tia dò gió không tự bắn trúng Drone**
  - *Cách làm:* Kiểm tra `_obstacleLayer` (hiện là `128` = `Wall`) không chứa layer `Drone` (6).
  - *Kiểm chứng:* `Debug.Log(Physics.Raycast(...))` khi drone ở giữa khoảng trống → luôn trả về `false`.
- [ ] **P1.3 — Xóa lực gió cũ khi `rb == null`**
  - *Cách làm:* Trong `WindForceFormula`, nếu thoát sớm do thiếu `Rigidbody` thì gán `_windForceVector = Vector3.zero` để tránh giữ lại lực từ frame trước.
  - *Kiểm chứng:* Tắt/bỏ `Rigidbody` của đối tượng → lực gió không còn đẩy tiếp bằng giá trị "ma" của frame cũ.

### Phase 2 — Mô phỏng Trường Gió Đô Thị

- [ ] **P2.1 — Tạo gió lùa khe nhà (Venturi) bằng 2 tia ngang**
  - *Cách làm:* Bắn 2 tia `Physics.Raycast` sang `transform.right` và `-transform.right` trên `_obstacleLayer`, tầm tối đa `_canyonMaxWidth` (~500), kèm `QueryTriggerInteraction.Ignore` để không đụng trigger zone. Nếu cả 2 tia đều trúng tường và `d_left + d_right < _canyonMaxWidth`, tính:
    `k_venturi = Mathf.Lerp(1.6f, 2.5f, 1f - (d_left + d_right) / _canyonMaxWidth)`.
  - *Kiểm chứng:* Bay vào khe giữa 2 tòa nhà (rộng ~454–504) → log `k_venturi ≈ 1.6`; bay ra chỗ trống hoặc lên trên nóc nhà → `k_venturi = 1.0`.
- [ ] **P2.2 — *(Tùy chọn)* Tạo gió Venturi bằng Trigger Zone**
  - *Cách làm:* Dùng `BoxCollider` bật `Is Trigger` đặt tại ngã tư/khe phố kết hợp `OverlapBox` để lấy hệ số nhân gió `windMultiplier`.
  - *Kiểm chứng:* Bay vào vùng trigger → hệ số tăng đúng; bay ra ngoài → về `1.0`.
- [ ] **P2.3 — Tạo gió xoáy sau nhà (Wake Turbulence) thay cho `Vector3.zero`**
  - *Cách làm:* Khi `IsBlockFromWind() == true`, tạo vector nhiễu bằng `Mathf.PerlinNoise`:
    - Dùng **2 seed/offset khác nhau** cho trục X và Z để hướng gió không bị chéo đều 45°.
    - Đưa giá trị `[0, 1]` của `PerlinNoise` về miền `[-1, 1]` bằng công thức `(n - 0.5f) * 2f`.
    - Tần số dao động khoảng `0.5 – 2 Hz`: `Mathf.PerlinNoise(Time.time * freq, seed)`.
    - Biên độ tỉ lệ với `_windSpeed` (ví dụ `0.3f * _windSpeed`).
  - *Kiểm chứng:* Đứng sau tòa nhà → lực gió không còn bằng 0 tuyệt đối; tia `Debug.DrawRay` màu vàng đổi hướng liên tục, biên độ `≤ 0.3 × _windSpeed`.
- [ ] **P2.4 — Tăng tốc độ gió theo độ cao (Wind Shear — theo Mục 3.3)**
  - *Cách làm:* Áp dụng `k_shear = Mathf.Clamp(1f + (y / 296f), 1f, 2.2f)`.
  - *Kiểm chứng:* Tại `y = 0.5` (mặt đất) → `1.00`; `y = 148` → `1.50`; `y = 296` (nóc cao nhất) → `2.00`; `y > 296` → bị giới hạn ở `2.00` (hoặc `2.2` tùy hệ số).
- [ ] **P2.5 — Tổng hợp vận tốc gió hoàn chỉnh**
  - *Cách làm:* `V_wind = V_mean · k_shear · k_venturi + v_turb`.
  - *Kiểm chứng:* Đo và log `V_wind` tại 4 vị trí (giữa phố trống / trong khe nhà / sau lưng nhà / trên nóc nhà), đối chiếu với giá trị tính tay trong Nhật ký.

### Phase 3 — Động lực học & Tự cân bằng cho Drone

- [ ] **P3.1 — Sửa lỗi nghiêng sai hướng khi xoay Yaw (theo Mục 3.9)**
  - *Kiểm chứng:* Xoay drone sang hướng 90°, bấm phím tiến (`W`) → drone phải nghiêng chúi mũi về **phía trước mặt nó**, không nghiêng ngang sang bên.
- [ ] **P3.2 — Tính `A_eff` động theo góc nghiêng (theo Mục 3.2)**
  - *Cách làm:* Dùng công thức hình chiếu hộp 3 trục (hoặc công thức rút gọn 1 tham số).
  - *Kiểm chứng:* Drone bay bằng và gặp gió ngang 8.14 m/s → `A_eff ≠ 0` (`≈ A_front`). Gió thổi từ trên/dưới → `A_eff ≈ A_top`. Không có tình huống `A_eff == 0` khi có gió tương đối.
- [ ] **P3.3 — Bộ điều khiển PID giữ cân bằng góc (Hướng A)**
  - *Cách làm:* Viết trong `FixedUpdate`, dùng `Time.fixedDeltaTime`:
    - **P:** tỉ lệ với sai số góc hiện tại.
    - **I:** cộng dồn sai số, nhớ **clamp** theo `_integralMax` để chống *Integral Windup*.
    - **D:** tính tốc độ thay đổi trên **góc đo được hiện tại** (tránh *Derivative Kick*).
    - Đầu ra của PID cộng bù vào góc nghiêng mục tiêu.
  - *Kiểm chứng:* Bật gió Venturi 15.2 N thổi ngang → drone không lật, góc nghiêng hồi về `< 5°` trong `≤ 2 giây`. Nghiêng thử drone `20°` rồi thả → tự cân bằng về `0°`, vọt lố (overshoot) `< 10%`.
- [ ] **P3.4 — Tự giữ vị trí tại chỗ (Position Hold / Hover)**
  - *Cách làm:* Dùng khâu **P** trên sai số vị trí để sinh góc nghiêng bù, kết hợp khâu **D** trên vận tốc để hãm dao động.
  - *Kiểm chứng:* Thả tay không điều khiển giữa luồng gió đều → drone tự giữ vị trí trong bán kính **`≤ 5 unit`** quanh điểm ban đầu.
- [ ] **P3.5 — *(Tùy chọn)* Mô phỏng 4 motor độc lập**
  - *Cách làm:* Dùng `_rb.AddForceAtPosition(motorUp * thrust, motorPos)` tại 4 vị trí motor.
  - *Lưu ý:* Chỉ thực hiện nếu ở Mục 3.4 bạn chọn **Hướng B** (bỏ `FreezeRotation`).
  - *Kiểm chứng:* Tăng lực đẩy 1 motor → drone nghiêng về phía đối diện.

### Phase 4 — Tương tác với Môi trường

> ⚠️ **Điều kiện tiên quyết:** Phải hoàn thành **P0.3** trước để tránh đẩy lực nhầm vào chính drone.

- [ ] **P4.1 — Hiệu ứng gió quẩn dưới cánh quạt (Downwash)**
  - *Cách làm:* Dùng `Physics.OverlapSphere(pos, radius, mask, QueryTriggerInteraction.Ignore)`, **bỏ qua `_objectRigibody` của chính drone**, lực đẩy giảm dần theo `1 - d / radius`, tác dụng bằng `AddForceAtPosition`.
  - *Kiểm chứng:* Đặt 1 Cube nhẹ dưới bụng drone → Cube bị thổi văng ra; bản thân drone đứng yên không tự đẩy chính mình.
- [ ] **P4.2 — Hiệu ứng đệm khí gần mặt đất / tường (Ground / Wall Effect)**
  - *Cách làm:* Bắn `Physics.Raycast` ngắn xuống dưới để đo khoảng cách tới bề mặt.
  - *Lưu ý:* Mặt đất `Cube` đang ở layer `Default` (0), chưa nằm trong `Wall` (7) — cần tạo layer mask riêng hoặc đổi layer mặt đất sang `Wall`.
  - *Kiểm chứng:* Hạ drone sát đất → lực nâng tăng lên khi khoảng cách nhỏ hơn ngưỡng; bay lên cao thì lực cộng thêm về `0`.
- [ ] **P4.3 — Hiệu ứng hình ảnh cho Downwash (WindZone + Particle)**
  - *Cách làm:* Gắn `WindZone` (dạng `Spherical`) làm con của drone, bật module **External Forces** trên Particle System.
  - *Kiểm chứng:* Hạt bụi/lá bị thổi tung khi drone bay sát đất, không bị ảnh hưởng khi drone bay cao.
- [ ] **P4.4 — Particle bụi/lá bay ở ngã tư**
  - *Kiểm chứng:* Hướng bay của particle khớp với `_windDirection` (tức `transform.forward` của `Simulate Controller`).

---

## 5. Nguyên tắc Review Code

Khi bạn viết xong một phần và gửi để kiểm tra, Agent sẽ rà soát lần lượt theo 7 tiêu chí sau (chỉ chỉ ra lỗi và gợi ý hướng sửa, không viết code thay):

1. **Biên dịch:** Kiểm tra lỗi cú pháp, tránh API cũ của Unity (ví dụ Unity 6 dùng `rb.linearVelocity` thay cho `rb.velocity`).
2. **An toàn Null (Null-safety):** Kiểm tra `_rb`, `_target`, kết quả raycast, `GetComponent` trước khi dùng.
3. **Đúng đơn vị:** Khoảng cách khớp với Mục 1 (unit), lực khớp với bảng đối chiếu Mục 1.5 (Newton).
4. **Đúng vòng đời Unity:** Đọc input ở `Update`, tính vật lý/áp lực ở `FixedUpdate`, tìm và lưu component ở `Awake`/`Start`.
5. **Không xung đột Constraint:** Không gọi `AddTorque` nếu vẫn đang bật `FreezeRotation` (xem Mục 3.4).
6. **Hiệu năng:** Raycast/Overlap trong `FixedUpdate` phải có `layerMask` và khoảng cách hợp lý; không gọi `GetComponent` hay tạo mảng mới liên tục mỗi frame.
7. **Tính ổn định (Determinism):** Dùng `Time.fixedDeltaTime` trong `FixedUpdate` và `Time.deltaTime` trong `Update`.

---

## 6. Nhật ký & Quyết định đã chốt

| Ngày | Công việc | Kết quả | Ghi chú |
|---|---|---|---|
| 2026-11-26 | Chuẩn hóa plan v2 → v3 | Xong | Sửa tham chiếu chéo, thêm giải thích thuật ngữ, tách rõ mục tiêu & kiểm chứng |
|  |  |  |  |

**Quyết định đã chốt (điền ở bước P0.2):**
- Mục 3.4 — Hướng điều khiển góc (Attitude): `_` *(A — `MoveRotation` / B — `AddTorque`)*
- Mục 3.7 — Quy ước diện tích đón gió `A`: `_` *(a — giữ `0.06` / b — tính theo kích thước thật)*
