using PixTrain.Core.Models;

namespace PixTrain.Core.Repositories
{
    /// <summary>
    /// Oyuncu ilerlemesinin saklandığı yer. Servisler yalnızca bu arayüzü bilir;
    /// Unity tarafında PlayerPrefs/JSON dosyası, ileride bulut kaydı (ASP.NET Core API) ile değiştirilebilir.
    /// </summary>
    public interface IProgressRepository
    {
        /// <summary>Bellekteki güncel ilerleme (ilk çağrıda yüklenir).</summary>
        PlayerProgress Get();

        /// <summary>Güncel ilerlemeyi kalıcı hale getirir.</summary>
        void Save();

        /// <summary>İlerlemeyi verilen kayıtla değiştirir (sıfırlama, buluttan geri yükleme).</summary>
        void Replace(PlayerProgress progress);
    }
}
