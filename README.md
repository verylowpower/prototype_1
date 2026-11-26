# prototype_1 — Urban UAV & Wind Simulation

Prototype Unity mô phỏng **drone bay trong môi trường thành phố** chịu tác động của **trường gió khí động học** (gió nền, hiệu ứng hẻm núi đô thị, nhiễu loạn sau toà nhà, gió tăng theo độ cao).

Repo: <https://github.com/verylowpower/prototype_1>

---

## Yêu cầu môi trường

| Thành phần | Phiên bản |
|---|---|
| Unity Editor | **6000.6.8f1** |
| Render Pipeline | Universal RP **17.6.0** |
| Input System | com.unity.inputsystem **1.20.1** |
| Nền tảng | Windows (đã cấu hình PC + Mobile renderer) |

Mở project bằng đúng phiên bản Unity ở trên. Nếu Unity tự nâng cấp `Packages/manifest.json`, hãy kiểm tra lại `ProjectSettings/VersionControlSettings`.

## Chạy thử

1. Mở scene **`Assets/1.Scenes/Prototype 1.unity`** (đây là scene duy nhất trong Build Settings).
2. Bấm **Play**.
3. Điều khiển drone:

| Phím | Tác dụng |
|---|---|
| `W` / `S` | Tiến / lùi (nghiêng thân theo trục pitch) |
| `A` / `D` | Sang trái / phải (nghiêng thân theo trục roll) |
| `Space` | Bay lên |
| `Ctrl` | Hạ xuống |

Drone tự bù lực nâng theo góc nghiêng để giữ độ cao (`_autoHover`), nên nghiêng thân là drone sẽ trượt ngang.

> **Lưu ý:** di chuyển đọc trực tiếp `Keyboard.current` trong `SimulateObject.ObjectControl()`. File `Assets/InputSystem_Actions.inputactions` là template mặc định của Unity, **chưa được nối** vào code.

---

## Cấu trúc thư mục

```
Assets/
├── 1.Scenes/Prototype 1.unity      # Scene chính: mặt đất + 12 toà nhà + drone + Simulate Controller
├── 2.Script/                       # Toàn bộ logic gameplay
│   ├── SimulateObject.cs           # Lớp cơ sở: input, thông số vật lý, dữ liệu từ database
│   ├── SimulateObjectDatabase.cs   # Đọc/cache Resources/SimulateObjectDatabase.json
│   ├── SimulateMain.cs             # Hệ thống gió: lực cản, kiểm tra chắn gió
│   └── Drone.cs                    # Điều khiển bay, nghiêng thân, tự bù lực nâng
├── 3.Material/                     # floor.mat, wall.mat
├── Resources/SimulateObjectDatabase.json   # Bảng thông số vật lý (Cd, lực, diện tích, khối lượng)
├── Settings/                       # URP pipeline + volume profile
├── Plugins/IngameDebugConsole/     # Console in-game (asset bên thứ ba)
└── vFavorites/                     # Công cụ bookmark của Editor (asset bên thứ ba)
```

Script ngoài phạm vi gameplay (`Plugins/`, `vFavorites/`) là asset có sẵn, **không chỉnh sửa**.

---

## Kiến trúc

```
SimulateObject (base)
   ├── Instance (static singleton)
   ├── Awake()  ──► ApplyFromDatabase()  ──► SimulateObjectDatabase.LoadDatabase()
   ├── ObjectControl()  (đọc bàn phím mỗi Update)
   ├── GetThrustForce() / GetMoveForce()  (virtual)
   └── Drone : SimulateObject  (override lực nâng + nghiêng thân)

SimulateMain  ──► mỗi FixedUpdate: WindForceFormula() → AddForce()
```

`SimulateMain` lấy `Rigidbody` mục tiêu từ `SimulateObject.Instance` **một lần trong `Start()`**, sau đó mỗi `FixedUpdate` cộng dồn ba thành phần lực: **lực gió + lực nâng + lực di chuyển**.

### Công thức gió hiện có

```
v_rel  = v_drone − v_wind
F_drag = 0.5 · ρ · |v_rel|² · Cd · A          (ρ = 1.225 kg/m³)
F_gió  = −v_rel_dir · F_drag
```

Nếu `IsBlockFromWind()` trả `true` (raycast ngược hướng gió chạm toà nhà), gió hiện được gán bằng **không** — đây là điểm sẽ được thay bằng nhiễu loạn ở roadmap.

---

## Dữ liệu thông số

`Assets/Resources/SimulateObjectDatabase.json` là **nguồn sự thật duy nhất** cho thông số vật lý:

```json
{ "id": "Drone", "objectCoefficient": 1.0, "objectForce": 14.715,
  "moveForce": 25.0, "crossSelectionArea": 0.06, "mass": 1.5 }
```

Khi `_autoLoadFromDatabase = true`, `Awake()` sẽ **ghi đè** `_objectCoefficient`, `_objectForce`, `_moveForce`, `_crossSelectionArea` và `rigidbody.mass` bằng giá trị trong file JSON.

> ⚠️ **Chỉnh các thông số này trên Inspector sẽ không có tác dụng.** Muốn thay đổi phải sửa file JSON.

Id tra cứu là `_objectId`; nếu để trống sẽ dùng tên class. So khớp **không phân biệt hoa thường**.

---

## Thiết lập scene

### Layer

| Layer | Index | Dùng cho |
|---|---|---|
| `Drone` | 6 | GameObject drone |
| `Wall` | 7 | Toàn bộ toà nhà (12 object `tower`) |

`SimulateMain._obstacleLayer` = **128** → chỉ `Wall`. `_windQuery` **không** bao gồm layer `Drone`, nên drone không tự chặn gió của chính nó.

### Quy mô

- Mặt đất `Cube`: kích thước `5134.7 × 1 × 3372.7`, **mặt trên y = 0.5**, layer `Default` (0).
- 12 toà nhà layer `Wall`, tiết diện ~`415.8 × Z`, **chiều cao 81.4 → 295.8**, khe phố theo trục X rộng **454 – 504 unit**.
- Drone: `SphereCollider` radius 0.5 × scale 51.1 → **đường kính thực ~51 unit**, khối lượng hiệu dụng 1.5.
- Gió: `_windSpeed = 8.14 m/s`, hướng **luôn lấy từ `transform.forward` của `Simulate Controller`** (xoay Transform để đổi hướng, giá trị `_windDirection` trên Inspector bị ghi đè mỗi frame).

### Lực tham chiếu

| Trường hợp | Giá trị |
|---|---|
| Trọng lượng drone | **14.72 N** |
| Lực cản gió @ 8.14 m/s | **≈ 2.44 N** (17% trọng lượng) |
| Lực cản gió @ 20.35 m/s (venturi ×2.5) | **≈ 15.2 N** (≈ 103% trọng lượng) |

---

## Roadmap

Kế hoạch chi tiết, số liệu hiệu chuẩn và checklist kiểm chứng nằm trong **[plan.md](plan.md)**:

1. **Phase 0** — Baseline & tách `SimulateMain` khỏi singleton
2. **Phase 1** — Hoàn thiện lõi gió (hiệu chuẩn `_windCheckDistance`)
3. **Phase 2** — Trường gió đô thị: Venturi, wake turbulence, wind shear
4. **Phase 3** — Động lực học: PID attitude/position hold, `A_eff` động
5. **Phase 4** — Tương tác môi trường: downwash, ground/wall effect, particle

---

## Quy tắc đóng góp

Dự án này được dùng để **tự học Unity/C#**, nên có ràng buộc riêng (xem [agents.md](agents.md)):

- **Không dùng AI để viết hoặc sửa file script.** Code do người dùng tự viết.
- AI chỉ được đọc và **review** code, nêu vấn đề và gợi ý hướng sửa.
- Mọi thay đổi file (kể cả tài liệu) phải được thông báo và đồng ý trước.
- Commit message **tối đa 1 dòng**, ngắn gọn.
- Push ưu tiên lên branch **khác `main`**; chỉ push trực tiếp `main` khi có yêu cầu rõ ràng.

## Asset bên thứ ba

- [Ingame Debug Console](https://github.com/yasirkula/UnityIngameDebugConsole)
- [vFavorites](https://github.com/verylowpower)
