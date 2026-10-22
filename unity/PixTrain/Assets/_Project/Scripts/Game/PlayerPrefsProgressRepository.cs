using System;
using PixTrain.Core.Data;
using PixTrain.Core.Models;
using PixTrain.Core.Repositories;
using UnityEngine;

namespace PixTrain.Game
{
    /// <summary>İlerlemeyi cihazda (PlayerPrefs, JSON) saklayan depo. WebGL'de tarayıcı depolamasına yazılır.</summary>
    public sealed class PlayerPrefsProgressRepository : IProgressRepository
    {
        private const string Key = "pixtrain.v1";

        private readonly GameCatalog _catalog;
        private PlayerProgress _progress;

        public PlayerPrefsProgressRepository(GameCatalog catalog)
        {
            _catalog = catalog;
        }

        public PlayerProgress Get()
        {
            if (_progress != null) return _progress;
            string json = PlayerPrefs.GetString(Key, null);
            if (!string.IsNullOrEmpty(json))
            {
                try { _progress = JsonUtility.FromJson<PlayerProgress>(json); }
                catch (Exception e) { Debug.LogWarning("PixTrain: kayıt okunamadı, yeni kayıt açılıyor. " + e.Message); }
            }
            if (_progress == null) _progress = PlayerProgress.CreateDefault(_catalog, DateTime.UtcNow);
            _progress.Normalize(_catalog);
            return _progress;
        }

        public void Save()
        {
            if (_progress == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_progress));
            PlayerPrefs.Save();
        }

        public void Replace(PlayerProgress progress)
        {
            _progress = progress;
            Save();
        }
    }
}
