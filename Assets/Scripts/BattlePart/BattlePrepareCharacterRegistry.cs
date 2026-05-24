using System;

/// <summary>
/// 战斗准备阶段「已确认准备」的玩家所锁定的角色（最多 4 人）；未准备前不占位。
/// </summary>
public sealed class BattlePrepareCharacterRegistry
{
    readonly E_Character[] _pickByPlayerIndex = new E_Character[4];

    public void Reset()
    {
        Array.Fill(_pickByPlayerIndex, E_Character.None);
    }

    public E_Character GetPick(byte playerIndex)
    {
        if (playerIndex >= _pickByPlayerIndex.Length)
            return E_Character.None;
        return _pickByPlayerIndex[playerIndex];
    }

    /// <summary>该角色是否可被指定玩家选用（仅统计已准备锁定的占用；未准备前互不影响）。</summary>
    public bool IsAvailable(E_Character character, byte forPlayerIndex)
    {
        if (character == E_Character.None)
            return false;
        if (forPlayerIndex < _pickByPlayerIndex.Length && _pickByPlayerIndex[forPlayerIndex] == character)
            return true;

        for (int i = 0; i < _pickByPlayerIndex.Length; i++)
        {
            if (i == forPlayerIndex)
                continue;
            if (_pickByPlayerIndex[i] == character)
                return false;
        }
        return true;
    }

    public void Release(byte playerIndex)
    {
        if (playerIndex < _pickByPlayerIndex.Length)
            _pickByPlayerIndex[playerIndex] = E_Character.None;
    }

    public bool TryClaim(byte playerIndex, E_Character character)
    {
        if (character == E_Character.None || playerIndex >= _pickByPlayerIndex.Length)
            return false;

        if (_pickByPlayerIndex[playerIndex] == character)
            return true;

        if (!IsAvailable(character, playerIndex))
            return false;

        _pickByPlayerIndex[playerIndex] = character;
        return true;
    }

    public bool TryGetLocker(E_Character character, out byte playerIndex)
    {
        for (int i = 0; i < _pickByPlayerIndex.Length; i++)
        {
            if (_pickByPlayerIndex[i] != character)
                continue;
            playerIndex = (byte)i;
            return true;
        }

        playerIndex = 0;
        return false;
    }

    public bool HasDuplicatePicks()
    {
        for (int i = 0; i < _pickByPlayerIndex.Length; i++)
        {
            E_Character a = _pickByPlayerIndex[i];
            if (a == E_Character.None)
                continue;
            for (int j = i + 1; j < _pickByPlayerIndex.Length; j++)
            {
                if (_pickByPlayerIndex[j] == a)
                    return true;
            }
        }
        return false;
    }
}
