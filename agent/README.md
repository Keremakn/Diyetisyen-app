# Dietitian App Agent

Bu repository Windows uzerinde calisan Dietitian App WhatsApp Web Agent icindir.

Faz 0 console POC basariyla tamamlandi. Faz 1'de bu POC, .NET 8 WPF Desktop Agent MVP'ye tasindi. Bu README, yarin projeye geri donuldugunde nerede kaldigimizi, neyin calistigini, neyin eksik oldugunu ve bir sonraki en mantikli adimlari tek basina anlatacak sekilde guncel tutulmustur.

Onemli guvenlik notu: Gercek danisan gruplariyla test yapilmaz. Manuel testlerde yalnizca sahte test gruplari kullanilir:

- `DYT-TEST-001`
- `DYT-TEST-002`
- `DYT-TEST-003`

## Mevcut Durum

Faz 1 Desktop Agent su an ayakta ve test edilebilir durumda.

Calisan ana ozellikler:

- WPF Desktop uygulamasi aciliyor.
- SQLite verileri kalici olarak saklaniyor.
- WhatsApp Web kalici browser profile ile aciliyor.
- QR ile giris yapildiktan sonra oturum korunuyor.
- WhatsApp grup adi tam eslesmeyle dogrulaniyor.
- Grup yonetimi UI'dan yapilabiliyor.
- Mesaj sablonlari UI'dan yonetilebiliyor.
- Aktif ve dogrulanmis gruplara sirali toplu gonderim yapiliyor.
- Gonderim oncesi acik kullanici onayi aliniyor.
- Her grup sirayla isleniyor, ayni anda birden fazla WhatsApp gonderimi yapilmiyor.
- Gonderim sonucu mesaj balonu/tik davranisi ile dogrulaniyor.
- Gonderim gecmisi batch ve item detaylariyla goruntuleniyor.
- Basarisiz item'lar yeniden denenebiliyor.
- Basarisiz ve iptal edilen item'lar birlikte yeniden denenebiliyor.
- Iptal davranisi duzgun calisiyor: baslamis item sonucu korunuyor, bekleyenler iptal oluyor.
- Uygulama kapanip acilinca veriler korunuyor.
- Uygulama gonderim sirasinda kapanirsa eski `Processing` batch'ler acilista toparlaniyor.
- Log dosyalari artik doluyor.
- Screenshot ve trace artifact'leri hata durumunda uretiliyor.

Son dogrulama:

- Build: basarili
- Unit testler: `17/17` basarili

## Proje Yapisi

```text
agent/
  src/
    DietitianApp.Agent.Domain/
    DietitianApp.Agent.Application/
    DietitianApp.Agent.Infrastructure/
    DietitianApp.Agent.Desktop/
    DietitianApp.Agent.Poc/
  tests/
    DietitianApp.Agent.Application.Tests/
    DietitianApp.Agent.Infrastructure.Tests/
    DietitianApp.Agent.Poc.Tests/
  docs/
    architecture.md
    manual-test-checklist.md
    phase-1-scope.md
    known-risks.md
```

Katmanlar:

- `Domain`: teknoloji bagimsiz entity ve enum'lar.
- `Application`: is kurallari, servis sozlesmeleri, batch gonderim orkestrasyonu.
- `Infrastructure`: Playwright, WhatsApp Web otomasyonu, SQLite, EF Core, path provider, log/artifact altyapisi.
- `Desktop`: WPF, MVVM, kullanici etkilesimi, DI, startup.
- `Poc`: Faz 0 console POC. Henuz silinmedi; calisan referans olarak korunuyor.

Bagimlilik yonu:

- `Desktop -> Application`
- `Infrastructure -> Application`
- `Application -> Domain`
- `Domain` dis kutuphane bilmez.

## Kullanilan Teknolojiler

- .NET 8
- C#
- WPF
- MVVM
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.Playwright
- Entity Framework Core
- SQLite
- Serilog
- xUnit
- FluentAssertions

## Yerel Veri Konumlari

Uygulama kullaniciya ozel mutlak path yazmaz. Tum runtime dosyalari merkezi path provider ile yonetilir.

- SQLite database: `%LOCALAPPDATA%\DietitianApp\agent.db`
- Browser profile: `%LOCALAPPDATA%\DietitianApp\BrowserProfile`
- Log klasoru: `%LOCALAPPDATA%\DietitianApp\Logs`
- Artifact klasoru: `%LOCALAPPDATA%\DietitianApp\Artifacts`
- Screenshot: `%LOCALAPPDATA%\DietitianApp\Artifacts\screenshots`
- Trace: `%LOCALAPPDATA%\DietitianApp\Artifacts\traces`

## Calistirma

Repository kokunden:

```powershell
cd agent
dotnet restore
dotnet build DietitianApp.Agent.sln
dotnet test DietitianApp.Agent.sln
```

Playwright Chromium kurulu degilse:

```powershell
pwsh src/DietitianApp.Agent.Desktop/bin/Debug/net8.0-windows/playwright.ps1 install chromium
```

Desktop uygulamayi calistirma:

```powershell
dotnet run --project src/DietitianApp.Agent.Desktop
```

Derlenmis exe ile calistirma:

```powershell
src/DietitianApp.Agent.Desktop/bin/Debug/net8.0-windows/DietitianApp.Agent.Desktop.exe
```

Not: Makinede .NET 8 Desktop Runtime kurulu olmalidir. Kurulu degilse `.exe` acilmayabilir. Gelistirme sirasinda `.tools/dotnet` portable SDK kullanildi.

## Faz 0'dan Tasinan Davranislar

Faz 0 console POC'de calisan su davranislar korunarak Faz 1'e tasindi:

- WhatsApp Web Playwright ile aciliyor.
- Kalici browser profile kullaniliyor.
- QR girisinden sonra oturum korunuyor.
- Grup tam adla araniyor.
- Benzer isimli ilk sohbet secilmiyor.
- Secilen sohbet basligi hedef grup adiyla tam eslesmeden mesaj yazilmiyor.
- Mesaj composer'a yaziliyor ve dogrulaniyor.
- Gonderim kullanici onayi olmadan baslamiyor.
- Screenshot ve trace hata durumunda uretiliyor.

Faz 1'de ek olarak:

- Tek console akisi yerine WPF Desktop UI geldi.
- Tek grup yerine coklu secim ve batch geldi.
- SQLite gecmis ve kalici grup/sablon yonetimi geldi.
- Iptal, retry, recovery ve batch detaylari geldi.

## WPF Ekranlari

### Dashboard

Basit durum ekrani. Verileri yenileme butonu var.

### Gruplar

Desteklenenler:

- Kayitli gruplari listeleme
- Yeni grup ekleme
- Grup secip duzenleme
- WhatsApp'ta tam eslesmeyle dogrulama
- Secili grubu yeniden dogrulama
- Aktif/pasif degistirme
- Dogrulanmamis/pasif/hazir durumunu gorme

Alanlar:

- `WhatsApp tam grup adi`: WhatsApp'taki birebir grup adi. Gonderimde guvenli arama icin kullanilir.
- `Uygulamada gorunen ad`: UI'da okunabilirlik icin kullanilir. Gonderimde hedef olarak kullanilmaz.

### Mesaj Sablonlari

Desteklenenler:

- Sablonlari listeleme
- Yeni sablon ekleme
- Sablon secip duzenleme
- Aktif/pasif degistirme
- Bos ad veya bos icerik engeli

Yeni gonderim ekraninda yalnizca aktif sablonlar listelenir.

### Yeni Gonderim

Desteklenenler:

- Aktif ve dogrulanmis gruplari secme
- Tumunu sec
- Secimi kaldir
- Aktif mesaj sablonu secme
- Mesaji gonderim oncesi duzenleme
- Secili grup sayisini gorme
- Gonderim oncesi acik onay penceresi
- Sirali gonderim baslatma
- Gonderim sirasinda iptal
- Ilerleme gostergesi

Kurallar:

- Hic grup secilmediyse gonderim baslamaz.
- Mesaj bossa gonderim baslamaz.
- Pasif veya dogrulanmamis grup secilemez.
- Kullanici onayi olmadan WhatsApp'a mesaj gonderilmez.

### Gonderim Gecmisi

Desteklenenler:

- Batch listesi
- Batch durum, toplam, basarili, hatali, iptal sayilari
- Secili batch'in grup bazli item detaylari
- Item durum, deneme sayisi, hata kodu, hata mesaji, screenshot path
- `Basarisizlari Yeniden Dene`
- `Hatali ve Iptal Edilenleri Yeniden Dene`

Retry kurallari:

- `Succeeded` item asla tekrar gonderime alinmaz.
- `Basarisizlari Yeniden Dene` sadece `Failed` item'lari alir.
- `Hatali ve Iptal Edilenleri Yeniden Dene` `Failed` ve `Cancelled` item'lari alir.
- Retry yeni kontrollu batch olarak calisir.

### Ayarlar / WhatsApp

Desteklenenler:

- WhatsApp baglantisini kontrol etme
- QR gerekiyorsa Chromium penceresinde QR girisi
- Kalici profile ile oturum koruma

## Gonderim Davranisi

Batch akisi:

1. Kullanici aktif/dogrulanmis gruplari secer.
2. Mesaj sablonu secilir veya mesaj manuel yazilir.
3. Uygulama grup sayisini, grup adlarini ve mesaj icerigini onay penceresinde gosterir.
4. Kullanici `Evet` derse batch olusur.
5. WhatsApp oturumu kontrol edilir.
6. Gruplar sirayla islenir.
7. Her grup icin sohbet tam adla aranir.
8. Baslik tam eslesmeden mesaj yazilmaz.
9. Mesaj composer'a yazilir.
10. Gonder butonu veya Enter ile gonderim tetiklenir.
11. Yeni mesaj balonu ve tik/hata durumu kontrol edilir.
12. Sonuc SQLite'a yazilir.
13. Siradaki gruba gecilir.

Bir grup basarisiz olursa:

- Hata item'a yazilir.
- Screenshot/trace uretilmeye calisilir.
- Sonraki gruba devam edilir.

Tum batch su durumlarda guvenli durabilir:

- WhatsApp oturumu hazir degilse
- QR gerekiyorsa
- CAPTCHA veya guvenlik dogrulamasi algilanirsa
- Arama kutusu bulunamazsa
- Mesaj kutusu bulunamazsa
- Tarayici acilamazsa

## Iptal Davranisi

Iptal davranisi gercek testle duzeltildi.

Mevcut kural:

- Bir item icin gonderim baslamadan iptal gelirse item `Cancelled` olur.
- Bir item icin gonderim basladiysa, o item'in sonucu alinmaya calisilir.
- Mesaj gittiyse item `Succeeded` kalir.
- Bekleyen item'lar `Cancelled` olur.

Gercek test sonucu:

- Ilk grup islenirken iptal edilince ilk gruba mesaj gitti ve `Succeeded` yazildi.
- Kalanlar `Cancelled` yazildi.

## Uygulama Kapanirsa / Recovery

Faz 1'de uygulama gonderim sirasinda kapatildiginda eski batch'in `Processing` halde askida kalmamasi icin startup recovery eklendi.

Acilista:

- `Processing` batch'ler aranir.
- `Succeeded` item'lara dokunulmaz.
- `Processing` item `Failed / INTERRUPTED` yapilir.
- `Pending` item `Cancelled / INTERRUPTED` yapilir.
- Batch sayaclari tekrar hesaplanir.
- Batch `CompletedWithErrors` veya `Cancelled` olarak kapatilir.
- Loga recovery satiri yazilir.

Gercek test sonucu:

- Ilk mesaj gittikten sonra uygulama kapatildi.
- Tekrar acilista recovery calisti.
- Son batch sonucu: `Success=1`, `Failure=1`, `Cancelled=1`.
- `Basarisizlari Yeniden Dene` failed olan grubu yeniden gonderdi.
- Son eklenen `Hatali ve Iptal Edilenleri Yeniden Dene` butonu cancelled olanlari da yeniden deneyebilir.

## Log ve Artifact

Log konumu:

```text
%LOCALAPPDATA%\DietitianApp\Logs
```

Artifact konumu:

```text
%LOCALAPPDATA%\DietitianApp\Artifacts
```

Loglarda:

- Uygulama baslangici
- Migration/DB islemleri
- Gonderim baslangic ve sonuclari
- Recovery bilgisi
- UI exception detaylari
- Playwright/WhatsApp hata detaylari

Not: Mesaj icerigi loglarda gereksiz yere detayli tutulmamalidir. Su an EF Core SQL loglari parametreleri gizli yaziyor.

## Simdiye Kadar Yapilan Manuel Testler

Gecenler:

- QR ile WhatsApp baglantisi
- Oturumun korunmasi
- Uc test grubunun eklenmesi
- Uc test grubuna toplu hazir mesaj gonderimi
- Gonderim dogrulamasinin hizlanmasi ve false failed probleminin duzelmesi
- Gonderim gecmisinin hatasiz yenilenmesi
- Grup aktif/pasif davranisi
- Sablon ekleme/duzenleme/pasif yapma
- Kapatip acinca grup/sablon/gecmis verilerinin korunmasi
- Iptal davranisi
- Gonderim sirasinda uygulama kapaninca recovery
- Failed item retry

Kismen veya henuz manuel tamamlanmayanlar:

- `Hatali ve Iptal Edilenleri Yeniden Dene` butonunun gercek cancelled item ile manuel testi
- Internet kesilmesi
- WhatsApp oturumunun telefondan dusurulmesi
- Mesaj alani selector'inin bulunamamasi
- Arama alani selector'inin bulunamamasi
- Benzer isimli iki grup testi
- Bir grubu bilincli basarisiz yapip digerlerine devam testi

## Unit Test Durumu

Mevcut testler:

- POC testleri: 4
- Application testleri: 11
- Infrastructure testleri: 2
- Toplam: 17

Kapsanan ana senaryolar:

- Grup secilmeden batch olusmaz.
- Bos mesajla batch olusmaz.
- Dogrulanmamis grup reddedilir.
- Pasif grup reddedilir.
- Bir grup basarisiz olsa bile sonraki grup islenir.
- Iptal baslamis item sonucunu korur, bekleyenleri iptal eder.
- Basarili item retry listesine girmez.
- Failed item retry edilebilir.
- Cancelled item, failed item'larla birlikte retry edilebilir.
- Batch sayaclari dogru hesaplanir.
- Processing batch ikinci kez baslatilamaz.
- Startup recovery interrupted batch'i kapatir.

## Bilinen Eksikler ve Riskler

### WhatsApp Web resmi API degil

WhatsApp Web DOM'u degisirse selector'lar bozulabilir. Bu durumda uygulama yanlis gruba mesaj gondermek yerine hata vermeli, screenshot/trace uretmeli ve durmalidir.

### Kirmizi hata retry davranisi

Faz 0'da kirmizi gonderim hatasi gorulmustu. Faz 1'de hata ikonunu ayri algilama var, fakat WhatsApp popup'inda "tekrar dene / tekrar gonder" butonuna otomatik tiklama henuz tam guclendirilmedi. Oneri: bu kisim manuel artifact ile tekrar test edilip selector merkezi hale getirilmeli.

### Internet kesilmesi

Internet kesilmesi henuz manuel test edilmedi. Beklenen: yanlis gruba mesaj gitmemesi, item/batch durumunun anlamli hata ile kapanmasi.

### Oturum dusmesi

Telefondan bagli cihaz cikisi yapilarak test edilmeli. Beklenen: QR/session durumu UI'da anlasilir hata olarak gorunmeli.

### UI polish

UI fonksiyonel, fakat tasarim hala MVP seviyesinde. Bir sonraki turda:

- Durum badge'leri
- Buton aktif/pasif gorunurlugu
- Daha temiz grid boyutlari
- Detay pencereleri
- Screenshot path tiklanabilir acma

eklenebilir.

## Yarin Icin Onerilen Devam Sirasi

1. `Hatali ve Iptal Edilenleri Yeniden Dene` butonunu gercek yarim batch uzerinde manuel test et.
2. Internet kesilmesi testini yap.
3. WhatsApp oturumunu telefondan dusurup baglanti kontrolu testini yap.
4. Benzer isimli iki test grubu ile tam eslesme guvenligini test et.
5. Kirmizi WhatsApp gonderim hatasi icin artifact yakala ve otomatik "tekrar dene" davranisini guclendir.
6. README ve `docs/manual-test-checklist.md` dosyasindaki test durumlarini isaretle.
7. Faz 1'i daha temiz bir release commit'i olarak etiketlemeyi dusun.

## Git Notu

Bu README guncellendikten sonra amac, mevcut stabil Faz 1 ara noktasini GitHub'a pushlamaktir. `.tools/` klasoru local portable .NET icindir ve commit'e dahil edilmemelidir.
