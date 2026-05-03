# UJIKOM - BuhenAR

Tanggal ringkasan: 28 April 2026

## Ringkasan Singkat

`BuhenAR` adalah aplikasi pembelajaran AR Android untuk anak-anak. Fokus materi saat ini diarahkan ke buah-buahan dan hewan, dengan alur sederhana:

1. Splash screen custom.
2. Main menu.
3. Tutorial / cara main.
4. Masuk ke kamera AR.
5. Scan marker flashcard.
6. Object 3D muncul di atas marker.
7. User bisa melihat info materi dan berinteraksi dengan object.

Project dibuat di Unity 6 dan menggunakan Vuforia sebagai engine AR utama.

## Identitas Project

- Nama aplikasi: `BuhenAR`
- Workspace: `/home/twentyone/AR_UJIKOM_PROJECT`
- Unity Editor: `6000.4.1f1`
- Target platform: `Android`
- AR engine: `Vuforia Engine 11.4.4`
- Render pipeline: `URP`
- Build output utama: `Builds/Android/TestBuild.apk`
- APK terakhir yang tercatat: `Builds/Android/TestBuild.apk`, 101037051 bytes, 27 April 2026 14:28 WIB

Catatan: APK terakhir tersebut dibuat sebelum beberapa perubahan UI terbaru pada 28 April 2026. Jika ingin test perubahan terbaru, build ulang APK dari Unity.

## Status AR Saat Ini

Scanner utama saat ini memakai Vuforia Image Target dari device database:

- Database: `MediaBelajarAR_DB`
- File database: `Assets/StreamingAssets/Vuforia/MediaBelajarAR_DB.xml`
- Target aktif: `apple`
- Ukuran target di XML: `0.750000 x 1.005891`

Konfigurasi `ARScanScene` saat ini:

- `useImportedVuforiaDeviceDatabase = true`
- `strictImportedVuforiaDeviceDatabaseOnly = true`
- `importedVuforiaDeviceDatabaseTargets = apple`
- `enableBarcodeFallback = false`
- `alwaysCreateBarcodeObserverOnMobile = false`
- `forceBarcodeOnlyScanning = false`
- `disableScannerForExperiment = false`
- `showRuntimeTrackingDiagnostics = true`

Artinya, mode utama sekarang adalah scan marker image target `apple`, bukan scan QR/barcode fallback.

## Alur Scene

- `SplashScene`: splash screen custom dengan artwork portrait/landscape.
- `MainMenuScene`: menu utama sederhana.
- `GuideScene`: tutorial / petunjuk penggunaan.
- `MaterialSelectScene`: layar cara main / material guide, tombol lanjut ke kamera AR.
- `ARScanScene`: kamera AR, scanner Vuforia, info panel, dan object AR.
- `AboutScene`: informasi aplikasi.
- `QuizScene` dan `ResultScene`: masih ada di project, tetapi alur utama aplikasi sekarang lebih fokus ke belajar dan scan AR.

## UI dan UX Terbaru

Perubahan UI terbaru:

- Background PNG UI dibuat tidak gepeng menggunakan frame aspect-ratio.
- Artwork portrait dan landscape dipakai sesuai orientasi layar.
- Android orientation diset ke `Auto Rotation`.
- Landscape kanan/kiri aktif.
- Portrait upside-down dimatikan agar layar tidak terbalik 180 derajat.
- `ARScanScene` dibuat responsif:
  - portrait memakai info bottom sheet.
  - landscape memindahkan info ke side panel kanan.
- Text info scanner dibuat auto-fit dan truncate agar tidak berantakan.
- Tombol utama tetap besar untuk penggunaan di HP.

## Konten dan Object

Data konten disimpan sebagai `ScriptableObject` di:

- `Assets/ScriptableObjects/`

Library utama:

- `Assets/Resources/ARtiGrafContentLibrary.asset`

Object 3D utama:

- `Assets/Prefabs/ARObjects/ApplePrefab.prefab`
- `Assets/Imported/Apple/Apple_Model.fbx`

Object demo Blender juga masih ada:

- `blend_apple`
- `blend_cube`
- `blend_roundcube`
- `blend_pyramid`
- `blend_torus`
- `blend_cylinder`

Lokasi model dan prefab demo:

- `Assets/Imported/BlenderDemo/`
- `Assets/Prefabs/BlenderDemo/`

## Asset UI

Artwork UI custom berada di:

- `Assets/Art/UI_APPS/`

File yang digunakan:

- `Splash_portrait.png`
- `Splash_landscape.png`
- `mainmenu_portrait.png`
- `mainmenu_landscape.png`
- `guide_portrait.png`
- `guide_landscape.png`
- `Material_Guide_portrait.png`
- `Material_Guide_Landscape.png`
- `about_Portrait.png`
- `about_Landscape.png`

Desain ini harus dijaga aspect ratio-nya. Jangan stretch full screen jika rasio layar berbeda, karena akan membuat PNG terlihat gepeng.

## Marker dan Flashcard

Marker image target aktif untuk Vuforia:

- Target name: `apple`
- Database: `MediaBelajarAR_DB`
- File lokal Vuforia:
  - `Assets/StreamingAssets/Vuforia/MediaBelajarAR_DB.xml`
  - `Assets/StreamingAssets/Vuforia/MediaBelajarAR_DB.dat`
  - `Assets/Editor/Vuforia/ImageTargetTextures/MediaBelajarAR_DB/apple_scaled.jpg`

Reference image dan marker tambahan:

- `Assets/Art/ReferenceImages/apple.png`
- `Assets/Art/BarcodeMarkers/`
- `Assets/Art/BarcodeTargets/`

Catatan penting untuk Vuforia:

- File target yang diupload ke Vuforia harus sama dengan yang dicetak.
- Jangan pakai PNG RGBA/transparan untuk target upload jika Vuforia menolak `Wrong_Color_Model`.
- Pakai RGB biasa, background solid, dan contrast cukup.
- Jika target dicetak berbeda contrast/warna dari file upload, tracking bisa gagal.
- Rating target di Vuforia minimal sebaiknya 3 bintang atau lebih.

## Audio

Background music sudah ditambahkan:

- `Assets/Audio/the_mountain-happy-playful-kids-music-450659.mp3`

Kontrol musik:

- Script: `Assets/Scripts/Core/BackgroundMusicController.cs`
- Toggle UI: `Assets/Scripts/UI/MusicToggleButton.cs`

## Script Penting

AR:

- `Assets/Scripts/AR/ARImageTrackingController.cs`
- `Assets/Scripts/AR/MaterialContentController.cs`
- `Assets/Scripts/AR/ARTouchInteractionController.cs`
- `Assets/Scripts/AR/ARLearningFeedbackController.cs` ← BARU
- `Assets/Scripts/AR/LinuxEditorDirectCameraFeed.cs`

UI:

- `Assets/Scripts/UI/UIOverlayController.cs`
- `Assets/Scripts/UI/ResponsiveLayoutController.cs`
- `Assets/Scripts/UI/TutorialMotionController.cs`
- `Assets/Scripts/UI/MusicToggleButton.cs`
- `Assets/Scripts/UI/QuizController.cs` ← BARU (gantikan quiz lama jika ada)

Core:

- `Assets/Scripts/Core/AppSession.cs`
- `Assets/Scripts/Core/SceneNavigationController.cs`
- `Assets/Scripts/Core/SplashController.cs`
- `Assets/Scripts/Core/BackgroundMusicController.cs`

Data:

- `Assets/Scripts/Data/MaterialContentData.cs`
- `Assets/Scripts/Data/MaterialContentLibrary.cs`
- `Assets/Scripts/Data/LearningCategory.cs`

Editor tools:

- `Assets/Editor/ARtiGrafProjectBuilder.cs`
- `Assets/Editor/BuildAndroidTest.cs`
- `Assets/Editor/ARtiGrafAndroidBuildWindow.cs`
- `Assets/Editor/ARtiGrafContentDashboardWindow.cs`
- `Assets/Editor/ARtiGrafCreateContentWizardWindow.cs`

## Fitur Pembelajaran Interaktif (Ditambahkan 28 April 2026)

Semua fitur berikut sudah diimplementasi di script. Perlu setup di Unity Editor untuk aktif.

### 1. Suara Nama Objek

Field `nameAudioClip` ditambahkan ke `MaterialContentData`. Isi AudioClip di Inspector asset ScriptableObject konten (misalnya apple.asset). Suara diputar otomatis saat marker terdeteksi via `ARLearningFeedbackController`.

### 2. Animasi Bounce + Fun Fact saat Tap

Saat anak tap objek AR, objek memantul singkat (bounce scale 1.18x). Fun fact muncul 3.5 detik lalu hilang sendiri. Field `funFact` diisi di Inspector ScriptableObject konten.

### 3. Typewriter Deskripsi

Teks deskripsi di info panel muncul satu karakter per waktu. `ARLearningFeedbackController` perlu di-assign ke field `descriptionText` yang sama dengan `UIOverlayController`.

### 4. Tombol Quiz

Tombol "Coba Quiz!" muncul otomatis di ARScanScene setelah marker terdeteksi jika konten memiliki data quiz. Field yang perlu diisi di ScriptableObject: `quizQuestions`, `quizAnswers`, `quizWrongOptions`.

### 5. Sistem Bintang

Bintang diberikan saat anak menjawab minimal 50% soal quiz dengan benar. Data disimpan ke `PlayerPrefs`. Bintang ditampilkan di ARScanScene saat konten yang sama discan ulang.

### 6. QuizController Baru

`Assets/Scripts/UI/QuizController.cs` membaca soal langsung dari `MaterialContentData` konten yang terakhir discan (`AppSession.LastViewedContentId`). Jika data quiz kosong di ScriptableObject, fallback ke soal umum otomatis.

### Setup di Unity Editor

1. Tambahkan `ARLearningFeedbackController` sebagai component di GameObject ARScanScene (misalnya di Canvas atau ARRoot).
2. Assign referensi: `audioSource`, `descriptionText`, `funFactPanel`, `quizButton`, `starIcon`, `navigator`.
3. Di `ARImageTrackingController` Inspector, assign field `learningFeedback` ke component tadi.
4. Buka asset ScriptableObject tiap konten (contoh: `apple.asset`), isi `nameAudioClip`, `funFact`, `quizQuestions`, `quizAnswers`, `quizWrongOptions`.
5. Pasang `QuizController` di GameObject di QuizScene, assign semua referensi UI-nya.

Menu penting:

- `Tools/BuhenAR/Regenerate AR Scene`
- `Tools/BuhenAR/Regenerate UI Artwork Scenes`
- `Tools/BuhenAR/Regenerate Application Scenes`
- `Tools/BuhenAR/Refresh Content Library`
- `Tools/ARtiGraf/Build/Open Android Build Window`
- `Tools/ARtiGraf/Content/Open Dashboard`
- `Tools/ARtiGraf/Dashboard`
- `Tools/ARtiGraf/Sync Dashboard Runtime Setup`

Gunakan regenerate dengan hati-hati karena beberapa scene dibuat ulang dari `ARtiGrafProjectBuilder.cs`.

### Dashboard Tambah Object Cepat

Gunakan `Tools/ARtiGraf/Dashboard` untuk menambah object tanpa banyak edit manual.

1. Import Vuforia database terbaru ke Unity lebih dulu.
2. Buka `Tools/ARtiGraf/Dashboard`.
3. Di panel `Tambah Object Cepat`, pilih `FBX / Prefab 3D`.
4. Pilih `Vuforia Target` dari dropdown. Nama ini dibaca dari `Assets/StreamingAssets/Vuforia/MediaBelajarAR_DB.xml`.
5. Isi `Content ID`, `Nama Tampilan`, `Kategori`, `Object Type`, dan deskripsi.
6. Klik `Create / Update + Sync Runtime`.

Tombol itu otomatis membuat atau update asset di `Assets/ScriptableObjects`, memasukkannya ke `Assets/Resources/ARtiGrafContentLibrary.asset`, menulis target scanner ke `ARScanScene`, lalu menjalankan validasi.

Catatan:

- `Vuforia Target` harus sama dengan target name di dashboard Vuforia.
- Kalau target baru belum muncul di dropdown, download ulang database dari Vuforia lalu import `.unitypackage` ke Unity.
- Untuk mode saat ini, scanner hanya memakai target yang benar-benar ada di Vuforia DB import supaya object lama/demo tidak ikut mengganggu scan.

## Cara Build

Build bisa dilakukan dari Unity Editor atau batch command.

Batch build Android:

```bash
/home/twentyone/Unity/Hub/Editor/6000.4.1f1/Editor/Unity \
  -batchmode \
  -quit \
  -projectPath /home/twentyone/AR_UJIKOM_PROJECT \
  -buildTarget Android \
  -executeMethod BuildAndroidTest.PerformBuild \
  -logFile -
```

Output:

```text
Builds/Android/TestBuild.apk
```

## Masalah yang Pernah Ditemui

- Marker tidak muncul object karena target Vuforia belum sinkron.
- QR/barcode fallback pernah bentrok dengan image target.
- Object apple pernah berubah menjadi cube demo karena mapping konten tidak ketat.
- UI sempat gepeng karena PNG di-stretch.
- Info panel scanner sempat berantakan di landscape.
- Vuforia kadang gagal jika target upload berbeda dengan hasil cetak.
- OOM / hang kamera pernah terjadi saat mode lama terlalu banyak membuat observer/runtime target.

## Status Terbaru

Yang sudah diperbaiki:

- Nama project menjadi `BuhenAR`.
- Main menu dibuat lebih sederhana.
- Splash screen Unity bawaan diganti custom.
- UI artwork portrait/landscape sudah dipasang.
- PNG UI dijaga agar tidak gepeng.
- `ARScanScene` dibuat responsif untuk landscape.
- Info panel scanner dibuat lebih rapi.
- Vuforia DB `MediaBelajarAR_DB` target `apple` sudah masuk ke project.
- Scanner memakai imported Vuforia database, tetapi strict mode dimatikan agar konten library A-Z tetap aman dipakai. Target Vuforia aktif mengikuti DB yang benar-benar ada.

## Update 29 April 2026 - Bank Soal dan DB Vuforia

- DB baru `/home/twentyone/Downloads/MediaBelajarAR_DB.unitypackage` sudah di-import ke project.
- DB Vuforia sekarang berisi target A-J: `apple`, `B_Buaya`, `C_Ceri`, `D_Domba`, `E_Elang`, `F_Flamingo`, `G_Gajah`, `H_Harimau`, `I_Ikan`, `J_Jagung`.
- `ARScanScene` disinkronkan ke target A-J tersebut untuk mengurangi mismatch observer Vuforia.
- Content library tetap lengkap A-Z untuk materi, quiz, koleksi, dan fallback konten.
- Setiap item A-Z sekarang punya 6 soal internal: nama objek, huruf awal, kelompok, warna, ciri utama, dan scan berdasarkan petunjuk ciri.
- `Assets/ScriptableObjects/ARtiGrafQuizBank.asset` sekarang berisi 130 soal global, yaitu 5 soal untuk setiap item A-Z.
- Generator maintenance: `Tools/BuhenAR/Content/Complete A-Z Materials` atau batch method `BuhenARAZMaterialCompleter.CompleteAndSave`.

Yang perlu diuji ulang setelah build berikutnya:

- Apakah marker `apple` hasil cetak berhasil dikenali.
- Apakah object `ApplePrefab` muncul saat marker `apple` discan.
- Apakah orientasi landscape berjalan benar di HP.
- Apakah info panel scanner sudah rapi di portrait dan landscape.
- Apakah audio musik dan mute/unmute berjalan.

## Catatan Presentasi

Penjelasan singkat untuk UJIKOM:

`BuhenAR` adalah aplikasi belajar buah dan hewan untuk anak-anak berbasis Augmented Reality. Anak membuka aplikasi, membaca petunjuk singkat, lalu mengarahkan kamera ke flashcard. Ketika marker dikenali, object 3D muncul di layar, nama objek dieja huruf per huruf (A-P-E-L) lalu dibaca ulang (Apel), dan anak bisa melihat informasi singkat. Fitur tambahan: Koleksi (kumpulkan semua flashcard), Quiz reguler, dan Quiz Hunt (baca ciri-ciri lalu cari flashcard yang cocok). Aplikasi dibuat dengan Unity dan Vuforia untuk Android.

## Fitur Pembelajaran Interaktif (Update 28 April 2026)

### TTS Ejaan + Cara Baca

Script: `Assets/Scripts/Core/TTSController.cs`

Saat marker terdeteksi, sistem memutar AudioClip per huruf (A-P-E-L) dengan jeda antar huruf, lalu memutar AudioClip nama lengkap (Apel). Fallback ke Android TTS native jika AudioClip tidak tersedia. Setup:
- Buat folder `Resources/TTS/Letters/` di Assets.
- Masukkan AudioClip nama file A.mp3, B.mp3, ... Z.mp3.
- Tambahkan `TTSController` sebagai component di scene (satu per scene, DontDestroyOnLoad).
- Isi field `nameAudioClip` di setiap ScriptableObject konten.
- Opsional: isi `spellOverride` jika ejaan berbeda dari judul (contoh: "Anggur" -> "A-N-G-G-U-R").

Cara paling cepat untuk UJIKOM:

- Tidak wajib generate MP3 dulu. `TTSController` sudah bisa memakai native Android TTS fallback.
- Jika `nameAudioClip` kosong dan folder `Resources/TTS/Letters` belum ada, saat di HP Android sistem tetap akan bicara, contoh: `A. Pe. E. El. Apel.`
- Untuk suara custom yang lebih bagus, buka `Tools/BuhenAR/TTS/Open TTS Generator`.
- Klik `Setup Folders` untuk membuat `Assets/Resources/TTS/Letters` dan `Assets/Resources/TTS/Words`.
- Klik `Generate CSV` untuk membuat daftar kata di `Assets/Resources/TTS/tts_word_list.csv`.
- Generate audio dari CSV memakai tool TTS pilihan, lalu taruh file kata di `Assets/Resources/TTS/Words` dengan nama sesuai `id`, contoh `apel.mp3`.
- Klik `Auto Assign Word Clips` agar `nameAudioClip` di ScriptableObject otomatis terisi.

Menu TTS:

- `Tools/BuhenAR/TTS/Open TTS Generator`
- `Tools/ARtiGraf/TTS Generator`

### Button Info Muncul Setelah Scan

Script: `ARLearningFeedbackController.cs`

Button Info disembunyikan di Start(). Muncul pertama kali setelah `OnContentDetected` dipanggil. Assign field `infoButton` dan `infoPanel` di Inspector.

### Koleksi

Script baru: `Assets/Scripts/Collection/CollectionManager.cs`, `CollectionItemUI.cs`, `CollectionDetailPopup.cs`

- CollectionManager adalah singleton DontDestroyOnLoad.
- Item belum ditemukan tampil sebagai siluet abu-abu + tanda tanya.
- Item ditemukan tampil dengan thumbnail berwarna + nama + bintang jika lulus quiz.
- Buat scene `CollectionScene`, tambahkan `CollectionManager` + grid UI.
- Tambahkan `CollectionItemPrefab` dengan component `CollectionItemUI`.

### Quiz Hunt

Script baru: `Assets/Scripts/QuizHunt/QuizHuntController.cs`

Alur: Anak baca ciri-ciri -> tap "Scan Sekarang" -> buka ARScanScene -> scan flashcard -> kembali ke QuizHuntScene -> hasil ditampilkan.
- Buat scene `QuizHuntScene` dan pasang `QuizHuntController`.
- ARImageTrackingController sudah diupdate untuk mengirim hasil scan ke QuizHuntController jika `AppSession.IsQuizHuntActive == true`.
- Tombol hint menampilkan thumbnail dan petunjuk pertama huruf nama.
- Mode `useTimed` opsional untuk level lebih menantang.

### Scene Baru yang Perlu Dibuat di Unity

- `CollectionScene`: tampilkan grid semua konten + CollectionManager.
- `QuizHuntScene`: tampilkan clue UI + QuizHuntController.
