# UJIKOM Project Info

Tanggal pembaruan: 17 April 2026

## Ringkasan Project

- Nama workspace: `AR_UJIKOM_PROJECT`
- Engine utama saat ini: `Unity 6 / 6000.4.1f1`
- Paket AR utama: `Vuforia Engine 11.4.4`
- Build target aktif: `Android`
- APK output terakhir: `Builds/Android/TestBuild.apk`

## Status Implementasi Saat Ini

- Project masih memakai `Vuforia` sebagai engine kamera dan scanner.
- Konfigurasi scene `ARScanScene` saat ini diset ke mode `barcode-only`.
- Artinya:
  - kamera dan scanner Vuforia tetap aktif
  - barcode / QR tetap bisa dibaca
  - `image target tracking` dinonaktifkan sementara agar tidak bentrok dengan hasil barcode
- Flag scene aktif saat ini:
  - `disableScannerForExperiment = 0`
  - `forceBarcodeOnlyScanning = 1`
  - `preferBarcodeTrackingForDemoMarkers = 1`
  - `anchorBarcodeContentToMarker = 1`

## Tujuan Mode Saat Ini

Mode sekarang dibuat untuk mengurangi kasus:

- marker `apple` terbaca sebagai `cube`
- object tidak muncul walau barcode sudah dibaca
- hasil barcode tertimpa oleh image target yang salah

Dengan mode ini, alur yang diharapkan adalah:

1. kamera Vuforia aktif
2. barcode dibaca
3. payload barcode dipetakan ke konten
4. object muncul berdasarkan barcode, bukan image target

## Build Terakhir

- File: `Builds/Android/TestBuild.apk`
- Timestamp build terakhir: `2026-04-17 14:31:27 +0700`
- Ukuran file: `67,246,449 bytes`
- Status install terakhir ke device Android: `Success`

## Marker Barcode Aktif

Desain marker aktif saat ini sudah dikembalikan ke versi lama dari backup folder:

- `blend_apple_marker.png`
- `blend_cube_marker.png`
- `blend_cylinder_marker.png`
- `blend_pyramid_marker.png`
- `blend_roundcube_marker.png`
- `blend_torus_marker.png`

Lokasi marker project:

- `Assets/Art/BarcodeMarkers/`

## File Penting Untuk Diedit

### Konfigurasi scan / tracking

- `Assets/Scripts/AR/ARImageTrackingController.cs`
- `Assets/Scenes/ARScanScene.unity`

### Konfigurasi Vuforia

- `Assets/Resources/VuforiaConfiguration.asset`

### Marker barcode

- `Assets/Art/BarcodeMarkers/`

### Library konten / mapping object

- `Assets/ScriptableObjects/`
- `Assets/Prefabs/`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/Data/MaterialContentLibrary.cs`

## Catatan Penting

- License key Vuforia sudah terpasang di asset konfigurasi.
- Karena engine Vuforia masih aktif, watermark Vuforia masih mungkin muncul tergantung status lisensi runtime.
- Jika ingin kembali ke mode Vuforia penuh dengan image target aktif, ubah:
  - `forceBarcodeOnlyScanning` menjadi `0`
- Jika ingin benar-benar eksperimen tanpa scanner, gunakan:
  - `disableScannerForExperiment = 1`
  - namun mode itu saat ini tidak aktif

## Arah Pengembangan Yang Sudah Dibahas

- Tetap memakai Vuforia untuk scan barcode
- Memisahkan barcode scan dari image target tracking
- Opsi masa depan:
  - scanner non-Vuforia untuk barcode saja
  - marker tracking tanpa Vuforia memakai solusi lain seperti AprilTag / ArUco

## Status Praktis Saat Ini

Kalau ingin mencoba `Vuforia dulu`, kondisi project sekarang adalah:

- masih berbasis Vuforia
- scanner masih aktif
- konflik image target sudah dikurangi
- fokus pengujian sekarang sebaiknya pada:
  - apakah barcode terdeteksi
  - apakah object muncul sesuai payload
  - apakah object yang muncul sudah benar dan tidak mismatch
