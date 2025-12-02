public class PlayerManager
{
    public const int MAX_PLAYERS = 4;

    private static CPlayer[] _players = new CPlayer[MAX_PLAYERS];
    private static CPosition[] _positions = new CPosition[MAX_PLAYERS];
    private static bool[] _isActive = new bool[MAX_PLAYERS];

    public static ref CPlayer GetPlayer(int playerId) => ref _players[playerId];
    public static ref CPosition GetPosition(int playerId) => ref _positions[playerId];
    public static bool IsActive(int playerId) => _isActive[playerId];

    public static void CreatePlayer(int playerId, CharacterConfig config)
    {
        // 初始化专用数组
    }
}
