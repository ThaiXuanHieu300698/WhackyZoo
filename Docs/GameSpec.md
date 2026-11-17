# Đập Thú Vui Nhộn (WhackyZoo) — Tài liệu thông số game

Bản xuất từ tài liệu Claude Docs ngày 13/11/2026. Bản gốc (luôn mới nhất): https://claude.ai/artifact/8dkZtf3x53MEfuL9DZNJUK

## Tổng quan

Đập Thú Vui Nhộn là game đập thú (whack-a-mole) 2.5D cho Android, làm bằng Unity, chơi dọc một tay. Mọi con số trong tài liệu lấy từ bản thiết kế canvas đã chốt; chỗ nào thiết kế chưa quyết thì ghi ở mục cuối.

**Vòng chơi cốt lõi:** chọn map → xem cách chơi 8 giây → đập thú trong thời gian giới hạn → nhận xu theo điểm → tiêu xu ở Cửa hàng (thú, dụng cụ, vật phẩm) → chơi tiếp.

| Hạng mục | Thông số |
| --- | --- |
| Nền tảng | Android trước, màn hình dọc |
| Khung thiết kế | 390 × 844 pt (xuất ảnh @1x/@2x/@3x) |
| Engine | Unity 6 (6000.6.9f1), mẫu Universal 2D · project tại `E:\Projects\WhackyZoo\Game\WhackyZoo` · package `com.thaihieu.whackyzoo` |
| Font | Baloo 2 (Medium 500, Bold 700, ExtraBold 800) — file ở `E:\Projects\WhackyZoo\fonts\static` |
| Số map bản 1.0 | 5: Trên cạn, Dưới biển, Sa mạc, Bắc cực, Nghĩa trang |
| Ngôn ngữ | Tiếng Việt, English |

**Phạm vi bản thử đầu tiên (prototype):** 1 map Trên cạn, đập thú, đếm giờ, điểm, combo, popup Hết giờ. Tài khoản, xếp hạng, quảng cáo và mua bằng tiền thật để sau cùng.

## Luật một ván

Một ván mặc định dài 60 giây trên lưới 9 hố (3 × 3), tối đa 4 con cùng lúc; xu nhận = điểm ÷ 10.

| Thông số | Giá trị |
| --- | --- |
| Thời gian ván | 30 giây (Nhanh) · 60 giây (Thường, mặc định) · 90 giây (Thử thách) |
| Cảnh báo sắp hết giờ | 10 giây cuối: số và thanh giờ chuyển đỏ |
| Màn cách chơi | Hiện mỗi khi vào map, tự đóng sau 8 giây, có nút Bỏ qua; đồng hồ chưa chạy |
| Nhịp sinh thú | Mỗi 1,15 giây |
| Thú rụt xuống | Mỗi nhịp, mỗi con đang ló có 60% khả năng rụt |
| Thú mới | 1–2 con mỗi nhịp vào hố trống, không quá 4 con trên lưới |
| Tỉ lệ ra | 80% thú thường (4 loài chia đều) · 10% thưởng +50 · 10% vật cản −20 |
| Thú bị đập | Choáng 0,43 giây rồi biến mất; không đập lại được |
| Điểm tối thiểu | 0 (không âm) |

**Combo**

- Mỗi lần đập trúng thú hoặc thưởng: combo +1.
- Đập nhầm vật cản: combo về x1 (về x3 nếu đang có Bùa combo); có Khiên thì giữ nguyên.
- Trong bản thiết kế, combo chỉ để hiển thị và tính nhiệm vụ, chưa nhân điểm.

**Hết giờ**

- Popup Hết giờ hiện ngay trên màn chơi: điểm, số thú đã đập, combo cao nhất, xu nhận được.
- Nút xem quảng cáo để nhân đôi xu (1 lần mỗi ván), nút Chơi lại và Về trang chủ.
- Tạm dừng: đồng hồ và thú đứng yên; có Tiếp tục, Chơi lại, Về trang chủ.

## Map và thú

Mỗi map có 4 thú có sẵn, 1 thưởng hiếm và 1 vật cản; map sau cho điểm thú cao hơn map trước.

| Map | Thú có sẵn | Điểm mỗi thú | Thưởng | Vật cản (−20) | Hố |
| --- | --- | --- | --- | --- | --- |
| Trên cạn | Chó, Mèo, Chuột, Heo | 10 (Chuột 15) | Gà vàng +50 | Bom | Đất nâu |
| Dưới biển | Cua, Bạch tuộc, Cá, Sao biển | 12 | Ngọc trai +52 | Cá nóc gai | Cát, viền sỏi |
| Sa mạc | Cáo, Rắn, Thằn lằn, Chồn đất | 14 | Bình vàng +50 | Xương rồng | Cát vàng |
| Bắc cực | Gấu bắc cực, Cánh cụt, Hải cẩu, Cá voi sát thủ | 16 | Cá hồi vàng +50 | Cầu băng gai | Băng |
| Nghĩa trang | Dơi, Mèo đen, Quạ, Nhện | 18 | Đom đóm vàng +50 | Bí ngô ma | Đất mộ |

- Map mới nhất gắn nhãn "MỚI" ở trang Chọn map (hiện là Nghĩa trang); cuối danh sách có thẻ "Map mới · Sắp ra mắt".
- Thú mua thêm chỉ xuất hiện trong đúng map của nó (xem mục Cửa hàng).
- Gợi ý Unity: mỗi map là một ScriptableObject (nền, hố, danh sách thú, thưởng, vật cản, điểm) để thêm map mà không sửa code.

## Vật phẩm hỗ trợ

Có 5 vật phẩm dùng được ở mọi map, chạm vào thanh dưới cùng màn chơi để dùng; mua gói x5 được giảm 20% (trả giá của 4).

| Vật phẩm | Tác dụng | Thời lượng | Giá x1 | Giá x5 |
| --- | --- | --- | --- | --- |
| +5 Giây | Cộng 5 giây vào đồng hồ | Tức thì | 150 xu | 600 xu |
| Nhân Đôi Điểm | Mọi điểm dương x2 | 10 giây | 300 xu | 1.200 xu |
| Khiên Bom | Đỡ 1 lần đập nhầm vật cản: không trừ điểm, giữ combo | Đến khi dùng | 250 xu | 1.000 xu |
| Đóng Băng Giờ | Dừng đồng hồ, thú vẫn ló lên | 5 giây | 350 xu | 1.400 xu |
| Bùa Combo | Combo bắt đầu từ x3 và rơi về x3 thay vì x1 | Cả ván | 500 xu | 2.000 xu |

- Mỗi nút hiển thị số lượng đang có (badge đỏ); hết thì nút mờ đi.
- Nhân Đôi và Đóng Băng không cộng dồn: dùng lại khi đang có hiệu lực thì chỉ làm mới thời lượng (đề xuất, cần chốt).

## Cửa hàng

Cửa hàng có 3 tab: Dụng cụ, Thú, Vật phẩm. Chỉ số chấm theo thang 1–5, hiển thị bằng một thanh (mỗi bậc 20%) kèm chữ mô tả mức.

**Dụng cụ đập** (Sức mạnh · Tầm đập · Tốc độ)

| Dụng cụ | Giá | Sức mạnh | Tầm | Tốc độ | Đặc tính |
| --- | --- | --- | --- | --- | --- |
| Búa Gỗ | Miễn phí (mặc định) | 1 | 1 | 3 | Đập từng ô một |
| Dép Lào | 500 xu | 1 | 1 | 5 | Mỗi cú trúng +2 điểm thưởng |
| Vợt Muỗi | 1.200 xu | 2 | 2 | 4 | Làm choáng cả thú ô bên cạnh |
| Búa Hơi | 2.000 xu | 2 | 2 | 4 | Mỗi ván +3 giây |
| Chảo Gang | 3.500 xu | 4 | 2 | 2 | Thú to cũng gục sau 1 cú |
| Gậy Răng Sói | 4.500 xu | 4 | 1 | 3 | Mỗi cú trúng +3 điểm |
| Chùy | 6.000 xu | 5 | 2 | 1 | Đập trúng thú đặc biệt (gà vàng, ngọc trai…) được x2 điểm |
| Gậy Như Ý | 7.500 xu | 3 | 4 | 4 | Đập trúng cả cột dọc 3 ô (ô bấm + ô trên + ô dưới) (Huyền thoại) |
| Búa Thor | 9.900 xu hoặc 49.000đ | 5 | 3 | 3 | Sét đánh cả hàng ngang 3 ô, điểm x1,5 cả ván (Huyền thoại) |
| Gậy Bóng Chày | 8.000 xu hoặc 39.000đ · miễn phí khi điểm danh ngày 30 | 4 | 2 | 4 | +20% xu mỗi ván (Huyền thoại) |

Gậy Răng Sói, Chùy: giá và đặc tính là đề xuất, cần chốt khi cân bằng. Gậy Như Ý đã chốt (17/11/2026): Huyền thoại, 7.500 xu, chỉ mua bằng xu (chưa có giá tiền thật), tầm đập cột dọc 3 ô. Hình: `images/sprites/tools/tool_wolf_tooth_club`, `tool_mace`, `tool_ruyi_staff`. Thứ tự trong Cửa hàng: Búa Gỗ, Dép Lào, Vợt Muỗi, Búa Hơi, Chảo Gang, Gậy Răng Sói, Chùy, Gậy Như Ý, Búa Thor, Gậy Bóng Chày.

Thang chữ: Sức mạnh Rất nhẹ → Nhẹ → Vừa → Mạnh → Rất mạnh; Tầm đập 1 ô → Hẹp → Vừa → Rộng → Cả hàng; Tốc độ Rất chậm → Chậm → Vừa → Nhanh → Rất nhanh.

**Thú** (Điểm thưởng · Tốc độ ló · Độ hiếm)

Mỗi bộ lọc map liệt kê 4 thú có sẵn (nhãn "Có sẵn", độ hiếm 1) rồi đến thú mua thêm. Thú đã mua bật/tắt bằng nút "Dùng ở <map>" và chỉ ra trong map đó.

| Thú | Map | Giá | Điểm | Tốc độ ló | Độ hiếm |
| --- | --- | --- | --- | --- | --- |
| Thỏ | Trên cạn | 800 xu | +12 | 4 | 1 |
| Vịt | Trên cạn | 1.000 xu | +12 | 3 | 1 |
| Gấu Trúc | Trên cạn | 2.500 xu | +20 (cần 2 cú) | 2 | 3 |
| Capybara | Trên cạn | 6.000 xu · miễn phí khi điểm danh ngày 14 | +25 | 2 | 4 |
| Kỳ Lân | Trên cạn | 9.900 xu hoặc 49.000đ | +40, x2 combo | 5 | 5 |
| Cá Heo | Dưới biển | 1.500 xu | +15 | 4 | 2 |
| Rùa Biển | Dưới biển | 3.000 xu | +22 | 1 | 3 |
| Khủng Long | Sa mạc | 5.000 xu | +25 | 3 | 4 |
| Lạc Đà | Sa mạc | 4.000 xu | +24 | 2 | 4 |
| Cú Tuyết | Bắc cực | 3.500 xu | +22 | 4 | 3 |
| Ma Nhỏ | Nghĩa trang | 4.500 xu | +26 | 5 | 3 |
| Cú Mèo | Nghĩa trang | 3.800 xu | +24 | 3 | 3 |

Thang chữ: Tốc độ ló Rất chậm → Rất nhanh (càng nhanh càng khó đập); Độ hiếm Phổ biến → Thường gặp → Ít gặp → Hiếm → Cực hiếm.

**Vật phẩm:** giá như mục Vật phẩm hỗ trợ; khung chi tiết hiện "Đang có: xN".

## Kinh tế xu

Người chơi kiếm xu chủ yếu từ điểm mỗi ván (điểm ÷ 10); quảng cáo và nhiệm vụ bù thêm, tiền thật chỉ dùng cho 2 món Huyền thoại.

| Nguồn xu | Giá trị |
| --- | --- |
| Kết thúc ván | Điểm ÷ 10, làm tròn xuống (ván 340 điểm = 34 xu) |
| Xem quảng cáo sau ván | x2 xu của ván đó, 1 lần mỗi ván |
| Tích xu miễn phí (Cửa hàng) | +100 xu mỗi quảng cáo, 4 lượt mỗi ngày |
| Nhiệm vụ, thành tựu | 50–3.000 xu (mục Tiến trình) |
| Điểm danh tháng | 100–1.000 xu theo ngày, vật phẩm, thú hiếm Capybara (ngày 14), Gậy Bóng Chày VIP (ngày 30) |
| Đăng ký tài khoản | +500 xu một lần |
| Xếp hạng | Top 3 nhận 3.000 xu, Top 10 nhận 1.000 xu |

**Chi xu:** dụng cụ 500–9.900 xu (Gậy Bóng Chày 8.000), thú 800–9.900 xu, vật phẩm 150–500 xu mỗi cái.

**Mua bằng tiền thật:** Búa Thor và Kỳ Lân, mỗi món 49.000đ (hoặc 9.900 xu); Gậy Bóng Chày 39.000đ (hoặc 8.000 xu). Nút thanh toán màu cam đỏ, tách biệt với nút mua bằng xu màu vàng.

**Ví dụ nhịp kiếm xu:** ván 60 giây được khoảng 300–400 điểm → 30–40 xu, hoặc 60–80 xu nếu xem quảng cáo. Mua Thỏ (800 xu) cần khoảng 10–20 ván; các thú 3.000–5.000 xu cần 40–140 ván, nên cần thử nghiệm lại khi chơi thật.

## Tiến trình

Người chơi tiến bộ qua cấp độ, nhiệm vụ, quà hằng ngày và xếp hạng; công thức kinh nghiệm chưa có trong thiết kế.

**Cấp độ:** Trang chủ hiện avatar, "Lv 7" và thanh kinh nghiệm.

**Nhiệm vụ** — 3 tab Hằng ngày, Hằng tuần, Thành tựu. Nhiệm vụ ngày/tuần cho xu và điểm nhiệm vụ; đủ điểm (thang 100, mốc 60 và 100) thì mở rương. Nhiệm vụ ngày làm mới lúc 00:00.

| Loại | Nhiệm vụ | Xu | Điểm NV |
| --- | --- | --- | --- |
| Ngày | Đạt combo x10 trong một ván | 150 | 20 |
| Ngày | Đập trúng 50 con thú | 100 | 20 |
| Ngày | Đập 3 gà vàng hoặc ngọc trai | 200 | 30 |
| Ngày | Chơi 3 ván bất kỳ | 80 | 20 |
| Ngày | Xem 1 quảng cáo nhân đôi xu | 50 | 10 |
| Tuần | Đập trúng 500 con thú | 800 | 30 |
| Tuần | Đạt combo x25 | 1.000 | 40 |
| Thành tựu | Thợ đập tập sự: đập 1.000 thú | 1.000 | — |
| Thành tựu | Thợ đập lành nghề: đập 5.000 thú | 3.000 | — |
| Thành tựu | Săn vàng: đập 100 gà vàng | 1.500 | — |

**Điểm danh tháng** (thay cho quà 7 ngày) — lịch 30 ngày, mỗi ngày nhận 1 phần quà, nút "NHẬN" màu vàng, có nút xem quảng cáo nhận x2. Bỏ lỡ ngày nào thì không mất lượt và không reset: có thể **điểm danh bù** bằng cách xem quảng cáo, tối đa 3 lần/tháng. Sang tháng mới, lịch quay về Ngày 1. Ô ngày 7, 21, 29 chỉ ghi "Bí ẩn" (icon rương), người chơi mở ra mới biết bên trong. Đầu trang là thẻ quà lớn ngày 30 kèm thanh tiến độ "Đã điểm danh N/30" và 4 mốc 7 · 14 · 21 · 30.

| Ngày | Quà | Ngày | Quà | Ngày | Quà |
| --- | --- | --- | --- | --- | --- |
| 1 | 100 xu | 11 | Bùa Combo x1 | 21 | **Mốc 3:** Rương bí ẩn (bên trong: +5 Giây, Nhân Đôi, Khiên, Đóng Băng, Bùa Combo, mỗi loại x2) |
| 2 | +5 Giây x2 | 12 | 250 xu | 22 | 250 xu |
| 3 | 150 xu | 13 | +5 Giây x3 | 23 | Bùa Combo x2 |
| 4 | Nhân Đôi x1 | 14 | **Mốc 2:** Thú hiếm Capybara (đã có thì đổi 5.000 xu) | 24 | 300 xu |
| 5 | 200 xu | 15 | 200 xu | 25 | +5 Giây x5 |
| 6 | Khiên x2 | 16 | Nhân Đôi x2 | 26 | 400 xu |
| 7 | **Mốc 1:** Rương bí ẩn (bên trong: 1.000 xu) | 17 | 250 xu | 27 | Nhân Đôi x3 |
| 8 | 150 xu | 18 | Khiên x3 | 28 | 500 xu |
| 9 | Đóng Băng x2 | 19 | 300 xu | 29 | Rương bí ẩn (bên trong: mỗi vật phẩm x2) |
| 10 | 200 xu | 20 | Đóng Băng x3 | 30 | **Quà lớn:** Gậy Bóng Chày VIP, dùng miễn phí 7 ngày |

**Gậy Bóng Chày (VIP)** — tạm thời thay cho Búa Vàng. Bán trong Cửa hàng (tab Dụng cụ) giá 8.000 xu hoặc 39.000đ, và điểm danh đủ ngày 30 được dùng miễn phí 7 ngày (hết 7 ngày phải mua để sở hữu vĩnh viễn). Chỉ số Sức mạnh 4 · Tầm 2 · Tốc độ 4, +20% xu mỗi ván. **Capybara** (thú Trên cạn, mắt híp, mặt "chill") bán 6.000 xu, +25 điểm, Tốc độ ló 2, Độ hiếm 4, tặng miễn phí ngày 14. Đã sở hữu trước thì quà ngày 30/14 đổi thành 3.000/5.000 xu (đề xuất, cần chốt). Trong Cửa hàng, hai món có nhãn vàng "Dùng miễn phí 7 ngày khi điểm danh ngày 30" (Gậy Bóng Chày) và "Miễn phí khi điểm danh ngày 14" (Capybara) (bấm mở trang Điểm danh), ẩn khi đã sở hữu. Hình: `images/sprites/tools/tool_baseball_bat`, `images/sprites/characters/animal_shop_capybara`.

**Xếp hạng** — 2 tab Bạn bè và Toàn quốc, bục Top 3, danh sách cuộn, dòng "Bạn" ghim ở dưới kèm số điểm còn thiếu để lên hạng. Thưởng: Top 3 nhận 3.000 xu, Top 10 nhận 1.000 xu.

## Tài khoản và cài đặt

Người chơi có thể chơi ngay không cần tài khoản; tài khoản dùng để lưu tiến trình và vào xếp hạng.

**Tài khoản**

- Đăng nhập: email hoặc số điện thoại + mật khẩu, ghi nhớ đăng nhập, hoặc Google / Apple / Facebook (dùng logo chính thức của hãng).
- Đăng ký: chọn avatar (Chó, Mèo, Chuột, Heo, Cáo), tên hiển thị, email, mật khẩu tối thiểu 8 ký tự, nhập lại mật khẩu, đồng ý điều khoản; tặng 500 xu.
- Quên mật khẩu: 3 bước Email → Mã xác nhận 6 chữ số → Mật khẩu mới.
- Trang Tài khoản: avatar, tên, ID, cấp; thống kê (ván đã chơi, kỷ lục, thú đã đập); liên kết Email, Số điện thoại, Google; đổi mật khẩu; Đăng xuất; Xóa tài khoản (hỏi xác nhận, xóa vĩnh viễn).
- Chỉnh sửa hồ sơ: popup dạng bottom sheet, đổi tên và avatar; avatar chỉ chọn từ thú đã có.

**Cài đặt**

| Mục | Lựa chọn | Mặc định |
| --- | --- | --- |
| Thời gian mỗi ván | 30 / 60 / 90 giây | 60 giây |
| Nhạc nền | 0–100% | 70% |
| Hiệu ứng | 0–100% | 85% |
| Rung khi đập trúng | Bật / Tắt | Bật |
| Ngôn ngữ | Tiếng Việt / English | Tiếng Việt |

## Màn hình và bộ hình

Game có 16 màn hình; toàn bộ ảnh đã xuất vào `E:\Projects\WhackyZoo\images` (324 file PNG).

| Nhóm | Màn hình | Ảnh tham chiếu (`images/screens/`) |
| --- | --- | --- |
| Chính | Trang chủ, Chọn map | 01, 02 |
| Chơi | 5 màn chơi (mỗi map 1 ảnh đang chơi + 1 ảnh cách chơi) | 03, 04, 05, 05b, 05c |
| Kinh tế | Cửa hàng, Nhiệm vụ, Xếp hạng, Điểm danh tháng | 06, 08, 09, 09b |
| Hệ thống | Cài đặt | 07 |
| Tài khoản | Đăng nhập, Đăng ký, Quên mật khẩu, Tài khoản | 10–13 |

**Bộ hình rời (`images/sprites/`, mỗi hình @1x/@2x/@3x, nền trong suốt):**

- `characters`: 20 thú theo map + 11 thú Cửa hàng (khung 100 × 100).
- `bonus_obstacles`: thưởng và vật cản của 5 map.
- `holes`: hố 5 map, tách lớp sau và lớp trước (110 × 76) để thú chui ra giữa.
- `tools`, `boosters`, `ui_icons`, `fx`, `logo`.
- `ui_buttons`: 8 màu nút + khung kem, dùng 9-slice (góc bo 18 px).
- `backgrounds`: nền Trang chủ và 5 map (390 × 844, @3x = 1170 × 2532).

**Quy ước giao diện:** viền nâu đậm #3A2412; nút chính xanh lá hoặc vàng; cam đỏ cho trả tiền thật; danh sách cuộn không hiện thanh cuộn, mép mờ dần.

## Cần chốt và lộ trình

Còn 7 điểm thiết kế chưa quyết; nên chốt trước khi làm phần liên quan, còn bản thử đầu tiên có thể làm ngay.

**Cần chốt**

- [ ] Combo có nhân điểm không (ví dụ từ x5 trở lên +10%), hay chỉ để hiển thị?
- [ ] Công thức kinh nghiệm và lên cấp, lên cấp được gì.
- [ ] Nhiệm vụ tuần còn sót nội dung cũ: "Đạt 3 sao trong 5 ván", "Chơi đủ cả 3 map", "Qua hết 12 màn Trên cạn" — game không còn sao, màn hay 3 map.
- [ ] Tốc độ sinh thú có tăng theo map hoặc theo thời gian còn lại không.
- [ ] Thú mua thêm đang bật chiếm tỉ lệ ra bao nhiêu so với thú có sẵn.
- [ ] Xếp hạng tính theo tuần hay mùa, xếp theo tổng điểm hay điểm cao nhất một ván.
- [ ] Giá xu có cân bằng chưa: cần chơi thử để đo số ván trung bình để mua một thú.

**Lộ trình Unity**

1. Prototype: map Trên cạn, đập thú, đồng hồ, điểm, combo, popup Hết giờ.
2. Dữ liệu map và thú bằng ScriptableObject, thêm 4 map còn lại và màn cách chơi.
3. Trang chủ, Chọn map, Cài đặt; lưu tiến trình trên máy.
4. Xu, Cửa hàng, vật phẩm hỗ trợ.
5. Nhiệm vụ, Quà hằng ngày, cấp độ.
6. Tài khoản và Xếp hạng (Firebase), quảng cáo, mua bằng tiền thật.
