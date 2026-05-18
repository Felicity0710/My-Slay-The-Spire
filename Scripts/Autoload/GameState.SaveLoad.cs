using Godot;
using System.Collections.Generic;

// Save/load: serialize the run state to disk so the player can quit and resume.
// The save mirrors NodeEntrySnapshot (used by the in-session "Re-enter node"
// rewind) but also persists the map graph, which the snapshot omits since the
// in-memory map is preserved across re-entries.
public partial class GameState
{
    public bool TryWriteSave(string sceneFilePath)
    {
        var data = BuildSaveData(sceneFilePath);
        SaveSystem.Write(data);
        return true;
    }

    public bool TryLoadSaveAndApply(out string sceneFilePath)
    {
        sceneFilePath = string.Empty;
        var data = SaveSystem.Read();
        if (data == null)
        {
            return false;
        }

        ApplySaveData(data);
        sceneFilePath = string.IsNullOrWhiteSpace(data.SceneFilePath)
            ? "res://Scenes/MapScene.tscn"
            : data.SceneFilePath;
        return true;
    }

    private SaveSystem.SaveData BuildSaveData(string sceneFilePath)
    {
        var data = new SaveSystem.SaveData
        {
            SceneFilePath = string.IsNullOrWhiteSpace(sceneFilePath)
                ? "res://Scenes/MapScene.tscn"
                : sceneFilePath,
            PlayerHp = PlayerHp,
            MaxHp = MaxHp,
            Gold = Gold,
            Floor = Floor,
            Act = Act,
            RunCompleted = RunCompleted,
            MerchantFled = MerchantFled,
            BattlesWon = BattlesWon,
            PotionCharges = PotionCharges,
            RngState = _rng.State.ToString(),
            SelectedDeckPresetId = SelectedDeckPresetId,
            HasCustomDeckOverride = HasCustomDeckOverride,
            CurrentMapRow = CurrentMapRow,
            CurrentMapColumn = CurrentMapColumn,
            PendingMapColumn = PendingMapColumn,
            PendingEncounterType = (int)PendingEncounterType,
            PendingEventId = PendingEventId,
            ShopSnapshotHasData = ShopSnapshotHasData,
            ShopSnapshotRemoveServiceUsed = ShopSnapshotRemoveServiceUsed,
            PendingMerchantFightVictory = PendingMerchantFightVictory
        };

        data.DeckCardIds.AddRange(DeckCardIds);
        data.RelicIds.AddRange(RelicIds);
        data.PotionIds.AddRange(PotionIds);
        data.CustomDeckOverrideIds.AddRange(_customDeckOverrideIds);
        data.PendingRewardOptions.AddRange(PendingRewardOptions);
        data.PendingPotionRewardOptions.AddRange(PendingPotionRewardOptions);
        data.PendingRelicOptions.AddRange(PendingRelicOptions);

        foreach (var entry in ShopSnapshot)
        {
            data.ShopSnapshot.Add(new SaveSystem.ShopEntryDto
            {
                Kind = entry.Kind,
                Id = entry.Id,
                Price = entry.Price,
                Sold = entry.Sold
            });
        }

        foreach (var row in MapLayout)
        {
            var rowInts = new List<int>(row.Count);
            foreach (var nodeType in row)
            {
                rowInts.Add((int)nodeType);
            }
            data.MapLayout.Add(rowInts);
        }

        foreach (var row in MapConnections)
        {
            var rowDto = new List<List<int>>(row.Count);
            foreach (var connections in row)
            {
                rowDto.Add(new List<int>(connections));
            }
            data.MapConnections.Add(rowDto);
        }

        return data;
    }

    private void ApplySaveData(SaveSystem.SaveData data)
    {
        PlayerHp = data.PlayerHp;
        MaxHp = data.MaxHp;
        Gold = data.Gold;
        Floor = data.Floor;
        Act = data.Act;
        RunCompleted = data.RunCompleted;
        MerchantFled = data.MerchantFled;
        BattlesWon = data.BattlesWon;
        PotionCharges = data.PotionCharges;

        if (ulong.TryParse(data.RngState, out var rngState) && rngState != 0)
        {
            _rng = new GameRng(rngState);
        }

        SelectedDeckPresetId = string.IsNullOrWhiteSpace(data.SelectedDeckPresetId)
            ? "starter"
            : data.SelectedDeckPresetId;
        HasCustomDeckOverride = data.HasCustomDeckOverride;
        _customDeckOverrideIds.Clear();
        _customDeckOverrideIds.AddRange(data.CustomDeckOverrideIds);

        CurrentMapRow = data.CurrentMapRow;
        CurrentMapColumn = data.CurrentMapColumn;
        PendingMapColumn = data.PendingMapColumn;
        PendingEncounterType = (MapNodeType)data.PendingEncounterType;
        PendingEventId = data.PendingEventId ?? string.Empty;

        DeckCardIds.Clear();
        DeckCardIds.AddRange(data.DeckCardIds);
        RelicIds.Clear();
        RelicIds.AddRange(data.RelicIds);
        PotionIds.Clear();
        PotionIds.AddRange(data.PotionIds);

        PendingRewardOptions.Clear();
        PendingRewardOptions.AddRange(data.PendingRewardOptions);
        PendingPotionRewardOptions.Clear();
        PendingPotionRewardOptions.AddRange(data.PendingPotionRewardOptions);
        PendingRelicOptions.Clear();
        PendingRelicOptions.AddRange(data.PendingRelicOptions);

        ShopSnapshot.Clear();
        foreach (var entry in data.ShopSnapshot)
        {
            ShopSnapshot.Add(new ShopInventoryEntry
            {
                Kind = entry.Kind,
                Id = entry.Id,
                Price = entry.Price,
                Sold = entry.Sold
            });
        }
        ShopSnapshotHasData = data.ShopSnapshotHasData;
        ShopSnapshotRemoveServiceUsed = data.ShopSnapshotRemoveServiceUsed;
        PendingMerchantFightVictory = data.PendingMerchantFightVictory;

        MapLayout.Clear();
        foreach (var row in data.MapLayout)
        {
            var rowTypes = new List<MapNodeType>(row.Count);
            foreach (var i in row)
            {
                rowTypes.Add((MapNodeType)i);
            }
            MapLayout.Add(rowTypes);
        }

        MapConnections.Clear();
        foreach (var row in data.MapConnections)
        {
            var rowList = new List<List<int>>(row.Count);
            foreach (var conn in row)
            {
                rowList.Add(new List<int>(conn));
            }
            MapConnections.Add(rowList);
        }

        // Re-entering at the start of the load goes back to the map; clear any
        // stale node-entry snapshot since the rewind target is no longer meaningful.
        _nodeEntrySnapshot = null;
    }
}
