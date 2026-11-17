namespace WhackyZoo
{
    /// <summary>
    /// Trạng thái truyền giữa các scene trong một lần mở game.
    /// Trang Chọn map (bước 3) gán SelectedMap trước khi tải scene Game.
    /// </summary>
    public static class GameSession
    {
        public static MapData SelectedMap;
    }
}
