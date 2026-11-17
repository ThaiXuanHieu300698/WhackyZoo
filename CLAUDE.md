# WhackyZoo (Đập Thú Vui Nhộn) — hướng dẫn cho Claude Code

Game đập thú (whack-a-mole) 2.5D, Unity 6 (6000.6.9f1), mẫu Universal 2D, build Android, màn hình dọc.

## Tài liệu bắt buộc đọc

- `Docs/GameSpec.md` (cạnh file này) — toàn bộ luật chơi, số liệu, giá, map, thú, vật phẩm. Mọi con số trong code phải lấy từ đây; chỗ nào tài liệu ghi "cần chốt" thì hỏi người dùng trước khi tự quyết.
- `Docs/Animations.md` — thông số chuyển động (thú lơ lửng, ló/đập/rụt, bong bóng, tuyết, đom đóm, mây…) và ảnh nền thẻ map. Giao diện phải có đủ các chuyển động này, không chỉ ảnh tĩnh.
- `Docs/DesignSource/*.dc.html` — mã HTML/CSS bản thiết kế mới nhất của từng màn. Khi cần vị trí, màu, cỡ chữ, keyframe chính xác thì đọc phần `<style>` và markup trong đây; so với ảnh `images/screens/` để kiểm tra.

## Đường dẫn

| Nội dung | Đường dẫn |
| --- | --- |
| Thư mục làm việc (chứa file này) | `E:\Projects\WhackyZoo\Game` |
| Project Unity (mở bằng Unity Hub) | `E:\Projects\WhackyZoo\Game\WhackyZoo` — `Assets/`, `Packages/`, `ProjectSettings/` nằm ở đây |
| Ảnh màn hình tham chiếu | `E:\Projects\WhackyZoo\images\screens\` |
| Hình rời (sprite) @1x/@2x/@3x | `E:\Projects\WhackyZoo\images\sprites\<nhóm>\` |
| Font Baloo 2 | `E:\Projects\WhackyZoo\fonts\static\` |
| Bản thiết kế HTML mới nhất | `Docs/DesignSource/` (thư mục `E:\Projects\WhackyZoo\canvas-project\` là bản cũ, đừng dùng) |
| Ảnh nền thẻ map (trang Chọn map) | `E:\Projects\WhackyZoo\images\sprites\map_cards\` |

Khi cần sprite, chép bản `@3x` (hoặc `@2x`) vào `WhackyZoo/Assets/Art/...`; không sửa thư mục `images` gốc.

## Thông số project

- Product Name: Đập Thú Vui Nhộn · Company: ThaiHieu · Package: `com.thaihieu.whackyzoo`
- Khung thiết kế 390 × 844 pt; Canvas Scaler: Scale With Screen Size, reference 1170 × 2532 (@3x), match 0.5.
- Orientation: Portrait.

## Quy ước code

- C#, namespace `WhackyZoo`. Script trong `WhackyZoo/Assets/Scripts/<Mảng>/` (Gameplay, UI, Data, Core).
- Dữ liệu map, thú, dụng cụ, vật phẩm dùng ScriptableObject trong `WhackyZoo/Assets/Data/`, để thêm map mới không phải sửa code.
- Không hard-code số liệu cân bằng trong MonoBehaviour; đưa vào ScriptableObject hoặc một config.
- Chạy thử bằng nút Play + Device Simulator; chưa có điện thoại Android thật.

## Quy trình git

Repo: https://github.com/ThaiXuanHieu300698/WhackyZoo (riêng tư, nhánh chính `main`). Ảnh/âm thanh/font lưu bằng Git LFS (`.gitattributes`).

- Không commit thẳng lên `main`. Mỗi yêu cầu sửa một nhánh từ `main` mới nhất: `feat/<việc>`, `fix/<việc>`, `chore/<việc>`, `docs/<việc>` (tên tiếng Anh không dấu, vd. `fix/daily-prize-icon`).
- Commit nhỏ, theo Conventional Commits, mô tả tiếng Việt: `fix(daily): ô Gậy Bóng Chày nhún quanh vị trí gốc`. Phạm vi thường dùng: `gameplay`, `shop`, `daily`, `missions`, `home`, `audio`, `ui`, `data`, `docs`.
- Xong việc: đẩy nhánh, mở pull request vào `main` (mô tả: đã đổi gì, cần dựng lại scene nào, đã kiểm tra gì); người dùng kiểm tra trong Unity rồi merge kiểu squash và xoá nhánh. Sau đó về `main`, `git pull`.
- Chỉ commit / đẩy / merge khi người dùng đồng ý; không `push --force` lên `main`.
- Không đưa vào repo: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Assets/_Recovery/`, file `.csproj`/`.sln`.

## Thứ tự làm việc

1. Prototype map Trên cạn: lưới 3×3, thú ló/rụt, chạm để đập, đồng hồ 60 giây, điểm, combo, popup Hết giờ.
2. ScriptableObject cho map/thú, thêm 4 map còn lại và màn cách chơi 8 giây.
3. Trang chủ, Chọn map, Cài đặt; lưu tiến trình trên máy.
4. Xu, Cửa hàng, vật phẩm hỗ trợ.
5. Nhiệm vụ, Quà hằng ngày, cấp độ.
6. Tài khoản, Xếp hạng (Firebase), quảng cáo, mua bằng tiền thật.
