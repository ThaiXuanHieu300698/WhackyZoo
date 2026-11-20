# Đập Thú Vui Nhộn (WhackyZoo)

Game đập thú (whack-a-mole) 2.5D cho Android, màn hình dọc. Unity 6 (6000.6.9f1), mẫu Universal 2D.

## Cấu trúc

| Đường dẫn | Nội dung |
| --- | --- |
| `WhackyZoo/` | Project Unity (mở bằng Unity Hub) |
| `Docs/GameSpec.md` | Luật chơi, số liệu, giá, map, thú, vật phẩm |
| `Docs/Animations.md` | Thông số chuyển động |
| `Docs/DesignSource/` | Bản thiết kế HTML từng màn |
| `Docs/MusicCredits.md` | Nguồn và giấy phép nhạc nền |
| `Tools/MusicMix/` | Công cụ chỉnh nhạc nền (ngoài Unity) |
| `CLAUDE.md` | Hướng dẫn làm việc cho Claude Code |

## Bắt đầu

1. Cài [Git LFS](https://git-lfs.com) rồi `git lfs install` (ảnh, âm thanh, font lưu bằng LFS).
2. `git clone https://github.com/ThaiXuanHieu300698/WhackyZoo.git`
3. Mở thư mục `WhackyZoo/` bằng Unity Hub (Unity 6000.6.9f1).
4. Scene được dựng bằng code: menu **WhackyZoo › Dựng lại scene Home** / **Dựng lại scene chơi**.

## Quy trình git (Git Flow, không dùng nhánh release)

- `main`: bản phát hành, mỗi bản có tag `vx.y.z`. `dev`: nhánh phát triển.
- Không commit thẳng lên `main` hay `dev`; mọi thay đổi đi qua pull request.
- Việc thường: nhánh `feat/…`, `fix/…`, `chore/…`, `docs/…` tạo từ `dev`, pull request vào `dev`, merge kiểu squash, xoá nhánh.
- Phát hành: tăng phiên bản qua nhánh `chore/bump-x.y.z` vào `dev` → pull request `dev` → `main` (merge commit) → đánh tag `vx.y.z`.
- Sửa gấp bản đã phát hành: nhánh `hotfix/x.y.z` từ `main` → merge vào `main`, đánh tag → merge lại vào `dev`.
- Commit theo [Conventional Commits](https://www.conventionalcommits.org): `feat(shop): thêm Gậy Bóng Chày`.
- Chi tiết: mục "Quy trình git" trong `CLAUDE.md`.
