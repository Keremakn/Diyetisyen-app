# Mimari

Domain dış kütüphane bilmez. Application, toplu gönderim kurallarını ve teknoloji sınırlarını tanımlar. Infrastructure bu sınırları EF Core/SQLite ve Playwright ile uygular. Desktop yalnızca kullanıcı etkileşimi, MVVM, DI, configuration, migration ve startup içerir.

Toplu akış: açık kullanıcı onayı -> batch oluşturma -> tek WhatsApp oturumu -> öğeleri sırayla Processing yapma -> tam sohbet adı doğrulama -> tek mesaj gönderme -> tik doğrulama -> sonucu SQLite'a kaydetme -> sonraki öğe. Bir öğe hatası diğerini durdurmaz. Oturum/CAPTCHA gibi sistemik hata batch'i durdurur. CancellationToken kalan öğeleri Cancelled yapar.

Faz 0 kaynakları silinmedi. `PlaywrightWhatsAppGateway`, kalıcı context, merkezi selector, Unicode normalizasyonu, arama/composer ayrımı, tam başlık eşleşmesi ve aynı mesaj balonundaki tik doğrulamasını taşır.
