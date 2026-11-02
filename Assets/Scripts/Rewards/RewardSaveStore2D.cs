using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Atomic replace; failed writes never replace the in-memory profile or a valid previous save.
public sealed class RewardSaveStore2D
{
    [Serializable] private sealed class Envelope
    { public int version; public string payload; public string sha256; public Envelope() { } }
    private readonly string path;
    private RewardProfile2D data;
    public string Error { get; private set; }
    public bool ReadOnly { get; private set; }
    public RewardSaveStore2D(string filePath) { path = filePath; Load(); }
    public RewardProfile2D Snapshot() => Clone(data);
    public RewardProfile2D GrowthSnapshot(PermanentGrowthData2D growth)
    {
        var snapshot = Snapshot();
        if (growth != null && growth.Validate(out _)) PermanentGrowthRules2D.Normalize(snapshot, growth);
        return snapshot;
    }

    private void Load()
    {
        data = RewardProfile2D.Empty();
        if (!File.Exists(path) && !File.Exists(path + ".bak"))
            return;
        try { data = Read(path); }
        catch (Exception error)
        {
            ReadOnly = true;
            Error = "Save unreadable; original files preserved: " + error.Message;
            try { data = Read(path + ".bak"); Error += " (backup displayed, writes disabled)"; }
            catch { /* Never replace unreadable files with an empty profile. */ }
        }
    }

    public bool TryCommit(string runId, IList<RewardRecord2D> rewards)
    {
        if (ReadOnly) return false;
        try
        {
            if (string.IsNullOrEmpty(runId) || rewards == null) throw new InvalidDataException("Invalid settlement.");
            RewardProfile2D baseline = File.Exists(path) ? Read(path) : data;
            if (baseline.settledRunIds.Contains(runId)) { data = baseline; Error = null; return true; }
            RewardProfile2D next = Clone(baseline);
            next.lastMagicStones = 0;
            var weaponIds = new HashSet<string>();
            foreach (WeaponSaveData2D weapon in next.weapons) weaponIds.Add(weapon.instanceId);
            var rewardIds = new HashSet<string>();
            foreach (RewardRecord2D record in rewards)
            {
                if (record == null || !record.CanExport || string.IsNullOrEmpty(record.rewardId) ||
                    record.quantity < 0 || (record.quantity == 0 && record.kind != RewardKind2D.MagicStone) || !rewardIds.Add(record.rewardId))
                    throw new InvalidDataException("Invalid or duplicate export reward.");
                if (record.kind == RewardKind2D.Currency)
                    next.currency = checked(next.currency + record.quantity);
                else if (record.kind == RewardKind2D.Weapon)
                {
                    if (record.quantity != 1 || record.weapon == null || !record.weapon.IsValid ||
                        record.weapon.source != record.source || !record.weapon.exportable ||
                        !weaponIds.Add(record.weapon.instanceId)) throw new InvalidDataException("Invalid or duplicate weapon.");
                    next.weapons.Add(JsonUtility.FromJson<WeaponSaveData2D>(JsonUtility.ToJson(record.weapon)));
                }
                else if (record.kind == RewardKind2D.MagicStone)
                {
                    if (record.currencyId != MagicStoneRules2D.CurrencyId || string.IsNullOrWhiteSpace(record.bossId) ||
                        record.firstKillAmount < 0 || record.repeatKillAmount < 0)
                        throw new InvalidDataException("Invalid boss stone reward.");
                    int amount = MagicStoneRules2D.Award(next, record);
                    next.magicStones = checked(next.magicStones + amount);
                    next.lastMagicStones = checked(next.lastMagicStones + amount);
                }
                else throw new InvalidDataException("Temporary rewards cannot be saved.");
            }
            next.settledRunIds.Add(runId);
            return Write(next);
        }
        catch (Exception error) { Error = "Rewards not saved: " + error.Message; return false; }
    }

    public bool TrySelectWeapon(string instanceId)
    {
        if (ReadOnly) return false;
        try
        {
            var next = Clone(File.Exists(path) ? Read(path) : data);
            if (!string.IsNullOrEmpty(instanceId) && !next.weapons.Exists(item => item.instanceId == instanceId))
            { Error = "Weapon is not stored in your inventory."; return false; }
            next.equippedWeaponInstanceId = string.IsNullOrEmpty(instanceId) ? null : instanceId;
            return Write(next);
        }
        catch (Exception error) { Error = "Weapon selection not saved: " + error.Message; return false; }
    }

    public bool TrySelectMagic(string magicId, PermanentGrowthData2D growth = null)
    {
        if (ReadOnly) return false;
        try
        {
            RewardProfile2D next = Clone(File.Exists(path) ? Read(path) : data);
            MagicOwnership2D.Normalize(next);
            if (growth != null)
            {
                if (!growth.Validate(out _)) { Error = "Invalid growth data."; return false; }
                PermanentGrowthRules2D.Normalize(next, growth);
            }
            if (string.IsNullOrWhiteSpace(magicId) || !next.ownedMagicIds.Contains(magicId))
            { Error = "Magic is not owned."; return false; }
            next.equippedMagicId = magicId;
            return Write(next);
        }
        catch (Exception error) { Error = "Magic selection not saved: " + error.Message; return false; }
    }

    public bool TryUnlockMagics(IList<string> ids)
    {
        if (ReadOnly || ids == null) return false;
        try
        {
            RewardProfile2D next = Clone(File.Exists(path) ? Read(path) : data);
            MagicOwnership2D.Normalize(next);
            foreach (string id in ids)
                if (!string.IsNullOrWhiteSpace(id) && !next.ownedMagicIds.Contains(id)) next.ownedMagicIds.Add(id);
            return Write(next);
        }
        catch (Exception error) { Error = "Magic unlocks not saved: " + error.Message; return false; }
    }

    public bool TryPurchaseNode(PermanentGrowthData2D growth, string nodeId)
    {
        if (ReadOnly) return false;
        try
        {
            if (growth == null || !growth.Validate(out _)) { Error = "Invalid tech tree data."; return false; }
            var node = growth.FindNode(nodeId);
            if (node == null) { Error = "Unknown tech node."; return false; }
            var next = Clone(File.Exists(path) ? Read(path) : data);
            PermanentGrowthRules2D.Normalize(next, growth);
            var state = PermanentGrowthRules2D.NodeState(next, node);
            if (state != MagicNodeState2D.Available)
            { Error = state == MagicNodeState2D.Unlocked ? "Already unlocked." : "Prerequisites not unlocked."; return false; }
            if (next.magicStones < node.cost) { Error = "Not enough Magic Stones."; return false; }
            next.magicStones -= node.cost;
            next.unlockedTechNodeIds.Add(node.nodeId);
            if (!next.ownedMagicIds.Contains(node.magicId)) next.ownedMagicIds.Add(node.magicId);
            return Write(next);
        }
        catch (Exception error) { Error = "Unlock not saved: " + error.Message; return false; }
    }

    public bool TryPurchaseUpgrade(PermanentGrowthData2D growth, PermanentUpgradeKind2D kind, int expectedLevel)
    {
        if (ReadOnly) return false;
        try
        {
            if (growth == null || !growth.Validate(out _)) { Error = "Invalid growth data."; return false; }
            var upgrade = growth.FindUpgrade(kind);
            if (upgrade == null) { Error = "Unknown upgrade."; return false; }
            var next = Clone(File.Exists(path) ? Read(path) : data);
            PermanentGrowthRules2D.Normalize(next, growth);
            int level = PermanentGrowthRules2D.Level(next, kind);
            if (level != expectedLevel) { Error = "Upgrade level changed; refresh before buying again."; return false; }
            if (level >= upgrade.MaxLevel) { Error = "Upgrade is MAX."; return false; }
            int cost = upgrade.costs[level];
            if (next.magicStones < cost) { Error = "Not enough Magic Stones."; return false; }
            next.magicStones -= cost;
            PermanentGrowthRules2D.SetLevel(next, kind, level + 1);
            return Write(next);
        }
        catch (Exception error) { Error = "Upgrade not saved: " + error.Message; return false; }
    }

    private bool Write(RewardProfile2D next)
    {
        string temporary = null;
        try
        {
            MagicOwnership2D.Normalize(next);
            string payload = JsonUtility.ToJson(next);
            string json = JsonUtility.ToJson(new Envelope { version = 1, payload = payload, sha256 = Hash(payload) }, true);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            // Validate the completed temporary file before touching the previous save.
            Read(temporary);
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
            else File.Move(temporary, path);
            temporary = null;
            data = next;
            Error = null;
            return true;
        }
        catch (Exception error) { Error = "Rewards not saved: " + error.Message; return false; }
        finally
        {
            if (temporary != null)
                try { File.Delete(temporary); } catch { /* Only our disposable temp file. */ }
        }
    }

    private static string Hash(string payload)
    {
        using (SHA256 hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
    }
    private static RewardProfile2D Clone(RewardProfile2D source) => JsonUtility.FromJson<RewardProfile2D>(JsonUtility.ToJson(source));
    private static RewardProfile2D Read(string file)
    {
        Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(file, Encoding.UTF8));
        if (envelope == null || envelope.version != 1 || envelope.payload == null ||
            !string.Equals(envelope.sha256, Hash(envelope.payload), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid save envelope/checksum.");
        RewardProfile2D profile = JsonUtility.FromJson<RewardProfile2D>(envelope.payload);
        if (profile == null || profile.version != 1 || profile.currency < 0 || profile.magicStones < 0 || profile.lastMagicStones < 0 ||
            profile.healthUpgradeLevel < 0 || profile.manaUpgradeLevel < 0 || profile.meleeUpgradeLevel < 0 ||
            profile.weapons == null || profile.settledRunIds == null)
            throw new InvalidDataException("Invalid reward profile.");
        var ids = new HashSet<string>();
        foreach (WeaponSaveData2D weapon in profile.weapons)
            if (weapon == null || !weapon.IsValid || !weapon.exportable ||
                !RewardRules2D.CanExport(RewardKind2D.Weapon, weapon.source) || !ids.Add(weapon.instanceId))
                throw new InvalidDataException("Invalid saved weapons.");
        ids.Clear();
        foreach (string id in profile.settledRunIds)
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new InvalidDataException("Invalid run ledger.");
        if (profile.defeatedBossIds == null) profile.defeatedBossIds = new List<string>();
        ids.Clear();
        foreach (string id in profile.defeatedBossIds)
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("Invalid boss ledger.");
        if (profile.unlockedTechNodeIds == null) profile.unlockedTechNodeIds = new List<string>();
        ids.Clear();
        foreach (string id in profile.unlockedTechNodeIds)
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("Invalid tech node ledger.");
        MagicOwnership2D.Normalize(profile);
        return profile;
    }
}
