# Faz 1 manuel test checklist

Yalnızca `DYT-TEST-001`, `DYT-TEST-002`, `DYT-TEST-003` sahte gruplarını kullanın. Gerçek danışan grubu kullanmayın.

1. İlk bağlantıda QR açılır, tarama sonrası durum hazır olur.
2. Uygulama kapanıp açılınca aynı browser profile ile oturum korunur.
3. `DYT-TEST-001` tam adı doğrulanır ve kaydedilir.
4. Var olmayan grup kaydedilmez.
5. Benzer isimli iki grupta yalnızca tam eşleşme doğrulanır.
6. Üç test grubuna açık onay sonrası sırayla toplu gönderim yapılır.
7. Bir grup başarısızken sonuç kaydedilir ve diğerlerine devam edilir.
8. Gönderim sırasında İptal seçilir; kalanlar Cancelled olur ve UI kontrolü geri gelir.
9. Geçmişte başarısız öğeler yeni kontrollü batch olarak yeniden denenir; başarılılar dahil edilmez.
10. İnternet kesilince yanlış gruba gönderim olmaz, anlaşılır hata ve artifact oluşur.
11. WhatsApp oturumu düşürülünce işlem güvenli durur/QR bekler.
12. Mesaj alanı selector'ı bulunamazsa mesaj yazılmaz ve screenshot/trace oluşur.
13. Arama alanı selector'ı bulunamazsa sohbet seçilmez ve işlem durur.
14. Uygulama kapatılıp yeniden açılır; pencere ve veriler yüklenir.
15. SQLite grup, şablon ve geçmiş kayıtları yeniden açılışta korunur; seed çoğalmaz.
16. Processing sırasında süreç zorla kapatılır; yarım batch yeniden açılışta geçmişte görünür.

Her testte `%LOCALAPPDATA%\DietitianApp\Logs` ile `Artifacts` klasörlerini ve `agent.db` kayıtlarını kontrol edin.
