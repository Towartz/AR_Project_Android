# BuhenAR

BuhenAR adalah project Unity Android untuk media belajar anak berbasis AR. Materi utama berisi flashcard buah, sayur, dan hewan A-Z. Saat kartu discan, aplikasi menampilkan objek 3D, suara nama objek, info singkat, koleksi item, dan mode Quiz Hunt.

## Fitur Utama

- AR scanner Android memakai Vuforia image target.
- Materi buah/sayur dan hewan A-Z.
- Objek 3D interaktif: rotate, move, dan scale lewat sentuhan.
- Koleksi item yang terbuka setelah flashcard berhasil discan.
- Quiz Hunt berbasis ciri-ciri objek.
- TTS/audio nama objek dan SFX reward saat scan pertama.
- UI responsive untuk portrait dan landscape.

## Struktur Penting

- `Assets/Scenes/` berisi scene utama seperti `SplashScene`, `MainMenuScene`, `MaterialSelectScene`, `ARScanScene`, `CollectionScene`, dan `QuizHuntScene`.
- `Assets/Scripts/` berisi logic AR, navigasi, koleksi, quiz hunt, audio, dan UI.
- `Assets/Resources/` berisi data library, TTS, SFX, font, dan konfigurasi runtime.
- `Assets/Art/` berisi aset UI dan flashcard.
- `Assets/Imported/` berisi model 3D yang dipakai aplikasi.
- `Packages/manifest.json` dan `Packages/packages-lock.json` menyimpan daftar package Unity.
- `ProjectSettings/` menyimpan konfigurasi Unity project.

## Requirement

- Unity `6000.4.1f1`.
- Android Build Support, OpenJDK, SDK, dan NDK.
- Vuforia Engine `11.4.4`.
- Device Android dengan kamera.

## Catatan Dependency Vuforia

File package Vuforia lokal `com.ptc.vuforia.engine-11.4.4.tgz` sengaja tidak dipush karena ukurannya besar dan melewati batas aman GitHub.

Setelah clone, taruh file berikut secara manual:

```text
Packages/com.ptc.vuforia.engine-11.4.4.tgz
```

Jika file belum ada, Unity Package Manager tidak bisa resolve dependency Vuforia dari `Packages/manifest.json`.

## Cara Membuka Project

1. Clone repository.
2. Restore package Vuforia `.tgz` ke folder `Packages/`.
3. Buka project memakai Unity `6000.4.1f1`.
4. Biarkan Unity melakukan import package dan asset.
5. Pastikan scene di Build Settings berurutan dari `SplashScene`, lalu scene UI dan AR lain.
6. Build target ke Android.

## Build Android

APK biasanya dibuat ke:

```text
Builds/Android/TestBuild.apk
```

Folder `Builds/`, `Library/`, `Logs/`, dan cache Unity lain tidak disimpan di repository.

## Dokumentasi Tambahan

Dokumen project tersedia di file:

- `UJIKOM.md`
- `UJIKOM_Guide.md`
- `UJIKOM_Changelog.md`
- `UJIKOM_Project_Info.md`
- `UJIKOM_AZ_Materi.md`
- `UJIKOM_AZ_Database.md`
