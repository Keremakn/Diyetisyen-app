# Dietitian App Agent POC

Faz 0 hedefi: WhatsApp Web uzerinde yalnizca kullanicinin yazdigi tek bir test grubuna, acik terminal onayi alindiktan sonra mesaj gonderebilen Windows/Desktop agent POC.

Bu fazda backend, React, PostgreSQL, Docker, n8n, AI veya Browser Use entegrasyonu yoktur.

## Gereksinimler

- .NET 8 SDK
- Chromium destekli Playwright tarayicilari
- WhatsApp Web kullanabilen bir WhatsApp hesabi

## Kurulum

```bash
cd agent
dotnet restore
dotnet build
```

Playwright tarayicilarini kurmak icin:

```bash
pwsh src/DietitianApp.Agent.Poc/bin/Debug/net8.0/playwright.ps1 install chromium
```

`pwsh` yoksa Microsoft Playwright dokumantasyonundaki isletim sisteminize uygun kurulum komutunu kullanin.

## Calistirma

```bash
cd agent
dotnet run --project src/DietitianApp.Agent.Poc
```

Uygulama terminalden test grup adini ve mesaj icerigini ister. Sonra grup adi ve mesaj tekrar gosterilir. Mesaj yalnizca onay ekraninda tam olarak `EVET` yazarsaniz gonderilir.

## Ilk QR Girisi

Ilk calistirmada Chromium persistent profile ile WhatsApp Web acilir. QR kodu telefonunuzdan tarayin. Oturum bilgisi `src/DietitianApp.Agent.Poc/.profile/whatsapp` altinda saklanir, sonraki calistirmalarda ayni profil kullanilir.

## Test Grubu

Gercek danisan gruplarini kullanmayin. Ayrica olusturulmus, test amacli tek bir WhatsApp grubu kullanin. Uygulama benzer isimli ilk sonucu secmemeye calisir ve sohbet basliginda tam eslesme dogrulanmadan mesaj gondermez.

## Log ve Artefact Konumlari

- Loglar: `agent/logs/`
- Screenshot: `agent/artifacts/screenshots/`
- Playwright trace: `agent/artifacts/traces/`

Hata durumunda screenshot ve trace kaydedilmeye calisilir.

## Bilinen Riskler

- WhatsApp Web DOM yapisi degisebilir; selector'lar `appsettings.json` icinde merkezi tutulur.
- CAPTCHA algilanirsa islem durdurulur.
- Bu POC toplu gonderim yapmaz ve grup listesinden rastgele secim yapmaz.
- Oturum veya internet sorunlarinda sureler `appsettings.json` uzerinden sinirlandirilmistir.
