# Manual Test Checklist

1. Ilk QR girisi
   - `dotnet run --project src/DietitianApp.Agent.Poc` calistirilir.
   - WhatsApp Web acilir ve QR kod telefondan taranir.
   - Oturum acilinca yalnizca girilen test grubuna devam edilir.

2. Oturumun korunmasi
   - Uygulama kapatilir ve tekrar calistirilir.
   - QR istenmeden mevcut persistent profile ile oturum acilmalidir.

3. Test grubuna mesaj gonderimi
   - Yalnizca test amacli bir WhatsApp grubu girilir.
   - Grup adi ve mesaj ekranda dogru gorunur.
   - `EVET` yazildiginda mesaj gonderilir ve loglanir.

4. Var olmayan grup
   - Bilinmeyen bir grup adi girilir.
   - Tam eslesme bulunamadigi icin gonderim durmalidir.

5. Benzer isimli grup
   - Ornegin `Test Grup` ve `Test Grup 2` varsa yalnizca tam ad girildiginde secim yapilmalidir.
   - Benzer ilk sonuc otomatik secilmemelidir.

6. Internet kesilmesi
   - Ag baglantisi kesilerek uygulama calistirilir.
   - Sonsuz bekleme olmamali, hata loglanmali ve gerekiyorsa artefact alinmalidir.

7. Oturum dusmesi
   - WhatsApp Web cihaz oturumu telefondan kapatilir.
   - Uygulama QR/login beklemeli, sure dolarsa gonderim yapmamalidir.

8. Mesaj kutusunun bulunamamasi
   - DOM degisikligi veya selector problemi simule edilir.
   - Mesaj gonderilmemeli, hata sonucu donmelidir.

9. Kullanicinin onay vermemesi
   - Onay ekraninda `EVET` disinda bir deger girilir.
   - Tarayici otomasyonuna gecilmeden islem durmalidir.

10. Ctrl+C ile kapanma
    - Islem sirasinda Ctrl+C basilir.
    - Uygulama 130 cikis kodu ile guvenli kapanmalidir.
