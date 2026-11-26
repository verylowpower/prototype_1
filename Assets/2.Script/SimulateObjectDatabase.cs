using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SimulateObjectData
{
    public string id;
    public float objectCoefficient;
    public float objectForce;
    public float moveForce;
    public float crossSelectionArea;
    public float mass;
}

[Serializable]
public class SimulateObjectDatabaseWrapper
{
    public List<SimulateObjectData> objects = new();
}

/// <summary>
/// Đọc và cache dữ liệu thông số từ Assets/Resources/SimulateObjectDatabase.json.
/// </summary>
public static class SimulateObjectDatabase
{
    private const string ResourcePath = "SimulateObjectDatabase";
    private static Dictionary<string, SimulateObjectData> _cache;

    public static bool TryGetData(string id, out SimulateObjectData data)
    {
        if (_cache == null)
        {
            LoadDatabase();
        }

        if (string.IsNullOrEmpty(id) || _cache == null)
        {
            data = null;
            return false;
        }

        return _cache.TryGetValue(id, out data);
    }

    public static void LoadDatabase()
    {
        _cache = new Dictionary<string, SimulateObjectData>(StringComparer.OrdinalIgnoreCase);

        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[SimulateObjectDatabase] Không tìm thấy file Resources/{ResourcePath}.json");
            return;
        }

        SimulateObjectDatabaseWrapper wrapper = JsonUtility.FromJson<SimulateObjectDatabaseWrapper>(jsonFile.text);
        if (wrapper?.objects == null) return;

        foreach (SimulateObjectData item in wrapper.objects)
        {
            if (string.IsNullOrEmpty(item.id)) continue;
            _cache[item.id] = item;
        }
    }
}
