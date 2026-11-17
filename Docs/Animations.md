# WhackyZoo — Thông số chuyển động và hiệu ứng (mọi màn hình)

Lấy đúng từ bản thiết kế (file HTML trong `Docs/DesignSource/`, phần `<style>` của từng màn). Đơn vị px là px thiết kế (khung 390 × 844); với Canvas Scaler reference 1170 × 2532 thì **1 px thiết kế = 3 đơn vị UI**. "ease-in-out" ≈ AnimationCurve.EaseInOut; có thể dùng DOTween hoặc Coroutine.

## 1. Trang Chọn map (`02_chon_map`)

Mỗi thẻ map = ảnh nền thẻ + hiệu ứng nền riêng của map + 4 con thú (thứ tự lớp: nền → hiệu ứng → thú). Cả 5 thẻ đều có chuyển động riêng: Trên cạn mây, Dưới biển bong bóng và tia nắng, Sa mạc gió, Bắc cực tuyết, Nghĩa trang đàn dơi. Sprite hiệu ứng nằm cùng thư mục `map_cards` (tên bắt đầu bằng `card_fx_`).

**Ảnh nền thẻ:** dùng `E:\Projects\WhackyZoo\images\sprites\map_cards\card_bg_<land|sea|desert|arctic|grave>@3x.png` (344 × 122 px thiết kế, góc vuông; thẻ tự bo góc 26 px bằng Mask, viền 3 px #3A2412). Đừng tự vẽ lại nền.

| Hiệu ứng | Áp cho | Thông số |
| --- | --- | --- |
| Thú lơ lửng (bob) | 4 thú trên mọi thẻ, cỡ 64 × 64 px, cách nhau 2 px, căn giữa, cách đáy ảnh 8 px | translateY 0 → −5 px → 0, chu kỳ 1,5 s, ease-in-out, lặp vô hạn; con thứ n trễ n × 0,25 s (0 / 0,25 / 0,5 / 0,75 s) |
| Bong bóng sủi | Chỉ thẻ Dưới biển: 3 bóng | Vòng tròn viền 2 px trắng xanh rgba(220,245,255,.9), lõi có điểm sáng. Vị trí (left, bottom, cỡ, trễ): (34, 30, 14 px, 0 s), (300, 40, 18 px, 1,3 s), (330, 30, 10 px, 2,4 s). Bay lên 110 px trong 4 s, tuyến tính; alpha 0 → 1 ở 15% → 0 ở cuối; lặp vô hạn |
| Tia nắng dưới nước | Thẻ Dưới biển: 2 tia ở left 40 px và 170 px (tia 2 trễ 1,5 s) | Dải 34 × 220 px, đỉnh ở top −30 px, gradient trắng 28% → trong suốt, xoay qua lại 12° ↔ 22°, 5 s, ease-in-out, ping-pong |
| Mây đung đưa | Thẻ Trên cạn: 2 đám mây (`card_fx_cloud_big` 92 × 30 tại left 18, top 22; `card_fx_cloud_small` 58 × 20, alpha 0,9, tại left 140, top 12) | Trôi ngang −8 ↔ +12 px, ease-in-out, ping-pong; mây lớn 5 s, mây nhỏ 6,5 s lệch pha −2 s |
| Gió thổi (dải gió mờ) | Thẻ Sa mạc, vùng trời phía sau đồi cát trước và sau thú: 4 dải trắng mờ `card_fx_wind_band` (sprite đã nhòe sẵn, kéo giãn theo chiều ngang được). Kích thước (top, rộng × cao): (14, 150 × 10) · (34, 110 × 9) · (52, 170 × 11) · (24, 90 × 8) | Mỗi dải trôi từ x −180 px sang x +380 px (thẻ rộng 344 px), ease-in-out; alpha 0 → 1 ở 20% → giữ 1 tới 80% → 0 ở cuối. Thời gian và trễ: 6,0 s/0 s · 7,5 s/1,8 s · 6,8 s/3,4 s · 8,0 s/4,6 s, lặp vô hạn. Không có hạt cát, không có vệt xoắn |
| Tuyết rơi | Thẻ Bắc cực: 9 bông tròn trắng (`card_fx_snow_dot`) 4–7 px, có quầng sáng nhẹ | Bắt đầu ở top −8 px, rơi 136 px và lệch trái 12 px, tuyến tính, 4,2–6,2 s mỗi bông; alpha 0 → 1 (12%) → 0,2. (left, cỡ, thời gian, trễ): (20,6,4.6,0) (62,4,5.8,1.8) (104,7,4.2,3.1) (150,5,5.2,0.7) (196,6,4.8,2.4) (238,4,6.0,4.0) (276,7,4.4,1.2) (318,5,5.4,3.6) (130,4,6.2,5.0) |
| Đàn dơi bay ngang | Thẻ Nghĩa trang: 5 dơi (`card_fx_bat_wings_up` / `_down`, 2 khung) rộng 44, 34, 30, 28, 24 px; bay theo đội hình V lệch, nằm trước mặt trăng, sau thú | Cả đàn bay từ x −110 (y 34) sang x +440 (y 4) trong 65% của chu kỳ 9 s, tuyến tính, rồi nghỉ 35%. Vỗ cánh: đổi khung up/down mỗi 0,22 s. Mỗi con nhấp nhô lên 4 px, 0,9 s ping-pong, lệch pha 0–0,55 s |
| Thẻ hiện ra | Mọi thẻ khi vào trang | từ dưới 30 px + scale 0,96 + alpha 0 → vị trí gốc, 0,5 s; thẻ sau trễ thêm 0,1 s (0, 0,1, 0,2, 0,3, 0,35, 0,45 s) |
| Nhấn thẻ | Mọi thẻ | lún xuống 3 px, bóng dưới thẻ co từ 6 px còn 2 px, 0,15 s, rồi chuyển màn |

## 2. Thú trong hố (mọi map)

| Hiệu ứng | Thông số |
| --- | --- |
| Ló lên (rise) | 0,42 s, curve cubic-bezier(.2,.9,.3,1.25) (vọt quá rồi nảy). Keyframe: 0% y +105% scale (0,9; 1,1) → 60% y −12% scale (0,95; 1,06) → 82% y +3% scale (1,06; 0,94) → 100% về gốc. Pivot đáy giữa |
| Thở/lơ lửng khi đang ló (bob) | Bắt đầu sau 0,45 s; 1,3 s/chu kỳ, ease-in-out: y 0 → −3 px, scale (1,01; 1,03) → về |
| Chớp mắt | 3,2 s/chu kỳ: 92–100% thời gian mắt scaleY 1 → 0,1 (ở 95%) → 1 |
| Bị đập (squash) | 0,42 s ease-in: 18% y +8% scale (1,3; 0,58) → 45% y +10% scale (1,15; 0,75) → 100% y +115% scale (1; 0,8) |
| Rụt xuống (sink) | 0,24 s ease-in tới y +110% scale (1,05; 0,95) |
| Lấp lánh (thưởng +50) | Ngôi sao nhỏ scale 0,8 ↔ 1,2, alpha 0,8 ↔ 1, 0,25 s ping-pong |
| Sao choáng | Vòng sao quay 360° mỗi 0,9 s trên đầu thú bị đập |
| Chữ điểm bay (+10) | 0,9 s ease-out: 0% y +10 scale 0,6 alpha 0 → 25% y 0 scale 1,15 alpha 1 → 100% y −46 px alpha 0. Chữ trắng viền nâu, 28 px |
| Nổ "BỐP!" | 0,55 s: scale 0,2 xoay −20° → 45% scale 1,1 xoay 4° → 100% scale 1 alpha 0 |
| Dụng cụ vung | 0,34 s: xoay −75° → 70% +12° → 85% +4° → 100% +6° alpha 0; pivot 85%/90% |
| Rung màn khi đập vật cản | 0,28 s: lệch (−4,2) → (4,−2) → (−3,1) → (2,−1) → 0 px |
| Đồng hồ ≤ 10 s | chữ đỏ #E53935, scale 1 ↔ 1,12, 0,6 s ping-pong |

## 3. Hiệu ứng nền từng map (màn chơi)

| Map | Hiệu ứng | Thông số |
| --- | --- | --- |
| Trên cạn | Mây trôi | 2 đám mây trắng bo tròn trôi ngang −14 ↔ +18 px, 26 s, ping-pong (đám thứ hai 34 s) |
| Dưới biển | Bong bóng sủi | 4 bóng (left, cỡ, trễ): (26, 14, 0), (120, 9, 1,4), (250, 16, 2,6), (340, 11, 3,6 s); bắt đầu dưới đáy màn, bay lên 520 px trong 5 s, lắc ngang +6 px giữa đường rồi −4 px; alpha 0 → 1 (10%) → 0 |
| Dưới biển | Tia nắng | Dải 50 × 560 px, trắng 22% → trong suốt, xoay 14° ↔ 24°, 6 s ping-pong, pivot trên |
| Dưới biển | Rong biển | skewX −7° ↔ 7°, 3 s ping-pong, pivot dưới |
| Dưới biển | Gợn sóng khi đập | Vòng tròn viền trắng 3 px, scale 1 → 5, alpha 1 → 0, 0,5 s |
| Dưới biển | Xúc tu bạch tuộc | skewX −6° ↔ 6°, 1 s ping-pong |
| Sa mạc | — | Nền tĩnh (không mây) |
| Bắc cực | Tuyết rơi | 8 bông tròn trắng 6–10 px rơi 560 px, lệch −20 px, 9–12 s mỗi bông, trễ lệch nhau; alpha 0 → 1 (10%) → 0 |
| Nghĩa trang | Đom đóm | 6 chấm vàng 6 px có quầng sáng, bay lệch (+18, −26) px, 5–7 s ping-pong, alpha 0,2 → 1 → 0,3 |

Dùng Particle System hoặc vài Image + script; nên đặt hiệu ứng nền vào ScriptableObject của map để thêm map mới không sửa code.

## 4. Trang chủ

| Hiệu ứng | Thông số |
| --- | --- |
| Logo hiện ra | 0,7 s, cubic-bezier(.2,.9,.3,1.3): scale 0,3 → 1, giữ xoay −3° |
| 3 thú trên logo | bob 1,6 s, −6 px; chớp mắt 3,4 s |
| Nút CHƠI NGAY | thở: scale 1 ↔ 1,035, 1,4 s, ease-in-out, lặp |
| Icon búa (thẻ Đang dùng) và icon hộp quà (Quà hằng ngày) | lắc (wiggle) 2,4 s/chu kỳ, pivot 50%/80%: đứng yên 80% thời gian rồi xoay −12° → +10° → −6° → +4° → 0° |
| Mây | trôi −16 ↔ +20 px, 24 s ping-pong |

## 5. Dùng chung cho mọi màn

| Hiệu ứng | Thông số |
| --- | --- |
| Nút 3D (mọi nút bo tròn có viền nâu) | Bình thường có "gờ" dưới dày 5–6 px màu `--edge` và vệt bóng trắng ở 40% trên. Khi nhấn: lún xuống 5 px, gờ còn 1 px, 0,08 s; thả ra bật lại |
| Vệt sáng quét (shine) | Dùng trên món Huyền thoại ở Cửa hàng: dải trắng 30 px nghiêng −20° quét từ trái qua phải; chu kỳ 2,6 s, chỉ chạy trong khoảng 50–85% chu kỳ, còn lại đứng yên |
| Khối nội dung hiện ra (enter) | Cài đặt, Đăng nhập, Đăng ký, Quên mật khẩu, Tài khoản: từ dưới 24–26 px + alpha 0 → vị trí gốc, 0,5 s, cubic-bezier(.2,.9,.3,1.2); các khối trễ lần lượt (Đăng nhập: logo 0 s, khung 0,3 s, nút dưới 0,6 s) |
| Số xu thay đổi (bump) | Ô xu ở góc trên: scale 1 → 1,18 (ở 40%) → 1, 0,35 s mỗi lần cộng/trừ xu |
| Thanh tiến độ | Khi giá trị đổi, chiều rộng chạy tới giá trị mới 0,35–0,4 s, cubic-bezier(.2,.9,.3,1.2) (vượt nhẹ rồi về) |
| Danh sách cuộn | Không hiện thanh cuộn; mép trên mờ 8–14 px, mép dưới mờ 26–28 px; cuộn có quán tính và nảy (ScrollRect Elastic) |

## 6. Màn chơi: lớp phủ và popup

| Hiệu ứng | Thông số |
| --- | --- |
| Màn cách chơi hiện ra | alpha 0 → 1, 0,35 s; thanh tải chạy đều 0 → 100% trong 8 s rồi tự đóng |
| Popup Tạm dừng / Hết giờ | scale 0,7 + alpha 0 → 1, 0,45 s, cubic-bezier(.2,.9,.3,1.3) (nảy) |
| Sao/biểu tượng trong popup kết quả | scale 0 xoay −90° → scale 1 xoay 0°, 0,45 s, cubic-bezier(.2,.9,.3,1.5); các sao trễ lần lượt |
| Số xu khi nhận x2 quảng cáo | scale 1 → 1,4 → 1, 0,55 s, cubic-bezier(.2,.9,.3,1.6) |

## 7. Cửa hàng

| Hiệu ứng | Thông số |
| --- | --- |
| Thẻ đang chọn | Nâng lên 6 px, bóng dưới dày 9 px, viền cam #FF6B4A 3 px cách 2 px; chuyển 0,15 s |
| Icon của thẻ đang chọn | Lắc lư (wobble) 1,2 s lặp: 25% xoay −8° lên 3 px, 75% xoay +8° lên 3 px |
| Khung chi tiết khi đổi món | scale 0,96 + alpha 0,6 → 1, 0,3 s, cubic-bezier(.2,.9,.3,1.3) |
| Thanh chỉ số | chạy tới giá trị mới 0,35 s (mục 5) |
| Món Huyền thoại (Búa Thor, Kỳ Lân) | nền tím + vệt sáng quét (mục 5) |
| Mua thành công | ô xu bump (mục 5) |

## 8. Nhiệm vụ

| Hiệu ứng | Thông số |
| --- | --- |
| Dòng nhiệm vụ hiện ra | từ phải 30 px + alpha 0 → gốc, 0,4 s, cubic-bezier(.2,.9,.3,1.2); dòng thứ i trễ i × 0,06 s |
| Nút "Nhận" (đã đủ điều kiện) | đập nhịp scale 1 ↔ 1,12, 0,8 s ping-pong |
| Rương đủ điểm, chưa mở | lắc 1,2 s lặp, pivot 50%/90%: yên 70% thời gian rồi xoay −10° (phóng 1,08) → +9° (1,08) → −5° → 0 |

## 9. Xếp hạng

| Hiệu ứng | Thông số |
| --- | --- |
| Bục Top 3 | mọc từ dưới lên: scaleY 0 + alpha 0 → 1, 0,55 s, cubic-bezier(.2,.9,.3,1.25), pivot đáy; mỗi bục trễ thêm 0,12 s |
| Vương miện hạng 1 | 1,6 s lặp: y 0 xoay −6° → y −4 px xoay +6° → về |
| Tia sáng nền | quay 360° mỗi 26 s, tuyến tính |
| Dòng xếp hạng | từ dưới 16 px + alpha 0 → gốc, 0,4 s; dòng thứ i trễ 0,3 + i × 0,05 s |

## 10. Quà hằng ngày

| Hiệu ứng | Thông số |
| --- | --- |
| Ô ngày hiện ra | scale 0,6 + alpha 0 → 1, 0,45 s, cubic-bezier(.2,.9,.3,1.3); thiết kế cho 7 ô hiện cùng lúc (có thể trễ i × 0,05 s cho sinh động) |
| Ô hôm nay | quầng sáng vàng nhấp nháy: 0 → 18 px rgba(255,225,74,.9), 1,1 s ping-pong |
| Rương Ngày 7 | nhún 1,2 s lặp: 30% lên 6 px xoay −6° → 60% về xoay +5° → 0 |
| Tia sáng nền | quay 360° mỗi 30 s |
| Nút NHẬN | thở scale 1 ↔ 1,035, 1,4 s |

## 11. Tài khoản

| Hiệu ứng | Thông số |
| --- | --- |
| Logo Đăng nhập | scale 0,3 + alpha 0 → 1, 0,7 s, cubic-bezier(.2,.9,.3,1.3) (không xoay) |
| Popup Chỉnh sửa hồ sơ (bottom sheet) | trượt từ đáy màn lên: y +100% → 0, 0,35 s, cubic-bezier(.2,.9,.3,1.1); nền tối mờ dần phía sau |
| Ô tích (checkbox) | nền chuyển xanh lá + dấu tích trắng phóng từ 0 → 1, 0,15 s |
| Ô nhập đang chọn | viền chuyển cam #E07A1A + quầng cam 4 px rgba(255,159,28,.35) |
