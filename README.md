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

## Quy trình git

- `main`: bản phát hành, luôn chạy được. `dev`: nhánh phát triển, gộp các việc đã xong.
- Không commit thẳng lên `main` hay `dev`.
- Mỗi thay đổi một nhánh tạo từ `dev`: `feat/…`, `fix/…`, `chore/…`, `docs/…`.
- Commit theo [Conventional Commits](https://www.conventionalcommits.org): `feat(shop): thêm Gậy Bóng Chày`.
- Mở pull request vào `dev`, kiểm tra trong Unity rồi merge (squash), xoá nhánh.
- Phát hành: pull request `dev` → `main`, merge kiểu merge commit (không squash).
